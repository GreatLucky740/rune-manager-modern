using System;
using System.Linq;
using RuneManagerModern;
static class WorldBossRegressionTest {
  static int Main(string[] args){
    if(args.Length<2)return 2;
    var r=WorldBossOptimizer.Analyze(args[0],args[1]);
    bool ordered=r.Rows.Select(x=>r.CandidateRanks.ContainsKey(x.UnitId)?r.CandidateRanks[x.UnitId]:int.MaxValue).Zip(r.Rows.Skip(1).Select(x=>r.CandidateRanks.ContainsKey(x.UnitId)?r.CandidateRanks[x.UnitId]:int.MaxValue),(a,b)=>a<=b).All(x=>x);
    bool ok=r.Rows.Count==60&&ordered&&r.Rows.All(x=>x.RuneDetails.Count==6)&&r.Rows.SelectMany(x=>x.RuneDetails).Select(x=>x.Id).Distinct().Count()==360&&r.Rows.All(x=>x.OptimizedScore>0)&&r.SkillRecommendations.All(x=>x.Gain>=0&&!string.IsNullOrWhiteSpace(x.Monster)&&!string.IsNullOrWhiteSpace(x.Element)&&!string.IsNullOrWhiteSpace(x.Reason)&&!string.IsNullOrWhiteSpace(x.Copy))&&r.Rows.All(x=>!x.Monster.StartsWith("Monstre "))&&r.Rows.SelectMany(x=>x.ArtifactDetails).All(x=>!string.IsNullOrWhiteSpace(x.IconKey)&&!string.IsNullOrWhiteSpace(x.Restriction));
    Console.WriteLine("ROWS="+r.Rows.Count+" UNRESOLVED="+r.Rows.Count(x=>x.Monster.StartsWith("Monstre ")));
    Console.WriteLine("RUNES="+r.Rows.SelectMany(x=>x.RuneDetails).Select(x=>x.Id).Distinct().Count()+" ORDERED="+ordered+" RECS="+r.SkillRecommendations.Count+" BADRECS="+r.SkillRecommendations.Count(x=>x.Gain<0)+" BADART="+r.Rows.SelectMany(x=>x.ArtifactDetails).Count(x=>string.IsNullOrWhiteSpace(x.IconKey)||string.IsNullOrWhiteSpace(x.Restriction)));
    foreach(var x in r.Rows.Where(x=>x.MasterId==29311))Console.WriteLine("NAMECHECK="+x.Monster+" "+x.Element);
    foreach(var x in r.Rows.Where(x=>x.Monster.StartsWith("Monstre ")))Console.WriteLine("UNRESOLVED_NAME="+x.MasterId+" "+x.Monster+" "+x.Element);
    Console.WriteLine(ok?"WORLD_BOSS=OK":"WORLD_BOSS=FAIL");return ok?0:1;
  }
}
