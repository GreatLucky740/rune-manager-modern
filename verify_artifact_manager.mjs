import fs from 'node:fs/promises';
import { FileBlob, SpreadsheetFile } from '@oai/artifact-tool';

const path = 'outputs/artifact_manager_modern/Artifact_Manager_Modern_V2_SWLens.xlsx';
const wb = await SpreadsheetFile.importXlsx(await FileBlob.load(path));
const sheets = await wb.inspect({kind:'sheet',include:'id,name',maxChars:3000});
const monster = await wb.inspect({kind:'table',sheetId:'Monstre',range:'A1:O12',include:'values,formulas',tableMaxRows:12,tableMaxCols:15,maxChars:8000});
const errors = await wb.inspect({kind:'match',searchTerm:'#REF!|#DIV/0!|#VALUE!|#NAME\\?|#N/A',options:{useRegex:true,maxResults:100},summary:'final formula error scan',maxChars:4000});
const preview = await wb.render({sheetName:'Monstre',range:'A1:O12',scale:1.3,format:'png'});
await fs.writeFile('outputs/artifact_manager_modern/preview_Monstre_Lushen_fix.png',new Uint8Array(await preview.arrayBuffer()));
await fs.writeFile('outputs/artifact_manager_modern/verification_latest.ndjson',[sheets.ndjson,monster.ndjson,errors.ndjson].join('\n'));
console.log('verification terminee');
