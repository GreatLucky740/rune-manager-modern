using System;
using System.Collections.Generic;
using System.Linq;
using RuneManagerModern;

static class RetentionLevelRegressionTest {
  static RuneRow Rune(long id,int level,double potential){return new RuneRow{Id=id,Level=level,Grade=5,Set="Violent",Slot=1,Main="Atk+",Potential=potential,Obtained=new DateTime(2026,1,1).AddSeconds(id)};}
  static int Main(){
    var rows=new List<RuneRow>();
    for(int i=1;i<=2001;i++)rows.Add(Rune(i,12,10000-i));
    var unfinished=Rune(9000,9,99999);rows.Add(unfinished);
    RuneEngine.ApplyRetentionRules(rows);
    bool completedCount=rows.Count(x=>x.Level>=12&&x.Action=="Keep")==RuneEngine.LimiteRunesConservees;
    bool weakestCompletedSold=rows.Single(x=>x.Id==2001).Action=="Sell";
    bool unfinishedDidNotReplace=rows.Single(x=>x.Id==2000).Action=="Keep"&&unfinished.Action=="Pwr up";
    bool thresholdFromCompleted=Math.Abs(RuneEngine.SeuilApres12-8000)<.001;
    bool ok=completedCount&&weakestCompletedSold&&unfinishedDidNotReplace&&thresholdFromCompleted;
    Console.WriteLine(ok?"LEVEL12_RETENTION=OK":"LEVEL12_RETENTION=FAIL");return ok?0:1;
  }
}
