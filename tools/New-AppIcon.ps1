param([Parameter(Mandatory=$true)][string]$OutputPath)
$ErrorActionPreference='Stop'
Add-Type -AssemblyName System.Drawing
if(!('WtsIconNative' -as [type])){Add-Type 'using System;using System.Runtime.InteropServices;public static class WtsIconNative{[DllImport("user32.dll")]public static extern bool DestroyIcon(IntPtr icon);}'}
$bitmap=New-Object Drawing.Bitmap 32,32
$graphics=[Drawing.Graphics]::FromImage($bitmap)
try{
    $graphics.Clear([Drawing.Color]::FromArgb(24,33,47))
    for($row=0;$row -lt 2;$row++){for($column=0;$column -lt 3;$column++){$color=if($column -eq 1){[Drawing.Color]::FromArgb(75,201,231)}else{[Drawing.Color]::FromArgb(74,139,236)};$brush=New-Object Drawing.SolidBrush $color;try{$graphics.FillRectangle($brush,3+$column*9,4+$row*13,8,11)}finally{$brush.Dispose()}}}
    $handle=$bitmap.GetHicon();$icon=[Drawing.Icon]::FromHandle($handle);$stream=[IO.File]::Create([IO.Path]::GetFullPath($OutputPath))
    try{$icon.Save($stream)}finally{$stream.Dispose();$icon.Dispose();[WtsIconNative]::DestroyIcon($handle)|Out-Null}
}finally{$graphics.Dispose();$bitmap.Dispose()}
