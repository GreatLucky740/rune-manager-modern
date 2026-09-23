using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace RuneManagerModern {
  sealed class RtaMonster {
    public int Id; public string Name="",Slug="",ImageFile=""; public double WinRate,PickRate,BanRate,LeadRate; public int Played;
  }
  sealed class RtaPair {
    public int OtherId; public int Against; public int Together; public double WinAgainst,WinTogether;
  }
  sealed class RtaRecommendation {
    public int Rank{get;set;} public Image Icon{get;set;} public string Monster{get;set;} public string Conseil{get;set;} public string WinRate{get;set;} public string Pick{get;set;} public string Ban{get;set;} public string Lead{get;set;} public string Games{get;set;} public string Sets{get;set;} public string Subs{get;set;} public string Score{get;set;} public double ScoreValue{get;set;} public RtaMonster Source;
  }

  static class RtaSetIcons {
    static readonly Dictionary<string,Image> Cache=new Dictionary<string,Image>(StringComparer.OrdinalIgnoreCase);
    public static Image Get(string setName){
      if(string.IsNullOrWhiteSpace(setName))return null;
      string key=setName.Trim();
      Image img;if(Cache.TryGetValue(key,out img))return img;
      string p=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","sets",key.ToLowerInvariant()+".png");
      if(!File.Exists(p)){Cache[key]=null;return null;}
      try{using(var fs=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var src=Image.FromStream(fs)){img=new Bitmap(src);Cache[key]=img;return img;}}catch{Cache[key]=null;return null;}
    }
    public static void Paint(DataGridViewCellPaintingEventArgs e,string text){
      e.Handled=true;
      e.Paint(e.CellBounds,DataGridViewPaintParts.Background|DataGridViewPaintParts.SelectionBackground|DataGridViewPaintParts.Border);
      if(string.IsNullOrEmpty(text)||text=="—"||text.StartsWith("—")){
        TextRenderer.DrawText(e.Graphics,string.IsNullOrEmpty(text)?"—":text,e.CellStyle.Font,e.CellBounds,e.CellStyle.ForeColor,TextFormatFlags.VerticalCenter|TextFormatFlags.Left|TextFormatFlags.EndEllipsis|TextFormatFlags.NoPadding);
        return;
      }
      var g=e.Graphics;g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
      var old=g.Clip;g.SetClip(Rectangle.Inflate(e.CellBounds,-3,-2));
      int x=e.CellBounds.X+4,cy=e.CellBounds.Y+(e.CellBounds.Height-22)/2;
      using(var pctFont=new Font("Segoe UI Semibold",8.5f))
      foreach(string combo in text.Split(new[]{", "},StringSplitOptions.RemoveEmptyEntries)){
        string piece=combo.Trim();if(piece.Length==0)continue;
        string names=piece,pct="";
        int cut=piece.LastIndexOf(' ');
        if(cut>0&&piece.IndexOf('%')>=0){names=piece.Substring(0,cut).Trim();pct=piece.Substring(cut+1).Trim();}
        foreach(string part in names.Split('+')){
          string set=part.Trim();if(set.Length==0)continue;
          var icon=Get(set);
          if(icon!=null)g.DrawImage(icon,new Rectangle(x,cy,22,22));
          else TextRenderer.DrawText(g,set.Length<=3?set:set.Substring(0,3),pctFont,new Rectangle(x,cy,22,22),Color.Silver,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);
          x+=23;
        }
        if(pct.Length>0){var sz=TextRenderer.MeasureText(g,pct,pctFont,new Size(80,22),TextFormatFlags.NoPadding);TextRenderer.DrawText(g,pct,pctFont,new Rectangle(x+1,cy,sz.Width+2,22),Color.FromArgb(220,230,240),TextFormatFlags.VerticalCenter|TextFormatFlags.Left|TextFormatFlags.NoPadding);x+=sz.Width+10;}
        else x+=8;
      }
      g.Clip=old;
    }
  }
  static class RtaData {
    static readonly Dictionary<int,Dictionary<int,RtaPair>> SnapshotPairs=new Dictionary<int,Dictionary<int,RtaPair>>();
    static Dictionary<string,object> D(object x){return x as Dictionary<string,object>;} static object[] A(object x){return x as object[]??new object[0];}
    static object G(Dictionary<string,object>d,string k,object z=null){object v;return d!=null&&d.TryGetValue(k,out v)?v:z;}
    static int I(object x){int n;return int.TryParse(Convert.ToString(x,CultureInfo.InvariantCulture),out n)?n:0;}
    static double F(object x){double n;return double.TryParse(Convert.ToString(x,CultureInfo.InvariantCulture),NumberStyles.Any,CultureInfo.InvariantCulture,out n)?n:0;}
    static string ReadShared(string p){using(var f=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete))using(var r=new StreamReader(f))return r.ReadToEnd();}
    public static Dictionary<int,string> Owned(string jsonPath,string catalogPath){
      var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var names=new Dictionary<int,string>();var groups=new Dictionary<int,int>();var elements=new Dictionary<int,string>();
      foreach(var raw in A(js.DeserializeObject(File.ReadAllText(catalogPath)))){var d=D(raw);int id=I(G(d,"id"));if(id>0){names[id]=Convert.ToString(G(d,"name",Loc.T("rta_monster_n",id)));groups[id]=I(G(d,"skillgroup"));elements[id]=Convert.ToString(G(d,"element"));}}
      var root=D(js.DeserializeObject(ReadShared(jsonPath)));var result=new Dictionary<int,string>();
      foreach(var raw in A(G(root,"unit_list"))){var d=D(raw);int id=I(G(d,"unit_master_id"));if(id>0&&!result.ContainsKey(id))result[id]=names.ContainsKey(id)?names[id]:Loc.T("rta_monster_n",id);}
      // Les éditions collaboration et leurs équivalents permanents partagent le
      // même groupe de sorts et le même élément. SWArena peut référencer l'une ou
      // l'autre : les deux identifiants sont donc considérés comme possédés.
      var ownedIds=result.Keys.ToList();foreach(int ownedId in ownedIds){int group;string element;if(!groups.TryGetValue(ownedId,out group)||group<=0||!elements.TryGetValue(ownedId,out element))continue;foreach(int equivalent in groups.Where(x=>x.Value==group).Select(x=>x.Key)){string otherElement;if(elements.TryGetValue(equivalent,out otherElement)&&string.Equals(element,otherElement,StringComparison.OrdinalIgnoreCase)&&!result.ContainsKey(equivalent))result[equivalent]=(names.ContainsKey(equivalent)?names[equivalent]:Loc.T("rta_monster_n",equivalent))+Loc.T("rta_owned_eq");}}
      return result;
    }
    static string Download(string url){
      ServicePointManager.SecurityProtocol=SecurityProtocolType.Tls12;var req=(HttpWebRequest)WebRequest.Create(url);req.UserAgent="Rune Manager Modern/1.0";req.Timeout=15000;req.ReadWriteTimeout=15000;
      using(var response=req.GetResponse())using(var stream=response.GetResponseStream())using(var reader=new StreamReader(stream,Encoding.UTF8))return reader.ReadToEnd();
    }
    public static List<RtaMonster> LoadSeason(string catalogPath){
      string local=Path.GetFullPath(Path.Combine(Path.GetDirectoryName(catalogPath),"..","rta","season38.json"));
      if(File.Exists(local)){
        var localJs=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=128};var localRoot=D(localJs.DeserializeObject(File.ReadAllText(local)));var localMonsters=new List<RtaMonster>();
        foreach(var raw in A(G(localRoot,"monsters"))){var x=A(raw);if(x.Length<9)continue;localMonsters.Add(new RtaMonster{Id=I(x[0]),Name=Convert.ToString(x[1]),Slug=Convert.ToString(x[2]),ImageFile=Convert.ToString(x[3]),WinRate=F(x[4]),PickRate=F(x[5]),BanRate=F(x[6]),LeadRate=F(x[7]),Played=I(x[8])});}
        SnapshotPairs.Clear();var savedPairs=D(G(localRoot,"pairs"));if(savedPairs!=null)foreach(var entry in savedPairs){int id;if(!int.TryParse(entry.Key,out id))continue;var map=new Dictionary<int,RtaPair>();foreach(var raw in A(entry.Value)){var x=A(raw);if(x.Length<5)continue;int other=I(x[0]);map[other]=new RtaPair{OtherId=other,Against=I(x[1]),WinAgainst=F(x[2]),Together=I(x[3]),WinTogether=F(x[4])};}SnapshotPairs[id]=map;}
        return localMonsters;
      }
      var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=128};var all=new List<RtaMonster>();
      for(int offset=0;offset<1000;offset+=25){string url="https://api.swarena.gg/monsters?season=38&isG3=false&isSL=false&played=0&orderBy=played&orderDirection=DESC&limit=25&offset="+offset;var root=D(js.DeserializeObject(Download(url)));var rows=A(G(root,"data"));foreach(var raw in rows){var d=D(raw);all.Add(new RtaMonster{Id=I(G(d,"monster_id")),Name=Convert.ToString(G(d,"name")),Slug=Convert.ToString(G(d,"slug")),ImageFile=Convert.ToString(G(d,"image_filename")),WinRate=F(G(d,"win_rate")),PickRate=F(G(d,"pick_rate")),BanRate=F(G(d,"ban_rate")),LeadRate=F(G(d,"lead_rate")),Played=I(G(d,"played"))});}if(rows.Length<25)break;}
      return all.Where(x=>x.Id>0).GroupBy(x=>x.Id).Select(g=>g.First()).ToList();
    }
    public static Dictionary<int,RtaPair> LoadPairs(int id){
      Dictionary<int,RtaPair> saved;if(SnapshotPairs.TryGetValue(id,out saved))return saved;
      if(SnapshotPairs.Count>0)return new Dictionary<int,RtaPair>();
      var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=128};var result=new Dictionary<int,RtaPair>();
      for(int offset=0;offset<600;offset+=100){string url="https://api.swarena.gg/monster/"+id+"/pairs?season=38&isG3=false&searchPairName=&orderBy=total_played_against&orderDirection=DESC&minPlayedAgainst=0&minPlayedTogether=0&limit=100&offset="+offset;var root=D(js.DeserializeObject(Download(url)));var rows=A(G(root,"data"));foreach(var raw in rows){var d=D(raw);int other=I(G(d,"b_monster_id"));if(other>0)result[other]=new RtaPair{OtherId=other,Against=I(G(d,"total_played_against")),Together=I(G(d,"total_played_together")),WinAgainst=F(G(d,"win_against_rate")),WinTogether=F(G(d,"win_together_rate"))};}if(rows.Length<100)break;}
      return result;
    }
  }
  static class RtaPickScore {
    public static double SynergyWeight(int enemyN){
      if(enemyN<=0)return 1.35;
      double w=1.12-enemyN*0.16;
      return w<0.32?0.32:w;
    }
    public static double CounterWeight(int enemyN){
      if(enemyN<=0)return 0;
      return 1.25+enemyN*0.28;
    }
    public static double TeamFitWeight(int enemyN){
      if(enemyN>=4)return 0.45;
      if(enemyN>=2)return 0.75;
      if(enemyN>=1)return 1.0;
      return 1.4;
    }
    public static double ComboTogetherWeight(int enemyN){
      if(enemyN>=3)return 0.7;
      if(enemyN>=1)return 1.15;
      return 1.6;
    }
    public static double SampleFactor(int n){
      if(n>=80)return 1;
      if(n>=20)return n/80.0;
      if(n>=8)return 0.22;
      return 0;
    }
    public static double FoeThreat(double winRate,double pickRate,double banRate,double leadRate){
      return 0.55+winRate+pickRate*2.5+banRate*1.8+leadRate;
    }
    public static double MatchupPoints(double winAgainst,int games,double foeWin,double foePick,double foeBan,double foeLead){
      double sf=SampleFactor(games);
      if(sf<=0)return 0;
      return (winAgainst-.5)*100*sf*FoeThreat(foeWin,foePick,foeBan,foeLead);
    }
  }
  sealed class RtaAdvisorForm:Form {
    readonly string json,catalog,icons;readonly Icon appIcon;readonly Color Bg=Color.FromArgb(7,13,22),Panel=Color.FromArgb(15,25,39),Pink=Color.FromArgb(190,68,145),Cyan=Color.FromArgb(20,184,210);
    readonly List<RtaMonster> ownPicks=new List<RtaMonster>(),enemyPicks=new List<RtaMonster>();List<RtaMonster> stats=new List<RtaMonster>(),pickerAll=new List<RtaMonster>();Dictionary<int,string> owned=new Dictionary<int,string>();readonly Dictionary<int,Dictionary<int,RtaPair>> pairs=new Dictionary<int,Dictionary<int,RtaPair>>();
    readonly FlowLayoutPanel ours=new FlowLayoutPanel(),enemies=new FlowLayoutPanel();readonly BufferedGrid grid=new BufferedGrid();readonly Label phase=new Label(),state=new Label();readonly ComboBox poolSize=new ComboBox();Button poolButton;readonly CountBadge poolBadge=new CountBadge();RtaPoolDiff poolDiff;List<RtaRecommendation> adviceAll=new List<RtaRecommendation>();string sortCol="Score";bool sortDesc=true;bool loading,draftLive;    readonly Dictionary<int,Image> iconCache=new Dictionary<int,Image>();readonly Dictionary<int,Image> banIconCache=new Dictionary<int,Image>();readonly Dictionary<int,Control> pickerTiles=new Dictionary<int,Control>();readonly Dictionary<int,string> pickerFold=new Dictionary<int,string>();Panel pickerOverlay;Label pickerTitle;TextBox pickerBox;BufferedFlow pickerStrip;RtaMonster pickerChosen;HashSet<int> iconFiles;Timer pickerFilterTimer;Image banMark;bool banMarkTried;int firstSide,holdOpenId,lastTurnKey=-1,lastSide,holdEnemyN=-1,pickerIndex,lastAutoEnemyN=-1,banAdviceId;bool pickerMine,pickerCommitted,ignoreSlotClicks,skipAuto;List<RtaMonster> holdTurn;HashSet<int> poolIdCache;int poolIdCacheN=-1;
    public RtaAdvisorForm(string jsonPath,string catalogPath,Icon icon){json=jsonPath;catalog=catalogPath;icons=Path.GetDirectoryName(catalogPath);appIcon=icon;Text=Loc.T("rta_win");Icon=icon;BackColor=Bg;ForeColor=Color.White;Size=new Size(1680,860);MinimumSize=new Size(1200,700);StartPosition=FormStartPosition.CenterParent;KeyPreview=true;KeyDown+=(s,e)=>{if(e.KeyCode==Keys.Escape&&pickerOverlay!=null&&pickerOverlay.Visible){e.Handled=true;ClosePicker();}};Build();Shown+=(s,e)=>{LoadData();};FormClosed+=(s,e)=>{if(pickerFilterTimer!=null)pickerFilterTimer.Stop();if(pickerOverlay!=null&&!pickerOverlay.IsDisposed)pickerOverlay.Dispose();};}
    Button B(string t,int w,Color c){return new Button{Text=t,Width=w,Height=32,FlatStyle=FlatStyle.Flat,BackColor=c,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",9),Cursor=Cursors.Hand};}
    void Build(){
      var head=new Panel{Dock=DockStyle.Top,Height=192,BackColor=Panel,Padding=new Padding(18,10,18,8)};
      head.Controls.Add(new Label{Text=Loc.T("rta_head"),AutoSize=true,Location=new Point(18,8),ForeColor=Color.FromArgb(255,82,180),Font=new Font("Segoe UI Semibold",20)});
      phase.SetBounds(18,47,1400,28);phase.ForeColor=Color.FromArgb(255,210,90);phase.Font=new Font("Segoe UI Semibold",12);head.Controls.Add(phase);state.SetBounds(18,76,1180,23);state.ForeColor=Color.Silver;head.Controls.Add(state);
      var ownLabel=new Label{Text=Loc.T("rta_yours"),Location=new Point(18,103),Size=new Size(120,22),ForeColor=Cyan,Font=new Font("Segoe UI Semibold",10)};var enemyLabel=new Label{Text=Loc.T("rta_theirs"),Location=new Point(650,103),Size=new Size(160,22),ForeColor=Color.FromArgb(255,105,100),Font=new Font("Segoe UI Semibold",10)};head.Controls.Add(ownLabel);head.Controls.Add(enemyLabel);
      ours.SetBounds(135,99,490,66);ours.WrapContents=false;ours.BackColor=Bg;enemies.SetBounds(810,99,490,66);enemies.WrapContents=false;enemies.BackColor=Bg;head.Controls.Add(ours);head.Controls.Add(enemies);
      var tools=new Panel{Dock=DockStyle.Top,Height=52,BackColor=Color.FromArgb(10,20,32),Padding=new Padding(18,9,18,7)};
      var first=B(Loc.T("rta_first"),155,Color.FromArgb(36,137,112));first.SetBounds(18,9,155,32);first.Click+=(s,e)=>StartDraft(1);tools.Controls.Add(first);
      var enemy=B(Loc.T("rta_enemy"),175,Color.FromArgb(160,70,70));enemy.SetBounds(180,9,175,32);enemy.Click+=(s,e)=>StartDraft(2);tools.Controls.Add(enemy);
      var reset=B(Loc.T("rta_new"),135,Pink);reset.SetBounds(365,9,135,32);reset.Click+=(s,e)=>{ownPicks.Clear();enemyPicks.Clear();firstSide=0;holdOpenId=0;lastTurnKey=-1;draftLive=false;skipAuto=false;lastAutoEnemyN=-1;ClearTurnHold();LoadData();};tools.Controls.Add(reset);
      poolSize.DropDownStyle=ComboBoxStyle.DropDownList;poolSize.Items.AddRange(new object[]{10,20,30,40,50,60});poolSize.SetBounds(510,10,70,30);int savedPool=RtaBuildOptimizer.LoadPoolSize();poolSize.SelectedItem=poolSize.Items.Cast<object>().FirstOrDefault(x=>Convert.ToInt32(x)==savedPool)??20;poolSize.SelectedIndexChanged+=(s,e)=>{RtaBuildOptimizer.SavePoolSize(Convert.ToInt32(poolSize.SelectedItem));RefreshPoolBadge();RefreshAdvice();};tools.Controls.Add(poolSize);
      poolButton=B(Loc.T("rta_pool"),188,Color.FromArgb(36,137,112));poolButton.SetBounds(590,9,188,32);poolButton.Padding=new Padding(8,0,8,0);poolButton.Click+=(s,e)=>OpenPool();tools.Controls.Add(poolButton);poolBadge.UseNewTag=false;poolBadge.SetBounds(4,2,28,28);poolBadge.Text="0";poolBadge.Visible=false;poolBadge.Font=new Font("Segoe UI Semibold",9f);poolBadge.BadgeColor=Color.FromArgb(255,170,40);poolBadge.Click+=(s,e)=>OpenPool();poolButton.Controls.Add(poolBadge);poolBadge.BringToFront();
      var editPool=B(Loc.T("rta_pool_edit"),130,Color.FromArgb(70,118,176));editPool.SetBounds(788,9,130,32);editPool.Click+=(s,e)=>EditPool();tools.Controls.Add(editPool);
      var resetPool=B(Loc.T("rta_reset"),115,Color.FromArgb(125,65,65));resetPool.SetBounds(926,9,115,32);resetPool.Click+=(s,e)=>{if(MessageBox.Show(Loc.T("rta_reset_q"),"RTA",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;RtaBuildOptimizer.ResetSettings();poolSize.SelectedItem=20;ownPicks.Clear();enemyPicks.Clear();firstSide=0;holdOpenId=0;lastTurnKey=-1;draftLive=false;skipAuto=false;lastAutoEnemyN=-1;ClearTurnHold();RefreshAll();RefreshPoolBadge();state.Text=Loc.T("rta_reset_ok");};tools.Controls.Add(resetPool);
      var refresh=B(Loc.T("rta_meta"),140,Color.FromArgb(76,72,155));refresh.SetBounds(1049,9,140,32);refresh.Click+=(s,e)=>LoadData(true);tools.Controls.Add(refresh);
      grid.Dock=DockStyle.Fill;grid.BackgroundColor=Bg;grid.BorderStyle=BorderStyle.None;grid.ReadOnly=true;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.RowHeadersVisible=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.RowTemplate.Height=58;grid.AutoGenerateColumns=false;grid.EnableHeadersVisualStyles=false;grid.ColumnHeadersHeight=42;grid.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(138,45,117),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10),SelectionBackColor=Color.FromArgb(138,45,117)};grid.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Bg,ForeColor=Color.White,Font=new Font("Segoe UI",10),SelectionBackColor=Color.FromArgb(45,55,83),SelectionForeColor=Color.White};
      grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Rank",HeaderText="#",Width=40});grid.Columns.Add(new DataGridViewImageColumn{DataPropertyName="Icon",HeaderText=Loc.T("rta_col_mon"),Width=58,ImageLayout=DataGridViewImageCellLayout.Zoom});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Monster",HeaderText=Loc.T("rta_col_name"),Width=150});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="WinRate",HeaderText="WR",Width=70});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Pick",HeaderText="Pick",Width=70});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Ban",HeaderText="Ban",Width=70});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Lead",HeaderText="Lead",Width=70});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Games",HeaderText="Games",Width=80});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Sets",HeaderText="Sets SWLens",Width=300});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Subs",HeaderText="Top subs",Width=220});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Score",HeaderText=Loc.T("rta_col_score"),Width=80});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Conseil",HeaderText=Loc.T("rta_col_why"),AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill});foreach(DataGridViewColumn c in grid.Columns){string p=c.DataPropertyName;c.SortMode=(p=="WinRate"||p=="Pick"||p=="Ban"||p=="Lead"||p=="Games"||p=="Score")?DataGridViewColumnSortMode.Programmatic:DataGridViewColumnSortMode.NotSortable;if(c.SortMode==DataGridViewColumnSortMode.Programmatic)c.HeaderCell.ToolTipText=Loc.T("rta_sort_tip");}
      grid.CellPainting+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex<0)return;if(grid.Columns[e.ColumnIndex].DataPropertyName!="Sets")return;var rec=grid.Rows[e.RowIndex].DataBoundItem as RtaRecommendation;RtaSetIcons.Paint(e,rec==null?Convert.ToString(e.FormattedValue):rec.Sets);};
      grid.CellFormatting+=(s,e)=>{
        if(e.RowIndex<0)return;
        var rec=e.RowIndex<grid.Rows.Count?grid.Rows[e.RowIndex].DataBoundItem as RtaRecommendation:null;
        if(BanPhase()&&rec!=null&&rec.Source!=null&&rec.Source.Id==banAdviceId){
          e.CellStyle.BackColor=Color.FromArgb(78,28,28);e.CellStyle.ForeColor=Color.FromArgb(255,196,186);
          e.CellStyle.SelectionBackColor=Color.FromArgb(110,40,40);e.CellStyle.SelectionForeColor=Color.FromArgb(255,220,210);return;
        }
        if(e.RowIndex!=0||!draftLive||!OurTurn())return;
        e.CellStyle.BackColor=Color.FromArgb(42,78,48);e.CellStyle.ForeColor=Color.FromArgb(255,214,96);
        e.CellStyle.SelectionBackColor=Color.FromArgb(55,98,60);e.CellStyle.SelectionForeColor=Color.FromArgb(255,230,140);
      };
      grid.ColumnHeaderMouseClick+=(s,e)=>{if(e.ColumnIndex<0)return;string p=grid.Columns[e.ColumnIndex].DataPropertyName;if(grid.Columns[e.ColumnIndex].SortMode!=DataGridViewColumnSortMode.Programmatic)return;if(sortCol==p)sortDesc=!sortDesc;else{sortCol=p;sortDesc=true;}ApplyAdviceSort();};
      Controls.Add(grid);Controls.Add(tools);Controls.Add(head);
      RenderSlots();SetPhase();
    }
    void StartDraft(int side){
      firstSide=side;draftLive=true;if(ownPicks.Count==0)holdOpenId=0;lastTurnKey=-1;skipAuto=false;lastAutoEnemyN=-1;ClearTurnHold();
      RefreshAll();
      if(side==1)state.Text=Loc.T("rta_hint_first");
      else state.Text=Loc.T("rta_hint_enemy");
    }
    void LoadData(bool live=false){if(loading)return;loading=true;state.Text=live?Loc.T("rta_load_live"):Loc.T("rta_load");grid.DataSource=null;System.Threading.ThreadPool.QueueUserWorkItem(_=>{List<RtaMonster> loaded=null;Dictionary<int,string> roster=null;Exception error=null;string note=null;try{if(live)note=RtaMetaRefresh.Run(catalog);RtaBuildOptimizer.RtaMetaBuilds.EnsureLoaded(catalog);roster=RtaData.Owned(json,catalog);loaded=RtaData.LoadSeason(catalog);}catch(Exception ex){error=ex;}try{BeginInvoke((MethodInvoker)delegate{loading=false;if(error!=null){state.Text=Loc.T("rta_load_fail",error.Message);return;}owned=roster;stats=loaded;poolIdCache=null;poolIdCacheN=-1;LoadPickerList();state.Text=Loc.T("rta_loaded",note??Loc.T("rta_loaded_stats",stats.Count),owned.Count);RefreshAll();RefreshPoolBadge();});}catch{}});}
    void LoadPickerList(){
      var list=new List<RtaMonster>();var seen=new HashSet<int>();
      try{
        var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};
        var arr=js.DeserializeObject(File.ReadAllText(catalog)) as object[];
        if(arr!=null)foreach(var raw in arr){
          var d=raw as Dictionary<string,object>;if(d==null)continue;
          object idObj;if(!d.TryGetValue("id",out idObj))continue;
          int id;if(!int.TryParse(Convert.ToString(idObj,CultureInfo.InvariantCulture),out id)||id<=0)continue;
          object nameObj;string name;name=d.TryGetValue("name",out nameObj)?Convert.ToString(nameObj):Loc.T("rta_monster_n",id);
          if(string.IsNullOrEmpty(name))name=Loc.T("rta_monster_n",id);
          var hit=stats.FirstOrDefault(x=>x.Id==id);
          list.Add(hit??new RtaMonster{Id=id,Name=name});seen.Add(id);
        }
      }catch{}
      foreach(var m in stats)if(m!=null&&m.Id>0&&!seen.Contains(m.Id)){list.Add(m);seen.Add(m.Id);}
      pickerAll=list.OrderBy(x=>x.Name??"",StringComparer.OrdinalIgnoreCase).ToList();
      pickerFold.Clear();foreach(var m in pickerAll)if(m!=null&&m.Id>0)pickerFold[m.Id]=Fold(m.Name);
      pickerTiles.Clear();
      if(pickerOverlay!=null&&!pickerOverlay.IsDisposed){pickerOverlay.Dispose();pickerOverlay=null;pickerStrip=null;pickerBox=null;pickerTitle=null;}
    }
    static string Fold(string s){
      if(string.IsNullOrEmpty(s))return "";
      string n=s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
      var sb=new StringBuilder(n.Length);
      foreach(char c in n)if(CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)sb.Append(c);
      return sb.ToString();
    }
    static bool NameMatch(string name,string q){
      string n=Fold(name);if(n.IndexOf(q)>=0)return true;
      int i=0;foreach(char c in n){if(i<q.Length&&c==q[i])i++;}return i==q.Length;
    }
    static void BindClick(Control c,EventHandler h){c.Cursor=Cursors.Hand;c.Click+=h;foreach(Control x in c.Controls)BindClick(x,h);}
    bool PickerMatch(RtaMonster m,string q){
      if(m==null)return false;
      string n;if(!pickerFold.TryGetValue(m.Id,out n))n=Fold(m.Name);
      if(n.IndexOf(q)>=0)return true;
      int i=0;foreach(char c in n){if(i<q.Length&&c==q[i])i++;}return i==q.Length;
    }
    Control PickerTile(RtaMonster m){
      Control cached;if(pickerTiles.TryGetValue(m.Id,out cached))return cached;
      var p=new Panel{Width=64,Height=80,Margin=new Padding(3),BackColor=Color.FromArgb(20,34,49),Cursor=Cursors.Hand,Tag=m};
      var pic=new PictureBox{Image=IconFor(m.Id),Bounds=new Rectangle(8,4,48,48),SizeMode=PictureBoxSizeMode.Zoom,Enabled=false};
      var nm=new Label{Text=m.Name??"",Bounds=new Rectangle(2,54,60,24),ForeColor=Color.Silver,Font=new Font("Segoe UI",7),TextAlign=ContentAlignment.TopCenter,AutoEllipsis=true,Enabled=false};
      p.Controls.Add(pic);p.Controls.Add(nm);
      RtaMonster pick=m;
      p.Click+=(s,e)=>AcceptPicker(pick);
      pickerTiles[m.Id]=p;return p;
    }
    void FillPicker(){
      if(pickerStrip==null||pickerBox==null)return;
      string q=Fold(pickerBox.Text.Trim());
      IEnumerable<RtaMonster> src;
      if(q.Length==0){var ranked=stats.OrderByDescending(x=>x.PickRate).ThenByDescending(x=>x.Played).Take(80).ToList();src=ranked.Count>0?(IEnumerable<RtaMonster>)ranked:pickerAll.Take(80);}
      else src=pickerAll.Where(x=>PickerMatch(x,q));
      pickerStrip.SuspendLayout();
      pickerStrip.Controls.Clear();
      int n=0;foreach(var m in src){if(m==null)continue;pickerStrip.Controls.Add(PickerTile(m));if(++n>=160)break;}
      pickerStrip.ResumeLayout();
    }
    void EnsurePickerOverlay(){
      if(pickerOverlay!=null&&!pickerOverlay.IsDisposed)return;
      pickerTiles.Clear();
      pickerOverlay=new Panel{Dock=DockStyle.Fill,Visible=false,BackColor=Bg};
      var top=new Panel{Dock=DockStyle.Top,Height=80,BackColor=Color.FromArgb(10,20,32)};
      pickerTitle=new Label{Text=Loc.T("rta_pick_title"),Location=new Point(12,8),Size=new Size(900,22),ForeColor=Color.Silver};
      var close=B(Loc.T("close"),90,Pink);close.Anchor=AnchorStyles.Top|AnchorStyles.Right;
      close.Click+=(s,e)=>ClosePicker();
      pickerBox=new TextBox{Location=new Point(12,42),Size=new Size(900,28),Font=new Font("Segoe UI",12),BackColor=Bg,ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle,Anchor=AnchorStyles.Top|AnchorStyles.Left|AnchorStyles.Right};
      top.Controls.Add(pickerTitle);top.Controls.Add(pickerBox);top.Controls.Add(close);
      top.Layout+=(s,e)=>{close.Location=new Point(Math.Max(12,top.ClientSize.Width-close.Width-12),8);pickerBox.Width=Math.Max(120,top.ClientSize.Width-24);};
      pickerStrip=new BufferedFlow{Dock=DockStyle.Fill,AutoScroll=true,WrapContents=true,BackColor=Bg,Padding=new Padding(8)};
      if(pickerFilterTimer==null){pickerFilterTimer=new Timer{Interval=35};pickerFilterTimer.Tick+=(s,e)=>{pickerFilterTimer.Stop();FillPicker();};}
      pickerBox.TextChanged+=(s,e)=>{pickerFilterTimer.Stop();pickerFilterTimer.Start();};
      pickerBox.KeyDown+=(s,e)=>{
        if(e.KeyCode==Keys.Escape){e.Handled=true;ClosePicker();return;}
        if(e.KeyCode!=Keys.Enter||pickerStrip==null||pickerStrip.Controls.Count==0)return;
        e.Handled=true;e.SuppressKeyPress=true;
        var first=pickerStrip.Controls[0].Tag as RtaMonster;
        if(first!=null)AcceptPicker(first);
      };
      pickerOverlay.Controls.Add(pickerStrip);pickerOverlay.Controls.Add(top);
      Controls.Add(pickerOverlay);
    }
    void OpenMonsterPicker(string title){
      pickerChosen=null;pickerCommitted=false;
      EnsurePickerOverlay();
      pickerTitle.Text=title+Loc.T("rta_pick_hint");
      pickerFilterTimer.Stop();
      pickerBox.Text="";
      pickerFilterTimer.Stop();
      FillPicker();
      pickerOverlay.Visible=true;
      pickerOverlay.BringToFront();
      pickerBox.Focus();
    }
    void ClosePicker(){if(pickerOverlay!=null)pickerOverlay.Visible=false;}
    void AcceptPicker(RtaMonster m){
      if(m==null||pickerCommitted)return;
      pickerChosen=m;pickerCommitted=true;ignoreSlotClicks=true;
      bool mine=pickerMine;int index=pickerIndex;
      BeginInvoke((MethodInvoker)delegate{
        try{ClosePicker();CommitPick(mine,index,m);}
        finally{BeginInvoke((MethodInvoker)delegate{ignoreSlotClicks=false;});}
      });
    }
    void CommitPick(bool mine,int index,RtaMonster picked){
      if(picked==null)return;
      var placed=Resolve(picked);
      if(placed==null)return;
      if(Taken(placed.Id)){state.Text=Loc.T("rta_already",placed.Name);return;}
      if(mine){if(index<ownPicks.Count)ownPicks[index]=placed;else if(ownPicks.Count<5)ownPicks.Add(placed);}
      else{if(index<enemyPicks.Count)enemyPicks[index]=placed;else if(enemyPicks.Count<5)enemyPicks.Add(placed);}
      lastSide=mine?1:2;draftLive=true;
      if(!mine)ClearTurnHold();
      else if(holdTurn!=null&&!holdTurn.Any(x=>x.Id==placed.Id))ClearTurnHold();
      RefreshAll();
    }
    void EnsureIconIndex(){
      if(iconFiles!=null)return;
      iconFiles=new HashSet<int>();
      try{foreach(string f in Directory.GetFiles(icons,"*.png")){int id;if(int.TryParse(Path.GetFileNameWithoutExtension(f),out id))iconFiles.Add(id);}}catch{}
    }
    void RefreshPoolBadge(){
      try{
        if(RtaPoolTracker.HasCustom()){
          int n=RtaPoolTracker.LoadCustomIds().Count;
          poolBadge.UseNewTag=false;poolBadge.Text=n.ToString();poolBadge.Visible=n>0;
          poolButton.Padding=n>0?new Padding(30,0,8,0):new Padding(8,0,8,0);
          Color c=Color.FromArgb(20,184,210);
          poolBadge.BadgeColor=c;poolButton.FlatAppearance.BorderColor=c;poolBadge.Invalidate();
          return;
        }
        int size=poolSize.SelectedItem==null?RtaBuildOptimizer.LoadPoolSize():Convert.ToInt32(poolSize.SelectedItem);
        poolDiff=RtaPoolTracker.Diff(json,catalog,stats,size);
        int count=poolDiff==null?0:poolDiff.Count;
        poolBadge.UseNewTag=false;poolBadge.Text=count.ToString();poolBadge.Visible=count>0;
        poolButton.Padding=count>0?new Padding(30,0,8,0):new Padding(8,0,8,0);
        Color alert=count>0?Color.FromArgb(218,70,62):Color.FromArgb(255,170,40);
        poolBadge.BadgeColor=alert;poolButton.FlatAppearance.BorderColor=alert;poolBadge.Invalidate();
      }catch{poolBadge.Text="0";poolBadge.Visible=false;poolButton.Padding=new Padding(8,0,8,0);}
    }
    void EditPool(){
      int n=poolSize.SelectedItem==null?20:Convert.ToInt32(poolSize.SelectedItem);
      bool saved=RtaPoolTracker.ShowEditor(this,owned,IconFor,json,catalog,stats,n);
      poolIdCache=null;poolIdCacheN=-1;
      RefreshPoolBadge();RefreshAdvice();
      if(!saved)return;
      state.Text=Loc.T("rta_pool_saved",RtaPoolTracker.LoadCustomIds().Count);
      OpenPool();
    }
    void OpenPool(){
      int n=poolSize.SelectedItem==null?20:Convert.ToInt32(poolSize.SelectedItem);
      if(!RtaPoolTracker.HasCustom()){
        if(poolDiff==null)poolDiff=RtaPoolTracker.Diff(json,catalog,stats,n);
        if(poolDiff!=null&&poolDiff.Count>0){
          RtaPoolTracker.ShowChanges(this,poolDiff,IconFor);
          RtaPoolTracker.SaveSnapshot(n,poolDiff.Current);
          poolDiff.Entered.Clear();poolDiff.Left.Clear();
          RefreshPoolBadge();
        }
      }
      RtaBuildOptimizer.Show(this,json,catalog,stats,IconFor,n);
    }
    int ResolveIconFile(int id){
      if(iconFiles==null||iconFiles.Count==0)return 0;
      if(iconFiles.Contains(id))return id;
      int[] deltas=new int[]{10,-10,20,-20,1,-1,11,-11,21,-21};
      for(int i=0;i<deltas.Length;i++){int cand=id+deltas[i];if(iconFiles.Contains(cand))return cand;}
      int element=id%10;
      if(element<1||element>5)return 0;
      int family=id/100,best=0,bestDist=int.MaxValue;
      foreach(int fid in iconFiles){
        if(fid/100!=family||fid%10!=element)continue;
        int dist=Math.Abs(fid-id);
        if(dist<bestDist){bestDist=dist;best=fid;}
      }
      return best;
    }
    Image IconFor(int id){
      Image cached;if(iconCache.TryGetValue(id,out cached))return cached;
      EnsureIconIndex();
      int fileId=ResolveIconFile(id);
      if(fileId>0){string p=Path.Combine(icons,fileId+".png");try{using(var f=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var img=Image.FromStream(f)){var bmp=new Bitmap(img,new Size(48,48));iconCache[id]=bmp;return bmp;}}catch{}}
      var b=new Bitmap(48,48);using(var g=Graphics.FromImage(b)){g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var bg=new SolidBrush(Color.FromArgb(30,40,52)))g.FillRectangle(bg,0,0,48,48);using(var fill=new SolidBrush(Color.FromArgb(90,105,120)))g.FillEllipse(fill,4,4,40,40);using(var pen=new Pen(Color.FromArgb(150,165,180),1.5f))g.DrawEllipse(pen,4,4,40,40);}
      iconCache[id]=b;return b;
    }
    bool BanPhase(){return ownPicks.Count>=5&&enemyPicks.Count>=5;}
    Image BanMark(){
      if(banMarkTried)return banMark;
      banMarkTried=true;
      try{
        string p=Path.GetFullPath(Path.Combine(icons,"..","rta","ban.png"));
        if(!File.Exists(p))p=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","rta","ban.png");
        if(File.Exists(p)){
          using(var fs=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
          using(var img=Image.FromStream(fs))banMark=new Bitmap(img);
        }
      }catch{}
      return banMark;
    }
    Image IconWithBan(int id){
      Image cached;if(banIconCache.TryGetValue(id,out cached))return cached;
      var bmp=new Bitmap(48,48);
      using(var g=Graphics.FromImage(bmp)){
        g.InterpolationMode=System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
        g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        g.PixelOffsetMode=System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
        var src=IconFor(id);
        if(src!=null)g.DrawImage(src,0,0,48,48);
        var mark=BanMark();
        if(mark!=null)g.DrawImage(mark,0,0,48,48);
      }
      banIconCache[id]=bmp;return bmp;
    }
    void RenderSlots(){
      ours.Controls.Clear();enemies.Controls.Clear();
      var recos=OurTurn()?SuggestedOurs():new List<RtaMonster>();
      int banId=BanPhase()?banAdviceId:0;
      for(int i=0;i<5;i++){
        if(i<ownPicks.Count)ours.Controls.Add(Slot(ownPicks[i],true,false,i,false));
        else if(i-ownPicks.Count<recos.Count)ours.Controls.Add(Slot(recos[i-ownPicks.Count],true,true,i,false));
        else ours.Controls.Add(Slot(null,true,false,i,false));
        var foe=i<enemyPicks.Count?enemyPicks[i]:null;
        enemies.Controls.Add(Slot(foe,false,false,i,banId>0&&foe!=null&&foe.Id==banId));
      }
    }
    List<RtaMonster> SuggestedOurs(){
      var list=new List<RtaMonster>();
      if(!draftLive||adviceAll==null||adviceAll.Count==0||ownPicks.Count>=5)return list;
      if(firstSide==0||!OurTurn())return list;
      var takenId=new HashSet<int>(ownPicks.Concat(enemyPicks).Select(p=>p.Id).Where(id=>id>0));
      var takenName=new HashSet<string>(ownPicks.Concat(enemyPicks).Select(p=>p.Name??"").Where(nm=>nm.Length>0&&nm!="pick"),StringComparer.OrdinalIgnoreCase);
      int n=PicksThisTurn();
      if(n<=0)return list;
      if(holdTurn!=null&&holdEnemyN==enemyPicks.Count){
        foreach(var m in holdTurn){
          if(m==null||takenId.Contains(m.Id)||takenName.Contains(m.Name??""))continue;
          list.Add(m);
          if(list.Count>=n)break;
        }
        if(list.Count>0)return list;
      }
      if(firstSide==1&&ownPicks.Count==0){
        RtaMonster locked=null;
        foreach(var rec in adviceAll.OrderByDescending(x=>x.ScoreValue)){
          if(rec==null||rec.Source==null)continue;
          if(holdOpenId==0){holdOpenId=rec.Source.Id;locked=rec.Source;break;}
          if(rec.Source.Id==holdOpenId||(rec.Source.Id/100==holdOpenId/100&&rec.Source.Id%10==holdOpenId%10)){locked=rec.Source;break;}
        }
        if(locked!=null)list.Add(locked);
        holdTurn=list;holdEnemyN=enemyPicks.Count;
        return list;
      }
      list=PickCombo(n,takenId,takenName);
      holdTurn=list;holdEnemyN=enemyPicks.Count;
      return list;
    }
    List<RtaMonster> PickCombo(int n,HashSet<int> takenId,HashSet<string> takenName){
      var pool=new List<RtaMonster>();
      foreach(var rec in adviceAll.OrderByDescending(x=>x.ScoreValue)){
        if(rec==null||rec.Source==null)continue;
        var m=rec.Source;
        if(takenId.Contains(m.Id)||takenName.Contains(m.Name??""))continue;
        if(ownPicks.Concat(enemyPicks).Any(p=>p.Id>0&&m.Id>0&&p.Id/100==m.Id/100&&p.Id%10==m.Id%10))continue;
        pool.Add(m);if(pool.Count>=20)break;
      }
      if(n<=1||pool.Count<=1)return pool.Take(Math.Max(0,n)).ToList();
      double best=-1e9;int ia=-1,ib=-1;
      double togetherW=RtaPickScore.ComboTogetherWeight(enemyPicks.Count);
      for(int i=0;i<pool.Count;i++)for(int j=i+1;j<pool.Count;j++){
        double s=SoloScore(pool[i])+SoloScore(pool[j])+TogetherScore(pool[i],pool[j])*togetherW+TeamFit(pool[i])+TeamFit(pool[j]);
        if(s>best){best=s;ia=i;ib=j;}
      }
      var pair=new List<RtaMonster>();
      if(ia<0)return pool.Take(n).ToList();
      if(SoloScore(pool[ia])>=SoloScore(pool[ib])){pair.Add(pool[ia]);pair.Add(pool[ib]);}
      else{pair.Add(pool[ib]);pair.Add(pool[ia]);}
      return pair;
    }
    double SoloScore(RtaMonster m){
      if(m==null||adviceAll==null)return 0;
      foreach(var rec in adviceAll)if(rec!=null&&rec.Source!=null&&rec.Source.Id==m.Id)return rec.ScoreValue;
      return 0;
    }
    double TeamFit(RtaMonster m){
      if(m==null||ownPicks.Count==0)return 0;
      double t=0;foreach(var ally in ownPicks)t+=TogetherScore(m,ally);
      return t*RtaPickScore.TeamFitWeight(enemyPicks.Count);
    }
    void ClearTurnHold(){holdTurn=null;holdEnemyN=-1;}
    Control Slot(RtaMonster m,bool mine,bool suggested,int index,bool banned){
      var p=new Panel{Width=90,Height=58,Margin=new Padding(3),BackColor=banned?Color.FromArgb(72,22,22):(suggested?Color.FromArgb(42,58,22):Color.FromArgb(20,34,49))};
      if(m==null){
        p.Controls.Add(new Label{Text="?",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.DimGray,Font=new Font("Segoe UI",20)});
        BindClick(p,(s,e)=>PlaceOnSlot(mine,index,false,null));
        return p;
      }
      var pic=new PictureBox{Image=banned?IconWithBan(m.Id):IconFor(m.Id),Location=new Point(2,2),Size=new Size(48,48),SizeMode=PictureBoxSizeMode.Zoom};
      string label=suggested?Loc.T("rta_slot_advice"):(string.IsNullOrEmpty(m.Name)?"?":m.Name);
      if(suggested&&!string.IsNullOrEmpty(m.Name))label=m.Name;
      var name=new Label{Text=label,Location=new Point(51,3),Size=new Size(37,48),ForeColor=suggested?Color.FromArgb(255,214,96):(mine?Color.FromArgb(95,220,255):Color.FromArgb(255,120,115)),Font=new Font("Segoe UI",7),AutoEllipsis=true};
      p.Controls.Add(pic);p.Controls.Add(name);
      BindClick(p,(s,e)=>PlaceOnSlot(mine,index,suggested,m));
      return p;
    }
    void PlaceOnSlot(bool mine,int index,bool suggested,RtaMonster current){
      if(ignoreSlotClicks||(pickerOverlay!=null&&pickerOverlay.Visible))return;
      if(current!=null&&!suggested){
        skipAuto=true;
        if(mine)ownPicks.Remove(current);else enemyPicks.Remove(current);
        RefreshAll();return;
      }
      if(suggested&&current!=null){CommitPick(mine,index,current);return;}
      pickerMine=mine;pickerIndex=index;pickerCommitted=false;
      OpenMonsterPicker(mine?Loc.T("rta_pick_mine"):Loc.T("rta_pick_foe"));
    }
    RtaMonster Resolve(RtaMonster m){
      if(m==null)return null;
      var hit=stats.FirstOrDefault(x=>x.Id==m.Id);
      return hit??m;
    }
    bool Taken(int id){return ownPicks.Any(x=>x.Id==id)||enemyPicks.Any(x=>x.Id==id);}
    void RememberFirstPick(){if(firstSide!=0)return;if(ownPicks.Count==1&&enemyPicks.Count==0)firstSide=1;else if(ownPicks.Count==0&&enemyPicks.Count==1)firstSide=2;}
    int PoolN(){return poolSize.SelectedItem==null?RtaBuildOptimizer.LoadPoolSize():Convert.ToInt32(poolSize.SelectedItem);}
    HashSet<int> CurrentPoolIds(){
      int n=PoolN();
      if(poolIdCache!=null&&poolIdCacheN==n)return poolIdCache;
      poolIdCache=stats.Count==0?new HashSet<int>():RtaPoolTracker.PickableIds(json,catalog,stats,n);
      poolIdCacheN=n;
      return poolIdCache;
    }
    string BestNames(int n){
      if(holdTurn!=null&&holdTurn.Count>0&&OurTurn()){
        var left=holdTurn.Where(m=>m!=null&&!ownPicks.Any(p=>p.Id==m.Id)).Take(Math.Max(1,n)).Select(x=>x.Name).ToArray();
        if(left.Length>0)return string.Join("  +  ",left);
      }
      if(adviceAll==null||adviceAll.Count==0)return "—";return string.Join("  +  ",adviceAll.OrderByDescending(x=>x.ScoreValue).Take(Math.Max(1,n)).Select(x=>x.Monster).ToArray());
    }
    bool OurTurn(){
      int a=ownPicks.Count,b=enemyPicks.Count;
      if(a>=5&&b>=5)return false;
      if(firstSide==1){if(b==0)return a==0;if(b==2)return a<3;if(b==4)return a<5;return false;}
      if(firstSide==2){if(b==1)return a<2;if(b==3)return a<4;if(b==5)return a<5;return false;}
      return a==0&&b==0;
    }
    int PicksThisTurn(){
      int a=ownPicks.Count;
      if(firstSide==1){if(a==0)return 1;if(a<3)return 3-a;return Math.Max(1,5-a);}
      if(firstSide==2){if(a<2)return 2-a;if(a<4)return 4-a;return Math.Max(1,5-a);}
      return 1;
    }
    void SetPhase(){
      RememberFirstPick();
      int a=ownPicks.Count,b=enemyPicks.Count,n=PoolN();
      if(firstSide==0&&a==0&&b==0){phase.Text=Loc.T("rta_phase_idle");phase.ForeColor=Color.Silver;return;}
      if(a>=5&&b>=5){phase.Text=Loc.T("rta_phase_ban",BestNames(1));phase.ForeColor=Pink;return;}
      if(firstSide==1&&a==0){phase.Text=Loc.T("rta_phase_you_first",BestNames(1));phase.ForeColor=Color.FromArgb(120,235,160);return;}
      if(firstSide==2&&b==0){phase.Text=Loc.T("rta_phase_foe_first");phase.ForeColor=Color.FromArgb(255,170,80);return;}
      if(firstSide==2&&a==0){phase.Text=Loc.T("rta_phase_answer",PicksThisTurn(),BestNames(PicksThisTurn()));phase.ForeColor=Color.FromArgb(120,235,160);return;}
      if(firstSide==1&&a>=1&&b==0){phase.Text=Loc.T("rta_phase_wait_two");phase.ForeColor=Cyan;return;}
      if(!OurTurn()){phase.Text=Loc.T("rta_phase_wait");phase.ForeColor=Color.FromArgb(255,140,130);return;}
      int take=PicksThisTurn();
      phase.Text=take==1?Loc.T("rta_phase_one",n,BestNames(take)):Loc.T("rta_phase_many",take,n,BestNames(take));phase.ForeColor=Color.FromArgb(120,235,160);
    }
    void EnsurePairs(IEnumerable<RtaMonster> monsters){foreach(var m in monsters.Where(x=>x!=null&&!pairs.ContainsKey(x.Id)).Take(40)){try{pairs[m.Id]=RtaData.LoadPairs(m.Id);}catch{pairs[m.Id]=new Dictionary<int,RtaPair>();}}}
    double PairValue(int a,int b,bool together,out int sample){sample=0;Dictionary<int,RtaPair> map;RtaPair p;if(!pairs.TryGetValue(a,out map)){try{map=RtaData.LoadPairs(a);}catch{map=new Dictionary<int,RtaPair>();}pairs[a]=map;}if(!map.TryGetValue(b,out p))return .5;sample=together?p.Together:p.Against;return together?p.WinTogether:p.WinAgainst;}
    double TogetherScore(RtaMonster a,RtaMonster b){
      if(a==null||b==null||a.Id==b.Id)return 0;
      int n;double w=PairValue(a.Id,b.Id,true,out n);
      if(n<20){int n2;double w2=PairValue(b.Id,a.Id,true,out n2);if(n2>n){n=n2;w=w2;}}
      double luck=n>=20?(w-.5)*100*Math.Min(1,n/300.0):0;
      double togetherFreq=n>=80?Math.Min(7,Math.Log10(n)*2.2):0;
      int sw=RtaBuildOptimizer.RtaMetaBuilds.SynCount(a.Id,a.Name,b.Id,b.Name);
      double freq=sw>=200?Math.Min(8,Math.Log10(sw)*2.0):0;
      return luck+togetherFreq+freq;
    }
    void ScoreCandidate(RtaMonster m,bool ban,out double score,out string why){
      double synergy=0,pickCounter=0,banCounter=0,glue=0;
      int syN=0,coN=0,gN=0;
      var beats=new List<string>();
      foreach(var ally in ownPicks){synergy+=TogetherScore(m,ally);syN++;}
      foreach(var foe in enemyPicks){
        int n;double w=PairValue(m.Id,foe.Id,false,out n);
        if(n>=20){banCounter+=(w-.5)*100*Math.Min(1,n/300.0);coN++;}
        pickCounter+=RtaPickScore.MatchupPoints(w,n,foe.WinRate,foe.PickRate,foe.BanRate,foe.LeadRate);
        if(n>=8&&w>=.52&&!string.IsNullOrEmpty(foe.Name))beats.Add(foe.Name);
        if(ban&&foe.Id!=m.Id){glue+=TogetherScore(m,foe);gN++;}
      }
      int eN=enemyPicks.Count;
      score=m.WinRate*100;
      if(syN>0)score+=synergy*RtaPickScore.SynergyWeight(eN);
      if(eN>0)score+=pickCounter*RtaPickScore.CounterWeight(eN);
      score+=Math.Min(3,Math.Log10(Math.Max(1,m.Played))*.55)+m.BanRate*5;
      bool leaderNeeded=ownPicks.Count==0||ownPicks.Max(x=>x.LeadRate)<.25;
      if(leaderNeeded)score+=m.LeadRate*8+m.PickRate*2;else score+=m.LeadRate*2;
      if(ban)score=m.BanRate*35+m.WinRate*20-banCounter/Math.Max(1,coN)+(gN>0?glue/gN*1.2:0);
      string with=string.Join(", ",ownPicks.Where(ally=>TogetherScore(m,ally)>1).Select(ally=>ally.Name).ToArray());
      string beat=string.Join(", ",beats.ToArray());
      if(ban)why=(gN>0&&glue>0?Loc.T("rta_why_glue"):Loc.T("rta_why_threat"))+Loc.T("rta_why_ban",(m.BanRate*100).ToString("0.0"));
      else if(eN>0&&pickCounter>2&&beat.Length>0)why=Loc.T("rta_why_vs",beats.Count)+(beat.Length>0?" • "+beat:"");
      else if(with.Length>0)why=Loc.T("rta_why_syn",with);
      else if(coN>0||(eN>0&&pickCounter!=0))why=Loc.T("rta_why_vs",Math.Max(1,coN));
      else why=Loc.T("rta_why_flex");
    }
    void RefreshAdvice(){if(stats.Count==0){banAdviceId=0;return;}EnsurePairs(ownPicks.Concat(enemyPicks));bool ban=ownPicks.Count>=5&&enemyPicks.Count>=5;var used=new HashSet<int>(ownPicks.Concat(enemyPicks).Select(x=>x.Id).Where(id=>id>0));var usedNames=new HashSet<string>(ownPicks.Concat(enemyPicks).Select(x=>x.Name??"").Where(nm=>nm.Length>0&&nm!="pick"&&!nm.StartsWith("#")),StringComparer.OrdinalIgnoreCase);var usedFam=new HashSet<int>(ownPicks.Concat(enemyPicks).Where(x=>x.Id>0).Select(x=>(x.Id/100)*10+(x.Id%10)));int poolN=poolSize.SelectedItem==null?RtaBuildOptimizer.LoadPoolSize():Convert.ToInt32(poolSize.SelectedItem);var poolIds=RtaPoolTracker.PickableIds(json,catalog,stats,poolN);if(poolIds.Count==0)poolIds=new HashSet<int>(owned.Keys);IEnumerable<RtaMonster> candidates=ban?enemyPicks:stats.Where(x=>poolIds.Contains(x.Id)&&!used.Contains(x.Id)&&!usedNames.Contains(x.Name)&&!usedFam.Contains((x.Id/100)*10+(x.Id%10)));var candList=candidates.ToList();if(!ban&&candList.Count==0)candList=stats.Where(x=>owned.ContainsKey(x.Id)&&!used.Contains(x.Id)&&!usedNames.Contains(x.Name)&&!usedFam.Contains((x.Id/100)*10+(x.Id%10))).ToList();candidates=candList;EnsurePairs(candList.Take(40));var rows=new List<Tuple<RtaMonster,double,string>>();foreach(var m in candidates){double score;string why;ScoreCandidate(m,ban,out score,out why);rows.Add(Tuple.Create(m,score,why));}
      var view=rows.OrderByDescending(x=>x.Item2).ThenByDescending(x=>x.Item1.Played).Select((x,i)=>{var m=x.Item1;var meta=RtaBuildOptimizer.RtaMetaBuilds.ForId(m.Id)??RtaBuildOptimizer.RtaMetaBuilds.For(m.Name);return new RtaRecommendation{Rank=i+1,Icon=(ban&&i==0)?IconWithBan(m.Id):IconFor(m.Id),Monster=m.Name,WinRate=(m.WinRate*100).ToString("0.00")+" %",Pick=(m.PickRate*100).ToString("0.00")+" %",Ban=(m.BanRate*100).ToString("0.00")+" %",Lead=(m.LeadRate*100).ToString("0")+" %",Games=m.Played.ToString("N0"),Sets=meta!=null?meta.SetsText(2):"—",Subs=meta!=null&&!string.IsNullOrEmpty(meta.SubsText)?meta.SubsText:(meta!=null&&meta.Focus!=null?string.Join(" > ",meta.Focus):"—"),Score=x.Item2.ToString("0.0"),ScoreValue=x.Item2,Conseil=x.Item3+(m.LeadRate>=.25?" • leader "+(m.LeadRate*100).ToString("0")+" %":"")+(meta!=null&&!string.IsNullOrEmpty(meta.SynergyText)?" • duo "+meta.SynergyText:"")+(meta!=null&&!string.IsNullOrEmpty(meta.SlotMains)?" • "+meta.SlotMains:""),Source=m};}).ToList();adviceAll=view;banAdviceId=(ban&&view.Count>0&&view[0].Source!=null)?view[0].Source.Id:0;ApplyAdviceSort();}
    void ApplyAdviceSort(){
      if(adviceAll==null||adviceAll.Count==0){grid.DataSource=null;return;}
      Func<RtaRecommendation,double> key=x=>{
        if(x==null||x.Source==null)return 0;
        if(sortCol=="WinRate")return x.Source.WinRate;
        if(sortCol=="Pick")return x.Source.PickRate;
        if(sortCol=="Ban")return x.Source.BanRate;
        if(sortCol=="Lead")return x.Source.LeadRate;
        if(sortCol=="Games")return x.Source.Played;
        return x.ScoreValue;
      };
      IEnumerable<RtaRecommendation> q=sortDesc?adviceAll.OrderByDescending(key).ThenByDescending(x=>x.Source!=null?x.Source.Played:0):adviceAll.OrderBy(key).ThenByDescending(x=>x.Source!=null?x.Source.Played:0);
      var view=q.Take(80).Select((x,i)=>{x.Rank=i+1;return x;}).ToList();
      grid.DataSource=null;grid.DataSource=view;
      foreach(DataGridViewColumn c in grid.Columns)c.HeaderCell.SortGlyphDirection=SortOrder.None;
      foreach(DataGridViewColumn c in grid.Columns)if(c.DataPropertyName==sortCol)c.HeaderCell.SortGlyphDirection=sortDesc?SortOrder.Descending:SortOrder.Ascending;
    }
    static string BuildHint(string name){var meta=RtaBuildOptimizer.RtaMetaBuilds.For(name);if(meta==null)return "";return " • SWLens : "+meta.SetsText(2)+" • subs "+string.Join("/",meta.Focus);}
    int TurnKey(){return firstSide*1000+ownPicks.Count*20+enemyPicks.Count*2+(OurTurn()?1:0);}
    void AutoCommitTurn(){
      if(!draftLive||firstSide==0||ownPicks.Count>=5||!OurTurn())return;
      if(enemyPicks.Count!=lastAutoEnemyN){skipAuto=false;lastAutoEnemyN=enemyPicks.Count;}
      if(skipAuto)return;
      int n=PicksThisTurn();
      if(n<=0)return;
      foreach(var m in SuggestedOurs()){
        if(n<=0||ownPicks.Count>=5)break;
        if(m==null)continue;
        var placed=Resolve(m);
        if(placed==null||Taken(placed.Id))continue;
        ownPicks.Add(placed);n--;lastSide=1;
      }
    }
    void RefreshAll(){
      int key=TurnKey();
      bool opening=draftLive&&firstSide==1&&ownPicks.Count==0&&OurTurn();
      bool waiting=draftLive&&firstSide!=0&&!OurTurn();
      if(!(opening&&holdOpenId!=0&&adviceAll!=null&&adviceAll.Count>0)&&!(waiting&&lastTurnKey==key&&adviceAll!=null&&adviceAll.Count>0)){
        RefreshAdvice();
        lastTurnKey=key;
      }
      int before=ownPicks.Count;
      AutoCommitTurn();
      if(ownPicks.Count!=before){RefreshAdvice();lastTurnKey=TurnKey();}
      if(ownPicks.Count>0)holdOpenId=0;
      RenderSlots();SetPhase();
    }
    void Undo(){skipAuto=true;if(lastSide==2&&enemyPicks.Count>0)enemyPicks.RemoveAt(enemyPicks.Count-1);else if(ownPicks.Count>0)ownPicks.RemoveAt(ownPicks.Count-1);else if(enemyPicks.Count>0)enemyPicks.RemoveAt(enemyPicks.Count-1);RefreshAll();}
  }
}
