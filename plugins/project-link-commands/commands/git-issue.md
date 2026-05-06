Create a GitHub issue and add it to the configured GitHub Projects v2 board with metadata.

Arguments: `$ARGUMENTS` format: `{area}:{issue_name}`

Examples:
- `공통:TODO-List 구조화 작업`
- `서버:로그인 API 구현`
- `클라:플레이어 이동 처리`

## Prerequisites

Read `.env` once at the start of the PowerShell script.

Required config:
- `GITHUB_REPO_URL`
- `GITHUB_DEFAULT_PROJECT`
- `GITHUB_DEFAULT_ASSIGNEE`

Authentication:
- Prefer `GITHUB_TOKEN` from `.env`; set `$env:GH_TOKEN` once at the top of the script.
- If `GITHUB_TOKEN` is missing, `gh auth status` must already be authenticated.
- If both `.env` token and global `gh` auth are unavailable, STOP and notify the user.
- Never print tokens or write them to logs.

## Issue Title Convention

```text
[{area}] {issue_name}
```

Example:

```text
[공통] TODO-List 구조화 작업
```

## Environment: Windows / PowerShell

Use PowerShell for every shell command.

Fragile GitHub CLI cases to avoid:
- Do not use `gh issue create --title` for Korean text or titles beginning with `[area]`; use REST API with a UTF-8 no-BOM temp JSON file.
- Do not use `gh issue list --search` for exact Korean titles with spaces; list JSON and filter in PowerShell.
- Do not send GraphQL with `-f query=` or inline literal arguments; use GraphQL variables and `--input` with a UTF-8 no-BOM temp JSON file.
- If `rg` fails locally, use PowerShell `Get-ChildItem` / `Select-String` fallback.

## Project Metadata

Determine these autonomously without confirmation:
- Priority: choose an existing project option, usually `P2` unless the issue is urgent or blocking.
- Size: choose one of `XS`, `S`, `M`, `L`, `XL`.
- Labels: select from existing labels, then create missing labels as needed.

Set these project fields:
- `Priority`
- `Size`
- `Iteration` set to the current sprint by matching today's date against `startDate <= today < startDate + duration`.

Leave these blank:
- `Start date`
- `Target date`
- `Status`

## Single-Script Flow

Although GitHub Projects v2 requires dependent API steps, run them in one PowerShell script whenever possible:

1. Parse `.env`, repo, project, and arguments.
2. Resolve project field IDs, option IDs, and iterations from `.agents/cache/git-issue-project-cache.json` when fresh.
3. Create missing labels.
4. Create the issue.
5. Add the issue to the project, receiving `item.id`.
6. Set Priority, Size, and Iteration on that project item.

Project field IDs and option IDs exist before the issue exists, so they can be queried or cached before issue creation. Only `item.id` requires the issue to be created and added first.

## Cache Policy

- Cache path: `.agents/cache/git-issue-project-cache.json`
- Cache contents: owner, repo, project title, project number/id, project fields/options, iteration configuration, cached timestamp.
- Default TTL: 24 hours.
- Refresh the cache when it is missing, expired, for a different repo/project, missing required fields/options, or has no iteration matching today.
- Do not cache issue IDs, project item IDs, labels, assignees, or token values.
- Treat the cache as local runtime state; it must not be committed.

## Canonical PowerShell Template

Before running, replace `$argumentsText`, `$priorityName`, `$sizeName`, and `$labels` based on the user's request.

```powershell
$ErrorActionPreference = 'Stop'

$argumentsText = '{area}:{issue_name}'
$priorityName = '{priority}' # e.g. P2
$sizeName = '{size}'         # XS/S/M/L/XL
$labels = @('{label}')
$cacheTtlHours = 24

function Read-DotEnv($path) {
  $vars = @{}
  if (Test-Path -LiteralPath $path) {
    Get-Content -LiteralPath $path | ForEach-Object {
      if ($_ -match '^\s*([^#][^=]+)=(.*)$') {
        $vars[$matches[1].Trim()] = $matches[2].Trim().Trim('"').Trim("'")
      }
    }
  }
  return $vars
}

function Write-Utf8JsonTemp($value) {
  $utf8NoBom = New-Object System.Text.UTF8Encoding $false
  $tmpFile = [System.IO.Path]::GetTempFileName() + '.json'
  $json = $value | ConvertTo-Json -Compress -Depth 20
  [System.IO.File]::WriteAllText($tmpFile, $json, $utf8NoBom)
  return $tmpFile
}

function Invoke-GhJsonInput($apiArgs, $payload) {
  $tmpFile = Write-Utf8JsonTemp $payload
  try {
    return & gh @apiArgs --input $tmpFile | ConvertFrom-Json
  }
  finally {
    if (Test-Path -LiteralPath $tmpFile) {
      Remove-Item -LiteralPath $tmpFile
    }
  }
}

function Read-JsonFile($path) {
  if (-not (Test-Path -LiteralPath $path)) {
    return $null
  }

  try {
    return Get-Content -LiteralPath $path -Raw | ConvertFrom-Json
  }
  catch {
    return $null
  }
}

function Write-JsonFile($path, $value) {
  $dir = Split-Path -Parent $path
  if ($dir -and -not (Test-Path -LiteralPath $dir)) {
    New-Item -ItemType Directory -Force -Path $dir | Out-Null
  }

  $utf8NoBom = New-Object System.Text.UTF8Encoding $false
  $json = $value | ConvertTo-Json -Depth 50
  [System.IO.File]::WriteAllText($path, $json, $utf8NoBom)
}

function Test-ProjectCache($cache, $owner, $repoFull, $projectTitle, $ttlHours) {
  if (-not $cache) {
    return $false
  }

  if ($cache.cacheVersion -ne 1 -or $cache.owner -ne $owner -or $cache.repo -ne $repoFull -or $cache.projectTitle -ne $projectTitle) {
    return $false
  }

  try {
    $cachedAt = [DateTimeOffset]::Parse([string]$cache.cachedAt)
  }
  catch {
    return $false
  }

  if ([DateTimeOffset]::UtcNow -gt $cachedAt.AddHours([double]$ttlHours)) {
    return $false
  }

  if (-not $cache.project.id -or -not $cache.project.number) {
    return $false
  }

  $fields = @($cache.fields)
  foreach ($fieldName in @('Priority', 'Size', 'Iteration')) {
    if (-not ($fields | Where-Object { $_.name -eq $fieldName } | Select-Object -First 1)) {
      return $false
    }
  }

  $iterations = @($cache.iterations | Where-Object { $_ })
  if ($iterations.Count -eq 0) {
    return $false
  }

  return $true
}

function Get-ProjectMetadataFromGitHub($owner, $repoFull, $projectTitle) {
  $projects = gh project list --owner $owner --format json | ConvertFrom-Json
  $project = $projects.projects | Where-Object { $_.title -eq $projectTitle } | Select-Object -First 1
  if (-not $project) {
    throw "Project not found: $projectTitle"
  }

  $fieldList = gh project field-list $project.number --owner $owner --format json | ConvertFrom-Json

  $iterationQuery = 'query($projectId: ID!, $fieldName: String!) { node(id: $projectId) { ... on ProjectV2 { field(name: $fieldName) { ... on ProjectV2IterationField { configuration { iterations { id title startDate duration } } } } } } }'
  $iterationPayload = @{
    query = $iterationQuery
    variables = @{
      projectId = $project.id
      fieldName = 'Iteration'
    }
  }
  $iterationData = Invoke-GhJsonInput @('api', 'graphql') $iterationPayload

  return [pscustomobject]@{
    cacheVersion = 1
    owner = $owner
    repo = $repoFull
    projectTitle = $projectTitle
    cachedAt = [DateTimeOffset]::UtcNow.ToString('o')
    project = [pscustomobject]@{
      id = $project.id
      number = $project.number
      title = $project.title
      url = $project.url
    }
    fields = @($fieldList.fields)
    iterations = @($iterationData.data.node.field.configuration.iterations)
  }
}

function Select-CurrentIteration($iterations) {
  $today = (Get-Date).Date
  return @($iterations) |
    Where-Object {
      $start = ([DateTime]$_.startDate).Date
      $end = $start.AddDays([int]$_.duration)
      $today -ge $start -and $today -lt $end
    } |
    Select-Object -First 1
}

$envVars = Read-DotEnv '.env'
foreach ($required in @('GITHUB_REPO_URL', 'GITHUB_DEFAULT_PROJECT', 'GITHUB_DEFAULT_ASSIGNEE')) {
  if (-not $envVars[$required]) {
    throw "Missing required .env value: $required"
  }
}

if ($envVars['GITHUB_TOKEN']) {
  $env:GH_TOKEN = $envVars['GITHUB_TOKEN']
} else {
  gh auth status | Out-Null
}

if ($argumentsText -notmatch '^\s*([^:]+):(.+)$') {
  throw 'Malformed arguments. Expected format: {area}:{issue_name}'
}

$area = $matches[1].Trim()
$issueName = $matches[2].Trim()
$title = "[$area] $issueName"

$repoUrl = $envVars['GITHUB_REPO_URL']
if ($repoUrl -notmatch 'github\.com[:/]([^/]+)/(.+?)(?:\.git)?/?$') {
  throw "Unsupported GITHUB_REPO_URL: $repoUrl"
}

$owner = $matches[1]
$repoName = $matches[2]
$repoFull = "$owner/$repoName"
$assignee = $envVars['GITHUB_DEFAULT_ASSIGNEE']
$projectTitle = $envVars['GITHUB_DEFAULT_PROJECT']
$cachePath = Join-Path (Join-Path (Get-Location) '.agents/cache') 'git-issue-project-cache.json'

$existingIssue = gh issue list --repo $repoFull --state all --json number,title,url,state --limit 200 |
  ConvertFrom-Json |
  Where-Object { $_.title -eq $title } |
  Select-Object -First 1

if ($existingIssue) {
  throw "Exact title already exists: $($existingIssue.url)"
}

$projectCache = Read-JsonFile $cachePath
if (-not (Test-ProjectCache $projectCache $owner $repoFull $projectTitle $cacheTtlHours)) {
  $projectCache = Get-ProjectMetadataFromGitHub $owner $repoFull $projectTitle
  Write-JsonFile $cachePath $projectCache
}

$project = $projectCache.project
$fields = @($projectCache.fields)
$priorityField = $fields | Where-Object { $_.name -eq 'Priority' } | Select-Object -First 1
$sizeField = $fields | Where-Object { $_.name -eq 'Size' } | Select-Object -First 1
$iterationField = $fields | Where-Object { $_.name -eq 'Iteration' } | Select-Object -First 1

$priorityOption = $priorityField.options | Where-Object { $_.name -eq $priorityName } | Select-Object -First 1
$sizeOption = $sizeField.options | Where-Object { $_.name -eq $sizeName } | Select-Object -First 1

$currentIteration = Select-CurrentIteration $projectCache.iterations
if (-not $priorityOption -or -not $sizeOption -or -not $currentIteration) {
  $projectCache = Get-ProjectMetadataFromGitHub $owner $repoFull $projectTitle
  Write-JsonFile $cachePath $projectCache
  $project = $projectCache.project
  $fields = @($projectCache.fields)
  $priorityField = $fields | Where-Object { $_.name -eq 'Priority' } | Select-Object -First 1
  $sizeField = $fields | Where-Object { $_.name -eq 'Size' } | Select-Object -First 1
  $iterationField = $fields | Where-Object { $_.name -eq 'Iteration' } | Select-Object -First 1
  $priorityOption = $priorityField.options | Where-Object { $_.name -eq $priorityName } | Select-Object -First 1
  $sizeOption = $sizeField.options | Where-Object { $_.name -eq $sizeName } | Select-Object -First 1
  $currentIteration = Select-CurrentIteration $projectCache.iterations
}

foreach ($field in @($priorityField, $sizeField, $iterationField, $priorityOption, $sizeOption, $currentIteration)) {
  if (-not $field) {
    throw 'Required project field, option, or current iteration was not found.'
  }
}

$labelDefaults = @{
  bug = @{ color = 'd73a4a'; description = "Something isn't working" }
  documentation = @{ color = '0075ca'; description = 'Improvements or additions to documentation' }
  enhancement = @{ color = 'a2eeef'; description = 'New feature or request' }
  feature = @{ color = 'a2eeef'; description = 'Feature work' }
  data = @{ color = '5319e7'; description = 'Data pipeline or game data work' }
  packet = @{ color = '0e8a16'; description = 'Packet/protocol work' }
  db = @{ color = 'c2e0c6'; description = 'Database schema or ORM work' }
  infra = @{ color = '6f42c1'; description = 'Infrastructure and tooling work' }
}

$existingLabels = gh label list --repo $repoFull --limit 200 --json name | ConvertFrom-Json
$existingLabelNames = @($existingLabels | ForEach-Object { $_.name })

foreach ($label in $labels) {
  if ($existingLabelNames -notcontains $label) {
    $default = $labelDefaults[$label]
    if (-not $default) {
      $default = @{ color = 'ededed'; description = 'Project work' }
    }
    gh label create $label --repo $repoFull --color $default.color --description $default.description | Out-Null
  }
}

$issuePayload = @{
  title = $title
  body = ''
  assignees = @($assignee)
  labels = $labels
}
$issue = Invoke-GhJsonInput @('api', "repos/$repoFull/issues", '--method', 'POST') $issuePayload
$issueUrl = $issue.html_url

$item = gh project item-add $project.number --owner $owner --url $issueUrl --format json | ConvertFrom-Json
$itemId = $item.id

$fieldResults = @()
try {
  gh project item-edit --id $itemId --project-id $project.id --field-id $priorityField.id --single-select-option-id $priorityOption.id | Out-Null
  $fieldResults += @{ Field = 'Priority'; Value = $priorityName; Status = 'OK' }
} catch {
  $fieldResults += @{ Field = 'Priority'; Value = $priorityName; Status = 'FAILED'; Error = $_.Exception.Message }
}

try {
  gh project item-edit --id $itemId --project-id $project.id --field-id $sizeField.id --single-select-option-id $sizeOption.id | Out-Null
  $fieldResults += @{ Field = 'Size'; Value = $sizeName; Status = 'OK' }
} catch {
  $fieldResults += @{ Field = 'Size'; Value = $sizeName; Status = 'FAILED'; Error = $_.Exception.Message }
}

try {
  gh project item-edit --id $itemId --project-id $project.id --field-id $iterationField.id --iteration-id $currentIteration.id | Out-Null
  $fieldResults += @{ Field = 'Iteration'; Value = $currentIteration.title; Status = 'OK' }
} catch {
  $fieldResults += @{ Field = 'Iteration'; Value = $currentIteration.title; Status = 'FAILED'; Error = $_.Exception.Message }
}

[pscustomobject]@{
  Issue = "#$($issue.number)"
  Url = $issueUrl
  Title = $title
  Assignee = $assignee
  Labels = ($labels -join ', ')
  Priority = $priorityName
  Size = $sizeName
  Iteration = $currentIteration.title
  Fields = $fieldResults
} | ConvertTo-Json -Depth 10
```

## Report

Final output must be compact:
- created issue URL
- assignee
- labels
- Priority
- Size
- Iteration
- any failed field updates
