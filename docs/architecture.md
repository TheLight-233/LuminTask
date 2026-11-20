# Runtime architecture

[简体中文](architecture.cn.md)

| Boundary | Contract |
| --- | --- |
| Task value | A zero handle holds synchronous success. A pending task transfers single-consumption ownership with its slot identity. |
| Global source | One non-generic source owns the slot protocol. A 48-bit generation distinguishes uses of an index. Pages have 32 slots, with at most 32,767 leased slots. |
| Result layout | Values up to 16 B without references stay inline. References are tracked by the GC. Large or reference-containing structures use typed cells. |
| Async builder | Suspended state machines use a typed runner and a reusable callback. The callback lease protects the runner during reentrancy and cross-thread completion. |
| Publication | Publish outcomes before continuations. Run callbacks outside allocator, cache and slot gates. A late producer cannot publish into a recycled source. |
| Storage | Idle objects and the type index share the 64 KiB default budget. Live slots and scheduler arrays are tracked separately. Trim releases idle pages, cache entries and idle scheduler storage. |
| Dependencies | Core and AsyncEx use managed assemblies with `ref` and `Unsafe`. Unity owns engine objects, subscriptions and scheduling. |

## Change constraints

1. Preserve slot generation, single consumption, exception identity and the cancellation token when optimizing an operation.
2. Clear pooled references before returning an object. Keep Unity APIs behind the adapter boundary and preserve their thread requirements.
3. Compare identical workloads before widening a cache or adding a specialization. Record allocation, GC, retained bytes and relevant AOT output sizes separately.
4. Run the relevant tests in Release and Debug. Repository checks enforce runtime assembly and project boundaries. Package and Unity checks exercise the shipped entry points.

[2.0 design record](design/2.0.md)
