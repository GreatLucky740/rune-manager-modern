using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
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
    static string CustomPath { get { return Path.Combine(AppDomain.CurrentDomain.BaseDirectory,"rta-custom-pool.json"); } }
    static Dictionary<string,object> D(object x){ return x as Dictionary<string,object>; }
    static object[] A(object x){ return x as object[]??new object[0]; }
    static object G(Dictionary<string,object> d,string k,object z=null){ object v; return d!=null&&d.TryGetValue(k,out v)?v:z; }
    static int I(object x){ int n; return int.TryParse(Convert.ToString(x,CultureInfo.InvariantCulture),out n)?n:0; }
    static long L(object x){ long n; return long.TryParse(Convert.ToString(x,CultureInfo.InvariantCulture),out n)?n:0; }
    static double F(object x){ double n; return double.TryParse(Convert.ToString(x,CultureInfo.InvariantCulture),NumberStyles.Any,CultureInfo.InvariantCulture,out n)?n:0; }
    static string S(object x){ return Convert.ToString(x??""); }
    static readonly JavaScriptSerializer Js=new JavaScriptSerializer{MaxJsonLength=int.MaxValue,RecursionLimit=128};

    public static void ClearSnapshot(){ try{ if(File.Exists(SnapPath)) File.Delete(SnapPath);}catch{} }
    public static void ClearCustom(){ try{ if(File.Exists(CustomPath)) File.Delete(CustomPath);}catch{} }
    public static bool HasCustom(){ return LoadCustomIds().Count>0; }
    public static List<int> LoadCustomIds(){
      var ids=new List<int>();
      try{
        if(!File.Exists(CustomPath)) return ids;
        var root=D(Js.DeserializeObject(File.ReadAllText(CustomPath)));
        foreach(var x in A(G(root,"ids"))){ int n=I(x); if(n>0&&!ids.Contains(n)) ids.Add(n); }
      }catch{}
      return ids;
    }
    public static void SaveCustomIds(List<int> ids){
      try{
        var list=(ids??new List<int>()).Where(x=>x>0).Distinct().ToList();
        File.WriteAllText(CustomPath,Js.Serialize(new Dictionary<string,object>{{"ids",list.ToArray()}}));
      }catch{}
    }

    public static List<RtaPoolMember> OwnedPool(string json,string catalog,List<RtaMonster> meta,int poolSize,bool ignoreCustom=false){
      var result=new List<RtaPoolMember>();
      if(string.IsNullOrEmpty(json)||!File.Exists(json)||meta==null) return result;
      try{
        var root=D(Js.DeserializeObject(Read(json)));
        var names=new Dictionary<int,string>(); var groups=new Dictionary<int,int>(); var elements=new Dictionary<int,string>();
        if(!string.IsNullOrEmpty(catalog)&&File.Exists(catalog))
          foreach(var x in A(Js.DeserializeObject(File.ReadAllText(catalog)))){ var d=D(x); int id=I(G(d,"id")); if(id>0){ names[id]=S(G(d,"name","Monstre "+id)); groups[id]=I(G(d,"skillgroup")); elements[id]=S(G(d,"element")); } }
        var metaBy=meta.Where(x=>x.Id>0).GroupBy(x=>x.Id).ToDictionary(g=>g.Key,g=>g.OrderByDescending(x=>x.Played).First());
        var custom=ignoreCustom?new List<int>():LoadCustomIds();
        var customSet=new HashSet<int>(custom);
        var ownedMasters=new HashSet<int>();
        var units=new List<RtaPoolMember>();
        foreach(var x in A(G(root,"unit_list"))){
          var d=D(x); int m=I(G(d,"unit_master_id")); if(m<=0) continue;
          ownedMasters.Add(m);
          RtaMonster direct; metaBy.TryGetValue(m,out direct);
          int group; string element; RtaMonster groupBest=null;
          if(groups.TryGetValue(m,out group)&&group>0&&elements.TryGetValue(m,out element))
            groupBest=metaBy.Values.Where(c=>groups.ContainsKey(c.Id)&&groups[c.Id]==group&&elements.ContainsKey(c.Id)&&string.Equals(elements[c.Id],element,StringComparison.OrdinalIgnoreCase)).OrderByDescending(c=>c.Played).FirstOrDefault();
          RtaMonster rm=direct!=null&&(groupBest==null||direct.Played>=groupBest.Played)?direct:groupBest;
          if(rm==null){
            if(!customSet.Contains(m)) continue;
            units.Add(new RtaPoolMember{Id=m,Name=names.ContainsKey(m)?names[m]:("Monstre "+m)});
            continue;
          }
          units.Add(new RtaPoolMember{Id=m,Name=names.ContainsKey(m)?names[m]:rm.Name,Played=rm.Played,PickRate=rm.PickRate});
        }
        result=units.GroupBy(x=>x.Id).Select(g=>g.OrderByDescending(x=>x.Played).First()).ToList();
        if(custom.Count>0){
          var byId=result.ToDictionary(x=>x.Id,x=>x);
          result=custom.Where(id=>ownedMasters.Contains(id)).Select(id=>{
            RtaPoolMember m;
            if(byId.TryGetValue(id,out m)) return m;
            return new RtaPoolMember{Id=id,Name=names.ContainsKey(id)?names[id]:("Monstre "+id)};
          }).ToList();
        }else result=result.OrderByDescending(x=>x.Played).ThenByDescending(x=>x.PickRate).Take(Math.Max(10,poolSize)).ToList();
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

    static string Fold(string s){
      if(string.IsNullOrEmpty(s))return "";
      string n=s.ToLowerInvariant().Normalize(NormalizationForm.FormD);
      var sb=new StringBuilder(n.Length);
      foreach(char c in n)if(CharUnicodeInfo.GetUnicodeCategory(c)!=UnicodeCategory.NonSpacingMark)sb.Append(c);
      return sb.ToString();
    }
    static bool NameMatch(string fold,string q){
      if(string.IsNullOrEmpty(q))return true;
      if(fold.IndexOf(q)>=0)return true;
      int i=0;foreach(char c in fold){if(i<q.Length&&c==q[i])i++;}
      return i==q.Length;
    }
    static int NameScore(string fold,string q){
      if(string.IsNullOrEmpty(q))return 0;
      if(fold.StartsWith(q,StringComparison.Ordinal))return 0;
      int at=fold.IndexOf(q);
      if(at>=0)return 10+at;
      return 100;
    }
    static string DisplayName(string name){
      if(string.IsNullOrEmpty(name))return "";
      string eq=Loc.T("rta_owned_eq");
      if(!string.IsNullOrEmpty(eq)&&name.EndsWith(eq,StringComparison.Ordinal))return name.Substring(0,name.Length-eq.Length).Trim();
      return name;
    }
    static void BindClick(Control c,EventHandler h){
      c.Cursor=Cursors.Hand;c.Click+=h;
      foreach(Control x in c.Controls)BindClick(x,h);
    }
    public static Dictionary<int,int> RtaPriority(string json,string catalog,List<RtaMonster> meta,int poolSize){
      var rank=new Dictionary<int,int>();
      try{
        int n=0;
        foreach(var m in OwnedPool(json,catalog,meta,Math.Max(10,poolSize),true)){
          if(m==null||m.Id<=0||rank.ContainsKey(m.Id))continue;
          rank[m.Id]=n++;
        }
      }catch{}
      return rank;
    }

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
      var f=new Form{Text=Loc.T("rta_chg_title",diff.PoolSize),BackColor=Color.FromArgb(7,13,22),ForeColor=Color.White,StartPosition=FormStartPosition.CenterParent,Size=new Size(760,520),MinimumSize=new Size(620,380),Icon=owner!=null?owner.Icon:null};
      var title=new Label{Text=Loc.T("rta_chg_head",diff.PoolSize),Dock=DockStyle.Top,Height=42,Padding=new Padding(18,12,18,0),Font=new Font("Segoe UI Semibold",12),ForeColor=Color.FromArgb(20,184,210)};
      var cols=new TableLayoutPanel{Dock=DockStyle.Fill,ColumnCount=2,RowCount=2,Padding=new Padding(16,8,16,8)};
      cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50)); cols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,50));
      cols.RowStyles.Add(new RowStyle(SizeType.AutoSize)); cols.RowStyles.Add(new RowStyle(SizeType.Percent,100));
      cols.Controls.Add(new Label{Text=Loc.T("rta_chg_in",diff.Entered.Count),ForeColor=Color.FromArgb(80,220,140),Font=new Font("Segoe UI Semibold",11),AutoSize=true,Margin=new Padding(4,4,4,8)},0,0);
      cols.Controls.Add(new Label{Text=Loc.T("rta_chg_out",diff.Left.Count),ForeColor=Color.FromArgb(255,120,115),Font=new Font("Segoe UI Semibold",11),AutoSize=true,Margin=new Padding(4,4,4,8)},1,0);
      cols.Controls.Add(MakeList(diff.Entered,icon,Color.FromArgb(80,220,140)),0,1);
      cols.Controls.Add(MakeList(diff.Left,icon,Color.FromArgb(255,120,115)),1,1);
      var ok=new Button{Text="OK",DialogResult=DialogResult.OK,Width=120,Height=34,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(36,137,112),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",10),Dock=DockStyle.Right};
      var bar=new Panel{Dock=DockStyle.Bottom,Height=52,Padding=new Padding(16,8,16,8)}; bar.Controls.Add(ok);
      f.Controls.Add(cols); f.Controls.Add(bar); f.Controls.Add(title); f.AcceptButton=ok;
      f.ShowDialog(owner);
    }

    static Control MakeList(List<RtaPoolMember> rows,Func<int,Image> icon,Color nameColor){
      var box=new Panel{Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.FromArgb(10,20,32),Margin=new Padding(4)};
      if(rows==null||rows.Count==0){ box.Controls.Add(new Label{Text=Loc.T("rta_chg_none"),ForeColor=Color.Silver,Font=new Font("Segoe UI",10),Location=new Point(12,14),AutoSize=true}); return box; }
      int y=8;
      foreach(var m in rows){
        var row=new Panel{Location=new Point(8,y),Size=new Size(320,44),Anchor=AnchorStyles.Left|AnchorStyles.Top|AnchorStyles.Right};
        var pic=new PictureBox{Location=new Point(2,2),Size=new Size(40,40),SizeMode=PictureBoxSizeMode.Zoom,Image=icon!=null?icon(m.Id):null};
        var name=new Label{Text=string.IsNullOrEmpty(m.Name)?Loc.T("rta_monster_n",m.Id):m.Name,Location=new Point(48,10),Size=new Size(260,24),ForeColor=nameColor,Font=new Font("Segoe UI Semibold",10),AutoEllipsis=true};
        row.Controls.Add(pic); row.Controls.Add(name); box.Controls.Add(row); y+=48;
      }
      return box;
    }

    static int PriorityOf(Dictionary<int,int> rank,int id){
      int n;return rank!=null&&rank.TryGetValue(id,out n)?n:100000;
    }
    public static bool ShowEditor(Form owner,Dictionary<int,string> owned,Func<int,Image> icon,string json,string catalogPath,List<RtaMonster> meta,int poolSize){
      var chosen=new List<int>(LoadCustomIds());
      var names=owned??new Dictionary<int,string>();
      var rtaRank=RtaPriority(json,catalogPath,meta,poolSize);
      var roster=new List<int>(names.Keys);
      var folds=new Dictionary<int,string>(roster.Count);
      foreach(int id in roster){
        string nm;if(!names.TryGetValue(id,out nm)||string.IsNullOrEmpty(nm))nm="Monstre "+id;
        folds[id]=Fold(nm)+" "+Fold(DisplayName(nm))+" "+id.ToString(CultureInfo.InvariantCulture);
      }
      roster.Sort(delegate(int a,int b){
        int c=PriorityOf(rtaRank,a).CompareTo(PriorityOf(rtaRank,b));if(c!=0)return c;
        string na,nb;names.TryGetValue(a,out na);names.TryGetValue(b,out nb);
        return string.Compare(DisplayName(na),DisplayName(nb),StringComparison.OrdinalIgnoreCase);
      });
      var f=new Form{Text=Loc.T("rta_pool_edit_title"),BackColor=Color.FromArgb(7,13,22),ForeColor=Color.White,Size=new Size(920,640),MinimumSize=new Size(720,480),StartPosition=FormStartPosition.CenterParent,Icon=owner!=null?owner.Icon:null};
      var head=new Panel{Dock=DockStyle.Top,Height=96,BackColor=Color.FromArgb(15,25,39)};
      var title=new Label{Text=Loc.T("rta_pool_edit_title"),Location=new Point(16,10),AutoSize=true,ForeColor=Color.FromArgb(20,184,210),Font=new Font("Segoe UI Semibold",14)};
      var hint=new Label{Text=Loc.T("rta_pool_edit_hint"),Location=new Point(16,38),Size=new Size(880,22),ForeColor=Color.Silver};
      var search=new TextBox{Location=new Point(16,64),Size=new Size(560,24),BackColor=Color.FromArgb(18,28,42),ForeColor=Color.White,BorderStyle=BorderStyle.FixedSingle,Font=new Font("Segoe UI",12)};
      head.Controls.Add(title);head.Controls.Add(hint);head.Controls.Add(search);
      var picked=new BufferedFlow{Dock=DockStyle.Top,Height=118,AutoScroll=true,WrapContents=false,BackColor=Color.FromArgb(10,20,32),Padding=new Padding(8,8,8,4)};
      var catalog=new BufferedFlow{Dock=DockStyle.Fill,AutoScroll=true,WrapContents=true,BackColor=Color.FromArgb(7,13,22),Padding=new Padding(8)};
      var empty=new Label{AutoSize=true,Location=new Point(16,16),ForeColor=Color.Silver,Font=new Font("Segoe UI",10)};
      catalog.Controls.Add(empty);
      var bar=new Panel{Dock=DockStyle.Bottom,Height=54,BackColor=Color.FromArgb(15,25,39)};
      var count=new Label{AutoSize=true,Location=new Point(16,16),ForeColor=Color.Gainsboro,Font=new Font("Segoe UI Semibold",10)};
      var clear=new Button{Text=Loc.T("rta_pool_clear"),AutoSize=true,Height=34,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(125,65,65),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",9)};
      var save=new Button{Text=Loc.T("rta_pool_save"),AutoSize=true,Height=34,FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(36,137,112),ForeColor=Color.White,Font=new Font("Segoe UI Semibold",9)};
      bar.Controls.Add(count);bar.Controls.Add(clear);bar.Controls.Add(save);
      bar.Resize+=(s,e)=>{save.Location=new Point(Math.Max(count.Right+12,bar.ClientSize.Width-save.Width-16),10);clear.Location=new Point(save.Left-clear.Width-8,10);};
      bool savedOk=false;
      var tiles=new Dictionary<int,Control>();
      var tips=new ToolTip{AutoPopDelay=8000,InitialDelay=400,ReshowDelay=100,ShowAlways=true};
      var filterTimer=new Timer{Interval=35};
      Action<int> toggle=null;
      Action paintPicked=null;
      Action paintCatalog=null;
      Action mark=null;
      mark=delegate{
        var selected=new HashSet<int>(chosen);
        foreach(var kv in tiles){
          var p=kv.Value as Panel;if(p==null)continue;
          bool on=selected.Contains(kv.Key);
          p.BackColor=on?Color.FromArgb(18,52,48):Color.FromArgb(20,34,49);
          foreach(Control c in p.Controls){var lb=c as Label;if(lb!=null)lb.ForeColor=on?Color.FromArgb(180,255,220):Color.White;}
        }
        count.Text=Loc.T("rta_pool_count",chosen.Count);
      };
      toggle=delegate(int id){
        if(id<=0)return;
        if(chosen.Contains(id))chosen.Remove(id);else chosen.Add(id);
        paintPicked();
        mark();
      };
      Func<int,bool,Control> makeTile=delegate(int id,bool inPool){
        string raw;if(!names.TryGetValue(id,out raw)||string.IsNullOrEmpty(raw))raw="Monstre "+id;
        string shown=DisplayName(raw);
        bool isRta=rtaRank.ContainsKey(id);
        var p=new Panel{Width=112,Height=104,Margin=new Padding(4,3,4,3),BackColor=inPool?Color.FromArgb(18,52,48):Color.FromArgb(20,34,49),Cursor=Cursors.Hand,Tag=id};
        if(isRta)p.Controls.Add(new Panel{Bounds=new Rectangle(0,0,112,3),BackColor=Color.FromArgb(20,184,210)});
        var pic=new PictureBox{Image=icon!=null?icon(id):null,Bounds=new Rectangle(32,8,48,48),SizeMode=PictureBoxSizeMode.Zoom,BackColor=Color.Transparent};
        var nm=new Label{Text=shown,AutoSize=false,Bounds=new Rectangle(2,58,108,44),ForeColor=inPool?Color.FromArgb(180,255,220):Color.White,Font=new Font("Segoe UI Semibold",9f),TextAlign=ContentAlignment.TopCenter};
        p.Controls.Add(pic);p.Controls.Add(nm);
        int captured=id;
        EventHandler click=delegate(object s,EventArgs e){toggle(captured);};
        BindClick(p,click);
        tips.SetToolTip(p,raw+(isRta?"  •  RTA":""));
        tips.SetToolTip(nm,raw+(isRta?"  •  RTA":""));
        return p;
      };
      Func<int,Control> catalogTile=delegate(int id){
        Control cached;
        if(!tiles.TryGetValue(id,out cached)){cached=makeTile(id,chosen.Contains(id));tiles[id]=cached;}
        return cached;
      };
      paintPicked=delegate{
        picked.SuspendLayout();picked.Controls.Clear();
        foreach(int id in chosen)picked.Controls.Add(makeTile(id,true));
        picked.ResumeLayout();
      };
      paintCatalog=delegate{
        string q=Fold((search.Text??"").Trim());
        var hits=new List<int>();
        if(q.Length==0){
          foreach(int id in roster){hits.Add(id);if(hits.Count>=160)break;}
        }else{
          var scored=new List<KeyValuePair<int,int>>();
          foreach(int id in roster){
            string fold;if(!folds.TryGetValue(id,out fold))fold="";
            if(!NameMatch(fold,q))continue;
            scored.Add(new KeyValuePair<int,int>(id,NameScore(fold,q)));
          }
          scored.Sort(delegate(KeyValuePair<int,int> a,KeyValuePair<int,int> b){
            int c=PriorityOf(rtaRank,a.Key).CompareTo(PriorityOf(rtaRank,b.Key));if(c!=0)return c;
            c=a.Value.CompareTo(b.Value);if(c!=0)return c;
            string na,nb;names.TryGetValue(a.Key,out na);names.TryGetValue(b.Key,out nb);
            return string.Compare(DisplayName(na),DisplayName(nb),StringComparison.OrdinalIgnoreCase);
          });
          int n=Math.Min(160,scored.Count);
          for(int i=0;i<n;i++)hits.Add(scored[i].Key);
        }
        catalog.SuspendLayout();catalog.Controls.Clear();
        if(hits.Count==0){
          empty.Text=q.Length==0?Loc.T("rta_pool_type"):Loc.T("rta_pool_none");
          catalog.Controls.Add(empty);
        }else foreach(int id in hits)catalog.Controls.Add(catalogTile(id));
        catalog.ResumeLayout();
        mark();
      };
      filterTimer.Tick+=(s,e)=>{filterTimer.Stop();paintCatalog();};
      search.TextChanged+=(s,e)=>{filterTimer.Stop();filterTimer.Start();};
      clear.Click+=(s,e)=>{chosen.Clear();paintPicked();mark();};
      save.Click+=(s,e)=>{
        if(chosen.Count==0){ClearCustom();savedOk=false;f.DialogResult=DialogResult.OK;f.Close();return;}
        SaveCustomIds(chosen);savedOk=true;f.DialogResult=DialogResult.OK;f.Close();
      };
      f.FormClosed+=(s,e)=>{filterTimer.Stop();filterTimer.Dispose();};
      f.Controls.Add(catalog);f.Controls.Add(picked);f.Controls.Add(bar);f.Controls.Add(head);
      count.Text=Loc.T("rta_pool_count",chosen.Count);
      paintPicked();
      paintCatalog();
      f.Shown+=(s,e)=>search.Focus();
      f.ShowDialog(owner);
      return savedOk;
    }
  }
}
