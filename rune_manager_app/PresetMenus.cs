using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
namespace RuneManagerModern {
  sealed class BlackPresetColors:ProfessionalColorTable {
    public override Color ToolStripDropDownBackground{get{return Color.Black;}}
    public override Color ImageMarginGradientBegin{get{return Color.Black;}}
    public override Color ImageMarginGradientMiddle{get{return Color.Black;}}
    public override Color ImageMarginGradientEnd{get{return Color.Black;}}
    public override Color MenuItemSelected{get{return Color.Black;}}
    public override Color MenuItemBorder{get{return Color.Teal;}}
    public override Color MenuBorder{get{return Color.DimGray;}}
    public override Color CheckBackground{get{return Color.Black;}}
    public override Color CheckSelectedBackground{get{return Color.Black;}}
    public override Color CheckPressedBackground{get{return Color.Black;}}
  }
  sealed class PresetMenuRenderer:ToolStripProfessionalRenderer {
    public PresetMenuRenderer():base(new BlackPresetColors()){}
    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e){
      using(var brush=new SolidBrush(e.Item.BackColor))e.Graphics.FillRectangle(brush,new Rectangle(Point.Empty,e.Item.Size));
      if(e.Item.Selected)e.Graphics.DrawRectangle(Pens.White,0,0,e.Item.Width-1,e.Item.Height-1);
    }
    // Sans ce override, ToolStripProfessionalRenderer peint quand même la colonne
    // d'icônes (le "carré" à gauche de chaque item) avec le gradient de la table de
    // couleurs, par-dessus le fond déjà coloré par item ci-dessus — d'où le carré
    // noir qui ne change jamais de couleur (rouge/bleu/noir) avec la sélection.
    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e){}
    // Rendu par defaut de ToolStrip = qualite basse quand l'icone source est bien plus
    // grande que la case 22x22 du menu (crenelage). On la redessine nous-memes en
    // haute qualite au lieu de laisser le renderer standard le faire.
    protected override void OnRenderItemImage(ToolStripItemImageRenderEventArgs e){
      if(e.Image==null)return;
      e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;e.Graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
      e.Graphics.DrawImage(e.Image,e.ImageRectangle);
    }
  }
  sealed partial class MainForm {
    // Couleurs fixes partagées entre le menu déroulant ET les puces affichées dans la
    // grille (colonne fermée), pour que rouge = Préféré et bleu = Acceptable toujours,
    // qu'on soit en train de choisir ou juste en train de regarder.
    static readonly Color PreferredColor=Color.FromArgb(125,30,35);
    static readonly Color AcceptableColor=Color.FromArgb(25,65,125);
    static bool PresetDropColumn(int col){return col==0||(col>=2&&col<=20);}
    static bool PresetMainColumn(int col){return col>=15&&col<=20;}
    static readonly string[] PresetSetOrder={"Energy","Guard","Swift","Blade","Rage","Focus","Endure","Fatal","Despair","Vampire","Violent","Nemesis","Will","Shield","Revenge","Destroy","Fight","Determination","Enhance","Accuracy","Tolerance","Seal","Intangible"};
    static readonly string[] Slot2Mains={"HP%","Atk%","Def%","Spd","HP+","Atk+","Def+"};
    static readonly string[] Slot4Mains={"HP%","Atk%","Def%","CtR%","CtD%","HP+","Atk+","Def+"};
    static readonly string[] Slot6Mains={"HP%","Atk%","Def%","Res%","Acc%","HP+","Atk+","Def+"};
    static List<string> OrderedNames(IEnumerable<string> selected,string[] order){
      var list=new List<string>();
      if(selected==null)return list;
      var set=new HashSet<string>(selected,StringComparer.OrdinalIgnoreCase);
      var seen=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      if(order!=null)for(int i=0;i<order.Length;i++)if(set.Contains(order[i])&&seen.Add(order[i]))list.Add(order[i]);
      foreach(string name in selected)if(name!=null&&name.Length>0&&seen.Add(name))list.Add(name);
      return list;
    }
    static string JoinOrderedNames(IEnumerable<string> selected,string[] order){return string.Join(",",OrderedNames(selected,order).ToArray());}
    static int PresetMainSlot(int col){return col==15||col==16?2:col==17||col==18?4:6;}
    static int PresetMainPrefCol(int col){return col<=16?15:col<=18?17:19;}
    static int PresetMainAccCol(int col){return PresetMainPrefCol(col)+1;}
    static string[] PresetMainChoices(int col){int slot=PresetMainSlot(col);return slot==2?Slot2Mains:slot==4?Slot4Mains:Slot6Mains;}
    static HashSet<string> CellSets(DataGridViewCell cell){return new HashSet<string>(Convert.ToString(cell.Value).Split(',').Select(x=>x.Trim()).Where(x=>x.Length>0),StringComparer.OrdinalIgnoreCase);}
    ContextMenuStrip CreatePresetMenu(DataGridView g,int row,int col){
      var menu=new ContextMenuStrip{BackColor=Color.Black,ForeColor=Color.White,ShowCheckMargin=false,ShowImageMargin=true,Renderer=new PresetMenuRenderer(),Font=new Font("Segoe UI",10),ImageScalingSize=new Size(22,22)};
      var cell=g.Rows[row].Cells[col];
      if(col==13||col==14){
        var selected=CellSets(cell);var other=g.Rows[row].Cells[col==13?14:13];
        string[] names=PresetSetOrder;
        // Couleur FIXE par catégorie (rouge = Préféré, bleu = Acceptable), jamais relative à
        // la colonne ouverte. Avant : rouge voulait dire "déjà dans cette colonne-ci", donc en
        // ouvrant la colonne Acceptables, les sets Acceptables s'affichaient en ROUGE (la
        // couleur du Préféré) — mêmes deux couleurs, sens inversé selon la colonne cliquée.
        var preferredCell=g.Rows[row].Cells[13];var acceptableCell=g.Rows[row].Cells[14];
        Action refreshColors=()=>{var preferredSet=CellSets(preferredCell);var acceptableSet=CellSets(acceptableCell);foreach(ToolStripMenuItem entry in menu.Items){entry.BackColor=preferredSet.Contains(entry.Text)?PreferredColor:acceptableSet.Contains(entry.Text)?AcceptableColor:Color.Black;entry.ToolTipText=preferredSet.Contains(entry.Text)?Loc.T("tip_set_pref"):acceptableSet.Contains(entry.Text)?Loc.T("tip_set_ok"):Loc.T("tip_set_add",col==13?Loc.T("tip_set_role_pref"):Loc.T("tip_set_role_ok"));}menu.Invalidate();};
        foreach(string name in names){string setName=name;var item=new ToolStripMenuItem(setName,GetSetIcon(setName)){ForeColor=Color.White,BackColor=Color.Black};item.Click+=(s,e)=>{if(!selected.Contains(setName)){selected.Add(setName);var opposite=CellSets(other);opposite.Remove(setName);other.Value=string.Join(",",names.Where(opposite.Contains));}else selected.Remove(setName);cell.Value=string.Join(",",names.Where(selected.Contains));refreshColors();g.InvalidateRow(row);};menu.Items.Add(item);}
        refreshColors();
        menu.Closing+=(s,e)=>{if(e.CloseReason==ToolStripDropDownCloseReason.ItemClicked)e.Cancel=true;};
      }else if(PresetMainColumn(col)){
        menu.ShowImageMargin=false;
        var selected=CellSets(cell);var other=g.Rows[row].Cells[col==PresetMainPrefCol(col)?PresetMainAccCol(col):PresetMainPrefCol(col)];
        string[] names=PresetMainChoices(col);
        var preferredCell=g.Rows[row].Cells[PresetMainPrefCol(col)];var acceptableCell=g.Rows[row].Cells[PresetMainAccCol(col)];
        Action refreshColors=()=>{var preferredSet=CellSets(preferredCell);var acceptableSet=CellSets(acceptableCell);foreach(ToolStripMenuItem entry in menu.Items){entry.BackColor=preferredSet.Contains(entry.Text)?PreferredColor:acceptableSet.Contains(entry.Text)?AcceptableColor:Color.Black;}menu.Invalidate();};
        foreach(string name in names){string stat=name;var item=new ToolStripMenuItem(stat){ForeColor=Color.White,BackColor=Color.Black};item.Click+=(s,e)=>{if(!selected.Contains(stat)){selected.Add(stat);var opposite=CellSets(other);opposite.Remove(stat);other.Value=string.Join(",",names.Where(opposite.Contains));}else selected.Remove(stat);cell.Value=string.Join(",",names.Where(selected.Contains));refreshColors();g.InvalidateRow(row);};menu.Items.Add(item);}
        refreshColors();
        menu.Closing+=(s,e)=>{if(e.CloseReason==ToolStripDropDownCloseReason.ItemClicked)e.Cancel=true;};
      }else{
        string currentText=Convert.ToString(cell.Value);
        // Colonne 0 = facteur preset (0–300%). Ne pas la traiter comme la ligne
        // "valeur globale par stat" (50–150%) juste parce que le texte finit par %.
        if(col!=0&&currentText!=null&&currentText.EndsWith("%")){
          // Ligne "valeur globale par stat" (RuneEnhancements.cs) : menu de pourcentages,
          // pas la liste Non/P1/P2/P3 de la colonne — reconnue au contenu de la cellule.
          for(int pct=50;pct<=150;pct+=10){string text=pct+"%";var item=new ToolStripMenuItem(text){Checked=text==currentText,ForeColor=Color.White,BackColor=Color.Black};item.Click+=(s,e)=>{cell.Value=text;g.InvalidateCell(cell);};menu.Items.Add(item);}
        }else{
          var column=g.Columns[col] as DataGridViewComboBoxColumn;if(column!=null)foreach(object value in column.Items){string text=Convert.ToString(value);var item=new ToolStripMenuItem(text){Checked=text==currentText,ForeColor=Color.White,BackColor=Color.Black};item.Click+=(s,e)=>{cell.Value=text;g.InvalidateCell(cell);};menu.Items.Add(item);}
        }
      }
      // WinForms still uses the popup after Closed; dispose on the next UI turn.
      EventHandler cleanup=(s,e)=>menu.Dispose();g.Disposed+=cleanup;
      menu.Disposed+=(s,e)=>g.Disposed-=cleanup;
      menu.Closed+=(s,e)=>{if(g.IsHandleCreated&&!g.IsDisposed)g.BeginInvoke((MethodInvoker)delegate{if(!menu.IsDisposed)menu.Dispose();});};return menu;
    }
    void ConfigureBlackPresetGrid(DataGridView g){
      g.BackgroundColor=Color.Black;g.GridColor=Color.FromArgb(55,55,55);g.DefaultCellStyle.BackColor=Color.Black;g.DefaultCellStyle.ForeColor=Color.White;g.DefaultCellStyle.SelectionBackColor=Color.Black;g.DefaultCellStyle.SelectionForeColor=Color.White;
      for(int i=0;i<g.Columns.Count;i++)if(PresetDropColumn(i))g.Columns[i].ReadOnly=true;
      g.EditingControlShowing+=(s,e)=>{e.Control.BackColor=Color.Black;e.Control.ForeColor=Color.White;};
      g.CellPainting+=(s,e)=>{
        if(e.RowIndex<0)return;
        e.PaintBackground(e.ClipBounds,true);
        // Sans ce reglage, GDI+ redimensionne les icones de set en basse qualite quand
        // elles sont reduites a 20x20 dans les puces — rendu crenele/flou.
        e.Graphics.SmoothingMode=SmoothingMode.AntiAlias;e.Graphics.InterpolationMode=InterpolationMode.HighQualityBicubic;e.Graphics.PixelOffsetMode=PixelOffsetMode.HighQuality;
        if(e.ColumnIndex==13||e.ColumnIndex==14){Color chipColor=e.ColumnIndex==13?PreferredColor:AcceptableColor;int x=e.CellBounds.X+3,y=e.CellBounds.Y+3;foreach(string name in OrderedNames(CellSets(g.Rows[e.RowIndex].Cells[e.ColumnIndex]),PresetSetOrder)){int w=TextRenderer.MeasureText(name,g.Font).Width+28;if(x+w>e.CellBounds.Right-18){x=e.CellBounds.X+3;y+=22;}if(y+22>e.CellBounds.Bottom)break;using(var chipBrush=new SolidBrush(chipColor))e.Graphics.FillRectangle(chipBrush,new Rectangle(x,y,w-4,20));var icon=GetSetIcon(name);if(icon!=null)e.Graphics.DrawImage(icon,new Rectangle(x,y,20,20));TextRenderer.DrawText(e.Graphics,name,g.Font,new Point(x+22,y+2),Color.White);x+=w;}}
        else if(PresetMainColumn(e.ColumnIndex)){Color chipColor=e.ColumnIndex==PresetMainPrefCol(e.ColumnIndex)?PreferredColor:AcceptableColor;int x=e.CellBounds.X+3,y=e.CellBounds.Y+3;foreach(string name in OrderedNames(CellSets(g.Rows[e.RowIndex].Cells[e.ColumnIndex]),PresetMainChoices(e.ColumnIndex))){int w=TextRenderer.MeasureText(name,g.Font).Width+16;if(x+w>e.CellBounds.Right-18){x=e.CellBounds.X+3;y+=22;}if(y+22>e.CellBounds.Bottom)break;using(var chipBrush=new SolidBrush(chipColor))e.Graphics.FillRectangle(chipBrush,new Rectangle(x,y,w-4,20));TextRenderer.DrawText(e.Graphics,name,g.Font,new Point(x+6,y+2),Color.White);x+=w;}}
        else TextRenderer.DrawText(e.Graphics,Convert.ToString(e.FormattedValue),g.Font,new Rectangle(e.CellBounds.X+4,e.CellBounds.Y+2,Math.Max(1,e.CellBounds.Width-22),e.CellBounds.Height-4),Color.White,TextFormatFlags.VerticalCenter|TextFormatFlags.EndEllipsis);
        if(PresetDropColumn(e.ColumnIndex)&&!(e.RowIndex==0&&PresetMainColumn(e.ColumnIndex))){int x=e.CellBounds.Right-12,y=e.CellBounds.Y+13;e.Graphics.FillPolygon(Brushes.White,new[]{new Point(x-4,y-2),new Point(x+4,y-2),new Point(x,y+3)});}
        e.Paint(e.ClipBounds,DataGridViewPaintParts.Border);e.Handled=true;
      };
      Action<int,int> open=(row,col)=>{if(row<0||!PresetDropColumn(col))return;if(PresetMainColumn(col)&&g.Rows[row].Cells[1].ReadOnly)return;g.EndEdit();var rect=g.GetCellDisplayRectangle(col,row,true);CreatePresetMenu(g,row,col).Show(g,new Point(rect.Left,rect.Top+Math.Min(30,rect.Height)));};
      g.CellClick+=(s,e)=>open(e.RowIndex,e.ColumnIndex);
      g.KeyDown+=(s,e)=>{if((e.KeyCode==Keys.Space||e.KeyCode==Keys.Enter||e.KeyCode==Keys.F4)&&g.CurrentCell!=null&&PresetDropColumn(g.CurrentCell.ColumnIndex)){open(g.CurrentCell.RowIndex,g.CurrentCell.ColumnIndex);e.Handled=true;e.SuppressKeyPress=true;}};
    }
  }
}
