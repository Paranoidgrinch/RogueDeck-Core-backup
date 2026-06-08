# RogueDeck Core Architecture

This document describes the current architecture rules for the RogueDeck Core engine.

The core goal is to build a deterministic, modular, UI-independent engine for roguelike deckbuilding games.

## Current scope

The current implementation focuses on the combat core.

The combat core is responsible for:

- representing the current combat state
- representing combatants
- representing statuses
- resolving effect requests
- processing pending effects deterministically
- registering definitions and handlers through packages

The core is not responsible for UI, animations, input handling, rendering, save files, or Godot integration.

## Core concepts

### CombatState

CombatState represents one running combat.

It owns the current combat data, such as:

- combatants
- current round
- current turn
- active combatant
- combat result
- pending effects
- pending events
- combat log
- deterministic seed and random step data

CombatState should describe state. It should not become a giant rules engine.

### CombatantState

CombatantState represents one fighting unit.

A combatant can be a player, enemy, boss, summon, minion, companion, pet, construct, illusion, or any other combat participant.

Important rule:

Do not add hardcoded status fields such as PoisonStacks, WeakTurns, IsStunned, Strength, or Thorns.

Statuses belong into StatusInstance, not directly into CombatantState.

### StatusDefinition

StatusDefinition describes the blueprint of a status.

Examples:

- poison
- weak
- strength
- thorns
- stun

A status definition answers questions like:

- What is this status called?
- Is it a buff or debuff?
- Does it use stacks?
- Does it use duration?
- How does it stack?
- Which tags does it have?

### StatusInstance

StatusInstance represents one active status on a combatant.

It stores runtime data such as:

- owner combatant
- source combatant
- source card
- stacks
- duration
- charges
- applied round
- applied turn

## Effect system

The effect system is built around effect requests and effect handlers.

### IEffectRequest

An IEffectRequest describes what should happen.

Examples:

- deal damage
- heal
- gain block
- clear a defensive pool
- apply a status

A request should be data only. It should not contain game logic.

### IEffectRequestHandler

An IEffectRequestHandler executes one kind of effect request.

Handlers contain the actual mutation logic.

Example mappings:

- DealDamageEffectRequest is handled by DealDamageEffectHandler
- HealEffectRequest is handled by HealEffectHandler
- ApplyStatusEffectRequest is handled by ApplyStatusEffectHandler

### EffectRequestHandler<TRequest>

EffectRequestHandler<TRequest> is the generic base class for effect handlers.

It centralizes the cast from IEffectRequest to the concrete request type.

Concrete handlers should usually inherit from this class instead of implementing IEffectRequestHandler directly.

## CombatEffectResolver rule

CombatEffectResolver must stay generic.

It must not contain a switch or if-chain for concrete effects.

Allowed pattern:

- get the handler from CombatDefinitionRegistry
- call handler.Resolve(...)
- do not inspect concrete request types

Not allowed pattern:

- switch on concrete effect request types
- if request is DealDamageEffectRequest
- if request is HealEffectRequest
- if request is ApplyStatusEffectRequest

Adding a new effect must not require changing CombatEffectResolver.

## Adding new effects

New standard effects should be added like this:

1. Add the request type to the appropriate grouped effect file.
2. Add the matching handler to the same grouped effect file.
3. Register the handler in StandardCombatPackage.
4. Add tests for the new effect.
5. Run dotnet test.

The resolver must remain unchanged.

## Effect file organization

Do not create one file for every tiny request and handler.

Prefer grouped files.

Current intended grouping:

- StatusEffects.cs
- DamageEffects.cs
- DefensivePoolEffects.cs

Future examples:

- CardEffects.cs
- ResourceEffects.cs
- SummonEffects.cs
- TurnEffects.cs

A new file should only be created when a new group of related effects appears.

## CombatDefinitionRegistry

CombatDefinitionRegistry stores registered definitions and handlers.

Currently it is responsible for:

- status definitions
- effect request handlers

Packages use the registry to register their content.

## ICombatPackage

ICombatPackage is the extension point for modular content.

A package can register:

- status definitions
- effect handlers
- later: card definitions, enemy definitions, relic definitions, and more

The standard package is StandardCombatPackage.

## StandardCombatPackage

StandardCombatPackage registers the standard combat content.

It should contain basic reusable combat building blocks.

Examples:

- standard statuses
- standard damage effects
- standard healing effects
- standard defensive pool effects

It should not become a game-specific content dump.

## Pending effects

Effects can be queued on CombatState.

The queue is processed by CombatEffectQueueProcessor.

This allows deterministic effect chains:

1. enqueue effect
2. process queue
3. handler may enqueue follow-up effects
4. queue continues until empty

This is important for later systems such as:

- thorns
- poison ticks
- card effects
- triggered effects
- enemy actions
- status reactions

## Determinism

The core should be deterministic.

Given the same initial state, same registered definitions, same player decisions, and same seed, the result should be reproducible.

Randomness should not be hidden inside arbitrary handlers without going through a deterministic random mechanism.

A proper deterministic random service is still a future task.

## Phase 1 status

Phase 1 is the combat foundation phase.

Phase 1 includes:

- combat state model
- combatant state model
- status definitions
- status instances
- package and registry system
- modular effect resolver
- effect handler registry
- standard combat package
- first grouped standard effects
- pending effect queue processor
- tests for the above systems

Phase 1 does not include:

- cards
- deck, hand, discard piles
- energy
- turn lifecycle
- status trigger lifecycle
- enemy intents
- run state
- rewards
- map
- save and load
- UI

## Phase 1 completion rule

Phase 1 is considered complete when:

- dotnet test is green
- CombatEffectResolver contains no concrete effect logic
- standard effects are grouped into readable files
- new effects can be added without changing the resolver
- the architecture rules are documented here

## Next phase

Phase 2 should introduce a minimal turn lifecycle.

The next phase should still avoid hardcoding concrete statuses into central systems.

Bad direction:

- if poison then do poison logic
- if stun then skip turn logic
- if weak then reduce damage logic

Better direction:

- phase or event happens
- registered rules or triggers produce effect requests
- effect queue resolves them

## Phase 2: Turn and event lifecycle

Phase 2 introduces the first neutral combat lifecycle.

The goal is not to hardcode concrete status rules into the turn processor. The goal is to make the combat lifecycle observable and extensible.

### Phase 2 core rules

CombatTurnProcessor is responsible for neutral turn flow only.

It may:

- start the current turn
- end the current turn
- move to the next living combatant
- advance the round when the turn order wraps
- emit lifecycle events
- process pending queues through CombatQueueProcessor

It must not contain concrete status logic such as poison, stun, weak, thorns, block, or any other specific mechanic.

### Turn lifecycle

The current turn lifecycle is:

- StartCurrentTurn
- process TurnStartedCombatEvent
- process effects created by event handlers
- EndCurrentTurn
- process TurnEndedCombatEvent
- process effects created by event handlers
- advance to the next living combatant
- advance round if the turn order wraps

A turn can only start when the combat is ongoing and the active combatant is alive.

Ending a turn is allowed even if the active combatant became non-living during its own turn. This supports future mechanics such as sacrifice, self-damage, thorns, or other reactive damage.

### Round lifecycle

When turn order wraps from the last living combatant back to the first living combatant, the round advances.

Round lifecycle events make this observable:

- RoundEndedCombatEvent
- RoundStartedCombatEvent

These events allow future systems to react to round changes without adding special-case logic to CombatTurnProcessor.

### Combat events

Combat events describe things that happened.

Examples include:

- TurnStartedCombatEvent
- TurnEndedCombatEvent
- RoundStartedCombatEvent
- RoundEndedCombatEvent
- DamageDealtCombatEvent
- HealedCombatEvent
- StatusAppliedCombatEvent
- StatusMergedCombatEvent
- StatusExpiredCombatEvent
- CombatantLifecycleChangedCombatEvent

Events should usually not mutate combat state directly.

Instead, event handlers may enqueue effect requests.

### Combat event handlers

CombatEventHandler<TEvent> is the modular reaction point for events.

Examples:

- a turn-start handler can clear standard block
- a turn-start handler can apply damage-over-time
- a turn-end handler can decrease timed status durations
- a damage-dealt handler can trigger reflected damage
- a damage-dealt handler can mark a combatant downed at zero health
- a lifecycle-changed handler can update the standard combat result

The event system should prevent central systems from growing large if-chains.

Bad direction:

- CombatTurnProcessor checks poison
- CombatTurnProcessor checks stun
- DealDamageEffectHandler checks thorns
- DealDamageEffectHandler decides victory

Good direction:

- lifecycle or damage event happens
- registered event handlers react
- event handlers enqueue effect requests
- effect queue mutates the state
- follow-up events can be processed deterministically

### CombatQueueProcessor

CombatQueueProcessor coordinates effect and event processing.

It processes pending queues until they are empty or until combat is no longer ongoing.

The intended flow is:

- resolve pending effects
- resolve pending events
- repeat while new effects or events were created
- stop when queues are empty
- stop when combat result is no longer Ongoing

This allows chains such as:

- damage effect
- damage event
- lifecycle event
- combat result effect

without hardcoding these chains into a central resolver.

### Standard lifecycle handlers

The standard package may register reusable standard lifecycle handlers.

Examples:

- clear standard block on turn start
- apply damage-over-time on turn start
- decrease timed status durations on turn end
- trigger reflected damage from triggered-damage statuses
- mark combatants downed at zero health
- update standard combat result from lifecycle changes

These are standard rules, not universal engine laws.

Other games or packages may register different handlers.

### Combat result

Combat result should not be decided directly by damage.

The standard flow is:

- damage reduces health
- damage emits DamageDealtCombatEvent
- zero-health handler marks combatant Downed
- lifecycle change emits CombatantLifecycleChangedCombatEvent
- standard result handler checks living player and enemy teams
- result effect sets Victory, Defeat, Draw, or leaves combat Ongoing

This keeps damage, lifecycle, and win-condition logic separate.

### Phase 2 completion direction

Phase 2 is complete when the combat lifecycle can:

- start and end turns safely
- advance between living combatants
- advance rounds
- emit lifecycle events
- process event handlers
- process effect chains deterministically
- stop processing after combat ends
- keep concrete mechanics out of central processors

Phase 2 should still not introduce cards, deck zones, energy, enemy AI, map state, rewards, or run progression.

Those belong to later phases.
