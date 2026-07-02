# Regenerate all ScholarPath diagrams (SVG + PNG + vector PDF) into ./img.
#
#   PS> ./render.ps1
#
# Rendering classes:
#
#   A. @startuml + plain @startchen (class, component)  -> plantuml.jar SVG+PNG+PDF.
#
#   B. EERD @startchen (clusters, core, context)        -> plantuml.jar SVG, then
#      dbl_participation.py rewrites the thick total-participation strokes into
#      TRUE Elmasri double lines, then Edge derives PNG + a tight vector PDF from
#      the post-processed SVG (the jar PNG/PDF would miss the double lines).
#
#   C. @startdot non-RelMap (eer-specialization)        -> jar SVG + Edge PNG
#      (jar's @startdot PNG/PDF backend writes a ~195-byte stub).
#
#   D. @startdot RelMap_* (relational mapping)          -> gen_relmap_svg.py hand
#      routes the orthogonal SVG; Edge rasterises the PNG.
#
# Java/jar resolved from PATH or %USERPROFILE%\plantuml-tools\. Needs Python + Edge.

$ErrorActionPreference = 'Stop'
$here    = Split-Path -Parent $MyInvocation.MyCommand.Path
$pumlDir = Join-Path $here 'plantuml'
$outDir  = Join-Path $here 'img'

$java = (Get-Command java -ErrorAction SilentlyContinue).Source
if (-not $java) {
  $java = Get-ChildItem (Join-Path $env:USERPROFILE 'plantuml-tools\jre') -Recurse -Filter java.exe -ErrorAction SilentlyContinue |
          Select-Object -First 1 -ExpandProperty FullName
}
if (-not $java) { throw "Java not found. Install a JRE or unzip one into %USERPROFILE%\plantuml-tools\jre." }
$jar = $env:PLANTUML_JAR
if (-not $jar) { $jar = Join-Path $env:USERPROFILE 'plantuml-tools\plantuml.jar' }
if (-not (Test-Path $jar)) { throw "plantuml.jar not found; set `$env:PLANTUML_JAR or place it at $jar." }
$python = (Get-Command python -ErrorAction SilentlyContinue).Source
if (-not $python) { $python = (Get-Command py -ErrorAction SilentlyContinue).Source }
if (-not $python) { throw "Python not found (gen_relmap_svg.py / dbl_participation.py)." }
$edge = @("$env:ProgramFiles\Microsoft\Edge\Application\msedge.exe",
          "${env:ProgramFiles(x86)}\Microsoft\Edge\Application\msedge.exe") |
        Where-Object { Test-Path $_ } | Select-Object -First 1
$gs = (Get-Command gswin64c -ErrorAction SilentlyContinue).Source
if (-not $gs) { $gs = (Get-ChildItem "$env:LOCALAPPDATA\Programs\MiKTeX\miktex\bin\x64\mgs.exe" -ErrorAction SilentlyContinue | Select-Object -First 1).FullName }

function Get-SvgPx($raw) {
  # honour px (PlantUML chen), pt (Graphviz, *96/72), or unitless (= px)
  $w = 1200.0; $h = 1200.0
  if ($raw -match 'width="([\d.]+)(px|pt)?"')  { $w = [double]$Matches[1]; if ($Matches[2] -eq 'pt') { $w *= 1.3334 } }
  if ($raw -match 'height="([\d.]+)(px|pt)?"') { $h = [double]$Matches[1]; if ($Matches[2] -eq 'pt') { $h *= 1.3334 } }
  return @{ w = $w; h = $h }
}
function Edge-Shot($svgPath, $pngPath, $scale) {
  $sz = Get-SvgPx ((Get-Content $svgPath -TotalCount 12) -join ' ')
  $w = [int][math]::Ceiling($sz.w) + 24; $h = [int][math]::Ceiling($sz.h) + 24
  try {
    & $edge --headless=new "--screenshot=$pngPath" "--window-size=$w,$h" `
            --force-device-scale-factor=$scale --hide-scrollbars `
            --default-background-color=FFFFFFFF "--user-data-dir=$env:TEMP\sp_shot_ud" `
            ('file:///' + $svgPath.Replace('\','/')) 2>$null | Out-Null
  } catch { }
}
function Edge-Pdf($svgPath, $pdfPath) {
  $raw = Get-Content $svgPath -Raw -Encoding UTF8
  $sz  = Get-SvgPx $raw
  $body = $raw -replace '(?s)^<\?xml.*?\?>\s*',''
  $pw = [int][math]::Ceiling($sz.w); $ph = [int][math]::Ceiling($sz.h)
  $html = "<!doctype html><html><head><meta charset='utf-8'><style>" +
          "@page{size:${pw}px ${ph}px;margin:0}*{margin:0;padding:0}svg{display:block}" +
          "</style></head><body>$body</body></html>"
  $hp = Join-Path $env:TEMP ([IO.Path]::GetFileNameWithoutExtension($svgPath) + '.html')
  Set-Content -Path $hp -Value $html -Encoding UTF8
  Remove-Item $pdfPath -Force -ErrorAction SilentlyContinue
  $udx = Join-Path $env:TEMP ('sp_pr_' + [guid]::NewGuid().ToString('N'))
  New-Item -ItemType Directory -Force -Path $udx | Out-Null
  $proc = Start-Process -FilePath $edge -ArgumentList @('--headless=new', "--print-to-pdf=$pdfPath",
      '--no-pdf-header-footer', '--disable-gpu', "--user-data-dir=$udx",
      ('file:///' + $hp.Replace('\','/'))) -WindowStyle Hidden -PassThru
  $last = -1; $stable = 0
  for ($i = 0; $i -lt 80; $i++) {
    Start-Sleep -Milliseconds 250
    if (Test-Path $pdfPath) { $s = (Get-Item $pdfPath).Length
      if ($s -gt 800 -and $s -eq $last) { $stable++; if ($stable -ge 2) { break } } else { $stable = 0 }
      $last = $s }
  }
  # headless Edge lingers after printing; kill the whole process tree so they
  # don't accumulate across the loop.
  try { & taskkill /PID $proc.Id /T /F 2>$null | Out-Null } catch { }
  Remove-Item $udx -Recurse -Force -ErrorAction SilentlyContinue
}

# ---- classify sources ----
$all     = Get-ChildItem $pumlDir -Filter *.puml
$isDot   = { param($f) [bool](Select-String -Path $f.FullName -Pattern '@startdot' -SimpleMatch -Quiet) }
$chenUml = $all | Where-Object { -not (& $isDot $_) }
$dotEer  = $all | Where-Object { (& $isDot $_) -and $_.Name -notlike 'RelMap_*' }

# ---- A+B(svg): jar renders every @startchen/@startuml diagram to SVG (+PNG+PDF) ----
$a = $chenUml.FullName
Write-Host "[A] plantuml.jar SVG+PNG+PDF for $($a.Count) chen/uml file(s)"
& $java '-DPLANTUML_LIMIT_SIZE=32768' -jar $jar -tsvg            -o $outDir $a
& $java '-DPLANTUML_LIMIT_SIZE=32768' -jar $jar -tpng '-Sdpi=200' -o $outDir $a
# plantuml.jar's PDF (openpdf) backend needs Java 21+. On an older JRE this throws
# UnsupportedClassVersionError; make it non-fatal — export_figures.ps1 (and step B
# below) print the PDFs from the SVG with Edge, so a missing native PDF is fine.
$savedEAP = $ErrorActionPreference; $ErrorActionPreference = 'Continue'
try { & $java '-DPLANTUML_LIMIT_SIZE=32768' -jar $jar -tpdf -o $outDir $a 2>$null }
catch { Write-Warning "plantuml.jar -tpdf skipped (needs Java 21+); PDFs will be printed from SVG via Edge." }
$ErrorActionPreference = $savedEAP

# ---- C: @startdot non-RelMap -> SVG via jar ----
if ($dotEer) {
  & $java '-DPLANTUML_LIMIT_SIZE=32768' -jar $jar -tsvg -o $outDir $dotEer.FullName
}

# ---- D: RelMap orthogonal schema diagrams -> SVG via the hand router ----
Write-Host "[D] gen_relmap_svg.py -> orthogonal RelMap_*.svg"
Push-Location $pumlDir; & $python 'gen_relmap_svg.py'; Pop-Location

if (-not $edge) { Write-Warning "Edge not found; PNG/PDF post-processing skipped."; Write-Host "Done -> $outDir"; exit 0 }

# native exes (Edge/gs) print to stderr; relax EAP so a 2> redirect can't abort them
$ErrorActionPreference = 'Continue'

# ---- B: EERD double-line post-process -> vector PDF (Edge) + PNG (Ghostscript) ----
# EERD chen outputs that carry total participation (stroke-width:2 strokes).
$eerd = @()
$eerd += Get-ChildItem $outDir -Filter 'EERD_*.svg'
foreach ($n in 'ScholarPath_EERD_Core','ScholarPath_Context') {
  $p = Join-Path $outDir "$n.svg"; if (Test-Path $p) { $eerd += Get-Item $p }
}
$eerd = $eerd | Where-Object { Select-String -Path $_.FullName -Pattern 'stroke-width:2;' -SimpleMatch -Quiet }
if ($eerd) {
  Write-Host "[B] double-line participation + PDF/PNG for $($eerd.Count) EERD diagram(s)"
  & $python (Join-Path $here 'dbl_participation.py') @($eerd.FullName) | Out-Null
  foreach ($svg in $eerd) {
    $pdf = Join-Path $outDir ($svg.BaseName + '.pdf')
    $png = Join-Path $outDir ($svg.BaseName + '.png')
    Edge-Pdf $svg.FullName $pdf
    # PNG from the (double-line) PDF via Ghostscript -- reliable at any size,
    # unlike Edge screenshots which blank out on large bitmaps.
    if ($gs -and (Test-Path $pdf)) {
      & $gs -q -dNOPAUSE -dBATCH -dSAFER -sDEVICE=png16m "-r200" `
            -dGraphicsAlphaBits=4 -dTextAlphaBits=4 -dFirstPage=1 -dLastPage=1 `
            "-sOutputFile=$png" "$pdf" 2>$null | Out-Null
    } else {
      Edge-Shot $svg.FullName $png 2
    }
  }
}

# ---- C+D: Edge rasterises the @startdot SVGs (eer-specialization + RelMap_*) ----
$shoot = @()
$shoot += Get-ChildItem $outDir -Filter 'RelMap_*.svg'
foreach ($f in $dotEer) { $s = Join-Path $outDir ($f.BaseName + '.svg'); if (Test-Path $s) { $shoot += Get-Item $s } }
foreach ($svg in $shoot) { Edge-Shot $svg.FullName (Join-Path $outDir ($svg.BaseName + '.png')) 2 }
Write-Host "Rasterised $($shoot.Count) @startdot PNG(s) via Edge"

Write-Host "Done -> $outDir  (vector .svg + .png + .pdf)"
exit 0
