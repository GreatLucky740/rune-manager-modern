using System;
using System.IO;
using System.Drawing;
using System.Reflection;
using System.Collections;
using System.Windows.Forms;
class VerifyOriginalRuneIcons {
 [STAThread] static void Main(string[] args){
  var flags=BindingFlags.Instance|BindingFlags.NonPublic;
  var old=Assembly.LoadFile(Path.GetFullPath(args[0])).GetType("RuneManagerModern.MainForm");
  var current=Assembly.LoadFile(Path.GetFullPath("outputs/Rune_Manager_Principal_Update.exe")).GetType("RuneManagerModern.MainForm");
  using(var a=(Form)Activator.CreateInstance(old))using(var b=(Form)Activator.CreateInstance(current))using(var sheet=new Bitmap(480,160))using(var g=Graphics.FromImage(sheet)){
   g.Clear(Color.Black);int count=0;
   foreach(var file in Directory.GetFiles(args[1],"*.png")){
    string set=Path.GetFileNameWithoutExtension(file);
    ((IDictionary)old.GetField("setIcons",flags).GetValue(a))[set]=Image.FromFile(file);
    ((IDictionary)current.GetField("setIcons",flags).GetValue(b))[set]=Image.FromFile(file);
    for(int slot=1;slot<=6;slot++)for(int ancient=0;ancient<2;ancient++){
     object[] p={set,4,12,slot,ancient==1};
     using(var left=(Bitmap)old.GetMethod("BuildWorldBossRuneIcon",flags).Invoke(a,p))using(var right=(Bitmap)current.GetMethod("BuildWorldBossRuneIcon",flags).Invoke(b,p)){
      for(int y=0;y<64;y++)for(int x=0;x<64;x++)if(left.GetPixel(x,y)!=right.GetPixel(x,y))throw new Exception("Mismatch "+set+" "+slot);
      if(set=="violent")g.DrawImageUnscaled(right,(slot-1)*80+8,ancient*80+8);
      count++;
     }
    }
   }
   sheet.Save("outputs/original-rune-icons-restored.png");Console.WriteLine("PASS: "+count+" original icons pixel-identical to backup.");
  }
 }
}
