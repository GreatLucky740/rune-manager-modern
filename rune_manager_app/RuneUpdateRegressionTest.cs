using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using RuneManagerModern;
static class RuneUpdateRegressionTest {
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
  [STAThread] static int Main(string[] args){try{
    var counts=new[]{10,5,10,7,11,6};Check(Math.Abs(RuneEngine.ScarcityBonus(counts,2)-1)<.00001,"least populated slot +1");Check(RuneEngine.ScarcityBonus(counts,5)==0,"most populated slot no penalty");Check(RuneEngine.ScarcityBonus(new[]{5,5,5,5,5,5},1)==0,"equal slots zero bonus");Check(Math.Abs(RuneEngine.ScarcityBonus(new[]{0,0,0,0,0,0},3)-1)<.00001,"empty stock aims for 1 per slot");Check(RuneEngine.ScarcityBonus(new[]{1,1,1,1,1,1},2)==0,"one rune per slot is filled");RuneEngine.PresetSlotCounts.Clear();Check(Math.Abs(RuneEngine.InventoryBonus(RuneEngine.Presets[0],"Blade",1)-1)<.00001,"missing preset-set stock still boosts");
    Check(RtaPickScore.CounterWeight(0)==0,"rta no counter before enemy pick");
    Check(RtaPickScore.CounterWeight(5)>RtaPickScore.CounterWeight(1),"rta counter grows pick by pick");
    Check(RtaPickScore.SynergyWeight(5)<RtaPickScore.SynergyWeight(0),"rta synergy yields to enemy later");
    Check(RtaPickScore.TeamFitWeight(5)<RtaPickScore.TeamFitWeight(0),"rta late picks less core lock");
    Check(RtaPickScore.MatchupPoints(.62,200,.52,.25,.10,.20)>0&&RtaPickScore.MatchupPoints(.45,200,.52,.25,.10,.20)<0,"rta winning matchup scores positive");
    Check(RtaPickScore.MetaPoints(.25,.10,20000)>RtaPickScore.MetaPoints(.04,.01,800),"rta meta pick rate beats low-use");
    Check(RtaPickScore.LeaderPoints(.40,true)>RtaPickScore.LeaderPoints(.05,true),"rta needed leader scores more");
    Check(RtaPickScore.OpeningScore(.52,.22,.12,.38,40000)>RtaPickScore.OpeningScore(.56,.03,.01,.02,500),"rta first pick prefers used leader over niche wr");
    var peekList=new List<RtaMonster>{new RtaMonster{Id=1,Name="Eleni"},new RtaMonster{Id=2,Name="Leo"},new RtaMonster{Id=3,Name="Angelmon"},new RtaMonster{Id=4,Name="King Angelmon"},new RtaMonster{Id=5,Name="Giana"}};
    string eleniCard="Normal Battle\nSelect your Monsters.\nOK\nEleni\nAttack\nMax Lv. 40\nHP 9720\nATK 867\nDEF 626\nSPD 100\nLEADER";
    Check(RtaPeekName.LooksLikeMonsterCard(eleniCard),"held monster card is detected from Max Lv HP SPD");
    Check(RtaPeekName.Match(eleniCard,peekList)!=null&&RtaPeekName.Match(eleniCard,peekList).Name=="Eleni","held card name Eleni maps to enemy pick");
    Check(RtaPeekName.Match("Eleni\nGiana\nLeo",peekList)==null,"names without the hold card are ignored");
    Check(RtaPeekName.Match("King Angelmon\nAttack\nMax Lv. 40\nHP 1\nSPD 100",peekList).Name=="King Angelmon","longer name wins over Angelmon");
    Check(RtaPeekName.Match("Elenl\nAttack\nMax Lv. 40\nHP 1\nSPD 100",peekList).Name=="Eleni","one-letter OCR typo still maps Eleni");
    Check(RtaPeekName.Match("Great-Lucky Attack Max Lv. ATK DEF SPD Select your Monsters. Eleni ASSASSINS 40 9720 867 626 100",peekList).Name=="Eleni","screenshot OCR without HP still maps Eleni");
    peekList.Add(new RtaMonster{Id=6,Name="Kumar"});
    Check(RtaPeekName.Match("HP\nMax Lv. 40\nHP 13005\nATK 593\nDEF 681\nSPD 101\nKumar",peekList).Name=="Kumar","HP-type hold card maps Kumar");
    var tets=new List<RtaMonster>{
      new RtaMonster{Id=31101,Name="Tetsuya",Element="water"},
      new RtaMonster{Id=31102,Name="Tetsuya",Element="fire"},
      new RtaMonster{Id=31103,Name="Tetsuya",Element="wind"},
      new RtaMonster{Id=31112,Name="Fire Tetsuya",Element="fire",PickRate=.2}
    };
    string tetsCard="Tetsuya\nHP\nMax Lv. 40\nHP 1\nATK 1\nDEF 1\nSPD 100";
    Check(RtaPeekName.Match(tetsCard,tets)==null,"same name without element is not guessed");
    Check(RtaPeekName.Match(tetsCard,tets,"fire")!=null&&RtaPeekName.Match(tetsCard,tets,"fire").Id==31112,"fire gem picks fire Tetsuya not water");
    Check(RtaPeekName.Match(tetsCard,tets,"water").Id==31101,"water gem picks water Tetsuya");
    Check(RtaPeekName.Match("Moore\nAttack\nMax Lv. 40\nHP 1\nSPD 100",new List<RtaMonster>{new RtaMonster{Id=24511,Name="Moore",Element="water"}},"fire").Id==24511,"unique Moore still maps if gem color is misread");
    var verm=new List<RtaMonster>{
      new RtaMonster{Id=32601,Name="Vermilion Bird Dancer",Element="water"},
      new RtaMonster{Id=32602,Name="Vermilion Bird Dancer",Element="fire",PickRate=.3},
      new RtaMonster{Id=32612,Name="Fire Vermilion Bird Dancer",Element="fire",PickRate=.3}
    };
    string vermCard="Vermilion Bird Dancer\nAttack\nMax Lv. 40\nHP 1\nSPD 100";
    Check(RtaPeekName.Match(vermCard,verm,"water")!=null&&RtaPeekName.Match(vermCard,verm,"water").Element=="water","water gem picks water Vermilion not fire meta");
    using(var bmp=new System.Drawing.Bitmap(400,400)){
      using(var g=System.Drawing.Graphics.FromImage(bmp)){
        g.Clear(System.Drawing.Color.FromArgb(90,55,30));
        using(var water=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(20,170,230)))g.FillEllipse(water,176,48,32,32);
        using(var fireArt=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(230,70,20)))g.FillRectangle(fireArt,70,170,260,210);
      }
      Check(RtaPeekName.DetectElement(bmp)=="water","title gem wins over fire-colored monster art");
    }
    using(var bmp=new System.Drawing.Bitmap(400,400)){
      using(var g=System.Drawing.Graphics.FromImage(bmp)){
        g.Clear(System.Drawing.Color.FromArgb(90,55,30));
        using(var water=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(20,170,230)))g.FillEllipse(water,164,52,20,20);
        using(var gold=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(230,190,70)))g.FillRectangle(gold,210,48,140,22);
        using(var fireArt=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(230,70,20)))g.FillRectangle(fireArt,70,170,260,210);
      }
      Check(RtaPeekName.DetectElement(bmp)=="water","gold name text does not vote fire or wind");
    }
    string iconDir=System.IO.Path.Combine(System.IO.Path.GetTempPath(),"rta_peek_icons_"+Guid.NewGuid().ToString("N"));
    System.IO.Directory.CreateDirectory(iconDir);
    try{
      using(var waterI=new System.Drawing.Bitmap(48,48)){
        using(var g=System.Drawing.Graphics.FromImage(waterI))g.Clear(System.Drawing.Color.FromArgb(40,140,230));
        waterI.Save(System.IO.Path.Combine(iconDir,"32601.png"));
      }
      using(var fireI=new System.Drawing.Bitmap(48,48)){
        using(var g=System.Drawing.Graphics.FromImage(fireI))g.Clear(System.Drawing.Color.FromArgb(230,60,20));
        fireI.Save(System.IO.Path.Combine(iconDir,"32602.png"));
      }
      using(var portrait=new System.Drawing.Bitmap(80,80)){
        using(var g=System.Drawing.Graphics.FromImage(portrait))g.Clear(System.Drawing.Color.FromArgb(50,150,220));
        var hit=RtaPeekName.Match(vermCard,verm,"",portrait,iconDir);
        Check(hit!=null&&hit.Id==32601,"portrait matches water Vermilion icon not fire meta");
      }
      using(var portrait=new System.Drawing.Bitmap(80,80)){
        using(var g=System.Drawing.Graphics.FromImage(portrait))g.Clear(System.Drawing.Color.FromArgb(230,60,20));
        var hit=RtaPeekName.Match(vermCard,verm,"water",portrait,iconDir);
        Check(hit!=null&&hit.Element=="water","water gem wins over a fire-looking portrait");
      }
    }finally{try{System.IO.Directory.Delete(iconDir,true);}catch{}}
    string gemDir=@"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets\rta";
    RtaPeekName.LoadElementGems(gemDir);
    using(var waterGem=new System.Drawing.Bitmap(System.IO.Path.Combine(gemDir,"el-water.png")))
    using(var fireGem=new System.Drawing.Bitmap(System.IO.Path.Combine(gemDir,"el-fire.png")))
    using(var bmp=new System.Drawing.Bitmap(420,320)){
      using(var g=System.Drawing.Graphics.FromImage(bmp)){
        g.Clear(System.Drawing.Color.FromArgb(90,55,30));
        g.DrawImage(waterGem,48,40,36,36);
        using(var fireArt=new System.Drawing.SolidBrush(System.Drawing.Color.FromArgb(230,70,20)))g.FillRectangle(fireArt,80,150,260,150);
      }
      Check(RtaPeekName.DetectElement(bmp)=="water","real water gem icon beats fire costume");
    }
    using(var fireGem=new System.Drawing.Bitmap(System.IO.Path.Combine(gemDir,"el-fire.png")))
    using(var bmp=new System.Drawing.Bitmap(420,320)){
      using(var g=System.Drawing.Graphics.FromImage(bmp)){
        g.Clear(System.Drawing.Color.FromArgb(90,55,30));
        g.DrawImage(fireGem,48,40,36,36);
      }
      Check(RtaPeekName.DetectElement(bmp)=="fire","real fire gem icon is fire");
    }
    var ino=new List<RtaMonster>{
      new RtaMonster{Id=32111,Name="Inosuke Hashibira",Element="water"},
      new RtaMonster{Id=32112,Name="Inosuke Hashibira",Element="fire",PickRate=.4},
      new RtaMonster{Id=32113,Name="Inosuke Hashibira",Element="wind"}
    };
    string inoTip="Inosuke Hashibira\nDEMON SLAYER";
    Check(RtaPeekName.Match(inoTip,ino)==null,"name tooltip without gem is ignored");
    Check(RtaPeekName.Match(inoTip,ino,"water")!=null&&RtaPeekName.Match(inoTip,ino,"water").Id==32111,"tooltip plus water gem picks water Inosuke not fire meta");
    string savedLang=Loc.Lang;Loc.Lang="en";Check(Loc.T("no_preset")=="No Good Preset","english no-preset label");Loc.Lang="fr";Check(Loc.T("no_preset")=="Aucun preset","french no-preset label");Loc.Lang=savedLang;
    var scoreM=typeof(RuneEngine).GetMethod("Score",BindingFlags.NonPublic|BindingFlags.Static);
    var mainPreset=RuneEngine.ClonePreset(RuneEngine.Presets[0],"MainAccGate");
    mainPreset.Main[2].Clear();mainPreset.Main[2].Add("Spd");mainPreset.MainAccepted[2].Clear();
    var mainRune=new RuneRow{Set="Violent",Slot=2,Main="Spd",Grade=5,Stars=6,Level=12};
    double prefScore=(double)scoreM.Invoke(null,new object[]{mainRune,mainPreset});
    mainPreset.Main[2].Clear();mainPreset.MainAccepted[2].Add("Spd");
    double accScore=(double)scoreM.Invoke(null,new object[]{mainRune,mainPreset});
    Check(prefScore>0&&accScore>0,"preferred and accepted mains both score");
    Check(Math.Abs(accScore/prefScore-RuneEngine.FacteurMainAcceptable)<.0000001,"acceptable main is 85 percent of preferred");
    mainPreset.MainAccepted[2].Clear();
    Check((double)scoreM.Invoke(null,new object[]{mainRune,mainPreset})==0,"main outside both lists scores 0");
    var keepM=typeof(RuneEngine).GetMethod("PremiumKeep",BindingFlags.NonPublic|BindingFlags.Static);
    var pwrM=typeof(RuneEngine).GetMethod("PowerSpeed",BindingFlags.NonPublic|BindingFlags.Static);
    var slot2Spd=new RuneRow{Set="Violent",Slot=2,Main="Atk%",Grade=5,Stars=6,Level=12};slot2Spd.Subs.Add(new SubStat{Stat="Spd",Value=24});
    var slot1Spd=new RuneRow{Set="Violent",Slot=1,Main="Atk+",Grade=5,Stars=6,Level=12};slot1Spd.Subs.Add(new SubStat{Stat="Spd",Value=24});
    Check(!(bool)keepM.Invoke(null,new object[]{slot2Spd}),"slot 2 spd does not auto-keep");
    Check((bool)keepM.Invoke(null,new object[]{slot1Spd}),"slot 1 spd still auto-keeps");
    var pwr2=new RuneRow{Set="Violent",Slot=2,Main="Atk%",Grade=5,Stars=6,Level=6};pwr2.Subs.Add(new SubStat{Stat="Spd",Value=12});
    var pwr1=new RuneRow{Set="Violent",Slot=1,Main="Atk+",Grade=5,Stars=6,Level=6};pwr1.Subs.Add(new SubStat{Stat="Spd",Value=12});
    Check(!(bool)pwrM.Invoke(null,new object[]{pwr2}),"slot 2 spd does not power-up from simulated spd");
    Check((bool)pwrM.Invoke(null,new object[]{pwr1}),"slot 1 spd still power-up from simulated spd");
    var coverSaved=RuneEngine.Presets.ToList();
    try{
      var coverBase=RuneEngine.MakePreset("CoverBase",new[]{"P1","P2","Non","P1","Non","Non","P1","P2","P2","Non","Non"},"Violent","Will","HP%,Spd","CtD%","HP%");
      var coverAcc=RuneEngine.MakePreset("CoverAcc",new[]{"P1","P2","Non","P1","Non","P1","P1","P2","P2","Non","Non"},"Violent","Will","HP%,Spd","CtD%","HP%");
      RuneEngine.ReplacePresets(new List<Preset>{coverBase,coverAcc});
      var coverRune=new RuneRow{Set="Violent",Slot=1,Main="Atk+",MainValue=160,Grade=5,Stars=6,Level=12};
      coverRune.Subs.Add(new SubStat{Stat="CtR%",Value=17});
      coverRune.Subs.Add(new SubStat{Stat="HP%",Value=18});
      coverRune.Subs.Add(new SubStat{Stat="Spd",Value=16});
      coverRune.Subs.Add(new SubStat{Stat="Atk%",Value=11});
      RuneEngine.Calculate(new List<RuneRow>{coverRune});
      Check(coverRune.BestBuild=="CoverBase","rune without Acc shows the simpler preset name");
      coverRune.Subs[3].Stat="Acc%";coverRune.Subs[3].Value=20;coverRune.BestBuild="";
      RuneEngine.Calculate(new List<RuneRow>{coverRune});
      Check(coverRune.BestBuild=="CoverAcc","rune with Acc keeps the Acc P1 preset name");
      var fast=RuneEngine.MakePreset("Fast DD MAX DPS",new[]{"Non","P1","Non","P1","Non","Non","P1","P1","Non","P1","Non"},"Violent","","Atk%,Spd","CtD%","Atk%");
      var slow=RuneEngine.MakePreset("Slow DD MAX DPS",new[]{"Non","P1","Non","Non","Non","Non","P1","P1","Non","P1","Non"},"Blade,Rage","Violent","Atk%","CtD%","Atk%");
      RuneEngine.ReplacePresets(new List<Preset>{fast,slow});
      var cdRune=new RuneRow{Set="Violent",Slot=4,Main="CtD%",MainValue=59,Grade=5,Stars=6,Level=12};
      cdRune.Subs.Add(new SubStat{Stat="Atk%",Value=14});
      cdRune.Subs.Add(new SubStat{Stat="CtR%",Value=10});
      cdRune.Subs.Add(new SubStat{Stat="Atk+",Value=27});
      cdRune.Subs.Add(new SubStat{Stat="Def+",Value=14});
      RuneEngine.Calculate(new List<RuneRow>{cdRune});
      Check(cdRune.BestBuild=="Fast DD MAX DPS","CD rune with dead Def+ stays Fast DD not Slow DD");
      Check(cdRune.RecommendSource=="Def+"&&cdRune.RecommendTarget=="Spd","gem replaces dead Def+ with Spd not more Atk");
      Check(!cdRune.RecommendationInStock,"CD rune without Spd gem is not marked in-stock");
      string riftSpd="{\"command\":\"BattleRiftDungeonResult\",\"ret_code\":0,\"item_list\":[{\"type\":27,\"id\":null,\"quantity\":1,\"is_boxing\":1,\"info\":{\"craft_item_id\":1990000001,\"wizard_id\":1,\"craft_type\":1,\"craft_type_id\":130805,\"sell_value\":30000,\"amount\":1}}]}";
      string gemMsg=RuneEngine.ApplyLiveEvent(new List<RuneRow>(),riftSpd);
      RuneEngine.Calculate(new List<RuneRow>{cdRune});
      Check(gemMsg.Length>0,"live gem drop reports a stock sync");
      Check(cdRune.RecommendationInStock&&cdRune.RecommendTarget=="Spd","live gem drop marks the upgrade as in stock");
      using(var form=new MainForm()){
        var created=form.Handle;
        var type=typeof(MainForm);var flags=BindingFlags.Instance|BindingFlags.NonPublic;
        type.GetField("all",flags).SetValue(form,new List<RuneRow>{cdRune});
        type.GetField("viewMode",flags).SetValue(form,"upgrade");
        var actionBox=(ComboBox)type.GetField("action",flags).GetValue(form);
        if(actionBox.Items.Count>0)actionBox.SelectedIndex=0;
        var filtered=((IEnumerable<RuneRow>)type.GetMethod("Filter",flags).Invoke(form,null)).ToList();
        Check(filtered.Contains(cdRune),"upgrade list includes the rune after live gem drop");
      }
      var support=RuneEngine.MakePreset("Support",new[]{"P1","Non","P2","P1","Non","P1","Non","Non","P2","Non","Non"},"Despair","","HP%,Spd","HP%,CtR%","HP%,Acc%","Def%","Def%","Def%");
      RuneEngine.ReplacePresets(new List<Preset>{support});
      var despair=new RuneRow{Set="Despair",Slot=6,Main="Acc%",MainValue=48,Innate="HP+",InnateValue=288,Grade=5,Stars=6,Level=12};
      despair.Subs.Add(new SubStat{Stat="CtR%",Value=8});
      despair.Subs.Add(new SubStat{Stat="Spd",Value=16});
      despair.Subs.Add(new SubStat{Stat="Atk+",Value=15,Grind=19});
      despair.Subs.Add(new SubStat{Stat="HP%",Value=15});
      RuneEngine.Calculate(new List<RuneRow>{despair});
      Check(despair.BestBuild=="Support","Despair Acc slot 6 stays Support");
      Check(despair.RecommendSource=="Atk+"&&despair.RecommendTarget=="Def%","gem the dead Atk+ flat not the rolled Crit");
      var gemKeep=RuneEngine.MakePreset("GemKeep",new[]{"Non","P1","Non","P1","Non","Non","P1","P1","Non","P1","Non"},"Violent","","Atk%,Spd","CtD%","Atk%");
      var gemStock=RuneEngine.MakePreset("GemStock",new[]{"Non","P1","Non","Non","Non","Non","P1","P1","Non","P1","Non"},"Blade,Rage","Violent","Atk%","CtD%","Atk%");
      double gemKeepRaw=(double)scoreM.Invoke(null,new object[]{cdRune,gemKeep});
      double gemStockRaw=(double)scoreM.Invoke(null,new object[]{cdRune,gemStock});
      Check(gemKeepRaw>gemStockRaw,"gem-keep raw beats empty-stock preset");
      if(gemStockRaw>0)gemStock.ScoreFactor=Math.Max(0.05,(gemKeepRaw-0.3)/gemStockRaw);
      var gemPack=new List<RuneRow>();
      var gemFocus=new RuneRow{Id=9001,Set="Violent",Slot=4,Main="CtD%",MainValue=59,Grade=5,Stars=6,Level=12};
      gemFocus.Subs.Add(new SubStat{Stat="Atk%",Value=14});
      gemFocus.Subs.Add(new SubStat{Stat="CtR%",Value=10});
      gemFocus.Subs.Add(new SubStat{Stat="Atk+",Value=27});
      gemFocus.Subs.Add(new SubStat{Stat="Def+",Value=14});
      gemPack.Add(gemFocus);
      for(int gi=0;gi<8;gi++){
        var filler=new RuneRow{Id=9100+gi,Set="Violent",Slot=4,Main="CtD%",MainValue=80,Grade=5,Stars=6,Level=12};
        filler.Subs.Add(new SubStat{Stat="Atk%",Value=20});
        filler.Subs.Add(new SubStat{Stat="Spd",Value=20});
        filler.Subs.Add(new SubStat{Stat="CtR%",Value=15});
        filler.Subs.Add(new SubStat{Stat="Atk+",Value=20});
        gemPack.Add(filler);
      }
      RuneEngine.ReplacePresets(new List<Preset>{gemKeep,gemStock});
      RuneEngine.Calculate(gemPack);
      Check(gemFocus.Scores!=null&&gemFocus.Scores.Length>=2&&gemFocus.Scores[1]>gemFocus.Scores[0],"empty stock still boosts display score");
      Check(gemFocus.BestBuild=="GemKeep","stock bonus does not steal best build from max raw");
      Check(gemFocus.RecommendSource=="Def+"&&gemFocus.RecommendTarget=="Spd","stock bonus does not change gem rec");
      var despairAtk=new RuneRow{Id=9201,Set="Despair",Slot=2,Main="Atk%",MainValue=47,Innate="HP+",InnateValue=348,Grade=5,Stars=6,Level=12};
      despairAtk.Subs.Add(new SubStat{Stat="CtD%",Value=11});
      despairAtk.Subs.Add(new SubStat{Stat="Acc%",Value=16});
      despairAtk.Subs.Add(new SubStat{Stat="Spd",Value=18});
      despairAtk.Subs.Add(new SubStat{Stat="CtR%",Value=5});
      var fastDps=RuneEngine.MakePreset("Fast DD MAX DPS",new[]{"Non","P1","Non","P1","Non","Non","P1","P1","Non","P1","Non"},"Swift,Blade,Rage,Violent,Will,Intangible","Fatal,Despair,Vampire,Nemesis,Shield,Revenge,Fight","Atk%,Spd","CtD%","Atk%");
      var fastHp=RuneEngine.MakePreset("Fast DD HP",new[]{"P2","P1","Non","P1","Non","P2","P1","P1","Non","P1","Non"},"Swift,Blade,Rage,Violent,Will,Intangible","Focus,Fatal,Despair,Vampire,Nemesis,Shield,Revenge,Fight","Atk%,Spd","CtD%","Atk%","HP%","HP%","HP%");
      var slowDps=RuneEngine.MakePreset("Slow DD MAX DPS",new[]{"Non","P1","Non","Non","Non","Non","P1","P1","Non","P1","Non"},"Blade,Rage,Violent,Will,Intangible","Fatal,Despair,Nemesis","Atk%","CtD%","Atk%");
      var slowHp=RuneEngine.MakePreset("Slow DD HP",new[]{"P2","P1","Non","Non","Non","P2","P1","P1","Non","P1","Non"},"Blade,Rage,Violent,Will,Shield,Intangible","Focus,Fatal,Despair,Vampire,Nemesis,Revenge,Fight","Atk%","CtD%","Atk%","HP%","HP%","HP%");
      RuneEngine.ReplacePresets(new List<Preset>{fastDps,fastHp,slowDps,slowHp});
      RuneEngine.Calculate(new List<RuneRow>{despairAtk});
      Check(despairAtk.BestBuild=="Fast DD HP","Despair Atk slot 2 with Spd stays Fast DD HP not Slow DD HP");
      Check(despairAtk.Scores!=null&&despairAtk.Scores.Length>=4&&despairAtk.Scores[1]>despairAtk.Scores[3],"Fast DD HP score stays above Slow DD HP");
      Check(Math.Abs(despairAtk.Potential-Math.Round(despairAtk.Scores[1],3))<.001,"displayed potential is Fast DD HP max");
      var liveFast=RuneEngine.MakePreset("Fast DD",new[]{"P2","P1","Non","P1","Non","P2","P1","P1","Non","P1","Non"},"Swift,Blade,Rage,Violent,Will,Intangible","Fatal,Despair,Vampire,Nemesis,Shield,Revenge,Fight","Atk%,Spd","CtD%","Atk%");
      var liveSlow=RuneEngine.MakePreset("Slow DD",new[]{"P2","P1","Non","Non","Non","P2","P1","P1","Non","P1","Non"},"Blade,Rage,Violent,Will,Shield,Intangible","Focus,Fatal,Despair,Vampire,Nemesis,Revenge,Fight","Atk%","CtD%","Atk%");
      RuneEngine.ReplacePresets(new List<Preset>{liveFast,liveSlow});
      var noJunk=new RuneRow{Id=9301,Set="Violent",Slot=5,Main="HP+",MainValue=2448,Innate="Atk%",InnateValue=21,Grade=5,Stars=6,Level=12};
      noJunk.Subs.Add(new SubStat{Stat="CtD%",Value=7});
      noJunk.Subs.Add(new SubStat{Stat="Atk+",Value=17,Grind=28});
      noJunk.Subs.Add(new SubStat{Stat="CtR%",Value=17});
      noJunk.Subs.Add(new SubStat{Stat="HP%",Value=11});
      RuneEngine.Calculate(new List<RuneRow>{noJunk});
      Check(noJunk.BestBuild!="Slow DD"||noJunk.RecommendTarget!="Spd","Slow DD does not recommend a Spd gem");
      var liveFastHp=RuneEngine.MakePreset("Fast DD",new[]{"P2","P1","Non","P1","Non","P2","P1","P1","Non","Non","Non"},"Swift,Blade,Rage,Violent,Will,Intangible","Fatal,Despair,Vampire,Nemesis,Shield,Revenge,Fight","Atk%,Spd","CtD%","Atk%");
      var liveFastDps=RuneEngine.MakePreset("Fast DD MAX DPS",new[]{"Non","P1","Non","P1","Non","Non","P1","P1","Non","P1","Non"},"Swift,Blade,Rage,Violent,Will,Intangible","Fatal,Despair,Vampire,Nemesis,Shield,Revenge,Fight","Atk%,Spd","CtD%","Atk%");
      var liveSlowHp=RuneEngine.MakePreset("Slow DD",new[]{"P2","P1","Non","Non","Non","P2","P1","P1","Non","Non","Non"},"Blade,Rage,Violent,Will,Shield,Intangible","Focus,Fatal,Despair,Vampire,Nemesis,Revenge,Fight","Atk%","CtD%","Atk%");
      var liveSlowDps=RuneEngine.MakePreset("Slow DD MAX DPS",new[]{"Non","P1","Non","Non","Non","Non","P1","P1","Non","P1","Non"},"Blade,Rage,Violent,Will,Shield,Intangible","Focus,Fatal,Despair,Vampire,Nemesis,Revenge,Fight","Atk%","CtD%","Atk%");
      RuneEngine.ReplacePresets(new List<Preset>{liveFastHp,liveFastDps,liveSlowHp,liveSlowDps});
      var gemHp=new RuneRow{Id=9501,Set="Violent",Slot=5,Main="HP+",MainValue=2448,Innate="Atk%",InnateValue=22,Grade=5,Stars=6,Level=12};
      gemHp.Subs.Add(new SubStat{Stat="Atk%",Value=10,Grind=7});
      gemHp.Subs.Add(new SubStat{Stat="CtR%",Value=10});
      gemHp.Subs.Add(new SubStat{Stat="HP%",Value=12,Gemmed=true});
      gemHp.Subs.Add(new SubStat{Stat="CtD%",Value=14});
      RuneEngine.Calculate(new List<RuneRow>{gemHp});
      Check(gemHp.BestBuild!="Slow DD","gemmed HP% does not rename the rune Slow DD");
      Check(gemHp.RecommendTarget=="Spd","gemmed HP% hole still gems into Spd on Fast DD");
      RuneEngine.ReplacePresets(new List<Preset>{liveFastHp});
      var hpVsDef=new RuneRow{Id=9601,Set="Violent",Slot=4,Main="CtD%",MainValue=59,Grade=5,Stars=6,Level=12};
      hpVsDef.Subs.Add(new SubStat{Stat="Atk%",Value=21});
      hpVsDef.Subs.Add(new SubStat{Stat="Def+",Value=25});
      hpVsDef.Subs.Add(new SubStat{Stat="HP+",Value=263,Grind=447});
      hpVsDef.Subs.Add(new SubStat{Stat="CtR%",Value=12});
      RuneEngine.Calculate(new List<RuneRow>{hpVsDef});
      Check(hpVsDef.RecommendSource=="Def+"&&hpVsDef.RecommendTarget=="Spd","P0 HP flat is kept over P0 Def flat");
      var bruiserBomb=RuneEngine.MakePreset("Bruiser Bomber",new[]{"P1","P1","Non","P1","Non","P1","P1","Non","P3","Non","Non"},"Swift,Violent,Will,Intangible","Despair,Revenge","HP%,Atk%,Spd","HP%,Atk%,CtR%","HP%,Atk%,Acc%");
      RuneEngine.ReplacePresets(new List<Preset>{bruiserBomb});
      var hpVsCd=new RuneRow{Id=9701,Set="Swift",Slot=2,Main="Atk%",MainValue=47,Innate="Res%",InnateValue=6,Grade=5,Stars=6,Level=12};
      hpVsCd.Subs.Add(new SubStat{Stat="HP+",Value=348});
      hpVsCd.Subs.Add(new SubStat{Stat="CtR%",Value=15});
      hpVsCd.Subs.Add(new SubStat{Stat="HP%",Value=16});
      hpVsCd.Subs.Add(new SubStat{Stat="CtD%",Value=5});
      RuneEngine.Calculate(new List<RuneRow>{hpVsCd});
      Check(hpVsCd.RecommendSource=="CtD%"&&hpVsCd.RecommendTarget=="Spd","P0 HP flat is kept over P0 CtD");
      RuneEngine.ReplacePresets(new List<Preset>{liveSlow});
      var hpToAtk=new RuneRow{Id=9401,Set="Violent",Slot=5,Main="HP+",MainValue=2448,Innate="Atk%",InnateValue=22,Grade=5,Stars=6,Level=12};
      hpToAtk.Subs.Add(new SubStat{Stat="HP%",Value=10,Grind=7});
      hpToAtk.Subs.Add(new SubStat{Stat="CtR%",Value=10});
      hpToAtk.Subs.Add(new SubStat{Stat="CtD%",Value=12,Grind=3});
      hpToAtk.Subs.Add(new SubStat{Stat="Acc%",Value=14});
      var savedAtk=RuneEngine.StatGlobalFactor["Atk+"];
      var savedHp=RuneEngine.StatGlobalFactor["HP%"];
      RuneEngine.StatGlobalFactor["Atk+"]=0.9;RuneEngine.StatGlobalFactor["HP%"]=1;
      RuneEngine.Stocks.Add(new CraftStock{Type="Gemme",Set="Violent",Stat="HP%",Grade=4,Amount=1,Ancient=false});
      RuneEngine.Stocks.Add(new CraftStock{Type="Gemme",Set="Violent",Stat="Atk+",Grade=4,Amount=1,Ancient=false});
      RuneEngine.Calculate(new List<RuneRow>{hpToAtk});
      RuneEngine.StatGlobalFactor["Atk+"]=savedAtk;RuneEngine.StatGlobalFactor["HP%"]=savedHp;
      RuneEngine.Stocks.RemoveAll(x=>x.Id==0&&(x.Stat=="HP%"||x.Stat=="Atk+")&&x.Grade==4);
      Check(hpToAtk.RecommendTarget=="Atk+","Slow DD gems P2 HP% into P1 Atk+");
      RuneEngine.ReplacePresets(new List<Preset>{liveFastHp});
      var slot3Hp=new RuneRow{Id=9402,Set="Violent",Slot=3,Main="Def+",MainValue=118,Grade=5,Stars=6,Level=12,Ancient=true};
      slot3Hp.Subs.Add(new SubStat{Stat="CtR%",Value=1});
      slot3Hp.Subs.Add(new SubStat{Stat="HP%",Value=5});
      slot3Hp.Subs.Add(new SubStat{Stat="CtD%",Value=25});
      slot3Hp.Subs.Add(new SubStat{Stat="Spd",Value=11});
      RuneEngine.Calculate(new List<RuneRow>{slot3Hp});
      Check(slot3Hp.RecommendSource=="CtR%"&&slot3Hp.RecommendTarget=="CtR%","Fast DD gems P1 Crit Rate not P2 HP%");
      var slot1Hp=new RuneRow{Id=9403,Set="Violent",Slot=1,Main="Def+",MainValue=118,Grade=5,Stars=6,Level=12,Ancient=true};
      slot1Hp.Subs.Add(new SubStat{Stat="HP%",Value=5});
      slot1Hp.Subs.Add(new SubStat{Stat="CtD%",Value=25});
      slot1Hp.Subs.Add(new SubStat{Stat="Spd",Value=11});
      RuneEngine.Calculate(new List<RuneRow>{slot1Hp});
      Check(slot1Hp.RecommendSource=="HP%"&&slot1Hp.RecommendTarget=="Atk%","Fast DD slot 1 still gems P2 HP% into P1 Atk%");
      RuneEngine.ReplacePresets(new List<Preset>{liveFastDps});
      var flatVsCrit=new RuneRow{Id=9404,Set="Violent",Slot=5,Main="HP+",MainValue=1600,Innate="HP%",InnateValue=8,Grade=5,Stars=6,Level=12};
      flatVsCrit.Subs.Add(new SubStat{Stat="Atk%",Value=11});
      flatVsCrit.Subs.Add(new SubStat{Stat="CtD%",Value=10});
      flatVsCrit.Subs.Add(new SubStat{Stat="Spd",Value=6});
      flatVsCrit.Subs.Add(new SubStat{Stat="Atk+",Value=11});
      RuneEngine.Calculate(new List<RuneRow>{flatVsCrit});
      Check(flatVsCrit.RecommendSource=="Atk+"&&flatVsCrit.RecommendTarget=="CtR%","Fast DD MAX DPS gems P1 Atk flat into P1 Crit not more Spd");
      var junkToFlat=new RuneRow{Id=9405,Set="Violent",Slot=5,Main="HP+",MainValue=1600,Innate="CtR%",InnateValue=8,Grade=5,Stars=6,Level=12};
      junkToFlat.Subs.Add(new SubStat{Stat="Atk%",Value=11});
      junkToFlat.Subs.Add(new SubStat{Stat="CtD%",Value=10});
      junkToFlat.Subs.Add(new SubStat{Stat="Spd",Value=6});
      junkToFlat.Subs.Add(new SubStat{Stat="HP%",Value=5});
      RuneEngine.Calculate(new List<RuneRow>{junkToFlat});
      Check(junkToFlat.RecommendSource=="HP%"&&junkToFlat.RecommendTarget=="Atk+","missing P1 Atk flat is filled from junk not by gemming Atk%");
      var defDd=RuneEngine.MakePreset("Def DD",new[]{"P2","Non","P1","P2","Non","P2","P1","P1","P3","Non","P1"},"Guard,Blade,Rage,Will,Determination,Intangible","Despair,Violent,Fight","Def%","Def%,CtD%","Def%");
      RuneEngine.ReplacePresets(new List<Preset>{defDd});
      var defJunk=new RuneRow{Id=9406,Set="Will",Slot=5,Main="HP+",MainValue=1600,Innate="CtR%",InnateValue=8,Grade=5,Stars=6,Level=12};
      defJunk.Subs.Add(new SubStat{Stat="Def%",Value=11});
      defJunk.Subs.Add(new SubStat{Stat="CtD%",Value=10});
      defJunk.Subs.Add(new SubStat{Stat="Spd",Value=8});
      defJunk.Subs.Add(new SubStat{Stat="Atk%",Value=6});
      RuneEngine.Calculate(new List<RuneRow>{defJunk});
      Check(defJunk.RecommendSource=="Atk%"&&defJunk.RecommendTarget=="Def+","Def DD fills missing P1 Def flat from junk not Def%");
      var defFlatVsCrit=new RuneRow{Id=9407,Set="Will",Slot=5,Main="HP+",MainValue=1600,Innate="HP%",InnateValue=8,Grade=5,Stars=6,Level=12};
      defFlatVsCrit.Subs.Add(new SubStat{Stat="Def%",Value=11});
      defFlatVsCrit.Subs.Add(new SubStat{Stat="CtD%",Value=10});
      defFlatVsCrit.Subs.Add(new SubStat{Stat="Def+",Value=15});
      RuneEngine.Calculate(new List<RuneRow>{defFlatVsCrit});
      Check(defFlatVsCrit.RecommendSource=="Def+"&&defFlatVsCrit.RecommendTarget=="CtR%","Def DD gems P1 Def flat into P1 Crit not more Def%");
      var bomber=RuneEngine.MakePreset("Bomber",new[]{"P2","P1","Non","P1","Non","P1","Non","Non","Non","P1","Non"},"Fatal,Will,Intangible","Focus,Violent,Fight","Atk%,Spd","Atk%","Atk%","HP%","HP%","HP%,Acc%");
      RuneEngine.ReplacePresets(new List<Preset>{bomber});
      var bomberJunk=new RuneRow{Id=9408,Set="Will",Slot=5,Main="HP+",MainValue=1600,Innate="Acc%",InnateValue=8,Grade=5,Stars=6,Level=12};
      bomberJunk.Subs.Add(new SubStat{Stat="Atk%",Value=11});
      bomberJunk.Subs.Add(new SubStat{Stat="Spd",Value=10});
      bomberJunk.Subs.Add(new SubStat{Stat="HP%",Value=8});
      bomberJunk.Subs.Add(new SubStat{Stat="CtR%",Value=5});
      RuneEngine.Calculate(new List<RuneRow>{bomberJunk});
      Check(bomberJunk.RecommendSource=="CtR%"&&bomberJunk.RecommendTarget=="Atk+","Bomber fills missing P1 Atk flat from junk not Atk%");
      var slowDpsGem=RuneEngine.MakePreset("Slow DD MAX DPS",new[]{"Non","P1","Non","Non","Non","Non","P1","P1","Non","P1","Non"},"Blade,Rage,Violent,Will,Shield,Intangible","Focus,Fatal,Despair,Vampire,Nemesis,Revenge,Fight","Atk%","CtD%","Atk%");
      RuneEngine.ReplacePresets(new List<Preset>{slowDpsGem});
      var ctdVsAtkFlat=new RuneRow{Id=9409,Set="Violent",Slot=5,Main="HP+",MainValue=2448,Innate="Acc%",InnateValue=4,Grade=5,Stars=6,Level=12};
      ctdVsAtkFlat.Subs.Add(new SubStat{Stat="CtR%",Value=11});
      ctdVsAtkFlat.Subs.Add(new SubStat{Stat="Atk%",Value=19,Grind=21});
      ctdVsAtkFlat.Subs.Add(new SubStat{Stat="CtD%",Value=8});
      ctdVsAtkFlat.Subs.Add(new SubStat{Stat="Atk+",Value=20});
      RuneEngine.Stocks.Add(new CraftStock{Type="Gemme",Set="Violent",Stat="Atk+",Grade=4,Amount=1,Ancient=false});
      RuneEngine.Calculate(new List<RuneRow>{ctdVsAtkFlat});
      RuneEngine.Stocks.RemoveAll(x=>x.Id==0&&x.Stat=="Atk+"&&x.Grade==4);
      Check(ctdVsAtkFlat.RecommendSource=="CtD%"&&ctdVsAtkFlat.RecommendTarget=="CtD%","Slow DD MAX DPS gems P1 CtD not P1 Atk flat");
      var bruiserCritAcc=RuneEngine.MakePreset("Bruiser Crit/Acc",new[]{"P1","P1","P3","P1","Non","P2","P1","Non","P3","Non","Non"},"Swift,Violent,Will,Intangible","Despair,Revenge","HP%,Atk%,Spd","HP%,Atk%,CtR%","HP%,Atk%,Acc%");
      RuneEngine.ReplacePresets(new List<Preset>{bruiserCritAcc});
      var junkDef=new RuneRow{Id=9901,Set="Intangible",Slot=2,Main="HP%",MainValue=47,Grade=5,Stars=6,Level=12};
      junkDef.Subs.Add(new SubStat{Stat="Atk%",Value=6});
      junkDef.Subs.Add(new SubStat{Stat="Spd",Value=17});
      junkDef.Subs.Add(new SubStat{Stat="CtR%",Value=11});
      junkDef.Subs.Add(new SubStat{Stat="Def+",Value=13});
      RuneEngine.Calculate(new List<RuneRow>{junkDef});
      Check(junkDef.RecommendSource=="Def+"&&junkDef.RecommendTarget=="Acc%","junk Def flat is gemmed instead of a valued Atk%");
      var resToAcc=new RuneRow{Id=9902,Set="Intangible",Slot=4,Main="CtR%",MainValue=58,Grade=5,Stars=6,Level=12};
      resToAcc.Subs.Add(new SubStat{Stat="Spd",Value=11,Grind=4});
      resToAcc.Subs.Add(new SubStat{Stat="Atk%",Value=23,Grind=7});
      resToAcc.Subs.Add(new SubStat{Stat="HP%",Value=5});
      resToAcc.Subs.Add(new SubStat{Stat="Res%",Value=8});
      RuneEngine.Calculate(new List<RuneRow>{resToAcc});
      Check(resToAcc.RecommendSource=="Res%"&&resToAcc.RecommendTarget=="Acc%","gemming Res unlocks Acc on Bruiser Crit/Acc");
      var defPctLeft=new RuneRow{Id=9410,Set="Swift",Slot=2,Main="HP%",MainValue=63,Innate="Acc%",InnateValue=8,Grade=5,Stars=6,Level=12};
      defPctLeft.Subs.Add(new SubStat{Stat="Def%",Value=5});
      defPctLeft.Subs.Add(new SubStat{Stat="Atk%",Value=13,Grind=17});
      defPctLeft.Subs.Add(new SubStat{Stat="Spd",Value=10,Grind=6});
      defPctLeft.Subs.Add(new SubStat{Stat="CtR%",Value=11});
      RuneEngine.Calculate(new List<RuneRow>{defPctLeft});
      Check(defPctLeft.RecommendSource=="Def%"&&defPctLeft.RecommendTarget=="Def%","Bruiser Crit/Acc gems leftover P3 Def% when P1s are at cap");
      Check(defPctLeft.Recommendation.IndexOf("+13",StringComparison.Ordinal)>=0,"Def% legend gem max is 13 not 9");
    }finally{RuneEngine.ReplacePresets(coverSaved);}
    var overlay=new List<RuneRow>();
    string hammer="{\"command\":\"UpgradeRuneList\",\"ret_code\":0,\"upgrade_rune_list\":[{\"rune_id\":65236285954,\"slot_no\":3,\"rank\":14,\"class\":16,\"set_id\":13,\"upgrade_curr\":6,\"pri_eff\":[5,70],\"prefix_eff\":[0,0],\"sec_eff\":[[11,11,0,0],[6,7,0,0],[2,14,0,0]]}]}";
    RuneEngine.ApplyLiveEvent(overlay,hammer,false);
    Check(overlay.Count==0,"json import overlay does not resurrect sold hammer rune");
    RuneEngine.ApplyLiveEvent(overlay,hammer,true);
    Check(overlay.Any(r=>r.Id==65236285954),"live hammer still adds a new rune during the session");
    RuneEngine.ApplyLiveEvent(overlay,"{\"command\":\"SellRune\",\"ret_code\":0,\"rune_id_list\":[65236285954]}",false);
    Check(!overlay.Any(r=>r.Id==65236285954),"sell still removes after overlay");
    bool deckFixe=RuneEngine.SeuilVenteFixe;double deckSeuil=RuneEngine.ValeurSeuilVenteFixe;
    RuneEngine.SeuilVenteFixe=true;RuneEngine.ValeurSeuilVenteFixe=9;
    try{
      var d1=new RuneRow{Id=101,Set="Violent",Slot=1,Main="HP+",MainValue=160,Grade=5,Stars=6,Level=12,Potential=5,Marker="",Action="Keep"};
      var d2=new RuneRow{Id=102,Set="Violent",Slot=3,Main="Def+",MainValue=160,Grade=5,Stars=6,Level=12,Potential=5,Marker="",Action="Keep"};
      var d3=new RuneRow{Id=103,Set="Violent",Slot=5,Main="HP+",MainValue=2448,Grade=5,Stars=6,Level=12,Potential=5,Marker="",Action="Keep"};
      var deckRows=new List<RuneRow>{d1,d2,d3};
      string otherType="{\"command\":\"setDeckList\",\"deck_type\":2,\"deck_list\":[{\"deck_type\":2,\"equip\":[{\"rune_id_list\":[103]}]}]}";
      string fullType="{\"command\":\"setDeckList\",\"deck_type\":1,\"deck_list\":[{\"deck_type\":1,\"equip\":[{\"rune_id_list\":[101,102]}]}]}";
      string dropOne="{\"command\":\"setDeckList\",\"deck_type\":1,\"deck_list\":[{\"deck_type\":1,\"equip\":[{\"rune_id_list\":[102]}]}]}";
      Check(RuneEngine.ApplyLiveDeckProtection(deckRows,otherType)>0,"other deck type marks protection");
      Check(RuneEngine.ApplyLiveDeckProtection(deckRows,fullType)>0,"arena deck marks both runes");
      RuneEngine.ApplyRetentionRules(deckRows);
      Check(d1.Action=="Keep"&&d2.Action=="Keep"&&d3.Action=="Keep","deck runes stay Keep below threshold while listed");
      Check(RuneEngine.ApplyLiveDeckProtection(deckRows,dropOne)>0,"removing a rune from setDeckList unmarks it");
      RuneEngine.ApplyRetentionRules(deckRows);
      Check(d1.Action=="Sell"&&(d1.Marker??"").IndexOf("Deck",StringComparison.OrdinalIgnoreCase)<0,"removed deck rune is checked against the sell threshold");
      Check(d2.Action=="Keep"&&(d2.Marker??"").IndexOf("Deck",StringComparison.OrdinalIgnoreCase)>=0,"rune still on the same deck stays protected");
      Check(d3.Action=="Keep"&&(d3.Marker??"").IndexOf("Deck",StringComparison.OrdinalIgnoreCase)>=0,"other deck type is not unmarked");
    }finally{RuneEngine.SeuilVenteFixe=deckFixe;RuneEngine.ValeurSeuilVenteFixe=deckSeuil;}
    var iconFlags=BindingFlags.Static|BindingFlags.NonPublic;
    var qualityKey=typeof(MainForm).GetMethod("CroquisQualityKey",iconFlags);
    Check((int)qualityKey.Invoke(null,new object[]{1})==1,"normal rune uses quality 1");
    Check((int)qualityKey.Invoke(null,new object[]{2})==2,"magic rune uses quality 2");
    Check((int)qualityKey.Invoke(null,new object[]{4})==4,"hero rune uses quality 4");
    Check((int)qualityKey.Invoke(null,new object[]{5})==5,"legend rune uses quality 5");
    var legendCol=(Color)typeof(MainForm).GetMethod("RuneLogoColor",iconFlags).Invoke(null,new object[]{5});
    Check(legendCol.R>200&&legendCol.G>150&&legendCol.G<190&&legendCol.B<80,"legend orange matches in-game gold");
    using(var src=new Bitmap(8,8,PixelFormat.Format32bppArgb)){
      for(int y=0;y<8;y++)for(int x=0;x<8;x++)src.SetPixel(x,y,Color.FromArgb(255,224,159,75));
      var tinted=(Bitmap)typeof(MainForm).GetMethod("TintCroquisCreux",iconFlags).Invoke(null,new object[]{src,Color.FromArgb(64,210,110)});
      var p=tinted.GetPixel(3,3);
      Check(p.G>p.R&&p.G>80,"magic tint turns orange glyph green");
    }
    using(var cyan=new Bitmap(8,8,PixelFormat.Format32bppArgb)){
      for(int y=0;y<8;y++)for(int x=0;x<8;x++)cyan.SetPixel(x,y,Color.FromArgb(255,69,226,200));
      var legend=(Bitmap)typeof(MainForm).GetMethod("TintCroquisCreux",iconFlags).Invoke(null,new object[]{cyan,Color.FromArgb(255,126,20)});
      var lp=legend.GetPixel(3,3);
      Check(lp.R>lp.B&&lp.R>lp.G&&lp.R>180,"legend tint turns cyan glyph orange");
    }
    string destroySlot6=Path.Combine(@"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets","croquis-rune-3d-destroy-slot6.png");
    if(File.Exists(destroySlot6)){
      var rawDestroy=(Bitmap)typeof(MainForm).GetMethod("LoadPng32",iconFlags).Invoke(null,new object[]{destroySlot6});
      var legendDestroy=(Bitmap)typeof(MainForm).GetMethod("TintCroquisCreux",iconFlags).Invoke(null,new object[]{rawDestroy,Color.FromArgb(255,126,20)});
      int orangeAccent=0,cyanLeft=0;
      for(int y=0;y<legendDestroy.Height;y++)for(int x=0;x<legendDestroy.Width;x++){
        var c=legendDestroy.GetPixel(x,y);if(c.A<80)continue;
        int mx=c.R;if(c.G>mx)mx=c.G;if(c.B>mx)mx=c.B;
        int mn=c.R;if(c.G<mn)mn=c.G;if(c.B<mn)mn=c.B;
        if(mx-mn<50||mx<120)continue;
        if(c.R>c.B&&c.R>c.G)orangeAccent++;
        if(c.B>c.R+40&&c.G>c.R)cyanLeft++;
      }
      Check(orangeAccent>80,"legend destroy rim is orange");
      Check(cyanLeft<20,"legend destroy rim is not left cyan");
    }
    string outlined=Path.Combine(@"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets","croquis-rune-3d-violent-slot1.png");
    if(File.Exists(outlined)){
      var raw=(Bitmap)typeof(MainForm).GetMethod("LoadPng32",iconFlags).Invoke(null,new object[]{outlined});
      Check(raw!=null&&raw.GetPixel(0,0).A<8,"loaded png keeps transparent corner");
      var baked=(Bitmap)typeof(MainForm).GetMethod("BakeCroquisPng",iconFlags).Invoke(null,new object[]{outlined});
      Check(baked!=null&&baked.Width<=160&&baked.Height<=160,"transparent rune stays icon size");
      Check(baked.Width>40&&baked.Height>40,"cropped rune still has a stone");
      int whiteLeft=0,clear=0;
      for(int y=0;y<baked.Height;y++)for(int x=0;x<baked.Width;x++){
        var c=baked.GetPixel(x,y);if(c.A<8){clear++;continue;}if(c.A>=40&&c.R>=240&&c.G>=240&&c.B>=240)whiteLeft++;
      }
      Check(whiteLeft<8,"cutout checkerboard is not kept as a white card");
      Check(clear>100,"transparent icons keep a real alpha hole");
      int dark=0;
      for(int y=0;y<baked.Height;y++)for(int x=0;x<baked.Width;x++){
        var c=baked.GetPixel(x,y);if(c.A>=180&&c.R<50&&c.G<50&&c.B<50)dark++;
      }
      Check(dark>80,"black outline around the rune is kept");
      var blitFn=typeof(MainForm).GetMethod("BlitOpaqueToSquare",iconFlags,null,new Type[]{typeof(Image),typeof(RectangleF),typeof(int)},null);
      var measureFn=typeof(MainForm).GetMethod("MeasureOpaqueStone",iconFlags);
      var ob=(RectangleF)measureFn.Invoke(null,new object[]{baked});
      var blit=(Bitmap)blitFn.Invoke(null,new object[]{baked,ob,52});
      Check(blit!=null&&blit.Width==52&&blit.Height==52,"cell blit is a 52px square");
      int bx0=52,by0=52,bx1=-1,by1=-1;
      for(int y=0;y<blit.Height;y++)for(int x=0;x<blit.Width;x++){
        var c=blit.GetPixel(x,y);
        if(c.A<40)continue;
        if(c.R<40&&c.G<40&&c.B<40)continue;
        if(x<bx0)bx0=x;if(y<by0)by0=y;if(x>bx1)bx1=x;if(y>by1)by1=y;
      }
      Check(bx1>=0&&(bx1-bx0+1)>=40&&(by1-by0+1)>=40,"cell blit fills the square with the stone");
      var boxFn=typeof(MainForm).GetMethod("BlitOpaqueToBox",iconFlags,null,new Type[]{typeof(Image),typeof(RectangleF),typeof(int),typeof(int),typeof(Color)},null);
      var onGrid=(Bitmap)boxFn.Invoke(null,new object[]{baked,ob,52,52,Color.FromArgb(30,32,36)});
      var corner=onGrid.GetPixel(1,1);
      Check(corner.A==255&&!(corner.R<12&&corner.G<12&&corner.B<12),"transparent pixels stay the cell color not black");
      var destFn=typeof(MainForm).GetMethod("CroquisDestSize",iconFlags);
      string slot2Path=Path.Combine(@"C:\Users\Great-Lucky\Documents\Rune_Manager_Modern\Donnees\assets","croquis-rune-3d-violent-slot2.png");
      if(File.Exists(slot2Path)){
        var baked2=(Bitmap)typeof(MainForm).GetMethod("BakeCroquisPng",iconFlags).Invoke(null,new object[]{slot2Path});
        var ob2=(RectangleF)measureFn.Invoke(null,new object[]{baked2});
        var s1=(Size)destFn.Invoke(null,new object[]{ob,84,50});
        var s2=(Size)destFn.Invoke(null,new object[]{ob2,84,50});
        int max1=Math.Max(s1.Width,s1.Height),max2=Math.Max(s2.Width,s2.Height);
        Check(Math.Abs(max1-max2)<=2,"slot 2 uses the same max size as slot 1");
        Check(s2.Width<=s1.Height+2,"slot 2 hex is not wider than slot 1 diamond height");
      }
      var tintFn=typeof(MainForm).GetMethod("TintCroquisCreux",iconFlags);
      var logoFn=typeof(MainForm).GetMethod("RuneLogoColor",iconFlags);
      string previewDir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory);
      baked.Save(Path.Combine(previewDir,"_rune_q5.png"));
      for(int qg=1;qg<=4;qg++){
        var tinted=(Bitmap)tintFn.Invoke(null,new object[]{baked,(Color)logoFn.Invoke(null,new object[]{qg})});
        tinted.Save(Path.Combine(previewDir,"_rune_q"+qg+".png"));
      }
    }
    var rows=RuneEngine.Import(args[0]);Check(rows.Count>0,"real inventory imported");Check(RuneEngine.PresetSlotCounts.Values.All(c=>Enumerable.Range(1,6).All(s=>RuneEngine.ScarcityBonus(c,s)>=0&&RuneEngine.ScarcityBonus(c,s)<=1)),"all inventory bonuses in [0,1]");
    var rune=rows.First(r=>r.Action=="Sell");RuneEngine.ProtectedWorldBossRuneIds.Add(rune.Id);RuneEngine.ApplyRetentionRules(rows);Check(rune.Action!="Sell","World Boss sale protection overrides low score");
    var scored=rows.First(r=>r.Potential>1);Check(RuneEngine.ExplainPotential(scored).Contains("Score brut"),"score explanation available");Check(RuneEngine.ExplainGem(scored).Contains("Preset"),"gem explanation available");
    string entry="{\"rune_id\":0,\"set_id\":13,\"slot_no\":3,\"class\":6,\"rank\":5,\"extra\":5,\"upgrade_curr\":15,\"pri_eff\":[5,160],\"prefix_eff\":[0,0],\"sec_eff\":[[11,10],[8,10],[2,10],[1,617]]}";
    string dict="{\"command\":\"ReceiveMail\",\"ret_code\":0,\"mail_list\":[{\"extra\":{\"101\":"+entry+",\"102\":"+entry+",\"103\":"+entry+"}}]}";
    var before=string.Join(";",RuneEngine.PresetSlotCounts.OrderBy(x=>x.Key).Select(x=>x.Key+string.Join(",",x.Value)));var choice=RuneEngine.CompareRuneChoice(rows,dict);Check(choice!=null&&choice.Choices.Count==3,"mail dictionary chest detected");Check(before==string.Join(";",RuneEngine.PresetSlotCounts.OrderBy(x=>x.Key).Select(x=>x.Key+string.Join(",",x.Value))),"chest does not alter inventory bonuses");
    string five="{\"command\":\"OpenReward\",\"ret_code\":0,\"choices\":["+string.Join(",",Enumerable.Repeat(entry,5))+"]}";Check(RuneEngine.CompareRuneChoice(rows,five).Choices.Count==5,"five rune chest retained");
    string ancientBox="{\"command\":\"GetMailList\",\"ret_code\":0,\"mail_list\":[{\"mail_type\":273,\"item_master_type\":49,\"item_master_id\":11,\"extra\":{\"201\":"+entry+",\"202\":"+entry+",\"203\":"+entry+",\"204\":"+entry+",\"205\":"+entry+"}}]}";
    var ancientChoice=RuneEngine.CompareRuneChoice(rows,ancientBox);Check(ancientChoice!=null&&ancientChoice.Choices.Count==5,"ancient rune box in GetMailList is ranked");
    Check(RuneEngine.CompareRuneChoice(rows,ancientBox)==null,"same ancient box not ranked twice");
    var peek=RuneEngine.CompareRuneChoice(rows,ancientBox,false);Check(peek!=null&&peek.Choices.Count==5,"peek still ranks already seen ancient box");
    int presetCount=RuneEngine.Presets.Count;
    Check(presetCount>=7,"default presets loaded");
    string starterSets=System.IO.Path.Combine("rune_manager_app","defaults","parametres-runes.tsv");
    if(System.IO.File.Exists(starterSets)){
      var starterLines=System.IO.File.ReadAllLines(starterSets);
      Check(starterLines.Count(x=>x.StartsWith("PRESET\t"))==12,"first-run zip ships 12 starter presets");
      Check(!starterLines.Any(x=>x.StartsWith("SKILLHIDDEN")),"starter settings omit personal skill-hidden");
    }
    string oldName=RuneEngine.Presets[0].Name;
    var sample=rows.Where(r=>r.BestBuild==oldName).Take(8).ToList();
    var beforeScores=sample.Select(r=>r.Potential).ToArray();
    RuneEngine.Presets[0].Name="RenamedTestDD";
    foreach(var r in rows)if(r.BestBuild==oldName)r.BestBuild="RenamedTestDD";
    RuneEngine.Calculate(rows);
    Check(rows.Any(r=>r.BestBuild=="RenamedTestDD"),"renamed preset still assigned by engine");
    Check(sample.Count==0||sample.Zip(beforeScores,(r,v)=>Math.Abs(r.Potential-v)<0.001).All(x=>x),"rename does not change scores");
    RuneEngine.Presets[0].Name=oldName;
    foreach(var r in rows)if(r.BestBuild=="RenamedTestDD")r.BestBuild=oldName;
    RuneEngine.Calculate(rows);
    var extra=RuneEngine.ClonePreset(RuneEngine.Presets[0],"Extra Test Preset");
    RuneEngine.Presets.Add(extra);
    RuneEngine.Calculate(rows);
    Check(rows.All(r=>r.Scores!=null&&r.Scores.Length==presetCount+1),"added preset resizes score array");
    RuneEngine.Presets.RemoveAt(RuneEngine.Presets.Count-1);
    RuneEngine.Calculate(rows);
    Check(RuneEngine.Presets.Count==presetCount,"preset list restored after add test");
    var kept=RuneEngine.Presets[RuneEngine.Presets.Count-1];
    RuneEngine.Presets.RemoveAt(RuneEngine.Presets.Count-1);
    RuneEngine.Calculate(rows);
    Check(rows.All(r=>r.Scores!=null&&r.Scores.Length==presetCount-1),"removed preset resizes score array");
    RuneEngine.Presets.Add(kept);
    RuneEngine.Calculate(rows);
    Check(RuneEngine.Presets.Count==presetCount,"preset list restored after remove test");
    using(var form=new MainForm()){var type=typeof(MainForm);var flags=BindingFlags.Instance|BindingFlags.NonPublic;var created=form.Handle;type.GetField("all",flags).SetValue(form,rows);var action=(ComboBox)type.GetField("action",flags).GetValue(form);if(!action.Items.Contains("Sell"))action.Items.Add("Sell");int sellIdx=-1;for(int i=0;i<action.Items.Count;i++)if(Convert.ToString(action.Items[i])=="Sell")sellIdx=i;action.SelectedIndex=Math.Max(0,sellIdx);type.GetField("viewMode",flags).SetValue(form,"potential");type.GetField("all",flags).SetValue(form,rows);var filtered=((IEnumerable<RuneRow>)type.GetMethod("Filter",flags).Invoke(form,null)).ToList();if(filtered.Count==0)Console.WriteLine("PASS Sell filter skipped (no remaining sells)");else Check(filtered.All(r=>r.Action=="Sell"),"Sell filter works in Potential view");type.GetField("viewMode",flags).SetValue(form,"upgrade");filtered=((IEnumerable<RuneRow>)type.GetMethod("Filter",flags).Invoke(form,null)).ToList();if(filtered.Count==0)Console.WriteLine("PASS Sell filter upgrade skipped");else Check(filtered.All(r=>r.Action=="Sell"),"Sell filter overrides upgrade restrictions");}
    var real=RuneEngine.CompareRuneChoice(rows,System.IO.File.ReadAllText("tools/chest_three_fixture.json"));Check(real!=null&&real.Choices.Count==3,"actual recovered chest payload");foreach(var c in real.Choices)Console.WriteLine("CHEST "+c.Id+" "+c.Rune+" SCORE="+c.Potential+" PRESET="+c.BestBuild);
    using(var form=new MainForm()){
      var flags=BindingFlags.Instance|BindingFlags.NonPublic;
      using(var window=(Form)typeof(MainForm).GetMethod("CreatePresetWindow",flags).Invoke(form,null)){
        var g=window.Controls.OfType<DataGridView>().First();
        var created=g.Handle;
        int beforeRows=g.Rows.Count;
        typeof(MainForm).GetMethod("AddPresetGridRow",flags).Invoke(form,new object[]{g,0,new[]{"HP%","Atk%","Def%","Spd","Res%","Acc%","CtR%","CtD%","HP+","Atk+","Def+"}});
        Check(g.Rows.Count==beforeRows+1,"add preset inserts a grid row");
        Check(!g.Columns[1].ReadOnly,"preset name column is editable");
        g.CurrentCell=g.Rows[g.Rows.Count-1].Cells[1];
        var remove=typeof(MainForm).GetMethods(flags).First(m=>m.Name=="RemovePresetGridRow"&&m.GetParameters().Length==3);
        remove.Invoke(form,new object[]{g,0,false});
        Check(g.Rows.Count==beforeRows,"remove preset deletes the selected row");
        string name1=Convert.ToString(g.Rows[1].Cells[1].Value);
        string name2=Convert.ToString(g.Rows[2].Cells[1].Value);
        g.CurrentCell=g.Rows[2].Cells[1];
        var move=typeof(MainForm).GetMethods(flags).First(m=>m.Name=="MovePresetGridRow"&&m.GetParameters().Length==4);
        move.Invoke(form,new object[]{g,0,-1,false});
        Check(Convert.ToString(g.Rows[1].Cells[1].Value)==name2,"move up puts selected preset above");
        Check(Convert.ToString(g.Rows[2].Cells[1].Value)==name1,"move up swaps with previous preset");
        g.CurrentCell=g.Rows[1].Cells[1];
        move.Invoke(form,new object[]{g,0,-1,false});
        Check(Convert.ToString(g.Rows[1].Cells[1].Value)==name2,"first preset cannot move above global row");
        g.CurrentCell=g.Rows[1].Cells[1];
        move.Invoke(form,new object[]{g,0,1,false});
        Check(Convert.ToString(g.Rows[1].Cells[1].Value)==name1&&Convert.ToString(g.Rows[2].Cells[1].Value)==name2,"move down restores original preset order");
        var setOrder=(string[])typeof(MainForm).GetField("PresetSetOrder",BindingFlags.NonPublic|BindingFlags.Static).GetValue(null);
        var joinSets=typeof(MainForm).GetMethod("JoinOrderedNames",BindingFlags.NonPublic|BindingFlags.Static);
        Check((string)joinSets.Invoke(null,new object[]{new[]{"Intangible","Fight","Will","Violent","Rage","Blade","Swift"},setOrder})=="Swift,Blade,Rage,Violent,Will,Fight,Intangible","set chips follow dropdown order");
        for(int presetRow=1;presetRow<g.Rows.Count;presetRow++){
          if(g.Rows[presetRow].IsNewRow)continue;
          string prefSets=Convert.ToString(g.Rows[presetRow].Cells[13].Value)??"";
          string accSets=Convert.ToString(g.Rows[presetRow].Cells[14].Value)??"";
          var prefParts=prefSets.Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray();
          var accParts=accSets.Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).ToArray();
          Check(prefSets==(string)joinSets.Invoke(null,new object[]{prefParts,setOrder}),"preferred sets stay in dropdown order");
          Check(accSets==(string)joinSets.Invoke(null,new object[]{accParts,setOrder}),"accepted sets stay in dropdown order");
        }
        using(var slot2=(ContextMenuStrip)typeof(MainForm).GetMethod("CreatePresetMenu",flags).Invoke(form,new object[]{g,1,15})){
          Check(slot2.Items.OfType<ToolStripMenuItem>().Any(x=>x.Text=="Spd")&&!slot2.Items.OfType<ToolStripMenuItem>().Any(x=>x.Text=="CtD%"),"slot 2 menu lists Spd not CtD");
          var current=Convert.ToString(g.Rows[1].Cells[15].Value)??"";
          var selected=current.Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).ToList();
          Check(selected.Count>0,"slot 2 cell has at least one main");
          var marked=slot2.Items.OfType<ToolStripMenuItem>().First(x=>x.Text==selected[0]);
          Check(marked.BackColor.R>marked.BackColor.B,"existing slot 2 main is highlighted");
          bool hadDef=selected.Contains("Def%");
          slot2.Items.OfType<ToolStripMenuItem>().First(x=>x.Text=="Def%").PerformClick();
          bool hasDef=(Convert.ToString(g.Rows[1].Cells[15].Value)??"").Split(',').Select(x=>x.Trim()).Contains("Def%");
          Check(hasDef!=hadDef,"slot 2 menu toggles Def%");
        }
        using(var slot4=(ContextMenuStrip)typeof(MainForm).GetMethod("CreatePresetMenu",flags).Invoke(form,new object[]{g,1,17})){
          Check(slot4.Items.OfType<ToolStripMenuItem>().Any(x=>x.Text=="CtD%")&&!slot4.Items.OfType<ToolStripMenuItem>().Any(x=>x.Text=="Spd"),"slot 4 menu lists CtD not Spd");
        }
        using(var slot6=(ContextMenuStrip)typeof(MainForm).GetMethod("CreatePresetMenu",flags).Invoke(form,new object[]{g,1,19})){
          Check(slot6.Items.OfType<ToolStripMenuItem>().Any(x=>x.Text=="Acc%")&&!slot6.Items.OfType<ToolStripMenuItem>().Any(x=>x.Text=="Spd"),"slot 6 menu lists Acc not Spd");
        }
        using(var acc=(ContextMenuStrip)typeof(MainForm).GetMethod("CreatePresetMenu",flags).Invoke(form,new object[]{g,1,16})){
          Check(acc.Items.OfType<ToolStripMenuItem>().Any(x=>x.Text=="Spd")&&!acc.Items.OfType<ToolStripMenuItem>().Any(x=>x.Text=="CtD%"),"slot 2 accepted menu lists Spd not CtD");
          var prefNow=(Convert.ToString(g.Rows[1].Cells[15].Value)??"").Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).ToList();
          Check(prefNow.Count>0,"slot 2 preferred still has a main");
          string pick=prefNow[0];
          acc.Items.OfType<ToolStripMenuItem>().First(x=>x.Text==pick).PerformClick();
          Check((Convert.ToString(g.Rows[1].Cells[16].Value)??"").Split(',').Select(x=>x.Trim()).Contains(pick),"accepted column receives the moved main");
          Check(!(Convert.ToString(g.Rows[1].Cells[15].Value)??"").Split(',').Select(x=>x.Trim()).Contains(pick),"preferred column loses the moved main");
        }
        Check(g.Columns.Count==21,"preset grid has accepted main columns");
      }
    }
    using(var form=new MainForm()){
      var flags=BindingFlags.Instance|BindingFlags.NonPublic;
      string[] stats={"HP%","Atk%","Def%","Spd","Res%","Acc%","CtR%","CtD%","HP+","Atk+","Def+"};
      using(var window=(Form)typeof(MainForm).GetMethod("CreatePresetWindow",flags).Invoke(form,null)){
        var g=window.Controls.OfType<DataGridView>().First();
        var created=g.Handle;
        int shareRows=g.Rows.Count;
        string shareFirst=Convert.ToString(g.Rows[1].Cells[1].Value);
        string shareSlot2=Convert.ToString(g.Rows[1].Cells[15].Value);
        var lines=(string[])typeof(MainForm).GetMethod("BuildPresetShareLines",flags).Invoke(form,new object[]{g,0,stats});
        Check(lines.Length>=3&&lines[0].StartsWith("RMM-PRESETS"),"export writes share header");
        Check(lines.Any(x=>x.StartsWith("PRESET\t"+shareFirst)),"export includes first preset");
        Check(!(bool)typeof(MainForm).GetMethod("ApplyPresetShare",flags).Invoke(form,new object[]{g,0,stats,new[]{"nope"},false}),"garbage share file is rejected");
        var one=new[]{"PRESET\tShareOnly\tP1,Non,Non,P1,Non,Non,Non,Non,Non,Non,Non\tViolent\tWill\tSpd\tCtD%\tAtk%\tHP%\t\t\t0.8"};
        Check((bool)typeof(MainForm).GetMethod("ApplyPresetShare",flags).Invoke(form,new object[]{g,0,stats,one,false}),"single preset file imports");
        Check(g.Rows.Count==2&&Convert.ToString(g.Rows[1].Cells[1].Value)=="ShareOnly","import replaces grid with shared presets");
        Check(Convert.ToString(g.Rows[1].Cells[15].Value)=="Spd","import keeps preferred slot 2 mains");
        Check((bool)typeof(MainForm).GetMethod("ApplyPresetShare",flags).Invoke(form,new object[]{g,0,stats,lines,false}),"roundtrip export imports back");
        Check(g.Rows.Count==shareRows&&Convert.ToString(g.Rows[1].Cells[1].Value)==shareFirst&&Convert.ToString(g.Rows[1].Cells[15].Value)==shareSlot2,"roundtrip restores original presets");
      }
    }
    using(var form=new MainForm()){
      var flags=BindingFlags.Instance|BindingFlags.NonPublic;
      var snapshot=RuneEngine.Presets.ToList();
      string first=snapshot[0].Name;
      var mapped=new RuneRow{BestBuild=first,RefinementPreset=first,ReevalBestBuild=first};
      typeof(MainForm).GetField("all",flags).SetValue(form,new List<RuneRow>{mapped});
      var swapped=snapshot.ToList();var hold=swapped[0];swapped[0]=swapped[1];swapped[1]=hold;
      var oldNames=snapshot.Select(p=>p.Name).ToList();
      RuneEngine.ReplacePresets(swapped);
      typeof(MainForm).GetMethod("RemapRunePresetNames",flags).Invoke(form,new object[]{oldNames});
      Check(mapped.BestBuild==first&&mapped.RefinementPreset==first&&mapped.ReevalBestBuild==first,"reorder does not remap builds by index");
      snapshot[0].Name="RenamedMovePreset";
      RuneEngine.ReplacePresets(snapshot);
      typeof(MainForm).GetMethod("RemapRunePresetNames",flags).Invoke(form,new object[]{oldNames});
      Check(mapped.BestBuild=="RenamedMovePreset","in-place rename still remaps BestBuild");
      snapshot[0].Name=first;
      RuneEngine.ReplacePresets(snapshot);
    }
    using(var form=new MainForm()){var flags=BindingFlags.Instance|BindingFlags.NonPublic;var icons=(Dictionary<string,System.Drawing.Image>)typeof(MainForm).GetField("setIcons",flags).GetValue(form);foreach(var path in System.IO.Directory.GetFiles("outputs/rune_manager_release/Donnees/assets/sets","*.png"))icons[System.IO.Path.GetFileNameWithoutExtension(path)]=System.Drawing.Image.FromFile(path);using(var window=(Form)typeof(MainForm).GetMethod("CreatePresetWindow",flags).Invoke(form,null)){window.CreateControl();using(var bitmap=new System.Drawing.Bitmap(window.Width,window.Height)){window.PerformLayout();foreach(Control child in window.Controls){var handle=child.Handle;child.DrawToBitmap(bitmap,new System.Drawing.Rectangle(child.Left,child.Top,child.Width,child.Height));}bitmap.Save("outputs/presets-update-preview.png");}var g=window.Controls.OfType<DataGridView>().First();var menuMethod=typeof(MainForm).GetMethod("CreatePresetMenu",flags);using(var menu=(ContextMenuStrip)menuMethod.Invoke(form,new object[]{g,0,14})){var swift=menu.Items.OfType<ToolStripMenuItem>().First(x=>x.Text=="Swift");swift.PerformClick();Check(Convert.ToString(g.Rows[0].Cells[14].Value).Split(',').Contains("Swift")&&!Convert.ToString(g.Rows[0].Cells[13].Value).Split(',').Contains("Swift"),"set moves between preferred and acceptable");swift.PerformClick();Check(!Convert.ToString(g.Rows[0].Cells[14].Value).Split(',').Contains("Swift"),"set removed by unchecking");}Check(g.DefaultCellStyle.BackColor==System.Drawing.Color.Black,"black preset background");Check(g.Columns[0] is DataGridViewComboBoxColumn&&g.Rows.Count==1+RuneEngine.Presets.Count,"preset window rendered with manual factors");}}
    using(var form=new MainForm()){
      var flags=BindingFlags.Instance|BindingFlags.NonPublic;
      using(var window=(Form)typeof(MainForm).GetMethod("CreatePresetWindow",flags).Invoke(form,null)){
        var g=window.Controls.OfType<DataGridView>().First();var handle=g.Handle;
        g.Rows[0].Cells[13].Value="Swift";g.Rows[0].Cells[14].Value="Will";
        using(var menu=(ContextMenuStrip)typeof(MainForm).GetMethod("CreatePresetMenu",flags).Invoke(form,new object[]{g,0,13})){
          var swift=menu.Items.OfType<ToolStripMenuItem>().First(x=>x.Text=="Swift");var will=menu.Items.OfType<ToolStripMenuItem>().First(x=>x.Text=="Will");
          Check(swift.BackColor.R>swift.BackColor.B&&will.BackColor.B>will.BackColor.R,"current sets red and opposite sets blue");
          Check(!menu.ShowCheckMargin&&menu.Items.OfType<ToolStripMenuItem>().All(x=>!x.Checked),"no blue check squares");
          will.PerformClick();Check(will.BackColor.R>will.BackColor.B,"moved set becomes red immediately");
          typeof(ToolStripDropDown).GetMethod("OnClosed",flags).Invoke(menu,new object[]{new ToolStripDropDownClosedEventArgs(ToolStripDropDownCloseReason.AppClicked)});
          Check(!menu.IsDisposed,"menu survives synchronous close processing");Application.DoEvents();Check(menu.IsDisposed,"menu disposed on next UI turn");
        }
      }
    }
    return 0;
  }catch(Exception ex){Console.WriteLine(ex);return 1;}}
}
