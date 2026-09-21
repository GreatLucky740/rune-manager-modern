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
        string root=AppDomain.CurrentDomain.BaseDirectory;
        string data=Path.Combine(root,"Donnees");
        string core=Path.Combine(data,"Rune_Manager_Modern_Core.exe");
        if(!File.Exists(core))throw new FileNotFoundException("Le moteur Rune Manager est absent.",core);
        var start=new ProcessStartInfo(core){UseShellExecute=true,WorkingDirectory=data};
        if(args.Length>0)start.Arguments=string.Join(" ",args.Select(Quote).ToArray());
        Process.Start(start);
      } catch(Exception ex) {
        MessageBox.Show("Rune Manager ne peut pas démarrer :\r\n"+ex.Message,"Rune Manager",MessageBoxButtons.OK,MessageBoxIcon.Error);
      }
    }
    static string Quote(string value){return "\""+(value??"").Replace("\"","\\\"")+"\"";}
  }
}
