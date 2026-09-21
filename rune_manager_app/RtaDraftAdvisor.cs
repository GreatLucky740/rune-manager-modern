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
      foreach(var raw in A(js.DeserializeObject(File.ReadAllText(catalogPath)))){var d=D(raw);int id=I(G(d,"id"));if(id>0){names[id]=Convert.ToString(G(d,"name","Monstre "+id));groups[id]=I(G(d,"skillgroup"));elements[id]=Convert.ToString(G(d,"element"));}}
      var root=D(js.DeserializeObject(ReadShared(jsonPath)));var result=new Dictionary<int,string>();
      foreach(var raw in A(G(root,"unit_list"))){var d=D(raw);int id=I(G(d,"unit_master_id"));if(id>0&&!result.ContainsKey(id))result[id]=names.ContainsKey(id)?names[id]:"Monstre "+id;}
      // Les éditions collaboration et leurs équivalents permanents partagent le
      // même groupe de sorts et le même élément. SWArena peut référencer l'une ou
      // l'autre : les deux identifiants sont donc considérés comme possédés.
      var ownedIds=result.Keys.ToList();foreach(int ownedId in ownedIds){int group;string element;if(!groups.TryGetValue(ownedId,out group)||group<=0||!elements.TryGetValue(ownedId,out element))continue;foreach(int equivalent in groups.Where(x=>x.Value==group).Select(x=>x.Key)){string otherElement;if(elements.TryGetValue(equivalent,out otherElement)&&string.Equals(element,otherElement,StringComparison.OrdinalIgnoreCase)&&!result.ContainsKey(equivalent))result[equivalent]=(names.ContainsKey(equivalent)?names[equivalent]:"Monstre "+equivalent)+" (équivalent possédé)";}}
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
  sealed class RtaAdvisorForm:Form {
    readonly string json,catalog,icons;readonly Icon appIcon;readonly Color Bg=Color.FromArgb(7,13,22),Panel=Color.FromArgb(15,25,39),Pink=Color.FromArgb(190,68,145),Cyan=Color.FromArgb(20,184,210);
    readonly List<RtaMonster> ownPicks=new List<RtaMonster>(),enemyPicks=new List<RtaMonster>();List<RtaMonster> stats=new List<RtaMonster>();Dictionary<int,string> owned=new Dictionary<int,string>();readonly Dictionary<int,Dictionary<int,RtaPair>> pairs=new Dictionary<int,Dictionary<int,RtaPair>>();
    readonly FlowLayoutPanel ours=new FlowLayoutPanel(),enemies=new FlowLayoutPanel();readonly BufferedGrid grid=new BufferedGrid();readonly Label phase=new Label(),state=new Label();readonly TextBox search=new TextBox();readonly ComboBox side=new ComboBox(),poolSize=new ComboBox();Button poolButton;readonly CountBadge poolBadge=new CountBadge();RtaPoolDiff poolDiff;List<RtaRecommendation> adviceAll=new List<RtaRecommendation>();string sortCol="Score";bool sortDesc=true;readonly Timer captureTimer=new Timer();bool loading,captureBusy;string pendingCapture="";int pendingCaptureCount;
    public RtaAdvisorForm(string jsonPath,string catalogPath,Icon icon){json=jsonPath;catalog=catalogPath;icons=Path.GetDirectoryName(catalogPath);appIcon=icon;Text="Assistant Pick / Ban RTA";Icon=icon;BackColor=Bg;ForeColor=Color.White;Size=new Size(1680,860);MinimumSize=new Size(1200,700);StartPosition=FormStartPosition.CenterParent;Build();captureTimer.Interval=900;captureTimer.Tick+=(s,e)=>ScanDraft();Shown+=(s,e)=>LoadData();FormClosed+=(s,e)=>captureTimer.Stop();}
    Button B(string t,int w,Color c){return new Button{Text=t,Width=w,Height=32,FlatStyle=FlatStyle.Flat,BackColor=c,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",9),Cursor=Cursors.Hand};}
    void Build(){
      var head=new Panel{Dock=DockStyle.Top,Height=192,BackColor=Panel,Padding=new Padding(18,10,18,8)};Controls.Add(head);head.Controls.Add(new Label{Text="RTA  •  ASSISTANT PICK / BAN",AutoSize=true,Location=new Point(18,8),ForeColor=Color.FromArgb(255,82,180),Font=new Font("Segoe UI Semibold",20)});
      phase.SetBounds(18,47,900,25);phase.ForeColor=Color.FromArgb(100,235,175);phase.Font=new Font("Segoe UI Semibold",11);head.Controls.Add(phase);state.SetBounds(18,72,1180,23);state.ForeColor=Color.Silver;head.Controls.Add(state);
      var ownLabel=new Label{Text="TES PICKS",Location=new Point(18,103),Size=new Size(120,22),ForeColor=Cyan,Font=new Font("Segoe UI Semibold",10)};var enemyLabel=new Label{Text="PICKS ADVERSES",Location=new Point(650,103),Size=new Size(160,22),ForeColor=Color.FromArgb(255,105,100),Font=new Font("Segoe UI Semibold",10)};head.Controls.Add(ownLabel);head.Controls.Add(enemyLabel);
      ours.SetBounds(135,99,490,66);ours.WrapContents=false;ours.BackColor=Bg;enemies.SetBounds(810,99,490,66);enemies.WrapContents=false;enemies.BackColor=Bg;head.Controls.Add(ours);head.Controls.Add(enemies);
      var tools=new Panel{Dock=DockStyle.Top,Height=52,BackColor=Color.FromArgb(10,20,32),Padding=new Padding(18,9,18,7)};Controls.Add(tools);var capture=B("CADRER LE DRAFT",170,Cyan);capture.SetBounds(18,9,170,32);capture.Click+=(s,e)=>{captureTimer.Stop();if(!RtaDraftCapture.ChooseRegion(this).IsEmpty){state.Text="Capture du draft active • les picks se rempliront automatiquement";captureTimer.Start();ScanDraft();}};tools.Controls.Add(capture);var live=new Label{Text="● CAPTURE AUTO",Location=new Point(198,16),AutoSize=true,ForeColor=Color.FromArgb(70,225,145),Font=new Font("Segoe UI Semibold",9)};tools.Controls.Add(live);search.SetBounds(315,10,170,30);search.BackColor=Bg;search.ForeColor=Color.White;search.BorderStyle=BorderStyle.FixedSingle;search.TextChanged+=(s,e)=>RefreshAdvice();tools.Controls.Add(search);side.Items.AddRange(new object[]{"Ajouter à mes picks","Ajouter à l'adversaire"});side.SelectedIndex=0;var reset=B("NOUVEAU DRAFT",135,Pink);reset.SetBounds(495,9,135,32);reset.Click+=(s,e)=>{ownPicks.Clear();enemyPicks.Clear();LoadData();};tools.Controls.Add(reset);poolSize.DropDownStyle=ComboBoxStyle.DropDownList;poolSize.Items.AddRange(new object[]{10,20,30,40,50,60});poolSize.SetBounds(640,10,70,30);int savedPool=RtaBuildOptimizer.LoadPoolSize();poolSize.SelectedItem=poolSize.Items.Cast<object>().FirstOrDefault(x=>Convert.ToInt32(x)==savedPool)??20;poolSize.SelectedIndexChanged+=(s,e)=>{RtaBuildOptimizer.SavePoolSize(Convert.ToInt32(poolSize.SelectedItem));RefreshPoolBadge();RefreshAdvice();};tools.Controls.Add(poolSize);poolButton=B("POOL + BUILDS",188,Color.FromArgb(36,137,112));poolButton.SetBounds(720,9,188,32);poolButton.Padding=new Padding(8,0,8,0);poolButton.Click+=(s,e)=>OpenPool();tools.Controls.Add(poolButton);poolBadge.UseNewTag=false;poolBadge.SetBounds(4,2,28,28);poolBadge.Text="0";poolBadge.Visible=false;poolBadge.Font=new Font("Segoe UI Semibold",9f);poolBadge.BadgeColor=Color.FromArgb(255,170,40);poolBadge.Click+=(s,e)=>OpenPool();poolButton.Controls.Add(poolBadge);poolBadge.BringToFront();var resetPool=B("RESET RTA",115,Color.FromArgb(125,65,65));resetPool.SetBounds(918,9,115,32);resetPool.Click+=(s,e)=>{if(MessageBox.Show("Réinitialiser la sélection et les réglages RTA ?","RTA",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;RtaBuildOptimizer.ResetSettings();poolSize.SelectedItem=20;ownPicks.Clear();enemyPicks.Clear();RefreshAll();RefreshPoolBadge();state.Text="Réglages et monstres RTA réinitialisés";};tools.Controls.Add(resetPool);var refresh=B("MAJ META",140,Color.FromArgb(76,72,155));refresh.SetBounds(1043,9,140,32);refresh.Click+=(s,e)=>LoadData(true);tools.Controls.Add(refresh);
      grid.Dock=DockStyle.Fill;grid.BackgroundColor=Bg;grid.BorderStyle=BorderStyle.None;grid.ReadOnly=true;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.RowHeadersVisible=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.RowTemplate.Height=58;grid.AutoGenerateColumns=false;grid.EnableHeadersVisualStyles=false;grid.ColumnHeadersHeight=42;grid.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(138,45,117),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10),SelectionBackColor=Color.FromArgb(138,45,117)};grid.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Bg,ForeColor=Color.White,Font=new Font("Segoe UI",10),SelectionBackColor=Color.FromArgb(45,55,83),SelectionForeColor=Color.White};
      grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Rank",HeaderText="#",Width=40});grid.Columns.Add(new DataGridViewImageColumn{DataPropertyName="Icon",HeaderText="Monstre",Width=58,ImageLayout=DataGridViewImageCellLayout.Zoom});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Monster",HeaderText="Nom",Width=150});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="WinRate",HeaderText="WR",Width=70});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Pick",HeaderText="Pick",Width=70});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Ban",HeaderText="Ban",Width=70});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Lead",HeaderText="Lead",Width=70});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Games",HeaderText="Games",Width=80});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Sets",HeaderText="Sets SWLens",Width=300});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Subs",HeaderText="Top subs",Width=220});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Score",HeaderText="Indice",Width=80});grid.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName="Conseil",HeaderText="Pourquoi",AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill});foreach(DataGridViewColumn c in grid.Columns){string p=c.DataPropertyName;c.SortMode=(p=="WinRate"||p=="Pick"||p=="Ban"||p=="Lead"||p=="Games"||p=="Score")?DataGridViewColumnSortMode.Programmatic:DataGridViewColumnSortMode.NotSortable;if(c.SortMode==DataGridViewColumnSortMode.Programmatic)c.HeaderCell.ToolTipText="Clic : du meilleur au pire, reclic pour inverser";}
      grid.CellPainting+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex<0)return;if(grid.Columns[e.ColumnIndex].DataPropertyName!="Sets")return;var rec=grid.Rows[e.RowIndex].DataBoundItem as RtaRecommendation;RtaSetIcons.Paint(e,rec==null?Convert.ToString(e.FormattedValue):rec.Sets);};
      grid.ColumnHeaderMouseClick+=(s,e)=>{if(e.ColumnIndex<0)return;string p=grid.Columns[e.ColumnIndex].DataPropertyName;if(grid.Columns[e.ColumnIndex].SortMode!=DataGridViewColumnSortMode.Programmatic)return;if(sortCol==p)sortDesc=!sortDesc;else{sortCol=p;sortDesc=true;}ApplyAdviceSort();};
      grid.CellDoubleClick+=(s,e)=>{if(e.RowIndex>=0)AddSelected();};Controls.Add(grid);grid.BringToFront();RenderSlots();SetPhase();
    }
    void LoadData(bool live=false){if(loading)return;loading=true;state.Text=live?"Mise à jour meta RTA (LuckSack + SWLens)…":"Chargement des statistiques RTA (LuckSack) saison 38…";grid.DataSource=null;System.Threading.ThreadPool.QueueUserWorkItem(_=>{List<RtaMonster> loaded=null;Dictionary<int,string> roster=null;Exception error=null;string note=null;try{if(live)note=RtaMetaRefresh.Run(catalog);RtaBuildOptimizer.RtaMetaBuilds.EnsureLoaded(catalog);roster=RtaData.Owned(json,catalog);loaded=RtaData.LoadSeason(catalog);}catch(Exception ex){error=ex;}try{BeginInvoke((MethodInvoker)delegate{loading=false;if(error!=null){state.Text="Données RTA indisponibles : "+error.Message;return;}owned=roster;stats=loaded;state.Text=(note??(stats.Count+" monstres LuckSack S38 • top 300 pick + builds SWLens"))+" • "+owned.Count+" possédés • capture automatique prête";RefreshAll();RefreshPoolBadge();if(RtaDraftCapture.HasRegion){captureTimer.Start();ScanDraft();}else state.Text+=" • clique CADRER LE DRAFT une seule fois";});}catch{}});}
    void RefreshPoolBadge(){
      try{
        int n=poolSize.SelectedItem==null?RtaBuildOptimizer.LoadPoolSize():Convert.ToInt32(poolSize.SelectedItem);
        poolDiff=RtaPoolTracker.Diff(json,catalog,stats,n);
        int count=poolDiff==null?0:poolDiff.Count;
        poolBadge.UseNewTag=false;
        poolBadge.Text=count.ToString();
        poolBadge.Visible=count>0;
        poolButton.Padding=count>0?new Padding(30,0,8,0):new Padding(8,0,8,0);
        Color c=count>0?Color.FromArgb(218,70,62):Color.FromArgb(255,170,40);
        poolBadge.BadgeColor=c;poolButton.FlatAppearance.BorderColor=c;poolBadge.Invalidate();
      }catch{poolBadge.Text="0";poolBadge.Visible=false;poolButton.Padding=new Padding(8,0,8,0);}
    }
    void OpenPool(){
      int n=poolSize.SelectedItem==null?20:Convert.ToInt32(poolSize.SelectedItem);
      if(poolDiff==null)poolDiff=RtaPoolTracker.Diff(json,catalog,stats,n);
      if(poolDiff!=null&&poolDiff.Count>0){
        RtaPoolTracker.ShowChanges(this,poolDiff,IconFor);
        RtaPoolTracker.SaveSnapshot(n,poolDiff.Current);
        poolDiff.Entered.Clear();poolDiff.Left.Clear();
        RefreshPoolBadge();
      }
      RtaBuildOptimizer.Show(this,json,catalog,stats,IconFor,n);
    }
    void ScanDraft(){if(captureBusy||stats.Count==0)return;captureBusy=true;try{var frame=RtaDraftCapture.Scan(icons,stats);if(frame==null){state.Text="Capture RTA hors écran • clique CADRER LE DRAFT";captureTimer.Stop();return;}string signature=string.Join(",",frame.Ours)+"|"+string.Join(",",frame.Enemies);if(signature!=pendingCapture){pendingCapture=signature;pendingCaptureCount=1;return;}if(++pendingCaptureCount<2)return;var byId=stats.GroupBy(x=>x.Id).ToDictionary(g=>g.Key,g=>g.OrderByDescending(x=>x.Played).First());var nextOwn=frame.Ours.Where(owned.ContainsKey).Distinct().Where(byId.ContainsKey).Select(x=>byId[x]).Take(5).ToList();var nextEnemy=frame.Enemies.Distinct().Where(byId.ContainsKey).Select(x=>byId[x]).Take(5).ToList();if(nextOwn.Select(x=>x.Id).SequenceEqual(ownPicks.Select(x=>x.Id))&&nextEnemy.Select(x=>x.Id).SequenceEqual(enemyPicks.Select(x=>x.Id)))return;ownPicks.Clear();ownPicks.AddRange(nextOwn);enemyPicks.Clear();enemyPicks.AddRange(nextEnemy);state.Text="Draft lu automatiquement • "+ownPicks.Count+" pick(s) allié(s) • "+enemyPicks.Count+" adverse(s) • confiance "+(frame.Confidence*100).ToString("0")+" %";RefreshAll();}catch(Exception ex){state.Text="Capture RTA reportée : "+ex.Message;}finally{captureBusy=false;}}
    // Comme WorldBossMonsterIcon/MonsterPicture (RuneManagerApp.cs) : si l'id exact n'a
    // pas de portrait local, on cherche le fichier de la même famille (préfixe id/100 —
    // formes éveillées/non-éveillées), MAIS filtré au même élément (dernier chiffre de
    // l'id : 1 eau, 2 feu, 3 vent, 4 lumière, 5 ténèbres). Sans ce filtre, le "plus proche
    // numériquement" pouvait être un AUTRE élément de la même famille (ex. Nine-tailed Fox
    // Feu affichait l'icône Eau car 11211 est plus proche de 11202 que 11212). Mieux vaut
    // le placeholder générique qu'une icône d'un mauvais élément affichée avec assurance.
    Image IconFor(int id){
      string p=Path.Combine(icons,id+".png");
      if(!File.Exists(p)){try{
        string alt=Path.Combine(icons,(id+10)+".png");if(File.Exists(alt))p=alt;
        else{alt=Path.Combine(icons,(id-10)+".png");if(File.Exists(alt))p=alt;
        else{int element=id%10;p=null;if(element>=1&&element<=5){string prefix=(id/100).ToString();p=Directory.GetFiles(icons,prefix+"*.png").Where(f=>{int fid;return int.TryParse(Path.GetFileNameWithoutExtension(f),out fid)&&fid%10==element;}).OrderBy(f=>{int fid;int.TryParse(Path.GetFileNameWithoutExtension(f),out fid);return Math.Abs(fid-id);}).FirstOrDefault();}}}
      }catch{p=null;}}
      if(p!=null&&File.Exists(p))try{using(var f=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var img=Image.FromStream(f))return new Bitmap(img,new Size(48,48));}catch{}
      var b=new Bitmap(48,48);using(var g=Graphics.FromImage(b)){g.SmoothingMode=System.Drawing.Drawing2D.SmoothingMode.AntiAlias;using(var bg=new SolidBrush(Color.FromArgb(30,40,52)))g.FillRectangle(bg,0,0,48,48);using(var fill=new SolidBrush(Color.FromArgb(90,105,120)))g.FillEllipse(fill,4,4,40,40);using(var pen=new Pen(Color.FromArgb(150,165,180),1.5f))g.DrawEllipse(pen,4,4,40,40);}
      return b;
    }
    void RenderSlots(){ours.Controls.Clear();enemies.Controls.Clear();for(int i=0;i<5;i++){ours.Controls.Add(Slot(i<ownPicks.Count?ownPicks[i]:null,true));enemies.Controls.Add(Slot(i<enemyPicks.Count?enemyPicks[i]:null,false));}}
    Control Slot(RtaMonster m,bool mine){var p=new Panel{Width=90,Height=58,Margin=new Padding(3),BackColor=Color.FromArgb(20,34,49)};if(m==null){p.Controls.Add(new Label{Text="?",Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.DimGray,Font=new Font("Segoe UI",20)});return p;}var pic=new PictureBox{Image=IconFor(m.Id),Location=new Point(2,2),Size=new Size(48,48),SizeMode=PictureBoxSizeMode.Zoom};var name=new Label{Text=m.Name,Location=new Point(51,3),Size=new Size(37,48),ForeColor=mine?Color.FromArgb(95,220,255):Color.FromArgb(255,120,115),Font=new Font("Segoe UI",7),AutoEllipsis=true};p.Controls.Add(pic);p.Controls.Add(name);p.Tag=m;p.Cursor=Cursors.Hand;p.Click+=(s,e)=>{if(mine)ownPicks.Remove(m);else enemyPicks.Remove(m);RefreshAll();};return p;}
    void SetPhase(){int total=ownPicks.Count+enemyPicks.Count;if(ownPicks.Count>=5&&enemyPicks.Count>=5)phase.Text="PHASE BAN  •  le premier conseil indique la cible à bannir";else if(total==0)phase.Text="PREMIER PICK  •  priorité à un monstre fort, flexible et souvent leader";else phase.Text="PROCHAIN PICK  •  adapté à tes picks et aux menaces adverses";}
    void EnsurePairs(IEnumerable<RtaMonster> monsters){foreach(var m in monsters.Where(x=>x!=null&&!pairs.ContainsKey(x.Id)).Take(6)){try{pairs[m.Id]=RtaData.LoadPairs(m.Id);}catch{pairs[m.Id]=new Dictionary<int,RtaPair>();}}}
    double PairValue(int a,int b,bool together,out int sample){sample=0;Dictionary<int,RtaPair> map;RtaPair p;if(!pairs.TryGetValue(a,out map)){try{map=RtaData.LoadPairs(a);}catch{map=new Dictionary<int,RtaPair>();}pairs[a]=map;}if(!map.TryGetValue(b,out p))return .5;sample=together?p.Together:p.Against;return together?p.WinTogether:p.WinAgainst;}
    void RefreshAdvice(){if(stats.Count==0)return;EnsurePairs(ownPicks.Concat(enemyPicks));bool ban=ownPicks.Count>=5&&enemyPicks.Count>=5;var used=new HashSet<int>(ownPicks.Concat(enemyPicks).Select(x=>x.Id));bool enteringEnemy=side.SelectedIndex==1&&!ban;int poolN=poolSize.SelectedItem==null?RtaBuildOptimizer.LoadPoolSize():Convert.ToInt32(poolSize.SelectedItem);var poolIds=RtaPoolTracker.PickableIds(json,catalog,stats,poolN);if(poolIds.Count==0)poolIds=new HashSet<int>(owned.Keys);IEnumerable<RtaMonster> candidates=ban?enemyPicks:(enteringEnemy?stats.Where(x=>!used.Contains(x.Id)&&x.Played>=1):stats.Where(x=>poolIds.Contains(x.Id)&&!used.Contains(x.Id)));string find=search.Text.Trim();if(find.Length>0)candidates=candidates.Where(x=>x.Name.IndexOf(find,StringComparison.OrdinalIgnoreCase)>=0);var rows=new List<Tuple<RtaMonster,double,string>>();foreach(var m in candidates){double score=m.WinRate*100,synergy=0,counter=0;int syN=0,coN=0;foreach(var ally in ownPicks){int n;double w=PairValue(m.Id,ally.Id,true,out n);if(n>=20){synergy+=(w-.5)*100*Math.Min(1,n/300.0);syN++;}}foreach(var foe in enemyPicks){int n;double w=PairValue(m.Id,foe.Id,false,out n);if(n>=20){counter+=(w-.5)*100*Math.Min(1,n/300.0);coN++;}}if(syN>0)score+=synergy/syN*.70;if(coN>0)score+=counter/coN*1.10;score+=Math.Min(3,Math.Log10(Math.Max(1,m.Played))*.55)+m.BanRate*5;bool leaderNeeded=ownPicks.Count==0||ownPicks.Max(x=>x.LeadRate)<.25;if(leaderNeeded)score+=m.LeadRate*8+m.PickRate*2;else score+=m.LeadRate*2;if(ban)score=m.BanRate*35+m.WinRate*20-counter/Math.Max(1,coN);string why=ban?"Menace : ban "+(m.BanRate*100).ToString("0.0")+" %, force globale et matchups contre ton équipe":enteringEnemy?"Sélection du pick adverse":coN>0?"Contre les picks adverses • "+coN+" matchup(s) fiable(s)":syN>0?"Bonne synergie avec ton équipe":"Choix global solide et flexible";rows.Add(Tuple.Create(m,score,why));}
      var view=rows.Select((x,i)=>{var m=x.Item1;var meta=RtaBuildOptimizer.RtaMetaBuilds.ForId(m.Id)??RtaBuildOptimizer.RtaMetaBuilds.For(m.Name);return new RtaRecommendation{Rank=i+1,Icon=IconFor(m.Id),Monster=m.Name,WinRate=(m.WinRate*100).ToString("0.00")+" %",Pick=(m.PickRate*100).ToString("0.00")+" %",Ban=(m.BanRate*100).ToString("0.00")+" %",Lead=(m.LeadRate*100).ToString("0")+" %",Games=m.Played.ToString("N0"),Sets=meta!=null?meta.SetsText(2):"—",Subs=meta!=null&&!string.IsNullOrEmpty(meta.SubsText)?meta.SubsText:(meta!=null&&meta.Focus!=null?string.Join(" > ",meta.Focus):"—"),Score=x.Item2.ToString("0.0"),ScoreValue=x.Item2,Conseil=x.Item3+(m.LeadRate>=.25?" • leader "+(m.LeadRate*100).ToString("0")+" %":"")+(meta!=null&&!string.IsNullOrEmpty(meta.SynergyText)?" • duo "+meta.SynergyText:"")+(meta!=null&&!string.IsNullOrEmpty(meta.SlotMains)?" • "+meta.SlotMains:""),Source=m};}).ToList();adviceAll=view;ApplyAdviceSort();}
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
    // Ajoute le meilleur combo de sets + priorité de sous-stats connus (swlens.io RTA BUILDS)
    // quand on a la donnée pour ce monstre, pour répondre à la demande de Jeremy d'afficher
    // aussi les meilleurs sets/sub stats directement dans l'assistant pick/ban, pas seulement
    // dans POOL + BUILDS.
    static string BuildHint(string name){var meta=RtaBuildOptimizer.RtaMetaBuilds.For(name);if(meta==null)return "";return " • SWLens : "+meta.SetsText(2)+" • subs "+string.Join("/",meta.Focus);}
    void RefreshAll(){RenderSlots();SetPhase();RefreshAdvice();}
    RtaRecommendation Selected(){return grid.CurrentRow==null?null:grid.CurrentRow.DataBoundItem as RtaRecommendation;}
    void AddSelected(){var r=Selected();if(r==null)return;if(side.SelectedIndex==0){if(ownPicks.Count<5&&!ownPicks.Any(x=>x.Id==r.Source.Id)&&!enemyPicks.Any(x=>x.Id==r.Source.Id))ownPicks.Add(r.Source);}else{if(enemyPicks.Count<5&&!ownPicks.Any(x=>x.Id==r.Source.Id)&&!enemyPicks.Any(x=>x.Id==r.Source.Id))enemyPicks.Add(r.Source);}RefreshAll();}
    void Undo(){if(side.SelectedIndex==0&&ownPicks.Count>0)ownPicks.RemoveAt(ownPicks.Count-1);else if(side.SelectedIndex==1&&enemyPicks.Count>0)enemyPicks.RemoveAt(enemyPicks.Count-1);RefreshAll();}
  }
}
