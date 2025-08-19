Refactor the simulation code to reduce allocations per tick and clarify ownership.


Constraints: keep Domain pure and tests green. Use structs where safe, object pools, and Span<byte> in snapshot codec. Add a benchmark test that runs 10k ticks and asserts <X allocations.