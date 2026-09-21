using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace RuneManagerModern {
  public sealed class WorldBossRuneView {public long Id;public int Slot,Level,CurrentLevel,Grade,EquippedMasterId;public bool Ancient;public string Set="",Main="",Innate="";public List<string> Stats=new List<string>();}
  public sealed class WorldBossArtifactView {public long Id;public int Level,Rank,EquippedMasterId;public string Kind="",Restriction="",IconKey="",Main="";public List<string> Effects=new List<string>();}
  public sealed class WorldBossRow {
    public int Team {get;set;} public int Position {get;set;} public long UnitId {get;set;} public int MasterId {get;set;} public string Monster {get;set;} public string Element {get;set;} public int SkillUps {get;set;} public int MaxSkillUps {get;set;} public int MissingSkillUps {get;set;} public double SkillUpGain {get;set;} public string SkillUpAdvice {get;set;} public double BaseScore {get;set;} public double CurrentScore {get;set;} public double OptimizedScore {get;set;} public string RuneSets {get;set;} public string Runes {get;set;} public string Artifacts {get;set;} public int Changes {get;set;}
    public double Gain {get{return Math.Max(0,OptimizedScore-CurrentScore);}}
    public List<WorldBossRuneView> RuneDetails=new List<WorldBossRuneView>();public List<WorldBossArtifactView> ArtifactDetails=new List<WorldBossArtifactView>();public List<long> CurrentRuneIds=new List<long>();public List<long> CurrentArtifactIds=new List<long>();
    public WorldBossRow(){Monster=Element=RuneSets=Runes=Artifacts=SkillUpAdvice="";}
  }
  public sealed class WorldBossSkillRecommendation {public long UnitId{get;set;} public int MasterId{get;set;} public string Monster{get;set;} public string Element{get;set;} public string Reason{get;set;} public int NaturalStars{get;set;} public int Current{get;set;} public int Maximum{get;set;} public int Missing{get;set;} public int CurrentLevel{get;set;} public bool NeedsLevel40{get;set;} public double Gain{get;set;} public double MaximumScore{get;set;} public bool InCurrentTeam{get;set;} public string Copy {get{return "#"+(UnitId%1000000).ToString("000000");}} public string NaturalGrade{get{return NaturalStars+"★ nat";}} public string LevelAdvice{get{return NeedsLevel40?CurrentLevel+" → 40":"Déjà 40";}} public WorldBossSkillRecommendation(){Monster=Element=Reason="";}}
  public sealed class WorldBossResult {
    // Meme formule en Stable et Test depuis round 7 (pont vers WorldBossStableV8.dll retire).
    public const string CurrentFormula="World Boss 2026 v10 • formule communautaire (attack power+attribute)x10 • skill-ups non normalisés";
    public List<WorldBossRow> Rows=new List<WorldBossRow>(); public List<WorldBossSkillRecommendation> SkillRecommendations=new List<WorldBossSkillRecommendation>(); public double CurrentTotal,OptimizedTotal; public int WaterCount; public bool UsesGameOrder; public string Formula=CurrentFormula;
    // Ordre reel colle par l'utilisateur (voir RealOrderNames) : true si les 60 noms ont
    // ete resolus sans ambiguite et servent d'ordre Team/Position en mode verification ;
    // sinon RealOrderUnresolved liste les noms non reconnus pour affichage a l'utilisateur.
    public bool RealOrderApplied; public List<string> RealOrderUnresolved=new List<string>();
    public string TeamScores {get{return string.Join("  •  ",Rows.GroupBy(x=>x.Team).OrderBy(x=>x.Key).Select(g=>"Équipe "+g.Key+" : "+g.Sum(x=>x.CurrentScore).ToString("N0")+" → "+g.Sum(x=>x.OptimizedScore).ToString("N0")).ToArray());}}
    public Dictionary<long,int> CandidateRanks=new Dictionary<long,int>();public Dictionary<long,double> CandidateScores=new Dictionary<long,double>();public Dictionary<long,int> CurrentEquipmentRanks=new Dictionary<long,int>();public Dictionary<long,double> CurrentEquipmentScores=new Dictionary<long,double>();public int CurrentEquipmentCount;public List<string> InventoryKeys=new List<string>();
  }
  // Round 7 (2026-09-15) : le gate "#if !WORLD_BOSS_STABLE" a ete retire. Avant, l'appli
  // Stable/Principale utilisait WorldBossStableBridge.cs (charge WorldBossStableV8.dll,
  // un binaire fige avec l'ancienne formule) au lieu de cette classe — c'est pourquoi
  // aucun changement de poids/formule ne se voyait jamais sur l'appli principale. Jeremy
  // a demande explicitement de basculer Stable sur ce moteur moderne. Voir aussi
  // WorldBossStableBridge.cs (desactive, #if false).
  public static class WorldBossOptimizer {
    // Évite qu'une simple montée +12 ou un micro-gain fasse redistribuer toute la chaîne.
    // L'unité n'est libérée que pour un gain visible (250 points dans l'interface).
    const double MinimumUsefulEquipmentGain=25.0;
    // Multiplicateur applique a TOUS les scores affiches (grille, totaux d'equipe, badges).
    // Round 7 (2026-09-15) : Jeremy a fourni un post communautaire complet ("Total Attack
    // power, Attribute bonus, Total damage done") qui donne la formule REELLE du jeu :
    // (Total attack power + Attribute) x10 = score reel affiche en combat. Remplace le
    // 7.5 cosmetique du round precedent (qui n'etait qu'un ajustement a la baisse sans
    // vraie source) — 10 est la vraie valeur du jeu, forcee explicitement par Jeremy.
    const double DisplayScale=10.0;
    // Calibrage global sur les 60 positions réelles du jeu. Ces valeurs sont
    // communes à tous les monstres : aucune exception par famille ou exemplaire.
    // Recalibrage du 2026-09-14 : l'ancien calibrage (2026-09-13, REAL_MASTER_ORDER
    // hypothétique) donnait un ordre FAUX confirmé par l'utilisateur en jeu (bug
    // team1/team3 inversées). Nouvel ordre réel obtenu directement de l'utilisateur
    // (60 noms tapés depuis l'écran du jeu, résolus sans ambiguïté via RealOrderNames).
    // Régression pairwise-ranking sur des coefficients extraits EXACTEMENT du moteur
    // réel (différences finies sur CurrentEquipmentScores, pas une approximation
    // Python séparée — tools/calibrate_v3.py + tools/ExtractOne.cs).
    // Round 2 (8 poids seuls) : MAE=5.27, MAX=35, 7/60 exact (avant : MAE=8.27, MAX=31, 3/60).
    // Round 3 (2026-09-14, 2e partie) : ajout d'un bonus fixe pour 5* natif — l'utilisateur
    // a demandé si l'étoile native donnait un bonus (skill-up et élément eau étaient déjà
    // pris en compte, l'étoile native NON). Corrélation empirique confirmée sur l'ordre réel
    // (rang moyen 4*natif=50.2, 5*natif=25.1 sur 60 unités) : signal réel manquant.
    // Round 4 (2026-09-14, 3e partie) : le "score du jeu" d'un artefact (rareté/niveau/
    // sous-stats) était figé en dur (20/2/10), puis calibré statistiquement (~0.07,
    // non fiable — colonnes quasi colinéaires avec seulement 60 positions).
    // Round 5 (2026-09-14, 4e partie) : formule REELLE retrouvée à partir de 4 captures
    // d'écran (score affiché en jeu) fournies par l'utilisateur, au lieu d'un calibrage
    // statistique. Score artefact = 25 × efficacité (efficacité = Σ valeur/roll-max des
    // sous-stats, déjà calculée). Rang et niveau n'entrent PAS dans le score — testé sur
    // 4 artefacts Legend niveau 15 (max), tous avec rang/niveau identiques : prédiction
    // = arrondi(25×efficacité) reproduit EXACTEMENT les 4 scores réels (182,168,170,147)
    // une fois arrondi. Pas encore vérifié sur un artefact de rang différent ou pas au
    // niveau max — à confirmer si possible avec d'autres captures.
    // Bonus 5* et 8 poids stats : toujours du round 3 (round 4 n'a changé QUE les
    // artefacts, remplacé par cette formule réelle round 5).
    // Écart résiduel attendu : un modèle linéaire à poids partagés ne peut pas
    // reproduire une formule de jeu avec d'éventuelles synergies non linéaires par
    // monstre/rune — voir tools/worldboss-coefficient-fit-20260914.json.
    // Round 6 (2026-09-14, 5e partie) : utilisateur a partagé un post Reddit externe
    // ("World Boss - Attack Power Modeling" par Blehified) suggérant des ratios très
    // différents entre stats (SPD/CR/RES/ACC ~7x ATK, CD ~5x, HP ~1/15x). Les ratios
    // Reddit tels quels testent PIRE que le calibrage actuel (MAE=5.47 vs 4.83). Mais
    // recherche par descente de coordonnées + grille fine sur l'ordre réel (60 positions)
    // confirme le SIGNAL qualitatif du post : SPD et CR étaient sous-évalués dans notre
    // calibrage. Nouveaux poids SPD/CR (et léger ajustement HP/DEF) trouvés par recherche
    // directe sur l'écart réel (pas les ratios Reddit bruts) : MAE=4.33 (avant 4.83),
    // MAX=36 (inchangé), 12/60 exact (avant 8/60) — vérifié avec le moteur réel, pas
    // seulement l'approximation linéaire.
    // Round 7 (2026-09-15) : Jeremy a explicitement demande de forcer les valeurs d'un
    // nouveau post communautaire complet (methode : monstres "nus" 6*, niv 40, sans runes,
    // isolation du score de base puis regression sur Total attack power/Attribute par
    // point de stat). Poids ci-dessous = (valeur "Total attack power" + valeur "Attribute")
    // par point, tel que fourni par Jeremy, remplace le calibrage round 6 (fit sur l'ordre
    // reel des 60 positions). Note : le round 6 avait teste un post similaire ("Blehified"
    // sur Reddit) et ses ratios bruts degradaient le fit sur l'ordre reel (MAE 5.47 vs 4.83) —
    // ce nouveau post est plus complet/methodique, mais le risque de re-degrader le fit sur
    // l'ordre reel des 60 n'a pas ete revalide ici. Applique sur demande explicite de Jeremy.
    static double HpStatWeight=1.30;
    static double AttackStatWeight=1.30;
    static double DefenseStatWeight=1.30;
    static double SpeedStatWeight=8.6703;
    static double CritRateStatWeight=9.3367;
    static double CritDamageStatWeight=6.8486;
    static double ResistanceStatWeight=8.5;
    static double AccuracyStatWeight=8.5;
    // Bonus fixe si monstre natif 5 étoiles (voir commentaire calibrage ci-dessus).
    static double NaturalFiveStarBonus=324.1828;
    // Score artefact = formule réelle du jeu retrouvée round 5 (voir commentaire
    // ci-dessus) : 25 × efficacité des sous-stats, aucun terme rang/niveau.
    static double ArtifactEfficiencyWeight=25.0;
    // Mode verification (appli test Sigmarus) : force les 6 runes + 2 artefacts REELLEMENT
    // equipes en jeu sur chaque monstre des 3 listes World Boss, au lieu de la suggestion
    // d'optimisation. Sert a comparer le score calcule ici au score affiche dans le jeu
    // sans decalage d'equipement (aucune rune/artefact suggeree n'est jamais differente
    // de ce qui est reellement porte). Classement des 60 = score actuel reel, decroissant.
    public static bool ForceCurrentEquipment=false;
    // Ordre reel exact du jeu, colle par l'utilisateur (60 noms dans l'ordre affiche en
    // jeu : team1 positions 1-20, team2 21-40, team3 41-60). Utilise UNIQUEMENT en mode
    // ForceCurrentEquipment pour le classement Team/Position ; le score affiche reste
    // toujours celui calcule par l'appli (sert a comparer, pas a remplacer, ce score).
    public static List<string> RealOrderNames=null;
    static readonly Dictionary<string,string> RealOrderElementWords=new Dictionary<string,string>{{"eau","water"},{"feu","fire"},{"vent","wind"},{"lumiere","light"},{"tenebres","dark"}};
    static string RealOrderNormalize(string s){if(string.IsNullOrEmpty(s))return "";var sb=new System.Text.StringBuilder();foreach(char ch in s.ToLowerInvariant()){char c=ch;switch(c){case 'é':case 'è':case 'ê':case 'ë':c='e';break;case 'à':case 'â':c='a';break;case 'î':case 'ï':c='i';break;case 'ô':c='o';break;case 'û':case 'ù':case 'ü':c='u';break;case 'ç':c='c';break;}if(char.IsLetterOrDigit(c))sb.Append(c);}return sb.ToString();}
    // Distance d'edition simple (insertions/suppressions/substitutions), pour rattraper
    // une faute de frappe ("psamate" / "Psamathe", "hwaorang" / "Hwoarang") sans jamais
    // deborder sur un autre monstre : n'est utilisee que si le nom exact echoue, et
    // seulement si un seul nom du catalogue est a distance <= 2 (sinon ambigu, echec).
    static int RealOrderDistance(string a,string b){int[,] d=new int[a.Length+1,b.Length+1];for(int i=0;i<=a.Length;i++)d[i,0]=i;for(int j=0;j<=b.Length;j++)d[0,j]=j;for(int i=1;i<=a.Length;i++)for(int j=1;j<=b.Length;j++){int cost=a[i-1]==b[j-1]?0:1;d[i,j]=Math.Min(Math.Min(d[i-1,j]+1,d[i,j-1]+1),d[i-1,j-1]+cost);}return d[a.Length,b.Length];}
    // Resout les 60 noms colles par l'utilisateur en 60 unit_id reellement possedes,
    // dans l'ordre donne. Un nom peut porter un suffixe element ("eau"/"feu"/"vent"/
    // "lumiere"/"tenebres" — variantes elementaires partageant le meme nom de catalogue)
    // et/ou un numero de copie ("bastet 2" = 2e exemplaire possede, trie par unit_id).
    // Echec (liste vide + unresolved rempli) si un seul nom ne se resout pas sans
    // ambiguite : un ordre "presque" exact n'est pas ce qui est demande.
    static List<long> ResolveRealOrder(List<string> requested,List<Unit> units,Dictionary<int,string> names,Dictionary<int,string> elements,out List<string> unresolved){
      unresolved=new List<string>();var resolved=new List<long>();
      if(requested==null||requested.Count!=60)return resolved;
      var byNormName=new Dictionary<string,List<int>>();
      foreach(var kv in names){string key=RealOrderNormalize(kv.Value);List<int> list;if(!byNormName.TryGetValue(key,out list)){list=new List<int>();byNormName[key]=list;}list.Add(kv.Key);}
      var used=new HashSet<long>();
      foreach(var raw in requested){
        string work=(raw??"").Trim();int copyIndex=1;
        var m=System.Text.RegularExpressions.Regex.Match(work,@"^(.*?)\s*([0-9]+)$");
        if(m.Success){work=m.Groups[1].Value.Trim();int.TryParse(m.Groups[2].Value,out copyIndex);if(copyIndex<1)copyIndex=1;}
        string elementFilter=null;
        foreach(var ew in RealOrderElementWords){string suffix=" "+ew.Key;if(work.ToLowerInvariant().EndsWith(suffix,StringComparison.Ordinal)){elementFilter=ew.Value;work=work.Substring(0,work.Length-suffix.Length).Trim();break;}}
        string workKey=RealOrderNormalize(work);
        // Certains monstres ont DEUX fiches catalogue pour le meme nom d'affichage : une
        // generique ("White Tiger Blade Master", element seulement en attribut) et une
        // avec l'element ecrit EN PREFIXE dans le nom lui-meme ("Water White Tiger Blade
        // Master"). On essaie donc plusieurs cles, et on ne retient que celle dont les
        // ids sont reellement possedes — pas seulement celle qui matche en premier.
        var keyCandidates=new List<string>{workKey};
        if(elementFilter!=null)keyCandidates.Add(elementFilter+workKey);
        List<long> owned20=null;
        foreach(var key in keyCandidates){
          List<int> masterIds;if(!byNormName.TryGetValue(key,out masterIds))continue;
          var filtered=elementFilter!=null?masterIds.Where(id=>elements.ContainsKey(id)&&string.Equals(elements[id],elementFilter,StringComparison.OrdinalIgnoreCase)).ToList():masterIds;
          if(filtered.Count==0)filtered=masterIds;
          var owned=units.Where(u=>filtered.Contains(u.Master)).OrderBy(u=>u.Id).Select(u=>u.Id).ToList();
          if(owned.Count>0){owned20=owned;break;}
        }
        if(owned20==null){
          // Repli tolerant aux fautes de frappe : un seul nom du catalogue proche (<=2)
          // ET reellement possede, sinon on abandonne plutot que de deviner au hasard.
          var close=byNormName.Keys.Select(k=>new{Key=k,Dist=RealOrderDistance(workKey,k)}).Where(x=>x.Dist<=2).OrderBy(x=>x.Dist).ToList();
          for(int ci=0;ci<close.Count&&owned20==null;ci++){if(ci+1<close.Count&&close[ci+1].Dist==close[ci].Dist)break;var closeIds=byNormName[close[ci].Key];var closeFiltered=elementFilter!=null?closeIds.Where(id=>elements.ContainsKey(id)&&string.Equals(elements[id],elementFilter,StringComparison.OrdinalIgnoreCase)).ToList():closeIds;if(closeFiltered.Count==0)closeFiltered=closeIds;var ownedTry=units.Where(u=>closeFiltered.Contains(u.Master)).OrderBy(u=>u.Id).Select(u=>u.Id).ToList();if(ownedTry.Count>0)owned20=ownedTry;}
        }
        if(owned20==null){unresolved.Add(raw);continue;}
        long chosen=0;
        if(owned20.Count>=copyIndex&&!used.Contains(owned20[copyIndex-1]))chosen=owned20[copyIndex-1];
        else{long alt=owned20.FirstOrDefault(id=>!used.Contains(id));if(alt!=0)chosen=alt;}
        if(chosen==0){unresolved.Add(raw);continue;}
        used.Add(chosen);resolved.Add(chosen);
      }
      if(unresolved.Count>0||resolved.Count!=60||resolved.Distinct().Count()!=60){if(unresolved.Count==0)unresolved.Add("(doublons ou liste incomplète)");return new List<long>();}
      return resolved;
    }
    sealed class Unit {public long Id;public int Master,PortraitMaster,SkillGroup,Level,OriginalLevel,Grade,Style,Attribute,SkillUps,NaturalStars,MaxSkillUps;public bool HasElementalNat4SkillSource;public double Hp,Atk,Def,Spd,Cr,Cd,Res,Acc,Base,RelicHpPct,RelicAtkPct,RelicDefPct;public string Name="",Element="";public List<long> CurrentRunes=new List<long>();public List<long> CurrentArtifacts=new List<long>();public List<Rune> Assigned=new List<Rune>();public List<Artifact> AssignedArtifacts=new List<Artifact>();}
    sealed class Level40Stats {public double hp;public double atk;public double def;public double spd;}
    sealed class RuneEffect {public int Stat;public double Base,Grind;public bool Gemmed,Innate;}
    sealed class Rune {public Rune Actual;public long Id;public int Slot,Set,Grade,Level,CurrentLevel,Main,Quality,EquippedMasterId;public bool Ancient;public double MainValue;public Dictionary<int,double> Stats=new Dictionary<int,double>();public List<RuneEffect> Effects=new List<RuneEffect>();}
    static double AverageRoll(int stat){return stat==1?300:stat==3||stat==5?18:stat==8?5:stat==9?5:stat==10?6:6;}
    static void ProjectRemainingRolls(Rune r,int originalLevel){int total=r.Quality>=5?4:r.Quality==4?3:r.Quality==3?2:r.Quality==2?1:0,missing=Math.Max(0,total-Math.Min(total,originalLevel/3));var subs=r.Effects.Where(x=>!x.Innate&&x.Stat!=r.Main).ToList();if(missing<=0||subs.Count==0)return;double share=(double)missing/subs.Count;foreach(var e in subs){double add=AverageRoll(e.Stat)*share;e.Base+=add;r.Stats[e.Stat]=r.Stats[e.Stat]+add;}}
    sealed class Artifact {public long Id;public int Type,Attribute,Style,Rank,Level,Main,EquippedMasterId;public double MainValue,Quality;public List<string> Effects=new List<string>();}
    static Dictionary<string,object> D(object o){return o as Dictionary<string,object>;} static object[] A(object o){return o as object[]??new object[0];} static object G(Dictionary<string,object>d,string k,object z=null){object v;return d!=null&&d.TryGetValue(k,out v)?v:z;} static int I(object o){int v;return int.TryParse(Convert.ToString(o,CultureInfo.InvariantCulture),out v)?v:0;} static long L(object o){long v;return long.TryParse(Convert.ToString(o,CultureInfo.InvariantCulture),out v)?v:0;} static double F(object o){double v;return double.TryParse(Convert.ToString(o,CultureInfo.InvariantCulture),NumberStyles.Any,CultureInfo.InvariantCulture,out v)?v:0;}
    static string ReadSharedText(string path){using(var fs=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var sr=new StreamReader(fs))return sr.ReadToEnd();}
    static void ReadLiveOverlays(JavaScriptSerializer js,IEnumerable<string> events,Dictionary<long,Dictionary<string,object>> runes,HashSet<long> sold,Dictionary<long,Dictionary<string,object>> units,Dictionary<long,HashSet<long>> removedRunes){
      if(events==null)return;
      foreach(string json in events){if(string.IsNullOrWhiteSpace(json))continue;try{
        var root=D(js.DeserializeObject(json));if(root==null||I(G(root,"ret_code"))!=0)continue;string command=Convert.ToString(G(root,"command"));
        // Les réponses d'arène contiennent les fiches complètes des défenseurs adverses.
        // Elles ne représentent jamais l'inventaire du joueur et ne doivent donc pas
        // alimenter les unités/runes utilisées par l'optimiseur World Boss.
        if(command.StartsWith("arenav2_",StringComparison.OrdinalIgnoreCase)||command.IndexOf("ArenaDefense",StringComparison.OrdinalIgnoreCase)>=0)continue;
        if(command=="SellRune"){foreach(var x in A(G(root,"rune_id_list"))){long id=L(x);if(id>0){sold.Add(id);runes.Remove(id);}}}else CollectLiveRunes(root,runes,sold);
        bool ownedUnitMutation=command=="UpdateUnitEquip"||command=="SummonUnit"||command=="SacrificeUnit_V4"||command.StartsWith("UpgradeUnitSkill",StringComparison.OrdinalIgnoreCase)||command.StartsWith("PowerupUnit",StringComparison.OrdinalIgnoreCase);
        if(ownedUnitMutation)CollectLiveUnits(root,units);
        if(command=="UpdateUnitEquip"){CollectLiveUnits(root,units);var equip=D(G(root,"EquipRuneList"));if(equip!=null){var moved=new HashSet<long>(A(G(equip,"equip_rune_id_list")).Select(L).Where(x=>x>0));foreach(var donorRaw in A(G(equip,"unequip_unit_list"))){long donor=L(donorRaw);if(donor<=0)continue;HashSet<long> removed;if(!removedRunes.TryGetValue(donor,out removed)){removed=new HashSet<long>();removedRunes[donor]=removed;}foreach(long runeId in moved)removed.Add(runeId);}}}
        if(command=="SummonUnit")foreach(var x in A(G(root,"unit_list"))){var u=D(x);long id=L(G(u,"unit_id"));if(id>0)units[id]=u;}
        if(command=="SacrificeUnit_V4"){
          var target=D(G(root,"target_unit"));long id=L(G(target,"unit_id"));if(id>0)units[id]=target;
          foreach(var x in A(G(root,"unit_list"))){var u=D(x);long unitId=L(G(u,"unit_id"));if(unitId>0)units[unitId]=u;}
        }
      }catch{}}
    }
    static void CollectLiveUnits(object value,Dictionary<long,Dictionary<string,object>> units){var d=D(value);if(d!=null){bool identified=G(d,"unit_id")!=null&&(G(d,"skills")!=null||(G(d,"unit_master_id")!=null&&(G(d,"runes")!=null||G(d,"artifacts")!=null||G(d,"unit_level")!=null)));if(identified){long id=L(G(d,"unit_id"));if(id>0){Dictionary<string,object> previous;if(units.TryGetValue(id,out previous)){foreach(var field in d)previous[field.Key]=field.Key=="skills"?MergeSkills(G(previous,"skills"),field.Value):field.Value;}else units[id]=d;}return;}foreach(var kv in d)CollectLiveUnits(kv.Value,units);return;}foreach(var x in A(value))CollectLiveUnits(x,units);}
    static object MergeSkills(object savedValue,object liveValue){var saved=A(savedValue).ToList();var live=A(liveValue);if(live.Length==0)return savedValue;foreach(var update in live){var current=A(update);int skillId=current.Length>0?I(current[0]):0;int index=skillId>0?saved.FindIndex(x=>{var old=A(x);return old.Length>0&&I(old[0])==skillId;}):-1;if(index>=0)saved[index]=update;else saved.Add(update);}return saved.ToArray();}
    static void CollectLiveRunes(object value,Dictionary<long,Dictionary<string,object>> runes,HashSet<long> sold){var d=D(value);if(d!=null){if(G(d,"rune_id")!=null&&G(d,"slot_no")!=null&&G(d,"set_id")!=null&&G(d,"pri_eff")!=null){long id=L(G(d,"rune_id"));if(id>0){runes[id]=d;sold.Remove(id);}return;}foreach(var kv in d)CollectLiveRunes(kv.Value,runes,sold);return;}foreach(var x in A(value))CollectLiveRunes(x,runes,sold);}
    static Dictionary<int,int> LoadArtifactProfileStyles(){var result=new Dictionary<int,int>();try{string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"Artifact_Manager_Data.json");if(!File.Exists(path))return result;var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=D(js.DeserializeObject(File.ReadAllText(path)));foreach(var raw in A(G(root,"profiles"))){var p=D(raw);int id=I(G(p,"monster_id")),style=I(G(p,"style_id"));if(id>0&&style>=1&&style<=4)result[id]=style;}}catch{}return result;}
    static Dictionary<int,Level40Stats> LoadLevel40Stats(string catalogPath){var result=new Dictionary<int,Level40Stats>();try{string path=Path.Combine(Path.GetDirectoryName(catalogPath)??"","level40-stats.json");if(!File.Exists(path))return result;var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var raw=js.Deserialize<Dictionary<string,Level40Stats>>(File.ReadAllText(path));foreach(var kv in raw){int id;if(int.TryParse(kv.Key,out id)&&kv.Value!=null&&kv.Value.hp>0)result[id]=kv.Value;}var catalog=A(js.DeserializeObject(File.ReadAllText(catalogPath))).Select(D).Where(x=>x!=null).ToList();var knownByEquivalent=new Dictionary<string,Level40Stats>();foreach(var c in catalog){int id=I(G(c,"id")),group=I(G(c,"skillgroup"));string element=Convert.ToString(G(c,"element","")).ToLowerInvariant();Level40Stats stats;if(group>0&&element.Length>0&&result.TryGetValue(id,out stats))knownByEquivalent[group+":"+element]=stats;}foreach(var c in catalog){int id=I(G(c,"id")),group=I(G(c,"skillgroup"));string element=Convert.ToString(G(c,"element","")).ToLowerInvariant();Level40Stats equivalent;if(id>0&&!result.ContainsKey(id)&&knownByEquivalent.TryGetValue(group+":"+element,out equivalent))result[id]=equivalent;}for(int element=1;element<=5;element++){Level40Stats replacement;if(!result.ContainsKey(29210+element)&&result.TryGetValue(29610+element,out replacement))result[29210+element]=replacement;}}catch{}return result;}
    public static WorldBossResult Analyze(string jsonPath,string catalogPath,IEnumerable<string> liveEvents=null,WorldBossResult previousPlan=null){
      var profileStyles=LoadArtifactProfileStyles();
      string frozenPath=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"worldboss-calibration-locked.json");
      bool calibrationLocked=File.Exists(frozenPath);
      if(calibrationLocked){previousPlan=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256}.Deserialize<WorldBossResult>(ReadSharedText(frozenPath));if(previousPlan==null||previousPlan.Rows==null||previousPlan.Rows.Count!=60||previousPlan.Rows.Select(x=>x.UnitId).Distinct().Count()!=60)throw new InvalidOperationException("Sauvegarde de calibration invalide : aucun build ne sera recalcule.");}
      var level40Stats=LoadLevel40Stats(catalogPath);
      var liveRunes=new Dictionary<long,Dictionary<string,object>>();var soldRunes=new HashSet<long>();var liveUnits=new Dictionary<long,Dictionary<string,object>>();var removedRunes=new Dictionary<long,HashSet<long>>();
      var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=D(js.DeserializeObject(ReadSharedText(jsonPath)));var names=new Dictionary<int,string>();var elements=new Dictionary<int,string>();var naturalStars=new Dictionary<int,int>();var maximumSkillUps=new Dictionary<int,int>();var skillGroups=new Dictionary<int,int>();var familyNames=new Dictionary<int,string>();var familyStars=new Dictionary<int,int>();var familySkillUps=new Dictionary<int,int>();var familySkillGroups=new Dictionary<int,int>();var familyPortraits=new Dictionary<long,int>();var linkedNames=new Dictionary<long,string>();var linkedStars=new Dictionary<long,int>();var linkedSkillUps=new Dictionary<long,int>();var linkedPortraits=new Dictionary<long,int>();var elementalNat4SkillGroups=new HashSet<int>();try{foreach(var raw in A(js.DeserializeObject(File.ReadAllText(catalogPath)))){var c=D(raw);int id=I(G(c,"id"));if(id>0){string name=Convert.ToString(G(c,"name","Monstre "+id)),element=Convert.ToString(G(c,"element",""));int stars=I(G(c,"stars")),skillups=I(G(c,"skillups")),family=I(G(c,"familyid")),skillgroup=I(G(c,"skillgroup"));if(family<=0)family=id/100*100;if(skillgroup<=0)skillgroup=family;names[id]=name;elements[id]=element;naturalStars[id]=stars;maximumSkillUps[id]=skillups;skillGroups[id]=skillgroup;long familyKey=((long)family<<8)+(id%10);familyPortraits[familyKey]=id;if(!familyNames.ContainsKey(family))familyNames[family]=name;if(!familyStars.ContainsKey(family))familyStars[family]=stars;if(!familySkillUps.ContainsKey(family))familySkillUps[family]=skillups;if(!familySkillGroups.ContainsKey(family))familySkillGroups[family]=skillgroup;if(stars==4&&(element=="water"||element=="fire"||element=="wind"))elementalNat4SkillGroups.Add(skillgroup);if(skillgroup>0){long key=((long)skillgroup<<8)+(id%10);linkedNames[key]=name;linkedStars[key]=stars;linkedSkillUps[key]=skillups;linkedPortraits[key]=id;}}}}catch{}
      ReadLiveOverlays(js,liveEvents,liveRunes,soldRunes,liveUnits,removedRunes);if(liveUnits.Count>0||removedRunes.Count>0){var merged=A(G(root,"unit_list")).Select(D).Where(x=>x!=null).ToDictionary(x=>L(G(x,"unit_id")));foreach(var kv in liveUnits){Dictionary<string,object> existing;if(merged.TryGetValue(kv.Key,out existing)){foreach(var field in kv.Value)existing[field.Key]=field.Key=="skills"?MergeSkills(G(existing,"skills"),field.Value):field.Value;}else merged[kv.Key]=kv.Value;}foreach(var kv in removedRunes){Dictionary<string,object> unit;if(!merged.TryGetValue(kv.Key,out unit))continue;unit["runes"]=A(G(unit,"runes")).Where(x=>{var rune=D(x);return rune==null||!kv.Value.Contains(L(G(rune,"rune_id")));}).ToArray();}root["unit_list"]=merged.Values.Cast<object>().ToArray();}var runes=new Dictionary<long,Rune>();var artifacts=new Dictionary<long,Artifact>();var units=new List<Unit>();foreach(var raw in A(G(root,"runes")))AddRune(D(raw),runes);foreach(var raw in A(G(root,"artifacts")))AddArtifact(D(raw),artifacts);
      foreach(var raw in A(G(root,"unit_list"))){var d=D(raw);if(d==null)continue;var u=new Unit{Id=L(G(d,"unit_id")),Master=I(G(d,"unit_master_id")),Level=I(G(d,"unit_level")),Grade=I(G(d,"class")),Style=I(G(d,"unit_style")),Attribute=I(G(d,"attribute")),Hp=F(G(d,"con"))*15,Atk=F(G(d,"atk")),Def=F(G(d,"def")),Spd=F(G(d,"spd")),Cr=F(G(d,"critical_rate")),Cd=F(G(d,"critical_damage")),Res=F(G(d,"resist")),Acc=F(G(d,"accuracy"))};if(u.Style==0)u.Style=I(G(d,"type"));int family=u.Master/100*100;long linkedKey=((long)family<<8)+(u.Master%10),familyKey=((long)family<<8)+(u.Master%10);string linkedName=linkedNames.ContainsKey(linkedKey)?linkedNames[linkedKey]:"";if(family==34400)linkedName="Gandalf";u.Name=names.ContainsKey(u.Master)?names[u.Master]:(familyNames.ContainsKey(family)?familyNames[family]:(linkedName.Length>0?linkedName:"Monstre "+u.Master));u.PortraitMaster=names.ContainsKey(u.Master)?u.Master:(familyPortraits.ContainsKey(familyKey)?familyPortraits[familyKey]:(linkedPortraits.ContainsKey(linkedKey)?linkedPortraits[linkedKey]:u.Master));if(family==34400)u.PortraitMaster=u.Master;u.SkillGroup=skillGroups.ContainsKey(u.Master)?skillGroups[u.Master]:(familySkillGroups.ContainsKey(family)?familySkillGroups[family]:family);u.HasElementalNat4SkillSource=elementalNat4SkillGroups.Contains(u.SkillGroup);u.Element=elements.ContainsKey(u.Master)?elements[u.Master]:Element(u.Attribute);u.NaturalStars=naturalStars.ContainsKey(u.Master)?naturalStars[u.Master]:(familyStars.ContainsKey(family)?familyStars[family]:(linkedStars.ContainsKey(linkedKey)?linkedStars[linkedKey]:0));u.MaxSkillUps=maximumSkillUps.ContainsKey(u.Master)?maximumSkillUps[u.Master]:(familySkillUps.ContainsKey(family)?familySkillUps[family]:(linkedSkillUps.ContainsKey(linkedKey)?linkedSkillUps[linkedKey]:0));foreach(var skill in A(G(d,"skills"))){var s=A(skill);if(s.Length>1)u.SkillUps+=Math.Max(0,I(s[1])-1);}foreach(var relicRaw in A(G(d,"relics"))){var relic=D(relicRaw);var pri=A(G(relic,"pri_effect"));if(pri.Length<2)continue;int stat=I(pri[0]);double value=F(pri[1]);if(stat==100)u.RelicHpPct=Math.Max(u.RelicHpPct,value);else if(stat==101)u.RelicAtkPct=Math.Max(u.RelicAtkPct,value);else if(stat==102)u.RelicDefPct=Math.Max(u.RelicDefPct,value);}foreach(var rr in A(G(d,"runes"))){var rd=D(rr);AddRune(rd,runes);long id=L(G(rd,"rune_id"));if(id>0){u.CurrentRunes.Add(id);Rune equipped;if(runes.TryGetValue(id,out equipped))equipped.EquippedMasterId=u.PortraitMaster;}}foreach(var aa in A(G(d,"artifacts"))){var ad=D(aa);AddArtifact(ad,artifacts);if(ad!=null&&I(G(ad,"type"))==2){int artifactStyle=I(G(ad,"unit_style"));if(artifactStyle>=1&&artifactStyle<=4)u.Style=artifactStyle;}long id=L(G(ad,"rid"));if(id>0){u.CurrentArtifacts.Add(id);Artifact equippedArtifact;if(artifacts.TryGetValue(id,out equippedArtifact))equippedArtifact.EquippedMasterId=u.PortraitMaster;}}int exactStyle;if(profileStyles.TryGetValue(u.Master,out exactStyle))u.Style=exactStyle;else if(u.Style<=0||u.Style==98){double hpWeight=u.Hp/15.0;u.Style=u.Atk>=u.Def&&u.Atk>=hpWeight?1:(u.Def>=hpWeight?2:3);}if(u.Id>0&&u.Level>0)units.Add(u);}
      foreach(var u in units){int family=u.Master/100*100;string exactPortrait=Path.Combine(Path.GetDirectoryName(catalogPath)??"",u.Master+".png");if(family==29200)u.Name="Geralt";if(File.Exists(exactPortrait))u.PortraitMaster=u.Master;}
      foreach(var kv in liveRunes)AddRune(kv.Value,runes);foreach(long id in soldRunes)runes.Remove(id);foreach(var owner in units)foreach(long runeId in owner.CurrentRunes){Rune equipped;if(runes.TryGetValue(runeId,out equipped))equipped.EquippedMasterId=owner.PortraitMaster;}foreach(var u in units)u.Base=UnitBase(u);var preLevelScores=units.ToDictionary(u=>u.Id,u=>CandidateScore(u,runes.Values,artifacts.Values));var actualEquipmentScores=units.ToDictionary(u=>u.Id,u=>Current(u,runes,artifacts));SimulateAllAtLevel40(units,level40Stats);foreach(var u in units)u.Base=UnitBase(u);
      var ranked=units.Select(u=>new{Unit=u,Score=CandidateScore(u,runes.Values,artifacts.Values)}).OrderByDescending(x=>x.Score).ThenBy(x=>x.Unit.Id).ToList();var formulaScores=ranked.ToDictionary(x=>x.Unit.Id,x=>x.Score);var gameOrder=GameWorldBossOrder(liveEvents);var candidates=ranked.Take(Math.Min(60,units.Count)).Select(x=>x.Unit).ToList();double cutoff=ranked.Count>=60?ranked[59].Score:(ranked.Count>0?ranked.Last().Score:0);
      // L'ordre observé dans le jeu est une vérité de calibration uniquement. Il ne
      // sélectionne ni ne classe jamais les monstres : seule la formule générale le fait.
      // Une nouvelle formule libère une seule fois les équipements des 60 monstres
      // déjà validés, sans remplacer silencieusement six participants pendant le test.
      // Dès que le plan v9 est enregistré, les calculs suivants sont stabilisés.
      bool formulaChanged=previousPlan!=null&&!string.Equals(previousPlan.Formula,WorldBossResult.CurrentFormula,StringComparison.Ordinal);
      var inventoryKeys=BuildInventoryKeys(units,runes,artifacts);
      // Le plan sauvegardé est la référence des 60 participants. Un changement
      // d'état d'équipement ou un nouvel export ne doit jamais refaire la sélection.
      // On ne rouvre cette sélection que si un véritable nouvel exemplaire apparaît.
      if(previousPlan!=null&&previousPlan.Rows!=null&&previousPlan.Rows.Count>0){
        var allUnits=units.ToDictionary(x=>x.Id);var plannedIds=previousPlan.Rows.OrderBy(x=>x.Team).ThenBy(x=>x.Position).Select(x=>x.UnitId).ToList();
        var oldUnitIds=new HashSet<long>((previousPlan.InventoryKeys??new List<string>()).Where(x=>x.StartsWith("U:")).Select(KeyId));
        bool newMonster=units.Any(x=>!oldUnitIds.Contains(x.Id));
        if(!newMonster&&plannedIds.All(allUnits.ContainsKey))candidates=plannedIds.Select(id=>allUnits[id]).ToList();
      }
      var unlocked=formulaChanged?new HashSet<long>(candidates.Select(x=>x.Id)):MeaningfulInventoryTargets(candidates,runes,artifacts,previousPlan,inventoryKeys);
      // Premier calcul de cette version : repartir de tout l'inventaire et figer chaque
      // monstre successivement. Calculs suivants : conserver le plan sauvegardé ; seuls
      // les bénéficiaires d'une acquisition réellement utile sont libérés.
      bool realOrderApplied=false;List<string> realOrderUnresolvedNames=new List<string>();
      if(ForceCurrentEquipment){
        // Mode verification Sigmarus : jamais de build suggere. Chaque monstre garde
        // exactement ses 6 runes + 2 artefacts reellement equipes en jeu.
        // BUG CORRIGE (v2) : ce mode essayait avant de reconstruire les 3 vraies teams
        // depuis le dernier combat reseau capture (gameOrder/BattleWorldBossStart_v2).
        // Verifie contre un export reel : cet ordre reseau ne correspond PAS a l'ecran
        // du jeu (seule la position 1 coincidait, par hasard). Le classement que le jeu
        // affiche reellement pour choisir qui envoyer, c'est le score de l'equipement
        // ACTUELLEMENT porte par chaque monstre (actualEquipmentScores), trie decroissant
        // — exactement CurrentEquipmentRanks/CurrentEquipmentScores deja calcules plus
        // bas. On reutilise ce meme tri ici pour les 60 candidats et le decoupage en
        // 3 blocs de 20 (Team 1 = meilleur score, ordre naturel, aucun melange).
        // Si l'utilisateur a colle l'ordre EXACT du jeu (RealOrderNames), il prime sur
        // ce tri par score : c'est l'ordre reel demande, le score reste affiche a cote
        // pour comparer, jamais pour reclasser.
        List<string> realOrderUnresolved;var realOrderIds=ResolveRealOrder(RealOrderNames,units,names,elements,out realOrderUnresolved);
        if(realOrderIds.Count==60){var byIdReal=units.ToDictionary(x=>x.Id);candidates=realOrderIds.Select(id=>byIdReal[id]).ToList();realOrderApplied=true;}
        else{candidates=units.OrderByDescending(u=>actualEquipmentScores.ContainsKey(u.Id)?actualEquipmentScores[u.Id]:Current(u,runes,artifacts)).ThenBy(u=>u.Id).Take(Math.Min(60,units.Count)).ToList();realOrderUnresolvedNames=realOrderUnresolved;}
        foreach(var u in candidates){
          u.Assigned.Clear();u.AssignedArtifacts.Clear();
          foreach(long id in u.CurrentRunes){Rune rune;if(runes.TryGetValue(id,out rune))u.Assigned.Add(WorldBossEvaluationRune(rune));}
          foreach(long id in u.CurrentArtifacts){Artifact artifact;if(artifacts.TryGetValue(id,out artifact))u.AssignedArtifacts.Add(artifact);}
        }
      }else{
        if(previousPlan!=null&&!formulaChanged){LockCompletedPlan(candidates,runes,artifacts,previousPlan,unlocked);LockCurrentEquipment(candidates,runes,artifacts,unlocked);}
        if(calibrationLocked){
          var byId=units.ToDictionary(x=>x.Id);candidates=new List<Unit>();var usedRunes=new HashSet<long>();var usedArtifacts=new HashSet<long>();
          foreach(var plan in previousPlan.Rows){Unit u;if(!byId.TryGetValue(plan.UnitId,out u))throw new InvalidOperationException("Monstre verrouille absent du JSON : "+plan.Monster);
            u.Assigned.Clear();u.AssignedArtifacts.Clear();
            foreach(var item in plan.RuneDetails){Rune rune;if(!runes.TryGetValue(item.Id,out rune)||!usedRunes.Add(item.Id))throw new InvalidOperationException("Rune verrouillee absente ou dupliquee : "+item.Id+". Aucun remplacement automatique.");u.Assigned.Add(WorldBossEvaluationRune(rune));}
            foreach(var item in plan.ArtifactDetails){Artifact artifact;if(!artifacts.TryGetValue(item.Id,out artifact)||!usedArtifacts.Add(item.Id))throw new InvalidOperationException("Artefact verrouille absent ou duplique : "+item.Id+". Aucun remplacement automatique.");u.AssignedArtifacts.Add(artifact);}candidates.Add(u);
          }
        }else AssignEquipment(candidates,runes,artifacts);
        // L'ordre issu des combats sert uniquement à sélectionner/calibrer les 60 candidats.
        // Une fois leurs équipements définitifs connus, les équipes affichées doivent être
        // classées selon leur vraie valeur finale et non selon une ancienne position du jeu.
        // Les builds restent verrouillés, mais le classement est toujours recalculé
        // par la formule générale. On garde le meilleur score entre l'équipement
        // réellement lu et le build proposé afin qu'un déséquipement temporaire ne
        // fasse pas chuter artificiellement un monstre.
        candidates=candidates.OrderByDescending(u=>!calibrationLocked&&u.CurrentRunes.Count(id=>runes.ContainsKey(id))==6&&u.CurrentArtifacts.Count(id=>artifacts.ContainsKey(id))==2?Current(u,runes,artifacts):Optimized(u)).ThenBy(x=>x.Id).ToList();
      }
      // Le classement observé du jeu sert à ordonner le World Boss, mais ne doit
      // jamais contaminer la simulation intrinsèque niveau 40/full skill-up.
      var pureScores=units.ToDictionary(u=>u.Id,u=>PureMonsterScore(u));
      // Le score "pur max" décrit le modèle du monstre, pas son exemplaire ni sa
      // position dans le dernier World Boss. Deux Bastet/Camilla identiques doivent
      // donc afficher exactement la même valeur une fois niveau 40 et full skill.
      foreach(var family in units.GroupBy(u=>u.Master)){double shared=family.Max(u=>pureScores[u.Id]);foreach(var u in family)pureScores[u.Id]=shared;}
      // Le maximum pur représente toujours le même monstre niveau 40 et full skill-up.
      // Il ne dépend donc pas du nombre de skill-ups déjà présents sur cet exemplaire.
      var skillMaximum=units.ToDictionary(u=>u.Id,u=>pureScores[u.Id]+SkillUpGain(u,Math.Max(0,u.MaxSkillUps)));
      var result=new WorldBossResult{UsesGameOrder=gameOrder.Count==60,InventoryKeys=inventoryKeys,RealOrderApplied=realOrderApplied,RealOrderUnresolved=realOrderUnresolvedNames};for(int debugRank=0;debugRank<ranked.Count;debugRank++){result.CandidateRanks[ranked[debugRank].Unit.Id]=debugRank+1;result.CandidateScores[ranked[debugRank].Unit.Id]=ranked[debugRank].Score;}var currentEquipmentOrder=units.Select(u=>new{Unit=u,Score=actualEquipmentScores[u.Id]}).OrderByDescending(x=>x.Score).ThenBy(x=>x.Unit.Id).ToList();result.CurrentEquipmentCount=currentEquipmentOrder.Count;for(int currentRank=0;currentRank<currentEquipmentOrder.Count;currentRank++){result.CurrentEquipmentRanks[currentEquipmentOrder[currentRank].Unit.Id]=currentRank+1;result.CurrentEquipmentScores[currentEquipmentOrder[currentRank].Unit.Id]=Math.Round(currentEquipmentOrder[currentRank].Score*DisplayScale,1);}var selected=new HashSet<long>(candidates.Select(x=>x.Id));
      if(calibrationLocked)result.Formula="World Boss - score reel - 60 builds verrouilles (test)";
      foreach(var u in units.OrderByDescending(x=>skillMaximum[x.Id]).ThenBy(x=>x.Id)){double candidate=formulaScores[u.Id];int missing=Math.Max(0,u.MaxSkillUps-u.SkillUps);bool needs40=u.OriginalLevel<40,inTeam=selected.Contains(u.Id);double skillGain=missing>0?SkillUpGain(u,missing):0,projected=candidate+skillGain;bool scoreEnough=inTeam,fullScoreEnough=projected+0.000001>=cutoff;string reason=!needs40&&missing<=0?(inTeam?"Terminé • présent dans les 60":"Terminé • actuellement hors des 60"):(scoreEnough?(needs40?"Score suffisant au niveau 40 • skill-ups encore possibles":"Déjà dans les 60 • skill-ups encore manquants"):(fullScoreEnough?(needs40?"Entre dans les 60 au niveau 40 et full skill-up":"Entre dans les 60 une fois full skill-up"):"Maximum encore insuffisant pour entrer dans les 60"));double gain=Math.Max(0,projected-(preLevelScores.ContainsKey(u.Id)?preLevelScores[u.Id]:0)),maximum=skillMaximum.ContainsKey(u.Id)?skillMaximum[u.Id]:projected;result.SkillRecommendations.Add(new WorldBossSkillRecommendation{UnitId=u.Id,MasterId=u.PortraitMaster,Monster=u.Name,Element=FrenchElement(u.Element),NaturalStars=u.NaturalStars,Current=u.SkillUps,Maximum=u.MaxSkillUps,Missing=missing,CurrentLevel=u.OriginalLevel,NeedsLevel40=needs40,Gain=Math.Round(gain*DisplayScale,1),MaximumScore=Math.Round(maximum*DisplayScale,1),InCurrentTeam=inTeam,Reason=reason});}
      // Cette page sert à décider où investir : un monstre dont le maximum reste
      // sous le top 60 n'y apporte aucune action utile et ne doit plus être affiché.
      result.SkillRecommendations=result.SkillRecommendations.Where(x=>x.InCurrentTeam||x.Reason.StartsWith("Entre dans les 60",StringComparison.OrdinalIgnoreCase)).OrderByDescending(x=>x.MaximumScore).ThenBy(x=>x.Monster).ThenBy(x=>x.UnitId).ToList();int pos=0;
      foreach(var u in candidates){
        pos++;double current=ForceCurrentEquipment&&actualEquipmentScores.ContainsKey(u.Id)?actualEquipmentScores[u.Id]:Current(u,runes,artifacts),optimized=Optimized(u);int missing=Math.Max(0,u.MaxSkillUps-u.SkillUps);double skillGain=u.NaturalStars==5&&!u.HasElementalNat4SkillSource?SkillUpGain(u,missing):0;
        // Le pool global peut assigner a ce monstre precis un jeu different de son
        // equipement actuel sans que sa valeur reelle progresse (le meilleur equip
        // part ailleurs). Dans ce cas on ne propose PAS ce changement — on affiche
        // son equipement actuel tel quel, coherent avec Amelioration="Equipement
        // actuel conserve" (sinon Sets proposes/Changements racontaient un swap que
        // Amelioration disait ne pas exister).
        bool sameRunes=u.Assigned.Count==u.CurrentRunes.Count&&new HashSet<long>(u.Assigned.Select(r=>r.Id)).SetEquals(u.CurrentRunes),sameArtifacts=u.AssignedArtifacts.Count==u.CurrentArtifacts.Count&&new HashSet<long>(u.AssignedArtifacts.Select(a=>a.Id)).SetEquals(u.CurrentArtifacts);
        bool improves=optimized>current+0.000001;
        List<Rune> effRunes=improves?u.Assigned:u.CurrentRunes.Where(runes.ContainsKey).Select(id=>runes[id]).ToList();
        List<Artifact> effArtifacts=improves?u.AssignedArtifacts:u.CurrentArtifacts.Where(artifacts.ContainsKey).Select(id=>artifacts[id]).ToList();
        var row=new WorldBossRow{Team=(pos-1)/20+1,Position=(pos-1)%20+1,UnitId=u.Id,MasterId=u.PortraitMaster,Monster=u.Name+"  #"+(u.Id%1000000).ToString("000000"),Element=FrenchElement(u.Element),SkillUps=u.SkillUps,MaxSkillUps=u.MaxSkillUps,MissingSkillUps=missing,SkillUpGain=Math.Round(skillGain*DisplayScale,1),SkillUpAdvice=skillGain>0?"OUI • "+missing+" skill-ups • +"+(skillGain*DisplayScale).ToString("0"):"—",BaseScore=Math.Round(u.Base*DisplayScale,1),CurrentScore=Math.Round(current*DisplayScale,1),OptimizedScore=Math.Round((improves?optimized:current)*DisplayScale,1),RuneSets=improves?SetSummary(u,u.Assigned):"Équipement actuel conservé",Runes=string.Join("  ",effRunes.OrderBy(r=>r.Slot).Select(r=>"S"+r.Slot+" #"+r.Id)),Artifacts=string.Join("  ",effArtifacts.Select(a=>"#"+a.Id)),Changes=improves?u.Assigned.Count(r=>!u.CurrentRunes.Contains(r.Id))+u.AssignedArtifacts.Count(a=>!u.CurrentArtifacts.Contains(a.Id)):0,CurrentRuneIds=new List<long>(u.CurrentRunes),CurrentArtifactIds=new List<long>(u.CurrentArtifacts)};
        if(sameRunes&&sameArtifacts){row.OptimizedScore=row.CurrentScore;row.Changes=0;row.RuneSets="Équipement actuel conservé";}
        foreach(var r in effRunes.OrderBy(x=>x.Slot)){var innate=r.Effects.FirstOrDefault(x=>x.Innate);row.RuneDetails.Add(new WorldBossRuneView{Id=r.Id,Slot=r.Slot,EquippedMasterId=r.EquippedMasterId,Level=r.Level,CurrentLevel=r.CurrentLevel,Grade=r.Grade,Ancient=r.Ancient,Set=SetLabel(r.Set),Main=RuneStatDisplay(r.Main,r.MainValue,0),Innate=innate==null?"—":RuneStatDisplay(innate.Stat,innate.Base,innate.Grind,false),Stats=r.Effects.Where(x=>!x.Innate).Select(x=>RuneStatDisplay(x.Stat,x.Base,x.Grind,x.Gemmed)).ToList()});}
        foreach(var a in effArtifacts)row.ArtifactDetails.Add(new WorldBossArtifactView{Id=a.Id,EquippedMasterId=a.EquippedMasterId,Level=a.Level,Rank=a.Rank,Kind=a.Type==1?"Élément":"Type",Restriction=ArtifactRestriction(a,u),IconKey=ArtifactIconKey(a),Main=ArtifactMain(a.Main)+"  +"+a.MainValue.ToString("0.##"),Effects=new List<string>(a.Effects)});
        result.Rows.Add(row);result.CurrentTotal+=row.CurrentScore;result.OptimizedTotal+=row.OptimizedScore;if(u.Element.Equals("water",StringComparison.OrdinalIgnoreCase))result.WaterCount++;
      }
      // Dernière barrière de sécurité : une détection théorique peut libérer un
      // monstre, mais le build complet (sets + 6 runes + 2 artefacts) doit faire
      // progresser le total réel. Sinon on rend exactement le plan protégé.
      if(previousPlan!=null&&previousPlan.Rows!=null&&previousPlan.Rows.Count==result.Rows.Count){
        var oldRows=previousPlan.Rows.ToDictionary(x=>x.UnitId);bool sameMembers=result.Rows.All(x=>oldRows.ContainsKey(x.UnitId));
        bool oldEquipmentStillExists=sameMembers&&previousPlan.Rows.All(x=>x.RuneDetails.Count==6&&x.ArtifactDetails.Count==2&&x.RuneDetails.All(r=>runes.ContainsKey(r.Id))&&x.ArtifactDetails.All(a=>artifacts.ContainsKey(a.Id)));
        bool equipmentChanged=sameMembers&&result.Rows.Any(x=>{var old=oldRows[x.UnitId];return !new HashSet<long>(old.RuneDetails.Select(r=>r.Id)).SetEquals(x.RuneDetails.Select(r=>r.Id))||!new HashSet<long>(old.ArtifactDetails.Select(a=>a.Id)).SetEquals(x.ArtifactDetails.Select(a=>a.Id));});
        if(oldEquipmentStillExists&&equipmentChanged&&result.OptimizedTotal<=previousPlan.OptimizedTotal+0.5)return previousPlan;
      }
      return result;
    }
    static void AddRune(Dictionary<string,object>d,Dictionary<long,Rune> found,bool project=true){if(d==null)return;long id=L(G(d,"rune_id"));if(id<=0)return;int rank=I(G(d,"rank")),cls=I(G(d,"class")),stars=cls>=10?cls-10:cls;int originalLevel=I(G(d,"upgrade_curr")),quality=rank>=10?rank-10:rank;var r=new Rune{Id=id,Slot=I(G(d,"slot_no")),Set=I(G(d,"set_id")),Grade=stars,Ancient=rank>=10||cls>=10,Level=project?15:originalLevel,CurrentLevel=originalLevel,Quality=quality};var main=A(G(d,"pri_eff"));if(main.Length>1){r.Main=I(main[0]);r.MainValue=project?MainStatAt15(r.Main,stars,F(main[1])):F(main[1]);AddEffect(r,r.Main,r.MainValue,0,false);}var innate=A(G(d,"prefix_eff"));if(innate.Length>1)AddEffect(r,I(innate[0]),F(innate[1]),0,true,false,true);foreach(var raw in A(G(d,"sec_eff"))){var e=A(raw);if(e.Length>1)AddEffect(r,I(e[0]),F(e[1]),e.Length>3?F(e[3]):0,true,e.Length>2&&I(e[2])!=0,false);}if(project){ProjectRemainingRolls(r,originalLevel);var actual=new Dictionary<long,Rune>();AddRune(d,actual,false);r.Actual=actual[id];}found[id]=r;}
    static double MainStatAt15(int stat,int stars,double current){if(stars>=6){if(stat==1)return 2448;if(stat==3||stat==5)return 160;if(stat==2||stat==4||stat==6)return 63;if(stat==8)return 42;if(stat==9)return 58;if(stat==10)return 80;if(stat==11||stat==12)return 64;}if(stars==5){if(stat==1)return 2088;if(stat==3||stat==5)return 135;if(stat==2||stat==4||stat==6)return 51;if(stat==8)return 39;if(stat==9)return 47;if(stat==10)return 65;if(stat==11||stat==12)return 51;}return current;}
    static void AddEffect(Rune r,int stat,double value,double grind,bool displayed,bool gemmed=false,bool innate=false){if(stat<=0)return;r.Stats[stat]=(r.Stats.ContainsKey(stat)?r.Stats[stat]:0)+value+grind;if(displayed)r.Effects.Add(new RuneEffect{Stat=stat,Base=value,Grind=grind,Gemmed=gemmed,Innate=innate});}
    static double ArtifactRollMax(int id){if(id>=400&&id<=409||id==411)return 6;if(id>=300&&id<=304)return 5;if(id>=305&&id<=309)return 6;if(id>=200&&id<=202)return 14;if(id==203||id==207)return 6;if(id==204||id==206||id==226)return id==206?6:5;if(id==205||id==208||id==209||id==210||id==211||id==212||id==214||id==224||id==225)return 4;if(id==213)return 3;if(id==215)return 8;if(id==216||id==217)return 6;if(id==218)return .3;if(id==219||id==220)return 4;if(id==221)return 40;if(id==222)return 6;if(id==223)return 12;return 6;}
    static void AddArtifact(Dictionary<string,object>d,Dictionary<long,Artifact> found){if(d==null)return;long id=L(G(d,"rid"));if(id<=0)return;var pri=A(G(d,"pri_effect"));var a=new Artifact{Id=id,Type=I(G(d,"type")),Attribute=I(G(d,"attribute")),Style=I(G(d,"unit_style")),Rank=I(G(d,"natural_rank")),Level=I(G(d,"level")),Main=pri.Length>0?I(pri[0]):0,MainValue=pri.Length>1?F(pri[1]):0};double efficiency=0;foreach(var raw in A(G(d,"sec_effects"))){var e=A(raw);if(e.Length>1){int effect=I(e[0]);double value=Math.Abs(F(e[1]));efficiency+=value/Math.Max(.01,ArtifactRollMax(effect));bool converted=e.Length>3&&I(e[3])!=0;a.Effects.Add((converted?"↻ ":"")+ArtifactEffect(effect)+"  +"+F(e[1]).ToString("0.##")+(e.Length>2&&F(e[2])>0?"  ["+F(e[2]).ToString("0")+"]":""));}}a.Quality=efficiency*ArtifactEfficiencyWeight;found[id]=a;}
    static void SimulateAllAtLevel40(List<Unit> units,Dictionary<int,Level40Stats> official){var exact=units.Where(x=>x.Grade==6&&x.Level>=40).GroupBy(x=>x.Master).ToDictionary(g=>g.Key,g=>g.First());var equivalent=units.Where(x=>x.Grade==6&&x.Level>=40&&x.SkillGroup>0).GroupBy(x=>x.SkillGroup+":"+x.Element).ToDictionary(g=>g.Key,g=>g.First());foreach(var u in units){u.OriginalLevel=u.Level;Level40Stats known;Unit reference;if(exact.TryGetValue(u.Master,out reference)||equivalent.TryGetValue(u.SkillGroup+":"+u.Element,out reference)){u.Hp=reference.Hp;u.Atk=reference.Atk;u.Def=reference.Def;u.Spd=reference.Spd;u.Cr=reference.Cr;u.Cd=reference.Cd;u.Res=reference.Res;u.Acc=reference.Acc;}else if(official.TryGetValue(u.Master,out known)){u.Hp=known.hp;u.Atk=known.atk;u.Def=known.def;u.Spd=known.spd;}else{int grade=Math.Max(1,Math.Min(6,u.Grade)),maxLevel=10+grade*5,level=Math.Max(1,Math.Min(maxLevel,u.Level));double evolution=Math.Pow(1.36,6-grade),remaining=1+(maxLevel-level)*.015;u.Hp*=evolution*remaining;u.Atk*=evolution*remaining;u.Def*=evolution*remaining;}u.Level=40;u.Grade=6;}}
    static double UnitBase(Unit u){double relic=u.Hp*u.RelicHpPct/100*(HpStatWeight/15)+u.Atk*u.RelicAtkPct/100*AttackStatWeight+u.Def*u.RelicDefPct/100*DefenseStatWeight;return StatScore(u,u.Hp,u.Atk,u.Def,u.Spd,u.Cr,u.Cd,u.Res,u.Acc,0)+relic+SkillUpPoints(u,u.SkillUps)*SkillUpValue(u)+(u.NaturalStars==5?NaturalFiveStarBonus:0)+900;}
    static double PureMonsterScore(Unit u){double neutral=StatScore(u,u.Hp,u.Atk,u.Def,u.Spd,u.Cr,u.Cd,u.Res,u.Acc,0)+900;return neutral*ElementMultiplier(u);}
    // Le classement hors équipements récompense une vraie spécialisation naturelle :
    // HP est ramené à l'échelle ATK/DEF. Seule une statistique dépassant nettement
    // la moyenne reçoit un bonus progressif ; aucune statistique faible n'est pénalisée.
    // Round 7 (2026-09-15) : post communautaire donne 53.93 en "Total attack power" par
    // skill-up reel, 0 en Attribute, SANS normalisation par le nombre max de skill-ups du
    // monstre (contrairement a l'ancien 22.0*12/MaxSkillUps). Force par Jeremy.
    static double SkillUpValue(Unit u){return 53.93;}
    static double SkillUpPoints(Unit u,int count){return Math.Max(0,count);}
    static double SkillUpGain(Unit u,int missing){return SkillUpPoints(u,missing)*SkillUpValue(u)*ElementMultiplier(u);}
    static double StatScore(Unit u,double hp,double atk,double def,double spd,double cr,double cd,double res,double acc,double element){return hp*(HpStatWeight/15.0)+atk*AttackStatWeight+def*DefenseStatWeight+spd*SpeedStatWeight+cr*CritRateStatWeight+cd*CritDamageStatWeight+res*ResistanceStatWeight+acc*AccuracyStatWeight;}
    static double ElementFactor(Unit u){return u.Element.Equals("water",StringComparison.OrdinalIgnoreCase)?1:u.Element.Equals("fire",StringComparison.OrdinalIgnoreCase)?-1:0;}
    static double ElementMultiplier(Unit u){return 1+ElementFactor(u)*.10;}
    static List<long> GameWorldBossOrder(IEnumerable<string> events){var decks=new List<List<long>>();if(events==null)return new List<long>();var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};foreach(string json in events){if(string.IsNullOrWhiteSpace(json)||json.IndexOf("\"command\":\"BattleWorldBossStart_v2\"",StringComparison.OrdinalIgnoreCase)<0)continue;try{var root=D(js.DeserializeObject(json));if(root==null||G(root,"ret_code")!=null)continue;var ids=A(G(root,"unit_id_list")).Select(x=>{var d=D(x);return d==null?L(x):L(G(d,"unit_id"));}).Where(x=>x>0).ToList();if(ids.Count==20)decks.Add(ids);}catch{}}if(decks.Count<3)return new List<long>();var latest=decks.Skip(decks.Count-3).SelectMany(x=>x).ToList();return latest.Count==60&&latest.Distinct().Count()==60?latest:new List<long>();}
    static double RuneScore(Unit u,Rune r){double score=0;foreach(var kv in r.Stats){double v=kv.Value;switch(kv.Key){case 1:score+=v*(HpStatWeight/15);break;case 2:score+=u.Hp*v/100*(HpStatWeight/15);break;case 3:score+=v*AttackStatWeight;break;case 4:score+=u.Atk*v/100*AttackStatWeight;break;case 5:score+=v*DefenseStatWeight;break;case 6:score+=u.Def*v/100*DefenseStatWeight;break;case 8:score+=v*SpeedStatWeight;break;case 9:score+=v*CritRateStatWeight;break;case 10:score+=v*CritDamageStatWeight;break;case 11:score+=v*ResistanceStatWeight;break;case 12:score+=v*AccuracyStatWeight;break;}}score+=RuneQuality(r);return score;}
    // Score de qualité réel retrouvé le 2026-09-14 à partir de 20 captures d'écran
    // (score affiché en jeu sur la fiche de la rune). Remplace l'ancien terme
    // arbitraire (Grade*3.75+Level*1.2) qui ignorait totalement les sous-stats
    // réelles. Formule : 68*efficacité + 21 (si rareté Legend) + 59, où
    // efficacité = somme sur les 4 sous-stats de (valeur+meule)/max théorique.
    // Ajustement sur 20 runes 6* (12 Legend/Hero mixtes + 8 Hero) : erreur
    // moyenne 5.8, erreur max 15, sur des scores 174-258 (~2.7% d'erreur
    // moyenne) — imparfait mais bien plus fiable que l'ancien terme, qui
    // n'utilisait ni les vraies valeurs de sous-stats ni la rareté.
    // Table de max théorique (+12, 4 rolls) par stat et par grade (1 à 6),
    // source : wiki officiel summonerswar.fandom.com/wiki/Runes. Valeurs
    // ancient (grade 6 uniquement, pas de données ancient pour grade<6) :
    // source captures d'écran utilisateur (tableau meules/gemmes en jeu).
    static double RuneRollMax(int stat,int grade,bool ancient){int g=Math.Max(1,Math.Min(6,grade))-1;if(ancient&&grade>=6){switch(stat){case 1:return 1960;case 3:case 5:return 105;case 2:case 4:case 6:return 42;case 8:return 31;case 9:return 31;case 10:return 37;case 11:case 12:return 42;}}switch(stat){case 1:return new double[]{300,525,825,1125,1500,1875}[g];case 2:case 4:case 6:return new double[]{10,15,25,30,35,40}[g];case 3:case 5:return new double[]{20,25,40,50,75,100}[g];case 8:return new double[]{5,10,15,20,25,30}[g];case 9:return new double[]{5,10,15,20,25,30}[g];case 10:return new double[]{10,15,20,25,25,35}[g];case 11:case 12:return new double[]{10,15,20,25,35,40}[g];}return 0;}
    static double RuneQuality(Rune r){double eff=0;foreach(var e in r.Effects){if(e.Innate||e.Stat==r.Main)continue;double max=RuneRollMax(e.Stat,r.Grade,r.Ancient);if(max>0)eff+=(e.Base+e.Grind)/max;}double legendBonus=r.Quality>=5?21.0:0.0;return 68.0*eff+legendBonus+59.0;}
    static double SetShare(Unit u,int set){switch(set){case 1:return u.Hp*.15/2*(HpStatWeight/15);case 2:return u.Def*.15/2*DefenseStatWeight;case 3:return u.Spd*.25/4*SpeedStatWeight;case 4:return 12.0/2*CritRateStatWeight;case 5:return 40.0/4*CritDamageStatWeight;case 6:return 20.0/2*AccuracyStatWeight;case 7:return 20.0/2*ResistanceStatWeight;case 8:return u.Atk*.35/4*AttackStatWeight;case 19:return u.Atk*.08/2*AttackStatWeight;case 20:return u.Def*.08/2*DefenseStatWeight;case 21:return u.Hp*.08/2*(HpStatWeight/15);case 22:return 10.0/2*AccuracyStatWeight;case 23:return 10.0/2*ResistanceStatWeight;
      // Sets a effet special (pas un %stat classique) : valeur par piece calibree a partir
      // d'un post communautaire (score reel mesure en jeu, monstres "nus" sans runes,
      // 6*, niv 40) partage par l'utilisateur le 2026-09-15. Valeur totale du set complet
      // divisee par le nombre de pieces (2pc ou 4pc), coherente avec le pattern deja utilise
      // pour Blade/Rage/Focus/Endure/Accuracy/Tolerance ci-dessus. Constante, independante
      // du monstre (contrairement a Energy/Guard/Swift/Fatal/Fight/Determination/Enhance).
      case 10:return 77.275;  // Despair (4pc)
      case 11:return 78.75;   // Vampire (4pc)
      case 13:return 80.65;   // Violent (4pc)
      case 14:return 67.25;   // Nemesis (2pc)
      case 15:return 66.5;    // Will (2pc)
      case 16:return 67.05;   // Shield (2pc)
      case 17:return 66.5;    // Revenge (2pc)
      case 18:return 67.75;   // Destroy (2pc)
      default:return 8;}}
    static double ArtifactMainAffinity(Unit u,int main){double hp=u.Hp/15.0,atk=u.Atk,def=u.Def,best=Math.Max(hp,Math.Max(atk,def));if(best<=0)return 1;double matching=main==100?hp:main==101?atk:main==102?def:best;return .65+.35*Math.Max(0,Math.Min(1,matching/best));}
    static double ArtifactScore(Unit u,Artifact a){double main=0;if(a.Main==100)main=a.MainValue*(HpStatWeight/15);else if(a.Main==101)main=a.MainValue*AttackStatWeight;else if(a.Main==102)main=a.MainValue*DefenseStatWeight;return a.Quality+main*ArtifactMainAffinity(u,a.Main);}
    static int PreferredArtifactMain(Unit u){return u.Style==1?101:u.Style==2?102:u.Style==3?100:0;}
    static bool Compatible(Unit u,Artifact a,int type){if(a.Type!=type)return false;return type==1?(a.Attribute==0||a.Attribute==98||u.Attribute==0||a.Attribute==u.Attribute):(a.Style==0||a.Style==98||u.Style==0||a.Style==u.Style);}
    static double CandidateScore(Unit u,IEnumerable<Rune> runes,IEnumerable<Artifact> artifacts){double score=u.Base;for(int s=1;s<=6;s++){var best=runes.Where(r=>r.Slot==s).Select(r=>RuneScore(u,r)+SetShare(u,r.Set)).DefaultIfEmpty(0).Max();score+=best;}for(int t=1;t<=2;t++)score+=artifacts.Where(a=>Compatible(u,a,t)).Select(a=>ArtifactScore(u,a)).DefaultIfEmpty(0).Max();return score*ElementMultiplier(u);}
    sealed class Build {public List<Rune> Runes=new List<Rune>();public double Raw,Estimate;}
    static List<string> BuildInventoryKeys(List<Unit> units,Dictionary<long,Rune> runes,Dictionary<long,Artifact> artifacts){var keys=new List<string>();keys.AddRange(units.OrderBy(x=>x.Id).Select(x=>"U:"+x.Id+":"+x.Master));keys.AddRange(runes.Values.OrderBy(x=>x.Id).Select(x=>"R:"+x.Id+":"+x.Slot+":"+x.Set+":"+x.CurrentLevel+":"+x.Main+":"+x.MainValue.ToString("0.###",CultureInfo.InvariantCulture)+":"+string.Join(",",x.Stats.OrderBy(s=>s.Key).Select(s=>s.Key+"="+s.Value.ToString("0.###",CultureInfo.InvariantCulture)))));keys.AddRange(artifacts.Values.OrderBy(x=>x.Id).Select(x=>"A:"+x.Id+":"+x.Type+":"+x.Main+":"+x.MainValue.ToString("0.###",CultureInfo.InvariantCulture)+":"+x.Quality.ToString("0.###",CultureInfo.InvariantCulture)));return keys;}
    static long KeyId(string key){if(string.IsNullOrEmpty(key)||key.Length<3)return 0;int end=key.IndexOf(':',2);long id;return long.TryParse(end<0?key.Substring(2):key.Substring(2,end-2),out id)?id:0;}
    static HashSet<long> MeaningfulInventoryTargets(List<Unit> units,Dictionary<long,Rune> runes,Dictionary<long,Artifact> artifacts,WorldBossResult previous,List<string> currentKeys){
      var unlocked=new HashSet<long>();if(previous==null||previous.InventoryKeys==null||previous.InventoryKeys.Count==0)return unlocked;
      var oldKeys=new HashSet<string>(previous.InventoryKeys);var oldRuneIds=new HashSet<long>(oldKeys.Where(x=>x.StartsWith("R:")).Select(KeyId));var oldArtifactIds=new HashSet<long>(oldKeys.Where(x=>x.StartsWith("A:")).Select(KeyId));var changedRunes=currentKeys.Where(x=>x.StartsWith("R:")).Select(KeyId).Where(id=>!oldRuneIds.Contains(id)&&runes.ContainsKey(id)).Distinct().Select(x=>runes[x]).ToList();var changedArtifacts=currentKeys.Where(x=>x.StartsWith("A:")).Select(KeyId).Where(id=>!oldArtifactIds.Contains(id)&&artifacts.ContainsKey(id)).Distinct().Select(x=>artifacts[x]).ToList();var plans=previous.Rows.ToDictionary(x=>x.UnitId);
      foreach(var u in units)if(!plans.ContainsKey(u.Id))unlocked.Add(u.Id);
      // Une nouvelle rune ne libère qu'un seul build : celui qui gagne réellement
      // le plus avec elle. Une amélioration faible ne dépasse pas le seuil et ne
      // déclenche donc absolument aucun changement.
      foreach(var fresh in changedRunes){Unit best=null;double bestGain=MinimumUsefulEquipmentGain;foreach(var u in units){WorldBossRow plan;if(!plans.TryGetValue(u.Id,out plan))continue;var oldView=plan.RuneDetails.FirstOrDefault(x=>x.Slot==fresh.Slot);Rune old;if(oldView==null||!runes.TryGetValue(oldView.Id,out old))continue;double gain=RuneScore(u,fresh)+SetShare(u,fresh.Set)-RuneScore(u,old)-SetShare(u,old.Set);if(gain>bestGain){bestGain=gain;best=u;}}if(best!=null)unlocked.Add(best.Id);}
      foreach(var fresh in changedArtifacts){Unit best=null;double bestGain=MinimumUsefulEquipmentGain;foreach(var u in units){WorldBossRow plan;if(!plans.TryGetValue(u.Id,out plan)||!Compatible(u,fresh,fresh.Type))continue;long oldId=plan.ArtifactDetails.Where(x=>x.Kind==(fresh.Type==1?"Élément":"Type")).Select(x=>x.Id).FirstOrDefault();Artifact old;if(oldId<=0||!artifacts.TryGetValue(oldId,out old))continue;double gain=ArtifactScore(u,fresh)-ArtifactScore(u,old);if(gain>bestGain){bestGain=gain;best=u;}}if(best!=null)unlocked.Add(best.Id);}
      return unlocked;
    }
    static void LockCurrentEquipment(List<Unit> units,Dictionary<long,Rune> runes,Dictionary<long,Artifact> artifacts,HashSet<long> unlocked){var reservedRunes=new HashSet<long>(units.SelectMany(x=>x.Assigned).Select(x=>x.Id));var reservedArtifacts=new HashSet<long>(units.SelectMany(x=>x.AssignedArtifacts).Select(x=>x.Id));foreach(var u in units){if(unlocked.Contains(u.Id))continue;if(u.Assigned.Count==0){var currentRunes=u.CurrentRunes.Where(runes.ContainsKey).Distinct().Select(x=>runes[x]).GroupBy(x=>x.Slot).Select(x=>x.First()).Where(x=>!reservedRunes.Contains(x.Id)).ToList();u.Assigned.AddRange(currentRunes);foreach(var rune in currentRunes)reservedRunes.Add(rune.Id);}if(u.AssignedArtifacts.Count==0){var currentArtifacts=u.CurrentArtifacts.Where(artifacts.ContainsKey).Distinct().Select(x=>artifacts[x]).GroupBy(x=>x.Type).Select(x=>x.First()).Where(x=>!reservedArtifacts.Contains(x.Id)).ToList();u.AssignedArtifacts.AddRange(currentArtifacts);foreach(var artifact in currentArtifacts)reservedArtifacts.Add(artifact.Id);}}}
    static void LockCompletedPlan(List<Unit> units,Dictionary<long,Rune> runes,Dictionary<long,Artifact> artifacts,WorldBossResult previous,HashSet<long> unlocked){if(previous==null)return;var plans=previous.Rows.ToDictionary(x=>x.UnitId);var reservedRunes=new HashSet<long>(units.SelectMany(x=>x.Assigned).Select(x=>x.Id));var reservedArtifacts=new HashSet<long>(units.SelectMany(x=>x.AssignedArtifacts).Select(x=>x.Id));foreach(var u in units){if(unlocked.Contains(u.Id))continue;WorldBossRow plan;if(!plans.TryGetValue(u.Id,out plan))continue;var wantedRunes=plan.RuneDetails.Select(x=>x.Id).Where(x=>x>0).Distinct().ToList();var wantedArtifacts=plan.ArtifactDetails.Select(x=>x.Id).Where(x=>x>0).Distinct().ToList();bool validRunes=u.Assigned.Count==0&&wantedRunes.Count==6&&plan.RuneDetails.Count(x=>x.Set=="Intangible")<=1&&wantedRunes.All(runes.ContainsKey)&&wantedRunes.All(x=>!reservedRunes.Contains(x));bool validArtifacts=u.AssignedArtifacts.Count==0&&wantedArtifacts.Count==2&&wantedArtifacts.All(artifacts.ContainsKey)&&wantedArtifacts.All(x=>!reservedArtifacts.Contains(x));if(validRunes){u.Assigned.AddRange(wantedRunes.Select(x=>runes[x]));foreach(long id in wantedRunes)reservedRunes.Add(id);}if(validArtifacts){u.AssignedArtifacts.AddRange(wantedArtifacts.Select(x=>artifacts[x]));foreach(long id in wantedArtifacts)reservedArtifacts.Add(id);}}}
    static void AssignEquipment(List<Unit> units,Dictionary<long,Rune> runes,Dictionary<long,Artifact> artifacts){var freeRunes=new HashSet<long>(runes.Keys);var freeArtifacts=new HashSet<long>(artifacts.Keys);foreach(var locked in units.SelectMany(x=>x.Assigned))freeRunes.Remove(locked.Id);foreach(var locked in units.SelectMany(x=>x.AssignedArtifacts))freeArtifacts.Remove(locked.Id);foreach(var u in units){if(u.Assigned.Count!=6)AssignRuneBuilds(new List<Unit>{u},runes.Values.Where(r=>freeRunes.Contains(r.Id)).ToList());if(!u.AssignedArtifacts.Any(a=>a.Type==1))AssignArtifacts(new List<Unit>{u},artifacts.Values.Where(a=>a.Type==1&&freeArtifacts.Contains(a.Id)).ToList(),1);if(!u.AssignedArtifacts.Any(a=>a.Type==2))AssignArtifacts(new List<Unit>{u},artifacts.Values.Where(a=>a.Type==2&&freeArtifacts.Contains(a.Id)).ToList(),2);foreach(var r in u.Assigned)freeRunes.Remove(r.Id);foreach(var a in u.AssignedArtifacts)freeArtifacts.Remove(a.Id);}if(units.Any(x=>x.Assigned.Count(r=>r.Set==25)>1))throw new InvalidOperationException("Une attribution contient plusieurs runes Intangibles.");}
    static void AssignRuneBuilds(List<Unit> units,List<Rune> runes){var free=new HashSet<long>(runes.Select(r=>r.Id));foreach(var used in units.SelectMany(x=>x.Assigned))free.Remove(used.Id);foreach(var u in units.Where(x=>x.Assigned.Count<6)){var fixedRunes=new List<Rune>(u.Assigned);var beam=new List<Build>{new Build{Runes=new List<Rune>(fixedRunes),Raw=fixedRunes.Sum(r=>RuneScore(u,r)),Estimate=fixedRunes.Sum(r=>RuneScore(u,r))+PartialSetEstimate(u,fixedRunes)}};for(int slot=1;slot<=6;slot++){if(fixedRunes.Any(x=>x.Slot==slot))continue;var eligible=runes.Where(r=>r.Slot==slot&&free.Contains(r.Id));var options=eligible.GroupBy(r=>r.Set).OrderBy(g=>g.Key).SelectMany(g=>g.OrderByDescending(r=>RuneScore(u,r)+SetShare(u,r.Set)).ThenBy(r=>r.Id).Take(3)).Concat(eligible.OrderByDescending(r=>RuneScore(u,r)+SetShare(u,r.Set)).ThenBy(r=>r.Id).Take(30)).GroupBy(r=>r.Id).Select(g=>g.First()).OrderBy(r=>r.Id).ToList();var next=new List<Build>();foreach(var b in beam)foreach(var r in options){if(r.Set==25&&b.Runes.Any(x=>x.Set==25))continue;var nr=new List<Rune>(b.Runes){r};double raw=b.Raw+RuneScore(u,r);next.Add(new Build{Runes=nr,Raw=raw,Estimate=raw+PartialSetEstimate(u,nr)});}beam=next.GroupBy(b=>RuneSetSignature(b.Runes)).SelectMany(g=>g.OrderByDescending(b=>b.Estimate).ThenBy(b=>string.Join(",",b.Runes.Select(r=>r.Id))).Take(2)).OrderByDescending(b=>b.Estimate).ThenBy(b=>string.Join(",",b.Runes.Select(r=>r.Id))).Take(500).ToList();if(beam.Count==0)break;}var best=beam.Where(b=>IsCompleteSetBuild(b.Runes)).OrderByDescending(b=>b.Raw+CompletedSetBonus(u,b.Runes)).ThenBy(b=>string.Join(",",b.Runes.Select(r=>r.Id))).FirstOrDefault();if(best==null)best=FindCompleteBuild(u,runes,free);if(best==null)continue;u.Assigned.Clear();u.Assigned.AddRange(best.Runes);foreach(var r in best.Runes)free.Remove(r.Id);}}
    static string RuneSetSignature(IEnumerable<Rune> source){return string.Join(",",source.GroupBy(r=>r.Set).OrderBy(g=>g.Key).Select(g=>g.Key+"x"+g.Count()));}
    static int SetPieces(int set){return set==3||set==5||set==8||set==10||set==11||set==13||set==24?4:2;}
    static Build RepairCompleteBuild(Unit u,List<Rune> original,List<Rune> all,HashSet<long> free){Build best=null;double bestScore=double.NegativeInfinity;var choices=new Dictionary<int,List<Rune>>();for(int slot=1;slot<=6;slot++)choices[slot]=all.Where(r=>r.Slot==slot&&free.Contains(r.Id)).GroupBy(r=>r.Set).SelectMany(g=>g.OrderByDescending(r=>RuneScore(u,r)).Take(4)).ToList();Action<List<Rune>> consider=list=>{if(!IsCompleteSetBuild(list))return;double score=list.Sum(r=>RuneScore(u,r))+CompletedSetBonus(u,list);if(score>bestScore){bestScore=score;best=new Build{Runes=new List<Rune>(list),Raw=list.Sum(r=>RuneScore(u,r)),Estimate=score};}};for(int a=0;a<6;a++)foreach(var ra in choices[original[a].Slot]){var one=new List<Rune>(original);one[a]=ra;consider(one);}if(best!=null)return best;for(int a=0;a<6;a++)for(int b=a+1;b<6;b++)foreach(var ra in choices[original[a].Slot])foreach(var rb in choices[original[b].Slot]){var two=new List<Rune>(original);two[a]=ra;two[b]=rb;consider(two);}return best;}
    static Build FindCompleteBuild(Unit u,List<Rune> all,HashSet<long> free){var bestBySlotSet=new Dictionary<string,Rune>();for(int slot=1;slot<=6;slot++)foreach(int set in SimulatableSets){var r=all.Where(x=>x.Slot==slot&&free.Contains(x.Id)&&(x.Set==set||x.Set==25)).OrderByDescending(x=>RuneScore(u,x)).FirstOrDefault();if(r!=null)bestBySlotSet[slot+":"+set]=r;}Build best=null;double score=double.NegativeInfinity;var four=SimulatableSets.Where(s=>SetPieces(s)==4).ToArray();var two=SimulatableSets.Where(s=>SetPieces(s)==2).ToArray();foreach(int a in four)foreach(int b in two)SearchSetTemplate(u,new[]{a,a,a,a,b,b},0,new Dictionary<int,int>(),new List<Rune>(),bestBySlotSet,ref best,ref score);for(int i=0;i<two.Length;i++)for(int j=i;j<two.Length;j++)for(int k=j;k<two.Length;k++)SearchSetTemplate(u,new[]{two[i],two[i],two[j],two[j],two[k],two[k]},0,new Dictionary<int,int>(),new List<Rune>(),bestBySlotSet,ref best,ref score);return best;}
    static void SearchSetTemplate(Unit u,int[] wanted,int slot,Dictionary<int,int> used,List<Rune> picked,Dictionary<string,Rune> available,ref Build best,ref double bestScore){if(slot==6){if(!IsCompleteSetBuild(picked))return;double raw=picked.Sum(r=>RuneScore(u,r)),score=raw+CompletedSetBonus(u,picked);if(score>bestScore){bestScore=score;best=new Build{Runes=new List<Rune>(picked),Raw=raw,Estimate=score};}return;}foreach(int set in wanted.Distinct()){int need=wanted.Count(x=>x==set),have=used.ContainsKey(set)?used[set]:0;if(have>=need)continue;Rune rune;if(!available.TryGetValue((slot+1)+":"+set,out rune))continue;used[set]=have+1;picked.Add(rune);SearchSetTemplate(u,wanted,slot+1,used,picked,available,ref best,ref bestScore);picked.RemoveAt(picked.Count-1);if(have==0)used.Remove(set);else used[set]=have;}}
    static bool IsCompleteSetBuild(IEnumerable<Rune> source){var runes=source.ToList();if(runes.Count!=6||runes.Count(r=>r.Set==25)>1)return false;int wild=runes.Count(r=>r.Set==25),needed=0;foreach(var group in runes.Where(r=>r.Set!=25).GroupBy(r=>r.Set)){int pieces=SetPieces(group.Key),remainder=group.Count()%pieces;if(remainder>0)needed+=pieces-remainder;}return needed<=wild&&(wild-needed)%2==0;}
    static readonly int[] SimulatableSets={1,2,3,4,5,6,7,8,10,11,13,14,15,16,17,18,19,20,21,22,23,24};
    static double BestSetValueWithIntangibles(Unit u,IEnumerable<Rune> source,bool completedOnly){var runes=source.ToList();int wild=runes.Count(r=>r.Set==25);var counts=runes.Where(r=>r.Set!=25).GroupBy(r=>r.Set).ToDictionary(g=>g.Key,g=>g.Count());var dp=Enumerable.Repeat(double.NegativeInfinity,wild+1).ToArray();dp[0]=0;foreach(int set in SimulatableSets){int baseCount=counts.ContainsKey(set)?counts[set]:0,pieces=SetPieces(set);double share=SetShare(u,set);var next=Enumerable.Repeat(double.NegativeInfinity,wild+1).ToArray();for(int used=0;used<=wild;used++)if(!double.IsNegativeInfinity(dp[used]))for(int add=0;used+add<=wild;add++){int total=baseCount+add;double value=completedOnly?share*pieces*(total/pieces):share*total;next[used+add]=Math.Max(next[used+add],dp[used]+value);}dp=next;}double best=dp.Where(x=>!double.IsNegativeInfinity(x)).DefaultIfEmpty(0).Max();return best;}
    static double PartialSetEstimate(Unit u,IEnumerable<Rune> source){var runes=source.ToList();double ordinary=runes.Where(r=>r.Set!=25).Sum(r=>SetShare(u,r.Set));int wild=runes.Count(r=>r.Set==25);double bestWildcard=SimulatableSets.Select(set=>SetShare(u,set)).DefaultIfEmpty(0).Max();return ordinary+wild*bestWildcard;}
    static double CompletedSetBonus(Unit u,IEnumerable<Rune> runes){return BestSetValueWithIntangibles(u,runes,true);}
    static void AssignArtifacts(List<Unit> units,List<Artifact> artifacts,int type){var freeArtifacts=new HashSet<long>(artifacts.Select(a=>a.Id));foreach(var used in units.SelectMany(u=>u.AssignedArtifacts))freeArtifacts.Remove(used.Id);foreach(var u in units){if(u.AssignedArtifacts.Any(a=>a.Type==type))continue;var best=artifacts.Where(a=>freeArtifacts.Contains(a.Id)&&Compatible(u,a,type)).OrderByDescending(a=>ArtifactScore(u,a)).ThenBy(a=>a.Id).FirstOrDefault();if(best==null)continue;u.AssignedArtifacts.Add(best);freeArtifacts.Remove(best.Id);}}
    static double Current(Unit u,Dictionary<long,Rune> runes,Dictionary<long,Artifact> artifacts){double s=u.Base;var equipped=u.CurrentRunes.Where(runes.ContainsKey).Select(id=>WorldBossEvaluationRune(runes[id])).ToList();s+=equipped.Sum(r=>RuneScore(u,r))+CompletedSetBonus(u,equipped);foreach(long id in u.CurrentArtifacts)if(artifacts.ContainsKey(id))s+=ArtifactScore(u,artifacts[id]);return s*ElementMultiplier(u);}
    // +12 and above: retain the World Boss +15 projection. Lower-level test
    // runes (+0, +2, +5...) are evaluated as observed, without invented rolls.
    static Rune WorldBossEvaluationRune(Rune rune){return rune.CurrentLevel>=12?rune:(rune.Actual??rune);}
    static double Optimized(Unit u){return (u.Base+u.Assigned.Sum(r=>RuneScore(u,r))+CompletedSetBonus(u,u.Assigned)+u.AssignedArtifacts.Sum(a=>ArtifactScore(u,a)))*ElementMultiplier(u);}
    static string SetSummary(Unit u,IEnumerable<Rune> source){var runes=source.ToList();var counts=runes.Where(r=>r.Set!=25).GroupBy(r=>r.Set).ToDictionary(g=>g.Key,g=>g.Count());int wild=runes.Count(r=>r.Set==25);if(IsCompleteSetBuild(runes)){foreach(int set in counts.Keys.ToList()){int rem=counts[set]%SetPieces(set);if(rem>0){int add=SetPieces(set)-rem;counts[set]+=add;wild-=add;}}if(wild>0){int best=SimulatableSets.Where(s=>SetPieces(s)==2).OrderByDescending(s=>SetShare(u,s)).First();counts[best]=(counts.ContainsKey(best)?counts[best]:0)+wild;wild=0;}}else if(wild>0)counts[25]=wild;string summary=string.Join(" + ",counts.OrderByDescending(g=>g.Value).ThenBy(g=>SetLabel(g.Key)).Select(g=>SetLabel(g.Key)+" ×"+g.Value));return IsCompleteSetBuild(runes)?summary:"Broken • "+summary;}
    static string SetLabel(int set){var one=new[]{new{I=1,N="Energy"},new{I=2,N="Guard"},new{I=3,N="Swift"},new{I=4,N="Blade"},new{I=5,N="Rage"},new{I=6,N="Focus"},new{I=7,N="Endure"},new{I=8,N="Fatal"},new{I=10,N="Despair"},new{I=11,N="Vampire"},new{I=13,N="Violent"},new{I=14,N="Nemesis"},new{I=15,N="Will"},new{I=16,N="Shield"},new{I=17,N="Revenge"},new{I=18,N="Destroy"},new{I=19,N="Fight"},new{I=20,N="Determination"},new{I=21,N="Enhance"},new{I=22,N="Accuracy"},new{I=23,N="Tolerance"},new{I=24,N="Seal"},new{I=25,N="Intangible"}};var x=one.FirstOrDefault(v=>v.I==set);return x==null?"Set "+set:x.N;}
    static string StatLabel(int id){return id==1?"HP flat":id==2?"HP%":id==3?"ATK flat":id==4?"ATK%":id==5?"DEF flat":id==6?"DEF%":id==8?"SPD":id==9?"CRate":id==10?"CDmg":id==11?"RES":id==12?"ACC":"Stat #"+id;}
    static string RuneStatDisplay(int id,double value,double grind,bool gemmed=false){string label=id==1?"HP":id==2?"HP":id==3?"ATK":id==4?"ATK":id==5?"DEF":id==6?"DEF":id==8?"SPD":id==9?"CRI Rate":id==10?"CRI Dmg":id==11?"Resistance":id==12?"Accuracy":"Stat #"+id;bool percent=id==2||id==4||id==6||id==9||id==10||id==11||id==12;return (gemmed?"↻ ":"")+label+" +"+(value+grind).ToString("0.##")+(percent?"%":"");}
    static string ArtifactMain(int id){return id==100?"HP flat":id==101?"ATK flat":id==102?"DEF flat":"Stat principale #"+id;}
    static string ArtifactEffect(int id){switch(id){case 200:return "ATK+ Prop. to Lost HP";case 201:return "DEF+ Prop. to Lost HP";case 202:return "SPD+ Prop. to Lost HP";case 203:return "SPD Under Inability +";case 204:return "ATK UP Effect +";case 205:return "DEF UP Effect +";case 206:return "SPD UP Effect +";case 207:return "CRIT Rate Increasing Effect +";case 208:return "Counterattack DMG +";case 209:return "Co-op Attack DMG +";case 210:return "Bomb DMG +";case 211:return "Damage Dealt by Reflect DMG +";case 212:return "Crushing Hit DMG +";case 213:return "Damage Received Under Inability -";case 214:return "CRIT DMG Taken -";case 215:return "Life Drain +";case 216:return "HP when Revived +";case 217:return "Attack Bar when Revived +";case 218:return "Add'l DMG Prop. to HP";case 219:return "Add'l DMG Prop. to ATK";case 220:return "Add'l DMG Prop. to DEF";case 221:return "Add'l DMG Prop. to SPD";case 222:return "CD+ as Enemy HP is More";case 223:return "CD+ as Enemy HP is Less";case 224:return "Own Turn 1-target CD+";case 225:return "Counterattack/Co-op Attack DMG +";case 226:return "ATK/DEF UP Effect +";case 300:return "DMG dealt on Fire +";case 301:return "DMG dealt on Water +";case 302:return "DMG dealt on Wind +";case 303:return "DMG dealt on Light +";case 304:return "DMG dealt on Dark +";case 305:return "DMG taken from Fire -";case 306:return "DMG taken from Water -";case 307:return "DMG taken from Wind -";case 308:return "DMG taken from Light -";case 309:return "DMG taken from Dark -";case 400:return "[Skill 1] CRIT DMG +";case 401:return "[Skill 2] CRIT DMG +";case 402:return "[Skill 3] CRIT DMG +";case 403:return "[Skill 4] CRIT DMG +";case 404:return "[Skill 1] Recovery +";case 405:return "[Skill 2] Recovery +";case 406:return "[Skill 3] Recovery +";case 407:return "[Skill 1] Accuracy +";case 408:return "[Skill 2] Accuracy +";case 409:return "[Skill 3] Accuracy +";case 410:return "[Skill 3/4] CRIT DMG +";case 411:return "First Attack CRIT DMG +";default:return "Effet #"+id;}}
    static string ArtifactRestriction(Artifact a,Unit u){if(a.Type==1){if(a.Attribute==0||a.Attribute==98)return "Intangible → "+FrenchElement(u.Element);return a.Attribute==1?"Eau":a.Attribute==2?"Feu":a.Attribute==3?"Vent":a.Attribute==4?"Lumière":a.Attribute==5?"Ténèbres":"Élément inconnu";}if(a.Style==0||a.Style==98)return "Intangible → "+StyleLabel(u.Style);return StyleLabel(a.Style);}
    static string StyleLabel(int style){return style==1?"Attaque":style==2?"Défense":style==3?"PV":style==4?"Support":"Type inconnu";}
    static string ArtifactIconKey(Artifact a){if(a.Type==1)return a.Attribute==1?"water":a.Attribute==2?"fire":a.Attribute==3?"wind":a.Attribute==4?"light":a.Attribute==5?"dark":"intangible";return a.Style==1?"attack":a.Style==2?"defense":a.Style==3?"hp":a.Style==4?"support":"intangible";}
    static string Element(int a){return a==1?"water":a==2?"fire":a==3?"wind":a==4?"light":a==5?"dark":"";}static string FrenchElement(string e){return e=="water"?"Eau":e=="fire"?"Feu":e=="wind"?"Vent":e=="light"?"Lumière":e=="dark"?"Ténèbres":e;}
  }
}
