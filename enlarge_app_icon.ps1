Add-Type -AssemblyName System.Drawing

$sourcePath = Join-Path $PSScriptRoot "outputs\rune_manager_app\assets\app_icon.png"
$iconPath = Join-Path $PSScriptRoot "outputs\rune_manager_app\assets\app_icon.ico"
$source = [System.Drawing.Bitmap]::FromFile($sourcePath)

# L'icone de l'application ne conserve que le grand symbole de rune.
# Le badge +15 est utile dans le tableau, mais il réduisait fortement le logo Windows.
$crop = New-Object System.Drawing.Rectangle 18, 0, 166, 187
$canvas = New-Object System.Drawing.Bitmap 256, 256, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$graphics = [System.Drawing.Graphics]::FromImage($canvas)
$graphics.Clear([System.Drawing.Color]::Transparent)
$graphics.CompositingMode = [System.Drawing.Drawing2D.CompositingMode]::SourceCopy
$graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

$targetSize = 252
$scale = [Math]::Min($targetSize / $crop.Width, $targetSize / $crop.Height)
$drawWidth = [Math]::Round($crop.Width * $scale)
$drawHeight = [Math]::Round($crop.Height * $scale)
$drawX = [Math]::Floor((256 - $drawWidth) / 2)
$drawY = [Math]::Floor((256 - $drawHeight) / 2)
$destination = New-Object System.Drawing.Rectangle $drawX, $drawY, $drawWidth, $drawHeight
$graphics.DrawImage($source, $destination, $crop, [System.Drawing.GraphicsUnit]::Pixel)
$graphics.Dispose()
$source.Dispose()

$tempPng = "$sourcePath.tmp.png"
$canvas.Save($tempPng, [System.Drawing.Imaging.ImageFormat]::Png)
$canvas.Dispose()
Move-Item -LiteralPath $tempPng -Destination $sourcePath -Force

$png = [System.Drawing.Bitmap]::FromFile($sourcePath)
$small = New-Object System.Drawing.Bitmap 64, 64, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$smallGraphics = [System.Drawing.Graphics]::FromImage($small)
$smallGraphics.Clear([System.Drawing.Color]::Transparent)
$smallGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$smallGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$smallGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$smallGraphics.DrawImage($png, 0, 0, 64, 64)
$smallGraphics.Dispose()
$png.Dispose()

$handle = $small.GetHicon()
$icon = [System.Drawing.Icon]::FromHandle($handle)
$stream = [System.IO.File]::Create($iconPath)
$icon.Save($stream)
$stream.Dispose()
$icon.Dispose()
$small.Dispose()

Write-Output "Recadrage source: $($crop.Width)x$($crop.Height); rendu: ${drawWidth}x${drawHeight}"
