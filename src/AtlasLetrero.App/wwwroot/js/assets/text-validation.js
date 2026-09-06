export function validateText(text,font,scale,spacing,matrix){
 if(!text.trim())throw new Error('Escribe el texto del letrero.');
 if(!Number.isInteger(scale)||scale<1||scale>8)throw new Error('La escala debe ser un número entero entre 1 y 8.');
 if(!Number.isInteger(spacing)||spacing<0||spacing>10)throw new Error('El espaciado debe ser un entero entre 0 y 10.');
 const height=font.height*scale;
 if(matrix&&height>matrix.height)throw new Error(`El texto mide ${height} LED de alto y la matriz tiene ${matrix.height}. Usa una escala máxima de ${Math.floor(matrix.height/font.height)} con esta fuente o elige una fuente menor.`);
 return {width:[...text].length*(font.width*scale+spacing)-spacing,height};
}
