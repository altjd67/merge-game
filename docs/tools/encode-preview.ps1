param(
    [Parameter(Mandatory = $true)][string]$FrameDirectory,
    [Parameter(Mandatory = $true)][string]$OutputPath
)

$ErrorActionPreference = 'Stop'
# Windows 내장 WPF 인코더로 실제 플레이 캡처를 GIF로 묶는다.
Add-Type -AssemblyName PresentationCore
Add-Type -AssemblyName WindowsBase
$encoder = New-Object System.Windows.Media.Imaging.GifBitmapEncoder
$frames = Get-ChildItem -LiteralPath $FrameDirectory -Filter 'frame-*.png' | Sort-Object Name
for ($frameIndex = 0; $frameIndex -lt $frames.Count; $frameIndex += 2) {
    $frameFile = $frames[$frameIndex]
    $bitmap = New-Object System.Windows.Media.Imaging.BitmapImage
    $bitmap.BeginInit()
    $bitmap.CacheOption = [System.Windows.Media.Imaging.BitmapCacheOption]::OnLoad
    $bitmap.DecodePixelWidth = 360
    $bitmap.UriSource = New-Object System.Uri($frameFile.FullName)
    $bitmap.EndInit()
    $bitmap.Freeze()
    $metadata = New-Object System.Windows.Media.Imaging.BitmapMetadata('gif')
    $metadata.SetQuery('/grctlext/Delay', [UInt16]20)
    $metadata.SetQuery('/grctlext/Disposal', [byte]2)
    $encoder.Frames.Add([System.Windows.Media.Imaging.BitmapFrame]::Create($bitmap, $null, $metadata, $null))
}
$stream = [System.IO.File]::Open($OutputPath, [System.IO.FileMode]::Create)
try { $encoder.Save($stream) } finally { $stream.Dispose() }
Write-Output "GIF 프레임 수: $($encoder.Frames.Count)"
