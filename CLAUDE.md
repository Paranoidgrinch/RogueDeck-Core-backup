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

The trigger-definition migration and both post-migration inconsistency fixes are complete. The two most contained next steps are:

1. **Source-free target selection** — add an explicit way for a recipe to produce no requests when it has no meaningful target, rather than relying on the `ShouldSuppressMissingSourceTarget` guard.

2. **Definition build, validation, and freeze phases** — add a validation pass that runs when a definition is registered or frozen, catching misconfigured selectors, missing filters, or invalid combinations before they reach runtime.

## Deferred larger work (each needs its own planning pass)

- Broad card-effect-definition migration
- Enemy-command migration
- Legacy-wrapper removal
- Arbitrary interleaving of actions and recipes
- Broader per-chain activation budgets beyond trigger depth

## Validation

Always run `dotnet test` after changes. Add or update tests for every behavior change.

## Git workflow

- `git status --short --branch` before substantial work.
- Keep diffs focused.
- Do not commit or push unless explicitly asked.
- Run `dotnet test` and show diff summary before suggesting a commit.
