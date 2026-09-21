using System;
using System.Linq;
using RuneManagerModern;

static class CraftLiveRegressionTest {
  static int Main(string[] args){
    if(args.Length<2)return 2;var rows=RuneEngine.Import(args[0]);string amplify="",convert="";
    foreach(string line in System.IO.File.ReadAllLines(args[1])){if(line.IndexOf("\"command\":\"AmplifyRune_v2\"",StringComparison.Ordinal)>=0&&line.IndexOf("\"ret_code\":0",StringComparison.Ordinal)>=0)amplify=line;if(line.IndexOf("\"command\":\"ConvertRune_v2\"",StringComparison.Ordinal)>=0&&line.IndexOf("\"ret_code\":0",StringComparison.Ordinal)>=0)convert=line;}
    double oldGrind=rows.First(x=>x.Id==64423858914).Potential,oldGem=rows.First(x=>x.Id==63684913329).Potential;string a=RuneEngine.ApplyLiveEvent(rows,amplify),c=RuneEngine.ApplyLiveEvent(rows,convert);RuneEngine.Calculate(rows);var grind=rows.FirstOrDefault(x=>x.Id==64423858914);var gem=rows.FirstOrDefault(x=>x.Id==63684913329);RuneEngine.PreserveAfterCraft(grind,oldGrind);RuneEngine.PreserveAfterCraft(gem,oldGem);var gemStock=RuneEngine.Stocks.FirstOrDefault(x=>x.Id==1590402543);var grindStock=RuneEngine.Stocks.FirstOrDefault(x=>x.Id==1536400248);
    Console.WriteLine(a);Console.WriteLine(c);Console.WriteLine("GRIND="+(grind==null?0:grind.Subs[0].Grind)+" GRIND_STOCK="+(grindStock==null?-1:grindStock.Amount)+" GEMMED="+(gem!=null&&gem.Subs.Count>2&&gem.Subs[2].Gemmed)+" GEM_STAT="+(gem==null||gem.Subs.Count<3?"":gem.Subs[2].Stat)+" GEM_STOCK="+(gemStock==null?-1:gemStock.Amount)+" POTENTIAL_SAME="+(grind!=null&&gem!=null&&grind.Potential==oldGrind&&gem.Potential==oldGem));return grind!=null&&grind.Subs[0].Grind==7&&grindStock!=null&&grindStock.Amount==3&&gem!=null&&gem.Subs[2].Gemmed&&gem.Subs[2].Stat=="Acc%"&&gemStock!=null&&gemStock.Amount==0&&grind.Potential==oldGrind&&gem.Potential==oldGem?0:1;
  }
}
