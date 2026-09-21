using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Web.Script.Serialization;

class ArtifactManagerEngine {
  static Dictionary<string,object> D(object o){ return o as Dictionary<string,object>; }
  static object[] A(object o){ return o as object[] ?? new object[0]; }
  static object G(Dictionary<string,object> d,string k,object z=null){ object v; return d!=null&&d.TryGetValue(k,out v)?v:z; }
  static string S(object o){ return Convert.ToString(o,CultureInfo.InvariantCulture)??""; }
  static int I(object o){ int v; return int.TryParse(S(o),NumberStyles.Any,CultureInfo.InvariantCulture,out v)?v:0; }
  static double F(object o){ double v; return double.TryParse(S(o),NumberStyles.Any,CultureInfo.InvariantCulture,out v)?v:0; }
  static string DisplayValue(double v){ return (Math.Truncate(v*100)/100).ToString("0.##",CultureInfo.InvariantCulture); }
  class Roll { public double Avg,Max; }
  class Profile { public int E,T; public string Monster,Preset,Mode,PE,PT,RE,RT; public Dictionary<string,double> WE,WT,MPE,MPT; }
  class Sub { public string Name; public double Value; public bool Converted; }
  class Usage { public double Score; public string Content; public Dictionary<string,double> Scores=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase); }
  class Eval { public double Value; public string Reco; public Profile Profile; }
  static Dictionary<string,string> names, units;
  static Dictionary<string,Roll> rolls;
  static Dictionary<string,List<Profile>> groups;
  static Dictionary<string,Usage> monsterUsage;
  static HashSet<string> deckArtifacts;
  static HashSet<string> LoadWorldBossArtifacts(){
    var result=new HashSet<string>();
    try{
      string[] candidates={
        @"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\worldboss-main\worldboss-plan.json",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RuneManagerModern","worldboss-plan.json")
      };
      foreach(var path in candidates){
        if(!File.Exists(path))continue;
        var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};
        var root=D(js.DeserializeObject(File.ReadAllText(path,Encoding.UTF8)));
        foreach(var rowRaw in A(G(root,"Rows"))){
          var row=D(rowRaw);
          foreach(var artRaw in A(G(row,"ArtifactDetails"))){
            var art=D(artRaw);string id=S(G(art,"Id"));if(id.Length>0)result.Add(id);
          }
        }
        if(result.Count>0)break;
      }
    }catch{}
    return result;
  }
  static double keep=6,bonus=.75,divisor=2.5,conversionFactor=.2;
  static string Stat(int code){ string v; return names.TryGetValue(code.ToString(),out v)?v:code.ToString(); }
  static string UnitName(Int64 id){string v;if(units.TryGetValue(id.ToString(),out v))return v;int family=(int)(id/100)*100,element=(int)(id%10);if(element>=1&&element<=5&&units.TryGetValue((family+10+element).ToString(),out v))return v;return "";}
  static Dictionary<string,double> WeightMap(object o){ var r=new Dictionary<string,double>();var d=D(o);if(d!=null)foreach(var x in d)r[x.Key]=F(x.Value);return r; }
  static Dictionary<string,double> MainPctMap(object swlensSection){var r=new Dictionary<string,double>();var d=D(swlensSection);if(d==null)return r;foreach(var raw in A(G(d,"primary"))){var e=D(raw);if(e==null)continue;string stat=S(G(e,"stat"));if(stat.Length>0)r[stat]=F(G(e,"pct"));}return r;}
  static bool FlatMatch(string pri,string pref){return (pri=="HP flat"&&pref=="HP")||(pri=="ATK flat"&&pref=="Attack")||(pri=="DEF flat"&&pref=="Defense");}
  // Score du stat principal a partir des pourcentages reels releves sur swlens (site tiers,
  // % de joueurs utilisant chaque stat principale par monstre). Remplace l'ancien bonus fixe :
  // le pire stat du monstre est penalise, et si un stat depasse 90% les deux autres sont
  // consideres inutiles (penalises) meme s'ils ne sont pas le pire au sens strict.
  static double MainStatScore(string priName,Dictionary<string,double> mainPct,string preferredFlat){
    if(mainPct==null||mainPct.Count==0){
      if(FlatMatch(priName,preferredFlat))return bonus;
      if((priName=="DEF flat"&&preferredFlat=="Attack")||(priName=="ATK flat"&&preferredFlat=="Defense"))return -bonus;
      return 0;
    }
    string key=priName=="HP flat"?"HP+":priName=="ATK flat"?"ATK+":priName=="DEF flat"?"DEF+":"";
    if(key.Length==0)return 0;
    double pct;mainPct.TryGetValue(key,out pct);
    double maxPct=0,minPct=100;
    foreach(var k in new[]{"HP+","ATK+","DEF+"}){double v;mainPct.TryGetValue(k,out v);if(v>maxPct)maxPct=v;if(v<minPct)minPct=v;}
    if(maxPct>=90)return pct>=90?bonus:-bonus;
    if(pct<=minPct+0.05)return -bonus;
    return bonus*(pct/100.0);
  }
  static bool EligibleUsage(Profile p){Usage u;if(p==null||!monsterUsage.TryGetValue(p.Monster,out u))return false;double score=0;if(p.Mode.Equals("RTA",StringComparison.OrdinalIgnoreCase)){u.Scores.TryGetValue("rta",out score);return score>=85;}if(p.Mode.Equals("Siege",StringComparison.OrdinalIgnoreCase)){double value;foreach(var key in new[]{"siege_atk","siege_def","wgb_atk","wgb_def"})if(u.Scores.TryGetValue(key,out value))score=Math.Max(score,value);return score>=85;}return u.Score>=85;}
  static Eval Evaluate(Dictionary<string,object> art,List<Sub> subs,Profile p){
    bool elem=I(G(art,"type"))==1; var w=elem?p.WE:p.WT; string pref=elem?p.PE:p.PT;
    bool intangible=elem?I(G(art,"attribute"))==98:I(G(art,"unit_style"))==98;
    bool alreadyConverted=subs.Any(s=>s.Converted);
    string required=elem?p.RE:p.RT;var pri=A(G(art,"pri_effect"));
    if(required.Length>0&&(pri.Length==0||!FlatMatch(Stat(I(pri[0])),required)))return null;
    double total=0; var present=new HashSet<string>();var contributions=new Dictionary<Sub,double>();
    foreach(var s in subs){Roll rr;double av=rolls.TryGetValue(s.Name,out rr)&&rr.Avg!=0?rr.Avg:1;double ww;w.TryGetValue(s.Name,out ww);double c=s.Value/av*ww;total+=c;present.Add(s.Name);contributions[s]=c;}
    if(pri.Length>0)total+=MainStatScore(Stat(I(pri[0])),elem?p.MPE:p.MPT,pref);
    double gainBest=0;string source="-",target="";double targetMax=0;
    foreach(var s in subs.Where(x=>!alreadyConverted||x.Converted)){
      Roll same;double sameWeight;if(rolls.TryGetValue(s.Name,out same)&&same.Avg>0&&w.TryGetValue(s.Name,out sameWeight)){double sameGain=(same.Max-s.Value)/same.Avg*sameWeight;if(sameGain>gainBest+.000001){gainBest=sameGain;source=s.Name;target=s.Name;targetMax=same.Max;}}
      foreach(var x in w){if(present.Contains(x.Key))continue;Roll rr;if(!rolls.TryGetValue(x.Key,out rr)||rr.Avg==0)continue;double gain=rr.Max/rr.Avg*x.Value-contributions[s];if(gain>gainBest+.000001){gainBest=gain;source=s.Name;target=x.Key;targetMax=rr.Max;}}
    }
    string reco=intangible?"Intangible : conversion impossible":alreadyConverted?"Conversion deja optimale":"Deja optimal";if(!intangible&&gainBest>.05){total+=gainBest*conversionFactor;reco=source+" -> "+target+" "+targetMax.ToString("0.###",CultureInfo.InvariantCulture);}
    return new Eval{Value=total/divisor,Reco=reco,Profile=p};
  }
  static string Clean(object o){return S(o).Replace("\t"," ").Replace("\r"," ").Replace("\n"," ");}
  static void CollectDeckArtifactIds(object o,HashSet<string> ids){
    var d=D(o);if(d!=null){foreach(var x in d){if(x.Key.Equals("artifact_id_list",System.StringComparison.OrdinalIgnoreCase)){foreach(var id in A(x.Value)){var v=S(id);if(v!=""&&v!="0")ids.Add(v);}}CollectDeckArtifactIds(x.Value,ids);}return;}
    foreach(var x in A(o))CollectDeckArtifactIds(x,ids);
  }
  public static int Main(string[] args){
    try{
      if(args.Length<3){Console.Error.WriteLine("json data out");return 2;}
      var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};
      var db=D(js.DeserializeObject(File.ReadAllText(args[1],Encoding.UTF8)));var game=D(js.DeserializeObject(File.ReadAllText(args[0],Encoding.UTF8)));
      deckArtifacts=new HashSet<string>();foreach(var x in game){if(x.Key.IndexOf("deck",System.StringComparison.OrdinalIgnoreCase)>=0)CollectDeckArtifactIds(x.Value,deckArtifacts);}
      var worldBossArtifacts=LoadWorldBossArtifacts();
      names=D(G(db,"stat_names")).ToDictionary(x=>x.Key,x=>S(x.Value));units=D(G(db,"unit_names")).ToDictionary(x=>x.Key,x=>S(x.Value));
      monsterUsage=new Dictionary<string,Usage>(StringComparer.OrdinalIgnoreCase);var usageRaw=D(G(db,"monster_usage_scores"));if(usageRaw!=null)foreach(var x in usageRaw){var ud=D(x.Value);if(ud!=null){var usage=new Usage{Score=F(G(ud,"best_score")),Content=S(G(ud,"best_content"))};var scores=D(G(ud,"scores"));if(scores!=null)foreach(var value in scores)usage.Scores[value.Key]=F(value.Value);monsterUsage[x.Key]=usage;}}
      rolls=new Dictionary<string,Roll>();foreach(var x in D(G(db,"rolls"))){var r=D(x.Value);rolls[x.Key]=new Roll{Avg=F(G(r,"avg")),Max=F(G(r,"max"))};}
      var cfg=D(G(db,"config"));keep=F(G(cfg,"keep",6));bonus=F(G(cfg,"main_bonus",.75));divisor=F(G(cfg,"divisor",2.5));conversionFactor=F(G(cfg,"conversion_factor",.2));
      groups=new Dictionary<string,List<Profile>>();foreach(var o in A(G(db,"profiles"))){var d=D(o);var p=new Profile{E=I(G(d,"element_id")),T=I(G(d,"style_id")),Monster=S(G(d,"monster")),Preset=S(G(d,"preset")),Mode=S(G(d,"mode")),PE=S(G(d,"preferred_flat_element")),PT=S(G(d,"preferred_flat_type")),RE=S(G(d,"required_main_element")),RT=S(G(d,"required_main_type")),WE=WeightMap(G(d,"weights_element")),WT=WeightMap(G(d,"weights_type")),MPE=MainPctMap(G(d,"swlens_element")),MPT=MainPctMap(G(d,"swlens_type"))};foreach(var k in new[]{"E"+p.E,"S"+p.T}){if(!groups.ContainsKey(k))groups[k]=new List<Profile>();groups[k].Add(p);}}
      string monsterFilter=args.Length>3?args[3].Trim():"",categoryFilter=args.Length>4?args[4].Trim():"",modeFilter=args.Length>5?args[5].Trim():"";
      var priorityMonsters=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var reappTargets=new HashSet<string>();
      if(args.Length>6&&File.Exists(args[6]))foreach(var line in File.ReadAllLines(args[6],Encoding.UTF8))if(line.Trim().Length>0)priorityMonsters.Add(line.Trim());
      if(args.Length>7&&File.Exists(args[7]))foreach(var line in File.ReadAllLines(args[7],Encoding.UTF8))if(line.Trim().Length>0)reappTargets.Add(line.Trim());
      var owner=new Dictionary<string,string>();var ownedMonsters=new HashSet<string>(StringComparer.OrdinalIgnoreCase);var all=new List<Dictionary<string,object>>();
      foreach(var uo in A(G(game,"unit_list"))){var u=D(uo);string nm=UnitName((Int64)F(G(u,"unit_master_id")));owner[S(G(u,"unit_id"))]=nm.Length>0?nm:S(G(u,"unit_master_id"));if(nm.Length>0)ownedMonsters.Add(nm);foreach(var ao in A(G(u,"artifacts")))all.Add(D(ao));}
      foreach(var ao in A(G(game,"artifacts")))all.Add(D(ao));
      var seen=new HashSet<string>();var rows=new List<object[]>();var en=new Dictionary<int,string>{{1,"Eau"},{2,"Feu"},{3,"Vent"},{4,"Lumiere"},{5,"Tenebres"},{98,"Intangible"}};var sn=new Dictionary<int,string>{{1,"Attaque"},{2,"Defense"},{3,"PV"},{4,"Support"},{98,"Intangible"}};
      foreach(var a in all){string rid=S(G(a,"rid"));if(!seen.Add(rid))continue;var subs=new List<Sub>();foreach(var so in A(G(a,"sec_effects"))){var s=A(so);subs.Add(new Sub{Name=Stat(I(s[0])),Value=F(s[1]),Converted=s.Length>4&&I(s[4])!=0});}
        int type=I(G(a,"type")),attr=I(G(a,"attribute")),style=I(G(a,"unit_style"));if(categoryFilter.Equals("Element",StringComparison.OrdinalIgnoreCase)&&type!=1)continue;if(categoryFilter.Equals("Type",StringComparison.OrdinalIgnoreCase)&&type==1)continue;
        var candidates=new List<Profile>();
        if(type==1&&attr==98){foreach(var key in new[]{"E1","E2","E3","E4","E5"}){List<Profile> plist;if(groups.TryGetValue(key,out plist))candidates.AddRange(plist);}}
        else if(type!=1&&style==98){foreach(var key in new[]{"S1","S2","S3","S4"}){List<Profile> plist;if(groups.TryGetValue(key,out plist))candidates.AddRange(plist);}}
        else{List<Profile> exact;if(groups.TryGetValue(type==1?"E"+attr:"S"+style,out exact))candidates.AddRange(exact);}candidates=candidates.Where(x=>ownedMonsters.Contains(x.Monster)&&EligibleUsage(x)).ToList();
        Eval best=null;foreach(var p in candidates){if(monsterFilter.Length>0&&!string.Equals(p.Monster,monsterFilter,StringComparison.OrdinalIgnoreCase))continue;if(modeFilter.Length>0&&!string.Equals(p.Mode,modeFilter,StringComparison.OrdinalIgnoreCase))continue;var e=Evaluate(a,subs,p);if(e!=null&&(best==null||e.Value>best.Value))best=e;}if(best==null){if(monsterFilter.Length>0)continue;best=new Eval{Value=0,Reco="Aucune donnee SWLens",Profile=new Profile{Preset="",Mode=""}};}
        string restriction=type==1?(en.ContainsKey(attr)?en[attr]:attr.ToString()):(sn.ContainsKey(style)?sn[style]:style.ToString()),cat=type==1?"Element":"Type",rar=I(G(a,"natural_rank"))==5?"Legendaire":"Heroique",label=rar+" "+cat+" "+restriction+" +"+I(G(a,"level"));var pri=A(G(a,"pri_effect"));string primary=Stat(I(pri[0]))+" +"+S(pri[1]);string[] st=new string[4];for(int q=0;q<Math.Min(4,subs.Count);q++)st[q]=(subs[q].Converted?"[C] ":"")+subs[q].Name+" +"+DisplayValue(subs[q].Value);string own;owner.TryGetValue(S(G(a,"occupied_id")),out own);string action=(best.Value>=keep||deckArtifacts.Contains(rid)||worldBossArtifacts.Contains(rid))?"Keep":"Sell";
        bool intangible=(type==1&&attr==98)||(type!=1&&style==98);double reappPriority=0;string reappReason=intangible?"Intangible : Reappraisal impossible":priorityMonsters.Count==0?"Aucun monstre prioritaire":"Non compatible avec les monstres prioritaires";var reappCandidates=candidates.Where(x=>priorityMonsters.Contains(x.Monster)).ToList();if(!intangible&&I(G(a,"natural_rank"))==5&&I(G(a,"level"))>=15&&reappCandidates.Count>0){double possibleBest=0;Profile possibleProfile=null;foreach(var cp in reappCandidates){string required=type==1?cp.RE:cp.RT;if(required.Length>0&&(pri.Length==0||!FlatMatch(Stat(I(pri[0])),required)))continue;var wm=type==1?cp.WE:cp.WT;var maximumStats=new List<double>();foreach(var x in wm){Roll rr;if(!rolls.TryGetValue(x.Key,out rr)||rr.Avg<=0)continue;maximumStats.Add(rr.Max/rr.Avg*x.Value);}var chosen=maximumStats.OrderByDescending(x=>x).Take(4).ToList();double possible=chosen.Sum();if(chosen.Count>0)possible+=chosen.Max()*4;if(pri.Length>0)possible+=MainStatScore(Stat(I(pri[0])),type==1?cp.MPE:cp.MPT,type==1?cp.PE:cp.PT);possible/=divisor;if(possible>possibleBest){possibleBest=possible;possibleProfile=cp;}}reappPriority=Math.Round(possibleBest,3);double gain=reappPriority-best.Value;reappReason=possibleProfile==null?"Stat principale incompatible":"Gain potentiel "+gain.ToString("+0.000;-0.000;0.000",CultureInfo.InvariantCulture)+" | "+possibleProfile.Monster+" — "+possibleProfile.Preset;}
        rows.Add(new object[]{label,S(G(a,"date_add",rid)),cat,restriction,rar,I(G(a,"level")),primary,st[0],st[1],st[2],st[3],action,Math.Round(best.Value,3),best.Reco,best.Profile.Preset,best.Profile.Mode,own??"",I(G(a,"locked"))!=0?"Oui":"Non",rid,reappPriority,reappReason,0,best.Profile.Monster??""});
      }
      int artifactLimit=args.Length>8?Math.Max(100,Math.Min(10000,I(args[8]))):1500;
      bool fixedMode=args.Length>9&&args[9].Trim().Equals("fixed",StringComparison.OrdinalIgnoreCase);
      double fixedThreshold=args.Length>10?F(args[10]):8.8;
      var rowIds=new HashSet<string>(rows.Select(x=>S(x[18])));
      var eligibleTargets=new HashSet<string>(rows.Where(r=>reappTargets.Contains(S(r[18]))&&F(r[19])>0).Select(r=>S(r[18])));
      var baselineMandatory=new HashSet<string>(deckArtifacts.Concat(worldBossArtifacts).Where(x=>rowIds.Contains(x)&&!eligibleTargets.Contains(x)));
      var baselineKept=new HashSet<string>(baselineMandatory);
      if(fixedMode){foreach(var r in rows.Where(r=>!eligibleTargets.Contains(S(r[18]))&&F(r[12])>=fixedThreshold))baselineKept.Add(S(r[18]));}
      else{foreach(var r in rows.Where(r=>!eligibleTargets.Contains(S(r[18]))&&F(r[12])>=keep).OrderByDescending(x=>F(x[12])).ThenByDescending(x=>F(x[18]))){if(baselineKept.Count>=Math.Min(artifactLimit,rows.Count-eligibleTargets.Count))break;baselineKept.Add(S(r[18]));}}
      double baselineThreshold=fixedMode?fixedThreshold:rows.Where(x=>baselineKept.Contains(S(x[18]))&&!baselineMandatory.Contains(S(x[18]))).Select(x=>F(x[12])).DefaultIfEmpty(0).Min();
      var pendingTargets=new HashSet<string>(rows.Where(r=>eligibleTargets.Contains(S(r[18]))&&F(r[12])<baselineThreshold).Select(r=>S(r[18])));
      var mandatory=new HashSet<string>(deckArtifacts.Concat(worldBossArtifacts).Where(x=>rowIds.Contains(x)&&!pendingTargets.Contains(x)));
      var kept=new HashSet<string>(mandatory);
      if(fixedMode){foreach(var r in rows.Where(r=>!pendingTargets.Contains(S(r[18]))&&F(r[12])>=fixedThreshold))kept.Add(S(r[18]));}
      else{foreach(var r in rows.Where(r=>!pendingTargets.Contains(S(r[18]))&&F(r[12])>=keep).OrderByDescending(x=>F(x[12])).ThenByDescending(x=>F(x[18]))){if(kept.Count>=Math.Min(artifactLimit,rows.Count-pendingTargets.Count))break;kept.Add(S(r[18]));}}
      double retentionThreshold=fixedMode?fixedThreshold:rows.Where(x=>kept.Contains(S(x[18]))&&!mandatory.Contains(S(x[18]))).Select(x=>F(x[12])).DefaultIfEmpty(0).Min();
      foreach(var r in rows){string id=S(r[18]);r[11]=pendingTargets.Contains(id)?"Cible Reappraisal":kept.Contains(id)?"Keep":"Sell";r[21]=Math.Round(retentionThreshold,3);}
      rows.Sort((x,y)=>F(y[12]).CompareTo(F(x[12])));using(var w=new StreamWriter(args[2],false,new UTF8Encoding(false))){foreach(var r in rows)w.WriteLine(string.Join("\t",r.Select(Clean)));}Console.WriteLine(rows.Count);return 0;
    }catch(Exception ex){Console.Error.WriteLine(ex.ToString());return 1;}
  }
}
