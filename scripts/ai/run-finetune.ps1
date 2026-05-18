#requires -Version 7.0
<#
.SYNOPSIS
    Uploads a ScholarPath fine-tuning dataset to Azure OpenAI, starts a
    fine-tuning job, and polls it until completion.

.DESCRIPTION
    Part of the ScholarPath RAG + fine-tuning pipeline. Export the dataset first
    from Admin -> AI knowledge base -> "Export .jsonl" (or
    GET /api/admin/ai/fine-tuning/dataset), then run this script.

    See docs/ai/rag-and-fine-tuning.md for the full runbook.

.EXAMPLE
    ./run-finetune.ps1 -Endpoint "https://my-aoai.openai.azure.com" `
                       -ApiKey $env:AZURE_OPENAI_KEY `
                       -DatasetPath ./scholarpath-finetune.jsonl
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Endpoint,
    [Parameter(Mandatory)][string] $ApiKey,
    [Parameter(Mandatory)][string] $DatasetPath,
    [string] $BaseModel  = "gpt-4o-mini",
    [string] $ApiVersion = "2024-10-21",
    [int]    $PollSeconds = 60
)

$ErrorActionPreference = "Stop"
$Endpoint = $Endpoint.TrimEnd("/")
$headers  = @{ "api-key" = $ApiKey }

if (-not (Test-Path -LiteralPath $DatasetPath)) {
    throw "Dataset file not found: $DatasetPath"
}

# ── 1. Upload the training file ───────────────────────────────────────────
Write-Host "1/3  Uploading dataset '$DatasetPath'..." -ForegroundColor Cyan
$uploadUri = "$Endpoint/openai/files?api-version=$ApiVersion"
$file = Invoke-RestMethod -Method Post -Uri $uploadUri -Headers $headers `
    -Form @{ purpose = "fine-tune"; file = Get-Item -LiteralPath $DatasetPath }
$fileId = $file.id
Write-Host "     uploaded as file id: $fileId"

# Azure must finish processing the file before a job can reference it.
do {
    Start-Sleep -Seconds 5
    $fileStatus = (Invoke-RestMethod -Method Get -Headers $headers `
        -Uri "$Endpoint/openai/files/$fileId`?api-version=$ApiVersion").status
    Write-Host "     file status: $fileStatus"
} while ($fileStatus -in @("pending", "running", "uploaded"))

if ($fileStatus -ne "processed") {
    throw "Training file did not process successfully (status: $fileStatus)."
}

# ── 2. Create the fine-tuning job ─────────────────────────────────────────
Write-Host "2/3  Creating fine-tuning job (base model: $BaseModel)..." -ForegroundColor Cyan
$jobUri = "$Endpoint/openai/fine_tuning/jobs?api-version=$ApiVersion"
$body   = @{ model = $BaseModel; training_file = $fileId } | ConvertTo-Json
$job    = Invoke-RestMethod -Method Post -Uri $jobUri -Headers $headers `
    -ContentType "application/json" -Body $body
$jobId  = $job.id
Write-Host "     job id: $jobId"

# ── 3. Poll until the job finishes ────────────────────────────────────────
Write-Host "3/3  Polling every $PollSeconds s (fine-tuning usually takes 30-90 min)..." -ForegroundColor Cyan
do {
    Start-Sleep -Seconds $PollSeconds
    $job = Invoke-RestMethod -Method Get -Headers $headers `
        -Uri "$Endpoint/openai/fine_tuning/jobs/$jobId`?api-version=$ApiVersion"
    Write-Host "     status: $($job.status)"
} while ($job.status -in @("pending", "queued", "running", "validating_files"))

if ($job.status -ne "succeeded") {
    throw "Fine-tuning job did not succeed (status: $($job.status))."
}

Write-Host ""
Write-Host "Done. Fine-tuned model: $($job.fine_tuned_model)" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "  1. In Azure AI Foundry, create a DEPLOYMENT for '$($job.fine_tuned_model)'."
Write-Host "  2. Set Ai:AzureOpenAi:FineTunedDeploymentName to that deployment name."
Write-Host "  3. Restart the API - the chatbot will use the fine-tuned model."
