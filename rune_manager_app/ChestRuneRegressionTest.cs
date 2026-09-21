using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RuneManagerModern;

static class ChestRuneRegressionTest {
  static int Main(string[] args){
    if(args.Length<2)return 2;
    var rows=RuneEngine.Import(args[0]);string payload="";
    foreach(string line in File.ReadLines(args[1]))if(line.IndexOf("\"command\":\"getGuildBossReceiveRewardCrate\"",StringComparison.OrdinalIgnoreCase)>=0&&line.IndexOf("\"ret_code\":0",StringComparison.OrdinalIgnoreCase)>=0&&line.IndexOf("\"rune_id\"",StringComparison.OrdinalIgnoreCase)>=0)payload=line;
    var ids=Regex.Matches(payload,"\\\"rune_id\\\":(\\d+)").Cast<Match>().Select(x=>long.Parse(x.Groups[1].Value)).Distinct().ToArray();rows.RemoveAll(x=>ids.Contains(x.Id));int before=rows.Count;string message=RuneEngine.ApplyLiveEvent(rows,payload);int added=rows.Count-before;
    Console.WriteLine("CHEST_RUNES="+ids.Length+" ADDED="+added+" MESSAGE="+message);return ids.Length>0&&added>0?0:1;
  }
}
