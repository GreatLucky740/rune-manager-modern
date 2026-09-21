using System;
using System.IO;
using System.Reflection;
using System.Web.Script.Serialization;
class WorldBossStableBridgeTest {
  static int Main(string[] args){var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};string expected=null;foreach(string file in new[]{"outputs/WorldBossStableV8.dll","outputs/Rune_Manager_Principal_Update.exe"}){var asm=Assembly.LoadFile(Path.GetFullPath(file));var method=asm.GetType("RuneManagerModern.WorldBossOptimizer").GetMethod("Analyze");object previous=js.Deserialize(File.ReadAllText(args[2]),method.ReturnType);var result=method.Invoke(null,new object[]{args[0],args[1],null,previous});string actual=js.Serialize(method.ReturnType.GetField("Rows").GetValue(result));if(expected==null)expected=actual;else if(expected!=actual){Console.WriteLine("FAIL V8 rows differ");return 1;}}Console.WriteLine("PASS original v8 and main update: all World Boss row values identical");return 0;}
}
