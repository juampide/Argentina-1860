import fs from 'fs'; import zlib from 'zlib';
const d=zlib.gunzipSync(fs.readFileSync('public/datos/ciudad.pak'));
const n=d.readUInt32LE(0); const h=JSON.parse(d.slice(4,4+n).toString());
console.log(JSON.stringify(h));
let off=4+n; const out={};
for(const p of h.partes){const raw=d.slice(off,off+p.bytes);const n2=p.bytes/p.registro;const s=Buffer.alloc(p.bytes);
 for(let e=0;e<p.registro;e++){const t=e*n2;for(let k=0;k<n2;k++)s[k*p.registro+e]=raw[t+k];}
 off+=p.bytes; out[p.nombre]=s; console.log(p.nombre,p.bytes,p.registro, 'items',p.bytes/p.registro);}
const f=new Float32Array(out.manzanas.buffer,out.manzanas.byteOffset,out.manzanas.length/4);
console.log('manzanas first 40 floats',Array.from(f.slice(0,40)).map(x=>+x.toFixed(1)));
const t=new Uint16Array(out.techos.buffer,out.techos.byteOffset,out.techos.length/2);
console.log('techos first 30',Array.from(t.slice(0,30)));
const m=new Uint16Array(out.muros.buffer,out.muros.byteOffset,out.muros.length/2);
console.log('muros first 30',Array.from(m.slice(0,30)));
const a=new Float32Array(out.arboles.buffer,out.arboles.byteOffset,out.arboles.length/4);
console.log('arboles first 12',Array.from(a.slice(0,12)).map(x=>+x.toFixed(1)));
const st=(a)=>{const s=[...a].sort((x,y)=>x-y);return [s[0],s[s.length>>1],s[s.length-1]]};
const A=[],B=[],W=[],H=[];
for(let i=0;i<3635;i++){A.push(f[i*10+8]);B.push(f[i*10+9]);
 const xs=[0,2,4,6].map(k=>f[i*10+k]),ys=[1,3,5,7].map(k=>f[i*10+k]);W.push(Math.max(...xs)-Math.min(...xs));H.push(Math.max(...ys)-Math.min(...ys));}
console.log('A',st(A),'B',st(B),'w',st(W),'h',st(H));
const hist={};A.forEach(v=>hist[v]=(hist[v]||0)+1);console.log('A hist',hist);
const hb={};B.forEach(v=>hb[v]=(hb[v]||0)+1);console.log('B hist',hb);
