# Combat Effect Recipes

## Status

The typed combat-effect recipe architecture has completed its foundation and temporal-trigger migration.

The following temporal definitions support generic `Effects` alongside legacy `Actions`:

- `RoundStartedTriggeredEffectDefinition`
- `RoundEndedTriggeredEffectDefinition`
- `TurnStartedTriggeredEffectDefinition`
- `TurnEndedTriggeredEffectDefinition`

The migration is additive:

- existing trigger-specific action APIs remain supported
- no existing public trigger API was removed
- generic recipes reuse the existing target resolvers and request builders
- the effect queue, event queue, resolver, registry, and primitive request handlers remain unchanged
- no central effect-kind switch was introduced

## Purpose

Trigger-specific action wrappers create an event-by-effect matrix.

Without recipes, every combination can require another engine type:

- gain block when a turn starts
- gain block when a round ends
- heal when a turn ends
- heal when a card is played

Recipes separate the primitive effect from the event that caused it.

The intended composition is:

    WHEN event
    IF event-specific filters match
    WHO target selector
    HOW value provider
    WHAT combat effect recipe

A new combination of an existing event and an existing primitive effect should not require a new engine class.

A genuinely new primitive effect may still require:

- a new `IEffectRequest`
- a new typed request handler
- registration in a package
- focused tests

## Runtime flow

The accepted runtime flow is:

    event-specific definition
    -> event-specific context
    -> filters
    -> legacy actions
    -> generic effect recipes
    -> IEffectRequest
    -> existing effect queue
    -> typed request handler
    -> state mutation
    -> follow-up combat events

Recipes only build requests. They do not mutate combat state and do not resolve requests directly.

`TriggeredEffectDefinitionRunner` preserves definition order, evaluates the supplied match predicate once per definition, and queues every produced request exactly once.

## Core contracts

`ICombatValueProvider<TContext, TValue>` resolves a value from a typed context.

`ICombatEffectRecipe<TContext>` builds effect requests from:

- the typed event or command context
- `TriggeredEffectActionBuildContext`

The build context preserves two different concepts:

- `CombatantTargetSelectionContext.Source` is the targeting anchor
- `TriggeredEffectActionSource` is effect attribution

These values must not be treated as interchangeable.

The current build-context name is historical. A neutral rename remains deferred until it can be performed without a broad compatibility change.

## Standard recipes

The current standard recipes are:

- `GainBlockEffectRecipe<TContext>`
- `HealEffectRecipe<TContext>`

They delegate to the shared `TriggeredEffectActionBuilder`.

A standard recipe must not:

- construct primitive requests directly when a shared builder already exists
- know concrete combat events
- mutate combat state
- resolve requests directly
- bypass the existing queue
- introduce an `EffectKind` enum or central recipe switch

Additional primitive recipes should follow the same pattern.

## Value providers

Value providers may be generic or context-specific.

Current reusable examples include:

- `FixedCombatValue<TValue>`
- `RoundEndedCompletedRoundAmount`
- `TurnStartedCombatantStatusStacksAmount`
- `TurnEndedCombatantStatusStacksAmount`

Temporal providers that already served legacy actions may implement both the legacy amount-provider contract and `ICombatValueProvider<TContext, int>`.

Their generic `Resolve` method delegates to the existing calculation so legacy and recipe paths preserve identical semantics.

Event-specific providers are valid because they interpret event-specific data. Effect recipes remain event-agnostic.

## Ordering and compatibility semantics

For every matching recipe-capable definition:

1. filters are evaluated
2. legacy actions execute in list order
3. generic effects execute afterward in list order
4. every produced request is queued exactly once

There is no automatic request deduplication.

Configuring the same behavior in both collections intentionally produces both behaviors.

The two-list compatibility model cannot represent arbitrary interleaving such as:

    legacy action
    generic effect
    legacy action

Definitions with order-dependent entries should therefore be migrated as a whole or only when their relative order is irrelevant.

Legacy wrappers remain available. Their removal is not part of milestone B.

## Temporal targeting semantics

The temporal migration preserves the targeting and attribution rules that existed before recipes:

- `RoundEnded` uses the last active combatant as its available temporal anchor
- `TurnStarted` uses the turn combatant as targeting source and effect source
- `TurnEnded` uses the turn combatant as targeting source and effect source
- `TurnStarted` and `TurnEnded` do not invent an `EventTargetId`
- a temporal handler executes neither actions nor recipes when its required combatant context is missing

Recipes reuse each trigger family's existing target resolver. They do not reinterpret event data.

## Lifecycle ordering

Recipe support does not change lifecycle-handler order.

Validated temporal rules include:

- `TurnStarted` triggered effects remain in their existing position relative to standard turn-start automation
- `TurnEnded` processing remains hand discard, then triggered effects, then timed-status duration decrease
- round transition processing remains owned by the existing lifecycle system

The recipe layer changes how requests are declared, not when lifecycle events occur.

## Completed milestone B: Temporal Trigger Recipe Migration

Milestone B completed:

1. generic recipe and value-provider contracts
2. `GainBlockEffectRecipe<TContext>`
3. `HealEffectRecipe<TContext>`
4. the `RoundStarted` pilot
5. reusable temporal value-provider adapters
6. additive recipe support for `RoundEnded`
7. additive recipe support for `TurnStarted`
8. additive recipe support for `TurnEnded`
9. ordering, targeting, compatibility, missing-context, and lifecycle tests
10. preservation of the existing queues, resolver, registry, primitive handlers, and public legacy APIs

## Deferred architecture work

Milestone B intentionally does not solve:

- effect-chain causality metadata
- explicit gameplay reentry policies
- source-free target selection
- definition build, validation, and freeze phases
- a neutral rename of `TriggeredEffectActionBuildContext`
- cards, enemy actions, and abilities using recipes
- removal of legacy trigger-specific wrappers
- arbitrary interleaving of legacy actions and generic effects

Potentially recursive combinations remain restricted until explicit reentry semantics exist.

Examples include:

- healed -> heal
- resource gained -> gain resource
- card created -> create card
- status applied -> apply status
- damage received -> deal damage

## Next bounded milestone: Section C

Section C must begin with a fresh audit of the reactive trigger families before any code is changed.

Candidate families include:

- `DamageDealt`
- `DamageReceived`
- `Healed`
- `StatusApplied`
- status lifecycle triggers
- `ResourceGained`
- card lifecycle and cost-paid triggers

The audit must classify each candidate by:

- available typed context
- existing filters and value providers
- source and target semantics
- lifecycle ordering
- follow-up events
- self-trigger and cross-trigger recursion risk
- whether existing primitive recipes are sufficient
- whether a new primitive recipe is genuinely required

Section C should migrate one bounded family at a time.

It must preserve legacy actions, reuse the shared runner and builders, and add explicit reentry tests before enabling any self-triggering combination.

Section C must not begin with:

- broad removal of legacy wrappers
- a central recipe switch
- card-effect-definition migration
- enemy-command migration
- source-free targeting
- recursive trigger combinations without an explicit policy
