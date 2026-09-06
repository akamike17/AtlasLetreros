export function textRaster(text,font,scale=1,spacing=1,align='left'){
 const lines=text.split('\n'),width=Math.max(1,...lines.map(l=>Math.max(0,l.length*(font.width+spacing)-spacing)))*scale,height=Math.max(1,lines.length*font.height*scale),pixels=new Uint8Array(width*height);
 lines.forEach((line,lineIndex)=>{const lineWidth=Math.max(0,line.length*(font.width+spacing)-spacing)*scale;const offset=align==='center'?Math.floor((width-lineWidth)/2):align==='right'?width-lineWidth:0;[...line].forEach((ch,i)=>{const glyph=font.glyphs[ch.codePointAt(0)]||font.glyphs[63];if(!glyph)return;glyph.rows.forEach((row,y)=>row.forEach((v,x)=>{if(!v)return;for(let sy=0;sy<scale;sy++)for(let sx=0;sx<scale;sx++){const px=offset+(i*(font.width+spacing)+glyph.x+x)*scale+sx,py=(lineIndex*font.height+font.ascent-glyph.h-glyph.y+y)*scale+sy;if(px>=0&&px<width&&py>=0&&py<height)pixels[py*width+px]=1;}}));});});
 return {width,height,mask:[...pixels]};
}
