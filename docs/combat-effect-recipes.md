# Combat Effect Recipes

## Status

The typed combat-effect recipe architecture now includes:

- generic recipe and typed value-provider contracts
- the completed temporal-trigger migration
- queue-level effect-chain causality metadata
- trigger-definition ancestry
- explicit triggered-effect reentry policies
- a configurable maximum trigger depth
- the first bounded reactive-trigger migrations

The following definitions currently support generic `Effects` alongside legacy `Actions`.

### Temporal definitions

- `RoundStartedTriggeredEffectDefinition`
- `RoundEndedTriggeredEffectDefinition`
- `TurnStartedTriggeredEffectDefinition`
- `TurnEndedTriggeredEffectDefinition`

### Reactive definitions

- `CardCostPaidTriggeredEffectDefinition`
- `HealedTriggeredEffectDefinition`
- `ResourceGainedTriggeredEffectDefinition`

The migration remains additive:

- existing trigger-specific `Actions` collections remain supported
- no public legacy trigger API has been removed
- generic recipes reuse existing target resolvers and request builders
- all requests continue through the existing effect queue
- typed request handlers remain responsible for state mutation
- no central `EffectKind` switch exists
- requests are not automatically deduplicated

## Purpose

Trigger-specific action wrappers create an event-by-effect matrix.

Recipes separate:

1. the event that occurred
2. the event-specific filters
3. the target selection
4. the typed value calculation
5. the primitive effect to request

The intended composition is:

- WHEN: event-specific typed context
- IF: event-specific filters
- WHO: target selector
- HOW MUCH: typed value provider
- WHAT: event-independent effect recipe

A new combination of an existing event and an existing primitive effect should not require a new engine class.

A genuinely new primitive effect may still require:

- a new `IEffectRequest`
- a new typed request handler
- package registration
- a reusable recipe
- focused runtime and architecture tests

## Core contracts

`ICombatValueProvider<TContext, TValue>` resolves a typed value from a typed context.

`ICombatEffectRecipe<TContext>` builds effect requests from:

- the typed event context
- `TriggeredEffectActionBuildContext`

Recipes build requests only.

They must not:

- mutate combat state
- resolve requests directly
- bypass the queue
- contain a central effect-kind switch
- reinterpret event attribution

`TriggeredEffectActionBuildContext` preserves two distinct concepts:

- `CombatantTargetSelectionContext.Source` is the targeting anchor
- `TriggeredEffectActionSource` is effect attribution

These values are not interchangeable.

## Standard recipes

The current standard recipes are:

- `GainBlockEffectRecipe<TContext>`
- `HealEffectRecipe<TContext>`

They delegate primitive request construction to the existing shared builder.

Additional primitive recipes should follow the same pattern.

## Runtime ordering

For every matching recipe-capable definition:

1. reentry policy is evaluated
2. maximum trigger depth is checked
3. event-specific filters are evaluated
4. legacy actions build requests in list order
5. generic effects build requests afterward in list order
6. every produced request is queued exactly once

There is no automatic request deduplication.

Equivalent behavior configured in both `Actions` and `Effects` intentionally executes twice.

The compatibility model does not support arbitrary interleaving between the two collections.

## Effect-chain causality

Effect-chain metadata belongs to queue infrastructure.

It does not belong to:

- `IEffectRequest`
- `ICombatEvent`
- primitive effect handlers

`CombatEffectChainContext` contains:

- `CombatEffectChainId`
- ordered triggered-definition ancestry
- current trigger depth
- maximum trigger depth

Effects and events produced while a chain is active inherit the same chain.

When a triggered definition produces requests, its definition ID is appended to the child ancestry.

Independent root effects receive independent chain IDs.

## Reentry policy

`ITriggeredEffectDefinition` exposes `TriggeredEffectReentryPolicy`.

The default policy is:

- `SuppressRecursiveReentry`

Under this policy, a definition is skipped when its ID already appears in the active ancestor path.

This blocks direct and indirect return cycles without suppressing independent sibling events.

The explicit alternative is:

- `AllowRecursiveReentry`

Recursive opt-in remains bounded by the maximum trigger depth.

Reentry checks happen before filters.

## Maximum trigger depth

Each `CombatState` has a configurable maximum trigger depth.

The default is:

- `64`

The value is copied into root and child effect-chain contexts.

The depth limit protects against long chains involving different definitions, including chains that do not repeat a definition ID.

The broader queue limits remain final technical safeguards rather than the primary gameplay recursion rule.

## Value providers

Current reusable or migrated providers include:

- `FixedCombatValue<TValue>`
- `RoundEndedCompletedRoundAmount`
- `TurnStartedCombatantStatusStacksAmount`
- `TurnEndedCombatantStatusStacksAmount`
- `CardCostPaidTotalAmount`
- `FixedHealedTriggeredEffectAmount`
- `HealedAmount`
- `HealedRequestedAmount`
- `FixedResourceGainedTriggeredEffectAmount`
- `ResourceGainedAmount`
- `ResourceGainedNewCurrentAmount`

A legacy provider may also implement `ICombatValueProvider<TContext, int>`.

Its generic `Resolve` method should delegate to its existing calculation.

## Targeting and attribution

Recipes reuse the existing target resolver of each trigger family.

### CardCostPaid

- the paying combatant is the targeting source
- the paying combatant is the event target
- effect attribution uses the paying combatant
- the paid card definition is not invented as `SourceCardId`

### Healed

- the healed combatant is the event target
- the original healing source remains the targeting source when present
- the healed combatant remains the targeting fallback when no source exists
- source combatant and source card attribution are preserved

### ResourceGained

- the resource owner is the targeting source
- the resource owner is the event target
- effect attribution uses the resource owner
- no source card attribution is invented

## Completed milestones

### Temporal migration

The completed temporal migration covers:

- `RoundStarted`
- `RoundEnded`
- `TurnStarted`
- `TurnEnded`

It preserves:

- lifecycle-handler order
- existing target semantics
- existing attribution
- public legacy APIs
- queue-based request resolution

### Reactive foundation

The completed reactive foundation includes:

1. a full reactive-trigger audit
2. `CardCostPaid` recipe support
3. effect-chain metadata across effect and event queues
4. definition ancestry
5. default ancestor-path reentry suppression
6. explicit recursive opt-in
7. configurable maximum trigger depth
8. `Healed` recipe support
9. `ResourceGained` recipe support

### Healed validation

Validated behavior includes:

- actual healed amount
- requested heal amount
- source and target attribution
- source-card attribution
- `Heal -> Healed` self-reentry suppression
- continued chains through different Healed definitions
- legacy actions before generic effects

### ResourceGained validation

Validated behavior includes:

- actual gained amount
- new current amount
- resource-owner targeting and attribution
- same-definition self-reentry suppression
- continued chains through different ResourceGained definitions
- legacy actions before generic effects

No general runtime `GainResourceEffectRecipe` was added during this migration.

## Deferred work

The following work remains deferred:

- `DamageDealt` recipe migration
- `DamageReceived` recipe migration
- status-application recipe migration
- status-lifecycle recipe migration
- card-lifecycle recipe migration
- general `CardPlayed` recipe migration
- combatant-lifecycle recipe migration
- source-free target selection
- definition build, validation, and freeze phases
- broad card-effect-definition migration
- enemy-command migration
- legacy-wrapper removal
- arbitrary interleaving of actions and recipes
- broader per-chain activation budgets beyond trigger depth

## Next bounded runtime block

The next block begins with a focused read-only audit of `DamageDealt`.

The audit must verify:

- `DamageDealtCombatEvent`
- `DamageReceivedCombatEvent`
- typed trigger context
- filters and amount providers
- source and damaged-target semantics
- source-card attribution
- damage-kind semantics
- requested, health, and blocked damage
- primitive damage requests and handlers
- follow-up events
- reflected-damage ordering
- downed and combat-result ordering
- existing tests and architecture guards

The first Damage patch must remain limited to `DamageDealtTriggeredEffectDefinition`.

It should prove:

- same-definition `DamageDealt -> DamageDealt` reentry is suppressed
- `DamageDealt -> Heal -> Healed` remains allowed
- the original `DamageReceivedCombatEvent` behavior remains unchanged
- legacy actions still execute before recipes
- source, target, card, kind, and amount semantics remain unchanged

It must not simultaneously migrate:

- `DamageReceived`
- status triggers
- card lifecycle
- card-effect definitions
- enemy commands
