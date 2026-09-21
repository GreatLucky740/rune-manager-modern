using System;
using System.Linq;
using RuneManagerModern;

static class LiveLogRegressionTest {
  static int Main(string[] args){
    if(args.Length<2)return 2;
    var rows=RuneEngine.Import(args[0]);
    var oldRune=rows.FirstOrDefault(x=>x.Id==64431535928);int oldLevel=oldRune==null?-1:oldRune.Level;double oldPotential=oldRune==null?0:oldRune.Potential;
    string[] lines=System.IO.File.ReadAllLines(args[1]);string payload="";
    for(int i=0;i<lines.Length;i++)if(lines[i].IndexOf("\"command\":\"UpgradeRune_v2\"",StringComparison.Ordinal)>=0&&lines[i].IndexOf("\"ret_code\":0",StringComparison.Ordinal)>=0)payload=lines[i];
    string message=RuneEngine.ApplyLiveEvent(rows,payload);
    RuneEngine.Calculate(rows);
    var calculated=rows.FirstOrDefault(x=>x.Id==64431535928);double rawPotential=calculated==null?0:calculated.Potential;if(calculated!=null&&calculated.Level>oldLevel&&oldLevel<12)RuneEngine.CapAfterUpgrade(calculated,oldPotential);
    Console.WriteLine(message);
    var rune=rows.FirstOrDefault(x=>x.Id==64431535928);Console.WriteLine("COUNT="+rows.Count+" MATCH="+rows.Count(x=>x.Id==64431535928)+" LEVEL="+(rune==null?-1:rune.Level)+" SUBS="+(rune==null?0:rune.Subs.Count)+" LAST="+(rune==null||rune.Subs.Count==0?"":rune.Subs.Last().Display)+" OLD="+oldPotential.ToString("0.000")+" RAW="+rawPotential.ToString("0.000")+" CAPPED="+(rune==null?0:rune.Potential).ToString("0.000"));
    return message.Length>0?0:1;
  }
}
