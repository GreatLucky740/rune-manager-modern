using System;
using System.Drawing;
using System.Windows.Forms;

namespace RuneManagerModern {
  sealed partial class MainForm {
    Panel leftNav, navHeader, navFooter, filterBar, currentToolHost, contentHost, centerPanel, rightPanel;
    FlowLayoutPanel navFlow;
    Form currentTool;
    Button currentToolButton;
    Label navSortLbl, navToolsLbl, navAppLbl, navKeepLbl;
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
        if(avail<80)avail=Math.Max(900,ClientSize.Width-(leftNav==null?252:leftNav.Width));
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
    void FitWrapBar(FlowLayoutPanel bar){
      if(bar==null||bar.IsDisposed||wrapBarBusy)return;
      wrapBarBusy=true;
      try{
        bar.WrapContents=true;
        int bottom=bar.Padding.Top;
        foreach(Control c in bar.Controls){
          if(c==null||c.IsDisposed||!c.Visible)continue;
          int b=c.Bottom+c.Margin.Bottom;
          if(b>bottom)bottom=b;
        }
        int h=Math.Max(56,bottom+bar.Padding.Bottom+4);
        if(h>220)h=220;
        if(Math.Abs(bar.Height-h)>2)bar.Height=h;
      }finally{wrapBarBusy=false;}
    }
    void AttachWrapBar(FlowLayoutPanel bar){
      if(bar==null)return;
      bar.WrapContents=true;
      bar.AutoScroll=false;
      if(bar.Padding.All==0)bar.Padding=new Padding(8,10,8,8);
      bar.Layout+=(s,e)=>FitWrapBar(bar);
      bar.Resize+=(s,e)=>FitWrapBar(bar);
      bar.HandleCreated+=(s,e)=>FitWrapBar(bar);
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
      string raw=(text??"").Replace("\r"," ").Replace("\n"," ").Trim();
      while(raw.IndexOf("  ",StringComparison.Ordinal)>=0)raw=raw.Replace("  "," ");
      if(raw.Length==0||font==null||maxWidth<24)return raw;
      if(NavTextPx(raw,font)<=maxWidth)return raw;
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
    void FitNavButtonText(){
      Button[] all={importButton,potButton,obtButton,improveButton,reevalButton,refinementButton,presetButton,coefficientButton,autoKeepButton,spdRankButton,skillButton,rtaButton,codesButton,retentionButton,updateButton};
      for(int i=0;i<all.Length;i++){
        Button b=all[i];if(b==null||b.IsDisposed)continue;
        b.AutoEllipsis=false;b.UseCompatibleTextRendering=true;
        string wrapped=WrapNavText(b.Text,b.Font,NavTextWidth(b));
        if(wrapped!=b.Text)b.Text=wrapped;
        int lines=1;for(int k=0;k<wrapped.Length;k++)if(wrapped[k]=='\n')lines++;
        int lineH=NavLineH(b.Font);
        int need=Math.Max(navBtnH,lines*(lineH+2)+6);
        if(b.Height!=need)b.Height=need;
      }
    }
    int NavHeadNeed(int btnH,int titleH,int cH){return 8+titleH+4+cH+6+btnH+8;}
    int NavFootNeed(int btnH,int iconS,int langH){return 6+langH+6+iconS+6+btnH+4+14+6;}
    void ScaleNavToFit(){
      if(navHeader==null||navFooter==null)return;
      int total=leftNav!=null&&leftNav.ClientSize.Height>80?leftNav.ClientSize.Height:Math.Max(400,ClientSize.Height);
      int btnH=40,btnMV=4,secH=22,secMV=14,titleH=26,cH=102,iconS=32,langH=28,gap=8;
      float btnFs=9.5f,secFs=8f,titleFs=15f,countFs=10f;
      for(int h=40;h>=22;h--){
        int mv=h>=34?4:(h>=28?2:0);
        int sh=Math.Max(12,h*11/20);
        int sm=h>=34?14:(h>=28?8:4);
        int th=h>=36?26:(h>=30?20:16);
        float cfs=h>=36?10f:(h>=30?8.5f:7.5f);
        int ch=Math.Max(36,(int)(cfs*5.4f)+6);
        int ic=h>=34?32:(h>=28?24:20);
        int lh=h>=34?28:(h>=28?22:18);
        int head=NavHeadNeed(h,th,ch);
        int foot=NavFootNeed(h,ic,lh);
        int flow=NavFlowNeed(h,mv,sh,sm);
        if(head+foot+flow<=total||h==22){
          btnH=h;btnMV=mv;secH=sh;secMV=sm;titleH=th;cH=ch;iconS=ic;langH=lh;gap=h>=34?8:(h>=28?6:4);
          btnFs=h>=36?9.5f:(h>=30?8.2f:(h>=26?7.4f:6.6f));
          secFs=h>=34?8f:(h>=28?7f:6.2f);
          titleFs=h>=36?15f:(h>=30?12f:10f);
          countFs=cfs;
          break;
        }
      }
      navBtnH=btnH;navIconS=iconS;navGap=gap;navLangH=langH;
      if(scaleBtnFont==null||Math.Abs(scaleBtnFont.Size-btnFs)>0.05f)scaleBtnFont=new Font("Segoe UI Semibold",btnFs);
      if(scaleSecFont==null||Math.Abs(scaleSecFont.Size-secFs)>0.05f)scaleSecFont=new Font("Segoe UI Semibold",secFs);
      if(scaleTitleFont==null||Math.Abs(scaleTitleFont.Size-titleFs)>0.05f)scaleTitleFont=new Font("Segoe UI Semibold",titleFs);
      if(scaleCountFont==null||Math.Abs(scaleCountFont.Size-countFs)>0.05f)scaleCountFont=new Font("Segoe UI Semibold",countFs);
      float tinyFs=Math.Max(6.5f,secFs);
      if(scaleTinyFont==null||Math.Abs(scaleTinyFont.Size-tinyFs)>0.05f)scaleTinyFont=new Font("Segoe UI",tinyFs);
      int headH=NavHeadNeed(btnH,titleH,cH);
      int footH=NavFootNeed(btnH,iconS,langH);
      if(navHeader.Height!=headH)navHeader.Height=headH;
      if(navFooter.Height!=footH)navFooter.Height=footH;
      int titleY=6;int cY=titleY+titleH+4;int impY=cY+cH+6;
      if(titleLabel!=null){titleLabel.Location=new Point(12,titleY);titleLabel.Size=new Size(228,titleH);AssignFont(titleLabel,scaleTitleFont);}
      if(counters!=null){counters.Location=new Point(12,cY);counters.Size=new Size(228,cH);AssignFont(counters,scaleCountFont);}
      int nw=Math.Max(180,(leftNav==null?252:leftNav.ClientSize.Width)-20);
      Button[] flowBtns={potButton,obtButton,improveButton,reevalButton,refinementButton,presetButton,coefficientButton,autoKeepButton,spdRankButton,skillButton,rtaButton,codesButton,retentionButton};
      Padding btnPad=new Padding(8,Math.Max(0,btnMV/2),8,Math.Max(0,btnMV/2));
      for(int i=0;i<flowBtns.Length;i++){
        Button b=flowBtns[i];if(b==null)continue;
        b.Width=nw;b.Height=btnH;b.Margin=btnPad;AssignFont(b,scaleBtnFont);
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
    void BuildShell(){
      leftNav=new Panel{Dock=DockStyle.Fill,BackColor=Color.FromArgb(10,16,26),Padding=new Padding(0)};
      navHeader=new Panel{Dock=DockStyle.Top,Height=196,BackColor=Color.FromArgb(10,16,26)};
      navFooter=new Panel{Dock=DockStyle.Bottom,Height=156,BackColor=Color.FromArgb(10,16,26)};
      navFlow=new FlowLayoutPanel{Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false,AutoScroll=true,BackColor=Color.FromArgb(10,16,26),Padding=new Padding(0,4,0,8)};
      leftNav.Controls.Add(navFlow);leftNav.Controls.Add(navHeader);leftNav.Controls.Add(navFooter);
      filterBar=new Panel{Dock=DockStyle.Top,Height=52,BackColor=Panel,Padding=new Padding(12,8,12,8)};
      topBar=filterBar;
      centerPanel=new Panel{Dock=DockStyle.Fill,BackColor=Bg};
      centerPanel.Controls.Add(grid);
      centerPanel.Controls.Add(filterBar);
      rightPanel=new Panel{Dock=DockStyle.Right,Width=0,Visible=false,BackColor=Bg};
      contentHost=new Panel{Dock=DockStyle.Fill,BackColor=Bg};
      contentHost.Controls.Add(centerPanel);
      contentHost.Controls.Add(rightPanel);
      var root=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,BackColor=Bg,Padding=new Padding(0)};
      root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,252));
      root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,100));
      root.RowStyles.Add(new RowStyle(SizeType.Percent,100));
      root.RowStyles.Add(new RowStyle(SizeType.Absolute,24));
      root.Controls.Add(leftNav,0,0);root.SetRowSpan(leftNav,2);
      root.Controls.Add(contentHost,1,0);
      status.Dock=DockStyle.Fill;status.TextAlign=ContentAlignment.MiddleLeft;status.Padding=new Padding(10,0,8,0);
      root.Controls.Add(status,1,1);
      Controls.Add(root);
    }
  }
}
