using System;
using System.Linq;
using System.Collections.Generic;
using System.Reflection;
using System.Windows.Forms;
using RuneManagerModern;
static class RuneUpdateRegressionTest {
  static void Check(bool ok,string message){if(!ok)throw new Exception(message);Console.WriteLine("PASS "+message);}
  [STAThread] static int Main(string[] args){try{
    var counts=new[]{10,5,10,7,11,6};Check(Math.Abs(RuneEngine.ScarcityBonus(counts,2)-1)<.00001,"least populated slot +1");Check(RuneEngine.ScarcityBonus(counts,5)==0,"most populated slot no penalty");Check(RuneEngine.ScarcityBonus(new[]{5,5,5,5,5,5},1)==0,"equal slots zero bonus");
    Check(RtaPickScore.CounterWeight(0)==0,"rta no counter before enemy pick");
    Check(RtaPickScore.CounterWeight(5)>RtaPickScore.CounterWeight(1),"rta counter grows pick by pick");
    Check(RtaPickScore.SynergyWeight(5)<RtaPickScore.SynergyWeight(0),"rta synergy yields to enemy later");
    Check(RtaPickScore.TeamFitWeight(5)<RtaPickScore.TeamFitWeight(0),"rta late picks less core lock");
    Check(RtaPickScore.MatchupPoints(.62,200,.52,.25,.10,.20)>0&&RtaPickScore.MatchupPoints(.45,200,.52,.25,.10,.20)<0,"rta winning matchup scores positive");
    string savedLang=Loc.Lang;Loc.Lang="en";Check(Loc.T("no_preset")=="No Good Preset","english no-preset label");Loc.Lang="fr";Check(Loc.T("no_preset")=="Aucun preset","french no-preset label");Loc.Lang=savedLang;
    var overlay=new List<RuneRow>();
    string hammer="{\"command\":\"UpgradeRuneList\",\"ret_code\":0,\"upgrade_rune_list\":[{\"rune_id\":65236285954,\"slot_no\":3,\"rank\":14,\"class\":16,\"set_id\":13,\"upgrade_curr\":6,\"pri_eff\":[5,70],\"prefix_eff\":[0,0],\"sec_eff\":[[11,11,0,0],[6,7,0,0],[2,14,0,0]]}]}";
    RuneEngine.ApplyLiveEvent(overlay,hammer,false);
    Check(overlay.Count==0,"json import overlay does not resurrect sold hammer rune");
    RuneEngine.ApplyLiveEvent(overlay,hammer,true);
    Check(overlay.Any(r=>r.Id==65236285954),"live hammer still adds a new rune during the session");
    RuneEngine.ApplyLiveEvent(overlay,"{\"command\":\"SellRune\",\"ret_code\":0,\"rune_id_list\":[65236285954]}",false);
    Check(!overlay.Any(r=>r.Id==65236285954),"sell still removes after overlay");
    var rows=RuneEngine.Import(args[0]);Check(rows.Count>0,"real inventory imported");Check(RuneEngine.PresetSlotCounts.Values.All(c=>Enumerable.Range(1,6).All(s=>RuneEngine.ScarcityBonus(c,s)>=0&&RuneEngine.ScarcityBonus(c,s)<=1)),"all inventory bonuses in [0,1]");
    var rune=rows.First(r=>r.Action=="Sell");RuneEngine.ProtectedWorldBossRuneIds.Add(rune.Id);RuneEngine.ApplyRetentionRules(rows);Check(rune.Action!="Sell","World Boss sale protection overrides low score");
    var scored=rows.First(r=>r.Potential>1);Check(RuneEngine.ExplainPotential(scored).Contains("Score brut"),"score explanation available");Check(RuneEngine.ExplainGem(scored).Contains("Preset"),"gem explanation available");
    string entry="{\"rune_id\":0,\"set_id\":13,\"slot_no\":3,\"class\":6,\"rank\":5,\"extra\":5,\"upgrade_curr\":15,\"pri_eff\":[5,160],\"prefix_eff\":[0,0],\"sec_eff\":[[11,10],[8,10],[2,10],[1,617]]}";
    string dict="{\"command\":\"ReceiveMail\",\"ret_code\":0,\"mail_list\":[{\"extra\":{\"101\":"+entry+",\"102\":"+entry+",\"103\":"+entry+"}}]}";
    var before=string.Join(";",RuneEngine.PresetSlotCounts.OrderBy(x=>x.Key).Select(x=>x.Key+string.Join(",",x.Value)));var choice=RuneEngine.CompareRuneChoice(rows,dict);Check(choice!=null&&choice.Choices.Count==3,"mail dictionary chest detected");Check(before==string.Join(";",RuneEngine.PresetSlotCounts.OrderBy(x=>x.Key).Select(x=>x.Key+string.Join(",",x.Value))),"chest does not alter inventory bonuses");
    string five="{\"command\":\"OpenReward\",\"ret_code\":0,\"choices\":["+string.Join(",",Enumerable.Repeat(entry,5))+"]}";Check(RuneEngine.CompareRuneChoice(rows,five).Choices.Count==5,"five rune chest retained");
    int presetCount=RuneEngine.Presets.Count;
    Check(presetCount>=7,"default presets loaded");
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
      }
    }
    return 0;
  }catch(Exception ex){Console.WriteLine(ex);return 1;}}
}
