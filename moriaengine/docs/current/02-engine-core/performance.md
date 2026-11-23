# Performance and concurrency

World mutation is serialized under `MainClass.globalLock`. A long callback delays other world
work even when CPU cores are idle. [Runtime lifecycle](runtime-lifecycle.md) and
[timers](tags-and-timers.md) define the current ownership and scheduling contracts.

## Rules for changes

- Keep work under the world lock bounded. Move independent I/O outside it through explicit
  snapshots/results, not background traversal of live mutable objects.
- Cache reusable keys; unbounded dynamic names remain in the key registry after tag removal.
- Avoid repeated broad world scans in per-character/per-tick paths. Use appropriate existing
  indexes and measure candidate counts and allocations.
- Preserve background saving's boundary: format state under the lock, write buffers outside.
- Measure individual timer callback duration; the round budget cannot interrupt a callback.

A concurrent collection does not make operations spanning many entities atomic. A different
world concurrency model needs an architecture decision and representative measurements.

## Investigating a slowdown

1. Record revision, configuration, content/save data, clients and workload.
2. Separate compilation/startup, loading, steady gameplay and saving.
3. Measure latency and lock duration alongside CPU, allocations and throughput.
4. Repeat the same workload after a change and verify gameplay semantics as well as speed.

The [research archive](../08-history/research/README.md) contains the earlier performance
review, follow-up measurements and STM investigation. Distinguish observed bottlenecks,
implemented changes and unimplemented proposals. Historical numbers are not current guarantees.

Sources: [Main](../../../GameCore/Main.cs), [Timer](../../../GameCore/Timers/Timer.cs),
[WorldSaver](../../../GameCore/Persistence/WorldSaver.cs).
