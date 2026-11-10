# Performance report, after the plan (2026-11-06)

The second measurement of the engine under a populated world, after every step of the plan in the
[performance review](performance-review.md) (#1161, plan in its section 8, tracked on #1176). Same harness, same
machine and the same settings as the review's [section 5](performance-review.md#5-measurements), so every number here
sits beside its "before". Everything below was measured unless it says *estimate*. Further work is listed at the end
and tracked on #1228.

**Short version.**
- **The game thread is nearly idle in every phase.**
  - The design world (30k NPCs, 626k items) with no players went from 72% busy to 0.2%.
  - With 50 players it went from 82% to 1.6% (crowd) and from 62% to 4.3% (dungeon).
  - At 50k NPCs it went from 86% to 7.0%.
- **Timers are on time.** Lateness p99 is 7–15 ms in P0–P4 and 19–27 ms in P5, against 0.9–5 s before. The 100 ms
  probe timer fired every time it should (300 times in a 30 s window, 600 in a 60 s one); before it fired 41 and 25
  times of 600 in P3 and P5.
- **Allocation is down 98–99.9% (P1–P5), and GC takes at most about 0.5% of wall time** (7–12% before).
- **Steps:**
  - In the crowd, p50 fell from 42 to 11.5 ms and p99 from 231 to 49 ms.
  - In the dungeon, p99 fell from 51 to 15 ms.
- **Saves:** the autosave stops the world for 0.74 s (2.6–5.0 s before). A `.save` takes 0.85 s, and the first save
  right after the world was built takes 1.4 s.
- **Boot to ready** fell from 48 to 36 s, nearly all of it a faster script compile, which the plan didn't touch
  (section 3).
- **Targets (section 5):**
  - Every pass line of the review's section 9 is met except one: the first save after building the world takes 1.4 s,
    over the 1 s target.
  - Section 1's limits on single events are missed now and then:
    - single background GC pauses of 67–101 ms, over the 50 ms limit;
    - single steps of 477–813 ms in P3b and P4, over 300 ms;
    - once, with the profiler attached, a timer 260 ms late, over 250 ms.

Contents: [1. What changed](#1-what-changed) · [2. How it was measured](#2-how-it-was-measured) ·
[3. Results against the review](#3-results-against-the-review) · [4. Where the time goes now](#4-where-the-time-goes-now) ·
[5. Pass lines and the expected end state](#5-pass-lines-and-the-expected-end-state) ·
[6. Further improvements](#6-further-improvements) · [7. Limits](#7-limits)

## 1. What changed

One issue and one PR per step, in the plan's order. Each PR has its own test, a red check and an independent
review. The per-step numbers are in the review's section 8, under each phase.

| Step | What | Issue | PR |
|---|---|---|---|
| A1 | Every code-registered def exists before a save loads | #1163 | #1175 |
| A2 | A skill under way at a save goes on after the restart | #1172 | #1177 |
| A3 | Save on shutdown, `.exitnosave`, autosave interval in `steamengine.ini` | #1173 | #1178 |
| B1 | The benchmark harness on `main`, `.perf` commands | #1179 | #1182 |
| B2 | Release builds | #1185 | #1186 |
| B3 | The timer thread signals itself only while it sleeps | #1187 | #1189 |
| B4 | Range queries without LINQ | #1192 | #1195 |
| B5 | A bounded timer round | #1194 | #1197 |
| B6 | A monotonic game clock | #1196 | #1198 |
| C1 | Walk home straight first; A* backs off after a failed search | #1199 | #1200 |
| C2 | NPCs sleep where no player is near | #1201 | #1202 |
| D1 | Where a save's time goes | #1203 | #1208 |
| D2a | Big save buffers; the autosave writes on a worker | #1209, #1205 | #1210 |
| D2b | Parallel formatting: profiled, skipped by the user's call | #1211 | – |
| E1 | A sector keeps characters and items apart | #1214 | #1215 |
| E2 | Range queries as structs; a `foreach` allocates nothing | #1216 | #1218 |
| E3 | Map files memory-mapped once per facet | #1217 | #1219 |
| E4 | GC re-measured; no tuning | #1220 | #1221 |
| E5 | Nagle off on game sockets | #1222 | #1223 |

Found and fixed on the way:
- #1183, two saves in one second shared a folder (PR #1184);
- #1188, `Timer.Clear` raced the timer thread on a recompile (PR #1191);
- #1190, a fallback load came up with no regions (PR #1193);
- #1204, a sleeping NPC was put back home (PR #1207);
- #1206, GM-moved NPCs keep their home, as in Sphere: a decision documented in PR #1213;
- the test-suite issues #1212, #1180, #1174 and #1104 (PRs #1224–#1227).

## 2. How it was measured

- **Harness:** the review's harness (`scripts/test-perf.sh`, see the review's
  [section 9](performance-review.md#9-benchmark-and-stress-test-setup)), on `main` at 454432d.
- **Settings:** Release, scale 1, 50 players, 30 s windows, all phases:

  ```
  PERF_OUT=$PWD/perf.txt PERF_CONFIG=Release PERF_SCALE=1 PERF_PLAYERS=50 PERF_SECONDS=30 PERF_PHASES=all scripts/test-perf.sh
  PERF_CONFIG=Release scripts/test-perf.sh PerfLoadTests
  ```

- **Machine:** the same dev container as the review: 8 shared cores, 5 GB of memory.
- **Runs:** one full run, then `PerfLoadTests` on the world it saved, then two more runs for the profile in section 4.
  - The two extra runs used `PERF_PHASES=players,stress`. That still builds the world through P0–P2, and skips only
    the save phase.
  - The profiler attached to run 3 for 25 s in the middle of P5, so run 3's P5 carries its overhead. Its P0–P4 are
    clean.
  - All three runs are compared in section 3, under [Repeat runs](#repeat-runs).
- **Phases:** as in the review: P0 empty, P1 dungeons, P2 the design world, P3 a 50-player crowd in town, P4 the players
  in the largest dungeon, P5 stress at 50k NPCs.
  - Since #1222 there is also **P3b**: the same crowd walking lanes, so nearly every step is accepted. The review has
    no P3b to compare against.
- **"Game thread busy"** is the share of wall time something held `globalLock`, the same counter as the review's.

**One difference from the review's P5.** The harness's 2,000 "stored" containers (150 items each, standing in for banks
and houses) lie on the ground, and they decayed during P5: the window shows 2,000 `ItemDecayTimer` firings, and the
item count fell from 709k to 407k.
- So P5 ran with 50k NPCs but only 407k items, and the save after it and the boot on that save had the same smaller
  world.
- Every phase before P5, including the save phase, had the full 626k items.
- The review's P5 lists 709k items. Fixing the harness so the containers can't decay is item 11 in section 6.

## 3. Results against the review

### Phases

Before = the review's section 5 (Release, before any change). After = this run.

| Phase | Game thread busy | NPC AI | CPU | Timer lateness p50 / p99 / max | Step round trip p50 / p99 / max | Alloc | GC pause |
|---|---|---|---|---|---|---|---|
| P0 empty world | 0.1% → **0.0%** | – | 2% → 1% | 0 / 6 / 18 → 0 / 6.9 / 8.4 ms | – | 0.1 → 0.1 MB/s | 0.04% → 0.03% |
| P1 15k dungeon NPCs | 27% → **0.2%** | 27% → 0.2% | 34% → 1% | −1 / 16 / 76 → −0.1 / 7.0 / 7.7 ms | – | 87 → **0.1** MB/s | 1.7% → 0.02% |
| P2 design world, no players | 72% → **0.2%** | 70% → 0.2% | 91% → 2% | −89 / 911 / 2303 → 0 / **7.2** / 8.4 ms | – | 218 → **0.1** MB/s | 7.0% → 0.03% |
| P3 50 players, crowd | 82% → **1.6%** | 77% → 0.1% | 116% → 11% | 1108 / 4374 / 4374 → 0 / **7.0** / 15.6 ms | 42 / 231 / 424 → **11.5 / 48.7 / 65.3** ms | 220 → 2.9 MB/s | 10.5% → 0.36% |
| P3b the crowd on lanes (new) | 2.8% | 0.1% | 12% | 0 / 7.0 / 7.9 ms | 15.0 / 45.3 / 625 ms | 8.8 MB/s | 0.50% |
| P4 50 players, dungeon | 62% → **4.3%** | 60% → 3.0% | 80% → 14% | −84 / 102 / 864 → 0 / **7.6** / 14.0 ms | 0.6 / 51 / 868 → 1.6 / **14.6** / 292 ms | 248 → 1.1 MB/s | 9.3% → 0.24% |
| P5 stress, 50k NPCs | 86% → **7.0%** | 84% → 5.5% | 105% → 15% | 1996 / 5023 / 5023 → 0.1 / **19.0** / 34.3 ms | lost sync → 1.8 / 12.6 / 134 ms | 251 → 4.9 MB/s | 11.9% → 0.43% |

- **The timer probe fired every time.** It ran 300 or 600 times in every window, all it should. Before, P3 and P5 got
  41 and 25 of 600, because the timer thread did whole rounds of every due timer.
- **Longest single GC pause:**
  - P3: 85 ms, a background gen2 at a 673 MB heap (94 ms in #1220's run).
  - P5: 33 ms, a background gen2 at 875 MB.
  - Every other window: 7–17 ms. The review had no per-pause number.
- **P4's step p50 rose** from 0.6 ms to 0.9–1.6 ms in the three runs (E5 measured 1.2–1.5). About 83% of steps there
  are refused by walls and other players, so p50 is mostly a refusal's round trip. Why it is higher than the review's
  wasn't looked into.
- **Single slow steps:**
  - P4's maximum was 292 ms here and 477–509 ms in the other runs.
  - P3b's was 625–813 ms. E5 thought it probably comes from the window starting while the lanes' teleport burst is
    still arriving (#1222); that isn't confirmed.
  - They are single steps, but they exceed section 1's 300 ms limit (section 5).

### Repeat runs

Each cell: run 1 (the main run) / run 2 / run 3. Run 3's P5 had the profiler attached.

| Phase | Busy | Timer lateness p99 | Timer lateness max | Step p50 | Step p99 | Step max | Longest GC pause |
|---|---|---|---|---|---|---|---|
| P3 crowd | 1.6 / 1.5 / 1.6% | 7.0 / 7.4 / 7.2 ms | 16 / 56 / 49 ms | 11.5 / 9.4 / 8.9 ms | 49 / 38 / 40 ms | 65 / 74 / 97 ms | **85 / 76 / 101** ms |
| P3b lanes | 2.8 / 3.2 / 2.9% | 7.0 / 6.1 / 6.2 ms | 8 / 8 / 7 ms | 15.0 / 22.3 / 14.8 ms | 45 / 85 / 46 ms | **625 / 679 / 813** ms | 11 / 12 / 5 ms |
| P4 dungeon | 4.3 / 4.4 / 4.2% | 7.6 / 13.0 / 15.2 ms | 14 / 23 / 25 ms | 1.6 / 0.9 / 1.3 ms | 15 / 17 / 15 ms | 292 / **477 / 509** ms | 17 / 13 / 13 ms |
| P5 stress | 7.0 / 7.5 / 8.1% | 19.0 / 20.3 / 26.5 ms | 34 / **149 / 260** ms | 1.8 / 1.1 / 1.2 ms | 13 / 18 / 16 ms | 134 / 82 / 60 ms | 33 / **72 / 68** ms |

- **What repeats:**
  - Busy repeats within a point.
  - Lateness p99 stays under 30 ms but moves: P4's went 7.6, 13.0, 15.2 ms.
  - P3b's step p99 ranged 45–85 ms.
- **What doesn't:** every maximum moves from run to run.
- **P5's timer maxima of 149 and 260 ms:**
  - Run 3's (260 ms) came while the profiler was attached. In that window, the longest single hold was an
    `ItemDecayTimer` of 68 ms: deleting one of the decaying 150-item containers at once (section 2).
  - Run 2's 149 ms came with a 108 ms longest wait for the lock. No holder recorded anything that long: the longest
    think was 51 ms, the longest decay 11 ms, the longest GC pause 72 ms. The cause is unknown.
- **Background gen2 pauses**, every sample so far:
  - P3: 94 ms (E4), then 85, 76 and 101 ms, at heaps of 673–686 MB.
  - P5: 43 ms (E4), then 33, 72 and 68 ms, at 773–875 MB.
  - P4 windows had background gen2s too, with every pause in them 17 ms or less.

### NPC thinks

| | Before | After |
|---|---|---|
| Thinks per second, design world without players (P2) | ~32,000 | **182** |
| Thinks per second, players in the dungeon (P4) | not measured | 380 |
| Thinks per second, P5 stress (50k NPCs) | not measured (*estimate* ~53,000, scaled from P2) | 980 |
| Average time per think | 22–39 µs | 11 µs (P2), 56–78 µs (P4, P5) |

- **C2's sleep:** an NPC far from players now sleeps with its timer paused, so the thinks left are the ones near
  players: fights, spells and paths.
- **Fewer, dearer thinks:** each of those costs more than the review's average think, but at the design world there are
  about 175 times fewer of them.

### Saves

| Save | Before | After |
|---|---|---|
| Autosave (the pause; the files are written on a worker after it) | 2.6 s and 5.0 s | **0.74 s** |
| `.save`, shutdown, recompile (written before it returns) | 2.6 s and 5.0 s | 0.85 s |
| First save after building the world | 2.2–7.7 s (D1) | 1.4 s |
| After P5 (407k items, 50k NPCs, 98 MB) | 2.6 s (709k items, 50k NPCs, 123 MB) | 1.1 s |

Every save in this run held 94.3 MB of text (98.4 MB after P5).

### Boot

On the save left after P5:

| | Before (709k items, 50k NPCs) | After (407k items, 50k NPCs) |
|---|---|---|
| Boot to ready | 48 s | **36 s** |
| Script compile | 25 s | 15.9 s |
| Script loading | 6 s | 5.3 s |
| World load | 12.3 s | 10.3 s |

- **The compile accounts for 9.1 of the 12 s gained.** The plan didn't change it, and why it got faster wasn't looked
  into: the scripts changed over the week, the build environment changed (#1212), and the machine's load differs.
  It is still the biggest part of a boot (section 6).
- **The world loads aren't comparable:** this world had 302k fewer items. So the plan's own share of the faster boot
  isn't shown by this run.

### Memory

| | Before | After |
|---|---|---|
| Working set, empty world | 0.37 GB | 0.38 GB |
| Working set, design world (P2) | 0.92 GB | 0.89 GB |
| Working set, P5 | 1.04 GB | 1.12 GB |
| GC heap | 0.6–0.8 GB | 0.62 GB (P2), 0.76 GB (P5) |

- **Mapped files:** since E3 the working set includes about 97 MB of memory-mapped map files, page cache the kernel can
  drop, not heap.
- **P5 isn't like for like:** its world had 302k fewer items than the review's.

### Range queries

`.TestPerf bench`, 2,000 calls each:

| | Before | After |
|---|---|---|
| Empty spot (P0) | 0.6–2 µs, 1–1.4 KB per call | 0.5–1.0 µs, **0 B** |
| Densest dungeon spot (P2): characters within 14 | – | 1.6 µs, 0 B |
| Densest dungeon spot (P2): items within 18 | – | 6.0 µs, 0 B |
| Densest dungeon spot (P2): things within 18 | 8–10 µs, **9.5 KB** | 9.7 µs, **0 B** |

- **The times in this run are slower than E2's own bench** (0.7 / 3.1 / 4.4 µs). That bench ran straight after
  P2's window here, and a single 2,000-call run moves this much from run to run.
- **The allocation is the solid result:** no range query allocates anything now. The other part is that NPC thinks,
  the main callers of these queries, run far less often.

## 4. Where the time goes now

**The profile:**
- the review's recipe (`dotnet-trace`, the sample profiler, speedscope);
- the timer thread, which runs NPC AI and every other timer;
- 25 s inside run 3's P5 (50k NPCs, 50 players). The timer thread's work (`ProcessTimingOut`) is 8.2% of its wall time
  in the profile; all holders of the lock together were 8.1% busy in that window.

The review's profile was taken while that thread was saturated, so both columns below are shares of its busy time.
Packet handlers (`MoveRequestInPacket` and the rest) run on other threads and aren't in this profile; the counters in
section 3 cover them.

| Share of the timer thread's busy time | Review (P5) | Now (P5) |
|---|---|---|
| **Idle** (`Thread.Sleep`, share of wall time) | ~0% | **90.8%** of wall time |
| `NPCAiTimer.Think` | 57% | 56% (4.6% of wall time) |
| ↳ fights (`NPCAiTimer.Fight`) | – | 40% |
| ↳ A* (`PlanPath` → `AStar.FindPath`), nearly all in `Map.CheckMovement` | 30% | 34–38% |
| ↳ `FindTarget` | 16% | 5% |
| `Globals.TimeAsSpan` (the game clock) | 1% (`DateTime.Now`) | **21%** |
| ↳ called from `BoundTimer.OnTimeout`, `Think`, `PlanPath` | | 9%, 7%, 4% |
| `EventWaitHandle.Set` (the timer thread waking itself) | 19% | 1% |
| Timer heap work | 6% | ~5% |
| Loading map and statics sectors | 3% | not seen |
| `PerfCounters.Now` (the harness's own counters, off on a live shard) | – | 11% |
| Frames the profiler couldn't resolve (`?!?`) | – | 12% |

- **The thread is idle** nine tenths of the time, with 50k NPCs and 50 players.
- **Of what is left, fights and their path searches are the main cost.** These are NPCs near players, doing what
  they're there for.
- **The game clock costs a fifth of the busy time.**
  - In absolute terms it went from about 1% of the thread's time to 1.7%. It looks large now because everything
    around it shrank.
  - B6 made it monotonic; each read is one `Stopwatch.GetTimestamp` (`clock_gettime`) plus a division;
  - timeouts, thinks and A* read it thousands of times a second;
  - inside a container that clock can be a real system call;
  - reading the clock once per timer round and handing that time to everything in the round is how Sphere does it
    (section 6).
- **The rest of the review's list is gone:**
  - the self-signal (B3);
  - the LINQ in `FindTarget` (B4, E1, E2);
  - per-sector file opens (E3);
  - the timer heap's share (C2: fewer timers fire).

## 5. Pass lines and the expected end state

**The review's pass lines** (its section 9), and the limits on single events from its section 1. "After" is the main
run; for single events, all three runs.

| Target | Before | After | |
|---|---|---|---|
| Game thread at P3 / P4 ≤ 50% busy | 82% / 62% | 1.6% / 4.3% | met |
| Timer lateness p99 ≤ 100 ms | 4374 / 102 ms | 7.0 / 7.6 ms | met |
| Step round trip p99 ≤ 100 ms | 231 / 51 ms | 48.7 / 14.6 ms | met |
| GC pause ≤ 2% | 10.5% / 9.3% | 0.36% / 0.24% | met |
| Save pause ≤ 1 s | 2.6–5.0 s | 0.74 s autosave, 0.85 s `.save` | met |
| Save pause ≤ 1 s, first save after building | 7.7 s | 1.4 s | **not met** |
| Boot ≤ 2 min | 48 s | 36 s | met |
| No single GC pause > 50 ms (section 1) | not measured | P3 76–101 ms, P5 33–72 ms (background gen2) | **not met** |
| Step round trip max ≤ 300 ms (section 1) | 424 / 868 ms (P3 / P4) | P3 65–97 ms; P4 292–509 ms; P3b 625–813 ms | **not met** in P4 and P3b |
| Timer lateness max ≤ 250 ms (section 1) | 4374 / 864 ms (P3 / P4) | P3/P4 at most 56 ms; P5 34 and 149 ms, 260 ms with the profiler attached | met, except 260 ms in run 3 with the profiler attached |

**The plan's expected end state** (review, section 8 summary):

| Expected (*estimate* at the time) | Measured |
|---|---|
| The design world idle at about 10% of a core | 0.2% of the game thread, 2% CPU |
| 50 players at about 20% | 1.6% (crowd) to 4.3% (dungeon); 11–14% CPU |
| Timers p99 well under 100 ms | 7–15 ms (P0–P4), 19–27 ms (P5) |
| A save freeze cut to a fraction of 2.6–5 s, saves every 15–30 minutes and on shutdown | 0.74 s autosave; saves on shutdown since A3; the interval is in `steamengine.ini` (`autosaveMinutes`, still 60) |
| Any save boots without an ERROR | Since A1 and A2 (BootLogTests) |

## 6. Further improvements

All tracked on #1228. Ordered by what a player would notice first. Items 1–3 and 7 are where section 5's targets are
still missed; the rest are improvements.

1. **First save after building or loading a big world: 1.4 s.**
   - It is the one section 9 pass line still missed.
   - D1 saw the same effect larger (7.7 s against 1.9 s), probably one-time costs: JIT of the save implementors, growth
     of `ObjectSaver`'s dictionaries, GC of a freshly grown heap.
   - A world loaded from a save may do the same, which would make the first autosave after every restart the longest.
   - Measure a loaded world's first save. If it is long, a warm-up save into memory at boot, or the cheap formatting
     wins (item 4), would address it.
2. **Background gen2 pauses of 33–101 ms**, about one a minute under load, over section 1's 50 ms limit.
   - Eight samples so far (section 3, [Repeat runs](#repeat-runs)):
     - the crowd (P3) was 76–101 ms at 673–686 MB;
     - the stress phase (P5) was 33–72 ms at a bigger heap;
     - P4's were all 17 ms or less.
   - The pause may follow how much the world changes during the collection more than the heap size.
   - Next step: a pause-by-pause trace (GC events) in a longer P3, then pick a fix. Candidates: fewer long-lived small
     objects per item, `GCgen0size`, or concurrent GC settings (#1220 tried only server GC).
3. **Crowd steps: 9–12 ms p50 in P3 and 15–22 ms in P3b, against 1–2 ms in a quiet place; single steps up to
   477–813 ms** in P4 and P3b, over section 1's 300 ms.
   - Each step is broadcast to every player in view as its own small send.
   - `MoveRequestInPacket` costs 122–135 µs on average in P3b, against 39–62 µs elsewhere.
   - What the single slow steps wait on isn't known. The lanes' teleport burst is only a guess for P3b.
   - Collecting each client's packets and flushing once per cycle is what ServUO, RunUO and Sphere do; this connection
     code already has an unused path for it (`PacketGroupsJoiningAllowed`).
   - Measure first where the 11–15 ms go: the server's sync or the test client reading 49 players' updates.
4. **Cheap save formatting wins, about 25% of the formatting** (D2b's profile, #1211):
   - enum type names found by reflection on every write;
   - `globals.sav`'s defs (130 ms);
   - `SaveBuffer.Rent` (80 ms with a warm pool);
   - `TimeSpan` formatting.
5. **Script compile: 15.9 s of a 36 s boot.** Done in #1358: a boot with no change reuses both script assemblies
   (test server boot 27 s -> 11 s), see scripting-pipeline.md.
   - Every boot and every test server compiles all of `scripts/`.
   - Reusing the last build's assembly when no source changed (a hash of the sources) would cut most of it.
   - That speeds up restarts, and every protocol test boot even more.
6. **Read the game clock once per timer round.**
   - `Globals.TimeAsSpan` is 21% of the timer thread's busy time in P5 (section 4).
   - Taking the time once at the start of a round and handing it to timeouts, thinks and A* would remove most of
     that, as Sphere's per-tick clock does.
   - Before changing it, check what still needs the live clock inside a round (the round's 20 ms budget, B5).
7. **Deleting a big container runs in one go.**
   - In run 3's P5 one `ItemDecayTimer` held the lock for 68 ms, deleting a container of 150 items. That was with the
     profiler attached; the longest in run 2 was 11 ms.
   - It's the likeliest cause of that window's 260 ms late timer.
   - Bank boxes and house chests hold far more than 150 items. Deleting a big container (decay, eviction, a GM
     `.remove`) in slices, or measuring one with thousands of items, would show whether it matters.
8. **Awake NPC thinks cost 56–78 µs on average,** mostly fights and their path searches.
   - They are now the largest holder in P4 and P5 (3.0% and 5.5% of the game thread).
   - A* inside fights is a third of the timer thread's busy time, nearly all in `Map.CheckMovement` (section 4).
   - Options: reuse a chase path while the target stays near its end, or make `CheckMovement` cheaper (it walks the
     statics on each tile).
   - Only worth it if a full shard puts many more players in fights at once.
9. **A GM command to set an NPC's home** (#1206's decision).
   - A GM-moved NPC keeps its old home, as in Sphere, and nothing can set a new one.
10. **The NPC timers that still tick:**
    - A ridden mount is `Flag_Disconnected`; its timer fires and returns.
    - An elite monster's 2 s regeneration tick.
    - Cheap today, but they are the remaining periodic work for NPCs with no player near.
11. **Harness fixes:**
    - The stored containers shouldn't decay mid-run (section 2).
    - `bench` should repeat and report a median, given the run-to-run spread in section 3.
    - P3b's window should start after the lanes' teleport burst has arrived.
12. **Measure on the real server.**
    - Everything here ran in a shared 8-core dev container on Linux.
    - The live shard's hardware and OS (Windows' 200 ms delayed ACK, see E5) and players' real 3.0.6m clients may move
      the absolute numbers, the step times most of all.

## 7. Limits

- **One full run, plus two runs of the player and stress phases.** The review's section 5 was one run.
  - Ratios this large (tens to thousands of times) don't hinge on noise.
  - The smaller differences do: P4's step p50, the range-query times, every maximum.
- **P5 had 302k fewer items** than the review's (section 2).
- **The walkers are synthetic:** a step every 220 ms, random or on lanes, no combat, spells or trading by players.
- **Saves were timed into the container's page cache.** The autosave's write (on a worker, fsynced since D2a) doesn't
  stop the world, but a slow disk would delay the next save, which waits for it.
