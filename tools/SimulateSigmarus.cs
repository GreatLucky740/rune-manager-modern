using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Web.Script.Serialization;
class SimulateSigmarus {
 static Dictionary<string,object> D(object o){return (Dictionary<string,object>)o;}
 static object Snapshot(object result){var t=result.GetType();var scores=(IDictionary)t.GetField("CurrentEquipmentScores").GetValue(result);var clean=new Dictionary<string,double>();foreach(DictionaryEntry kv in scores)clean[Convert.ToString(kv.Key)]=Convert.ToDouble(kv.Value);var rows=((IEnumerable)t.GetField("Rows").GetValue(result)).Cast<object>().Select(r=>new{id=r.GetType().GetProperty("UnitId").GetValue(r,null),master=r.GetType().GetProperty("MasterId").GetValue(r,null),score=r.GetType().GetProperty("OptimizedScore").GetValue(r,null)}).ToArray();return new{scores=clean,rows=rows};}
 static int Main(string[] args){try{
  var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};
  var fixture=D(js.DeserializeObject(File.ReadAllText("tools/sigmarus_virtual_runes.json")));long id=Convert.ToInt64(fixture["unit_id"]);
  string export=File.ReadAllText(args[0]);var asm=Assembly.LoadFrom(Path.GetFullPath("outputs/Rune_Manager_Modern_Core_Test.exe"));var method=asm.GetType("RuneManagerModern.WorldBossOptimizer").GetMethod("Analyze");
  var reports=new List<object>();var featureReports=new List<object>();int index=0;
  bool capture=args.Length>2&&args[2]=="--features";
  string[] weightNames={"HpStatWeight","AttackStatWeight","DefenseStatWeight","SpeedStatWeight","CritRateStatWeight","CritDamageStatWeight","ResistanceStatWeight","AccuracyStatWeight"};
  var engine=asm.GetType("RuneManagerModern.WorldBossOptimizer");var fields=weightNames.Select(n=>engine.GetField(n,BindingFlags.NonPublic|BindingFlags.Static)).ToArray();
  foreach(var item in (object[])fixture["scenarios"]){var scenario=D(item);var root=D(js.DeserializeObject(export));var unit=((object[])root["unit_list"]).Select(D).Single(u=>Convert.ToInt64(u["unit_id"])==id);
   foreach(string key in new[]{"runes","artifacts"}){object current;var inventory=root.TryGetValue(key,out current)?((object[])current).ToList():new List<object>();object equipped;if(unit.TryGetValue(key,out equipped))inventory.AddRange((object[])equipped);root[key]=inventory.ToArray();unit[key]=new object[0];}unit["relics"]=new object[0];
   if(Convert.ToInt32(scenario["slot"])>0){var rune=new Dictionary<string,object>{{"rune_id",9000000000000L+index},{"slot_no",scenario["slot"]},{"set_id",scenario["set"]},{"class",scenario["stars"]},{"rank",1},{"upgrade_curr",scenario["level"]},{"pri_eff",new object[]{scenario["stat"],scenario["value"]}},{"prefix_eff",new[]{0,0}},{"sec_eff",scenario["subs"]}};unit["runes"]=new object[]{rune};}
   string temporary=Path.GetTempFileName();object result;
   try{File.WriteAllText(temporary,js.Serialize(root));result=method.Invoke(null,new object[]{temporary,args[1],null,null});
    if(capture){var samples=new List<object>();samples.Add(Snapshot(result));foreach(var field in fields){double old=Convert.ToDouble(field.GetValue(null));try{field.SetValue(null,old+1);samples.Add(Snapshot(method.Invoke(null,new object[]{temporary,args[1],null,null})));}finally{field.SetValue(null,old);}}featureReports.Add(new{name=scenario["name"],target=scenario["game_rank"],samples=samples});}
   }finally{File.Delete(temporary);}
   var ranks=(IDictionary)method.ReturnType.GetField("CurrentEquipmentRanks").GetValue(result);var scores=(IDictionary)method.ReturnType.GetField("CurrentEquipmentScores").GetValue(result);
   reports.Add(new{name=scenario["name"],simulated_rank=ranks[id],simulated_score=scores[id],historical_game_rank=scenario["game_rank"]});Console.WriteLine(scenario["name"]+": simulation="+ranks[id]+" jeu historique="+scenario["game_rank"]+" score="+scores[id]);index++;
  }
  File.WriteAllText("outputs/sigmarus-virtual-results.json",js.Serialize(new{warning="Inventaire actuel, pas un instantane historique. Aucun inventaire reel modifie.",results=reports}));
  if(capture)File.WriteAllText("outputs/worldboss-calibration-features.json",js.Serialize(new{names=weightNames,weights=fields.Select(f=>Convert.ToDouble(f.GetValue(null))).ToArray(),scenarios=featureReports}));return 0;
 }catch(Exception e){Console.WriteLine(e);return 1;}}
}
