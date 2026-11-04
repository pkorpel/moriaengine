# Multithreading with software transactional memory (Shielded): research (#1165)

The original SteamEngine author recommended [Shielded](https://github.com/jbakic/Shielded), a software transactional
memory (STM) library for .NET, as a way to run game logic on several threads with optimistic concurrency. This page looks
at what it is, what newer options exist, what it costs, and whether it would help this server, measured against the
[performance review](performance-review.md) (#1161). Research only: nothing was implemented.

**Short version.**
- **Not now, and probably not ever for this server's target.**
- **The bottleneck is too much work, not too few cores.** The #1161 review found the game thread too busy because every NPC
  thinks everywhere; three prototype fixes took the design world from 72% to 12% of one core. At 50 players we have
  several times the headroom we need on one thread.
- **STM makes each piece of work several times slower to buy parallelism.** In a model of one NPC think, Shielded's
  bookkeeping alone was 19 µs, about as much as the whole real think (22–39 µs). Four threads won 2.8× back, eight didn't
  do better.
- **Adopting it means converting nearly the whole engine.** That is about 3,600 mutable fields made transactional and
  about 3,500 packet and log call sites deferred to commit.
- **The library hasn't had a release since 2020, and nothing newer replaces it in .NET** except LanguageExt's STM, which is
  the same model in a functional style.
- If more cores are ever needed, do it the way other UO servers do: keep one game thread, and move separable work (save
  serialization, path planning, map loading) to other threads.

## 1. What Shielded is

Analyzed at master `f7f69fa` (version 4.4.9, 2020-04-13), the last commit and the latest NuGet package.

| | |
|---|---|
| Kind | Software transactional memory, MVCC (multi-version concurrency control), "strict and mostly lock-free" |
| License | MIT |
| Targets | `netstandard2.0` and `net4` (runs on .NET 8 through .NET Standard; measured below) |
| Size | about 5,400 lines in `Shielded/` |
| Activity | first commit 2013; last release 4.4.9 in April 2020; no commits since; 247 stars, 20 forks, none of them active |
| Sibling | [Shielded.Gossip](https://github.com/jbakic/Shielded.Gossip), a distributed key/value store built on it (last push 2022); for replicas across servers, not relevant here |

**How you use it.** Shared state lives in `Shielded<T>` fields and in transactional collections: `ShieldedDict`,
`ShieldedSeq` (a linked list) and `ShieldedTree` (a red-black tree). Changes happen only inside
`Shield.InTransaction(() => …)`. Reads outside a transaction are allowed but see only the latest committed value, with no
consistency between fields. Anything that isn't shielded, such as sending a packet, writing a log line or scheduling a
timer, has to be deferred with `Shield.SideEffect(onCommit, onRollback)`, because the transaction body can run several
times. `ProxyGen` can turn a class's virtual properties into shielded ones, but only on the full .NET Framework
(CodeDom), not on .NET Standard.

**How it works inside** (`Shielded/Shielded.cs`, `Shielded/Shield.cs`):
- **Fields.** Each `Shielded<T>` keeps a linked list of versions (`ValueKeeper`). A transaction reads the newest version
  not newer than its start stamp, so a transaction always sees one consistent snapshot. That is MVCC, and a read-only
  transaction never retries.
- **Tracking.** The first access to a field in a transaction enlists it in a per-transaction hash set (`Shield.Enlist`).
  A write goes to a thread-local copy (`TransactionalStorage`) and allocates a new version object.
- **Commit.** Validation runs under **one global lock** (`_checkLock` in `Shield.cs`). If any field the transaction read
  or wrote has a newer version, it rolls back and the whole body runs again. Validation is strict: no write skew.
- **Extras:**
  - *Commutes*: `Commute(…)`, for example a counter decrement, run against the latest value at commit, so they don't
    conflict.
  - *Triggers*: `Shield.Conditional` (an AFTER-commit trigger) and `Shield.PreCommit` (a check before commit).
  - *Hooks*: `SyncSideEffect` and `WhenCommitting`, which run code while the written fields are locked.
  - *Split commit*: `RunToCommit`, which commits later or from another thread.

It is a careful, complete implementation. The question is whether this server needs it.

## 2. Newer or better options

What turned up when looking for something newer:

| Option | State | Notes |
|---|---|---|
| **Shielded** 4.4.9 | Unmaintained since 2020 | The only full STM for .NET with transactional collections. Still runs on .NET 8. |
| **[LanguageExt](https://github.com/louthy/language-ext) STM** (`Ref<A>`, `Atom`, `atomic(…)`) | Active: v5 betas through October 2026, about 7,000 stars | The same MVCC model, with snapshot or serializable isolation and commute. Built for immutable values and a functional style ([docs](https://louthy.github.io/language-ext/LanguageExt.Core/Concurrency/STM)). Adopting it means rewriting state as immutable records, a bigger change than Shielded. No published benchmarks. |
| **Microsoft STM.NET** | Dead, a 2009 experiment ([download page](https://www.microsoft.com/en-us/download/details.aspx?id=8317)) | Needed a modified .NET Framework 4 beta; never shipped. |
| STM in other languages (Haskell `stm`, Clojure refs, Rust `stm`) | – | Not usable from C#. |

No newer .NET STM library replaces Shielded. The research on using STM in a game server is consistent:
**[QuakeTM](https://www.usenix.org/legacy/event/hotpar09/tech/full_papers/baldassin/baldassin_html/) and Atomic Quake**
converted the Quake server (27,600 lines in 49 files) to transactions, which took about 10 person-months. They found it
scales, but the STM overhead and abort rates (up to 26% of transactions at 8 threads on a small map) left it slower than
the lock-based version ([ICS 2009 paper](https://timharris.uk/papers/2009-ics.pdf)).

How other servers use more than one core, for comparison:
- **ModernUO**, the most active C# UO server, runs its game logic on one "Core Thread" event loop
  (`Projects/Server/Main.cs`, `RunEventLoop`). Its threads are for saves: `World.Snapshot` freezes the world and serializes
  it on parallel `SerializationThreadWorker`s, then `WriteFiles` writes to disk on the thread pool
  (`Projects/Server/World/World.cs`).
- **Sphere 0.56b and 0.99z8** (EreborNext) are single-threaded too (see the [review](performance-review.md#2-how-the-reference-engines-do-it)).
- The **[LEARS paper](https://web-backend.simula.no/sites/default/files/publications/srmpds-lears.pdf)** makes the general
  point that dividing the world spatially can't split an area where many players interact closely. That is exactly UO's
  hot spot: towns, banks, mass PvP.

## 3. What it costs: measured

A throwaway benchmark (`tools/perf/stmbench` on the local branch `1161-perf-harness`, never pushed): .NET 8, Release,
Shielded 4.4.9 from NuGet, in the same 8-core container as the review.

**Per access:**

| | Time | Allocated |
|---|---|---|
| Plain field read | 1.8 ns | 0 |
| `Shielded<int>` read outside a transaction | 4.5 ns | 0 |
| `Shielded<int>` read inside a transaction (1000 distinct fields) | **68 ns** | 57 B |
| `Shielded<int>` write inside a transaction (1000 distinct fields) | **169 ns** | 147 B |
| Empty transaction | 56 ns | 280 B |
| Transaction writing one field | 366 ns | 840 B |
| `Dictionary` lookup (how tags are read today) | 4.2 ns | 0 |
| `ShieldedDict` lookup inside a transaction | 28 ns | 5 B |

**One NPC think, modelled:** 30,000 NPCs; each think reads 4 fields of 50 neighbours (200 reads, about what `FindTarget`
and the abilities read in a dense spot) and writes 3 of its own. The plain version is 0.1 µs per think, because the model
does nothing but the accesses. The Shielded version is **19.3 µs**, so the bookkeeping alone costs about 95 ns per access.
The real think measured in #1161 is 22–39 µs in Release.

| Threads (each takes a slice of the NPCs, as a sector scheduler would) | Spread out | Hot spot (200 NPCs per slice hit one player) | Hot spot with `Commute` |
|---|---|---|---|
| 1 | 47.7k thinks/s | 50.5k/s, 0% retries | 51.3k/s |
| 2 | 84.0k/s | 88.1k/s, 0.5% retries | 89.2k/s |
| 4 | **134.6k/s (2.8×)** | 134.8k/s, 4.5% retries | 129.2k/s, 0.3% retries |
| 8 | 99.9k/s (worse than 4) | 96.6k/s, 7.2% retries | 99.3k/s |

What this says:
- **Scaling.** In the best case, four threads give 2.8×, and eight do worse. The global commit lock, the shared
  version list and the GC churn (hundreds of bytes per transaction) eat the rest. On a dedicated server this might reach a
  little further; it won't be linear.
- **The model is kind to STM.** It has none of the engine's shared structures, which every real action touches: the
  sector lists a move rewrites, the `CharSyncQueue` every change is queued on, the timer heap, packet pools, `Globals.src`,
  caches. Each of those would become a conflict point unless rewritten as a commute or moved out of the transaction.
  Real retry rates would be higher than the 0–7% here.
- **Net effect.** Bookkeeping at about 95 ns per access, applied to a real think that reads far more than 200 fields (a
  dense dungeon query alone walks 141 things), makes each think several times slower on one thread. Four threads at best
  recover that. **Expected net result for this engine: no faster, possibly slower, with much more GC.** This is an
  estimate built on the measured per-access costs, not a measurement of the engine converted.

## 4. What adopting it here would take

Shielded only protects state that lives in its containers. Making the game logic safe to run on several threads would mean:

- **Every mutable field transactional.** About **3,000 mutable instance fields and 600 mutable static fields** across 999
  source files in GameCore, Common, Communication and scripts. On top of that come the generated `.ct` fields
  (`*.Generated.cs`, 93 files) and the 515 `[SaveableData]` fields: positions, flags, stats, skills, tags, plugins,
  timers, sector lists, region data, settings. Without ProxyGen on .NET 8, each one changes by hand or in the code
  generators.
- **Every collection replaced.** Tags are a `Hashtable` per `TagHolder`. Sectors hold intrusive lists, the timer queue is
  a heap, and `List`/`Dictionary` are everywhere. All of them would become `ShieldedDict`, `ShieldedSeq` or immutable
  collections.
- **Every side effect deferred.** About **1,650 packet-send call sites** and **1,800 log and console calls**, plus timer
  scheduling, file I/O and `Random`, would all go through `Shield.SideEffect`. Any one that runs inside a transaction and
  is missed fires again on every retry: a duplicated packet, a doubled message, a doubled roll.
- **LScript.** The interpreter's state, and every `.scp`/`.def` trigger that changes the world, would have to run inside
  transactions too.
- **Reads outside transactions.** Packet building, the network threads and the `.info` dialogs read the world without a
  transaction, and would see fields from different moments mixed together.
- **Lost guarantees.** Today all game code runs one piece at a time behind `globalLock`, in a fixed order (#17, #798):
  code can assume nothing changes under it, and a bug reproduces. Optimistic threads give that up for retries and
  run-to-run differences.

That is an engine rewrite, larger than QuakeTM's 10 person-months, with **high risk** and nothing to gain at the target
load.

## 5. Would it help this server?

Not at the target, and not as the next step at any target:

- **The measured problem is the amount of work.** #1161's profile showed NPC AI for unseen NPCs (problem 1), A* retries
  (7), allocation (4) and the timer self-signal (5). Those are fixed by doing less, which the prototype showed: **72% →
  12% of one core**, and the 50-player crowd 82% → 12%.
- **After those fixes one core has room.** The crowd of 50 players cost about 12% of the game thread. *Extrapolated, not
  measured:* if player-driven load grows linearly, one thread carries several hundred players before passing the 50%
  target.
- **STM would take that room away.** Every think would cost several times more, with at best 2.8× back from four cores, and
  GC would rise in exactly the place problem 4 shows it already hurts.
- **Saves.** MVCC would give a consistent snapshot while the world keeps running. But it only covers state that is all
  shielded, and freezing for a fast in-memory serialization (ModernUO, the review's problem 3b) gets the same result
  cheaply.

No narrow use worth it was found. The only state Shielded could protect is state that lives in its containers, and the
interesting state is everywhere.

## 6. If more cores are needed later

In order of value for effort, all of it compatible with the single game thread:

1. **The #1161 fixes first** (its recommended order). They reduce the work itself.
2. **Move separable work off the game thread.** None of these need STM, because what they read either doesn't change or is
   copied first:
   - **Save serialization:** freeze, serialize on parallel workers, write on the thread pool, as ModernUO does (review
     problem 3b).
   - **A\* path planning:** the map and statics never change, so path searches can run on worker threads and hand the path
     back to the game thread. Characters in the way are a snapshot, re-checked at each step, as `NpcPathCache` already
     does.
   - **Map and statics loading:** load sectors ahead of a player on a worker (review problem 8).
   - **Packet compression:** already on the send thread.
3. **Plan in parallel, apply on one thread.** Compute NPC decisions (target choice, next step) in parallel against a
   read-only snapshot of positions, then apply them on the game thread in a fixed order. This needs a fixed tick and a
   snapshot, but no transactions and no retries.
4. **Partition by map plane or region,** each with its own game thread and messages between them (gates, chat, guilds,
   parties). Worth it only if the world grows to many busy, separate areas. It doesn't help a crowded town: the hot spot
   stays on one thread.
5. **STM only after all of that,** and only for a contained subsystem whose state can live in shielded containers and whose
   side effects are few.

**When to revisit:** when, after the #1161 fixes, the harness shows the game thread above 50% at the real player count, and
the load is player-driven work that can't be offloaded. The review's harness (section 9 there) and `tools/perf/stmbench`
are the tools to decide it with.

## Sources

- Shielded: [github.com/jbakic/Shielded](https://github.com/jbakic/Shielded) (master `f7f69fa`, NuGet `Shielded` 4.4.9);
  [Shielded.Gossip](https://github.com/jbakic/Shielded.Gossip)
- LanguageExt STM: [github.com/louthy/language-ext](https://github.com/louthy/language-ext),
  [STM docs](https://louthy.github.io/language-ext/LanguageExt.Core/Concurrency/STM)
- Microsoft STM.NET: [download page](https://www.microsoft.com/en-us/download/details.aspx?id=8317)
- Transactional memory in a game server:
  - QuakeTM, HotPar 2009: [paper](https://www.usenix.org/legacy/event/hotpar09/tech/full_papers/baldassin/baldassin_html/)
  - Atomic Quake, ICS 2009: [paper](https://timharris.uk/papers/2009-ics.pdf)
- LEARS, on parallel processing inside one spatial partition: [paper](https://web-backend.simula.no/sites/default/files/publications/srmpds-lears.pdf)
- ModernUO: [github.com/modernuo/ModernUO](https://github.com/modernuo/ModernUO), `Projects/Server/Main.cs` and
  `Projects/Server/World/World.cs` (read 2026-11-04)
