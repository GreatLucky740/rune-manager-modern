using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Windows.Forms;

namespace RuneManagerModernInstaller {
  static class Program {
    [STAThread]
    static void Main() {
      Application.EnableVisualStyles();
      string source=AppDomain.CurrentDomain.BaseDirectory.TrimEnd(Path.DirectorySeparatorChar);
      string target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"Rune Manager Modern");
      var answer=MessageBox.Show(
        "Rune Manager Modern va être installé ou mis à jour.\r\n\r\n"+
        "L'application ouverte sera fermée, puis tous les exécutables et les données seront remplacés.\r\n"+
        "Tes réglages personnels seront conservés.\r\n\r\nContinuer ?",
        "Installation / mise à jour Rune Manager",MessageBoxButtons.YesNo,MessageBoxIcon.Information);
      if(answer!=DialogResult.Yes)return;
      try {
        Directory.CreateDirectory(target);
        StopInstalledApplication(target);
        CopyDirectory(source,target);
        CreateDesktopShortcut(target);
        string exe=Path.Combine(target,"Rune_Manager_Modern.exe");
        if(!File.Exists(exe))throw new FileNotFoundException("L'exécutable principal manque dans l'archive.",exe);
        MessageBox.Show("Rune Manager Modern est à jour.\r\n\r\nL'exécutable principal et le moteur ont bien été remplacés.","Mise à jour terminée",MessageBoxButtons.OK,MessageBoxIcon.Information);
        Process.Start(new ProcessStartInfo(exe){UseShellExecute=true,WorkingDirectory=target});
      } catch(Exception ex) {
        MessageBox.Show("La mise à jour n'a pas pu être terminée :\r\n"+ex.Message+"\r\n\r\nFerme Rune Manager puis relance l'installateur.","Erreur d'installation",MessageBoxButtons.OK,MessageBoxIcon.Error);
      }
    }
    static void StopInstalledApplication(string target) {
      foreach(string name in new[]{"Rune_Manager_Modern","Rune_Manager_Modern_Core"})foreach(var process in Process.GetProcessesByName(name))try {
        string path=process.MainModule.FileName;
        if(path.StartsWith(target,StringComparison.OrdinalIgnoreCase)){process.CloseMainWindow();if(!process.WaitForExit(2500)){process.Kill();process.WaitForExit(2500);}}
      } catch {}
      Thread.Sleep(250);
    }
    static void CopyDirectory(string source,string target) {
      foreach(string directory in Directory.GetDirectories(source,"*",SearchOption.AllDirectories)) {
        string relative=directory.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar);
        Directory.CreateDirectory(Path.Combine(target,relative));
      }
      string installer=Assembly.GetExecutingAssembly().Location;
      foreach(string file in Directory.GetFiles(source,"*",SearchOption.AllDirectories)) {
        if(string.Equals(file,installer,StringComparison.OrdinalIgnoreCase))continue;
        string relative=file.Substring(source.Length).TrimStart(Path.DirectorySeparatorChar);
        string destination=Path.Combine(target,relative);
        Directory.CreateDirectory(Path.GetDirectoryName(destination));
        File.Copy(file,destination,true);
      }
    }
    static void CreateDesktopShortcut(string target) {
      Type type=Type.GetTypeFromProgID("WScript.Shell");if(type==null)return;
      dynamic shell=Activator.CreateInstance(type);
      string shortcutPath=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory),"Rune Manager Modern.lnk");
      dynamic shortcut=shell.CreateShortcut(shortcutPath);
      shortcut.TargetPath=Path.Combine(target,"Rune_Manager_Modern.exe");shortcut.WorkingDirectory=target;shortcut.Description="Rune Manager Modern";shortcut.Save();
    }
  }
}
