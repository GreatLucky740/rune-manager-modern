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
      var b=new Button{Text=text,AutoSize=false,Size=new Size(228,40),Margin=new Padding(8,2,8,2),FlatStyle=FlatStyle.Flat,BackColor=NavIdle,ForeColor=Color.FromArgb(205,216,228),Font=new Font("Segoe UI Semibold",9.5f),Cursor=Cursors.Hand,TextAlign=ContentAlignment.MiddleLeft,Padding=new Padding(12,0,12,0),UseMnemonic=false,AutoEllipsis=true};
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
      using(var brush=new SolidBrush(selected?Cyan:accent))e.Graphics.FillRectangle(brush,0,6,3,b.Height-12);
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
    void PlaceNavBadges(){
      PlaceBadgeRight(improveBadge,improveButton,4);
      PlaceBadgeRight(spdRankBadge,spdRankButton,4);
      PlaceBadgeRight(skillBadge,skillButton,4);
      PlaceBadgeRight(codesBadge,codesButton,4);
      PlaceBadgeRight(updateBadge,updateButton,4);
      if(refinementButton!=null&&refinementBadge!=null){
        refinementBadge.Size=new Size(64,32);
        refinementBadge.Location=new Point(Math.Max(4,refinementButton.Width-refinementBadge.Width-6),Math.Max(2,(refinementButton.Height-refinementBadge.Height)/2));
      }
      if(reevalButton!=null&&reappNormalBadge!=null&&reappAncientBadge!=null){
        reappAncientBadge.Size=new Size(62,30);
        reappNormalBadge.Size=new Size(62,30);
        int y=Math.Max(2,(reevalButton.Height-reappNormalBadge.Height)/2);
        reappAncientBadge.Location=new Point(Math.Max(4,reevalButton.Width-66),y);
        reappNormalBadge.Location=new Point(Math.Max(4,reevalButton.Width-130),y);
      }
    }
    void PlaceBadgeRight(Control badge,Control host,int pad){
      if(badge==null||host==null)return;
      badge.Location=new Point(Math.Max(4,host.Width-badge.Width-pad),Math.Max(2,(host.Height-badge.Height)/2));
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
