### 0.12.1 (2024-02-25)

- Add support for seeking to arbitrary ticks with `DemoParser.SeekToTickAsync`. \
  It supports seeking backwards and forwards, and makes use of Source 2 demo 'full packet' snapshots to do this efficiently.

### 0.11.1 (2024-02-19)

- Reading CDemoFileInfo is best effort
- Throw EndOfStreamException when unable to read any more bytes from the demo

### 0.10.1 (2024-02-17)

- Synthesise `Source1GameEvents.RoundStart` and `Source1GameEvents.RoundEnds` events on newer demos

### 0.9.1 (2024-02-09)

- Update schema for v13987

### 0.8.1 (2023-12-30)

- BREAKING CHANGE: `DemoParser.Start` is now `DemoParser.ReadAllAsync`
- Added ability to read tick-by-tick with `DemoParser.StartReadingAsync` and `DemoParser.MoveNextAsync`
- Added comparison operators to `DemoTick` and `GameTick`

### 0.7.1 (2023-12-23)

- Read CDemoFileInfo in seekable streams (available on complete demo recordings only)

### 0.6.1 (2023-12-23)

- Only read the first entity snapshot in GOTV demos (~10% perf improvement)
- Added `DemoFile.IsGotv`, indicating whether the demo was recorded by GOTV
- Fix pawn position jumping to multiples of 1024 for a single tick (https://github.com/saul/demofile-net/issues/27)

### 0.5.1 (2023-12-19)

- Fix decode exception caused by .NET runtime bug (https://github.com/dotnet/runtime/issues/96174)

### 0.4.1 (2023-12-09)

- Update to schema v13980
- Improve fallback field decoding (GameTime_t, GameTick_t and heuristics for enums)

### 0.3.6 (2023-11-25)

- Fix alternate baseline regression introduced in 0.3.x

### 0.3.5 (2023-11-25)

- Added release notes

### 0.3.1 (2023-11-25)

- Fix StrictEHandle event key parsing (fixes `FooPawn` properties on game events)

### 0.2.9 (2023-11-12)

- Use lower byte of user ID to get controller slot in game events

### 0.2.8 (2023-11-12)

- Fix stringtable updates sometimes being off by one

### 0.2.7 (2023-11-12)

- Fix userid event vars being parsed as entity indexes
- Add GetPlayerByUserId/SteamId helper methods

### 0.2.6 (2023-11-11)

- Add autogen helpers for entity handle schema fields

### 0.2.5 (2023-11-11)

- Add support for MNetworkVarTypeOverride
- Add class-level metadata as comments

### 0.2.4 (2023-11-08)

- Add GameItems class
- Add name/paintkit/rarity/quality helper properties to CEconItemView

### 0.2.3 (2023-11-05)

- Update schema for v13966
- Add CCSPlayerPawn.InputButtons and CSMovementServices

### 0.2.2 (2023-10-29)

- Add events for entity creation/update/deletion
- Improve game event generation

### 0.2.1 (2023-10-25)

- Update schema for v13964
- Support fallback field decoding
