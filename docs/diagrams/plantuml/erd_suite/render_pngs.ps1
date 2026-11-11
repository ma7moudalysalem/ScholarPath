# Render every SVG in ../../img/erd_suite to PNG at its EXACT pixel size (x2
# device scale) so nothing is ever cropped. Optional: pass a name filter.
param([string]$Filter = "*")
$edge = @("$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe","${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
$dir = Join-Path (Split-Path -Parent $MyInvocation.MyCommand.Path) "..\..\img\erd_suite"
$dir = [IO.Path]::GetFullPath($dir)
Get-ChildItem -Path $dir -Filter "$Filter.svg" | ForEach-Object {
  $raw = Get-Content $_.FullName -Raw
  $w = 1600; $h = 1000
  if ($raw -match 'viewBox="0 0 ([\d.]+) ([\d.]+)"') {
    $w = [int][math]::Ceiling([double]$Matches[1]); $h = [int][math]::Ceiling([double]$Matches[2])
  } else {
    if ($raw -match '\swidth="([\d.]+)"')  { $w = [int][math]::Ceiling([double]$Matches[1]) }
    if ($raw -match '\sheight="([\d.]+)"') { $h = [int][math]::Ceiling([double]$Matches[1]) }
  }
  $png = [IO.Path]::ChangeExtension($_.FullName, ".png")
  Remove-Item $png -ErrorAction SilentlyContinue
  $ud = Join-Path $env:TEMP ("sp_es_" + $_.BaseName)
  & $edge --headless=new "--screenshot=$png" "--window-size=$($w+8),$($h+8)" --force-device-scale-factor=2 `
          --hide-scrollbars --default-background-color=FFFFFFFF "--user-data-dir=$ud" `
          ('file:///' + $_.FullName.Replace('\','/')) 2>$null | Out-Null
  Write-Host ("  {0}  ({1}x{2})" -f $_.Name, $w, $h)
}
Write-Host "done -> $dir"
