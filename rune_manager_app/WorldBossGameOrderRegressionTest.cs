using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RuneManagerModern;

static class WorldBossGameOrderRegressionTest {
  static int Main(string[] args){
    if(args.Length<3)return 2;
    var latest=new Queue<string>();
    foreach(string line in File.ReadLines(args[2]))if(line.IndexOf("\"command\":\"BattleWorldBossStart_v2\"",StringComparison.OrdinalIgnoreCase)>=0&&line.IndexOf("\"unit_id_list\"",StringComparison.OrdinalIgnoreCase)>=0&&line.IndexOf("\"ret_code\"",StringComparison.OrdinalIgnoreCase)<0){latest.Enqueue(line);while(latest.Count>3)latest.Dequeue();}
    var result=WorldBossOptimizer.Analyze(args[0],args[1],latest);
    Console.WriteLine("GAME_ORDER="+result.UsesGameOrder+" ROWS="+result.Rows.Count);
    Console.WriteLine(string.Join(" | ",result.Rows.Take(20).Select(x=>x.Position+":"+x.Monster).ToArray()));
    return result.UsesGameOrder&&result.Rows.Count==60?0:1;
  }
}
