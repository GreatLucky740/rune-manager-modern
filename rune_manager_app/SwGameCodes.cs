using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace RuneManagerModern {
  sealed class SwGameCodeReward {
    public string Name, Qty, IconUrl;
    public Image Icon;
  }
  sealed class SwGameCode {
    public string Code, Added, HiveUrl;
    public readonly List<SwGameCodeReward> Rewards=new List<SwGameCodeReward>();
  }
  sealed class SwGameCodesRefresh {
    public List<SwGameCode> Codes=new List<SwGameCode>();
    public int Added, Removed;
  }
  static class SwGameCodes {
    public const string PageUrl="https://swgt.io/gamecodes/";
    static readonly object Gate=new object();
    static readonly Dictionary<string,Image> IconCache=new Dictionary<string,Image>(StringComparer.OrdinalIgnoreCase);
    static readonly HashSet<string> Used=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    static readonly Regex RowRe=new Regex(@"<tr[\s\S]*?</tr>",RegexOptions.IgnoreCase);
    static readonly Regex CodeRe=new Regex(@"data-gamecode=""([^""]+)""",RegexOptions.IgnoreCase);
    static readonly Regex DateRe=new Regex(@"<td class=""text-md-center"">([^<]+)</td>",RegexOptions.IgnoreCase);
    static readonly Regex RewardRe=new Regex(@"<div[^>]*title=""([^""]+)""[^>]*>[\s\S]*?src=""([^""]+)""[\s\S]*?</span>\s*([^<]+)",RegexOptions.IgnoreCase);
    static bool usedLoaded;

    public static List<SwGameCode> LastFetch { get; private set; }
    static string UsedPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"sw-codes-used.txt"); } }
    static string SeenPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"sw-codes-seen.txt"); } }

    public static bool IsUsed(string code){
      LoadUsed();
      lock(Gate)return !string.IsNullOrEmpty(code)&&Used.Contains(code);
    }
    public static int UnusedCount(){
      List<SwGameCode> list;lock(Gate)list=LastFetch;
      if(list==null)return 0;
      int n=0;foreach(var c in list)if(c!=null&&!IsUsed(c.Code))n++;
      return n;
    }
    public static void MarkUsed(string code,bool used){
      LoadUsed();
      if(string.IsNullOrEmpty(code))return;
      lock(Gate){if(used)Used.Add(code);else Used.Remove(code);}
      SaveUsed();
    }
    public static SwGameCodesRefresh Refresh(){
      LoadUsed();
      bool first=!File.Exists(SeenPath);
      var prev=LoadSeen();
      var list=Fetch();
      int added=0,removed=0;
      var live=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      foreach(var c in list)if(c!=null&&!string.IsNullOrEmpty(c.Code))live.Add(c.Code);
      if(!first){
        foreach(string code in live)if(!prev.Contains(code))added++;
        foreach(string code in prev)if(!live.Contains(code))removed++;
      }
      lock(Gate)LastFetch=list;
      PruneUsed(list);
      SaveSeen(list);
      return new SwGameCodesRefresh{Codes=list,Added=added,Removed=removed};
    }

    static List<SwGameCode> Fetch(){
      ServicePointManager.SecurityProtocol=(SecurityProtocolType)3072;
      var req=(HttpWebRequest)WebRequest.Create(PageUrl);
      req.Timeout=15000;req.ReadWriteTimeout=15000;
      req.UserAgent="RuneManager";
      req.AutomaticDecompression=DecompressionMethods.GZip|DecompressionMethods.Deflate;
      string html;
      using(var resp=req.GetResponse())
      using(var sr=new StreamReader(resp.GetResponseStream()))
        html=sr.ReadToEnd();
      var list=Parse(html);
      LoadIcons(list);
      return list;
    }

    public static List<SwGameCode> Parse(string html){
      var list=new List<SwGameCode>();
      if(string.IsNullOrEmpty(html))return list;
      foreach(Match row in RowRe.Matches(html)){
        string block=row.Value;
        var codeM=CodeRe.Match(block);
        if(!codeM.Success)continue;
        string code=codeM.Groups[1].Value.Trim();
        if(code.Length==0||list.Exists(x=>string.Equals(x.Code,code,StringComparison.OrdinalIgnoreCase)))continue;
        var item=new SwGameCode{Code=code,HiveUrl="http://withhive.me/313/"+code};
        var dateM=DateRe.Match(block);
        if(dateM.Success)item.Added=dateM.Groups[1].Value.Trim();
        foreach(Match rw in RewardRe.Matches(block)){
          item.Rewards.Add(new SwGameCodeReward{
            Name=rw.Groups[1].Value.Trim(),
            IconUrl=rw.Groups[2].Value.Trim(),
            Qty=rw.Groups[3].Value.Trim().Replace("&nbsp;","")
          });
        }
        list.Add(item);
      }
      return list;
    }

    static void LoadIcons(List<SwGameCode> codes){
      foreach(var c in codes){
        if(c==null)continue;
        foreach(var r in c.Rewards){
          if(r==null||string.IsNullOrEmpty(r.IconUrl))continue;
          Image cached;
          if(IconCache.TryGetValue(r.IconUrl,out cached)){r.Icon=cached;continue;}
          try{
            var req=(HttpWebRequest)WebRequest.Create(r.IconUrl);
            req.Timeout=8000;req.ReadWriteTimeout=8000;req.UserAgent="RuneManager";
            using(var resp=req.GetResponse())
            using(var stream=resp.GetResponseStream())
            using(var img=Image.FromStream(stream)){
              cached=new Bitmap(img);
              IconCache[r.IconUrl]=cached;
              r.Icon=cached;
            }
          }catch{}
        }
      }
    }

    static void LoadUsed(){
      lock(Gate){
        if(usedLoaded)return;
        usedLoaded=true;
        try{
          if(!File.Exists(UsedPath))return;
          foreach(string line in File.ReadAllLines(UsedPath,Encoding.UTF8)){
            string code=line.Trim();
            if(code.Length>0)Used.Add(code);
          }
        }catch{}
      }
    }
    static void SaveUsed(){
      string[] lines;lock(Gate)lines=new List<string>(Used).ToArray();
      try{File.WriteAllLines(UsedPath,lines,Encoding.UTF8);}catch{}
    }
    static HashSet<string> LoadSeen(){
      var set=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
      try{
        if(!File.Exists(SeenPath))return set;
        foreach(string line in File.ReadAllLines(SeenPath,Encoding.UTF8)){
          string code=line.Trim();
          if(code.Length>0)set.Add(code);
        }
      }catch{}
      return set;
    }
    static void SaveSeen(List<SwGameCode> list){
      try{
        var codes=new List<string>();
        if(list!=null)foreach(var c in list)if(c!=null&&!string.IsNullOrEmpty(c.Code))codes.Add(c.Code);
        File.WriteAllLines(SeenPath,codes.ToArray(),Encoding.UTF8);
      }catch{}
    }
    static void PruneUsed(List<SwGameCode> active){
      var drop=new List<string>();
      lock(Gate){
        if(active==null||Used.Count==0)return;
        var live=new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach(var c in active)if(c!=null&&!string.IsNullOrEmpty(c.Code))live.Add(c.Code);
        foreach(string code in Used)if(!live.Contains(code))drop.Add(code);
        if(drop.Count==0)return;
        foreach(string code in drop)Used.Remove(code);
      }
      if(drop.Count>0)SaveUsed();
    }
  }
}
