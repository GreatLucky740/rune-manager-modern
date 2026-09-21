import urllib.request
from pathlib import Path

base = "https://sw-forge.ru/assets/artifacts/"
out = Path("outputs/artifact_manager_app/assets/artifacts")
out.mkdir(parents=True, exist_ok=True)
for name in ("attack", "defense", "hp", "support", "water", "fire", "wind", "light", "dark"):
    request = urllib.request.Request(base + name + ".png", headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(request, timeout=30) as response:
        data = response.read()
        if not response.headers.get_content_type().startswith("image/"):
            raise RuntimeError(f"Réponse non-image pour {name}")
    (out / f"{name}.png").write_bytes(data)
    print(name, len(data))
