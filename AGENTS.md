# RogueDeck-Core Codex Instructions

## Project context

This repository is a reusable C# core engine for roguelike deckbuilding games.

The current priority is to build a generic, modular combat engine. Avoid game-specific hardcoding unless explicitly requested.

All player-facing game content and technical identifiers must be in English.

## Architecture rules

* Do not add concrete card/status/relic/effect cases to a central resolver when a modular registry/handler/definition approach is possible.
* New combat effects should live under `src/RogueDeck.Core/Combat/Effects` unless the existing structure clearly indicates a better location.
* Prefer schema/definition-friendly internal abstractions, but do not jump to JSON/schema serialization until the internal Trigger/Target/Condition/Amount/Action language is stable.
* Keep existing public APIs compatible where practical. If compatibility must be broken, explain why.
* Avoid broad rewrites. Prefer small, well-tested, modular refactor steps.
* Before modifying code, inspect the actual local files and existing tests. Do not guess names, signatures, enum values, constructors, file paths, or behavior.
* Use existing naming conventions and patterns from the repository.

## Validation rules

* Always run `dotnet test` after code changes.
* Add or update tests for every behavior change.
* If a test needs a combatant with a defensive pool, inspect how existing tests create that state instead of assuming it exists.
* Do not read dictionary keys directly in tests unless the setup guarantees the key exists.
* When unsure about an enum value, constructor, record property, or helper method, search the repo first.

## Git workflow

* Show `git status --short --branch` before starting substantial work.
* Keep diffs focused.
* Do not commit or push unless explicitly asked.
* Before suggesting a commit, run `dotnet test` and show the diff summary.

## Communication style

* Explain what changed and why.
* Mention files modified.
* Mention tests run and whether they passed.
* Flag uncertainty instead of guessing.
