# infra/systemd

## Files
| file | role |
|------|------|
| `project-link.service` | systemd unit template for the project-link Docker Compose stack. Install to `/etc/systemd/system/project-link.service`. |

## Rules
- Edit `WorkingDirectory` to the actual server deploy path before installing.
- Must start after `platform.service` (madalang-net is created by platform stack).
- Enable with `systemctl enable project-link && systemctl start project-link`.
