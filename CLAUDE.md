# RogueDeck Core

C# engine for roguelike deckbuilding games. Modular, deterministic, UI-independent.

## Project layout

- `src/RogueDeck.Core` — engine library
- `src/RogueDeck.ConsoleSandbox` — scratch runner
- `tests/RogueDeck.Core.Tests` — test suite
- `docs/architecture.md` — architecture rules and phase history (authoritative)
- `docs/combat-effect-recipes.md` — recipe system detail, known gaps, deferred work (authoritative)

## Architecture rules (short form)

Full rules live in `docs/architecture.md`. Key constraints:

- `CombatEffectResolver` must stay generic — no switch/if on concrete request types.
- No hardcoded status fields on `CombatantState` (use `StatusInstance`).
- New effects: add request + handler to a grouped effects file, register in `StandardCombatPackage`, add tests.
- Effect file grouping: `StatusEffects.cs`, `DamageEffects.cs`, `DefensivePoolEffects.cs` — new file only for a new group.
- Recipes must not mutate state, resolve requests, or bypass the queue.
- Recipes build requests only.

## Current state (as of 2026-06-17)

**Trigger-definition recipe migration: complete.**

Every `ITriggeredEffectDefinition` now supports generic `Effects` alongside legacy `Actions`. There is no remaining trigger-migration backlog.

## Next bounded work block

Two small, low-risk inconsistency fixes are the natural next step:

1. **Guard parity gap** — `StatusRemoved`, `StatusChargesReduced`, `StatusExpired`: the generic `Effects` recipe path does not yet apply the `ShouldSuppressMissingSourceTarget` guard that the legacy `Actions` path applies. A recipe using `SourceCombatantTargetSelector` on these three triggers silently targets the affected combatant instead of producing no requests. Fix: add the same guard to the recipe execution path.

2. **`DiscardPileShuffled` `EventTargetId` mismatch** — `GainBlockOnDiscardPileShuffledTriggeredEffectAction` builds its own `TriggeredEffectActionBuildContext` inline and omits `EventTargetId`, while the shared helper (and all generic recipes) set it. A pinning test exists: `DiscardPileShuffledLegacyActionAndGenericRecipeDisagreeOnEventTargetAttribution`. Fix: align the legacy action to use the shared helper, then delete the pinning test.

## Deferred larger work (each needs its own planning pass)

- Broad card-effect-definition migration
- Enemy-command migration
- Legacy-wrapper removal
- Source-free target selection
- Definition build, validation, and freeze phases
- Arbitrary interleaving of actions and recipes
- Broader per-chain activation budgets beyond trigger depth

## Validation

Always run `dotnet test` after changes. Add or update tests for every behavior change.

## Git workflow

- `git status --short --branch` before substantial work.
- Keep diffs focused.
- Do not commit or push unless explicitly asked.
- Run `dotnet test` and show diff summary before suggesting a commit.
