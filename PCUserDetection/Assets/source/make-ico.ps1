# Builds the multi-size .ico the app ships from the PNGs in .\png.
#
# Run with no arguments to rebuild the committed icon in place:
#
#     powershell -ExecutionPolicy Bypass -File make-ico.ps1
#
# Only Windows PowerShell and System.Drawing are needed; there is nothing to
# install. See README.md for where the PNGs themselves come from.

param(
    [string] $SourceDir = (Join-Path $PSScriptRoot 'png'),
    [string] $Prefix    = 'pcud-idle',
    [string] $OutFile   = (Join-Path (Split-Path $PSScriptRoot -Parent) 'PCUserDetection.ico')
)

Add-Type -AssemblyName System.Drawing

# Windows picks whichever of these is closest to the size it needs, so a size
# that is missing here is one that gets drawn as a resample of another.
$sizes = 16, 20, 24, 32, 48, 64, 128, 256
$entries = @()

foreach ($size in $sizes) {
    $path = Join-Path $SourceDir "$Prefix-$size.png"
    if (-not (Test-Path $path)) { throw "missing source image: $path" }

    if ($size -eq 256) {
        # The largest entry goes in as the PNG itself. Every Windows that draws
        # an icon that big reads PNG entries, and storing it uncompressed would
        # cost about 250 KB on its own.
        $entries += , @{ Size = $size; Data = [System.IO.File]::ReadAllBytes($path) }
        continue
    }

    # Everything smaller goes in as an uncompressed 32bpp DIB, which is what
    # the older shell code paths still expect to find in an icon.
    $src = New-Object System.Drawing.Bitmap($path)
    $bmp = New-Object System.Drawing.Bitmap($src.Width, $src.Height, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.DrawImage($src, 0, 0, $src.Width, $src.Height)
    $g.Dispose()
    $src.Dispose()

    $w = $bmp.Width
    $h = $bmp.Height
    $rect = New-Object System.Drawing.Rectangle(0, 0, $w, $h)
    $data = $bmp.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $pixels = New-Object byte[] ($data.Stride * $h)
    [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $pixels.Length)
    $stride = $data.Stride
    $bmp.UnlockBits($data)
    $bmp.Dispose()

    # The AND mask is unused at 32bpp, where the alpha channel decides what
    # shows, but the format still requires rows of it to be present.
    $maskStride = [int][math]::Floor(($w + 31) / 32) * 4

    $ms = New-Object System.IO.MemoryStream
    $bw = New-Object System.IO.BinaryWriter($ms)
    $bw.Write([int]    40)              # biSize
    $bw.Write([int]    $w)              # biWidth
    $bw.Write([int]    ($h * 2))        # biHeight: colour rows plus mask rows
    $bw.Write([int16]  1)               # biPlanes
    $bw.Write([int16]  32)              # biBitCount
    $bw.Write([int]    0)               # biCompression: BI_RGB
    $bw.Write([int]    ($w * $h * 4 + $maskStride * $h))
    $bw.Write([int] 0); $bw.Write([int] 0); $bw.Write([int] 0); $bw.Write([int] 0)

    # A DIB is stored bottom-up, so the rows come off the bitmap in reverse.
    for ($y = $h - 1; $y -ge 0; $y--) {
        $bw.Write($pixels, $y * $stride, $w * 4)
    }
    $bw.Write((New-Object byte[] ($maskStride * $h)))
    $bw.Flush()

    $entries += , @{ Size = $size; Data = $ms.ToArray() }
    $bw.Dispose()
    $ms.Dispose()
}

$out = New-Object System.IO.MemoryStream
$bw = New-Object System.IO.BinaryWriter($out)

# ICONDIR
$bw.Write([int16] 0)                # reserved
$bw.Write([int16] 1)                # type: icon
$bw.Write([int16] $entries.Count)

# One ICONDIRENTRY each, then the images themselves in the same order.
$offset = 6 + 16 * $entries.Count
foreach ($e in $entries) {
    # 256 does not fit in a byte and is written as 0, which is how it is read.
    $dim = if ($e.Size -ge 256) { 0 } else { $e.Size }
    $bw.Write([byte]  $dim)         # width
    $bw.Write([byte]  $dim)         # height
    $bw.Write([byte]  0)            # palette size: none at 32bpp
    $bw.Write([byte]  0)            # reserved
    $bw.Write([int16] 1)            # colour planes
    $bw.Write([int16] 32)           # bits per pixel
    $bw.Write([int]   $e.Data.Length)
    $bw.Write([int]   $offset)
    $offset += $e.Data.Length
}
foreach ($e in $entries) { $bw.Write($e.Data) }

$bw.Flush()
[System.IO.File]::WriteAllBytes($OutFile, $out.ToArray())
$bw.Dispose()
$out.Dispose()

Write-Output "wrote $OutFile ($((Get-Item $OutFile).Length) bytes, $($entries.Count) sizes)"
