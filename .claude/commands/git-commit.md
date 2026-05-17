Commit changes from the current `git diff` by grouping them into logical work units.

Arguments: $ARGUMENTS — optional guidance for work type, issue number, or commit scope.

## Repository Source
1. Read `.env.dev`.
2. Use `GITHUB_REPO_URL` as the target GitHub repository.
3. If `GITHUB_REPO_URL` is missing, STOP and return an exception to the user.
4. Use the issue list at `https://github.com/madalang-games/project-link/issues` to choose matching issue numbers.

## Commit Message Convention
Use one of the following formats:

```
{작업 성질}#{이슈번호}: {커밋 메시지}
{작업 성질}: {커밋 메시지}
```

- `{작업 성질}`: concise work type such as `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `build`, or `ci`.
- `{이슈번호}`: matching GitHub issue number from the repository issue list.
- `{커밋 메시지}`: concise Korean summary; avoid verbose explanations.

Examples:
- `refactor#5: git관련 커맨드 리팩토링`
- `feat#99: 결제 시스템 초기 로직 작성`
- `docs: 커밋 규칙 문서 정리`

## Git Command Rule
Never use `cd {path} && git ...`. Always use `git -C {repo_path} <subcommand>` to avoid permission prompts.

## Steps
1. Read `.env.dev` and verify `GITHUB_REPO_URL` exists.
2. Run `git status` and inspect the current `git diff`.
3. Fetch or inspect the issue list at `https://github.com/madalang-games/project-link/issues`.
4. Group changed files and hunks by work nature.
   If multiple distinct work units exist, create separate commits.
5. For each work unit:
   a. Match the work unit to an existing issue when appropriate.
   b. If no suitable issue exists and the work is large enough to need issue tracking, share the list of issues that should be created with the user before committing.
   c. If the work is small enough that issue tracking is unnecessary, omit the issue number and use `{작업 성질}: {커밋 메시지}`.
   d. Stage only files or hunks belonging to that work unit.
      Never stage `.env.dev` or files ignored by `.gitignore`.
   e. Commit immediately with the selected message format.
6. Report each commit hash and message after completion.

## Exceptions
- Missing `GITHUB_REPO_URL`: return an exception and stop.
- No suitable issue for issue-worthy work: report the issue creation list to the user before committing.
- Small work that does not require an issue: commit without an issue number.
