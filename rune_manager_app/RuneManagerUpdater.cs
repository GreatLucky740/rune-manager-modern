using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Threading;
using System.Windows.Forms;

namespace RuneManagerModernUpdater {
  static class Program {
    const string CoreUrl="https://github.com/GreatLucky740/rune-manager-modern/releases/latest/download/Rune_Manager_Modern_Core.exe";
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
        string url=Arg(args,"--url")??CoreUrl;
        if(!LooksLikeExe(pending)) DownloadCore(url,pending);
        if(!LooksLikeExe(pending)) throw new InvalidOperationException("Le fichier téléchargé n'est pas un exe valide.");
        ReplaceFile(pending,core);
        try{File.Delete(pending);}catch{}
        string launcher=Path.Combine(target,"Rune_Manager_Modern.exe");
        string start=File.Exists(launcher)?launcher:core;
        Process.Start(new ProcessStartInfo(start){UseShellExecute=true,WorkingDirectory=File.Exists(launcher)?target:data});
      } catch(Exception ex) {
        MessageBox.Show("La mise à jour n'a pas pu être terminée :\r\n"+ex.Message+"\r\n\r\nFerme Rune Manager puis relance cet exe, ou extraire le zip GitHub.","Erreur de mise à jour",MessageBoxButtons.OK,MessageBoxIcon.Error);
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
    static bool LooksLikeExe(string path){
      if(string.IsNullOrEmpty(path)||!File.Exists(path)||new FileInfo(path).Length<1024*1024)return false;
      try{using(var f=File.OpenRead(path)){byte[] b=new byte[2];return f.Read(b,0,2)==2&&b[0]==(byte)'M'&&b[1]==(byte)'Z';}}catch{return false;}
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
    static void DownloadCore(string url,string dest){
      var form=new Form{Text="Mise à jour Rune Manager",Width=420,Height=130,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false,StartPosition=FormStartPosition.CenterScreen};
      var lbl=new Label{Text="Téléchargement de la mise à jour…",AutoSize=true,Location=new System.Drawing.Point(18,18)};
      var bar=new ProgressBar{Location=new System.Drawing.Point(18,50),Width=370,Height=22,Style=ProgressBarStyle.Marquee,MarqueeAnimationSpeed=30};
      form.Controls.Add(lbl);form.Controls.Add(bar);
      form.Show();Application.DoEvents();
      try{
        ServicePointManager.SecurityProtocol=(SecurityProtocolType)3072;
        var req=(HttpWebRequest)WebRequest.Create(url);
        req.Timeout=180000;req.ReadWriteTimeout=180000;req.UserAgent="RuneManager-Updater";req.AllowAutoRedirect=true;
        using(var resp=req.GetResponse()){
          string host=(resp.ResponseUri!=null?resp.ResponseUri.Host:"").ToLowerInvariant();
          if(host.IndexOf("github")<0)throw new InvalidOperationException("Source de mise à jour inattendue : "+host);
          using(var input=resp.GetResponseStream())
          using(var output=File.Create(dest)){
            var buf=new byte[64*1024];
            int first=input.Read(buf,0,buf.Length);
            if(first<2||buf[0]!=(byte)'M'||buf[1]!=(byte)'Z')throw new InvalidOperationException("Le téléchargement n'est pas un exe.");
            output.Write(buf,0,first);
            int n;while((n=input.Read(buf,0,buf.Length))>0){output.Write(buf,0,n);Application.DoEvents();}
          }
        }
      } finally {
        try{form.Close();form.Dispose();}catch{}
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
