using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Reflection;

namespace RuneManagerModern {
  sealed partial class MainForm {
    // Generated plates are shared by every set; equipment data stays dynamic.
    static readonly Dictionary<string,Bitmap> premiumParts=new Dictionary<string,Bitmap>();
    readonly Dictionary<string,Bitmap> premiumEmblems=new Dictionary<string,Bitmap>();
    static Bitmap PremiumPart(string name){
      Bitmap result;if(premiumParts.TryGetValue(name,out result))return result;
      using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("RuneArt."+name+".png")){
        if(stream==null)return null;
        using(var original=new Bitmap(stream))using(var crop=CropOpaque(original)){
          float scale=Math.Min(1,512f/Math.Max(crop.Width,crop.Height));
          result=HighQualityScale(crop,Math.Max(1,(int)(crop.Width*scale)),Math.Max(1,(int)(crop.Height*scale)));
        }
      }
      premiumParts[name]=result;return result;
    }
    Image RenderPremiumRune(string set,int slot,int grade,bool ancient,int stars,int level){
      var stone=PremiumPart("slot"+slot);var star=PremiumPart("star");
      if(stone==null||star==null)return null;
      stars=Math.Max(1,Math.Min(6,stars));
      Color quality=RuneLogoColor(grade);
      var result=new Bitmap(256,256,PixelFormat.Format32bppArgb);
      using(var g=Graphics.FromImage(result)){
        g.SmoothingMode=SmoothingMode.AntiAlias;g.InterpolationMode=InterpolationMode.HighQualityBicubic;
        g.PixelOffsetMode=PixelOffsetMode.HighQuality;g.CompositingQuality=CompositingQuality.HighQuality;
        float gap=7f;float starSize=Math.Min(32f,(232f-(stars-1)*gap)/stars);
        float rowWidth=stars*starSize+(stars-1)*gap;float starX=(256-rowWidth)/2f;
        for(int i=0;i<stars;i++)g.DrawImage(star,starX+i*(starSize+gap),4,starSize,starSize);
        float stoneTop=4+starSize+10;float stoneBox=256-stoneTop-34;
        float scale=Math.Min(200f/stone.Width,stoneBox/stone.Height);
        int w=(int)(stone.Width*scale),h=(int)(stone.Height*scale),x=(256-w)/2,y=(int)(stoneTop+(stoneBox-h)/2);
        using(var body=HighQualityScale(stone,w,h)){
          if(ancient)DrawAncientEdge(g,body,x,y);
          g.DrawImage(body,x,y,w,h);
          if(ancient){DrawEdgeGlint(g,body,x,y,.18f);DrawEdgeGlint(g,body,x,y,.78f);}
        }
        Image glyph=PremiumPart("glyph-"+set);
        if(glyph!=null){
          string key=set+"|"+grade;Bitmap relief;
          if(!premiumEmblems.TryGetValue(key,out relief)){
            relief=ColorGem(glyph,grade);premiumEmblems[key]=relief;
          }
          float size=Math.Min(w*.52f,h*.56f);
          float ratio=Math.Min(size/relief.Width,size/relief.Height);
          float ew=relief.Width*ratio,eh=relief.Height*ratio;
          g.DrawImage(relief,x+(w-ew)/2,y+(h-eh)/2,ew,eh);
        }
        DrawMetalLevel(g,level,quality);
      }
      return result;
    }
    static Bitmap ColorGem(Image source,int grade){
      var color=RuneLogoColor(grade);
      var result=HighQualityScale(source,Math.Max(1,source.Width/2),Math.Max(1,source.Height/2));
      for(int y=0;y<result.Height;y++)for(int x=0;x<result.Width;x++){
        var p=result.GetPixel(x,y);if(p.A==0)continue;
        float lum=(p.R+p.G+p.B)/3f;
        float highlight=Math.Max(0,(lum-225)/30f),shade=Math.Min(1,lum/210f);
        int r=(int)(color.R*shade+(255-color.R)*highlight),g=(int)(color.G*shade+(255-color.G)*highlight),b=(int)(color.B*shade+(255-color.B)*highlight);
        result.SetPixel(x,y,Color.FromArgb(p.A,Math.Min(255,r),Math.Min(255,g),Math.Min(255,b)));
      }return result;
    }
    static Bitmap FlatMask(Bitmap source,Color color){
      var b=new Bitmap(source.Width,source.Height,PixelFormat.Format32bppArgb);
      using(var g=Graphics.FromImage(b))using(var attributes=new ImageAttributes()){
        var m=new ColorMatrix(new float[][]{new float[]{0,0,0,0,0},new float[]{0,0,0,0,0},new float[]{0,0,0,0,0},new float[]{0,0,0,color.A/255f,0},new float[]{color.R/255f,color.G/255f,color.B/255f,0,1}});
        attributes.SetColorMatrix(m);g.DrawImage(source,new Rectangle(0,0,b.Width,b.Height),0,0,b.Width,b.Height,GraphicsUnit.Pixel,attributes);
      }return b;
    }
    static void DrawAncientEdge(Graphics g,Bitmap stone,int x,int y){
      using(var soft=FlatMask(stone,Color.FromArgb(32,255,255,255)))
      using(var sharp=FlatMask(stone,Color.White)){
        for(int i=0;i<16;i++){double a=i*Math.PI/8;g.DrawImageUnscaled(soft,x+(int)Math.Round(Math.Cos(a)*4),y+(int)Math.Round(Math.Sin(a)*4));}
        for(int i=0;i<12;i++){double a=i*Math.PI/6;g.DrawImageUnscaled(sharp,x+(int)Math.Round(Math.Cos(a)*2),y+(int)Math.Round(Math.Sin(a)*2));}
      }
    }
    static void DrawEdgeGlint(Graphics g,Bitmap body,int x,int y,float fraction){
      int row=Math.Max(0,Math.Min(body.Height-1,(int)(body.Height*fraction))),col=0;
      if(fraction<.5f){while(col<body.Width-1&&body.GetPixel(col,row).A<180)col++;}
      else {col=body.Width-1;while(col>0&&body.GetPixel(col,row).A<180)col--;}
      float cx=x+col,cy=y+row;
      using(var path=new GraphicsPath()){
        path.AddPolygon(new[]{new PointF(cx,cy-9),new PointF(cx+1.3f,cy-1.3f),new PointF(cx+9,cy),new PointF(cx+1.3f,cy+1.3f),new PointF(cx,cy+9),new PointF(cx-1.3f,cy+1.3f),new PointF(cx-9,cy),new PointF(cx-1.3f,cy-1.3f)});
        g.FillPath(Brushes.White,path);
      }
      g.FillEllipse(Brushes.White,cx+5,cy-13,1.8f,1.8f);
    }
    static void DrawMetalLevel(Graphics g,int level,Color quality){
      using(var path=new GraphicsPath())using(var format=new StringFormat(StringFormat.GenericTypographic)){
        try{path.AddString("+"+level,new FontFamily("Segoe UI"),(int)FontStyle.Bold,36,PointF.Empty,format);}
        catch{path.AddString("+"+level,FontFamily.GenericSansSerif,(int)FontStyle.Bold,36,PointF.Empty,format);}
        var b=path.GetBounds();
        using(var shift=new Matrix()){shift.Translate(5-b.X,256-b.Height-5-b.Y);path.Transform(shift);}
        using(var q=new Pen(quality,3.6f){LineJoin=LineJoin.Round,StartCap=LineCap.Round,EndCap=LineCap.Round})g.DrawPath(q,path);
        using(var k=new Pen(Color.FromArgb(18,14,12),1.2f){LineJoin=LineJoin.Round})g.DrawPath(k,path);
        using(var w=new SolidBrush(Color.White))g.FillPath(w,path);
      }
    }
  }
}
