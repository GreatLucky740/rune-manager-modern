using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Text;
using System.Net;
using System.Net.Cache;
using System.Collections;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace RuneManagerModern {
  sealed class BufferedGrid:DataGridView {
    public BufferedGrid(){DoubleBuffered=true;SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint,true);UpdateStyles();}
  }
  sealed class BufferedFlow:FlowLayoutPanel {
    public BufferedFlow(){DoubleBuffered=true;SetStyle(ControlStyles.OptimizedDoubleBuffer|ControlStyles.AllPaintingInWmPaint|ControlStyles.UserPaint,true);UpdateStyles();}
  }
  sealed class CountBadge:Control {
    public Color BadgeColor=Color.FromArgb(5,86,82);
    public static Image NewIcon;
    public bool UseNewTag;
    public CountBadge(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;ForeColor=Color.White;Font=new Font("Segoe UI",9.75f,FontStyle.Bold);Cursor=Cursors.Hand;Size=new Size(30,30);}
    protected override void OnPaint(PaintEventArgs e){
      base.OnPaint(e);
      e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;
      if(UseNewTag){
        if(NewIcon==null)return;
        e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;
        e.Graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
        float iw=NewIcon.Width,ih=NewIcon.Height;
        if(iw<1||ih<1)return;
        float s=Math.Min(ClientSize.Width/iw,ClientSize.Height/ih);
        int dw=Math.Max(1,(int)Math.Round(iw*s)),dh=Math.Max(1,(int)Math.Round(ih*s));
        e.Graphics.DrawImage(NewIcon,new Rectangle((ClientSize.Width-dw)/2,(ClientSize.Height-dh)/2,dw,dh));
        return;
      }
      var rect=new Rectangle(3,3,Width-7,Height-7);
      using(var path=Rounded(rect,6)){
        using(var fill=new SolidBrush(Color.FromArgb(22,34,50)))e.Graphics.FillPath(fill,path);
        using(var pen=new Pen(BadgeColor,1.5f))e.Graphics.DrawPath(pen,path);
      }
      var textFont=(Text!=null&&Text.Length>=3)?new Font(Font.FontFamily,Font.Size*0.8f,Font.Style):Font;
      TextRenderer.DrawText(e.Graphics,Text,textFont,rect,ForeColor,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);
      if(textFont!=Font)textFont.Dispose();
    }
    static GraphicsPath Rounded(Rectangle r,int radius){var p=new GraphicsPath();int d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
    protected override void OnTextChanged(EventArgs e){
      base.OnTextChanged(e);
      if(UseNewTag){
        int n;string t=(Text??"").Replace(" ","").Replace("\u00A0","").Replace(",","");
        Visible=int.TryParse(t,NumberStyles.Integer,CultureInfo.InvariantCulture,out n)&&n>0;
      }
      Invalidate();
    }
  }
  sealed class IconBadge:Control {
    // Remplace Label+BackgroundImage : celui-ci laissait voir le carre noir de l'image
    // source (fond non transparent / Zoom qui deforme) autour de la pierre. Ici on
    // decoupe l'icone dans un cercle propre (anti-alias) et on dessine nous-memes le
    // fond degrade + le compteur, donc plus aucun carre noir visible.
    public Image IconImage;
    public Color BadgeColor=Color.FromArgb(17,130,121);
    public bool WideLayout=false;
    // Pour un badge WideLayout place a GAUCHE du texte du bouton (ex: pierre normale
    // dans TRI REEVAL) : colle le bloc icone+"xN" vers la droite du badge (donc pres
    // du texte), au lieu de vers la gauche (qui le colle au bord du bouton).
    public bool ContentAlignRight=false;
    public IconBadge(){SetStyle(ControlStyles.UserPaint|ControlStyles.AllPaintingInWmPaint|ControlStyles.OptimizedDoubleBuffer|ControlStyles.SupportsTransparentBackColor,true);BackColor=Color.Transparent;ForeColor=Color.White;Font=new Font("Segoe UI Semibold",7f,FontStyle.Bold);Cursor=Cursors.Hand;}
    protected override void OnPaint(PaintEventArgs e){
      base.OnPaint(e);var g=e.Graphics;g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
      // Plus de rond/pilule colore derriere (abandonne apres test) : juste l'icone +
      // le texte, directement sur le fond du bouton.
      // Non-wide : zone de texte fixe en pixels (pas une fraction de Height) pour
      // garantir que "xN" ne soit jamais rogne, quelle que soit la taille du badge.
      // L'icone prend tout le reste au-dessus, agrandie au maximum.
      const int textZone=14,textGap=1,topMargin=1;
      int d=WideLayout?Height-4:Math.Max(10,Height-textZone-textGap-topMargin);
      Rectangle iconRect;
      if(WideLayout){
        // Icone+texte formen un seul bloc, colle du cote du texte du bouton (avant :
        // icone toujours collee a gauche du badge => pour le badge de gauche (avant
        // "TRI REEVAL"), le bloc se retrouvait colle au bord du bouton au lieu d'etre
        // pres du texte). On mesure le texte reel pour placer le bloc correctement.
        var textSize=TextRenderer.MeasureText(Text,Font,new Size(int.MaxValue,int.MaxValue),TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);
        int unit=d+2+textSize.Width;
        int startX=ContentAlignRight?Math.Max(0,Width-2-unit):2;
        iconRect=new Rectangle(startX,2,d,d);
      }else{
        iconRect=new Rectangle((Width-d)/2,topMargin,d,d);
      }
      if(IconImage!=null){
        // Avant : clip elliptique dur en plus de l'alpha de l'image -> le clip GDI+ est
        // aliase (pas anti-alias), ca laissait un liseré sombre a la frontiere du cercle
        // meme avec un PNG deja detoure proprement. On s'appuie desormais uniquement sur
        // le canal alpha (deja nettoye au chargement), plus de decoupe dure en trop.
        g.DrawImage(IconImage,iconRect);
      }
      if(WideLayout){
        var textRect=new Rectangle(iconRect.Right+2,0,Width-iconRect.Right-2,Height);
        TextRenderer.DrawText(g,Text,Font,new Rectangle(textRect.X,textRect.Y+1,textRect.Width,textRect.Height),Color.FromArgb(150,0,0,0),TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g,Text,Font,textRect,ForeColor,TextFormatFlags.Left|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);
      }else{
        // Plus de rectangle de fond (lisait comme une tache noire sur l'icone, meme
        // fonce en teal) : ombre portee seule sous le texte, comme les CountBadge.
        var chip=new Rectangle(0,iconRect.Bottom+textGap,Width,Height-iconRect.Bottom-textGap);
        TextRenderer.DrawText(g,Text,Font,new Rectangle(chip.X,chip.Y+1,chip.Width,chip.Height),Color.FromArgb(200,0,0,0),TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);
        TextRenderer.DrawText(g,Text,Font,chip,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix);
      }
    }
    static GraphicsPath Rounded(Rectangle r,int radius){var p=new GraphicsPath();int d=radius*2;p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;}
    protected override void OnTextChanged(EventArgs e){base.OnTextChanged(e);Invalidate();}
  }
  sealed class StockViewRow {
    public Image IconeSet {get;set;} public string Set {get;set;} public string Type {get;set;} public string Stat {get;set;} public string Qualite {get;set;} public string Rune {get;set;} public int Quantite {get;set;} public CraftStock Source {get;set;} public string Vider {get{return Loc.T("stock_clear",Quantite);}}
  }
  sealed class WorldBossChangeRow {public string Monster{get;set;} public string Changement{get;set;} public string Actuel{get;set;} public string Nouveau{get;set;} public double Gain{get;set;} public WorldBossChangeRow(){Monster=Changement=Actuel=Nouveau="";}}
  sealed class WorldBossSavedPlan {public List<WorldBossRow> Rows=new List<WorldBossRow>();public List<WorldBossSkillRecommendation> SkillRecommendations=new List<WorldBossSkillRecommendation>();public List<string> InventoryKeys=new List<string>();public double CurrentTotal,OptimizedTotal;public int WaterCount;public bool UsesGameOrder;public string Formula="";public WorldBossResult ToResult(){return new WorldBossResult{Rows=Rows??new List<WorldBossRow>(),SkillRecommendations=SkillRecommendations??new List<WorldBossSkillRecommendation>(),InventoryKeys=InventoryKeys??new List<string>(),CurrentTotal=CurrentTotal,OptimizedTotal=OptimizedTotal,WaterCount=WaterCount,UsesGameOrder=UsesGameOrder,Formula=Formula};}public static WorldBossSavedPlan From(WorldBossResult x){return new WorldBossSavedPlan{Rows=x.Rows,SkillRecommendations=x.SkillRecommendations,InventoryKeys=x.InventoryKeys,CurrentTotal=x.CurrentTotal,OptimizedTotal=x.OptimizedTotal,WaterCount=x.WaterCount,UsesGameOrder=x.UsesGameOrder,Formula=x.Formula};}}
  static class Program {
    static bool reportingCrash;
    static void ReportCrash(Exception ex){try{string folder=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"RuneManagerModern");Directory.CreateDirectory(folder);File.AppendAllText(Path.Combine(folder,"crash.log"),DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")+"\r\n"+(ex==null?"Erreur inconnue":ex.ToString())+"\r\n\r\n");}catch{}if(reportingCrash)return;reportingCrash=true;try{MessageBox.Show(Loc.T("crash",ex==null?"Erreur inconnue":ex.Message),"Rune Manager",MessageBoxButtons.OK,MessageBoxIcon.Error);}catch{}finally{reportingCrash=false;}}
    [STAThread] static void Main(string[] args){
      if(args.Length>1&&args[0]=="--craft-stock-test"){RuneEngine.Import(args[1]);var violent=RuneEngine.Stocks.Where(x=>x.Type=="Meule"&&x.Set=="Violent"&&x.Stat=="Atk%").OrderByDescending(x=>x.Grade).ToList();Console.WriteLine("STOCK_LINES="+RuneEngine.Stocks.Count+" STOCK_TOTAL="+RuneEngine.Stocks.Sum(x=>x.Amount)+" VIOLENT_ATK="+string.Join(",",violent.Select(x=>"G"+x.Grade+"="+x.Amount).ToArray())+" INVALID="+RuneEngine.Stocks.Count(x=>string.IsNullOrWhiteSpace(x.Set)||string.IsNullOrWhiteSpace(x.Stat)||x.Grade<1||x.Grade>5));return;}
      if(args.Length>2&&args[0]=="--rune-choice-test"){var rows=RuneEngine.Import(args[1]);var choice=RuneEngine.CompareRuneChoice(rows,File.ReadAllText(args[2]));Console.WriteLine(choice==null?"CHOICE_NOT_FOUND":"CHOICES="+choice.Choices.Count+" BEST="+choice.Choices[0].Rune+" POTENTIAL="+choice.Choices[0].Potential.ToString("0.000")+" PRESET="+choice.Choices[0].BestBuild);return;}
      if(args.Length>1&&args[0]=="--self-test"){var sw=Stopwatch.StartNew();var x=RuneEngine.Import(args[1]);var re=x.Where(r=>r.Marker.IndexOf("Reeval",StringComparison.OrdinalIgnoreCase)>=0).OrderByDescending(r=>r.ReevalPriority).ToList();Console.WriteLine("RUNES="+x.Count+" KEEP="+x.Count(r=>r.Action=="Keep")+" PWR="+x.Count(r=>r.Action=="Pwr up")+" SELL="+x.Count(r=>r.Action=="Sell")+" RETAINED="+x.Count(r=>r.Action!="Sell")+" THRESHOLD="+RuneEngine.SeuilApres12.ToString("0.000")+" BLUE_ERRORS="+x.Count(r=>r.Grade<=3&&r.Marker.IndexOf("Deck",StringComparison.OrdinalIgnoreCase)<0&&r.Action!="Sell")+" DECK_SELL_ERRORS="+x.Count(r=>r.Marker.IndexOf("Deck",StringComparison.OrdinalIgnoreCase)>=0&&r.Action=="Sell")+" REEVAL="+re.Count+" REAPP_NORMAL="+RuneEngine.ReappNormal+" REAPP_ANCIENT="+RuneEngine.ReappAncient+" REEVAL_TOP="+(re.Count>0?re[0].ReevalPriority:0)+" STOCK_LIGNES="+RuneEngine.Stocks.Count+" STOCK_TOTAL="+RuneEngine.Stocks.Sum(s=>s.Amount)+" MS="+sw.ElapsedMilliseconds);return;}
      if(args.Length>2&&args[0]=="--skill-test"){var x=RuneEngine.AnalyzeSkillUps(args[1],args[2]);Console.WriteLine("GROUPES="+x.Count+" DOUBLONS="+x.Sum(g=>g.Available)+" MAX="+(x.Count==0?0:x.Max(g=>g.Available))+" CIBLES="+string.Join(",",x.Select(g=>g.Target.Name+"/"+g.Target.Element).ToArray()));return;}
      if(args.Length>2&&args[0]=="--worldboss-test"){var x=WorldBossOptimizer.Analyze(args[1],args[2]);string dir=Path.GetDirectoryName(args[2]);var anders=x.SkillRecommendations.FirstOrDefault(r=>r.UnitId%1000000==476040);long ubelId=28533046994;int ubelRank=x.CandidateRanks.ContainsKey(ubelId)?x.CandidateRanks[ubelId]:0;double ubelScore=x.CandidateScores.ContainsKey(ubelId)?x.CandidateScores[ubelId]:0;var molong=x.Rows.FirstOrDefault(r=>r.Monster.StartsWith("Mo Long"));var missing=x.Rows.Where(r=>!File.Exists(Path.Combine(dir,r.MasterId+".png"))).Select(r=>r.Monster+":"+r.MasterId).Concat(x.SkillRecommendations.Where(r=>!File.Exists(Path.Combine(dir,r.MasterId+".png"))).Select(r=>r.Monster+":"+r.MasterId)).Distinct().ToArray();Console.WriteLine("ROWS="+x.Rows.Count+" RECS="+x.SkillRecommendations.Count+" RECOMMENDED="+string.Join(",",x.SkillRecommendations.Select(r=>r.Monster).Distinct().ToArray())+" ICONS_MISSING="+missing.Length+" MISSING="+string.Join(",",missing)+" ANDERS_ICON="+(anders==null?0:anders.MasterId)+" UBEL_RANK="+ubelRank+" UBEL_SCORE="+ubelScore.ToString("0")+" MOLONG="+(molong==null?"missing":string.Join(" / ",molong.ArtifactDetails.Select(a=>a.Restriction+" "+a.Main).ToArray()))+" SETS="+string.Join(" || ",x.Rows.Select(r=>r.RuneSets).ToArray()));return;}
      if(args.Length>2&&args[0]=="--worldboss-stability"){var first=WorldBossOptimizer.Analyze(args[1],args[2]);var saved=WorldBossSavedPlan.From(first);var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};string serialized=js.Serialize(saved);var restored=js.Deserialize<WorldBossSavedPlan>(serialized).ToResult();var second=WorldBossOptimizer.Analyze(args[1],args[2],null,restored);var old=first.Rows.ToDictionary(x=>x.UnitId,x=>string.Join(",",x.RuneDetails.OrderBy(r=>r.Slot).Select(r=>r.Id).Concat(x.ArtifactDetails.Select(a=>a.Id))));int changed=second.Rows.Count(x=>!old.ContainsKey(x.UnitId)||old[x.UnitId]!=string.Join(",",x.RuneDetails.OrderBy(r=>r.Slot).Select(r=>r.Id).Concat(x.ArtifactDetails.Select(a=>a.Id))));Console.WriteLine("STABILITY_ROWS="+second.Rows.Count+" CHANGED="+changed+" SERIALIZED="+serialized.Length);return;}
      if(args.Length>3&&args[0]=="--worldboss-plan-test"){var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var saved=js.Deserialize<WorldBossSavedPlan>(File.ReadAllText(args[3]));var previous=saved.ToResult();var next=WorldBossOptimizer.Analyze(args[1],args[2],null,previous);var old=previous.Rows.ToDictionary(x=>x.UnitId,x=>string.Join(",",x.RuneDetails.OrderBy(r=>r.Slot).Select(r=>r.Id).Concat(x.ArtifactDetails.Select(a=>a.Id))));var oldPositions=previous.Rows.ToDictionary(x=>x.UnitId,x=>x.Team+":"+x.Position);int changed=next.Rows.Count(x=>!old.ContainsKey(x.UnitId)||old[x.UnitId]!=string.Join(",",x.RuneDetails.OrderBy(r=>r.Slot).Select(r=>r.Id).Concat(x.ArtifactDetails.Select(a=>a.Id))));int moved=next.Rows.Count(x=>!oldPositions.ContainsKey(x.UnitId)||oldPositions[x.UnitId]!=x.Team+":"+x.Position);var teshar=next.Rows.FirstOrDefault(x=>x.Monster.StartsWith("Teshar",StringComparison.OrdinalIgnoreCase));var bolverk=next.Rows.FirstOrDefault(x=>x.Monster.StartsWith("Bolverk",StringComparison.OrdinalIgnoreCase));var bastets=next.Rows.Where(x=>x.Monster.StartsWith("Bastet",StringComparison.OrdinalIgnoreCase)).OrderBy(x=>x.UnitId).ToList();Func<WorldBossRow,string> rank=x=>x==null?"ABSENT":((x.Team-1)*20+x.Position).ToString();Console.WriteLine("PERSISTED_PLAN_ROWS="+next.Rows.Count+" CHANGED="+changed+" MOVED="+moved+" TESHAR="+rank(teshar)+" BOLVERK="+rank(bolverk)+" BASTET="+string.Join(",",bastets.Select(rank).ToArray())+" FORMULA="+previous.Formula);return;}
      if(args.Length>3&&args[0]=="--worldboss-calibration-test"){var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var saved=js.Deserialize<WorldBossSavedPlan>(File.ReadAllText(args[3]));saved.Formula=WorldBossResult.CurrentFormula;var next=WorldBossOptimizer.Analyze(args[1],args[2],null,saved.ToResult());int[] truth={14511,20511,21111,14411,24511,15711,26111,17411,28911,25611,21211,29311,14611,20511,22611,25311,25711,34411,18611,32811,27911,16611,31311,13811,13811,19711,25211,35611,26811,18911,16811,14513,33411,21511,32911,19211,34911,17011,21811,16911,17911,18411,28211,33211,19911,21411,19411,13411,30711,29211,23111,11911,18811,25011,11211,24911,15511,13911,22711,28611};var expected=truth.Select((master,index)=>new{master,pos=index+1}).GroupBy(x=>x.master).ToDictionary(g=>g.Key,g=>new Queue<int>(g.Select(x=>x.pos)));double error=0;int measured=0;foreach(var group in next.Rows.OrderBy(r=>r.Team).ThenBy(r=>r.Position).GroupBy(r=>r.MasterId)){Queue<int> targets;if(!expected.TryGetValue(group.Key,out targets))continue;foreach(var row in group.OrderBy(r=>r.Team).ThenBy(r=>r.Position)){if(targets.Count==0)break;int actual=(row.Team-1)*20+row.Position,target=targets.Dequeue();error+=Math.Abs(actual-target);measured++;}}Console.WriteLine("CALIBRATION_MAE="+(measured>0?error/measured:999).ToString("0.000")+" MEASURED="+measured+"/60");return;}
      if(args.Length>4&&args[0]=="--worldboss-seal-plan"){var js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256};var saved=js.Deserialize<WorldBossSavedPlan>(File.ReadAllText(args[3]));var previous=saved.ToResult();previous.InventoryKeys=WorldBossOptimizer.Analyze(args[1],args[2]).InventoryKeys;var sealedPlan=WorldBossOptimizer.Analyze(args[1],args[2],null,previous);File.WriteAllText(args[4],js.Serialize(WorldBossSavedPlan.From(sealedPlan)));Console.WriteLine("SEALED_ROWS="+sealedPlan.Rows.Count);return;}
      if(args.Length>3&&args[0]=="--worldboss-rune-test"){var x=WorldBossOptimizer.Analyze(args[1],args[2]);long id=long.Parse(args[3]);var rune=x.Rows.SelectMany(r=>r.RuneDetails).FirstOrDefault(r=>r.Id==id);Console.WriteLine(rune==null?"RUNE_NOT_ASSIGNED":"GRADE="+rune.Grade+" ANCIENT="+rune.Ancient+" MAIN="+rune.Main+" SUBS="+string.Join(" | ",rune.Stats.ToArray()));return;}
      if(args.Length>2&&args[0]=="--worldboss-debug"){var x=WorldBossOptimizer.Analyze(args[1],args[2]);foreach(var r in x.Rows.Where(r=>r.Changes>0))Console.WriteLine("T"+r.Team+"#"+r.Position+" "+r.Monster+" CH="+r.Changes+" CURRENT_R="+r.CurrentRuneIds.Count+" CURRENT_A="+r.CurrentArtifactIds.Count+" PROPOSED_R="+r.RuneDetails.Count+" PROPOSED_A="+r.ArtifactDetails.Count);return;}
      if(args.Length>2&&args[0]=="--worldboss-skill-debug"){var x=WorldBossOptimizer.Analyze(args[1],args[2]);foreach(var r in x.SkillRecommendations)Console.WriteLine(r.Copy+" "+r.Monster+" LV="+r.CurrentLevel+" SK="+r.Current+"/"+r.Maximum+" MAX="+r.MaximumScore.ToString("0.0")+" TEAM="+r.InCurrentTeam+" REASON="+r.Reason);return;}
      if(args.Length>2&&args[0]=="--rta-test"){var owned=RtaData.Owned(args[1],args[2]);var season=RtaData.LoadSeason(args[2]);var top=season.Where(x=>owned.ContainsKey(x.Id)).OrderByDescending(x=>x.Played).FirstOrDefault();Console.WriteLine("RTA="+season.Count+" OWNED="+owned.Count+" TOP="+(top==null?"aucun":top.Name+"/"+(top.WinRate*100).ToString("0.00")+"%")+" PAIRS="+(top==null?0:RtaData.LoadPairs(top.Id).Count));return;}
      if(args.Length>2&&args[0]=="--rta-build-test"){var season=RtaData.LoadSeason(args[2]);var rows=RtaBuildOptimizer.BuildForTest(args[1],args[2],season);foreach(var r in rows)Console.WriteLine(r.Turn+" "+r.Tier+" "+r.Monster+" | "+r.Meta+" | "+r.Focus+" | "+r.Sets+" | "+r.Runes+" | "+r.Artifacts);return;}
      try{Process.GetCurrentProcess().PriorityClass=ProcessPriorityClass.AboveNormal;int workers,ports;System.Threading.ThreadPool.GetMinThreads(out workers,out ports);System.Threading.ThreadPool.SetMinThreads(Math.Max(workers,Environment.ProcessorCount),ports);}catch{}
      Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);Application.ThreadException+=(s,e)=>ReportCrash(e.Exception);AppDomain.CurrentDomain.UnhandledException+=(s,e)=>ReportCrash(e.ExceptionObject as Exception);Application.EnableVisualStyles();Application.SetCompatibleTextRenderingDefault(false);Application.Run(new MainForm());
    }
  }
  sealed partial class MainForm:Form {
    readonly Color Bg=Color.FromArgb(7,13,22), Panel=Color.FromArgb(15,25,39), Teal=Color.FromArgb(17,130,121), Cyan=Color.FromArgb(20,184,210), Grid=Color.FromArgb(30,32,36);
    // Contour rouge = "action a faire" (idee de Jeremy, etendue depuis World Boss a
    // Skill-up et Tri Amelioration) : bordure du bouton passe en rouge quand son badge
    // affiche un compte > 0, revient a l'orange normal sinon. NormalActionBorder =
    // meme orange que les autres boutons "outils/fonction".
    readonly Color RedAlertBorder=Color.FromArgb(218,70,62), NormalActionBorder=Color.FromArgb(255,170,40);
    const int AppBuild=6;
    void SetActionBorder(Button b,bool active){SetActionBorder(b,null,active);}
    // Le cadre du badge suit la meme couleur que le contour du bouton ou il se trouve
    // (rouge si action a faire, orange sinon) au lieu d'une couleur fixe independante.
    void SetActionBorder(Button b,CountBadge badge,bool active){Color idle=b==spdRankButton?CroquisViolet:b==updateButton?Cyan:NormalActionBorder;Color c=active&&b!=updateButton?RedAlertBorder:idle;if(b!=null)b.FlatAppearance.BorderColor=c;if(badge!=null)badge.Invalidate();}
    void SetCountAlert(Button b,CountBadge badge,int count){
      if(badge!=null)badge.Text=count.ToString(CultureInfo.InvariantCulture);
      bool on=count>0;
      if(b!=null){
        var pad=on?new Padding(72,0,16,0):new Padding(16,0,16,0);
        if(b.Padding!=pad)b.Padding=pad;
      }
      SetActionBorder(b,badge,on);
      if(on&&badge!=null)badge.BringToFront();
      LayoutToolbar();
    }
    readonly DataGridView grid=new BufferedGrid(); readonly Label status=new Label(), counters=new Label(); readonly IconBadge reappNormalBadge=new IconBadge(),reappAncientBadge=new IconBadge(),refinementBadge=new IconBadge(); readonly CountBadge improveBadge=new CountBadge(),skillBadge=new CountBadge(),worldBossBadge=new CountBadge(),spdRankBadge=new CountBadge(),updateBadge=new CountBadge(); readonly TextBox search=new TextBox(); readonly Label searchHint=new Label(); readonly ComboBox set=new ComboBox(),slot=new ComboBox(),action=new ComboBox(),build=new ComboBox(),runeType=new ComboBox();
    readonly Dictionary<string,Image> setIcons=new Dictionary<string,Image>(StringComparer.OrdinalIgnoreCase); readonly Dictionary<string,Image> runeIcons=new Dictionary<string,Image>(StringComparer.OrdinalIgnoreCase); readonly Image[] slotLayers=new Image[7]; readonly Dictionary<int,Image> worldBossMonsterIcons=new Dictionary<int,Image>(),monsterPortraits=new Dictionary<int,Image>(); readonly Dictionary<int,string> portraitPathById=new Dictionary<int,string>(); readonly Dictionary<string,string[]> portraitFilesByPrefix=new Dictionary<string,string[]>(); readonly Dictionary<long,double> upgradeCaps=new Dictionary<long,double>(),craftPotentials=new Dictionary<long,double>();
    readonly Image[] croquis3dSlot=new Image[7]; readonly Image[] croquis3dBlankSlot=new Image[7]; readonly Dictionary<string,Bitmap> croquis3dBaseByKey=new Dictionary<string,Bitmap>(StringComparer.OrdinalIgnoreCase); readonly Dictionary<string,Image> croquis3dByKey=new Dictionary<string,Image>(StringComparer.OrdinalIgnoreCase); readonly Dictionary<string,Image> croquis3dPulseByKey=new Dictionary<string,Image>(StringComparer.OrdinalIgnoreCase); readonly Dictionary<string,RectangleF> croquis3dOpaqueByKey=new Dictionary<string,RectangleF>(StringComparer.OrdinalIgnoreCase); readonly Dictionary<string,RectangleF> croquis3dSetByKey=new Dictionary<string,RectangleF>(StringComparer.OrdinalIgnoreCase); readonly RectangleF[] croquis3dSetBounds=new RectangleF[7]; readonly RectangleF[] croquis3dOpaqueBounds=new RectangleF[7];
    static readonly Color CroquisOrange=Color.FromArgb(255,170,40),CroquisViolet=Color.FromArgb(150,90,230),CroquisBlue=Color.FromArgb(20,184,210);
    Point[] shineOuterPts,shineInnerPts; float[] shineOuterAng,shineInnerAng; int shineMapW,shineMapH;
    readonly HashSet<long> hiddenUpgradeIds=new HashSet<long>(); readonly HashSet<int> hiddenSkillTargetIds=new HashSet<int>();
    FileSystemWatcher jsonWatcher; readonly Timer jsonDebounce=new Timer(),liveLogTimer=new Timer(),worldBossTimer=new Timer(),ancientShineTimer=new Timer(),updateCheckTimer=new Timer(); float ancientPulseT; string pendingJson="",liveLogPath="",liveLogPending=""; DateTime lastAutoImport=DateTime.MinValue,liveLogStableSince=DateTime.MinValue; long liveLogOffset=0,liveLogObservedLength=-1; Action liveStockRefresh;
    List<RuneRow> all=new List<RuneRow>(); List<SkillUpGroup> skillGroups=new List<SkillUpGroup>(); List<SkillUpFamily> skillFamilies=new List<SkillUpFamily>(); int skillGroupsRevision,worldBossRevision; readonly List<string> liveSavedEvents=new List<string>(); readonly List<WorldBossChangeRow> worldBossChanges=new List<WorldBossChangeRow>(); string currentFile="",viewMode="normal",worldBossCalculatedFile=""; bool potentialDesc=true,liveAwaitingResponse=false,liveAwaitingRequest=false,liveRequestEquipment=false,liveRequestSkill=false,liveRequestCraft=false,showHiddenSkillTargets=false,worldBossCalculating=false; Button worldBossButton,retentionButton,rtaButton,improveButton,skillButton,reevalButton,refinementButton,spdRankButton,importButton,potButton,obtButton,presetButton,coefficientButton,autoKeepButton,rulesButton; Panel row2Divider,row1Divider,topBar; Label titleLabel; ComboBox langCombo; PictureBox paypalButton,discordButton,twitchButton; Button updateButton; readonly ToolTip reappNormalTip=new ToolTip(),reappAncientTip=new ToolTip(),refinementTip=new ToolTip(),paypalTip=new ToolTip(),discordTip=new ToolTip(),twitchTip=new ToolTip(),searchTip=new ToolTip(),updateTip=new ToolTip(); bool applyingLang; const int Row2Gap=8; const string PaypalDonateUrl="https://www.paypal.me/greatlucky"; const string DiscordInviteUrl="https://discord.gg/YGEt9eNKuH"; const string TwitchUrl="https://www.twitch.tv/imgreatlucky"; const string UpdateManifestUrl="https://api.github.com/repos/GreatLucky740/rune-manager-modern/contents/tools/update.json"; string pendingUpdateUrl="",pendingUpdateVersion=""; string[] pendingUpdateNotes=new string[0]; bool updateAvailable,updateCheckBusy; WorldBossResult worldBossLatest,worldBossSeen;
    // Le bouton WORLD BOSS MAX change de largeur au runtime (Padding gauche 16->38
    // quand le badge de compte s'affiche), mais skillButton/rtaButton/divider/retention
    // ont ete positionnes une seule fois a la construction (avant que le badge
    // n'apparaisse) => sans ceci ils restaient fixes et WORLD BOSS MAX les depassait.
    // worldBossButton n'est plus ajoute a top.Controls (bouton retire de l'interface) mais garde
    // ses coordonnees d'origine (18,rowY2 ...) — s'appuyer sur worldBossButton.Right ici faisait
    // sauter skill-up a la place theorique du bouton cache des qu'un refresh (badge, etc.)
    // redeclenchait ce calcul. Part directement de x=18 tant que le bouton reste cache.
    void RelayoutRow2(){if(worldBossButton==null||skillButton==null)return;int x=worldBossButton.Parent==null?18:worldBossButton.Right+Row2Gap;skillButton.Left=x;x=skillButton.Right+Row2Gap;if(rtaButton!=null){rtaButton.Left=x;x=rtaButton.Right+Row2Gap;}if(row2Divider!=null){row2Divider.Left=x;x=row2Divider.Right+Row2Gap;}if(retentionButton!=null)retentionButton.Left=x;}
    string StockSavePath {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"stock-sauvegarde-v2.tsv");}}
    string SettingsSavePath {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"parametres-runes.tsv");}}
    string RetentionSavePath {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"seuil-dynamique.txt");}}
    string LiveSavePath {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"changements-live.jsonl");}}
    string CapsSavePath {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"plafonds-potential-v2.tsv");}}
    string CraftPotentialSavePath {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"potential-gemmes-meules-v2.tsv");}}
    string SpdBestSavePath {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"spd-record-vu.tsv");}}
    string RuneChoiceSeenSavePath {get{return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"choix-rune-vus.tsv");}}
    Form runeChoiceForm,reappDecisionForm,refineDecisionForm;
    Dictionary<string,double> spdSeenBest=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
    public MainForm(){LoadRetentionSetting();LoadEngineSettings();RuneEngine.StatGlobalFactor["Spd"]=1.1;SaveEngineSettings();LoadSpdSeenBest();LoadRuneChoiceSeen();RuneEngine.RuneChoiceDetected+=ShowRuneChoiceDecision;Text=Loc.T("app_title");Icon=LoadAppIcon();WindowState=FormWindowState.Maximized;MinimumSize=new Size(1100,650);BackColor=Bg;ForeColor=Color.White;Font=new Font("Segoe UI",10);KeyPreview=true;FormClosing+=ClosingWithSave;BuildUi();InstallRuneEnhancements();}
    Icon LoadAppIcon(){try{string p=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","app_icon.ico");if(File.Exists(p))return new Icon(p,256,256);}catch{}return SystemIcons.Application;}
    void BuildUi(){
      topBar=new Panel{Dock=DockStyle.Top,Height=232,BackColor=Panel,Padding=new Padding(18,12,18,10)};Controls.Add(topBar);var top=topBar;
      titleLabel=new Label{Text="RUNE MANAGER",ForeColor=Cyan,Font=new Font("Segoe UI Semibold",21),AutoSize=true,Location=new Point(18,12)};top.Controls.Add(titleLabel);
      counters.AutoSize=true;counters.Location=new Point(255,22);counters.ForeColor=Color.Gainsboro;top.Controls.Add(counters);
      langCombo=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,FlatStyle=FlatStyle.Flat,BackColor=ComboBg,ForeColor=Color.White,Size=new Size(118,28),Anchor=AnchorStyles.Top|AnchorStyles.Right};
      langCombo.Items.Add("Français");langCombo.Items.Add("English");langCombo.SelectedIndex=Loc.En?1:0;
      langCombo.SelectedIndexChanged+=(s,e)=>{if(applyingLang)return;string next=langCombo.SelectedIndex==1?"en":"fr";if(next==Loc.Lang)return;Loc.Lang=next;SaveEngineSettings();ApplyLanguage();};
      top.Controls.Add(langCombo);
      paypalButton=new PictureBox{Size=new Size(36,36),SizeMode=PictureBoxSizeMode.Zoom,Image=LoadPaypalIcon(),Cursor=Cursors.Hand,BackColor=Color.Transparent,Anchor=AnchorStyles.Top|AnchorStyles.Right};paypalButton.Click+=(s,e)=>OpenPaypalDonate();paypalTip.SetToolTip(paypalButton,Loc.T("tip_paypal"));top.Controls.Add(paypalButton);
      discordButton=new PictureBox{Size=new Size(36,36),SizeMode=PictureBoxSizeMode.Zoom,Image=LoadDiscordIcon(),Cursor=Cursors.Hand,BackColor=Color.Transparent,Anchor=AnchorStyles.Top|AnchorStyles.Right};discordButton.Click+=(s,e)=>OpenDiscordInvite();discordTip.SetToolTip(discordButton,Loc.T("tip_discord"));top.Controls.Add(discordButton);
      twitchButton=new PictureBox{Size=new Size(36,36),SizeMode=PictureBoxSizeMode.Zoom,Image=LoadTwitchIcon(),Cursor=Cursors.Hand,BackColor=Color.Transparent,Anchor=AnchorStyles.Top|AnchorStyles.Right};twitchButton.Click+=(s,e)=>OpenTwitchChannel();twitchTip.SetToolTip(twitchButton,Loc.T("tip_twitch"));top.Controls.Add(twitchButton);
      updateButton=Button(Loc.T("update_check_btn"),0,12,0,Cyan,36);updateButton.Padding=new Padding(16,0,16,0);updateButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;updateButton.Click+=(s,e)=>{if(updateAvailable)ShowUpdateDialog();else StartUpdateCheck(true);};top.Controls.Add(updateButton);ConfigureCountBadge(updateBadge,3,3,Cyan);updateBadge.Click+=(s,e)=>{if(updateAvailable)ShowUpdateDialog();else StartUpdateCheck(true);};updateButton.Controls.Add(updateBadge);updateBadge.BringToFront();updateTip.SetToolTip(updateButton,Loc.T("tip_update_check"));updateTip.SetToolTip(updateBadge,Loc.T("tip_update_check"));
      importButton=Button(Loc.T("import_json"),18,61,0,Cyan);importButton.Click+=(s,e)=>ChooseFile();top.Controls.Add(importButton);
      search.SetBounds(importButton.Right+8,61,268,32);search.BackColor=Bg;search.ForeColor=Color.White;search.BorderStyle=BorderStyle.FixedSingle;search.TextChanged+=(s,e)=>{RefreshSearchHint();RefreshGrid();};top.Controls.Add(search);
      searchHint.AutoSize=false;searchHint.BackColor=Bg;searchHint.ForeColor=Color.FromArgb(130,150,165);searchHint.TextAlign=ContentAlignment.MiddleLeft;searchHint.Cursor=Cursors.IBeam;searchHint.Click+=(s,e)=>search.Focus();top.Controls.Add(searchHint);RefreshSearchHint();searchTip.SetToolTip(search,Loc.T("search_tip"));searchTip.SetToolTip(searchHint,Loc.T("search_tip"));
      int cx=search.Right+8;AddCombo(top,set,cx,Loc.T("filter_all_sets"));cx+=150;AddCombo(top,slot,cx,Loc.T("filter_all_slots"));cx+=150;AddCombo(top,action,cx,Loc.T("filter_all_actions"));cx+=150;AddCombo(top,build,cx,Loc.T("filter_all_presets"));cx+=150;AddCombo(top,runeType,cx,Loc.T("filter_all_runes"));runeType.Items.Add(Loc.T("filter_normal"));runeType.Items.Add(Loc.T("filter_ancient"));
      // Boutons regroupés par catégorie (une couleur = une catégorie, plus de couleurs
      // au hasard) avec un séparateur fin entre chaque groupe : TRI (teal) → OUTILS
      // (violet) sur la ligne du haut, FONCTIONNALITÉS (bleu) → SEUILS (orange) en dessous.
      // Largeurs en dur remplacées par AutoSize (ajustées au texte, plus de vide
      // inutile a gauche/droite) : le parametre w de Button() sert desormais de
      // largeur MINIMALE (0 = aucune, juste texte + padding) pour les boutons qui
      // portent un badge flottant loin du bord (ex: reeval, badge a x=96).
      // Idee de Jeremy : abandon des fonds de couleur pleine, boutons "fantome" (fond
      // sombre du bandeau) avec juste un contour colore repris des couleurs de rarete
      // des runes (Legendaire=orange, Epique=violet) — plus proche du theme runes.
      Color LegendaryOrange=Color.FromArgb(255,170,40),EpicPurple=Color.FromArgb(150,90,230);
      Color OutilsColor=EpicPurple,FonctionColor=LegendaryOrange,SeuilColor=LegendaryOrange;
      Func<int,int,int,Panel> divider=(x,y,h)=>{var pnl=new Panel{BackColor=Color.FromArgb(55,75,92),Location=new Point(x,y),Size=new Size(2,h)};top.Controls.Add(pnl);return pnl;};
      // Lignes 1/2 en hauteur 42 (au lieu de 32) : boutons + badges dedans plus
      // grands et plus lisibles. Ligne 0 (recherche/menus) reste en 32.
      const int rh=42;int gap=8,rx=18;
      potButton=Button(Loc.T("sort_potential"),rx,101,0,LegendaryOrange,rh);potButton.Click+=(s,e)=>{viewMode="potential";potentialDesc=true;RefreshGrid();status.Text=Loc.T("status_potential",RuneEngine.SeuilApres12.ToString("0.###"));};top.Controls.Add(potButton);rx=potButton.Right+gap;
      obtButton=Button(Loc.T("sort_obtained"),rx,101,0,LegendaryOrange,rh);obtButton.Click+=(s,e)=>SortObtained();top.Controls.Add(obtButton);rx=obtButton.Right+gap;
      improveButton=Button(Loc.T("sort_upgrade"),rx,101,0,LegendaryOrange,rh);improveButton.Click+=(s,e)=>{viewMode="upgrade";potentialDesc=true;RefreshGrid();status.Text=Loc.T("status_upgrade",RuneEngine.SeuilApres12.ToString("0.###"));};top.Controls.Add(improveButton);ConfigureCountBadge(improveBadge,5,6,NormalActionBorder);improveBadge.Click+=(s,e)=>improveButton.PerformClick();improveButton.Controls.Add(improveBadge);improveBadge.BringToFront();rx=improveButton.Right+gap;
      // Largeur mini reduite (272->248, "trop long") : juste assez pour badge+texte
      // centre+badge sans se toucher. Chaque badge est ensuite centre dans l'espace
      // libre de son cote (entre le bord du bouton et le texte "TRI REEVAL"), pas
      // colle au bord.
      reevalButton=Button(Loc.T("sort_reeval"),rx,101,248,LegendaryOrange,rh);reevalButton.Click+=(s,e)=>SortReeval();top.Controls.Add(reevalButton);ConfigureReappBadge(reappNormalBadge,4,5,"reappraisal-stone.png",Loc.T("tip_reapp_n"));ConfigureReappBadge(reappAncientBadge,174,5,"ancient-reappraisal-stone.png",Loc.T("tip_reapp_a"));reappNormalBadge.Click+=(s,e)=>SortReeval();reappAncientBadge.Click+=(s,e)=>SortReeval();reevalButton.Controls.Add(reappNormalBadge);reevalButton.Controls.Add(reappAncientBadge);reappNormalBadge.BringToFront();reappAncientBadge.BringToFront();rx=reevalButton.Right+gap;
      refinementButton=Button(Loc.T("sort_refinement"),rx,101,0,LegendaryOrange,rh);refinementButton.Padding=new Padding(74,0,16,0);refinementButton.Click+=(s,e)=>{viewMode="refinement";potentialDesc=true;RefreshGrid();status.Text=Loc.T("status_refinement");};top.Controls.Add(refinementButton);ConfigureRefinementBadge(refinementBadge,2,5);refinementBadge.Click+=(s,e)=>refinementButton.PerformClick();refinementButton.Controls.Add(refinementBadge);refinementBadge.BringToFront();rx=refinementButton.Right+gap;
      row1Divider=divider(rx,101,rh);rx+=gap+2;
      presetButton=Button(Loc.T("preset"),rx,101,0,OutilsColor,rh);presetButton.Click+=(s,e)=>ShowPresets();top.Controls.Add(presetButton);rx=presetButton.Right+gap;
      coefficientButton=Button(Loc.T("coefficient"),rx,101,0,OutilsColor,rh);coefficientButton.Click+=(s,e)=>ShowCoefficients();top.Controls.Add(coefficientButton);rx=coefficientButton.Right+gap;
      autoKeepButton=Button(Loc.T("autokeep"),rx,101,0,OutilsColor,rh);autoKeepButton.Click+=(s,e)=>ShowAutoKeepSettings();top.Controls.Add(autoKeepButton);rx=autoKeepButton.Right+gap;
      spdRankButton=Button(Loc.T("spd_rank"),rx,101,0,OutilsColor,rh);spdRankButton.Click+=(s,e)=>ShowSpdRanking();top.Controls.Add(spdRankButton);ConfigureCountBadge(spdRankBadge,5,6,CroquisViolet);spdRankBadge.Click+=(s,e)=>spdRankButton.PerformClick();spdRankButton.Controls.Add(spdRankBadge);spdRankBadge.BringToFront();
      // Bouton Regles retire (Jeremy : trop chiant a regler). Les ScoreRules restent chargees
      // et appliquees (Spd 23+/25+/27+ etc). On garde l'objet pour pouvoir le remettre plus tard.
      rulesButton=Button(Loc.T("rules"),rx,101,0,OutilsColor,rh);rulesButton.Click+=(s,e)=>ShowScoreRules();
      int rowY2=101+rh+gap;int rx2=18;
      // Bouton retire de l'interface a la demande de Jeremy (valeurs World Boss pas bonnes pour
      // l'instant, sera remis un autre jour). On garde l'objet Button et tout le code qui s'y
      // rattache (badge, ShowWorldBoss, etc.) intacts, juste pas ajoute a top.Controls et rx2 pas
      // avance a sa place, pour pouvoir le remettre plus tard en decommentant top.Controls.Add.
      worldBossButton=Button("WORLD BOSS MAX",rx2,rowY2,0,FonctionColor,rh);worldBossButton.Click+=(s,e)=>ShowWorldBoss();/*top.Controls.Add(worldBossButton);*/ConfigureCountBadge(worldBossBadge,7,1,NormalActionBorder);worldBossBadge.Text="0";worldBossBadge.Visible=false;worldBossBadge.Click+=(s,e)=>ShowWorldBoss();worldBossButton.Controls.Add(worldBossBadge);worldBossBadge.BringToFront();
      skillButton=Button(Loc.T("skillup"),rx2,rowY2,0,FonctionColor,rh);skillButton.Click+=(s,e)=>ShowSkillUps();top.Controls.Add(skillButton);ConfigureCountBadge(skillBadge,5,6,NormalActionBorder);skillBadge.Click+=(s,e)=>skillButton.PerformClick();skillButton.Controls.Add(skillBadge);skillBadge.BringToFront();rx2=skillButton.Right+gap;
      rtaButton=Button(Loc.T("rta"),rx2,rowY2,0,FonctionColor,rh);rtaButton.Click+=(s,e)=>ShowRtaAdvisor();top.Controls.Add(rtaButton);rx2=rtaButton.Right+gap;
      row2Divider=divider(rx2,rowY2,rh);rx2+=gap+2;
      retentionButton=Button("",rx2,rowY2,0,SeuilColor,rh);retentionButton.Click+=(s,e)=>ShowRetentionSettings();top.Controls.Add(retentionButton);RefreshRetentionButton();
      status.Dock=DockStyle.Bottom;status.Height=22;status.ForeColor=Color.Silver;top.Controls.Add(status);
      grid.Dock=DockStyle.Fill;grid.BackgroundColor=Grid;grid.BorderStyle=BorderStyle.None;grid.GridColor=Color.Black;grid.CellBorderStyle=DataGridViewCellBorderStyle.Single;grid.RowHeadersVisible=false;grid.AllowUserToAddRows=false;grid.AllowUserToDeleteRows=false;grid.ReadOnly=true;grid.MultiSelect=false;grid.SelectionMode=DataGridViewSelectionMode.CellSelect;grid.AutoSizeRowsMode=DataGridViewAutoSizeRowsMode.None;grid.RowTemplate.Height=56;grid.EnableHeadersVisualStyles=false;grid.ColumnHeadersHeight=44;grid.ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.DisableResizing;grid.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Teal,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",12),SelectionBackColor=Teal};grid.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Grid,ForeColor=Color.White,Font=new Font("Segoe UI",12),SelectionBackColor=Color.FromArgb(26,70,83),SelectionForeColor=Color.White,Padding=new Padding(2,0,2,0)};grid.CellFormatting+=FormatCell;grid.CellPainting+=PaintCraftBorder;grid.KeyDown+=GridKeyDown;grid.CellMouseDown+=GridMouseDown;grid.CellToolTipTextNeeded+=(s,e)=>{if(e.RowIndex>=0){var r=grid.Rows[e.RowIndex].DataBoundItem as RuneRow;if(r!=null&&e.ColumnIndex==0)e.ToolTipText=r.Rune;}};Controls.Add(grid);grid.BringToFront();
      AddColumns();jsonDebounce.Interval=1200;jsonDebounce.Tick+=(s,e)=>ImportPendingJson();liveLogTimer.Interval=500;liveLogTimer.Tick+=(s,e)=>ReadLiveLog();worldBossTimer.Interval=700;worldBossTimer.Tick+=(s,e)=>{worldBossTimer.Stop();StartWorldBossRealtimeCalculation();};ancientShineTimer.Interval=70;ancientShineTimer.Tick+=(s,e)=>{ancientPulseT+=0.040f;if(ancientPulseT>=1f)ancientPulseT-=1f;if(grid.Columns.Count>0)grid.InvalidateColumn(0);};Shown+=(s,e)=>{LayoutToolbar();TryAutoLoad();LoadWorldBossOrderEvents();LoadWorldBossRealOrder();StartJsonWatcher();StartLiveLog();ancientShineTimer.Start();StartUpdateCheck(false);if(!updateCheckTimer.Enabled){updateCheckTimer.Interval=3600000;updateCheckTimer.Tick+=(t,ev)=>StartUpdateCheck(false);updateCheckTimer.Start();}};      Resize+=(s,e)=>{FitColumns();PlaceLangCombo();};
      ApplyLanguage();
    }
    void PlaceLangCombo(){
      if(langCombo==null||topBar==null)return;
      int right=topBar.ClientSize.Width-18;
      if(paypalButton!=null){paypalButton.Location=new Point(right-paypalButton.Width,12);right=paypalButton.Left-10;}
      if(discordButton!=null){discordButton.Location=new Point(right-discordButton.Width,12);right=discordButton.Left-10;}
      if(twitchButton!=null){twitchButton.Location=new Point(right-twitchButton.Width,12);right=twitchButton.Left-10;}
      if(updateButton!=null){updateButton.Location=new Point(right-updateButton.Width,10);right=updateButton.Left-10;}
      langCombo.Location=new Point(Math.Max((titleLabel==null?18:titleLabel.Right)+24,right-langCombo.Width),14);
    }
    void OpenPaypalDonate(){try{Process.Start(PaypalDonateUrl);}catch{}}
    void OpenDiscordInvite(){try{Process.Start(DiscordInviteUrl);}catch{}}
    void OpenTwitchChannel(){try{Process.Start(TwitchUrl);}catch{}}
    void StartUpdateCheck(){StartUpdateCheck(false);}
    void StartUpdateCheck(bool report){
      if(updateCheckBusy)return;
      updateCheckBusy=true;
      System.Threading.ThreadPool.QueueUserWorkItem(_=>{
        try{
          ServicePointManager.SecurityProtocol=(SecurityProtocolType)3072;
          var req=(HttpWebRequest)WebRequest.Create(UpdateManifestUrl);
          req.Timeout=8000;req.ReadWriteTimeout=8000;req.UserAgent="RuneManager/"+AppBuild;
          req.Accept="application/vnd.github.raw";
          req.CachePolicy=new RequestCachePolicy(RequestCacheLevel.BypassCache);
          req.AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate;
          string json;
          using(var resp=req.GetResponse())
          using(var sr=new StreamReader(resp.GetResponseStream()))
            json=sr.ReadToEnd();
          var ser=new JavaScriptSerializer();
          var root=ser.DeserializeObject(json) as Dictionary<string,object>;
          if(root!=null&&root.ContainsKey("content")&&root.ContainsKey("encoding")){
            string enc=Convert.ToString(root["encoding"]);
            if(string.Equals(enc,"base64",StringComparison.OrdinalIgnoreCase)){
              string b64=Convert.ToString(root["content"]).Replace("\n","").Replace("\r","");
              json=Encoding.UTF8.GetString(Convert.FromBase64String(b64));
              root=ser.DeserializeObject(json) as Dictionary<string,object>;
            }
          }
          if(root==null){FinishUpdateCheck(true,report);return;}
          object b;if(!root.TryGetValue("build",out b)){FinishUpdateCheck(true,report);return;}
          int remote=Convert.ToInt32(b,CultureInfo.InvariantCulture);
          if(remote<=AppBuild){FinishUpdateCheck(false,report);return;}
          string ver=root.ContainsKey("version")?Convert.ToString(root["version"]):remote.ToString(CultureInfo.InvariantCulture);
          string url=root.ContainsKey("url")?Convert.ToString(root["url"]):DiscordInviteUrl;
          string notesKey=Loc.En?"notes_en":"notes_fr";
          var notes=new List<string>();object n;
          if(!root.TryGetValue(notesKey,out n))root.TryGetValue("notes_fr",out n);
          var list=n as IEnumerable;
          if(list!=null&&!(n is string))foreach(var x in list){string line=Convert.ToString(x);if(!string.IsNullOrWhiteSpace(line))notes.Add(line);}
          if(IsHandleCreated&&!IsDisposed)BeginInvoke((MethodInvoker)delegate{updateCheckBusy=false;if(!IsDisposed)ApplyRemoteUpdate(ver,url,notes.ToArray());});
          else updateCheckBusy=false;
        }catch{FinishUpdateCheck(true,report);}
      });
    }
    void FinishUpdateCheck(bool failed,bool report){
      if(!(IsHandleCreated&&!IsDisposed)){updateCheckBusy=false;return;}
      BeginInvoke((MethodInvoker)delegate{
        updateCheckBusy=false;
        if(IsDisposed)return;
        if(failed){if(!updateAvailable)MarkUpdateIdle();}else MarkUpdateIdle();
        if(report){
          string msg=failed?Loc.T("update_fail"):Loc.T("update_ok");
          status.Text=msg;
          if(!failed)MessageBox.Show(msg,Loc.T("update_check_btn"),MessageBoxButtons.OK,MessageBoxIcon.Information);
        }
      });
    }
    void MarkUpdateIdle(){
      updateAvailable=false;
      if(updateButton==null)return;
      updateButton.Visible=true;updateButton.Text=Loc.T("update_check_btn");updateButton.Padding=new Padding(16,0,16,0);
      SetActionBorder(updateButton,updateBadge,false);
      if(updateBadge!=null)updateBadge.Visible=false;
      updateTip.SetToolTip(updateButton,Loc.T("tip_update_check"));
      PlaceLangCombo();
    }
    void ApplyRemoteUpdate(string ver,string url,string[] notes){
      pendingUpdateVersion=ver??"";pendingUpdateUrl=string.IsNullOrWhiteSpace(url)?DiscordInviteUrl:url;pendingUpdateNotes=notes??new string[0];
      updateAvailable=true;
      if(updateButton!=null){
        updateButton.Visible=true;updateButton.Text=Loc.T("update_check_btn");updateButton.Padding=new Padding(72,0,16,0);
        SetActionBorder(updateButton,updateBadge,false);
        if(updateBadge!=null){updateBadge.Visible=true;updateBadge.BringToFront();updateTip.SetToolTip(updateBadge,Loc.T("tip_update"));}
        updateTip.SetToolTip(updateButton,Loc.T("tip_update"));
      }
      PlaceLangCombo();
      status.Text=Loc.T("update_status",pendingUpdateVersion);
    }
    void ShowUpdateDialog(){
      if(updateButton==null)return;
      if(!updateAvailable){StartUpdateCheck(true);return;}
      var f=new Form{Text=Loc.T("update_title"),Icon=Icon,BackColor=Bg,ForeColor=Color.White,ClientSize=new Size(520,420),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,StartPosition=FormStartPosition.CenterParent};
      var title=new Label{Text=Loc.T("update_head",string.IsNullOrEmpty(pendingUpdateVersion)?"?":pendingUpdateVersion),Dock=DockStyle.Top,Height=72,BackColor=Panel,ForeColor=Color.FromArgb(255,178,55),Font=new Font("Segoe UI Semibold",16),Padding=new Padding(18,12,18,8)};
      var wrap=new Panel{Dock=DockStyle.Fill,BackColor=Bg,Padding=new Padding(22,16,18,16),AutoScroll=true};
      var body=new Label{AutoSize=true,MaximumSize=new Size(460,0),BackColor=Bg,ForeColor=Color.Gainsboro,Font=new Font("Segoe UI",11),Text=pendingUpdateNotes.Length==0?Loc.T("update_none"):"• "+string.Join("\r\n\r\n• ",pendingUpdateNotes)};
      wrap.Controls.Add(body);
      var bar=new Panel{Dock=DockStyle.Bottom,Height=58,BackColor=Panel};
      var later=Button(Loc.T("update_later"),18,12,0,Color.FromArgb(120,140,155),36);later.Click+=(s,e)=>f.Close();bar.Controls.Add(later);
      later.PerformLayout();
      var dl=Button(Loc.T("update_download"),later.Right+10,12,0,CroquisOrange,36);dl.Click+=(s,e)=>StartUpdateApply(f,dl);bar.Controls.Add(dl);
      f.Controls.Add(wrap);f.Controls.Add(title);f.Controls.Add(bar);
      f.ShowDialog(this);
    }
    void StartUpdateApply(Form dialog,Button dl){
      string url=string.IsNullOrWhiteSpace(pendingUpdateUrl)?DiscordInviteUrl:pendingUpdateUrl;
      if(!url.EndsWith(".exe",StringComparison.OrdinalIgnoreCase)){try{Process.Start(url);}catch{}if(dialog!=null)dialog.Close();return;}
      if(dl!=null){dl.Enabled=false;dl.Text=Loc.T("update_applying");}
      if(status!=null)status.Text=Loc.T("update_applying");
      System.Threading.ThreadPool.QueueUserWorkItem(_=>{
        try{
          ServicePointManager.SecurityProtocol=(SecurityProtocolType)3072;
          string tmp=Path.Combine(Path.GetTempPath(),"Rune_Manager_Update.exe");
          var req=(HttpWebRequest)WebRequest.Create(url);
          req.Timeout=180000;req.ReadWriteTimeout=180000;req.UserAgent="RuneManager/"+AppBuild;req.AllowAutoRedirect=true;
          using(var resp=req.GetResponse())
          using(var input=resp.GetResponseStream())
          using(var output=File.Create(tmp)){
            var buf=new byte[64*1024];int n;int first=input.Read(buf,0,buf.Length);
            if(first<2||buf[0]!='M'||buf[1]!='Z')throw new InvalidOperationException("not-exe");
            output.Write(buf,0,first);
            while((n=input.Read(buf,0,buf.Length))>0)output.Write(buf,0,n);
          }
          string root=Directory.GetParent(AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar)).FullName;
          Process.Start(new ProcessStartInfo(tmp){UseShellExecute=true,Arguments="--target \""+root+"\" --wait "+Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture)});
          if(IsHandleCreated&&!IsDisposed)BeginInvoke((MethodInvoker)delegate{if(dialog!=null&&!dialog.IsDisposed)dialog.Close();Close();});
        }catch{
          if(IsHandleCreated&&!IsDisposed)BeginInvoke((MethodInvoker)delegate{
            if(dl!=null){dl.Enabled=true;dl.Text=Loc.T("update_download");}
            if(status!=null)status.Text=Loc.T("update_dl_fail");
            MessageBox.Show(Loc.T("update_dl_fail"),Loc.T("update_title"),MessageBoxButtons.OK,MessageBoxIcon.Error);
          });
        }
      });
    }
    static Image LoadPaypalIcon(){
      string p=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","paypal.png");
      try{if(File.Exists(p)){using(var fs=new FileStream(p,FileMode.Open,FileAccess.Read))using(var img=Image.FromStream(fs))return new Bitmap(img);}}catch{}
      return null;
    }
    static Image LoadDiscordIcon(){
      string p=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","discord.png");
      try{if(File.Exists(p)){using(var fs=new FileStream(p,FileMode.Open,FileAccess.Read))using(var img=Image.FromStream(fs))return new Bitmap(img);}}catch{}
      return null;
    }
    static Image LoadTwitchIcon(){
      string p=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","twitch.png");
      try{if(File.Exists(p)){using(var fs=new FileStream(p,FileMode.Open,FileAccess.Read))using(var img=Image.FromStream(fs))return new Bitmap(img);}}catch{}
      return null;
    }
    void ApplyLanguage(){
      applyingLang=true;
      Text=Loc.T("app_title");
      if(importButton!=null)importButton.Text=Loc.T("import_json");
      if(potButton!=null)potButton.Text=Loc.T("sort_potential");
      if(obtButton!=null)obtButton.Text=Loc.T("sort_obtained");
      if(improveButton!=null)improveButton.Text=Loc.T("sort_upgrade");
      if(reevalButton!=null)reevalButton.Text=Loc.T("sort_reeval");
      if(refinementButton!=null)refinementButton.Text=Loc.T("sort_refinement");
      if(presetButton!=null)presetButton.Text=Loc.T("preset");
      if(coefficientButton!=null)coefficientButton.Text=Loc.T("coefficient");
      if(autoKeepButton!=null)autoKeepButton.Text=Loc.T("autokeep");
      if(rulesButton!=null)rulesButton.Text=Loc.T("rules");
      if(skillButton!=null)skillButton.Text=Loc.T("skillup");
      if(spdRankButton!=null)spdRankButton.Text=Loc.T("spd_rank");
      RefreshRtaSavedStatus(currentFile);
      reappNormalTip.SetToolTip(reappNormalBadge,Loc.T("tip_reapp_n"));
      reappAncientTip.SetToolTip(reappAncientBadge,Loc.T("tip_reapp_a"));
      refinementTip.SetToolTip(refinementBadge,Loc.T("tip_refine"));
      if(paypalButton!=null)paypalTip.SetToolTip(paypalButton,Loc.T("tip_paypal"));
      if(discordButton!=null)discordTip.SetToolTip(discordButton,Loc.T("tip_discord"));
      if(twitchButton!=null)twitchTip.SetToolTip(twitchButton,Loc.T("tip_twitch"));
      if(updateButton!=null){if(updateAvailable){updateButton.Text=Loc.T("update_check_btn");updateTip.SetToolTip(updateButton,Loc.T("tip_update"));if(updateBadge!=null)updateTip.SetToolTip(updateBadge,Loc.T("tip_update"));}else{updateButton.Text=Loc.T("update_check_btn");updateTip.SetToolTip(updateButton,Loc.T("tip_update_check"));if(updateBadge!=null)updateTip.SetToolTip(updateBadge,Loc.T("tip_update_check"));}}
      if(searchTip!=null){searchTip.SetToolTip(search,Loc.T("search_tip"));searchTip.SetToolTip(searchHint,Loc.T("search_tip"));}
      RefreshSearchHint();
      if(langCombo!=null){int want=Loc.En?1:0;if(langCombo.SelectedIndex!=want)langCombo.SelectedIndex=want;}
      RefreshFilterLabels();
      ApplyColumnHeaders();
      LayoutToolbar();
      PlaceLangCombo();
      RefreshRetentionButton();
      if(all.Count>0){RuneEngine.Calculate(all);if(viewMode=="obtained")SortObtained();else if(viewMode=="reeval")SortReeval();else RefreshGrid();}
      applyingLang=false;
    }
    void RefreshSearchHint(){
      if(searchHint==null||search==null)return;
      searchHint.Text=Loc.T("search_hint");
      searchHint.Visible=string.IsNullOrEmpty(search.Text);
      searchHint.SetBounds(search.Left+6,search.Top+1,Math.Max(10,search.Width-8),search.Height-2);
      if(searchHint.Visible)searchHint.BringToFront();
    }
    void LayoutToolbar(){if(potButton==null)return;int gap=8,rx=18;
      if(importButton!=null){importButton.PerformLayout();search.Left=importButton.Right+gap;int cx=search.Right+gap;set.Left=cx;cx+=150;slot.Left=cx;cx+=150;action.Left=cx;cx+=150;build.Left=cx;cx+=150;runeType.Left=cx;RefreshSearchHint();}
      potButton.Left=rx;rx=potButton.Right+gap;obtButton.Left=rx;rx=obtButton.Right+gap;improveButton.Left=rx;rx=improveButton.Right+gap;reevalButton.Left=rx;rx=reevalButton.Right+gap;refinementButton.Left=rx;rx=refinementButton.Right+gap;
      if(row1Divider!=null){row1Divider.Left=rx;rx+=gap+2;}presetButton.Left=rx;rx=presetButton.Right+gap;coefficientButton.Left=rx;rx=coefficientButton.Right+gap;autoKeepButton.Left=rx;rx=autoKeepButton.Right+gap;if(spdRankButton!=null)spdRankButton.Left=rx;
      RelayoutRow2();
    }
    void RefreshFilterLabels(){
      if(all.Count>0){FillFilters();return;}
      ReplaceFirst(set,Loc.T("filter_all_sets"));ReplaceFirst(slot,Loc.T("filter_all_slots"));ReplaceFirst(action,Loc.T("filter_all_actions"));ReplaceFirst(build,Loc.T("filter_all_presets"));
      int typeIdx=runeType.SelectedIndex;runeType.Items.Clear();runeType.Items.Add(Loc.T("filter_all_runes"));runeType.Items.Add(Loc.T("filter_normal"));runeType.Items.Add(Loc.T("filter_ancient"));runeType.SelectedIndex=Math.Max(0,Math.Min(2,typeIdx));
    }
    void ReplaceFirst(ComboBox c,string first){if(c.Items.Count==0){c.Items.Add(first);c.SelectedIndex=0;return;}c.Items[0]=first;}
    void ApplyColumnHeaders(){if(grid.Columns.Count<12)return;grid.Columns[0].HeaderText=Loc.T("col_rune");grid.Columns[1].HeaderText=Loc.T("col_main");grid.Columns[2].HeaderText=Loc.T("col_innate");grid.Columns[3].HeaderText=Loc.T("col_innate_val");grid.Columns[4].HeaderText=Loc.T("col_stat1");grid.Columns[5].HeaderText=Loc.T("col_stat2");grid.Columns[6].HeaderText=Loc.T("col_stat3");grid.Columns[7].HeaderText=Loc.T("col_stat4");grid.Columns[8].HeaderText=Loc.T("col_action");ConfigureGridForView();}
    // w = largeur MINIMALE (0 = aucune). AutoSize ajuste la largeur reelle au texte
    // + Padding ; MinimumSize.Height=32 garde toutes les lignes alignees (le texte
    // seul sur une police 9pt tient toujours sous 32, donc la hauteur ne bouge pas).
    Button Button(string text,int x,int y,int w,Color c){return Button(text,x,y,w,c,32);}
    // Surcharge avec hauteur explicite : lignes 1/2 (h=42) agrandies pour que les
    // badges rond/icone dedans deviennent plus lisibles ; police plus grande avec.
    // Style "fantome" (idee de Jeremy) : plus de fond de couleur pleine — fond sombre
    // du bandeau + contour colore (c) repris des couleurs de rarete des runes, avec
    // une legere surbrillance teintee au survol/clic.
    Button Button(string text,int x,int y,int w,Color c,int h){var b=new Button{Text=text,AutoSize=true,AutoSizeMode=AutoSizeMode.GrowAndShrink,MinimumSize=new Size(w,h),Padding=new Padding(16,0,16,0),Location=new Point(x,y),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(22,34,50),ForeColor=Color.Gainsboro,Font=new Font("Segoe UI Semibold",h>=40?10.5f:9f),Cursor=Cursors.Hand,TextAlign=ContentAlignment.MiddleCenter};b.FlatAppearance.BorderSize=2;b.FlatAppearance.BorderColor=c;b.FlatAppearance.MouseOverBackColor=Color.FromArgb(55,c.R,c.G,c.B);b.FlatAppearance.MouseDownBackColor=Color.FromArgb(90,c.R,c.G,c.B);return b;}
    void ShowRtaAdvisor(){if(string.IsNullOrWhiteSpace(currentFile)||!File.Exists(currentFile)){MessageBox.Show(Loc.T("import_first"),Loc.T("rta_title"),MessageBoxButtons.OK,MessageBoxIcon.Information);return;}string catalog=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","monsters","catalog.json");if(!File.Exists(catalog)){MessageBox.Show(Loc.T("catalog_missing"),Loc.T("rta_title"),MessageBoxButtons.OK,MessageBoxIcon.Error);return;}new RtaAdvisorForm(currentFile,catalog,Icon).Show(this);}
    void RefreshRtaSavedStatus(string jsonPath){if(rtaButton==null)return;int saved=RtaTargetEditor.SavedProfileCount();if(saved==0){rtaButton.Text=Loc.T("rta");return;}int runeUp=RtaTargetEditor.CountSavedRuneImprovements(jsonPath),artifactUp=RtaTargetEditor.CountSavedArtifactImprovements(jsonPath);rtaButton.Text="RTA "+saved+" • R↑"+runeUp+" A↑"+artifactUp;}
    // Badges agrandis avec les boutons (h=42) : rond 30->40, icone-rond 26->34,
    // pilule refinement 42x26->55x34, police proportionnelle a la taille (au lieu
    // des tailles fixes de l'ancien petit format).
    // Police alignee sur celle des boutons (Segoe UI Semibold) au lieu de Segoe UI
    // normal — le cadre doit avoir l'air de faire partie du bouton, pas d'etre colle dessus.
    void ConfigureCountBadge(CountBadge badge,int x,int y,Color color){
      badge.UseNewTag=true;
      if(CountBadge.NewIcon==null){
        string p=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","new-tag.png");
        try{if(File.Exists(p)){using(var fs=new FileStream(p,FileMode.Open,FileAccess.Read))using(var img=Image.FromStream(fs))CountBadge.NewIcon=HighQualityScale(img,60,30);}}catch{}
      }
      badge.SetBounds(x,y,60,30);badge.Text="0";badge.BadgeColor=color;badge.Visible=false;badge.Invalidate();
    }
    void ConfigureReappBadge(IconBadge badge,int x,int y,string imageName,string tip){badge.SetBounds(x,y,70,32);badge.Text="x0";badge.BadgeColor=Teal;badge.WideLayout=true;badge.ContentAlignRight=(badge==reappNormalBadge);badge.Font=new Font("Segoe UI Semibold",8.5f,FontStyle.Bold);string p=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets",imageName);try{if(File.Exists(p)){using(var fs=new FileStream(p,FileMode.Open,FileAccess.Read)){using(var img=Image.FromStream(fs))badge.IconImage=HighQualityScale(img,28,28);}}}catch{}if(badge==reappNormalBadge)reappNormalTip.SetToolTip(badge,tip);else reappAncientTip.SetToolTip(badge,tip);}
    // Meme style rond que les pierres reeval (icone + "xN" en chip dessous), plus
    // la pilule large d'avant.
    void ConfigureRefinementBadge(IconBadge badge,int x,int y){badge.SetBounds(x,y,64,32);badge.Text="x0";badge.BadgeColor=Teal;badge.WideLayout=true;badge.ContentAlignRight=true;badge.Font=new Font("Segoe UI Semibold",8.5f,FontStyle.Bold);string p=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","refinement-stone.png");try{if(File.Exists(p)){using(var fs=new FileStream(p,FileMode.Open,FileAccess.Read)){using(var img=Image.FromStream(fs))badge.IconImage=HighQualityScale(img,28,28);}}}catch{}refinementTip.SetToolTip(badge,Loc.T("tip_refine"));}
    // Meme fond que les boutons (22,34,50) au lieu du Bg quasi-noir (7,13,22) : les
    // filtres faisaient tache, trop sombres comparé au reste de la barre.
    static readonly Color ComboBg=Color.FromArgb(22,34,50);
    void AddCombo(Control p,ComboBox c,int x,string first){c.SetBounds(x,61,142,32);c.DropDownStyle=ComboBoxStyle.DropDownList;c.FlatStyle=FlatStyle.Flat;c.BackColor=ComboBg;c.ForeColor=Color.White;c.Items.Add(first);c.SelectedIndex=0;c.SelectedIndexChanged+=(s,e)=>RefreshGrid();p.Controls.Add(c);}
    void AddColumns(){string[] names={Loc.T("col_rune"),Loc.T("col_main"),Loc.T("col_innate"),Loc.T("col_innate_val"),Loc.T("col_stat1"),Loc.T("col_stat2"),Loc.T("col_stat3"),Loc.T("col_stat4"),Loc.T("col_action"),Loc.T("col_potential"),Loc.T("col_gem"),Loc.T("col_preset")};string[] props={"Rune","MainDisplay","Innate","InnateValueText","Stat1","Stat2","Stat3","Stat4","Action","Potential","Recommendation","BestBuild"};for(int i=0;i<names.Length;i++)grid.Columns.Add(new DataGridViewTextBoxColumn{Name=props[i],HeaderText=names[i],DataPropertyName=props[i],SortMode=DataGridViewColumnSortMode.NotSortable});grid.Columns[9].DefaultCellStyle.Format="0.000";}
    string DefaultExportFolder(){return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Summoners War Exporter Files");}
    string ActiveExportFolder(){if(!string.IsNullOrWhiteSpace(currentFile)){string folder=Path.GetDirectoryName(currentFile);if(!string.IsNullOrWhiteSpace(folder)&&Directory.Exists(folder))return folder;}return DefaultExportFolder();}
    void ChooseFile(){using(var d=new OpenFileDialog{Filter=Loc.T("filter_json"),Title=Loc.T("open_json")})if(d.ShowDialog()==DialogResult.OK){LoadFile(d.FileName);StartJsonWatcher();StartLiveLog();}}
    void TryAutoLoad(){string folder=DefaultExportFolder();if(!Directory.Exists(folder))return;try{var f=new DirectoryInfo(folder).GetFiles("*.json").Where(x=>x.Length>100000).OrderByDescending(x=>x.LastWriteTimeUtc).FirstOrDefault();if(f!=null)LoadFile(f.FullName);}catch{}}
    void StartJsonWatcher(){
      string folder=ActiveExportFolder();
      if(!Directory.Exists(folder))return;
      try{
        if(jsonWatcher!=null){jsonWatcher.EnableRaisingEvents=false;jsonWatcher.Dispose();jsonWatcher=null;}
        jsonWatcher=new FileSystemWatcher(folder,"*.json");
        jsonWatcher.NotifyFilter=NotifyFilters.FileName|NotifyFilters.LastWrite|NotifyFilters.CreationTime|NotifyFilters.Size;
        jsonWatcher.Created+=JsonDetected;jsonWatcher.Changed+=JsonDetected;jsonWatcher.Renamed+=(s,e)=>QueueJson(e.FullPath);
        jsonWatcher.EnableRaisingEvents=true;
        status.Text=(status.Text.Length>0?status.Text+"  •  ":"")+Loc.T("status_json_watch");
      }catch(Exception ex){status.Text=Loc.T("status_json_watch_fail",ex.Message);}
    }
    void JsonDetected(object sender,FileSystemEventArgs e){QueueJson(e.FullPath);}
    // liveSavedEvents (donc gameOrder/les 3 vraies teams World Boss) est en memoire pure et
    // repart a zero a chaque lancement de l'appli : sans combat World Boss REFAIT pendant
    // que l'appli tourne, "MODE VERIF - EQUIP. REEL" retombe sur le plan sauvegarde, qui
    // peut avoir derive au fil des recalculs de formule (le module recommande d'autres
    // monstres et ca finit par remplacer les vrais). On persiste donc sur disque les 3
    // dernieres compositions REELLEMENT vues (BattleWorldBossStart_v2), separement du
    // plan, pour les recharger immediatement au demarrage.
    string WorldBossRealOrderPath(){return Path.Combine(WorldBossDataFolder(),"worldboss-real-order.txt");}
    void LoadWorldBossRealOrder(){try{string path=WorldBossRealOrderPath();if(!File.Exists(path))return;var lines=File.ReadAllLines(path).Where(x=>!string.IsNullOrWhiteSpace(x)).Select(x=>x.Trim()).ToList();if(lines.Count==60)WorldBossOptimizer.RealOrderNames=lines;}catch{}}
    void SaveWorldBossRealOrder(List<string> names){try{string path=WorldBossRealOrderPath();Directory.CreateDirectory(Path.GetDirectoryName(path));File.WriteAllLines(path,names);}catch{}}
    // Petite boite de dialogue "coller du texte" maison (WinForms n'a pas d'InputBox
    // multiligne integre). Retourne null si Annuler, sinon le texte tape/colle.
    string PromptMultilineText(string prompt,string initial,Form owner){
      using(var dlg=new Form{Text="Coller l'ordre réel",Icon=Icon,BackColor=Bg,ForeColor=Color.White,Size=new Size(640,540),StartPosition=FormStartPosition.CenterParent,MinimizeBox=false,MaximizeBox=false}){
        var label=new Label{Text=prompt,Dock=DockStyle.Top,Height=76,Padding=new Padding(10,8,10,4),ForeColor=Color.Silver,Font=new Font("Segoe UI",9)};
        var box=new TextBox{Multiline=true,Dock=DockStyle.Fill,ScrollBars=ScrollBars.Vertical,Text=initial,BackColor=Color.FromArgb(30,40,50),ForeColor=Color.White,Font=new Font("Segoe UI",10)};
        var buttons=new Panel{Dock=DockStyle.Bottom,Height=48};
        var ok=Button("OK",dlg.ClientSize.Width-220,8,90,Color.FromArgb(53,94,116));ok.Anchor=AnchorStyles.Top|AnchorStyles.Right;
        var cancel=Button("Annuler",dlg.ClientSize.Width-120,8,100,Color.FromArgb(90,90,90));cancel.Anchor=AnchorStyles.Top|AnchorStyles.Right;
        bool okClicked=false;
        ok.Click+=(s,e)=>{okClicked=true;dlg.Close();};
        cancel.Click+=(s,e)=>{dlg.Close();};
        buttons.Controls.Add(ok);buttons.Controls.Add(cancel);
        dlg.Controls.Add(box);dlg.Controls.Add(buttons);dlg.Controls.Add(label);
        dlg.ShowDialog(owner);
        return okClicked?box.Text:null;
      }
    }
    string WorldBossOrderEventsPath(){return Path.Combine(WorldBossDataFolder(),"worldboss-order-events.jsonl");}
    void LoadWorldBossOrderEvents(){try{string path=WorldBossOrderEventsPath();if(!File.Exists(path))return;foreach(string line in File.ReadAllLines(path))if(!string.IsNullOrWhiteSpace(line))liveSavedEvents.Add(line);}catch{}}
    void SaveWorldBossOrderEvent(string line){try{string path=WorldBossOrderEventsPath();Directory.CreateDirectory(Path.GetDirectoryName(path));var kept=new List<string>();if(File.Exists(path))kept.AddRange(File.ReadAllLines(path).Where(x=>!string.IsNullOrWhiteSpace(x)));kept.Add(line);while(kept.Count>3)kept.RemoveAt(0);File.WriteAllLines(path,kept);}catch{}}
    void StartLiveLog(){
      liveLogTimer.Stop();string active=Path.Combine(ActiveExportFolder(),"full_log.txt"),fallback=Path.Combine(DefaultExportFolder(),"full_log.txt");liveLogPath=File.Exists(active)?active:fallback;try{liveLogOffset=File.Exists(liveLogPath)?new FileInfo(liveLogPath).Length:0;liveLogObservedLength=liveLogOffset;liveLogStableSince=DateTime.UtcNow;liveLogPending="";liveAwaitingResponse=false;liveAwaitingRequest=false;liveRequestCraft=liveRequestEquipment=liveRequestSkill=false;if(File.Exists(liveLogPath)){liveLogTimer.Start();status.Text=(status.Text.Length>0?status.Text+"  •  ":"")+Loc.T("status_swex_on",Path.GetFileName(ActiveExportFolder()));}else status.Text=Loc.T("status_swex_missing",ActiveExportFolder());}catch(Exception ex){status.Text=Loc.T("status_swex_fail",ex.Message);}
    }
    void ReadLiveLog(){
      if(string.IsNullOrWhiteSpace(liveLogPath)||!File.Exists(liveLogPath))return;try{long observed=new FileInfo(liveLogPath).Length;if(observed!=liveLogObservedLength){liveLogObservedLength=observed;liveLogStableSince=DateTime.UtcNow;return;}if(observed==liveLogOffset||(DateTime.UtcNow-liveLogStableSince).TotalMilliseconds<400)return;using(var fs=new FileStream(liveLogPath,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){if(fs.Length<liveLogOffset){liveLogOffset=0;liveLogPending="";liveAwaitingResponse=false;liveAwaitingRequest=false;liveRequestCraft=liveRequestEquipment=liveRequestSkill=false;}if(fs.Length==liveLogOffset)return;fs.Seek(liveLogOffset,SeekOrigin.Begin);using(var sr=new StreamReader(fs,Encoding.UTF8,true,4096,true)){liveLogPending+=sr.ReadToEnd();liveLogOffset=fs.Position;liveLogObservedLength=liveLogOffset;}}ProcessLiveLog();}catch(IOException){}catch(UnauthorizedAccessException){}catch(Exception ex){status.Text=Loc.T("status_swex_read",ex.Message);}
    }
    bool IsWorldBossSkillEvent(string line){if(string.IsNullOrEmpty(line))return false;return line.IndexOf("\"command\":\"UpgradeUnitSkill",StringComparison.OrdinalIgnoreCase)>=0||line.IndexOf("\"command\":\"PowerupUnit",StringComparison.OrdinalIgnoreCase)>=0||line.IndexOf("\"command\":\"SacrificeUnit",StringComparison.OrdinalIgnoreCase)>=0;}
    void ProcessLiveLog(){
      string normalized=liveLogPending.Replace("\r\n","\n");int lastNewline=normalized.LastIndexOf('\n');if(lastNewline<0)return;string complete=normalized.Substring(0,lastNewline+1);liveLogPending=normalized.Substring(lastNewline+1);string[] lines=complete.Split('\n');var messages=new List<string>();var craftIds=new HashSet<long>();bool skillChanged=false,equipmentChanged=false;var before=all.ToDictionary(x=>x.Id,x=>Tuple.Create(x.Level,x.Potential));for(int i=0;i<lines.Length;i++){string line=lines[i].Trim();if(line=="Request:"){liveAwaitingRequest=true;continue;}if(line=="Response:"){liveAwaitingRequest=false;liveAwaitingResponse=true;continue;}if(liveAwaitingRequest&&line.Length>0){liveAwaitingRequest=false;DismissLiveChoice(line);bool worldBossOrder=line.IndexOf("\"command\":\"BattleWorldBossStart_v2\"",StringComparison.OrdinalIgnoreCase)>=0&&line.IndexOf("\"unit_id_list\"",StringComparison.OrdinalIgnoreCase)>=0;liveRequestEquipment=line.IndexOf("\"command\":\"UpdateUnitEquip\"",StringComparison.Ordinal)>=0;liveRequestCraft=line.IndexOf("\"command\":\"AmplifyRune_v2\"",StringComparison.Ordinal)>=0||line.IndexOf("\"command\":\"ConvertRune_v2\"",StringComparison.Ordinal)>=0;liveRequestSkill=IsWorldBossSkillEvent(line)||line.IndexOf("\"command\":\"SummonUnit\"",StringComparison.Ordinal)>=0||line.IndexOf("\"command\":\"ConvertUnitToStorage\"",StringComparison.Ordinal)>=0||line.IndexOf("\"command\":\"getUnitStorageList\"",StringComparison.Ordinal)>=0;int protectedCount=RuneEngine.ApplyLiveDeckProtection(all,line);int markerCount=RuneEngine.ApplyLiveRuneMarker(all,line);if(protectedCount>0)messages.Add(protectedCount+" rune"+(protectedCount>1?"s":"")+" protégée"+(protectedCount>1?"s":"")+" par les decks");if(markerCount>0)messages.Add("Marquage de rune mis à jour");if(worldBossOrder){liveSavedEvents.Add(line);while(liveSavedEvents.Count(x=>x.IndexOf("\"command\":\"BattleWorldBossStart_v2\"",StringComparison.OrdinalIgnoreCase)>=0)>3){int old=liveSavedEvents.FindIndex(x=>x.IndexOf("\"command\":\"BattleWorldBossStart_v2\"",StringComparison.OrdinalIgnoreCase)>=0);if(old>=0)liveSavedEvents.RemoveAt(old);else break;}SaveWorldBossOrderEvent(line);messages.Add("Ordre réel du World Boss enregistré");}else if(protectedCount>0||markerCount>0)liveSavedEvents.Add(line);continue;}if(!liveAwaitingResponse||line.Length==0)continue;liveAwaitingResponse=false;DismissLiveChoice(line);bool craft=liveRequestCraft||line.IndexOf("\"command\":\"AmplifyRune_v2\"",StringComparison.Ordinal)>=0||line.IndexOf("\"command\":\"ConvertRune_v2\"",StringComparison.Ordinal)>=0;bool equipmentEvent=liveRequestEquipment||line.IndexOf("\"command\":\"UpdateUnitEquip\"",StringComparison.Ordinal)>=0;bool skillEvent=liveRequestSkill||IsWorldBossSkillEvent(line)||line.IndexOf("\"command\":\"SummonUnit\"",StringComparison.Ordinal)>=0||line.IndexOf("\"command\":\"ConvertUnitToStorage\"",StringComparison.Ordinal)>=0||line.IndexOf("\"command\":\"getUnitStorageList\"",StringComparison.Ordinal)>=0;liveRequestCraft=liveRequestEquipment=liveRequestSkill=false;try{var refinementComparison=RuneEngine.CompareRefinement(all,line);if(refinementComparison!=null){ShowRefinementDecision(refinementComparison);messages.Add("Comparaison de raffinage calculée");}var comparison=RuneEngine.CompareReappraisal(all,line);if(comparison!=null){ShowReappraisalDecision(comparison);messages.Add("Comparaison de réévaluation calculée");}int markerCount=RuneEngine.ApplyLiveRuneMarker(all,line);string message=RuneEngine.ApplyLiveEvent(all,line);if(markerCount>0)messages.Add("Marquage de rune confirmé");if(message.Length>0)messages.Add(message);if(skillEvent)skillChanged=true;if(equipmentEvent)equipmentChanged=true;if(markerCount>0||message.Length>0||skillEvent||equipmentEvent){liveSavedEvents.Add(line);if(craft){long id=RuneEngine.LiveRuneId(line);if(id>0)craftIds.Add(id);}}}catch{}}
      if(messages.Count==0&&!skillChanged&&!equipmentChanged)return;RuneEngine.Calculate(all);foreach(var rune in all){Tuple<int,double> old;if(before.TryGetValue(rune.Id,out old)&&rune.Level>old.Item1&&old.Item1<12){RuneEngine.CapAfterUpgrade(rune,old.Item2);upgradeCaps[rune.Id]=rune.Potential;}else if(craftIds.Contains(rune.Id)&&old!=null){RuneEngine.PreserveAfterCraft(rune,old.Item2);craftPotentials[rune.Id]=old.Item2;}}RuneEngine.ApplyRetentionRules(all);if(skillChanged)RefreshSkillUpSummary();RefreshReappBadges();RefreshUpgradeBadge();RefreshCurrentView(all.Count!=before.Count||all.Any(r=>!before.ContainsKey(r.Id)));if(equipmentChanged&&messages.Count==0)messages.Add(Loc.T("status_wb_equip"));if(liveStockRefresh!=null)try{liveStockRefresh();}catch{}if(craftIds.Count>0||messages.Any(m=>m.IndexOf("stock",StringComparison.OrdinalIgnoreCase)>=0))SaveStock();status.Text=(messages.Count>0?string.Join(" • ",messages.ToArray()):Loc.T("status_skill_updated"))+Loc.T("status_live_done");
    }
    void RefreshCurrentView(bool inventoryChanged=false){long topId=0;int top=grid.FirstDisplayedScrollingRowIndex;if(top>=0&&top<grid.Rows.Count){var visible=grid.Rows[top].DataBoundItem as RuneRow;if(visible!=null)topId=visible.Id;}if(viewMode=="obtained")SortObtained();else if(viewMode=="reeval")SortReeval();else RefreshGrid();RestoreGridPosition(inventoryChanged?0:topId,inventoryChanged?0:top);RefreshRetentionButton();QueueWorldBossRealtimeCalculation();}
    void RestoreGridPosition(long topId,int fallback){if(grid.Rows.Count==0)return;int target=-1;if(topId>0)for(int i=0;i<grid.Rows.Count;i++){var r=grid.Rows[i].DataBoundItem as RuneRow;if(r!=null&&r.Id==topId){target=i;break;}}if(target<0)target=Math.Min(Math.Max(0,fallback),grid.Rows.Count-1);try{grid.FirstDisplayedScrollingRowIndex=target;}catch{}}
    void RefreshRetentionButton(){if(retentionButton==null)return;string baseText=RuneEngine.SeuilVenteFixe?Loc.T("seuil_fixe",RuneEngine.SeuilApres12.ToString("0.000")):Loc.T("seuil_dyn",RuneEngine.LimiteRunesConservees.ToString("N0"),RuneEngine.SeuilApres12.ToString("0.000"));retentionButton.Text=baseText+(RuneEngine.ManaSaverMode?Loc.T("eco_mana"):"");}
    void ShowRetentionSettings(){
      var f=new Form{Text=Loc.T("retention_title"),Icon=Icon,BackColor=Bg,ForeColor=Color.White,ClientSize=new Size(540,400),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,StartPosition=FormStartPosition.CenterParent};
      f.Controls.Add(new Label{Text=Loc.T("retention_mode"),AutoSize=true,Location=new Point(28,22),Font=new Font("Segoe UI Semibold",12)});
      var mode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(30,55),Size=new Size(260,32),Font=new Font("Segoe UI",11),BackColor=Bg,ForeColor=Color.White};
      mode.Items.Add(Loc.T("retention_dyn"));mode.Items.Add(Loc.T("retention_fix"));mode.SelectedIndex=RuneEngine.SeuilVenteFixe?1:0;f.Controls.Add(mode);
      var dynamicLabel=new Label{Text=Loc.T("retention_count"),AutoSize=true,Location=new Point(28,105)};
      var number=new NumericUpDown{Minimum=100,Maximum=10000,Increment=50,Value=Math.Max(100,Math.Min(10000,RuneEngine.LimiteRunesConservees)),Location=new Point(30,132),Size=new Size(190,30),Font=new Font("Segoe UI",11),ThousandsSeparator=true};
      var fixedLabel=new Label{Text=Loc.T("retention_score"),AutoSize=true,Location=new Point(285,105)};
      var fixedValue=new NumericUpDown{Minimum=0,Maximum=100,DecimalPlaces=3,Increment=.050M,Value=(decimal)Math.Max(0,Math.Min(100,RuneEngine.ValeurSeuilVenteFixe)),Location=new Point(287,132),Size=new Size(190,30),Font=new Font("Segoe UI",11)};
      var info=new Label{AutoSize=false,Location=new Point(28,180),Size=new Size(485,60),ForeColor=Color.FromArgb(90,225,180),Font=new Font("Segoe UI Semibold",10)};
      f.Controls.Add(dynamicLabel);f.Controls.Add(number);f.Controls.Add(fixedLabel);f.Controls.Add(fixedValue);f.Controls.Add(info);
      // Économie de mana : NE touche pas au seuil de vente ci-dessus (Keep/Sell des runes
      // +12 inchangé) — ajoute juste une marge exigée en plus sur le score pour qu'une
      // rune sous +12 soit encore proposée "Pwr up" (utile quand le mana pour monter les
      // runes est limité).
      var manaTitle=new Label{Text=Loc.T("retention_mana"),AutoSize=true,Location=new Point(28,248),Font=new Font("Segoe UI Semibold",12)};
      var manaCheck=new CheckBox{Text=Loc.T("retention_mana_check"),AutoSize=true,Location=new Point(30,280),Checked=RuneEngine.ManaSaverMode,ForeColor=Color.White};
      var marginLabel=new Label{Text=Loc.T("retention_margin"),AutoSize=true,Location=new Point(30,312)};
      var margin=new NumericUpDown{Minimum=0,Maximum=50,DecimalPlaces=3,Increment=.250M,Value=(decimal)Math.Max(0,Math.Min(50,RuneEngine.MargePwrUpStrict)),Location=new Point(287,308),Size=new Size(190,30),Font=new Font("Segoe UI",10),Enabled=RuneEngine.ManaSaverMode};
      manaCheck.CheckedChanged+=(s,e)=>margin.Enabled=marginLabel.Enabled=manaCheck.Checked;
      f.Controls.Add(manaTitle);f.Controls.Add(manaCheck);f.Controls.Add(marginLabel);f.Controls.Add(margin);
      Action update=delegate{bool fixedMode=mode.SelectedIndex==1;number.Enabled=dynamicLabel.Enabled=!fixedMode;fixedValue.Enabled=fixedLabel.Enabled=fixedMode;double value=fixedMode?(double)fixedValue.Value:RuneEngine.RetentionThreshold(all,(int)number.Value);int count=all.Count(r=>r.Level>=12&&r.Grade>3&&r.Potential>=value);info.Text=fixedMode?Loc.T("retention_fix_info",value.ToString("0.000"),count.ToString("N0")):Loc.T("retention_dyn_info",value.ToString("0.000"));};
      mode.SelectedIndexChanged+=(s,e)=>update();number.ValueChanged+=(s,e)=>update();fixedValue.ValueChanged+=(s,e)=>update();
      var ok=Button(Loc.T("apply"),285,352,110,Teal);var cancel=Button(Loc.T("cancel").ToUpperInvariant(),405,352,105,Color.FromArgb(80,90,105));ok.DialogResult=DialogResult.OK;cancel.DialogResult=DialogResult.Cancel;f.Controls.Add(ok);f.Controls.Add(cancel);f.AcceptButton=ok;f.CancelButton=cancel;update();
      if(f.ShowDialog(this)!=DialogResult.OK)return;RuneEngine.SeuilVenteFixe=mode.SelectedIndex==1;RuneEngine.LimiteRunesConservees=(int)number.Value;RuneEngine.ValeurSeuilVenteFixe=(double)fixedValue.Value;RuneEngine.ManaSaverMode=manaCheck.Checked;RuneEngine.MargePwrUpStrict=(double)margin.Value;RuneEngine.ApplyRetentionRules(all);SaveRetentionSetting();RefreshUpgradeBadge();RefreshGrid();RefreshRetentionButton();status.Text=(RuneEngine.SeuilVenteFixe?Loc.T("status_seuil_fix",RuneEngine.SeuilApres12.ToString("0.000")):Loc.T("status_seuil_dyn",RuneEngine.SeuilApres12.ToString("0.000")))+(RuneEngine.ManaSaverMode?Loc.T("status_mana",RuneEngine.MargePwrUpStrict.ToString("0.000")):"");
    }
    void ShowRetentionSettingsLegacy(){var f=new Form{Text="Seuil dynamique",Icon=Icon,BackColor=Bg,ForeColor=Color.White,ClientSize=new Size(500,245),FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,StartPosition=FormStartPosition.CenterParent};f.Controls.Add(new Label{Text="Nombre de runes fortes à conserver",AutoSize=true,Location=new Point(28,25),Font=new Font("Segoe UI Semibold",12)});var number=new NumericUpDown{Minimum=100,Maximum=10000,Increment=50,Value=Math.Max(100,Math.Min(10000,RuneEngine.LimiteRunesConservees)),Location=new Point(30,62),Size=new Size(180,32),Font=new Font("Segoe UI",12),ThousandsSeparator=true};var threshold=new Label{AutoSize=false,Location=new Point(28,108),Size=new Size(445,72),ForeColor=Color.FromArgb(90,225,180),Font=new Font("Segoe UI Semibold",11)};Action update=delegate{int requested=(int)number.Value,eligible=RuneEngine.RetentionEligibleCount(all);double value=RuneEngine.RetentionThreshold(all,requested);threshold.Text="Seuil correspondant : "+value.ToString("0.000")+"\r\nRunes admissibles (score ≥ 8,8) : "+eligible.ToString("N0")+(eligible<requested?"\r\nQuota non rempli : seulement "+eligible.ToString("N0")+" runes admissibles.":"");threshold.ForeColor=eligible<requested?Color.FromArgb(255,190,70):Color.FromArgb(90,225,180);};number.ValueChanged+=(s,e)=>update();f.Controls.Add(number);f.Controls.Add(threshold);var ok=Button("APPLIQUER",250,195,105,Teal);var cancel=Button("ANNULER",365,195,105,Color.FromArgb(80,90,105));ok.DialogResult=DialogResult.OK;cancel.DialogResult=DialogResult.Cancel;f.Controls.Add(ok);f.Controls.Add(cancel);f.AcceptButton=ok;f.CancelButton=cancel;update();if(f.ShowDialog(this)!=DialogResult.OK)return;long topId=0;int top=grid.FirstDisplayedScrollingRowIndex;if(top>=0&&top<grid.Rows.Count){var r=grid.Rows[top].DataBoundItem as RuneRow;if(r!=null)topId=r.Id;}RuneEngine.LimiteRunesConservees=(int)number.Value;RuneEngine.ApplyRetentionRules(all);SaveRetentionSetting();SaveEngineSettings();RefreshUpgradeBadge();if(viewMode=="obtained")SortObtained();else if(viewMode=="reeval")SortReeval();else RefreshGrid();RestoreGridPosition(topId,top);RefreshRetentionButton();int eligibleNow=RuneEngine.RetentionEligibleCount(all);status.Text="Seuil dynamique : "+RuneEngine.LimiteRunesConservees.ToString("N0")+" demandées • "+eligibleNow.ToString("N0")+" admissibles • seuil "+RuneEngine.SeuilApres12.ToString("0.000");}
    void CloseLiveDecision(ref Form f){var x=f;f=null;if(x!=null&&!x.IsDisposed)try{x.Close();}catch{}}
    // Clic en jeu : ConfirmRune (reeval), confirmRefineRune (refinement), ReceiveMail.selected_rid (coffre 1 parmi 3/5).
    // Ferme la page-conseil sans attendre le bouton FERMER. Appeler sur Request ET Response du live log.
    void DismissLiveChoice(string line){
      if(string.IsNullOrEmpty(line))return;
      if(line.IndexOf("\"command\":\"ConfirmRune\"",StringComparison.OrdinalIgnoreCase)>=0)CloseLiveDecision(ref reappDecisionForm);
      if(line.IndexOf("\"command\":\"confirmRefineRune\"",StringComparison.OrdinalIgnoreCase)>=0)CloseLiveDecision(ref refineDecisionForm);
      if(line.IndexOf("\"selected_rid\"",StringComparison.OrdinalIgnoreCase)>=0)CloseLiveDecision(ref runeChoiceForm);
    }
    void ShowReappraisalDecision(ReappraisalComparison c){if(reappDecisionForm!=null&&!reappDecisionForm.IsDisposed)reappDecisionForm.Close();var f=new Form{Text=Loc.T("reeval_title"),Size=new Size(540,300),StartPosition=FormStartPosition.Manual,TopMost=true,BackColor=Color.FromArgb(7,18,30),ForeColor=Color.White,FormBorderStyle=FormBorderStyle.FixedToolWindow,ShowInTaskbar=false};reappDecisionForm=f;f.FormClosed+=(s,e)=>{if(reappDecisionForm==f)reappDecisionForm=null;};var area=Screen.PrimaryScreen.WorkingArea;f.Location=new Point(area.Right-f.Width-20,area.Top+20);string verdict=c.TakeAfter?Loc.T("take_new"):Loc.T("keep_current");Color verdictColor=c.TakeAfter?Color.FromArgb(30,235,155):Color.FromArgb(255,190,60);var title=new Label{Text=verdict,ForeColor=verdictColor,Font=new Font("Segoe UI",17,FontStyle.Bold),AutoSize=false,TextAlign=ContentAlignment.MiddleCenter,Dock=DockStyle.Top,Height=52};var beforeLabel=new Label{Text=Loc.T("before")+"\r\nPotential  "+c.BeforePotential.ToString("0.000")+"\r\n"+Loc.T("preset_colon",c.BeforePreset)+"\r\n"+c.BeforeStats,Font=new Font("Segoe UI",10),AutoSize=false,Location=new Point(18,62),Size=new Size(245,155),Padding=new Padding(8),BackColor=Color.FromArgb(13,30,45)};var afterLabel=new Label{Text=Loc.T("after")+"\r\nPotential  "+c.AfterPotential.ToString("0.000")+"\r\n"+Loc.T("preset_colon",c.AfterPreset)+"\r\n"+c.AfterStats,Font=new Font("Segoe UI",10),AutoSize=false,Location=new Point(272,62),Size=new Size(245,155),Padding=new Padding(8),BackColor=Color.FromArgb(13,30,45)};var close=new Button{Text=Loc.T("close"),Location=new Point(397,228),Size=new Size(120,30),BackColor=Color.FromArgb(180,45,65),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};close.Click+=(s,e)=>f.Close();f.Controls.Add(title);f.Controls.Add(beforeLabel);f.Controls.Add(afterLabel);f.Controls.Add(close);f.Show(this);}
    void ShowRefinementDecision(ReappraisalComparison c){if(refineDecisionForm!=null&&!refineDecisionForm.IsDisposed)refineDecisionForm.Close();var f=new Form{Text=Loc.T("refine_title"),Size=new Size(540,300),StartPosition=FormStartPosition.Manual,TopMost=true,BackColor=Color.FromArgb(7,18,30),ForeColor=Color.White,FormBorderStyle=FormBorderStyle.FixedToolWindow,ShowInTaskbar=false};refineDecisionForm=f;f.FormClosed+=(s,e)=>{if(refineDecisionForm==f)refineDecisionForm=null;};var area=Screen.PrimaryScreen.WorkingArea;f.Location=new Point(area.Right-f.Width-20,area.Top+20);string verdict=c.TakeAfter?Loc.T("take_new"):Loc.T("keep_current");Color verdictColor=c.TakeAfter?Color.FromArgb(30,235,155):Color.FromArgb(255,190,60);var title=new Label{Text=verdict,ForeColor=verdictColor,Font=new Font("Segoe UI",17,FontStyle.Bold),AutoSize=false,TextAlign=ContentAlignment.MiddleCenter,Dock=DockStyle.Top,Height=52};var beforeLabel=new Label{Text=Loc.T("before_refine")+"\r\nPotential  "+c.BeforePotential.ToString("0.000")+"\r\n"+Loc.T("preset_colon",c.BeforePreset)+"\r\n"+c.BeforeStats,Font=new Font("Segoe UI",10),AutoSize=false,Location=new Point(18,62),Size=new Size(245,155),Padding=new Padding(8),BackColor=Color.FromArgb(13,30,45)};var afterLabel=new Label{Text=Loc.T("after_refine")+"\r\nPotential  "+c.AfterPotential.ToString("0.000")+"\r\n"+Loc.T("preset_colon",c.AfterPreset)+"\r\n"+c.AfterStats,Font=new Font("Segoe UI",10),AutoSize=false,Location=new Point(272,62),Size=new Size(245,155),Padding=new Padding(8),BackColor=Color.FromArgb(13,30,45)};var close=new Button{Text=Loc.T("close"),Location=new Point(397,228),Size=new Size(120,30),BackColor=Color.FromArgb(180,45,65),ForeColor=Color.White,FlatStyle=FlatStyle.Flat};close.Click+=(s,e)=>f.Close();f.Controls.Add(title);f.Controls.Add(beforeLabel);f.Controls.Add(afterLabel);f.Controls.Add(close);f.Show(this);}
    void ShowRuneChoiceDecision(RuneChoiceComparison comparison){if(comparison==null||comparison.Choices==null||comparison.Choices.Count==0)return;
      // Sauve la signature deja consommee (RuneEngine.CompareRuneChoice l'a ajoutee) tout de
      // suite, pas seulement a la fermeture de l'appli — protege meme apres un crash/coupure.
      // Ferme aussi un ancien popup de coffre encore ouvert (jamais traite) avant d'en ouvrir un
      // nouveau, pour ne pas en accumuler qui reviennent au premier plan (TopMost) sans prevenir.
      SaveRuneChoiceSeen();if(runeChoiceForm!=null&&!runeChoiceForm.IsDisposed)runeChoiceForm.Close();
      var f=new Form{Text=Loc.T("chest_title",comparison.Choices.Count),Icon=Icon,Size=new Size(1120,520),StartPosition=FormStartPosition.CenterScreen,TopMost=true,BackColor=Bg,ForeColor=Color.White};runeChoiceForm=f;f.FormClosed+=(s,e)=>{if(runeChoiceForm==f)runeChoiceForm=null;};var title=new Label{Text=Loc.T("chest_head"),Dock=DockStyle.Top,Height=62,TextAlign=ContentAlignment.MiddleCenter,ForeColor=Color.FromArgb(255,184,45),Font=new Font("Segoe UI Semibold",18)};var g=new BufferedGrid{Dock=DockStyle.Fill,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoGenerateColumns=false,BackgroundColor=Bg,GridColor=Color.FromArgb(45,65,82),SelectionMode=DataGridViewSelectionMode.FullRowSelect,RowTemplate={Height=56}};g.EnableHeadersVisualStyles=false;g.ColumnHeadersHeight=42;g.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Teal,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",11),SelectionBackColor=Teal};g.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Bg,ForeColor=Color.White,SelectionBackColor=Color.FromArgb(26,70,83),Font=new Font("Segoe UI",10)};string[] heads={Loc.T("chest_rank"),Loc.T("col_rune"),Loc.T("col_main"),Loc.T("chest_subs"),Loc.T("col_potential"),Loc.T("col_preset"),Loc.T("chest_advice")};string[] props={"Rank","Rune","Main","Subs","Potential","Preset","Advice"};int[] widths={90,190,150,330,105,150,115};for(int i=0;i<heads.Length;i++)g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=heads[i],DataPropertyName=props[i],Width=widths[i]});int rank=0;g.DataSource=comparison.Choices.Select(x=>new{Rank=++rank,Rune=x.Set+" • slot "+x.Slot+" • "+x.Quality,Main=x.MainDisplay,Subs=string.Join("  •  ",x.Subs.Select(s=>s.BaseDisplay)),Potential=x.Potential.ToString("0.000"),Preset=x.BestBuild,Advice=rank==1?Loc.T("take"):"—"}).ToList();f.Controls.Add(g);f.Controls.Add(title);f.Show(this);}
    void QueueJson(string path){
      if(string.IsNullOrWhiteSpace(path)||!path.EndsWith(".json",StringComparison.OrdinalIgnoreCase))return;
      try{BeginInvoke((MethodInvoker)delegate{pendingJson=path;jsonDebounce.Stop();jsonDebounce.Start();status.Text=Loc.T("status_json_detected");});}catch{}
    }
    void ImportPendingJson(){
      jsonDebounce.Stop();string path=pendingJson;pendingJson="";
      if(path.Length==0||!File.Exists(path))return;
      try{
        using(var fs=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)){if(fs.Length==0){pendingJson=path;jsonDebounce.Start();return;}}
        DateTime write=File.GetLastWriteTimeUtc(path);if(path.Equals(currentFile,StringComparison.OrdinalIgnoreCase)&&write<=lastAutoImport)return;
        LoadFile(path);lastAutoImport=write;status.Text=Loc.T("status_json_imported",all.Count.ToString("N0"),Path.GetFileName(path));
      }catch(IOException){pendingJson=path;jsonDebounce.Start();}
      catch(UnauthorizedAccessException){pendingJson=path;jsonDebounce.Start();}
    }
    void LoadFile(string f){try{UseWaitCursor=true;status.Text=Loc.T("status_importing");Application.DoEvents();var sw=Stopwatch.StartNew();hiddenUpgradeIds.Clear();RuneEngine.ResetLiveSkillUnits();all=RuneEngine.Import(f,all);LoadSavedStock(f);LoadLiveChanges(f);RuneEngine.Calculate(all);LoadUpgradeCaps(f);RuneEngine.ApplyRetentionRules(all);currentFile=f;RefreshWorldBossSaleProtection();RefreshRtaSavedStatus(f);RefreshReappBadges();RefreshSkillUpSummary();RefreshUpgradeBadge();RefreshSpdRankBadge();FillFilters();SortObtained();QueueWorldBossRealtimeCalculation();status.Text=Loc.T("status_imported",all.Count.ToString("N0"),sw.Elapsed.TotalSeconds.ToString("0.0"),RuneEngine.LimiteRunesConservees.ToString("N0"),RuneEngine.SeuilApres12.ToString("0.###"),Path.GetFileName(f));}catch(Exception ex){MessageBox.Show(Loc.T("status_import_fail",ex.Message),"Rune Manager",MessageBoxButtons.OK,MessageBoxIcon.Error);}finally{UseWaitCursor=false;}}
    void RefreshReappBadges(){reappNormalBadge.Text="x"+RuneEngine.ReappNormal;reappAncientBadge.Text="x"+RuneEngine.ReappAncient;refinementBadge.Text="x"+RuneEngine.RefinementStones;reappNormalBadge.BringToFront();reappAncientBadge.BringToFront();refinementBadge.BringToFront();SetActionBorder(reevalButton,RuneEngine.ReappNormal>0||RuneEngine.ReappAncient>0);SetActionBorder(refinementButton,RuneEngine.RefinementStones>0);}
    void RefreshSkillUpSummary(){if(string.IsNullOrWhiteSpace(currentFile)||!File.Exists(currentFile))return;string catalog=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","monsters","catalog.json");if(!File.Exists(catalog))return;try{var roster=RuneEngine.LoadSkillUpRoster(currentFile,catalog);skillGroups=RuneEngine.AnalyzeSkillUps(roster);skillFamilies=RuneEngine.AnalyzeSkillUpFamilies(roster);skillGroupsRevision++;RefreshSkillUpBadge();}catch{}}
    void RefreshSkillUpBadge(){if(string.IsNullOrWhiteSpace(currentFile)||!File.Exists(currentFile)){SetCountAlert(skillButton,skillBadge,0);return;}int count=RuneEngine.SkillUpStockIncluded(currentFile)?skillGroups.Where(x=>!hiddenSkillTargetIds.Contains(x.Target.MasterId)).Sum(x=>x.UsableUpgrades):-1;SetCountAlert(skillButton,skillBadge,count>0?count:0);}
    void RefreshUpgradeBadge(){int count=all.Count(r=>r.Level>=12&&r.Action!="Sell"&&r.Potential>=RuneEngine.SeuilApres12&&!hiddenUpgradeIds.Contains(r.Id)&&HasAvailableImprovement(r));SetCountAlert(improveButton,improveBadge,count);}
    // Notification "nouveau record SPD" : compare le SPD max actuel de chaque slot+set au
    // dernier etat vu (spdSeenBest, persiste sur disque). Si une rune montee ameliore le SPD
    // max d'un slot, ce slot compte dans le badge et le contour du bouton passe rouge — meme
    // cadre/fonctionnement que les autres boutons (improveBadge etc). Se remet a zero des que
    // Jeremy ouvre le Classement SPD (notification consultee).
    void RefreshSpdRankBadge(){
      if(all.Count==0){SetCountAlert(spdRankButton,spdRankBadge,0);return;}
      var current=CurrentSpdBestMap();
      if(spdSeenBest.Count==0)spdSeenBest=new Dictionary<string,double>(current,StringComparer.OrdinalIgnoreCase);
      int count=0;foreach(var kv in current){double prev;if(!spdSeenBest.TryGetValue(kv.Key,out prev)||kv.Value>prev)count++;}
      SetCountAlert(spdRankButton,spdRankBadge,count);
    }
    void LoadSpdSeenBest(){spdSeenBest.Clear();if(!File.Exists(SpdBestSavePath))return;try{foreach(string line in File.ReadAllLines(SpdBestSavePath)){string[] p=line.Split('\t');double v;if(p.Length==3&&double.TryParse(p[2],NumberStyles.Any,CultureInfo.InvariantCulture,out v))spdSeenBest[p[0]+"|"+p[1]]=v;}}catch{}}
    void SaveSpdSeenBest(){try{File.WriteAllLines(SpdBestSavePath,spdSeenBest.Select(kv=>{int bar=kv.Key.IndexOf('|');return kv.Key.Substring(0,bar)+"\t"+kv.Key.Substring(bar+1)+"\t"+kv.Value.ToString(CultureInfo.InvariantCulture);}));}catch{}}
    // Demande Jeremy 2026-09-16 : un coffre "choisis 1 rune" deja traite il y a une semaine est
    // ressorti pendant une session de farm. La deduplication (RuneEngine.SeenRuneChoices) doit
    // survivre aux redemarrages de l'appli, sinon un rechargement repart de zero et peut rejouer
    // un vieux coffre. Persiste juste les signatures (memes rune_id reels tries).
    void LoadRuneChoiceSeen(){RuneEngine.SeenRuneChoices.Clear();if(!File.Exists(RuneChoiceSeenSavePath))return;try{foreach(string line in File.ReadAllLines(RuneChoiceSeenSavePath))if(!string.IsNullOrWhiteSpace(line))RuneEngine.SeenRuneChoices.Add(line.Trim());}catch{}}
    void SaveRuneChoiceSeen(){try{File.WriteAllLines(RuneChoiceSeenSavePath,RuneEngine.SeenRuneChoices);}catch{}}
    void FillFilters(){int setI=set.SelectedIndex,slotI=slot.SelectedIndex,actionI=action.SelectedIndex,buildI=build.SelectedIndex,typeI=runeType.SelectedIndex;Fill(set,Loc.T("filter_all_sets"),all.Select(x=>x.Set));Fill(action,Loc.T("filter_all_actions"),new[]{"Keep","Pwr up","Sell"});Fill(build,Loc.T("filter_all_presets"),all.Select(x=>x.BestBuild));slot.Items.Clear();slot.Items.Add(Loc.T("filter_all_slots"));for(int i=1;i<=6;i++)slot.Items.Add("Slot "+i);runeType.Items.Clear();runeType.Items.Add(Loc.T("filter_all_runes"));runeType.Items.Add(Loc.T("filter_normal"));runeType.Items.Add(Loc.T("filter_ancient"));Action<ComboBox,int> pick=(c,i)=>{if(c.Items.Count==0)return;c.SelectedIndex=Math.Max(0,Math.Min(c.Items.Count-1,i));};pick(set,setI);pick(slot,slotI);pick(action,actionI);pick(build,buildI);pick(runeType,typeI);}
    void Fill(ComboBox c,string first,IEnumerable<string> vals){c.Items.Clear();c.Items.Add(first);foreach(var v in vals.Where(x=>!string.IsNullOrWhiteSpace(x)).Distinct().OrderBy(x=>x))c.Items.Add(v);c.SelectedIndex=0;}
    // Demande Jeremy : bouton TRI REFINEMENT limite a ces 4 sets seulement (Violent, Swift,
    // Will, Despair), meme si CanRefine() (utilise pour le calcul global de RefinementGain)
    // reste valable pour tous les sets.
    static readonly HashSet<string> RefinementAllowedSets=new HashSet<string>(StringComparer.OrdinalIgnoreCase){"Violent","Swift","Will","Despair"};
    // Regroupe les 3 conditions du bouton TRI REFINEMENT (set autorisé, slot != 2, aucune
    // sous-stat flat) en une seule methode partagee — reutilisee aussi par ShowSpdRanking().
    static bool IsRefinementEligible(RuneRow r){return RuneEngine.CanRefine(r)&&RefinementAllowedSets.Contains(r.Set)&&r.Slot!=2&&!r.Subs.Any(x=>x.Stat=="HP+"||x.Stat=="Atk+"||x.Stat=="Def+");}
    // SPD "de base" (hors meule/Grind) : Main + Innate + toutes les sous-stats SPD. Demande
    // Jeremy pour reperer, par set+slot, la ou le SPD manque le plus avant de reorienter la
    // recommandation de refinement (actuellement basee sur le Potential moyen) vers le SPD.
    static double SpdBase(RuneRow r){double v=0;if(r.Main=="Spd")v+=r.MainValue;if(r.Innate=="Spd")v+=r.InnateValue;foreach(var sub in r.Subs)if(sub.Stat=="Spd")v+=sub.Value;return v;}
    static double SpdWithGrind(RuneRow r){double v=SpdBase(r);foreach(var sub in r.Subs)if(sub.Stat=="Spd")v+=sub.Grind;return v;}
    IEnumerable<RuneRow> Filter(){RefreshWorldBossSaleProtection();IEnumerable<RuneRow> q=all;bool selling=Convert.ToString(action.SelectedItem)=="Sell";string s=search.Text.Trim();int exactLevel;
      // "violent slot 1" (nom de set + "slot" + numero) : filtre ce set+slot precis et trie
      // par SPD (base, hors meule) decroissant — outil de verification manuelle demande par
      // Jeremy, pour qu'il puisse lui-meme controler quelle rune est la plus rapide sur un
      // slot/set donne (sans les exclusions specifiques a TRI REFINEMENT ou au Classement
      // SPD, qui se sont plantees plusieurs fois : gemme, antique, sous-stat flat...). Ce
      // resultat ignore volontairement les autres filtres (set/slot/action/etc.) — recherche
      // prioritaire, court-circuite le reste de Filter().
      int slotIdx=s.ToLowerInvariant().IndexOf("slot");
      if(slotIdx>=0){
        string afterSlot=s.Substring(slotIdx+4).Trim();
        int digitCount=0;while(digitCount<afterSlot.Length&&char.IsDigit(afterSlot[digitCount]))digitCount++;
        int spdSearchSlot;
        if(digitCount>0&&int.TryParse(afterSlot.Substring(0,digitCount),out spdSearchSlot)&&spdSearchSlot>=1&&spdSearchSlot<=6){
          string setToken=s.Substring(0,slotIdx).Trim();
          string matchedSet=setToken.Length>0?all.Select(r=>r.Set).Distinct().FirstOrDefault(name=>name.IndexOf(setToken,StringComparison.OrdinalIgnoreCase)>=0):null;
          var spdQuery=all.Where(r=>r.Slot==spdSearchSlot);
          if(matchedSet!=null)spdQuery=spdQuery.Where(r=>r.Set.Equals(matchedSet,StringComparison.OrdinalIgnoreCase));
          return spdQuery.OrderByDescending(r=>SpdBase(r));
        }
      }
      // "+9", "+12", "+0" : recherche exacte du niveau de la rune (+0 a +15) au lieu d'une
      // recherche texte substring — demande Jeremy pour filtrer par niveau precis.
      if(s.Length>0&&s[0]=='+'&&int.TryParse(s.Substring(1),out exactLevel))q=q.Where(r=>r.Level==exactLevel);
      else if(s.Length>0)q=q.Where(r=>(r.Rune+" "+r.Main+" "+r.Innate+" "+r.Stat1+" "+r.Stat2+" "+r.Stat3+" "+r.Stat4+" "+r.Marker).IndexOf(s,StringComparison.OrdinalIgnoreCase)>=0);if(set.SelectedIndex>0)q=q.Where(r=>r.Set==(string)set.SelectedItem);if(slot.SelectedIndex>0)q=q.Where(r=>r.Slot==slot.SelectedIndex);if(action.SelectedIndex>0)q=q.Where(r=>r.Action==(string)action.SelectedItem);if(build.SelectedIndex>0)q=q.Where(r=>r.BestBuild==(string)build.SelectedItem);if(runeType.SelectedIndex==1)q=q.Where(r=>!r.Ancient);else if(runeType.SelectedIndex==2)q=q.Where(r=>r.Ancient);if(!selling&&(viewMode=="potential"||viewMode=="upgrade"))q=q.Where(r=>r.Action!="Sell");if(!selling&&viewMode=="upgrade")q=q.Where(r=>r.Potential>=RuneEngine.SeuilApres12).Where(r=>r.Level>=12).Where(r=>!hiddenUpgradeIds.Contains(r.Id)).Where(HasAvailableImprovement);if(!selling&&viewMode=="refinement")return RefinementTargetsInPriorityOrder();if(!selling&&viewMode=="reeval")return ReevalTargetsInPriorityOrder(q);return potentialDesc?q.OrderByDescending(r=>r.Potential):q.OrderBy(r=>r.Potential);}
    void RefreshGrid(){if(grid.Columns.Count==0)return;RefreshWorldBossSaleProtection();ConfigureGridForView();var q=Filter().ToList();grid.AutoGenerateColumns=false;grid.RowTemplate.Height=56;grid.DataSource=q;foreach(DataGridViewRow row in grid.Rows)row.Height=56;counters.Text=Loc.T("counters",all.Count.ToString("N0"),all.Count(x=>x.Action=="Keep").ToString("N0"),all.Count(x=>x.Action=="Pwr up").ToString("N0"),all.Count(x=>x.Action=="Sell").ToString("N0"),q.Count.ToString("N0"));FitColumns();}
    void ConfigureGridForView(){bool refinement=viewMode=="refinement";grid.Columns[9].DataPropertyName="Potential";grid.Columns[9].HeaderText=Loc.T("col_potential");grid.Columns[10].DataPropertyName="Recommendation";grid.Columns[10].HeaderText=Loc.T("col_gem");grid.Columns[11].DataPropertyName=refinement?"RefinementPreset":"BestBuild";grid.Columns[11].HeaderText=refinement?Loc.T("col_refine_preset"):Loc.T("col_preset");}
    void SortObtained(){viewMode="obtained";potentialDesc=false;ConfigureGridForView();var q=Filter().OrderByDescending(r=>r.Obtained).ThenByDescending(r=>r.Id).ToList();grid.DataSource=q;counters.Text=Loc.T("counters",all.Count.ToString("N0"),all.Count(x=>x.Action=="Keep").ToString("N0"),all.Count(x=>x.Action=="Pwr up").ToString("N0"),all.Count(x=>x.Action=="Sell").ToString("N0"),q.Count.ToString("N0"));FitColumns();status.Text=Loc.T("status_obtained",q.Count.ToString("N0"));}
    void SortReeval(){viewMode="reeval";potentialDesc=true;ConfigureGridForView();var q=Filter().ToList();grid.DataSource=q;counters.Text=Loc.T("counters_reeval",all.Count.ToString("N0"),q.Count.ToString("N0"),RuneEngine.ReappNormal,RuneEngine.ReappAncient,q.Count.ToString("N0"));FitColumns();status.Text=Loc.T("status_reeval",q.Count.ToString("N0"));}
    bool HasAvailableImprovement(RuneRow r){if(r.RecommendationInStock)return true;foreach(var s in r.Subs){bool replaced=r.RecommendSource==s.Stat&&r.RecommendTarget!=s.Stat;if(!replaced&&CanUseGrind(r,s))return true;}return false;}
    void FitColumns(){if(grid.Columns.Count==0)return;int[] caps={90,145,105,100,245,245,245,245,90,125,390,155};for(int c=0;c<grid.Columns.Count;c++){grid.Columns[c].AutoSizeMode=DataGridViewAutoSizeColumnMode.None;if(c==0){grid.Columns[c].Width=90;continue;}int w=TextRenderer.MeasureText(grid.Columns[c].HeaderText,grid.ColumnHeadersDefaultCellStyle.Font).Width+18;int limit=Math.Min(grid.Rows.Count,220);for(int r=0;r<limit;r++){object v=grid.Rows[r].Cells[c].Value;if(v!=null)w=Math.Max(w,TextRenderer.MeasureText(Convert.ToString(v),grid.DefaultCellStyle.Font).Width+12);}grid.Columns[c].Width=Math.Min(caps[c],Math.Max(62,w));}int total=grid.Columns.Cast<DataGridViewColumn>().Sum(x=>x.Width);int spare=grid.ClientSize.Width-4-total;if(spare>0){int[] grow={4,5,6,7,10};int each=spare/grow.Length;foreach(int c in grow)grid.Columns[c].Width+=each;}}
    void FormatCell(object sender,DataGridViewCellFormattingEventArgs e){var r=grid.Rows[e.RowIndex].DataBoundItem as RuneRow;if(r==null)return;e.CellStyle.Padding=new Padding(4);bool orange=r.Grade>=5;if(r.Action=="Sell"){e.CellStyle.BackColor=Color.FromArgb(82,20,22);if(e.ColumnIndex==0)e.CellStyle.ForeColor=orange?Color.FromArgb(255,116,22):Color.FromArgb(176,58,255);}if(e.ColumnIndex==8)e.CellStyle.ForeColor=r.Action=="Sell"?Color.FromArgb(255,110,110):Color.FromArgb(22,210,119);if(e.ColumnIndex==9)e.CellStyle.ForeColor=Color.FromArgb(116,235,184);if(e.ColumnIndex==0&&r.Action!="Sell")e.CellStyle.ForeColor=orange?Color.FromArgb(255,126,20):Color.FromArgb(183,58,255);if(e.ColumnIndex>=4&&e.ColumnIndex<=7){int si=e.ColumnIndex-4;if(si<r.Subs.Count){var s=r.Subs[si];bool replaced=r.RecommendSource==s.Stat&&r.RecommendTarget!=s.Stat;if(!replaced&&CanUseGrind(r,s))e.CellStyle=new DataGridViewCellStyle(e.CellStyle){Padding=new Padding(3),BackColor=e.CellStyle.BackColor,ForeColor=e.CellStyle.ForeColor,SelectionBackColor=e.CellStyle.SelectionBackColor,SelectionForeColor=e.CellStyle.SelectionForeColor};}}if(e.ColumnIndex==10&&r.RecommendationInStock)e.CellStyle.ForeColor=Color.FromArgb(255,105,105);}
    bool IsGrindable(string s){return s=="HP+"||s=="HP%"||s=="Atk+"||s=="Atk%"||s=="Def+"||s=="Def%"||s=="Spd";}
    double MaxGrind(string s,bool a){if(s=="HP+")return a?610:550;if(s=="Atk+"||s=="Def+")return a?34:30;if(s=="HP%"||s=="Atk%"||s=="Def%")return a?12:10;if(s=="Spd")return a?6:5;return 0;}
    double MaxGrindViolet(string s,bool a){if(s=="HP+")return a?460:400;if(s=="Atk+"||s=="Def+")return a?22:18;if(s=="HP%"||s=="Atk%"||s=="Def%")return a?9:7;if(s=="Spd")return a?5:4;return 0;}
    double MaxGemViolet(string s,bool a){if(s=="HP+")return a?440:380;if(s=="Atk+"||s=="Def+")return a?30:26;if(s=="HP%"||s=="Atk%"||s=="Def%")return a?13:11;if(s=="Spd")return a?9:8;if(s=="CtR%")return a?8:7;if(s=="CtD%")return a?10:8;if(s=="Res%"||s=="Acc%")return a?11:9;return 0;}
    double MaxGemLegendary(string s,bool a){if(s=="HP+")return a?640:580;if(s=="Atk+"||s=="Def+")return a?44:40;if(s=="HP%"||s=="Atk%"||s=="Def%")return a?15:13;if(s=="Spd")return a?11:10;if(s=="CtR%")return a?10:9;if(s=="CtD%")return a?12:10;if(s=="Res%"||s=="Acc%")return a?13:11;return 0;}
    Color CraftValueColor(double value,double violetMax,double legendaryMax){const double eps=.0001;if(legendaryMax>0&&value>=legendaryMax-eps)return Color.FromArgb(255,126,20);if(violetMax>0&&value>=violetMax-eps)return Color.FromArgb(183,58,255);return Color.FromArgb(52,160,255);}
    bool HasCraftGrade(RuneRow r,string type,string stat,int grade){return RuneEngine.StockCountDetail(r,type,stat,false,grade)>0||(!r.Ancient&&RuneEngine.StockCountDetail(r,type,stat,true,grade)>0);}
    bool CanUseGrind(RuneRow r,SubStat s){if(!IsGrindable(s.Stat))return false;if(HasCraftGrade(r,"Meule",s.Stat,5)&&s.Grind<MaxGrind(s.Stat,r.Ancient))return true;if(HasCraftGrade(r,"Meule",s.Stat,4)&&s.Grind<MaxGrindViolet(s.Stat,r.Ancient))return true;return false;}
    void PaintCraftBorder(object sender,DataGridViewCellPaintingEventArgs e){if(e.RowIndex<0)return;var r=grid.Rows[e.RowIndex].DataBoundItem as RuneRow;if(r==null)return;if(e.ColumnIndex==0){PaintRuneIcon(e,r);return;}Color? border=null;SubStat paintedStat=null;bool recommendationCell=e.ColumnIndex==10;if(e.ColumnIndex>=4&&e.ColumnIndex<=7){int i=e.ColumnIndex-4;if(i<r.Subs.Count){var s=r.Subs[i];paintedStat=s;bool replaced=r.RecommendSource==s.Stat&&r.RecommendTarget!=s.Stat;if(!replaced&&CanUseGrind(r,s))border=Color.FromArgb(34,211,238);}}else if(recommendationCell&&r.RecommendationInStock)border=Color.FromArgb(239,68,68);if(paintedStat!=null)PaintCraftText(e,r,paintedStat);else if(recommendationCell)PaintRecommendationText(e,r);else if(border.HasValue)e.Paint(e.CellBounds,DataGridViewPaintParts.All);if(border.HasValue){using(var p=new Pen(border.Value,2))e.Graphics.DrawRectangle(p,e.CellBounds.X+1,e.CellBounds.Y+1,e.CellBounds.Width-3,e.CellBounds.Height-3);}if(paintedStat!=null||recommendationCell||border.HasValue)e.Handled=true;}
    void PaintCraftText(DataGridViewCellPaintingEventArgs e,RuneRow r,SubStat s){e.Paint(e.CellBounds,DataGridViewPaintParts.Background|DataGridViewPaintParts.Border|DataGridViewPaintParts.SelectionBackground|DataGridViewPaintParts.Focus);string text=s.Display,first=text,grind="";int cut=text.LastIndexOf(" (+",StringComparison.Ordinal);if(cut>=0){first=text.Substring(0,cut);grind=text.Substring(cut);}Color normal=e.State.HasFlag(DataGridViewElementStates.Selected)?e.CellStyle.SelectionForeColor:e.CellStyle.ForeColor;Color firstColor=s.Gemmed?CraftValueColor(s.Value,MaxGemViolet(s.Stat,r.Ancient),MaxGemLegendary(s.Stat,r.Ancient)):normal;Color grindColor=s.Grind!=0?CraftValueColor(s.Grind,MaxGrindViolet(s.Stat,r.Ancient),MaxGrind(s.Stat,r.Ancient)):Color.FromArgb(45,210,115);Font font=e.CellStyle.Font??grid.Font;var flags=TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter;int x=e.CellBounds.X+Math.Max(4,e.CellStyle.Padding.Left),y=e.CellBounds.Y,w=Math.Max(1,e.CellBounds.Right-x-3),h=e.CellBounds.Height;TextRenderer.DrawText(e.Graphics,first,font,new Rectangle(x,y,w,h),firstColor,flags);int used=TextRenderer.MeasureText(e.Graphics,first,font,new Size(int.MaxValue,h),flags).Width;if(grind.Length>0&&used<w)TextRenderer.DrawText(e.Graphics,grind,font,new Rectangle(x+used,y,w-used,h),grindColor,flags);}
    void PaintRecommendationText(DataGridViewCellPaintingEventArgs e,RuneRow r){e.Paint(e.CellBounds,DataGridViewPaintParts.Background|DataGridViewPaintParts.Border|DataGridViewPaintParts.SelectionBackground|DataGridViewPaintParts.Focus);string text=r.Recommendation??"";Color normal=e.State.HasFlag(DataGridViewElementStates.Selected)?e.CellStyle.SelectionForeColor:e.CellStyle.ForeColor;Font font=e.CellStyle.Font??grid.Font;var flags=TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix|TextFormatFlags.SingleLine|TextFormatFlags.VerticalCenter;int x=e.CellBounds.X+Math.Max(4,e.CellStyle.Padding.Left),y=e.CellBounds.Y,w=Math.Max(1,e.CellBounds.Right-x-3),h=e.CellBounds.Height;int arrow=text.IndexOf(" → ",StringComparison.Ordinal);if(arrow<0){TextRenderer.DrawText(e.Graphics,text,font,new Rectangle(x,y,w,h),normal,flags);return;}string left=text.Substring(0,arrow),middle=" → ",right=text.Substring(arrow+3);bool statChange=r.RecommendSource.Length>0&&r.RecommendTarget.Length>0&&r.RecommendSource!=r.RecommendTarget;if(statChange)DrawRecommendationPart(e.Graphics,"⇄ ",font,Color.FromArgb(255,92,145),flags,ref x,y,ref w,h);var source=r.Subs.FirstOrDefault(s=>s.Stat==r.RecommendSource);Color leftColor=source!=null&&source.Gemmed?CraftValueColor(source.Value,MaxGemViolet(source.Stat,r.Ancient),MaxGemLegendary(source.Stat,r.Ancient)):Color.FromArgb(45,210,115);double targetValue=RecommendationValue(right);Color rightColor=CraftValueColor(targetValue,MaxGemViolet(r.RecommendTarget,r.Ancient),MaxGemLegendary(r.RecommendTarget,r.Ancient));DrawRecommendationPart(e.Graphics,left,font,leftColor,flags,ref x,y,ref w,h);DrawRecommendationPart(e.Graphics,middle,font,normal,flags,ref x,y,ref w,h);DrawRecommendationPart(e.Graphics,right,font,rightColor,flags,ref x,y,ref w,h);}
    void DrawRecommendationPart(Graphics g,string text,Font font,Color color,TextFormatFlags flags,ref int x,int y,ref int w,int h){if(w<=0)return;TextRenderer.DrawText(g,text,font,new Rectangle(x,y,w,h),color,flags);int used=TextRenderer.MeasureText(g,text,font,new Size(int.MaxValue,h),flags).Width;x+=used;w=Math.Max(0,w-used);}
    double RecommendationValue(string text){int plus=text.LastIndexOf('+');if(plus<0)return 0;int end=plus+1;while(end<text.Length&&(char.IsDigit(text[end])||text[end]=='.'||text[end]==','))end++;double value;return double.TryParse(text.Substring(plus+1,end-plus-1).Replace(',','.'),NumberStyles.Any,CultureInfo.InvariantCulture,out value)?value:0;}
    static Color CroquisQualityColor(int grade){return grade>=5?CroquisOrange:grade==4?CroquisViolet:CroquisBlue;}
    static int CroquisQualityKey(int grade){return grade>=5?5:grade==4?4:3;}
    Image LoadCroquis3dPreview(string setName,int slot,int grade){
      slot=Math.Max(1,Math.Min(6,slot));
      if(!EnsureCroquisSlot(slot))return null;
      string set=string.IsNullOrEmpty(setName)?"Despair":setName;
      int q=CroquisQualityKey(grade);
      string artKey=set+"|"+slot;
      string key=artKey+"|"+q;
      Image cached;if(croquis3dByKey.TryGetValue(key,out cached))return cached;
      Bitmap baked;
      if(!croquis3dBaseByKey.TryGetValue(artKey,out baked)||baked==null){
        string custom=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","croquis-rune-3d-"+set.ToLowerInvariant()+"-slot"+slot+".png");
        if(File.Exists(custom))baked=BakeCroquisPng(custom);
        if(baked==null){
          if(set.Equals("Despair",StringComparison.OrdinalIgnoreCase))baked=new Bitmap(croquis3dSlot[slot]);
          else baked=new Bitmap(EnsureBlankSlot(slot));
        }
        croquis3dBaseByKey[artKey]=baked;
        croquis3dOpaqueByKey[artKey]=MeasureOpaqueStone(baked);
      }
      Bitmap bmp=q>=5?baked:(q==4?TintCroquisCreux(baked,CroquisViolet):TintCroquisCreux(baked,Cyan));
      croquis3dByKey[key]=bmp;
      croquis3dOpaqueByKey[key]=croquis3dOpaqueByKey[artKey];
      return bmp;
    }
    Bitmap EnsureBlankSlot(int slot){
      if(croquis3dBlankSlot[slot]==null)croquis3dBlankSlot[slot]=InpaintOrangeGlyph((Bitmap)croquis3dSlot[slot]);
      return (Bitmap)(croquis3dBlankSlot[slot]??croquis3dSlot[slot]);
    }
    Image AncientPulse(string artKey,Image preview){
      Image p;if(croquis3dPulseByKey.TryGetValue(artKey,out p))return p;
      var bmp=preview as Bitmap;
      p=bmp!=null?BuildAncientPulseMask(bmp):preview;
      croquis3dPulseByKey[artKey]=p;
      return p;
    }
    static Bitmap BakeCroquisPng(string p){
      try{
        using(var fs=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.Read))
        using(var src=Image.FromStream(fs,false,false))
          return BakeCroquisImage(src);
      }catch{return null;}
    }
    static Bitmap BakeCroquisImage(Image src){
      int max=140,w,h;
      if(src.Width<=max&&src.Height<=max){w=src.Width;h=src.Height;}
      else if(src.Width>=src.Height){w=max;h=Math.Max(1,src.Height*w/src.Width);}
      else{h=max;w=Math.Max(1,src.Width*h/src.Height);}
      int pad=6;
      var baked=new Bitmap(w+pad*2,h+pad*2,PixelFormat.Format32bppArgb);
      using(var g=Graphics.FromImage(baked)){
        g.CompositingMode=CompositingMode.SourceOver;
        g.SmoothingMode=SmoothingMode.None;
        g.InterpolationMode=(src.Width>max||src.Height>max)?InterpolationMode.HighQualityBicubic:InterpolationMode.NearestNeighbor;
        g.PixelOffsetMode=PixelOffsetMode.HighQuality;
        var dest=new Rectangle(pad,pad,w,h);
        using(var ia=new ImageAttributes()){
          ia.SetColorMatrix(new ColorMatrix(new float[][]{
            new float[]{0,0,0,0,0},
            new float[]{0,0,0,0,0},
            new float[]{0,0,0,0,0},
            new float[]{0,0,0,0.50f,0},
            new float[]{0,0,0,0,1}
          }));
          int[] ox={-2,2,0,0,-2,-2,2,2},oy={0,0,-2,2,-2,2,-2,2};
          for(int i=0;i<ox.Length;i++)
            g.DrawImage(src,new Rectangle(dest.X+ox[i],dest.Y+oy[i],dest.Width,dest.Height),0,0,src.Width,src.Height,GraphicsUnit.Pixel,ia);
        }
        g.DrawImage(src,dest);
      }
      return baked;
    }
    static RectangleF MeasureOpaqueStone(Bitmap bmp){
      int w=bmp.Width,h=bmp.Height;
      var data=bmp.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
      int stride=data.Stride,bytes=Math.Abs(stride)*h;var buf=new byte[bytes];
      Marshal.Copy(data.Scan0,buf,0,bytes);bmp.UnlockBits(data);
      int x0=w,y0=h,x1=-1,y1=-1;
      for(int y=0;y<h;y++){
        int row=y*stride;
        for(int x=0;x<w;x++){
          int i=row+x*4,a=buf[i+3];if(a<40)continue;
          int b=buf[i],gch=buf[i+1],r=buf[i+2];
          if(r<45&&gch<45&&b<45)continue;
          if(x<x0)x0=x;if(y<y0)y0=y;if(x>x1)x1=x;if(y>y1)y1=y;
        }
      }
      return x1<0?new RectangleF(0,0,w,h):new RectangleF(x0,y0,x1-x0+1,y1-y0+1);
    }
    static Bitmap TintCroquisCreux(Bitmap src,Color tint){
      var bmp=new Bitmap(src);
      int w=bmp.Width,h=bmp.Height;
      var data=bmp.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
      int stride=data.Stride,bytes=Math.Abs(stride)*h;var buf=new byte[bytes];
      Marshal.Copy(data.Scan0,buf,0,bytes);
      int tr=tint.R,tg=tint.G,tb=tint.B;
      for(int y=0;y<h;y++){
        int row=y*stride;
        for(int x=0;x<w;x++){
          int i=row+x*4,a=buf[i+3];if(a<80)continue;
          int b=buf[i],gch=buf[i+1],r=buf[i+2];
          if(!(r>=155&&r>=gch+8&&b<=130&&r>=b+25))continue;
          int lum=(r*77+gch*150+b*29)>>8;
          int nr=tr*lum/169,ng=tg*lum/169,nb=tb*lum/169;
          if(nr>255)nr=255;if(ng>255)ng=255;if(nb>255)nb=255;
          buf[i]=(byte)nb;buf[i+1]=(byte)ng;buf[i+2]=(byte)nr;
        }
      }
      Marshal.Copy(buf,0,data.Scan0,bytes);bmp.UnlockBits(data);
      return bmp;
    }
    bool EnsureCroquisSlot(int slot){
      if(croquis3dSlot[slot]!=null)return true;
      string dir=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets");
      string p=Path.Combine(dir,"croquis-rune-3d-slot"+slot+".png");
      if(!File.Exists(p))p=Path.Combine(dir,"croquis-rune-3d.png");
      if(!File.Exists(p))return false;
      var baked=BakeCroquisPng(p);
      if(baked==null)return false;
      croquis3dSlot[slot]=baked;
      croquis3dOpaqueBounds[slot]=MeasureOpaqueStone(baked);
      croquis3dSetBounds[slot]=croquis3dOpaqueBounds[slot];
      return true;
    }
    void StampSetGlyph(Bitmap stone,string setName,int slot,Color tint){
      Image emblem=GetSetIcon(setName);if(emblem==null)return;
      RectangleF sb=croquis3dSetBounds[slot];
      if(sb.Width<2f||sb.Height<2f)sb=new RectangleF(stone.Width*0.28f,stone.Height*0.28f,stone.Width*0.44f,stone.Height*0.44f);
      using(var crop=CropOpaque(emblem))
      using(var gem=TintSetFill(crop,tint,Math.Max(8,(int)Math.Max(sb.Width,sb.Height)))){
        float scale=Math.Min(sb.Width/gem.Width,sb.Height/gem.Height);
        float dw=gem.Width*scale,dh=gem.Height*scale;
        var dest=new RectangleF(sb.X+(sb.Width-dw)*0.5f,sb.Y+(sb.Height-dh)*0.5f,dw,dh);
        using(var g=Graphics.FromImage(stone)){
          g.SmoothingMode=SmoothingMode.AntiAlias;
          g.InterpolationMode=InterpolationMode.HighQualityBicubic;
          g.PixelOffsetMode=PixelOffsetMode.HighQuality;
          g.DrawImage(gem,dest);
        }
      }
    }
    static Bitmap InpaintOrangeGlyph(Bitmap src){
      var bmp=new Bitmap(src);
      int w=bmp.Width,h=bmp.Height,n=w*h;
      var px=new Color[n];var orange=new bool[n];
      for(int y=0;y<h;y++)
        for(int x=0;x<w;x++){
          int i=y*w+x;var c=bmp.GetPixel(x,y);px[i]=c;
          orange[i]=c.A>=80&&c.R>=185&&c.R>=c.G+18&&c.B<=100&&c.R>=c.B+40;
        }
      var q=new Queue<int>();
      for(int i=0;i<n;i++){
        if(!orange[i])continue;
        int x=i%w,y=i/w;
        bool edge=false;
        if(x>0&&!orange[i-1]&&px[i-1].A>=40)edge=true;
        else if(x+1<w&&!orange[i+1]&&px[i+1].A>=40)edge=true;
        else if(y>0&&!orange[i-w]&&px[i-w].A>=40)edge=true;
        else if(y+1<h&&!orange[i+w]&&px[i+w].A>=40)edge=true;
        if(edge)q.Enqueue(i);
      }
      while(q.Count>0){
        int i=q.Dequeue();if(!orange[i])continue;
        int x=i%w,y=i/w,sr=0,sg=0,sb=0,sa=0,cnt=0;
        if(x>0&&!orange[i-1]&&px[i-1].A>=40){sr+=px[i-1].R;sg+=px[i-1].G;sb+=px[i-1].B;sa+=px[i-1].A;cnt++;}
        if(x+1<w&&!orange[i+1]&&px[i+1].A>=40){sr+=px[i+1].R;sg+=px[i+1].G;sb+=px[i+1].B;sa+=px[i+1].A;cnt++;}
        if(y>0&&!orange[i-w]&&px[i-w].A>=40){sr+=px[i-w].R;sg+=px[i-w].G;sb+=px[i-w].B;sa+=px[i-w].A;cnt++;}
        if(y+1<h&&!orange[i+w]&&px[i+w].A>=40){sr+=px[i+w].R;sg+=px[i+w].G;sb+=px[i+w].B;sa+=px[i+w].A;cnt++;}
        if(cnt==0)continue;
        px[i]=Color.FromArgb(sa/cnt,sr/cnt,sg/cnt,sb/cnt);orange[i]=false;
        if(x>0&&orange[i-1])q.Enqueue(i-1);
        if(x+1<w&&orange[i+1])q.Enqueue(i+1);
        if(y>0&&orange[i-w])q.Enqueue(i-w);
        if(y+1<h&&orange[i+w])q.Enqueue(i+w);
      }
      for(int y=0;y<h;y++)
        for(int x=0;x<w;x++)bmp.SetPixel(x,y,px[y*w+x]);
      return bmp;
    }
    static void TintStonePreserveShade(Bitmap bmp,Color tint){
      int w=bmp.Width,h=bmp.Height;
      var rect=new Rectangle(0,0,w,h);
      var data=bmp.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
      int bytes=Math.Abs(data.Stride)*h;var buf=new byte[bytes];Marshal.Copy(data.Scan0,buf,0,bytes);
      int tr=tint.R,tg=tint.G,tb=tint.B;
      for(int i=0;i<buf.Length;i+=4){
        int a=buf[i+3];if(a<20)continue;
        int lum=(buf[i]*29+buf[i+1]*150+buf[i+2]*77)>>8;
        double shade=0.38+0.85*(lum/255.0);
        int r=(int)(tr*shade),g=(int)(tg*shade),b=(int)(tb*shade);
        if(r>255)r=255;if(g>255)g=255;if(b>255)b=255;
        buf[i]=(byte)b;buf[i+1]=(byte)g;buf[i+2]=(byte)r;
      }
      Marshal.Copy(buf,0,data.Scan0,bytes);bmp.UnlockBits(data);
    }
    private void PaintRuneIcon(DataGridViewCellPaintingEventArgs e, RuneRow r)
		{
			e.PaintBackground(e.CellBounds, true);
			e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
			e.Graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			Image preview = LoadCroquis3dPreview(r.Set, r.Slot, r.Grade);
			if (preview != null)
			{
				e.Graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
				int slotN=Math.Max(1,Math.Min(6,r.Slot));
				string artKey=(string.IsNullOrEmpty(r.Set)?"Despair":r.Set)+"|"+slotN;
				string bkey=artKey+"|"+CroquisQualityKey(r.Grade);
				RectangleF ob;
				if(!croquis3dOpaqueByKey.TryGetValue(bkey,out ob)||ob.Width<1f)
					if(!croquis3dOpaqueByKey.TryGetValue(artKey,out ob)||ob.Width<1f)ob=croquis3dOpaqueBounds[slotN];
				if(ob.Width<1f||ob.Height<1f)ob=new RectangleF(0,0,preview.Width,preview.Height);
				float box=Math.Min(e.CellBounds.Width-8,e.CellBounds.Height-4)*0.82f;
				float scale=Math.Min(box/preview.Width,box/preview.Height);
				float dw=preview.Width*scale,dh=preview.Height*scale;
				float ocx=ob.X+ob.Width*0.5f,ocy=ob.Y+ob.Height*0.5f;
				float cellCx=e.CellBounds.X+e.CellBounds.Width*0.5f;
				float cellCy=e.CellBounds.Y+e.CellBounds.Height*0.5f;
				var destPreview=new RectangleF(cellCx-ocx*scale,cellCy-ocy*scale,dw,dh);
				var clip=e.Graphics.Save();
				e.Graphics.SetClip(e.CellBounds);
				if(r.Ancient){
					e.Graphics.DrawImage(preview, destPreview);
					DrawAncientShine(e.Graphics,AncientPulse(artKey,preview),destPreview);
				}else e.Graphics.DrawImage(preview, destPreview);
				e.Graphics.Restore(clip);
				string sPreview = "+" + r.Level;
				RectangleF rectanglePreview = new RectangleF(e.CellBounds.X + 3, e.CellBounds.Bottom - 18, 28f, 15f);
				using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(235, 20, 24, 30)))
					e.Graphics.FillRectangle(brush2, rectanglePreview);
				using (Pen pen3 = new Pen(Color.FromArgb(105, 115, 125)))
					e.Graphics.DrawRectangle(pen3, rectanglePreview.X, rectanglePreview.Y, rectanglePreview.Width, rectanglePreview.Height);
				using (Font font = new Font("Segoe UI Semibold", 7.5f))
				using (SolidBrush brush2 = new SolidBrush(Color.White))
				{
					StringFormat format = new StringFormat();
					format.Alignment = StringAlignment.Center;
					format.LineAlignment = StringAlignment.Center;
					e.Graphics.DrawString(sPreview, font, brush2, rectanglePreview, format);
				}
				if (r.EquippedMasterId > 0)
				{
					Image image = WorldBossMonsterIcon(r.EquippedMasterId, "", "");
					if (image != null)
					{
						Rectangle rect = new Rectangle(e.CellBounds.Right - 27, e.CellBounds.Bottom - 27, 25, 25);
						e.Graphics.DrawImage(image, rect);
						using (Pen pen3 = new Pen(Color.FromArgb(235, 255, 255, 255), 1.5f))
							e.Graphics.DrawRectangle(pen3, rect);
					}
				}
				e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
				e.Handled = true;
				return;
			}
			int num = Math.Max(1, Math.Min(6, r.Slot));
			double[] array = new double[7] { 0.0, -90.0, -30.0, 30.0, 90.0, 150.0, -150.0 };
			double num2 = array[num] * Math.PI / 180.0;
			double num3 = 16.0 - 8.0 * Math.Cos(num2);
			double num4 = 16.0 - 8.0 * Math.Sin(num2);
			PointF[] array2 = new PointF[5];
			for (int i = 0; i < 5; i++)
			{
				double num5 = num2 + (double)(2 * i) * Math.PI / 5.0;
				double num6;
				switch (i)
				{
				default:
					num6 = 16.0;
					break;
				case 2:
				case 3:
					num6 = 9.92;
					break;
				case 0:
					num6 = 30.4;
					break;
				}
				double num7 = num6;
				array2[i] = new PointF((float)(num3 + num7 * Math.Cos(num5)), (float)(num4 + num7 * Math.Sin(num5)));
			}
			float num8 = 48f;
			float num9 = (float)e.CellBounds.X + ((float)e.CellBounds.Width - num8) / 2f;
			float num10 = (float)e.CellBounds.Y + ((float)e.CellBounds.Height - num8) / 2f;
			float num11 = num8 / 64f;
			PointF[] array3 = new PointF[5];
			for (int i = 0; i < 5; i++)
			{
				array3[i] = new PointF(num9 + (array2[i].X + 16f) * num11, num10 + (array2[i].Y + 16f) * num11);
			}
			Color color = ((r.Grade >= 5) ? Color.FromArgb(255, 126, 20) : ((r.Grade == 4) ? Color.FromArgb(178, 62, 255) : Color.FromArgb(52, 160, 255)));
			using (GraphicsPath graphicsPath = new GraphicsPath())
			{
				graphicsPath.AddPolygon(array3);
				if (r.Ancient)
				{
					using (Pen pen = new Pen(Color.FromArgb(70, color), 7f))
					{
						e.Graphics.DrawPath(pen, graphicsPath);
					}
					using (SolidBrush brush = new SolidBrush(Color.FromArgb(150, color)))
					{
						e.Graphics.FillPath(brush, graphicsPath);
					}
				}
				else
				{
					using (SolidBrush brush = new SolidBrush(Color.FromArgb(20, 24, 29)))
					{
						e.Graphics.FillPath(brush, graphicsPath);
					}
				}
				using (Region clip = e.Graphics.Clip)
				{
					e.Graphics.SetClip(graphicsPath, CombineMode.Intersect);
					Image setIcon = GetSetIcon(r.Set);
					if (setIcon != null)
					{
						e.Graphics.DrawImage(setIcon, new RectangleF(num9 + 16f * num11, num10 + 16f * num11, 32f * num11, 32f * num11));
					}
					e.Graphics.Clip = clip;
				}
				using (Pen pen2 = new Pen(color, 2f))
				{
					e.Graphics.DrawPath(pen2, graphicsPath);
				}
			}
			string s = "+" + r.Level;
			RectangleF rectangleF = new RectangleF(num9 + 30f, num10 + 34f, 28f, 15f);
			using (SolidBrush brush2 = new SolidBrush(Color.FromArgb(235, 20, 24, 30)))
			{
				e.Graphics.FillRectangle(brush2, rectangleF);
			}
			using (Pen pen3 = new Pen(Color.FromArgb(105, 115, 125)))
			{
				e.Graphics.DrawRectangle(pen3, rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
			}
			using (Font font = new Font("Segoe UI Semibold", 7.5f))
			{
				using (SolidBrush brush2 = new SolidBrush(Color.White))
				{
					StringFormat stringFormat = new StringFormat();
					stringFormat.Alignment = StringAlignment.Center;
					stringFormat.LineAlignment = StringAlignment.Center;
					StringFormat format = stringFormat;
					e.Graphics.DrawString(s, font, brush2, rectangleF, format);
				}
			}
			if (r.EquippedMasterId > 0)
			{
				Image image = WorldBossMonsterIcon(r.EquippedMasterId, "", "");
				if (image != null)
				{
					Rectangle rect = new Rectangle(e.CellBounds.X + 2, e.CellBounds.Y + 2, 25, 25);
					e.Graphics.DrawImage(image, rect);
					using (Pen pen3 = new Pen(Color.FromArgb(235, 255, 255, 255), 1.5f))
					{
						e.Graphics.DrawRectangle(pen3, rect);
					}
				}
			}
			e.Paint(e.CellBounds, DataGridViewPaintParts.Border);
			e.Handled = true;
    }
    // Les icones sources (pierres, sets) sont de grandes images redimensionnees a la
    // volee vers de tres petites tailles (badges de bouton, puces de menu). Sans ce
    // redimensionnement en haute qualite, GDI+ utilise un mode rapide par defaut qui
    // donne des icones crenelees/floues a petite taille. Utilise partout ou une icone
    // est reduite pour un badge ou une puce.
    static Bitmap HighQualityScale(Image source,int width,int height){var result=new Bitmap(width,height);using(var g=Graphics.FromImage(result)){g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.CompositingQuality=CompositingQuality.HighQuality;g.DrawImage(source,new Rectangle(0,0,width,height));}return result;}
    Image GetSetIcon(string setName){Image img;if(setIcons.TryGetValue(setName,out img))return img;string p=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","sets",setName.ToLowerInvariant()+".png");if(!File.Exists(p))return null;try{using(var fs=new FileStream(p,FileMode.Open,FileAccess.Read))using(var source=Image.FromStream(fs)){setIcons[setName]=new Bitmap(source);return setIcons[setName];}}catch{return null;}}
    static Color RuneLogoColor(int grade){return grade>=5?Color.FromArgb(255,126,20):grade==4?Color.FromArgb(239,135,251):grade==3?Color.FromArgb(72,196,236):grade==2?Color.FromArgb(64,210,110):Color.FromArgb(232,228,220);}
    static Bitmap TintSetFill(Image src,Color tint,int size){
      var bmp=HighQualityScale(src,size,size);
      var rect=new Rectangle(0,0,size,size);
      var data=bmp.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
      int bytes=Math.Abs(data.Stride)*size;var px=new byte[bytes];Marshal.Copy(data.Scan0,px,0,bytes);
      int tr=tint.R,tg=tint.G,tb=tint.B;
      for(int i=0;i<px.Length;i+=4){int a=px[i+3];if(a==0)continue;int lum=(px[i]*29+px[i+1]*150+px[i+2]*77)>>8;if(lum<125)continue;int r=tr*lum/210,g=tg*lum/210,b=tb*lum/210;if(r>255)r=255;if(g>255)g=255;if(b>255)b=255;px[i]=(byte)b;px[i+1]=(byte)g;px[i+2]=(byte)r;}
      Marshal.Copy(px,0,data.Scan0,bytes);bmp.UnlockBits(data);return bmp;
    }
    static GraphicsPath RoundRectPath(Rectangle r,float radius){return RoundRectPath(new RectangleF(r.X,r.Y,r.Width,r.Height),radius);}
    static GraphicsPath RoundRectPath(RectangleF r,float radius){
      var p=new GraphicsPath();float d=Math.Max(1f,Math.Min(radius*2f,Math.Min(r.Width,r.Height)));
      p.AddArc(r.X,r.Y,d,d,180,90);p.AddArc(r.Right-d,r.Y,d,d,270,90);p.AddArc(r.Right-d,r.Bottom-d,d,d,0,90);p.AddArc(r.X,r.Bottom-d,d,d,90,90);p.CloseFigure();return p;
    }
    static Color RuneStoneColor(bool ancient){return ancient?Color.FromArgb(214,168,78):Color.FromArgb(204,160,92);}
    static Bitmap PaintSlot(Image src,Color tint,int size,bool keepShade){return RecolorSlot(src,tint,size,size,keepShade);}
    static Bitmap RecolorSlot(Image src,Color tint,int w,int h,bool keepShade){
      var bmp=HighQualityScale(src,w,h);
      var rect=new Rectangle(0,0,w,h);
      var data=bmp.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
      int bytes=Math.Abs(data.Stride)*h;var px=new byte[bytes];Marshal.Copy(data.Scan0,px,0,bytes);
      int tr=tint.R,tg=tint.G,tb=tint.B;
      for(int i=0;i<px.Length;i+=4){int a=px[i+3];if(a==0)continue;if(!keepShade){px[i]=(byte)tb;px[i+1]=(byte)tg;px[i+2]=(byte)tr;continue;}int lum=(px[i]*29+px[i+1]*150+px[i+2]*77)>>8;double shade=0.52+0.70*(lum/255.0);int r=(int)(tr*shade),g=(int)(tg*shade),b=(int)(tb*shade);if(r>255)r=255;if(g>255)g=255;if(b>255)b=255;px[i]=(byte)b;px[i+1]=(byte)g;px[i+2]=(byte)r;}
      Marshal.Copy(px,0,data.Scan0,bytes);bmp.UnlockBits(data);return bmp;
    }
    static void ApplyAlphaMask(Bitmap dest,Bitmap mask){
      int w=dest.Width,h=dest.Height;if(mask.Width!=w||mask.Height!=h)return;
      var dr=dest.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
      var mr=mask.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
      int db=Math.Abs(dr.Stride)*h,mb=Math.Abs(mr.Stride)*h;var dp=new byte[db];var mp=new byte[mb];
      Marshal.Copy(dr.Scan0,dp,0,db);Marshal.Copy(mr.Scan0,mp,0,mb);
      for(int y=0;y<h;y++){int di=y*dr.Stride,mi=y*mr.Stride;for(int x=0;x<w;x++){int a=dp[di+x*4+3]*mp[mi+x*4+3]/255;dp[di+x*4+3]=(byte)a;}}
      Marshal.Copy(dp,0,dr.Scan0,db);dest.UnlockBits(dr);mask.UnlockBits(mr);
    }
    static Bitmap TintEmblem(Image src,Color tint,int size){
      var bmp=HighQualityScale(src,size,size);
      var rect=new Rectangle(0,0,size,size);
      var data=bmp.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
      int bytes=Math.Abs(data.Stride)*size;var px=new byte[bytes];Marshal.Copy(data.Scan0,px,0,bytes);
      int tr=tint.R,tg=tint.G,tb=tint.B;
      for(int i=0;i<px.Length;i+=4){int a=px[i+3];if(a==0)continue;int lum=(px[i]*29+px[i+1]*150+px[i+2]*77)>>8;if(lum<70){px[i]=12;px[i+1]=10;px[i+2]=8;continue;}int r=tr*lum/190,g=tg*lum/190,b=tb*lum/190;if(r>255)r=255;if(g>255)g=255;if(b>255)b=255;px[i]=(byte)b;px[i+1]=(byte)g;px[i+2]=(byte)r;}
      Marshal.Copy(px,0,data.Scan0,bytes);bmp.UnlockBits(data);return bmp;
    }
    static Rectangle OpaqueBounds(Bitmap bmp,int minA){
      int w=bmp.Width,h=bmp.Height;var rect=new Rectangle(0,0,w,h);
      var data=bmp.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
      int stride=data.Stride,bytes=Math.Abs(stride)*h;var px=new byte[bytes];Marshal.Copy(data.Scan0,px,0,bytes);bmp.UnlockBits(data);
      int minX=w,minY=h,maxX=-1,maxY=-1;
      for(int y=0;y<h;y++){int row=y*stride;for(int x=0;x<w;x++){if(px[row+x*4+3]<minA)continue;if(x<minX)minX=x;if(y<minY)minY=y;if(x>maxX)maxX=x;if(y>maxY)maxY=y;}}
      if(maxX<0)return rect;return new Rectangle(minX,minY,maxX-minX+1,maxY-minY+1);
    }
    static Bitmap CropOpaque(Image src){
      var bmp=src as Bitmap;bool own=false;if(bmp==null){bmp=new Bitmap(src);own=true;}
      var r=OpaqueBounds(bmp,16);r.Inflate(1,1);r=Rectangle.Intersect(r,new Rectangle(0,0,bmp.Width,bmp.Height));if(r.Width<1||r.Height<1)r=new Rectangle(0,0,bmp.Width,bmp.Height);
      var crop=new Bitmap(r.Width,r.Height,PixelFormat.Format32bppArgb);
      using(var g=Graphics.FromImage(crop)){g.CompositingMode=CompositingMode.SourceOver;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(bmp,new Rectangle(0,0,r.Width,r.Height),r,GraphicsUnit.Pixel);}
      if(own)bmp.Dispose();return crop;
    }
    static PointF[] StarPoly(float cx,float cy,float r,float inner){
      var pts=new PointF[10];
      for(int i=0;i<10;i++){double a=-Math.PI/2+i*Math.PI/5;float rad=i%2==0?r:r*inner;pts[i]=new PointF(cx+(float)(rad*Math.Cos(a)),cy+(float)(rad*Math.Sin(a)));}
      return pts;
    }
    static void DrawGoldStar(Graphics g,float cx,float cy,float r){
      var pts=StarPoly(cx,cy,r,0.39f);
      PointF core=new PointF(cx,cy+r*0.04f);
      for(int i=0;i<5;i++){
        int t=i*2;
        var hi=new PointF[]{core,pts[(t+9)%10],pts[t]};
        var lo=new PointF[]{core,pts[t],pts[(t+1)%10]};
        Color cHi=i==0?Color.FromArgb(255,244,150):i==1||i==4?Color.FromArgb(255,214,70):Color.FromArgb(255,186,36);
        Color cLo=i==0?Color.FromArgb(255,200,48):Color.FromArgb(168,108,12);
        using(var b1=new SolidBrush(cHi))g.FillPolygon(b1,hi);
        using(var b2=new SolidBrush(cLo))g.FillPolygon(b2,lo);
      }
      using(var path=new GraphicsPath()){
        path.AddPolygon(pts);
        using(var p=new Pen(Color.FromArgb(62,36,6),Math.Max(1.4f,r*0.09f)){LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawPath(p,path);
      }
    }
    static void DrawGoldStars(Graphics g,int count,int width,int bandH,int y0){
      count=Math.Max(1,Math.Min(6,count));float r=bandH*(count>=6?0.42f:0.47f);float spread=count>=5?width*0.88f:width*0.80f;float left=(width-spread)/2f;
      for(int i=0;i<count;i++){float t=count==1?0.5f:i/(float)(count-1);float cx=left+t*spread;float cy=y0+bandH*0.55f-(float)Math.Sin(t*Math.PI)*bandH*0.05f;DrawGoldStar(g,cx,cy,r);}
    }
    Image LoadPngFile(string path){if(string.IsNullOrEmpty(path)||!File.Exists(path))return null;try{using(var fs=new FileStream(path,FileMode.Open,FileAccess.Read,FileShare.Read))using(var img=Image.FromStream(fs))return new Bitmap(img);}catch{return null;}}
    static PointF[] SlotPentagon(int slot,float cx,float cy,float r){
      double[] tip={0,-90,180,140,90,40,0};
      double c=tip[Math.Max(1,Math.Min(6,slot))]*Math.PI/180.0;
      var pts=new PointF[5];float ax=0,ay=0;
      for(int i=0;i<5;i++){double t=c+2*i*Math.PI/5,n=i==0?r:((i==2||i==3)?r*0.42:r*0.68);pts[i]=new PointF(cx+(float)(n*Math.Cos(t)),cy+(float)(n*Math.Sin(t)));ax+=pts[i].X;ay+=pts[i].Y;}
      ax/=5f;ay/=5f;for(int i=0;i<5;i++)pts[i]=new PointF(pts[i].X-ax+cx,pts[i].Y-ay+cy);
      return pts;
    }
    static Bitmap PaintStoneFromMask(Image slotImg,int w,int h,bool ancient){
      var mask=HighQualityScale(slotImg,w,h);
      var bmp=new Bitmap(w,h,PixelFormat.Format32bppArgb);
      Color light=ancient?Color.FromArgb(198,162,96):Color.FromArgb(176,166,156);
      Color dark=ancient?Color.FromArgb(86,64,30):Color.FromArgb(58,52,48);
      var rect=new Rectangle(0,0,w,h);
      using(var g=Graphics.FromImage(bmp)){
        g.SmoothingMode=SmoothingMode.AntiAlias;g.PixelOffsetMode=PixelOffsetMode.HighQuality;
        using(var br=new LinearGradientBrush(rect,light,dark,128f))g.FillRectangle(br,rect);
        using(var p=new Pen(Color.FromArgb(110,22,18,16),Math.Max(1.4f,w/42f))){
          g.DrawLine(p,w*0.20f,h*0.30f,w*0.52f,h*0.68f);
          g.DrawLine(p,w*0.58f,h*0.20f,w*0.82f,h*0.52f);
          g.DrawLine(p,w*0.28f,h*0.72f,w*0.64f,h*0.86f);
        }
        using(var p=new Pen(Color.FromArgb(50,230,220,210),Math.Max(1f,w/70f))){
          g.DrawLine(p,w*0.18f,h*0.22f,w*0.42f,h*0.18f);
          g.DrawLine(p,w*0.70f,h*0.28f,w*0.84f,h*0.48f);
        }
      }
      var mr=mask.LockBits(rect,ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
      var brp=bmp.LockBits(rect,ImageLockMode.ReadWrite,PixelFormat.Format32bppArgb);
      int mb=Math.Abs(mr.Stride)*h,bb=Math.Abs(brp.Stride)*h;var mp=new byte[mb];var bp=new byte[bb];
      Marshal.Copy(mr.Scan0,mp,0,mb);Marshal.Copy(brp.Scan0,bp,0,bb);
      for(int y=0;y<h;y++){
        int mi=y*mr.Stride,di=y*brp.Stride;
        for(int x=0;x<w;x++){
          int a=mp[mi+x*4+3];
          if(a<8){bp[di+x*4+3]=0;continue;}
          int lum=(mp[mi+x*4]*29+mp[mi+x*4+1]*150+mp[mi+x*4+2]*77)>>8;
          double shade=0.46+0.92*(lum/255.0);
          int n=((x*131)+(y*17))&15;shade*=0.96+(n-7)*0.006;
          int b=(int)(bp[di+x*4]*shade),gg=(int)(bp[di+x*4+1]*shade),rr=(int)(bp[di+x*4+2]*shade);
          if(rr>255)rr=255;if(gg>255)gg=255;if(b>255)b=255;
          if(rr<0)rr=0;if(gg<0)gg=0;if(b<0)b=0;
          bp[di+x*4]=(byte)b;bp[di+x*4+1]=(byte)gg;bp[di+x*4+2]=(byte)rr;bp[di+x*4+3]=(byte)a;
        }
      }
      Marshal.Copy(bp,0,brp.Scan0,bb);bmp.UnlockBits(brp);mask.UnlockBits(mr);mask.Dispose();
      return bmp;
    }
    static void DrawQualityGlow(Graphics g,Image slotImg,Color quality,int sx,int sy,int sw,int sh){
      int[] pad={10,6,3};float[] alpha={0.22f,0.38f,0.70f};
      for(int i=0;i<pad.Length;i++){
        int p=pad[i];
        using(var glow=RecolorSlot(slotImg,quality,sw+p*2,sh+p*2,false)){
          var cm=new ColorMatrix();cm.Matrix33=alpha[i];
          using(var ia=new ImageAttributes()){ia.SetColorMatrix(cm);g.DrawImage(glow,new Rectangle(sx-p,sy-p,sw+p*2,sh+p*2),0,0,sw+p*2,sh+p*2,GraphicsUnit.Pixel,ia);}
        }
      }
    }
    static void DrawPentagonStone(Graphics g,int slot,Color quality,bool ancient,int tile,out float cx,out float cy,out float radius,out GraphicsPath path){
      cx=tile/2f;cy=tile*0.58f;radius=tile*0.30f;
      var pts=SlotPentagon(slot,cx,cy,radius);
      path=new GraphicsPath();path.AddPolygon(pts);
      if(ancient){
        using(var glow=new Pen(Color.FromArgb(55,Color.White),tile*0.055f){LineJoin=LineJoin.Round})g.DrawPath(glow,path);
        using(var glow2=new Pen(Color.FromArgb(150,Color.White),tile*0.028f){LineJoin=LineJoin.Round})g.DrawPath(glow2,path);
      }
      Color light=ancient?Color.FromArgb(198,162,96):Color.FromArgb(168,158,148);
      Color dark=ancient?Color.FromArgb(78,56,26):Color.FromArgb(52,46,42);
      using(var pgb=new PathGradientBrush(path)){
        pgb.CenterColor=light;pgb.SurroundColors=new[]{dark};
        pgb.CenterPoint=new PointF(cx-radius*0.18f,cy-radius*0.22f);
        g.FillPath(pgb,path);
      }
      using(var p=new Pen(Color.FromArgb(90,18,16,14),1.8f)){
        g.SetClip(path);
        g.DrawLine(p,cx-radius*0.35f,cy-radius*0.1f,cx+radius*0.05f,cy+radius*0.35f);
        g.DrawLine(p,cx+radius*0.1f,cy-radius*0.28f,cx+radius*0.38f,cy+radius*0.08f);
        g.ResetClip();
      }
      using(var pen=new Pen(ancient?Color.White:Color.FromArgb(68,64,60),Math.Max(2.6f,tile*0.014f)){LineJoin=LineJoin.Round})g.DrawPath(pen,path);
    }
    Image GetGameRuneIcon(string setName,int slot,int grade,bool ancient,int stars,int level){
      slot=Math.Max(1,Math.Min(6,slot));if(stars<1)stars=1;if(stars>6)stars=6;if(level<0)level=0;
      string setKey=(setName??"").Trim().ToLowerInvariant();
      string key="normal-ancient-white-v1|"+setKey+"|"+slot+"|"+grade+"|"+(ancient?"1":"0")+"|"+stars+"|"+level;Image cached;if(runeIcons.TryGetValue(key,out cached))return cached;
      const int TILE=256;
      Color quality=RuneLogoColor(grade);
      var canvas=new Bitmap(TILE,TILE,PixelFormat.Format32bppArgb);
      using(var g=Graphics.FromImage(canvas)){
        g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.CompositingQuality=CompositingQuality.HighQuality;g.Clear(Color.Transparent);
        DrawGoldStars(g,stars,TILE-6,52,1);
        Image slotImg=GetSlotLayer(slot);
        if(slotImg!=null){
          int stoneTop=68,stoneBox=TILE-stoneTop-4;
          float sc=Math.Min((TILE*0.82f)/slotImg.Width,stoneBox/(float)slotImg.Height);
          int sw=Math.Max(1,(int)Math.Round(slotImg.Width*sc)),sh=Math.Max(1,(int)Math.Round(slotImg.Height*sc));
          int sx=(TILE-sw)/2,sy=stoneTop+(stoneBox-sh)/2-6;
          if(ancient)DrawQualityGlow(g,slotImg,Color.White,sx,sy,sw,sh);
          using(var body=PaintStoneFromMask(slotImg,sw,sh,ancient))g.DrawImage(body,sx,sy,sw,sh);
          Image emblem=GetSetIcon(setKey);
          if(emblem!=null){
            int es=(int)(Math.Min(sw,sh)*0.56f);if(es<16)es=16;
            int ex=sx+(sw-es)/2,ey=sy+(sh-es)/2;
            using(var cropped=CropOpaque(emblem))
            using(var shade=TintEmblem(cropped,Color.FromArgb(18,14,12),es))
            using(var logo=TintEmblem(cropped,quality,es))
            using(var layer=new Bitmap(sw,sh,PixelFormat.Format32bppArgb))
            using(var mask=HighQualityScale(slotImg,sw,sh)){
              using(var lg=Graphics.FromImage(layer)){
                lg.Clear(Color.Transparent);lg.InterpolationMode=InterpolationMode.HighQualityBicubic;
                lg.DrawImage(shade,ex-sx+2,ey-sy+3,es,es);
                lg.DrawImage(logo,ex-sx,ey-sy,es,es);
              }
              ApplyAlphaMask(layer,mask);g.DrawImage(layer,sx,sy);
            }
          }
        }else{
          float cx,cy,radius;GraphicsPath path;
          DrawPentagonStone(g,slot,quality,ancient,TILE,out cx,out cy,out radius,out path);
          using(path){
            Image emblem=GetSetIcon(setKey);
            if(emblem!=null){
              int es=(int)(radius*1.12f);var st=g.Save();g.SetClip(path);
              using(var cropped=CropOpaque(emblem))
              using(var logo=TintEmblem(cropped,quality,es))
                g.DrawImage(logo,(int)(cx-es/2f),(int)(cy-es/2f),es,es);
              g.Restore(st);
            }
          }
        }
        DrawLevelPlus(g,level,quality,TILE);
      }
      runeIcons[key]=canvas;return canvas;
    }
    Image GetSlotLayer(int slot){
      slot=Math.Max(1,Math.Min(6,slot));
      if(slotLayers[slot]!=null)return slotLayers[slot];
      string[] roots={AppDomain.CurrentDomain.BaseDirectory,Application.StartupPath,Path.GetDirectoryName(Application.ExecutablePath)};
      for(int i=0;i<roots.Length;i++){
        if(string.IsNullOrEmpty(roots[i]))continue;
        var img=LoadPngFile(Path.Combine(roots[i],"assets","runes","_layers","slot"+slot+".png"));
        if(img!=null){slotLayers[slot]=img;return img;}
      }
      return null;
    }
    static void DrawLevelPlus(Graphics g,int level,Color quality,int tile){
      string lvl="+"+level;
      using(var gp=new GraphicsPath()){
        float em=tile*0.155f;
        var fmt=new StringFormat(StringFormat.GenericTypographic);
        try{gp.AddString(lvl,new FontFamily("Segoe UI"),(int)FontStyle.Bold,em,PointF.Empty,fmt);}
        catch{gp.AddString(lvl,FontFamily.GenericSansSerif,(int)FontStyle.Bold,em,PointF.Empty,fmt);}
        var b=gp.GetBounds();
        float x=tile*0.015f-b.X,y=tile-b.Height-tile*0.02f-b.Y;
        if(x+b.Width>tile-3)x=tile-3-b.Width-b.X;
        if(y<tile*0.62f)y=tile*0.62f-b.Y;
        using(var m=new Matrix()){m.Translate(x,y);gp.Transform(m);}
        float ow=Math.Max(3.8f,tile*0.018f);
        using(var q=new Pen(quality,ow+1.4f){LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawPath(q,gp);
        using(var k=new Pen(Color.FromArgb(18,14,10),Math.Max(1.6f,ow*0.32f)){LineJoin=LineJoin.Round})g.DrawPath(k,gp);
        using(var w=new SolidBrush(Color.FromArgb(248,250,255)))g.FillPath(w,gp);
      }
    }
    internal Image RtaSetIcon(string setName){return GetSetIcon(setName);}
    void GridMouseDown(object sender,DataGridViewCellMouseEventArgs e){
      if(e.Button!=MouseButtons.Right||e.RowIndex<0||viewMode!="upgrade")return;grid.CurrentCell=grid.Rows[e.RowIndex].Cells[Math.Max(0,e.ColumnIndex)];var r=grid.Rows[e.RowIndex].DataBoundItem as RuneRow;if(r==null)return;
      var menu=new ContextMenuStrip{BackColor=Panel,ForeColor=Color.White,ShowImageMargin=false,Font=new Font("Segoe UI Semibold",10)};var item=new ToolStripMenuItem(Loc.T("hide_upgrade")){ForeColor=Color.FromArgb(255,120,120)};item.Click+=(s,a)=>{hiddenUpgradeIds.Add(r.Id);RefreshUpgradeBadge();RefreshGrid();status.Text=Loc.T("hide_upgrade_done");};menu.Items.Add(item);menu.Show(grid,grid.PointToClient(Cursor.Position));
    }
    void GridKeyDown(object sender,KeyEventArgs e){if(e.Control&&e.KeyCode==Keys.J){e.SuppressKeyPress=true;e.Handled=true;OpenCraftMenu();}}
    void OpenCraftMenu(){if(grid.CurrentCell==null||grid.CurrentCell.RowIndex<0)return;var r=grid.Rows[grid.CurrentCell.RowIndex].DataBoundItem as RuneRow;if(r==null)return;string type="",stat="";int col=grid.CurrentCell.ColumnIndex;if(col>=4&&col<=7){int i=col-4;if(i<r.Subs.Count){type="Meule";stat=r.Subs[i].Stat;}}else if(col==10&&r.RecommendTarget.Length>0){type="Gemme";stat=r.RecommendTarget;}if(type.Length==0||stat.Length==0){status.Text="Ctrl+J : sélectionnez une statistique ou la recommandation de gemme.";return;}if(type=="Meule"&&!IsGrindable(stat)){status.Text=stat+" ne peut pas être meulée.";return;}var menu=new ContextMenuStrip{BackColor=Panel,ForeColor=Color.White,ShowImageMargin=false,Font=new Font("Segoe UI Semibold",10)};menu.Items.Add(new ToolStripLabel(type.ToUpper()+"  •  "+r.Set+"  •  "+stat){ForeColor=Cyan});menu.Items.Add(new ToolStripSeparator());AddCraftItem(menu,r,type,stat,false,4,"SET VIOLET",Color.FromArgb(151,49,230));AddCraftItem(menu,r,type,stat,false,5,"SET LEGENDAIRE",Color.FromArgb(255,112,18));if(!r.Ancient){menu.Items.Add(new ToolStripSeparator());AddCraftItem(menu,r,type,stat,true,4,"IMM. VIOLET",Color.FromArgb(151,49,230));AddCraftItem(menu,r,type,stat,true,5,"IMM. LEGENDAIRE",Color.FromArgb(255,112,18));}Rectangle cell=grid.GetCellDisplayRectangle(col,grid.CurrentCell.RowIndex,true);menu.Show(grid,new Point(cell.Left,cell.Bottom));}
    void AddCraftItem(ContextMenuStrip menu,RuneRow r,string type,string stat,bool imm,int grade,string label,Color color){int count=RuneEngine.StockCountDetail(r,type,stat,imm,grade);var item=new ToolStripMenuItem(label+"  x"+count){ForeColor=color,Enabled=count>0};item.Click+=(s,e)=>{RuneEngine.ClearStock(r,type,stat,imm,grade);if(type=="Gemme")RuneEngine.Calculate(all);RefreshAfterStockChange();int remaining=RuneEngine.StockCount(r,type,stat);status.Text=label+" "+type.ToLowerInvariant()+" "+stat+" : stock vidé — restant utilisable x"+remaining+".";};menu.Items.Add(item);}
    void RefreshAfterStockChange(){RefreshUpgradeBadge();if(viewMode=="upgrade")RefreshGrid();else{var position=grid.FirstDisplayedScrollingRowIndex;var source=grid.DataSource;grid.DataSource=null;grid.DataSource=source;if(position>=0&&position<grid.Rows.Count)try{grid.FirstDisplayedScrollingRowIndex=position;}catch{}}grid.Invalidate(true);grid.Refresh();Application.DoEvents();}
    void ShowSkillUps(){
      if(string.IsNullOrWhiteSpace(currentFile)||!File.Exists(currentFile)){MessageBox.Show(Loc.T("import_first"),Loc.T("skill_title"),MessageBoxButtons.OK,MessageBoxIcon.Information);return;}
      string catalog=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","monsters","catalog.json");if(!File.Exists(catalog)){MessageBox.Show(Loc.T("catalog_missing"),Loc.T("skill_title"),MessageBoxButtons.OK,MessageBoxIcon.Error);return;}
      List<SkillUpGroup> groups;try{UseWaitCursor=true;if(skillGroups.Count==0)RefreshSkillUpSummary();groups=skillGroups.ToList();}catch(Exception ex){MessageBox.Show(Loc.T("skill_analyse_fail",ex.Message),Loc.T("skill_title"),MessageBoxButtons.OK,MessageBoxIcon.Error);return;}finally{UseWaitCursor=false;}
      var f=new Form{Text=Loc.T("skill_title"),Icon=Icon,BackColor=Bg,ForeColor=Color.White,Size=new Size(1280,820),MinimumSize=new Size(950,620),StartPosition=FormStartPosition.CenterParent};
      var head=new Panel{Dock=DockStyle.Top,Height=128,BackColor=Panel,Padding=new Padding(22,14,22,12)};f.Controls.Add(head);head.Controls.Add(new Label{Text="SKILL-UP 4★",ForeColor=Color.FromArgb(244,96,178),Font=new Font("Segoe UI Semibold",22),AutoSize=true,Location=new Point(20,12)});
      var summary=new Label{ForeColor=Color.Gainsboro,AutoSize=true,Location=new Point(230,25),Font=new Font("Segoe UI",11)};head.Controls.Add(summary);
      bool stockIncluded=RuneEngine.SkillUpStockIncluded(currentFile);var info=new Label{Text=stockIncluded?Loc.T("skill_protected"):Loc.T("skill_sealed"),ForeColor=stockIncluded?Color.FromArgb(175,188,203):Color.FromArgb(255,184,85),AutoSize=true,Location=new Point(22,92),Font=new Font("Segoe UI",9)};head.Controls.Add(info);
      var searchHint=new Label{Text=Loc.T("skill_search"),ForeColor=Color.FromArgb(140,155,170),AutoSize=true,Location=new Point(650,8),Font=new Font("Segoe UI",8)};head.Controls.Add(searchHint);
      var searchSkill=new TextBox{BackColor=Bg,ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle,Location=new Point(650,24),Size=new Size(220,30),Font=new Font("Segoe UI",11)};head.Controls.Add(searchSkill);
      bool familyMode=false;
      var familyButton=Button(Loc.T("skill_families"),880,22,175,Color.FromArgb(70,110,150));head.Controls.Add(familyButton);
      var hiddenButton=Button(Loc.T("skill_hidden_show"),1065,22,190,Color.FromArgb(82,70,135));head.Controls.Add(hiddenButton);
      var flow=new BufferedFlow{Dock=DockStyle.Fill,BackColor=Bg,AutoScroll=true,Padding=new Padding(14),WrapContents=true};f.Controls.Add(flow);flow.BringToFront();
      var familyTip=new ToolTip();var familyCards=new List<Tuple<Control,SkillUpFamily,string>>();int familyBuiltRevision=-1;bool showingFamilies=false;
      var emptyFam=new Label{Text=Loc.T("skill_gap_none"),ForeColor=Color.Silver,AutoSize=true,Font=new Font("Segoe UI",13),Margin=new Padding(20),Visible=false};
      Action buildFamilyCards=()=>{if(familyBuiltRevision==skillGroupsRevision&&familyCards.Count==skillFamilies.Count)return;foreach(var old in familyCards){if(!old.Item1.IsDisposed)old.Item1.Dispose();}familyCards.Clear();showingFamilies=false;foreach(var fam in skillFamilies){var card=SkillFamilyCard(fam,familyTip);familyCards.Add(Tuple.Create(card,fam,string.IsNullOrEmpty(fam.Search)?(fam.Family+" "+string.Join(" ",fam.Elements.Select(e=>e.Name+" "+e.Element))):fam.Search));}familyBuiltRevision=skillGroupsRevision;};
      Action render=null;render=delegate{
        string find=searchSkill.Text.Trim();
        familyButton.Text=familyMode?Loc.T("skill_fodder"):Loc.T("skill_families");
        familyButton.BackColor=familyMode?Color.FromArgb(183,71,145):Color.FromArgb(70,110,150);
        if(familyMode){
          hiddenButton.Visible=false;
          buildFamilyCards();
          flow.SuspendLayout();
          if(!showingFamilies){flow.Controls.Clear();foreach(var x in familyCards)flow.Controls.Add(x.Item1);flow.Controls.Add(emptyFam);showingFamilies=true;}
          int vis=0,gaps=0;foreach(var x in familyCards){bool show=find.Length==0||x.Item3.IndexOf(find,StringComparison.OrdinalIgnoreCase)>=0;x.Item1.Visible=show;if(show){vis++;gaps+=x.Item2.Gaps;}}
          emptyFam.Visible=vis==0;flow.ResumeLayout();
          summary.Text=string.Format(Loc.T("skill_family_sum"),vis.ToString("N0"),gaps.ToString("N0"));
          info.Text=Loc.T("skill_family_hint");info.ForeColor=Color.FromArgb(175,188,203);
        }else{
          hiddenButton.Visible=true;showingFamilies=false;
          flow.SuspendLayout();flow.Controls.Clear();
          var visible=groups.Where(x=>(showHiddenSkillTargets||!hiddenSkillTargetIds.Contains(x.Target.MasterId))&&(find.Length==0||(x.Target.Name+" "+x.Target.Family).IndexOf(find,StringComparison.OrdinalIgnoreCase)>=0)).ToList();
          foreach(var group in visible)flow.Controls.Add(SkillCard(group,hiddenSkillTargetIds.Contains(group.Target.MasterId),render));
          int hidden=groups.Count(x=>hiddenSkillTargetIds.Contains(x.Target.MasterId));
          summary.Text=string.Format(Loc.T("skill_summary"),visible.Count.ToString("N0"),visible.Sum(x=>x.UsableUpgrades).ToString("N0"),hidden.ToString("N0"),hidden>1?"s":"");
          hiddenButton.Text=showHiddenSkillTargets?Loc.T("skill_hidden_hide"):Loc.T("skill_hidden_show");
          info.Text=stockIncluded?Loc.T("skill_protected"):Loc.T("skill_sealed");
          info.ForeColor=stockIncluded?Color.FromArgb(175,188,203):Color.FromArgb(255,184,85);
          if(flow.Controls.Count==0)flow.Controls.Add(new Label{Text=hidden>0&&!showHiddenSkillTargets?Loc.T("skill_empty_hidden"):Loc.T("skill_empty_none"),ForeColor=Color.Silver,AutoSize=true,Font=new Font("Segoe UI",13),Margin=new Padding(20)});
          flow.ResumeLayout();
        }
      };
      hiddenButton.Click+=(s,e)=>{showHiddenSkillTargets=!showHiddenSkillTargets;render();};
      familyButton.Click+=(s,e)=>{familyMode=!familyMode;if(familyMode)UseWaitCursor=true;render();UseWaitCursor=false;};
      var searchDebounce=new Timer{Interval=120};searchDebounce.Tick+=(s,e)=>{searchDebounce.Stop();render();};
      searchSkill.TextChanged+=(s,e)=>{searchDebounce.Stop();searchDebounce.Start();};render();
      int seenSkillRevision=skillGroupsRevision;var liveSkillTimer=new Timer{Interval=500};liveSkillTimer.Tick+=(s,e)=>{if(seenSkillRevision==skillGroupsRevision)return;seenSkillRevision=skillGroupsRevision;groups=skillGroups.ToList();stockIncluded=RuneEngine.SkillUpStockIncluded(currentFile);render();};f.Shown+=(s,e)=>liveSkillTimer.Start();f.FormClosed+=(s,e)=>{searchDebounce.Stop();searchDebounce.Dispose();liveSkillTimer.Stop();liveSkillTimer.Dispose();};f.ShowDialog(this);
    }
    Control SkillCard(SkillUpGroup group,bool hidden,Action refresh){
      var card=new Panel{Width=575,Height=190,BackColor=hidden?Color.FromArgb(34,27,39):Color.FromArgb(13,23,36),Margin=new Padding(10),Padding=new Padding(12)};var accent=new Panel{BackColor=hidden?Color.FromArgb(115,90,125):Color.FromArgb(210,70,155),Dock=DockStyle.Left,Width=4};card.Controls.Add(accent);
      var portrait=MonsterPicture(group.Target,82);portrait.Location=new Point(18,18);card.Controls.Add(portrait);card.Controls.Add(new Label{Text=group.Target.Name,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",15),AutoSize=true,Location=new Point(112,18)});card.Controls.Add(new Label{Text=group.Target.Family+"  •  "+ElementName(group.Target.Element)+"  •  "+Loc.T("skill_native",group.Target.NaturalStars),ForeColor=Color.FromArgb(166,182,200),Font=new Font("Segoe UI",9),AutoSize=true,Location=new Point(113,50)});card.Controls.Add(new Label{Text=Loc.T("skill_usable",group.UsableUpgrades,group.UsableUpgrades>1?"s":""),ForeColor=group.UsableUpgrades>0?Color.FromArgb(80,225,160):Color.FromArgb(150,163,180),Font=new Font("Segoe UI Semibold",11),AutoSize=true,Location=new Point(113,76)});
      card.Controls.Add(new Label{Text=hidden?Loc.T("skill_hidden_card"):group.Fodders.Count>0?Loc.T("skill_fodder_ok"):Loc.T("skill_fodder_none"),ForeColor=hidden?Color.FromArgb(205,150,220):group.Fodders.Count>0?Color.FromArgb(20,184,210):Color.FromArgb(150,163,180),Font=new Font("Segoe UI Semibold",9),AutoSize=true,Location=new Point(18,113)});int x=170;var stacks=group.Fodders.GroupBy(v=>v.MasterId).Select(v=>new{Monster=v.First(),Count=v.Count()}).OrderByDescending(v=>v.Count).ThenBy(v=>v.Monster.Name).ToList();foreach(var stack in stacks.Take(6)){var fodder=stack.Monster;var pic=MonsterPicture(fodder,48);pic.Location=new Point(x,106);pic.Tag=fodder;var tip=new ToolTip();tip.SetToolTip(pic,fodder.Name+" • "+ElementName(fodder.Element)+" • "+Loc.T("skill_native",fodder.NaturalStars)+" • ×"+stack.Count);card.Controls.Add(pic);card.Controls.Add(new Label{Text="×"+stack.Count,ForeColor=Color.FromArgb(238,214,166),BackColor=Color.Transparent,Font=new Font("Segoe UI Semibold",10),TextAlign=ContentAlignment.MiddleCenter,Location=new Point(x,155),Size=new Size(48,22)});x+=62;}if(stacks.Count>6)card.Controls.Add(new Label{Text="+"+(stacks.Count-6)+" types",ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10),AutoSize=true,Location=new Point(x+2,128)});var menu=new ContextMenuStrip{BackColor=Panel,ForeColor=Color.White,ShowImageMargin=false,Font=new Font("Segoe UI Semibold",10)};var toggle=new ToolStripMenuItem(hidden?Loc.T("skill_unhide"):Loc.T("skill_hide")){ForeColor=hidden?Color.FromArgb(120,230,175):Color.FromArgb(255,125,135)};toggle.Click+=(s,e)=>{if(hidden)hiddenSkillTargetIds.Remove(group.Target.MasterId);else hiddenSkillTargetIds.Add(group.Target.MasterId);SaveEngineSettings();RefreshSkillUpBadge();refresh();};menu.Items.Add(toggle);      AttachContextMenu(card,menu);return card;
    }
    Control SkillFamilyCard(SkillUpFamily fam,ToolTip tip){
      var rows=fam.Rows!=null&&fam.Rows.Count>0?fam.Rows:new List<SkillUpFamilyRow>{new SkillUpFamilyRow{Family=fam.Family,Elements=fam.Elements??new List<SkillUpElementGap>()}};
      int cols=Math.Max(1,rows.Max(r=>Math.Max(1,r.Elements.Count)));
      bool dual=rows.Count>1;
      int rowPitch=dual?132:110;
      var card=new Panel{Width=Math.Max(620,24+cols*118),Height=52+rows.Count*rowPitch,BackColor=Color.FromArgb(13,23,36),Margin=new Padding(10)};
      var accent=new Panel{BackColor=fam.Gaps>0?Color.FromArgb(255,170,40):Color.FromArgb(80,200,140),Dock=DockStyle.Left,Width=4};card.Controls.Add(accent);
      card.Controls.Add(new Label{Text=fam.Family,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",14),AutoSize=true,Location=new Point(18,8)});
      card.Controls.Add(new Label{Text=string.Format(Loc.T("skill_elem_gap"),fam.Gaps)+"  •  "+Loc.T("skill_native",4),ForeColor=Color.FromArgb(166,182,200),Font=new Font("Segoe UI",9),AutoSize=true,Location=new Point(18,32)});
      int y=dual?54:58;
      foreach(var row in rows){
        if(dual)card.Controls.Add(new Label{Text=row.Family+"  •  "+(row.Collab?Loc.T("skill_row_collab"):Loc.T("skill_row_sw")),ForeColor=row.Collab?Color.FromArgb(255,184,85):Color.FromArgb(140,190,220),Font=new Font("Segoe UI Semibold",8),AutoSize=true,Location=new Point(18,y)});
        int x=18;int picY=dual?y+16:y;
        foreach(var el in row.Elements){
          var pic=MonsterPicture(el.Display,52);pic.Location=new Point(x,picY);if(!el.Owned)pic.Enabled=false;card.Controls.Add(pic);
          string st=el.FullySkilled?Loc.T("skill_max"):(el.Owned?string.Format(Loc.T("skill_missing"),el.Need):Loc.T("skill_not_owned"));
          Color sc=el.FullySkilled?Color.FromArgb(80,225,160):(el.Owned?Color.FromArgb(255,170,40):Color.FromArgb(150,163,180));
          card.Controls.Add(new Label{Text=ElementName(el.Element),ForeColor=Color.Gainsboro,Font=new Font("Segoe UI",8),TextAlign=ContentAlignment.MiddleCenter,Location=new Point(x,picY+54),Size=new Size(52,16)});
          card.Controls.Add(new Label{Text=st,ForeColor=sc,Font=new Font("Segoe UI Semibold",8),TextAlign=ContentAlignment.MiddleCenter,Location=new Point(x-8,picY+70),Size=new Size(68,18)});
          if(tip!=null)tip.SetToolTip(pic,el.Name+" • "+ElementName(el.Element)+" • "+st+(el.Owned?" • "+el.Used+"/"+el.ToMax:""));
          x+=118;
        }
        y+=rowPitch;
      }
      return card;
    }
    void AttachContextMenu(Control control,ContextMenuStrip menu){control.ContextMenuStrip=menu;foreach(Control child in control.Controls)AttachContextMenu(child,menu);}
    void QueueWorldBossRealtimeCalculation(){worldBossRevision++;if(string.IsNullOrWhiteSpace(currentFile)||!File.Exists(currentFile))return;worldBossTimer.Stop();worldBossTimer.Start();}
    string WorldBossPlanPath(){string folder=WorldBossDataFolder(),path=Path.Combine(folder,"worldboss-plan.json"),legacy=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"worldboss-plan.json"),protectedPlan=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"worldboss-plan-optimise-protege.json"),marker=Path.Combine(folder,"worldboss-protection-v2.applied");try{Directory.CreateDirectory(folder);if(File.Exists(protectedPlan)&&!File.Exists(marker)){File.Copy(protectedPlan,path,true);File.WriteAllText(marker,DateTime.UtcNow.ToString("O"));}else if(!File.Exists(path)&&File.Exists(legacy))File.Copy(legacy,path,false);}catch{}return path;}
    WorldBossResult LoadWorldBossPlan(){try{string path=WorldBossPlanPath();if(!File.Exists(path))return null;var saved=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256}.Deserialize<WorldBossSavedPlan>(File.ReadAllText(path));var plan=saved==null?null:saved.ToResult();if(plan==null||plan.Rows==null||plan.Rows.Count==0)return null;
      // Les IDs d'équipement restent valides même si la formule ou son libellé
      // évolue. On recharge donc le verrou, puis les scores sont recalculés et le
      // plan est sauvegardé automatiquement sous la formule courante.
      plan.Formula=WorldBossResult.CurrentFormula;return plan;}catch{return null;}}
    void SaveWorldBossPlan(WorldBossResult result){if(result==null)return;try{var saved=WorldBossSavedPlan.From(result);string path=WorldBossPlanPath(),protectedPath=Path.Combine(Path.GetDirectoryName(path),"worldboss-plan-optimise-protege.json"),temporary=path+".tmp";if(File.Exists(path)&&!File.Exists(protectedPath))File.Copy(path,protectedPath,false);File.WriteAllText(temporary,new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=256}.Serialize(saved));if(File.Exists(path))File.Replace(temporary,path,null);else File.Move(temporary,path);}catch(Exception ex){status.Text="Plan World Boss non sauvegardé : "+ex.Message;}}
    void StartWorldBossRealtimeCalculation(){if(worldBossCalculating||string.IsNullOrWhiteSpace(currentFile)||!File.Exists(currentFile))return;int calculationRevision=worldBossRevision;string file=currentFile,catalog=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","monsters","catalog.json");if(!File.Exists(catalog))return;string[] liveEvents=liveSavedEvents.ToArray();WorldBossResult previousPlan=worldBossLatest??LoadWorldBossPlan();worldBossCalculating=true;System.Threading.ThreadPool.QueueUserWorkItem(_=>{WorldBossResult calculated=null;Exception error=null;try{calculated=WorldBossOptimizer.Analyze(file,catalog,liveEvents,previousPlan);}catch(Exception ex){error=ex;}try{BeginInvoke((MethodInvoker)delegate{worldBossCalculating=false;if(error!=null){status.Text="Mise à jour World Boss reportée : "+error.Message;return;}if(calculated==null)return;worldBossCalculatedFile=file;worldBossLatest=calculated;RefreshWorldBossSaleProtection();SaveWorldBossPlan(calculated);if(worldBossSeen==null){worldBossSeen=calculated;worldBossChanges.Clear();}else BuildWorldBossChanges(worldBossSeen,calculated);RefreshWorldBossUpdateBadge();long wbTopId=0;int wbTop=grid.FirstDisplayedScrollingRowIndex;if(wbTop>=0&&wbTop<grid.Rows.Count){var wbVisible=grid.Rows[wbTop].DataBoundItem as RuneRow;if(wbVisible!=null)wbTopId=wbVisible.Id;}
// RefreshWorldBossSaleProtection() met a jour rune.Action en memoire (protection WB) mais
// la grille liee au DataGridView n'est jamais repeinte : sans ce refresh, une rune WB
// protegee reste affichee "Sell" jusqu'au prochain tri/filtre manuel. On ne rappelle PAS
// RefreshCurrentView() ici (elle relance QueueWorldBossRealtimeCalculation() en fin de
// methode -> boucle infinie avec ce callback WB lui-meme).
if(viewMode=="obtained")SortObtained();else if(viewMode=="reeval")SortReeval();else RefreshGrid();
RestoreGridPosition(wbTopId,wbTop);
if(currentFile!=file||calculationRevision!=worldBossRevision)QueueWorldBossRealtimeCalculation();});}catch{worldBossCalculating=false;}});}
    void BuildWorldBossChanges(WorldBossResult before,WorldBossResult after){worldBossChanges.Clear();if(after.OptimizedTotal<=before.OptimizedTotal+0.01)return;var old=before.Rows.ToDictionary(x=>x.UnitId);var now=after.Rows.ToDictionary(x=>x.UnitId);foreach(var row in after.Rows){WorldBossRow previous;if(!old.TryGetValue(row.UnitId,out previous)){worldBossChanges.Add(new WorldBossChangeRow{Monster=row.Monster,Changement="Nouveau monstre dans les 60",Actuel="Hors classement",Nouveau="Équipe "+row.Team+" • position "+row.Position,Gain=row.OptimizedScore});continue;}double gain=row.OptimizedScore-previous.OptimizedScore;var oldRunes=previous.RuneDetails.ToDictionary(x=>x.Slot);foreach(var rune in row.RuneDetails){WorldBossRuneView oldRune;if(!oldRunes.TryGetValue(rune.Slot,out oldRune)||oldRune.Id!=rune.Id)worldBossChanges.Add(new WorldBossChangeRow{Monster=row.Monster,Changement="Rune slot "+rune.Slot,Actuel=oldRune==null?"Aucune":oldRune.Set+" #"+oldRune.Id,Nouveau=rune.Set+" #"+rune.Id,Gain=gain});}for(int i=0;i<row.ArtifactDetails.Count;i++){var artifact=row.ArtifactDetails[i];var oldArtifact=i<previous.ArtifactDetails.Count?previous.ArtifactDetails[i]:null;if(oldArtifact==null||oldArtifact.Id!=artifact.Id)worldBossChanges.Add(new WorldBossChangeRow{Monster=row.Monster,Changement="Artefact "+artifact.Kind+" "+artifact.Restriction,Actuel=oldArtifact==null?"Aucun":oldArtifact.Kind+" "+oldArtifact.Restriction+" #"+oldArtifact.Id,Nouveau=artifact.Kind+" "+artifact.Restriction+" #"+artifact.Id,Gain=gain});}}foreach(var row in before.Rows.Where(x=>!now.ContainsKey(x.UnitId)))worldBossChanges.Add(new WorldBossChangeRow{Monster=row.Monster,Changement="Monstre remplacé",Actuel="Équipe "+row.Team+" • position "+row.Position,Nouveau="Hors des 60",Gain=-row.OptimizedScore});}
    // Avant (design solid-color d'avant le style "fantome") : ecrasait BackColor en
    // plein rouge/bleu ET perdait le padding droit (Padding.Empty / (38,0,0,0)) —
    // bouton devenait plein et le texte collait au bord droit, faisant paraitre
    // l'ecart avec SKILL-UP plus petit qu'il ne l'est. Fix : garde le style fantome
    // (fond sombre + contour), seul le contour change de couleur pour signaler l'etat,
    // padding droit toujours 16 comme les autres boutons.
    void RefreshWorldBossUpdateBadge(){if(worldBossBadge!=null){int count=worldBossLatest==null?0:worldBossLatest.Rows.Count(x=>x.Gain>0.5);worldBossBadge.Text=count.ToString();worldBossBadge.Visible=count>0;if(worldBossButton!=null)worldBossButton.Padding=count>0?new Padding(38,0,16,0):new Padding(16,0,16,0);worldBossBadge.BringToFront();SetActionBorder(worldBossButton,worldBossBadge,count>0);if(count>0)new ToolTip().SetToolTip(worldBossBadge,count+" monstre(s) avec une amélioration World Boss");RelayoutRow2();}if(worldBossLatest!=null)RefreshOpenWorldBoss(worldBossLatest);}
    void RefreshOpenWorldBoss(WorldBossResult result){
      if(result==null)return;
      var main=Application.OpenForms.Cast<Form>().FirstOrDefault(x=>x.Text=="World Boss — Maximum absolu");
      if(main!=null&&!main.IsDisposed){var head=main.Controls.OfType<Panel>().FirstOrDefault();var grid=main.Controls.OfType<BufferedGrid>().FirstOrDefault();if(grid!=null){grid.DataSource=null;var toggle=head==null?null:head.Controls.OfType<Button>().FirstOrDefault(x=>x.Name=="OptimizedToggle");bool showAll=toggle!=null&&Convert.ToBoolean(toggle.Tag);grid.DataSource=result.Rows.Where(x=>showAll||!string.Equals(x.RuneSets,"Équipement actuel conservé",StringComparison.OrdinalIgnoreCase)).ToList();grid.Refresh();}if(head!=null){double gain=result.OptimizedTotal-result.CurrentTotal;var summary=head.Controls.OfType<Label>().FirstOrDefault(x=>x.Location.Y==48);if(summary!=null)summary.Text=result.TeamScores+"  •  "+result.WaterCount+" monstres Eau";var changes=head.Controls.OfType<Button>().FirstOrDefault(x=>x.Text.StartsWith("CHANGEMENTS DÉTECTÉS",StringComparison.Ordinal));if(changes!=null){changes.Text="CHANGEMENTS DÉTECTÉS  "+worldBossChanges.Count;changes.BackColor=worldBossChanges.Count>0?Color.FromArgb(190,68,58):Color.FromArgb(53,94,116);}var skills=head.Controls.OfType<Button>().FirstOrDefault(x=>x.Text.StartsWith("MONTER 40 + SKILL-UP",StringComparison.Ordinal));if(skills!=null)skills.Text="MONTER 40 + SKILL-UP  "+result.SkillRecommendations.Count;}}
      var sigmarusTest=Application.OpenForms.Cast<Form>().FirstOrDefault(x=>x.Name=="SigmarusWorldBossTest");if(sigmarusTest!=null&&!sigmarusTest.IsDisposed)RefreshSigmarusTest(sigmarusTest,result);
      var advice=Application.OpenForms.Cast<Form>().FirstOrDefault(x=>x.Text=="Monter niveau 40 + Skill-up — World Boss");
      if(advice!=null&&!advice.IsDisposed){var grid=advice.Controls.OfType<BufferedGrid>().FirstOrDefault();if(grid!=null){grid.DataSource=null;var hideButton=advice.Controls.OfType<Button>().FirstOrDefault(x=>x.Name=="HideFinishedToggle");bool hide=hideButton!=null&&Convert.ToBoolean(hideButton.Tag);grid.DataSource=result.SkillRecommendations.Where(x=>!hide||x.Missing>0||x.NeedsLevel40).ToList();grid.Refresh();}var title=advice.Controls.OfType<Label>().FirstOrDefault();if(title!=null)title.Text=result.SkillRecommendations.Count==0?"MONTER 40 + SKILL-UP\nClassement pur hors équipements":"MONTER 40 + SKILL-UP\nTous les exemplaires • classement pur hors équipements";}
      foreach(var equipment in Application.OpenForms.Cast<Form>().Where(x=>x.Name=="WorldBossEquipment"&&!x.IsDisposed).ToList()){long unitId=equipment.Tag is long?(long)equipment.Tag:0;var updated=result.Rows.FirstOrDefault(x=>x.UnitId==unitId);if(updated!=null)PopulateWorldBossEquipment(equipment,updated);}
    }
    void ShowWorldBossChanges(WorldBossResult result,Form owner){var f=new Form{Text="Changements World Boss détectés",Icon=Icon,BackColor=Bg,ForeColor=Color.White,Size=new Size(1150,720),MinimumSize=new Size(850,520),StartPosition=FormStartPosition.CenterParent};var title=new Label{Text="AMÉLIORATIONS DÉTECTÉES EN TEMPS RÉEL\nMonstres, runes et artefacts à remplacer depuis ta dernière consultation",Dock=DockStyle.Top,Height=76,BackColor=Panel,ForeColor=Color.FromArgb(255,178,55),Font=new Font("Segoe UI Semibold",16),Padding=new Padding(18,9,4,4)};var g=new BufferedGrid{Dock=DockStyle.Fill,BackgroundColor=Bg,BorderStyle=BorderStyle.None,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoGenerateColumns=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,RowTemplate={Height=48},GridColor=Color.FromArgb(45,61,75)};g.EnableHeadersVisualStyles=false;g.ColumnHeadersHeight=44;g.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(34,116,151),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10),SelectionBackColor=Color.FromArgb(34,116,151)};g.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Bg,ForeColor=Color.White,Font=new Font("Segoe UI",10),SelectionBackColor=Color.FromArgb(35,66,84),SelectionForeColor=Color.White};Action<string,string,int> col=(p,h,w)=>g.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=p,HeaderText=h,FillWeight=w,AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill});col("Monster","Monstre",160);col("Changement","À modifier",150);col("Actuel","Actuel",230);col("Nouveau","Nouveau conseillé",250);col("Gain","Gain total du monstre",110);g.Columns[4].DefaultCellStyle.Format="+0;-0;0";g.Columns[4].DefaultCellStyle.ForeColor=Color.FromArgb(95,235,165);g.DataSource=worldBossChanges.ToList();if(worldBossChanges.Count==0)title.Text="WORLD BOSS À JOUR\nAucune meilleure attribution détectée";f.Controls.Add(g);f.Controls.Add(title);f.FormClosed+=(s,e)=>{worldBossSeen=worldBossLatest??result;worldBossChanges.Clear();RefreshWorldBossUpdateBadge();};f.ShowDialog(owner);}
    void ShowWorldBoss(){
      if(string.IsNullOrWhiteSpace(currentFile)||!File.Exists(currentFile)){MessageBox.Show("Importe d'abord ton JSON Summoners War.","World Boss",MessageBoxButtons.OK,MessageBoxIcon.Information);return;}string catalog=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","monsters","catalog.json");if(!File.Exists(catalog)){MessageBox.Show("Le catalogue des monstres est absent de l'application.","World Boss",MessageBoxButtons.OK,MessageBoxIcon.Error);return;}WorldBossResult result=worldBossLatest!=null&&worldBossCalculatedFile==currentFile?worldBossLatest:null;if(result==null)try{UseWaitCursor=true;status.Text="Optimisation World Boss : calcul des 60 monstres, 360 runes et 120 artefacts…";Application.DoEvents();result=WorldBossOptimizer.Analyze(currentFile,catalog,liveSavedEvents.ToArray(),LoadWorldBossPlan());worldBossLatest=result;SaveWorldBossPlan(result);worldBossCalculatedFile=currentFile;if(worldBossSeen==null)worldBossSeen=result;}catch(Exception ex){MessageBox.Show("Optimisation impossible : "+ex.Message,"World Boss",MessageBoxButtons.OK,MessageBoxIcon.Error);return;}finally{UseWaitCursor=false;}
      var f=new Form{Text="World Boss — Maximum absolu",Icon=Icon,BackColor=Bg,ForeColor=Color.White,Size=new Size(1500,850),MinimumSize=new Size(1100,650),StartPosition=FormStartPosition.CenterParent};var head=new Panel{Dock=DockStyle.Top,Height=122,BackColor=Panel,Padding=new Padding(18,12,18,10)};f.Controls.Add(head);head.Controls.Add(new Label{Text="WORLD BOSS  •  MAXIMUM ABSOLU"+(result.UsesGameOrder?"  •  CALIBRÉ SUR LE JEU":""),ForeColor=Color.FromArgb(80,190,255),Font=new Font("Segoe UI Semibold",20),AutoSize=true,Location=new Point(18,10)});double gain=result.OptimizedTotal-result.CurrentTotal;head.Controls.Add(new Label{Text=result.TeamScores+"  •  "+result.WaterCount+" monstres Eau",ForeColor=Color.FromArgb(100,235,175),Font=new Font("Segoe UI Semibold",11),AutoSize=true,Location=new Point(18,48)});head.Controls.Add(new Label{Text=(result.UsesGameOrder?"Les 60 positions du jeu étalonnent le calcul ; tous les autres monstres peuvent encore s'insérer. ":"")+"Chaque équipement n'est attribué qu'une fois.",ForeColor=Color.Silver,Font=new Font("Segoe UI",9),AutoSize=true,Location=new Point(18,76)});head.Controls.Add(new Label{Text="Clic droit sur un monstre : afficher ses 6 runes et ses 2 artefacts proposés.",ForeColor=Color.FromArgb(255,178,55),Font=new Font("Segoe UI Semibold",9),AutoSize=true,Location=new Point(18,96)});var sigmarus=Button("TEST SIGMARUS",595,17,165,Color.FromArgb(33,128,116));sigmarus.Anchor=AnchorStyles.Top|AnchorStyles.Right;sigmarus.Click+=(s,e)=>ShowSigmarusTest(result,f);
      // Round 7 : plus de moteur "Sigmarus" separe (pont v8 retire, meme moteur partout).
      // Ce bouton reste comme outil de verification (force equipement/ordre reels).
      sigmarus.Visible=true;
      head.Controls.Add(sigmarus);var detected=Button("CHANGEMENTS DÉTECTÉS  "+worldBossChanges.Count,775,17,260,worldBossChanges.Count>0?Color.FromArgb(190,68,58):Color.FromArgb(53,94,116));detected.Anchor=AnchorStyles.Top|AnchorStyles.Right;detected.Click+=(s,e)=>ShowWorldBossChanges(result,f);head.Controls.Add(detected);var skillAdvice=Button("MONTER 40 + SKILL-UP  "+result.SkillRecommendations.Count,1050,17,260,Color.FromArgb(183,71,145));skillAdvice.Anchor=AnchorStyles.Top|AnchorStyles.Right;skillAdvice.Click+=(s,e)=>ShowWorldBossSkillRecommendations(result,f);head.Controls.Add(skillAdvice);var optimizedToggle=Button("AFFICHER LES DÉJÀ OPTI",1318,17,155,Color.FromArgb(65,84,105));optimizedToggle.Name="OptimizedToggle";optimizedToggle.Tag=false;optimizedToggle.Anchor=AnchorStyles.Top|AnchorStyles.Right;head.Controls.Add(optimizedToggle);
#if !WORLD_BOSS_STABLE
      // Mode verification (appli test Sigmarus uniquement) : force l'equipement REELLEMENT
      // porte en jeu (6 runes + 2 artefacts) sur les 60 monstres des 3 listes, relics inclus
      // dans le score, et trie par score actuel reel — pour comparer sans decalage avec le jeu.
      var forceCurrentToggle=Button(WorldBossOptimizer.ForceCurrentEquipment?"MODE VÉRIF ACTIF (ÉQUIP. RÉEL)":"MODE VÉRIF • ÉQUIP. RÉEL",1108,54,265,WorldBossOptimizer.ForceCurrentEquipment?Color.FromArgb(190,68,58):Color.FromArgb(90,90,90));
      forceCurrentToggle.Name="ForceCurrentToggle";forceCurrentToggle.Anchor=AnchorStyles.Top|AnchorStyles.Right;
      head.Controls.Add(forceCurrentToggle);
      // Ordre EXACT du jeu colle manuellement : prime sur le tri par score en mode
      // verification (voir WorldBossOptimizer.RealOrderNames). Le score affiche reste
      // celui de l'appli, pour comparer avec le classement reel colle ici.
      var importOrderButton=Button(result.RealOrderApplied?"ORDRE RÉEL ACTIF":"IMPORTER ORDRE RÉEL",828,54,270,result.RealOrderApplied?Color.FromArgb(58,140,90):Color.FromArgb(65,84,105));
      importOrderButton.Name="ImportRealOrderButton";importOrderButton.Anchor=AnchorStyles.Top|AnchorStyles.Right;
      head.Controls.Add(importOrderButton);
#endif
      var g=new BufferedGrid{Dock=DockStyle.Fill,BackgroundColor=Bg,BorderStyle=BorderStyle.None,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoGenerateColumns=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,RowTemplate={Height=54},GridColor=Color.FromArgb(35,55,72)};
g.EnableHeadersVisualStyles=false;
g.ColumnHeadersHeight=44;
g.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(23,118,155),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10),SelectionBackColor=Color.FromArgb(23,118,155)};
g.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Bg,ForeColor=Color.White,Font=new Font("Segoe UI",9),SelectionBackColor=Color.FromArgb(30,67,88),SelectionForeColor=Color.White,Padding=new Padding(3)};
Action<string,string,int> col=(p,h,w)=>g.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=p,HeaderText=h,Width=w,AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill,SortMode=DataGridViewColumnSortMode.Automatic});
col("Team","Équipe",58);
col("Position","#",42);
g.Columns.Add(new DataGridViewImageColumn{Name="MonsterIcon",HeaderText="Monstre",Width=68,MinimumWidth=64,AutoSizeMode=DataGridViewAutoSizeColumnMode.None,ImageLayout=DataGridViewImageCellLayout.Zoom,DefaultCellStyle=new DataGridViewCellStyle{NullValue=null,Padding=new Padding(5)}});
col("Element","Élément",75);
col("SkillUps","Skill-ups",72);
col("CurrentScore","Indice actuel",90);
col("OptimizedScore","Valeur max totale",125);
col("Gain","Amélioration",170);
col("SkillUpAdvice","Conseil skill-up 5★",150);
col("RuneSets","Sets proposés",220);
col("Changes","Changements",90);
g.Columns[0].FillWeight=55;
g.Columns[1].FillWeight=40;
g.Columns[3].FillWeight=70;
g.Columns[4].FillWeight=65;
g.Columns[5].FillWeight=85;
g.Columns[6].FillWeight=115;
g.Columns[7].FillWeight=80;
g.Columns[8].FillWeight=145;
g.Columns[9].FillWeight=220;
g.Columns[10].FillWeight=85;
g.Columns[5].DefaultCellStyle.Format="0";
g.Columns[6].DefaultCellStyle.Format="0";
g.Columns[7].DefaultCellStyle.Format="+0;-0;+0";
Action bindRows=()=>{bool showAll=Convert.ToBoolean(optimizedToggle.Tag);
g.DataSource=null;
g.DataSource=result.Rows.Where(x=>showAll||!string.Equals(x.RuneSets,"Équipement actuel conservé",StringComparison.OrdinalIgnoreCase)).ToList();
optimizedToggle.Text=showAll?"CACHER LES DÉJÀ OPTI":"AFFICHER LES DÉJÀ OPTI";
};
optimizedToggle.Click+=(s,e)=>{optimizedToggle.Tag=!Convert.ToBoolean(optimizedToggle.Tag);
bindRows();
};
#if !WORLD_BOSS_STABLE
forceCurrentToggle.Click+=(s,e)=>{
  WorldBossOptimizer.ForceCurrentEquipment=!WorldBossOptimizer.ForceCurrentEquipment;
  try{
    UseWaitCursor=true;string catalog2=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","monsters","catalog.json");
    result=WorldBossOptimizer.Analyze(currentFile,catalog2,liveSavedEvents.ToArray(),worldBossLatest);
    worldBossLatest=result;worldBossCalculatedFile=currentFile;
  }catch(Exception ex){
    WorldBossOptimizer.ForceCurrentEquipment=!WorldBossOptimizer.ForceCurrentEquipment;
    MessageBox.Show("Recalcul impossible : "+ex.Message,"World Boss",MessageBoxButtons.OK,MessageBoxIcon.Error);
    return;
  }finally{UseWaitCursor=false;}
  forceCurrentToggle.Text=WorldBossOptimizer.ForceCurrentEquipment?"MODE VÉRIF ACTIF (ÉQUIP. RÉEL)":"MODE VÉRIF • ÉQUIP. RÉEL";
  forceCurrentToggle.BackColor=WorldBossOptimizer.ForceCurrentEquipment?Color.FromArgb(190,68,58):Color.FromArgb(90,90,90);
  bindRows();
  var summary2=head.Controls.OfType<Label>().FirstOrDefault(x=>x.Location.Y==48);if(summary2!=null)summary2.Text=result.TeamScores+"  •  "+result.WaterCount+" monstres Eau";
  status.Text=WorldBossOptimizer.ForceCurrentEquipment?"Mode vérification activé : équipement réel forcé sur les 60 monstres, trié par score actuel.":"Mode vérification désactivé : retour aux builds optimisés proposés.";
  RefreshWorldBossUpdateBadge();
};
importOrderButton.Click+=(s,e)=>{
  string initial=WorldBossOptimizer.RealOrderNames!=null?string.Join(Environment.NewLine,WorldBossOptimizer.RealOrderNames):"";
  string pasted=PromptMultilineText("Colle les 60 noms dans l'ordre EXACT du jeu (un par ligne ou séparés par virgule).\nÉquipe 1 = lignes 1-20, Équipe 2 = 21-40, Équipe 3 = 41-60.\nPour une variante élémentaire ajoute l'élément (\"eau\"/\"feu\"/\"vent\"/\"lumière\"/\"ténèbres\"), pour un doublon ajoute son numéro (\"Bastet 2\").",initial,f);
  if(pasted==null)return;
  var list=pasted.Split(new[]{',','\n','\r'},StringSplitOptions.RemoveEmptyEntries).Select(x=>x.Trim()).Where(x=>x.Length>0).ToList();
  if(list.Count!=60){MessageBox.Show("Il faut exactement 60 noms (trouvé : "+list.Count+").","Ordre réel",MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}
  WorldBossOptimizer.RealOrderNames=list;SaveWorldBossRealOrder(list);
  try{
    UseWaitCursor=true;string catalog3=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","monsters","catalog.json");
    result=WorldBossOptimizer.Analyze(currentFile,catalog3,liveSavedEvents.ToArray(),worldBossLatest);
    worldBossLatest=result;worldBossCalculatedFile=currentFile;
  }catch(Exception ex){MessageBox.Show("Recalcul impossible : "+ex.Message,"World Boss",MessageBoxButtons.OK,MessageBoxIcon.Error);return;}finally{UseWaitCursor=false;}
  bindRows();
  importOrderButton.Text=result.RealOrderApplied?"ORDRE RÉEL ACTIF":"IMPORTER ORDRE RÉEL";
  importOrderButton.BackColor=result.RealOrderApplied?Color.FromArgb(58,140,90):Color.FromArgb(65,84,105);
  if(!result.RealOrderApplied)MessageBox.Show("Ordre réel PAS appliqué (retour au tri par score). Noms non reconnus :\n"+string.Join("\n",result.RealOrderUnresolved),"Ordre réel",MessageBoxButtons.OK,MessageBoxIcon.Warning);
  else MessageBox.Show("Ordre réel appliqué : les 60 monstres suivent maintenant exactement l'ordre collé.","Ordre réel",MessageBoxButtons.OK,MessageBoxIcon.Information);
  var summary3=head.Controls.OfType<Label>().FirstOrDefault(x=>x.Location.Y==48);if(summary3!=null)summary3.Text=result.TeamScores+"  •  "+result.WaterCount+" monstres Eau";
  RefreshWorldBossUpdateBadge();
};
#endif
bindRows();
g.CellFormatting+=(s,e)=>{if(e.RowIndex<0)return;
var row=g.Rows[e.RowIndex].DataBoundItem as WorldBossRow;
if(row==null)return;
if(row.Team==1)e.CellStyle.BackColor=Color.FromArgb(11,31,48);
else if(row.Team==2)e.CellStyle.BackColor=Color.FromArgb(18,28,43);
else e.CellStyle.BackColor=Color.FromArgb(24,25,39);
if(e.ColumnIndex==2){e.Value=WorldBossMonsterIcon(row.MasterId,row.Monster,row.Element);
e.FormattingApplied=true;
g.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText=row.Monster;
}if(e.ColumnIndex==6)e.CellStyle.ForeColor=Color.FromArgb(65,235,165);
if(e.ColumnIndex==7){e.CellStyle.ForeColor=row.Gain>0?Color.FromArgb(65,235,165):Color.Silver;if(row.Gain<=0){e.Value="Équipement actuel conservé";e.FormattingApplied=true;}}
if(e.ColumnIndex==8&&row.SkillUpGain>0){e.CellStyle.ForeColor=Color.FromArgb(255,116,205);
e.CellStyle.Font=new Font("Segoe UI Semibold",9);
}};
g.CellMouseDown+=(s,e)=>{if(e.Button!=MouseButtons.Right||e.RowIndex<0)return;
g.ClearSelection();
g.Rows[e.RowIndex].Selected=true;
var row=g.Rows[e.RowIndex].DataBoundItem as WorldBossRow;
if(row!=null)ShowWorldBossEquipment(row,f);
};
f.Controls.Add(g);
g.BringToFront();
status.Text="Optimisation World Boss terminée : gain d'indice estimé +"+gain.ToString("N0");
f.ShowDialog(this);

    }
    void ShowSigmarusTest(WorldBossResult result,Form owner){var existing=Application.OpenForms.Cast<Form>().FirstOrDefault(x=>x.Name=="SigmarusWorldBossTest");if(existing!=null&&!existing.IsDisposed){existing.Activate();RefreshSigmarusTest(existing,result);return;}var f=new Form{Name="SigmarusWorldBossTest",Text="Test World Boss — Sigmarus",Icon=Icon,BackColor=Bg,ForeColor=Color.White,ClientSize=new Size(720,430),MinimumSize=new Size(650,390),StartPosition=FormStartPosition.CenterParent};var title=new Label{Name="Title",Text="SIGMARUS  •  TEST DE CALIBRATION",Dock=DockStyle.Top,Height=72,BackColor=Panel,ForeColor=Color.FromArgb(80,190,255),Font=new Font("Segoe UI Semibold",20),Padding=new Padding(112,16,8,8)};var portrait=new PictureBox{Name="Portrait",Image=new Bitmap(WorldBossMonsterIcon(14511,"Sigmarus","Eau")),Location=new Point(24,12),Size=new Size(76,76),SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(8,12,15),BorderStyle=BorderStyle.FixedSingle};var info=new Label{Name="Info",Location=new Point(28,102),Size=new Size(660,218),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",14),BackColor=Bg};var help=new Label{Name="Help",Text="Après avoir retiré une rune : exporte/importe le nouveau JSON, puis clique sur ACTUALISER. Compare la position ÉQUIPEMENT RÉEL avec celle du jeu.",Location=new Point(28,318),Size=new Size(445,78),ForeColor=Color.Silver,Font=new Font("Segoe UI",10)};var refresh=Button("ACTUALISER LE JSON",490,330,198,Color.FromArgb(34,133,164));refresh.Height=48;refresh.Click+=(s,e)=>{try{UseWaitCursor=true;string catalog=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","monsters","catalog.json");var updated=WorldBossOptimizer.Analyze(currentFile,catalog,liveSavedEvents.ToArray(),LoadWorldBossPlan());worldBossLatest=updated;worldBossCalculatedFile=currentFile;RefreshSigmarusTest(f,updated);}catch(Exception ex){MessageBox.Show("Actualisation impossible : "+ex.Message,"Test Sigmarus",MessageBoxButtons.OK,MessageBoxIcon.Error);}finally{UseWaitCursor=false;}};f.Controls.Add(info);f.Controls.Add(help);f.Controls.Add(refresh);f.Controls.Add(portrait);f.Controls.Add(title);portrait.BringToFront();f.FormClosed+=(s,e)=>{if(portrait.Image!=null){portrait.Image.Dispose();portrait.Image=null;}};RefreshSigmarusTest(f,result);f.Show(owner);}
    void RefreshSigmarusTest(Form form,WorldBossResult result){if(form==null||form.IsDisposed||result==null)return;var info=form.Controls.Find("Info",true).FirstOrDefault() as Label;var row=result.Rows.FirstOrDefault(x=>x.MasterId==14511||x.Monster.StartsWith("Sigmarus",StringComparison.OrdinalIgnoreCase));if(info==null)return;if(row==null){info.Text="Sigmarus n'est pas présent dans les 60 monstres suivis.";return;}int proposed=(row.Team-1)*20+row.Position,currentRank=result.CurrentEquipmentRanks.ContainsKey(row.UnitId)?result.CurrentEquipmentRanks[row.UnitId]:0,total=Math.Max(60,result.CurrentEquipmentCount);double currentScore=result.CurrentEquipmentScores.ContainsKey(row.UnitId)?result.CurrentEquipmentScores[row.UnitId]:row.CurrentScore;var previous=form.Tag as Tuple<int,double,int>;string variation=previous==null?"MESURE DE RÉFÉRENCE ENREGISTRÉE":"DEPUIS LA MESURE PRÉCÉDENTE     rang "+(currentRank-previous.Item1).ToString("+0;-0;0")+"  •  score "+(currentScore-previous.Item2).ToString("+N0;-N0;0")+"  •  runes "+(row.CurrentRuneIds.Count-previous.Item3).ToString("+0;-0;0");info.Text="POSITION AVEC ÉQUIPEMENT RÉEL     "+(currentRank>0?currentRank.ToString()+" / "+total:"—")+Environment.NewLine+"POSITION PROPOSÉE PAR L'OPTIMISEUR     "+proposed+" / 60"+Environment.NewLine+"ÉQUIPE / POSITION PROPOSÉE     "+row.Team+" / "+row.Position+Environment.NewLine+"SCORE ACTUEL CALCULÉ     "+currentScore.ToString("N0")+Environment.NewLine+"ÉQUIPEMENT DÉTECTÉ     "+row.CurrentRuneIds.Count+" rune(s)  •  "+row.CurrentArtifactIds.Count+" artefact(s)"+Environment.NewLine+variation+Environment.NewLine+"IDENTIFIANT     #"+(row.UnitId%1000000).ToString("000000");form.Tag=Tuple.Create(currentRank,currentScore,row.CurrentRuneIds.Count);}
    void ShowWorldBossSkillRecommendations(WorldBossResult result,Form owner){var f=new Form{Text="Monter niveau 40 + Skill-up — World Boss",Icon=Icon,BackColor=Bg,ForeColor=Color.White,Size=new Size(1120,650),MinimumSize=new Size(850,450),StartPosition=FormStartPosition.CenterParent};
var title=new Label{Dock=DockStyle.Top,Height=112,BackColor=Panel,ForeColor=Color.FromArgb(255,102,195),Font=new Font("Segoe UI Semibold",18),Padding=new Padding(18,12,4,4),Text="MONTER 40 + SKILL-UP\nDans les 60 ou capables d'y entrer après amélioration"};
f.Controls.Add(title);
var hideFinished=Button("CACHER LES TERMINÉS",850,18,235,Color.FromArgb(105,70,135));
hideFinished.Name="HideFinishedToggle";
hideFinished.Anchor=AnchorStyles.Top|AnchorStyles.Right;
hideFinished.Tag=false;
f.Controls.Add(hideFinished);
var nativeFilter=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(20,76),Size=new Size(150,28)};
nativeFilter.Items.AddRange(new object[]{"Tous les natifs","3★ nat seulement","4★ nat seulement","5★ nat seulement"});
nativeFilter.SelectedIndex=0;
var reasonFilter=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Location=new Point(185,76),Size=new Size(270,28)};
reasonFilter.Items.AddRange(new object[]{"Tous les potentiels","Dans les 60 actuellement","Peut entrer après amélioration","Dans les 60 ou peut entrer","Maximum insuffisant","Terminés seulement"});
reasonFilter.SelectedIndex=0;
f.Controls.Add(nativeFilter);
f.Controls.Add(reasonFilter);
var g=new BufferedGrid{Dock=DockStyle.Fill,BackgroundColor=Bg,BorderStyle=BorderStyle.None,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoGenerateColumns=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,RowTemplate={Height=54},GridColor=Color.FromArgb(35,55,72)};
g.EnableHeadersVisualStyles=false;
g.ColumnHeadersHeight=42;
g.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(140,48,112),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10),SelectionBackColor=Color.FromArgb(140,48,112)};
g.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Bg,ForeColor=Color.White,Font=new Font("Segoe UI",10),SelectionBackColor=Color.FromArgb(55,36,67),SelectionForeColor=Color.White};
Action<string,string,int> col=(p,h,w)=>g.Columns.Add(new DataGridViewTextBoxColumn{DataPropertyName=p,HeaderText=h,Width=w,AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill});
g.Columns.Add(new DataGridViewImageColumn{Name="MonsterIcon",HeaderText="Monstre",Width=68,MinimumWidth=64,AutoSizeMode=DataGridViewAutoSizeColumnMode.None,ImageLayout=DataGridViewImageCellLayout.Zoom,DefaultCellStyle=new DataGridViewCellStyle{NullValue=null,Padding=new Padding(5)}});
col("Copy","Exemplaire",75);
col("Element","Élément",65);
col("NaturalGrade","Natif",65);
col("LevelAdvice","Niveau",80);
col("Current","Skills actuels",70);
col("Maximum","Skills max",70);
col("Missing","Skill-ups à ajouter",80);
col("MaximumScore","Score pur max",105);
col("Reason","Pourquoi",260);
g.Columns[8].DefaultCellStyle.Format="0";
g.Columns[8].DefaultCellStyle.ForeColor=Color.FromArgb(95,235,165);
Action bindAdvice=()=>{bool hide=Convert.ToBoolean(hideFinished.Tag);
IEnumerable<WorldBossSkillRecommendation> filtered=result.SkillRecommendations;
if(nativeFilter.SelectedIndex>0)filtered=filtered.Where(x=>x.NaturalStars==nativeFilter.SelectedIndex+2);
if(reasonFilter.SelectedIndex==1)filtered=filtered.Where(x=>x.InCurrentTeam);
else if(reasonFilter.SelectedIndex==2)filtered=filtered.Where(x=>!x.InCurrentTeam&&x.Reason.StartsWith("Entre dans les 60",StringComparison.OrdinalIgnoreCase));
else if(reasonFilter.SelectedIndex==3)filtered=filtered.Where(x=>x.InCurrentTeam||x.Reason.StartsWith("Entre dans les 60",StringComparison.OrdinalIgnoreCase));
else if(reasonFilter.SelectedIndex==4)filtered=filtered.Where(x=>x.Reason.IndexOf("insuffisant",StringComparison.OrdinalIgnoreCase)>=0);
else if(reasonFilter.SelectedIndex==5)filtered=filtered.Where(x=>x.Missing<=0&&!x.NeedsLevel40);
g.DataSource=null;
g.DataSource=filtered.Where(x=>!hide||x.Missing>0||x.NeedsLevel40).ToList();
hideFinished.Text=hide?"AFFICHER LES TERMINÉS":"CACHER LES TERMINÉS";
};
hideFinished.Click+=(s,e)=>{hideFinished.Tag=!Convert.ToBoolean(hideFinished.Tag);
bindAdvice();
};
nativeFilter.SelectedIndexChanged+=(s,e)=>bindAdvice();
reasonFilter.SelectedIndexChanged+=(s,e)=>bindAdvice();
bindAdvice();
g.CellFormatting+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex!=0)return;
var recommendation=g.Rows[e.RowIndex].DataBoundItem as WorldBossSkillRecommendation;
if(recommendation==null)return;
e.Value=WorldBossMonsterIcon(recommendation.MasterId,recommendation.Monster,recommendation.Element);
e.FormattingApplied=true;
g.Rows[e.RowIndex].Cells[e.ColumnIndex].ToolTipText=recommendation.Monster+"  "+recommendation.Copy+"  •  "+recommendation.NaturalGrade;
};
if(result.SkillRecommendations.Count==0)title.Text="MONTER 40 + SKILL-UP\nClassement pur hors équipements";
f.Controls.Add(g);
g.BringToFront();
hideFinished.BringToFront();
nativeFilter.BringToFront();
reasonFilter.BringToFront();
f.ShowDialog(owner);
}
    internal void ShowWorldBossEquipment(WorldBossRow row,Form owner){
      var f=new Form{Name="WorldBossEquipment",Tag=row.UnitId,Text=(row.Team>0?"Équipement World Boss — ":"Équipement RTA — ")+row.Monster,Icon=Icon,BackColor=Color.FromArgb(14,18,22),ForeColor=Color.White,Size=new Size(1280,850),MinimumSize=new Size(980,700),StartPosition=FormStartPosition.Manual,AutoScroll=true};f.Location=new Point(owner.Left,owner.Top);PopulateWorldBossEquipment(f,row);f.ShowDialog(owner);
    }
    void PopulateWorldBossEquipment(Form f,WorldBossRow row){
      if(f==null||f.IsDisposed||row==null)return;f.SuspendLayout();foreach(Control old in f.Controls.Cast<Control>().ToArray())old.Dispose();f.Tag=row.UnitId;f.Text=(row.Team>0?"Équipement World Boss — ":"Équipement RTA — ")+row.Monster;var runes=row.RuneDetails.OrderBy(r=>r.Slot==6?0:r.Slot==1?1:r.Slot==2?2:r.Slot==5?3:r.Slot==4?4:5).ToList();var root=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,Padding=new Padding(12),BackColor=Color.FromArgb(14,18,22)};f.Controls.Add(root);string equipmentContext=row.Team>0?"Équipe "+row.Team+" / position "+row.Position+"  •  valeur max totale "+row.OptimizedScore.ToString("N0")+"  •  amélioration "+row.Gain.ToString("+0;-0;+0"):"Pool RTA • priorité "+row.Position+" • "+row.RuneSets;var title=new Label{Text=row.Monster+"  •  "+equipmentContext,ForeColor=Color.FromArgb(255,174,38),Font=new Font("Segoe UI Semibold",18),Height=48,Width=1215,TextAlign=ContentAlignment.MiddleLeft};root.Controls.Add(title);root.Controls.Add(new Label{Text="RUNES PROPOSÉES",ForeColor=Color.FromArgb(255,174,38),Font=new Font("Segoe UI Light",16),Height=42,Width=1215,TextAlign=ContentAlignment.MiddleLeft});var runeGrid=new TableLayoutPanel{Width=1215,Height=500,ColumnCount=3,RowCount=2,BackColor=Color.FromArgb(14,18,22),Margin=new Padding(0)};for(int i=0;i<3;i++)runeGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,33.333f));for(int i=0;i<2;i++)runeGrid.RowStyles.Add(new RowStyle(SizeType.Percent,50));for(int i=0;i<6;i++){Control c=i<runes.Count?WorldBossRuneCard(runes[i],!row.CurrentRuneIds.Contains(runes[i].Id)):WorldBossEmptyCard("Emplacement de rune vide");runeGrid.Controls.Add(c,i%3,i/3);}root.Controls.Add(runeGrid);root.Controls.Add(new Label{Text="ARTEFACTS PROPOSÉS",ForeColor=Color.FromArgb(255,174,38),Font=new Font("Segoe UI Light",16),Height=46,Width=1215,TextAlign=ContentAlignment.BottomLeft});var artifactGrid=new TableLayoutPanel{Width=1215,Height=215,ColumnCount=2,RowCount=1,BackColor=Color.FromArgb(14,18,22),Margin=new Padding(0)};artifactGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));artifactGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));for(int i=0;i<2;i++)artifactGrid.Controls.Add(i<row.ArtifactDetails.Count?WorldBossArtifactCard(row.ArtifactDetails[i],!row.CurrentArtifactIds.Contains(row.ArtifactDetails[i].Id)):WorldBossEmptyCard("Emplacement d'artefact vide"),i,0);root.Controls.Add(artifactGrid);f.ResumeLayout(true);
    }
    Control WorldBossRuneCard(WorldBossRuneView rune,bool changed){int displayLevel=Math.Max(0,rune.CurrentLevel);string levelTitle=displayLevel<15?"+"+displayLevel+" → +15 simulé":"+15";string stars=new string('★',Math.Max(1,Math.Min(6,rune.Grade)));string body=Environment.NewLine+Environment.NewLine+"PRINCIPALE  •  "+rune.Main+Environment.NewLine+"INNÉE  •  "+rune.Innate+Environment.NewLine+Environment.NewLine+"SOUS-STATS"+Environment.NewLine+string.Join(Environment.NewLine,rune.Stats);string icon="rune|"+rune.Set+"|"+rune.Grade+"|"+displayLevel+"|"+rune.Slot+"|"+(rune.Ancient?"1":"0");var card=WorldBossEquipmentCard(stars+"     "+levelTitle+" "+rune.Set+"  (slot "+rune.Slot+")",body,"ID : "+rune.Id,Color.FromArgb(255,166,20),icon,rune.Slot.ToString(),changed);if(rune.EquippedMasterId>0){var ownerImage=new Bitmap(WorldBossMonsterIcon(rune.EquippedMasterId,"",""));var owner=new PictureBox{Image=ownerImage,Location=new Point(8,40),Size=new Size(34,34),SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(8,12,15),BorderStyle=BorderStyle.FixedSingle};card.Controls.Add(owner);owner.BringToFront();new ToolTip().SetToolTip(owner,"Monstre qui équipe actuellement cette rune");card.Disposed+=(s,e)=>{if(owner.Image!=null){owner.Image.Dispose();owner.Image=null;}};}return card;}
    Control WorldBossArtifactCard(WorldBossArtifactView artifact,bool changed){string body=Environment.NewLine+Environment.NewLine+"PRINCIPALE  •  "+artifact.Main+Environment.NewLine+string.Join(Environment.NewLine,artifact.Effects);string icon="artifact|"+artifact.IconKey+"|"+artifact.Rank+"|"+artifact.Level;var card=WorldBossEquipmentCard("+"+artifact.Level+" Artefact "+artifact.Kind+" • "+artifact.Restriction,body,"ID : "+artifact.Id,Color.FromArgb(255,166,20),icon,artifact.Kind=="Élément"?"E":"T",changed);if(artifact.EquippedMasterId>0){var ownerImage=new Bitmap(WorldBossMonsterIcon(artifact.EquippedMasterId,"",""));var owner=new PictureBox{Image=ownerImage,Location=new Point(8,40),Size=new Size(34,34),SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(8,12,15),BorderStyle=BorderStyle.FixedSingle};card.Controls.Add(owner);owner.BringToFront();new ToolTip().SetToolTip(owner,"Monstre qui équipe actuellement cet artefact");card.Disposed+=(s,e)=>{if(owner.Image!=null){owner.Image.Dispose();owner.Image=null;}};}return card;}
    Control WorldBossEmptyCard(string text){return WorldBossEquipmentCard(text,"Aucun équipement proposé","",Color.FromArgb(95,105,112),"","",false);}
    Control WorldBossEquipmentCard(string header,string body,string footer,Color accent,string iconRelative,string badge,bool changed){Color card=changed?Color.FromArgb(82,20,22):Color.FromArgb(16,21,26),edge=changed?Color.FromArgb(62,13,16):Color.FromArgb(8,12,15);var p=new Panel{Dock=DockStyle.Fill,Margin=new Padding(7),BackColor=card,BorderStyle=BorderStyle.FixedSingle};var h=new Label{Text=header,Dock=DockStyle.Top,Height=36,BackColor=edge,ForeColor=accent,Font=new Font("Segoe UI Semibold",10),TextAlign=ContentAlignment.MiddleCenter};var foot=new Label{Text=footer,Dock=DockStyle.Bottom,Height=35,BackColor=edge,ForeColor=Color.FromArgb(185,220,218),Font=new Font("Segoe UI",8),Padding=new Padding(14,5,4,2)};var b=new Label{Text=body,Dock=DockStyle.Fill,BackColor=card,ForeColor=Color.FromArgb(225,224,214),Font=new Font("Segoe UI",10),Padding=new Padding(84,12,12,8),AutoEllipsis=true};p.Controls.Add(b);PictureBox pic=null;Label mark=null;Image image=LoadWorldBossEquipmentIcon(iconRelative);if(image!=null){pic=new PictureBox{Image=image,Location=new Point(14,54),Size=new Size(56,56),SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.Transparent};p.Controls.Add(pic);p.Disposed+=(s,e)=>{if(pic.Image!=null){pic.Image.Dispose();pic.Image=null;}};if(badge.Length>0){mark=new Label{Text=badge,Location=new Point(50,91),Size=new Size(22,19),BackColor=Color.FromArgb(235,4,11,18),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",8),TextAlign=ContentAlignment.MiddleCenter,BorderStyle=BorderStyle.FixedSingle};p.Controls.Add(mark);}}p.Controls.Add(foot);p.Controls.Add(h);if(pic!=null)pic.BringToFront();if(mark!=null)mark.BringToFront();h.BringToFront();foot.BringToFront();return p;}
    Image LoadWorldBossEquipmentIcon(string relative){if(string.IsNullOrWhiteSpace(relative))return null;try{var parts=relative.Split('|');if(parts.Length>=5&&parts[0]=="rune")return BuildWorldBossRuneIcon(parts[1],int.Parse(parts[2]),int.Parse(parts[3]),int.Parse(parts[4]),parts.Length>=6&&parts[5]=="1");if(parts.Length>=4&&parts[0]=="artifact")return BuildWorldBossArtifactIcon(parts[1],int.Parse(parts[2]),int.Parse(parts[3]));string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets",relative);if(File.Exists(path)){using(var source=Image.FromFile(path))return new Bitmap(source);}}catch{}return null;}
    private Image BuildWorldBossRuneIcon(string setName, int grade, int level, int slot, bool ancient)
		{
			Bitmap bitmap = new Bitmap(64, 64);
			using (Graphics graphics = Graphics.FromImage(bitmap))
			{
				graphics.SmoothingMode = SmoothingMode.AntiAlias;
				graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
				slot = Math.Max(1, Math.Min(6, slot));
				double[] array = new double[7] { 0.0, -90.0, -30.0, 30.0, 90.0, 150.0, -150.0 };
				double num = array[slot] * Math.PI / 180.0;
				double num2 = 16.0 - 8.0 * Math.Cos(num);
				double num3 = 16.0 - 8.0 * Math.Sin(num);
				PointF[] array2 = new PointF[5];
				for (int i = 0; i < 5; i++)
				{
					double num4 = num + (double)(2 * i) * Math.PI / 5.0;
					double num5;
					switch (i)
					{
					default:
						num5 = 16.0;
						break;
					case 2:
					case 3:
						num5 = 9.92;
						break;
					case 0:
						num5 = 30.4;
						break;
					}
					double num6 = num5;
					array2[i] = new PointF((float)(16.0 + num2 + num6 * Math.Cos(num4)), (float)(16.0 + num3 + num6 * Math.Sin(num4)));
				}
				Color color = ((grade >= 5 || level >= 12) ? Color.FromArgb(255, 126, 20) : ((grade == 4) ? Color.FromArgb(178, 62, 255) : Color.FromArgb(52, 160, 255)));
				using (GraphicsPath graphicsPath = new GraphicsPath())
				{
					graphicsPath.AddPolygon(array2);
					if (ancient)
					{
						using (Pen pen = new Pen(Color.FromArgb(80, color), 7f))
						{
							graphics.DrawPath(pen, graphicsPath);
						}
						using (SolidBrush brush = new SolidBrush(Color.FromArgb(185, color)))
						{
							graphics.FillPath(brush, graphicsPath);
						}
					}
					else
					{
						using (SolidBrush brush = new SolidBrush(Color.FromArgb(20, 24, 29)))
						{
							graphics.FillPath(brush, graphicsPath);
						}
					}
					using (Region clip = graphics.Clip)
					{
						graphics.SetClip(graphicsPath, CombineMode.Intersect);
						Image setIcon = GetSetIcon(setName);
						if (setIcon != null)
						{
							graphics.DrawImage(setIcon, new RectangleF(16f, 16f, 32f, 32f));
						}
						graphics.Clip = clip;
					}
					using (Pen pen2 = new Pen(color, 2f))
					{
						graphics.DrawPath(pen2, graphicsPath);
					}
				}
				RectangleF rectangleF = new RectangleF(34f, 46f, 28f, 15f);
				using (SolidBrush brush = new SolidBrush(Color.FromArgb(235, 20, 24, 30)))
				{
					graphics.FillRectangle(brush, rectangleF);
				}
				using (Pen pen2 = new Pen(Color.FromArgb(105, 115, 125)))
				{
					graphics.DrawRectangle(pen2, rectangleF.X, rectangleF.Y, rectangleF.Width, rectangleF.Height);
				}
				using (Font font = new Font("Segoe UI Semibold", 7.5f))
				{
					using (SolidBrush brush2 = new SolidBrush(Color.White))
					{
						StringFormat stringFormat = new StringFormat();
						stringFormat.Alignment = StringAlignment.Center;
						stringFormat.LineAlignment = StringAlignment.Center;
						StringFormat format = stringFormat;
						graphics.DrawString("+" + level, font, brush2, rectangleF, format);
					}
				}
			}
			return bitmap;
		}
    Image BuildWorldBossArtifactIcon(string key,int rank,int level){var canvas=new Bitmap(64,64);using(var g=Graphics.FromImage(canvas)){g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;Color quality=rank>=5?Color.FromArgb(255,153,0):Color.FromArgb(170,68,221);var frame=new Rectangle(5,5,50,50);using(var fill=new SolidBrush(Color.FromArgb(25,quality)))g.FillRectangle(fill,frame);using(var pen=new Pen(quality,2))g.DrawRectangle(pen,frame);string path=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","artifacts",key+".png");if(File.Exists(path))using(var source=Image.FromFile(path))g.DrawImage(source,new Rectangle(14,14,32,32));else if(key.Equals("intangible",StringComparison.OrdinalIgnoreCase)){var intangible=GetSetIcon("Intangible");if(intangible!=null)g.DrawImage(intangible,new Rectangle(14,14,32,32));}var badge=new Rectangle(37,47,25,14);using(var fill=new SolidBrush(Color.FromArgb(245,5,14,24)))g.FillRectangle(fill,badge);using(var pen=new Pen(Color.FromArgb(100,120,138)))g.DrawRectangle(pen,badge);using(var font=new Font("Segoe UI Semibold",7))TextRenderer.DrawText(g,"+"+level,font,badge,Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding);}return canvas;}
    PictureBox MonsterPicture(SkillUpMonster monster,int size){int portraitId=monster.CatalogId>0?monster.CatalogId:monster.MasterId;Image cached;if(!monsterPortraits.TryGetValue(portraitId,out cached)||cached==null){string local=ResolveMonsterPortraitFile(portraitId);if(local!=null)try{using(var fs=new FileStream(local,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var src=Image.FromStream(fs))cached=new Bitmap(src);if(cached!=null)monsterPortraits[portraitId]=cached;}catch{}}var fallback=cached??MonsterPlaceholder(monster,size);var p=new PictureBox{Size=new Size(size,size),SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.FromArgb(8,15,24),BorderStyle=BorderStyle.FixedSingle,Image=fallback,InitialImage=fallback,ErrorImage=fallback,WaitOnLoad=false};if(cached!=null)return p;string url=(monster.Icon??"").Replace("https://do9d4mpqk497d.cloudfront.net/common/images/monsters36/","https://swarfarm.com/static/herders/images/monsters/");if(url.Length>0){p.ImageLocation=url;try{p.LoadAsync();}catch{p.Image=fallback;}}return p;}
    Image MonsterPlaceholder(SkillUpMonster monster,int size){var b=new Bitmap(size,size);using(var g=Graphics.FromImage(b)){g.SmoothingMode=SmoothingMode.AntiAlias;Color c=monster.Element=="fire"?Color.FromArgb(184,64,51):monster.Element=="water"?Color.FromArgb(45,120,185):monster.Element=="wind"?Color.FromArgb(191,157,54):monster.Element=="light"?Color.FromArgb(214,195,105):Color.FromArgb(117,72,157);using(var bg=new SolidBrush(Color.FromArgb(18,28,42)))g.FillRectangle(bg,0,0,size,size);using(var fill=new SolidBrush(c))g.FillEllipse(fill,5,5,size-10,size-10);string t=string.IsNullOrWhiteSpace(monster.Name)?"?":monster.Name.Substring(0,1).ToUpperInvariant();using(var font=new Font("Segoe UI Semibold",Math.Max(11,size*.34f)))using(var textBrush=new SolidBrush(Color.White)){var sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};g.DrawString(t,font,textBrush,new RectangleF(0,0,size,size),sf);}}return b;}
    // Recherche partagée d'un portrait local : exact d'abord, sinon le fichier de la même
    // famille (préfixe id/100, ex. formes éveillées/non-éveillées) le plus proche par id.
    // Utilisée par tous les boutons à icône de monstre pour éviter les icônes manquantes
    // quand seul un id de forme différente est présent sur le disque.
    // Le dernier chiffre de l'id du jeu code l'élément (1 eau, 2 feu, 3 vent, 4 lumière,
    // 5 ténèbres) et l'avant-dernier le palier d'éveil — même famille = mêmes chiffres sauf
    // ces deux-là (ex. Nine-tailed Fox Feu = 11202, sa forme éveillée = 11212). Avant, le
    // fallback "même famille" prenait juste le fichier numériquement le plus proche, ce qui
    // pouvait tomber sur un AUTRE élément de la même famille (11211 = Eau est plus proche de
    // 11202 que 11212 = Feu) : icône fausse mais affichée avec assurance. Corrigé pour
    // n'accepter qu'un fichier du MEME élément ; sinon on renvoie null (placeholder
    // générique) plutôt qu'une icône trompeuse.
    string ResolveMonsterPortraitFile(int id){
      string cachedPath;if(portraitPathById.TryGetValue(id,out cachedPath))return string.IsNullOrEmpty(cachedPath)?null:cachedPath;
      string directory=Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","monsters"),local=Path.Combine(directory,id+".png");
      if(File.Exists(local)){portraitPathById[id]=local;return local;}
      try{
        string alt=Path.Combine(directory,(id+10)+".png");if(File.Exists(alt)){portraitPathById[id]=alt;return alt;}
        alt=Path.Combine(directory,(id-10)+".png");if(File.Exists(alt)){portraitPathById[id]=alt;return alt;}
        int element=id%10;
        if(element>=1&&element<=5){
          string prefix=(id/100).ToString();
          string[] files;if(!portraitFilesByPrefix.TryGetValue(prefix,out files)){files=Directory.Exists(directory)?Directory.GetFiles(directory,prefix+"*.png"):new string[0];portraitFilesByPrefix[prefix]=files;}
          var match=files.Where(p=>ParsePortraitId(p)%10==element).OrderBy(p=>Math.Abs(ParsePortraitId(p)-id)).FirstOrDefault();
          if(match!=null){portraitPathById[id]=match;return match;}
        }
      }catch{}
      portraitPathById[id]="";return null;
    }
    Image WorldBossMonsterIcon(int masterId,string name,string element){Image cached;if(worldBossMonsterIcons.TryGetValue(masterId,out cached))return cached;string local=ResolveMonsterPortraitFile(masterId);if(local!=null)try{using(var fs=new FileStream(local,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))using(var source=Image.FromStream(fs))cached=new Bitmap(source);}catch{}if(cached==null){var b=new Bitmap(44,44);using(var gr=Graphics.FromImage(b)){gr.SmoothingMode=SmoothingMode.AntiAlias;Color c=element=="Feu"?Color.FromArgb(184,64,51):element=="Eau"?Color.FromArgb(45,120,185):element=="Vent"?Color.FromArgb(191,157,54):element=="Lumière"?Color.FromArgb(214,195,105):Color.FromArgb(117,72,157);using(var fill=new SolidBrush(c))gr.FillEllipse(fill,2,2,40,40);using(var font=new Font("Segoe UI Semibold",16))using(var brush=new SolidBrush(Color.White)){var sf=new StringFormat{Alignment=StringAlignment.Center,LineAlignment=StringAlignment.Center};gr.DrawString(string.IsNullOrWhiteSpace(name)?"?":name.Substring(0,1).ToUpperInvariant(),font,brush,new RectangleF(0,0,44,44),sf);}}cached=b;}worldBossMonsterIcons[masterId]=cached;return cached;}
    int ParsePortraitId(string path){int id;return int.TryParse(Path.GetFileNameWithoutExtension(path),out id)?id:int.MaxValue/2;}
    string ElementName(string e){return Loc.Element(e);}
    void ShowStock(){
      var f=new Form{Text=Loc.T("stock_title"),BackColor=Bg,ForeColor=Color.White,Size=new Size(1180,780),MinimumSize=new Size(900,600),StartPosition=FormStartPosition.CenterParent};
      var head=new Panel{Dock=DockStyle.Top,Height=104,BackColor=Panel,Padding=new Padding(18,12,18,10)};f.Controls.Add(head);var title=new Label{Text=Loc.T("stock_head"),ForeColor=Cyan,Font=new Font("Segoe UI Semibold",20),AutoSize=true,Location=new Point(18,12)};head.Controls.Add(title);var summary=new Label{ForeColor=Color.Silver,AutoSize=true,Location=new Point(390,23),Font=new Font("Segoe UI",10)};head.Controls.Add(summary);
      var mode=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Bg,ForeColor=Color.White,Location=new Point(18,58),Size=new Size(155,30)};mode.Items.AddRange(new object[]{Loc.T("stock_all_runes"),Loc.T("stock_normal"),Loc.T("stock_ancient")});mode.SelectedIndex=0;head.Controls.Add(mode);
      var kind=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,BackColor=Bg,ForeColor=Color.White,Location=new Point(183,58),Size=new Size(155,30)};kind.Items.AddRange(new object[]{Loc.T("stock_both"),Loc.T("stock_gems"),Loc.T("stock_grinds")});kind.SelectedIndex=0;head.Controls.Add(kind);
      var find=new TextBox{BackColor=Bg,ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle,Location=new Point(348,58),Size=new Size(230,30),Font=new Font("Segoe UI",11)};head.Controls.Add(find);
      var g=new BufferedGrid{Dock=DockStyle.Fill,BackgroundColor=Bg,BorderStyle=BorderStyle.None,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoGenerateColumns=false,SelectionMode=DataGridViewSelectionMode.FullRowSelect,MultiSelect=false,RowTemplate={Height=66},GridColor=Color.FromArgb(24,40,54)};g.EnableHeadersVisualStyles=false;g.ColumnHeadersHeight=44;g.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Teal,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",11),SelectionBackColor=Teal};g.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Bg,ForeColor=Color.White,Font=new Font("Segoe UI",11),SelectionBackColor=Color.FromArgb(26,70,83),SelectionForeColor=Color.White,Padding=new Padding(4)};g.AlternatingRowsDefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(10,18,29),ForeColor=Color.White,SelectionBackColor=Color.FromArgb(26,70,83)};
      g.Columns.Add(new DataGridViewImageColumn{Name="IconeSet",HeaderText="Set",DataPropertyName="IconeSet",Width=64,ImageLayout=DataGridViewImageCellLayout.Zoom});g.Columns.Add(new DataGridViewTextBoxColumn{Name="Set",HeaderText=Loc.T("stock_set"),DataPropertyName="Set",Width=210});g.Columns.Add(new DataGridViewTextBoxColumn{Name="Type",HeaderText=Loc.T("stock_type"),DataPropertyName="Type",Width=125});g.Columns.Add(new DataGridViewTextBoxColumn{Name="Stat",HeaderText=Loc.T("stock_stat"),DataPropertyName="Stat",Width=145});g.Columns.Add(new DataGridViewTextBoxColumn{Name="Qualite",HeaderText=Loc.T("stock_quality"),DataPropertyName="Qualite",Width=145});g.Columns.Add(new DataGridViewTextBoxColumn{Name="Rune",HeaderText=Loc.T("stock_rune"),DataPropertyName="Rune",Width=125});g.Columns.Add(new DataGridViewButtonColumn{Name="Vider",HeaderText=Loc.T("stock_qty"),DataPropertyName="Vider",AutoSizeMode=DataGridViewAutoSizeColumnMode.Fill,MinimumWidth=150,FlatStyle=FlatStyle.Flat,DefaultCellStyle=new DataGridViewCellStyle{BackColor=Color.FromArgb(138,35,44),ForeColor=Color.White,SelectionBackColor=Color.FromArgb(180,45,55),SelectionForeColor=Color.White,Font=new Font("Segoe UI Semibold",11),Alignment=DataGridViewContentAlignment.MiddleCenter}});
      g.CellFormatting+=(s,e)=>{var row=g.Rows[e.RowIndex].DataBoundItem as StockViewRow;if(row==null)return;if(e.ColumnIndex==4)e.CellStyle.ForeColor=(row.Source!=null&&row.Source.Grade>=5)?Color.FromArgb(255,126,20):Color.FromArgb(178,62,255);};
      f.Controls.Add(g);g.BringToFront();Action refresh=()=>{IEnumerable<CraftStock> q=RuneEngine.Stocks.Where(x=>x.Amount>0);if(mode.SelectedIndex==1)q=q.Where(x=>!x.Ancient);else if(mode.SelectedIndex==2)q=q.Where(x=>x.Ancient);if(kind.SelectedIndex==1)q=q.Where(x=>x.Type=="Gemme");else if(kind.SelectedIndex==2)q=q.Where(x=>x.Type=="Meule");string txt=find.Text.Trim();if(txt.Length>0)q=q.Where(x=>(x.Set+" "+x.Type+" "+x.Stat+" "+x.Quality).IndexOf(txt,StringComparison.OrdinalIgnoreCase)>=0);var rows=q.OrderBy(x=>x.Ancient).ThenBy(x=>x.Set).ThenBy(x=>x.Type).ThenBy(x=>x.Stat).ThenByDescending(x=>x.Grade).Select(x=>new StockViewRow{IconeSet=GetSetIcon(x.Set),Set=x.Set+(x.Ancient?" "+Loc.T("rune_ancient"):""),Type=x.Type=="Gemme"?Loc.T("type_gem"):Loc.T("type_grind"),Stat=x.Stat,Qualite=x.Quality,Rune=x.RuneType,Quantite=x.Amount,Source=x}).ToList();g.DataSource=rows;summary.Text=Loc.T("stock_summary",rows.Sum(x=>x.Quantite).ToString("N0"),rows.Count.ToString("N0"));};g.CellContentClick+=(s,e)=>{if(e.RowIndex<0||e.ColumnIndex!=6)return;var row=g.Rows[e.RowIndex].DataBoundItem as StockViewRow;if(row==null||row.Source==null)return;var answer=MessageBox.Show(Loc.T("stock_clear_q",row.Quantite,row.Type,row.Stat,row.Set),Loc.T("stock_clear_t"),MessageBoxButtons.YesNo,MessageBoxIcon.Question);if(answer!=DialogResult.Yes)return;row.Source.Amount=0;if(row.Source!=null&&row.Source.Type=="Gemme")RuneEngine.Calculate(all);RefreshAfterStockChange();refresh();status.Text=Loc.T("stock_cleared",row.Set,row.Type,row.Stat);};mode.SelectedIndexChanged+=(s,e)=>refresh();kind.SelectedIndexChanged+=(s,e)=>refresh();find.TextChanged+=(s,e)=>refresh();refresh();liveStockRefresh=refresh;f.FormClosed+=(s,e)=>{if(liveStockRefresh==refresh)liveStockRefresh=null;};f.ShowDialog(this);
    }
    void ShowPresetsLegacy(){
      var f=new Form{Text="Paramètres des presets",Icon=Icon,BackColor=Bg,ForeColor=Color.White,Size=new Size(1500,560),MinimumSize=new Size(1100,480),StartPosition=FormStartPosition.CenterParent};
      var g=new BufferedGrid{Dock=DockStyle.Fill,BackgroundColor=Bg,BorderStyle=BorderStyle.None,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoGenerateColumns=false,SelectionMode=DataGridViewSelectionMode.CellSelect,RowTemplate={Height=38},GridColor=Color.FromArgb(24,40,54)};g.EnableHeadersVisualStyles=false;g.ColumnHeadersHeight=42;g.ColumnHeadersDefaultCellStyle=new DataGridViewCellStyle{BackColor=Teal,ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10),SelectionBackColor=Teal};g.DefaultCellStyle=new DataGridViewCellStyle{BackColor=Bg,ForeColor=Color.White,SelectionBackColor=Color.FromArgb(26,70,83),SelectionForeColor=Color.White,Font=new Font("Segoe UI",10)};
      string[] stats={"HP%","Atk%","Def%","Spd","Res%","Acc%","CtR%","CtD%","HP+","Atk+","Def+"};g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Preset",ReadOnly=true,Width=100});foreach(string stat in stats){var c=new DataGridViewComboBoxColumn{HeaderText=stat,Width=62,FlatStyle=FlatStyle.Flat};c.Items.AddRange("Non","P1","P2","P3");g.Columns.Add(c);}foreach(var h in new[]{"Sets préférés","Sets acceptables","Slot 2","Slot 4","Slot 6"})g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=h,Width=h.StartsWith("Sets")?260:145});
      foreach(var p in RuneEngine.Presets){int row=g.Rows.Add();g.Rows[row].Cells[0].Value=p.Name;for(int i=0;i<stats.Length;i++)g.Rows[row].Cells[i+1].Value=PriorityLabel(p.W[stats[i]]);g.Rows[row].Cells[12].Value=string.Join(",",p.Preferred);g.Rows[row].Cells[13].Value=string.Join(",",p.Accepted);g.Rows[row].Cells[14].Value=string.Join(",",p.Main[2]);g.Rows[row].Cells[15].Value=string.Join(",",p.Main[4]);g.Rows[row].Cells[16].Value=string.Join(",",p.Main[6]);}
      var save=Button("ENREGISTRER ET RECALCULER",18,9,245,Cyan);var bottom=new Panel{Dock=DockStyle.Bottom,Height=52,BackColor=Panel};bottom.Controls.Add(save);f.Controls.Add(g);f.Controls.Add(bottom);save.Click+=(s,e)=>{for(int row=0;row<RuneEngine.Presets.Count;row++){var p=RuneEngine.Presets[row];for(int i=0;i<stats.Length;i++)p.W[stats[i]]=PriorityValue(Convert.ToString(g.Rows[row].Cells[i+1].Value));ReplaceSet(p.Preferred,Convert.ToString(g.Rows[row].Cells[12].Value));ReplaceSet(p.Accepted,Convert.ToString(g.Rows[row].Cells[13].Value));ReplaceSet(p.Main[2],Convert.ToString(g.Rows[row].Cells[14].Value));ReplaceSet(p.Main[4],Convert.ToString(g.Rows[row].Cells[15].Value));ReplaceSet(p.Main[6],Convert.ToString(g.Rows[row].Cells[16].Value));}ApplySettingsAndClose(f,Loc.T("presets_saved"));};f.ShowDialog(this);
    }
    void ShowCoefficients(){
      var f=new Form{Text=Loc.T("coeff_title"),Icon=Icon,BackColor=Bg,ForeColor=Color.White,ClientSize=new Size(540,430),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false};
      var bar=new Panel{Dock=DockStyle.Bottom,Height=64,BackColor=Bg};f.Controls.Add(bar);
      var panel=new TableLayoutPanel{Dock=DockStyle.Fill,Padding=new Padding(28,18,28,8),ColumnCount=2,RowCount=7,BackColor=Bg,GrowStyle=TableLayoutPanelGrowStyle.FixedSize};
      panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,58));
      panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,42));
      string[] names={Loc.T("coeff_p1"),Loc.T("coeff_p2"),Loc.T("coeff_p3"),Loc.T("coeff_main"),Loc.T("coeff_ok_set"),Loc.T("coeff_bad_set"),Loc.T("coeff_sim")};
      double[] values={RuneEngine.PoidsP1,RuneEngine.PoidsP2,RuneEngine.PoidsP3,RuneEngine.BonusStatPrincipale,RuneEngine.FacteurSetAcceptable,RuneEngine.FacteurSetExclu,RuneEngine.SeuilApres12};
      var boxes=new List<TextBox>();
      for(int i=0;i<names.Length;i++){
        panel.RowStyles.Add(new RowStyle(SizeType.Percent,100f/7f));
        panel.Controls.Add(new Label{Text=names[i],Dock=DockStyle.Fill,TextAlign=ContentAlignment.MiddleLeft,ForeColor=Color.Gainsboro,Font=new Font("Segoe UI",11),AutoSize=false,AutoEllipsis=true,Margin=new Padding(0,0,16,0)},0,i);
        var host=new Panel{Dock=DockStyle.Fill,BackColor=Panel,Margin=new Padding(0,8,0,8)};
        var box=new TextBox{Text=values[i].ToString("0.###",CultureInfo.InvariantCulture),BorderStyle=BorderStyle.None,BackColor=Panel,ForeColor=Color.White,Font=new Font("Segoe UI",11),TextAlign=HorizontalAlignment.Center,Height=20};
        Action place=()=>{box.Width=Math.Max(24,host.ClientSize.Width-16);box.Left=8;box.Top=Math.Max(0,(host.ClientSize.Height-box.Height)/2);};
        host.Resize+=(s,e)=>place();host.Click+=(s,e)=>box.Focus();
        host.Paint+=(s,e)=>{var r=host.ClientRectangle;r.Width--;r.Height--;using(var pen=new Pen(Color.FromArgb(70,95,115)))e.Graphics.DrawRectangle(pen,r);};
        host.Controls.Add(box);place();
        boxes.Add(box);panel.Controls.Add(host,1,i);
      }
      f.Controls.Add(panel);panel.BringToFront();
      var save=Button(Loc.T("save_recalc"),28,14,0,Cyan,36);bar.Controls.Add(save);
      save.Click+=(s,e)=>{var parsed=new double[7];for(int i=0;i<7;i++)if(!TryDouble(boxes[i].Text,out parsed[i])||parsed[i]<0){MessageBox.Show(Loc.T("coeff_bad",names[i]),Loc.T("coefficient"),MessageBoxButtons.OK,MessageBoxIcon.Warning);return;}RuneEngine.PoidsP1=parsed[0];RuneEngine.PoidsP2=parsed[1];RuneEngine.PoidsP3=parsed[2];RuneEngine.BonusStatPrincipale=parsed[3];RuneEngine.FacteurSetAcceptable=parsed[4];RuneEngine.FacteurSetExclu=parsed[5];RuneEngine.SeuilApres12=parsed[6];ApplySettingsAndClose(f,Loc.T("coeff_saved"));};
      f.ShowDialog(this);
    }
    // Auto-Keep : tableau set x stat. Une rune +12 est gardee (Keep) automatiquement des
    // qu'une de ses sous-stats atteint le seuil configure pour son set, meme si son
    // Potential est sous le seuil de vente. 0/vide = desactive. Remplace l'ancien
    // mecanisme fige (SPD 22/24 selon "set premium" en dur dans PremiumKeep) par une
    // grille editable par Jeremy, sur toutes les stats et tous les sets.
    static readonly string[] AutoKeepStats={"HP%","Atk%","Def%","Spd","Res%","Acc%","CtR%","CtD%","HP+","Atk+","Def+"};
    void ShowAutoKeepSettings(){
      var sets=RuneEngine.AutoKeepThresholds.Keys.OrderBy(x=>x,StringComparer.OrdinalIgnoreCase).ToList();
      var f=new Form{Text=Loc.T("autokeep_title"),Icon=Icon,BackColor=Bg,ForeColor=Color.White,Size=new Size(1180,680),MinimumSize=new Size(820,420),StartPosition=FormStartPosition.CenterParent};
      var info=new Label{Text=Loc.T("autokeep_info"),AutoSize=false,Height=40,Dock=DockStyle.Top,ForeColor=Color.Silver,Font=new Font("Segoe UI",9.5f),Padding=new Padding(14,10,14,4)};
      f.Controls.Add(info);
      var grid=new DataGridView{Dock=DockStyle.Fill,BackgroundColor=Bg,ForeColor=Color.White,GridColor=Color.FromArgb(60,70,85),BorderStyle=BorderStyle.None,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.AutoSize,EnableHeadersVisualStyles=false,SelectionMode=DataGridViewSelectionMode.CellSelect,RowTemplate={Height=40}};
      grid.DefaultCellStyle.BackColor=Panel;grid.DefaultCellStyle.ForeColor=Color.White;grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(60,90,120);grid.DefaultCellStyle.SelectionForeColor=Color.White;grid.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(30,42,58);grid.ColumnHeadersDefaultCellStyle.ForeColor=Color.White;grid.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI Semibold",9.5f);
      // Icone du set (meme source que la grille principale : assets/sets/<set>.png) au lieu
      // du nom en texte — le nom du set reste accessible via le Tag de la ligne + tooltip.
      var setCol=new DataGridViewImageColumn{HeaderText="Set",ReadOnly=true,FillWeight=90,ImageLayout=DataGridViewImageCellLayout.Zoom};grid.Columns.Add(setCol);
      foreach(var st in AutoKeepStats)grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=st,FillWeight=70});
      foreach(var setName in sets){
        var row=RuneEngine.AutoKeepThresholds[setName];
        var icon=GetSetIcon(setName);
        var values=new object[1+AutoKeepStats.Length];values[0]=(object)icon??DBNull.Value;
        for(int i=0;i<AutoKeepStats.Length;i++){double v;values[i+1]=row.TryGetValue(AutoKeepStats[i],out v)&&v>0?v.ToString(CultureInfo.InvariantCulture):"";}
        int rowIndex=grid.Rows.Add(values);grid.Rows[rowIndex].Tag=setName;grid.Rows[rowIndex].Cells[0].ToolTipText=setName;
      }
      f.Controls.Add(grid);grid.BringToFront();
      var bottom=new Panel{Dock=DockStyle.Bottom,Height=54,BackColor=Panel};f.Controls.Add(bottom);
      var cancel=Button(Loc.T("cancel_btn"),0,10,120,Color.FromArgb(120,90,90),34);cancel.Click+=(s,e)=>f.Close();bottom.Controls.Add(cancel);
      var save=Button(Loc.T("save"),0,10,150,Color.FromArgb(255,170,40),34);save.Click+=(s,e)=>{
        foreach(DataGridViewRow r in grid.Rows){string setName=Convert.ToString(r.Tag);Dictionary<string,double> row;if(!RuneEngine.AutoKeepThresholds.TryGetValue(setName,out row)){row=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);RuneEngine.AutoKeepThresholds[setName]=row;}for(int i=0;i<AutoKeepStats.Length;i++){string text=Convert.ToString(r.Cells[i+1].Value);double v;row[AutoKeepStats[i]]=(!string.IsNullOrWhiteSpace(text)&&TryDouble(text,out v))?v:0;}}
        SaveEngineSettings();if(all.Count>0)RuneEngine.Calculate(all);RefreshGrid();status.Text=Loc.T("autokeep_saved");f.Close();
      };bottom.Controls.Add(save);
      f.Load+=(s,e)=>{save.Location=new Point(bottom.ClientSize.Width-save.Width-14,10);save.Anchor=AnchorStyles.Top|AnchorStyles.Right;cancel.Location=new Point(save.Left-cancel.Width-8,10);cancel.Anchor=AnchorStyles.Top|AnchorStyles.Right;};
      f.ShowDialog(this);
    }
    static readonly string[] RefinementSetOrder={"Violent","Swift","Will","Despair"};
    static readonly int[] RefinementSlots={1,3,4,5,6};
    // Ordre de priorite GLOBAL (tous sets/slots confondus, pas le regroupement par set utilise
    // pour l'AFFICHAGE du Classement SPD) : du SPD max le plus faible au plus fort, et a
    // egalite priorise les slots 4 et 6. C'est cet ordre que la cible de refinement parcourt.
    // SPD max actuel par set+slot (cle "Set|Slot"), meme regle que la recherche "<set> slot <n>"
    // (aucune restriction). Reutilise par SpdPriorityOrder et par le badge de notification du
    // bouton CLASSEMENT SPD (detection d'un nouveau record apres montee de rune).
    Dictionary<string,double> CurrentSpdBestMap(){Dictionary<string,RuneRow> bestRune;return CurrentSpdBestMap(out bestRune);}
    Dictionary<string,double> CurrentSpdBestMap(out Dictionary<string,RuneRow> bestRune){
      var map=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);
      bestRune=new Dictionary<string,RuneRow>(StringComparer.OrdinalIgnoreCase);
      foreach(var setName in RefinementSetOrder)foreach(int slot in RefinementSlots){
        double best=0;bool any=false;RuneRow bestR=null;
        foreach(var r in all){if(!r.Set.Equals(setName,StringComparison.OrdinalIgnoreCase)||r.Slot!=slot)continue;double v=SpdBase(r);if(!any||v>best){best=v;any=true;bestR=r;}}
        string key=setName+"|"+slot;map[key]=any?best:0;bestRune[key]=bestR;
      }
      return map;
    }
    // Ameliorations detectees depuis la derniere consultation du Classement SPD (montee de
    // rune qui bat le record SPD d'un slot+set). Vide si premiere ouverture (rien a comparer).
    List<Tuple<string,int,double,double,RuneRow>> ComputeSpdImprovements(){
      var result=new List<Tuple<string,int,double,double,RuneRow>>();
      if(spdSeenBest.Count==0)return result;
      Dictionary<string,RuneRow> bestRune;var current=CurrentSpdBestMap(out bestRune);
      foreach(var setName in RefinementSetOrder)foreach(int slotN in RefinementSlots){
        string key=setName+"|"+slotN;double prev;double now=current[key];
        if(spdSeenBest.TryGetValue(key,out prev)&&now>prev)result.Add(Tuple.Create(setName,slotN,prev,now,bestRune[key]));
      }
      return result;
    }
    // Popup listant les slot+set ameliores depuis la derniere consultation, avec la rune
    // responsable de la nouvelle meilleure SPD (demande Jeremy : "me dire quel slot a ete
    // augmenté avec la rune en question").
    void ShowSpdImprovements(List<Tuple<string,int,double,double,RuneRow>> improvements){
      var f=new Form{Text=Loc.T("spd_improved_title"),Icon=Icon,BackColor=Bg,ForeColor=Color.White,Size=new Size(820,80+70*Math.Min(improvements.Count,6)+120),MinimumSize=new Size(560,260),StartPosition=FormStartPosition.CenterParent};
      var title=new Label{Text=improvements.Count==1?Loc.T("spd_improved_one"):Loc.T("spd_improved_many",improvements.Count),Dock=DockStyle.Top,Height=70,BackColor=Panel,ForeColor=Color.FromArgb(255,178,55),Font=new Font("Segoe UI Semibold",15),Padding=new Padding(18,9,4,4)};
      var g=new DataGridView{Dock=DockStyle.Fill,BackgroundColor=Bg,ForeColor=Color.White,GridColor=Color.FromArgb(60,70,85),BorderStyle=BorderStyle.None,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.AutoSize,EnableHeadersVisualStyles=false,SelectionMode=DataGridViewSelectionMode.CellSelect,RowTemplate={Height=40}};
      g.DefaultCellStyle.BackColor=Panel;g.DefaultCellStyle.ForeColor=Color.White;g.DefaultCellStyle.SelectionBackColor=Color.FromArgb(60,90,120);g.DefaultCellStyle.SelectionForeColor=Color.White;g.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(30,42,58);g.ColumnHeadersDefaultCellStyle.ForeColor=Color.White;g.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI Semibold",9.5f);
      g.Columns.Add(new DataGridViewImageColumn{HeaderText="Set",ReadOnly=true,FillWeight=45,ImageLayout=DataGridViewImageCellLayout.Zoom});
      g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Slot",FillWeight=40});
      g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("spd_old"),FillWeight=55});
      g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("spd_new"),FillWeight=55});
      g.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("spd_rune"),FillWeight=220});
      foreach(DataGridViewColumn c in g.Columns)c.SortMode=DataGridViewColumnSortMode.NotSortable;
      foreach(var imp in improvements.OrderBy(x=>Array.IndexOf(RefinementSetOrder,x.Item1)).ThenBy(x=>x.Item2)){
        var icon=GetSetIcon(imp.Item1);var r=imp.Item5;
        string subs=r!=null?"+"+r.Level+"  "+r.Grade+"★  "+string.Join(" | ",r.Subs.Select(x=>x.BaseDisplay)):"";
        int idx=g.Rows.Add((object)icon??DBNull.Value,"Slot "+imp.Item2,imp.Item3.ToString("0"),imp.Item4.ToString("0"),subs);
        g.Rows[idx].Cells[3].Style.ForeColor=Color.FromArgb(95,235,165);g.Rows[idx].Cells[0].ToolTipText=imp.Item1;
      }
      f.Controls.Add(g);f.Controls.Add(title);g.BringToFront();
      var bottom=new Panel{Dock=DockStyle.Bottom,Height=54,BackColor=Panel};f.Controls.Add(bottom);
      var close=Button(Loc.T("close"),0,10,120,Color.FromArgb(120,90,90),34);close.Click+=(s,e)=>f.Close();bottom.Controls.Add(close);
      f.Load+=(s,e)=>{close.Location=new Point(bottom.ClientSize.Width-close.Width-14,10);close.Anchor=AnchorStyles.Top|AnchorStyles.Right;};
      f.ShowDialog(this);
    }
    List<Tuple<string,int>> SpdPriorityOrder(){
      var map=CurrentSpdBestMap();
      var combos=new List<Tuple<string,int,double>>();
      foreach(var setName in RefinementSetOrder)foreach(int slot in RefinementSlots)combos.Add(Tuple.Create(setName,slot,map[setName+"|"+slot]));
      return combos.OrderBy(c=>c.Item3).ThenBy(c=>(c.Item2==4||c.Item2==6)?0:1).Select(c=>Tuple.Create(c.Item1,c.Item2)).ToList();
    }
    // Seuil "top 10% meilleures runes" (Potential Value, meme metrique que partout ailleurs
    // dans l'appli) — une rune AU-DESSUS de ce seuil est deja excellente : on garde le
    // refinement pour une rune plus faible qui en a davantage besoin (demande Jeremy).
    double Top10PercentPotentialThreshold(){
      var scores=all.Select(r=>r.Potential).OrderByDescending(x=>x).ToList();
      if(scores.Count==0)return double.MaxValue;
      int cutoff=Math.Max(0,(int)Math.Ceiling(scores.Count*0.10)-1);
      return scores[Math.Min(scores.Count-1,cutoff)];
    }
    // Stats principales autorisees pour un slot, d'apres la config des presets (p.Main[2/4/6]
    // = stats principales choisies par Jeremy pour ce slot, tous presets confondus). Slot 1/3/5
    // ont une stat principale fixee par le jeu (pas de config Main) donc pas de restriction ici.
    HashSet<string> AllowedMainStats(int slot){
      if(slot!=2&&slot!=4&&slot!=6)return null;
      var set=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach(var p in RuneEngine.Presets){HashSet<string> m;if(p.Main.TryGetValue(slot,out m))foreach(var stat in m)set.Add(stat);}
      return set;
    }
    // Candidats valables pour un slot+set donne (regles Jeremy) : LEGENDAIRE (grade 6
    // obligatoire), SPD reelle (>0, sinon le refinement ne garantit rien sur ce stat), pas de
    // stat plate en sous-stat, pas deja dans le top 10% de ses meilleures runes (Potential), et
    // stat principale liee a celles utilisees sur les presets pour ce slot (slot 1/3/5 : pas de
    // restriction, stat principale fixee par le jeu). excludeAncient : TRI REFINEMENT seulement
    // (Jeremy : antiques absentes du tri refinement, pas du tri reeval).
    IEnumerable<RuneRow> RefinementCandidates(string setName,int slot,double threshold,bool excludeAncient=false){
      var allowedMain=AllowedMainStats(slot);
      return all.Where(r=>r.Set.Equals(setName,StringComparison.OrdinalIgnoreCase)&&r.Slot==slot&&r.Grade==5&&(!excludeAncient||!r.Ancient)&&SpdBase(r)>0&&r.Potential<threshold&&!r.Subs.Any(x=>x.Stat=="HP+"||x.Stat=="Atk+"||x.Stat=="Def+")&&(allowedMain==null||allowedMain.Count==0||allowedMain.Contains(r.Main)));
    }
    // Cible de refinement recommandee : parcourt les slots/sets du moins bon SPD au meilleur
    // (SpdPriorityOrder). Le premier slot/set avec au moins un candidat valable gagne ; sinon on
    // passe au suivant de la liste de priorite ("si je n'ai pas de cible bah après c'est une
    // violent en 5 etc").
    RuneRow FindRefinementTarget(out string chosenSet,out int chosenSlot){
      chosenSet=null;chosenSlot=0;
      double threshold=Top10PercentPotentialThreshold();
      foreach(var combo in SpdPriorityOrder()){
        var candidates=RefinementCandidates(combo.Item1,combo.Item2,threshold,true).ToList();
        if(candidates.Count>0){chosenSet=combo.Item1;chosenSlot=combo.Item2;return candidates.OrderBy(x=>x.Potential).First();}
      }
      return null;
    }
    // Liste complete des cibles de refinement valables, dans l'ordre de priorite (moins bon SPD
    // en premier, egalite -> slot 4/6 avant), pour le bouton TRI REFINEMENT. A l'interieur d'un
    // meme slot+set, la plus mauvaise Potential Value d'abord.
    IEnumerable<RuneRow> RefinementTargetsInPriorityOrder(bool excludeAncient=true){
      double threshold=Top10PercentPotentialThreshold();
      var result=new List<RuneRow>();
      foreach(var combo in SpdPriorityOrder())result.AddRange(RefinementCandidates(combo.Item1,combo.Item2,threshold,excludeAncient).OrderBy(x=>x.Potential));
      return result;
    }
    // Innate reevaluable : stat prefix presente et differente de SPD. Une innate SPD ne se
    // reevalue pas (perd la SPD) ; une rune sans innate n'a rien a reevaluer. L'innate ne
    // peut pas apparaitre en sous-stat, donc ce filtre est independant du pool refinement.
    static bool HasReevalableInnate(RuneRow r){
      return !string.IsNullOrEmpty(r.Innate)&&!r.Innate.Equals("Spd",StringComparison.OrdinalIgnoreCase);
    }
    // TRI REEVAL : si le compte a au moins un lock_type 1 (marqueur "Reeval"), pool inchange
    // (lock + Potential <= 10, priorite SPD). Sinon meme liste que TRI REFINEMENT, restreinte
    // aux runes avec innate non-SPD. Lock type 2 ("rune spd") n'est pas touche.
    IEnumerable<RuneRow> ReevalTargetsInPriorityOrder(IEnumerable<RuneRow> baseQuery){
      bool hasReevalLock=all.Any(r=>r.Marker.IndexOf("Reeval",StringComparison.OrdinalIgnoreCase)>=0);
      if(!hasReevalLock)return RefinementTargetsInPriorityOrder(false).Where(HasReevalableInnate);
      var priority=SpdPriorityOrder();
      var rank=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);
      for(int i=0;i<priority.Count;i++){string key=priority[i].Item1+"|"+priority[i].Item2;if(!rank.ContainsKey(key))rank[key]=i;}
      Func<RuneRow,int> rankOf=r=>{int v;return rank.TryGetValue(r.Set+"|"+r.Slot,out v)?v:int.MaxValue;};
      return baseQuery.Where(r=>r.Marker.IndexOf("Reeval",StringComparison.OrdinalIgnoreCase)>=0&&r.Potential<=10).OrderBy(rankOf).ThenBy(r=>r.Potential).ThenBy(r=>r.Obtained);
    }
    // Rapport en lecture seule, aucune modification de rune/action ici (demande explicite de
    // Jeremy : "avant de changer quoi que ce soit fait moi un classement"). Une seule ligne
    // par set+slot (le SPD MAX trouve dans ce slot pour ce set — pas la liste de toutes les
    // runes du slot, juste la meilleure), triee du SPD max le plus faible au plus fort : la
    // ligne du haut = le slot/set ou meme la meilleure rune actuelle a le moins de SPD, donc
    // le plus urgent a ameliorer via le refinement.
    void ShowSpdRanking(){
      // Meme regle que la recherche "<set> slot <n>" (aucune restriction de grade/niveau/
      // gemme/antique/flat) — demande explicite de Jeremy pour que ce bouton donne EXACTEMENT
      // le meme resultat que ce qu'il peut verifier lui-meme a la main via la recherche. Les
      // exclusions (gemme, antique, flat) venaient de IsRefinementEligible/IsSpdRankEligible
      // et ont chacune cache a tort la vraie meilleure rune au moins une fois.
      var combos=new List<Tuple<string,int,double,RuneRow>>();
      foreach(var setName in RefinementSetOrder){
        foreach(int slot in RefinementSlots){
          RuneRow best=null;double bestSpd=-1;
          foreach(var r in all){
            if(!r.Set.Equals(setName,StringComparison.OrdinalIgnoreCase)||r.Slot!=slot)continue;
            double v=SpdBase(r);if(best==null||v>bestSpd){best=r;bestSpd=v;}
          }
          combos.Add(Tuple.Create(setName,slot,best==null?0:bestSpd,best));
        }
      }
      // Regroupe par set (lignes du meme set consecutives) — a l'interieur d'un set,
      // slots dans l'ordre 1, 3, 4, 5, 6 (pas de slot 2 : SPD en stat principale).
      var ordered=combos.OrderBy(c=>Array.IndexOf(RefinementSetOrder,c.Item1)).ThenBy(c=>Array.IndexOf(RefinementSlots,c.Item2)).ToList();
      // Ouverture = notification consultee : avant de remettre a zero, on montre quel(s)
      // slot+set ont ete ameliores depuis la derniere fois (rune montee qui bat le record SPD),
      // avec la rune en question — puis le nouvel etat SPD devient la reference (badge a 0).
      var improvements=ComputeSpdImprovements();
      spdSeenBest=CurrentSpdBestMap();RefreshSpdRankBadge();SaveSpdSeenBest();
      if(improvements.Count>0)ShowSpdImprovements(improvements);
      var f=new Form{Text=Loc.T("spd_title"),Icon=Icon,BackColor=Bg,ForeColor=Color.White,Size=new Size(980,700),MinimumSize=new Size(720,420),StartPosition=FormStartPosition.CenterParent};
      var info=new Label{Text=Loc.T("spd_info"),AutoSize=false,Height=40,Dock=DockStyle.Top,ForeColor=Color.Silver,Font=new Font("Segoe UI",9.5f),Padding=new Padding(14,10,14,4)};
      f.Controls.Add(info);
      var grid=new DataGridView{Dock=DockStyle.Fill,BackgroundColor=Bg,ForeColor=Color.White,GridColor=Color.FromArgb(60,70,85),BorderStyle=BorderStyle.None,ReadOnly=true,AllowUserToAddRows=false,AllowUserToDeleteRows=false,RowHeadersVisible=false,AutoSizeColumnsMode=DataGridViewAutoSizeColumnsMode.Fill,ColumnHeadersHeightSizeMode=DataGridViewColumnHeadersHeightSizeMode.AutoSize,EnableHeadersVisualStyles=false,SelectionMode=DataGridViewSelectionMode.CellSelect,RowTemplate={Height=36}};
      grid.DefaultCellStyle.BackColor=Panel;grid.DefaultCellStyle.ForeColor=Color.White;grid.DefaultCellStyle.SelectionBackColor=Color.FromArgb(60,90,120);grid.DefaultCellStyle.SelectionForeColor=Color.White;grid.ColumnHeadersDefaultCellStyle.BackColor=Color.FromArgb(30,42,58);grid.ColumnHeadersDefaultCellStyle.ForeColor=Color.White;grid.ColumnHeadersDefaultCellStyle.Font=new Font("Segoe UI Semibold",9.5f);
      grid.Columns.Add(new DataGridViewImageColumn{HeaderText="Set",ReadOnly=true,FillWeight=50,ImageLayout=DataGridViewImageCellLayout.Zoom});
      grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText="Slot",FillWeight=45});
      grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("spd_base"),FillWeight=80});
      grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("spd_grind"),FillWeight=90});
      grid.Columns.Add(new DataGridViewTextBoxColumn{HeaderText=Loc.T("spd_subs"),FillWeight=240});
      // Rapport en lecture seule a ordre voulu (priorite croissante) — pas de tri par clic
      // d'en-tete qui casserait cet ordre (probleme deja rencontre : tri texte par defaut).
      foreach(DataGridViewColumn c in grid.Columns)c.SortMode=DataGridViewColumnSortMode.NotSortable;
      foreach(var c in ordered){
        var icon=GetSetIcon(c.Item1);
        var r=c.Item4;
        string subs=r!=null?string.Join(" | ",r.Subs.Select(x=>x.BaseDisplay)):Loc.T("spd_none");
        string grind=r==null?"0":SpdWithGrind(r).ToString("0");
        int idx=grid.Rows.Add((object)icon??DBNull.Value,"Slot "+c.Item2,c.Item3.ToString("0"),grind,subs);
        grid.Rows[idx].Cells[0].ToolTipText=c.Item1;
      }
      f.Controls.Add(grid);grid.BringToFront();
      var bottom=new Panel{Dock=DockStyle.Bottom,Height=54,BackColor=Panel};f.Controls.Add(bottom);
      var close=Button(Loc.T("close"),0,10,120,Color.FromArgb(120,90,90),34);close.Click+=(s,e)=>f.Close();bottom.Controls.Add(close);
      f.Load+=(s,e)=>{close.Location=new Point(bottom.ClientSize.Width-close.Width-14,10);close.Anchor=AnchorStyles.Top|AnchorStyles.Right;};
      f.ShowDialog(this);
    }
    string PriorityLabel(double v){return v==1?"P1":v==2?"P2":v==3?"P3":"Non";}string PriorityDisplay(double v){return v==1?"P1":v==2?"P2":v==3?"P3":Loc.T("prio_none");}double PriorityValue(string s){return s=="P1"?1:s=="P2"?2:s=="P3"?3:0;}double ParsePresetFactor(string text){if(string.IsNullOrWhiteSpace(text))return 1;string raw=text.Trim();bool pct=raw.EndsWith("%");if(pct)raw=raw.TrimEnd('%');double v;if(!TryDouble(raw,out v))return 1;if(pct||v>3)v=v/100.0;return Math.Max(0,Math.Min(3,v));}void ReplaceSet(HashSet<string> target,string text){target.Clear();foreach(string s in (text??"").Split(',')){string v=s.Trim();if(v.Length>0)target.Add(v);}}bool TryDouble(string s,out double v){return double.TryParse(s.Replace(',','.'),NumberStyles.Any,CultureInfo.InvariantCulture,out v);}
    void ApplySettingsAndClose(Form f,string message){SaveEngineSettings();upgradeCaps.Clear();craftPotentials.Clear();SaveLiveChanges();if(all.Count>0)RuneEngine.Calculate(all);RefreshUpgradeBadge();FillFilters();RefreshGrid();status.Text=message;f.DialogResult=DialogResult.OK;f.Close();}
    void SaveEngineSettings(){try{var lines=new List<string>();lines.Add("COEFF\t"+string.Join("\t",new[]{RuneEngine.PoidsP1,RuneEngine.PoidsP2,RuneEngine.PoidsP3,RuneEngine.BonusStatPrincipale,RuneEngine.FacteurSetAcceptable,RuneEngine.FacteurSetExclu,RuneEngine.SeuilApres12}.Select(x=>x.ToString(CultureInfo.InvariantCulture))));lines.Add("SKILLHIDDEN\t"+string.Join(",",hiddenSkillTargetIds.OrderBy(x=>x)));string[] stats={"HP%","Atk%","Def%","Spd","Res%","Acc%","CtR%","CtD%","HP+","Atk+","Def+"};foreach(var p in RuneEngine.Presets)lines.Add("PRESET\t"+p.Name+"\t"+string.Join(",",stats.Select(x=>PriorityLabel(p.W[x])))+"\t"+string.Join(",",p.Preferred)+"\t"+string.Join(",",p.Accepted)+"\t"+string.Join(",",p.Main[2])+"\t"+string.Join(",",p.Main[4])+"\t"+string.Join(",",p.Main[6]));foreach(var kv in RuneEngine.AutoKeepThresholds)lines.Add("AUTOKEEP\t"+kv.Key+"\t"+string.Join(",",AutoKeepStats.Select(st=>{double v;return st+"="+(kv.Value.TryGetValue(st,out v)?v:0).ToString(CultureInfo.InvariantCulture);})));lines.Add("STATFACTOR\t"+string.Join(",",stats.Select(x=>{double v;return x+"="+(RuneEngine.StatGlobalFactor.TryGetValue(x,out v)?v:1.0).ToString(CultureInfo.InvariantCulture);})));lines.Add("LANG\t"+Loc.Lang);File.WriteAllLines(SettingsSavePath,lines);}catch(Exception ex){MessageBox.Show(Loc.T("save_settings_fail",ex.Message),"Rune Manager",MessageBoxButtons.OK,MessageBoxIcon.Error);}}
    void LoadEngineSettings(){if(!File.Exists(SettingsSavePath))return;try{string[] stats={"HP%","Atk%","Def%","Spd","Res%","Acc%","CtR%","CtD%","HP+","Atk+","Def+"};foreach(string line in File.ReadAllLines(SettingsSavePath)){string[] x=line.Split('\t');if(x[0]=="COEFF"&&x.Length>=8){double[] v=new double[x.Length-1];bool ok=true;for(int i=0;i<v.Length;i++)ok&=TryDouble(x[i+1],out v[i]);if(ok){RuneEngine.PoidsP1=v[0];RuneEngine.PoidsP2=v[1];RuneEngine.PoidsP3=v[2];RuneEngine.BonusStatPrincipale=v[3];RuneEngine.FacteurSetAcceptable=v[4];RuneEngine.FacteurSetExclu=v[5];RuneEngine.SeuilApres12=v.Length>=8?v[7]:v[6];}}else if(x[0]=="SKILLHIDDEN"&&x.Length>1){foreach(string raw in x[1].Split(',')){int id;if(int.TryParse(raw,out id)&&id>0)hiddenSkillTargetIds.Add(id);}}else if(x.Length>=8&&x[0]=="PRESET"){var p=RuneEngine.Presets.FirstOrDefault(z=>z.Name==x[1]);if(p==null)continue;string[] w=x[2].Split(',');for(int i=0;i<stats.Length&&i<w.Length;i++)p.W[stats[i]]=PriorityValue(w[i]);ReplaceSet(p.Preferred,x[3]);ReplaceSet(p.Accepted,x[4]);ReplaceSet(p.Main[2],x[5]);ReplaceSet(p.Main[4],x[6]);ReplaceSet(p.Main[6],x[7]);}else if(x[0]=="AUTOKEEP"&&x.Length>=3){Dictionary<string,double> row;if(!RuneEngine.AutoKeepThresholds.TryGetValue(x[1],out row)){row=new Dictionary<string,double>(StringComparer.OrdinalIgnoreCase);RuneEngine.AutoKeepThresholds[x[1]]=row;}foreach(string pair in x[2].Split(',')){int eq=pair.IndexOf('=');if(eq<=0)continue;string statName=pair.Substring(0,eq);double v;if(TryDouble(pair.Substring(eq+1),out v))row[statName]=v;}}else if(x[0]=="STATFACTOR"&&x.Length>=2){foreach(string pair in x[1].Split(',')){int eq=pair.IndexOf('=');if(eq<=0)continue;string statName=pair.Substring(0,eq);double v;if(TryDouble(pair.Substring(eq+1),out v))RuneEngine.StatGlobalFactor[statName]=v;}}else if(x[0]=="LANG"&&x.Length>=2){string l=(x[1]??"").Trim().ToLowerInvariant();if(l=="en"||l=="fr")Loc.Lang=l;}}}catch{}}
    void LoadRetentionSetting(){try{if(!File.Exists(RetentionSavePath))return;string raw=File.ReadAllText(RetentionSavePath).Trim();string[] x=raw.Split('\t');int limit;double value;if(x.Length>=2&&x[0]=="FIXED"&&TryDouble(x[1],out value)){RuneEngine.SeuilVenteFixe=true;RuneEngine.ValeurSeuilVenteFixe=Math.Max(0,value);RuneEngine.SeuilApres12=RuneEngine.ValeurSeuilVenteFixe;}else if(x.Length>=2&&x[0]=="DYNAMIC"&&int.TryParse(x[1],out limit)){RuneEngine.SeuilVenteFixe=false;RuneEngine.LimiteRunesConservees=Math.Max(100,Math.Min(10000,limit));}else if(int.TryParse(raw,out limit)){RuneEngine.SeuilVenteFixe=false;RuneEngine.LimiteRunesConservees=Math.Max(100,Math.Min(10000,limit));}
      // Champs supplémentaires "économie de mana" ajoutés après coup sur la même ligne
      // (MANA\t0/1\tmarge) — position variable donc on la cherche au lieu d'un index fixe.
      int manaIdx=Array.IndexOf(x,"MANA");double margeVal;if(manaIdx>=0&&manaIdx+2<x.Length){RuneEngine.ManaSaverMode=x[manaIdx+1]=="1";if(TryDouble(x[manaIdx+2],out margeVal))RuneEngine.MargePwrUpStrict=Math.Max(0,margeVal);}}catch{}}
    void SaveRetentionSetting(){try{string raw=(RuneEngine.SeuilVenteFixe?"FIXED\t"+RuneEngine.ValeurSeuilVenteFixe.ToString(CultureInfo.InvariantCulture):"DYNAMIC\t"+RuneEngine.LimiteRunesConservees.ToString(CultureInfo.InvariantCulture))+"\tMANA\t"+(RuneEngine.ManaSaverMode?"1":"0")+"\t"+RuneEngine.MargePwrUpStrict.ToString(CultureInfo.InvariantCulture);File.WriteAllText(RetentionSavePath,raw);}catch{}}
    void BuildAncientShineEdges(Bitmap bmp){
      int w=bmp.Width,h=bmp.Height;
      shineMapW=w; shineMapH=h;
      var op=new bool[w,h]; var orange=new bool[w,h];
      int sx=0,sy=0,sn=0;
      for(int y=0;y<h;y++)
        for(int x=0;x<w;x++){
          Color c=bmp.GetPixel(x,y);
          if(c.A<40)continue;
          op[x,y]=true; sx+=x; sy+=y; sn++;
          orange[x,y]=c.R>=160&&c.R>=c.G+20&&c.B<=110;
        }
      float cx=sn>0?sx/(float)sn:w*0.5f, cy=sn>0?sy/(float)sn:h*0.46f;
      var outer=new List<Point>(); var inner=new List<Point>();
      var oAng=new List<float>(); var iAng=new List<float>();
      for(int y=1;y<h-1;y++)
        for(int x=1;x<w-1;x++){
          if(!op[x,y])continue;
          bool edge=!op[x-1,y]||!op[x+1,y]||!op[x,y-1]||!op[x,y+1];
          bool nearOrange=orange[x-1,y]||orange[x+1,y]||orange[x,y-1]||orange[x,y+1]
            ||orange[x-1,y-1]||orange[x+1,y-1]||orange[x-1,y+1]||orange[x+1,y+1];
          bool lip=!orange[x,y]&&nearOrange;
          float ang=(float)(Math.Atan2(y-cy,x-cx)*180.0/Math.PI); if(ang<0)ang+=360f;
          if(edge){outer.Add(new Point(x,y)); oAng.Add(ang);}
          else if(lip){inner.Add(new Point(x,y)); iAng.Add(ang);}
        }
      shineOuterPts=outer.ToArray(); shineOuterAng=oAng.ToArray();
      shineInnerPts=inner.ToArray(); shineInnerAng=iAng.ToArray();
    }
    static void MeasureCroquisIcons(Bitmap bmp,out RectangleF opaque,out RectangleF setIcon){
      int w=bmp.Width,h=bmp.Height;
      int ox0=w,oy0=h,ox1=-1,oy1=-1;
      var orange=new bool[w,h];
      for(int y=0;y<h;y++)
        for(int x=0;x<w;x++){
          Color c=bmp.GetPixel(x,y);
          bool glow=c.A>=40&&c.R<45&&c.G<45&&c.B<45;
          if(c.A>=40&&!glow){if(x<ox0)ox0=x;if(y<oy0)oy0=y;if(x>ox1)ox1=x;if(y>oy1)oy1=y;}
          orange[x,y]=c.A>=80&&c.R>=185&&c.R>=c.G+18&&c.B<=100&&c.R>=c.B+40;
        }
      opaque=ox1>=ox0?new RectangleF(ox0,oy0,ox1-ox0+1,oy1-oy0+1):new RectangleF(0,0,w,h);
      var seen=new bool[w,h];
      int bestN=0;
      var comps=new List<int[]>();
      int[] dx={1,-1,0,0},dy={0,0,1,-1};
      for(int y=0;y<h;y++)
        for(int x=0;x<w;x++){
          if(!orange[x,y]||seen[x,y])continue;
          int n=0,x0=x,y0=y,x1=x,y1=y;
          var st=new Stack<Point>();
          st.Push(new Point(x,y));seen[x,y]=true;
          while(st.Count>0){
            var p=st.Pop();n++;
            if(p.X<x0)x0=p.X;if(p.Y<y0)y0=p.Y;if(p.X>x1)x1=p.X;if(p.Y>y1)y1=p.Y;
            for(int k=0;k<4;k++){
              int nx=p.X+dx[k],ny=p.Y+dy[k];
              if(nx<0||ny<0||nx>=w||ny>=h||seen[nx,ny]||!orange[nx,ny])continue;
              seen[nx,ny]=true;st.Push(new Point(nx,ny));
            }
          }
          comps.Add(new[]{n,x0,y0,x1,y1});
          if(n>bestN)bestN=n;
        }
      int sx0=w,sy0=h,sx1=-1,sy1=-1;bool any=false;
      int minKeep=Math.Max(8,(int)(bestN*0.22));
      foreach(var c in comps){
        if(c[0]<minKeep)continue;
        any=true;
        if(c[1]<sx0)sx0=c[1];if(c[2]<sy0)sy0=c[2];if(c[3]>sx1)sx1=c[3];if(c[4]>sy1)sy1=c[4];
      }
      setIcon=any?new RectangleF(sx0,sy0,sx1-sx0+1,sy1-sy0+1):opaque;
    }
    static Bitmap BuildAncientPulseMask(Bitmap baked){
      int w=baked.Width,h=baked.Height,n=w*h;
      var data=baked.LockBits(new Rectangle(0,0,w,h),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
      int stride=data.Stride,bytes=Math.Abs(stride)*h;var src=new byte[bytes];
      Marshal.Copy(data.Scan0,src,0,bytes);baked.UnlockBits(data);
      var stone=new bool[n];
      var glyph=new bool[n];
      for(int y=0;y<h;y++){
        int row=y*stride;
        for(int x=0;x<w;x++){
          int i=row+x*4,idx=y*w+x,a=src[i+3];
          if(a<40)continue;
          int b=src[i],g=src[i+1],r=src[i+2];
          if(r<45&&g<45&&b<45)continue;
          stone[idx]=true;
          int mx=Math.Max(r,Math.Max(g,b)),mn=Math.Min(r,Math.Min(g,b));
          if(mx-mn>=28&&mx>=90)glyph[idx]=true;
        }
      }
      var pulse=new Bitmap(w,h,PixelFormat.Format32bppArgb);
      var pd=pulse.LockBits(new Rectangle(0,0,w,h),ImageLockMode.WriteOnly,PixelFormat.Format32bppArgb);
      var dst=new byte[Math.Abs(pd.Stride)*h];
      for(int y=0;y<h;y++){
        int rs=y*stride,rd=y*pd.Stride;
        for(int x=0;x<w;x++){
          int idx=y*w+x;
          if(!stone[idx]||glyph[idx])continue;
          int si=rs+x*4;
          int di=rd+x*4;dst[di]=src[si];dst[di+1]=src[si+1];dst[di+2]=src[si+2];dst[di+3]=src[si+3];
        }
      }
      Marshal.Copy(dst,0,pd.Scan0,dst.Length);pulse.UnlockBits(pd);
      return pulse;
    }
    static float AncientPulseAmt(float t){
      if(t<0.22f)return t/0.22f;
      if(t<0.40f)return 1f;
      if(t<0.64f)return 1f-(t-0.40f)/0.24f;
      return 0f;
    }
    void DrawAncientUnderGlow(Graphics g,Image preview,Rectangle dest){
      if(preview==null)return;
      g.SmoothingMode=SmoothingMode.None;
      g.InterpolationMode=InterpolationMode.NearestNeighbor;
      g.PixelOffsetMode=PixelOffsetMode.Half;
      const int px=1;
      using(var ia=new ImageAttributes()){
        ia.SetColorMatrix(new ColorMatrix(new float[][]{
          new float[]{0,0,0,0,0},
          new float[]{0,0,0,0,0},
          new float[]{0,0,0,0,0},
          new float[]{0,0,0,0.55f,0},
          new float[]{1,1,1,0,1}
        }));
        for(int oy=-px;oy<=px;oy++)
          for(int ox=-px;ox<=px;ox++){
            if(ox==0&&oy==0)continue;
            g.DrawImage(preview,new Rectangle(dest.X+ox,dest.Y+oy,dest.Width,dest.Height),0,0,preview.Width,preview.Height,GraphicsUnit.Pixel,ia);
          }
      }
      g.SmoothingMode=SmoothingMode.AntiAlias;
      g.InterpolationMode=InterpolationMode.HighQualityBicubic;
      g.PixelOffsetMode=PixelOffsetMode.HighQuality;
    }
    void DrawAncientShine(Graphics g,Image body,RectangleF dest){
      g.SmoothingMode=SmoothingMode.AntiAlias;
      float pulse=AncientPulseAmt(ancientPulseT);
      if(pulse<=0.02f||body==null)return;
      float a=0.32f*pulse;
      using(var ia=new ImageAttributes()){
        ia.SetColorMatrix(new ColorMatrix(new float[][]{
          new float[]{0.42f,0,0,0,0},
          new float[]{0,0.42f,0,0,0},
          new float[]{0,0,0.42f,0,0},
          new float[]{0,0,0,a,0},
          new float[]{0.58f,0.56f,0.50f,0,1}
        }));
        var d=Rectangle.Round(dest);
        g.DrawImage(body,d,0,0,body.Width,body.Height,GraphicsUnit.Pixel,ia);
      }
    }
    void ClosingWithSave(object sender,FormClosingEventArgs e){ancientShineTimer.Stop();updateCheckTimer.Stop();SaveRetentionSetting();SaveEngineSettings();SaveSpdSeenBest();SaveRuneChoiceSeen();if(!SaveStock()||!SaveLiveChanges())e.Cancel=true;}
    bool SaveStock(){try{var lines=RuneEngine.Stocks.Select(x=>(x.Ancient?"1":"0")+"\t"+x.Type+"\t"+x.Set+"\t"+x.Stat+"\t"+x.Grade+"\t"+x.Amount);File.WriteAllLines(StockSavePath,lines);return true;}catch(Exception ex){MessageBox.Show(Loc.T("save_stock_fail",ex.Message),"Rune Manager",MessageBoxButtons.OK,MessageBoxIcon.Error);return false;}}
    void LoadSavedStock(string jsonPath){if(!File.Exists(StockSavePath))return;try{
      // Un export plus récent contient le stock réel du compte et ne doit jamais
      // être écrasé par une ancienne sauvegarde locale (notamment des zéros).
      if(File.Exists(jsonPath)&&File.GetLastWriteTimeUtc(StockSavePath)<=File.GetLastWriteTimeUtc(jsonPath))return;
      var saved=new Dictionary<string,int>(StringComparer.OrdinalIgnoreCase);var extra=new List<string[]>();foreach(string line in File.ReadAllLines(StockSavePath)){string[] p=line.Split('\t');int grade,amount;if(p.Length==6&&int.TryParse(p[4],out grade)&&int.TryParse(p[5],out amount)){saved[p[0]+"|"+p[1]+"|"+p[2]+"|"+p[3]+"|"+grade]=amount;extra.Add(p);}}foreach(var x in RuneEngine.Stocks){string key=(x.Ancient?"1":"0")+"|"+x.Type+"|"+x.Set+"|"+x.Stat+"|"+x.Grade;int amount;if(saved.TryGetValue(key,out amount))x.Amount=amount;}var known=new HashSet<string>(RuneEngine.Stocks.Select(x=>(x.Ancient?"1":"0")+"|"+x.Type+"|"+x.Set+"|"+x.Stat+"|"+x.Grade),StringComparer.OrdinalIgnoreCase);foreach(var p in extra){string key=p[0]+"|"+p[1]+"|"+p[2]+"|"+p[3]+"|"+p[4];int grade,amount;if(known.Contains(key)||!int.TryParse(p[4],out grade)||!int.TryParse(p[5],out amount)||amount<=0)continue;RuneEngine.Stocks.Add(new CraftStock{Ancient=p[0]=="1",Type=p[1],Set=p[2],Stat=p[3],Grade=grade,Amount=amount});known.Add(key);}}catch(Exception ex){status.Text="Stock sauvegardé non chargé : "+ex.Message;}}
    void LoadLiveChanges(string jsonPath){liveSavedEvents.Clear();if(!File.Exists(LiveSavePath)||File.GetLastWriteTimeUtc(LiveSavePath)<=File.GetLastWriteTimeUtc(jsonPath))return;try{foreach(string line in File.ReadAllLines(LiveSavePath)){if(string.IsNullOrWhiteSpace(line))continue;bool worldBossOrder=line.IndexOf("\"command\":\"BattleWorldBossStart_v2\"",StringComparison.OrdinalIgnoreCase)>=0&&line.IndexOf("\"unit_id_list\"",StringComparison.OrdinalIgnoreCase)>=0;int protectedCount=RuneEngine.ApplyLiveDeckProtection(all,line);int markerCount=RuneEngine.ApplyLiveRuneMarker(all,line);string message=RuneEngine.ApplyLiveEvent(all,line);if(worldBossOrder||protectedCount>0||markerCount>0||message.Length>0)liveSavedEvents.Add(line);}}catch(Exception ex){status.Text="Changements automatiques non restaurés : "+ex.Message;}}
    void LoadUpgradeCaps(string jsonPath){upgradeCaps.Clear();craftPotentials.Clear();try{if(File.Exists(CapsSavePath)&&File.GetLastWriteTimeUtc(CapsSavePath)>File.GetLastWriteTimeUtc(jsonPath))foreach(string line in File.ReadAllLines(CapsSavePath)){string[] p=line.Split('\t');long id;double cap;if(p.Length==2&&long.TryParse(p[0],out id)&&double.TryParse(p[1],NumberStyles.Any,CultureInfo.InvariantCulture,out cap))upgradeCaps[id]=cap;}if(File.Exists(CraftPotentialSavePath)&&File.GetLastWriteTimeUtc(CraftPotentialSavePath)>File.GetLastWriteTimeUtc(jsonPath))foreach(string line in File.ReadAllLines(CraftPotentialSavePath)){string[] p=line.Split('\t');long id;double value;if(p.Length==2&&long.TryParse(p[0],out id)&&double.TryParse(p[1],NumberStyles.Any,CultureInfo.InvariantCulture,out value))craftPotentials[id]=value;}foreach(var rune in all){double value;if(craftPotentials.TryGetValue(rune.Id,out value))RuneEngine.PreserveAfterCraft(rune,value);else if(upgradeCaps.TryGetValue(rune.Id,out value))RuneEngine.CapAfterUpgrade(rune,value);}}catch(Exception ex){status.Text="Potential sauvegardé non restauré : "+ex.Message;}}
    bool SaveLiveChanges(){try{File.WriteAllLines(LiveSavePath,liveSavedEvents);File.WriteAllLines(CapsSavePath,upgradeCaps.Select(x=>x.Key+"\t"+x.Value.ToString(CultureInfo.InvariantCulture)));File.WriteAllLines(CraftPotentialSavePath,craftPotentials.Select(x=>x.Key+"\t"+x.Value.ToString(CultureInfo.InvariantCulture)));return true;}catch(Exception ex){MessageBox.Show("Sauvegarde des changements automatiques impossible : "+ex.Message,"Rune Manager",MessageBoxButtons.OK,MessageBoxIcon.Error);return false;}}
  }
}
