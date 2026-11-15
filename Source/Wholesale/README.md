# Wholesale domain planning

`PackingPlanner.cs` is the pure C# planner used by the wholesale service. It produces a quote and a proposed per-carrier payment/cargo allocation without moving goods, changing the input objects or creating money. Ordering uses ordinal carrier IDs. Failed packing exposes no partial allocation. It supports people and animals through the caller's eligibility predicate, with no faction whitelist.

The planner uses a deterministic payment order; it does not search every possible redistribution of silver to find an optimal packing. The caller proposes a fixed quantity and can try a smaller quantity on failure. Quotes round the whole batch once at 55% of market value. Mass released by the proposed payment is credited only to the carrier paying those coins.

Run `Tools/test_wholesale_planner.ps1` from the mod directory. Reviewed independent fixture data in `Tests/Data/WholesalePackingCases.json` is checked by default; use `-Cases <path>` to select another dataset. This compiles the planner and standalone numerical tests against .NET Framework, without loading or running RimWorld. Results are recorded in `Documentation/WholesalePackingTests.json` with source and fixture hashes. The fixture adapter explicitly reports excluded lifecycle/receipt cases instead of claiming the pure planner tested those systems.

This directory is explicitly included in the production build and source-freshness guards. `WholesaleTrading.cs` validates actual members, mass, money and transfers; `WholesaleVisits.cs` owns the persisted shopping-only group lifecycle. Targeted game evidence is recorded in `Documentation/WholesaleStageVerification.json`; it is separate from these standalone arithmetic tests.

Runtime integration must derive eligibility, current mass, silver and capacity from actual accessible pawns, reject non-finite native floats before decimal conversion, revalidate the plan before committing, and inspect actual item/currency transfers. A successful mathematical plan does not prove navigation, reservation, rollback, receipt placement, departure or WorldPawns retention.
