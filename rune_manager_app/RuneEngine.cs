using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;

namespace RuneManagerModern {
  public sealed class SubStat {
    public string Stat=""; public double Value; public double Grind; public bool Gemmed;
    public string Display { get { string icon=Gemmed?"↻ ":""; string pct=Stat.EndsWith("%")?"%":""; string n=Label(Stat); if(Grind!=0)return icon+(Value+Grind).ToString("0")+n+pct+" · "+Value.ToString("0")+pct+" (+"+Grind.ToString("0")+pct+")";string baseText=icon+n+" +"+Value.ToString("0")+pct;return GrindableStat(Stat)?baseText+" (+0)":baseText; } }
    public string BaseDisplay { get { string pct=Stat.EndsWith("%")?"%":""; return Label(Stat)+" +"+Value.ToString("0")+pct; } }
    static string Label(string s){return s=="Atk%"?"Atk":s=="Def%"?"Def":s=="HP%"?"HP":s=="Atk+"?"Atk":s=="Def+"?"Def":s=="HP+"?"HP":s;}
    static bool GrindableStat(string s){return s=="HP+"||s=="HP%"||s=="Atk+"||s=="Atk%"||s=="Def+"||s=="Def%"||s=="Spd";}
  }
  public sealed class RuneRow {
    public long Id; public long EquippedUnitId; public DateTime Obtained; public string Set=""; public int Slot; public string Main {get;set;} public double MainValue; public string Innate {get;set;} public double InnateValue {get;set;} public int Grade; public int Stars; public int Level; public bool Ancient; public bool Equipped; public int EquippedMasterId; public string Marker {get;set;} public List<SubStat> Subs=new List<SubStat>();
    public string Quality {get{return Loc.Quality(Grade);}}
    public string Rune {get{return Set+" "+Quality+(Ancient?" "+Loc.T("rune_ancient"):"")+" Slot "+Slot+" +"+Level;}}
    public string MainDisplay {get{return Main.Length==0?"":MainLabel(Main)+" +"+MainValue.ToString("0")+(Main.EndsWith("%")?"%":"");}}
    public string InnateValueText {get{return Innate.Length==0?"":InnateValue.ToString("0");}}
    public string Stat1 {get{return Subs.Count>0?Subs[0].Display:"";}} public string Stat2 {get{return Subs.Count>1?Subs[1].Display:"";}} public string Stat3 {get{return Subs.Count>2?Subs[2].Display:"";}} public string Stat4 {get{return Subs.Count>3?Subs[3].Display:"";}}
    public string BestBuild {get;set;} public double Potential {get;set;} public double RefinementPotential {get;set;} public double RefinementGain {get;set;} public string RefinementPreset {get;set;} public string RefinementGainText {get{return "+"+RefinementGain.ToString("0.000");}} public double ReevalPriority {get;set;} public string ReevalBestBuild {get;set;} public string Action {get;set;} public string Recommendation {get;set;} public string RecommendSource="",RecommendTarget=""; public bool RecommendationInStock; public double[] Scores=new double[7];
    public RuneRow(){Main="";Innate="";Marker="";BestBuild="";RefinementPreset="";ReevalBestBuild="";Action="";Recommendation="";}
    static string MainLabel(string s){return s=="Atk%"?"Atk":s=="Def%"?"Def":s=="HP%"?"HP":s=="Atk+"?"Atk":s=="Def+"?"Def":s=="HP+"?"HP":s;}
  }
  public sealed class ReappraisalComparison {
    public long RuneId; public double BeforePotential; public double AfterPotential; public string BeforePreset=""; public string AfterPreset=""; public string BeforeStats=""; public string AfterStats="";
    public bool TakeAfter { get { return AfterPotential>BeforePotential; } }
  }
  public sealed class RuneChoiceComparison { public List<RuneRow> Choices=new List<RuneRow>(); }
  public sealed class CraftStock { public long Id {get;set;} public bool Ancient {get;set;} public string Type {get;set;} public string Set {get;set;} public string Stat {get;set;} public int Grade {get;set;} public int Amount {get;set;} public string RuneType{get{return Ancient?Loc.T("rune_ancient"):Loc.T("rune_normal");}} public string Quality{get{return Loc.CraftQuality(Grade);}} public CraftStock(){Type="";Set="";Stat="";} }
  public sealed class MonsterCatalogEntry { public int id; public string name="",family="",element="",icon=""; public int stars,skillups,familyid,skillgroup; }
  public sealed class FarmAdvice { public string Set=""; public int Slot,Owned,Target,Manque; public string Dungeon=""; }
  public sealed class SkillUpMonster { public long UnitId; public int MasterId,CatalogId,FamilyId,SkillGroup,NaturalStars,CurrentStars,Level,SkillUpsUsed,SkillUpsToMax; public string Name="",Family="",Element="",Icon=""; public bool Protected,FullySkilled,InSealedShrine; }
public sealed class SkillUpGroup { public SkillUpMonster Target; public List<SkillUpMonster> Fodders=new List<SkillUpMonster>(); public int Available{get{return Fodders.Count;}} public int UsableUpgrades{get{return Math.Min(Available,Math.Max(0,Target.SkillUpsToMax-Target.SkillUpsUsed));}} }
  public sealed class SkillUpRoster { public List<MonsterCatalogEntry> Catalog=new List<MonsterCatalogEntry>(); public List<SkillUpMonster> Owned=new List<SkillUpMonster>(); public HashSet<int> CrossGroups=new HashSet<int>(); }
  public sealed class SkillUpElementGap {
    public string Element="", Name="", Icon="", Family="";
    public int CatalogId, Used, ToMax;
    public bool Owned, FullySkilled;
    public SkillUpMonster Display;
    public int Need { get { return Owned?Math.Max(0,ToMax-Used):ToMax; } }
  }
  public sealed class SkillUpFamilyRow {
    public int FamilyId;
    public string Family="";
    public bool Collab;
    public List<SkillUpElementGap> Elements=new List<SkillUpElementGap>();
  }
  public sealed class SkillUpFamily {
    public int FamilyId, SkillGroup, NaturalStars;
    public string Family="", Search="";
    public List<SkillUpFamilyRow> Rows=new List<SkillUpFamilyRow>();
    public List<SkillUpElementGap> Elements=new List<SkillUpElementGap>();
    public int Gaps { get { return (Rows.Count>0?Rows.SelectMany(r=>r.Elements):Elements).Count(x=>!x.Owned||!x.FullySkilled); } }
  }
  public sealed class Preset {
    public string Name=""; public double ScoreFactor=1; public Dictionary<string,double> W=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase); public HashSet<string> Preferred=new HashSet<string>(StringComparer.OrdinalIgnoreCase); public HashSet<string> Accepted=new HashSet<string>(StringComparer.OrdinalIgnoreCase); public Dictionary<int,HashSet<string>> Main=new Dictionary<int,HashSet<string>>();
  }
  public static partial class RuneEngine {
    public static event Action<RuneChoiceComparison> RuneChoiceDetected;
    public static readonly HashSet<string> SeenRuneChoices=new HashSet<string>();
    static readonly Dictionary<int,string> Sets=new Dictionary<int,string>{{1,"Energy"},{2,"Guard"},{3,"Swift"},{4,"Blade"},{5,"Rage"},{6,"Focus"},{7,"Endure"},{8,"Fatal"},{10,"Despair"},{11,"Vampire"},{13,"Violent"},{14,"Nemesis"},{15,"Will"},{16,"Shield"},{17,"Revenge"},{18,"Destroy"},{19,"Fight"},{20,"Determination"},{21,"Enhance"},{22,"Accuracy"},{23,"Tolerance"},{24,"Seal"},{25,"Intangible"}};
    static readonly Dictionary<int,string> Stats=new Dictionary<int,string>{{1,"HP+"},{2,"HP%"},{3,"Atk+"},{4,"Atk%"},{5,"Def+"},{6,"Def%"},{8,"Spd"},{9,"CtR%"},{10,"CtD%"},{11,"Res%"},{12,"Acc%"}};
    public static readonly List<Preset> Presets=CreatePresets(); public static readonly List<CraftStock> Stocks=new List<CraftStock>(); public static readonly HashSet<long> ProtectedWorldBossRuneIds=new HashSet<long>(); public static readonly Dictionary<string,int[]> PresetSlotCounts=new Dictionary<string,int[]>();
    public static double ScarcityBonus(int[] counts,int slot){int max=counts.Max(),min=counts.Min(),n=counts[Math.Max(0,Math.Min(5,slot-1))];return max==min?0:Math.Max(0,Math.Min(1,(max-n)/(double)(max-min)));}
    public static double InventoryBonus(Preset p,string set,int slot){int[] c;return PresetSlotCounts.TryGetValue(p.Name+"|"+set,out c)?ScarcityBonus(c,slot):0;}
    public static readonly HashSet<long> ProtectedArtifactIds=new HashSet<long>(); public static int ReappNormal,ReappAncient,RefinementStones;
    // Seuls 3 donjons a runes existent (confirme par Jeremy) : chacun donne un pool
    // fixe de sets. Sets hors de ces 3 pools = pas farmables en donjon (craft/boutique/rift).
    static readonly Dictionary<string,string> SetDungeon=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase){
      {"Despair","Castel du Géant (GB)"},{"Swift","Castel du Géant (GB)"},{"Fatal","Castel du Géant (GB)"},{"Blade","Castel du Géant (GB)"},{"Energy","Castel du Géant (GB)"},
      {"Violent","Tanière du Dragon (DB)"},{"Guard","Tanière du Dragon (DB)"},{"Focus","Tanière du Dragon (DB)"},{"Endure","Tanière du Dragon (DB)"},{"Shield","Tanière du Dragon (DB)"},{"Revenge","Tanière du Dragon (DB)"},
      {"Rage","Nécropolis (NB)"},{"Vampire","Nécropolis (NB)"},{"Will","Nécropolis (NB)"},{"Nemesis","Nécropolis (NB)"},{"Destroy","Nécropolis (NB)"},
      {"Fight","Craft / Boutique (pas dans GB/DB/NB)"},{"Determination","Craft / Boutique (pas dans GB/DB/NB)"},{"Enhance","Craft / Boutique (pas dans GB/DB/NB)"},
      {"Accuracy","Craft / Boutique (pas dans GB/DB/NB)"},{"Tolerance","Craft / Boutique (pas dans GB/DB/NB)"},
      {"Seal","Craft / Boutique (pas dans GB/DB/NB)"},{"Intangible","Craft / Boutique (pas dans GB/DB/NB)"}
    };
    // Cible par slot (1 rune du set par slot pour ~3 monstres qui l'utilisent).
    // Change juste ce chiffre si tu veux viser plus/moins large.
    const int FarmRotationDepth=3;
    // Manque calcule par SET + SLOT (1 a 6), uniquement pour les sets reellement
    // farmables dans un des 3 donjons (GB/DB/NB) — les sets craft/boutique ne sont
    // jamais utiles ici puisqu'aucun donjon ne peut les fournir.
    public static List<FarmAdvice> RecommendFarm(List<RuneRow> all){
      var sets=Presets.SelectMany(p=>p.Preferred.Concat(p.Accepted)).Distinct(StringComparer.OrdinalIgnoreCase)
        .Where(s=>{string d;return SetDungeon.TryGetValue(s,out d)&&d.IndexOf("Craft",StringComparison.OrdinalIgnoreCase)<0;});
      var result=new List<FarmAdvice>();
      foreach(var set in sets){
        string dungeon=SetDungeon[set];
        for(int slot=1;slot<=6;slot++){
          int owned=all.Count(r=>!r.Ancient&&r.Slot==slot&&string.Equals(r.Set,set,StringComparison.OrdinalIgnoreCase));
          int manque=FarmRotationDepth-owned;
          if(manque<=0)continue;
          result.Add(new FarmAdvice{Set=set,Slot=slot,Owned=owned,Target=FarmRotationDepth,Manque=manque,Dungeon=dungeon});
        }
      }
      result.Sort((a,b)=>b.Manque.CompareTo(a.Manque));
      return result;
    }
    static readonly Dictionary<long,Dictionary<string,object>> LiveSummonedUnits=new Dictionary<long,Dictionary<string,object>>();
    static readonly Dictionary<long,int> LiveStorageQuantities=new Dictionary<long,int>();
    public static void ResetLiveSkillUnits(){LiveSummonedUnits.Clear();LiveStorageQuantities.Clear();}
    public static int LimiteRunesConservees=1500; public const double SeuilQualiteMinimum=8.8;
    public static bool SeuilVenteFixe=false;
    public static double ValeurSeuilVenteFixe=8.8;
    public static double PoidsP1=1,PoidsP2=.5,PoidsP3=.2,BonusStatPrincipale=1.75,FacteurSetAcceptable=.95,FacteurSetExclu=.85,SeuilApres12=SeuilQualiteMinimum;
    // Multiplicateur global par stat (1.0 = 100%, valeur par defaut) applique dans Weight()
    // EN PLUS de la priorite Non/P1/P2/P3 propre a chaque preset — reglable depuis la ligne
    // "Valeur globale par stat" de la fenetre Presets (RuneEnhancements.cs). Ex : Atk% a 0.9
    // fait que l'Atk% compte pour 90% de sa valeur normale, quel que soit le preset.
    public static readonly Dictionary<string,double> StatGlobalFactor=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase){{"HP%",1},{"Atk%",1},{"Def%",1},{"Spd",1.1},{"Res%",1},{"Acc%",1},{"CtR%",1},{"CtD%",1},{"HP+",1},{"Atk+",1},{"Def+",1}};
    // Regles de bonus pur (bouton "Regles") : liste ouverte, editable via RuneEnhancements.cs
    // (ShowScoreRules). Chaque regle ajoute Bonus au Potential final si la rune est d'un des
    // Sets listes (vide = tous sets) ET si sa valeur pour Stat atteint Threshold. BuiltIn=true
    // pour les 2 regles Spd 23/25 historiques (non supprimables, mais modifiables). Projected=true
    // simule les rolls futurs restants (seulement implemente pour Stat=="Spd" pour l'instant) ;
    // sinon on lit juste la valeur actuelle de la sub (ou "toujours vrai" si c'est la stat principale).
    public sealed class ScoreRule{
      public string Name="";
      public List<string> Sets=new List<string>();
      public List<int> Slots=new List<int>();
      public string Stat="Spd";
      public double Threshold=23;
      public double Bonus=.5;
      public bool BuiltIn;
      public bool Projected;
    }
    public static List<ScoreRule> ScoreRules=new List<ScoreRule>{
      new ScoreRule{Name="Spd 23+ sur Will/Despair/Violent/Swift",Sets=new List<string>{"Will","Despair","Violent","Swift"},Stat="Spd",Threshold=23,Bonus=.5,BuiltIn=true,Projected=true},
      new ScoreRule{Name="Spd 25+ sur Will/Despair/Violent/Swift",Sets=new List<string>{"Will","Despair","Violent","Swift"},Stat="Spd",Threshold=25,Bonus=.5,BuiltIn=true,Projected=true},
    };
    // Mode économie de mana : rend l'appli plus dure sur "quelle rune vaut la peine d'etre
    // montee" (Pwr up, Level<12) SANS toucher au seuil de vente des runes deja +12
    // (SeuilApres12 reste la reference du Keep/Sell). PwrUpThreshold() est utilise
    // uniquement pour la branche Level<12.
    public static bool ManaSaverMode=false;
    public static double MargePwrUpStrict=2.0;
    public static double PwrUpThreshold(){return SeuilApres12+(ManaSaverMode?Math.Max(0,MargePwrUpStrict):0);}
    // Ecart minimum pour changer de BestBuild d'un calcul a l'autre (ex. Fast DD <-> Slow
    // DD, qui ne different que par le poids SPD). En dessous, on garde le preset precedent
    // au lieu de basculer pour un gain marginal du a InventoryBonus (qui bouge avec le
    // reste de l'inventaire, meme si CETTE rune n'a pas change).
    public static double BuildStabilityMargin=0.5;
    static Dictionary<string,object> D(object o){return o as Dictionary<string,object>;}
    static object[] A(object o){
      var arr=o as object[];if(arr!=null)return arr;
      var list=o as System.Collections.IList;if(list==null)return new object[0];
      var copy=new object[list.Count];for(int i=0;i<list.Count;i++)copy[i]=list[i];return copy;
    }
    static object G(Dictionary<string,object>d,string k,object z=null){object v;return d!=null&&d.TryGetValue(k,out v)?v:z;} static int I(object o){int v;return int.TryParse(Convert.ToString(o,CultureInfo.InvariantCulture),out v)?v:0;} static long L(object o){long v;return long.TryParse(Convert.ToString(o,CultureInfo.InvariantCulture),out v)?v:0;} static double F(object o){double v;return double.TryParse(Convert.ToString(o,CultureInfo.InvariantCulture),NumberStyles.Any,CultureInfo.InvariantCulture,out v)?v:0;}
    static string ReadSharedText(string path){using(var fs=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var sr=new StreamReader(fs))return sr.ReadToEnd();}
    static string Stat(int id){string s;return Stats.TryGetValue(id,out s)?s:"";} static string SetName(int id){string s;return Sets.TryGetValue(id,out s)?s:"Set "+id;}
    public static List<RuneRow> Import(string path,List<RuneRow> previous=null){var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=D(js.DeserializeObject(ReadSharedText(path)));ReadStocks(root);ReadReappStock(root);ReadRefinementStock(root);var protectedIds=new HashSet<long>();ProtectedArtifactIds.Clear();CollectDeckRunes(root,protectedIds);CollectDeckArtifacts(root,ProtectedArtifactIds);var marks=new Dictionary<long,string>();foreach(var x in A(G(root,"rune_lock_list"))){var d=D(x);long id=L(G(d,"rune_id"));int t=I(G(d,"lock_type"));if(t==1)marks[id]="Reeval";else if(t==2)marks[id]="rune spd";}
      var rows=new List<RuneRow>();var seen=new HashSet<long>();foreach(var x in A(G(root,"runes")))AddRune(D(x),false,rows,seen,marks,protectedIds);foreach(var ux in A(G(root,"unit_list"))){var u=D(ux);int owner=I(G(u,"unit_master_id"));foreach(var x in A(G(u,"runes"))){int before=rows.Count;AddRune(D(x),true,rows,seen,marks,protectedIds);if(rows.Count>before)rows[rows.Count-1].EquippedMasterId=owner;else{long runeId=L(G(D(x),"rune_id"));var existing=rows.FirstOrDefault(r=>r.Id==runeId);if(existing!=null){existing.Equipped=true;existing.EquippedMasterId=owner;}}}}
      // Le jeu peut avancer date_add d'une rune (ex. annulation d'une vente au marché
      // des runes, ou retour d'échange) sans que ce soit une nouvelle rune pour le
      // joueur. On garde la date d'obtention déjà connue par l'appli pour qu'une
      // rune déjà vue ne saute pas en haut du tri Obtained lors d'un simple réimport.
      if(previous!=null){var priorObtained=previous.ToDictionary(r=>r.Id,r=>r.Obtained);
        // Fast DD et Slow DD (entre autres paires proches) ne different que par le poids
        // de la SPD : deux scores quasi egaux peuvent inverser l'ordre d'un import a
        // l'autre a cause du seul InventoryBonus (qui bouge avec le reste de l'inventaire),
        // sans que la rune elle-meme ait change. On seme BestBuild avec le choix precedent
        // AVANT Calculate() pour que la logique anti flip-flop (voir Calculate) puisse s'en
        // servir meme apres un reimport complet du JSON (une nouvelle RuneRow est creee).
        var priorBuild=previous.ToDictionary(r=>r.Id,r=>r.BestBuild);
        foreach(var r in rows){DateTime prior;if(priorObtained.TryGetValue(r.Id,out prior)&&prior<r.Obtained)r.Obtained=prior;string pb;if(priorBuild.TryGetValue(r.Id,out pb)&&!string.IsNullOrEmpty(pb))r.BestBuild=pb;}
      }
      Calculate(rows);return rows;}
    public static SkillUpRoster LoadSkillUpRoster(string jsonPath,string catalogPath){
      var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=D(js.DeserializeObject(ReadSharedText(jsonPath)));
      var catalog=js.Deserialize<List<MonsterCatalogEntry>>(System.IO.File.ReadAllText(catalogPath));var byId=catalog.GroupBy(x=>x.id).ToDictionary(x=>x.Key,x=>x.First());
      var byFamilyElement=catalog.Where(x=>x.familyid>0).GroupBy(x=>((long)x.familyid<<8)+(x.id%10)).ToDictionary(x=>x.Key,x=>x.First());
      Func<int,MonsterCatalogEntry> resolveCatalog=mid=>{MonsterCatalogEntry c;if(byId.TryGetValue(mid,out c))return c;int family=mid/100*100,element=mid%10;byFamilyElement.TryGetValue(((long)family<<8)+element,out c);return c;};
      var protectedIds=new HashSet<long>();CollectUnitIds(G(root,"favorite_unit_list"),protectedIds);CollectUnitIds(G(root,"unit_lock_list"),protectedIds);CollectMarkedUnitIds(G(root,"unit_marker_list"),1,protectedIds);CollectMarkedUnitIds(G(root,"unit_marker_list"),100,protectedIds);
      var owned=new List<SkillUpMonster>();foreach(var ux in A(G(root,"unit_list"))){var u=D(ux);if(u==null)continue;long uid=L(G(u,"unit_id"));Dictionary<string,object> live;if(uid>0&&LiveSummonedUnits.TryGetValue(uid,out live))u=live;int mid=I(G(u,"unit_master_id"));MonsterCatalogEntry c=resolveCatalog(mid);if(c==null)continue;int current=I(G(u,"class")),level=I(G(u,"unit_level")),used=SkillUpsUsed(u),group=c.skillgroup>0?c.skillgroup:(c.familyid>0?c.familyid:mid/100*100);bool full=c.skillups>0&&used>=c.skillups;bool ld=c.element=="light"||c.element=="dark";bool locked=I(G(u,"locked"))!=0||protectedIds.Contains(uid);owned.Add(new SkillUpMonster{UnitId=uid,MasterId=mid,CatalogId=c.id,FamilyId=c.familyid,SkillGroup=group,NaturalStars=c.stars,CurrentStars=current,Level=level,SkillUpsUsed=used,SkillUpsToMax=c.skillups,FullySkilled=full,InSealedShrine=false,Name=c.name,Family=string.IsNullOrEmpty(c.family)?c.name:c.family,Element=c.element,Icon=c.icon,Protected=ld||c.stars>=5||locked||full});}
      object[] sealedStock=A(G(root,"unit_storage_list"));
      if(sealedStock.Length==0)sealedStock=A(G(root,"unit_storage_normal_list"));
      long synthetic=-1;var seenStorage=new HashSet<long>();var effectiveStorage=new List<Dictionary<string,object>>();foreach(var sx in sealedStock){var s=D(sx);if(s!=null)effectiveStorage.Add(s);}foreach(var kv in LiveStorageQuantities){int mid=(int)(kv.Key>>8),cls=(int)(kv.Key&255);if(!effectiveStorage.Any(s=>I(G(s,"unit_master_id"))==mid&&I(G(s,"class"))==cls))effectiveStorage.Add(new Dictionary<string,object>{{"unit_master_id",mid},{"class",cls},{"quantity",kv.Value}});}foreach(var s in effectiveStorage){int mid=I(G(s,"unit_master_id"));if(mid==0)mid=I(G(s,"monster_master_id"));if(mid==0)mid=I(G(s,"item_master_id"));MonsterCatalogEntry c=resolveCatalog(mid);if(mid==0||c==null)continue;int cls=I(G(s,"class"));long storageKey=((long)mid<<8)+(cls&255);int amount;seenStorage.Add(storageKey);if(!LiveStorageQuantities.TryGetValue(storageKey,out amount)){amount=I(G(s,"amount"));if(amount<=0)amount=I(G(s,"quantity"));if(amount<=0)amount=I(G(s,"count"));if(amount<=0)amount=I(G(s,"unit_count"));if(amount<=0)amount=1;}if(amount<=0)continue;int group=c.skillgroup>0?c.skillgroup:(c.familyid>0?c.familyid:mid/100*100);bool ld=c.element=="light"||c.element=="dark";for(int n=0;n<amount;n++)owned.Add(new SkillUpMonster{UnitId=synthetic--,MasterId=mid,CatalogId=c.id,FamilyId=c.familyid,SkillGroup=group,NaturalStars=c.stars,CurrentStars=cls>0?cls:c.stars,Level=1,SkillUpsUsed=0,SkillUpsToMax=c.skillups,FullySkilled=false,InSealedShrine=true,Name=c.name,Family=string.IsNullOrEmpty(c.family)?c.name:c.family,Element=c.element,Icon=c.icon,Protected=ld||c.stars>=5});}
      var known=new HashSet<long>(owned.Where(x=>x.UnitId>0).Select(x=>x.UnitId));foreach(var u in LiveSummonedUnits.Values){long uid=L(G(u,"unit_id"));if(uid<=0||known.Contains(uid))continue;int mid=I(G(u,"unit_master_id"));MonsterCatalogEntry c=resolveCatalog(mid);if(c==null||c.stars<4||c.stars>5)continue;int group=c.skillgroup>0?c.skillgroup:(c.familyid>0?c.familyid:mid/100*100);bool ld=c.element=="light"||c.element=="dark";int used=SkillUpsUsed(u);owned.Add(new SkillUpMonster{UnitId=uid,MasterId=mid,CatalogId=c.id,FamilyId=c.familyid,SkillGroup=group,NaturalStars=c.stars,CurrentStars=I(G(u,"class")),Level=Math.Max(1,I(G(u,"unit_level"))),SkillUpsUsed=used,SkillUpsToMax=c.skillups,FullySkilled=c.skillups>0&&used>=c.skillups,InSealedShrine=false,Name=c.name,Family=string.IsNullOrEmpty(c.family)?c.name:c.family,Element=c.element,Icon=c.icon,Protected=ld||c.stars>=5});known.Add(uid);}
      var crossGroups=new HashSet<int>(catalog.Where(x=>x.skillgroup>0&&x.familyid>0).GroupBy(x=>x.skillgroup).Where(g=>g.Select(x=>x.familyid).Distinct().Count()>1).Select(g=>g.Key));
      return new SkillUpRoster{Catalog=catalog,Owned=owned,CrossGroups=crossGroups};
    }
    public static List<SkillUpGroup> AnalyzeSkillUps(string jsonPath,string catalogPath){
      return AnalyzeSkillUps(LoadSkillUpRoster(jsonPath,catalogPath));
    }
    public static List<SkillUpGroup> AnalyzeSkillUps(SkillUpRoster roster){
      var owned=roster.Owned;var crossGroups=roster.CrossGroups;
  var result=new List<SkillUpGroup>();
  foreach(var targetSet in owned.Where(x=>!x.InSealedShrine&&x.NaturalStars>=4&&x.NaturalStars<=5).GroupBy(x=>x.MasterId)){
    SkillUpMonster target=targetSet.Where(x=>!x.FullySkilled).OrderByDescending(x=>x.Protected).ThenByDescending(x=>x.CurrentStars).ThenByDescending(x=>x.Level).FirstOrDefault();
    if(target==null)continue;
    bool equivalentCompleted=crossGroups.Contains(target.SkillGroup)&&owned.Any(x=>!x.InSealedShrine&&x.FullySkilled&&x.SkillGroup==target.SkillGroup&&x.Element==target.Element&&x.FamilyId!=target.FamilyId);
    if(equivalentCompleted)continue;
    int bestEquivalentProgress=owned.Where(x=>!x.InSealedShrine&&!x.FullySkilled&&x.SkillGroup==target.SkillGroup&&x.Element==target.Element).Max(x=>x.SkillUpsUsed);
    if(crossGroups.Contains(target.SkillGroup)&&target.SkillUpsUsed<bestEquivalentProgress)continue;
    var fodder=owned.Where(x=>x.UnitId!=target.UnitId&&x.InSealedShrine&&x.SkillGroup==target.SkillGroup&&(x.NaturalStars==3||x.NaturalStars==4)&&!x.Protected&&x.Element!="light"&&x.Element!="dark").OrderBy(x=>x.NaturalStars).ThenBy(x=>x.Name).ToList();
    if(fodder.Count>0)result.Add(new SkillUpGroup{Target=target,Fodders=fodder});
  }
  return result.OrderByDescending(x=>x.UsableUpgrades).ThenByDescending(x=>x.Target.NaturalStars).ThenBy(x=>x.Target.Family).ThenBy(x=>x.Target.Name).ToList();
    }
    static readonly string[] SkillElements=new[]{"water","fire","wind","light","dark"};
    public static List<SkillUpFamily> AnalyzeSkillUpFamilies(string jsonPath,string catalogPath){
      return AnalyzeSkillUpFamilies(LoadSkillUpRoster(jsonPath,catalogPath));
    }
    public static List<SkillUpFamily> AnalyzeSkillUpFamilies(SkillUpRoster roster){
      var catalog=roster.Catalog;var owned=roster.Owned;
      var island=owned.Where(x=>!x.InSealedShrine&&x.NaturalStars==4&&x.FamilyId>0).ToList();
      var ownedFids=new HashSet<int>(island.Select(x=>x.FamilyId));
      var four=catalog.Where(x=>x.stars==4&&x.familyid>0).ToList();
      var fids=four.Select(x=>x.familyid).Distinct().ToList();
      var sgOf=new Dictionary<int,int>();
      foreach(int fid in fids){int sg=four.Where(x=>x.familyid==fid).Select(x=>x.skillgroup>0?x.skillgroup:fid).GroupBy(x=>x).OrderByDescending(g=>g.Count()).Select(g=>g.Key).FirstOrDefault();sgOf[fid]=sg>0?sg:fid;}
      var result=new List<SkillUpFamily>();
      foreach(var cluster in fids.GroupBy(fid=>sgOf[fid])){
        var clusterFids=cluster.ToList();
        if(!clusterFids.Any(ownedFids.Contains))continue;
        int sg=cluster.Key;
        clusterFids=clusterFids.OrderBy(fid=>fid==sg?0:1).ThenBy(fid=>fid).ToList();
        var fam=new SkillUpFamily{FamilyId=clusterFids[0],SkillGroup=sg,NaturalStars=4};
        var searchParts=new List<string>();
        foreach(int fid in clusterFids){
          var cat4=four.Where(x=>x.familyid==fid).ToList();
          if(cat4.Count==0)continue;
          string label=FamilyLabel(cat4,island,fid);
          var row=new SkillUpFamilyRow{FamilyId=fid,Family=label,Collab=fid==sg&&clusterFids.Count>1};
          foreach(string extra in cat4.Select(x=>((x.family??"")+" "+(x.name??"")).Trim()))if(extra.Length>0)searchParts.Add(extra);
          searchParts.Add(label);
          foreach(string el in SkillElements){
            var opts=cat4.Where(x=>string.Equals(x.element,el,StringComparison.OrdinalIgnoreCase)).OrderBy(x=>HasHangul(x.name)?1:0).ThenByDescending(x=>x.id%100>=10?1:0).ThenByDescending(x=>string.IsNullOrEmpty(x.icon)?0:1).ThenByDescending(x=>x.id).ToList();
            if(opts.Count==0)continue;
            var c=opts[0];
            string shown=PreferLatin(new[]{c.name,c.family,label});if(string.IsNullOrEmpty(shown))shown=c.name;
            var copies=island.Where(x=>x.FamilyId==fid&&string.Equals(x.Element,el,StringComparison.OrdinalIgnoreCase)).ToList();
            var gap=new SkillUpElementGap{Element=el,Name=shown,Icon=c.icon,Family=label,CatalogId=c.id,ToMax=c.skillups,Display=new SkillUpMonster{MasterId=c.id,CatalogId=c.id,FamilyId=fid,Name=shown,Family=label,Element=el,Icon=c.icon,NaturalStars=4,SkillUpsToMax=c.skillups}};
            if(copies.Count>0){
              var best=copies.OrderByDescending(x=>x.FullySkilled).ThenByDescending(x=>x.SkillUpsUsed).ThenByDescending(x=>x.Level).First();
              gap.Owned=true;gap.FullySkilled=copies.Any(x=>x.FullySkilled);gap.Used=best.SkillUpsUsed;gap.ToMax=best.SkillUpsToMax>0?best.SkillUpsToMax:c.skillups;
              gap.Display=best;if(string.IsNullOrEmpty(gap.Display.Icon))gap.Display.Icon=c.icon;
              if(string.IsNullOrEmpty(gap.Display.Name)||gap.Display.Name==label||HasHangul(gap.Display.Name))gap.Display.Name=shown;
            }
            row.Elements.Add(gap);
          }
          if(row.Elements.Count>0)fam.Rows.Add(row);
        }
        fam.Elements=fam.Rows.SelectMany(r=>r.Elements).ToList();
        fam.Family=string.Join("  /  ",fam.Rows.Select(r=>r.Family).Where(x=>!string.IsNullOrEmpty(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        fam.Search=string.Join(" ",searchParts.Concat(fam.Elements.Select(e=>e.Name+" "+e.Element)).Where(x=>!string.IsNullOrEmpty(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray());
        if(fam.Elements.Count>0)result.Add(fam);
      }
      return result.OrderByDescending(x=>x.Gaps).ThenBy(x=>x.Family).ToList();
    }
    static bool HasHangul(string s){if(string.IsNullOrEmpty(s))return false;for(int i=0;i<s.Length;i++){int c=s[i];if(c>=0xAC00&&c<=0xD7A3)return true;}return false;}
    static bool IsLatinName(string n){if(string.IsNullOrWhiteSpace(n)||HasHangul(n))return false;int letters=0;for(int i=0;i<n.Length;i++)if(char.IsLetter(n[i]))letters++;return letters>=3;}
    static string PreferLatin(IEnumerable<string> names){foreach(string n in names){if(IsLatinName(n))return n.Trim();}return "";}
    static string FamilyLabel(List<MonsterCatalogEntry> cat4,List<SkillUpMonster> island,int fid){
      string latin=PreferLatin(cat4.Select(x=>x.family));if(latin.Length>0)return latin;
      latin=PreferLatin(cat4.Where(x=>x.id%100>=10).Select(x=>x.name));if(latin.Length>0)return latin;
      latin=PreferLatin(cat4.Select(x=>x.name));if(latin.Length>0)return latin;
      latin=PreferLatin(island.Where(x=>x.FamilyId==fid).Select(x=>x.Family));if(latin.Length>0)return latin;
      return cat4.Select(x=>x.family).FirstOrDefault(x=>!string.IsNullOrEmpty(x))??(cat4.Count>0?cat4[0].name:"");
    }
    public static bool SkillUpStockIncluded(string jsonPath){try{var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=D(js.DeserializeObject(ReadSharedText(jsonPath)));return A(G(root,"unit_storage_list")).Length>0||A(G(root,"unit_storage_normal_list")).Length>0;}catch{return false;}}
    static int SkillUpsUsed(Dictionary<string,object> unit){int total=0;foreach(var x in A(G(unit,"skills"))){var skill=A(x);if(skill.Length>1)total+=Math.Max(0,I(skill[1])-1);}return total;}
    static void CollectMarkedUnitIds(object o,int lockType,HashSet<long> ids){foreach(var x in A(o)){var d=D(x);if(d==null||I(G(d,"lock_type"))!=lockType)continue;long id=L(G(d,"unit_id"));if(id>0)ids.Add(id);}}
    static void CollectUnitIds(object o,HashSet<long> ids){var d=D(o);if(d!=null){foreach(var kv in d){if(kv.Key.Equals("unit_id",StringComparison.OrdinalIgnoreCase)){long id=L(kv.Value);if(id>0)ids.Add(id);}else if(kv.Key.Equals("unit_id_list",StringComparison.OrdinalIgnoreCase))CollectUnitIds(kv.Value,ids);else CollectUnitIds(kv.Value,ids);}return;}var a=A(o);if(a.Length>0){foreach(var x in a)CollectUnitIds(x,ids);return;}long value=L(o);if(value>0)ids.Add(value);}
    static void ReadStocks(Dictionary<string,object> root){Stocks.Clear();foreach(var x in A(G(root,"rune_craft_item_list"))){var d=D(x);int ct=I(G(d,"craft_type")),type=I(G(d,"craft_type_id")),amount=I(G(d,"amount"));if(amount<=0||ct<1||ct>6)continue;int gradeRaw=type%100,rest=type/100,statId=rest%100,setId=rest/100,grade=gradeRaw>=10?gradeRaw-10:gradeRaw;string stat=Stat(statId),set=(ct==3||ct==4||setId==99)?"Immemorial":SetName(setId);if(stat.Length>0&&set.Length>0)Stocks.Add(new CraftStock{Id=L(G(d,"craft_item_id")),Ancient=ct==5||ct==6,Type=(ct==1||ct==3||ct==5)?"Gemme":"Meule",Set=set,Stat=stat,Grade=grade,Amount=amount});}}
    static void ReadReappStock(Dictionary<string,object> root){ReappNormal=0;ReappAncient=0;foreach(var x in A(G(root,"inventory_info"))){var d=D(x);if(d==null||I(G(d,"item_master_type"))!=37)continue;int id=I(G(d,"item_master_id")),amount=Math.Max(0,I(G(d,"item_quantity")));if(id==1)ReappNormal+=amount;else if(id==2)ReappAncient+=amount;}}
    static void ReadRefinementStock(Dictionary<string,object> root){RefinementStones=0;foreach(var x in A(G(root,"inventory_info"))){var d=D(x);if(d!=null&&I(G(d,"item_master_type"))==114&&I(G(d,"item_master_id"))==1)RefinementStones=Math.Max(0,I(G(d,"item_quantity")));}}
    public static int StockCount(RuneRow r,string type,string stat){return Stocks.Where(x=>x.Type==type&&x.Stat==stat&&x.Ancient==r.Ancient&&(x.Set==r.Set||(!r.Ancient&&x.Set=="Immemorial"))&&(x.Grade==4||x.Grade==5)).Sum(x=>x.Amount);}
    public static int StockCountDetail(RuneRow r,string type,string stat,bool immemorial,int grade){string wanted=immemorial?"Immemorial":r.Set;if(immemorial&&r.Ancient)return 0;return Stocks.Where(x=>x.Type==type&&x.Stat==stat&&x.Set==wanted&&x.Ancient==r.Ancient&&x.Grade==grade).Sum(x=>x.Amount);}
    public static void ClearStock(RuneRow r,string type,string stat,bool immemorial,int grade){string wanted=immemorial?"Immemorial":r.Set;if(immemorial&&r.Ancient)return;foreach(var x in Stocks.Where(x=>x.Type==type&&x.Stat==stat&&x.Set==wanted&&x.Ancient==r.Ancient&&x.Grade==grade))x.Amount=0;}
    static void CollectDeckRunes(object o,HashSet<long> ids){var d=D(o);if(d!=null){foreach(var kv in d){if(kv.Key.Equals("rune_id_list",StringComparison.OrdinalIgnoreCase))foreach(var x in A(kv.Value)){long id=L(x);if(id>0)ids.Add(id);}CollectDeckRunes(kv.Value,ids);}return;}foreach(var x in A(o))CollectDeckRunes(x,ids);}
    static void CollectDeckArtifacts(object o,HashSet<long> ids){var d=D(o);if(d!=null){foreach(var kv in d){if(kv.Key.Equals("artifact_id_list",StringComparison.OrdinalIgnoreCase))foreach(var x in A(kv.Value)){long id=L(x);if(id>0)ids.Add(id);}CollectDeckArtifacts(kv.Value,ids);}return;}foreach(var x in A(o))CollectDeckArtifacts(x,ids);}
    public static int ApplyLiveDeckProtection(List<RuneRow> rows,string json){try{var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=D(js.DeserializeObject(json));if(root==null)return 0;string command=Convert.ToString(G(root,"command"));if(command.IndexOf("Deck",StringComparison.OrdinalIgnoreCase)<0&&command.IndexOf("Defense",StringComparison.OrdinalIgnoreCase)<0)return 0;var ids=new HashSet<long>();CollectDeckRunes(root,ids);CollectDeckArtifacts(root,ProtectedArtifactIds);int changed=0;foreach(var r in rows.Where(x=>ids.Contains(x.Id))){if(r.Marker.IndexOf("Deck",StringComparison.OrdinalIgnoreCase)<0){r.Marker=(r.Marker+" | Deck").Trim(' ','|');changed++;}r.Action=r.Level<12?"Pwr up":"Keep";}return changed;}catch{return 0;}}
    public static int ApplyLiveRuneMarker(List<RuneRow> rows,string json){try{var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=D(js.DeserializeObject(json));if(root==null||!string.Equals(Convert.ToString(G(root,"command")),"setRuneMarker",StringComparison.OrdinalIgnoreCase))return 0;var updates=new List<Tuple<long,int>>();int requestType=I(G(root,"lock_type"));foreach(var x in A(G(root,"rune_id_list"))){long id=L(x);if(id>0)updates.Add(Tuple.Create(id,requestType));}foreach(var x in A(G(root,"rune_lock_list"))){var d=D(x);long id=L(G(d,"rune_id"));if(id>0)updates.Add(Tuple.Create(id,I(G(d,"lock_type"))));}int changed=0;foreach(var update in updates){var r=rows.FirstOrDefault(x=>x.Id==update.Item1);if(r==null)continue;string old=r.Marker;var parts=old.Split(new[]{'|' },StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).Where(x=>!x.Equals("Reeval",StringComparison.OrdinalIgnoreCase)&&!x.Equals("rune spd",StringComparison.OrdinalIgnoreCase)).ToList();if(update.Item2==1)parts.Insert(0,"Reeval");else if(update.Item2==2)parts.Insert(0,"rune spd");r.Marker=string.Join(" | ",parts.Distinct(StringComparer.OrdinalIgnoreCase));if(!string.Equals(old,r.Marker,StringComparison.OrdinalIgnoreCase))changed++;}return changed;}catch{return 0;}}
    public static ReappraisalComparison CompareReappraisal(List<RuneRow> rows,string json){try{var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=D(js.DeserializeObject(json));if(root==null||I(G(root,"ret_code"))!=0||HasRefinementItem(root)||!string.Equals(Convert.ToString(G(root,"command")),"RevalueRune",StringComparison.OrdinalIgnoreCase))return null;return CompareRuneCandidate(rows,D(G(root,"rune")));}catch{return null;}}
    public static ReappraisalComparison CompareRefinement(List<RuneRow> rows,string json){try{var choice=CompareRuneChoice(rows,json);if(choice!=null&&RuneChoiceDetected!=null)RuneChoiceDetected(choice);var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=D(js.DeserializeObject(json));if(root==null||I(G(root,"ret_code"))!=0||!HasRefinementItem(root))return null;return CompareRuneCandidate(rows,D(G(root,"rune")));}catch{return null;}}
    public static RuneChoiceComparison CompareRuneChoice(List<RuneRow> rows,string json){try{var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=D(js.DeserializeObject(json));if(root==null||I(G(root,"ret_code"))!=0)return null;string command=Convert.ToString(G(root,"command"));string cmdKey=command.Replace("_","").ToLowerInvariant();if(command.IndexOf("receive",StringComparison.OrdinalIgnoreCase)<0&&command.IndexOf("reward",StringComparison.OrdinalIgnoreCase)<0&&command.IndexOf("open",StringComparison.OrdinalIgnoreCase)<0&&command.IndexOf("claim",StringComparison.OrdinalIgnoreCase)<0&&cmdKey!="getmaillist")return null;if(cmdKey.IndexOf("guildboss")>=0||cmdKey.IndexOf("siege")>=0||cmdKey.IndexOf("labyrinth")>=0||cmdKey.IndexOf("subjugation")>=0)return null;
      // Bug du 2026-09-15 : ReceiveMail (courrier avec 3 runes en piece jointe, ex :
      // recompense "3 runes Violent" du rune_set_id) contient un rune_list de 3 runes
      // qui ressemble en forme a un choix "1 rune sur 3" (meme nombre, memes champs
      // pri_eff/slot_no/set_id) alors que les 3 sont donnees d'un coup, pas a choisir.
      // Fix du 2026-09-18 (signale par Jeremy, coffre "Blessed Rune Box" jamais detecte) :
      // l'ancien garde-fou excluait TOUTE commande contenant "mail" et toute reponse avec
      // "mail_id_list" — or le vrai choix "1 rune sur 3" d'un coffre arrive justement via
      // ReceiveMail/GetMailList, imbrique dans mail_list[].extra (mail_type 254), et cette
      // meme reponse peut contenir un mail_id_list pour un AUTRE courrier reclame en meme
      // temps. Le bon critere pour distinguer "vrai choix, pas encore pris" de "3 runes deja
      // donnees d'un coup" (bug 2026-09-15) est le rune_id des candidats : 0/absent tant que
      // rien n'est choisi (coffre), toujours reel et positif quand elles sont deja possedees
      // (rune_list du bug). Filtre applique juste en dessous plutot que sur le nom de commande.
      var groups=new List<List<Dictionary<string,object>>>();CollectRuneChoices(root,groups);var selected=groups.FirstOrDefault(x=>(x.Count==3||x.Count==5)&&x.All(d=>L(G(d,"rune_id"))==0));if(selected==null)return null;
      // Signale par Jeremy le 2026-09-16 : un popup "choisis 1 rune" d'un coffre DEJA traite il y
      // a une semaine est ressorti pendant une session de farm sans rapport. Peu importe la cause
      // exacte (relecture du log, fenetre restee ouverte...), un meme coffre ne doit jamais
      // redeclencher le popup une deuxieme fois. Signature basee sur le "rid" (cle du dictionnaire
      // extra, injectee par CollectRuneChoices) puisque rune_id est toujours 0 avant le choix ;
      // repli sur une empreinte des valeurs si "rid" absent (forme en liste, pas en dictionnaire).
      Func<Dictionary<string,object>,string> fp=d=>{object rid;if(d.TryGetValue("rid",out rid)&&rid!=null)return Convert.ToString(rid);return I(G(d,"slot_no"))+"-"+I(G(d,"set_id"))+"-"+L(G(d,"base_value"))+"-"+L(G(d,"sell_value"));};
      string signature=string.Join(",",selected.Select(fp).OrderBy(x=>x));
      if(signature.Length==0||SeenRuneChoices.Contains(signature))return null;
      var candidates=new List<RuneRow>();long fake=-1;foreach(var d in selected){var copy=new Dictionary<string,object>(d);copy["rune_id"]=fake--;var parsed=new List<RuneRow>();AddRune(copy,false,parsed,new HashSet<long>(),new Dictionary<long,string>(),new HashSet<long>());if(parsed.Count>0)candidates.Add(parsed[0]);}if(candidates.Count!=3&&candidates.Count!=5)return null;SeenRuneChoices.Add(signature);var test=candidates.Select(CopyRune).ToList();foreach(var candidate in test){double best=-1;foreach(var preset in Presets){var projection=BestProjection(candidate,preset);double rawScore=Score(projection,preset);double value=rawScore>0?rawScore+InventoryBonus(preset,candidate.Set,candidate.Slot):0;if(value>best){best=value;candidate.Potential=Math.Round(value,3);candidate.BestBuild=preset.Name;candidate.Recommendation=Recommend(projection,preset);}}}return new RuneChoiceComparison{Choices=test.Where(x=>x.Id<0).OrderByDescending(x=>x.Potential).ToList()};}catch{return null;}}
    // Cas coffre a choix (ex. Blessed Rune Box) : dictionnaire "extra":{"<rid>":{...rune...},...} —
    // le "rid" (cle) est la seule identite stable du candidat tant que rune_id vaut 0 (rien choisi),
    // donc on le copie dans chaque candidat (cle "rid") pour la signature anti-doublon plus haut.
    static void CollectRuneChoices(object value,List<List<Dictionary<string,object>>> found){var d=D(value);if(d!=null){var direct=new List<Dictionary<string,object>>();foreach(var kv in d){var dd=D(kv.Value);if(dd==null||G(dd,"pri_eff")==null||G(dd,"set_id")==null)continue;var copy=new Dictionary<string,object>(dd);if(!copy.ContainsKey("rid"))copy["rid"]=kv.Key;direct.Add(copy);}if((direct.Count==3||direct.Count==5)&&direct.Count==d.Count)found.Add(direct);foreach(var kv in d)CollectRuneChoices(kv.Value,found);return;}var a=A(value);if(a.Length==3||a.Length==5){var runes=a.Select(D).Where(x=>x!=null&&G(x,"pri_eff")!=null&&G(x,"slot_no")!=null&&G(x,"set_id")!=null).ToList();if(runes.Count==a.Length)found.Add(runes);}foreach(var x in a)CollectRuneChoices(x,found);}
    static ReappraisalComparison CompareRuneCandidate(List<RuneRow> rows,Dictionary<string,object>d){if(d==null)return null;long id=L(G(d,"rune_id"));var current=rows.FirstOrDefault(x=>x.Id==id);if(current==null)return null;var parsed=new List<RuneRow>();AddRune(d,current.Equipped,parsed,new HashSet<long>(),new Dictionary<long,string>(),new HashSet<long>());if(parsed.Count==0)return null;var candidate=parsed[0];candidate.Marker=current.Marker;candidate.Obtained=current.Obtained;candidate.EquippedMasterId=current.EquippedMasterId;var test=rows.Select(CopyRune).ToList();int index=test.FindIndex(x=>x.Id==id);if(index<0)return null;test[index]=candidate;Calculate(test);var scored=test[index];return new ReappraisalComparison{RuneId=id,BeforePotential=current.Potential,AfterPotential=scored.Potential,BeforePreset=current.BestBuild,AfterPreset=scored.BestBuild,BeforeStats=StatsText(current),AfterStats=StatsText(scored)};}
    static bool HasRefinementItem(object value){var d=D(value);if(d!=null){if(I(G(d,"item_master_type"))==114&&I(G(d,"item_master_id"))==1)return true;foreach(var kv in d)if(HasRefinementItem(kv.Value))return true;return false;}foreach(var x in A(value))if(HasRefinementItem(x))return true;return false;}
    static string StatsText(RuneRow r){return string.Join("  ",r.Subs.Select(x=>x.BaseDisplay).ToArray());}
    static void AddRune(Dictionary<string,object>d,bool eq,List<RuneRow> rows,HashSet<long> seen,Dictionary<long,string> marks,HashSet<long> decks){if(d==null)return;long id=L(G(d,"rune_id"));if(id==0||!seen.Add(id))return;
      // "class" = etoiles (1-6, +10 pour antique) — voir WorldBossOptimizer.cs (var "stars").
      // "rank" = qualite/rarete (1 Magique .. 5 Legendaire, +10 pour antique) — voir
      // WorldBossOptimizer.cs (var "quality"). Grade ici sert de RARETE partout dans ce fichier
      // (Quality, CanRefine, IsBlue, nombre de rolls de substats, etc.), donc doit venir de
      // "rank", pas de "class". Un fix precedent avait bascule sur "class" par erreur (pensant
      // regler un probleme d'etoiles), ce qui faisait passer des runes Heroique(violette) en
      // Legendaire des qu'elles etaient 6 etoiles — bug d'affichage signale par Jeremy.
      int rank=I(G(d,"rank")),cls=I(G(d,"class")),quality=rank>=10?rank-10:rank,stars=cls>=10?cls-10:cls;if(stars<1)stars=1;if(stars>6)stars=6;var r=new RuneRow{Id=id,Set=SetName(I(G(d,"set_id"))),Slot=I(G(d,"slot_no")),Grade=quality,Stars=stars,Level=I(G(d,"upgrade_curr")),Ancient=rank>=10||cls>=10,Equipped=eq,EquippedUnitId=L(G(d,"occupied_id"))};DateTime dt;if(DateTime.TryParse(Convert.ToString(G(d,"date_add")),out dt))r.Obtained=dt;var p=A(G(d,"pri_eff"));if(p.Length>1){r.Main=Stat(I(p[0]));r.MainValue=F(p[1]);}var innate=A(G(d,"prefix_eff"));if(innate.Length>1&&I(innate[0])>0){r.Innate=Stat(I(innate[0]));r.InnateValue=F(innate[1]);}foreach(var sx in A(G(d,"sec_eff"))){var s=A(sx);if(s.Length>1)r.Subs.Add(new SubStat{Stat=Stat(I(s[0])),Value=F(s[1]),Gemmed=s.Length>2&&I(s[2])!=0,Grind=s.Length>3?F(s[3]):0});}string m;if(!marks.TryGetValue(id,out m)||m==null)m="";if(decks.Contains(id))m=(m+" | Deck").Trim(' ','|');r.Marker=m;rows.Add(r);}
    public static string ApplyLiveEvent(List<RuneRow> rows,string responseJson){
      if(rows==null||string.IsNullOrWhiteSpace(responseJson))return "";
      var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=D(js.DeserializeObject(responseJson));if(root==null||I(G(root,"ret_code"))!=0)return "";string command=Convert.ToString(G(root,"command"));int craftStockUpdates=0;
      // Le batiment de craft (meule/gemme depuis Beast Claw ou Beast Horn) n'envoie pas
      // une commande listee dans IsCraftStockMutationCommand (liste pensee pour
      // combat/achat/amplify/convert), donc son stock ne se mettait jamais a jour en
      // temps reel. Premiere tentative : scanner CHAQUE reponse pour n'importe quel objet
      // ayant la forme craft_item_id+craft_type+craft_type_id+amount, sans regarder sous
      // quelle cle il se trouve. Resultat : des stocks fantomes (bug rapporte) car d'autres
      // reponses du jeu (catalogues/previsualisations/historique) contiennent ailleurs des
      // objets de MEME forme mais dont "amount" ne represente pas le stock reellement
      // possede. Corrige : on ne fait plus confiance qu'aux cles precises deja utilisees
      // ailleurs dans ce fichier pour le stock reel ("rune_craft_item_list" = liste complete,
      // "rune_craft_item" = un seul item, cf. ReadStocks et AmplifyRune_v2/ConvertRune_v2),
      // peu importe leur profondeur dans la reponse — jamais par simple forme d'objet.
      // SellRuneCraftItem_v2 renvoie un "rune_craft_item.amount" BIDON (toujours 5, verifie
      // sur 20 ventes differentes avec sell_amount 8/9/10 variables) : lui faire confiance
      // remettait du stock fantome a chaque vente au lieu de le retirer. Sur une vente, on
      // force donc l'item vendu a 0 au lieu d'utiliser son "amount".
      // Le JSON SWEX EST HubUserLogin (snapshot login). Relog = rune_craft_item_list complet
      // avec le stock reel, y compris les meules R5 absentes du resultat de combat. On
      // l'appliquait seulement a l'import fichier, pas au live log — le meme paquet etait
      // ignore a cause du filtre Hub*. GuestLogin = meme payload.
      if(string.Equals(command,"HubUserLogin",StringComparison.OrdinalIgnoreCase)||string.Equals(command,"GuestLogin",StringComparison.OrdinalIgnoreCase)){
        if(A(G(root,"rune_craft_item_list")).Length>0){ReadStocks(root);ReadReappStock(root);ReadRefinementStock(root);craftStockUpdates++;}
      }else if(command.IndexOf("SellRuneCraft",StringComparison.OrdinalIgnoreCase)>=0)ApplySoldCraftStock(root,ref craftStockUpdates);else if(ShouldCollectLiveCraftDrops(command))CollectLiveCraftStocks(root,ref craftStockUpdates);
      int reappUpdates=UpdateLiveReappStock(root,command);int refinementUpdates=UpdateLiveRefinementStock(root,command);
      if(command=="UpgradeRuneList"){
        int count=0;DateTime batchObtained=DateTime.Now;foreach(var x in A(G(root,"upgrade_rune_list"))){var d=D(x);if(d==null)continue;long id=L(G(d,"rune_id"));if(id<=0)continue;var existing=rows.FirstOrDefault(r=>r.Id==id);var parsed=new List<RuneRow>();AddRune(d,existing!=null&&existing.Equipped,parsed,new HashSet<long>(),new Dictionary<long,string>(),new HashSet<long>());if(parsed.Count==0)continue;var updated=parsed[0];if(existing!=null){updated.Marker=existing.Marker;updated.Obtained=existing.Obtained;updated.EquippedMasterId=existing.EquippedMasterId;updated.EquippedUnitId=existing.EquippedUnitId;rows[rows.IndexOf(existing)]=updated;}else{updated.Obtained=batchObtained;rows.Add(updated);}count++;}return count>0?Loc.T("live_hammer",count):"";
      }
      if(command=="UpgradeRune_v2"||command=="UpgradeRune"||command=="AmplifyRune_v2"||command=="ConvertRune_v2"||command=="ConfirmRune"||command=="confirmRefineRune"){
        var d=D(G(root,"rune"));if(d==null)return "";long id=L(G(d,"rune_id"));if(id<=0)return "";var existing=rows.FirstOrDefault(x=>x.Id==id);var parsed=new List<RuneRow>();AddRune(d,existing!=null&&existing.Equipped,parsed,new HashSet<long>(),new Dictionary<long,string>(),new HashSet<long>());if(parsed.Count==0)return "";var updated=parsed[0];if(existing!=null){updated.Marker=existing.Marker;updated.Obtained=existing.Obtained;updated.EquippedMasterId=existing.EquippedMasterId;updated.EquippedUnitId=existing.EquippedUnitId;int index=rows.IndexOf(existing);rows[index]=updated;}else{updated.Obtained=DateTime.Now;rows.Add(updated);}if(command=="AmplifyRune_v2"||command=="ConvertRune_v2")ConsumeLiveCraftStock(D(G(root,"rune_craft_item")));return command=="AmplifyRune_v2"?Loc.T("live_grind",id):command=="ConvertRune_v2"?Loc.T("live_gem",id):command=="ConfirmRune"?Loc.T("live_reapp_pick",id):command=="confirmRefineRune"?Loc.T("live_refine_pick",id):Loc.T("live_upgrade",id,updated.Level);
      }
      if(command=="SellRune"){
        var ids=new HashSet<long>();foreach(var x in A(G(root,"rune_id_list"))){long id=L(x);if(id>0)ids.Add(id);}foreach(var x in A(G(root,"runes"))){var d=D(x);long id=L(G(d,"rune_id"));if(id>0)ids.Add(id);}if(ids.Count==0)return "";int removed=rows.RemoveAll(x=>ids.Contains(x.Id));return removed>0?(removed>1?Loc.T("live_sold_many",removed):Loc.T("live_sold_one",removed)):"";
      }
      if(command=="SummonUnit"){
        int summoned=0;foreach(var x in A(G(root,"unit_list"))){var u=D(x);if(u==null)continue;long id=L(G(u,"unit_id"));if(id<=0)continue;if(!LiveSummonedUnits.ContainsKey(id))summoned++;LiveSummonedUnits[id]=u;}return summoned>0?Loc.T("live_summon",summoned,summoned>1?"s":""):"";
      }
      if(command=="SacrificeUnit_V4"||command=="ConvertUnitToStorage"||command=="getUnitStorageList"){
        int changed=UpdateLiveSkillState(root);return changed>0?(command=="SacrificeUnit_V4"?Loc.T("live_skill_monsters"):Loc.T("live_monster_stock")):"";
      }
      // Les gains de runes ne passent pas par une commande unique : ils peuvent venir
      // d'un combat, d'une récompense ou d'un achat. On accepte uniquement les réponses
      // d'action et les objets qui possèdent la structure complète d'une vraie rune.
      if(!IsReadOnlyLiveCommand(command)){
        int added=0;CollectNewLiveRunes(root,rows,ref added);
        if(added>0)return Loc.T("live_new_runes",added,added>1?"s":"",added>1?"s":"")+(craftStockUpdates>0?" • "+Loc.T("live_stock_short"):"")+(reappUpdates>0?" • "+Loc.T("live_reapp_short"):"");
      }
      var updates=new List<string>();if(craftStockUpdates>0)updates.Add(Loc.T("live_stock"));if(reappUpdates>0)updates.Add(Loc.T("live_reapp"));if(refinementUpdates>0)updates.Add(Loc.T("live_refine"));return string.Join(" • ",updates.ToArray());
    }
    static int UpdateLiveSkillState(Dictionary<string,object> root){int changed=0;var target=D(G(root,"target_unit"));if(target!=null){long id=L(G(target,"unit_id"));if(id>0){LiveSummonedUnits[id]=target;changed++;}}foreach(var x in A(G(root,"remove_unit_id_list"))){long id=L(x);if(id>0&&LiveSummonedUnits.Remove(id))changed++;}foreach(var x in A(G(root,"unit_storage_list"))){var s=D(x);if(s==null)continue;int mid=I(G(s,"unit_master_id")),cls=I(G(s,"class"));if(mid<=0)continue;long key=((long)mid<<8)+(cls&255);int amount=Math.Max(0,I(G(s,"quantity")));int old;if(!LiveStorageQuantities.TryGetValue(key,out old)||old!=amount){LiveStorageQuantities[key]=amount;changed++;}}return changed;}
    // "battleScoutResult" EXACTEMENT (arene interserveur/scout) genere jusqu'a 180+ objets rune
    // complets (rune_id/stats reels, wizard_id correct) en simple APERCU/catalogue de
    // recompense possible — confirme sur export reel de Jeremy : AUCUN de ces rune_id n'existe
    // dans son inventaire. Sans exclusion, chacun etait pris pour une "nouvelle rune obtenue"
    // (rune_id inconnu) et ajoute avec la date du jour -> runes fantomes remontant en haut de
    // Tri Obtained et polluant Tri Amelioration Disponible pendant les sessions d'arene.
    // ATTENTION : exclusion sur le NOM EXACT uniquement, pas sur un simple "contient Scout" —
    // une premiere version trop large excluait aussi les vraies recompenses (ex. coffre de
    // scout battle), qui doivent au contraire etre ajoutees normalement.
    // "battleScoutResult" etait exclu (v11) pour bloquer un faux-positif observe pendant l'arene
    // interserveur. Verifie sur 40 vraies reponses "battleScoutResult" (390 Mo de log reel,
    // 11 jours) : c'est en fait le mecanisme du "Scout Battle" (coffre 4h) qui tombe toutes les
    // ~4-8h, et les 550 runes recompensees dedans avaient TOUTES le wizard_id du joueur — aucune
    // trace de la donnee fantome originale sous ce nom de commande. Exclure ce nom bloquait donc
    // uniquement du vrai loot (regression signalee par Jeremy : coffre scout battle plus mis a
    // jour). Le vrai bug fantome (opp_unit_list de battleServerArenaLoadSuccess_v2 /
    // BattleServerGuildWarLoadSuccess) reste corrige plus bas, independamment de cette commande.
    static bool IsReadOnlyLiveCommand(string command){if(string.IsNullOrEmpty(command))return true;if(command.IndexOf("Receive",StringComparison.OrdinalIgnoreCase)>=0&&command.IndexOf("Reward",StringComparison.OrdinalIgnoreCase)>=0)return false;return command.StartsWith("Get",StringComparison.OrdinalIgnoreCase)||command.StartsWith("Hub",StringComparison.OrdinalIgnoreCase)||command.StartsWith("Update",StringComparison.OrdinalIgnoreCase)||command.IndexOf("List",StringComparison.OrdinalIgnoreCase)>=0||command.IndexOf("Info",StringComparison.OrdinalIgnoreCase)>=0;}
    static bool IsCraftStockMutationCommand(string command){if(string.IsNullOrEmpty(command))return false;return command.StartsWith("Battle",StringComparison.OrdinalIgnoreCase)||command.StartsWith("battle",StringComparison.OrdinalIgnoreCase)||command.StartsWith("pick",StringComparison.OrdinalIgnoreCase)||command.StartsWith("Receive",StringComparison.OrdinalIgnoreCase)||command.StartsWith("Buy",StringComparison.OrdinalIgnoreCase)||command.StartsWith("collabo",StringComparison.OrdinalIgnoreCase)||command.StartsWith("AmplifyRune",StringComparison.OrdinalIgnoreCase)||command.StartsWith("ConvertRune",StringComparison.OrdinalIgnoreCase)||command.StartsWith("SellRuneCraft",StringComparison.OrdinalIgnoreCase)||command.Equals("RevalueRune",StringComparison.OrdinalIgnoreCase)||command.StartsWith("combine",StringComparison.OrdinalIgnoreCase)||command.StartsWith("reward",StringComparison.OrdinalIgnoreCase);}
    // Drops NORMAUX confirmes dans full_log.txt (craft_type 1 gemme / 2 meule) :
    // - BattleRiftDungeonResult.item_list[].type==27 (rift/betes, is_boxing:1, amount=stock reel)
    // - BattleWorldBossResult_v2.changed_item_list[].type==27
    // - pickGuildMazeBattleClearReward.pick_changestone_list[].after
    // - getGuildBossReceiveRewardCrate (coffres raid de guilde — nom "get*" mais c'est un claim)
    // battleGuildMazeResult : si les 3 choix sont IDENTIQUES (souvent meule/gemme
    // immemoriale craft_type 3/4), le jeu grant auto dans reward.crate.changestones
    // SANS pickGuildMazeBattleClearReward — confirme log 20/09/2026, craft_type_id 990805
    // (Immemorial SPD legend). selectable_item_list n'est que l'apercu (runecraft_*).
    static bool ShouldCollectLiveCraftDrops(string command){
      if(string.IsNullOrEmpty(command))return false;
      // Convert/Amplify portent rune_craft_item = restant APRES usage, pas un drop.
      // Si on les collectait comme loot, UpdateLiveCraftStock creait une ligne a l'id reel
      // (souvent amount=0) et ConsumeLiveCraftStock croyait l'id connu : la pile TSV/Id=0
      // du meme set/stat/grade (gemme, meule, antique, immemoriale) n'etait jamais videe.
      if(command.StartsWith("AmplifyRune",StringComparison.OrdinalIgnoreCase)||command.StartsWith("ConvertRune",StringComparison.OrdinalIgnoreCase))return false;
      if(command.IndexOf("ReceiveReward",StringComparison.OrdinalIgnoreCase)>=0)return true;
      if(command.StartsWith("Get",StringComparison.OrdinalIgnoreCase)||command.StartsWith("Hub",StringComparison.OrdinalIgnoreCase))return false;
      if(command.IndexOf("RewardInfo",StringComparison.OrdinalIgnoreCase)>=0)return false;
      if(command.IndexOf("Repurchase",StringComparison.OrdinalIgnoreCase)>=0)return false;
      return true;
    }
    static int UpdateLiveReappStock(Dictionary<string,object> root,string command){if(root==null||string.IsNullOrEmpty(command))return 0;bool action=IsCraftStockMutationCommand(command)||command.StartsWith("receivePlayPassReward",StringComparison.OrdinalIgnoreCase);if(!action)return 0;var absolute=new Dictionary<int,int>();CollectAbsoluteReapp(root,absolute);int changed=0;if(absolute.Count>0){foreach(var kv in absolute){if(kv.Key==1){if(ReappNormal!=kv.Value)changed++;ReappNormal=kv.Value;}else if(kv.Key==2){if(ReappAncient!=kv.Value)changed++;ReappAncient=kv.Value;}}return changed;}int normal=0,ancient=0;CollectReappReward(G(root,"reward_normal_list"),ref normal,ref ancient);CollectReappReward(G(root,"reward_pass_list"),ref normal,ref ancient);if(normal>0){ReappNormal+=normal;changed++;}if(ancient>0){ReappAncient+=ancient;changed++;}return changed;}
    static int UpdateLiveRefinementStock(Dictionary<string,object> root,string command){if(root==null||IsReadOnlyLiveCommand(command))return 0;int absolute=-1,reward=0;CollectRefinementItems(root,ref absolute,ref reward);if(absolute>=0){if(RefinementStones==absolute)return 0;RefinementStones=absolute;return 1;}if(reward>0){RefinementStones+=reward;return 1;}return 0;}
    static void CollectRefinementItems(object value,ref int absolute,ref int reward){var d=D(value);if(d!=null){if(I(G(d,"item_master_type"))==114&&I(G(d,"item_master_id"))==1){int amount=G(d,"item_quantity_curr")!=null?I(G(d,"item_quantity_curr")):I(G(d,"item_quantity"));if(G(d,"rid")!=null||G(d,"wizard_id")!=null||G(d,"item_quantity_curr")!=null)absolute=Math.Max(0,amount);else reward+=Math.Max(0,amount);return;}foreach(var kv in d)CollectRefinementItems(kv.Value,ref absolute,ref reward);return;}foreach(var x in A(value))CollectRefinementItems(x,ref absolute,ref reward);}
    static void CollectAbsoluteReapp(object value,Dictionary<int,int> found){var d=D(value);if(d!=null){if(I(G(d,"item_master_type"))==37&&(G(d,"rid")!=null||G(d,"wizard_id")!=null)){int id=I(G(d,"item_master_id"));int amount=G(d,"item_quantity_curr")!=null?I(G(d,"item_quantity_curr")):I(G(d,"item_quantity"));if((id==1||id==2)&&amount>=0)found[id]=amount;}foreach(var kv in d)CollectAbsoluteReapp(kv.Value,found);return;}foreach(var x in A(value))CollectAbsoluteReapp(x,found);}
    static void CollectReappReward(object value,ref int normal,ref int ancient){var d=D(value);if(d!=null){if(I(G(d,"item_master_type"))==37){int id=I(G(d,"item_master_id")),amount=Math.Max(0,I(G(d,"item_quantity")));if(id==1)normal+=amount;else if(id==2)ancient+=amount;}foreach(var kv in d)CollectReappReward(kv.Value,ref normal,ref ancient);return;}foreach(var x in A(value))CollectReappReward(x,ref normal,ref ancient);}
    static void CollectNewLiveRunes(object value,List<RuneRow> rows,ref int added){
      var d=D(value);if(d!=null){
        if(G(d,"rune_id")!=null&&G(d,"slot_no")!=null&&G(d,"set_id")!=null&&G(d,"pri_eff")!=null&&G(d,"sec_eff")!=null){
          long id=L(G(d,"rune_id"));if(id>0&&!rows.Any(r=>r.Id==id)){var parsed=new List<RuneRow>();AddRune(d,false,parsed,new HashSet<long>(),new Dictionary<long,string>(),new HashSet<long>());if(parsed.Count>0){var rune=parsed[0];if(rune.Obtained==default(DateTime))rune.Obtained=DateTime.Now;rows.Add(rune);added++;}}return;
        }
        // "opp_unit_list" (arene interserveur "battleServerArenaLoadSuccess_v2" et autres
        // reponses de combat/scout) transporte l'equipe ADVERSE, avec des runes completes
        // (meme forme qu'une vraie rune) mais un wizard_id different du proprietaire —
        // confirme sur donnees reelles de Jeremy. Sans exclusion, chaque debut d'arene
        // ajoutait les runes de l'adversaire comme si c'etaient les siennes. Toute cle
        // commencant par "opp_" designe systematiquement des donnees adverses dans ce jeu.
        foreach(var kv in d){if(kv.Key!=null&&kv.Key.StartsWith("opp_",StringComparison.OrdinalIgnoreCase))continue;CollectNewLiveRunes(kv.Value,rows,ref added);}return;
      }
      foreach(var x in A(value))CollectNewLiveRunes(x,rows,ref added);
    }
    static void ApplySoldCraftStock(Dictionary<string,object> root,ref int updated){
      object single;if(root.TryGetValue("rune_craft_item",out single)){var item=D(single);if(item!=null){long id=L(G(item,"craft_item_id"));if(id>0){var stock=Stocks.FirstOrDefault(x=>x.Id==id);if(stock!=null&&stock.Amount!=0){stock.Amount=0;updated++;}}}}
      object list;if(root.TryGetValue("rune_craft_item_list",out list))foreach(var x in A(list)){var item=D(x);if(item==null)continue;long id=L(G(item,"craft_item_id"));if(id<=0)continue;var stock=Stocks.FirstOrDefault(s=>s.Id==id);if(stock!=null&&stock.Amount!=0){stock.Amount=0;updated++;}}
    }
    // Drops reels confirmes dans full_log.txt :
    // - BattleRiftDungeonResult.item_list type 27 : gemmes/meules NORMALES (craft_type 1/2)
    // - BattleWorldBossResult_v2.changed_item_list type 27 : gemmes/meules NORMALES
    // - pick_changestone_list[].after (labyrinthe de guilde)
    // - getGuildBossReceiveRewardCrate type 27 (coffres)
    // La forme craft_item_id+craft_type+craft_type_id+amount est dans info ; type 27 est le
    // wrapper. Sans ce wrapper, rune_craft_item_list ratait tout le loot rift/raid.
    static bool HasCraftShape(Dictionary<string,object> d){return d!=null&&d.ContainsKey("craft_item_id")&&d.ContainsKey("craft_type")&&d.ContainsKey("craft_type_id")&&(d.ContainsKey("amount")||d.ContainsKey("quantity")||d.ContainsKey("item_quantity"));}
    static Dictionary<string,object> CraftWithAmount(Dictionary<string,object> d){if(d.ContainsKey("amount"))return d;var copy=new Dictionary<string,object>(d);object qty=G(d,"quantity");if(qty==null)qty=G(d,"item_quantity");copy["amount"]=qty??0;return copy;}
    static void CollectLiveCraftStocks(object value,ref int updated){var d=D(value);if(d!=null){
        if(I(G(d,"type"))==27){var info=D(G(d,"info"));if(info!=null&&HasCraftShape(info)){UpdateLiveCraftStock(CraftWithAmount(info));updated++;return;}if(ApplyViewCraftDrop(d,ref updated))return;}
        if(HasCraftShape(d)){UpdateLiveCraftStock(CraftWithAmount(d));updated++;return;}
        foreach(var kv in d){
          if(kv.Key!=null&&(kv.Key.Equals("selectable_item_list",StringComparison.OrdinalIgnoreCase)||kv.Key.Equals("unit_list",StringComparison.OrdinalIgnoreCase)))continue;
          CollectLiveCraftStocks(kv.Value,ref updated);
        }
        return;
      }foreach(var x in A(value))CollectLiveCraftStocks(x,ref updated);}
    static bool ApplyViewCraftDrop(Dictionary<string,object> wrap,ref int updated){var view=D(G(wrap,"view"))??wrap;int ct=I(G(view,"runecraft_type"));if(ct<1||ct>6)return false;int typeId=I(G(view,"runecraft_item_id"));if(typeId<=0)return false;int qty=I(G(view,"item_quantity"));if(qty<=0)qty=I(G(wrap,"quantity"));if(qty<=0)qty=1;IncrementLiveCraftStock(ct,typeId,qty);updated++;return true;}
    static void IncrementLiveCraftStock(int ct,int type,int qty){if(qty<=0||ct<1||ct>6)return;int gradeRaw=type%100,rest=type/100,statId=rest%100,setId=rest/100,grade=gradeRaw>=10?gradeRaw-10:gradeRaw;string stat=Stat(statId),set=(ct==3||ct==4||setId==99)?"Immemorial":SetName(setId);if(stat.Length==0||set.Length==0)return;bool ancient=ct==5||ct==6;string kind=(ct==1||ct==3||ct==5)?"Gemme":"Meule";var stock=Stocks.FirstOrDefault(x=>x.Id==0&&x.Ancient==ancient&&x.Type==kind&&x.Set==set&&x.Stat==stat&&x.Grade==grade);if(stock==null){Stocks.Add(new CraftStock{Id=0,Ancient=ancient,Type=kind,Set=set,Stat=stat,Grade=grade,Amount=qty});return;}stock.Amount=Math.Max(0,stock.Amount)+qty;}
    static void UpdateLiveCraftStock(Dictionary<string,object> d){if(d==null)return;long id=L(G(d,"craft_item_id"));int ct=I(G(d,"craft_type")),type=I(G(d,"craft_type_id")),amount=Math.Max(0,I(G(d,"amount")));if(ct<1||ct>6)return;
      // "GetRuneCraftRepurchaseList" (menu de RACHAT, un catalogue, pas le stock possede)
      // renvoie ce meme format d'objet (craft_item_id/craft_type/craft_type_id/amount) mais
      // avec craft_item_id=-1 pour CHAQUE entree (sentinelle "non possede") — confirme sur
      // donnees reelles. Avant, id<=0 tombait dans le fallback par attributs ci-dessous et
      // ECRASAIT le vrai stock avec la quantite du catalogue (bug de stock corrompu des
      // qu'on ouvrait ce menu). id NEGATIF = jamais un vrai item possede : on l'ignore
      // entierement. Le fallback par attributs reste pour id==0 (deja utilise ailleurs pour
      // des evenements reels sans id fiable).
      if(id<0)return;
      int gradeRaw=type%100,rest=type/100,statId=rest%100,setId=rest/100,grade=gradeRaw>=10?gradeRaw-10:gradeRaw;string stat=Stat(statId),set=(ct==3||ct==4||setId==99)?"Immemorial":SetName(setId);bool ancient=ct==5||ct==6;string kind=(ct==1||ct==3||ct==5)?"Gemme":"Meule";// Chaque meule/gemme a un craft_item_id UNIQUE (pas un "stack" partage). Si id>0, on ne
// doit JAMAIS recycler l'entree d'un AUTRE item juste parce qu'il a le meme type/stat/set/
// grade : ca ecraserait/volerait son compte (bug source de stock fantome). Le fallback par
// attributs sert uniquement quand on n'a vraiment aucun id fiable (id==0).
var stock=id>0?Stocks.FirstOrDefault(x=>x.Id==id):null;if(stock==null&&id==0)stock=Stocks.FirstOrDefault(x=>x.Ancient==ancient&&x.Type==kind&&x.Set==set&&x.Stat==stat&&x.Grade==grade);if(stock==null){stock=new CraftStock{Id=id,Ancient=ancient,Type=kind,Set=set,Stat=stat,Grade=grade};Stocks.Add(stock);}stock.Id=id;stock.Amount=amount;}
    static void ConsumeLiveCraftStock(Dictionary<string,object> d){if(d==null)return;long id=L(G(d,"craft_item_id"));int ct=I(G(d,"craft_type")),type=I(G(d,"craft_type_id")),amount=Math.Max(0,I(G(d,"amount")));if(ct<1||ct>6||id<0)return;
      int gradeRaw=type%100,rest=type/100,statId=rest%100,setId=rest/100,grade=gradeRaw>=10?gradeRaw-10:gradeRaw;string stat=Stat(statId),set=(ct==3||ct==4||setId==99)?"Immemorial":SetName(setId);if(stat.Length==0||set.Length==0)return;bool ancient=ct==5||ct==6;string kind=(ct==1||ct==3||ct==5)?"Gemme":"Meule";
      // Convert/Amplify : amount = restant de CET item. Id connu = on ecrit ce restant.
      // Id inconnu (drop live / TSV overlay sans id) : on retire 1 sur la pile compatible.
      // Avant, on ajoutait une ligne fantome amount=0 et l'ancienne pile restait a 1 —
      // HasGemGrade voyait encore du stock, rune coincée dans Amelioration possible.
      var stock=id>0?Stocks.FirstOrDefault(x=>x.Id==id):null;
      if(stock!=null){stock.Amount=amount;return;}
      var hit=Stocks.FirstOrDefault(x=>x.Id==0&&x.Ancient==ancient&&x.Type==kind&&x.Set==set&&x.Stat==stat&&x.Grade==grade&&x.Amount>0)
           ??Stocks.FirstOrDefault(x=>x.Ancient==ancient&&x.Type==kind&&x.Set==set&&x.Stat==stat&&x.Grade==grade&&x.Amount>0);
      if(hit!=null)hit.Amount=Math.Max(0,hit.Amount-1);}
    public static void CapAfterUpgrade(RuneRow rune,double previousPotential){
      if(rune==null||rune.Potential<=previousPotential)return;rune.Potential=Math.Round(previousPotential,3);for(int i=0;i<rune.Scores.Length;i++)if(rune.Scores[i]>rune.Potential)rune.Scores[i]=rune.Potential;
      rune.Action=rune.Level<12?(PowerSpeed(rune)||rune.Potential>=PwrUpThreshold()?"Pwr up":"Sell"):(PremiumKeep(rune)||rune.Potential>=SeuilApres12?"Keep":"Sell");if(ForceInstantSell(rune))rune.Action="Sell";if(rune.Marker.IndexOf("Reeval",StringComparison.OrdinalIgnoreCase)>=0||rune.Marker.IndexOf("Deck",StringComparison.OrdinalIgnoreCase)>=0)rune.Action=rune.Level<12?"Pwr up":"Keep";
    }
    public static long LiveRuneId(string responseJson){try{var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var root=D(js.DeserializeObject(responseJson));return L(G(D(G(root,"rune")),"rune_id"));}catch{return 0;}}
    public static void PreserveAfterCraft(RuneRow rune,double previousPotential){
      if(rune==null)return;rune.Potential=Math.Round(previousPotential,3);rune.Action=rune.Level<12?(PowerSpeed(rune)||rune.Potential>=PwrUpThreshold()?"Pwr up":"Sell"):(PremiumKeep(rune)||rune.Potential>=SeuilApres12?"Keep":"Sell");if(ForceInstantSell(rune))rune.Action="Sell";if(rune.Marker.IndexOf("Reeval",StringComparison.OrdinalIgnoreCase)>=0||rune.Marker.IndexOf("Deck",StringComparison.OrdinalIgnoreCase)>=0)rune.Action=rune.Level<12?"Pwr up":"Keep";
    }
    public static void Calculate(List<RuneRow> rows){PresetSlotCounts.Clear();int[,] counts=new int[7,6];var setSlot=new Dictionary<string,int>();var setTotal=new Dictionary<string,int>();var raw=new double[rows.Count,7];var projected=new RuneRow[rows.Count,7];for(int i=0;i<rows.Count;i++){int bp=0;double best=-1;for(int p=0;p<7;p++){projected[i,p]=BestProjection(rows[i],Presets[p]);raw[i,p]=Score(projected[i,p],Presets[p]);if(raw[i,p]>best){best=raw[i,p];bp=p;}}// Demande Jeremy 2026-09-17 : recevoir un paquet de runes +0 en donjon faisait bouger le
      // "bonus stock" (ScarcityBonus/InventoryBonus) de runes deja finies (+12/+15), les faisant
      // passer le seuil et apparaitre/disparaitre de Tri Amelioration Disponible pour rien — la
      // rune +0 elle-meme n'avait pas change, juste le compte relatif par slot. Une rune +0 recoit
      // toujours son propre bonus stock normalement (lu depuis PresetSlotCounts plus bas via
      // InventoryBonus), mais ne doit plus ALIMENTER ce compte tant qu'elle n'est pas elle-meme
      // +12 — sinon du loot jamais touche destabilise le score de runes deja gardees.
      if(best>0&&IsUsable(rows[i],best)&&rows[i].Level>=12){counts[bp,Math.Max(0,rows[i].Slot-1)]++;string pk=Presets[bp].Name+"|"+rows[i].Set;int[] pc;if(!PresetSlotCounts.TryGetValue(pk,out pc))PresetSlotCounts[pk]=pc=new int[6];pc[Math.Max(0,Math.Min(5,rows[i].Slot-1))]++;string s=rows[i].Set.ToLowerInvariant(),k=s+"|"+rows[i].Slot;setSlot[k]=setSlot.ContainsKey(k)?setSlot[k]+1:1;setTotal[s]=setTotal.ContainsKey(s)?setTotal[s]+1:1;}}
      double[] avg=new double[7];for(int p=0;p<7;p++){for(int s=0;s<6;s++)avg[p]+=counts[p,s];avg[p]/=6.0;}for(int i=0;i<rows.Count;i++){var r=rows[i];string previousBuild=r.BestBuild;double best=-1,refinementBest=0;int bp=0,refinementPreset=-1;for(int p=0;p<7;p++){double bonus=raw[i,p]>0?InventoryBonus(Presets[p],r.Set,r.Slot):0;r.Scores[p]=raw[i,p]+bonus;if(r.Scores[p]>best){best=r.Scores[p];bp=p;}if(CanRefine(r)){double rs=ExpectedRefinementScore(r,Presets[p])+bonus;if(rs>refinementBest){refinementBest=rs;refinementPreset=p;}}}CalculateReevalPriority(r,counts,avg,setSlot,setTotal);if(best<=0){SetNoPreset(r);continue;}int prevIdx=string.IsNullOrEmpty(previousBuild)?-1:Presets.FindIndex(x=>x.Name==previousBuild);if(prevIdx>=0&&prevIdx!=bp&&raw[i,prevIdx]>0&&best-r.Scores[prevIdx]<BuildStabilityMargin){bp=prevIdx;best=r.Scores[prevIdx];}var chosen=projected[i,bp];r.Potential=Math.Round(best+RuleBonus(r),3);r.BestBuild=Presets[bp].Name;r.Recommendation=Recommend(chosen,Presets[bp]);r.RecommendSource=chosen.RecommendSource;r.RecommendTarget=chosen.RecommendTarget;r.RecommendationInStock=chosen.RecommendationInStock;r.RefinementPotential=Math.Round(refinementBest,3);r.RefinementGain=Math.Round(Math.Max(0,refinementBest-best),3);r.RefinementPreset=refinementPreset>=0?Presets[refinementPreset].Name:"";}ApplyRetentionRules(rows);}
    public static void ApplyRetentionRules(List<RuneRow> rows){if(rows==null)return;var completed=rows.Where(r=>r.Level>=12).ToList();var mandatory=completed.Where(IsMandatoryProtected).OrderByDescending(r=>r.Potential).ThenByDescending(r=>r.Id).ToList();IEnumerable<RuneRow> eligible=completed.Where(r=>!IsBlue(r)&&!ForceInstantSell(r));List<RuneRow> selected;if(SeuilVenteFixe){SeuilApres12=Math.Max(0,ValeurSeuilVenteFixe);selected=eligible.Where(r=>r.Potential>=SeuilApres12).OrderByDescending(r=>r.Potential).ThenByDescending(r=>r.Obtained).ThenByDescending(r=>r.Id).ToList();}else{selected=eligible.Where(r=>r.Potential>=SeuilQualiteMinimum).OrderByDescending(r=>r.Potential).ThenByDescending(r=>r.Obtained).ThenByDescending(r=>r.Id).Take(LimiteRunesConservees).ToList();SeuilApres12=selected.Count>=LimiteRunesConservees?Math.Max(SeuilQualiteMinimum,selected[selected.Count-1].Potential):SeuilQualiteMinimum;}var keep=new HashSet<long>(mandatory.Concat(selected).Select(r=>r.Id));foreach(var r in rows){if(r.Level>=12)r.Action=keep.Contains(r.Id)?"Keep":"Sell";else r.Action=IsMandatoryProtected(r)||(!IsBlue(r)&&!ForceInstantSell(r)&&r.Potential>=PwrUpThreshold())?"Pwr up":"Sell";}}
    public static double RetentionThreshold(List<RuneRow> rows,int limit){if(rows==null||limit<1)return SeuilQualiteMinimum;var selected=rows.Where(r=>r.Level>=12&&!IsBlue(r)&&!ForceInstantSell(r)&&r.Potential>=SeuilQualiteMinimum).OrderByDescending(r=>r.Potential).ThenByDescending(r=>r.Obtained).ThenByDescending(r=>r.Id).Take(limit).ToList();return selected.Count>=limit?Math.Max(SeuilQualiteMinimum,selected[selected.Count-1].Potential):SeuilQualiteMinimum;}
    public static int RetentionEligibleCount(List<RuneRow> rows){return rows==null?0:rows.Count(r=>r.Level>=12&&!IsBlue(r)&&!ForceInstantSell(r)&&r.Potential>=SeuilQualiteMinimum);}
    static bool IsDeckProtected(RuneRow r){return r.Marker.IndexOf("Deck",StringComparison.OrdinalIgnoreCase)>=0;}
    static bool IsReevalProtected(RuneRow r){return r.Marker.IndexOf("Reeval",StringComparison.OrdinalIgnoreCase)>=0;}
    static bool IsSpeedProtected(RuneRow r){return r.Marker.IndexOf("rune spd",StringComparison.OrdinalIgnoreCase)>=0||PowerSpeed(r)||PremiumKeep(r);}
    static bool IsMandatoryProtected(RuneRow r){return ProtectedWorldBossRuneIds.Contains(r.Id)||IsDeckProtected(r)||(!IsBlue(r)&&!ForceInstantSell(r)&&(IsReevalProtected(r)||IsSpeedProtected(r)));}
    static bool IsBlue(RuneRow r){return r.Grade<=3;}
    public static bool CanRefine(RuneRow r){return r!=null&&r.Grade>=5&&r.Level>=12&&!r.Ancient&&!r.Set.Equals("Intangible",StringComparison.OrdinalIgnoreCase)&&r.Subs.Count==4&&!r.Subs.Any(x=>x.Gemmed)&&!ForceInstantSell(r);}
    static RuneRow BestProjection(RuneRow source,Preset preset){if(source.Level>=12)return source;int total=source.Grade>=5?4:source.Grade==4?3:source.Grade==3?2:1;int remaining=Math.Max(0,total-source.Level/3);int rollTargets=source.Subs.Count;var variants=new List<RuneRow>();if(source.Subs.Count>=4)variants.Add(CopyRune(source));else{string[] stats={"HP+","HP%","Atk+","Atk%","Def+","Def%","Spd","CtR%","CtD%","Res%","Acc%"};foreach(string stat in stats){if(stat==source.Main||stat==source.Innate||source.Subs.Any(x=>x.Stat==stat)||!SlotPossible(source.Slot,stat)||!AccResCompatible(source,stat))continue;var v=CopyRune(source);v.Subs.Add(new SubStat{Stat=stat,Value=RollMax(stat)});variants.Add(v);}}if(variants.Count==0)variants.Add(CopyRune(source));RuneRow winner=null;double winnerScore=-1;foreach(var variant in variants){var allocation=new int[Math.Max(1,rollTargets)];SearchProjection(variant,preset,rollTargets,remaining,0,allocation,ref winner,ref winnerScore);}return winner??CopyRune(source);}
    static RuneRow BestRefinementProjection(RuneRow source,Preset preset){var variant=CopyRune(source);foreach(var s in variant.Subs){s.Value=RollMax(s.Stat);s.Gemmed=false;}int rolls=source.Grade>=5?4:source.Grade==4?3:source.Grade==3?2:1;RuneRow winner=null;double score=-1;SearchProjection(variant,preset,variant.Subs.Count,rolls,0,new int[variant.Subs.Count],ref winner,ref score);return winner??variant;}
    static double ExpectedRefinementScore(RuneRow source,Preset preset){var variant=CopyRune(source);foreach(var s in variant.Subs){s.Value=RollAverage(s.Stat);s.Grind=0;s.Gemmed=false;}int rolls=source.Grade>=5?4:source.Grade==4?3:source.Grade==3?2:1;double weighted=0,totalWeight=0;ExpectedRefinementAllocation(variant,preset,rolls,0,new int[variant.Subs.Count],ref weighted,ref totalWeight);return totalWeight>0?weighted/totalWeight:0;}
    static void ExpectedRefinementAllocation(RuneRow variant,Preset preset,int remaining,int index,int[] allocation,ref double weighted,ref double totalWeight){if(index==allocation.Length-1){allocation[index]=remaining;var candidate=CopyRune(variant);candidate.Level=12;int rolls=0;double combinations=1;for(int i=0;i<allocation.Length;i++){rolls+=allocation[i];candidate.Subs[i].Value+=allocation[i]*RollAverage(candidate.Subs[i].Stat);combinations/=Factorial(allocation[i]);}combinations*=Factorial(rolls);weighted+=Score(candidate,preset)*combinations;totalWeight+=combinations;return;}for(int n=0;n<=remaining;n++){allocation[index]=n;ExpectedRefinementAllocation(variant,preset,remaining-n,index+1,allocation,ref weighted,ref totalWeight);}}
    static double Factorial(int n){double result=1;for(int i=2;i<=n;i++)result*=i;return result;}
    static void SearchProjection(RuneRow variant,Preset preset,int targets,int remaining,int index,int[] allocation,ref RuneRow winner,ref double winnerScore){if(targets==0){var empty=CopyRune(variant);empty.Level=12;double emptyScore=Score(empty,preset);if(emptyScore>winnerScore){winnerScore=emptyScore;winner=empty;}return;}if(index==targets-1){allocation[index]=remaining;var candidate=CopyRune(variant);candidate.Level=12;for(int i=0;i<targets;i++)candidate.Subs[i].Value+=allocation[i]*RollMax(candidate.Subs[i].Stat);double score=Score(candidate,preset);if(score>winnerScore){winnerScore=score;winner=candidate;}return;}for(int rolls=0;rolls<=remaining;rolls++){allocation[index]=rolls;SearchProjection(variant,preset,targets,remaining-rolls,index+1,allocation,ref winner,ref winnerScore);}}
    static RuneRow CopyRune(RuneRow source){var copy=new RuneRow{Id=source.Id,Set=source.Set,Slot=source.Slot,Main=source.Main,MainValue=source.MainValue,Innate=source.Innate,InnateValue=source.InnateValue,Grade=source.Grade,Stars=source.Stars,Level=source.Level,Ancient=source.Ancient,Equipped=source.Equipped,EquippedUnitId=source.EquippedUnitId,EquippedMasterId=source.EquippedMasterId,Marker=source.Marker,Obtained=source.Obtained};foreach(var s in source.Subs)copy.Subs.Add(new SubStat{Stat=s.Stat,Value=s.Value,Gemmed=s.Gemmed,Grind=s.Grind});return copy;}
    static void CalculateReevalPriority(RuneRow r,int[,] counts,double[] avg,Dictionary<string,int> setSlot,Dictionary<string,int> setTotal){r.ReevalPriority=0;r.ReevalBestBuild="";if(r.Marker.IndexOf("Reeval",StringComparison.OrdinalIgnoreCase)<0||r.Grade<5)return;string sk=r.Set.ToLowerInvariant()+"|"+r.Slot;double setScarcity=1;int ss=setSlot.ContainsKey(sk)?setSlot[sk]:0;double setMean=setTotal.ContainsKey(r.Set.ToLowerInvariant())?setTotal[r.Set.ToLowerInvariant()]/6.0:0;setScarcity=ss<=0?1.35:Math.Max(1,Math.Min(1.35,Math.Pow(Math.Max(1,setMean)/ss,.35)));for(int pi=0;pi<Presets.Count;pi++){var p=Presets[pi];if(!p.Preferred.Contains(r.Set)&&!p.Accepted.Contains(r.Set))continue;HashSet<string> mains;if((r.Slot==2||r.Slot==4||r.Slot==6)&&p.Main.TryGetValue(r.Slot,out mains)&&!mains.Contains(r.Main))continue;var candidates=p.W.Where(k=>k.Value>0&&k.Key!=r.Main&&k.Key!=r.Innate&&SlotPossible(r.Slot,k.Key)).Select(k=>new{Stat=k.Key,W=Weight(p,k.Key,r.Set)}).OrderByDescending(x=>x.W).ToList();double theoretical=0;int used=0;foreach(var c in candidates){if(used>=4)break;double rolls=3.0;if(c.Stat=="Spd"&&PremiumSet(r.Set))rolls=r.Ancient?4.35:3.85;theoretical+=c.W*rolls;used++;}if(used<4)continue;double main=BonusStatPrincipale*((r.Slot==2||r.Slot==4||r.Slot==6)?Math.Max(Weight(p,r.Main,r.Set),.35):.35);double slotScarcity=counts[pi,Math.Max(0,r.Slot-1)]<=0?1.45:Math.Max(.82,Math.Min(1.45,Math.Pow(Math.Max(1,avg[pi])/counts[pi,Math.Max(0,r.Slot-1)],.45)));double setFit=p.Preferred.Contains(r.Set)?1:.92;double speedUpside=candidates.Any(x=>x.Stat=="Spd")?(PremiumSet(r.Set)?1.18:1.06):1;double score=(theoretical+main)*setFit*speedUpside*p.ScoreFactor+InventoryBonus(p,r.Set,r.Slot);if(score>r.ReevalPriority){r.ReevalPriority=Math.Round(score,3);r.ReevalBestBuild=p.Name;}}}
    static void SetNoPreset(RuneRow r){r.Potential=0;r.BestBuild="Aucun preset";r.Recommendation="";r.RecommendSource="";r.RecommendTarget="";r.RecommendationInStock=false;r.Action=r.Level<12?(PowerSpeed(r)?"Pwr up":"Sell"):(PremiumKeep(r)?"Keep":"Sell");if(ForceInstantSell(r))r.Action="Sell";if(r.Marker.IndexOf("Reeval",StringComparison.OrdinalIgnoreCase)>=0||r.Marker.IndexOf("Deck",StringComparison.OrdinalIgnoreCase)>=0)r.Action=r.Level<12?"Pwr up":"Keep";}
    static bool InvalidFlatSlot2(RuneRow r){return r!=null&&r.Slot==2&&(r.Main=="HP+"||r.Main=="Atk+"||r.Main=="Def+");}
    // Regle demandee par Jeremy : runes slot 4/6 a stat principale flat (HP+/Atk+/Def+) —
    // "testees" (evaluation normale, preset/score/Keep-Sell habituels) seulement si elles
    // peuvent atteindre 26 Spd (Spd actuelle + rolls futurs projetes via AchievableSpeed,
    // meme methode que les regles perso Stat=="Spd" Projected du bouton "Regles"). Sinon
    // vente instantanee. Protection deck conservee explicitement (rune equipee sur un deck
    // protege, ex. World Boss) : jamais de vente forcee par cette regle sur ces runes-la.
    static bool FlatMainSlot46LowSpeed(RuneRow r){return r!=null&&(r.Slot==4||r.Slot==6)&&(r.Main=="HP+"||r.Main=="Atk+"||r.Main=="Def+")&&!IsDeckProtected(r)&&AchievableSpeed(r)<26;}
    static bool ForceInstantSell(RuneRow r){return InvalidFlatSlot2(r)||FlatMainSlot46LowSpeed(r);}
    static bool IsUsable(RuneRow r,double score){if(ForceInstantSell(r))return false;if(IsBlue(r)&&!IsDeckProtected(r))return false;return score>0||IsDeckProtected(r);}
    static double Score(RuneRow r,Preset p){if(ForceInstantSell(r))return 0;if(!p.Preferred.Contains(r.Set)&&!p.Accepted.Contains(r.Set))return 0;HashSet<string> allowed;if((r.Slot==2||r.Slot==4||r.Slot==6)&&p.Main.TryGetValue(r.Slot,out allowed)&&!allowed.Contains(r.Main))return 0;double points=0,bestW=0;foreach(var s in r.Subs){double w=Weight(p,s.Stat,r.Set);bestW=Math.Max(bestW,w);points+=w*(s.Value+(r.Level>=12?GrindMax(s.Stat,r.Ancient):0))/RollMax(s.Stat);}if(r.Level>=12)points+=GemBonus(r,p);int totalRolls=r.Grade>=5?4:r.Grade==4?3:r.Grade==3?2:1;int remaining=r.Level<12?Math.Max(0,totalRolls-r.Level/3):0;points+=remaining*bestW*.75;points+=BonusStatPrincipale*((r.Slot==2||r.Slot==4||r.Slot==6)?Math.Max(Weight(p,r.Main,r.Set),.35):.35);double setF=p.Preferred.Contains(r.Set)?1:FacteurSetAcceptable;return 10*Math.Pow(Math.Max(0,points/11),1.15)*setF*p.ScoreFactor;}
    // Simule le Spd "atteignable" en projetant les rolls futurs restants sur une rune pas
    // encore +12 (ou renvoie le Spd actuel si deja +12). Utilise par les regles Stat=="Spd"
    // Projected=true dans RuleBonus — independant du preset choisi ou de la repartition de
    // rolls jugee "optimale" pour ce preset, sinon le bonus se perdait des qu'un autre stat
    // rapportait un peu plus de points, meme quand la rune POUVAIT viser un palier Spd.
    static double AchievableSpeed(RuneRow r){
      double achievable;
      if(r.Level>=12)achievable=Speed(r);
      else{
        var spdSub=r.Subs.FirstOrDefault(x=>x.Stat=="Spd");
        int total=r.Grade>=5?4:r.Grade==4?3:r.Grade==3?2:1;int remaining=Math.Max(0,total-r.Level/3);
        if(spdSub!=null)achievable=spdSub.Value+remaining*RollMax("Spd");
        else if(remaining>0&&r.Subs.Count<4&&SlotPossible(r.Slot,"Spd")&&r.Main!="Spd"&&r.Innate!="Spd")achievable=remaining*RollMax("Spd");
        else achievable=Speed(r);
      }
      return achievable;
    }
    // Valeur actuelle (non projetee) d'une stat sur une rune, pour les regles perso qui ne
    // sont pas Stat=="Spd"+Projected. Si la stat est la stat principale de la rune, on
    // considere le seuil toujours atteint (valeur sentinelle tres haute).
    static double CurrentStatValue(RuneRow r,string stat){
      if(r.Main==stat)return 999999;
      var sub=r.Subs.FirstOrDefault(x=>x.Stat==stat);
      if(sub==null)return 0;
      return sub.Value+(r.Level>=12?GrindMax(stat,r.Ancient):0);
    }
    // Applique toutes les regles de ScoreRules (bouton "Regles") et additionne les bonus des
    // regles qui matchent (Set + seuil de Stat atteint). Voir commentaire sur ScoreRules.
    static double RuleBonus(RuneRow r){
      double total=0;
      foreach(var rule in ScoreRules){
        if(rule.Sets.Count>0&&!rule.Sets.Any(x=>string.Equals(x,r.Set,StringComparison.OrdinalIgnoreCase)))continue;
        if(rule.Slots.Count>0&&!rule.Slots.Contains(r.Slot))continue;
        double value=rule.Projected&&string.Equals(rule.Stat,"Spd",StringComparison.OrdinalIgnoreCase)?AchievableSpeed(r):CurrentStatValue(r,rule.Stat);
        if(value>=rule.Threshold)total+=rule.Bonus;
      }
      return total;
    }
    static double GemBonus(RuneRow r,Preset p){double best=0;bool hasGem=r.Subs.Any(x=>x.Gemmed);foreach(var src in r.Subs){if(hasGem&&!src.Gemmed)continue;if(PercentMustBeKept(r,src,hasGem))continue;foreach(string target in p.W.Keys){bool same=src.Stat==target;if(p.W[target]<=0||(!same&&r.Subs.Any(x=>x.Stat==target))||target==r.Main||target==r.Innate||!SlotPossible(r.Slot,target)||!AccResCompatible(r,target))continue;if(Rank(p,target)>Rank(p,src.Stat))continue;if(IsFlat(target)&&!IsFlat(src.Stat)&&Rank(p,target)>=Rank(p,src.Stat))continue;double gain=Contribution(target,GemMax(target,r.Ancient),Weight(p,target,r.Set),r.Ancient)-Contribution(src.Stat,src.Value,Weight(p,src.Stat,r.Set),r.Ancient);best=Math.Max(best,gain);}}return Math.Max(0,best);}
    static string Recommend(RuneRow r,Preset p){double best=double.MinValue;SubStat source=null;string target="";bool hasGem=r.Subs.Any(x=>x.Gemmed);r.RecommendationInStock=false;foreach(var src in r.Subs){if(hasGem&&!src.Gemmed)continue;if(PercentMustBeKept(r,src,hasGem))continue;foreach(string t in p.W.Keys){if(p.W[t]<=0||t==r.Main||t==r.Innate||!SlotPossible(r.Slot,t)||!AccResCompatible(r,t))continue;bool same=src.Stat==t;if(!same&&r.Subs.Any(x=>x.Stat==t))continue;if(Rank(p,t)>Rank(p,src.Stat))continue;if(IsFlat(t)&&!IsFlat(src.Stat)&&Rank(p,t)>=Rank(p,src.Stat))continue;double available=DisplayedGemMax(r,t);if(same&&available<=src.Value&&GemMax(t,r.Ancient)>src.Value)available=GemMax(t,r.Ancient);double gain=Contribution(t,available,Weight(p,t,r.Set),r.Ancient)-Contribution(src.Stat,src.Value,Weight(p,src.Stat,r.Set),r.Ancient);if(gain>best){best=gain;source=src;target=t;}}}if(source==null){r.RecommendSource="";r.RecommendTarget="";return Loc.T("gem_max");}double displayed=DisplayedGemMax(r,target);bool inStock=HasGemGrade(r,target,5)||HasGemGrade(r,target,4);if(target==source.Stat&&displayed<=source.Value&&GemMax(target,r.Ancient)>source.Value){displayed=GemMax(target,r.Ancient);inStock=HasGemGrade(r,target,5);}double actualGain=Contribution(target,displayed,Weight(p,target,r.Set),r.Ancient)-Contribution(source.Stat,source.Value,Weight(p,source.Stat,r.Set),r.Ancient);if((target==source.Stat&&displayed<=source.Value)||actualGain<=0){r.RecommendSource="";r.RecommendTarget="";return Loc.T("gem_max");}r.RecommendSource=source.Stat;r.RecommendTarget=target;r.RecommendationInStock=inStock;return source.BaseDisplay+" → "+new SubStat{Stat=target,Value=displayed}.BaseDisplay;}
    static bool PercentMustBeKept(RuneRow r,SubStat src,bool hasGem){string flat=src.Stat=="HP%"?"HP+":src.Stat=="Atk%"?"Atk+":src.Stat=="Def%"?"Def+":"";if(flat.Length==0)return false;return r.Subs.Any(x=>x.Stat==flat&&(!hasGem||x.Gemmed));}
    static double Contribution(string s,double v,double w,bool a){return w*(v/RollMax(s)+(Grindable(s)?.5:0));} static bool IsFlat(string s){return s=="HP+"||s=="Atk+"||s=="Def+";} static bool Grindable(string s){return s=="HP+"||s=="HP%"||s=="Atk+"||s=="Atk%"||s=="Def+"||s=="Def%"||s=="Spd";}
    static bool SlotPossible(int slot,string s){return !(slot==1&&(s=="Def+"||s=="Def%"))&&!(slot==3&&(s=="Atk+"||s=="Atk%"));} static bool AccResCompatible(RuneRow r,string t){if(t=="Acc%")return r.Main!="Res%"&&!r.Subs.Any(x=>x.Stat=="Res%");if(t=="Res%")return r.Main!="Acc%"&&!r.Subs.Any(x=>x.Stat=="Acc%");return true;}
    static double Weight(Preset p,string s,string set){double priority;if(!p.W.TryGetValue(s,out priority)||priority<=0)return 0;double w=priority==1?PoidsP1:priority==2?PoidsP2:PoidsP3;if(IsFlat(s))w*=priority==1?.55:.45;if(s=="CtR%"||s=="CtD%"||s=="Acc%"||s=="Res%")w*=1.3;string q=set.ToLowerInvariant();if(s=="Res%"&&(q=="endure"||q=="energy"))w*=1.15;if(s=="Acc%"&&(q=="seal"||q=="fight"||q=="despair"||q=="focus"))w*=1.15;if(s=="Spd"&&(p.Name=="Support"||p.Name=="PvP def"||p.Name=="Bomber"||p.Name=="Bruiser"||p.Name=="Fast DD"))w*=1.05;double gf;if(StatGlobalFactor.TryGetValue(s,out gf))w*=gf;return w;}
    static int Rank(Preset p,string s){double priority;return p.W.TryGetValue(s,out priority)&&priority>0?(int)Math.Round(priority):4;}
    static double RollMax(string s){return s=="HP+"?375:(s=="Atk+"||s=="Def+")?20:(s=="Spd"||s=="CtR%")?6:s=="CtD%"?7:8;} static double RollAverage(string s){return s=="HP+"?255:(s=="Atk+"||s=="Def+")?15:s=="Spd"?5:s=="CtR%"?5:s=="CtD%"?5.5:6;} static double GrindMax(string s,bool a){if(s=="HP+")return a?610:550;if(s=="Atk+"||s=="Def+")return a?34:30;if(s=="HP%"||s=="Atk%"||s=="Def%")return a?12:10;if(s=="Spd")return a?6:5;return 0;} static double GemMax(string s,bool a){if(s=="HP+")return a?640:580;if(s=="Atk+"||s=="Def+")return a?44:40;if(s=="HP%"||s=="Atk%"||s=="Def%")return a?15:13;if(s=="Spd")return a?11:10;if(s=="CtR%")return a?10:9;if(s=="CtD%")return a?12:10;if(s=="Res%"||s=="Acc%")return a?13:11;return 0;}
    static double GemMaxViolet(string s,bool a){if(s=="HP+")return a?440:380;if(s=="Atk+"||s=="Def+")return a?30:26;if(s=="HP%"||s=="Atk%"||s=="Def%")return a?13:11;if(s=="Spd")return a?9:8;if(s=="CtR%")return a?8:7;if(s=="CtD%")return a?10:8;if(s=="Res%"||s=="Acc%")return a?11:9;return 0;}
    static bool HasGemGrade(RuneRow r,string stat,int grade){return Stocks.Any(x=>x.Type=="Gemme"&&x.Stat==stat&&x.Ancient==r.Ancient&&(x.Set==r.Set||(!r.Ancient&&x.Set=="Immemorial"))&&x.Grade==grade&&x.Amount>0);}
    static double DisplayedGemMax(RuneRow r,string stat){if(HasGemGrade(r,stat,5))return GemMax(stat,r.Ancient);if(HasGemGrade(r,stat,4))return GemMaxViolet(stat,r.Ancient);return GemMax(stat,r.Ancient);}
    static bool PremiumSet(string s){return new[]{"Violent","Swift","Will","Intangible","Despair","Seal"}.Contains(s);} static double Speed(RuneRow r){var s=r.Subs.FirstOrDefault(x=>x.Stat=="Spd");return s==null?0:s.Value;}
    // Auto-Keep : grille set x stat editable par l'utilisateur (bouton "Auto-Keep" dans
    // RuneManagerApp.cs). Une rune +12 est gardee (Keep) des qu'une de ses sous-stats
    // atteint le seuil configure pour son set, meme sous le seuil de vente normal (Potential).
    // 0/absent = desactive pour cette case. Valeurs par defaut = ancien comportement fige
    // (seul SPD avait un seuil : 22 pour les sets "premium", 24 pour les autres), pour ne
    // rien changer tant que Jeremy n'edite pas la grille.
    public static readonly string[] AutoKeepStats={"HP%","Atk%","Def%","Spd","Res%","Acc%","CtR%","CtD%","HP+","Atk+","Def+"};
    static Dictionary<string,Dictionary<string,double>> BuildDefaultAutoKeep(){var d=new Dictionary<string,Dictionary<string,double>>(StringComparer.OrdinalIgnoreCase);foreach(var setName in Sets.Values.Distinct()){var row=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);foreach(var st in AutoKeepStats)row[st]=0;row["Spd"]=PremiumSet(setName)?22:24;d[setName]=row;}return d;}
    public static Dictionary<string,Dictionary<string,double>> AutoKeepThresholds=BuildDefaultAutoKeep();
    static bool PremiumKeep(RuneRow r){if(r.Level<12)return false;Dictionary<string,double> row;if(!AutoKeepThresholds.TryGetValue(r.Set,out row))return false;foreach(var sub in r.Subs){double th;if(row.TryGetValue(sub.Stat,out th)&&th>0&&sub.Value>=th)return true;}return false;}
    static bool PowerSpeed(RuneRow r){if(r.Level>=12||Speed(r)<=0)return false;int rem=r.Grade>=5?Math.Max(0,4-r.Level/3):PremiumSet(r.Set)?Math.Max(0,3-r.Level/3):0;return Speed(r)+rem*6>=23;}
    static Preset P(string name,string[] weights,string pref,string acc,string s2,string s4,string s6){var p=new Preset{Name=name,ScoreFactor=(name=="Def DD"||name=="Bomber")?.8:1};string[] st={"HP%","Atk%","Def%","Spd","Res%","Acc%","CtR%","CtD%","HP+","Atk+","Def+"};for(int i=0;i<st.Length;i++)p.W[st[i]]=weights[i]=="P1"?1:weights[i]=="P2"?2:weights[i]=="P3"?3:0;foreach(var x in pref.Split(','))p.Preferred.Add(x.Trim());foreach(var x in acc.Split(','))p.Accepted.Add(x.Trim());p.Main[2]=new HashSet<string>(s2.Split(',').Select(x=>x.Trim()));p.Main[4]=new HashSet<string>(s4.Split(',').Select(x=>x.Trim()));p.Main[6]=new HashSet<string>(s6.Split(',').Select(x=>x.Trim()));return p;}
    static List<Preset>CreatePresets(){return new List<Preset>{
      P("Fast DD",new[]{"P2","P1","Non","P1","Non","P2","P1","P1","P3","P1","Non"},"Blade,Fight,Rage,Intangible,Swift,Will,Violent","Despair,Nemesis,Fatal,Focus,Vampire,Shield,Revenge","Atk%,Spd","CtD%","Atk%"),
      P("Slow DD",new[]{"P2","P1","Non","P2","Non","P2","P1","P1","P3","P1","Non"},"Fight,Rage,Violent,Will,Intangible,Blade,Shield","Fatal,Revenge,Vampire,Despair,Nemesis,Focus","Atk%","CtD%","Atk%"),
      P("Def DD",new[]{"P2","Non","P1","P2","Non","P2","P1","P1","P2","Non","P1"},"Determination,Guard,Rage,Will,Intangible,Blade","Despair,Violent,Fight","Def%","CtD%,Def%","Def%"),
      P("Bomber",new[]{"P2","P1","Non","P1","Non","P1","Non","Non","P3","P1","Non"},"Fatal,Will,Intangible","Fight,Focus,Violent","Atk%,Spd","Atk%","Atk%"),
      P("Support",new[]{"P1","P2","P2","P1","P2","P1","Non","Non","P2","Non","P3"},"Despair,Swift,Violent,Will,Intangible,Seal","Determination,Enhance,Energy,Tolerance,Guard,Nemesis,Revenge,Shield","Spd,HP%","HP%,Atk%,Def%","HP%,Acc%,Atk%,Def%"),
      P("PvP def",new[]{"P1","Non","P2","P1","P1","P2","Non","Non","P2","Non","P3"},"Intangible,Despair,Nemesis,Violent,Will,Seal","Shield,Destroy,Determination,Energy,Revenge,Fight,Endure","Spd,HP%","HP%,Def%","HP%,Def%"),
      P("Bruiser",new[]{"P1","P2","P3","P1","Non","P2","P1","P2","P2","P3","Non"},"Swift,Despair,Destroy,Revenge,Vampire,Violent,Will,Intangible","Seal,Nemesis","Spd,Atk%,HP%","CtD%,CtR%,HP%,Atk%","Atk%,HP%")};}
  }
}
