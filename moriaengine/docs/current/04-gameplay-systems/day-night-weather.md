# Day/night cycle & weather

`LightAndWeather` (`scripts/Basic/LightAndWeather.cs`, #23) drives both time-of-day
lighting and season/weather from a single recurring clock.

## What existed before this, and the gap

Light switching already reached the client via real packets (region-flag-driven,
underground vs. surface), but **nothing drove it by time of day** - `nightLight` was
explicitly marked "unused for now" in the source. There was no weather/season system at
all: `SeasonalInformationOutPacket` existed, but every call site was commented out, and
the `AbstractCharacter.Season`/`.Cursor` properties that dead code referenced didn't even
exist yet.

Classic UO has no separate rain/snow packet of its own - the client renders
season-appropriate weather (snow in Winter, etc.) purely from the season byte the server
sends. So **cycling `Season` *is* the weather system here** - there's no separate weather
mechanic layered on top.

## `WorldClockTimer`

A single recurring `BoundTimer` on `Globals.Instance` - the same idiom as `NPCAiTimer`/
`CriminalExpiryTimer` elsewhere in this codebase (see
[Tags & timers](../02-engine-core/tags-and-timers.md)) - that periodically:
1. Flips day/night lighting based on elapsed real time modulo a configurable day length.
2. Rolls a new season to simulate weather changing over time.

`IsNight` (`LightAndWeather.cs:81-90`) is a simple half-and-half split: the first half of
the configured day cycle is day, the second half is night - "simple and good enough
without a real sunrise/sunset curve." `dayLengthSeconds` defaults to 24 real minutes = 1
game day (a classic Sphere-ish default), GM-tunable.

`GetLightAt(IPoint4D)` (`LightAndWeather.cs:92-100`) resolves the actual light level for
a point: checks for a `FlaggedRegion`-specific override first (e.g. an always-dark
dungeon), falling back to the plain day/night value otherwise.

`CurrentSeason`'s setter calls `RefreshAllPlayers()` - every connected player gets a
fresh `SeasonalInformationOutPacket` the moment the season actually changes, not just on
next login.

## Real bug found and fixed while building this (#94)

`GameState.lastSentGlobalLightLevel`/`lastSentPersonalLightLevel` defaulted to `0`, which
collides with the real "full daylight"/"no personal light" value - so the very first
light packet on any daytime login was silently dropped as a no-op change, even though the
client had never actually received it yet. (`lastSentSeason` right below it already used
the correct `-1` sentinel pattern - this was an inconsistency within the same class, not
a new idea.) Also, `SeasonalInformationOutPacket` was never sent at login at all. Both
fixed: the light-level fields now use a real sentinel, and season is sent at login like
every other initial-state packet.

## Test coverage

`DayNightWeatherTests` (`scripts/Tests/`, `Tests/Protocol/`) shortens/re-anchors the day
cycle for testing (`ResetDayCycleForTest`-style anchor offset, see
`LightAndWeather.cs:57`'s `dayCycleAnchorSeconds`) to flip light level to night within a
test's own timeout, and confirms a season-change setter actually pushes the real packet.
