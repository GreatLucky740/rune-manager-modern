using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace RuneManagerModern {
  sealed partial class MainForm {
    Panel leftNav, navHeader, navFooter, filterBar, currentToolHost, contentHost, centerPanel, rightPanel, navRail;
    TableLayoutPanel shellRoot;
    FlowLayoutPanel navFlow;
    Form currentTool;
    Button currentToolButton, navPinButton;
    Label navSortLbl, navToolsLbl, navAppLbl, navKeepLbl;
    Timer navShowTimer, navHideTimer, navWatchTimer, navArrowTimer;
    bool navPinned, navOpen;
    float navArrowT;
    const int NavFullW=252, NavRailW=32;
    readonly Color NavIdle=Color.FromArgb(14,22,34), NavHover=Color.FromArgb(28,44,64), NavActive=Color.FromArgb(18,56,74);

    Button NavItem(string text,Color accent){
      var b=new Button{Text=text,AutoSize=false,Size=new Size(228,40),Margin=new Padding(8,2,8,2),FlatStyle=FlatStyle.Flat,BackColor=NavIdle,ForeColor=Color.FromArgb(205,216,228),Font=new Font("Segoe UI Semibold",9.5f),Cursor=Cursors.Hand,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(12,0,12,0),UseMnemonic=false,AutoEllipsis=false,UseCompatibleTextRendering=true};
      b.FlatAppearance.BorderSize=0;b.FlatAppearance.BorderColor=accent;b.FlatAppearance.MouseOverBackColor=NavHover;b.FlatAppearance.MouseDownBackColor=NavActive;
      b.Paint+=PaintNavItem;
      return b;
    }
    Label NavSection(string key){
      return new Label{Text=Loc.T(key),AutoSize=false,Size=new Size(228,22),Margin=new Padding(14,12,8,2),ForeColor=Color.FromArgb(108,138,158),Font=new Font("Segoe UI Semibold",8f),TextAlign=ContentAlignment.MiddleLeft};
    }
    void PaintNavItem(object sender,PaintEventArgs e){
      var b=sender as Button;if(b==null)return;
      Color accent=b.FlatAppearance.BorderColor;
      if(accent.IsEmpty||accent.A==0)return;
      bool selected=b==currentToolButton||b==ViewModeButton();
      if(!selected&&accent.ToArgb()==NavIdle.ToArgb())return;
      int pad=Math.Max(2,b.Height/8);
      using(var brush=new SolidBrush(selected?Cyan:accent))e.Graphics.FillRectangle(brush,0,pad,3,Math.Max(4,b.Height-pad*2));
    }
    Button ViewModeButton(){
      if(viewMode=="obtained")return obtButton;
      if(viewMode=="upgrade")return improveButton;
      if(viewMode=="reeval")return reevalButton;
      if(viewMode=="refinement")return refinementButton;
      if(viewMode=="potential"||viewMode=="normal")return potButton;
      return potButton;
    }
    void PaintNavSelection(){
      Button[] items={importButton,potButton,obtButton,improveButton,reevalButton,refinementButton,presetButton,coefficientButton,autoKeepButton,spdRankButton,skillButton,rtaButton,codesButton,retentionButton};
      Button view=ViewModeButton();
      for(int i=0;i<items.Length;i++){
        Button b=items[i];if(b==null)continue;
        bool on=b==view||b==currentToolButton;
        b.BackColor=on?NavActive:NavIdle;
        b.ForeColor=on?Color.White:Color.FromArgb(205,216,228);
        b.Invalidate();
      }
    }
    void ShowRuneList(){
      if(centerPanel!=null&&!centerPanel.Visible)CloseTool();
      else PaintNavSelection();
    }
    bool ToggleOffTool(Button b){
      if(b!=null&&currentToolButton==b&&currentTool!=null){CloseTool();return true;}
      return false;
    }
    void OpenToolFrom(Button b,Form f,int preferredWidth,bool takeMain){
      if(ToggleOffTool(b))return;
      currentToolButton=b;
      PaintNavSelection();
      OpenTool(f,preferredWidth,takeMain);
    }
    void OpenTool(Form f,int preferredWidth,bool takeMain){
      CloseToolKeepButton();
      if(f==null||rightPanel==null||centerPanel==null)return;
      string title=f.Text??"";
      f.TopLevel=false;f.FormBorderStyle=FormBorderStyle.None;f.ControlBox=false;f.MinimizeBox=false;f.MaximizeBox=false;
      f.MinimumSize=Size.Empty;f.MaximumSize=Size.Empty;f.Dock=DockStyle.Fill;f.Visible=true;
      currentToolHost=new Panel{Dock=DockStyle.Fill,BackColor=Bg};
      var bar=new Panel{Dock=DockStyle.Top,Height=34,BackColor=Color.FromArgb(12,20,32)};
      var titleLbl=new Label{Text=title,Dock=DockStyle.Fill,ForeColor=Color.Gainsboro,Font=new Font("Segoe UI Semibold",10f),TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(12,0,0,0)};
      var closeLbl=new Label{Text="✕",Dock=DockStyle.Right,Width=38,ForeColor=Color.Silver,Font=new Font("Segoe UI Semibold",12f),TextAlign=ContentAlignment.MiddleCenter,Cursor=Cursors.Hand};
      closeLbl.Click+=(s,e)=>CloseTool();
      bar.Controls.Add(titleLbl);bar.Controls.Add(closeLbl);
      currentToolHost.Controls.Add(f);currentToolHost.Controls.Add(bar);
      rightPanel.Controls.Add(currentToolHost);
      currentTool=f;
      f.FormClosed+=ToolFormClosed;
      if(takeMain){
        centerPanel.Visible=false;
        rightPanel.Dock=DockStyle.Fill;
        rightPanel.Visible=true;
      }else{
        centerPanel.Visible=true;
        rightPanel.Dock=DockStyle.Right;
        int avail=contentHost!=null?contentHost.ClientSize.Width:ClientSize.Width;
        if(avail<80)avail=Math.Max(900,ClientSize.Width-(navPinned?NavFullW:NavRailW));
        int keep=Math.Min(420,Math.Max(180,avail/3));
        int toolW=preferredWidth;
        if(toolW>avail-keep)toolW=avail-keep;
        if(toolW<320)toolW=Math.Min(avail,Math.Max(320,avail/2));
        if(toolW<1)toolW=1;
        if(toolW>=avail)toolW=Math.Max(1,avail-8);
        rightPanel.Visible=true;
        rightPanel.Width=toolW;
      }
    }
    void HideRightPanel(){
      if(rightPanel==null||rightPanel.IsDisposed)return;
      rightPanel.Controls.Clear();
      rightPanel.Dock=DockStyle.Right;
      rightPanel.Width=0;
      rightPanel.Visible=false;
      if(centerPanel!=null&&!centerPanel.IsDisposed)centerPanel.Visible=true;
    }
    void ToolFormClosed(object sender,FormClosedEventArgs e){
      var f=sender as Form;
      if(f!=null)f.FormClosed-=ToolFormClosed;
      if(currentTool==f)currentTool=null;
      if(currentToolHost!=null){
        if(!currentToolHost.IsDisposed)currentToolHost.Dispose();
        currentToolHost=null;
      }
      currentToolButton=null;
      HideRightPanel();
      PaintNavSelection();
    }
    void CloseToolKeepButton(){
      if(currentTool!=null&&!currentTool.IsDisposed){
        currentTool.FormClosed-=ToolFormClosed;
        try{currentTool.Close();}catch{}
        try{if(!currentTool.IsDisposed)currentTool.Dispose();}catch{}
      }
      currentTool=null;
      if(currentToolHost!=null){
        if(!currentToolHost.IsDisposed)currentToolHost.Dispose();
        currentToolHost=null;
      }
      HideRightPanel();
    }
    void CloseTool(){
      CloseToolKeepButton();
      currentToolButton=null;
      PaintNavSelection();
    }
    int navBtnH=40,navIconS=32,navGap=8,navLangH=28;
    Font scaleBtnFont,scaleSecFont,scaleTitleFont,scaleCountFont,scaleTinyFont;
    void AssignFont(Control c,Font f){if(c==null||f==null||c.Font==f)return;c.Font=f;}
    void PlaceNavBadges(){
      int count=Math.Max(16,Math.Min(30,navBtnH-6));
      Size countSz=new Size(count,count);
      Size newSz=new Size(Math.Max(34,count+16),Math.Max(16,navBtnH-4));
      if(improveBadge!=null)improveBadge.Size=improveBadge.UseNewTag?newSz:countSz;
      if(spdRankBadge!=null)spdRankBadge.Size=countSz;
      if(skillBadge!=null)skillBadge.Size=countSz;
      if(codesBadge!=null)codesBadge.Size=countSz;
      if(updateBadge!=null)updateBadge.Size=countSz;
      PlaceBadgeRight(improveBadge,improveButton,4);
      PlaceBadgeRight(spdRankBadge,spdRankButton,4);
      PlaceBadgeRight(skillBadge,skillButton,4);
      PlaceBadgeRight(codesBadge,codesButton,4);
      PlaceBadgeRight(updateBadge,updateButton,4);
      int wideH=Math.Max(16,navBtnH-6);
      int wideW=Math.Max(38,(int)(wideH*1.9));
      if(refinementButton!=null&&refinementBadge!=null){
        refinementBadge.Size=new Size(wideW,wideH);
        refinementBadge.Location=new Point(Math.Max(4,refinementButton.Width-refinementBadge.Width-6),Math.Max(1,(refinementButton.Height-refinementBadge.Height)/2));
      }
      if(reevalButton!=null&&reappNormalBadge!=null&&reappAncientBadge!=null){
        reappAncientBadge.Size=new Size(wideW,wideH);
        reappNormalBadge.Size=new Size(wideW,wideH);
        int y=Math.Max(1,(reevalButton.Height-reappNormalBadge.Height)/2);
        reappAncientBadge.Location=new Point(Math.Max(4,reevalButton.Width-wideW-4),y);
        reappNormalBadge.Location=new Point(Math.Max(4,reevalButton.Width-wideW*2-8),y);
      }
      int side=Math.Max(8,navBtnH/5);
      if(reevalButton!=null)reevalButton.Padding=new Padding(side,0,wideW*2+12,0);
      if(refinementButton!=null)refinementButton.Padding=new Padding(side,0,wideW+10,0);
      if(improveButton!=null)improveButton.Padding=new Padding(side,0,(improveBadge!=null&&improveBadge.Visible?improveBadge.Width:0)+8,0);
    }
    void PlaceBadgeRight(Control badge,Control host,int pad){
      if(badge==null||host==null)return;
      badge.Location=new Point(Math.Max(4,host.Width-badge.Width-pad),Math.Max(2,(host.Height-badge.Height)/2));
    }
    bool wrapBarBusy;
    void FitWrapBarApply(FlowLayoutPanel bar,int h){
      if(bar==null||bar.IsDisposed)return;
      if(bar.ClientSize.Width<80)return;
      wrapBarBusy=true;
      try{
        if(bar.MinimumSize.Height<62)bar.MinimumSize=new Size(0,62);
        if(bar.Height<62||Math.Abs(bar.Height-h)>2)bar.Height=h;
      }finally{wrapBarBusy=false;}
    }
    void FitWrapBar(FlowLayoutPanel bar){
      if(bar==null||bar.IsDisposed||wrapBarBusy)return;
      if(!bar.IsHandleCreated)return;
      int cw=bar.ClientSize.Width;
      if(cw<80)return;
      wrapBarBusy=true;
      try{
        bar.WrapContents=true;
        Size pref=bar.GetPreferredSize(new Size(cw,0));
        int bottom=bar.Padding.Top;
        foreach(Control c in bar.Controls){
          if(c==null||c.IsDisposed||!c.Visible)continue;
          int b=c.Bottom+c.Margin.Bottom;
          if(b>bottom)bottom=b;
        }
        int h=pref.Height;
        int fromKids=bottom+bar.Padding.Bottom+4;
        if(fromKids>h)h=fromKids;
        if(h<62)h=62;
        if(h>140)h=140;
        if(bar.Height>=62&&Math.Abs(bar.Height-h)<=2)return;
        int want=h;
        bar.BeginInvoke((MethodInvoker)delegate{FitWrapBarApply(bar,want);});
      }finally{wrapBarBusy=false;}
    }
    void AttachWrapBar(FlowLayoutPanel bar){
      if(bar==null)return;
      bar.WrapContents=true;
      bar.AutoScroll=false;
      bar.MinimumSize=new Size(0,62);
      if(bar.Height<62)bar.Height=62;
      if(bar.Padding.All==0)bar.Padding=new Padding(8,10,8,8);
      LayoutEventHandler onLayout=(s,e)=>FitWrapBar(bar);
      EventHandler onFit=(s,e)=>FitWrapBar(bar);
      bar.Layout+=onLayout;
      bar.HandleCreated+=onFit;
      bar.ParentChanged+=(s,e)=>{
        Control p=bar.Parent;
        if(p==null)return;
        p.SizeChanged-=onFit;
        p.SizeChanged+=onFit;
        Form hostForm=p as Form;
        if(hostForm==null)hostForm=p.FindForm();
        if(hostForm!=null){
          hostForm.Shown-=onFit;
          hostForm.Shown+=onFit;
          hostForm.SizeChanged-=onFit;
          hostForm.SizeChanged+=onFit;
        }
      };
    }
    void CompactNavChrome(){ScaleNavToFit();}
    int NavFlowNeed(int btnH,int btnMV,int secH,int secMV){return 13*(btnH+btnMV)+4*(secH+secMV)+48;}
    int NavTextWidth(Button b){
      if(b==null)return 120;
      return Math.Max(40,b.Width-b.Padding.Left-b.Padding.Right-10);
    }
    int NavTextPx(string s,Font font){
      if(string.IsNullOrEmpty(s)||font==null)return 0;
      try{return TextRenderer.MeasureText(s,font,new Size(int.MaxValue,int.MaxValue),TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix).Width;}catch{return Math.Max(8,s.Length*6);}
    }
    int NavLineH(Font font){
      if(font==null)return 12;
      try{
        int h=TextRenderer.MeasureText("Ag",font,new Size(int.MaxValue,int.MaxValue),TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix).Height;
        if(h>0)return h;
      }catch{}
      return Math.Max(12,navBtnH>0?navBtnH/2:12);
    }
    string WrapNavText(string text,Font font,int maxWidth){
      bool hadWrap=text!=null&&text.IndexOf('\n')>=0;
      string raw=(text??"").Replace("\r"," ").Replace("\n"," ").Trim();
      while(raw.IndexOf("  ",StringComparison.Ordinal)>=0)raw=raw.Replace("  "," ");
      if(raw.Length==0||font==null||maxWidth<24)return raw;
      int one=NavTextPx(raw,font);
      if(one<=maxWidth-(hadWrap?12:0))return raw;
      string[] words=raw.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
      if(words.Length==1){
        int cut=Math.Max(1,words[0].Length/2);
        return words[0].Substring(0,cut)+"\n"+words[0].Substring(cut);
      }
      var lines=new System.Collections.Generic.List<string>();
      string cur=words[0];
      for(int i=1;i<words.Length;i++){
        string trial=cur+" "+words[i];
        if(NavTextPx(trial,font)<=maxWidth)cur=trial;
        else{
          lines.Add(cur);cur=words[i];
          if(lines.Count>=2){
            for(int k=i+1;k<words.Length;k++)cur+=" "+words[k];
            break;
          }
        }
      }
      if(cur.Length>0)lines.Add(cur);
      return string.Join("\n",lines.ToArray());
    }
    int NavWrappedHeight(Button b,int minH){
      if(b==null||string.IsNullOrEmpty(b.Text))return minH;
      int lines=1;string t=b.Text;for(int k=0;k<t.Length;k++)if(t[k]=='\n')lines++;
      if(lines<=1)return minH;
      return Math.Max(minH,lines*(NavLineH(b.Font)+2)+6);
    }
    void FitNavButtonText(){
      Button[] all={importButton,potButton,obtButton,improveButton,reevalButton,refinementButton,presetButton,coefficientButton,autoKeepButton,spdRankButton,skillButton,rtaButton,codesButton,retentionButton,updateButton};
      for(int i=0;i<all.Length;i++){
        Button b=all[i];if(b==null||b.IsDisposed)continue;
        b.AutoEllipsis=false;b.UseCompatibleTextRendering=true;
        string wrapped=WrapNavText(b.Text,b.Font,NavTextWidth(b));
        if(wrapped!=b.Text)b.Text=wrapped;
        int need=NavWrappedHeight(b,navBtnH);
        if(Math.Abs(b.Height-need)>1)b.Height=need;
      }
    }
    int NavHeadNeed(int btnH,int titleH){return 8+titleH+8+btnH+8;}
    int NavFootNeed(int btnH,int iconS,int langH){return 6+langH+6+iconS+6+btnH+4+14+6;}
    void ScaleNavToFit(){
      if(navHeader==null||navFooter==null)return;
      int total=Math.Max(400,ClientSize.Height);
      if(leftNav!=null&&leftNav.Visible&&leftNav.Height>80)total=leftNav.Height;
      int btnH=40,btnMV=4,secH=22,secMV=14,titleH=26,iconS=32,langH=28,gap=8;
      float btnFs=9.5f,secFs=8f,titleFs=15f;
      for(int h=40;h>=22;h--){
        int mv=h>=34?4:(h>=28?2:0);
        int sh=Math.Max(12,h*11/20);
        int sm=h>=34?14:(h>=28?8:4);
        int th=h>=36?26:(h>=30?20:16);
        int ic=h>=34?32:(h>=28?24:20);
        int lh=h>=34?28:(h>=28?22:18);
        int head=NavHeadNeed(h,th);
        int foot=NavFootNeed(h,ic,lh);
        int flow=NavFlowNeed(h,mv,sh,sm);
        if(head+foot+flow<=total||h==22){
          btnH=h;btnMV=mv;secH=sh;secMV=sm;titleH=th;iconS=ic;langH=lh;gap=h>=34?8:(h>=28?6:4);
          btnFs=h>=36?9.5f:(h>=30?8.2f:(h>=26?7.4f:6.6f));
          secFs=h>=34?8f:(h>=28?7f:6.2f);
          titleFs=h>=36?15f:(h>=30?12f:10f);
          break;
        }
      }
      navBtnH=btnH;navIconS=iconS;navGap=gap;navLangH=langH;
      if(scaleBtnFont==null||Math.Abs(scaleBtnFont.Size-btnFs)>0.05f)scaleBtnFont=new Font("Segoe UI Semibold",btnFs);
      if(scaleSecFont==null||Math.Abs(scaleSecFont.Size-secFs)>0.05f)scaleSecFont=new Font("Segoe UI Semibold",secFs);
      if(scaleTitleFont==null||Math.Abs(scaleTitleFont.Size-titleFs)>0.05f)scaleTitleFont=new Font("Segoe UI Semibold",titleFs);
      if(scaleCountFont==null||Math.Abs(scaleCountFont.Size-9.5f)>0.05f)scaleCountFont=new Font("Segoe UI Semibold",9.5f);
      float tinyFs=Math.Max(6.5f,secFs);
      if(scaleTinyFont==null||Math.Abs(scaleTinyFont.Size-tinyFs)>0.05f)scaleTinyFont=new Font("Segoe UI",tinyFs);
      int headH=NavHeadNeed(btnH,titleH);
      int footH=NavFootNeed(btnH,iconS,langH);
      if(navHeader.Height!=headH)navHeader.Height=headH;
      if(navFooter.Height!=footH)navFooter.Height=footH;
      int titleY=6;int impY=titleY+titleH+8;
      if(titleLabel!=null){titleLabel.Location=new Point(12,titleY);titleLabel.Size=new Size(228,titleH);AssignFont(titleLabel,scaleTitleFont);}
      if(counters!=null)AssignFont(counters,scaleCountFont);
      int nw=Math.Max(180,NavFullW-20);
      Button[] flowBtns={potButton,obtButton,improveButton,reevalButton,refinementButton,presetButton,coefficientButton,autoKeepButton,spdRankButton,skillButton,rtaButton,codesButton,retentionButton};
      Padding btnPad=new Padding(8,Math.Max(0,btnMV/2),8,Math.Max(0,btnMV/2));
      for(int i=0;i<flowBtns.Length;i++){
        Button b=flowBtns[i];if(b==null)continue;
        b.Width=nw;b.Margin=btnPad;AssignFont(b,scaleBtnFont);
        int wantH=NavWrappedHeight(b,btnH);
        if(Math.Abs(b.Height-wantH)>1)b.Height=wantH;
      }
      if(importButton!=null){importButton.SetBounds(8,impY,nw,btnH);AssignFont(importButton,scaleBtnFont);}
      if(updateButton!=null)AssignFont(updateButton,scaleBtnFont);
      Label[] secs={navSortLbl,navToolsLbl,navAppLbl,navKeepLbl};
      Padding secPad=new Padding(12,Math.Max(2,secMV-2),8,2);
      for(int i=0;i<secs.Length;i++){
        Label s=secs[i];if(s==null)continue;
        s.Width=nw;s.Height=secH;s.Margin=secPad;AssignFont(s,scaleSecFont);
      }
      if(langCombo!=null)AssignFont(langCombo,scaleTinyFont);
      if(navPinButton!=null)AssignFont(navPinButton,scaleTinyFont);
      if(versionLabel!=null)AssignFont(versionLabel,scaleTinyFont);
      CountBadge[] badges={improveBadge,skillBadge,spdRankBadge,codesBadge,updateBadge};
      for(int i=0;i<badges.Length;i++)if(badges[i]!=null)AssignFont(badges[i],scaleTinyFont);
      if(reappNormalBadge!=null)AssignFont(reappNormalBadge,scaleTinyFont);
      if(reappAncientBadge!=null)AssignFont(reappAncientBadge,scaleTinyFont);
      if(refinementBadge!=null)AssignFont(refinementBadge,scaleTinyFont);
      if(paypalButton!=null)paypalButton.Size=new Size(iconS,iconS);
      if(discordButton!=null)discordButton.Size=new Size(iconS,iconS);
      if(twitchButton!=null)twitchButton.Size=new Size(iconS,iconS);
      if(navFlow!=null){
        navFlow.Padding=new Padding(0,2,0,4);
        bool overflow=headH+footH+NavFlowNeed(btnH,btnMV,secH,secMV)>total+8;
        navFlow.AutoScroll=overflow;
      }
    }
    void PaintNavArrow(Graphics g,int cy,int ox){
      int x0=9+ox;
      int x1=navRail.ClientSize.Width-9+ox;
      int h=8;
      using(var p=new Pen(Cyan,2.2f)){
        p.StartCap=LineCap.Round;p.EndCap=LineCap.Round;p.LineJoin=LineJoin.Round;
        g.DrawLines(p,new[]{new Point(x0,cy-h),new Point(x1,cy),new Point(x0,cy+h)});
      }
    }
    void TickNavArrow(object sender,EventArgs e){
      if(navRail==null||!navRail.Visible||navOpen||navPinned){
        if(navArrowTimer!=null)navArrowTimer.Stop();
        return;
      }
      navArrowT+=0.16f;
      if(navArrowT>(float)(Math.PI*2))navArrowT-=(float)(Math.PI*2);
      navRail.Invalidate();
    }
    void PaintNavRail(object sender,PaintEventArgs e){
      if(navRail==null)return;
      Graphics g=e.Graphics;
      g.SmoothingMode=SmoothingMode.AntiAlias;
      using(var bg=new SolidBrush(Color.FromArgb(10,16,26)))g.FillRectangle(bg,navRail.ClientRectangle);
      using(var edge=new SolidBrush(Cyan))g.FillRectangle(edge,0,0,3,navRail.Height);
      Font f=scaleSecFont??scaleTinyFont??navRail.Font;
      string label=Loc.T("nav_rail");
      if(string.IsNullOrEmpty(label))label="MENU";
      TextFormatFlags flags=TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter|TextFormatFlags.NoPadding|TextFormatFlags.NoPrefix;
      int ox=(int)Math.Round(3.0+3.0*Math.Sin(navArrowT));
      PaintNavArrow(g,22,ox);
      PaintNavArrow(g,navRail.Height-22,ox);
      int line=Math.Max(12,NavLineH(f)+2);
      int block=label.Length*line;
      int y=Math.Max(44,(navRail.Height-block)/2);
      for(int i=0;i<label.Length;i++)
        TextRenderer.DrawText(g,label.Substring(i,1),f,new Rectangle(0,y+i*line,navRail.Width,line),Cyan,flags);
    }
    bool NavPointerInside(){
      Point screen=Cursor.Position;
      if(leftNav!=null&&leftNav.Visible){
        Rectangle r=leftNav.RectangleToScreen(leftNav.ClientRectangle);
        if(r.Contains(screen))return true;
      }
      if(navRail!=null&&navRail.Visible){
        Rectangle r=navRail.RectangleToScreen(navRail.ClientRectangle);
        if(r.Contains(screen))return true;
      }
      return false;
    }
    void NavChildAdded(object sender,ControlEventArgs e){if(e!=null&&e.Control!=null)HookNavPointer(e.Control);}
    void HookNavPointer(Control c){
      if(c==null)return;
      c.MouseEnter-=NavPointerEnter;c.MouseEnter+=NavPointerEnter;
      c.MouseLeave-=NavPointerLeave;c.MouseLeave+=NavPointerLeave;
      c.ControlAdded-=NavChildAdded;c.ControlAdded+=NavChildAdded;
      foreach(Control k in c.Controls)HookNavPointer(k);
    }
    void NavPointerEnter(object sender,EventArgs e){ShowNavSoon();}
    void NavPointerLeave(object sender,EventArgs e){if(IsHandleCreated&&!IsDisposed)BeginInvoke((MethodInvoker)HideNavSoon);}
    void WatchNavPointer(object sender,EventArgs e){
      if(navPinned||!navOpen){if(navWatchTimer!=null)navWatchTimer.Stop();return;}
      if(NavPointerInside()||(langCombo!=null&&langCombo.DroppedDown)){
        if(navHideTimer!=null)navHideTimer.Stop();
        return;
      }
      HideNavSoon();
    }
    void ShowNavSoon(){
      if(navHideTimer!=null)navHideTimer.Stop();
      if(navPinned||navOpen)return;
      if(navShowTimer!=null){navShowTimer.Stop();navShowTimer.Start();}
    }
    void HideNavSoon(){
      if(navPinned)return;
      if(NavPointerInside())return;
      if(langCombo!=null&&langCombo.DroppedDown)return;
      if(navShowTimer!=null)navShowTimer.Stop();
      if(navHideTimer!=null&&!navHideTimer.Enabled)navHideTimer.Start();
    }
    void HideNavNow(){
      if(navPinned)return;
      if(NavPointerInside())return;
      if(langCombo!=null&&langCombo.DroppedDown)return;
      navOpen=false;
      ApplyNavChrome();
    }
    void ToggleNavPin(){
      navPinned=!navPinned;
      navOpen=true;
      if(navShowTimer!=null)navShowTimer.Stop();
      if(navHideTimer!=null)navHideTimer.Stop();
      ApplyNavChrome();
      SaveEngineSettings();
      LayoutToolbar();
    }
    void PaintNavPin(){
      if(navPinButton==null)return;
      navPinButton.FlatAppearance.BorderColor=navPinned?Cyan:Color.FromArgb(40,70,90);
      navPinButton.ForeColor=navPinned?Cyan:Color.FromArgb(205,216,228);
      if(navPinTip!=null)navPinTip.SetToolTip(navPinButton,Loc.T(navPinned?"nav_unpin":"nav_pin"));
    }
    void ApplyNavChrome(){
      if(shellRoot==null||leftNav==null)return;
      int colW=navPinned?NavFullW:NavRailW;
      if(shellRoot.ColumnStyles.Count>0){
        ColumnStyle st=shellRoot.ColumnStyles[0];
        if(st.SizeType!=SizeType.Absolute||Math.Abs(st.Width-colW)>0.5f)shellRoot.ColumnStyles[0]=new ColumnStyle(SizeType.Absolute,colW);
      }
      if(navRail!=null)navRail.Visible=!navPinned;
      bool show=navPinned||navOpen;
      leftNav.Visible=show;
      if(show){
        leftNav.Bounds=new Rectangle(0,0,NavFullW,Math.Max(1,ClientSize.Height));
        leftNav.BringToFront();
      }
      if(navWatchTimer!=null){
        if(!navPinned&&navOpen)navWatchTimer.Start();
        else navWatchTimer.Stop();
      }
      if(navArrowTimer!=null){
        if(!navPinned&&!navOpen)navArrowTimer.Start();
        else navArrowTimer.Stop();
      }
      PaintNavPin();
    }
    void InitNavAutoHide(){
      if(navShowTimer==null){navShowTimer=new Timer();navShowTimer.Interval=220;navShowTimer.Tick+=(s,e)=>{navShowTimer.Stop();navOpen=true;ApplyNavChrome();LayoutToolbar();};}
      if(navHideTimer==null){navHideTimer=new Timer();navHideTimer.Interval=180;navHideTimer.Tick+=(s,e)=>{navHideTimer.Stop();HideNavNow();};}
      if(navWatchTimer==null){navWatchTimer=new Timer();navWatchTimer.Interval=80;navWatchTimer.Tick+=WatchNavPointer;}
      if(navArrowTimer==null){navArrowTimer=new Timer();navArrowTimer.Interval=40;navArrowTimer.Tick+=TickNavArrow;}
      HookNavPointer(leftNav);
      HookNavPointer(navRail);
      if(navRail!=null){
        navRail.Click+=(s,e)=>{navOpen=true;ApplyNavChrome();LayoutToolbar();};
        if(navRailTip!=null)navRailTip.SetToolTip(navRail,Loc.T("nav_hover"));
      }
      if(langCombo!=null){
        langCombo.DropDown+=(s,e)=>{if(navHideTimer!=null)navHideTimer.Stop();};
        langCombo.DropDownClosed+=(s,e)=>HideNavSoon();
      }
      Shown+=(s,e)=>{if(!navPinned&&!NavPointerInside()){navOpen=false;ApplyNavChrome();}};
      navOpen=navPinned;
      ApplyNavChrome();
    }
    void StopNavAutoHide(){
      if(navShowTimer!=null){navShowTimer.Stop();navShowTimer.Dispose();navShowTimer=null;}
      if(navHideTimer!=null){navHideTimer.Stop();navHideTimer.Dispose();navHideTimer=null;}
      if(navWatchTimer!=null){navWatchTimer.Stop();navWatchTimer.Dispose();navWatchTimer=null;}
      if(navArrowTimer!=null){navArrowTimer.Stop();navArrowTimer.Dispose();navArrowTimer=null;}
    }
    void BuildShell(){
      leftNav=new Panel{Location=new Point(0,0),Size=new Size(NavFullW,400),Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Bottom,BackColor=Color.FromArgb(10,16,26),Padding=new Padding(0),Visible=false};
      navHeader=new Panel{Dock=DockStyle.Top,Height=196,BackColor=Color.FromArgb(10,16,26)};
      navFooter=new Panel{Dock=DockStyle.Bottom,Height=156,BackColor=Color.FromArgb(10,16,26)};
      navFlow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=Color.FromArgb(10,16,26),Padding=new Padding(0,4,0,8)};
      leftNav.Controls.Add(navFlow);leftNav.Controls.Add(navHeader);leftNav.Controls.Add(navFooter);
      navRail=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(10,16,26),Cursor=Cursors.Hand};
      typeof(Control).GetProperty("DoubleBuffered",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).SetValue(navRail,true,null);
      navRail.Paint+=PaintNavRail;
      filterBar=new Panel{Dock=DockStyle.Top,Height=52,BackColor=Panel,Padding=new Padding(12,8,12,8)};
      topBar=filterBar;
      centerPanel=new Panel{Dock=DockStyle.Fill,BackColor=Bg};
      centerPanel.Controls.Add(grid);
      centerPanel.Controls.Add(filterBar);
      rightPanel=new Panel{Dock=DockStyle.Right,Width=0,Visible=false,BackColor=Bg};
      contentHost=new Panel{Dock=DockStyle.Fill,BackColor=Bg};
      contentHost.Controls.Add(centerPanel);
      contentHost.Controls.Add(rightPanel);
      shellRoot=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,BackColor=Bg,Padding=new Padding(0)};
      shellRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,NavRailW));
      shellRoot.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
      shellRoot.RowStyles.Add(new RowStyle(SizeType.Percent,100));
      shellRoot.RowStyles.Add(new RowStyle(SizeType.Absolute,24));
      shellRoot.Controls.Add(navRail,0,0);shellRoot.SetRowSpan(navRail,2);
      shellRoot.Controls.Add(contentHost,1,0);
      status.Dock=DockStyle.Fill;status.TextAlign=ContentAlignment.MiddleLeft;status.Padding=new Padding(10,0,8,0);
      shellRoot.Controls.Add(status,1,1);
      Controls.Add(shellRoot);
      Controls.Add(leftNav);
    }
  }
}
