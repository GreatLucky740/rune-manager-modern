const fs = require('fs');
const out = process.argv[2];
const base = 'https://api.swarena.gg';
const sleep = ms => new Promise(r => setTimeout(r, ms));
async function json(url) {
  for (let i=0;i<4;i++) {
    const r=await fetch(url); const x=await r.json();
    if (x && !x.error) return x;
    await sleep(250*(i+1));
  }
  return {data:[],count:0};
}
async function main(){
  let monsters=[];
  for(let offset=0;offset<1000;offset+=25){
    const x=await json(`${base}/monsters?season=38&isG3=false&isSL=false&played=0&orderBy=played&orderDirection=DESC&limit=25&offset=${offset}`);
    monsters.push(...(x.data||[])); if((x.data||[]).length<25)break;
  }
  const relevant=monsters.filter(x=>x.played>=250).slice(0,180);
  const pairs={}; let done=0;
  async function one(m){
    let rows=[];
    for(let offset=0;offset<600;offset+=100){
      const x=await json(`${base}/monster/${m.monster_id}/pairs?season=38&isG3=false&searchPairName=&orderBy=total_played_against&orderDirection=DESC&minPlayedAgainst=0&minPlayedTogether=0&limit=100&offset=${offset}`);
      rows.push(...(x.data||[])); if((x.data||[]).length<100)break;
    }
    pairs[m.monster_id]=rows.filter(x=>x.total_played_against>=20||x.total_played_together>=20).map(x=>[x.b_monster_id,x.total_played_against,x.win_against_rate,x.total_played_together,x.win_together_rate]);
    done++; if(done%10===0)console.log(`pairs ${done}/${relevant.length}`);
  }
  for(let i=0;i<relevant.length;i+=8)await Promise.all(relevant.slice(i,i+8).map(one));
  const compact=monsters.map(x=>[x.monster_id,x.name,x.slug,x.image_filename,x.win_rate,x.pick_rate,x.ban_rate,x.lead_rate,x.played]);
  fs.writeFileSync(out,JSON.stringify({season:38,created:new Date().toISOString(),monsters:compact,pairs}));
  console.log(`saved ${monsters.length} monsters, ${Object.values(pairs).reduce((n,x)=>n+x.length,0)} pairs`);
}
main().catch(e=>{console.error(e);process.exit(1)});
