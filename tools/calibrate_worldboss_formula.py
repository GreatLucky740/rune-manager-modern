"""Fit global World Boss stat weights against a real 60-monster game order."""
from collections import defaultdict
import json
import math
from pathlib import Path
import sys

import numpy as np


REAL_MASTER_ORDER = [
    14511,20511,21111,14411,24511,15711,26111,17411,28911,25611,
    21211,29311,14611,20511,22611,25311,25711,34411,18611,32811,
    27911,16611,31311,13811,13811,19711,25211,35611,26811,18911,
    16811,14513,33411,21511,32911,19211,34911,17011,21811,16911,
    17911,18411,28211,33211,19911,21411,19411,13411,30711,29211,
    23111,11911,18811,25011,11211,24911,15511,13911,22711,28611,
]

STAT_NAMES = ["HP", "ATK", "DEF", "SPD", "CR", "CD", "RES", "ACC"]
STAT_IDS = {1: 0, 2: 0, 3: 1, 4: 1, 5: 2, 6: 2, 8: 3, 9: 4, 10: 5, 11: 6, 12: 7}
PERCENT = {2, 4, 6}


def unit_vector(unit):
    base = np.array([unit.get("con", 0) * 15, unit.get("atk", 0), unit.get("def", 0),
                     unit.get("spd", 0), unit.get("critical_rate", 0), unit.get("critical_damage", 0),
                     unit.get("resist", 0), unit.get("accuracy", 0)], dtype=float)
    bonus = np.zeros(8)
    runes = unit.get("runes") or []
    for rune in runes:
        effects = [rune.get("pri_eff", []), rune.get("prefix_eff", [])] + (rune.get("sec_eff") or [])
        for effect in effects:
            if len(effect) < 2 or effect[0] not in STAT_IDS:
                continue
            stat, value = int(effect[0]), float(effect[1])
            if len(effect) > 3:
                value += float(effect[3])
            index = STAT_IDS[stat]
            bonus[index] += base[index] * value / 100.0 if stat in PERCENT else value
    for artifact in unit.get("artifacts") or []:
        effect = artifact.get("pri_effect") or artifact.get("pri_eff") or []
        if len(effect) >= 2 and 100 <= int(effect[0]) <= 102:
            bonus[int(effect[0]) - 100] += float(effect[1])
    skills = unit.get("skills") or []
    skillups = sum(max(0, int(skill[1]) - 1) for skill in skills if len(skill) > 1)
    rune_levels = sum(float(rune.get("upgrade_curr", 0)) for rune in runes) / 90.0
    rune_grades = sum(float(rune.get("class", 0)) for rune in runes) / 36.0
    artifacts = unit.get("artifacts") or []
    artifact_levels = sum(float(a.get("level", a.get("upgrade_curr", 0))) for a in artifacts) / 30.0
    artifact_quality = 0.0
    for artifact in artifacts:
        for effect in artifact.get("sec_effects", artifact.get("sec_eff", [])) or []:
            if len(effect) > 1: artifact_quality += abs(float(effect[1]))
    artifact_quality /= 100.0
    water = 1.0 if int(unit.get("attribute", 0)) == 1 else (-1.0 if int(unit.get("attribute", 0)) == 2 else 0.0)
    # Scale each stat to comparable units; base and equipment remain separate so
    # calibration can discover whether the game values them differently.
    scales = np.array([15000, 1000, 1000, 100, 100, 100, 100, 100], dtype=float)
    total_scaled = (base + bonus) / scales
    skill_ratio = skillups / 12.0
    return np.concatenate((base / scales, bonus / scales,
                           [skill_ratio, rune_levels, rune_grades,
                            artifact_levels, artifact_quality, water],
                           np.sqrt(np.maximum(total_scaled, 0)), total_scaled ** 2,
                           total_scaled * skill_ratio))


def rank_positions(scores):
    order = np.argsort(-scores, kind="stable")
    positions = np.empty(len(scores), dtype=int)
    positions[order] = np.arange(1, len(scores) + 1)
    return positions


def fit_pairwise(features, iterations=40000):
    pairs = np.array([features[i] - features[j] for i in range(60) for j in range(i + 1, 60)])
    weights = np.ones(features.shape[1])
    for step in range(iterations):
        margins = np.clip(pairs @ weights, -30, 30)
        gradient = -(pairs.T @ (1.0 / (1.0 + np.exp(margins)))) / len(pairs) + 0.0005 * weights
        rate = 0.12 / math.sqrt(1 + step / 1000)
        weights = np.maximum(0, weights - rate * gradient)
    return weights


def rank_loss(features, weights, reference=None):
    positions = rank_positions(features @ weights)
    errors = np.abs(positions - np.arange(1, 61))
    regularity = 0 if reference is None else 0.35 * float(np.mean(np.log((weights + 0.01) / (reference + 0.01)) ** 2))
    return float(np.mean(errors) + 0.35 * np.percentile(errors, 90) + 0.20 * np.max(errors) + regularity), positions


def refine_rank(features, initial, iterations=250000, seed=9662):
    rng = np.random.default_rng(seed)
    floor = max(1e-4, np.median(initial[initial > 0]) * 0.04)
    current = np.maximum(initial, floor)
    current /= np.linalg.norm(current)
    reference = current.copy()
    current_loss, _ = rank_loss(features, current, reference)
    best = current.copy(); best_loss = current_loss
    temperature = 0.08
    for step in range(iterations):
        proposal = current.copy()
        count = int(rng.integers(1, min(7, len(proposal)) + 1))
        indices = rng.choice(len(proposal), count, replace=False)
        proposal[indices] *= np.exp(rng.normal(0, 0.35, count))
        if rng.random() < 0.15:
            proposal[int(rng.integers(len(proposal)))] += rng.random() * 0.15
        proposal = np.maximum(proposal, floor / max(1e-9, np.linalg.norm(initial)))
        proposal /= max(1e-9, np.linalg.norm(proposal))
        loss, _ = rank_loss(features, proposal, reference)
        cooling = max(0.002, temperature * (1.0 - step / iterations))
        if loss <= current_loss or rng.random() < math.exp((current_loss - loss) / cooling):
            current, current_loss = proposal, loss
        if loss < best_loss:
            best, best_loss = proposal.copy(), loss
    return best


def main(json_path, plan_path):
    root = json.loads(Path(json_path).read_text(encoding="utf-8-sig"))
    plan = json.loads(Path(plan_path).read_text(encoding="utf-8-sig"))
    plan_score = {int(row["UnitId"]): float(row.get("CurrentScore", 0)) for row in plan.get("Rows", [])}
    candidates = defaultdict(list)
    allowed_ids = set(plan_score)
    for unit in root.get("unit_list", []):
        if int(unit.get("unit_id", 0)) in allowed_ids:
            candidates[int(unit.get("unit_master_id", 0))].append(unit)
    for group in candidates.values():
        group.sort(key=lambda u: plan_score.get(int(u["unit_id"]), 0), reverse=True)
    chosen = []
    used = defaultdict(int)
    for master in REAL_MASTER_ORDER:
        index = used[master]
        if master not in candidates or index >= len(candidates[master]):
            raise SystemExit(f"missing master {master} occurrence {index + 1}")
        chosen.append(candidates[master][index]); used[master] += 1
    x = np.stack([unit_vector(unit) for unit in chosen])
    game_rank_by_unit = {int(unit["unit_id"]): rank for rank, unit in enumerate(chosen, 1)}
    app_order = sorted(chosen, key=lambda unit: plan_score[int(unit["unit_id"])], reverse=True)
    app_positions = {int(unit["unit_id"]): rank for rank, unit in enumerate(app_order, 1)}
    app_mae = np.mean([abs(app_positions[unit_id] - game_rank) for unit_id, game_rank in game_rank_by_unit.items()])
    print(f"CURRENT_APP_MAE={app_mae:.3f}")
    classic_weights = np.zeros(x.shape[1])
    # Published reverse-engineered baseline: HP/15, ATK/DEF, four points for
    # every other displayed stat, about 80 points per skill-up, then element.
    classic_weights[:8] = [1000, 1000, 1000, 400, 400, 400, 400, 400]
    classic_weights[8:16] = classic_weights[:8]
    classic_weights[16] = 960
    classic_raw = x @ classic_weights
    classic_raw *= np.where(x[:, 21] > 0, 1.10, np.where(x[:, 21] < 0, 0.90, 1.0))
    classic_positions = rank_positions(classic_raw)
    print(f"CLASSIC_MAE={np.mean(np.abs(classic_positions - np.arange(1, 61))):.3f}")
    unified_x = np.concatenate((x[:, :8] + x[:, 8:16], x[:, 16:22]), axis=1)
    unified_weights = fit_pairwise(unified_x)
    unified_positions = rank_positions(unified_x @ unified_weights)
    print(f"UNIFIED_MAE={np.mean(np.abs(unified_positions - np.arange(1, 61))):.3f}")
    print("UNIFIED_STATS=" + ",".join(f"{name}:{value:.6f}" for name, value in zip(STAT_NAMES, unified_weights[:8])))
    print("UNIFIED_EXTRAS=" + ",".join(f"{name}:{value:.6f}" for name, value in zip(["SKILL","RUNE_LEVEL","RUNE_GRADE","ARTIFACT_LEVEL","ARTIFACT_QUALITY","ELEMENT"], unified_weights[8:])))
    # Pairwise logistic ranking: every earlier game position must score above
    # every later position. Project weights to positive values after each step.
    weights = fit_pairwise(x, 30000)
    weights = refine_rank(x, weights)
    scores = x @ weights
    positions = rank_positions(scores)
    mae = float(np.mean(np.abs(positions - np.arange(1, 61))))
    max_error = int(np.max(np.abs(positions - np.arange(1, 61))))
    exact = int(np.sum(positions == np.arange(1, 61)))
    print(f"MAE={mae:.3f} MAX_ERROR={max_error} EXACT={exact}/60")
    for name, base_weight, bonus_weight in zip(STAT_NAMES, weights[:8], weights[8:16]):
        print(f"{name}\tBASE={base_weight:.6f}\tEQUIP={bonus_weight:.6f}")
    for name, weight in zip(["SKILL", "RUNE_LEVEL", "RUNE_GRADE", "ARTIFACT_LEVEL", "ARTIFACT_QUALITY", "ELEMENT"], weights[16:]):
        print(f"{name}={weight:.6f}")
    print("NONLINEAR_SQRT=" + ",".join(f"{name}:{value:.6f}" for name, value in zip(STAT_NAMES, weights[22:30])))
    print("NONLINEAR_SQUARE=" + ",".join(f"{name}:{value:.6f}" for name, value in zip(STAT_NAMES, weights[30:38])))
    print("SKILL_INTERACTION=" + ",".join(f"{name}:{value:.6f}" for name, value in zip(STAT_NAMES, weights[38:46])))
    for expected, (unit, actual) in enumerate(zip(chosen, positions), 1):
        if abs(actual - expected) >= 3:
            print(f"MISS expected={expected:02d} calculated={actual:02d} master={unit['unit_master_id']} unit={unit['unit_id']}")


if __name__ == "__main__":
    if len(sys.argv) != 3:
        raise SystemExit("usage: calibrate_worldboss_formula.py export.json worldboss-plan.json")
    main(sys.argv[1], sys.argv[2])
