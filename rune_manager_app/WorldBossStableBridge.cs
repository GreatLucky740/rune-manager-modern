#if false
// Round 7 (2026-09-15) : pont desactive sur demande de Jeremy. L'appli Stable/Principale
// utilisait ce pont pour charger WorldBossStableV8.dll (ancien moteur fige) au lieu du
// vrai moteur moderne de WorldBossOptimizer.cs — resultat : aucun changement de formule/
// poids ne se voyait jamais sur l'appli principale. Le gate "#if !WORLD_BOSS_STABLE" a
// ete retire de WorldBossOptimizer.cs, donc la vraie classe compile maintenant dans les
// deux modes et ce fichier n'est plus necessaire (garde pour reference, jamais compile).
using System;
using System.IO;
using System.Reflection;
using System.Collections.Generic;
using System.Web.Script.Serialization;
namespace RuneManagerModern {
  // Main installation deliberately runs the original v8 engine byte-for-byte.
  // The experimental engine is compiled only for the separate Sigmarus build.
  public static class WorldBossOptimizer {
    // Stubs : outils de vérification (mode équip. réel forcé, ordre réel collé) qui
    // n'existent que dans le moteur v9 moderne. Sans eux ici, RuneManagerApp.cs ne
    // compile pas en Stable (il les référence sans #if). Inertes en Stable : le
    // vieux moteur v8 (WorldBossStableV8.dll) ne les lit pas.
    public static bool ForceCurrentEquipment=false;
    public static List<string> RealOrderNames=null;
    static readonly Lazy<Type> Engine=new Lazy<Type>(()=>Assembly.Load(File.ReadAllBytes(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"WorldBossStableV8.dll"))).GetType("RuneManagerModern.WorldBossOptimizer",true));
    public static WorldBossResult Analyze(string path,string catalog,IEnumerable<string> events=null,WorldBossResult previous=null){
      var method=Engine.Value.GetMethod("Analyze",BindingFlags.Static|BindingFlags.Public);var oldType=method.ReturnType;var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};object oldPrevious=null;
      if(previous!=null){oldPrevious=Activator.CreateInstance(oldType);foreach(var field in oldType.GetFields(BindingFlags.Instance|BindingFlags.Public)){var source=typeof(WorldBossResult).GetField(field.Name);if(source==null||field.IsInitOnly)continue;object value=source.GetValue(previous);field.SetValue(oldPrevious,field.FieldType==source.FieldType?value:js.Deserialize(js.Serialize(value),field.FieldType));}}
      object result;try{result=method.Invoke(null,new object[]{path,catalog,events,oldPrevious});}catch(TargetInvocationException ex){throw ex.InnerException??ex;}
      var converted=new WorldBossResult();foreach(var source in oldType.GetFields(BindingFlags.Instance|BindingFlags.Public)){var target=typeof(WorldBossResult).GetField(source.Name);if(target==null||target.IsInitOnly)continue;object value=source.GetValue(result);target.SetValue(converted,target.FieldType==source.FieldType?value:js.Deserialize(js.Serialize(value),target.FieldType));}return converted;
    }
  }
}
#endif
