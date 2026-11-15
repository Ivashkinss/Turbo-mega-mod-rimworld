# Wholesale domain planning

`PackingPlanner.cs` is a pure C# candidate for the future wholesale service. It produces a quote and a proposed per-carrier payment/cargo allocation without moving goods, changing the input objects or creating money. Ordering uses ordinal carrier IDs. Failed packing exposes no partial allocation. It supports people and animals through the caller's eligibility predicate, with no faction whitelist.

The planner uses a deterministic payment order; it does not search every possible redistribution of silver to find an optimal packing. The caller proposes a fixed quantity and can try a smaller quantity on failure. Quotes round the whole batch once at 55% of market value. Mass released by the proposed payment is credited only to the carrier paying those coins.

Run `Tools/test_wholesale_planner.ps1` from the mod directory. This compiles the planner and standalone numerical tests against .NET Framework, without loading or running RimWorld. Results are recorded in `Documentation/WholesalePackingTests.json` with both source hashes.

This directory is deliberately **not yet included in the production DLL**: `Tools/build_assembly.ps1` compiles only top-level Source files. When the wholesale service consumes this planner, add these sources explicitly to the compiler and source-freshness guards, rebuild the game assembly, and test actual transactions and saved visits. The checked hotel/retail assembly remains unchanged at this checkpoint.

Runtime integration must derive eligibility, current mass, silver and capacity from actual accessible pawns, reject non-finite native floats before decimal conversion, revalidate the plan before committing, and inspect actual item/currency transfers. A successful mathematical plan does not prove navigation, reservation, rollback, receipt placement, departure or WorldPawns retention.
