using System;
using System.Collections.Generic;
using System.Linq;
using RuneManagerModern;

static class RetentionDynamicQuotaTest {
  static RuneRow Rune(long id,double potential){
    return new RuneRow{Id=id,Level=12,Grade=5,Set="Violent",Slot=1,Main="Atk%",Potential=potential,Obtained=new DateTime(2026,1,1).AddSeconds(id)};
  }
  static int Main(){
    var rows=new List<RuneRow>();
    for(int i=1;i<=200;i++)rows.Add(Rune(i,201-i));
    RuneEngine.SeuilVenteFixe=false;
    RuneEngine.LimiteRunesConservees=50;
    RuneEngine.ApplyRetentionRules(rows);
    double t50=RuneEngine.SeuilApres12;
    int keep50=rows.Count(r=>r.Action=="Keep");
    bool rose=t50>RuneEngine.SeuilQualiteMinimum+0.001;
    bool cut=Math.Abs(t50-151)<.001;
    RuneEngine.LimiteRunesConservees=10000;
    RuneEngine.ApplyRetentionRules(rows);
    bool floor=Math.Abs(RuneEngine.SeuilApres12-RuneEngine.SeuilQualiteMinimum)<.001;
    bool preview=Math.Abs(RuneEngine.RetentionThreshold(rows,50)-151)<.001;
    bool ok=rose&&cut&&floor&&preview&&keep50>=50;
    Console.WriteLine("t50="+t50+" keep50="+keep50+" t10k="+RuneEngine.SeuilApres12+" rose="+rose+" cut="+cut+" floor="+floor+" preview="+preview);
    Console.WriteLine(ok?"DYNAMIC_QUOTA=OK":"DYNAMIC_QUOTA=FAIL");
    return ok?0:1;
  }
}
