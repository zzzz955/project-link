---
name: project-link-commands
description: Project Link repository command workflows. Use when the user invokes or asks about project-local commands `/gen`, `/git-commit`, `/git-commitandpush`, `/git-issue`, `/git-pull`, `/git-push`, `/new-domain`, or `/sync-check`, including the same command names without a leading slash.
---

# Project Link Commands

## Workflow

1. Match the requested command name to `../../commands/<name>.md`.
2. Treat text after the command name as `$ARGUMENTS`.
3. Read only the matching command file.
4. Follow the command file exactly, adapted to Codex tools and the repository's `AGENTS.md` convention.
5. Keep command behavior project-local; do not use global skills for these workflows.

## Command Map

| request | command file |
|---|---|
| `/gen` | `../../commands/gen.md` |
| `/git-commit` | `../../commands/git-commit.md` |
| `/git-commitandpush` | `../../commands/git-commitandpush.md` |
| `/git-issue` | `../../commands/git-issue.md` |
| `/git-pull` | `../../commands/git-pull.md` |
| `/git-push` | `../../commands/git-push.md` |
| `/new-domain` | `../../commands/new-domain.md` |
| `/sync-check` | `../../commands/sync-check.md` |

## Codex Adaptation

- Interpret `CLAUDE.md` references in legacy command text as `AGENTS.md` unless the command explicitly needs Claude compatibility wrappers.
- Use PowerShell commands in this Windows project.
- Preserve the repository rule that `AGENTS.md` is the single source of truth and `CLAUDE.md` wrappers are not edited directly.
