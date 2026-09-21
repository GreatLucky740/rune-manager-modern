using System;
using System.Linq;
using RuneManagerModern;

static class GroupUpgradeRegressionTest {
  static int Main(string[] args){
    if(args.Length<2)return 2;var rows=RuneEngine.Import(args[0]);string payload="";foreach(string line in System.IO.File.ReadAllLines(args[1]))if(line.IndexOf("\"command\":\"UpgradeRuneList\"",StringComparison.Ordinal)>=0&&line.IndexOf("\"ret_code\":0",StringComparison.Ordinal)>=0)payload=line;var before=rows.ToDictionary(x=>x.Id,x=>x.Level);string message=RuneEngine.ApplyLiveEvent(rows,payload);var ids=new long[]{64431535924,64431535908,64431535906,64431535905,64431535903,64431535895,64431535894,64431535889,64431535885};int updated=ids.Count(id=>rows.Any(x=>x.Id==id&&x.Level==9&&(!before.ContainsKey(id)||before[id]<9)));Console.WriteLine(message);Console.WriteLine("UPDATED="+updated+" LEVEL9="+ids.Count(id=>rows.Any(x=>x.Id==id&&x.Level==9)));return message.StartsWith("9 runes")&&ids.All(id=>rows.Any(x=>x.Id==id&&x.Level==9))?0:1;
  }
}
