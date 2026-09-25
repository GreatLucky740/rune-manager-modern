using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace RuneManagerModernLauncher {
  static class Program {
    [STAThread]
    static void Main(string[] args) {
      try {
        string root=ExeDirectory();
        string core=FindCore(root);
        if(core==null)throw new FileNotFoundException(MissingCoreMessage(root));
        string data=Path.GetDirectoryName(core);
        var start=new ProcessStartInfo(core){UseShellExecute=true,WorkingDirectory=data};
        if(args.Length>0)start.Arguments=string.Join(" ",args.Select(Quote).ToArray());
        Process.Start(start);
      } catch(Exception ex) {
        MessageBox.Show("Rune Manager ne peut pas démarrer :\r\n"+ex.Message,"Rune Manager",MessageBoxButtons.OK,MessageBoxIcon.Error);
      }
    }
    static string ExeDirectory(){
      try{
        string path=Application.ExecutablePath;
        if(!string.IsNullOrEmpty(path))return Path.GetFullPath(Path.GetDirectoryName(path));
      }catch{}
      try{
        string path=Process.GetCurrentProcess().MainModule.FileName;
        if(!string.IsNullOrEmpty(path))return Path.GetFullPath(Path.GetDirectoryName(path));
      }catch{}
      return Path.GetFullPath(AppDomain.CurrentDomain.BaseDirectory);
    }
    static string FindCore(string root){
      var seen=new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach(string dir in CandidateDirs(root)){
        if(string.IsNullOrEmpty(dir)||!Directory.Exists(dir)||!seen.Add(dir))continue;
        foreach(string name in new[]{"Rune_Manager_Modern_Core.exe","Rune_Manager_Modern_Core.pending.exe"}){
          string path=Path.Combine(dir,name);
          if(LooksLikeCore(path))return path;
        }
        try{
          foreach(var file in new DirectoryInfo(dir).GetFiles("*.exe")){
            if(file.Name.IndexOf("Core",StringComparison.OrdinalIgnoreCase)>=0&&LooksLikeCore(file.FullName))return file.FullName;
          }
        }catch{}
      }
      return null;
    }
    static string[] CandidateDirs(string root){
      string parent=null;
      try{var p=Directory.GetParent(root);if(p!=null)parent=p.FullName;}catch{}
      return new[]{
        Path.Combine(root,"Donnees"),
        root,
        Path.Combine(root,"Rune_Manager_Modern","Donnees"),
        parent==null?null:Path.Combine(parent,"Donnees"),
        parent==null?null:Path.Combine(parent,"Rune_Manager_Modern","Donnees")
      };
    }
    static bool LooksLikeCore(string path){
      try{return File.Exists(path)&&new FileInfo(path).Length>1000000;}catch{return false;}
    }
    static string MissingCoreMessage(string root){
      string expected=Path.Combine(root,"Donnees","Rune_Manager_Modern_Core.exe");
      return "Le petit exe ne trouve pas le moteur, alors que tu peux lancer Core.exe à la main.\r\n\r\nCherché depuis :\r\n"+root+"\r\n\r\nFichier attendu :\r\n"+expected+"\r\n\r\nEn attendant : ouvre Donnees et lance Rune_Manager_Modern_Core.exe.\r\nSinon place Rune_Manager_Modern.exe à côté du dossier Donnees (pas dedans, pas tout seul sur le Bureau).";
    }
    static string Quote(string value){return "\""+(value??"").Replace("\"","\\\"")+"\"";}
  }
}
