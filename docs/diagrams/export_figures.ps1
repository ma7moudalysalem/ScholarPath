# Collect every figure embedded in the four LaTeX reports into a tidy export
# tree, each diagram in three formats at the highest quality:
#
#   exports/
#     01-EERD/                {svg, pdf, png}/...
#     02-Relational-Mapping/  {svg, pdf, png}/...
#     03-Class-Diagrams/      {svg, pdf, png}/...
#     04-Component-Diagram/   {svg, pdf, png}/...
#
#   svg = vector master (infinite quality, editable)
#   pdf = vector (print / LaTeX / Word).  Native plantuml.jar PDF where it
#         exists; for the hand-routed RelMap_* and the @startdot EER diagram we
#         print the inlined SVG to a tight one-page vector PDF with headless Edge.
#   png = 300-DPI raster, rasterised from the vector PDF with Ghostscript
#         (anti-aliased).  We deliberately do NOT screenshot the SVG with Edge:
#         at high device-scale the bitmap exceeds Edge's headless capture limit
#         and comes out blank.  PDF->PNG via Ghostscript has no such limit.
#
#   PS> ./export_figures.ps1
#
# Run ./render.ps1 first so img/ holds the current SVG/PDF sources.

$ErrorActionPreference = 'Stop'
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$img  = Join-Path $here 'img'
$out  = Join-Path $here 'exports'
$DPI  = 600   # PNG rasterisation density (print-grade; SVG/PDF stay vector)

# --- tool resolution ---
$edge = @("$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
          "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $edge) { throw "Microsoft Edge not found (needed to print RelMap/EER SVGs to PDF)." }

$gs = (Get-Command gswin64c -ErrorAction SilentlyContinue).Source
if (-not $gs) { $gs = (Get-ChildItem "$env:ProgramFiles\gs\*\bin\gswin64c.exe" -ErrorAction SilentlyContinue | Select-Object -First 1).FullName }
if (-not $gs) { $gs = (Get-ChildItem "$env:LOCALAPPDATA\Programs\MiKTeX\miktex\bin\x64\mgs.exe" -ErrorAction SilentlyContinue | Select-Object -First 1).FullName }
if (-not $gs) { throw "Ghostscript not found (gswin64c.exe or MiKTeX mgs.exe) -- needed for PDF->PNG." }

$wrk = Join-Path $env:TEMP 'sp_export_wrap'
$ud  = Join-Path $env:TEMP 'sp_export_ud'
New-Item -ItemType Directory -Force -Path $wrk | Out-Null

# report -> ordered list of figure base-names (exactly what each .tex embeds)
$reports = [ordered]@{
  '01-EERD' = @(
    'eer-specialization','ScholarPath_Context','EERD_Identity_Profile',
    'EERD_Scholarships_Applications','EERD_Booking_Payments_Ratings',
    'EERD_Community_Chat','EERD_Resources_Notifications',
    'EERD_AI_Knowledge_Platform','ScholarPath_EERD_Core')
  '02-Relational-Mapping' = @(
    'RelMap_Identity_Profile','RelMap_Scholarships_Applications',
    'RelMap_Booking_Payments_Ratings','RelMap_Community_Chat',
    'RelMap_Resources_Notifications','RelMap_AI_Platform')
  '03-Class-Diagrams' = @(
    'Class_Base_Types','Class_Identity_Profile','Class_Scholarships_Applications',
    'Class_Booking_Payments_Ratings','Class_Community_Chat',
    'Class_Resources_Notifications','Class_AI_Platform',
    'ScholarPath_Ports_Adapters')
  '04-Component-Diagram' = @('ScholarPath_Components')
}

function Get-SvgPx($svgText) {
  # honour px (PlantUML chen), pt (Graphviz, *96/72), or unitless (= px)
  $w = 1000.0; $h = 1000.0
  if ($svgText -match 'width="([\d.]+)(px|pt)?"')  { $w = [double]$Matches[1]; if ($Matches[2] -eq 'pt') { $w *= 1.3334 } }
  if ($svgText -match 'height="([\d.]+)(px|pt)?"') { $h = [double]$Matches[1]; if ($Matches[2] -eq 'pt') { $h *= 1.3334 } }
  return @{ w = $w; h = $h }
}

# Print an HTML wrapper to PDF with headless Edge.  --print-to-pdf is async and
# returns before the file is flushed, so we launch with a throwaway profile and
# poll until the output size stops changing -- guaranteeing the PDF is complete
# before Ghostscript reads it in Pass 2.
function Export-EdgePdf($edgeExe, $htmlFile, $pdfOut) {
  Remove-Item $pdfOut -Force -ErrorAction SilentlyContinue
  $udx = Join-Path $env:TEMP ('sp_pdf_ud\' + [guid]::NewGuid().ToString('N'))
  New-Item -ItemType Directory -Force -Path $udx | Out-Null
  $a = @('--headless=new', "--print-to-pdf=$pdfOut", '--no-pdf-header-footer',
         '--disable-gpu', "--user-data-dir=$udx", ('file:///' + $htmlFile.Replace('\','/')))
  $proc = Start-Process -FilePath $edgeExe -ArgumentList $a -WindowStyle Hidden -PassThru
  $last = -1; $stable = 0
  for ($i = 0; $i -lt 80; $i++) {           # up to ~20 s
    Start-Sleep -Milliseconds 250
    if (Test-Path $pdfOut) {
      $s = (Get-Item $pdfOut).Length
      if ($s -gt 800 -and $s -eq $last) { $stable++; if ($stable -ge 2) { break } } else { $stable = 0 }
      $last = $s
    }
  }
  try { & taskkill /PID $proc.Id /T /F 2>$null | Out-Null } catch { }   # kill lingering headless tree
  Remove-Item $udx -Recurse -Force -ErrorAction SilentlyContinue
}

# flatten into a job list and create the per-report folders up front.
# Clean format folders file-by-file (best effort) instead of nuking the whole
# tree, so a transient directory lock (indexer / open Explorer) can't abort us.
$jobs = @()
foreach ($report in $reports.Keys) {
  $svgDir = Join-Path $out "$report\svg"
  $pdfDir = Join-Path $out "$report\pdf"
  $pngDir = Join-Path $out "$report\png"
  New-Item -ItemType Directory -Force -Path $svgDir,$pdfDir,$pngDir | Out-Null
  Get-ChildItem $svgDir,$pdfDir,$pngDir -File -ErrorAction SilentlyContinue |
    ForEach-Object { Remove-Item $_.FullName -Force -ErrorAction SilentlyContinue }
  foreach ($name in $reports[$report]) {
    $svgSrc = Join-Path $img "$name.svg"
    if (-not (Test-Path $svgSrc)) { Write-Warning "missing $name.svg"; continue }
    $jobs += [pscustomobject]@{
      Name=$name; SvgSrc=$svgSrc
      SvgDst=(Join-Path $svgDir "$name.svg"); PdfDst=(Join-Path $pdfDir "$name.pdf")
      PngDst=(Join-Path $pngDir "$name.png"); PdfSrc=(Join-Path $img "$name.pdf")
    }
  }
}

# Native exes (Edge, Ghostscript) print progress to stderr; under EAP=Stop a
# 2> redirect would promote that to a terminating error and abort the exe
# mid-run (Ghostscript prints its banner BEFORE rendering -> no PNG). Relax EAP
# for the rendering passes so the tools run to completion.
$ErrorActionPreference = 'Continue'

# --- Pass 1: SVG copy + vector PDF (native plantuml, else print inlined SVG) ---
Write-Host "Pass 1: SVG + vector PDF ($($jobs.Count) figures)"
foreach ($j in $jobs) {
  Copy-Item $j.SvgSrc $j.SvgDst -Force
  if ((Test-Path $j.PdfSrc) -and ((Get-Item $j.PdfSrc).Length -gt 1000)) {
    Copy-Item $j.PdfSrc $j.PdfDst -Force
  } else {
    $raw  = Get-Content $j.SvgSrc -Raw -Encoding UTF8   # SVG is UTF-8; avoid ANSI mojibake of ⊂ — etc.
    $sz   = Get-SvgPx $raw
    $body = $raw -replace '(?s)^<\?xml.*?\?>\s*',''
    $pw = [int][math]::Ceiling($sz.w); $ph = [int][math]::Ceiling($sz.h)
    $html = "<!doctype html><html><head><meta charset='utf-8'><style>" +
            "@page{size:${pw}px ${ph}px;margin:0}*{margin:0;padding:0}svg{display:block}" +
            "</style></head><body>$body</body></html>"
    $hp = Join-Path $wrk "$($j.Name).html"
    Set-Content -Path $hp -Value $html -Encoding UTF8
    Export-EdgePdf $edge $hp $j.PdfDst
  }
}

# --- Pass 2: PNG at $DPI, rasterised from the vector PDF with Ghostscript ---
Write-Host "Pass 2: PNG @ $DPI DPI ($($jobs.Count) figures)"
foreach ($j in $jobs) {
  if (-not (Test-Path $j.PdfDst)) { Write-Warning "no PDF for $($j.Name); skipping PNG"; continue }
  # NOTE: gs needs the resolution attached to -r (e.g. -r300); pass it as one
  # quoted token, otherwise PowerShell splits "-r$DPI" into "-r" "300".
  & $gs -q -dNOPAUSE -dBATCH -dSAFER -sDEVICE=png16m "-r$DPI" `
        -dGraphicsAlphaBits=4 -dTextAlphaBits=4 -dFirstPage=1 -dLastPage=1 `
        "-sOutputFile=$($j.PngDst)" "$($j.PdfDst)" 2>$null | Out-Null
}

foreach ($report in $reports.Keys) { Write-Host ("  {0}: {1} figure(s)" -f $report, $reports[$report].Count) }
Write-Host "Exported $($jobs.Count) figures (svg + pdf + png) -> $out"
exit 0
