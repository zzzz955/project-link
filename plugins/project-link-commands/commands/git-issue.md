Create a GitHub issue and add it to the project with appropriate metadata.

Arguments: $ARGUMENTS — format: `{도메인}:{이슈명}` e.g. `서버:로그인 API 구현`, `클라:플레이어 이동 처리`

## Prerequisites
Read `.env` and verify the following are set. If any are missing, STOP and notify the user:
- `GITHUB_TOKEN`
- `GITHUB_REPO_URL`
- `GITHUB_DEFAULT_PROJECT`
- `GITHUB_DEFAULT_ASSIGNEE`

## Issue Title Convention
```
[{도메인}] {이슈명}
```
Example: `[서버] 로그인 API 구현`

## Environment: Windows / PowerShell
**ALL shell commands MUST use the PowerShell tool — never the Bash tool.**
Set `GH_TOKEN` at the start of every PowerShell command block:
```powershell
$env:GH_TOKEN = "{GITHUB_TOKEN}"
```

## Steps

### 1. Parse arguments
- Extract `{도메인}` and `{이슈명}` from $ARGUMENTS (split on first `:`).
- If malformed, ask the user to clarify.

### 2. Date fields
- Start date and Target date: leave blank — the user sets these directly in GitHub.

### 3. Determine Priority and Size (autonomous — no confirmation needed)
Assess based on the issue name and domain and set immediately.

Priority options (use the option name as-is from the project's Priority field):
- Check actual option names via `gh project field-list` — they may be P0/P1/P2/P3/P4 or Urgent/High/Medium/Low.

Size options: `XS` / `S` / `M` / `L` / `XL`

Report chosen values in the final summary. User will adjust in GitHub if needed.

### 4. Determine labels
Select appropriate labels (e.g. `bug`, `enhancement`, `feature`, `data`, `packet`, `db`, `infra`).

List existing labels:
```powershell
$env:GH_TOKEN = "{GITHUB_TOKEN}"
gh label list --repo {owner/repo}
```

Create any missing label:
```powershell
gh label create "{label}" --repo {owner/repo} --color "{hex}" --description "{desc}"
```

### 5. Query project fields and current Iteration (Sprint)

**Step A — Get field IDs and option IDs** via `gh project field-list`:
```powershell
$env:GH_TOKEN = "{GITHUB_TOKEN}"
gh project field-list {project_number} --owner {owner} --format json
```
Returns field IDs and single-select option IDs (Priority, Size, Status, etc.).
**Limitation:** Iteration field shows only its field ID — it does NOT include iteration list (startDate, duration, id).

**Step B — Get iteration list** via GraphQL (required for Iteration field):

> **Critical:** GitHub GraphQL API rejects inline literal arguments (`argumentLiteralsIncompatible`).
> Always pass query arguments as GraphQL variables, and send the full request body via `--input` with a UTF-8 no-BOM temp file.
> Never use `-f query=` or `--field query=` with hardcoded node IDs or string arguments inline.

```powershell
$env:GH_TOKEN = "{GITHUB_TOKEN}"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$tmpFile = [System.IO.Path]::GetTempFileName() + ".json"
$content = '{"query":"query($projectId: ID!, $fieldName: String!) { node(id: $projectId) { ... on ProjectV2 { field(name: $fieldName) { ... on ProjectV2IterationField { configuration { iterations { id title startDate duration } } } } } } }","variables":{"projectId":"{project_node_id}","fieldName":"Iteration"}}'
[System.IO.File]::WriteAllText($tmpFile, $content, $utf8NoBom)
$iterData = gh api graphql --input $tmpFile | ConvertFrom-Json
Remove-Item $tmpFile
```

Select the current iteration by matching today's date against `startDate` + `duration` (days).

If field IDs and iteration data were already resolved earlier in this session, skip re-querying.

### 6. Create the issue
**Do NOT use `gh issue create --title` with Korean text** — PowerShell parses `[도메인]` as
array notation and splits on spaces, causing argument errors.

Instead, use the REST API with a UTF-8 no-BOM temp file:
```powershell
$env:GH_TOKEN = "{GITHUB_TOKEN}"
$utf8NoBom = New-Object System.Text.UTF8Encoding $false
$tmpFile = [System.IO.Path]::GetTempFileName() + ".json"
$content = '{"title":"[{도메인}] {이슈명}","body":"","assignees":["{assignee}"],"labels":["{label}"]}'
[System.IO.File]::WriteAllText($tmpFile, $content, $utf8NoBom)
$result = gh api repos/{owner}/{repo}/issues --method POST --input $tmpFile | ConvertFrom-Json
Remove-Item $tmpFile
$issueUrl = $result.html_url
```

**Why temp file:** PowerShell pipe (`|`) transmits UTF-16, which GitHub's API cannot parse.
`[System.IO.File]::WriteAllText` with explicit UTF-8 no-BOM encoding is the only reliable method.

### 7. Add the issue to the project and set fields
**API constraint:** GitHub Projects v2 requires 2 separate steps — Step A result feeds Step B.

Step A — add to project (returns item.id):
```powershell
$env:GH_TOKEN = "{GITHUB_TOKEN}"
$item = gh project item-add {project_number} --owner {owner} --url $issueUrl --format json | ConvertFrom-Json
$itemId = $item.id
```

Step B — set fields via `gh project item-edit`:
```powershell
$env:GH_TOKEN = "{GITHUB_TOKEN}"
$projectId = "{project_node_id}"   # PVT_... from field-list or project list
# Priority
gh project item-edit --id $itemId --project-id $projectId --field-id {priority_field_id} --single-select-option-id {priority_option_id}
# Size
gh project item-edit --id $itemId --project-id $projectId --field-id {size_field_id} --single-select-option-id {size_option_id}
# Iteration
gh project item-edit --id $itemId --project-id $projectId --field-id {iteration_field_id} --iteration-id {iteration_id}
```

Fields to set: Priority, Size, Iteration (current sprint).
Fields to leave blank: Start date, Target date, Status.

### 8. Report
Output the created issue URL and a summary of all fields set.
If any field failed to set, note it clearly so the user can fix it manually.
