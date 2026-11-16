# Engine parity notes (demofile-net vs CS2 engine)

Living notes from reverse-engineering the CS2 engine to find gaps, bugs and inconsistencies
between demofile-net and the real implementation. **Not committed / work in progress.**

- Binaries: `game/bin/win64/networksystem.dll`, `game/bin/win64/engine2.dll` (files dated 2026‑11‑14, CS2 patch 14197 / build 10955), plus string scans of `tier0.dll` and `game/csgo/bin/win64/client.dll`.
- Tooling: Ghidra 12.2 headless + a small decompile/disassemble script; scratch .NET tools (not in the repo) that (a) dump each demo's own `CSVCMsg_FlattenedSerializer` / `CSVCMsg_ServerInfo`, (b) collect `CSVCMsg_PacketEntities` field statistics, (c) replicate our entity reader while also consuming the deletions/non-transmitted/OOPVS streams, and (d) scan entities for out-of-range enum values. Run against `demos/14197.dem` (patch 14197) and older GOTV/POV demos back to 13968.
- Addresses below are image VAs in the listed DLL (image base `0x180000000`). They will drift between builds; the strings next to them are the stable way to re-find code.

Status legend: **BUG** = we produce wrong values or desync; **MISSING** = engine supports it, we throw or ignore; **OK** = verified to match; **INFO** = context, no action needed yet; **FIXED** = fixed in the working tree (uncommitted), with unit tests in `src/DemoFile.Test/Unit/EngineParityDecodeTest.cs`.

---

## 1. Summary table

| # | Area | Status | One-liner |
|---|------|--------|-----------|
| 1 | `qangle` encoder with `bit_count == 32` | **FIXED** | Engine reads 3 raw float32s; we ran `ReadAngle(32)` → garbage. Aim punch (`CCSPlayer_AimPunchServices.m_predictableBaseAngle`, `…AngleVel`, `m_unpredictableBaseAngle`) was wrong: 14197.dem now gives `(-0.0695, -0.0167, ~0)` / vel `(-26.46, -6.76)` instead of `(266.6, 265.1, 249.1)`. |
| 2 | `BitBuffer.ReadAngle(bits)` scale | **FIXED** | Engine: `(float)((int)v * (360.0 / (float)(1u << bits)))`. We divided by `(1 << bits) - 1`. Affected `m_ragAngles` (qangle, 13 bits) and any bit-packed angle. |
| 3 | `qangle_precise` with `bit_count != 0` | **FIXED** (was latent desync) | Engine reads 3 × `ReadBitAngle(bit_count)` with **no presence bits**. Real field: `CBodyComponentBaseModelEntity#3.m_angRotation` (bits=32). |
| 4 | `qangle_pitch_yaw` with `bit_count` 0 or 32 | **FIXED** | Engine reads 2 raw float32s. |
| 5 | `fixed32` var encoder | **FIXED** | Signed + unsigned `fixed32` (raw 32 bits). The 16/32-bit, colour, handle, string-token and world-group decoders now also honour `fixed*` encoders instead of assuming varints. |
| 6 | `coord_integral` var encoder | **FIXED** | See §3.6. Supported for 1/3/6-component types (float, Vector, QAngle, CTransform). |
| 7 | `Quaternion`, `QuaternionStorage`, `CTransform`, `CTransformWS`, `RadianEuler`, `DegreeEuler`, encoder-less `QAngle` | **FIXED** | N float components using the field's float encoder (4 / 4 / 6 / 6 / 3 / 3 / 3). `CTransform` = position + QAngle converted with Source `AngleQuaternion` (same float op order as the engine). Previously threw `NotImplementedException`. |
| 8 | `CSVCMsg_FlattenedSerializer.coord_size_params` | **FIXED** | Read once per send tables (`CoordSizeParams.FromProto`, with the engine's validation and legacy fallback 14/5/11/3/11/20) and carried on `FieldEncodingInfo.CoordSizeParams`; used by `coord`, `coord_integral`, `normal`, `qangle` (bits 0) and `qangle_precise`. **Caveat:** both 14197 demos we have (14197.dem and forze-reload-vs-nexus-m3-dust2.dem, a pro GOTV match recorded 2026‑11‑15) send 14/5/11/3/11/20 — identical to the legacy values — so no real demo exercises non-default widths yet; that path is covered only by the engine code and synthetic unit tests. Neither demo sends `var_enum_info` or uses `coord_integral`. |
| 9 | Quantized float decode precision | **FIXED** | Decode is now `min(high, (float)((double)u * (1.0/(steps-1)) * (double)(high-low) + low))`. |
| 10 | `var_enum_info.is_signed_enum` | **FIXED** | Carried on `FieldEncodingInfo.IsSignedEnum`; takes precedence over the heuristic in #15. |
| 11 | `CSVCMsg_GameSessionConfiguration.quantized_float_encoder_aliases` / `max_coord` | **INFO** | Server-side named quantized-float encoders + world extent. Sendtables still carry fully-resolved bits/range for each field (e.g. `m_vecX` 15 bits, RoundDown, hi 1024 == alias `origin_cell_offset`), so nothing to do unless that changes. |
| 12 | `StringTable.ReplaceWith` removal callbacks | **BUG** (pure C#, open) | After `RemoveRange`, the loop `for (removedIdx = _entries.Count; removedIdx < items.Count; …)` never executes, so removed entries never fire `_onUpdatedEntry(index, null)` (e.g. instance baselines/userinfo not cleared on seek). |
| 13 | Protobuf definitions | **FIXED** | Vendored protos were stale. Updated from GameTracking-CS2 (see §6). |
| 14 | `GameTick_t` decoding | **FIXED** (affected all builds) | `GameTick_t` wraps an `int32` → zig-zag. We read unsigned → every networked tick was **doubled** (and `-1` read as `1`). 14197.dem now: `PredictableBaseTick = 2418` at game tick 2418; weapon `DeployTick` and `NextPrimaryAttackTick` are 64 ticks apart and in the past. |
| 15 | Signed enums decoded as unsigned | **FIXED** | Without `is_signed_enum`, an enum is treated as signed if it has a negative member (matches `demoinfo2.txt` and the data). `AnimLoopMode` now decodes `2` (`USE_SEQUENCE_SETTINGS`) instead of undefined `4`; an entity-wide scan of 14197.dem and pov_test.dem finds no remaining undefined enum values (only the legitimate `ItemQuality = -1` sentinel on an `int32` field). |
| 16 | `CPlayerSlot` decoding | **FIXED** (confirmed) | Zig-zag. Full pro match: fun-fact slots were 8,10,…,26 (+`1`) for controller slots 0–13; now 4–13 and `-1` (as `uint.MaxValue`). |
| 17 | Trailing **deletions** list in `entity_data` (demos ≤ ~14005) | **FIXED** | For delta packets without `non_transmitted_entities`, the engine reads `UBitVar count` + `count × UBitVar index-delta` after the last entity and deletes those entities. We stopped reading after `updated_entries`, leaving ghost entities (13968.dem: 772 live vs engine 768; 14005.dem 213 vs 211; pov.dem 750 vs 747 — dropped weapons). Now read in `OnPacketEntities` (`ReadTrailingEntityDeletions`); delete events fire as normal. |
| 18 | `non_transmitted_entities` + end-of-packet reconcile (demos ≥ ~14030) | **FIXED** | Engine keeps a persistent "non-transmitted" set (1-bit add/remove records, reset on full updates) and, after each packet, **deletes every client entity that is not in the frame, not being deleted and not non-transmitted**. We ignored the field, so in POV demos entities that left the PVS were never deleted (pov_test.dem: 32 weapons, 778 live vs 746). Now tracked (`_nonTransmittedEntities`, dormant-entity set) and reconciled in `OnPacketEntities`. GOTV demos were unaffected (no plain LeavePVS). |
| 19 | `outofpvs_entity_updates` | **IMPLEMENTED** (not yet observed in the wild) | Second bitstream of create/delta records for entities outside the recipient's PVS (see §7.3), read after the main records in `OnPacketEntities` (`ReadOutOfPvsEntities`). Creates share the enter-PVS path (`ReadEnterPvsEntity`, same baselines as the engine's `CopyNewEntity`); touched entities exist with `IsActive == false` and, as in the engine, survive the end-of-packet reconcile only if listed as non-transmitted. POV-only; gated by `sv_outofpvsentityupdates` / `MNetworkOutOfPVSUpdates`. No local demo contains it, so it's verified only against the engine code and synthetic packets (`OutOfPvsEntityUpdatesTest`); worth re-checking on a real POV demo from a server with it enabled. |
| 20 | `GameTick ± TimeSpan` operators | **BUG** (pure C#, open) | `tick.Value + duration.TotalSeconds / 64.0` should be `* 64`. |
| 21 | `BitBuffer` end-of-buffer bookkeeping | **FIXED** | `ReadUBits`' slow path set `_bitsAvail = 64 - n` even when the refill loaded < 8 bytes, so in the last 8 bytes of every buffer `TellBits` was too low and `RemainingBytes` too high. Consequences (gone after the fix): (a) the `DEM_Packet` loop dispatched **phantom trailing messages** — 14197.dem had 1,306 across 303 of 364 packets (fake `NetTick`, `NetSpawnGroupLoad`, `NetSetConVar`, `NetSignonState` …); (b) `update_baseline` copied the wrong bit range for some POV entities (pov.dem 5 of 12,182, pov_test.dem 1 of 802) → corrupted entity baselines. |
| 22 | Quantized float `EncodeIntegers` range | **FIXED** | Engine range is `2^(floor(log2((int)high-(int)low))+1)`, or 2 when the int difference ≤ 1; we used `2^ceil(log2(high-low))`. `m_flWaterLevel` (8 bits, [0,1]) was decoded at half scale. RoundUp+RoundDown no longer throws (engine only warns). |
| 23 | `svc_UserCmds` delta encoding (14197+) | **FIXED** | Decoded in place per player and exposed as `CsDemoParser.UserCmdEvents.UserCmd` (also covers full commands from 14030–14160 GOTV demos and `DEM_UserCmd` in POV demos). Format and engine behaviour in §9. |
| 24 | `DEM_AnimationHeader` / `DEM_Recovery` ignore `DEM_IsCompressed` | **BUG** (open) | Parsed raw instead of via `ReadDemoCommandCore`; pov_test.dem throws `InvalidProtocolBufferException` when subscribed. One-line fix each. |
| 25 | `OnServerInfo` indexes `_playerInfos[msg.PlayerSlot]` without a bounds check | **BUG** (POV, open) | pov_dm.dem (13967) throws `IndexOutOfRangeException` at tick 7856 on a mid-demo `ServerInfo` after string tables were cleared. |
| 26 | `QueuedPacket` priority: `SvcCreateStringTable \| SvcUpdateStringTable` | **BUG** (minor, open) | Bitwise OR (44 \| 45 = 45), so `CreateStringTable` doesn't get priority -9. Should be the pattern `or`. |
| 27 | `svc_EncryptedData` (78) | **MISSING** (not observed locally) | ICE-encrypted net message, key from `cl_decryptdata_key` / `_pub` by `key_type`. Tournament GOTV recorded with a key would hide wrapped messages. |
| 28 | `svc_NextMsgPredicted` (77) / `bi_PredictionEvent` (19) | **INFO** | Client prediction plumbing; nothing needed for parsing. Not observed locally. |
| 29 | `CSVCMsg_GameSessionConfiguration.data` (game session options) | **INFO** (not decoded, by choice) | Binary KeyValues (`VBKV` magic + CRC32 + KV1 nodes), present in every demo from 13968 to 14197: `map`, `mapgroup`, `mode` (`dedicated`/`listenserver`), `numSlots`, `c_game_type`/`c_game_mode` (0/1 competitive, 1/0 arms race, 1/2 deathmatch in our demos), `ct_arms`/`t_arms`, `ct_models`/`t_models`, drop/reward attributes. Not worth adding a KeyValues parser to the library for; users can decode the raw bytes from `ServerInfo.GameSessionConfig.Data` if needed. The rest of `GameSessionConfiguration` is already decoded as protobuf fields. |
| 30 | `CSVCMsg_ServerInfo.game_session_manifest` | **INFO** (not decoded) | Server precache resource manifest. Engine2 `FUN_1803f4fd0`: 1 bit "compressed", 24-bit size, then that many bytes at bit offset 25 (Valve `CLZSS` when compressed); decompressed: 16-bit counts + string tables of extensions (`vmdl`, `vnmskel`, `vpcf`, `vmat`, `vdata`), directories (119) and file names (467), then bit-packed entries. Lists weapon/agent/particle resources the server loaded; low analytics value, so not implemented. |
| 31 | Spawn group handle in the entity-create header | **FIXED** (exposed) | The varint after class id + serial in `FHDR_ENTERPVS` (was `_unknown`, "maybe spawngroup handle?") is the entity's spawn group handle — confirmed on 14197.dem and three 14197 pro matches (see §10). Now exposed as `CEntityInstance.SpawnGroupHandle` (0 = not in a spawn group). |
| 32 | Entity classes missing from our SDK | **MISSING** (latent crash on old demos) | Every pre-14197 demo declares server classes our current SDK has no factory for: 20 in 2023–24 demos (`CBaseFlex`, `CBreachCharge`, `CBumpMine`, `CSensorGrenade`, `CTablet`, `CFists`, `CMelee`, `CWeaponShield`, `CSun`, `CLightGlow`, `CEnvProjectedTexture`, …), `CEnvWindController`/`CEnvWindVolume` in 14090–14160. We install a factory that **throws** ("Attempted to create unknown entity") if one is instantiated; none of our test demos instantiate them, but e.g. a map with wind entities would. The engine instead builds a decode-only fake serializer from the demo's own metadata (`CreateReplayCompatSerializerFromMeta`, `"%s!fake"`) and skips creating the entity (§5.1). Fix: a generic placeholder entity decoded with `FallbackDecoder` (or decode-and-discard). |
| 33 | Valve's replay compatibility table (`scripts/replay_compatability_settings.txt`) | **INFO** (mostly covered) | The client's rename/alias table for old demos (§5.1). We cover its entries with hand-written fallbacks except `DecalRtEncoding_t` → `uint8` (no fallback; would throw if an old demo has such a field and the SDK lacks it) and `WeaponAttackType_t` → **`int32`** (signed; our `m_e…` heuristic reads it unsigned — harmless today because the value is discarded). Could be ported as data instead of code. |
| 34 | Spawn group loads outside the packet stream (`DEM_Recovery`, `DEM_SpawnGroups`) | **MISSING** | Many pro demos never send spawn groups as net messages; they're only in `DEM_Recovery` records and/or the `DEM_SpawnGroups` lump at the header offset (§10.1). So `PacketEvents.NetSpawnGroupLoad` never fires for them and there's nothing to resolve `SpawnGroupHandle` against. Recommended reading in §10.2. |
| 35 | Encrypted messages (`svc_EncryptedData`) | **FIXED** (new) | Never decoded (not even dispatched). Servers encrypt messages for GOTV with `tv_encryptdata_key` (`key_type` 1, private, e.g. team chat) and `tv_encryptdata_key_pub` (`key_type` 2, public). Now decrypted when the user supplies `DemoParser.DecryptionKey` / `PublicDecryptionKey` (§11); the key is never in the demo or broadcast. None of our 75 CS demos contain encrypted messages. |

---

## 2. Field type → decoder mapping (networksystem)

The engine resolves each sendtable field to *(type group, component count)* from a static table
(built in `FUN_1800ed230`, entries at `0x18026f8xx–0x1802707xx`), then picks the encoder within the
group by the field's `var_encoder` name.

Encoder groups (table at `0x18026f580`, `{vtable[], count}`):

| Group | Used by | Encoders (by `var_encoder`) |
|---|---|---|
| signed int (zig-zag varint64 default) | `int8/16/32/64` | default, `fixed8`, `fixed32`, `fixed64` |
| unsigned int (varint64 default) | `uint8/16/32/64`, `Color` (+custom packing) | default, `fixed8`, `fixed32`, `fixed64` |
| float | `float32` (1), `Vector2D` (2), `Vector`/`VectorWS` (3), `Vector4D` (4), `QAngle` (3), `Quaternion`/`QuaternionStorage` (4), `CTransform`/`CTransformWS` (6), `RadianEuler`/`DegreeEuler` (3), `matrix3x4_t`/`matrix3x4a_t` (8), … | default (per-component quantized/noscale), `qangle`, `normal`, `coord`, `coord_integral`, `qangle_pitch_yaw`, `qangle_precise` |
| float64 | `float64` | default only — **reads a 32-bit float**, widened to double |
| string | `CUtlString`, `string_t`, … | default |
| binary block | `CUtlBinaryBlock` | default: varint32 length + bytes |
| bool | bool | default |

Encoder vtables (slot `+0x38` = decode): default-signed `0x18026ee80`, fixed8-signed `0x18026ef00`,
fixed32-signed `0x18026ef80`, fixed64-signed `0x18026f000`, default-unsigned `0x18026ec80`,
fixed8/32/64-unsigned `0x18026ed00/ed80/ee00`, float default `0x18026f180`, qangle `0x18026f200`,
normal `0x18026f280`, coord `0x18026f300`, coord_integral `0x18026f380`, qangle_pitch_yaw
`0x18026f400`, qangle_precise `0x18026f480`.

Encoder name strings live next to `"CNetworkSerializer: Unable to find network encoder named %s!"`.
Note `simtime`/`runetime` are **not** networksystem encoders (they don't appear in CS2 sendtables;
`runetime` is Dota-only).

---

## 3. Primitive decoders: engine vs us

### 3.1 OK (verified against disassembly/decompilation)
- `ReadUVarInt32` (max 5 bytes) / `ReadUVarInt64` (max 10 bytes) — `FUN_1801c5020` / `FUN_1801c50a0`.
- Signed default = zig-zag of varint64 — `FUN_1800d79c0`.
- `fixed8` unsigned = 8 raw bits; signed = 8 bits sign-extended (`FUN_1801c4dd0(buf, 8)`).
- `fixed64` = two 32-bit reads, low word first (equivalent to our 8 little-endian bytes).
- float "noscale" = 32 raw bits (`FUN_1800d7300`).
- `float64` = 32-bit float (matches our fallback because those fields have `bit_count` 32/0).
- `CUtlBinaryBlock` = varint32 length + bytes.
- `normal` (`FUN_1800d9cc0`): hasX, hasY, [sign + N bits]×, negZ; z = sqrt(1 − x² − y²) if < 1. Scale = `1/((1<<normal_bits)-1)`. Matches ours for N = 11.
- `coord` (`FUN_1800de090`): Source-style `ReadBitCoord` per component — hasInt, hasFrac, sign, `int = read(intBits)+1`, `frac * 1/(1<<fracBits)`. Matches ours for 14/5.
- `qangle` with `bit_count == 0`: 3 presence bits, then `ReadBitCoord` per present component. Matches ours.
- `qangle_precise` with `bit_count == 0`: 3 presence bits, `read(angle_bits) * 360/(1<<angle_bits) − 180`. Matches ours for angle_bits = 20.
- Quantized float flag order: RoundDown(1) → `low`, RoundUp(2) → `high`, EncodeZero(4) → `0`, then `bit_count` bits.

### 3.2 `ReadBitAngle` — FIXED (#2)
`FUN_1801c4660`: `CVTSI2SS(1 << bits)`, `MOVSD 360.0`, `DIVSD`, `MULSD value` → `(float)(value * (360.0 / (1 << bits)))`.
Ours: `360.0f * value / ((1 << bits) - 1)`. Also note the engine's `1 << bits` is a 32-bit shift, so for bits = 32 the shift count wraps to 0 (divisor 1). (Irrelevant once #1/#3/#4 route bits=32 to raw floats.)

### 3.3 `qangle` (`FUN_1800dac50`) — FIXED (#1)
```
if bit_count == 0:  3 presence bits, ReadBitCoord per component   (OK today)
elif bit_count == 32: 3 × raw float32                              (we do ReadAngle(32) → garbage)
else:               3 × ReadBitAngle(bit_count)                    (we use wrong divisor, #2)
```
Evidence, 14197.dem: we report `PredictableBaseAngle = (266.56, 265.13, 249.13)`; reinterpreting our
reconstructed bit patterns as float32 gives `(-0.0695, -0.0167, ~0)` and velocity `(-26.46, -6.76)` —
the real aim-punch values.

### 3.4 `qangle_precise` (`FUN_1800dbec0`) — FIXED (#3)
```
if bit_count == 0: 3 presence bits; each present: read(angle_bits) * (360/(1<<angle_bits)) - 180
else:              3 × ReadBitAngle(bit_count)   -- no presence bits
```

### 3.5 `qangle_pitch_yaw` (`FUN_1800dc590`) — FIXED (#4)
```
if (bit_count & ~0x20) == 0 (i.e. 0 or 32): pitch, yaw = raw float32; roll = 0
else: pitch, yaw = ReadBitAngle(bit_count); roll = 0
```

### 3.6 `coord_integral` (`FUN_1800e1700`) — FIXED (#6)
Per integral component: read 2 bits `b`; if `!(b & 2)` → 0. Else `n = (b & 1) ? coord_integer_bits_mp : coord_integer_bits`;
read `n + 1` bits `u`; value = `±((u >> 1) + 1)` (sign = `u & 1`). For 3-component types all three are integral;
for 6-component types (`CTransform`) the first 3 (position) are integral and the remaining 3 use
`ReadBitCoord`-style presence bits (rotation). Single-component floats take a separate path (not yet transcribed).

### 3.7 Quantized floats (`FUN_1800d7650`) — FIXED (#9, #22)
Encoder layout: `low` (+0x00), `high` (+0x04), `decMul` **double** (+0x10), `bits` (+0x1c), flags nibble (+0x1d >> 4), noscale flag (+0x1e bit0).
Value = `(float)((double)u * decMul * (double)(high - low) + (double)low)`, then `if (high <= v) v = high`.

### 3.8 Fixed-width ints — FIXED (#5)
Unsigned fixed32 `FUN_1800d80b0` (32 raw bits), signed `FUN_1800d7f00` (`ReadSBitLong(32)`).
Also: `CreateDecoder_Int32/UInt32/Int16/UInt16` ignore `VarEncoder` entirely; only the 8/64-bit and enum paths honour it.

---

## 4. Coord size params / encoding config (back-compat)

New in 14197: `CSVCMsg_FlattenedSerializer.coord_size_params` (`ProtoCoordSizeParams_t`) and
`CSVCMsg_GameSessionConfiguration.{quantized_float_encoder_aliases, max_coord}`.

engine2 client handling:
- **Serializers** (`FUN_1800843b0`, string `"CL:  serializers carry no coord size params…"`):
  - Present → validated: every value in `[1, 31]` and `max(int, int_mp) + max(frac, frac_mp) + 3 ≤ 32`; otherwise rejected with `"CL:  rejecting unusable coord size params…"`.
  - Absent (all pre-14197 demos) → ask the client DLL (`IClient vtable +0x518`) for legacy defaults (`"…applying legacy encoding (bits %d/%d, mp %d/%d, normal %d, angle %d)"`), else fall back to the running build's `COORDSIZE`.
  - Derived values (`FUN_180099350`): `fracScale = 1/(1<<frac)`, `normalScale = 1/((1<<normal)-1)`, `angleScale = 360.0/(1<<angle)` (double), etc.
- **Server info** (`FUN_180083890` = `ProcessServerInfo`): if `max_coord` present → `Worldsize_SetSessionMaxCoord`, aliases → `OverrideQuantizedFloatEncoderAlias` (tier0). If absent → client-provided legacy aliases + world extent (`"…applying %d legacy encoder alias(es) and world extent %.1f for pre-config playback"`).

**Legacy defaults (client.dll):** `CSource2Client` (vtable `0x181c5aa90`) slot `+0x518` = `FUN_180b64fa0` fills
them from constants compiled into the client (found via RTTI + raw constant decoding; client.dll is too large for a
full Ghidra analysis on this machine):
- coord params `FUN_1809f1660`: **14 / 5, mp 11 / 3, normal 11, angle 20** — identical to `CoordSizeParams.Legacy`.
- world extent `FUN_1809f16b0`: **16384.0**.
- encoder aliases `FUN_1809f1690` → table at `0x182218df0`, filled by static initialiser `FUN_1800bec10`:

  | Alias | Bits | Flags | Range |
  |---|---|---|---|
  | `origin_cell_offset` | 15 | RoundDown | [0, 1024] |
  | `origin_cell_offset_ws` | 15 | RoundDown | [0, 1024] |
  | `origin` | 19 | EncodeZero | [-16384, 16384] |
  | `origin_ws` | 19 | EncodeZero | [-16384, 16384] |

What we do: read `coord_size_params` when present and otherwise use the same legacy values (#8), so pre-14197
demos decode exactly as the engine does. Aliases are only server-side for now: every demo's send tables carry
resolved bits/range per field and no field uses an alias name as its `var_encoder`. If that ever changes,
`CreateDecoder_float` would throw on the unknown encoder and would need the alias table (from
`quantized_float_encoder_aliases`, or the legacy table above).

---

## 5. Engine backwards-compatibility mechanisms (for comparison with our `CsDecoderSet` fallbacks)

| Engine mechanism | Where | Our equivalent |
|---|---|---|
| Legacy coord/normal/angle bit widths when serializers lack `coord_size_params` | engine2 `FUN_1800843b0` | Hard-coded constants (equal to legacy) |
| Legacy quantized-float aliases + world extent when server info lacks encoding config | engine2 `FUN_180083890` | n/a (sendtables carry resolved encodings) |
| Class renames on link: `"CL: CNetworkGameClientBase::LinkClasses renamed %s to %s for replay compatability"` | engine2 | Manual: e.g. `CCSGameModeRules_Scripted` compat class |
| Fake serializers for classes the build no longer has: `"CreateReplayCompatSerializerFromMeta creating fake serializer for %s"` | networksystem `FUN_1800b20d0` | `FallbackDecoder` + `CsDecoderSet.TryCreateFallbackDecoder` |
| Field-changed callbacks for removed fields: `MNetworkReplayCompatField`, `NETWORK_FIELD_CHANGED_REPLAY_COMPAT` | networksystem `FUN_1800b6bb0` | Hand-written `[Obsolete]` properties populated by fallback decoders |
| Whole-demo upconversion: `CDemoPlayer::UpconvertDemoIfNecessary` via `DemoUpconverterVersion001`; `"demo pre-dates demo versioning, will need to be upconverted"`; meta files `demo_<version>.meta` | engine2 `FUN_180032610`, `FUN_1801ffd60` | none |

TODO: transcribe `LinkClasses` rename table and `CreateReplayCompatSerializerFromMeta`.

---

### 5.1 Class renames and fake serializers (engine2 `LinkClasses` `FUN_180085a10`, networksystem `FUN_1800b20d0`)

For each server class in the demo the client doesn't have, `LinkClasses`:
1. asks the client's replay-compat helper (`IClient +0x510` → `FUN_180b65e40`) for a renamed name and links that class
   if it exists (`"CL: CNetworkGameClientBase::LinkClasses renamed %s to %s for replay compatability"`);
2. otherwise, when playing back a demo, asks networksystem to build a **fake serializer from the demo's own
   flattened-serializer metadata** (`CreateReplayCompatSerializerFromMeta`, registered as `"<class>!fake"`). Entities of
   that class are decoded (keeping the bit stream in sync) but never created
   (`"CopyNewEntity:  Skipping create of %d %s [fake class] for replay"`); failing that, `"Client missing networkable entity class %s"`.

The renames and type aliases are data, loaded by the client from `scripts/replay_compatability_settings.txt` (in
`pak01_dir.vpk`; schema `m_RenamedSerializers`, `m_RenamedTypes`, `m_FieldTypeAliases`, `m_PointerFieldTypes`,
`m_UserDefinedSerializerLookup`, `m_RenamedFields`). The whole file for this build:

| Table | Old | New |
|---|---|---|
| `m_RenamedSerializers` | `CCSGameModeRules_Scripted` | `CCSGameModeRules_Noop` |
| `m_RenamedTypes` | `PlayerAnimEvent_t` | `WeaponGameplayAnimState` |
| `m_FieldTypeAliases` (deleted types, used by `CFlattenedSerializer::InitFakeField`) | `EGrenadeThrowState` / `CEntityIndex` / `CSWeaponState_t` / `WorldGroupId_t` / `PlayerAnimEvent_t` / `WeaponAttackType_t` / `DecalRtEncoding_t` | `uint32` / `int32` / `uint32` / `uint32` / `WeaponGameplayAnimState` / `int32` / `uint8` |

Game code also has field-level compat callbacks (`MNetworkReplayCompatField`, e.g. `OnReplayCompat_DamageList`,
`OnReplayCompat_GameSceneNodeOriginAngles`) that translate removed fields into current state.

Our equivalents: `CCSGameModeRules_Scripted` compat class, `CsDecoderSet.TryCreateFallbackDecoder` (`m_ePlayerFireEvent`,
`m_iState`, …) and `FallbackDecoder` (type-name and `E…`/`m_e…` heuristics). Gaps: #32 (unknown classes throw) and #33.
Pre-release 13858 demos (`private-pov.dem`, `maybe-pov.dem`, `casual_inferno_gotv.dem`) use the even older type name
`WeaponState_t`, which neither we nor Valve's table handle.

The `compat_%X` string in engine2 (`FUN_180081b60`) is unrelated: it's the fallback name for an unnamed spawn group.

**Enum signedness, cross-check:** Valve's `demoinfo2.txt` lists `WeaponGameplayAnimState` as signed (`INT64`), but its
values on the wire are plain unsigned varints — a full 14197 pro match has odd raw values throughout (DROPPED 1,
DEPLOY 11, SHOOT_SECONDARY 101, KNIFE misses 501/503, RELOAD_OUTRO 801, INSPECT_OUTRO 1001), impossible under
zig-zag. It has no negative members, so our "negative member ⇒ signed" rule (#15) gets it right where demoinfo2 doesn't.

## 6. Protobuf update (done in working tree, uncommitted)

- Source: `SteamTracking/GameTracking-CS2` `Protobufs/` @ `82e2b9d` (2026‑11‑11). Both `src/DemoFile/Protobufs` and `src/DemoFile.Game.Cs/Protobufs` refreshed.
- Normalisation (script in scratch, reproducible): strip options not declared by any `extend` (`boxed_type`, `synthetic_default`, `additional_includes`), prepend `syntax = "proto2";`, UTF‑8 BOM + CRLF.
- New compiled protos in core: `valveextensions.proto` (now imported by `networkbasetypes.proto`), `source2_steam_stats.proto` (now imported by `netmessages.proto`).
- CS messages Valve removed but older demos still contain are kept in hand-maintained `cs_legacy.proto`: `CCSUsrMsg_WarmupHasEnded` (338), `CCSUsrMsg_GlowPropTurnOff` (360). The removed core messages (`CMsgTEBSPDecal`, `CMsgTEPlayerDecal`, `CMsgTEProjectedDecal`, `CMsgClearDecalsForSkeletonInstanceEvent`, `CUserMessageCloseCaption(Direct)`) were dropped along with their events: their dispatch was already disabled, so nothing could receive them.
- Renames handled: `CSVCMsg_PacketEntities.has_pvs_vis_bits` → `has_pvs_vis_bits_deprecated`, `CDemoFileHeader.network_protocol` → `patch_version`.
- Notable new protocol surface not yet consumed: `svc_NextMsgPredicted` (77), `svc_EncryptedData` (78), `bi_PredictionEvent` (19), `CSVCMsg_PacketEntities.outofpvs_entity_updates` / `non_transmitted_entities`, `CMsgPlayerInfo.clan_member/officer`.

---

## 7. `CSVCMsg_PacketEntities`: how the engine writes and reads entity updates

Reader: engine2 `FUN_18008a710` ("ReadPacketEntities", called from `FUN_180046410` =
`CNetworkGameClient::InternalProcessPacketEntities`). `CNetworkGameClient` vtable `0x1805d0760`:
`+0x460` CopyNewEntity `FUN_180045230`, `+0x468` CopyExistingEntity/ReadDelta `FUN_180045620`.
Writer (server/GOTV): `FUN_1800cfb70` ("WriteDeltaEntities" + "WriteOOPVSDeltaEntities"), type
decision `FUN_1800cd5c0`, header writer `FUN_1800ccd70`, WriteEnterPVS `FUN_1800cd7a0`,
LeavePVS `FUN_1800cdd40`, WriteDeltaEntity_Internal `FUN_1800cdf00`, deletions tail `FUN_1800cecb0`,
WriteNonTransmitted `FUN_1800cf330`.

### 7.1 Main stream (`entity_data`) — OK
Per entity record, `updated_entries` times:
```
UBitVar  indexDelta        index = prev + 1 + indexDelta   (must be < 16384 and increasing)
2 bits   type              bit0 = leave; if leave: bit1 = delete, else bit1 = enter
                           00 delta | 01 leave PVS (dormant) | 11 leave + delete | 10 enter PVS
enter:   classId (serverClassBits), serial (17 bits), varint32 (opaque, passed to entity creation),
         then field paths + values (delta vs instance baseline or the previous entity baseline)
delta:   field paths + values
```
- UBitVar extension widths 4/8/28 match `BitBuffer.ReadUBitVar`.
- Leave+delete is only written when the server's entity in that slot actually changed (true deletion).
- "Preserve" (type 3, entity unchanged) writes nothing; absent entities in a delta are kept.
- Delta vs full is decided by the presence of `delta_from` (hasbit). The writer always sets
  `legacy_is_delta` == has `delta_from`; verified identical in every local demo, so our use of `LegacyIsDelta` is fine.
- No PVS-vis bits in the current writer or reader. Old demos with `has_pvs_vis_bits_deprecated` (≈13987–14005, pov.dem) can't be checked against current code; our 2-bit read is consistent with the data (see 7.5).
- `CopyNewEntity` with an existing entity of the same class **and** serial reuses it as an update — matches our create-vs-update logic.
- Alternate baselines: the engine applies `alternate_baselines` on both create and delta. In every local demo they only ever target deltas (pov.dem: 1449 on delta, 0 on enter), so our `Debug.Assert` holds in practice.
- New cvar `cl_packetentities_merge_before_decode`: engine can merge baseline + wire delta and decode once, to avoid change callbacks firing with baseline values for an entity that already exists. Our enter path decodes baseline then delta into a fresh object before any events fire, so equivalent; on the alternate-baseline delta path we apply baseline then delta to the live entity (INFO for change-callback semantics).

### 7.2 What follows the entity records — BUG (#17/#18)
After the loop the engine reads one of two things (writer chooses by cvar):
- **Old: trailing deletions list** in `entity_data` (delta packets only, when `non_transmitted_entities` is unset):
  `UBitVar count`, then `count × UBitVar delta` (index starts at -1, each delta ≥ 1). Each listed entity is deleted (`FUN_180044f70(..., idx, 1)`).
  Written by `FUN_1800cecb0` ("Delta: [%d] deletions") for entities deleted server-side that weren't reported with leave+delete.
- **New: `non_transmitted_entities { header_count, data }`** (`FUN_18008a240`): copy last frame's non-transmitted set, then per record `UBitVar indexDelta` + 1 bit (1 = add, 0 = remove).
  Then reconcile: every client entity not in this frame, not being deleted and not non-transmitted is deleted (`"CL:  deleting entity %d %s due to reconcile issue!"`).

### 7.3 Out-of-PVS updates (`outofpvs_entity_updates { count, data }`) — MISSING (#19)
Separate bit buffer, read after the main stream:
```
count times:
  UBitVar indexDelta       index = prev + 1 + indexDelta (prev starts at -1; must increase, < 16384)
  1 bit                    1 = OOPVS create (flags 0xC), 0 = OOPVS delta (flags 0x8)
  create: classId, serial (17), varint32, then fields (same as enter, CopyNewEntity)
  delta:  fields (CopyExistingEntity on an entity that is outside the PVS)
```
Entities touched here are tracked in a separate out-of-PVS set (frame `+0x1020`) and excluded from
"preserve". Writer: entities flagged out-of-PVS for this client are written as create when the
from-frame has neither the entity in its PVS set nor its OOPVS set (or the serial changed), else
delta. Header classes: `COutOfPVSDeltaEntitytHeaderReader` / `COutOfPVSDeltaEntityHeaderWriter`.
Gated server-side by `sv_outofpvsentityupdates` and `MNetworkOutOfPVSUpdates` field/class metadata.
This is POV-only: GOTV sees everything.

### 7.4 Implementation in demofile-net
1. **Done (#17):** after the `updated_entries` loop, delta packets without `NonTransmittedEntities` read the deletions tail and delete those entities (delete events fire as normal).
2. **Done (#18):** `NonTransmittedEntities` maintains a persistent per-slot `BitArray` (reset on full updates, like the engine), applies the 1-bit add/remove records, then deletes dormant entities (left the PVS without `FHDR_DELETE`, i.e. not in the frame) that aren't being deleted already and aren't non-transmitted. "In frame" ≈ `IsActive`; dormant indices are tracked in a set so this costs nothing on GOTV demos.
   Storage: `System.Collections.BitArray` (2.1 KB per parser vs 16.4 KB for `bool[16384]`). Benchmarked (BenchmarkDotNet, in-process): set/lookup ~1.37× slower per operation than `bool[]` (21.6 ns vs 15.8 ns for 8 ops per packet), clear ~5× faster (27 ns vs 133 ns), allocation 300 ns → 57 ns; end-to-end parse unchanged within noise (navi GOTV 1,401.6 ± 23.5 ms `bool[]` vs 1,380.4 ± 21.1 ms `BitArray`; pov_test 51.45 ± 1.47 ms vs 51.22 ± 1.10 ms).
3. **Done (#19):** `OutofpvsEntityUpdates` is read after the main records with its own `BitBuffer`: creates go through the same enter-PVS code (baselines, `update_baseline`, slot reuse), deltas apply alternate baselines like the main stream; both leave the entity with `IsActive == false` and track it as dormant so the reconcile treats it like the engine does. As in the engine, the entity reader is re-pointed at the OOPVS stream, so in the (old-format) case without `non_transmitted_entities` the trailing deletions list is read from the end of the OOPVS data.

Validation: the scratch replica (which follows the engine's reader) and the parser now have **identical sets of live entities after every packet** on 13968, 13987, 14005, 14030, 14197, pov_test (14030 POV), pov.dem (14000 POV, 127,611 packets), faze-vs-spirit-m1-nuke and forze-reload-vs-nexus-m3-dust2 (14197, 182,816 packets). Covered by `EntityDeletionIntegrationTest` (all parse modes) and `EntityDeletionLiveCountTest`.

### 7.5 Empirical validation (scratch replica of our reader + the extra streams)

| Demo | Era | Leftover bits after all streams | Trailing deletions | non_transmitted records | Engine-only deletions we missed (before #17/#18) |
|---|---|---|---|---|---|
| 13968.dem (GOTV) | tail list | ≤ 7 (byte padding) in all 477 msgs | 4 | — | 4 (weapons) |
| 13987.dem (GOTV) | tail + PVS bits | ≤ 7 | 3 | — | 0 (already unknown) |
| 14005.dem (GOTV) | tail + PVS bits | ≤ 7 | 2 | — | 2 (weapons) |
| pov.dem 14000 (POV) | tail + PVS bits | ≤ 7 in 127,611 msgs | 1082 | — | 3 |
| 14030 pov_test.dem (POV) | non_transmitted | ≤ 7 | — | 224 | 32 (weapons) |
| 14030/14090/14160/14197 (GOTV) | non_transmitted | ≤ 7 | — | 67–430 | 0 |

Measured after fixing #21 (an earlier version of this table said "< 64 bits / 64-bit padding", which
was an artifact of the `TellBits` bug). In no message do we leave ≥ 8 bits unread, i.e. only byte
padding remains, so the per-entity reading (headers, field-path Huffman decoding, field values) is
bit-exact in practice.
Entity serials wrap at 1024 (max seen in a full pro match = 1023), so comparing the 17-bit header
serial with the 10-bit serial in networked handles is safe.

### 7.6 Other observations
- `pov_dm.dem` (13967) and `pov_ammo_test.dem` (13956) fail with `IndexOutOfRangeException`; `private-pov.dem`/`maybe-pov.dem` (13858) fail with `Unrecognised serializer field: m_iState (WeaponState_t)`. None are covered by tests; not investigated.

---

## 8. String tables (`CNetworkStringTableClient::ParseUpdate`, engine2 `FUN_180107210`) — OK

Per entry: 1 bit "next index" (else `index = prev + varint32 + 2`); 1 bit key present → 1 bit
"use history" → 5-bit position + 5-bit length into a 32-entry ring of previous keys (32-byte slots,
so prefixes ≤ 31 chars) + suffix string, else full string; 1 bit user data present → fixed size:
`user_data_size_bits` bits, else [1 bit compressed if `flags & 1`] + size in **bytes**
(UBitVar if `using_varint_bitcounts`, else 17 bits) + bytes, Snappy-decompressed if flagged;
size capped at 0x138800. All of this matches `StringTable.ReadUpdate`, including the history ring
indexing (`(head - count + 32 + position) & 31` ≡ our `i < 32 ? position : i - (32 - position)`).

Edge case (**minor BUG**): an entry with no key whose index is beyond the current count is added with an
empty name by the engine; we index `_entries[index]` and would throw. Plus #12 (`ReplaceWith`).

---

## 9. User commands (`svc_UserCmds`, `DEM_UserCmd`) — FIXED (#23)

### 9.1 Where user commands appear

| Demos | Representation | Handling |
|---|---|---|
| GOTV ≤ 14005 | none | — |
| GOTV 14030–14160 | `CMsgServerUserCmd.data`: full `CSGOUserCmdPB` | parsed directly |
| GOTV 14197+ | `CMsgServerUserCmd.delta_data` against the player's previous command; occasional full `data` | delta applied in place (§9.2) |
| POV (14000, 14030) | `DEM_UserCmd.data`: varint length + full `CSGOUserCmdPB` (+ optional trailing padding byte), recording player only (`ServerInfo.player_slot`) | parsed directly |

All of them are surfaced through the same event, `CsDemoParser.UserCmdEvents.UserCmd` (`UserCmd` record: slot,
command number, server tick executed, client tick, `CSGOUserCmdPB`, `IsComplete`, `IsDelta`, `Buttons`).
`buttonstate1` is the held-buttons mask: it matches the pawn's networked `m_nButtonDownMaskPrev` on 97.6% of pawn
updates in a full pre-14197 match (the rest is command-vs-snapshot timing), 92% in a POV demo.

### 9.2 Delta format (Valve `codegen_delta_encoder`)

Protobuf wire format plus wire type 7:
- scalar present → new value; wire type 7 (no payload) → clear the field;
- message field (wire type 2) → nested delta applied onto the existing sub-message (created if absent); `(n<<3)|7` deletes it;
- repeated field (wire type 2) → array delta: optional first tag `(n<<3)|7` resizes to `n` (removed elements are cleared and
  reused, so regrown elements start empty), then `(i<<3)|2` tags carry a nested delta for element `i < size`
  (`untrusted_delta_max` = 256 elements); `(field<<3)|7` on the repeated field empties it;
- unknown fields are skipped by wire type; wire types 3/4/6 make the whole delta invalid.

Engine (client.dll, build 10955): `FUN_180b567c0` processes `CSVCMsg_UserCommands`. A full command becomes the player's
baseline (`"full command %d for slot %d replaces newer baseline %d"`). A delta is applied to a copy of the baseline by
the generated applier (`FUN_1807aca00` → `FUN_1807ae3a0` for `CSGOUserCmdPB`, arrays `FUN_1807ab9d0`); the result is
written back as `data`, `delta_data` cleared, `delta_processed` set, and it becomes the new baseline. On failure:
`"delta apply failed"`, command dropped. `cl_svc_usercmds_delta_validate` + server `sv_cq_validate_encoded_svc_usercmds`
send both forms to cross-check.

**Deliberate difference:** with no baseline (recording started mid-session, or after a seek) the engine drops deltas
(`"Ignoring CSVCMsg_UserCommands_t: Missing command to delta from"`). We apply them onto an empty command and report
`IsComplete = false`: on forze-reload-vs-nexus-m3-dust2.dem (patch 14197, 1.81M commands, only 820 full) 66% of commands
are incomplete because most players never receive a full command, yet 99.5% of those already have view angles and 100%
have input history. Malformed deltas reset the player's state (as the engine drops them).

### 9.3 Validation and performance

- Hand-written appliers (no reflection on the hot path; reflection fallback for fields added in future protos) produce
  identical messages to a generic reflection-based reference on all 1,813,721 commands of the test demo; the decoded
  `base.client_tick` equals the outer `client_tick` for all but the very first command of one player (its delta omits the
  unchanged tick).
- Performance on forze-reload-vs-nexus-m3-dust2.dem (334 MB, 222 MB of which are user command messages), median of 3:
  baseline 2.454 s; user commands off 2.469 s (no measurable change, same 620 MB allocated); on 2.842 s (+0.39 s, +16%,
  +18 MB allocated). Isolated decode ≈ 230 ns/command (≈110 bytes, 28.5 fields, 7.8 nested messages each).
  Commands are only decoded while `UserCmdEvents.UserCmd` has a subscriber.

---

## 10. Spawn groups

A spawn group is Source 2's unit of loadable world content: a map or prefab ("world"), its entity lump, and a
resource manifest, loaded/activated/unloaded at runtime as a handle. They form a hierarchy (`spawngroupparenthandle`,
`spawngroupownerhandle`) and each belongs to a world group / scene world (`worldgroupid`, `worldgroupname`).
Engine2 client lifecycle (`CNetworkClientSpawnGroup`): `ProcessSpawnGroup_Load` → prerequisites (allocate, load
entities, wait for owner/child groups, async asset load) → `OnSpawnGroupActivate` → … → `AsyncUnloadSpawnGroup`.
Each load carries a creation sequence; `CSVCMsg_PacketEntities.max_spawngroup_creationsequence` must not exceed
what the client has received ("Packet Entities has a larger spawn group creation sequence … discarding").

**In CS2** (both 14197 demos we have, cs_italy and de_dust2) there are 8 groups, all loaded at sign-on:

| Handle | World | Flags | Entities created in it |
|---|---|---|---|
| 1 | the map (`de_dust2`) | 0x98 | map props, weapons, grenades, most game entities |
| 2 | `maps/prefabs/<map>/<map>_skybox` (world group 2, own scene world) | 0x198 | `CEnvSky`, `CSkyCamera`, `CFuncBrush` |
| 3 | `prefabs/misc/team_select` | 0x98 | `CCSGO_TeamSelect*Position` |
| 4 | `prefabs/misc/end_of_match` | 0x98 | `CCSGO_EndOfMatchLineupStart/End` |
| 5–8 | `prefabs/misc/{counterterrorist,terrorist}_team_intro[_variant2]` | 0x98 | `CCSGO_TeamIntro*Position` |

Players, controllers, pawns, teams and `CWorld` are created with handle 0 (no spawn group). Order and numbering
vary by map. Flags 0x98 = `IS_INITIAL_SPAWN_GROUP | CREATE_CLIENT_ONLY_ENTITIES | LOAD_STREAMING_DATA`; 0x100 adds
`CREATE_NEW_SCENE_WORLD`. Prefab groups have a `world_offset_pos` (where the UI scene sits in the map).
`CSVCMsg_PacketEntities.active_spawngroup_handle` is always 1 (the map).

### 10.1 How spawn groups reach a demo, and what `DEM_Recovery` is

Engine2 functions: recorder `AddSpawnMessageToDemoSpawnGroups` (`FUN_18010d460`, called from the HLTV demo recorder
`FUN_1801239b0`/`FUN_180123de0`), `WriteSpawnGroupToDemo` (`FUN_180025cb0`), `CDemoRecorder::WriteSpawnGroups`
(`FUN_180025fa0`, called when recording stops), playback `CDemoPlayer::LoadSpawnGroups` (`FUN_18002b250`),
`CDemoPlayer::InternalReadPacket` (`FUN_18002bc80`), and `CDemoPlayer::TryRecoverDemoFile` (`FUN_1800312e0`).

All spawn-group payloads outside the packet stream use networksystem's **bit-packed net message framing** (the same
as inside `DEM_Packet`): `UBitVar type` (8 Load, 9 ManifestUpdate, 11 SetCreationTick, 12 Unload), `varint size`,
protobuf payload.

- **`DEM_SpawnGroups` lump** (header bytes 12–15 = its file offset). `CDemoSpawnGroups.msgs` is a list of framed
  messages. The recorder accumulates every spawn group message and writes the lump when recording stops (after
  `DEM_Stop` for GOTV); client/POV recorders write it near the start of the stream instead. On playback the engine
  reads it **at open** (`LoadSpawnGroups`) and dispatches each message to the same `ProcessSpawnGroup_*` handlers as
  live net messages, "preloading" the groups.
- **`DEM_Recovery`** (command 18). Written by the HLTV recorder *as each spawn group message happens*, as a pair:
  `{ spawn_group_message = <framed message> }` then `{ initial_spawn_group { spawngrouphandle, was_created } }`
  (Load messages are written with `SPAWN_GROUP_IS_INITIAL_SPAWN_GROUP` forced on). Normal playback **ignores**
  command 18 (default branch of `InternalReadPacket`). Its only consumer is `TryRecoverDemoFile`, which repairs a
  truncated recording (e.g. the server crashed before stopping): back up to `.bak`, truncate at the last valid
  command, replay the recovery records to rebuild the `DEM_SpawnGroups` lump, then write `DEM_FileInfo` (fixed
  playback time) and the missing `DEM_Stop`, patching the header offsets. In other words, `DEM_Recovery` is a
  write-ahead copy of the spawn group lump, so a crashed recording can still be played back.

Observed across the local demos:

| Recorder | Sign-on net messages | In-stream `DEM_SpawnGroups` | `DEM_Recovery` pairs | Trailing lump |
|---|---|---|---|---|
| Game-server GOTV (13968–14197 small demos) | ✔ (each sent twice) | – | – | empty |
| Pro GOTV (faze-vs-spirit, navi, mouz, …) | – | – | – | ✔ all loads |
| HLTV relay (forze-reload-vs-nexus ×3, big-vs-nip, parivision-vs-spirit, `1-<guid>` ×3) | – | – | ✔ 6–10 pairs at the start | ✔ all loads |
| POV / client | ✔ | ✔ (the header offset points here) | – | – |

### 10.2 How demofile-net should read them (recommendation)

1. Fix #24 so `DEM_Recovery` (and `DEM_AnimationHeader`) honour the compressed flag.
2. Unwrap framed spawn group messages from `DEM_Recovery.spawn_group_message`, in-stream `DEM_SpawnGroups`, and —
   for seekable streams, at start like `DEM_FileInfo` — the lump at the header offset, then feed them through the
   **same** path as net messages. That mirrors the engine (lump at open → `ProcessSpawnGroup_*`) and makes
   `NetSpawnGroupLoad` fire for every demo type. Dedupe by handle, as the engine does ("spawn group already
   loaded"), because POV and game-server GOTV demos carry the same loads twice.
3. Keep a parser-level `SpawnGroups` registry (handle → world name, parent, flags, offset, world group), updated by
   Load/Unload, so `CEntityInstance.SpawnGroupHandle` can be resolved for any demo.
4. Don't depend on `DEM_Recovery` alone: game-server and pro GOTV demos don't have it, and it duplicates the lump.
   Use it for streams where the lump isn't reachable (non-seekable/HTTP) or when the file is truncated.

For analysis, the useful part is the per-entity handle (#31): it separates real map entities from the skybox and
the UI prefabs (team intro / team select / end-of-match lineup), which otherwise look like ordinary entities.

---

## 11. Encrypted messages (`svc_EncryptedData`) — FIXED (#35)

**Format (engine2 `CmdEncryptedDataMessageCodec` decode `FUN_180233a70`, encode `SVCMsg_EncryptedData_EncryptMessage` `FUN_180233590`).**
The server-side encoder is the CS:GO scheme unchanged:

1. Key: the convar string itself, which must be exactly 16 bytes (`"key size is %d, but %d is expected!"`), used as
   an ICE level-2 key (`IceKey(2)`: ctor `FUN_180372620`, set `FUN_1803730d0`, decrypt block `FUN_1803726f0`).
2. Plaintext: `[padding length n][n random bytes 0x10..0xFA][uint32 big-endian size][message]`, where
   `n = 8 + r - (size + 5 + r) % 8` for a random `r` in [16, 72], so the total is a whole number of 8-byte blocks.
   The message is serialised by `CNetworkMessages::Serialize` (networksystem `FUN_1800e6fd0`): `UBitVar` message id,
   `varint` size, protobuf bytes, i.e. exactly like a message inside `DEM_Packet`.
3. Ciphertext: each 8-byte block ICE-encrypted independently (ECB), at most 0x138800 bytes.

The client (`FUN_180088f30`) picks the key by `key_type`: **1 → `cl_decryptdata_key`**, **2 → `cl_decryptdata_key_pub`**
(any other value → empty key → ignored). Decoding then rejects data whose length isn't a multiple of 8, whose first
byte (padding length) is 0, or whose big-endian size doesn't account for exactly the rest of the buffer; that's what
makes a wrong key fail silently. A valid embedded message is processed immediately, in place of the encrypted one.

**Where keys come from.** The key is never part of the stream:
- Valve servers get it from the GC in `CMsgGCCStrike15_v2_MatchmakingGC2ServerReserve.encryption_key` /
  `encryption_key_pub` (uint64).
- Clients get the same values from the GC as `WatchableMatchInfo.cl_decryptdata_key` / `cl_decryptdata_key_pub`
  (uint64); client.dll applies them with `cl_decryptdata_key %016llX;` (16 upper-case hex digits) right before
  `playcast "%s/%llui%d";` or connecting to GOTV. `WatchableMatchInfo` is also in `CDataGCCStrike15_v2_MatchInfo`,
  i.e. a matchmaking demo's `.dem.info` file and the GC's match list.
- Community servers set `tv_encryptdata_key` / `tv_encryptdata_key_pub` themselves (any 16-character string).

**HTTP broadcast (playcast): the key isn't available.** The `/sync` response only carries stream timing fields
(`tick`, `endtick`, `maxtick`, `rtdelay`, `rcvage`, `fragment`, `signup_fragment`, `tps`, `keyframe_interval`, `map`,
`protocol`, `token_redirect`; engine2 strings around `0x1805e7c98`), the GC sync alternative
(`CEngineGotvSyncPacket`) has no key field either, and neither of our local broadcast captures contains anything
key-like. A broadcast consumer has to obtain the key out of band, as the game client does from the GC.

**demofile-net.** `PacketEvents.SvcEncryptedData` exposes the raw message. `DemoParser.DecryptionKey` and
`PublicDecryptionKey` (`DemoDecryptionKey`: a 16-character string, or a uint64 formatted as `%016llX`) enable
decryption; the embedded message is raised through its normal event. `IceKey` matches engine2's ICE bit for bit
(300 random keys/blocks, including non-hex string keys, compared by calling the engine's own functions from
the DLL); `EncryptedDataTest` covers the codec with messages built like `SVCMsg_EncryptedData_EncryptMessage`.
No real demo with encrypted data was available: none of the 75 CS demos in `demos/` contains `svc_EncryptedData`.

## 12. Still to investigate

- `matrix3x4_t` 8-component layout (no field uses it in any demo we have).

Resolved: legacy client defaults (§4), `LinkClasses` renames and fake serializers (§5.1), and the single-component
`coord_integral` path (same 2-bit header + `bits+1` value as each component of the 3/6-component forms, i.e.
`ReadCoordIntegral`; `FUN_1800e1700` also handles 8 components for matrices).
