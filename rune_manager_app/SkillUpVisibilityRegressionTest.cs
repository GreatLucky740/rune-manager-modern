using System;
using System.IO;
using System.Linq;
using RuneManagerModern;

static class SkillUpVisibilityRegressionTest {
  static int Main(){
    string dir=Path.Combine(Path.GetTempPath(),"rune_skillup_"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
    try {
      string catalog=Path.Combine(dir,"catalog.json"),profile=Path.Combine(dir,"profile.json");
      File.WriteAllText(catalog,"[{\"id\":21312,\"name\":\"Ludo\",\"family\":\"Dice Magician\",\"element\":\"fire\",\"stars\":4,\"skillups\":9,\"familyid\":21300,\"skillgroup\":21300},{\"id\":21314,\"name\":\"Tablo\",\"family\":\"Dice Magician\",\"element\":\"light\",\"stars\":4,\"skillups\":11,\"familyid\":21300,\"skillgroup\":21300}]");
      File.WriteAllText(profile,"{\"unit_list\":[{\"unit_id\":1,\"unit_master_id\":21314,\"class\":4,\"unit_level\":1,\"skills\":[[1,1]]}],\"unit_storage_list\":[{\"unit_master_id\":21302,\"class\":4,\"quantity\":1}]}");
      var groups=RuneEngine.AnalyzeSkillUps(profile,catalog);bool ok=groups.Count==1&&groups[0].Target.MasterId==21314&&groups[0].Available==1&&groups[0].Fodders[0].MasterId==21302&&groups[0].Fodders[0].CatalogId==21312;
      var fams=RuneEngine.AnalyzeSkillUpFamilies(profile,catalog);
      var dice=fams.FirstOrDefault(x=>x.FamilyId==21300);
      bool famOk=dice!=null&&dice.Elements.Count>=2;
      var light=dice==null?null:dice.Elements.FirstOrDefault(x=>x.Element=="light");
      var fire=dice==null?null:dice.Elements.FirstOrDefault(x=>x.Element=="fire");
      famOk=famOk&&light!=null&&light.Owned&&!light.FullySkilled&&light.Need>0;
      famOk=famOk&&fire!=null&&!fire.Owned;
      File.WriteAllText(catalog,"[{\"id\":26605,\"name\":\"마들렌맛 쿠키(어둠)\",\"family\":\"\",\"element\":\"dark\",\"stars\":4,\"skillups\":11,\"familyid\":26600,\"skillgroup\":26600},{\"id\":26615,\"name\":\"Madeleine Cookie\",\"family\":\"마들렌맛 쿠키(어둠)\",\"element\":\"dark\",\"stars\":4,\"skillups\":11,\"familyid\":26600,\"skillgroup\":26600},{\"id\":26613,\"name\":\"Madeleine Cookie\",\"family\":\"마들렌맛 쿠키(어둠)\",\"element\":\"wind\",\"stars\":4,\"skillups\":10,\"familyid\":26600,\"skillgroup\":26600},{\"id\":27115,\"name\":\"Dark Choco Knight\",\"family\":\"Choco Knight\",\"element\":\"dark\",\"stars\":4,\"skillups\":11,\"familyid\":27100,\"skillgroup\":26600},{\"id\":27113,\"name\":\"Wind Choco Knight\",\"family\":\"Choco Knight\",\"element\":\"wind\",\"stars\":4,\"skillups\":10,\"familyid\":27100,\"skillgroup\":26600},{\"id\":14711,\"name\":\"Camilla\",\"family\":\"Vampire\",\"element\":\"water\",\"stars\":4,\"skillups\":9,\"familyid\":14700,\"skillgroup\":14700},{\"id\":23015,\"name\":\"Eirgar\",\"family\":\"Vampire Lord\",\"element\":\"dark\",\"stars\":5,\"skillups\":11,\"familyid\":23000,\"skillgroup\":14700}]");
      File.WriteAllText(profile,"{\"unit_list\":[{\"unit_id\":2,\"unit_master_id\":26613,\"class\":4,\"unit_level\":40,\"skills\":[[1,11]]},{\"unit_id\":3,\"unit_master_id\":14711,\"class\":4,\"unit_level\":40,\"skills\":[[1,1]]}]}");
      var paired=RuneEngine.AnalyzeSkillUpFamilies(profile,catalog);
      var cookie=paired.FirstOrDefault(x=>x.FamilyId==26600||(x.Family??"").IndexOf("Madeleine",StringComparison.OrdinalIgnoreCase)>=0);
      bool collabOk=cookie!=null&&cookie.Rows.Count==2&&cookie.Rows[0].Collab&&cookie.Rows[0].Family.IndexOf("Madeleine",StringComparison.OrdinalIgnoreCase)>=0&&cookie.Rows[1].Family.IndexOf("Choco",StringComparison.OrdinalIgnoreCase)>=0;
      var vamp=paired.FirstOrDefault(x=>x.FamilyId==14700);
      collabOk=collabOk&&vamp!=null&&vamp.Rows.Count==1;
      File.WriteAllText(profile,"{\"unit_list\":[");
      bool keep=false;
      try{RuneEngine.AnalyzeSkillUpFamilies(profile,catalog);}catch{keep=true;}
      Console.WriteLine(ok?"UNAWAKENED_FODDER=OK":"UNAWAKENED_FODDER=FAIL");
      Console.WriteLine(famOk?"FAMILY_GAPS=OK":"FAMILY_GAPS=FAIL");
      Console.WriteLine(collabOk?"COLLAB_PAIR=OK":"COLLAB_PAIR=FAIL");
      Console.WriteLine(keep?"PARTIAL_JSON=OK":"PARTIAL_JSON=FAIL");
      return ok&&famOk&&collabOk&&keep?0:1;
    } finally {try{Directory.Delete(dir,true);}catch{}}
  }
}
