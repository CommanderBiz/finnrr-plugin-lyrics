# 🦁 Finnrr Lyrics — Jellyfin Plugin

Finnrr's own Jellyfin lyrics plugin. Downloads synced lyrics from [lrclib.net](https://lrclib.net) for your music library — and the Finnrr difference: **automatically syncs lyrics for newly added music** after every library scan. No manual task runs when new albums land.

A GPL-3.0 fork of [Felitendo/jellyfin-plugin-lyrics](https://github.com/Felitendo/jellyfin-plugin-lyrics) v1.6.6.0, rebranded as part of the Finnrr project (Finnrr = Chaldean 23, the Royal Star of the Lion).

## Features

- 🔄 Automatically downloads lyrics for your entire library (scheduled task: *Download and upgrade lyrics*)
- ⚡ **Finnrr auto-sync** — fetches lyrics for newly added tracks right after each library scan (new in this fork)
- 🌐 Fetches lyrics directly from lrclib.net (or a self-hosted LRCLIB instance)
- 🕒 Synced lyrics show up in any Jellyfin client's Now Playing screen — including Finnrr
- ⚡ Smart scheduled task that avoids retrying failed songs every day (adaptive backoff)
- 🎯 Match filtering by artist + duration tolerance to keep intro clips and wrong matches out

## Installation

1. Jellyfin **10.11.6 or newer**.
2. If the old "LrcLib" plugin (`jellyfin-plugin-lrclib`) is installed, uninstall it and restart Jellyfin (this plugin also auto-marks it for removal on startup).
3. Add the plugin repository to Jellyfin:
   `https://raw.githubusercontent.com/CommanderBiz/jellyfin-plugin-finnrr/master/manifest.json`
4. Open the Plugin Catalog → **Finnrr Lyrics** (Metadata category) → Install → restart Jellyfin.
5. Run *Download and upgrade lyrics* once under Scheduled Tasks to backfill the library.
6. Scan all libraries. Everything added afterwards gets lyrics **automatically**.

## Settings

| Setting | Default | What it does |
|---|---|---|
| Use strict search | off | Exact match only (artist + title) instead of fuzzy |
| Exclude artist / album name | album off | Removes those from search parameters |
| Filter matches by song length | on (15s tolerance) | Rejects lyrics whose duration differs too much (kills intro-clip matches) |
| Skip repeated misses | on | Backs off 1, 3, 7, 30 days for tracks with no lyrics online |
| Limit work per run | on (2000) | Caps tracks checked per scheduled run |
| **Auto-sync new music (Finnrr)** | **on (100)** | **After each library scan, fetches lyrics for the newest tracks missing them** |
| LRCLIB server URL | lrclib.net | Point at a self-hosted LRCLIB instance if you run one |

## Troubleshooting

- **Lyrics not showing for a track?** Right-click the song → *Edit song text* → search icon, or refresh metadata on the album.
- **Wrong lyrics on instrumentals/interludes?** Lower the duration tolerance (e.g. 5s).
- **Legit songs skipped?** Raise duration tolerance (e.g. 30s) or toggle strict search.

## License

GPL-3.0. Fork of [Felitendo/jellyfin-plugin-lyrics](https://github.com/Felitendo/jellyfin-plugin-lyrics) (GPL-3.0). Lyrics from [lrclib.net](https://lrclib.net).
