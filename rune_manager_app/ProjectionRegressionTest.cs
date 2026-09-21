using System;
using System.Collections.Generic;
using RuneManagerModern;

static class ProjectionRegressionTest {
  static RuneRow Rune(int level,double atk,double cd,double cr,bool fourth,double def){
    var r=new RuneRow{Id=1,Set="Violent",Slot=1,Main="Atk+",MainValue=160,Grade=4,Level=level};
    r.Subs.Add(new SubStat{Stat="Atk+",Value=atk});
    r.Subs.Add(new SubStat{Stat="CtD%",Value=cd});
    r.Subs.Add(new SubStat{Stat="CtR%",Value=cr});
    if(fourth)r.Subs.Add(new SubStat{Stat="Def%",Value=def});
    return r;
  }
  static double Score(RuneRow r){var rows=new List<RuneRow>{r};RuneEngine.Calculate(rows);return r.Potential;}
  public static int Main(){
    double p0=Score(Rune(0,17,12,4,false,0));
    double p3max=Score(Rune(3,17,12,10,false,0));
    double p3low=Score(Rune(3,17,12,8,false,0));
    double p9=Score(Rune(9,17,12,16,false,0));
    double p12max=Score(Rune(12,17,12,16,true,8));
    double p12low=Score(Rune(12,17,12,16,true,5));
    Console.WriteLine("P0="+p0+" P3MAX="+p3max+" P3LOW="+p3low+" P9="+p9+" P12MAX="+p12max+" P12LOW="+p12low);
    bool ok=p3max<=p0+.001&&p3low<=p0+.001&&p12max<=p9+.001&&p12low<=p9+.001;
    Console.WriteLine(ok?"MONOTONIC_OK":"MONOTONIC_FAIL");return ok?0:1;
  }
}
