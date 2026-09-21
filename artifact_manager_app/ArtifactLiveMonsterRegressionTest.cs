using System;
using System.Collections.Generic;
using System.Linq;
using ArtifactManagerModern;

static class ArtifactLiveMonsterRegressionTest {
  static int Main(string[] args){
    if(args.Length<1)return 2;
    var before=EngineBridge.Run(args[0],"");
    string summon="{\"command\":\"SummonUnit\",\"ret_code\":0,\"unit_list\":[{\"unit_id\":99999999999,\"unit_master_id\":10211,\"unit_level\":1,\"class\":3,\"skills\":[[1,1]],\"artifacts\":[]}]}";
    var after=EngineBridge.Run(args[0],"","","",new List<string>{summon});
    int changed=before.Join(after,x=>x.Rid,x=>x.Rid,(x,y)=>Math.Abs(x.Potential-y.Potential)>.0001||x.Preset!=y.Preset?1:0).Sum();
    Console.WriteLine("LIVE_MONSTER_CHANGED="+changed);return changed>0?0:1;
  }
}
