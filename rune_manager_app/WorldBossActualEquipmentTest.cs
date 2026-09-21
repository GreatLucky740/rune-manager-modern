using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Collections;
using System.Web.Script.Serialization;
class WorldBossActualEquipmentTest {
 static void Check(bool ok,string text){if(!ok)throw new Exception(text);Console.WriteLine("PASS "+text);}
 static int Main(string[] args){try{
  var asm=Assembly.LoadFrom(Path.GetFullPath("outputs/Rune_Manager_Modern_Core_Test.exe"));var engine=asm.GetType("RuneManagerModern.WorldBossOptimizer");var flags=BindingFlags.NonPublic|BindingFlags.Static;var runeType=engine.GetNestedType("Rune",BindingFlags.NonPublic);
  var dictType=typeof(System.Collections.Generic.Dictionary<,>).MakeGenericType(typeof(long),runeType);var dict=(IDictionary)Activator.CreateInstance(dictType);
  var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var raw=js.DeserializeObject("{\"rune_id\":1,\"slot_no\":2,\"set_id\":3,\"class\":2,\"rank\":1,\"upgrade_curr\":2,\"pri_eff\":[8,4],\"prefix_eff\":[0,0],\"sec_eff\":[]}");
  engine.GetMethod("AddRune",flags).Invoke(null,new object[]{raw,dict,true});var projected=dict[1L];var actual=runeType.GetField("Actual").GetValue(projected);
  Check((int)runeType.GetField("Level").GetValue(actual)==2,"actual rune stays +2");Check((double)runeType.GetField("MainValue").GetValue(actual)==4,"actual speed stays +4");Check((int)runeType.GetField("Level").GetValue(projected)==15,"potential remains separate at +15");
  var evaluate=engine.GetMethod("WorldBossEvaluationRune",flags);
  Check(object.ReferenceEquals(evaluate.Invoke(null,new[]{projected}),actual),"+2 test rune evaluated without projection");
  runeType.GetField("CurrentLevel").SetValue(projected,5);Check(object.ReferenceEquals(evaluate.Invoke(null,new[]{projected}),actual),"+5 test rune evaluated without projection");
  runeType.GetField("CurrentLevel").SetValue(projected,12);Check(object.ReferenceEquals(evaluate.Invoke(null,new[]{projected}),projected),"+12 rune evaluated with +15 projection");
  runeType.GetField("CurrentLevel").SetValue(projected,15);Check(object.ReferenceEquals(evaluate.Invoke(null,new[]{projected}),projected),"+15 rune stays fully upgraded");
  var method=engine.GetMethod("Analyze");var frozen=js.Deserialize(File.ReadAllText("outputs/worldboss-calibration-locked.json"),method.ReturnType);var result=method.Invoke(null,new object[]{args[0],args[1],null,frozen});
  Func<object,System.Collections.Generic.Dictionary<long,string>> builds=o=>((IEnumerable)method.ReturnType.GetField("Rows").GetValue(o)).Cast<object>().ToDictionary(x=>(long)x.GetType().GetProperty("UnitId").GetValue(x,null),x=>string.Join(",",new[]{"RuneDetails","ArtifactDetails"}.SelectMany(f=>((IEnumerable)x.GetType().GetField(f).GetValue(x)).Cast<object>().Select(v=>Convert.ToString(v.GetType().GetField("Id").GetValue(v))))));
  var before=builds(frozen);var after=builds(result);Check(before.Count==60&&before.Count==after.Count&&before.All(x=>after.ContainsKey(x.Key)&&after[x.Key]==x.Value),"all 60 builds preserved exactly");
  var second=method.Invoke(null,new object[]{args[0],args[1],null,result});Check(builds(second).All(x=>before[x.Key]==x.Value),"second calculation keeps every rune and artifact");
  var ranks=(IDictionary)method.ReturnType.GetField("CurrentEquipmentRanks").GetValue(result);Console.WriteLine("SIGMARUS_ACTUAL_RANK="+ranks[3027870181L]);
  Console.WriteLine("FORMULA="+method.ReturnType.GetField("Formula").GetValue(result));return 0;
 }catch(Exception e){Console.WriteLine(e);return 1;}}
}
