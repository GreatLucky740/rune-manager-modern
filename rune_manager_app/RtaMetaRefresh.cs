using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;

namespace RuneManagerModern {
  static class RtaMetaRefresh {
    const string Sb="https://jljnwnydzodbisihlqcj.supabase.co/rest/v1";
    const string Key="sb_publishable_orn9Kf_ygbbjina8jLsdag_tnh1cwVS";
    static readonly Dictionary<int,string> ArtNames=new Dictionary<int,string>{
      {200,"ATK+ Prop. to Lost HP"},{201,"DEF+ Prop. to Lost HP"},{202,"SPD+ Prop. to Lost HP"},{203,"SPD Under Inability +"},
      {204,"ATK UP Effect +"},{205,"DEF UP Effect +"},{206,"SPD UP Effect +"},{207,"CRIT Rate Increasing Effect +"},
      {208,"Counterattack DMG +"},{209,"Co-op Attack DMG +"},{210,"Bomb DMG +"},{211,"Damage Dealt by Reflect DMG +"},
      {212,"Crushing Hit DMG +"},{213,"Damage Received Under Inability -"},{214,"CRIT DMG Taken -"},{215,"Life Drain +"},
      {216,"HP when Revived +"},{217,"Attack Bar when Revived +"},{218,"Add'l DMG Prop. to HP"},{219,"Add'l DMG Prop. to ATK"},
      {220,"Add'l DMG Prop. to DEF"},{221,"Add'l DMG Prop. to SPD"},{222,"CD+ as Enemy HP is More"},{223,"CD+ as Enemy HP is Less"},
      {224,"Own Turn 1-target CD+"},{225,"Counterattack/Co-op Attack DMG +"},{226,"ATK/DEF UP Effect +"},
      {300,"DMG dealt on Fire +"},{301,"DMG dealt on Water +"},{302,"DMG dealt on Wind +"},{303,"DMG dealt on Light +"},
      {304,"DMG dealt on Dark +"},{305,"DMG taken from Fire -"},{306,"DMG taken from Water -"},{307,"DMG taken from Wind -"},
      {308,"DMG taken from Light -"},{309,"DMG taken from Dark -"},{400,"[Skill 1] CRIT DMG +"},{401,"[Skill 2] CRIT DMG +"},
      {402,"[Skill 3] CRIT DMG +"},{403,"[Skill 4] CRIT DMG +"},{404,"S1 Recovery+"},{405,"S2 Recovery+"},{406,"S3 Recovery+"},
      {407,"S1 ACC+"},{408,"S2 ACC+"},{409,"S3 ACC+"},{410,"S3/S4 CRIT DMG+"},{411,"First Attack CD+"}
    };
    static Dictionary<string,object> D(object x){return x as Dictionary<string,object>;}
    static object[] A(object x){return x as object[]??new object[0];}
    static object G(Dictionary<string,object>d,string k,object z=null){object v;return d!=null&&d.TryGetValue(k,out v)?v:z;}
    static int I(object x){int n;return int.TryParse(Convert.ToString(x,CultureInfo.InvariantCulture),out n)?n:0;}
    static double F(object x){double n;return double.TryParse(Convert.ToString(x,CultureInfo.InvariantCulture),NumberStyles.Any,CultureInfo.InvariantCulture,out n)?n:0;}
    static string S(object x){return Convert.ToString(x??"");}
    static readonly JavaScriptSerializer Js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=128};

    static string HttpGet(string url,bool lucksack){
      ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;
      var req=(HttpWebRequest)WebRequest.Create(url);
      req.Timeout=45000;req.ReadWriteTimeout=45000;req.AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate;
      if(lucksack){
        req.UserAgent="Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/128.0.0.0 Safari/537.36";
        req.Referer="https://lucksack.gg/monsters/solo";req.Accept="application/json";
        req.Headers["Origin"]="https://lucksack.gg";
      }else{
        req.UserAgent="RuneManager/1.0";req.Accept="application/json";req.Headers["apikey"]=Key;
      }
      using(var response=req.GetResponse())using(var stream=response.GetResponseStream())using(var reader=new StreamReader(stream,Encoding.UTF8))return reader.ReadToEnd();
    }

    static List<Dictionary<string,object>> SbAll(string table,string select,string extra=null,int limit=1000){
      var rows=new List<Dictionary<string,object>>();
      for(int offset=0;offset<20000;offset+=limit){
        string url=Sb+"/"+table+"?select="+Uri.EscapeDataString(select)+"&limit="+limit+"&offset="+offset;
        if(!string.IsNullOrEmpty(extra))url+="&"+extra;
        var page=A(Js.DeserializeObject(HttpGet(url,false)));
        foreach(var raw in page){var d=D(raw);if(d!=null)rows.Add(d);}
        if(page.Length<limit)break;
      }
      return rows;
    }

    static List<Dictionary<string,object>> SbIn(string table,string select,IList<int> ids){
      var rows=new List<Dictionary<string,object>>();
      for(int i=0;i<ids.Count;i+=40){
        var slice=ids.Skip(i).Take(40).ToList();
        if(slice.Count==0)break;
        string extra="unit_master_id=in.("+string.Join(",",slice)+")";
        rows.AddRange(SbAll(table,select,extra));
      }
      return rows;
    }

    static string ArtName(Dictionary<string,object> row){
      int id=I(G(row,"effect_id"));string mapped;if(ArtNames.TryGetValue(id,out mapped))return mapped;
      string n=S(G(row,"effect_name"));return n.Length>0&&!n.StartsWith("Unknown")?n:"#"+id;
    }
    static string JoinPct(IEnumerable<Dictionary<string,object>> rows,int take,bool sub){
      return string.Join(sub?", ":" / ",rows.Take(take).Select(r=>(sub?ArtName(r):S(G(r,"effect_name")))+" "+F(G(r,"usage_percentage")).ToString("0.0",CultureInfo.InvariantCulture)+"%"));
    }
    static string SlotMains(IEnumerable<Dictionary<string,object>> rows){
      return string.Join(" • ",rows.GroupBy(r=>I(G(r,"slot_id"))).OrderBy(g=>g.Key).Select(g=>{
        var top=g.OrderByDescending(r=>F(G(r,"usage_percentage"))).Take(2).ToList();
        return "S"+g.Key+" "+string.Join(" / ",top.Select(r=>S(G(r,"effect_name"))+" "+F(G(r,"usage_percentage")).ToString("0.0",CultureInfo.InvariantCulture)+"%"));
      }));
    }

    static List<RtaMonster> TryLuckSack(){
      var all=new List<RtaMonster>();
      try{
        foreach(int season in new[]{38,39,37}){
          all.Clear();
          for(int offset=0;offset<2000;offset+=25){
            string url="https://api.swarena.gg/monsters?season="+season+"&isG3=false&isSL=false&played=0&orderBy=played&orderDirection=DESC&limit=25&offset="+offset;
            var root=D(Js.DeserializeObject(HttpGet(url,true)));
            var rows=A(G(root,"data"));
            foreach(var raw in rows){var d=D(raw);if(d==null)continue;all.Add(new RtaMonster{Id=I(G(d,"monster_id")),Name=S(G(d,"name")),Slug=S(G(d,"slug")),ImageFile=S(G(d,"image_filename")),WinRate=F(G(d,"win_rate")),PickRate=F(G(d,"pick_rate")),BanRate=F(G(d,"ban_rate")),LeadRate=F(G(d,"lead_rate")),Played=I(G(d,"played"))});}
            if(rows.Length<25)break;
          }
          if(all.Count>=50)return all.Where(x=>x.Id>0).GroupBy(x=>x.Id).Select(g=>g.First()).ToList();
        }
      }catch{}
      return new List<RtaMonster>();
    }

    public static string Run(string catalogPath){
      if(string.IsNullOrEmpty(catalogPath))throw new InvalidOperationException("catalogue manquant");
      string rtaDir=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(catalogPath),"..","rta"));
      Directory.CreateDirectory(rtaDir);
      string seasonPath=Path.Combine(rtaDir,"season38.json");
      string metaPath=Path.Combine(rtaDir,"rta-meta-top300.json");
      var lucksack=TryLuckSack();
      bool lsOk=lucksack.Count>=50;
      var local=new Dictionary<int,RtaMonster>();
      object savedPairs=null;int localSeason=38;
      if(File.Exists(seasonPath)){
        var localRoot=D(Js.DeserializeObject(File.ReadAllText(seasonPath)));
        savedPairs=G(localRoot,"pairs");localSeason=I(G(localRoot,"season"));if(localSeason<=0)localSeason=38;
        foreach(var raw in A(G(localRoot,"monsters"))){var x=A(raw);if(x.Length<9)continue;int id=I(x[0]);if(id>0&&!local.ContainsKey(id))local[id]=new RtaMonster{Id=id,Name=S(x[1]),Slug=S(x[2]),ImageFile=S(x[3]),WinRate=F(x[4]),PickRate=F(x[5]),BanRate=F(x[6]),LeadRate=F(x[7]),Played=I(x[8])};}
      }
      if(lsOk){
        lucksack.ForEach(m=>local[m.Id]=m);
        var seasonOut=new Dictionary<string,object>{{"season",localSeason},{"created",DateTime.UtcNow.ToString("o")},{"monsters",lucksack.Select(m=>new object[]{m.Id,m.Name,m.Slug,m.ImageFile,m.WinRate,m.PickRate,m.BanRate,m.LeadRate,m.Played}).ToArray()}};
        if(savedPairs!=null)seasonOut["pairs"]=savedPairs;
        File.WriteAllText(seasonPath,Js.Serialize(seasonOut),Encoding.UTF8);
      }
      var latest=SbAll("monster_stats","season_id","order=season_id.desc",1);
      string seasonId=latest.Count>0?S(G(latest[0],"season_id")):"";
      var swRows=string.IsNullOrEmpty(seasonId)?new List<Dictionary<string,object>>():SbAll("monster_stats","monster_id,play_rate,win_rate,ban_rate,season_id","season_id=eq."+Uri.EscapeDataString(seasonId));
      var swBy=swRows.GroupBy(r=>I(G(r,"monster_id"))).ToDictionary(g=>g.Key,g=>g.First());
      List<int> topIds;
      if(lsOk) topIds=lucksack.OrderByDescending(x=>x.PickRate).Select(x=>x.Id).Where(id=>id>0).Distinct().Take(300).ToList();
      else topIds=swRows.OrderByDescending(r=>F(G(r,"play_rate"))).Select(r=>I(G(r,"monster_id"))).Where(id=>id>0).Distinct().Take(300).ToList();
      var names=SbAll("monsters","id,name,element,family_name,archetype,image_filename").GroupBy(r=>I(G(r,"id"))).ToDictionary(g=>g.Key,g=>g.First());
      bool injected=false;
      foreach(int id in topIds){
        if(local.ContainsKey(id))continue;
        Dictionary<string,object> sw,info;swBy.TryGetValue(id,out sw);names.TryGetValue(id,out info);
        local[id]=new RtaMonster{Id=id,Name=info!=null?S(G(info,"name")):("Monstre "+id),ImageFile=info!=null?S(G(info,"image_filename")):"",WinRate=sw!=null?F(G(sw,"win_rate"))/100.0:0,PickRate=sw!=null?F(G(sw,"play_rate"))/100.0:0,BanRate=sw!=null?F(G(sw,"ban_rate"))/100.0:0,Played=0};
        injected=true;
      }
      if(injected&&!lsOk){
        var seasonOut=new Dictionary<string,object>{{"season",localSeason},{"created",DateTime.UtcNow.ToString("o")},{"monsters",local.Values.Select(m=>new object[]{m.Id,m.Name,m.Slug,m.ImageFile,m.WinRate,m.PickRate,m.BanRate,m.LeadRate,m.Played}).ToArray()}};
        if(savedPairs!=null)seasonOut["pairs"]=savedPairs;
        File.WriteAllText(seasonPath,Js.Serialize(seasonOut),Encoding.UTF8);
      }
      var sets=SbIn("monster_community_rune_sets","unit_master_id,set_names,usage_percentage,rank",topIds);
      var prim=SbIn("monster_community_primary_stats","unit_master_id,slot_id,effect_name,usage_percentage,rank",topIds);
      var subs=SbIn("monster_community_top_substats","unit_master_id,effect_name,total_usage_count,avg_percentage,rank",topIds);
      var artP=SbIn("monster_community_artifact_primary","unit_master_id,slot_id,effect_name,usage_percentage,rank",topIds);
      var artS=SbIn("monster_community_artifact_substats","unit_master_id,slot_id,effect_id,effect_name,usage_percentage,rank",topIds);
      var syn=SbAll("monster_community_synergy","monster1_id,monster2_id,using_count");
      var bySets=sets.GroupBy(r=>I(G(r,"unit_master_id"))).ToDictionary(g=>g.Key,g=>g.ToList());
      var byPrim=prim.GroupBy(r=>I(G(r,"unit_master_id"))).ToDictionary(g=>g.Key,g=>g.ToList());
      var bySubs=subs.GroupBy(r=>I(G(r,"unit_master_id"))).ToDictionary(g=>g.Key,g=>g.ToList());
      var byAp=artP.GroupBy(r=>I(G(r,"unit_master_id"))).ToDictionary(g=>g.Key,g=>g.ToList());
      var byAs=artS.GroupBy(r=>I(G(r,"unit_master_id"))).ToDictionary(g=>g.Key,g=>g.ToList());
      var bySyn=new Dictionary<int,Dictionary<int,int>>();
      foreach(var r in syn){
        int a=I(G(r,"monster1_id")),b=I(G(r,"monster2_id")),n=I(G(r,"using_count"));
        if(topIds.Contains(a)&&b>0){if(!bySyn.ContainsKey(a))bySyn[a]=new Dictionary<int,int>();int prev;bySyn[a][b]=bySyn[a].TryGetValue(b,out prev)?Math.Max(prev,n):n;}
        if(topIds.Contains(b)&&a>0){if(!bySyn.ContainsKey(b))bySyn[b]=new Dictionary<int,int>();int prev;bySyn[b][a]=bySyn[b].TryGetValue(a,out prev)?Math.Max(prev,n):n;}
      }
      var pack=new List<object>();int withSets=0;
      for(int rank=0;rank<topIds.Count;rank++){
        int id=topIds[rank];
        RtaMonster ls;local.TryGetValue(id,out ls);
        Dictionary<string,object> info,sw;names.TryGetValue(id,out info);swBy.TryGetValue(id,out sw);
        List<Dictionary<string,object>> setRows,subRows,pRows,apRows,asRows;
        bySets.TryGetValue(id,out setRows);bySubs.TryGetValue(id,out subRows);byPrim.TryGetValue(id,out pRows);byAp.TryGetValue(id,out apRows);byAs.TryGetValue(id,out asRows);
        setRows=setRows??new List<Dictionary<string,object>>();subRows=subRows??new List<Dictionary<string,object>>();pRows=pRows??new List<Dictionary<string,object>>();apRows=apRows??new List<Dictionary<string,object>>();asRows=asRows??new List<Dictionary<string,object>>();
        var combos=setRows.OrderByDescending(r=>F(G(r,"usage_percentage"))).Take(8).Select(r=>new Dictionary<string,object>{{"name",S(G(r,"set_names")).Replace(","," + ")},{"pct",Math.Round(F(G(r,"usage_percentage")),1)}}).ToList();
        if(combos.Count>0)withSets++;
        var subList=subRows.OrderByDescending(r=>F(G(r,"total_usage_count"))).Take(6).Select(r=>new Dictionary<string,object>{{"name",S(G(r,"effect_name"))},{"pct",Math.Round(F(G(r,"avg_percentage")),1)},{"count",I(G(r,"total_usage_count"))}}).ToList();
        Dictionary<int,int> synMap;bySyn.TryGetValue(id,out synMap);
        var synList=(synMap??new Dictionary<int,int>()).OrderByDescending(kv=>kv.Value).Take(6).Select(kv=>{Dictionary<string,object> nm;names.TryGetValue(kv.Key,out nm);return new Dictionary<string,object>{{"id",kv.Key},{"name",nm!=null?S(G(nm,"name")):kv.Key.ToString()},{"count",kv.Value}};}).ToList();
        var ap1=apRows.Where(r=>I(G(r,"slot_id"))==1).OrderByDescending(r=>F(G(r,"usage_percentage")));
        var ap2=apRows.Where(r=>I(G(r,"slot_id"))==2).OrderByDescending(r=>F(G(r,"usage_percentage")));
        var as1=asRows.Where(r=>I(G(r,"slot_id"))==1).OrderByDescending(r=>F(G(r,"usage_percentage")));
        var as2=asRows.Where(r=>I(G(r,"slot_id"))==2).OrderByDescending(r=>F(G(r,"usage_percentage")));
        pack.Add(new Dictionary<string,object>{
          {"rank",rank+1},{"id",id},{"name",info!=null?S(G(info,"name")):(ls!=null?ls.Name:("Monstre "+id))},
          {"slug",ls!=null?ls.Slug:""},{"element",info!=null?S(G(info,"element")):null},{"family",info!=null?S(G(info,"family_name")):null},{"role",info!=null?S(G(info,"archetype")):null},
          {"lucksack",new Dictionary<string,object>{{"win_rate",ls!=null?ls.WinRate:0},{"pick_rate",ls!=null?ls.PickRate:0},{"ban_rate",ls!=null?ls.BanRate:0},{"lead_rate",ls!=null?ls.LeadRate:0},{"played",ls!=null?ls.Played:0}}},
          {"swlens_rta",new Dictionary<string,object>{{"pick_rate",sw!=null?(object)(F(G(sw,"play_rate"))/100.0):null},{"win_rate",sw!=null?(object)(F(G(sw,"win_rate"))/100.0):null},{"ban_rate",sw!=null?(object)(F(G(sw,"ban_rate"))/100.0):null}}},
          {"sets",combos},{"sets_text",string.Join(", ",combos.Take(5).Select(c=>c["name"]+" "+c["pct"]+"%"))},
          {"subs",subList},{"focus",subList.Take(4).Select(s=>S(s["name"])).Where(n=>n.Length>0).ToArray()},
          {"slot_mains",SlotMains(pRows)},{"art_elem_main",JoinPct(ap1,3,false)},{"art_elem_subs",JoinPct(as1,4,true)},
          {"art_type_main",JoinPct(ap2,3,false)},{"art_type_subs",JoinPct(as2,4,true)},
          {"synergy",synList},{"synergy_text",string.Join(", ",synList.Take(5).Select(s=>S(s["name"])+" ("+s["count"]+")"))}
        });
      }
      var root=new Dictionary<string,object>{{"source",new Dictionary<string,object>{{"lucksack",lsOk?"api.swarena.gg":"season38.json"},{"swlens","supabase live"}}},{"season",localSeason},{"created",DateTime.UtcNow.ToString("o")},{"swlens_season_id",seasonId},{"monsters",pack}};
      File.WriteAllText(metaPath,Js.Serialize(root),Encoding.UTF8);
      RtaBuildOptimizer.RtaMetaBuilds.Reload(catalogPath);
      return (lsOk?"LuckSack live • ":"LuckSack hors ligne, snapshot local • ")+"SWLens saison "+seasonId+" • top "+pack.Count+" • "+withSets+" avec sets";
    }
  }
}
