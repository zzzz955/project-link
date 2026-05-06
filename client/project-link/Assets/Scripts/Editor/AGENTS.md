# Editor Tools

## Role
Unity Editor-only automation for project setup, scene composition, validation, and content generation.

## Rules
- Put files in an `Editor` assembly/folder so they are excluded from player builds.
- Prefer UnityEditor APIs over direct `.unity` YAML edits.
- Do not touch `Assets/Resources/Data/` or `Assets/Scripts/Data/Generated/`.
- Scene builders must be idempotent: remove or update their own generated roots before recreating.
- Keep generated scene root names stable for diff readability.

