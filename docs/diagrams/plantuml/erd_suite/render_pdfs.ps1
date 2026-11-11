# Convert every SVG in a directory to a print-ready vector PDF at its exact
# page size (Edge print-to-pdf; the plantuml.jar PDF backend needs Java 21+).
#   PS> ./render_pdfs.ps1 -SrcDir <img folder> [-Filter *]
param([Parameter(Mandatory=$true)][string]$SrcDir, [string]$Filter = "*")
$edge = @("$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe","${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
Get-ChildItem -Path $SrcDir -Filter "$Filter.svg" | ForEach-Object {
  $raw = Get-Content $_.FullName -Raw -Encoding UTF8
  $pw = 1600; $ph = 1000
  # viewBox="0 0 W H" is the true coordinate size (works for both our SVGs and
  # PlantUML's, whose width/height carry px/pt units + a small style size).
  if ($raw -match 'viewBox="0 0 ([\d.]+) ([\d.]+)"') {
    $pw = [int][math]::Ceiling([double]$Matches[1]); $ph = [int][math]::Ceiling([double]$Matches[2])
  } elseif ($raw -match 'width="([\d.]+)"' -and $raw -match 'height="([\d.]+)"') {
    if ($raw -match 'width="([\d.]+)"')  { $pw = [int][math]::Ceiling([double]$Matches[1]) }
    if ($raw -match 'height="([\d.]+)"') { $ph = [int][math]::Ceiling([double]$Matches[1]) }
  }
  $body = $raw -replace '(?s)^<\?xml.*?\?>\s*',''
  $html = "<!doctype html><html><head><meta charset='utf-8'><style>@page{size:${pw}px ${ph}px;margin:0}*{margin:0;padding:0}svg{display:block}</style></head><body>$body</body></html>"
  $hp = Join-Path $env:TEMP ($_.BaseName + ".html")
  Set-Content -Path $hp -Value $html -Encoding UTF8
  $pdf = [IO.Path]::ChangeExtension($_.FullName, ".pdf")
  Remove-Item $pdf -Force -ErrorAction SilentlyContinue
  $udx = Join-Path $env:TEMP ("sp_pdf_" + $_.BaseName)
  New-Item -ItemType Directory -Force -Path $udx | Out-Null
  $p = Start-Process -FilePath $edge -ArgumentList @('--headless=new', "--print-to-pdf=$pdf",
       '--no-pdf-header-footer', '--disable-gpu', "--user-data-dir=$udx",
       ('file:///' + $hp.Replace('\','/'))) -WindowStyle Hidden -PassThru
  $last=-1;$stable=0
  for ($i=0;$i -lt 80;$i++){ Start-Sleep -Milliseconds 200
    if (Test-Path $pdf){ $s=(Get-Item $pdf).Length; if($s -gt 800 -and $s -eq $last){$stable++; if($stable -ge 2){break}} else {$stable=0}; $last=$s } }
  try { & taskkill /PID $p.Id /T /F 2>$null | Out-Null } catch {}
  Remove-Item $udx -Recurse -Force -ErrorAction SilentlyContinue
  if (Test-Path $pdf) { Write-Host ("  {0}.pdf  ({1}x{2}, {3}KB)" -f $_.BaseName, $pw, $ph, [int]((Get-Item $pdf).Length/1KB)) }
}
