# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project

HeatDeath is a 2D roguelike space combat game built in Unity 6 (`6000.4.1f1`) with URP, the Input System, and UI Toolkit. It has one scene, `Assets/Scenes/SampleScene.unity`.

The code and assets are the source of truth. `README.md` describes the intended game, so verify against the project before relying on it. `Assets/Docs.docx` holds the design notes. `Assets/ToDo.md` is the user's personal roadmap; it's gitignored, and it should stay current when work it lists is finished.

Balance targets (level curve, enemy scaling, module and upgrade budgets) are in `Docs/Balance.md`. The work in progress is planned in `Docs/Plans/ModulesAndBalance/`: start with its `README.md`, then the brief for your block. `Docs/Plans/` holds temporary plans and is gitignored, so anything that should last goes into `Docs/Balance.md` or this file.

Current state: 5 ships, 7 weapons with 2 evolution paths each (all 21 playable), 7 ship modules, and one playable sector, Outer Belt. Iron Reach and Storm Verge have sprites only. Every weapon has its upgrade set (base upgrades, Legendaries, evolution upgrades, and an infinite upgrade); the core has 3 placeholder upgrades, and ship modules have none yet. Shields recharge after a delay, purple shields block control, and weapons have damage types and on-hit debuffs, each with its own look on the ship. `RunStatistics` counts damage per source, shown only in the dev `[Stats]` log. Run saving, a run statistics screen, core evolution, a main menu, and a game-over flow are not implemented yet.

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

`ShipCoreModule` holds health and shields, and builds the hits for ramming (`RamHit`). All damage is a `Hit` value (`Assets/Scripts/Damage`) that the attacker builds and hands to the target: `IHittable.TakeHit(in Hit)` → `ShipCoreModule.ApplyDamage`. `Ship.TakeHit` always runs damage, death, flash, on-hit effects, then knockback, and returns the `DamageTaken`.

### Shields and damage

The rules and numbers are in `Docs/Balance.md` → "Shields and damage types".

- **Damage types** are the hitter's `ShieldDamageMultiplier` and `HPDamageMultiplier`, set per weapon prefab. Every weapon's damage stats (damage, shield and hull multipliers, backstab, knockback) live in `WeaponModule`, under "Damage". Shields absorb damage first; a shield multiplier of 0 or less sends all damage to the hull (shield bypass). `ShieldsOffline` on the target's core (the Disrupted debuff) does the same for every hit and stops shield recharge; anything that depends on shields being up reads `ActiveShields`, which counts offline shields as 0. `DamageTakenMultiplier` on the target's core multiplies all incoming damage (the Exposed debuff).
- **Hit payload:** a shot carries a `HitPayload` (`Assets/Scripts/Damage`), fixed when it's fired: damage, shield and hull multipliers, backstab, knockback, damage bonuses, source definition, attacker ship, and weapon. Nothing in it changes while the shot flies. Weapons build it with `WeaponModule.CreatePayload()`; projectiles store it with their fire point, and an explosion gets the payload, fire point, and on-hit effects from whatever exploded (missile, mine, `ExplosionAbility`).
- **Hit:** one hit on one target: the payload plus what's specific to this target. `FiredFrom` (distance bonus), `From` (backstab when above the target, the shield ripple), `Push` (knockback; its length scales the push), `EnemiesHitBefore` (multi-hit bonus), the on-hit effects, and `DamageScale` (a share of the whole damage, bonuses included). Projectiles and explosions keep their own hit counters. Tesla hits each ship from the previous link of its chain, and each jump reaches `ChainJumpRange` of the one before and deals `ChainJumpDamage` of its damage. Its evolutions are subclasses that override `PickTarget` and `Strike`, and chain targets beyond the first repeat their strike instead of jumping, each weaker by `ChainJumpDamage` (`TeslaModule.RepeatStrikes`): `ThunderSpearModule` locks onto the most-shielded enemy and adds bolts on it, and `ShockFieldModule` adds blasts around the ship sized to the weapon's range, only the first one applying on-hit effects. With `DischargeDamage` (the Discharge Legendary), an enemy any Tesla kills releases bolts to the closest enemies around it (`TeslaModule.OnHitLanded`, which every Tesla hit reports to), and their deaths discharge again.
- **Damage bonuses:** `ApplyDamage` gets the damage from `payload.Bonuses.Apply(hit, target)`. Weapons build a `DamageBonuses` from their bonus stats (percent max hull, percent of current shields, per unit of distance, per enemy hit before, execute, against debuffed targets, focus fire) into the payload. Momentum (fire rate after kills) lives on the weapon. New bonuses go into `DamageBonuses.Apply` and a weapon stat, so upgrades use them without code.
  - Percent max hull and percent of current shields are added to the hit's damage, and the other bonuses multiply the sum.
  - Distance counts from where the shot was fired to the target.
  - Multi-hit counts the enemies the shot hit before this one: pierced, caught earlier in the blast, or earlier in the Tesla chain.
  - Focus fire and momentum need the weapon's own history. Each hitter reports what its hit took back to the weapon (`HitPayload.ReportHit` → `WeaponModule.OnHitLanded`, skipped once the weapon is gone), which tracks the target it last hit, its hits in a row, and momentum after kills. The payload snapshots the focus target and stacks when fired, and the bonus applies only against that target.
  - The stats start at 0, so upgrades raise them with Flat values (Flat 20 = +20%).
  - A target is debuffed while it carries any on-hit effect. On-hit effects therefore need a finite duration (the default is infinite), and a one-shot effect ends itself in `Update`, like `MultiplyRigidbodyVelocityEffect`.
- **Shield recharge:** after `ShieldRegenDelay` without damage, shields refill at max ÷ `ShieldRechargeTime` × `ShieldRechargeSpeed` per second. The flat `ShieldRegen` is an always-on trickle. Any hit that takes shields or hull restarts the delay.
- **Purple shields:** `HardenedShields` on the core. While shields are up, `ShipCoreModule.BlocksControl` makes `Ship.TakeHit` skip the on-hit effects and knockback. The core recolours its own shield material, and the shield bars follow.
- **Shield visuals:** `ShipShield.shader` uses premultiplied blending: its colour is added light, and `_Cover` = 1 (purple shields) lets the fill also cover the hull. Each hit starts a ripple in one of 4 slots (`MaxRipples`, which must match `MAX_RIPPLES` in the shader). The overlay is drawn only while a hit flash lasts, because the shader is too costly to run on every ship.
- **Damage events:** `ShipCoreModule.OnDamageTaken` per ship and the static `OnAnyDamageTaken` for every ship carry a `DamageTaken`: its `Source` (the module definition the damage came from), the attacker ship, shields and hull taken, shields broken, and `Killed`. `TakeHit` returns the same value to whoever hit.

### Stats, effects, and upgrades

This pipeline spans most of the code:

1. An `UpgradeDefinition` holds a list of `Effect`s. Whenever the stack count changes, `UpgradeManager` passes copies made with `Effect.Copy(stacks)` to `EffectManager.SetEffects`, with the upgrade as the owner.
2. `EffectManager` owns the active effects, ticks their durations, and sets `shouldRecalculate` when the list changes. The recalculation runs once in its `Update`, not immediately.
3. Each module clears its modifiers, applies every effect where `IsApplicableTo(moduleDefinition)` is true (matched by `TagDefinition`), recalculates every stat listed in its `CollectStats`, then calls `OnStatsChanged`.

A `StatModifierEffect` can also grow with the player's level: its `perLevel` modifier is added once per level, and `GameController` marks the player's effects for recalculation on every level-up.

Stats (`ResourceStat`, `ScalarStat`, `BoolStat` in `Assets/Scripts/Modules/Stats.cs`) resolve to `(base + Flat) * (1 + Percent/100)`. Stacks of one upgrade add up (+25% × 4 = +100%), and percents from different sources multiply (×2 and ×2 give ×4), so -100% always wins. A percent below -100% counts as -100%. `BoolStat` is true if `Flat > 0`, false if `Flat < 0`, otherwise its base value.

To add a stat:
- Append to `StatType`. Its values are serialized as integers, so never renumber them.
- Pass the type to the stat's constructor.
- Add the stat to the module's `CollectStats` override. A stat missing from it silently ignores effects.

On-hit effects arrive in a `Hit`; `Ship.TakeHit` gives each target its own copy and uses `EffectManager.AddHitEffect`:
- A repeat (a copy of the same asset effect at the same strength) only extends the one the ship has, which keeps running: a DOT keeps its ticks.
- Effects in the same slot (`SharesSlotWith`) don't stack. The strongest applies, the first of equals, and the others wait with their time running and take over when it ends: a 2 s slow that lands with a 0.5 s stun slows for the 1.5 s after it. Waiting effects reach neither modules nor the appearance, and a waiting DOT doesn't tick.
- A debuff made of several effects shows its look from the part that defines it: a stun's Thrust part, since a full jam can match its weapon part and keep it waiting.

Modules hand out effects through `ModuleBase.EmitEffects`, which scales their durations by the module's `Duration` modifier.

**On-hit effects from upgrades.** A `HitEffectsEffect` gives the modules it targets extra on-hit effects (slow, stun, jam, expose), with a chance per hit. Stacks multiply the chance (up to 1); they multiply the nested effects' strength only with `stacksRaiseStrength`, so a debuff keeps the strength it's sized for, and their durations only with `stacksRaiseDuration` (stuns, which are always at full strength). `ModuleBase` rebuilds the granted ones on every recalculation, like modifiers. `EmitHitEffects` hands a shot its `HitEffects`: the module's own on-hit effects plus the granted sets with their chances, fixed when fired. `Ship.TakeHit` rolls them for each hit, so every ship a shot pierces or a blast catches, and every blast of a mine, rolls on its own. Every weapon's own on-hit list lives in `WeaponModule` (`GetHitEffects`); the core (ramming) and `ExplosionAbility` use the same helper.

**Damage over time.** `DamageOverTimeEffect` deals a percent of the target's max hull per second, in 0.5 s ticks, as typeless hits (1× to shields and hull) through `Ship.TakeHit`. One slot per ship: the strongest ticks, the others wait. `DamageTicks` counts the ticks for it and for hazard zones, the tick on the last moment included (4 s ticks 8 times). Effects that deal damage learn who handed them out from `Effect.OnEmittedBy`, which `ModuleBase.EmitEffects` calls on every copy, so `[Stats]` credits the weapon. A `HazardZone` (an explosive shot's `hazardPrefab`, left over its blast) hits every enemy inside the same way each tick for a few seconds; `HitPayload.PercentOfMaxHull` builds both payloads.

To add an `Effect`, override only the hooks it needs; they all default to doing nothing. Effects are handed out only as copies made with `Effect.Copy(times)`. Its default clones the effect, so override the protected `Stacked` only when strength scales with stacks or a multiplier, and `IsAtLeastAsStrongAs` with it. Definition assets are shared, so effects must not be mutated at runtime.

**Effect sources.** A ship's effects come in two ways: effects an owner holds, and effects attackers put on it (`AddHitEffect`, above). `AddEffect` and `RemoveEffect` are private. Anything that gives a ship effects for as long as it holds them (an upgrade, an ability while active, a ship-module feature, the spawner's enemy scaling, F4) calls `EffectManager.SetEffects(source, copies)` with itself as the owner, and `ClearEffects(source)` to end them. `HasEffects(source)` tells whether any are still running. `SetEffects` makes the owner's effects exactly the given copies:
- A copy equal to one the source already has restarts that one without reapplying it. Equal means made from the same asset effect (`Effect.Template`), with the same strength and duration.
- A changed copy replaces the old one.
- The source's copies missing from the list are removed.

So setting them again refreshes a timed buff instead of stacking it. Make copies with `Effect.Copy`, which records the template; `EmitEffects` already does.

**Appearance.** Effects that change how a ship looks or collides contribute through `Effect.ApplyToAppearance`; they don't change renderers or layers in `ApplyToShip`. `ShipAppearance` is rebuilt from all active effects on every recalculation, like stats:
- Tints multiply together.
- The ship is on the Ghost layer while any effect asks for it.
- The newest thruster colour wins. Its old particles fade out on their own. `ShipAppearance.Thrusters` lists the thruster particles that can emit (the originals and the colour copies), and `OriginalOf` maps a copy to its original, for looks that change the engine fire.
- Any effect can show a look while it's on a ship: its `visual`, a `StatusVisual` prefab (a stun's sparks, a slow's smoke). `ShipAppearance` keeps one instance per distinct prefab as a child of the ship; effects with the same prefab share it, and one no effect references any more fades out. The prefab's `StatusVisual` draws its material over the hull or over each weapon; other components on it act while it's there (`ThrusterCut` stops the thrusters, `ThrusterSmoke` turns their fire into grey smoke, `ShockSparks` flickers shocks on the hull). A new look is a new prefab, no code. Every stun, Thunder's EMP included, uses the same `StunVisual`.

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

Enemy scaling follows run progress, not level or XP. `EnemySpawnController.StartWave` computes p = sectors started − 1 + wave index ÷ waves in the sector, once per wave. Each enemy spawned in that wave gets `enemyScalerEffects` stacked by M(p) − 1. They cover health, shields, shield regen, weapon damage, and ramming damage, and must have infinite duration. Shield recharge is a share of max shields, so it scales with shields; the shield-regen entry only scales the trickle. Its scrap reward is multiplied by `1 + xpRewardGrowthPerSector × p`. `Enemy.XPReward` and `WaveDefinition.TotalXP` stay base values. The formulas and keys are in `Docs/Balance.md`.

In a wave with an `isBoss` entry, the bosses spawn first, followed by the wave's `bossEscortAtStart` other enemies. The remaining enemies are held back and released in `bossEscortReleaseSteps` even groups as the bosses' health falls (not shields, which recharge), all of them by `releaseAllAtBossFraction`. The boss counts toward `maxConcurrentEnemies`, so leave room for a group. Dead bosses count as 0, so every escort spawns and the wave's scrap stays fixed.

`RunStatistics` (owned by `GameController`, in `Assets/Scripts/Statistics`) counts damage dealt per source (shields, hull, kills) and damage the player took, for the whole run and the current wave. It listens to `ShipCoreModule.OnAnyDamageTaken`; `EnemySpawnController` starts and ends its wave periods, and skipped waves never end. It's the base for run statistics: add counters to `StatisticsPeriod` and feed them from events.

The game area stays between 9:16 and 9:21 (width:height) on every platform, the editor included. `Pillarbox` (`Assets/Scripts/Visuals`) installs itself and adds black bars at the sides of a wider screen (tablets, desktop windows) or above and below a taller one:
- It sets `Camera.main.rect` to the area, and a camera behind it clears the bars while there are any.
- The UI's Panel Settings use the Expand match mode, which scales by the area's width (720 reference) in every case. `GameUIController` lays its root out inside the area with `Pillarbox.FitToArea`; a new UI document must do the same.
- Convert screen positions through the camera (`ScreenToWorldPoint`), and take the visible world size from `cam.aspect` and `orthographicSize`, not from `Screen.width`/`Screen.height`. The orthographic size fixes the visible height, so a taller screen shows a narrower slice of the world.

Desktop builds (Windows, macOS, Linux) are for testing the mobile game. Player Settings start them windowed at 540×960 with Resizable Window on, and Unity reopens the window at its last size. The window can take any shape; `Pillarbox` keeps the game area in range.

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
- Evolved prefabs have their module on the root with the base module's `moduleDefinition`. The base weapon's upgrades carry over, so an evolved value must survive them: build the evolved shape from stats the upgrades are meant to change (Octuple Shotgun is 8 `ProjectileDirections` of a normal shotgun volley, so Tight Pattern tightens every barrel), use a stat's `minValue` to protect a value, and avoid special values that a Flat upgrade would break or make useless (Particle Accelerator pierces 5, not −1: −1 means infinite, Piercing Rounds' +1 would make it 0, and an infinite pierce would leave the upgrade nothing to add). The Cannon's evolved versions show two ways to build one: a copy with new values (Minigun), or another module type with prefab variants for its projectile and explosion (Vacuum Cannon is a `MissileModule` whose shell doesn't steer).
- A player ship's `ShipCoreModule` sits on a child named `ShipCore`, with the ship's own core definition. `Ship` finds it with `GetComponentInChildren`.
- Player prefabs need an `AbilityModule` on the root, because `Player.Update` reads it without a null check. Space activates it.
- `WeaponModule` sets its own layer from the owning ship: `DetectEnemy` on the player's weapons, `DetectPlayer` on enemies'. `RangeDetector` targeting depends on these layers.
- Gameplay code doesn't run in edit mode (no `[ExecuteAlways]`). A selected weapon draws its range in the scene view as a gizmo (`WeaponModule.OnDrawGizmosSelected`).
- Input must work without a keyboard or mouse: phones have neither. Read the pointer through `Pointer.current` (mouse or touch), and null-check `Keyboard.current` and `Mouse.current` for desktop-only shortcuts.
- Projectiles get direction and speed separately (`Projectile.Initialize(direction, speed, module)`), so a projectile with speed 0, like the laser beam, still points along its shot.
- A shader that reads sizes from `unity_ObjectToWorld` (`LaserBeam`, `EMPWave`, `LightningRing`, `ToxicCloud`, `NaniteSwarm`, `VacuumPull`, the `Status*` shaders) needs the `"DisableBatching"="True"` tag: batching merges sprites into one world-space mesh with an identity matrix, which made Yamato's two laser beams invisible.

Keep each `.meta` file with its asset when moving or renaming, since prefabs and assets reference each other by the GUID stored there.
