E3Plus (C++)

- Entry command: `/e3plus` with subcommands:
  - Core: `on|off|status|tick <ms>`
  - Heals: `heal on|off|status`, `heal life "Spell" <pct>`, `heal single "Spell" <pct>`, `heal group "Spell" <pct> <minInjured>`
- Heals implements a minimal port of E3Next healing priorities: life support > group > single.
- Uses MQ TLOs via parser to query state; casts via `/cast "Spell Name"`.

UI
- Open with `/e3plus ui`. Edit core and heal settings; click Save to persist to `MQ2E3Plus.ini`.
