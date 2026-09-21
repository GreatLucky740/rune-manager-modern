using System;
using System.Collections.Generic;
using System.Linq;
using RuneManagerModern;

static class CraftConsumeAllTypesTest {
  static int Fail(string msg){Console.WriteLine("FAIL "+msg);return 1;}
  static string Event(string command,int runeId,int setId,int ct,int typeId,long craftId,int amount){
    return "{\"command\":\""+command+"\",\"ret_code\":0,\"rune\":{\"rune_id\":"+runeId+",\"slot_no\":2,\"set_id\":"+setId+",\"rank\":5,\"class\":6,\"upgrade_curr\":12,\"pri_eff\":[4,63],\"sec_eff\":[[8,7,1,0],[2,10,0,0]]},\"rune_craft_item\":{\"craft_item_id\":"+craftId+",\"craft_type\":"+ct+",\"craft_type_id\":"+typeId+",\"amount\":"+amount+"}}";
  }
  static int Sum(string type,string set,string stat,int grade,bool ancient){
    return RuneEngine.Stocks.Where(x=>x.Type==type&&x.Set==set&&x.Stat==stat&&x.Grade==grade&&x.Ancient==ancient).Sum(x=>x.Amount);
  }
  static int Main(){
    var cases=new[]{
      new {Cmd="ConvertRune_v2", Ct=1, TypeId=10204, Set="Energy", Stat="HP%", Grade=4, Ancient=false, Kind="Gemme", SetId=1},
      new {Cmd="AmplifyRune_v2", Ct=2, TypeId=10204, Set="Energy", Stat="HP%", Grade=4, Ancient=false, Kind="Meule", SetId=1},
      new {Cmd="ConvertRune_v2", Ct=3, TypeId=990805, Set="Immemorial", Stat="Spd", Grade=5, Ancient=false, Kind="Gemme", SetId=1},
      new {Cmd="AmplifyRune_v2", Ct=4, TypeId=990805, Set="Immemorial", Stat="Spd", Grade=5, Ancient=false, Kind="Meule", SetId=1},
      new {Cmd="ConvertRune_v2", Ct=5, TypeId=110804, Set="Vampire", Stat="Spd", Grade=4, Ancient=true, Kind="Gemme", SetId=11},
      new {Cmd="AmplifyRune_v2", Ct=6, TypeId=110804, Set="Vampire", Stat="Spd", Grade=4, Ancient=true, Kind="Meule", SetId=11}
    };
    int runeId=100;
    foreach(var c in cases){
      RuneEngine.Stocks.Clear();
      RuneEngine.Stocks.Add(new CraftStock{Id=0,Ancient=c.Ancient,Type=c.Kind,Set=c.Set,Stat=c.Stat,Grade=c.Grade,Amount=1});
      var rows=new List<RuneRow>();
      long usedId=9000000000L+c.Ct;
      string msg=RuneEngine.ApplyLiveEvent(rows,Event(c.Cmd,++runeId,c.SetId,c.Ct,c.TypeId,usedId,0));
      if(string.IsNullOrEmpty(msg))return Fail(c.Kind+" ct"+c.Ct+" empty event");
      int left=Sum(c.Kind,c.Set,c.Stat,c.Grade,c.Ancient);
      bool ghost=RuneEngine.Stocks.Any(x=>x.Id==usedId&&x.Amount>0);
      if(left!=0||ghost)return Fail(c.Kind+" ct"+c.Ct+" "+c.Set+" left="+left+" ghost="+ghost+" stocks="+string.Join("|",RuneEngine.Stocks.Select(x=>x.Id+":"+x.Amount)));
    }
    RuneEngine.Stocks.Clear();
    RuneEngine.Stocks.Add(new CraftStock{Id=1617699546,Ancient=false,Type="Gemme",Set="Vampire",Stat="Spd",Grade=4,Amount=1});
    var matched=new List<RuneRow>();
    RuneEngine.ApplyLiveEvent(matched,Event("ConvertRune_v2",200,11,1,110804,1617699546,0));
    var hit=RuneEngine.Stocks.FirstOrDefault(x=>x.Id==1617699546);
    if(hit==null||hit.Amount!=0)return Fail("id-matched remaining not applied");
    Console.WriteLine("PASS");
    return 0;
  }
}
