# shared/datas/string - Client Localization Strings

## Role
Client-facing localized UI string tables.

## Tables
| file | role |
|------|------|
| `clientstring.csv` | Static client UI text keyed by string ID and language code |

## Rules
- `stringId` is the stable lookup key used by Unity UI components.
- `languageCode` uses `EN`, `KO`, `ZH_CN`, `ZH_TW`, `TH`.
- `countryCode` uses ISO-style country/region codes for initial device matching.
- Keep `EN` rows complete because runtime fallback resolves to English.
