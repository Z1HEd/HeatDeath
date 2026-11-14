# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

HeatDeath is a 2D roguelike space combat game built in Unity 6 (`6000.4.1f1`) with URP, the Input System, and UI Toolkit. It has one scene, `Assets/Scenes/SampleScene.unity`.

The code and assets are the source of truth. `README.md` describes the intended game, so verify against the project before relying on it. `Assets/Docs.docx` holds the design notes. `Assets/ToDo.md` is the user's personal roadmap; it's gitignored, and it should stay current when work it lists is finished.

Balance targets (level curve, enemy scaling, module and upgrade budgets) are in `Docs/Balance.md`. The work in progress is planned in `Docs/Plans/ModulesAndBalance/`: start with its `README.md`, then the brief for your block. `Docs/Plans/` holds temporary plans and is gitignored, so anything that should last goes into `Docs/Balance.md` or this file.

Current state: 5 ships, 7 weapons, 7 ship modules, and one playable sector, Outer Belt. Iron Reach and Storm Verge have sprites only. Run saving, run statistics, core evolution, the new mechanics of evolved weapons, a main menu, and a game-over flow are not implemented yet. The upgrade system (slots, evolution, infinite upgrades) is in place; its content is a Cannon test set and 3 placeholder core upgrades.

Terminology: "scrap" is the in-game name for XP, not a separate currency. The planned 21 weapons are the 7 base weapons plus 2 evolution paths each.

## Rules

Unity assets (prefabs, scenes, ScriptableObject `.asset` files, materials, `.meta` files, and similar) may be edited directly. Give every new asset, prefab, and folder a `.meta` with a fresh unique GUID. The user may have `SampleScene` open in the editor, where saving overwrites scene edits made on disk, so say whenever you change the scene.

The user reviews every code change in batches. Split big refactors into separate logical batches, ideally no more than 100 changed lines each. Work through a whole batch without stopping, then stop editing until the user approves it.

When the user brings up a new idea for later:
1. Note it where future content work will find it: `Docs/Balance.md` for balance and content rules, the matching brief in `Docs/Plans/` for planned work.
2. Check whether the current code supports it.
3. If it doesn't, decide by how it relates to the current task. Do it now if it's small and part of the current work. Add it to the current plan as a batch if it belongs to the current block. Otherwise, add it to the matching later plan and to `Assets/ToDo.md`. Say which you chose.

Git belongs to the user: staged changes are the ones they approved, unstaged ones are up for review. Never stage, unstage, or commit, and ask before any other change to git state. `git mv` stages, so move files without it.

## Commands

`dotnet` is not installed and `Assembly-CSharp.csproj` is generated and gitignored, so compile through Unity. Headless Unity refuses to run while the project is open in the editor.

```bash
# Compile check: exits non-zero on failure; look for "error CS" in the log
~/Unity/Hub/Editor/6000.4.1f1/Editor/Unity -batchmode -nographics -quit -projectPath . -logFile /tmp/unity.log
```

Dev shortcuts work in the editor and in development builds. `Assets/Scripts/Dev/DevShortcuts.cs` installs itself at startup, so the scene doesn't contain it, and logs the key list once:

- F1: gain one level. F2: gain five. Each level opens its draft as usual, but levels gained before the ship draft open none.
- F3: skip the current wave without rewards and start the next one at once. During the pause between waves, it starts the upcoming wave at once.
- F4: toggle the player's invincibility and knockback immunity.
- F5-F10: jump to the start of sector 1-6, removing current enemies without rewards. Enemy scaling then uses that sector's run progress; past the sector list it's endless mode.
- F11: open a ship-module draft at once.
- F12: open a weapon draft at once.

Each cleared wave also logs `[Stats]`: damage dealt per source (damage, DPS, shields and hull, kills) and damage the player took, from `RunStatistics`. Skipped waves don't log.

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

1. An `UpgradeDefinition` holds a list of `Effect`s. Whenever the stack count changes, `UpgradeManager` passes copies made with `Effect.Copy(stacks)` to `EffectManager.Apply`, with the upgrade as the source.
2. `EffectManager` owns the active effects, ticks their durations, and sets `shouldRecalculate` when the list changes. The recalculation runs once in its `Update`, not immediately.
3. Each module clears its modifiers, applies every effect where `IsApplicableTo(moduleDefinition)` is true (matched by `TagDefinition`), recalculates every stat listed in its `CollectStats`, then calls `OnStatsChanged`.

A `StatModifierEffect` can also grow with the player's level: its `perLevel` modifier is added once per level, and `GameController` marks the player's effects for recalculation on every level-up.

Stats (`ResourceStat`, `ScalarStat`, `BoolStat` in `Assets/Scripts/Modules/Stats.cs`) resolve to `(base + Flat) * (1 + Percent/100)`. Stacks of one upgrade add up (+25% × 4 = +100%), and percents from different sources multiply (×2 and ×2 give ×4), so -100% always wins. A percent below -100% counts as -100%. `BoolStat` is true if `Flat > 0`, false if `Flat < 0`, otherwise its base value.

To add a stat:
- Append to `StatType`. Its values are serialized as integers, so never renumber them.
- Pass the type to the stat's constructor.
- Add the stat to the module's `CollectStats` override. A stat missing from it silently ignores effects.

On-hit effects arrive through `Ship.ApplyEffects`, which gives each target its own copy and uses `EffectManager.AddStatusEffect`: effects in the same slot (`SharesSlotWith`) don't stack, and the stronger or equal one replaces the old one and restarts it. Modules hand out effects through `ModuleBase.EmitEffects`, which scales their durations by the module's `Duration` modifier.

To add an `Effect`, override only the hooks it needs; they all default to doing nothing. Effects are handed out only as copies made with `Effect.Copy(times)`. Its default clones the effect, so override the protected `Stacked` only when strength scales with stacks or a multiplier, and `IsAtLeastAsStrongAs` with it. Definition assets are shared, so effects must not be mutated at runtime.

**Effect sources.** Anything that gives a ship effects for as long as it holds them (an upgrade, an ability while active, a ship-module feature) calls `EffectManager.Apply(source, copies)` with itself as the source, and `Remove(source)` to end them. `IsActive(source)` tells whether any are still running. `Apply` makes the source's effects exactly the given copies:
- A copy equal to one the source already has restarts that one without reapplying it. Equal means made from the same asset effect (`Effect.Template`), with the same strength and duration.
- A changed copy replaces the old one.
- The source's copies missing from the list are removed.

So applying again refreshes a timed buff instead of stacking it. Make copies with `Effect.Copy`, which records the template; `EmitEffects` already does. `AddEffect` remains for effects without a source, such as enemy scaling.

**Appearance.** Effects that change how a ship looks or collides contribute through `Effect.ApplyToAppearance`; they don't change renderers or layers in `ApplyToShip`. `ShipAppearance` is rebuilt from all active effects on every recalculation, like stats:
- Tints multiply together.
- The ship is on the Ghost layer while any effect asks for it.
- The newest thruster colour wins. Its old particles fade out on their own.

Overlapping effects therefore never leave a stale colour or layer.

### Upgrade slots and evolution

The rules and numbers are in `Docs/Balance.md` → "Module structure". In gameplay a module is a weapon, a ship module, or the core. The core is everything the base ship comes with, including its thruster and ability.

**Binding.** Every upgrade is bound to one module definition (`boundModule`). It's offered only while that module is installed and fills that module's slots. Its effects may target any tags.
- Each player ship has its own core definition (`Assets/Resources/Modules/<Ship>Core`), whose `baseDefinition` is the shared `ShipCore.asset`. Enemies use the shared core directly.
- Bind a ship-specific upgrade to that ship's core, and an upgrade for every ship to the shared core. `ModuleManager.FindInstalled` matches a definition or one built on it, so `UpgradeManager.GroupOf` maps both to the ship's own core.
- Never bind to a thruster or ability definition: they have no slots of their own.

**Slots and roles.** `UpgradeManager` keeps 3 base slots per module, in pick order, plus the chosen evolution path. `CanTake` is the only rule for what drafts may offer. `UpgradeRole`:
- Base fills one of the 3 slots.
- Evolution fills the 4th slot, and only for its `evolutionPath` once the module evolved along it.
- Infinite takes no slot, ignores `maxStacks`, and appears only after evolution.

Owned upgrades can always stack up to `maxStacks`. `mutuallyExclusiveWith` blocks an upgrade while any listed one is owned, in either direction.

**Evolution.** A module is ready once its 3 slots hold max-stack upgrades and its definition lists `evolutionPaths`. `UpgradeDraftService` then mixes evolution options into upgrade drafts (`UpgradeDraftOption` is an upgrade or a module plus a path). Picking one calls `UpgradeManager.Evolve`:
- it records the path, which opens the 4th slot and the infinite upgrade;
- it calls `ModuleManager.ReplaceModule`, which swaps every instance of the module (including Yamato's copy) for the path's `evolvedPrefab` in the same parent;
- `OnModuleReplaced` lets the HUD weapon button and `DuplicateWeaponAbility` follow the swap.

An evolved prefab keeps the module's definition, so tags and upgrades carry over. Core evolution will swap the whole ship by rebuilding it from definitions and upgrade state. For now `Evolve` stubs it with an error, readiness skips the core, and cores have no paths.

**Validation.** `UpgradeValidation` (editor only) checks bindings, roles, paths, evolved prefabs, and ship cores. It runs from `HeatDeath/Validate Upgrades` and whenever drafts load in the editor.

### Data

Content is defined by ScriptableObjects (`Assets/Scripts/Data`, created under the `HeatDeath/` asset menu). How each type is found differs:

- **Ships, weapons, ship modules, upgrades:** loaded by `Resources.LoadAll` from `Assets/Resources/{Ships,Weapons,ShipModules,Upgrades}` in the draft services. Adding an asset there adds it to the game.
- **Evolution paths:** `Assets/Resources/EvolutionPaths`, listed on the module definition they evolve. Evolved prefabs are in `Assets/Prefabs/Modules/Weapons/Evolved`.
- **Sectors:** not loaded from `Resources`. They must be added to the `sectors` list on `EnemySpawnController` in the scene. Every sector needs 600 base scrap (within 2%), 8-10 waves, and a single wave holding an `isBoss` entry in its highest `waveOrder` group; `SectorDefinition` warns otherwise. Its totals don't refresh when a wave asset changes, so use its "Recalculate totals" context menu then.
- **Tags:** `Assets/Resources/ModuleTags`, referenced from module definitions and effects. A `StatModifierEffect` with no `targetTags` applies to nothing.

`Effect` lists use `[SerializeReference]` with `[SubclassSelector]` from the MackySoft package. Assets store the concrete class name, so renaming or moving an `Effect` subclass breaks them unless you add `[MovedFrom]`.

### Run flow

`GameController` (XP, levels), `EnemySpawnController` (sector and wave progression, then endless mode), and `GameUIController` are singletons, with `GameUIController` instantiated from the `GameUI` prefab by `GameController.Start`.

No player exists at scene start. The ship is picked in a draft, and `GameUIController` then calls `GameController.SpawnPlayer`, so code must not assume a player is present.

`GameController.OnLevelChanged` drives `GameUIController.StartDraftSequence`, which sets `Time.timeScale = 0` while drafts are shown. Levels in `weaponDraftLevels` (10, 30, 45) give a weapon draft, levels in `shipModuleDraftLevels` (5, 20, 35, 50) a ship-module draft, and other levels an upgrade draft. Pending drafts show in the order ship, weapon, ship module, upgrade. A weapon or ship-module draft with no candidates becomes an upgrade draft. Evolutions are options inside upgrade drafts, not a draft of their own. `Time.deltaTime`-based code therefore stops during drafts and pause.

A level costs `round(baseXPPerLevel + xpIncreasePerLevel × (level − 1))` scrap: 20, 21, 22, 24, and so on. `GameController.AddXP` multiplies all scrap by the player's ShipCore `XPGain` stat (base 1).

Enemy scaling follows run progress, not level or XP. `EnemySpawnController.StartWave` computes p = sectors started − 1 + wave index ÷ waves in the sector, once per wave. Each enemy spawned in that wave gets `enemyScalerEffects` stacked by M(p) − 1. They cover health, shields, shield regen, weapon damage, and ramming damage, and must have infinite duration. Its scrap reward is multiplied by `1 + xpRewardGrowthPerSector × p`. `Enemy.XPReward` and `WaveDefinition.TotalXP` stay base values. The formulas and keys are in `Docs/Balance.md`.

In a wave with an `isBoss` entry, the bosses spawn first, followed by the wave's `bossEscortAtStart` other enemies. The remaining enemies are held back and released in `bossEscortReleaseSteps` even groups as the bosses' health falls (not shields, which recharge), all of them by `releaseAllAtBossFraction`. The boss counts toward `maxConcurrentEnemies`, so leave room for a group. Dead bosses count as 0, so every escort spawns and the wave's scrap stays fixed.

### Ship modules

Drafted modules for defense, mobility, and economy (`Assets/Scripts/Modules/Ship`). They aren't weapons, though a module may deal incidental damage. `ModuleManager.AddShipModule` instantiates a `ShipModuleDefinition`'s prefab on the ship root, with no mount and no HUD button.

A `ShipModule` does nothing by itself. It holds an `EffectMultiplier` stat and a list of `ShipModuleFeature`s picked in the inspector, and it forwards its lifecycle (`OnStart`, `Tick`, `OnMultiplierChanged`, `OnDestroy`) and `CollectStats` to them, passing itself. Each feature is its own effect source. The features:
- `PassiveEffects`: effects for as long as the module is installed, scaled by its multiplier.
- `Triggered`: a `ModuleTrigger` (hull damage, shields broken, kill streak), a `Cooldown` stat, and `TriggerResponse`s.
  - A trigger counts only while the cooldown is ready, and it restarts the cooldown. Once upgrades bring the cooldown below a buff's duration, the buff can stay up.
  - `ApplyEffects` is a timed buff. Its effects need durations of their own, which the module's Duration modifier scales.

Triggers listen to `ShipCoreModule.OnDamageTaken`, which includes the attacker, and to the static `Enemy.OnAnyKilled`.

Upgrades for a ship module target its own tag: `EffectMultiplier`, `Duration`, `Cooldown`. Its own effects must not target that tag, or the module feeds its own multiplier.

New behaviour (an explosion on hit, damage reflection, a bonus that holds only under a condition) goes in as a new feature, trigger, or response, not as a `ShipModule` subclass.

### Prefab conventions the code assumes

- Ship prefabs need a child named `TurretMounts`; `ModuleManager.AddWeapon` fills the first empty mount.
- A weapon prefab's first child is its fire point (`transform.GetChild(0)`).
- Ship-module prefabs hold only a `ShipModule` on the root, with its `moduleDefinition` set.
- Evolved prefabs have their module on the root with the base module's `moduleDefinition`. The Cannon's evolved versions show two ways to build one: a copy with new values (Minigun), or another module type with prefab variants for its projectile and explosion (Vacuum Cannon is a `MissileModule` whose shell doesn't steer).
- A player ship's `ShipCoreModule` sits on a child named `ShipCore`, with the ship's own core definition. `Ship` finds it with `GetComponentInChildren`.
- Player prefabs need an `AbilityModule` on the root, because `Player.Update` reads it without a null check. Space activates it.
- `WeaponModule` sets its own layer from the owning ship: `DetectEnemy` on the player's weapons, `DetectPlayer` on enemies'. `RangeDetector` targeting depends on these layers.
- `WeaponModule`, `ProjectileModule`, and `TeslaModule` are `[ExecuteAlways]`, so they run in edit mode. Each class declares the attribute itself; `MissileModule` and `MineModule` don't.

Keep each `.meta` file with its asset when moving or renaming, since prefabs and assets reference each other by the GUID stored there.
