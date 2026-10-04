using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RuneManagerModern {
  sealed class RtaPeekShot { public string Text=""; public string Element=""; public Bitmap Portrait; }
  static class RtaPeekName {
    public static bool LooksLikeMonsterCard(string text){
      string c=Compact(text);
      if(c.IndexOf("maxlv")<0)return false;
      return c.IndexOf("attack")>=0||c.IndexOf("atk")>=0||c.IndexOf("def")>=0||c.IndexOf("spd")>=0||c.IndexOf("hp")>=0;
    }
    public static string Fold(string s){
      if(string.IsNullOrEmpty(s))return "";
      string n=s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
      var sb=new StringBuilder(n.Length);
      foreach(char c in n)if(CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)sb.Append(c);
      return sb.ToString();
    }
    public static RtaMonster Match(string ocr,IList<RtaMonster> all){return Match(ocr,all,"",null,null);}
    public static RtaMonster Match(string ocr,IList<RtaMonster> all,string element){return Match(ocr,all,element,null,null);}
    public static RtaMonster Match(string ocr,IList<RtaMonster> all,string element,Bitmap portrait,string iconDir){
      if(all==null||all.Count==0)return null;
      string el=Fold(element??"");
      bool card=LooksLikeMonsterCard(ocr);
      if(!card&&el.Length==0)return null;
      string hay=WordHay(ocr);
      var hits=new List<RtaMonster>();
      int bestLen=0;
      for(int i=0;i<all.Count;i++){
        var m=all[i];if(m==null||string.IsNullOrEmpty(m.Name))continue;
        string fn=Fold(m.Name).Trim();if(fn.Length<3)continue;
        string key=BareName(fn);if(key.Length<3)continue;
        if(!ContainsWord(hay,key))continue;
        if(key.Length>bestLen){hits.Clear();bestLen=key.Length;hits.Add(m);}
        else if(key.Length==bestLen)hits.Add(m);
      }
      if(hits.Count==0){
        if(!card)return null;
        return Fuzzy(hay,all,el,portrait,iconDir);
      }
      return PickOne(hits,el,portrait,iconDir);
    }
    public static string ElementOf(RtaMonster m){
      if(m==null)return "";
      string e=Fold(m.Element??"");
      if(e=="water"||e=="fire"||e=="wind"||e=="light"||e=="dark")return e;
      int d=m.Id%10;
      if(d==1)return "water";if(d==2)return "fire";if(d==3)return "wind";if(d==4)return "light";if(d==5)return "dark";
      return "";
    }
    static readonly string[] GemNames=new string[]{"water","fire","wind","light","dark"};
    static Bitmap[] gemBmps;static bool gemTried;static int lastGemX,lastGemY,lastGemS;
    public static void LoadElementGems(string dir){
      DisposeGems();
      gemTried=true;
      if(string.IsNullOrEmpty(dir)||!Directory.Exists(dir))return;
      var loaded=new Bitmap[5];
      for(int i=0;i<5;i++){
        string p=Path.Combine(dir,"el-"+GemNames[i]+".png");
        if(!File.Exists(p)){for(int j=0;j<i;j++)loaded[j].Dispose();return;}
        using(var fs=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
        using(var img=Image.FromStream(fs))loaded[i]=new Bitmap(img);
      }
      gemBmps=loaded;
    }
    static void DisposeGems(){
      if(gemBmps==null)return;
      for(int i=0;i<gemBmps.Length;i++)if(gemBmps[i]!=null)gemBmps[i].Dispose();
      gemBmps=null;
    }
    static void EnsureGems(){
      if(gemTried)return;
      string[] dirs=new string[]{
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"assets","rta"),
        Path.GetFullPath(Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"..","assets","rta"))
      };
      for(int i=0;i<dirs.Length;i++){
        if(File.Exists(Path.Combine(dirs[i],"el-water.png"))){LoadElementGems(dirs[i]);return;}
      }
      gemTried=true;
    }
    public static string DetectElement(Bitmap bmp){
      lastGemS=0;
      if(bmp==null||bmp.Width<20||bmp.Height<20)return "";
      string gem=MatchGemIcons(bmp);
      if(gem.Length>0)return gem;
      return DetectElementHue(bmp);
    }
    static string DetectElementHue(Bitmap bmp){
      string bestEl="";int bestMargin=-1;int bestVotes=0;
      double[] starts=new double[]{.10,.14,.18,.22,.26};
      for(int b=0;b<starts.Length;b++){
        int y0=Math.Max(1,(int)(bmp.Height*starts[b]));
        int y1=Math.Min(bmp.Height,y0+Math.Max(8,(int)(bmp.Height*.055)));
        int x0=(int)(bmp.Width*.40),x1=(int)(bmp.Width*.48);
        if(x1-x0<8||y1-y0<6)continue;
        int fire=0,water=0,wind=0,light=0,dark=0;
        for(int y=y0;y<y1;y++){
          for(int x=x0;x<x1;x++){
            Color c=bmp.GetPixel(x,y);
            float h=c.GetHue(),s=c.GetSaturation(),br=c.GetBrightness();
            if(s<0.18f||br<0.16f)continue;
            if(h>18&&h<42&&s<0.62f&&br<0.50f)continue;
            if(h>=300&&h<348)continue;
            if(br>0.50f&&h>=32&&h<58&&s>0.34f)continue;
            if(s<0.40f&&br>0.58f&&br<0.92f){light++;continue;}
            if(s<0.38f||br>0.88f)continue;
            if(h<=16||h>=348)fire++;
            else if(h<40&&s>0.52f)fire++;
            else if(h>=48&&h<78&&s>0.45f)wind++;
            else if(h>=165&&h<215)water++;
            else if(h>=255&&h<295)dark++;
          }
        }
        int w0=0,w1=0;string el="";
        ScoreEl(fire,"fire",ref w0,ref w1,ref el);
        ScoreEl(water,"water",ref w0,ref w1,ref el);
        ScoreEl(wind,"wind",ref w0,ref w1,ref el);
        ScoreEl(light,"light",ref w0,ref w1,ref el);
        ScoreEl(dark,"dark",ref w0,ref w1,ref el);
        int margin=w0-w1;
        if(w0>=5&&(margin>bestMargin||(margin==bestMargin&&w0>bestVotes))){
          bestMargin=margin;bestVotes=w0;bestEl=el;
        }
      }
      return bestVotes>=5?bestEl:"";
    }
    static void ScoreEl(int n,string name,ref int w0,ref int w1,ref string el){
      if(n>w0){w1=w0;w0=n;el=name;}
      else if(n>w1)w1=n;
    }
    static string MatchGemIcons(Bitmap bmp){
      int w=bmp.Width,h=bmp.Height;
      int stride;byte[] img=Copy32(bmp,out stride);
      var lab=new byte[w*h];
      for(int y=0;y<h;y++){
        int row=y*stride,baseI=y*w;
        for(int x=0;x<w;x++){
          int i=row+x*4,r=img[i+2],g=img[i+1],b=img[i];
          byte tag=0;
          if(IsEl(r,g,b,0))tag=1;
          else if(IsEl(r,g,b,1))tag=2;
          else if(IsEl(r,g,b,2))tag=3;
          else if(IsEl(r,g,b,3))tag=4;
          else if(IsEl(r,g,b,4))tag=5;
          lab[baseI+x]=tag;
        }
      }
      int[] sizes=new int[]{20,26,32,40};
      long[] best=new long[]{0,0,0,0,0};
      int[] bx=new int[5];int[] by=new int[5];int[] bs=new int[5];
      int step=4;
      for(int el=0;el<5;el++){
        byte tag=(byte)(el+1);
        for(int s=0;s<sizes.Length;s++){
          int sz=sizes[s];
          if(sz>=w||sz>=h)continue;
          int minInner=sz*sz/7;
          for(int y=2;y+sz<h-2;y+=step){
            int yMid=(y+sz/2)*w;
            for(int x=2;x+sz<w-2;x+=step){
              if(lab[yMid+x+sz/2]!=tag)continue;
              int inner=CountLab(lab,w,x,y,x+sz,y+sz,h,tag);
              if(inner<minInner)continue;
              int pad=sz;
              int wide=CountLab(lab,w,x-pad,y-pad,x+sz+pad,y+sz+pad,h,tag);
              if(wide>inner*11/5)continue;
              int bar=CountLab(lab,w,x-sz*3,y,x+sz*4,y+sz,h,tag);
              if(bar>inner*2)continue;
              long score=((long)inner*1000)/(sz*sz);
              if(score>best[el]){best[el]=score;bx[el]=x;by[el]=y;bs[el]=sz;}
            }
          }
        }
      }
      int win=-1;long top=0,second=0;
      for(int i=0;i<5;i++){
        if(best[i]>top){second=top;top=best[i];win=i;}
        else if(best[i]>second)second=best[i];
      }
      if(win<0||top<160)return "";
      if(second>0&&top*100L<second*118L)return "";
      lastGemX=bx[win];lastGemY=by[win];lastGemS=bs[win];
      return GemNames[win];
    }
    static int CountLab(byte[] lab,int w,int x0,int y0,int x1,int y1,int h,byte tag){
      if(x0<0)x0=0;if(y0<0)y0=0;if(x1>w)x1=w;if(y1>h)y1=h;
      int n=0;
      for(int y=y0;y<y1;y++){
        int row=y*w;
        for(int x=x0;x<x1;x++)if(lab[row+x]==tag)n++;
      }
      return n;
    }
    static bool IsEl(int r,int g,int b,int el){
      if(el==0)return b>90&&b>r+28&&b>g+8&&g>35;
      if(el==1)return r>130&&r>g+18&&r>b+40&&g<175;
      if(el==2)return r>140&&g>105&&b<115&&r>b+40&&g>b+22&&r-g<85&&g-r<40;
      if(el==3){
        int max=r>g?r:g;if(b>max)max=b;
        int min=r<g?r:g;if(b<min)min=b;
        return min>130&&max>175&&max-min<72;
      }
      return b>95&&r>65&&g<r&&g<b&&(r+b)>g*2+30&&(b-g)>25;
    }
    static byte[] Copy32(Bitmap bmp,out int stride){
      var data=bmp.LockBits(new Rectangle(0,0,bmp.Width,bmp.Height),ImageLockMode.ReadOnly,PixelFormat.Format32bppArgb);
      stride=data.Stride;
      var buf=new byte[stride*bmp.Height];
      Marshal.Copy(data.Scan0,buf,0,buf.Length);
      bmp.UnlockBits(data);
      return buf;
    }
    public static async Task<string> ReadBitmap(Bitmap bmp){
      if(bmp==null)return "";
      string a="";
      try{a=await ScreenCaptureLens.ReadPlainText(bmp);}catch{}
      if(!string.IsNullOrEmpty(a)&&LooksLikeMonsterCard(a))return a;
      var parts=new List<string>();
      if(!string.IsNullOrEmpty(a))parts.Add(a);
      double[] tops=new double[]{0,.16,.28};
      for(int i=0;i<tops.Length;i++){
        int y=Math.Max(0,(int)(bmp.Height*tops[i]));
        int h=Math.Max(24,(int)(bmp.Height*.20));
        if(y+h>bmp.Height)h=bmp.Height-y;if(h<16)continue;
        try{
          using(var band=new Bitmap(bmp.Width,h,PixelFormat.Format32bppArgb)){
            using(var g=Graphics.FromImage(band))g.DrawImage(bmp,new Rectangle(0,0,band.Width,band.Height),new Rectangle(0,y,bmp.Width,h),GraphicsUnit.Pixel);
            string b=await ScreenCaptureLens.ReadPlainText(band);if(!string.IsNullOrEmpty(b))parts.Add(b);
          }
        }catch{}
        if(LooksLikeMonsterCard(string.Join("\n",parts.ToArray())))break;
      }
      return string.Join("\n",parts.ToArray());
    }
    public static bool HasGameWindow(){return FindGame()!=IntPtr.Zero;}
    public static async Task<RtaPeekShot> ReadGame(){
      var shot=new RtaPeekShot();
      using(var full=GrabGame()){
        if(full==null)return shot;
        shot.Element=DetectElement(full);
        string gemName="";
        if(lastGemS>0){
          try{gemName=await ReadGemName(full);}catch{}
        }
        using(var crop=CenterCrop(full)){
          if(crop==null){shot.Text=gemName??"";return shot;}
          shot.Portrait=CropPortrait(crop);
          if(!string.IsNullOrEmpty(gemName)&&gemName.Trim().Length>=3)shot.Text=gemName;
          else shot.Text=await ReadBitmap(crop);
        }
        if(!string.IsNullOrEmpty(gemName)&&shot.Text.IndexOf(gemName,StringComparison.Ordinal)<0)
          shot.Text=string.IsNullOrEmpty(shot.Text)?gemName:(shot.Text+"\n"+gemName);
      }
      return shot;
    }
    static async Task<string> ReadGemName(Bitmap bmp){
      if(bmp==null||lastGemS<=0)return "";
      int x=Math.Max(0,lastGemX+lastGemS/2);
      int y=Math.Max(0,lastGemY-lastGemS);
      int w=Math.Min(bmp.Width-x,Math.Max(220,lastGemS*14));
      int h=Math.Min(bmp.Height-y,Math.Max(48,lastGemS*4));
      if(w<40||h<20)return "";
      using(var band=new Bitmap(w,h,PixelFormat.Format32bppArgb)){
        using(var g=Graphics.FromImage(band))g.DrawImage(bmp,new Rectangle(0,0,w,h),new Rectangle(x,y,w,h),GraphicsUnit.Pixel);
        return await ScreenCaptureLens.ReadPlainText(band);
      }
    }
    [StructLayout(LayoutKind.Sequential)]struct RECT{public int Left,Top,Right,Bottom;}
    delegate bool EnumWindowsProc(IntPtr hWnd,IntPtr lParam);
    [DllImport("user32.dll")]static extern bool EnumWindows(EnumWindowsProc lpEnumFunc,IntPtr lParam);
    [DllImport("user32.dll",CharSet=CharSet.Unicode)]static extern int GetWindowText(IntPtr hWnd,StringBuilder lpString,int nMaxCount);
    [DllImport("user32.dll")]static extern bool IsWindowVisible(IntPtr hWnd);
    [DllImport("user32.dll")]static extern bool GetWindowRect(IntPtr hWnd,out RECT lpRect);
    [DllImport("user32.dll")]static extern bool PrintWindow(IntPtr hwnd,IntPtr hdcBlt,uint nFlags);
    static IntPtr FindGame(){
      IntPtr best=IntPtr.Zero;int bestArea=0;
      EnumWindowsProc cb=delegate(IntPtr h,IntPtr l){
        if(!IsWindowVisible(h))return true;
        var sb=new StringBuilder(260);
        if(GetWindowText(h,sb,sb.Capacity)<=0)return true;
        string f=Fold(sb.ToString());
        if(f.IndexOf("rune manager")>=0)return true;
        if(f.IndexOf("summoners war")<0&&f.IndexOf("summoner war")<0)return true;
        RECT r;if(!GetWindowRect(h,out r))return true;
        int area=Math.Max(0,r.Right-r.Left)*Math.Max(0,r.Bottom-r.Top);
        if(area>bestArea){best=h;bestArea=area;}
        return true;
      };
      EnumWindows(cb,IntPtr.Zero);
      return best;
    }
    static Bitmap GrabGame(){
      try{
        IntPtr hwnd=FindGame();
        if(hwnd==IntPtr.Zero)return null;
        RECT r;if(!GetWindowRect(hwnd,out r))return null;
        int w=r.Right-r.Left,h=r.Bottom-r.Top;
        if(w<200||h<160)return null;
        var full=new Bitmap(w,h,PixelFormat.Format32bppArgb);
        using(var g=Graphics.FromImage(full))g.CopyFromScreen(new Point(r.Left,r.Top),Point.Empty,new Size(w,h),CopyPixelOperation.SourceCopy);
        if(MostlyBlack(full)){
          using(var g=Graphics.FromImage(full)){
            IntPtr hdc=g.GetHdc();
            try{
              if(!PrintWindow(hwnd,hdc,2))PrintWindow(hwnd,hdc,0);
            }finally{g.ReleaseHdc(hdc);}
          }
        }
        return full;
      }catch{return null;}
    }
    static Bitmap CenterCrop(Bitmap full){
      if(full==null)return null;
      int w=full.Width,h=full.Height;
      int cw=Math.Max(220,(int)(w*.86));
      int ch=Math.Max(180,(int)(h*.82));
      int x=Math.Max(0,(w-cw)/2);
      int y=Math.Max(0,(int)(h*.08));
      if(x+cw>w)cw=w-x;if(y+ch>h)ch=h-y;
      var crop=new Bitmap(cw,ch,PixelFormat.Format32bppArgb);
      using(var g=Graphics.FromImage(crop))g.DrawImage(full,new Rectangle(0,0,cw,ch),new Rectangle(x,y,cw,ch),GraphicsUnit.Pixel);
      return crop;
    }
    static bool MostlyBlack(Bitmap bmp){
      try{
        int w=bmp.Width,h=bmp.Height,n=0,dark=0;
        for(int y=h/8;y<h;y+=Math.Max(8,h/20)){
          for(int x=w/8;x<w;x+=Math.Max(8,w/20)){
            Color c=bmp.GetPixel(x,y);n++;
            if(c.R<18&&c.G<18&&c.B<18)dark++;
          }
        }
        return n>0&&dark*10>=n*9;
      }catch{return false;}
    }
    static string Compact(string s){
      string n=Fold(s);
      var sb=new StringBuilder(n.Length);
      foreach(char c in n)if(char.IsLetterOrDigit(c))sb.Append(c);
      return sb.ToString();
    }
    static string WordHay(string s){
      string n=Fold(s);
      var sb=new StringBuilder(n.Length+2);
      sb.Append(' ');
      foreach(char c in n)sb.Append(char.IsLetterOrDigit(c)?c:' ');
      sb.Append(' ');
      return sb.ToString();
    }
    static bool ContainsWord(string hay,string needle){
      int i=0;
      while(true){
        i=hay.IndexOf(needle,i,StringComparison.Ordinal);
        if(i<0)return false;
        char before=i==0?' ':hay[i-1];
        int afterI=i+needle.Length;
        char after=afterI>=hay.Length?' ':hay[afterI];
        if(!char.IsLetterOrDigit(before)&&!char.IsLetterOrDigit(after))return true;
        i++;
      }
    }
    static Bitmap CropPortrait(Bitmap bmp){
      if(bmp==null||bmp.Width<40||bmp.Height<40)return null;
      int x=(int)(bmp.Width*.28),y=(int)(bmp.Height*.32);
      int w=(int)(bmp.Width*.44),h=(int)(bmp.Height*.48);
      if(x+w>bmp.Width)w=bmp.Width-x;
      if(y+h>bmp.Height)h=bmp.Height-y;
      if(w<16||h<16)return null;
      var crop=new Bitmap(w,h,PixelFormat.Format32bppArgb);
      using(var g=Graphics.FromImage(crop))g.DrawImage(bmp,new Rectangle(0,0,w,h),new Rectangle(x,y,w,h),GraphicsUnit.Pixel);
      return crop;
    }
    static RtaMonster Fuzzy(string hay,IList<RtaMonster> all,string el,Bitmap portrait,string iconDir){
      var tokens=hay.Split(new[]{' '},StringSplitOptions.RemoveEmptyEntries);
      var hits=new List<RtaMonster>();
      int bestDist=2;
      for(int t=0;t<tokens.Length;t++){
        string tok=tokens[t];
        if(tok.Length<5||Noise(tok))continue;
        for(int i=0;i<all.Count;i++){
          var m=all[i];if(m==null||string.IsNullOrEmpty(m.Name))continue;
          string fn=Fold(m.Name).Trim();
          string key=BareName(fn);
          if(key.Length<5||Math.Abs(key.Length-tok.Length)>1)continue;
          int d=Dist(tok,key);if(d==0||d>1)continue;
          if(d<bestDist){hits.Clear();bestDist=d;hits.Add(m);}
          else if(d==bestDist)hits.Add(m);
        }
      }
      return PickOne(hits,el,portrait,iconDir);
    }
    static RtaMonster PickOne(List<RtaMonster> hits,string el,Bitmap portrait,string iconDir){
      if(hits==null||hits.Count==0)return null;
      if(hits.Count==1)return hits[0];
      List<RtaMonster> filtered=hits;
      if(el.Length>0){
        var keep=new List<RtaMonster>();
        for(int i=0;i<hits.Count;i++)if(ElementOf(hits[i])==el)keep.Add(hits[i]);
        if(keep.Count>0)filtered=keep;
      }else{
        var iconHit=BestIcon(hits,portrait,iconDir);
        if(iconHit!=null){
          string ie=ElementOf(iconHit);
          var keep=new List<RtaMonster>();
          for(int i=0;i<hits.Count;i++)if(ElementOf(hits[i])==ie)keep.Add(hits[i]);
          if(keep.Count>0)filtered=keep;
        }else{
          string first=ElementOf(hits[0]);
          for(int i=1;i<hits.Count;i++)if(ElementOf(hits[i])!=first)return null;
        }
      }
      if(filtered.Count==1)return filtered[0];
      RtaMonster best=filtered[0];int bestScore=DupScore(best);
      for(int i=1;i<filtered.Count;i++){
        int s=DupScore(filtered[i]);
        if(s>bestScore){best=filtered[i];bestScore=s;}
      }
      return best;
    }
    static RtaMonster BestIcon(List<RtaMonster> hits,Bitmap portrait,string iconDir){
      if(portrait==null||string.IsNullOrEmpty(iconDir)||hits==null||hits.Count<2)return null;
      var bestByEl=new Dictionary<string,long>(StringComparer.Ordinal);
      var hitByEl=new Dictionary<string,RtaMonster>(StringComparer.Ordinal);
      for(int i=0;i<hits.Count;i++){
        long d=LoadIconDist(portrait,iconDir,hits[i].Id);
        if(d<0)continue;
        string e=ElementOf(hits[i]);
        if(e.Length==0)e="?";
        long prev;
        if(!bestByEl.TryGetValue(e,out prev)||d<prev){bestByEl[e]=d;hitByEl[e]=hits[i];}
      }
      if(hitByEl.Count==0)return null;
      if(hitByEl.Count==1){
        foreach(var kv in hitByEl)return kv.Value;
      }
      string bestEl="";long best=long.MaxValue,second=long.MaxValue;
      foreach(var kv in bestByEl){
        if(kv.Value<best){second=best;best=kv.Value;bestEl=kv.Key;}
        else if(kv.Value<second)second=kv.Value;
      }
      if(bestEl.Length==0||best*118L>=second*100L)return null;
      RtaMonster hit;hitByEl.TryGetValue(bestEl,out hit);return hit;
    }
    static long LoadIconDist(Bitmap portrait,string iconDir,int id){
      int[] tryIds=new int[]{id,id+10,id-10};
      long best=-1;
      for(int i=0;i<tryIds.Length;i++){
        string p=Path.Combine(iconDir,tryIds[i]+".png");
        if(!File.Exists(p))continue;
        try{
          using(var fs=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite))
          using(var img=Image.FromStream(fs))
          using(var bmp=new Bitmap(img)){
            long d=IconDist(portrait,bmp);
            if(best<0||d<best)best=d;
          }
        }catch{}
      }
      return best;
    }
    static long IconDist(Bitmap a,Bitmap b){
      const int n=20;
      using(var aa=new Bitmap(a,n,n))
      using(var bb=new Bitmap(b,n,n)){
        long s=0;int c=0;
        for(int y=2;y<n-2;y++){
          for(int x=2;x<n-2;x++){
            Color ca=aa.GetPixel(x,y),cb=bb.GetPixel(x,y);
            s+=Math.Abs(ca.R-cb.R)+Math.Abs(ca.G-cb.G)+Math.Abs(ca.B-cb.B);
            c++;
          }
        }
        return c>0?s/c:999999;
      }
    }
    static string BareName(string fn){
      if(fn.StartsWith("water "))return fn.Substring(6);
      if(fn.StartsWith("fire "))return fn.Substring(5);
      if(fn.StartsWith("wind "))return fn.Substring(5);
      if(fn.StartsWith("light "))return fn.Substring(6);
      if(fn.StartsWith("dark "))return fn.Substring(5);
      return fn;
    }
    static int DupScore(RtaMonster m){
      int s=0;
      if(m.PickRate>0||m.Played>0)s+=20;
      if((m.Id/10)%10==1)s+=5;
      string n=Fold(m.Name??"");
      if(!(n.StartsWith("water ")||n.StartsWith("fire ")||n.StartsWith("wind ")||n.StartsWith("light ")||n.StartsWith("dark ")))s+=3;
      return s;
    }
    static bool Noise(string tok){
      return tok=="attack"||tok=="leader"||tok=="select"||tok=="monsters"||tok=="monster"||tok=="battle"||tok=="normal"||tok=="accuracy"||tok=="resistance"||tok=="assassin"||tok=="creed"||tok=="maxlv";
    }
    static int Dist(string a,string b){
      int n=a.Length,m=b.Length;
      if(Math.Abs(n-m)>1)return 99;
      if(n==0)return m;if(m==0)return n;
      var prev=new int[m+1];var cur=new int[m+1];
      for(int j=0;j<=m;j++)prev[j]=j;
      for(int i=1;i<=n;i++){
        cur[0]=i;
        for(int j=1;j<=m;j++){
          int cost=a[i-1]==b[j-1]?0:1;
          int del=prev[j]+1,ins=cur[j-1]+1,sub=prev[j-1]+cost;
          int v=del<ins?del:ins;if(sub<v)v=sub;cur[j]=v;
        }
        var tmp=prev;prev=cur;cur=tmp;
      }
      return prev[m];
    }
  }
}
