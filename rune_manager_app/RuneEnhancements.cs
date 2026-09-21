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
      foreach(var sub in r.Subs){double w=Weight(p,sub.Stat,r.Set);double grind=r.Level>=12?GrindMax(sub.Stat,r.Ancient):0;double value=w*(sub.Value+grind)/RollMax(sub.Stat);points+=value;lines.Add(Loc.T("explain_stat",sub.Stat,sub.Value,grind,RollMax(sub.Stat),w.ToString("0.####"),value.ToString("0.####")));}
      double gem=r.Level>=12?GemBonus(r,p):0;points+=gem;lines.Add(Loc.T("explain_gemgain",gem.ToString("0.####")));
      double main=BonusStatPrincipale*((r.Slot==2||r.Slot==4||r.Slot==6)?Math.Max(Weight(p,r.Main,r.Set),.35):.35);points+=main;
      lines.Add(Loc.T("explain_main",main.ToString("0.####"),points.ToString("0.####")));
      double fit=p.Preferred.Contains(r.Set)?1:FacteurSetAcceptable,spdF;if(!StatGlobalFactor.TryGetValue("Spd",out spdF))spdF=1.1;
      lines.Add(Loc.T("explain_formula",fit.ToString("0.###"),p.ScoreFactor.ToString("0.###"),spdF.ToString("0.###")));
      double raw=Score(r,p),bonus=InventoryBonus(p,r.Set,r.Slot);lines.Add(Loc.T("explain_raw",raw.ToString("0.000"),bonus.ToString("0.000"),(raw+bonus).ToString("0.000")));
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
  sealed partial class MainForm {
    string WorldBossDataFolder(){
#if WORLD_BOSS_STABLE
      return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"worldboss-main");
#else
      return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RuneManagerModern");
#endif
    }
    WorldBossResult saleProtectionPlan;
    readonly Timer detailDelay=new Timer{Interval=2000};
    readonly ToolTip detailTip=new ToolTip{AutoPopDelay=30000,InitialDelay=2000,ReshowDelay=2000,ShowAlways=true};
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
    void InstallRuneEnhancements(){
      if(File.Exists(PresetFactorsPath))foreach(var line in File.ReadAllLines(PresetFactorsPath)){var a=line.Split('\t');double v;if(a.Length==2&&double.TryParse(a[1],NumberStyles.Float,CultureInfo.InvariantCulture,out v)){var p=RuneEngine.Presets.FirstOrDefault(x=>x.Name==a[0]);if(p!=null)p.ScoreFactor=Math.Max(0,Math.Min(3,v));}}
      LoadScoreRules();
      grid.ShowCellToolTips=false;
      grid.CellMouseEnter+=(s,e)=>{detailDelay.Stop();detailTip.Hide(grid);detailRow=e.RowIndex;detailColumn=e.ColumnIndex;if(e.RowIndex>=0&&(e.ColumnIndex==9||e.ColumnIndex==10))detailDelay.Start();};
      grid.CellMouseLeave+=(s,e)=>{detailDelay.Stop();detailTip.Hide(grid);};grid.Scroll+=(s,e)=>{detailDelay.Stop();detailTip.Hide(grid);};
      detailDelay.Tick+=(s,e)=>{detailDelay.Stop();if(detailRow<0||detailRow>=grid.Rows.Count)return;var r=grid.Rows[detailRow].DataBoundItem as RuneRow;if(r==null)return;string t=detailColumn==9?RuneEngine.ExplainPotential(r):RuneEngine.ExplainGem(r);if(viewMode=="refinement")t=Loc.T("refine_tip_head",r.RefinementPotential.ToString("0.000"),r.RefinementGain.ToString("0.000"))+t;var pt=grid.PointToClient(Cursor.Position);detailTip.Show(t,grid,Math.Min(pt.X,Math.Max(0,grid.Width-650)),pt.Y+24,30000);};
      FormClosed+=(s,e)=>{detailDelay.Dispose();detailTip.Dispose();};
    }
    void ShowPresets(){using(var f=CreatePresetWindow())f.ShowDialog(this);}
    Form CreatePresetWindow(){
      var f=new Form{Text=Loc.T("preset_win_title"),Icon=Icon,BackColor=Color.Black,ForeColor=Color.White,Size=new Size(1450,650),StartPosition=FormStartPosition.CenterParent};
      var g=new BufferedGrid{Dock=DockStyle.Fill,AllowUserToAddRows=false,RowHeadersVisible=false,BackgroundColor=Color.Black,AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.None};g.RowTemplate.Height=110;g.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.Black,ForeColor=Color.White,SelectionBackColor=Color.Black};g.EnableHeadersVisualStyles=false;g.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.Black,ForeColor=Color.White};
      var factor=new DataGridViewComboBoxColumn{Name="Factor",HeaderText=Loc.T("preset_global_col"),Width=115};for(int n=0;n<=30;n++)factor.Items.Add((n*10).ToString(CultureInfo.InvariantCulture)+"%");foreach(var p in RuneEngine.Presets){string v=Math.Round(p.ScoreFactor*100).ToString(CultureInfo.InvariantCulture)+"%";if(!factor.Items.Contains(v))factor.Items.Add(v);}g.Columns.Add(factor);
      g.Columns.Add("Preset","Preset");g.Columns[1].ReadOnly=true;
      string[] stats={"HP%","Atk%","Def%","Spd","Res%","Acc%","CtR%","CtD%","HP+","Atk+","Def+"};foreach(string stat in stats){var c=new DataGridViewComboBoxColumn{HeaderText=stat,Width=60};c.Items.AddRange(new object[]{Loc.T("prio_none"),"P1","P2","P3"});g.Columns.Add(c);}
      g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("preset_sets_pref"),Width=280});g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("preset_sets_ok"),Width=280});g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Slot 2",Width=135});g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Slot 4",Width=135});g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Slot 6",Width=135});
      // Ligne "globale" tout en haut : ses cellules stat (colonnes 2..12) sont des menus deroulants
      // de POURCENTAGE (50 a 150%), pas Non/P1/P2/P3. Ca regle RuneEngine.StatGlobalFactor[stat],
      // un multiplicateur applique dans Weight() pour TOUTES les stats de ce type, sur TOUS les
      // presets, en plus (pas a la place) de la priorite Non/P1/P2/P3 propre a chaque preset.
      // Ex : mettre Atk% a 90% fait que l'Atk% compte pour 90% de sa valeur normale partout.
      int controlRow=g.Rows.Add();var controlCells=g.Rows[controlRow].Cells;controlCells[0].ReadOnly=true;controlCells[1].Value="🌐 "+Loc.T("preset_global_row");controlCells[1].ReadOnly=true;for(int n=13;n<=17;n++)controlCells[n].ReadOnly=true;
      // Cellules remplacees par de simples DataGridViewTextBoxCell (au lieu du ComboBoxCell de
      // la colonne) : le menu Non/P1/P2/P3 de CreatePresetMenu (PresetMenus.cs) lit les Items de
      // la COLONNE, pas de la cellule — impossible d'y afficher une autre liste (pourcentages)
      // en gardant un ComboBoxCell. CreatePresetMenu detecte cette ligne au contenu ("...%") et
      // propose alors 50%-150% au lieu de Non/P1/P2/P3.
      for(int n=0;n<stats.Length;n++){double gf;if(!RuneEngine.StatGlobalFactor.TryGetValue(stats[n],out gf))gf=1.0;string current=Math.Round(gf*100).ToString(CultureInfo.InvariantCulture)+"%";g.Rows[controlRow].Cells[n+2]=new DataGridViewTextBoxCell{Value=current};}
      foreach(var p in RuneEngine.Presets){int i=g.Rows.Add();var cells=g.Rows[i].Cells;cells[0].Value=Math.Round(p.ScoreFactor*100).ToString(CultureInfo.InvariantCulture)+"%";cells[1].Value=p.Name;for(int n=0;n<stats.Length;n++)cells[n+2].Value=PriorityDisplay(p.W[stats[n]]);cells[13].Value=string.Join(",",p.Preferred);cells[14].Value=string.Join(",",p.Accepted);cells[15].Value=string.Join(",",p.Main[2]);cells[16].Value=string.Join(",",p.Main[4]);cells[17].Value=string.Join(",",p.Main[6]);}
      ConfigureBlackPresetGrid(g);
      g.CurrentCellDirtyStateChanged+=(s,e)=>{if(g.IsCurrentCellDirty)g.CommitEdit(DataGridViewDataErrorContexts.Commit);};
      var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=62,BackColor=Color.Black};var save=new Button{Text=Loc.T("save_recalc"),Width=260,Height=40,BackColor=Color.Black,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};var stock=new Button{Text=Loc.T("preset_stock_btn"),Width=230,Height=40,BackColor=Color.Black,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};bar.Controls.Add(save);bar.Controls.Add(stock);
      stock.Click+=(s,e)=>ShowPresetStock();save.Click+=(s,e)=>{g.EndEdit();int baseRow=controlRow+1;for(int n=0;n<stats.Length;n++){string txt=Convert.ToString(controlCells[n+2].Value);double pct;if(txt!=null&&txt.EndsWith("%")&&double.TryParse(txt.TrimEnd('%'),NumberStyles.Any,CultureInfo.InvariantCulture,out pct))RuneEngine.StatGlobalFactor[stats[n]]=pct/100.0;}for(int i=0;i<RuneEngine.Presets.Count;i++){var p=RuneEngine.Presets[i];var c=g.Rows[baseRow+i].Cells;p.ScoreFactor=ParsePresetFactor(Convert.ToString(c[0].Value));for(int n=0;n<stats.Length;n++)p.W[stats[n]]=PriorityValue(Convert.ToString(c[n+2].Value));ReplaceSet(p.Preferred,Convert.ToString(c[13].Value));ReplaceSet(p.Accepted,Convert.ToString(c[14].Value));ReplaceSet(p.Main[2],Convert.ToString(c[15].Value));ReplaceSet(p.Main[4],Convert.ToString(c[16].Value));ReplaceSet(p.Main[6],Convert.ToString(c[17].Value));}File.WriteAllLines(PresetFactorsPath,RuneEngine.Presets.Select(p=>p.Name+"\t"+p.ScoreFactor.ToString(CultureInfo.InvariantCulture)));ApplySettingsAndClose(f,Loc.T("presets_globals_saved"));};
      f.Controls.Add(g);f.Controls.Add(bar);return f;
    }
    void ShowPresetStock(){
      var f=new Form{Text=Loc.T("preset_stock_title"),Size=new Size(1150,680),StartPosition=FormStartPosition.CenterParent};var g=new BufferedGrid{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,RowHeadersVisible=false,AutoGenerateColumns=false};g.Columns.Add(new DataGridViewImageColumn{HeaderText="Set",Width=40,ImageLayout=DataGridViewImageCellLayout.Zoom});g.Columns.Add("Preset","Preset");g.Columns.Add("SetName","Set");for(int n=1;n<=6;n++)g.Columns.Add("S"+n,"Slot "+n+" : stock / bonus");
      foreach(var p in RuneEngine.Presets)foreach(string setName in p.Preferred.Concat(p.Accepted).Distinct().OrderBy(x=>x)){int[] c;if(!RuneEngine.PresetSlotCounts.TryGetValue(p.Name+"|"+setName,out c))c=new int[6];var values=new List<object>{GetSetIcon(setName),p.Name,setName};for(int n=1;n<=6;n++)values.Add(c[n-1]+" / +"+RuneEngine.ScarcityBonus(c,n).ToString("0.000"));g.Rows.Add(values.ToArray());}g.AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.AllCells;f.Controls.Add(g);f.ShowDialog(this);
    }
    // Bouton "Règles" : liste toutes les regles de bonus/malus pur (RuneEngine.ScoreRules),
    // appliquees une fois sur le Potential final (voir RuneEngine.RuleBonus), INDEPENDANTES
    // du preset choisi. Les 2 regles Spd 23/25 (Will/Despair/Violent/Swift) sont marquees
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
      string[] setNames={"Energy","Guard","Swift","Blade","Rage","Focus","Endure","Fatal","Despair","Vampire","Violent","Nemesis","Will","Shield","Revenge","Destroy","Fight","Determination","Enhance","Accuracy","Tolerance","Seal","Intangible"};
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
      var bar=new FlowLayoutPanel{Dock=DockStyle.Bottom,Height=62,BackColor=Color.Black};
      var add=new Button{Text="AJOUTER UNE RÈGLE",Width=210,Height=40,BackColor=Color.Black,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
      var save=new Button{Text="ENREGISTRER ET RECALCULER",Width=260,Height=40,BackColor=Color.Black,ForeColor=Color.White,FlatStyle=FlatStyle.Flat};
      bar.Controls.Add(add);bar.Controls.Add(save);
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
      f.Controls.Add(g);f.Controls.Add(bar);return f;
    }
  }
}
