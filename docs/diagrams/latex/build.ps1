# Compile all four ScholarPath diagram reports to PDF.
#   PS> ./build.ps1
# Requires a TeX distribution (MiKTeX or TeX Live) providing pdflatex.
# The .tex files embed the vector PDFs from ../img, so keep this folder next
# to the img/ folder. Each report is compiled twice (table of contents).

$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath (Split-Path -Parent $MyInvocation.MyCommand.Path)

$pdflatex = (Get-Command pdflatex -ErrorAction SilentlyContinue).Source
if (-not $pdflatex) {
  $guess = "$env:LOCALAPPDATA\Programs\MiKTeX\miktex\bin\x64\pdflatex.exe"
  if (Test-Path $guess) { $pdflatex = $guess } else { throw "pdflatex not found. Install MiKTeX (https://miktex.org) or TeX Live." }
}

Get-ChildItem -Filter *.tex | Sort-Object Name | ForEach-Object {
  Write-Host "Compiling $($_.Name) ..."
  & $pdflatex --enable-installer --interaction=nonstopmode $_.Name | Out-Null
  & $pdflatex --enable-installer --interaction=nonstopmode $_.Name | Out-Null
}
Write-Host "Done. PDFs:"
Get-ChildItem -Filter *.pdf | ForEach-Object { Write-Host "  $($_.Name)" }
