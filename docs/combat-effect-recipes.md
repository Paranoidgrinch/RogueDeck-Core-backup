# Combat Effect Recipes

## Status

The generic combat-effect recipe architecture completed its first pilot with `RoundStartedCombatEvent`.

The pilot is additive:

- existing trigger-specific `Actions` remain supported
- generic `Effects` are available alongside them
- no existing public trigger API was removed
- the effect queue, event queue, resolver, registry, and primitive request handlers remain unchanged

## Purpose

Trigger-specific action wrappers create an event-by-effect matrix.

Without recipes, each combination may require another type:

- gain block when a turn starts
- gain block when a round starts
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
    -> generic effect recipes
    -> IEffectRequest
    -> existing effect queue
    -> typed request handler
    -> state mutation
    -> follow-up combat events

Recipes only build requests. They do not mutate combat state and do not resolve requests directly.

## Core contracts

`ICombatValueProvider<TContext, TValue>` resolves a value from a typed context.

`ICombatEffectRecipe<TContext>` builds effect requests from:

- the typed event or command context
- `TriggeredEffectActionBuildContext`

The build context preserves two different concepts:

- `CombatantTargetSelectionContext.Source` is the targeting anchor
- `TriggeredEffectActionSource` is effect attribution

These values must not be treated as interchangeable.

## Value providers

Value providers may be generic or context-specific.

Examples:

- `FixedCombatValue<int>`
- current round
- damage received
- cards drawn
- resource gained
- removed status count

Event-specific value providers are valid because they interpret event-specific data. Effect recipes remain event-agnostic.

## Standard recipes

The first standard recipe is:

    GainBlockEffectRecipe<TContext>

It delegates to `TriggeredEffectActionBuilder.BuildGainBlockRequests`.

It must not:

- construct `GainBlockEffectRequest` directly
- know concrete combat events
- contain a central effect switch
- bypass the existing queue

Additional standard recipes should follow the same pattern.

## RoundStarted pilot semantics

`RoundStartedTriggeredEffectDefinition` currently has two ordered collections:

1. legacy `Actions`
2. generic `Effects`

For every matching definition:

- filters are evaluated once
- legacy actions execute in list order
- generic effects execute afterward in list order
- every produced request is queued exactly once

There is no automatic request deduplication.

Configuring the same behavior in both collections intentionally produces both behaviors.

The two-list compatibility model cannot represent arbitrary interleaving such as:

    legacy action
    generic effect
    legacy action

Definitions with order-dependent entries should therefore be migrated as a whole or only when their relative order is irrelevant.

## Compatibility rules

During migration:

- legacy actions remain available
- no broad rename or deletion is performed
- existing tests remain green
- one trigger family is migrated at a time
- existing target selectors, builders, factories, requests, and queues are reused
- no `EffectKind` enum or central recipe switch is introduced

## Deferred architecture work

The pilot intentionally does not solve:

- effect-chain causality metadata
- gameplay reentry policies
- source-free target selection
- definition build, validation, and freeze phases
- a neutral rename of `TriggeredEffectActionBuildContext`
- cards, enemy actions, and abilities using recipes
- removal of legacy trigger-specific wrappers

Potentially recursive combinations remain restricted until explicit reentry semantics exist.

Examples include:

- healed -> heal
- resource gained -> gain resource
- card created -> create card
- status applied -> apply status
- damage received -> deal damage

## Next bounded milestone

The recommended next milestone is:

    Temporal Trigger Recipe Migration

Its intended scope is:

1. re-audit `RoundEnded`, `TurnStarted`, and `TurnEnded`
2. add `HealEffectRecipe<TContext>` with isolated tests
3. adapt reusable temporal value providers to the generic provider contract
4. migrate `RoundEnded` while preserving legacy actions
5. migrate `TurnStarted` and `TurnEnded` without changing lifecycle processing
6. validate ordering, compatibility, and wrapper reduction
7. document the result and plan the next trigger group

The next milestone must not yet migrate:

- card triggers
- damage triggers
- status triggers
- resource triggers
- enemy commands
- card effect definitions
- reentry-sensitive self-trigger combinations
