using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Windows.Forms;
class PreviewRuneStyles {
 [STAThread] static int Main(string[] args){
  var a=Assembly.LoadFrom(Path.GetFullPath("outputs/Rune_Manager_Principal_Update.exe"));var t=a.GetType("RuneManagerModern.MainForm");var flags=BindingFlags.NonPublic|BindingFlags.Instance;
  using(var f=(Form)Activator.CreateInstance(t)){
   var sets=(IDictionary)t.GetField("setIcons",flags).GetValue(f);var slots=(Image[])t.GetField("slotLayers",flags).GetValue(f);
   foreach(var path in Directory.GetFiles(Path.Combine(args[0],"sets"),"*.png"))sets[Path.GetFileNameWithoutExtension(path)]=Image.FromFile(path);
   for(int s=1;s<=6;s++)slots[s]=Image.FromFile(Path.Combine(args[0],"runes","_layers","slot"+s+".png"));
   var method=t.GetMethod("GetGameRuneIcon",flags);string[] names={"energy","guard","swift","blade","rage","focus","endure","fatal","despair","vampire","violent","nemesis","will","shield","revenge","destroy","fight","determination","enhance","accuracy","tolerance","seal","intangible"};
   foreach(var name in names)using(var stream=a.GetManifestResourceStream("RuneArt.glyph-"+name+".png")){if(stream==null)throw new Exception("Missing glyph "+name);using(var b=new Bitmap(stream))if(b.GetPixel(0,0).A!=0)throw new Exception("Opaque glyph background "+name);}
   foreach(string resource in new[]{"slot1","slot2","slot3","slot4","slot5","slot6","star"})
    using(var stream=a.GetManifestResourceStream("RuneArt."+resource+".png")){
     if(stream==null)throw new Exception("Missing embedded component "+resource);
     using(var b=new Bitmap(stream))if(b.GetPixel(0,0).A!=0)throw new Exception("Background is not transparent");
    }
   Func<int,int,bool,int,Bitmap> iconFor=(grade,stars,ancient,level)=>(Bitmap)method.Invoke(f,new object[]{"violent",3,grade,ancient,stars,level});
   Func<Bitmap,Bitmap,bool> differs=(left,right)=>{for(int y=0;y<256;y++)for(int x=0;x<256;x++)if(left.GetPixel(x,y)!=right.GetPixel(x,y))return true;return false;};
   var baseline=iconFor(4,5,false,12);
   if(!object.ReferenceEquals(baseline,iconFor(4,5,false,12)))throw new Exception("Cache not reused");
   if(!differs(baseline,iconFor(4,5,true,12))||!differs(baseline,iconFor(5,5,false,12))||!differs(baseline,iconFor(4,6,false,12))||!differs(baseline,iconFor(4,5,false,2)))throw new Exception("Equipment metadata lost");
   for(int level=0;level<=15;level++)if(iconFor(4,5,false,level)==null)throw new Exception("Missing level "+level);
   Console.WriteLine("PASS alpha, embedded assets, cache, rarity, stars, ancient flag, levels 0..15");
   using(var comparison=new Bitmap(512,256))using(var cg=Graphics.FromImage(comparison)){cg.Clear(Color.Black);cg.DrawImageUnscaled(baseline,0,0);cg.DrawImageUnscaled(iconFor(4,5,true,12),256,0);comparison.Save("outputs/rune-styles-comparison.png");}
   using(var sheet=new Bitmap(1060,40+names.Length*84))using(var g=Graphics.FromImage(sheet))using(var font=new Font("Segoe UI",10)){
    g.Clear(Color.Black);for(int s=1;s<=6;s++)g.DrawString("Slot "+s+" : N / A",font,Brushes.White,130+(s-1)*150,10);
    int count=0;for(int n=0;n<names.Length;n++){g.DrawString(names[n],font,Brushes.White,6,65+n*84);for(int s=1;s<=6;s++)for(int ancient=0;ancient<2;ancient++){
     var icon=(Image)method.Invoke(f,new object[]{names[n],s,4,ancient==1,6,12});if(icon==null)throw new Exception("Missing icon");g.DrawImage(icon,new Rectangle(126+(s-1)*150+ancient*72,40+n*84,72,72));count++;
    }}sheet.Save("outputs/rune-styles-all.png");Console.WriteLine("PASS "+count+" icons: 23 sets x 6 slots x 2 variants");
   }
   using(var sheet=new Bitmap(900,310))using(var g=Graphics.FromImage(sheet))using(var font=new Font("Segoe UI",12)){
    g.Clear(Color.Black);for(int s=1;s<=6;s++){g.DrawString("Slot "+s,font,Brushes.White,10+(s-1)*150,3);for(int ancient=0;ancient<2;ancient++){var icon=(Image)method.Invoke(f,new object[]{s%2==0?"violent":"blade",s,s%2==0?5:4,ancient==1,6,12});g.DrawImage(icon,new Rectangle(10+(s-1)*150,27+ancient*140,135,135));}}sheet.Save("outputs/rune-styles-preview.png");
   }
  }return 0;
 }
}
