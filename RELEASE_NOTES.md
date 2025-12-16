### 0.39.1 (2025-12-16)

- Update to latest Deadlock schema

### 0.38.1 (2025-12-04)

- Update to latest Deadlock schema

### 0.37.1 (2025-11-12)

- Update to latest CS2 schema (v14121)

### 0.36.2 (2025-09-22)

- No code changes. Re-release of 0.36.1

### 0.36.1 (2025-09-18)

- Update to latest CS2 schema (2025-09-17)

### 0.35.1 (2025-09-05)

- Update to latest Deadlock schema

### 0.34.1 (2025-08-27)

- Update to latest CS2 schema (v14097)

### 0.33.1 (2025-08-18)

- Update to latest CS2 schema (v14095) - thanks @BoneTM

### 0.32.1 (2025-08-14)

- Add `CBaseEntity.HasModel(..)` to check if an entity has a specific model

### 0.31.2 (2025-08-06)

- Add compatibility shims for `CCSWeaponBase.State` and `CCSWeaponBase.PlayerAnimState` fields for pre 2025-07-28 demos

### 0.31.1 (2025-08-01)

- Update to latest CS2 schema after 2025-07-28 update

### 0.30.2 (2025-07-29)

- Temporary workaround to read latest CS2 demos after 2025-07-28 update

### 0.30.1 (2025-06-01)

- Update to latest Deadlock schema (v10423.dem)

### 0.29.1 (2025-03-01)

- Update to latest Deadlock schema

### 0.28.1 (2025-01-31)

- Update to latest CS2 schema (v14065)
- Add support for new `DemRecovery` message

### 0.27.1 (2024-11-02)

- BREAKING CHANGE: Exceptions when fetching data from the broadcast server are now immediately propagated to `MoveNextAsync`. You must handle retries and rate limiting in your `HttpClient`, e.g. by using `RetryingBroadcastHttpHandler`
- Added `RetryingBroadcastHttpHandler`:
  - Handles automatic (limited) retries of HTTP 404 responses for `/delta` requests. This logic has been removed from `HttpBroadcastReader`
  - Throttles successful `/delta` requests (rolling 1-second window)
- Added `Uri baseAddress` parameter to `HttpBroadcastReader` constructor to simplify instantiation by using `RetryingBroadcastHttpHandler`.
- Added `HttpBroadcastReader.BroadcastSyncDto` (Sync DTO as retrieved from the `/sync` endpoint)
- Added `HttpBroadcastReader.EnqueuedFragmentsCount` (Number of enqueued fragments that are ready to read in `MoveNextAsync`)

### 0.26.1 (2024-10-29)

- Update to latest Deadlock schema

### 0.25.1 (2024-10-12)

- Update to latest Deadlock schema
- Fixed bug where `ReadAllParallelAsync` could miss some data when reading a demo with no full packets (thanks [@in0finite](https://github.com/in0finite))
- Improved incomplete demo file parsing (thanks [@in0finite](https://github.com/in0finite))
- Added `DemoFileReader.FullPacketCount`, `IsIncompleteFile` and `IncompleteFileLastStreamPosition` properties
- Added `maxParallelism` parameter to `ReadAllParallelAsync`

### 0.24.1 (2024-10-07)

- Update to Counter-Strike 2 schema v14034

### 0.23.1 (2024-09-27)

- Update to Deadlock schema v48

### 0.22.3 (2024-09-23)

- Fixed parallel parsing of POV demos ([#92](https://github.com/saul/demofile-net/issues/92))

### 0.22.2 (2024-09-23)

- Fix entities being re-created multiple times ([#91](https://github.com/saul/demofile-net/issues/91))
- Added check to ensure demos are for the correct game (e.g. CsDemoParser reads 'csgo' demos, DeadlockDemoParser reads 'citadel' demos)

### 0.22.1 (2024-09-22) - Significant API change

- BREAKING CHANGE: Stream reading logic has been split out of `DemoParser` into `DemoFileReader`. \
  This change enables different sources of demo data, e.g. HTTP broadcasts.
  - Old `DemoParser.ReadAllAsync` (static) => now `DemoFileReader.ReadAllAsync` (instance)
  - Old `DemoParser.StartReadingAsync` => now `DemoFileReader.StartReadingAsync`
  - Old `DemoParser.MoveNextAsync` => now `DemoFileReader.MoveNextAsync`
  - Old `DemoParser.OnProgress` => now `DemoFileReader.OnProgress`
- Game-specific SDK types have moved out of the `DemoFile` namespace into `DemoFile.Game.Cs` and `DemoFile.Game.Deadlock`. \
  You may need to add some `using DemoFile.Game.Cs;` to your existing code.
- Added support for reading HTTP broadcasts, see [DemoFile.Example.HttpBroadcast](./examples/DemoFile.Example.HttpBroadcast)

### 0.21.1 (2024-09-14) - Significant API change

- BREAKING CHANGE: Split CS-specific code out of DemoFile into a new NuGet package called [`DemoFile.Game.Cs`](https://www.nuget.org/packages/DemoFile.Game.Cs)
- BREAKING CHANGE: `DemoParser` class is now abstract - port your code to `new CsDemoParser()` instead
- BREAKING CHANGE: Renamed `DemoParser.IsGotv` to `DemoParser.IsTvRecording`
- BREAKING CHANGE: Renamed `DemoParser.GameEvents` to `DemoParser.BaseGameEvents`
- BREAKING CHANGE: Renamed `DemoParser.UserMessageEvents` to `DemoParser.BaseUserMessageEvents`
- BREAKING CHANGE: Renamed `DemoParser.CsgoGameEvents` to `CsDemoParser.GameEvents`
- Added `CsDemoParser.UserMessageEvents` for CS2-specific user messages ([#81](https://github.com/saul/demofile-net/issues/81))
- Added preliminary support for Deadlock in the new [`DemoFile.Game.Deadlock`](https://www.nuget.org/packages/DemoFile.Game.Deadlock) package - use `new DeadlockDemoParser()` 
- Reduce allocations for compressed packets (reduces total allocs for a typical demo by -10%)

### 0.20.1 (2024-09-07)

- Add `DemoParser.ReadAllParallelAsync` to read a demo across multiple threads. \
  Many thanks to [@in0finite](https://github.com/in0finite) for the initial implementation.

### 0.19.1 (2024-09-07)

- Added support for POV demos

### 0.18.1 (2024-08-16)

- Update schema and demo format for v14026

### 0.17.1 (2024-08-11)

- Add new `BulletDamage` Source1 game event
- Parse `cs_gameevents.proto` messages (specifically CMsgTEFireBullets on `DemoFile.CsgoGameEvents.FireBullets`)
- Parse `te.proto` (temporary entities) via `DemoFile.TempEntityEvents`
- Don't try to parse messages that are never seen in demos (to improve performance)

### 0.16.3 (2024-08-02)

- Fixed backwards seeking failing for some demos ([#76](https://github.com/saul/demofile-net/issues/76))

### 0.16.2 (2024-08-01)

- Fixed CBaseCSGrenade.GrenadeCount always being 1 ([#71](https://github.com/saul/demofile-net/issues/71))

### 0.16.1 (2024-08-01)

- Fixed seeking failing for some demos ([#72](https://github.com/saul/demofile-net/issues/72)). Thanks to [@in0finite](https://github.com/in0finite) for reporting.

### 0.15.1 (2024-06-01)

- Update schema and demo format for v14011 

### 0.14.1 (2024-05-26)

- Fix parsing error since 2024-05-23 update (full schema update to follow)

### 0.13.1 (2024-04-30)

- Update schema and demo format for v14005

### 0.12.2 (2024-04-12)

- Fix player pawn positions occasionally jittering ([#37](https://github.com/saul/demofile-net/issues/37)). Thanks to [@in0finite](https://github.com/in0finite) for spotting the bug.

### 0.12.1 (2024-02-29)

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
