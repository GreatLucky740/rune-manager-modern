using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RuneManagerModern {
  sealed class RtaDraftFrame { public List<int> Ours=new List<int>(); public List<int> Enemies=new List<int>(); public double Confidence; }
  static class RtaDraftCapture {
    static string RegionFile { get { var d=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Rune_Manager_Modern");Directory.CreateDirectory(d);return Path.Combine(d,"rta-draft-region.txt"); } }
    public static bool HasRegion { get { return !LoadRegion().IsEmpty; } }
    public static Rectangle LoadRegion(){try{var p=File.ReadAllText(RegionFile).Split(',').Select(int.Parse).ToArray();if(p.Length==4)return new Rectangle(p[0],p[1],p[2],p[3]);}catch{}return Rectangle.Empty;}
    public static Rectangle ChooseRegion(IWin32Window owner){using(var f=new RtaRegionForm()){if(f.ShowDialog(owner)!=DialogResult.OK)return Rectangle.Empty;var r=f.SelectedRegion;File.WriteAllText(RegionFile,string.Join(",",new[]{r.X,r.Y,r.Width,r.Height}));return r;}}
    public static RtaDraftFrame Scan(string iconsFolder,IEnumerable<RtaMonster> monsters){var region=LoadRegion();if(region.IsEmpty||!SystemInformation.VirtualScreen.Contains(region))return null;using(var shot=new Bitmap(region.Width,region.Height,PixelFormat.Format24bppRgb)){using(var g=Graphics.FromImage(shot))g.CopyFromScreen(region.Location,Point.Empty,region.Size,CopyPixelOperation.SourceCopy);var templates=new Dictionary<int,byte[]>();foreach(var m in monsters.GroupBy(x=>x.Id).Select(x=>x.First())){string path=Path.Combine(iconsFolder,m.Id+".png");if(!File.Exists(path))continue;try{using(var image=new Bitmap(path))templates[m.Id]=Signature(image);}catch{}}var result=new RtaDraftFrame();double total=0;int read=0;for(int row=0;row<2;row++)for(int col=0;col<5;col++){var cell=new Rectangle(col*shot.Width/5,row*shot.Height/2,(col+1)*shot.Width/5-col*shot.Width/5,(row+1)*shot.Height/2-row*shot.Height/2);using(var crop=shot.Clone(CenteredSquare(cell),shot.PixelFormat)){var sig=Signature(crop);int id=0;double best=double.MaxValue;foreach(var t in templates){double d=Distance(sig,t.Value);if(d<best){best=d;id=t.Key;}}if(id>0&&best<58){if(row==0)result.Ours.Add(id);else result.Enemies.Add(id);total+=Math.Max(0,1-best/58);read++;}}}result.Confidence=read==0?0:total/read;return result;}}
    static Rectangle CenteredSquare(Rectangle cell){int side=(int)(Math.Min(cell.Width,cell.Height)*.82),x=cell.X+(cell.Width-side)/2,y=cell.Y+(cell.Height-side)/2;return new Rectangle(x,y,Math.Max(8,side),Math.Max(8,side));}
    static byte[] Signature(Bitmap source){using(var small=new Bitmap(12,12,PixelFormat.Format24bppRgb)){using(var g=Graphics.FromImage(small)){g.InterpolationMode=InterpolationMode.HighQualityBilinear;g.DrawImage(source,new Rectangle(0,0,12,12),new Rectangle(source.Width/10,source.Height/10,Math.Max(1,source.Width*8/10),Math.Max(1,source.Height*8/10)),GraphicsUnit.Pixel);}var v=new byte[12*12*3];int n=0;for(int y=0;y<12;y++)for(int x=0;x<12;x++){var c=small.GetPixel(x,y);v[n++]=c.R;v[n++]=c.G;v[n++]=c.B;}return v;}}
    static double Distance(byte[] a,byte[] b){long sum=0;for(int i=0;i<a.Length;i++)sum+=Math.Abs(a[i]-b[i]);return sum/(double)a.Length;}
  }
  sealed class RtaRegionForm:Form {
    Point start,current;bool dragging;Rectangle selected;public Rectangle SelectedRegion{get{return selected;}}
    public RtaRegionForm(){FormBorderStyle=FormBorderStyle.None;Bounds=SystemInformation.VirtualScreen;StartPosition=FormStartPosition.Manual;TopMost=true;ShowInTaskbar=false;BackColor=Color.Black;Opacity=.40;Cursor=Cursors.Cross;DoubleBuffered=true;KeyPreview=true;}
    protected override void OnKeyDown(KeyEventArgs e){if(e.KeyCode==Keys.Escape){DialogResult=DialogResult.Cancel;Close();}base.OnKeyDown(e);}
    protected override void OnMouseDown(MouseEventArgs e){if(e.Button==MouseButtons.Left){dragging=true;start=current=e.Location;Invalidate();}}
    protected override void OnMouseMove(MouseEventArgs e){if(dragging){current=e.Location;Invalidate();}}
    protected override void OnMouseUp(MouseEventArgs e){if(!dragging)return;dragging=false;current=e.Location;selected=Rect(start,current);if(selected.Width<300||selected.Height<100){selected=Rectangle.Empty;Invalidate();return;}selected.Offset(Bounds.Left,Bounds.Top);DialogResult=DialogResult.OK;Close();}
    protected override void OnPaint(PaintEventArgs e){base.OnPaint(e);using(var f=new Font("Segoe UI Semibold",18))TextRenderer.DrawText(e.Graphics,"Encadre les 5 portraits de TON équipe en haut et les 5 adversaires en bas",f,new Rectangle(0,15,Width,55),Color.White,TextFormatFlags.HorizontalCenter|TextFormatFlags.VerticalCenter);if(dragging){var r=Rect(start,current);using(var b=new SolidBrush(Color.FromArgb(100,190,68,145)))e.Graphics.FillRectangle(b,r);using(var p=new Pen(Color.FromArgb(255,82,180),3))e.Graphics.DrawRectangle(p,r);}}
    static Rectangle Rect(Point a,Point b){return Rectangle.FromLTRB(Math.Min(a.X,b.X),Math.Min(a.Y,b.Y),Math.Max(a.X,b.X),Math.Max(a.Y,b.Y));}
  }
}
