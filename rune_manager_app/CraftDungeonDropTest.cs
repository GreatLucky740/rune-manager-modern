using System;
using System.Collections.Generic;
using System.Linq;
using RuneManagerModern;

static class CraftDungeonDropTest {
  static int Main(){
    RuneEngine.Stocks.Clear();
    var rows=new List<RuneRow>();

    string riftGem="{\"command\":\"BattleRiftDungeonResult\",\"ret_code\":0,\"item_list\":[{\"type\":27,\"id\":null,\"quantity\":1,\"is_boxing\":1,\"info\":{\"craft_item_id\":1072922515,\"wizard_id\":1039662,\"craft_type\":1,\"craft_type_id\":230504,\"sell_value\":30000,\"amount\":9,\"update_ts\":1789717938}}],\"changed_item_list\":[]}";
    string riftGrind="{\"command\":\"BattleRiftDungeonResult\",\"ret_code\":0,\"item_list\":[{\"type\":27,\"id\":null,\"quantity\":1,\"is_boxing\":1,\"info\":{\"craft_item_id\":1615000001,\"wizard_id\":1039662,\"craft_type\":2,\"craft_type_id\":220805,\"sell_value\":35000,\"amount\":1,\"update_ts\":1789717938}}],\"changed_item_list\":[]}";
    string wb="{\"command\":\"BattleWorldBossResult_v2\",\"ret_code\":0,\"changed_item_list\":[{\"type\":27,\"info\":{\"craft_item_id\":1595216895,\"wizard_id\":1039662,\"craft_type\":2,\"craft_type_id\":220805,\"sell_value\":35000,\"amount\":1}}]}";
    string crate="{\"command\":\"getGuildBossReceiveRewardCrate\",\"ret_code\":0,\"changed_item\":{\"type\":27,\"info\":{\"craft_item_id\":1596146513,\"wizard_id\":1039662,\"craft_type\":2,\"craft_type_id\":190404,\"sell_value\":25000,\"amount\":1}}}";
    string mazePreview="{\"command\":\"battleGuildMazeResult\",\"ret_code\":0,\"selectable_item_list\":[{\"item_master_type\":27,\"runecraft_type\":2,\"runecraft_item_id\":110203,\"item_quantity\":1}]}";
    string mazeImm="{\"command\":\"battleGuildMazeResult\",\"ret_code\":0,\"selectable_item_list\":[{\"item_master_type\":27,\"runecraft_type\":4,\"runecraft_item_id\":990805,\"item_quantity\":1},{\"item_master_type\":27,\"runecraft_type\":4,\"runecraft_item_id\":990805,\"item_quantity\":1},{\"item_master_type\":27,\"runecraft_type\":4,\"runecraft_item_id\":990805,\"item_quantity\":1}],\"reward\":{\"crate\":{\"changestones\":[{\"craft_item_id\":1616621949,\"wizard_id\":1039662,\"craft_type\":4,\"craft_type_id\":990805,\"sell_value\":35000,\"amount\":1}]}}}";
    string catalog="{\"command\":\"GetGuildDataAll\",\"ret_code\":0,\"changed_item\":{\"type\":27,\"info\":{\"craft_item_id\":1071526347,\"wizard_id\":1039662,\"craft_type\":1,\"craft_type_id\":170204,\"sell_value\":30000,\"amount\":6}}}";
    string login="{\"command\":\"HubUserLogin\",\"ret_code\":0,\"rune_craft_item_list\":[{\"craft_item_id\":1602610525,\"wizard_id\":1039662,\"craft_type\":2,\"craft_type_id\":30204,\"sell_value\":25000,\"amount\":1}]}";

    string msg=RuneEngine.ApplyLiveEvent(rows,riftGem);
    RuneEngine.ApplyLiveEvent(rows,riftGrind);
    RuneEngine.ApplyLiveEvent(rows,wb);
    RuneEngine.ApplyLiveEvent(rows,crate);
    RuneEngine.ApplyLiveEvent(rows,mazePreview);
    RuneEngine.ApplyLiveEvent(rows,mazeImm);
    RuneEngine.ApplyLiveEvent(rows,catalog);
    bool dropsOk=RuneEngine.Stocks.Any(x=>x.Id==1072922515&&x.Type=="Gemme"&&x.Amount==9)
      && RuneEngine.Stocks.Any(x=>x.Id==1615000001&&x.Type=="Meule")
      && RuneEngine.Stocks.Any(x=>x.Id==1595216895)
      && RuneEngine.Stocks.Any(x=>x.Id==1596146513)
      && RuneEngine.Stocks.Any(x=>x.Id==1616621949&&x.Set=="Immemorial"&&x.Stat=="Spd"&&x.Type=="Meule"&&x.Grade==5&&x.Amount==1)
      && RuneEngine.Stocks.Count(x=>x.Id==1616621949)==1
      && RuneEngine.Stocks.All(x=>x.Id!=111&&x.Id!=1071526347);
    RuneEngine.ApplyLiveEvent(rows,login);

    var loginGrind=RuneEngine.Stocks.FirstOrDefault(x=>x.Id==1602610525);
    Console.WriteLine("MSG="+msg);
    Console.WriteLine("DROPS_OK="+dropsOk+" COUNT_AFTER_DROPS_THEN_LOGIN="+RuneEngine.Stocks.Count);
    Console.WriteLine("LOGIN="+(loginGrind==null?"null":loginGrind.Type+" "+loginGrind.Set+" "+loginGrind.Stat+" x"+loginGrind.Amount));

    bool ok=dropsOk&&loginGrind!=null&&loginGrind.Type=="Meule"&&loginGrind.Set=="Swift"&&loginGrind.Stat=="HP%"&&!loginGrind.Ancient&&loginGrind.Amount==1
      && RuneEngine.Stocks.Count==1;
    Console.WriteLine(ok?"PASS":"FAIL");
    return ok?0:1;
  }
}
