# shared/datas/string - Client Localization Strings

## Role
Client-facing localized UI string tables.

## Tables
| file | role |
|------|------|
| `clientstring.csv` | Client UI text — one row per stringId, one column per language |
| `error_messages.csv` | Server error code → localized message mapping — one row per errorCode |

## CSV Format (wide — one row per string)
```
stringId,EN,KO,ZH_CN,ZH_TW,TH
C,C,C,C,C,C
string(64),string(512),string(512),string(512),string(512),string(512)
PK,NN,NN,NN,NN,NN
```
- `stringId` is the stable lookup key used by Unity UI components.
- Language columns: `EN`, `KO`, `ZH_CN`, `ZH_TW`, `TH`.
- Keep `EN` complete — runtime fallback resolves to English.
- All language columns are NN (required).

## String Key Prefixes
| prefix | scope |
|--------|-------|
| `app_` | App-level (title, bootstrap) |
| `bootstrap_` | Bootstrap scene |
| `status_` | Status indicators |
| `title_` | Title scene |
| `footer_` | Footer links |
| `lobby_` | Lobby scene |
| `nav_` | Bottom navigation |
| `game_` | In-game UI and popups |
| `popup_` | Shared popup UI |
| `language_` | Language selector labels |
| `country_` | Country/region labels |