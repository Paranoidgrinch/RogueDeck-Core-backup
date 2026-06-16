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
- `DamageDealtTriggeredEffectDefinition`
- `DamageReceivedTriggeredEffectDefinition`
- `StatusAppliedTriggeredEffectDefinition`
- `StatusMergedTriggeredEffectDefinition`
- `StatusApplicationBlockedTriggeredEffectDefinition`
- `StatusChargesReducedTriggeredEffectDefinition`
- `StatusesRemovedByPolarityTriggeredEffectDefinition`
- `StatusExpiredTriggeredEffectDefinition`
- `StatusRemovedTriggeredEffectDefinition`
- `CardInstanceCreatedTriggeredEffectDefinition`
- `CombatantDownedTriggeredEffectDefinition`
- `CardPlayedTriggeredEffectDefinition`
- `CardsDrawnTriggeredEffectDefinition`
- `DiscardPileShuffledTriggeredEffectDefinition`
- `HandDiscardedTriggeredEffectDefinition`
- `CardExhaustedTriggeredEffectDefinition`
- `CardBanishedTriggeredEffectDefinition`

Every currently implemented `ITriggeredEffectDefinition` now supports generic `Effects` alongside legacy `Actions`. There is no "not yet migrated" trigger-definition list anymore; remaining work is the broader deferred items below (card-effect definitions, enemy commands, the guard-parity gap, etc.), not additional trigger families.

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
- `FixedDamageDealtTriggeredEffectAmount`, `DamageDealtHealthDamageAmount`, `DamageDealtBlockedDamageAmount`, `DamageDealtRequestedAmount`, `DamageDealtSourceStatusStacksAmount`, `DamageDealtTargetStatusStacksAmount`
- `FixedDamageReceivedTriggeredEffectAmount`, `DamageReceivedHealthDamageAmount`, `DamageReceivedBlockedDamageAmount`, `DamageReceivedRequestedAmount`, `DamageReceivedReceiverStatusStacksAmount`, `DamageReceivedSourceStatusStacksAmount`
- `FixedStatusAppliedTriggeredEffectAmount`, `StatusAppliedStacksAmount`, `StatusAppliedDurationTurnsAmount`, `StatusAppliedChargesAmount`, `StatusAppliedTargetStatusStacksAmount`, `StatusAppliedSourceStatusStacksAmount`
- `FixedStatusMergedTriggeredEffectAmount`, `StatusMergedStacksAmount`, `StatusMergedDurationTurnsAmount`, `StatusMergedChargesAmount`, `StatusMergedTargetStatusStacksAmount`, `StatusMergedSourceStatusStacksAmount`
- `FixedStatusApplicationBlockedTriggeredEffectAmount`
- `FixedStatusChargesReducedTriggeredEffectAmount`, `StatusChargesReducedChargeCountAmount`
- `FixedStatusesRemovedByPolarityTriggeredEffectAmount`, `StatusesRemovedByPolarityRemovedStatusCount`
- `FixedStatusExpiredTriggeredEffectAmount`, `StatusExpiredTargetStatusStacksAmount`, `StatusExpiredSourceStatusStacksAmount`
- `FixedStatusRemovedTriggeredEffectAmount`, `StatusRemovedCountAmount`, `StatusRemovedTargetStatusStacksAmount`, `StatusRemovedSourceStatusStacksAmount`
- `FixedCardInstanceCreatedTriggeredEffectAmount`, `CardInstanceCreatedCount`
- `FixedCombatantDownedTriggeredEffectAmount`
- `FixedCardPlayedTriggeredEffectAmount`, `CardPlayedSourceStatusStacksAmount`
- `FixedCardsDrawnTriggeredEffectAmount`, `CardsDrawnCardCountAmount`
- `FixedDiscardPileShuffledTriggeredEffectAmount`, `DiscardPileShuffledCardCountAmount`
- `FixedHandDiscardedTriggeredEffectAmount`, `HandDiscardedCardCountAmount`
- `FixedCardExhaustedTriggeredEffectAmount`, `CardExhaustedCardCountAmount`
- `FixedCardBanishedTriggeredEffectAmount`, `CardBanishedCardCountAmount`

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

### DamageDealt / DamageReceived

- the damage source is the targeting source when present; the damaged combatant is the fallback
- the damaged combatant is the event target
- effect attribution uses the original damage source combatant and source card
- `DamageDealtTriggeredEffectContext` exposes the dealing side's view (`TargetCombatant` = damaged combatant); `DamageReceivedTriggeredEffectContext` exposes the receiving side's view (`ReceiverCombatant` = damaged combatant). Semantics are otherwise identical.

### StatusApplied / StatusMerged

- the status source combatant is the targeting source when present; the affected combatant is the fallback
- the affected combatant is the event target
- effect attribution uses the status's source combatant and source card

### StatusApplicationBlocked

- the combatant that blocked the application is both the targeting source and the event target
- effect attribution uses that same combatant as source; no source card is invented
- the blocked status definition and the blocking status definition are available to filters, but are not used for targeting

### StatusesRemovedByPolarity

- the affected combatant is both the targeting source and the event target
- effect attribution uses `TriggeredEffectActionSource.None`; no source combatant or source card is invented

### StatusRemoved / StatusChargesReduced / StatusExpired

- these events carry no source-combatant data at all (`StatusRemovedCombatEvent`, `StatusChargesReducedCombatEvent`, and `StatusExpiredCombatEvent` have no `SourceCombatantId` field); the handler always passes `SourceCombatant: null`
- the targeting source therefore always falls back to the affected combatant
- effect attribution uses an empty `TriggeredEffectActionSource`

**Known gap:** because the targeting source always falls back to the affected combatant for these three triggers, a `SourceCombatantTargetSelector` recipe would resolve to the affected combatant rather than to nothing. The legacy `Actions` path guards against this explicitly (`ShouldSuppressMissingSourceTarget`, suppressing the action when there is no real source and the selector is `SourceCombatantTargetSelector`). The generic `Effects` recipe path does **not** apply this guard yet — a recipe using `SourceCombatantTargetSelector` on one of these three triggers will silently target the affected combatant instead of producing no requests. This parity gap is unresolved and tracked under deferred work.

### CardInstanceCreated

- the card owner is both the targeting source and the event target
- effect attribution uses the card owner as source combatant; no source card is invented

### CombatantDowned

- the downed combatant is both the targeting source and the event target
- effect attribution uses `TriggeredEffectActionSource.None`; no source combatant or source card is invented
- the downed combatant is not alive at trigger time, so a target selector that resolves only alive combatants (`SourceCombatantTargetSelector`, `EventTargetCombatantTargetSelector`) resolves to nothing for this trigger; recipes targeting the downed combatant's side typically use `AllAliveCombatants` or `AllEnemiesOfSource`/`AllAlliesOfSource` instead

**Known interaction:** downing the last living enemy (or the last living player-side combatant) changes `CombatResult` away from `Ongoing` in the same processing pass that resolves the `CombatantDowned` chain. Queue processing stops once the result is no longer `Ongoing`, so an effect chain started by a `CombatantDowned` recipe (e.g. `Heal -> Healed -> ...`) only completes if the trigger fires for a combatant that is not the last one on its side. This is existing queue-processor behavior, not something introduced by this migration, but it is easy to trip over when writing `CombatantDowned` recipes or tests.

### CardPlayed

- the playing combatant is both the targeting source and the event target's fallback (`EventTargetId` is the played card's target combatant, if any)
- effect attribution uses the playing combatant as source combatant, and the **played card's own definition ID** as source card; no other trigger family attributes a triggered effect to the card that caused it this way
- `CardPlayedTriggeredEffectTarget` (`Source`, `PlayedCardTarget`, `AllAllies`, `AllEnemies`) is a convenience enum predating the generic recipe work; `CardPlayedTriggeredEffectTargetResolver.ToSelector` converts it to an `ICombatantTargetSelector` for use with both legacy actions and generic recipes

**Behavior change in this migration:** unlike every other trigger family, `CardPlayedTriggeredEffectHandler` previously enqueued its legacy `Actions`' effect requests directly (`combat.EnqueueEffect(effectRequest)`), bypassing `TriggeredEffectDefinitionRunner` entirely. This meant `CardPlayed` triggered effects had no reentry suppression and no trigger-depth limit, unlike every other triggered-effect family. The migration switched the handler to route both `Actions` and the new `Effects` through `TriggeredEffectDefinitionRunner.EnqueueEffectRequests`, so `CardPlayed` now gets the same `SuppressRecursiveReentry` default and maximum-trigger-depth protection as every other trigger. No existing test exercised recursive `CardPlayed` reentry (there is no production effect-request path that re-enters `CombatCardPlayProcessor.PlayCard`), so this closes a latent gap rather than fixing an observed bug.

### CardsDrawn / DiscardPileShuffled / HandDiscarded / CardExhausted / CardBanished

All five share `CardLifecycleTriggeredEffectSupport`, an internal helper that resolves the affected `CardInstance`/`CardDefinition` data and builds a `TriggeredEffectActionBuildContext`.

- the card-owning combatant is both the targeting source and the event target
- effect attribution uses the card-owning combatant as source combatant; no source card is invented
- `CardExhausted` and `CardBanished` share the same underlying event (`CardMovedToZoneCombatEvent`), filtered by `ToZone` (`ExhaustPile` / `BanishedPile` respectively) in each handler; their `Card`/`CardDefinition` context fields are nullable and `null` when the moved card's data can no longer be resolved

**Known inconsistency:** `GainBlockOnDiscardPileShuffledTriggeredEffectAction` builds its own `TriggeredEffectActionBuildContext` inline instead of calling `CardLifecycleTriggeredEffectSupport.CreateActionBuildContext`, and its inline version omits `EventTargetId`. The other four definitions' legacy actions all go through the shared helper and do set `EventTargetId`. The new `DiscardPileShuffled` `Effects` path always uses the shared helper (for consistency with its own siblings and with every other migrated trigger family), so a recipe using `CombatantTargetSelectors.EventTarget` on `DiscardPileShuffled` resolves a target while the equivalent legacy action does not. This pre-existing inconsistency was found, not introduced, during this migration; `CardLifecycleEffectRecipeMigrationTests.DiscardPileShuffledLegacyActionAndGenericRecipeDisagreeOnEventTargetAttribution` pins the current behavior.

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

### Damage migration

The completed `DamageDealt` / `DamageReceived` migration validated:

- same-definition self-reentry suppression for each trigger independently
- `DamageDealt -> Heal -> Healed` and similar cross-definition chains remain allowed
- the original `DamageReceivedCombatEvent` behavior is unchanged by the `DamageDealt` migration
- source, target, card, kind, and amount semantics are unchanged
- legacy actions still execute before recipes

### Status migration

The completed status-application and status-lifecycle migration covers `StatusApplied`, `StatusMerged`, `StatusApplicationBlocked`, `StatusChargesReduced`, `StatusesRemovedByPolarity`, `StatusExpired`, and `StatusRemoved`. It validated:

- existing targeting and attribution semantics per trigger, including the self-attribution triggers (`StatusApplicationBlocked`, `StatusesRemovedByPolarity`) and the no-source triggers (`StatusRemoved`, `StatusChargesReduced`, `StatusExpired`)
- same-definition self-reentry suppression
- legacy actions still execute before recipes
- it surfaced, but did not fix, the missing-source-target guard parity gap described above

### Card-instance migration

The completed `CardInstanceCreated` migration validated targeting, attribution, and self-reentry suppression following the same pattern as the other reactive triggers.

### Combatant-lifecycle migration

The completed `CombatantDowned` migration validated:

- targeting and attribution (downed combatant as both source and event target, no invented attribution)
- same-definition self-reentry suppression, exercised by a recipe that downs a second combatant of the same team
- a different definition may continue a legitimate downed-chain
- legacy actions still execute before recipes
- it surfaced the `CombatResult`/queue-stop interaction described above; the validating `Heal -> Healed` test downs a non-last enemy specifically to avoid that interaction

### CardPlayed migration

The completed `CardPlayed` migration validated:

- existing targeting, attribution, and filter semantics (including the played-card-as-source-card attribution)
- legacy actions still execute before recipes
- it switched the handler from a bespoke direct-enqueue loop to `TriggeredEffectDefinitionRunner`, giving `CardPlayed` reentry suppression and trigger-depth limiting for the first time; all existing `CardPlayed` tests passed unchanged under the new routing
- a `CardPlayed -> Heal -> Healed` chain completes correctly under the new routing

### Card-lifecycle migration

The completed `CardsDrawn` / `DiscardPileShuffled` / `HandDiscarded` / `CardExhausted` / `CardBanished` migration was the last remaining trigger-definition migration. It validated:

- existing targeting, attribution, and filter semantics for all five definitions
- legacy actions still execute before recipes
- a `CardsDrawn -> Heal -> Healed` chain completes correctly
- missing-card-data safety (`CardExhausted`/`CardBanished` with an unresolvable `CardInstanceId`) does not break generic recipes
- it surfaced, but did not fix, the `DiscardPileShuffled` `EventTargetId` inconsistency described above

With this migration, every currently implemented trigger-definition family supports generic `Effects` recipes. There is no remaining "next trigger family" to migrate.

## Deferred work

The following work remains deferred:

- missing-source-target guard parity between legacy `Actions` and generic `Effects` for `StatusRemoved`, `StatusChargesReduced`, and `StatusExpired`
- `EventTargetId` parity between `DiscardPileShuffled`'s legacy action and its generic recipes
- source-free target selection
- definition build, validation, and freeze phases
- broad card-effect-definition migration
- enemy-command migration
- legacy-wrapper removal
- arbitrary interleaving of actions and recipes
- broader per-chain activation budgets beyond trigger depth

## Next bounded runtime block

All trigger-definition migrations are complete. The next block should pick one of the deferred items above — most likely starting with the two small, low-risk inconsistency fixes (`StatusRemoved`/`StatusChargesReduced`/`StatusExpired` guard parity, and the `DiscardPileShuffled` `EventTargetId` mismatch), since those are narrowly scoped bug fixes rather than new migrations. The larger items (card-effect-definition migration, enemy-command migration, legacy-wrapper removal) are broader architectural work and should get their own dedicated planning pass rather than being treated as "the next bounded block."
