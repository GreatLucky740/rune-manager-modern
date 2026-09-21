using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Web.Script.Serialization;
using System.Windows.Forms;

namespace RuneManagerModern {
  sealed class RtaPoolMember {
    public int Id; public string Name=""; public int Played; public double PickRate;
  }
  sealed class RtaPoolDiff {
    public int PoolSize; public List<RtaPoolMember> Current=new List<RtaPoolMember>(); public List<RtaPoolMember> Entered=new List<RtaPoolMember>(); public List<RtaPoolMember> Left=new List<RtaPoolMember>();
    public int Count { get { return Entered.Count+Left.Count; } }
  }
  static class RtaPoolTracker {
    static string SnapPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"rta-pool-snapshot.json"); } }
    static Dictionary<string,object> D(object x){ return x as Dictionary<string,object>; }
    static object[] A(object x){ return x as object[]??new object[0]; }
    static object G(Dictionary<string,object> d,string k,object z=null){ object v; return d!=null&&d.TryGetValue(k,out v)?v:z; }
    static int I(object x){ int n; return int.TryParse(Convert.ToString(x,CultureInfo.InvariantCulture),out n)?n:0; }
    static long L(object x){ long n; return long.TryParse(Convert.ToString(x,CultureInfo.InvariantCulture),out n)?n:0; }
    static double F(object x){ double n; return double.TryParse(Convert.ToString(x,CultureInfo.InvariantCulture),NumberStyles.Any,CultureInfo.InvariantCulture,out n)?n:0; }
    static string S(object x){ return Convert.ToString(x??""); }
    static readonly JavaScriptSerializer Js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=128};

    public static void ClearSnapshot(){ try{ if(File.Exists(SnapPath)) File.Delete(SnapPath);}catch{} }

    public static List<RtaPoolMember> OwnedPool(string json,string catalog,List<RtaMonster> meta,int poolSize){
      var result=new List<RtaPoolMember>();
      if(string.IsNullOrEmpty(json)||!File.Exists(json)||meta==null) return result;
      try{
        var root=D(Js.DeserializeObject(Read(json)));
        var names=new Dictionary<int,string>(); var groups=new Dictionary<int,int>(); var elements=new Dictionary<int,string>();
        if(!string.IsNullOrEmpty(catalog)&&File.Exists(catalog))
          foreach(var x in A(Js.DeserializeObject(File.ReadAllText(catalog)))){ var d=D(x); int id=I(G(d,"id")); if(id>0){ names[id]=S(G(d,"name","Monstre "+id)); groups[id]=I(G(d,"skillgroup")); elements[id]=S(G(d,"element")); } }
        var metaBy=meta.Where(x=>x.Id>0).GroupBy(x=>x.Id).ToDictionary(g=>g.Key,g=>g.OrderByDescending(x=>x.Played).First());
        var units=new List<RtaPoolMember>();
        foreach(var x in A(G(root,"unit_list"))){
          var d=D(x); int m=I(G(d,"unit_master_id")); if(m<=0) continue;
          RtaMonster direct; metaBy.TryGetValue(m,out direct);
          int group; string element; RtaMonster groupBest=null;
          if(groups.TryGetValue(m,out group)&&group>0&&elements.TryGetValue(m,out element))
            groupBest=metaBy.Values.Where(c=>groups.ContainsKey(c.Id)&&groups[c.Id]==group&&elements.ContainsKey(c.Id)&&string.Equals(elements[c.Id],element,StringComparison.OrdinalIgnoreCase)).OrderByDescending(c=>c.Played).FirstOrDefault();
          RtaMonster rm=direct!=null&&(groupBest==null||direct.Played>=groupBest.Played)?direct:groupBest;
          if(rm==null) continue;
          units.Add(new RtaPoolMember{Id=m,Name=names.ContainsKey(m)?names[m]:rm.Name,Played=rm.Played,PickRate=rm.PickRate});
        }
        result=units.GroupBy(x=>x.Id).Select(g=>g.OrderByDescending(x=>x.Played).First()).OrderByDescending(x=>x.Played).ThenByDescending(x=>x.PickRate).Take(Math.Max(10,poolSize)).ToList();
      }catch{}
      return result;
    }

    public static HashSet<int> PickableIds(string json,string catalog,List<RtaMonster> meta,int poolSize){
      var ids=new HashSet<int>(OwnedPool(json,catalog,meta,poolSize).Select(x=>x.Id));
      if(ids.Count==0||string.IsNullOrEmpty(catalog)||!File.Exists(catalog)) return ids;
      try{
        var groups=new Dictionary<int,int>(); var elements=new Dictionary<int,string>();
        foreach(var x in A(Js.DeserializeObject(File.ReadAllText(catalog)))){
          var d=D(x); int id=I(G(d,"id")); if(id<=0) continue;
          groups[id]=I(G(d,"skillgroup")); elements[id]=S(G(d,"element"));
        }
        foreach(int ownedId in ids.ToList()){
          int group; string element;
          if(!groups.TryGetValue(ownedId,out group)||group<=0||!elements.TryGetValue(ownedId,out element)) continue;
          foreach(var kv in groups){
            string other;
            if(kv.Value==group&&elements.TryGetValue(kv.Key,out other)&&string.Equals(element,other,StringComparison.OrdinalIgnoreCase)) ids.Add(kv.Key);
          }
        }
      }catch{}
      return ids;
    }

    static string Read(string p){ using(var f=new FileStream(p,FileMode.Open,FileAccess.Read,FileShare.ReadWrite|FileShare.Delete)) using(var r=new StreamReader(f)) return r.ReadToEnd(); }

    static Dictionary<int,List<RtaPoolMember>> LoadAll(){
      var map=new Dictionary<int,List<RtaPoolMember>>();
      try{
        if(!File.Exists(SnapPath)) return map;
        var root=D(Js.DeserializeObject(File.ReadAllText(SnapPath)));
        var pools=D(G(root,"pools"))??root;
        foreach(var kv in pools){
          int size; if(!int.TryParse(kv.Key,out size)) continue;
          var list=new List<RtaPoolMember>();
          foreach(var raw in A(kv.Value)){
            var d=D(raw); var arr=A(raw);
            if(d!=null) list.Add(new RtaPoolMember{Id=I(G(d,"id")),Name=S(G(d,"name"))});
            else if(arr.Length>=2) list.Add(new RtaPoolMember{Id=I(arr[0]),Name=S(arr[1])});
            else if(arr.Length==1||raw is int||raw is long) list.Add(new RtaPoolMember{Id=I(raw),Name="Monstre "+I(raw)});
          }
          map[size]=list;
        }
      }catch{}
      return map;
    }

    public static void SaveSnapshot(int poolSize,List<RtaPoolMember> current){
      try{
        var all=LoadAll();
        all[Math.Max(10,poolSize)]=(current??new List<RtaPoolMember>()).Select(x=>new RtaPoolMember{Id=x.Id,Name=x.Name}).ToList();
        var pools=new Dictionary<string,object>();
        foreach(var kv in all) pools[kv.Key.ToString(CultureInfo.InvariantCulture)]=kv.Value.Select(x=>new Dictionary<string,object>{{"id",x.Id},{"name",x.Name}}).ToArray();
        File.WriteAllText(SnapPath,Js.Serialize(new Dictionary<string,object>{{"updated",DateTime.UtcNow.ToString("o")},{"pools",pools}}));
      }catch{}
    }

    public static RtaPoolDiff Diff(string json,string catalog,List<RtaMonster> meta,int poolSize){
      int n=Math.Max(10,poolSize);
      var diff=new RtaPoolDiff{PoolSize=n,Current=OwnedPool(json,catalog,meta,n)};
      var all=LoadAll();
      List<RtaPoolMember> prev;
      if(!all.TryGetValue(n,out prev)||prev==null||prev.Count==0){
        SaveSnapshot(n,diff.Current);
        return diff;
      }
      var prevIds=new HashSet<int>(prev.Select(x=>x.Id));
      var nowIds=new HashSet<int>(diff.Current.Select(x=>x.Id));
      var prevBy=prev.GroupBy(x=>x.Id).ToDictionary(g=>g.Key,g=>g.First());
      diff.Entered=diff.Current.Where(x=>!prevIds.Contains(x.Id)).ToList();
      diff.Left=prev.Where(x=>!nowIds.Contains(x.Id)).Select(x=>prevBy[x.Id]).ToList();
      return diff;
    }

    public static void ShowChanges(Form owner,RtaPoolDiff diff,Func<int,Image> icon){
      if(diff==null) return;
      var f=new Form{Text="Top "+diff.PoolSize+"  •  changements meta",BackColor=Color.FromArgb(7,13,22),ForeColor=Color.White,StartPosition=FormStartPosition.CenterParent,Size=new Size(760,520),MinimumSize=new Size(620,380),Icon=owner!=null?owner.Icon:null};
      var title=new Label{Text="Monstres possédés qui rentrent ou sortent de ton top "+diff.PoolSize,Dock=DockStyle.Top,Height=42,Padding=new Padding(18,12,18,0),Font=new Font("Segoe UI Semibold",12),ForeColor=Color.FromArgb(20,184,210)};
      var cols=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(16,8,16,8)};
      cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
      cols.RowStyles.Add(new RowStyle(SizeType.AutoSize)); cols.RowStyles.Add(new RowStyle(SizeType.Percent,100));
      cols.Controls.Add(new Label{Text="ENTRENT  •  "+diff.Entered.Count,ForeColor=Color.FromArgb(80,220,140),Font=new Font("Segoe UI Semibold",11),AutoSize=true,Margin=new Padding(4,4,4,8)},0,0);
      cols.Controls.Add(new Label{Text="SORTENT  •  "+diff.Left.Count,ForeColor=Color.FromArgb(255,120,115),Font=new Font("Segoe UI Semibold",11),AutoSize=true,Margin=new Padding(4,4,4,8)},1,0);
      cols.Controls.Add(MakeList(diff.Entered,icon,Color.FromArgb(80,220,140)),0,1);
      cols.Controls.Add(MakeList(diff.Left,icon,Color.FromArgb(255,120,115)),1,1);
      var ok=new Button{Text="OK",DialogResult=DialogResult.OK,Width=120,Height=34,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(36,137,112),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10),Dock=DockStyle.Right};
      var bar=new Panel{Dock=DockStyle.Bottom,Height=52,Padding=new Padding(16,8,16,8)}; bar.Controls.Add(ok);
      f.Controls.Add(cols); f.Controls.Add(bar); f.Controls.Add(title); f.AcceptButton=ok;
      f.ShowDialog(owner);
    }

    static Control MakeList(List<RtaPoolMember> rows,Func<int,Image> icon,Color nameColor){
      var box=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.FromArgb(10,20,32),Margin=new Padding(4)};
      if(rows==null||rows.Count==0){ box.Controls.Add(new Label{Text="Aucun",ForeColor=Color.Silver,Font=new Font("Segoe UI",10),Location=new Point(12,14),AutoSize=true}); return box; }
      int y=8;
      foreach(var m in rows){
        var row=new Panel{Location=new Point(8,y),Size=new Size(320,44),Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right};
        var pic=new PictureBox{Location=new Point(2,2),Size=new Size(40,40),SizeMode=PictureBoxSizeMode.Zoom,Image=icon!=null?icon(m.Id):null};
        var name=new Label{Text=string.IsNullOrEmpty(m.Name)?"Monstre "+m.Id:m.Name,Location=new Point(48,10),Size=new Size(260,24),ForeColor=nameColor,Font=new Font("Segoe UI Semibold",10),AutoEllipsis=true};
        row.Controls.Add(pic); row.Controls.Add(name); box.Controls.Add(row); y+=48;
      }
      return box;
    }
  }
}
