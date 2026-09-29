using System;
using System.Collections.Generic;
using System.Linq;
using RuneManagerModern;

static class RefinementQualityTest {
  static RuneRow Rune(int grade,int stars,int level,string marker){
    var r=new RuneRow{Id=1,Set="Violent",Slot=1,Main="Atk%",Grade=grade,Stars=stars,Level=level,Marker=marker??"",Potential=9.2};
    r.Subs.Add(new SubStat{Stat="Spd",Value=6});
    r.Subs.Add(new SubStat{Stat="CtR%",Value=5});
    r.Subs.Add(new SubStat{Stat="CtD%",Value=7});
    r.Subs.Add(new SubStat{Stat="Atk%",Value=8});
    return r;
  }
  static int Main(){
    var legend=Rune(5,6,12,"");
    var hero=Rune(4,6,12,"");
    var plusZero=Rune(5,6,0,"");
    var fiveStar=Rune(5,5,12,"");
    var lockedHero=Rune(4,6,12,"Reeval");
    var lockedLegend=Rune(5,6,12,"Reeval");
    bool ok=RuneEngine.IsLegendSixStarPlus12(legend)
      &&!RuneEngine.IsLegendSixStarPlus12(hero)
      &&!RuneEngine.IsLegendSixStarPlus12(plusZero)
      &&!RuneEngine.IsLegendSixStarPlus12(fiveStar)
      &&!RuneEngine.IsLegendSixStarPlus12(lockedHero)
      &&RuneEngine.IsLegendSixStarPlus12(lockedLegend)
      &&RuneEngine.CanRefine(legend)
      &&!RuneEngine.CanRefine(hero);
    Console.WriteLine(ok?"REFINE_QUALITY=OK":"REFINE_QUALITY=FAIL");
    return ok?0:1;
  }
}
