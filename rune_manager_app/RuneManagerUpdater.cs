using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace RuneManagerModernUpdater {
  static class Program {
    [STAThread]
    static void Main(string[] args) {
      try {
        string target=Arg(args,"--target")??DetectInstallRoot();
        int waitPid;int.TryParse(Arg(args,"--wait")??"0",out waitPid);
        if(string.IsNullOrEmpty(target)||!LooksLikeInstall(target)){
          MessageBox.Show("Rune Manager introuvable.\r\nLance l'appli une fois, puis relance cette mise à jour.","Mise à jour Rune Manager",MessageBoxButtons.OK,MessageBoxIcon.Information);
          return;
        }
        if(waitPid>0){
          try{Process.GetProcessById(waitPid).WaitForExit(120000);}catch{}
        }
        StopApp(target);
        string data=Path.Combine(target,"Donnees");
        Directory.CreateDirectory(data);
        string core=Path.Combine(data,"Rune_Manager_Modern_Core.exe");
        string pending=Path.Combine(data,"Rune_Manager_Modern_Core.pending.exe");
        ExtractPayload(pending);
        ReplaceFile(pending,core);
        try{File.Delete(pending);}catch{}
        string launcher=Path.Combine(target,"Rune_Manager_Modern.exe");
        string start=File.Exists(launcher)?launcher:core;
        Process.Start(new ProcessStartInfo(start){UseShellExecute=true,WorkingDirectory=File.Exists(launcher)?target:data});
      } catch(Exception ex) {
        MessageBox.Show("La mise à jour n'a pas pu être terminée :\r\n"+ex.Message+"\r\n\r\nFerme Rune Manager puis relance cet exe.","Erreur de mise à jour",MessageBoxButtons.OK,MessageBoxIcon.Error);
      }
    }
    static string Arg(string[] args,string name){
      for(int i=0;i<args.Length-1;i++)if(string.Equals(args[i],name,StringComparison.OrdinalIgnoreCase))return args[i+1];
      return null;
    }
    static string DetectInstallRoot(){
      foreach(string name in new[]{"Rune_Manager_Modern_Core","Rune_Manager_Modern"}){
        foreach(var p in Process.GetProcessesByName(name)){
          try{
            string path=p.MainModule.FileName;
            string dir=Path.GetDirectoryName(path);
            if(string.Equals(name,"Rune_Manager_Modern_Core",StringComparison.OrdinalIgnoreCase)){
              var parent=Directory.GetParent(dir);if(parent!=null)dir=parent.FullName;
            }
            if(LooksLikeInstall(dir))return dir;
          }catch{}
        }
      }
      string here=AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
      if(LooksLikeInstall(here))return here;
      var up=Directory.GetParent(here);if(up!=null&&LooksLikeInstall(up.FullName))return up.FullName;
      return null;
    }
    static bool LooksLikeInstall(string root){
      if(string.IsNullOrEmpty(root)||!Directory.Exists(root))return false;
      return File.Exists(Path.Combine(root,"Rune_Manager_Modern.exe"))||File.Exists(Path.Combine(root,"Donnees","Rune_Manager_Modern_Core.exe"));
    }
    static void StopApp(string target){
      foreach(string name in new[]{"Rune_Manager_Modern","Rune_Manager_Modern_Core"}){
        foreach(var process in Process.GetProcessesByName(name)){
          try{
            string path=process.MainModule.FileName;
            if(path.StartsWith(target,StringComparison.OrdinalIgnoreCase)){
              process.CloseMainWindow();
              if(!process.WaitForExit(2500)){process.Kill();process.WaitForExit(2500);}
            }
          }catch{}
        }
      }
      Thread.Sleep(400);
    }
    static void ExtractPayload(string dest){
      using(var src=Assembly.GetExecutingAssembly().GetManifestResourceStream("Payload.Core")){
        if(src==null)throw new InvalidOperationException("Le moteur de mise à jour est absent de l'exe.");
        using(var dst=File.Create(dest)){
          var buf=new byte[64*1024];int n;
          while((n=src.Read(buf,0,buf.Length))>0)dst.Write(buf,0,n);
        }
      }
    }
    static void ReplaceFile(string pending,string core){
      for(int i=0;i<40;i++){
        try{File.Copy(pending,core,true);if(new FileInfo(core).Length==new FileInfo(pending).Length)return;}catch{}
        Thread.Sleep(250);
      }
      throw new IOException("Impossible de remplacer Rune_Manager_Modern_Core.exe.");
    }
  }
}
