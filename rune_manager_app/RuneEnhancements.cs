using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Windows.Forms;

namespace RuneManagerModern {
  public static partial class RuneEngine {
    public static string ExplainPotential(RuneRow source) {
      var p=Presets.FirstOrDefault(x=>x.Name==source.BestBuild);
      if(p==null)return Loc.T("explain_none");
      var r=BestProjection(source,p);var lines=new List<string>();
      lines.Add(Loc.T("explain_head",p.Name,source.Set,source.Slot));
      if(source.Level<12)lines.Add(Loc.T("explain_proj"));
      double points=0;
      foreach(var sub in r.Subs){double w=Weight(p,sub.Stat,r.Set);double grind=SubGrind(sub.Stat,r.Ancient,r.Level>=12);double value=SubPoints(p,r,sub);points+=value;lines.Add(Loc.T("explain_stat",sub.Stat,sub.Value,grind,RollMax(sub.Stat),w.ToString("0.####"),value.ToString("0.####")));}
      double gem=r.Level>=12?GemBonus(r,p):0;points+=gem;lines.Add(Loc.T("explain_gemgain",gem.ToString("0.####")));
      double main=BonusStatPrincipale*((r.Slot==2||r.Slot==4||r.Slot==6)?Math.Max(Weight(p,r.Main,r.Set),.35):.35);points+=main;
      lines.Add(Loc.T("explain_main",main.ToString("0.####"),points.ToString("0.####")));
      double fit=p.Preferred.Contains(r.Set)?1:FacteurSetAcceptable;
      lines.Add(Loc.T("explain_formula",fit.ToString("0.###"),p.ScoreFactor.ToString("0.###"),"1"));
      double raw=Score(r,p),bonus=InventoryBonus(p,r.Set,r.Slot,source.Id);lines.Add(Loc.T("explain_raw",raw.ToString("0.000"),bonus.ToString("0.000"),(raw+bonus).ToString("0.000")));
      lines.Add(Loc.T("explain_stock"));
      var matched=ScoreRules.Where(rule=>(rule.Sets.Count==0||rule.Sets.Any(x=>string.Equals(x,source.Set,StringComparison.OrdinalIgnoreCase)))&&(rule.Slots.Count==0||rule.Slots.Contains(source.Slot))&&(rule.Projected&&string.Equals(rule.Stat,"Spd",StringComparison.OrdinalIgnoreCase)?AchievableSpeed(source):CurrentStatValue(source,rule.Stat))>=rule.Threshold).ToList();
      if(matched.Count>0)lines.Add(Loc.T("explain_rules_hit",string.Join(" ; ",matched.Select(x=>x.Name+" → +"+x.Bonus.ToString("0.###"))),matched.Sum(x=>x.Bonus).ToString("0.###")));
      else lines.Add(Loc.T("explain_rules_none"));
      lines.Add(Loc.T("explain_weights",PoidsP1,PoidsP2,PoidsP3));
      if(Math.Abs(source.Potential-Math.Round(raw+bonus,3))>.001)lines.Add(Loc.T("explain_hist",(source.Potential-raw-bonus).ToString("+0.000;-0.000;0"),source.Potential.ToString("0.000")));
      return string.Join("\r\n",lines);
    }
    public static string ExplainGem(RuneRow source){
      var p=Presets.FirstOrDefault(x=>x.Name==source.BestBuild);if(p==null)return Loc.T("explain_gem_none");
      var r=BestProjection(source,p);string recommendation=Recommend(r,p);
      string reason=Loc.T("preset_colon",p.Name)+"\r\n"+recommendation+"\r\n";
      var sub=r.Subs.FirstOrDefault(x=>x.Stat==r.RecommendSource);
      if(sub!=null&&r.RecommendTarget.Length>0){double value=DisplayedGemMax(r,r.RecommendTarget);if(r.RecommendTarget==sub.Stat&&value<=sub.Value)value=GemMax(r.RecommendTarget,r.Ancient);double before=Contribution(sub.Stat,sub.Value,Weight(p,sub.Stat,r.Set),r.Ancient),after=Contribution(r.RecommendTarget,value,Weight(p,r.RecommendTarget,r.Set),r.Ancient);reason+=Loc.T("explain_gem_contrib",before.ToString("0.####"),after.ToString("0.####"),(after-before).ToString("0.####"))+"\r\n";}
      reason+=Loc.T("explain_gem_how")+"\r\n";
      reason+=r.RecommendationInStock?Loc.T("explain_gem_stock"):Loc.T("explain_gem_nostock");
      if(source.Level<12)reason+="\r\n"+Loc.T("explain_gem_plus12");
      return reason;
    }
  }
  sealed class ExplainCard:Control {
    string[] lines=new string[0];
    readonly Font titleFont=new Font("Segoe UI Semibold",12f);
    readonly Font bodyFont=new Font("Segoe UI",10f);
    readonly Font footFont=new Font("Segoe UI",8.5f);
    readonly Font valueFont=new Font("Segoe UI Semibold",10.5f);
    static readonly Color CardBg=Color.FromArgb(12,20,32),CardBorder=Color.FromArgb(20,184,210),Title=Color.FromArgb(255,178,55),Body=Color.FromArgb(220,228,235),Mute=Color.FromArgb(130,150,165),Value=Color.FromArgb(95,235,165),Score=Color.FromArgb(255,170,40),Row=Color.FromArgb(18,30,46);
    const int CardW=520,Pad=16;
    public ExplainCard(){
      SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.ResizeRedraw,true);
      Visible=false;BackColor=CardBg;ForeColor=Body;Cursor=Cursors.Default;
    }
    public void SetText(string text){
      lines=(text??"").Replace("\r\n","\n").Split('\n');
      Height=Math.Max(80,MeasureHeight());
      Width=CardW;
      Invalidate();
    }
    int MeasureHeight(){
      int y=Pad,w=CardW-Pad*2;
      for(int i=0;i<lines.Length;i++){
        string line=lines[i]??"";
        var font=i==0?titleFont:IsFoot(line)?footFont:bodyFont;
        var sz=TextRenderer.MeasureText(line.Length==0?" ":line,font,new Size(w,int.MaxValue),TextFormatFlags.WordBreak|TextFormatFlags.NoPadding|TextFormatFlags.TextBoxControl);
        y+=Math.Max(font.Height+2,sz.Height)+(i==0?10:4);
        if(i==0)y+=8;
      }
      return y+Pad;
    }
    static bool IsFoot(string line){return line.StartsWith("Poids",StringComparison.Ordinal)||line.StartsWith("Weights",StringComparison.Ordinal)||line.StartsWith("Bonus stock",StringComparison.Ordinal)||line.StartsWith("Stock bonus",StringComparison.Ordinal);}
    static bool IsScore(string line){return line.IndexOf("Score brut",StringComparison.Ordinal)>=0||line.IndexOf("Raw score",StringComparison.Ordinal)>=0;}
    static bool IsStat(string line){return line.IndexOf("poids effectif",StringComparison.Ordinal)>=0||line.IndexOf("effective weight",StringComparison.Ordinal)>=0;}
    protected override void OnPaint(PaintEventArgs e){
      base.OnPaint(e);
      var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
      var box=new Rectangle(0,0,Width-1,Height-1);
      using(var path=Round(box,12)){
        using(var fill=new SolidBrush(CardBg))g.FillPath(fill,path);
        using(var pen=new Pen(CardBorder,1.6f))g.DrawPath(pen,path);
      }
      int y=Pad,w=CardW-Pad*2,x=Pad;
      for(int i=0;i<lines.Length;i++){
        string line=lines[i]??"";
        if(i==0){
          TextRenderer.DrawText(g,line,titleFont,new Rectangle(x,y,w,32),Title,TextFormatFlags.NoPadding|TextFormatFlags.EndEllipsis);
          y+=titleFont.Height+8;
          using(var pen=new Pen(Color.FromArgb(40,70,90)))g.DrawLine(pen,x,y,x+w,y);
          y+=8;
          continue;
        }
        if(line.Length==0){y+=8;continue;}
        Color c=IsFoot(line)?Mute:IsScore(line)?Score:Body;
        var font=IsFoot(line)?footFont:IsScore(line)?valueFont:bodyFont;
        if(IsStat(line)){
          using(var fill=new SolidBrush(Row))g.FillRectangle(fill,new Rectangle(x-6,y-2,w+12,bodyFont.Height+6));
          int eq=line.LastIndexOf('=');
          if(eq>0){
            string left=line.Substring(0,eq).TrimEnd();
            string right=line.Substring(eq).Trim();
            TextRenderer.DrawText(g,left,bodyFont,new Rectangle(x,y,w-88,bodyFont.Height+4),Body,TextFormatFlags.NoPadding|TextFormatFlags.EndEllipsis);
            TextRenderer.DrawText(g,right,valueFont,new Rectangle(x+w-88,y,88,valueFont.Height+4),Value,TextFormatFlags.NoPadding|TextFormatFlags.Right);
            y+=bodyFont.Height+8;
            continue;
          }
        }
        var flags=TextFormatFlags.WordBreak|TextFormatFlags.NoPadding|TextFormatFlags.TextBoxControl;
        var sz=TextRenderer.MeasureText(line,font,new Size(w,int.MaxValue),flags);
        TextRenderer.DrawText(g,line,font,new Rectangle(x,y,w,sz.Height),c,flags);
        y+=sz.Height+4;
      }
    }
    static GraphicsPath Round(Rectangle r,int radius){
      var p=new GraphicsPath();int d=radius*2;
      p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
    protected override void Dispose(bool disposing){
      if(disposing){titleFont.Dispose();bodyFont.Dispose();footFont.Dispose();valueFont.Dispose();}
      base.Dispose(disposing);
    }
  }
  sealed partial class MainForm {
    string WorldBossDataFolder(){
#if WORLD_BOSS_STABLE
      return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"worldboss-main");
#else
      return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RuneManagerModern");
#endif
    }
    WorldBossResult saleProtectionPlan;
    readonly Timer detailDelay=new Timer{Interval=2000},explainHideDelay=new Timer{Interval=220},explainAutoPop=new Timer{Interval=30000};
    readonly ExplainCard explainCard=new ExplainCard();
    int detailRow=-1,detailColumn=-1;
    void RefreshWorldBossSaleProtection(){
      var plan=worldBossLatest??saleProtectionPlan;if(plan==null)plan=saleProtectionPlan=LoadWorldBossPlan();
      RuneEngine.ProtectedWorldBossRuneIds.Clear();if(plan==null)return;
      foreach(var row in plan.Rows.Where(x=>x.Team>=1&&x.Team<=3))foreach(long id in row.CurrentRuneIds)RuneEngine.ProtectedWorldBossRuneIds.Add(id);
      var owners=new HashSet<long>(plan.Rows.Where(x=>x.Team>=1&&x.Team<=3).Select(x=>x.UnitId));foreach(var rune in all)if(rune.Equipped&&owners.Contains(rune.EquippedUnitId))RuneEngine.ProtectedWorldBossRuneIds.Add(rune.Id);
      RuneEngine.ApplyRetentionRules(all);
    }
    string PresetFactorsPath {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"preset-factors.tsv");}}
    string ScoreRulesPath {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"score-rules.tsv");}}
    void LoadScoreRules(){
      if(!File.Exists(ScoreRulesPath))return;
      var loaded=new List<RuneEngine.ScoreRule>();
      foreach(var line in File.ReadAllLines(ScoreRulesPath)){
        var a=line.Split('\t');if(a.Length<6)continue;
        double threshold,bonus;if(!TryDouble(a[3],out threshold)||!TryDouble(a[4],out bonus))continue;
        bool builtIn=a[5]=="1";
        var slots=a.Length>=7?(a[6]??"").Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).Select(x=>{int v;return int.TryParse(x,out v)?v:0;}).Where(x=>x>=1&&x<=6).ToList():new List<int>();
        loaded.Add(new RuneEngine.ScoreRule{Name=a[0],Sets=(a[1]??"").Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).ToList(),Slots=slots,Stat=a[2],Threshold=threshold,Bonus=bonus,BuiltIn=builtIn,Projected=builtIn&&string.Equals(a[2],"Spd",StringComparison.OrdinalIgnoreCase)});
      }
      if(loaded.Count>0)RuneEngine.ScoreRules=loaded;
    }
    void HideExplainSoon(){explainHideDelay.Stop();explainHideDelay.Start();}
    void HideExplainNow(){explainHideDelay.Stop();explainAutoPop.Stop();if(explainCard!=null)explainCard.Visible=false;}
    void PlaceExplainCard(Point gridClient){
      var screen=grid.PointToScreen(gridClient);
      var local=PointToClient(screen);
      int x=Math.Min(local.X,Math.Max(8,ClientSize.Width-explainCard.Width-12));
      int y=local.Y+22;
      if(y+explainCard.Height>ClientSize.Height-8)y=Math.Max(8,local.Y-explainCard.Height-8);
      if(x<8)x=8;
      explainCard.Location=new Point(x,y);
      explainCard.BringToFront();
      explainCard.Visible=true;
    }
    void InstallRuneEnhancements(){
      if(File.Exists(PresetFactorsPath))foreach(var line in File.ReadAllLines(PresetFactorsPath)){var a=line.Split('\t');double v;if(a.Length==2&&double.TryParse(a[1],NumberStyles.Float,CultureInfo.InvariantCulture,out v)){var p=RuneEngine.Presets.FirstOrDefault(x=>x.Name==a[0]);if(p!=null)p.ScoreFactor=Math.Max(0,Math.Min(3,v));}}
      LoadScoreRules();
      if(explainCard.Parent==null){Controls.Add(explainCard);explainCard.MouseEnter+=(s,e)=>explainHideDelay.Stop();explainCard.MouseLeave+=(s,e)=>HideExplainSoon();}
      explainHideDelay.Tick+=(s,e)=>{explainHideDelay.Stop();if(explainCard.Visible&&explainCard.Bounds.Contains(PointToClient(Cursor.Position)))return;var gp=grid.PointToClient(Cursor.Position);var hit=grid.HitTest(gp.X,gp.Y);if(hit.RowIndex==detailRow&&(hit.ColumnIndex==9||hit.ColumnIndex==10))return;HideExplainNow();};
      explainAutoPop.Tick+=(s,e)=>HideExplainNow();
      grid.ShowCellToolTips=false;
      grid.CellMouseEnter+=(s,e)=>{detailDelay.Stop();HideExplainSoon();detailRow=e.RowIndex;detailColumn=e.ColumnIndex;if(e.RowIndex>=0&&(e.ColumnIndex==9||e.ColumnIndex==10))detailDelay.Start();};
      grid.CellMouseLeave+=(s,e)=>{detailDelay.Stop();HideExplainSoon();};
      grid.Scroll+=(s,e)=>{detailDelay.Stop();HideExplainNow();};
      detailDelay.Tick+=(s,e)=>{
        detailDelay.Stop();
        if(detailRow<0||detailRow>=grid.Rows.Count)return;
        var r=grid.Rows[detailRow].DataBoundItem as RuneRow;if(r==null)return;
        string t=detailColumn==9?RuneEngine.ExplainPotential(r):RuneEngine.ExplainGem(r);
        if(viewMode=="refinement")t=Loc.T("refine_tip_head",r.RefinementPotential.ToString("0.000"),r.RefinementGain.ToString("0.000"))+t;
        explainCard.SetText(t);
        PlaceExplainCard(grid.PointToClient(Cursor.Position));
        explainAutoPop.Stop();explainAutoPop.Start();
      };
      FormClosed+=(s,e)=>{detailDelay.Dispose();explainHideDelay.Dispose();explainAutoPop.Dispose();explainCard.Dispose();};
    }
    void ShowPresets(){if(ToggleOffTool(presetButton))return;OpenToolFrom(presetButton,CreatePresetWindow(),1680,true);}
    Form CreatePresetWindow(){
      var f=new Form{Text=Loc.T("preset_win_title"),Icon=Icon,BackColor=Color.Black,ForeColor=Color.White,Size=new Size(1680,650),StartPosition=FormStartPosition.CenterParent};
      var g=new BufferedGrid{Dock=DockStyle.Fill,AllowUserToAddRows=false,RowHeadersVisible=false,BackgroundColor=Color.Black,AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.None,ScrollBars=ScrollBars.None};g.RowTemplate.Height=56;g.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.Black,ForeColor=Color.White,SelectionBackColor=Color.Black};g.EnableHeadersVisualStyles=false;g.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing;g.ColumnHeadersHeight=28;g.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.Black,ForeColor=Color.White};
      var factor=new DataGridViewComboBoxColumn{Name="Factor",HeaderText=Loc.T("preset_global_col"),Width=115};for(int n=0;n<=30;n++)factor.Items.Add((n*10).ToString(CultureInfo.InvariantCulture)+"%");foreach(var p in RuneEngine.Presets){string v=Math.Round(p.ScoreFactor*100).ToString(CultureInfo.InvariantCulture)+"%";if(!factor.Items.Contains(v))factor.Items.Add(v);}g.Columns.Add(factor);
      g.Columns.Add("Preset","Preset");g.Columns[1].ReadOnly=false;g.Columns[1].Width=140;
      string[] stats={"HP%","Atk%","Def%","Spd","Res%","Acc%","CtR%","CtD%","HP+","Atk+","Def+"};foreach(string stat in stats){var c=new DataGridViewComboBoxColumn{HeaderText=stat,Width=62};for(int pct=0;pct<=100;pct+=10)c.Items.Add(pct+"%");g.Columns.Add(c);}
      g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("preset_sets_pref"),Width=280});g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("preset_sets_ok"),Width=280});g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("preset_slot2"),Width=125});g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("preset_slot2_ok"),Width=145});g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("preset_slot4"),Width=125});g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("preset_slot4_ok"),Width=145});g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("preset_slot6"),Width=125});g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("preset_slot6_ok"),Width=145});
      const int controlRow=-1;
      foreach(var p in RuneEngine.Presets)AddPresetDataToGrid(g,p,stats);
      ConfigureBlackPresetGrid(g);
      g.CurrentCellDirtyStateChanged+=(s,e)=>{if(g.IsCurrentCellDirty)g.CommitEdit(DataGridViewDataErrorContexts.Commit);};
      var host=new Panel{Dock=DockStyle.Bottom,Height=50,MinimumSize=new Size(0,48),BackColor=Color.Black};
      var bar=new FlowLayoutPanel{Dock=DockStyle.Fill,WrapContents=true,AutoScroll=false,BackColor=Color.Black,Padding=new Padding(6,6,6,6)};
      Action<Button> style=b=>{b.Height=32;b.Margin=new Padding(3,3,3,3);b.AutoSize=true;b.AutoSizeMode=AutoSizeMode.GrowAndShrink;b.MinimumSize=new Size(88,32);b.BackColor=Color.Black;b.ForeColor=Color.White;b.FlatStyle=FlatStyle.Flat;};
      var save=new Button{Text=Loc.T("save_recalc")};style(save);
      var add=new Button{Text=Loc.T("preset_add")};style(add);
      var remove=new Button{Text=Loc.T("preset_remove")};style(remove);
      var moveUp=new Button{Text=Loc.T("preset_move_up")};style(moveUp);
      var moveDown=new Button{Text=Loc.T("preset_move_down")};style(moveDown);
      var exportBtn=new Button{Text=Loc.T("preset_export")};style(exportBtn);
      var importBtn=new Button{Text=Loc.T("preset_import")};style(importBtn);
      var stock=new Button{Text=Loc.T("preset_stock_btn")};style(stock);
      bar.Controls.Add(save);bar.Controls.Add(add);bar.Controls.Add(remove);bar.Controls.Add(moveUp);bar.Controls.Add(moveDown);bar.Controls.Add(exportBtn);bar.Controls.Add(importBtn);bar.Controls.Add(stock);
      host.Controls.Add(bar);
      add.Click+=(s,e)=>{g.EndEdit();AddPresetGridRow(g,controlRow,stats);};
      remove.Click+=(s,e)=>{g.EndEdit();RemovePresetGridRow(g,controlRow);};
      moveUp.Click+=(s,e)=>{g.EndEdit();MovePresetGridRow(g,controlRow,-1);};
      moveDown.Click+=(s,e)=>{g.EndEdit();MovePresetGridRow(g,controlRow,1);};
      exportBtn.Click+=(s,e)=>{g.EndEdit();ExportPresetShare(g,controlRow,stats);};
      importBtn.Click+=(s,e)=>{g.EndEdit();ImportPresetShare(g,controlRow,stats);};
      stock.Click+=(s,e)=>ShowPresetStock();
      save.Click+=(s,e)=>{g.EndEdit();if(!TryCommitPresetGrid(g,controlRow,stats))return;File.WriteAllLines(PresetFactorsPath,RuneEngine.Presets.Select(p=>p.Name+"\t"+p.ScoreFactor.ToString(CultureInfo.InvariantCulture)));ApplySettingsAndClose(f,Loc.T("presets_globals_saved"));};
      bool fitBusy=false;
      EventHandler fit=(s,e)=>{if(fitBusy||f.IsDisposed||g.IsDisposed||host.IsDisposed)return;if(f.ClientSize.Width<80||f.ClientSize.Height<80)return;fitBusy=true;try{FitPresetLayout(f,g,host,bar);}finally{fitBusy=false;}};
      f.SizeChanged+=fit;g.SizeChanged+=fit;f.Shown+=fit;
      g.RowsAdded+=(s,e)=>fit(s,EventArgs.Empty);
      g.RowsRemoved+=(s,e)=>fit(s,EventArgs.Empty);
      f.Controls.Add(g);f.Controls.Add(host);return f;
    }
    void FitPresetLayout(Form f,DataGridView g,Panel host,FlowLayoutPanel bar){
      if(f==null||g==null||host==null||bar==null)return;
      int bottom=bar.Padding.Top;
      foreach(Control c in bar.Controls){
        if(c==null||!c.Visible)continue;
        int b=c.Bottom+c.Margin.Bottom;
        if(b>bottom)bottom=b;
      }
      int barH=50;
      if(bottom>bar.Padding.Top+8)barH=Math.Min(92,Math.Max(48,bottom+bar.Padding.Bottom+2));
      if(host.MinimumSize.Height>48)host.MinimumSize=new Size(0,48);
      if(Math.Abs(host.Height-barH)>1)host.Height=barH;
      int rows=g.Rows.Count;if(rows<=0)return;
      int[] baseW={88,108,52,52,52,52,52,52,52,52,52,52,52,190,190,86,96,86,96,86,96};
      int n=Math.Min(baseW.Length,g.Columns.Count);
      int sum=0;for(int i=0;i<n;i++)sum+=baseW[i];
      int cw=g.ClientSize.Width;if(cw<200)cw=Math.Max(200,f.ClientSize.Width);
      int used=0;
      for(int i=0;i<n;i++){
        int w=baseW[i]*cw/sum;if(w<24)w=24;
        if(i==n-1)w=Math.Max(24,cw-used);
        if(g.Columns[i].Width!=w)g.Columns[i].Width=w;
        used+=w;
      }
      int headerNow=Math.Max(22,g.ColumnHeadersHeight);
      int bodyGuess=Math.Max(36,g.ClientSize.Height-headerNow-2);
      int rhGuess=Math.Max(28,bodyGuess/rows);
      float fs=rhGuess>=70?9f:rhGuess>=46?8.5f:7.5f;
      if(g.Font==null||Math.Abs(g.Font.Size-fs)>0.2f){
        g.Font=new Font("Segoe UI",fs);
        g.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI Semibold",fs);
        g.DefaultCellStyle.Font=g.Font;
      }
      int hh=Math.Max(22,(int)(fs*2.4f));
      if(g.ColumnHeadersHeight!=hh)g.ColumnHeadersHeight=hh;
      int body=g.ClientSize.Height-g.ColumnHeadersHeight-2;
      if(body<36)body=36;
      int rh=body/rows;if(rh<28)rh=28;
      int extra=body-rh*rows;if(extra<0)extra=0;
      g.RowTemplate.Height=rh;
      for(int i=0;i<g.Rows.Count;i++){
        int h=rh+(i<extra?1:0);
        if(g.Rows[i].Height!=h)g.Rows[i].Height=h;
      }
    }
    void AddPresetGridRow(DataGridView g,int controlRow,string[] stats){
      if(g==null)return;
      var used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      for(int i=controlRow+1;i<g.Rows.Count;i++)used.Add((Convert.ToString(g.Rows[i].Cells[1].Value)??"").Trim());
      int n=used.Count+1;string name=Loc.T("preset_new",n);while(used.Contains(name)){n++;name=Loc.T("preset_new",n);}
      int src=g.Rows.Count-1;if(src<=controlRow)src=-1;
      int row=g.Rows.Add();
      if(src>=0){for(int c=0;c<g.Columns.Count;c++)g.Rows[row].Cells[c].Value=g.Rows[src].Cells[c].Value;}
      else{g.Rows[row].Cells[0].Value="100%";for(int i=0;i<stats.Length;i++)g.Rows[row].Cells[i+2].Value="0%";}
      g.Rows[row].Cells[1].Value=name;g.Rows[row].Cells[1].ReadOnly=false;
      try{g.CurrentCell=g.Rows[row].Cells[1];}catch{}
    }
    void RemovePresetGridRow(DataGridView g,int controlRow){RemovePresetGridRow(g,controlRow,true);}
    void RemovePresetGridRow(DataGridView g,int controlRow,bool confirm){
      if(g==null)return;
      int row=g.CurrentCell==null?-1:g.CurrentCell.RowIndex;
      if(row<=controlRow||row>=g.Rows.Count||g.Rows[row].IsNewRow){
        if(confirm)MessageBox.Show(Loc.T("preset_remove_pick"),Loc.T("preset_win_title"),MessageBoxButtons.OK,MessageBoxIcon.Information);
        return;
      }
      int dataRows=0;for(int i=controlRow+1;i<g.Rows.Count;i++)if(!g.Rows[i].IsNewRow)dataRows++;
      if(dataRows<=1){if(confirm)MessageBox.Show(Loc.T("preset_need_one"),Loc.T("preset_win_title"),MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
      string name=(Convert.ToString(g.Rows[row].Cells[1].Value)??"").Trim();
      if(name.Length==0)name="#"+row.ToString(CultureInfo.InvariantCulture);
      if(confirm&&MessageBox.Show(Loc.T("preset_remove_confirm",name),Loc.T("preset_win_title"),MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
      g.Rows.RemoveAt(row);
    }
    void MovePresetGridRow(DataGridView g,int controlRow,int delta){MovePresetGridRow(g,controlRow,delta,true);}
    void MovePresetGridRow(DataGridView g,int controlRow,int delta,bool warn){
      if(g==null||delta==0)return;
      int row=g.CurrentCell==null?-1:g.CurrentCell.RowIndex;
      if(row<=controlRow||row>=g.Rows.Count||g.Rows[row].IsNewRow){
        if(warn)MessageBox.Show(Loc.T("preset_move_pick"),Loc.T("preset_win_title"),MessageBoxButtons.OK,MessageBoxIcon.Information);
        return;
      }
      int dest=row+delta;
      if(dest<=controlRow||dest>=g.Rows.Count||g.Rows[dest].IsNewRow)return;
      int col=g.CurrentCell==null?1:g.CurrentCell.ColumnIndex;
      SwapPresetGridRows(g,row,dest);
      try{g.CurrentCell=g.Rows[dest].Cells[col];}catch{}
    }
    static void SwapPresetGridRows(DataGridView g,int a,int b){
      int cols=g.Columns.Count;
      var tmp=new object[cols];
      for(int c=0;c<cols;c++)tmp[c]=g.Rows[a].Cells[c].Value;
      for(int c=0;c<cols;c++)g.Rows[a].Cells[c].Value=g.Rows[b].Cells[c].Value;
      for(int c=0;c<cols;c++)g.Rows[b].Cells[c].Value=tmp[c];
    }
    void AddPresetDataToGrid(DataGridView g,Preset p,string[] stats){
      if(g==null||p==null)return;
      string factor=Math.Round(p.ScoreFactor*100).ToString(CultureInfo.InvariantCulture)+"%";
      var col=g.Columns[0] as DataGridViewComboBoxColumn;
      if(col!=null&&!col.Items.Contains(factor))col.Items.Add(factor);
      int i=g.Rows.Add();var cells=g.Rows[i].Cells;
      cells[0].Value=factor;cells[1].Value=p.Name;cells[1].ReadOnly=false;
      for(int n=0;n<stats.Length;n++){double w;string pct=RuneEngine.FormatStatWeight(p.W.TryGetValue(stats[n],out w)?w:0);var statCol=g.Columns[n+2] as DataGridViewComboBoxColumn;if(statCol!=null&&!statCol.Items.Contains(pct))statCol.Items.Add(pct);cells[n+2].Value=pct;}
      cells[13].Value=JoinOrderedNames(p.Preferred,PresetSetOrder);cells[14].Value=JoinOrderedNames(p.Accepted,PresetSetOrder);
      HashSet<string> mains;
      cells[15].Value=p.Main.TryGetValue(2,out mains)?JoinOrderedNames(mains,Slot2Mains):"";
      cells[16].Value=p.MainAccepted.TryGetValue(2,out mains)?JoinOrderedNames(mains,Slot2Mains):"";
      cells[17].Value=p.Main.TryGetValue(4,out mains)?JoinOrderedNames(mains,Slot4Mains):"";
      cells[18].Value=p.MainAccepted.TryGetValue(4,out mains)?JoinOrderedNames(mains,Slot4Mains):"";
      cells[19].Value=p.Main.TryGetValue(6,out mains)?JoinOrderedNames(mains,Slot6Mains):"";
      cells[20].Value=p.MainAccepted.TryGetValue(6,out mains)?JoinOrderedNames(mains,Slot6Mains):"";
    }
    string[] BuildPresetShareLines(DataGridView g,int controlRow,string[] stats){
      var lines=new List<string>();
      lines.Add("RMM-PRESETS\t1");
      for(int i=Math.Max(0,controlRow+1);i<g.Rows.Count;i++){
        if(g.Rows[i].IsNewRow)continue;
        var c=g.Rows[i].Cells;
        string name=(Convert.ToString(c[1].Value)??"").Trim();
        if(name.Length==0)continue;
        var w=new string[stats.Length];for(int n=0;n<stats.Length;n++)w[n]=RuneEngine.FormatStatWeight(RuneEngine.ParseStatWeight(Convert.ToString(c[n+2].Value),stats[n]));
        lines.Add("PRESET\t"+name+"\t"+string.Join(",",w)+"\t"+Convert.ToString(c[13].Value)+"\t"+Convert.ToString(c[14].Value)+"\t"+Convert.ToString(c[15].Value)+"\t"+Convert.ToString(c[17].Value)+"\t"+Convert.ToString(c[19].Value)+"\t"+Convert.ToString(c[16].Value)+"\t"+Convert.ToString(c[18].Value)+"\t"+Convert.ToString(c[20].Value)+"\t"+ParsePresetFactor(Convert.ToString(c[0].Value)).ToString(CultureInfo.InvariantCulture));
      }
      return lines.ToArray();
    }
    bool TryReadPresetShare(string[] lines,out List<Preset> presets,out Dictionary<string,double> statFactors){
      presets=new List<Preset>();
      statFactors=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
      var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      if(lines==null)return false;
      for(int i=0;i<lines.Length;i++){
        string line=lines[i]==null?"":lines[i].Trim();
        if(line.Length==0||line[0]=='#')continue;
        string[] x=line.Split('\t');
        if(x.Length==0)continue;
        if(x[0]=="STATFACTOR"&&x.Length>=2){
          foreach(string pair in x[1].Split(',')){
            int eq=pair.IndexOf('=');if(eq<=0)continue;
            double v;if(TryDouble(pair.Substring(eq+1),out v))statFactors[pair.Substring(0,eq)]=v;
          }
        }else if(x.Length>=8&&x[0]=="PRESET"){
          string name=(x[1]??"").Trim();
          if(name.Length==0||!seen.Add(name))continue;
          var p=x.Length>=11?RuneEngine.MakePreset(x[1],x[2].Split(','),x[3],x[4],x[5],x[6],x[7],x[8],x[9],x[10]):RuneEngine.MakePreset(x[1],x[2].Split(','),x[3],x[4],x[5],x[6],x[7]);
          if(x.Length>=12)p.ScoreFactor=ParsePresetFactor(x[11]);
          presets.Add(p);
        }else if(x[0]=="FACTOR"&&x.Length>=3){
          for(int n=0;n<presets.Count;n++)if(string.Equals(presets[n].Name,x[1],StringComparison.OrdinalIgnoreCase)){presets[n].ScoreFactor=ParsePresetFactor(x[2]);break;}
        }
      }
      return presets.Count>0;
    }
    bool ApplyPresetShare(DataGridView g,int controlRow,string[] stats,string[] lines,bool confirm){
      List<Preset> presets;Dictionary<string,double> statFactors;
      if(!TryReadPresetShare(lines,out presets,out statFactors)){
        if(confirm)MessageBox.Show(Loc.T("preset_import_none"),Loc.T("preset_import_title"),MessageBoxButtons.OK,MessageBoxIcon.Warning);
        return false;
      }
      if(confirm&&MessageBox.Show(Loc.T("preset_import_confirm"),Loc.T("preset_import_title"),MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return false;
      while(g.Rows.Count>Math.Max(0,controlRow+1))g.Rows.RemoveAt(g.Rows.Count-1);
      for(int i=0;i<presets.Count;i++)AddPresetDataToGrid(g,presets[i],stats);
      try{if(g.Rows.Count>Math.Max(0,controlRow+1))g.CurrentCell=g.Rows[Math.Max(0,controlRow+1)].Cells[1];}catch{}
      return true;
    }
    void ExportPresetShare(DataGridView g,int controlRow,string[] stats){
      using(var d=new SaveFileDialog{Filter=Loc.T("preset_share_filter"),Title=Loc.T("preset_export_title"),FileName="rune-manager-presets.tsv",OverwritePrompt=true}){
        if(d.ShowDialog()!=DialogResult.OK)return;
        try{File.WriteAllLines(d.FileName,BuildPresetShareLines(g,controlRow,stats));status.Text=Loc.T("preset_export_ok");}
        catch(Exception ex){MessageBox.Show(Loc.T("save_settings_fail",ex.Message),Loc.T("preset_export_title"),MessageBoxButtons.OK,MessageBoxIcon.Error);}
      }
    }
    void ImportPresetShare(DataGridView g,int controlRow,string[] stats){
      using(var d=new OpenFileDialog{Filter=Loc.T("preset_share_filter"),Title=Loc.T("preset_import_title")}){
        if(d.ShowDialog()!=DialogResult.OK)return;
        string[] lines;
        try{lines=File.ReadAllLines(d.FileName);}
        catch(Exception ex){MessageBox.Show(Loc.T("save_settings_fail",ex.Message),Loc.T("preset_import_title"),MessageBoxButtons.OK,MessageBoxIcon.Error);return;}
        if(ApplyPresetShare(g,controlRow,stats,lines,true))status.Text=Loc.T("preset_import_ok",g.Rows.Count-controlRow-1);
      }
    }
    bool TryCommitPresetGrid(DataGridView g,int controlRow,string[] stats){
      var next=new List<Preset>();var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      for(int i=Math.Max(0,controlRow+1);i<g.Rows.Count;i++){
        if(g.Rows[i].IsNewRow)continue;
        var c=g.Rows[i].Cells;
        string name=(Convert.ToString(c[1].Value)??"").Trim();
        if(name.Length==0){MessageBox.Show(Loc.T("preset_name_empty"),Loc.T("preset_win_title"),MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;}
        if(!seen.Add(name)){MessageBox.Show(Loc.T("preset_name_dup",name),Loc.T("preset_win_title"),MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;}
        var w=new string[stats.Length];for(int n=0;n<stats.Length;n++)w[n]=RuneEngine.FormatStatWeight(RuneEngine.ParseStatWeight(Convert.ToString(c[n+2].Value),stats[n]));
        var p=RuneEngine.MakePreset(name,w,Convert.ToString(c[13].Value),Convert.ToString(c[14].Value),Convert.ToString(c[15].Value),Convert.ToString(c[17].Value),Convert.ToString(c[19].Value),Convert.ToString(c[16].Value),Convert.ToString(c[18].Value),Convert.ToString(c[20].Value));
        p.ScoreFactor=ParsePresetFactor(Convert.ToString(c[0].Value));
        next.Add(p);
      }
      if(next.Count==0){MessageBox.Show(Loc.T("preset_need_one"),Loc.T("preset_win_title"),MessageBoxButtons.OK,MessageBoxIcon.Warning);return false;}
      var oldNames=RuneEngine.Presets.Select(x=>x.Name).ToList();
      RuneEngine.ReplacePresets(next);
      RemapRunePresetNames(oldNames);
      return true;
    }
    void RemapRunePresetNames(List<string> oldNames){
      if(oldNames==null||all==null)return;
      var newNames=new List<string>();
      for(int i=0;i<RuneEngine.Presets.Count;i++)newNames.Add(RuneEngine.Presets[i].Name);
      var newSet=new HashSet<string>(newNames,StringComparer.OrdinalIgnoreCase);
      var oldSet=new HashSet<string>(oldNames,StringComparer.OrdinalIgnoreCase);
      var map=new Dictionary<string,string>(StringComparer.OrdinalIgnoreCase);
      int n=Math.Min(oldNames.Count,newNames.Count);
      for(int i=0;i<n;i++){
        string from=oldNames[i],to=newNames[i];
        if(string.IsNullOrEmpty(from)||string.IsNullOrEmpty(to))continue;
        if(string.Equals(from,to,StringComparison.Ordinal))continue;
        if(newSet.Contains(from)||oldSet.Contains(to))continue;
        map[from]=to;
      }
      if(map.Count==0)return;
      foreach(var r in all){
        string next;
        if(!string.IsNullOrEmpty(r.BestBuild)&&map.TryGetValue(r.BestBuild,out next))r.BestBuild=next;
        if(!string.IsNullOrEmpty(r.RefinementPreset)&&map.TryGetValue(r.RefinementPreset,out next))r.RefinementPreset=next;
        if(!string.IsNullOrEmpty(r.ReevalBestBuild)&&map.TryGetValue(r.ReevalBestBuild,out next))r.ReevalBestBuild=next;
      }
    }
    void ShowPresetStock(){
      var f=new Form{Text=Loc.T("preset_stock_title"),Size=new Size(980,680),StartPosition=FormStartPosition.CenterParent};var g=new BufferedGrid{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,RowHeadersVisible=false,AutoGenerateColumns=false};g.Columns.Add(new DataGridViewImageColumn{HeaderText="Set",Width=40,ImageLayout=DataGridViewImageCellLayout.Zoom});g.Columns.Add("SetName","Set");for(int n=1;n<=6;n++)g.Columns.Add("S"+n,"Slot "+n+" : stock / bonus");
      foreach(string setName in RuneEngine.AutoKeepThresholds.Keys.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase)){int[] c;if(!RuneEngine.PresetSlotCounts.TryGetValue(setName,out c)||c==null)c=new int[6];var values=new List<object>{GetSetIcon(setName),setName};for(int n=1;n<=6;n++)values.Add(c[n-1]+(c[n-1]<=RuneEngine.StockSlotTarget?" / +1":" / top "+RuneEngine.StockSlotTarget+" +1"));g.Rows.Add(values.ToArray());}g.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.AllCells;f.Controls.Add(g);f.ShowDialog(this);
    }
    // Bouton "Règles" : liste toutes les regles de bonus/malus pur (RuneEngine.ScoreRules),
    // appliquees une fois sur le Potential final (voir RuneEngine.RuleBonus), INDEPENDANTES
    // du preset choisi. Les 3 regles Spd 23/25/27 (Will/Despair/Violent/Swift) sont marquees
    // "Système" : modifiables (seuil/bonus/sets) mais pas supprimables. Jeremy peut ajouter
    // ses propres regles "Perso" (n'importe quelle stat, seuil et bonus), modifiables et
    // supprimables librement.
    void ShowScoreRules(){using(var f=CreateScoreRulesWindow())f.ShowDialog(this);}
    Form CreateScoreRulesWindow(){
      var f=new Form{Text="Règles • bonus/malus pur sur le score final",Icon=Icon,BackColor=Color.Black,ForeColor=Color.White,Size=new Size(1100,520),StartPosition=FormStartPosition.CenterParent};
      var g=new BufferedGrid{Dock=DockStyle.Fill,AllowUserToAddRows=false,RowHeadersVisible=false,BackgroundColor=Color.Black,AutoGenerateColumns=false};
      g.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.Black,ForeColor=Color.White,SelectionBackColor=Color.FromArgb(35,35,70),SelectionForeColor=Color.White};g.EnableHeadersVisualStyles=false;g.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.Black,ForeColor=Color.White};
      g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Nom",Width=220});
      g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Sets (vide = tous)",Width=220});
      g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Slots (vide = tous)",Width=160,ReadOnly=true});
      var statCol=new DataGridViewComboBoxColumn{HeaderText="Stat",Width=75};statCol.Items.AddRange("HP%","Atk%","Def%","Spd","Res%","Acc%","CtR%","CtD%","HP+","Atk+","Def+");g.Columns.Add(statCol);
      g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Seuil (≥)",Width=75});
      g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Bonus score",Width=85});
      g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Type",Width=85,ReadOnly=true});
      var delCol=new DataGridViewButtonColumn{HeaderText="",Text="Suppr",UseColumnTextForButtonValue=true,Width=65};g.Columns.Add(delCol);
      g.RowTemplate.Height=50;
      // Colonne "Sets" : au lieu du texte brut "Will,Despair,Violent,Swift", on dessine des
      // puces avec l'icone du set (meme rendu que les colonnes Preferes/Acceptables de la
      // fenetre Presets, PresetMenus.cs). L'edition reste au clavier (texte separe par des
      // virgules) ; ce CellPainting ne change que l'affichage hors edition.
      int setsCol=1;
      g.CellPainting+=(s,e)=>{
        if(e.RowIndex<0||e.ColumnIndex!=setsCol||g.IsCurrentCellInEditMode&&g.CurrentCellAddress.X==setsCol&&g.CurrentCellAddress.Y==e.RowIndex)return;
        e.PaintBackground(e.ClipBounds,true);
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;e.Graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
        int x=e.CellBounds.X+3,y=e.CellBounds.Y+3;
        foreach(string name in Convert.ToString(e.Value).Split(',').Select(t=>t.Trim()).Where(t=>t.Length>0)){
          int w=TextRenderer.MeasureText(name,g.Font).Width+28;
          if(x+w>e.CellBounds.Right-4){x=e.CellBounds.X+3;y+=22;}
          if(y+22>e.CellBounds.Bottom)break;
          using(var chipBrush=new SolidBrush(Color.FromArgb(50,50,50)))e.Graphics.FillRectangle(chipBrush,new Rectangle(x,y,w-4,20));
          var icon=GetSetIcon(name);if(icon!=null)e.Graphics.DrawImage(icon,new Rectangle(x,y,20,20));
          TextRenderer.DrawText(e.Graphics,name,g.Font,new Point(x+22,y+2),Color.White);
          x+=w;
        }
        e.Paint(e.ClipBounds,DataGridViewPaintParts.Border);e.Handled=true;
      };
      // Colonne "Sets" : plus d'edition texte libre — un clic ouvre un menu deroulant a choix
      // multiples (icone + nom, meme rendu que CreatePresetMenu dans PresetMenus.cs) avec une
      // option "Tous les sets" en haut qui vide la selection (vide = regle valable sur tous les
      // sets, comportement deja existant de RuneEngine.ScoreRule.Sets).
      string[] setNames=PresetSetOrder;
      g.Columns[setsCol].ReadOnly=true;
      Action<int> openSetsMenu=row=>{
        var cell=g.Rows[row].Cells[setsCol];
        var menu=new ContextMenuStrip{BackColor=Color.Black,ForeColor=Color.White,ShowCheckMargin=false,ShowImageMargin=true,Renderer=new PresetMenuRenderer(),Font=new Font("Segoe UI",10),ImageScalingSize=new Size(22,22)};
        var allItem=new ToolStripMenuItem("— Tous les sets —"){ForeColor=Color.White,BackColor=Color.FromArgb(45,45,45)};
        allItem.Click+=(s,e)=>{cell.Value="";g.InvalidateRow(row);};
        menu.Items.Add(allItem);menu.Items.Add(new ToolStripSeparator());
        foreach(string setName0 in setNames){
          string setName=setName0;
          var item=new ToolStripMenuItem(setName,GetSetIcon(setName)){ForeColor=Color.White,BackColor=CellSets(cell).Contains(setName)?Color.FromArgb(25,65,125):Color.Black};
          item.Click+=(s,e)=>{
            var current=CellSets(cell);
            if(current.Contains(setName))current.Remove(setName);else current.Add(setName);
            cell.Value=string.Join(",",setNames.Where(current.Contains));
            item.BackColor=current.Contains(setName)?Color.FromArgb(25,65,125):Color.Black;
            g.InvalidateRow(row);
          };
          menu.Items.Add(item);
        }
        menu.Closing+=(s,e)=>{if(e.CloseReason==ToolStripDropDownCloseReason.ItemClicked)e.Cancel=true;};
        EventHandler cleanup=(s,e)=>menu.Dispose();g.Disposed+=cleanup;menu.Disposed+=(s,e)=>g.Disposed-=cleanup;
        menu.Closed+=(s,e)=>{if(g.IsHandleCreated&&!g.IsDisposed)g.BeginInvoke((MethodInvoker)delegate{if(!menu.IsDisposed)menu.Dispose();});};
        var rect=g.GetCellDisplayRectangle(setsCol,row,true);menu.Show(g,new Point(rect.Left,rect.Top+Math.Min(30,rect.Height)));
      };
      g.CellClick+=(s,e)=>{if(e.RowIndex>=0&&e.ColumnIndex==setsCol)openSetsMenu(e.RowIndex);};
      g.KeyDown+=(s,e)=>{if((e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter||e.KeyCode==Keys.F4)&&g.CurrentCell!=null&&g.CurrentCell.ColumnIndex==setsCol){openSetsMenu(g.CurrentCell.RowIndex);e.Handled=true;e.SuppressKeyPress=true;}};
      // Colonne "Slots" : meme principe que "Sets" — menu deroulant a choix multiples (slot 1 a
      // 6 individuellement, ou "Tous les slots" en une fois pour vider la selection). Vide =
      // regle valable sur tous les slots (comportement par defaut, compatible avec les regles
      // deja enregistrees avant l'ajout de cette colonne).
      int slotCol=2;
      g.CellPainting+=(s,e)=>{
        if(e.RowIndex<0||e.ColumnIndex!=slotCol||g.IsCurrentCellInEditMode&&g.CurrentCellAddress.X==slotCol&&g.CurrentCellAddress.Y==e.RowIndex)return;
        e.PaintBackground(e.ClipBounds,true);
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
        int x=e.CellBounds.X+3,y=e.CellBounds.Y+3;
        string raw=Convert.ToString(e.Value);
        string[] labels=string.IsNullOrEmpty(raw)?new[]{"Tous"}:raw.Split(',').Select(t=>t.Trim()).Where(t=>t.Length>0).Select(t=>"Slot "+t).ToArray();
        foreach(string label in labels){
          int w=TextRenderer.MeasureText(label,g.Font).Width+16;
          if(x+w>e.CellBounds.Right-4){x=e.CellBounds.X+3;y+=22;}
          if(y+22>e.CellBounds.Bottom)break;
          using(var chipBrush=new SolidBrush(string.IsNullOrEmpty(raw)?Color.FromArgb(40,60,40):Color.FromArgb(50,50,50)))e.Graphics.FillRectangle(chipBrush,new Rectangle(x,y,w-4,20));
          TextRenderer.DrawText(e.Graphics,label,g.Font,new Point(x+8,y+2),Color.White);
          x+=w;
        }
        e.Paint(e.ClipBounds,DataGridViewPaintParts.Border);e.Handled=true;
      };
      Action<int> openSlotsMenu=row=>{
        var cell=g.Rows[row].Cells[slotCol];
        var menu=new ContextMenuStrip{BackColor=Color.Black,ForeColor=Color.White,ShowCheckMargin=false,Renderer=new PresetMenuRenderer(),Font=new Font("Segoe UI",10)};
        var allItem=new ToolStripMenuItem("— Tous les slots —"){ForeColor=Color.White,BackColor=Color.FromArgb(45,45,45)};
        allItem.Click+=(s,e)=>{cell.Value="";g.InvalidateRow(row);};
        menu.Items.Add(allItem);menu.Items.Add(new ToolStripSeparator());
        for(int slotNum=1;slotNum<=6;slotNum++){
          int slot=slotNum;string slotText=slot.ToString();
          var item=new ToolStripMenuItem("Slot "+slot){ForeColor=Color.White,BackColor=CellSets(cell).Contains(slotText)?Color.FromArgb(25,65,125):Color.Black};
          item.Click+=(s,e)=>{
            var current=CellSets(cell);
            if(current.Contains(slotText))current.Remove(slotText);else current.Add(slotText);
            cell.Value=string.Join(",",Enumerable.Range(1,6).Select(x=>x.ToString()).Where(current.Contains));
            item.BackColor=current.Contains(slotText)?Color.FromArgb(25,65,125):Color.Black;
            g.InvalidateRow(row);
          };
          menu.Items.Add(item);
        }
        menu.Closing+=(s,e)=>{if(e.CloseReason==ToolStripDropDownCloseReason.ItemClicked)e.Cancel=true;};
        EventHandler cleanup=(s,e)=>menu.Dispose();g.Disposed+=cleanup;menu.Disposed+=(s,e)=>g.Disposed-=cleanup;
        menu.Closed+=(s,e)=>{if(g.IsHandleCreated&&!g.IsDisposed)g.BeginInvoke((MethodInvoker)delegate{if(!menu.IsDisposed)menu.Dispose();});};
        var rect=g.GetCellDisplayRectangle(slotCol,row,true);menu.Show(g,new Point(rect.Left,rect.Top+Math.Min(30,rect.Height)));
      };
      g.CellClick+=(s,e)=>{if(e.RowIndex>=0&&e.ColumnIndex==slotCol)openSlotsMenu(e.RowIndex);};
      g.KeyDown+=(s,e)=>{if((e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter||e.KeyCode==Keys.F4)&&g.CurrentCell!=null&&g.CurrentCell.ColumnIndex==slotCol){openSlotsMenu(g.CurrentCell.RowIndex);e.Handled=true;e.SuppressKeyPress=true;}};
      Action<RuneEngine.ScoreRule> addRow=rule=>{
        int i=g.Rows.Add();var c=g.Rows[i].Cells;
        c[0].Value=rule.Name;c[1].Value=string.Join(",",rule.Sets);c[2].Value=string.Join(",",rule.Slots);c[3].Value=rule.Stat;c[4].Value=rule.Threshold.ToString(CultureInfo.InvariantCulture);c[5].Value=rule.Bonus.ToString(CultureInfo.InvariantCulture);c[6].Value=rule.BuiltIn?"Système":"Perso";
        if(rule.BuiltIn)g.Rows[i].DefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(35,35,70),SelectionBackColor=Color.FromArgb(45,45,90),SelectionForeColor=Color.White};
      };
      foreach(var rule in RuneEngine.ScoreRules)addRow(rule);
      g.CellContentClick+=(s,e)=>{
        if(e.RowIndex<0||e.ColumnIndex!=delCol.Index)return;
        if(Convert.ToString(g.Rows[e.RowIndex].Cells[6].Value)=="Système"){status.Text="Règle système : modifiable, pas supprimable.";return;}
        g.Rows.RemoveAt(e.RowIndex);
      };
      g.CurrentCellDirtyStateChanged+=(s,e)=>{if(g.IsCurrentCellDirty)g.CommitEdit(DataGridViewDataErrorContexts.Commit);};
      var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=62,BackColor=Color.Black,WrapContents=true,Padding=new Padding(8,10,8,8)};
      var add=new Button{Text="AJOUTER UNE RÈGLE",AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(160,36),Height=36,Margin=new Padding(4),BackColor=Color.Black,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
      var save=new Button{Text="ENREGISTRER ET RECALCULER",AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(200,36),Height=36,Margin=new Padding(4),BackColor=Color.Black,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
      bar.Controls.Add(add);bar.Controls.Add(save);AttachWrapBar(bar);
      add.Click+=(s,e)=>addRow(new RuneEngine.ScoreRule{Name="Nouvelle règle",Sets=new List<string>(),Slots=new List<int>(),Stat="Spd",Threshold=20,Bonus=.5,BuiltIn=false,Projected=false});
      save.Click+=(s,e)=>{
        g.EndEdit();
        var rules=new List<RuneEngine.ScoreRule>();
        foreach(DataGridViewRow row in g.Rows){
          var c=row.Cells;bool builtIn=Convert.ToString(c[6].Value)=="Système";
          double threshold,bonus;if(!TryDouble(Convert.ToString(c[4].Value),out threshold))threshold=0;if(!TryDouble(Convert.ToString(c[5].Value),out bonus))bonus=0;
          string stat=Convert.ToString(c[3].Value);if(string.IsNullOrEmpty(stat))stat="Spd";
          string name=Convert.ToString(c[0].Value);if(string.IsNullOrEmpty(name))name="Règle";
          var slots=(Convert.ToString(c[2].Value)??"").Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).Select(x=>{int v;return int.TryParse(x,out v)?v:0;}).Where(x=>x>=1&&x<=6).ToList();
          rules.Add(new RuneEngine.ScoreRule{Name=name,Sets=(Convert.ToString(c[1].Value)??"").Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0).ToList(),Slots=slots,Stat=stat,Threshold=threshold,Bonus=bonus,BuiltIn=builtIn,Projected=builtIn&&string.Equals(stat,"Spd",StringComparison.OrdinalIgnoreCase)});
        }
        RuneEngine.ScoreRules=rules;
        File.WriteAllLines(ScoreRulesPath,rules.Select(r=>r.Name+"\t"+string.Join(",",r.Sets)+"\t"+r.Stat+"\t"+r.Threshold.ToString(CultureInfo.InvariantCulture)+"\t"+r.Bonus.ToString(CultureInfo.InvariantCulture)+"\t"+(r.BuiltIn?"1":"0")+"\t"+string.Join(",",r.Slots)));
        ApplySettingsAndClose(f,"Règles enregistrées et scores recalculés.");
      };
      f.Controls.Add(g);f.Controls.Add(bar);bar.BringToFront();return f;
    }
  }
}
