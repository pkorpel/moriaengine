# Performance review (#1161)

Done on 2026-11-04 to find the bottlenecks before the world is populated and players log in. This is research and
proposals only: no engine code changed. Every number below was measured unless it says *estimate* or *extrapolated*.

**Short version.** The engine runs the whole game on one thread (everything takes `globalLock`), and that is fine: so do
both reference engines. The problem is what that thread does. **Every NPC in the world thinks about once a second, wherever
it is, whether or not a player is near.** With the estimated full world (30k NPCs, 626k items) the game thread is 72% busy
**with no players online**, timers run up to 2.3 s late and the process allocates 218 MB/s. With 50 players it reaches 82%,
and at 50k NPCs it falls over (timers 2–5 s late). Sphere avoids this with *sector sleep*: nothing ticks where no player has
been for 10 minutes. A throwaway prototype of the same idea plus two small fixes took the design world from **72% to 12%**
busy and timer lateness p99 from **0.9 s to 37 ms**. The other big items: each save blocks the whole server for 2.6–5 s
(the hourly autosave included), and a Release build doesn't compile, so every server runs Debug, which costs 2–3× per NPC
think.

**Second pass (2026-11-04, #1168).** Corrected and added after review:
- The hourly autosave **does exist** (`scripts/Basic/PeriodicSave.scp`); the first pass missed it (problem 3).
- **#1163, saves that don't load cleanly**, is now problem 0. 11 of 24 code-registered defs don't exist when a save loads,
  and an in-progress skill loses its skill.
- **The user's return-home idea** (straight step first, A* only when it fails) cut the idle design world from 58% to 44%
  (problem 7).
- **Sector size** 16 vs 32 vs 64 is measured: keep 16 (section 12).
- **The full implementation plan** is section 8.

**After the plan (2026-11-06, #1176).** Every step of section 8 is done. The same harness, run again, is in the
[performance report after the plan](performance-report-2026-11.md): the design world idle went from 72% to 0.2% busy,
50 players from 82% / 62% (crowd / dungeon) to 1.6% / 4.3%, and the autosave pause from 2.6–5 s to 0.74 s.

Contents: [1. Assumptions and targets](#1-assumptions-and-targets) ·
[2. Reference engines](#2-how-the-reference-engines-do-it) · [3. Comparison](#3-comparison) ·
[4. Our engine](#4-our-engine-main-loop-and-threads) · [5. Measurements](#5-measurements) ·
[6. Problems and proposals](#6-problems-ranked-with-proposals) · [7. Checked and fine](#7-checked-and-fine) ·
[8. Implementation plan](#8-implementation-plan) · [9. Benchmark setup](#9-benchmark-and-stress-test-setup) ·
[10. Limits](#10-limits-of-this-study) · [11. More cores](#11-using-more-cores-stm) · [12. Sector size](#12-sector-size-measured)

## 1. Assumptions and targets

### World size (estimate)

There is no spawn data yet, so the world is estimated from what the archive's `map.def` says about geography, and from
what Sphere shards of this kind carry.

| | Estimate | Basis |
|---|---|---|
| Map planes in use | ~35 | 769 regions in `scripts/converted/map.def` use 35 planes; all are facet 0 (6144×4096) except plane 2 (2304×1600). |
| Dungeons | 71 regions, 98 rectangles, 1.54M tiles of bounding box on 15 planes | Regions named like dungeons/caves/mines in `map.def`; the largest are a_Ledove_jeskyne_level_1 (132k tiles), a_MORIA (114k). |
| NPCs, design | **30,000** | Dungeons ~15k (about 1 monster per 100 bounding-box tiles, roughly 1 per 30–40 walkable tiles), overworld monsters and animals ~12k, towns (vendors, guards, quest NPCs, `.SetupWorld` pieces) ~2–3k, players' pets and mounts ~1k. |
| NPCs, stress | **50,000** | Design × 1.7: a generous spawner or a later expansion. |
| Spawn points | ~6–10k | 30k NPCs at 3–5 per spawn gem. |
| Items, design | **~1,000,000** | NPC packs and loot (measured 4.1 items per monster, ~125k), world decoration and fixtures placed by GMs (~200k), player-owned items in banks, 281 houses, stalls and vendors (~300–500 accounts × ~1000, ~300–500k), transient loot and corpses (~10–20k). |
| Players | **50** concurrent at peak | The brief. Most on 3.0.6m clients. |

The harness populated 30k NPCs (15k in the 98 dungeon rectangles on their real planes, 10k on plane 0's overworld, 5k on
plane 1) and 626k items (150k decoration in the dungeons, 50k on the overworld, 124k in monsters' packs, 300k in 2,000
stored containers on plane 250 standing in for banks and houses). Item-driven numbers (save time, memory) are
extrapolated from 626k to 1M linearly and marked so.

### Targets

Our engine has no fixed world tick: it runs timers from a priority queue, so the "tick rate" target is stated as how late
timers fire and how busy the game thread is. Sphere's world tick is 100 ms (`TICK_PER_SEC 10`), which sets the scale.

| Metric | Target at peak (design world, 50 players) | Why |
|---|---|---|
| Game thread busy (time `globalLock` is held) | ≤ 50% | Leaves 2× headroom for bursts: mass PvP, events, a save. |
| Timer lateness | p99 ≤ 100 ms, max ≤ 250 ms | One Sphere tick; a swing or spell 250 ms late is felt. |
| Step round trip (client sends a step, gets the ack), LAN | p99 ≤ 100 ms, max ≤ 300 ms | Above that, walking rubber-bands. |
| GC pause | ≤ 2% of wall time, no pause > 50 ms | Pauses stop the game thread like a lock would. |
| Save | automatic every ≤ 30 min, game paused ≤ 1 s per save (≤ 3 s tolerable for now) | Erebor paused for its daily save; a few seconds every half hour is the limit players accept. |
| Boot with the full world | ≤ 2 min | A restart after a crash. |
| Memory | ≤ 4 GB working set | Fits an 8 GB server with room for the OS and a build. |

## 2. How the reference engines do it

### EreborNext (C# port of Sphere 0.99z8)

Analyzed at commit `1a7befa44d20ee2143a1bb58a98c237bdd81de0b` (gitlab.com/erebor/EreborNext, 2026-11-04). Paths are under
`src/EreborNext.Core/`.

EreborNext is a function-by-function port of the 0.99z8 binary. Its README calls it a partial port that can't run a shard
(about 605 `NotImplementedException` sites), so none of this was measured: it shows 0.99z8's design, not a running C# server.
It adds almost no modern machinery: no async I/O, no pools, no GC settings.

- **One thread for everything.** `CServTask` runs the world tick, AI, scripts, network receive/compress/send and saves
  (`SphereSvr/CServTask.cs:35-51`). The loop is paced by `Socket.Select(…, 1 ms)` (`SphereSvr/CServer.RuntimeTick.cs:96`).
  World time moves in 100 ms ticks; sectors pulse every 2 ticks = 200 ms (`SphereSvr/CWorld.Tick.cs:18-57`).
- **Sectors.** 64×64 tiles, one fixed grid of 96×64 = 6144 sectors **shared by all map planes**
  (`SphereCommon/SphereSectorConstants.cs:6-10`). A sector has four intrusive lists: active chars, disconnected chars,
  ground items with a timer, ground items without one (`SphereCommon/CSectorTemplate.Entities.cs:5-8`). Range queries walk the
  overlapping sectors and filter by distance (`SphereSvr/CWorldSearch.cs`); a character search never walks the item lists.
- **Sector sleep.** A sector with no client for 10 minutes sleeps (`SphereSvr/CSector.Tick.cs:19-24`) and then gets one
  pulse in 1024 (about 205 s, staggered by sector index, `SECTORSLEEP=10`). On that pulse ground-item timers (decay, spawns)
  fire, but characters in it essentially never tick. A client entering a sector, or any char being sent to a client, wakes
  the char's sector (`SphereSvr/CClientGame.CharacterPacket.cs:24-26`). Everything stays in memory.
- **Timers are polled.** No queue or wheel: each awake sector walks all its chars and all its timer items every 200 ms
  (`SphereSvr/CSector.Tick.cs:115-194`). Items without a timer are on the inert list and cost nothing.
- **NPC AI** runs only in awake sectors: 0.3 s + up to (150−DEX)/20 s between actions (`SphereSvr/CChar.NpcAction.cs:88-92`).
  The look-around is a radius-18 search, halved and capped at 16 chars in a crowded sector
  (`SphereSvr/CChar.NpcLook.cs:56-71`). Walking is a greedy step with random side-steps, no A*.
- **Networking.** Non-blocking sockets polled with `Select`, one packet per client per pass. Output is a per-client buffer
  capped at 31 KB, and an overflow or a short send drops the client (`SphereSvr/CClient.Transport.cs:85-106`,
  `SphereSvr/CClientGame.Compression.cs:39-44`). Huffman compression runs on the game thread.
- **Saves.** Text `.scp`, written "in the background" one sector per world tick, with a parity bit so moving chars stay
  consistent (`SphereSvr/CWorld.Save.cs:25-101`). With defaults that is about 6148 × 100 ms ≈ 10 minutes per save, each tick
  stalling for one sector's write. Forced and shutdown saves block for the whole world.
- **Scripts.** Each trigger opens the `.scp` file, seeks to the section and interprets the text line by line
  (`SphereLib/CResourceLock.cs:15-41`), a port addition that is slower than native 0.99z8, which cached.
- **Memory.** Every item and character carries its own 33–35-delegate "bindings" test-seam object, about 300 B each
  (`SphereSvr/CItem.cs:132`, `SphereSvr/CChar.cs:25`), about 0.3 GB at 1M items.
- **Limits at our target:** movement updates stop after 64 clients (`SphereSvr/CChar.Update.cs:28`, counted over all
  clients), each player step scans every ground item within radius 31, bursts can drop players at the 31 KB cap, one core
  for everything.

Erebor's own `sphere.ini` (in the archive) ran `SECTORSLEEP=10`, `SAVEBACKGROUND=0` (blocking save) and `SAVEPERIOD=1440`
(one save a day).

### Sphere 0.56b (C++)

Analyzed at tag `56b-20130616` (commit `e2546c5181233357e211b10d8ead09cc7a00e125`, github.com/Sphereserver/Source, the last
0.56b tag; master is 0.56d now, `5ed1466d`, with the same architecture). Paths are under `0.56.b/`.

- **One game thread.** All world logic, scripts, packet handling and saves run on "Main", a yielding busy loop
  (`graysvr/graysvr.cpp:622-665`, `Sphere_OnTick`). Optional network threads only receive, decrypt, compress, encrypt and
  send (`network/network.cpp:2457-2478`); the shipped ini sets `NetworkThreads=0`. No locks on world objects.
- **Ticks.** 10 ticks per second (`TICK_PER_SEC 10`, `graysvr/graysvr.h:36`); every sector of every map is pulsed every
  2 ticks = 200 ms (`SECTOR_TICK_PERIOD`, `graysvr/CWorld.h:14`; `CWorld::OnTick`, `graysvr/CWorld.cpp:2309-2415`).
- **Sectors.** Per map, 64×64 by default (8×8 on map4), in one flat array (`graysvr/graysvr.cpp:154-178`,
  `graysvr/CWorld.cpp:1052-1097`). The same four intrusive lists as 0.99z8, so 1M items without a timer cost no CPU.
- **Sleep.** As 0.99z8: 10 minutes after the last client (`graysvr/CSector.cpp:866-871`, hardcoded), then one full tick per
  1024 pulses, staggered (`graysvr/CSector.cpp:983-994`). On that tick NPC AI, regen, decay and spawns all run, bunched up.
  A sector wakes when a client enters or a char in it is sent to a client (`graysvr/CClientMsg.cpp:944-952`).
- **Timers polled** per awake sector every 200 ms; a 10-bucket wheel for script `TIMERF`s (`graysvr/CWorld.cpp:201-359`).
- **NPC AI** acts every 1 s + rand((150−DEX)/4…(150−DEX)/2) ticks, about 3.5–6 s at DEX 50
  (`graysvr/CCharNPCAct.cpp:3313-3320`). A* is optional (`NPC_AI_PATH`), limited to a 24×24 grid and run only by INT ≥ 75
  NPCs with probability INT/300 (`graysvr/CCharNPCAct.cpp:3333-3380`). Sector sleep is the only throttle.
- **Networking.** Five priority queues per client with per-tick caps (25 packets, 12000 bytes); each packet compressed and
  appended to one byte queue sent with one `send()` (`network/network.cpp:3588-3666, 3729-3818, 3870-3895`). No pooling.
- **Saves.** The default is a stop-the-world text save (`SaveBackground=0`), and the shipped ini also runs a full integrity
  pass over every object first (`ForceGarbageCollect=1`, `graysvr/CWorld.cpp:887-960, 1272-1341`). The background mode
  writes one sector per tick, about 84 minutes for 50k sectors, and doesn't protect items moving between sectors.
- **Scripts.** A per-definition trigger bitmask makes "no handler" a bit test (`common/CResourceBase.cpp:857-974`); script
  files are cached as lines but every executed line is parsed again (`common/CScriptObj.cpp:1887-2048`).
- **Memory.** Plain `new`/`delete`, deferred deletes, a flat UID array with a free-slot cache
  (`graysvr/CWorld.cpp:663-755`); map blocks loaded per 8×8 block and evicted after 120 s idle
  (`common/CSectorTemplate.cpp:120-221`).

**Where 0.56b differs from EreborNext, and what translates to C#:**

| 0.56b technique | Differs from EreborNext? | Translates to C#? |
|---|---|---|
| Sector grid per map, not one grid shared by planes | Yes | Yes. Ours already has a grid per plane. |
| Timer/inert item split; sector sleep with a staggered slow tick | Same | Yes, both are plain logic. Sleep is the single most important idea for us (problem 1). |
| Network I/O threads separate from the game thread | Yes (EreborNext does I/O on the game thread) | Yes; we already have async receive and a send thread. Don't copy its barrier-free queue, which is only correct on x86. |
| Per-client priority queues with byte/packet caps per tick | Yes (EreborNext drops the client at 31 KB) | Yes, cheap; protects crowds. |
| Trigger presence bitmask per definition | Yes (EreborNext scans with LINQ) | Yes, though our compiled C# triggers already avoid most of this cost. |
| Map blocks evicted after idle time | Same | Yes, with memory-mapped files or an LRU (problem 8). |
| Polling every char and timer every 200 ms | Same | Works, but a scheduler (what we have) is better. |
| One-sector-per-tick background save with parity | Same idea, different pacing | No: its consistency is fragile. In C#, snapshot under the lock, write on a worker thread (problem 3). |
| Interpreting script text per call | Same (worse in EreborNext) | No. We compile scripts to C#, which beats both. |
| Plain new/delete, no pools | Same | With a GC the hot paths need the opposite: no per-call allocation (problem 4). |

## 3. Comparison

| Topic | EreborNext (0.99z8 port) | Sphere 0.56b | Ours |
|---|---|---|---|
| Game threading | One thread for all, I/O included | One game thread; optional I/O threads | One `globalLock` taken by the timer thread, packet handlers (I/O callbacks), 2 sync-queue threads and the movement thread: one logical game thread |
| Tick | 100 ms world tick, 200 ms sector pulse | Same | No tick: a timer priority queue, timer thread sleeps 5 ms between rounds |
| Spatial partitioning | 64×64 sectors, one grid for all planes | 64×64 per map, flat array | 16×16 sectors per plane, created on first use |
| Range queries | Walk overlapping sectors, chars and items in separate lists | Same | Walk overlapping sectors through LINQ; **items and characters share one list** |
| Inactive areas / maps | Sleep after 10 min; one pulse per ~205 s; chars don't tick | Sleep after 10 min; one full tick per ~205 s | **No sleep.** An empty map costs nothing, but every NPC on it thinks all the time |
| Timers | Polled per awake sector | Polled per awake sector + TIMERF wheel | One binary heap of absolute times, O(log n) |
| NPC AI rate | 0.3–5.3 s, awake sectors only | ~1–6 s, awake sectors only | 1.5 s (0.3 s minimum), everywhere, always (measured ~1.07 thinks per NPC per second) |
| Pathfinding | Greedy steps | Optional 24×24 A*, INT ≥ 75, probabilistic | A* up to 1000 iterations, cached 8 steps, retried every think when it fails |
| Item decay / spawns | Timer items in sector lists; sleeping sectors every ~205 s | Same | One timer per decaying item or spawn; items in containers and fixtures cost nothing |
| Networking | Select + sync sends, 31 KB cap drops the client | Priority queues, per-tick caps, optional I/O threads | Async receive, one send worker thread, pooled buffers and packets, shared packet groups |
| Save | Text, one sector per tick (~10 min), forced save blocks | Text, stop-the-world by default | Text, **stop-the-world**; hourly autosave (`PeriodicSave.scp`); **no save on shutdown** |
| Load | Text parse | Text parse | Text parse, 12 s for 709k items + 50k NPCs |
| Scripts | Text interpreted per call, file opened per trigger | Text lines cached, parsed per execution | Compiled C# at boot; LScript only for `.scp`/`.def` bits |
| Memory / GC | No pools, ~7 objects per item, default GC | Plain new/delete | Pools for packets and connections; default workstation GC; hot-path LINQ allocates |
| Build used in practice | n/a | Release | **Debug** (Release doesn't compile) |

## 4. Our engine: main loop and threads

There is no main loop. `MainClass.SteamMain` (`GameCore/Main.cs:98`) initializes and then waits for the exit token. The
game runs on these threads, every one of which takes `MainClass.globalLock` (`GameCore/Main.cs:47`) around its work, so
only one runs game code at a time:

| Thread | What it does | Where |
|---|---|---|
| Timer thread | Loops: apply timer changes, run every due timer (each under `globalLock`), sleep 5 ms | `Timer.TimerThread` / `ProcessTimingOut`, `GameCore/Timers/Timer.cs:51, 118` |
| I/O callbacks (thread pool) | Receive, decode, run the packet handler under `globalLock` | `AsyncCore.HandlePacket`, `Communication/AsyncCore.cs:99`; `TcpConnection.OnReceieve` |
| Send worker | Compresses and sends queued packet groups; no lock | `AsyncCore.WorkerThreadMethod` |
| `CharSyncQueue`, `ItemSyncQueue` | Batched view updates (who sees whom), under `globalLock` | `SyncQueue.Cycle`, `GameCore/Networking/SyncQueue.cs:38` |
| Movement thread | Paces queued player steps, under `globalLock` | `MovementState.Cycle`, `GameCore/Networking/MovementState.cs:79` |

What scales with what:
- **NPC AI**: one `NPCAiTimer` per NPC (`scripts/Basic/AI/NPCAiTimer.cs:186`, `RescheduleForMoveRate`), period 1.5 s ×
  3 / MoveRate, never paused except for dead, disconnected, frozen or held NPCs. **Scales with all NPCs in the world.**
- **Ground items** sit in their sector's list and cost nothing per tick. Decaying ones carry one timer each (#236), and
  items in containers or banks cost nothing. **Scales with nothing per tick**, only with memory and save time.
- **Sectors** are created on first touch and have no per-tick work. An unvisited map costs its sector array
  (384×256 references, 0.8 MB per plane) and nothing else.
- **Players' steps** scan things within 18 tiles (`CharSyncQueue`, `GameCore/Networking/CharSyncQueue.cs:678`) and send to
  players within 19. **Scales with local density × players.**
- **Saves** walk everything. **Scale with all things.**

## 5. Measurements

### How

A harness written for this study, on a local branch at the time; it's on `main` since #1179 (see
[9](#9-benchmark-and-stress-test-setup)):
- **`PerfCounters`** times every holder of `globalLock`: each timer by its type, each packet handler by packet type, the
  sync queues and the movement thread, plus how long each waited for the lock.
- **`.TestPerf`** is a server command that populates the world, starts and reports measurement windows, saves, and
  benchmarks range queries.
- **Two probes**: a 100 ms timer that records how late it fires, and a thread that takes `globalLock` every 20 ms and
  records how long that took.
- **50 simulated clients** (`PerfProbeTests`): real logins through the login server, each running a step every 220 ms
  and timing the ack.
- **A sampling profile** of the game thread under load, taken with `dotnet-trace`.

Runs were in the dev container: 8 cores shared with other work, 5 GB of memory. Release build (with the `#error` in
`AuxiliaryServer/Commands.cs:74` stubbed out locally) unless it says Debug. Absolute numbers will differ on the real server;
the shape won't.

### Results, Release build, before any change

| Phase | Game thread busy | NPC AI | CPU | Timer lateness p50 / p99 / max | Step round trip p50 / p99 / max | Alloc | GC pause |
|---|---|---|---|---|---|---|---|
| P0 empty world | 0.1% | – | 2% | 0 / 6 / 18 ms | – | 0.1 MB/s | 0.04% |
| P1 15k dungeon NPCs, 212k items, no players | 27% | 27% | 34% | −1 / 16 / 76 ms | – | 87 MB/s | 1.7% |
| P2 **design world**, 30k NPCs, 626k items, no players | **72%** | 70% | 91% | −89 / **911** / 2303 ms | – | 218 MB/s | 7.0% |
| P3 + 50 players crowded at one spot in town | **82%** | 77% | 116% | 1108 / **4374** / 4374 ms | 42 / **231** / 424 ms | 220 MB/s | 10.5% |
| P4 50 players spread in the largest dungeon | 62% | 60% | 80% | −84 / 102 / 864 ms | 0.6 / 51 / 868 ms | 248 MB/s | 9.3% |
| P5 stress, 50k NPCs, 709k items | **86%** | 84% | 105% | 1996 / **5023** / 5023 ms | (walkers lost sync in the 5 s save before it) | 251 MB/s | 11.9% |

Negative lateness p50s are the probe timer firing early after a late one; read the p99s. In P3 and P5 the 100 ms probe ran
41 and 25 times in 60 s instead of 600: the timer thread was doing whole rounds of every due timer (problem 6).

**Per NPC think:** 22–39 µs in Release (more where NPCs fight or plan paths), **83–119 µs in Debug** at the same 10% world
(Release 41–46 µs there). Thinks ran at 32k per second for 30k NPCs.

**Saves** (blocking, measured as the time `Globals.Save` holds the server): **2.6 s and 5.0 s** at 626k items / 30k NPCs
(94 MB of text), 2.6 s at 709k items / 50k NPCs (123 MB). *Extrapolated* to 1M items: about 4–8 s and ~150–200 MB.

**Boot** on the 709k-item / 50k-NPC save: 48 s to ready, of which the script compile is 25 s, script loading 6 s and world
load 12.3 s.

**Memory:** working set 0.37 GB empty, 0.92 GB at the design world, 1.04 GB at 50k NPCs; GC heap 0.6–0.8 GB. *Extrapolated*
to 1M items: about 1.1 GB (300k stored items added 54 MB of heap, ~180 B per plain item).

**Range queries** (`.TestPerf bench`, 2000 calls each): at an empty spot 0.6–2 µs and 1–1.4 KB allocated per call; inside
the densest dungeon spot (141 things in range) 8–10 µs and **9.5 KB allocated per call**.

### Where the game thread's time goes (profile, P5 stress, 25 s)

| Share of the timer thread | What |
|---|---|
| 57% | `NPCAiTimer.Think` |
| ↳ 30% | A* (`NPCAiTimer.PlanPath` → `AStar.FindPath`), nearly all in `Map.CheckMovement` and the static-tile iterator `GetStaticsAndMultiComponentsOnCoords` |
| ↳ 16% | `NPCAiTimer.FindTarget`: the LINQ `SelectMany` over sectors (9.5%) and type checks of items mixed into the char query (7.6%) |
| 19% | `EventWaitHandle.Set`: the timer thread signals its own wake-up event on every periodic re-arm (a futex syscall each) |
| 6% | Timer heap work (`Dequeue`, `TrickleDown`, `ProcessChanges`) |
| 3% | Loading map and statics sectors from disk, a `FileStream` opened per sector |
| 1% | `DateTime.Now` in `Globals.TimeAsSpan` |

### Results with three prototype fixes (experiments, not proposed as written)

Second local commit on the harness branch: (a) an NPC with no target, owner, follow target or goal point skips its think
when no player is within 40 tiles (a crude form of sector sleep; its timer still fires), (b) the timer thread no longer
signals itself, (c) the engine's existing non-LINQ range-query code (`NOLINQ`, never defined until now).

| Phase | Game thread busy | CPU | Timer lateness p99 / max | Step round trip p99 / max | Alloc | GC pause |
|---|---|---|---|---|---|---|
| P2 design world, no players | 72% → **12%** | 91% → 30% | 911 → **37** / 2303 → 120 ms | – | 218 → 0.4 MB/s | 7.0% → 0.2% |
| P3 50 players crowded | 82% → **12%** | 116% → 42% | 4374 → **175** / 4374 → 426 ms | 231 → 152 / 424 → 375 ms | 220 → 2.8 MB/s | 10.5% → 0.8% |
| P4 50 players in a dungeon | 62% → **13%** | 80% → 40% | 102 → 94 / 864 → 210 ms | 51 → 50 / 868 → 448 ms | 248 → 4.1 MB/s | 9.3% → 0.5% |

The range query in the dense dungeon went from 8.3 µs / 9.5 KB to 4.2 µs / 480 B. What's left in P2 (3.7 µs × 32k firings
per second ≈ 12%) is the sleeping NPCs' timers still firing just to check for players: pausing them removes most of that
(problem 1's real design). The crowd's step p50 stayed at 41 ms with the game thread mostly idle, so it is movement pacing
or collisions between 50 walkers on one spot (78% of their steps were refused), not load; see problem 10.

## 6. Problems, ranked, with proposals

Severity: **Critical** misses the targets at the design load on its own; **High** misses them at peak or puts data at risk;
**Medium** costs real headroom; **Low** is worth doing when nearby.

### 0. Saves made mid-fight or mid-effect don't load cleanly (#1163) — High (correctness, first)

- **Where:** 11 plugin and role defs are created in a static field initializer and nothing touches them before
  `WorldSaver.Load`. A .NET static initializer only runs when its field is first read; the generated `Bootstrap()` that
  `ClassManager.InitClasses` calls doesn't read it.

  | Missing when a save loads | Defined in |
  |---|---|
  | `p_weaponSkillTargetQueue`, `p_weaponSkillTargetTracker` | `skills/Combat/WeaponSkillTarget*Plugin.cs` |
  | `p_paralyze` | `skills/Magery/Spells/Paralyze/ParalyzeEffectPlugin.cs` |
  | `p_discordanceEffect_` | `skills/Discordance/Discordance.cs` |
  | `p_warcryEffect`, `p_magic_charge`, `p_ignitionEffect` | `AbilitiesAndTalents/…` |
  | `p_poisoned_item`, `p_craftingProcess`, `p_snoopedBackpacks` | `skills/Poisoning`, `skills/Crafting`, `skills/Stealing/Snooping.cs` |
  | `r_party` | `Roles/Party/PartyDef.cs` |

  Already fixed one by one, by earlier `Bootstrap()`s: stat mods and curse/bless, magic reflection (#927), tracking (#444),
  realm, regeneration, race, guild. Two of the missing, `p_snoopedBackpacks` (`skills/Stealing/Snooping.cs:151`) and
  `p_discordanceEffect_` (`skills/Discordance/Discordance.cs:156`), are built without `.Register()` at all, so they are never
  registered, not just late.

  Measured: a probe at the start of `WorldSaver.Load` asked for each of 24 code-registered defs by name.
- **What happens to a missing one:** `Plugin.LoadingFinished` (`GameCore/Plugin.cs:132`) logs `ERROR … No PluginDef
  reference loaded … Deleting.` and drops the plugin:
  - a fight, a paralysis, a poisoned weapon or a craft in progress is lost;
  - **discordance lowers Str/Dex/Int and max hits/mana/stamina when it starts and puts them back only when it ends**
    (`skills/Discordance/Discordance.cs:181-212`), so a dropped one may leave the stats lowered for good. To be proven by
    the test in plan step A1.
- **Second bug:** `SkillSequenceArgs.LoadLine` (`scripts/Basic/skills/SkillDef.cs:452`) resolves the field *name*
  (`OptimizedLoad_Script(valueName)`) instead of its value. Every in-progress skill (swing, cast, craft) therefore loads
  with no skill, logs `We really do not know what could the loaded string 'skilldef' refer to`, and its stroke quietly
  does nothing after the restart. No other `[LoadLine]` has the same mistake.
- **Evidence:** the 709k-item save from the stress run booted with 27 + 27 such lines (#1163); the probe above.
- **Proposal:** plan steps A1 (every code-registered def exists before the world loads, enforced in one place) and A2
  (the `skilldef` fix). **Effort:** S–M. **Risk:** L–M.

### 1. Every NPC thinks everywhere, all the time — Critical

- **Where:** `NPCAiTimer.OnTimeout` (`scripts/Basic/AI/NPCAiTimer.cs:256`), scheduled by `RescheduleForMoveRate` (`:186`):
  a periodic 1.5 s timer per NPC that never pauses for lack of players. Each think runs `MonsterSpecialAttacks.OnAiTick`
  (abilities' own range queries), `FindTarget` (`:533`, a char query of AggroRange 14), and `Wander` (`:957`), which walks
  home through A* past the leash.
- **Why it's a problem:** it is the whole of the idle load: 27% of the game thread at 15k NPCs, 70% at 30k, 84% at 50k,
  with no player anywhere near. It also drives most of the allocation and GC (P2: 218 MB/s with no players).
- **How it scales:** with **total NPCs in the world**, on every map, visited or not.
- **Evidence:** measured (tables in 5), profiled, prototype measured.
- **Proposal:** sector sleep, adapted to a scheduler. Keep an "awake" flag per sector (or per 64×64 block of sectors): awake
  while a player is within 2 sectors of it, or for N minutes after the last one left (Sphere: 10 min, start with 2–5).
  An NPC in a sleeping area **pauses its AI timer** (`DueInSeconds = -1`, not an early return) and is woken when its area
  wakes. Waking happens when a player enters the area, which the sector code already sees when a player moves between
  sectors (`Map.ChangedP`), and when anything targets the NPC. Sleeping NPCs get one slow catch-up tick (Sphere: about every
  3 minutes) for regeneration and walking home. Exempt from sleep: anything with a target, a ControlMaster, a FollowTarget
  or a goal point; guards in pursuit (`GuardPursuit`); boss-event NPCs with their own timers; anything flagged by a script.
- **Expected impact:** the prototype (early return, timer still firing) already cut the design world from 72% to 12% and
  allocation from 218 to 0.4 MB/s. Pausing the timers removes most of the remaining 12%. With 50 players the AI cost becomes
  "NPCs near players", which at Sphere-like density is a few thousand NPCs at most.
- **Effort:** M. **Risk:** M, because it changes behaviour that's easy to miss: NPCs don't wander or regenerate unseen,
  monster abilities running from `OnAiTick` (dragon regeneration, backstab hiding, mass leap) pause too, and anything
  scheduled by "the next think" waits for wake-up. List what runs from the think before building it, and give the protocol
  tests that wait for NPCs a player in range (they all have one already).

### 2. A Release build doesn't compile, so everything runs Debug — Critical (and cheap)

**Status: fixed by B2 (#1185).** Release builds; `scripts/run-local.sh start --release` runs it; `test-fast.sh` and CI
compile Release on every run. The `#error` guarded the console's `restart` command, which never did anything (it built a
restarter and never started it); `restart` now says how to restart instead. The fast tier and a protocol sample passed on
Release.

- **Where:** `AuxiliaryServer/Commands.cs:74` has `#error Optimized_AuxiliaryServer.bat not defined (?)` in its non-DEBUG
  branch, so `build.ps1 -Configuration Release` fails. `scripts/run-local.sh` and the tests run Debug, and GameCore compiles
  the scripts in its own configuration (`CompilerInvoker`, `Build.Type`), so the scripts are Debug too.
- **Why:** in Debug every `Logger.WriteDebug` call is live: string concatenation in hot paths (`NPCAiTimer.TryStep` logs
  every refused step, `StepTowardsSmart` every failed path), each written synchronously to the console **and** the log file
  with a flush per line (`Logger.WriteLine` and `Rotate`, `Common/Logging/Logger.cs:704, 679`), under `globalLock`. Code is
  also unoptimized.
- **How it scales:** multiplies everything, 2–3× on NPC thinks.
- **Evidence:** measured: 83–119 µs per think in Debug against 41–46 µs in Release on the same world; game thread 17.5% vs
  10.3%.
- **Proposal:** make the `#error` branch build (as the harness did), build and run Release on the real server, and add a
  Release build to the checks so it can't break again unnoticed. Keep Debug for development.
- **Expected impact:** 2–3× less CPU per NPC think; no log I/O from debug lines. **Effort:** S. **Risk:** L. Run the fast
  tier and a protocol sample on Release once: nothing should depend on `#if DEBUG`, but that's unverified.

### 3. Saves stop the world, and nothing saves on shutdown — High

- **Where:** `Globals.Save` (`GameCore/Globals.cs:749`) pauses server time and calls `WorldSaver.Save`
  (`GameCore/Persistence/WorldSaver.cs:31`), which writes every object as text on the game thread.
  - **The autosave:** `scripts/Basic/PeriodicSave.scp`, trigger group `e_periodicsave_global`, attached to `Globals` like
    every `_global` group (`GameCore/TriggerGroup.cs:53`). It arms a timer at `@startup` and again after every save, every
    `periodicSave_Interval` = 60×60 s, a constant in that file. Verified in a local log: a boot at 15:44:30 on 10-15, a
    save at 16:44:45. *The first pass of this review missed it and said there was none.*
  - **Other callers:** the GM `.save` command and the save before a script recompile.
  - **Shutdown:** `.exit` (`Globals.Exit`, `GameCore/Globals.cs:741`) and the console both go straight to
    `MainClass.CommandExit` and don't save.
- **Why it's a problem:**
  - **Every save freezes every player.** At the design world that is 2.6–5 s, once an hour plus every manual save.
  - **A restart loses work.** Even a deliberate one drops up to an hour of play; Sphere saves before it shuts down.
  - **The interval is buried** in a script constant, not on `.settings`.
- **How it scales:** the freeze grows with **all things in the world**.
- **Evidence:** measured 2.6–5.0 s at 626k items and 30k NPCs (94 MB); *extrapolated* 4–8 s at 1M items. The 50 walkers
  lost sync waiting 5 s on a save.
- **Proposal:** plan step A3 (save on shutdown, interval on `.settings`), then D1–D2:
  - **D1:** measure how the save splits between formatting and disk writing.
  - **D2:** snapshot then write. Format everything into memory under the lock, then write and rename the files on a worker
    thread, as ModernUO does.

  Don't copy Sphere's one-sector-per-tick background save: 0.56b's own notes show it loses items moving between sectors.
  **Effort:** S (A3), M–L (D2). **Risk:** L, M.

### 4. Range queries allocate, and character queries walk items — High

- **Where:** `Map.GetCharsInRectangle`, `GetThingsInRectangle`, `GetPlayersInRectangle` and friends
  (`GameCore/Regions/Map.cs:719-950`) use LINQ `from … from … select` because `NOLINQ` is never defined; the plain-loop
  versions sit unused in `#else` branches. `Sector.Things` (`GameCore/Regions/Sector.cs:28`) holds items and characters
  together, so `GetCharsInRange` walks every ground item and type-checks it.
- **Why:** these run in every NPC think (`FindTarget`, abilities), every player step, every speech and every view update. In
  a dense dungeon one query allocates 9.5 KB.
- **How it scales:** with **local density × query rate**. Today the query rate scales with total NPCs (problem 1); after
  sleep it scales with players and the NPCs near them.
- **Evidence:** benchmarked (8.3 µs / 9.5 KB → 4.2 µs / 480 B with `NOLINQ`); profile: `FindTarget` 16% of the timer thread
  at P5, 9.5% in `SelectMany`; allocation 218–251 MB/s and GC pauses 7–12% before, 0.4–4 MB/s and under 1% with the
  prototypes.
- **Proposal:** define `NOLINQ` (or delete the LINQ branches) now. Then split `Sector.Things` into characters and items,
  so a character query never sees items. Then replace the `yield` enumerators with a struct enumerator or a callback that
  allocates nothing.
- **Expected impact:** ~2× faster queries and almost no allocation from them. **Effort:** S (define), M (split lists and
  struct enumerators). **Risk:** L–M (a list split touches `Sector.Add/Remove` and every mover).

### 5. The timer thread signals itself on every re-arm — Medium-High (cheap)

**Status: fixed by B3 (#1187).** `Common/WakeSignal` sets the timer thread's event only while that thread sleeps (nothing
scheduled at all); the thread's own `Set` calls are gone. It also fixed a lost wakeup in the old sleep (check for changes, then
`Reset`, which wiped a `Set` made between the two), and a script recompile's reload no longer takes the loaded timers into the
heap from the loading thread while the timer thread runs (it left them in the change queue for that thread instead).

- **Where:** `manualResetEvent.Set()` in `Timer.ProcessChanges`, `ProcessTimingOut` (`GameCore/Timers/Timer.cs:106, 139`)
  and `PrivateSetFireAt` / `Delete` (`:254, 265`). Every periodic timer re-arm, and every timer an NPC reschedules from
  inside its own think, signals the event the timer thread waits on, although that thread is already awake.
- **Why:** on Linux each `Set` is a syscall.
- **How it scales:** with **timer firings**: today, every NPC think.
- **Evidence:** profile, 19% of the timer thread at P5. Part of the prototype that was measured.
- **Proposal:** don't `Set` from the timer thread itself (the prototype's change), or only `Set` when the event is reset.
  **Effort:** S. **Risk:** L.

### 6. An overloaded timer thread starves new and re-armed timers — Medium

- **Where:** `Timer.ProcessTimingOut` (`GameCore/Timers/Timer.cs:118`) keeps running due timers until none is due, and only
  then does `ProcessChanges` put re-armed and newly scheduled timers back into the heap.
- **Why:** when more timers come due than the thread can run, the loop never returns. Every periodic timer then waits for
  the whole backlog to drain, and so does every new one (a skill stroke, a spell, a decay). Overload turns into
  multi-second rounds instead of a gradual slowdown.
- **How it scales:** it only appears under overload, but then everything feels frozen.
- **Evidence:** measured: in P3 and P5 the 100 ms probe ran 41 and 25 times a minute instead of 600, and the heap's count
  fell from ~31k to 8.7k while the rest waited in the changes queue.
- **Proposal:** bound each round by time (for example 20 ms) or by count, then run `ProcessChanges` before continuing.
  **Effort:** S. **Risk:** L–M (timers re-armed into the past then run in order with the rest, which is the intent).

### 7. Walking home and failed A* searches — Medium-High

- **Where:** `NPCAiTimer.Wander` (`scripts/Basic/AI/NPCAiTimer.cs:957`):
  - within `WanderLeash` (6 tiles) of home, an NPC steps randomly;
  - at 6 or more it walks home with `StepTowardsSmart`, which is A* (`PlanPath`, `:923`) up to
    `AStar.DefaultMaxIterations = 1000` (`scripts/Basic/AI/PathFinding/AStar.cs:36`).

  A path is cached for 8 steps, but the random steps in between make it stale, so an NPC at the leash edge plans again
  almost every time. A search that fails caches nothing and runs again on the next think. Every iteration calls
  `Map.CheckMovement` (8 neighbours) and the character-blocking check.
- **The user's idea, measured:** walking home takes a straight step toward home first and uses A* only when that step is
  refused. That is also what Sphere does: NPCs walk greedily with side-steps and never use A*. Design world with no players,
  today's code, back-to-back runs, one each:

  | | A* searches walking home (30 s) | A* iterations | Failed | Game thread busy | GC pause | Timer lateness p99 |
  |---|---|---|---|---|---|---|
  | Today | 1,475 | 111,153 | 182 | 57.7% | 1.6 s | 607 ms |
  | Straight step first | 15 | 1,611 | 2 | **44.4%** | 0.9 s | 290 ms |

  This run's baseline busy% (57.7%) is lower than the first pass's 72% for the same world: the machine was quieter. Only
  same-session pairs are compared.
- **Why:** unreachable homes and leash-edge drifting are common (spawns by walls, NPCs knocked or lured away).
- **How it scales:** with **NPCs walking home**: all of them today, those near players after sleep (problem 1).
- **Evidence:** the table above. The P5 profile also showed A* at 30% of the timer thread, partly from fights; random
  placement makes failures likelier than real spawns would.
- **Proposal:** plan step C1. Straight step first when walking home (the user's idea). Back off for 5–10 s after a failed
  search, for every caller, not just walking home. **Effort:** S. **Risk:** L–M (NPCs near walls move a little
  differently, closer to Sphere).

### 8. Map and statics sectors: a file opened per sector, never evicted — Low-Medium

- **Where:** `StaticSector.GetStaticSector` (`GameCore/Regions/StaticSector.cs:50`) opens a new `FileStream` on the `.mul`
  files for every 16×16 sector it loads (`:108, 146-147`), on the game thread, and keeps every loaded sector forever.
  Planes on the same facet share the cache.
- **Why:** each first visit to an area costs file opens and reads under `globalLock`; memory grows with explored area.
- **How it scales:** with **explored area of facets 0 and 2**, not with planes.
- **Evidence:** profile 3% at P5 (NPCs wandering into new sectors); populating 50k overworld items took 12–15 s, mostly
  sector loads (about 250 µs each). Memory: *estimate* 200–400 MB if all of facet 0 is touched; not measured.
- **Proposal:** keep the files open, or memory-map them (`MemoryMappedFile`) and read sectors from the view. Eviction can
  wait until memory says so. **Effort:** S–M. **Risk:** L.

### 9. Default workstation GC, no tuning — Medium (mostly a symptom)

- **Where:** `SteamEngine.GameCore.runtimeconfig.json` sets nothing, so it's workstation concurrent GC.
- **Why:** at 218–251 MB/s of allocation GC pauses took 7–12% of wall time. Most of that is problems 1 and 4 and goes with
  them (under 1% in the prototype runs).
- **Proposal:** after problems 1 and 4, measure again. Then consider `<ServerGarbageCollector>` on a multi-core server,
  `GCgen0size`, or `<TieredPGO>` (on by default in .NET 8). **Effort:** S. **Risk:** L.

### 10. Crowds: steps in a 50-player crowd — Medium, re-measure after 1–2

- **Where:** a player's step (`MoveRequestInPacket` → `MovementState` → `CharSyncQueue`,
  `GameCore/Networking/CharSyncQueue.cs:678`) scans every thing within 18 tiles and calls `CanSeeForUpdate` twice for each,
  then updates every player within 19.
- **Evidence:** 145 µs per step in the crowd (against 30–70 µs elsewhere). Step round trip p99 231 ms before the prototypes
  and 152 ms after, with the game thread 12% busy, so the rest isn't game-thread load. 78% of the walkers' steps were
  refused (they walked into each other), and the p50 of 41 ms didn't move. Sphere paces steps the same way.
- **Proposal:** re-measure on Release with problems 1 and 5 fixed, using walkers that don't collide (a test artifact).
  If steps are still slow, look at `MovementState`'s pacing reserve and at sending a step's view changes from a cached set of
  what the client already sees, instead of rescanning. **Effort:** S to measure. **Risk:** –.

### 11. `Globals.TimeAsSpan` reads `DateTime.Now` — Low

- **Where:** `GameCore/Globals.cs:942`. Every timer operation and many game checks read the time through it.
- **Why:** `DateTime.Now` converts to local time on each call (1% of the timer thread). It is also wall-clock time: an NTP
  step or a DST change jumps every timer.
- **Proposal:** `Stopwatch.GetTimestamp` (monotonic) for the elapsed part. **Effort:** S. **Risk:** L.

### Found on the way (not performance)

- **#1163:** a world saved while NPCs fight loads with ERRORs. Now problem 0, with the full list and a second bug.
- **Problem 2's `#error`** is also a plain build bug.

## 7. Checked and fine

- **Timer priority queue** (binary heap, `GameCore/Timers/TimerPriorityQueue.cs`): O(log n), 6% at 50k timers; scheduling
  beats Sphere's polling.
- **Sector grid** (16×16, per plane, created on first touch): queries at an empty spot take 0.6–2 µs; 0.8 MB of array per
  plane.
- **Unvisited maps and empty dungeons:** cost no CPU by themselves (no per-sector tick); only the NPCs on them do
  (problem 1).
- **Item decay** (one timer per decaying ground item, #236): fixtures, decoration and contained items cost nothing.
- **Items in containers, banks and houses:** no per-tick cost; 300k of them added 54 MB of heap and save time only.
- **Networking:** async receive, one send thread, pooled buffers and packets, packet groups shared across recipients;
  container contents in one packet (`PacketSequences.cs:128`). Packet handlers took 30–150 µs.
- **Speech:** one LOS check and trigger per listener in range, bounded by how often players talk.
- **Full-world scans** (`Thing.AllThings` in `.SetupWorld`, `House.ByRegion` once, GM commands): 656k things in 4–20 ms,
  only on rare events.
- **Boot and load:** 48 s for 709k items and 50k NPCs, 12 s of it the world.
- **Memory:** 0.92 GB at the design world, about 1.1 GB extrapolated to 1M items, well within 4 GB.
- **Scripts:** compiled C# at boot, so no text interpretation per call, unlike both references.
- **Sector size (16×16):** measured against 32×32 and 64×64 in section 12. Bigger sectors make every range query in a busy
  area slower, so keep 16. Sleep (problem 1) should use a coarser area of its own.
- **One global lock:** contention isn't the problem, the amount of work is: with the prototypes the lock-wait probe's p99
  was 1–11 ms. Keep the single-threaded game model; both reference engines do the same.

## 8. Implementation plan

**Status: proposed, waiting for the go-ahead. Nothing here is implemented.**

**How each step runs:**
- one issue, one branch, one PR, as usual;
- every change has a test that is red without it;
- merges are gated on the test results;
- every performance step reruns the harness at the same scale and phases, before and after, and the PR quotes both;
- docs are updated in the same PR.

Effort: S ≤ 1 day, M a few days, L a week or more.

**Phases:**
- **A**, correctness of saves (#1163 first, as asked).
- **B**, measuring plus cheap wins.
- **C**, NPC AI.
- **D**, saves without the freeze.
- **E**, the rest.

A and B don't depend on each other; within C and D the order matters.

### Phase A: saves that load cleanly (#1163)

**A1. Every code-registered def exists before the world loads.**
- **Work:**
  - *Force them:* right after `ScriptLoader.Load` and before `WorldSaver.Load` (both in `Main.Init` and in `LoadAll`, the
    recompile path), run the static initializer (`RuntimeHelpers.RunClassConstructor`) of every scripts type deriving from
    `Plugin`, `PluginDef`, `Role` or `RoleDef` that declares a static field of an `AbstractDef` type. That covers all 11
    missing defs.
  - *Why that point:* it comes after the scripts load, so an initializer that looks another def up by name still finds it.
    It is limited to plugin and role types, so unrelated static initializers keep running on first use as today.
  - *Register the unregistered:* `Snooping.cs:151` and `Discordance.cs:156` build their def without `.Register()`, so those
    two are never registered at all, not just late. They get `.Register()`.
  - *Check every boot:* afterwards, every such static field must hold a def registered under its own name. One that isn't
    logs an ERROR, which BootLogTests already fails on, so the next forgotten def is caught at once.
  - *Kept, harmless:* the hand-written fixes of #444, #927 and `GuildDef`.
  - *Rejected:* a `Bootstrap()` per class, which is what #444 and #927 did. It was missed 11 times; it will be missed again.
- **Benefit:** after a restart a fight, paralysis, discordance, warcry, magic charge, ignition, poisoned weapon, craft,
  snoop or party comes back as it was saved, instead of logging an ERROR and disappearing. Discordance can't leave lowered
  stats behind.
- **Test:** BootLogTests' saved-world boot, extended.
  - *Setup:* before the save, give characters each of the 11 states: an NPC mid-swing (combat queue and tracker),
    paralysis, discordance, warcry effect, magic charge, ignition, a poisoned weapon, a craft in progress, a snooped pack,
    a party.
  - *Then:* save, boot on the save.
  - *Assert:* no ERROR or WARNING. Each plugin and the party are back with their defs. The discorded character's stats
    return exactly to their pre-discord values when the effect ends.
  - *Red check:* red without A1, since 11 defs were measured missing. It also answers whether today's code leaves
    discordance's lowered stats behind.
- **Effort:** S–M. **Risk:** L–M. A few static initializers run earlier than before, still after every script has
  loaded; the boot tests show any that can't.

**A2. In-progress skills load with their skill.**
- **Work:** `scripts/Basic/skills/SkillDef.cs:452`, `valueName` → `valueString`.
- **Behaviour change:** a skill saved mid-way finishes after the restart (the stroke lands, the craft completes) instead
  of doing nothing, as Sphere, which saves the action and its timer.
- **Benefit:** no `'skilldef'` warnings; a restart doesn't silently eat a craft, a swing or a cast.
- **Test:** save during a craft and during a swing, boot; the item is made, the swing lands, no warning. Red without the
  fix.
- **Effort:** S. **Risk:** L.

**A3. Save on shutdown; the autosave interval on `.settings`.**
- **Work:**
  - `.exit` and the console's exit save first, with the usual "Server is pausing for worldsave" broadcast. If the save fails, it says so and exits anyway.
    *Alternative:* refuse to exit on a failed save. That's your call; I lean towards "say so and exit".
  - `periodicSave_Interval` moves from the `PeriodicSave.scp` constant to a `.settings` value. The default stays 60
    minutes until D2 makes saves cheap.
  - A GM command shows when the next save is due (`periodicSaveInformation` already exists in the script).
- **Benefit:** a deliberate restart loses nothing, and admins can tune the interval without editing scripts.
- **Test:** protocol test: change something, `.exit`, boot again, the change is there. A setting test for the interval.
- **Effort:** S. **Risk:** L.

### Phase B: measure and cheap wins

**B1. Land the benchmark harness** (section 9).
- **Work:**
  - `PerfCounters` behind a setting, off by default, plus a GM command printing the top `globalLock` holders.
  - `.TestPerf`, `PerfProbeTests` and `PerfLoadTests` as opt-in test classes, outside the default tiers.
  - `tools/perf` (dungeon rectangles, the STM benchmark).
- **Benefit:** every later step gets real before/after numbers, and the live shard gets a "why is it slow right now"
  answer.
- **Test:** the harness's own smoke run at scale 0.1.
- **Effort:** S. **Risk:** L.

**B2. Release builds work and are what the server runs.**
- **Work:**
  - make the `AuxiliaryServer/Commands.cs:74` `#error` branch build;
  - add `--release` to `run-local.sh`;
  - a Release compile in `test-fast.sh`, so it can't break unnoticed again;
  - run the fast tier and a protocol sample on Release once.
- **Benefit:** 2–3× less CPU per NPC think, and no debug logging written synchronously under the lock (problem 2).
- **Effort:** S. **Risk:** L.

**B3. The timer thread stops signalling itself** (problem 5).
- **Work:** no `Set()` from the timer thread itself.
- **Benefit:** about 19% of the game thread at load.
- **Test:** the timer tests (TimerBurstTests among them) and a protocol sample.
- **Effort:** S. **Risk:** L.

**B4. Range queries without LINQ** (problem 4, first part).
- **Work:** define `NOLINQ`, or delete the LINQ branches.
- **Benefit:** queries about 2× faster, allocation per query 9.5 KB → 0.5 KB, most of the GC pressure gone.
- **Effort:** S. **Risk:** L.

**B5. A bounded timer round** (problem 6).
- **Work:** `ProcessTimingOut` stops after about 20 ms and lets `ProcessChanges` run.
- **Benefit:** overload slows the world down gradually instead of freezing it in whole rounds.
- **Test:** a timer test that floods due timers and checks a new timer still fires on time.
- **Effort:** S. **Risk:** L–M.

**B6. A monotonic game clock** (problem 11).
- **Work:** `Stopwatch` instead of `DateTime.Now` in `Globals.TimeAsSpan`.
- **Benefit:** a little CPU, and immune to clock jumps (NTP, DST).
- **Effort:** S. **Risk:** L.

### Phase C: NPC AI

**C1. Walking home: straight step first; back off after failed searches** (problem 7, the user's idea).
- **Work:**
  - `Wander` past the leash takes `StepTowards(home)`, and `StepTowardsSmart` only if that step is refused.
  - After any failed A* search, `NpcPathCache` remembers it and doesn't search again for 5–10 s (a setting); the NPC steps
    directly or stands.
- **Benefit:** measured 1,475 → 15 searches walking home per 30 s, game thread 57.7% → 44.4% on the idle design world.
  Movement gets closer to Sphere's.
- **Test:**
  - an NPC past its leash on open ground walks home without searching;
  - one with a wall in the way gets round it;
  - one with an unreachable home doesn't search more than once per back-off;
  - existing movement and A* tests.
- **Effort:** S. **Risk:** L–M.

**C2. NPCs sleep where no player is near** (problem 1).
- **Work, (a) engine:** an activity grid per map plane in areas of 64×64 tiles (4×4 sectors, Sphere's size).
  - An area is awake while a player is in it or in a neighbouring area, and for a while after the last one leaves. Start
    at 2–5 minutes, a setting; Sphere uses 10.
  - Waking and sleeping raise events. The movement code already sees players change sectors (`Map.ChangedP`).
- **Work, (b) NPCAiTimer:**
  - A sleeping NPC's timer is paused (`DueInSeconds = -1`), so there's no early return and no cost while it sleeps.
  - Its area waking resumes it. A slow catch-up tick (about every 3 minutes, a setting) handles regeneration and walking
    home.
  - Never asleep: anything with a target, an owner, a follow target or a go-to point; guards in pursuit; boss events with
    their own timers; anything a script marks.
- **Work, (c) audit:** everything that runs from a think or `OnAiTick`: dragon regeneration, backstab hiding, mass leap,
  dispel bombs, NPC see-new-player, elite escape, guard pursuit, proactive bosses. Each one is listed in the PR with what it
  does while its NPC sleeps.
- **Work, (d) tests:**
  - an NPC far from players doesn't think, and wakes when a player arrives;
  - pets and followers, guards, bosses keep working;
  - the protocol tests that wait for NPCs all have a player in range already, so run the broad protocol set.
- **Benefit:** the prototype (early return, timer still firing) took the idle design world from 72% to 12% and allocation
  from 218 to 0.4 MB/s. Paused timers should go lower. This is **the step that makes 30–50k NPCs possible.**
- **Effort:** M. **Risk:** M. Unseen NPCs stop wandering and regenerating until a player comes near, which is Sphere's
  behaviour too.

### Phase D: saves without the freeze

**D1. Measure where a save's time goes.**
- **Work:** formatting under the lock vs. disk writing, at the design world.
- **Benefit:** shows how short D2 can make the freeze.
- **Effort:** S.

**D1 results (#1203, 2026-11-05).** Every save now logs its timings (see persistence-and-saving.md), and
`.TestPerf savesplit` formats each file into memory before writing it. Release, the design world at scale 1:
626,043 items, 30,000 NPCs, 94.2 MB of text, 93.9 MB of it in `things.sav`. Harness `PERF_PHASES=save`, one run.

| Save | Pause (total) | Formatting | Writing | `beforeSave` |
|---|---|---|---|---|
| First, right after the world was built | 7,659 ms | 7,429 ms (things 7,284) | 2 ms | 208 ms |
| Second, normal | 1,930 ms | 1,923 ms (things 1,796) | 1 ms | 2 ms |
| Third, formatted into memory first | 2,704 ms | **1,742 ms** (things 1,687) | **953 ms** (things 948) | 2 ms |

- **Formatting is the pause.** About 1.7 s of the 1.9 s goes to turning the world into text.
  - Interleaved with formatting, the buffered writes add about 0.2 s.
  - Done separately afterwards, the write takes 0.95 s: encoding 94 MB of UTF-16 plus the write itself, into the page cache with no fsync.
- **So D2 as planned would shrink the freeze by about 10%, not by half.** "Format under the lock, write on a worker" takes it from 1.9 s to about 1.7 s. The estimate above was wrong.
- **The first save after building the world was 4x slower.** Formatting took 7.4 s against 1.9 s for the next save. It is probably one-time cost:
  - JIT of the save implementors;
  - growth of ObjectSaver's dictionaries;
  - GC of a heap that has just grown by half a million objects.
  
  If a world that was loaded rather than built does the same, the first autosave after every restart is the longest freeze. Check that before D2.
- **What D2 needs instead:**
  - (a) Profile the formatting (`ObjectSaver`, the generated save implementors, number and string formatting) and make it cheaper.
  - (b) Format `things` in parallel under the lock, N chunks into N buffers written in order: the pause becomes about 1.7 s / cores.
  - (c) A copy-on-write snapshot formatted on a worker. This is the most complex.
  - Writing on a worker is still worth having on top: the last 0.2 s, and no stall on a slow disk.
  - Recommendation: (a), then (b), with the worker write; the user decides the scope of D2.
- **Found on the way:**
  - With `BackupsManager` (the default) every `.sav` is written straight into the save's folder; the engine's own `.tmp`-then-rename path is only the fallback.
  - A failed final flush or close still counts as a successful save: #1205.

**D2a results (#1209, 2026-11-06).** The user chose: profile, then parallel.
- **What the profile showed:** half of a save's pause was neither formatting nor the disk. It was write calls.
  - BackupsManager opened each file as `StreamWriter(File.Create)`: a 1K-char writer buffer and a 4 KB file buffer, so 94 MB became tens of thousands of `pwrite` calls.
  - D1's "formatting is the pause" counted those calls as formatting, because they happen inside it.
- **What changed:**
  - big buffers, 64K chars and 1 MB;
  - formatting into pooled 1 MB byte chunks under the lock;
  - the autosave (`Globals.SaveInBackground`) writes the files on a worker after the pause, fsynced, with `@afterSave` once they're written;
  - a failed write fails the save (#1205).
- **Release, scale 1, the same world as D1:**

| Save | Before | After |
|---|---|---|
| Autosave, world held | 1,930 ms | **790 ms** (then 160 ms writing off the lock) |
| `.save`, shutdown, recompile (written before returning) | 1,930 ms | 951 ms |
| First save after the world was built | 2.2-7.7 s | 1.6 s |

**D2. Snapshot, then write on a worker thread** (problem 3).
- **Work:**
  - each save coordinator formats into a pooled in-memory buffer while the world is paused;
  - the lock is released;
  - a worker thread writes, flushes and renames the save folder;
  - the next save waits for the previous write.
- **Benefit:** the freeze shrinks to the formatting time (*estimate* well under half of today's 2.6–5 s). The interval can
  then drop to 15–30 minutes.
- **Test:**
  - the existing save/restart tests (AuctionRestart, GuildStoneRestart, WorldSetupRestart, BootLogTests);
  - a test that changes things during the write and finds the save consistent;
  - a test that kills the process mid-write and finds the previous save intact.
- **Effort:** M–L. **Risk:** M.

### Phase E: the rest

- **E1. Split `Sector.Things` into characters and items** (problem 4).
  - **Benefit:** character queries never see items, removing the 7.6% of type checks seen at load.
  - **Effort:** M. **Risk:** M.

**E1 results (#1214, 2026-11-06).**
- **What changed:**
  - `Sector` keeps two lists, characters and items; `Add`, `Remove`, `Disconnected` and `Reconnected` route by type.
  - Character and NPC queries walk characters only, item queries items only.
  - `GetThingsInRectangle` and `CanFit` walk the lists by hand, so they make no enumerator at all.
- **Behaviour:** thing queries return a sector's characters, then its items. Before, the two were mixed in insertion order.
  No caller depends on that order. An item made while the characters are being walked can now be met in the same query.
- **Release, scale 1, the design world, `.TestPerf bench` (2000 calls each), one run each:**

| Query at the densest dungeon spot | Before (main 2f1eea8) | After |
|---|---|---|
| Characters within 14 (7 found) | 3.0 µs | **1.2–1.6 µs** |
| Items within 18 (131 found) | 6.9 µs | 4.8–5.1 µs |
| Things within 18 (142 found) | 13.2 µs, 480 B | **5.4 µs, 128 B** |

- **E2. Allocation-free range enumerators**, struct enumerators or callbacks.
  - **Effort:** M. **Risk:** L–M.

**E2 results (#1216, 2026-11-06).**
- **What changed:**
  - Map's range queries return structs (`MapQueries.cs`: `ThingsInArea<T>`, `PlayersInArea`, `ConnectionsInArea`, `InternalItemsInArea<T>`). Each has a struct enumerator that walks the sectors and lists by hand, reading the next element before handing out the current one, as the iterators did.
  - The `...InRange` queries keep their bounds as numbers, with no rectangle object.
  - The structs also implement `IEnumerable<T>`, so LScript, LINQ and `new List<T>(...)` work unchanged (boxed, as costly as before).
  - `ItemSyncQueue` walks the query directly instead of through an `IEnumerable` variable.
- **Behaviour:**
  - The players' list is walked by node. When the next player leaves the list during a walk, the walk goes on from the current one. `LinkedList`'s enumerator threw "collection was modified" there.
  - Statics on a tile off the map now come back empty instead of throwing.
  - The speech path still boxes, one allocation per speech, beside its arguments and packets.
- **Release, scale 1, the design world, `.TestPerf bench` (2000 calls each); before = E1's final run:**

| Query at the densest dungeon spot | Before (E1) | After |
|---|---|---|
| Characters within 14 (7 found) | 1.2 µs, 480 B | **0.7 µs, 0 B** |
| Items within 18 (131 found) | 5.1 µs, 480 B | **3.1 µs, 0 B** |
| Things within 18 (142 found) | 5.4 µs, 128 B | **4.4 µs, 0 B** |
| Players within 18 | 0.2 µs, 144 B | 0.1 µs, 0 B |

- **E3. Keep the map files open or memory-mapped** (problem 8).
  - **Benefit:** no file opens on the game thread as players explore.
  - **Effort:** S–M. **Risk:** L.

**E3 results (#1217, 2026-11-06).**
- **What changed:**
  - `StaticSector` memory-maps each facet's `map`, `staidx` and `statics` `.mul` files once, for the life of the process.
  - A sector load copies each 8×8 block, staidx entry and statics block out of the view with one read and parses it. Before, every load opened three new `FileStream`s and read them a seek and a few bytes at a time.
  - Missing, short or corrupt files still disable the map with the same messages.
- **Release, scale 1, world-building steps of the harness (they load sectors all over the map), main a6dee8c vs E3:**

| Step | Before (two runs) | After |
|---|---|---|
| 50,000 ground items over plane 0 | 9.2 / 9.3 s | **0.49 s** |
| 10,000 NPCs over plane 0 | 3.9 / 3.7 s | **0.82 s** |
| 5,000 NPCs over plane 1 | 1.5 / 1.5 s | 0.53 s |
| 15,000 NPCs + 150,000 items in the dungeon rectangles | 3.0 / 2.9 s | 1.6 s |

- **Memory:** the working set at the design world grows from 786 to 883 MB. That's the mapped files' pages counted as resident: file cache the kernel can drop, not heap (619 MB both sides).
- **Operations:** while the server runs, Windows won't let the mapped files be deleted or replaced, so a git checkout that changes `uo-files/*.mul` fails until the server stops. On Linux, a file replaced by rename is fine; one rewritten in place can kill the process.
- **E4. Re-measure GC and tune** (problem 9).
  - **Effort:** S.

**E4 results (#1220, 2026-11-06): no GC setting changes.**
- **Re-measured** on main after C2, B4, E1-E3 (Release, scale 1, all phases). The harness now also reports the longest single GC pause per window (`gcMaxPauseMs`, sampled every 20 ms per GC kind).

| Phase | GC pause at the review | GC pause now (total / longest) | Allocation then → now |
|---|---|---|---|
| P0 empty world | 0.04% | 10 ms / 30 s, longest 10 ms | 0.1 → 0.1 MB/s |
| P1 15k dungeon NPCs | 1.7% | 6 ms / 30 s, longest 6 ms | 87 → 0.1 MB/s |
| P2 design world, no players | 7.0% | 9 ms / 30 s, longest 9 ms | 218 → 0.1 MB/s |
| P3 50 players in a crowd | 10.5% | 188 ms / 60 s, longest **94 ms** (background gen2, 677 MB heap) | 220 → 3.0 MB/s |
| P4 50 players in a dungeon | 9.3% | 132 ms / 60 s, longest 10 ms | 248 → 1.3 MB/s |
| P5 stress, 50k NPCs | 11.9% | 193 ms / 60 s, longest 43 ms (background gen2, 874 MB) | 251 → 1.9 MB/s |

- **Tried:** server GC, one full run each, against the default workstation concurrent GC.

| | Workstation (kept) | Server + DATAS (`GCDynamicAdaptationMode=1`) | Server, 4 heaps |
|---|---|---|---|
| Longest pause P3 / P5 | 94 / 43 ms | 26 / 125 ms | no GC / 253 ms |
| Total pause, all windows | ~540 ms | ~150 ms | ~250 ms |
| Total pause P5 (60 s) | 193 ms | 125 ms | 253 ms |
| Working set P2 / P5 | 896 / 1117 MB | 1119 / 1297 MB | 1395 / 1781 MB |
| Saves at 30k NPCs (3) / at 50k (1) | 1.4, 1.2, 1.0 / 1.4 s | 1.4, 0.8, 0.9 / 2.2 s | 2.2, 1.9, 1.9 / 2.0 s |
| Timer lateness max P5 | 33 ms | 40 ms | 225 ms |

- **Verdict: the default workstation GC stays.**
  - GC is under 0.4% of wall time in every phase (the most is 193 ms in 60 s).
  - Server GC collects less often: total pause time over the run fell from ~540 ms to ~150-250 ms, and in the 4-heap run P0-P4 had no GC at all.
  - But each of its collections is longer: the worst pause was 125 and 253 ms, against 94. That's one GC per configuration in P5, so a small sample.
  - It also costs 180-660 MB more working set. The 4-heap run made every save slower; DATAS was mixed.
  - The worst pause is what a player feels as a hitch, and memory is tight on a shard like this one, so neither trade is worth it.
  - TieredPGO is on (the .NET 8 default; nothing turns it off).
- **Not tried:** concurrent GC off (would make every gen2 blocking) and a bigger gen0 budget (`GCgen0size`). The numbers didn't call for them.
- **Open question (final master issue):** a background gen2 still stops the world once for up to ~0.1 s.
  - Only two were seen: 94 ms in the P3 crowd at 677 MB and 43 ms in P5 at 874 MB, so the bigger heap paused shorter.
  - A background gen2's pauses are its initial root mark and its final revisit of what changed meanwhile. They may follow how much the world changes during the collection (P3 is 50 players moving) more than heap size.
  - Worth measuring with more samples before choosing a fix.
- **E5. Re-measure crowds** (problem 10) with walkers that don't collide; look at movement pacing if steps are still slow.
  - **Effort:** S.

**E5 results (#1222, 2026-11-06).**
- **Cause of the crowd's slow steps: Nagle, not the server.**
  - Game connections send every packet group as its own send, and nothing switched Nagle off.
  - In a crowd a client always has data in flight (the other players' moves), so a small send (the step's ack) waited for the client's delayed ACK. That's ~40 ms on Linux, the test client; on Windows the delayed-ACK timer is up to 200 ms, so players there waited longer.
  - The game lock was busy 1.2-3.1% of the time throughout (`lockBusy`).
- **Fix:** `TCPServer.OnAccept` sets `NoDelay` on every accepted socket.
  - ServUO, RunUO (unless `NagleEnabled`) and Sphere switch Nagle off too. They also collect a client's packets and flush them once per loop or network tick; this engine still sends each packet group on its own (see "Left" below).
  - **Cost on the wire:** in a 50-player crowd each client gets ~220 moves a second. As separate segments that's roughly 130 kbit/s per client, mostly TCP/IP headers, against ~40 before. Server-wide that's ~6-7 Mbit/s out instead of ~2. Fine for broadband; it would matter only on a host that caps packets per second.
- **Harness:**
  - `.TestPerf lanes` gives each walker its own row segment in the crowd. A new phase, P3b, runs the walkers along their lanes, and a refused step resyncs them.
  - `lanes` and `scatter` refill stamina: P3's minute of running leaves walkers unable to move (#779). P4's numbers didn't change measurably from it (82-83% refused, p50 1.2-1.5 ms, as in E4).
  - P3b isn't P3's tight crowd: the lanes fill about 50×60 tiles around the spawn, wider than the 18-tile view range, so a step there reaches fewer viewers. A walker the search can't place (one of 50 in every run) is moved out of view.
  - P3's random walkers stay as they were, so P3 is comparable with section 5. About 75% of their steps are still refused there: walls, other players, fatigue.
- **Release, scale 0.1, 50 walkers, step round trip (client sends, server acks):**

| | Without NoDelay | With NoDelay |
|---|---|---|
| P3 crowd, random walkers, p50 / p95 / p99 | 41.6 / 46.8 / 50.2 ms | **9.5 / 20.2 / 25.3 ms** |
| P3b crowd on lanes (97% of steps accepted), p50 / p95 / p99 | 42.5 / 111.8 / 131.3 ms | **19.2 / 45.0 / 59.4 ms** |
| P4 spread in a dungeon, p50 | 1.3 ms | 1.2 ms |

  - Four earlier alternating runs (two each, before the lane resync) gave the same split for the median: P3 p50 41.8-41.9 without, 9.8-11.3 with.
  - **The tails and P3b vary.** With NoDelay, P3's p95/p99 were 39-41 / 49 ms in two runs and 19-20 / 25-28 ms in two others. P3b's median was 19.2, 34.4 and 19.0 ms in three runs, against 42.5 without. So some steps still wait ~40 ms, and not always.
  - It isn't the test client's own Nagle: one run with `NoDelay` on `TestGameClient` too (as ClassicUO/TazUO set it) gave the same (P3 9.3 / 33.5 / 46.7, P3b 19.0 / 47.6 / 56.0 ms).
  - P4's p99 is noisy either way (8-42 ms without, 7-15 with); no conclusion from it.
  - Every P3b run has one 0.5-0.7 s maximum, with or without NoDelay. It's probably the teleport's burst of nearby items still arriving when the window starts.
- **Left (final master issue):** an accepted step among 50 walkers still takes ~19 ms at the median, against ~1 ms in a quiet place.
  - Each step is broadcast to every player in view as its own small send, and logins and teleports send every nearby item and character the same way.
  - Sending per cycle in batches (the connection already has an unused packet-joining path, `PacketGroupsJoiningAllowed`) or measuring where those 19 ms go (server sync vs. the test client reading 49 players' updates) is the next step.
  - Movement pacing (`MovementState`'s reserves) wasn't seen in the round trips (walkers step every 220 ms, slower than a run step), though no counter measures it.

### Summary

| Step | What | Effort | Risk | Benefit (measured unless *estimate*) | Needs |
|---|---|---|---|---|---|
| **A1** | Every code-registered def exists before load | S–M | L–M | 11 kinds of state survive a restart; no ERRORs | – |
| **A2** | In-progress skills load their skill | S | L | Crafts, swings and casts finish after a restart | – |
| **A3** | Save on shutdown, interval on `.settings` | S | L | A restart loses nothing | – |
| B1 | Land the harness | S | L | Before/after for everything below | – |
| B2 | Release builds | S | L | 2–3× per NPC think | – |
| B3 | No timer self-signal | S | L | ~19% of the game thread at load | B1 |
| B4 | `NOLINQ` | S | L | Queries 2×, allocation ~95% less per query | B1 |
| B5 | Bounded timer round | S | L–M | No freeze in whole rounds under overload | B1 |
| B6 | Monotonic clock | S | L | ~1%, immune to clock jumps | – |
| C1 | Walk home straight first, A* back-off | S | L–M | 57.7% → 44.4% idle design world | B1 |
| C2 | NPC sleep | M | M | 72% → ~12% or less (prototype) | B1, C1 |
| D1 | Measure save split | S | – | Sizes D2 | – |
| D2 | Snapshot save, write on a worker | M–L | M | Freeze → *estimate* < half of 2.6–5 s | D1, A3 |
| E1–E5 | Data structures, map files, GC, crowds | S–M each | L–M | Smaller; re-measure first | B1 |

**Expected end state** (*estimate*, to be checked with the harness after each phase):
- the design world idle at about 10% of a core;
- 50 players at about 20%;
- timers p99 well under 100 ms;
- a save freeze cut to a fraction of today's 2.6–5 s (D1 will size it), every 15–30 minutes and on shutdown;
- any save boots without an ERROR.

**Decisions for you before starting:**
1. A3: on a failed shutdown save, exit anyway (recommended) or refuse?
2. A2: an in-progress skill finishes after a restart (recommended, as Sphere) or is cancelled?
3. C2: how long an area stays awake after the last player leaves: 5 minutes (recommended to start), or Sphere's 10?
4. Order: A first as you asked. Then B1 + B2 before anything else in B–E, so every later step is measured on Release.

## 9. Benchmark and stress-test setup

The harness is on `main` since plan step B1 (#1179). It was written on the local branch `1161-perf-harness`; the
prototype commit there and its `AuxiliaryServer/Commands.cs` workaround didn't come along (the workaround is step B2).

**Pieces:**
- `Common/PerfCounters.cs` plus four call sites (`Timer.ProcessTimingOut`, `AsyncCore.HandlePacket`, `SyncQueue.Cycle`,
  `MovementState.Cycle`): the time the main `globalLock` holders held it, by type, and each wait for the lock, in a fixed
  log-scale histogram so its memory doesn't grow on a live shard. Logins, logouts and the server's own console aren't
  hooked.
  - **Off by default:** `perfCounters` in steamengine.ini's `[diagnostics]`. While off, a call site only reads a flag.
    While on, each holder costs three `Stopwatch.GetTimestamp` calls and two short locks of the counters' own, one of
    them around a dictionary update.
  - **The ini value applies at the first load only:** a script recompile reloads the ini but leaves what a GM set. An
    older steamengine.ini doesn't get the `[diagnostics]` section (the file is only written when it's missing): add
    `perfCounters = True` under it by hand to count from the start. After a recompile a timer type can show twice (old and
    new scripts) until `.perfreset`.
  - **GM commands:** `.perf` prints the busy share of the lock, the waits (p50, p99, max) and the top 15 holders.
    `.perfon` starts a fresh window, `.perfoff` stops counting and keeps what was counted (the window stops too), and
    `.perfreset` clears it. This answers "why is the server slow right now" on the live shard.
- `scripts/Tests/PerfProbe.cs`, `.TestPerf`: it refuses to run unless the server was started by the test harness (its
  environment variable), as it spawns tens of thousands of things, moves players and starts a thread that never stops:
  - `rects <file> <npcs> <items>` spreads monsters and decoration over the dungeon rectangles by area;
  - `npcs`, `ground` and `stored` populate a rectangle or containers;
  - `start` (switches the counters on) and `report <label>` open and close a measurement window. The report gives GC counts, total GC
    pause and allocation rate, and since #1220 the longest single GC pause of the window (`gcMaxPauseMs`, with its kind and
    generation). The lock prober samples it every 20 ms, so it's a lower bound: two GCs of one kind between samples, or GCs
    while the game thread holds the lock, show as one;
  - `gc` forces a blocking gen2 (for the test of the pause sampling);
  - `save` times a save and prints where its time went (`WorldSaver.LastTimings`, #1203); `savebg` saves as the
    autosave does (#1209: the pause is the formatting, the files are written on a worker) and waits for the write;
  - `bench <plane> <x> <y>` times range queries;
  - `scatter` moves the walkers.
- `Tests/Perf/PerfProbeTests.cs`: phases P0–P5 as in section 5, 50 walkers. `Tests/Perf/PerfLoadTests.cs` boots on the
  last save and times it.
  - **Opt-in:** both are `[PerfFact]`, skipped unless `PERF_RUN=1`, outside both test tiers. They boot their own servers,
    so a skipped one costs nothing.
- `scripts/test-perf.sh` runs them (it sets `PERF_RUN=1` and generous stuck limits, and goes through `test-full.sh`).
- `GameServerPairFixture` takes `PERF_CONFIG=Release` to boot the Release binaries (built by `scripts/test-perf.sh`, or
  `build.ps1 -Configuration Release`).
- `tools/perf/dungeon-rects.txt`, the dungeon rectangles from `map.def`, and `tools/perf/stmbench`, the STM benchmark of
  section 11 (`dotnet run -c Release` in that folder; it isn't part of the solution).

**Running it:**

```
PERF_OUT=$PWD/perf.txt PERF_SCALE=1 PERF_PLAYERS=50 PERF_SECONDS=30 PERF_PHASES=all scripts/test-perf.sh
scripts/test-perf.sh PerfLoadTests     # then: boot on the save it left, timing the load
```

| Knob | Values |
|---|---|
| `PERF_SCALE` | 1 = the design world (about 8 min end to end); 0.1 is a 2-minute smoke run |
| `PERF_PHASES` | `base` (P0–P2), `players`, `save`, `stress`, or `all` |
| `PERF_RECTS` | the dungeon rectangles; `tools/perf/dungeon-rects.txt` by default |

**What to compare, and the pass line** (the targets in section 1): at P3 and P4 the game thread ≤ 50% busy, timer lateness
p99 ≤ 100 ms, step round trip p99 ≤ 100 ms, GC pause ≤ 2%; save pause ≤ 1 s; boot ≤ 2 min. Rerun the same scale and phases
before and after a change, on the same machine, and compare busy% and p99s. Absolute numbers move with the machine, ratios
don't.

**Profiling recipe:** find GameCore's pid, then

```
dotnet-trace collect -p <pid> --duration 00:00:00:25 --format Speedscope \
  --providers 'Microsoft-DotNETCore-SampleProfiler,Microsoft-Windows-DotNETRuntime:0x14C14FCCBD:4'
```

Sum inclusive time per frame on the thread whose stacks contain `Timer.TimerThread`. With dotnet-trace 10 the old
`--profile cpu-sampling` name is gone.

## 10. Limits of this study

- **Machine.** The container shares 8 cores with other work and has 5 GB; the real server will differ in absolute numbers.
- **World.** The harness world is an estimate: random spots inside real dungeon rectangles, six monster types, decoration
  without real buildings. Real spawns are more often on walkable ground, so A*'s share (problem 7) is overstated here.
- **What wasn't simulated:**
  - **Combat:** the walkers had Str 30000 and never attacked, so monsters swung at them and nothing died. Mass PvP, spells
    and death/corpse/loot paths weren't stressed.
  - **Spawns:** no spawn gems were placed (no respawn timers).
  - **Clients:** no 3.0.6m client, no gumps.
- **P5's client numbers are missing:** the walkers' 5 s read timeout tripped during the 5 s save just before it.
- **1M items wasn't reached:** 626k at the design point; save time and memory at 1M are extrapolated linearly.
- **The prototypes are experiments.** The NPC-sleep prototype changes behaviour (unseen NPCs don't wander or regenerate) and
  is not what problem 1 proposes.

## 11. Using more cores (STM)

Whether software transactional memory ([Shielded](https://github.com/jbakic/Shielded), recommended by SteamEngine's
original author) could let the game logic run on several threads is researched in
[Multithreading with STM](multithreading-and-stm.md) (#1165). Short answer: not for this target. Shielded's bookkeeping
measured about 95 ns per field access in a transaction, roughly as much as a whole NPC think, and four threads won back at
best 2.8×. Adopting it means converting about 3,600 mutable fields and deferring about 3,500 packet and log calls. The
problems above are too much work, not too few cores. If cores are ever needed, keep the single game thread and move
separable work (save serialization, path planning, map loading) off it first, as ModernUO does.

## 12. Sector size, measured

**Question (review):** sectors are 16×16 tiles (`Map.sectorFactor = 4`, `GameCore/Regions/Map.cs:31`), smaller than a
player's 18-tile view. Should they be 32×32 or 64×64, like Sphere?

**What sector size trades.** A range query visits every sector its square touches and checks every thing in them:
- *Bigger sectors* mean fewer sectors to visit but more things outside the range to check and throw away. On average a
  query of side *L* scans about (*L* − 1 + *S*)² tiles for sector size *S*:

  | Query | Tiles in range | *S* = 16 | *S* = 32 | *S* = 64 |
  |---|---|---|---|---|
  | View range 18 (*L* = 37) | 1,369 | 2,704 (2.0×) | 4,624 (3.4×) | 10,000 (7.3×) |
  | Aggro range 14 (*L* = 29) | 841 | 1,936 (2.3×) | 3,600 (4.3×) | 8,464 (10×) |

- *Smaller sectors* mean more sector-to-sector moves. Each is two O(1) list operations, so cheap.
- Sphere can afford 64 because its character searches never walk the item lists; ours do until problem 4's split (E1).

**Measured:** the harness, design world, with the three prototypes on (NPC sleep among them), only `sectorFactor` changed.
Same session, one run each:

| | 16×16 | 32×32 | 64×64 |
|---|---|---|---|
| Range query, densest dungeon spot: characters within 14 | 4.2 µs | 5.6 µs | **14.0 µs** |
| Range query there: things within 18 | 7.9 µs | 6.6 µs | 14.5 µs |
| P2 idle design world, game thread busy | 12.2% | 8.0% | 6.4% |
| P3 50 players crowded, game thread busy | 12.3% | 4.6% | 4.6% |
| P4 50 players in a dense dungeon, game thread busy | 13.1% | 11.5% | **16.1%** |
| P4 step round trip p99 | 50 ms | 41 ms | 75 ms |

**Reading it:**
- **The idle and crowd numbers favour big sectors only because of the prototype.** Its per-NPC "any player within 40
  tiles?" check scans 36 sectors at 16×16, 9 at 32 and 4 at 64. Plan step C2 replaces that check with per-area wake flags,
  so the difference disappears.
- **Where NPCs are really active (P4), 64×64 is clearly worse:** queries 2–3× slower, more game-thread time, worse step
  times. 32×32 is about even with 16.
- **Recommendation: keep 16×16** for the sector grid. Give sleep (C2) its own coarser grid, 64×64 areas, which is the scale
  where Sphere's 64 makes sense.
- Re-measure 16 vs 32 once C2 and E1 are in. With characters in their own lists (E1), bigger sectors cost less, and 32 may
  win.
