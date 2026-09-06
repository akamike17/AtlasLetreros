$ErrorActionPreference='Stop'
$base='http://127.0.0.1:5187'
function Uid { [guid]::NewGuid().ToString() }
$font=@{
 'A'=@('01110','10001','10001','11111','10001','10001','10001');'B'=@('11110','10001','10001','11110','10001','10001','11110');'C'=@('01111','10000','10000','10000','10000','10000','01111');'D'=@('11110','10001','10001','10001','10001','10001','11110');'E'=@('11111','10000','10000','11110','10000','10000','11111');'I'=@('11111','00100','00100','00100','00100','00100','11111');'L'=@('10000','10000','10000','10000','10000','10000','11111');'M'=@('10001','11011','10101','10101','10001','10001','10001');'N'=@('10001','11001','10101','10011','10001','10001','10001');'O'=@('01110','10001','10001','10001','10001','10001','01110');'P'=@('11110','10001','10001','11110','10000','10000','10000');'R'=@('11110','10001','10001','11110','10100','10010','10001');'S'=@('01111','10000','10000','01110','00001','00001','11110');'T'=@('11111','00100','00100','00100','00100','00100','00100');'U'=@('10001','10001','10001','10001','10001','10001','01110')
}
function TextMask($text,$scale=1) { $w=256;$h=16;$mask=New-Object int[] ($w*$h);$x=0;foreach($ch in $text.ToCharArray()){if($ch -eq ' '){$x+=3*$scale;continue};$rows=@('11110','10001','10001','11110','10000','10000','10000');switch($ch.ToString().ToUpper()){ 'M' {$rows=@('10001','11011','10101','10101','10001','10001','10001')} 'G' {$rows=@('01110','10001','10000','10111','10001','10001','01110')} 'S' {$rows=@('01111','10000','10000','01110','00001','00001','11110')} 'O' {$rows=@('01110','10001','10001','10001','10001','10001','01110')} 'L' {$rows=@('10000','10000','10000','10000','10000','10000','11111')} 'U' {$rows=@('10001','10001','10001','10001','10001','10001','01110')} 'T' {$rows=@('11111','00100','00100','00100','00100','00100','00100')} 'I' {$rows=@('11111','00100','00100','00100','00100','00100','11111')} 'N' {$rows=@('10001','11001','10101','10011','10001','10001','10001')} 'R' {$rows=@('11110','10001','10001','11110','10100','10010','10001')} 'E' {$rows=@('11111','10000','10000','11110','10000','10000','11111')} 'P' {$rows=@('11110','10001','10001','11110','10000','10000','10000')} 'A' {$rows=@('01110','10001','10001','11111','10001','10001','10001')} 'C' {$rows=@('01111','10000','10000','10000','10000','10000','01111')} };for($y=0;$y -lt 7;$y++){for($xx=0;$xx -lt 5;$xx++){if($rows[$y].Substring($xx,1) -eq '1'){if($x+$xx*$scale -lt $w){$mask[(4+$y*$scale)*$w+$x+$xx*$scale]=1}}}};$x+=6*$scale;if($x -ge $w){break}};return @{width=$w;height=$h;mask=$mask}}
function Drawing($name,$content,$color,$effect,$duration,$x=0) { return @{id=(Uid);type='Drawing';name=$name;x=$x;y=0;width=$content.width;height=$content.height;rotation=0;opacity=1;visible=$true;content=$content;style=@{color=$color};effect=@{type=$effect;speed=12};durationMs=$duration} }
$layers=@()
$a=TextMask 'MG SOLUTION' 1;$a.x=-16
$b=TextMask 'SE REPARAN COMPUTADORAS' 1;$b.x=0
$icon=New-Object int[] (32*16);for($y=2;$y -lt 11;$y++){for($x=3;$x -lt 27;$x++){if($y -eq 2 -or $y -eq 10 -or $x -eq 3 -or $x -eq 26){$icon[$y*32+$x]=1}}};for($x=12;$x -lt 18;$x++){$icon[12*32+$x]=1};for($y=11;$y -lt 14;$y++){$icon[$y*32+14]=1}
$l1=@{id=(Uid);name='MG SOLUTION';visible=$true;locked=$false;opacity=1;order=0;objects=@((Drawing 'MG SOLUTION' $a '#ffffff' 'blink' 10000))}
$l2=@{id=(Uid);name='SE REPARAN COMPUTADORAS';visible=$true;locked=$false;opacity=1;order=0;objects=@((Drawing 'SE REPARAN COMPUTADORAS' $b '#36e6b0' 'marquee-left' 20000 0))}
$l3=@{id=(Uid);name='Reparación PC';visible=$true;locked=$false;opacity=1;order=0;objects=@((Drawing 'Icono reparación PC' @{width=32;height=16;mask=$icon} '#ffce66' 'blink' 10000))}
$scene=@{id=(Uid);name='Prueba en vivo';active=$true;durationMs=32000;layers=@($l1,$l2,$l3);frames=@(
 @{id=(Uid);durationMs=10000;layers=@($l1)},
 @{id=(Uid);durationMs=12000;layers=@($l2)},
 @{id=(Uid);durationMs=10000;layers=@($l3)})}
$p=@{id=(Uid);name='Prueba en vivo · Reparación PC';formatVersion=1;matrixConfiguration=@{technology='WS2812B';width=32;height=16;wiringMode='serpentine';origin='TL';primaryDirection='horizontal';colorOrder='GRB';rotation=0;flipX=$false;flipY=$false;preferredFps=12;brightnessLimit=80};scenes=@($scene);activeSceneId=$scene.id;palette=@('#ffffff','#36e6b0','#ffce66');embeddedAssets=@();createdUtc=(Get-Date).ToUniversalTime().ToString('o');modifiedUtc=(Get-Date).ToUniversalTime().ToString('o')}
$json=$p|ConvertTo-Json -Depth 30; $created=Invoke-RestMethod "$base/api/projects" -Method Post -ContentType 'application/json' -Body $json; Write-Output $created.id



