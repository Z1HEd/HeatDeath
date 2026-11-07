# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

HeatDeath is a 2D roguelike space combat game built in Unity 6 (`6000.4.1f1`) with URP, the Input System, and UI Toolkit. It has one scene, `Assets/Scenes/SampleScene.unity`.

The code and assets are the source of truth. `README.md` describes the intended game and `Assets/ToDo.md` is out of date, so verify against the project before relying on either. `Assets/Docs.docx` holds the design notes.

Current state: 5 ships, 7 weapons, and one playable sector, Outer Belt. Iron Reach and Storm Verge have sprites only. Run saving, run statistics, weapon evolution, a main menu, and a game-over flow are not implemented yet. The 4 existing upgrades are placeholders for testing; the upgrade system is work in progress.

Terminology: "scrap" is the in-game name for XP, not a separate currency. The planned 21 weapons are the 7 base weapons plus 2 evolution paths each.

## Rules

Do not edit Unity assets (prefabs, scenes, ScriptableObject `.asset` files, materials, `.meta` files, and similar) without explicit permission given at that moment. Permission from an earlier request does not carry over. If a code change needs an asset change, describe what is needed and ask. Once told to make asset changes, make all of them in one go without asking again for each file.

The user reviews every code change in batches. Split big refactors into separate logical batches, ideally no more than 100 changed lines each. Work through a whole batch without stopping, then stop editing until the user approves it.

Never commit unless the user explicitly asks for it.

## Commands

`dotnet` is not installed and `Assembly-CSharp.csproj` is generated and gitignored, so compile through Unity. Headless Unity refuses to run while the project is open in the editor.

```bash
# Compile check: exits non-zero on failure; look for "error CS" in the log
~/Unity/Hub/Editor/6000.4.1f1/Editor/Unity -batchmode -nographics -quit -projectPath . -logFile /tmp/unity.log
```

There are no tests yet and no `.asmdef` files; all scripts compile into `Assembly-CSharp`. `com.unity.test-framework` is installed, so tests would need a test assembly first. Then run them with:

```bash
~/Unity/Hub/Editor/6000.4.1f1/Editor/Unity -batchmode -nographics -projectPath . -runTests -testPlatform EditMode -testResults /tmp/results.xml -testFilter <TestName>
```

Gameplay can only be checked by running the scene in the editor.

## Architecture

### Ships are assembled from modules

`Ship` (subclassed by `Player` and `Enemy`) requires three manager components: `ModuleManager`, `EffectManager`, and `UpgradeManager`. All behaviour lives in `ModuleBase` subclasses (core, weapons, movement, abilities) found on the ship or its children. Modules register with `ModuleManager` in `Start`.

`ShipCoreModule` holds health and shields and is also the `IHitter` used for ramming. All damage goes through `IHitter` → `Ship.Hit` (`IHittable`) → `ShipCoreModule.ApplyDamage`.

### Stats, effects, and upgrades

This pipeline spans most of the code:

1. An `UpgradeDefinition` holds a list of `Effect`s. `UpgradeManager` clones them with `Effect.Stacked(stacks)` and replaces the previous clones whenever the stack count changes.
2. `EffectManager` owns the active effects, ticks their durations, and sets `shouldRecalculate` when the list changes. The recalculation runs once in its `Update`, not immediately.
3. Each module clears its modifiers, applies every effect where `IsApplicableTo(moduleDefinition)` is true (matched by `TagDefinition`), recalculates every stat listed in its `CollectStats`, then calls `OnStatsChanged`.

Stats (`ResourceStat`, `ScalarStat`, `BoolStat` in `Assets/Scripts/Modules/Stats.cs`) resolve to `(base + Flat) * (1 + Percent/100)`. Stacks of one upgrade add up (+25% × 4 = +100%), and percents from different sources multiply (×2 and ×2 give ×4), so -100% always wins. `BoolStat` is true if `Flat > 0`, false if `Flat < 0`, otherwise its base value.

To add a stat:
- Append to `StatType`. Its values are serialized as integers, so never renumber them.
- Pass the type to the stat's constructor.
- Add the stat to the module's `CollectStats` override. A stat missing from it silently ignores effects.

On-hit effects arrive through `Ship.ApplyEffects`, which gives each target its own copy and uses `EffectManager.AddStatusEffect`: effects in the same slot (`SharesSlotWith`) don't stack, and the stronger or equal one replaces the old one and restarts it. Modules hand out effects through `ModuleBase.EmitEffects`, which scales their durations by the module's `Duration` modifier.

To add an `Effect`, implement `Stacked` so it returns a new instance. Definition assets are shared, so effects must not be mutated at runtime.

### Data

Content is defined by ScriptableObjects (`Assets/Scripts/Data`, created under the `HeatDeath/` asset menu). How each type is found differs:

- **Ships, weapons, upgrades:** loaded by `Resources.LoadAll` from `Assets/Resources/{Ships,Weapons,Upgrades}` in the draft services. Adding an asset there adds it to the game.
- **Sectors:** not loaded from `Resources`. They must be added to the `sectors` list on `EnemySpawnController` in the scene.
- **Tags:** `Assets/Resources/ModuleTags`, referenced from module definitions and effects. A `StatModifierEffect` with no `targetTags` applies to nothing.

`Effect` lists use `[SerializeReference]` with `[SubclassSelector]` from the MackySoft package. Assets store the concrete class name, so renaming or moving an `Effect` subclass breaks them unless you add `[MovedFrom]`.

### Run flow

`GameController` (XP, levels), `EnemySpawnController` (sector and wave progression, then endless mode), and `GameUIController` are singletons, with `GameUIController` instantiated from the `GameUI` prefab by `GameController.Start`.

No player exists at scene start. The ship is picked in a draft, and `GameUIController` then calls `GameController.SpawnPlayer`, so code must not assume a player is present.

`GameController.OnLevelChanged` drives `GameUIController.StartDraftSequence`, which sets `Time.timeScale = 0` while drafts are shown. Levels in `weaponDraftLevels` give a weapon draft; other levels give an upgrade draft. `Time.deltaTime`-based code therefore stops during drafts and pause.

### Prefab conventions the code assumes

- Ship prefabs need a child named `TurretMounts`; `ModuleManager.AddWeapon` fills the first empty mount.
- A weapon prefab's first child is its fire point (`transform.GetChild(0)`).
- Player prefabs need an `AbilityModule` on the root, because `Player.Update` reads it without a null check. Space activates it.
- `WeaponModule` sets its own layer from the owning ship: `DetectEnemy` on the player's weapons, `DetectPlayer` on enemies'. `RangeDetector` targeting depends on these layers.
- `WeaponModule`, `ProjectileModule`, and `TeslaModule` are `[ExecuteAlways]`, so they run in edit mode. Each class declares the attribute itself; `MissileModule` and `MineModule` don't.

Keep each `.meta` file with its asset when moving or renaming, since prefabs and assets reference each other by the GUID stored there.
