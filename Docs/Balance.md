# Balance targets

Source of truth for run pacing, enemy scaling, and power budgets. Values marked *tunable* are inspector fields; change them in playtesting and update this file. Current enemy stats and core upgrades in the assets are placeholders and are **not** a reference. Weapon values and upgrades are a first pass sized on paper, until block F's playtests measure them.

The rules here are general guidelines. A module's specific design decisions take precedence over them (user).

## Difficulty goals

- All 5 sectors have the same base difficulty and the same base scrap. They come in random order, so a sector must never doom a run by being first.
- Sector 1 (whichever it is): easy. Losing should be hard even with a bad build, so late-scaling builds can gain momentum.
- Sector 2: a bit more challenge. Filters out the worst builds.
- Sector 3: the first big test of the build. Bad builds fail here.
- Sector 4: a steady step up from sector 3, not a jump.
- Sector 5: the final challenge. Clearing it needs a good build and decent skill.
- Endless: not an immediate death. Difficulty rises steadily until upgrades can't keep up. A high score should feel like an achievement.

The curve comes from three levers together: upgrade frequency (level curve), upgrade power (budgets below), and enemy scaling.

## Run structure

- 5 sectors, shuffled, each played once. Then endless mode, where sectors repeat.
- Scaling follows **how many sectors have been played**, never which sector it is.
- Each sector has **8-10 short waves**. The last wave is the boss wave.
- Run progress `p` = sectors completed + (index of the wave about to start ÷ number of waves in the current sector). It rises at every wave.

## Levels and scrap

"Scrap" is the in-game name for XP.

- Level cost: `round(20 + 1.2 × (level − 1))`. Level 1→2 costs 20, level 21 costs 44, level 78 costs 112. *Tunable.*
- Base scrap per sector: **600** (75 per wave at 8 waves, 60 at 10).
- Enemy scrap reward: base reward × `(1 + 0.3 × p)`. *Tunable.*
- The player's ShipCore `XPGain` stat (base 1) multiplies all scrap gained. Scrap-gain upgrades and ship modules raise it.

| Sector | Level-ups, no boost | Level-ups, +25% scrap | Level at sector end |
|---|---|---|---|
| 1 | 21 | 24 | 22 |
| 2 | 15 | 18 | 37 |
| 3 | 15 | 16 | 52 |
| 4 | 13 | 15 | 65 |
| 5 | 14 | 15 | 79 |
| Endless | ~13 | ~15 | |

## Module drafts

- Weapons: run start, then levels **10, 30, 45**.
- Ship modules: levels **5, 20, 35, 50**.
- A level in either list gives a module draft instead of an upgrade draft. If a module draft has no candidates, it becomes an upgrade draft.

Where they land in an 8-wave sector without scrap boosts:

| Level | Draft | Sector, wave |
|---|---|---|
| 5 | Ship module 1 | 1, wave 2 |
| 10 | Weapon 2 | 1, wave 3 |
| 20 | Ship module 2 | 1, wave 8 (boss) |
| 30 | Weapon 3 | 2, wave 4 |
| 35 | Ship module 3 | 2, wave 7 |
| 45 | Weapon 4 | 3, wave 4 |
| 50 | Ship module 4 | 3, wave 7 |

## Enemy scaling

- One multiplier `M(p)` applies to enemy health, shields, shield regen, weapon damage, and ramming damage, through `EnemySpawnController.enemyScalerEffects`. Health and damage scale together, so felt pressure grows roughly with M².
- Keys at sector starts: `1, 3.5, 9, 20, 30, 42` (sectors 1-6). Between keys, M grows geometrically: `M = key[i] × (key[i+1] / key[i])^(p − i)`. *Tunable.*
- Past the last key, M grows ×**1.3** per sector: `M = 42 × 1.3^(p − 5)`. *Tunable.*
- M is computed once per wave, when the wave starts.

| Sector | Start | 25% | 50% | 75% | Boss wave of 8 |
|---|---|---|---|---|---|
| 1 | 1.0 | 1.4 | 1.9 | 2.6 | 3.0 |
| 2 | 3.5 | 4.4 | 5.6 | 7.1 | 8.0 |
| 3 | 9.0 | 11.0 | 13.4 | 16.4 | 18.1 |
| 4 | 20.0 | 22.1 | 24.5 | 27.1 | 28.5 |
| 5 | 30.0 | 32.6 | 35.5 | 38.6 | 40.3 |
| Endless 1 | 42.0 | 44.8 | 47.9 | 51.1 | 52.8 |
| Endless 2 | 54.6 | 58.3 | 62.3 | 66.5 | 68.7 |

## Expected difficulty

D = the wave's M ÷ the player's power entering that wave. Power = √(offense × defense), where offense sums the weapon modules and defense sums the ShipCore and ship modules. 1.0 is a fresh ship on sector 1 wave 1. The table shows each sector's hardest wave, simulated with 8 waves per sector; 5 or 10 waves change it by under 0.1.

| Sector | Worst build | Bad | Average | Good |
|---|---|---|---|---|
| 1 | 1.22 | 1.06 | 1.00 | 1.00 |
| 2 | 1.81 | 1.53 | 0.70 | 0.63 |
| 3 | 3.04 | 2.53 | 1.05 | 0.93 |
| 4 | 4.28 | 3.38 | 1.23 | 1.12 |
| 5 | 5.40 | 4.07 | 1.38 | 1.27 |
| Endless 1 | 6.24 | 4.49 | 1.47 | 1.36 |
| Endless 2 | 7.15 | 4.94 | 1.59 | 1.48 |
| Endless 3 | 8.26 | 5.51 | 1.77 | 1.62 |

Model behind it (rebuild it if the structure below changes): each level-up is drafted per upgrade with the old stack/new/free draft rule, before the current rule below. Each module has 12 base upgrades (6 Common, 3 Rare, 2 Epic, 1 Legendary), a 4-upgrade evolution pool, and 1 infinite upgrade. Each weapon or ship module at base is worth about as much as the bare ShipCore. Builds: worst = random picks realizing 40% of nominal power; bad = random, 70%; average = takes the highest rarity, 100%; good = keeps offense and defense even and finishes modules it started, 130%, with +25% scrap from level 8. Averaged over 200 runs. Absolute values depend on how much rarity and synergy pay off, so the keys stay tunable. The table predates two decisions: the ShipCore has no infinite upgrade, and first-pass pools hold only 3-4 base upgrades per module. Treat it as approximate, especially the endless rows, until the model is rerun.

## Module structure

Implemented in block C, except core evolution (see ShipCore below).

- **9 modules:** 4 drafted weapons (first at run start), 4 drafted ship modules (first at level 5), and the ship-specific **ShipCore**, shown to the player as "Core".
- **Ship modules** cover survival, mobility, and economy. Most are passive; some add a buff that triggers on its own, such as invisibility on taking damage or a rage mode after several quick kills. The player never activates them: the ship's ability stays the only active control. They aren't weapons, though a module may deal incidental damage, such as reflection or a burst on hit. That damage, like a triggered buff that raises offense, counts on the defense side, so offense stays the weapons' share. The draft wants a pool of about 8 ship modules for 4 slots, as weapons have 7 for 4, and at least 6 so the 4th draft still offers 3. The count is the length of the ship-module draft-level list. A health or shield bonus is optional, up to each module's design. Some modules are purely utility (economy, for example, costs only the slot it takes), some are situational, and their value comes from that.
- **ShipCore** is everything the base ship comes with (hull, thruster, ability, and any ship part added later) as one module: one set of slots and one share of draft rolls. It follows the same slot rules. Each player ship has its own core definition, so its pool holds upgrades shared by every ship plus upgrades for that ship alone. It has no infinite upgrade. Its evolution will swap the whole ship for an evolved version (new ability, thruster, or look); until that exists, cores have no evolution paths.
- **Slots:** 3 before evolution. Filling them with Commons takes 12 picks and gives ×2.5 the module's base value. Max stacks are set per upgrade by the module's design, not by rarity.
- **Evolution:** possible once all 3 slots hold max-stack upgrades. It swaps the module for its evolved prefab, which keeps the module's definition, so slotted upgrades still apply. It opens a 4th slot drawing from a pool specific to that evolution path, and unlocks the module's infinite upgrade if it has one. Weapons have 2 paths. Paths differ in role, not only in size: one can be raw damage, the other utility that trades damage for control. Aim for an evolved prefab worth about ×1.6 in its role.
- **Infinite upgrades:** exactly **one per weapon and per ship module**, on its primary output stat (weapon damage, a ship module's main defensive stat). The ShipCore has none. Stats with natural limits stay finite: cooldown, projectile count, range, speed. Infinite upgrades never take a slot, appear only for evolved modules, and never count toward evolution. Each stack adds +10% within the upgrade, so the relative gain falls from 10% toward ~3% after ~25 stacks.
- **Draft rule:** each upgrade draft has 3 options in shuffled positions. Two are skewed: one stacks an owned upgrade 80% of the time, the other 20%, and each offers an evolution 20% of the time. The third is free: it stacks 50% of the time and offers an evolution 60% of the time. An option without candidates of its drawn kind takes the other kind. An upgrade option first picks a module uniformly among modules with a candidate, then rolls rarity (70/20/8/2) with the existing fallback. With 9 modules the ship side gets 5 of 9 rolls, so there is no separate side share.
- **Evolutions are draft options and cost a level.** Evolution options appear only while a module is ready to evolve. Each names a ready module and one of its paths, picked at random; one draft never shows the same path twice, but can show both of a module's paths. With 3 options, at least one evolution appears in about 74% of drafts.
- **Rush or balance** is an intended choice. Rushing one module evolves it around level 14 (late sector 1) but leaves the rest bare, and its infinite upgrade then keeps competing for draft options. Balanced builds evolve later and grow evenly. Taking the highest rarity each time evolves around level 19; random picks around level 49. More stacks barely delay the rush (6-stack Commons: level 18). These levels assume evolution on readiness; as a draft option it comes about 1-2 levels later. If sector-1 evolutions should be prevented, add a minimum evolution level.

## Shields and damage types

Shields are a barrier that recharges between fights. Hull is the budget for the whole run.

**Shields** (`ShipCoreModule`):
- **Recharge:** after `ShieldRegenDelay` seconds without taking damage, shields refill from empty to full in `ShieldRechargeTime` seconds, divided by `ShieldRechargeSpeed`. The rate is a share of max shields, so +max shields keeps the recharge time constant. Any hit that takes shields or hull restarts the delay.
- **Recharge upgrades** raise `ShieldRechargeSpeed` by +% (linear, like `TimeScale`) and lower `ShieldRegenDelay` by −%. Don't lower `ShieldRechargeTime` with −%: it grows faster than linearly (4 × −20% gives ×5 speed).
- **Trickle:** the flat `ShieldRegen` always regenerates, even under fire. It's for niche upgrades, ship modules, and Thunder, not for general shield upgrades.
- Defaults: delay 3 s, recharge 8 s, trickle 0.
- Bosses release their escorts as their health falls, not their shields, since shields come back.

**Hull regen** (`HPRegen`) is constant, even under fire. Most ships have none or a slow trickle; a ship without shields can have more. At about 300 s per sector, 0.1/s heals about 30 HP per sector and 1/s about 300. Flat regen doesn't grow with enemy scaling, so its value falls in later sectors unless upgrades raise it.

**Player ships** (placeholders; block E sets the ship base values):

| Ship | Health | Shields | Delay | Recharge | Trickle | Hull regen | Why |
|---|---|---|---|---|---|---|---|
| Flash | 75 | 40 | 2 s | 5 s | 0 | 0 | Dodge and shields are back |
| Bulwark | 225 | 0 | — | — | 0 | 1.0 | No shields: the hull is everything |
| Yamato | 100 | 75 | 3 s | 8 s | 0 | 0.25 | Sturdy flagship |
| Thunder | 75 | 100 | 2.5 s | 6 s | 2 | 0.1 | High-tech shield ship |
| Reaper | 75 | 75 | 4 s | 8 s | 0 | 0 | Its ability heals: trickle +500%, recharge speed +100%, no delay |

**Purple (hardened) shields** (`HardenedShields`): while they're up, the ship ignores knockback and every on-hit effect from every source. The hit that breaks them still pushes and applies its effects. Regular shields block nothing.
- Enemies: elites and bosses only, never swarms. They can't be pushed or stunned until their shields break, which gives bosses a phase and prevents stun-locks. This replaces "knockback resistance by rank".
- Player: a ship module or upgrade (block E).
- They look dark purple and ominous: a dark shroud over the hull with a bright violet rim and ripples, and a purple shield bar.

**Damage types** (damage multiplier vs shields / vs hull):

| Type | Weapons | Shields | Hull | Role |
|---|---|---|---|---|
| Kinetic | Cannon, Shotgun | 1 | 1 | Reliable damage anywhere |
| Kinetic | Railgun | 1 | 1.4 | Heavy skill shot |
| Explosive | Missile, Mines | 0.5 | 1.5 | Area damage and knockback: crowd control against unshielded swarms, poor against shields |
| Energy | Laser, Tesla | 1.5 | 0.75 | Anti-shield |

- Blue Laser bypasses shields (multiplier −1: its damage goes straight to the hull).
- Evolutions keep their weapon's multipliers (user decision); only a defining mechanic changes one, like the Blue Laser's bypass.
- Enemy weapons should follow the same table.
- Sectors can lean one way: Outer Belt mostly unshielded, Storm Verge and other high-tech sectors shield-heavy, so builds that invest only in hull damage struggle there. Kinetic stays neutral, so a bad sector order never ruins a run.

**Debuffs**, applied on hit. Upgrades grant them to weapons through `HitEffectsEffect`, with a chance per hit that stacks raise: every ship a shot pierces or a blast catches, and every blast of a Multicharge mine, rolls on its own (user). The same kind from different weapons doesn't stack: the strongest one applies, and weaker ones wait and take over when it ends (user). A 2 s slow that lands with a 0.5 s stun slows for the 1.5 s after it, so a debuff upgrade stays useful on a weapon that stuns on its own (Short Circuit on Shock Field). Hitting again with the same debuff extends it. Purple shields block all of them.

| Debuff | Effects (tags) |
|---|---|
| Exposed | `DamageTakenMultiplier` +X% (ShipCore) |
| Disruptor | `ShieldsOffline` (ShipCore): the shields that are left absorb nothing, so all damage hits the hull, and they don't recharge. Strong against shield-heavy enemies, so it's rare and short: the Laser's Disruptor is an Epic, 20% per stack (2 stacks), 2.5 s. It costs an energy build its own damage (1.5× vs shields, 0.75× vs hull) and pays off kinetic and explosive weapons |
| Jammed | weapon `TimeScale` −X% (Weapon) |
| Slow | `Thrust` and `MaxSpeed` −X% (Movement) |
| Stun | `TimeScale` −100% (Weapon), `Thrust` −100% and `Drag` +1 (Movement), and the ship's velocity ×0.1 once; 0.3-0.5 s, Thunder's EMP 4 s |

A stun doesn't stop the ship dead (user): it keeps drifting with its inertia at a tenth of its speed, and the drag (linear damping) slows it to a stop. Weapon stuns and Thunder's EMP work the same way. *Tunable.*

Size a debuff upgrade by the value it gives the whole build, not by its own damage. Support works best as control (Vacuum Cannon, debuffs), not only as stat buffs.

A debuff must be noticeable when it lands (user): about 50% slow or jam, or a full shutdown, not 10%. Upgrades scale it with a chance per stack (Impact Shock, Flashbang) rather than a weak effect on every hit.

## Weapon base values

Weapons are matched by value for their role, not by equal DPS. Harder weapons (skill shots, close range, placement) may deal more; weapons that bring area control deal less. The Cannon, the reliable generalist, is the reference.

**Scenario:** a typical Outer Belt wave, about 4 enemies alive at 2-8 units from the player, with pools about 40% shields. Effective DPS = raw DPS × hit rate × targets per hit × damage-type factor (1 kinetic, 1.21 Railgun, 0.83 explosive, 0.94 energy for this pool). These are paper estimates from the code; the `[Stats]` log measures them in play, and measured values replace them here.

| Weapon | Role | Target | Damage | Cooldown | Other | Raw DPS | Hit assumption | Est. effective |
|---|---|---|---|---|---|---|---|---|
| Cannon | Reliable generalist, auto-aim | 1.0 | 15 | 0.75 | range 8, speed 8 | 20 | 80% hits, in range 95% | 15 |
| Shotgun | Close range: risk | 1.2 | 5 × 8 | 1.0 | range 5, spread 20° | 40 | 55% of pellets hit, in range 80% | ~18 |
| Railgun | Straight-ahead skill shot | 1.2-1.3 | 55 | 1.5 | speed 35, hull ×1.4 | 37 | 40% hits (fixed forward) | ~18 |
| Laser | Straight beam, hits every enemy in line, anti-shield | ~1.15 | 40 | 1.25 | energy | 32 per target | 0.55 targets per beam | ~16.5 |
| Missile | Homing area damage and knockback | 0.85 | 30 | 1.75 | explosion 2.5 (radius 1.25) | 17 | 75% hits, 1.25 targets | ~13 |
| Mines | Placement, area damage and knockback | 1.05 | 48 | 2.0 | 3 mines, thrown ~2.7 units, proximity fuse 0.6 | 24 | 60% go off near enemies, 1.3 targets | ~15.5 |
| Tesla | Never misses, chains, flexible | 0.85 | 18 | 1.75 | chains 3 | 10 per target | 1.7 targets, in range 80% | ~13 |

Evolved Cannon: Minigun 6 damage every 0.18 s (33 raw DPS, ×1.67, range 6) for raw damage; Vacuum Cannon 12 damage every 0.9 s with splash 1.5 and knockback 10 for crowd control.

Evolved Shotgun: Eradicator 6 × 6 every 0.55 s (65 raw, ×1.6) for raw damage; Octuple Shotgun is eight weaker shotguns, one every 45° from its aim (`ProjectileDirections` 8), each 4 × 5 every 1.2 s with the Shotgun's 20° spread, for coverage; shotgun upgrades apply to every barrel. Evolved Railgun: Particle Accelerator pierces 5 enemies (6 hits, in practice a whole line), with shots at speed 70 (Railgun 35) and the same damage; Behemoth Rifle 160 every 2.5 s, knockback 12, plus 3% of the target's max hull.

Evolved Missile: Rocket Swarm 4 small fast rockets × 11 every 1.5 s (29 raw, ×1.7), blast radius 0.75; Nuke 120 every 5 s, blast radius 3.5, leaving a toxic zone of the same size that deals 3% of max hull per second to enemies inside for 5 s. Evolved Mines: Multicharge mines explode every time an enemy comes close, rearming 0.5 s after each blast (user decision, against stun-locks with Flashbang), 35 per blast every 2.5 s, until replaced; Nanite Bombs 40 per blast, radius 2, no knockback, and every blast stuns 0.4 s and leaves nanites dealing 2% of max hull per second for 4 s. Damage over time and the toxic zone are typeless and scale with the target's max hull (user decision), so they're weak early and keep up with enemy scaling.

Evolved Laser: Prism fires 3 beams in a fixed 45° fan, 22 damage each every 1.0 s (×1.55); Blue Laser 45 every 1.25 s, bypasses shields, and adds 15% of the target's current shields to each hit (×1.1 against unshielded enemies, ×1.65 against a 40%-shield pool). Evolved Tesla: Shock Field releases an EMP-like blast around the ship that reaches range 5, 20 damage every 2.5 s, stunning 0.5 s; each chain-target upgrade adds a pulse 0.3 s later at half the previous damage, without stun (user decisions). Thunder Spear locks onto the enemy in range 6 with the highest max shields, 12 damage every 0.5 s, and each hit jams its weapons for 0.3 s without stopping it (user decision: stun-locking it was too strong); each chain-target upgrade adds a bolt on it 0.1 s later at half the previous damage. With 3 chain upgrades, either deals at most ×1.9 per cooldown.

Stun durations are tuned per weapon to avoid stun-locks (user decision): Impact Shock 0.2 s and Flashbang 0.25 s per stack, and Multicharge mines rearm.

A bonus against debuffed enemies should pay off other weapons' debuffs, not the weapon's own (user). Exploit (+100% against debuffed enemies) is therefore the Cannon's Legendary and can't be taken with Suppressive Fire. The Tesla's Legendary is Discharge instead: enemies it kills release bolts at the 2 closest enemies for 50% damage, and their deaths discharge again. It's strong against swarms and weak against bosses. A weapon may have more than one Legendary (user): the Cannon also has Finisher (up to +100% damage the less hull the target has left) and Focus Fire (+15% per hit in a row on the same enemy, up to 5).

Evolved prefabs must survive the base weapon's upgrades, which carry over, and should keep them useful (user). Particle Accelerator pierces 5, not −1 (infinite), so Piercing Rounds still adds targets; a pierce of −1 would also turn into 0 with its +1.

Explosions push with full knockback at their centre, falling to half at their edge (user decision). Missile 2.5, Mines 3, and Vacuum Cannon 10 are stopgaps until the knockback rework in block F.

The Laser never gets knockback, from its base values or from upgrades: it's not that kind of weapon (user decision).

## Upgrade power by rarity

Percent of the targeted module's main output. Higher rarities fill a slot in fewer picks and give more per slot, so they carry tradeoffs or conditions. The table is a guideline: rarity sets roughly what a full slot is worth, and each upgrade's stacks follow its module's design. What matters is that rarer upgrades are stronger but more specific, situational, or costly.

| Rarity | Stacks | Per stack | Full slot | Cost to use |
|---|---|---|---|---|
| Common | 4 | +9% | +36% | None. One stat. |
| Rare | 3 | +17% | +50% | Narrower target or a mild condition |
| Epic | 2 | +37% | +75% | A clear tradeoff or condition |
| Legendary | 1 | ×2 or a mechanic | ×2 | Heavy tradeoff or very situational |

Draft weights: Common 70, Rare 20, Epic 8, Legendary 2.

**Level scaling as a tradeoff.** An Epic or Legendary effect can grow with the player's level through a stat effect's `perLevel` modifier, which is added once per level on top of its base value. It's weak when taken early, strong late, and it keeps growing in endless mode. Size it so it reaches the table's full-slot value around the level where such upgrades are usually taken, and stays below it before then.

**Percent max hull damage as late scaling.** `PercentMaxHullDamage` adds a percent of the target's max hull to each hit (max hull, the same against bosses). It's nearly worthless early (2% of a 30-hull enemy is 0.6) and grows exactly with enemy scaling. Size it per hit, so fast or multi-projectile weapons get less per hit.

**Every weapon gets at least one late-scaling upgrade**, level-scaled or percent max hull damage, usually an Epic or Legendary that's weak early (user decision). Without one, a weapon whose upgrades are all flat stats falls behind enemy scaling. Control (stun, slow, jam, knockback) doesn't need scaling: it works the same on a ×40 enemy.

**Damage that depends on how you hit** (user direction: more of these). Every weapon has these stats, in percent, so upgrades use them without code. They start at 0, so upgrades raise them with a flat value (Flat 20 = +20%); a percent modifier does nothing. The factors multiply each other and the whole hit, percent max hull included (user decision).

| Stat | Bonus | Fits |
|---|---|---|
| `PercentMaxHullDamage` | of the target's max hull, added to the hit before the other bonuses multiply it | late scaling, Behemoth |
| `PercentShieldsDamage` | of the target's current shields, added like percent max hull. With a shield bypass the whole hit goes to the hull, so it punishes shielded targets | Blue Laser |
| `DistanceDamage` | per unit from where the shot was fired to the target; negative values fall off | Railgun (Long Shot), a close-range Shotgun |
| `MultiHitDamage` | per enemy the shot hit before this one: pierced, caught earlier in the blast, or earlier in the Tesla chain, the same rule for every weapon (user decision). A laser beam's hits count the same way, in no set order. Counts at most `MultiHitMaxStacks` enemies (base 5, user decision), so area weapons in dense waves don't multiply their damage without limit | Tesla, piercing Railgun, Laser, explosives |
| `ExecuteDamage` | at 0 hull, less the more hull the target has left | finishers: Cannon, Minigun |
| `DebuffedTargetDamage` | against targets carrying any on-hit debuff | combos with support weapons |
| `FocusDamage` | per hit in a row on the same target, up to `FocusMaxStacks` (base 5). Counted when the shot is fired, and only if the shot hits that target; switching targets restarts at 1. Every landed projectile counts, so multi-projectile weapons ramp fastest, and a Tesla chain ends focused on its last target | Minigun, Thunder Spear, Laser |
| `MomentumFireRate` | fire rate for `MomentumDuration` seconds (base 3) after this weapon kills; each kill restarts it, no stacking | Minigun, Eradicator |

A shot carries what it was fired with: nothing changes while it flies (user decision). Focus fire is therefore decided when the shot is fired, and fast weapons ramp with a slight lag.

**Legendaries deepen their weapon's identity** and compose with both of its evolutions (user decision). A Legendary that rewrites how a weapon works (a shotgun that fires one slug, a railgun that aims) erases what makes the weapon distinct and breaks its evolved prefabs; that belongs in an evolution path instead.

Different weapons may have similar upgrades (the same debuff, the same bonus), as long as each fits its weapon (user).

Base upgrades should stay useful after either evolution (user). When a path changes what a stat means, it keeps the stat working in a new way: on Shock Field and Thunder Spear, chain-target upgrades repeat the strike (another, weaker blast or bolt) instead of jumping to new enemies (user decisions).

A full pool is about 6 Common, 3 Rare, 2 Epic, and 1 Legendary per module. That mix is a guideline, and pools may differ per module. Every upgrade follows the table above, so adding upgrades later widens choice without shifting the balance.

## Content budgets

**Sectors** (all 5 must match):
- 600 base scrap in total, within 2%.
- 8-10 waves. The last wave group is a single boss wave.
- `SectorDefinition` warns in the editor when a sector breaks one of these rules.
- Equal difficulty is checked by playtest, not by a stat total. Health + shields ignores enemy damage and shield regen, so it doesn't show whether two sectors are equally hard.

**Waves:**
- Short and frequent: about 15-20 enemies per regular wave.
- Use `waveOrder` groups so difficulty ramps through the sector, with the order varying within each tier. Outer Belt's tiers: a calm opener (1), light waves (2), medium waves (3), heavy waves led by Strikers and Breakers (4), and the boss (5).
- Boss wave: the boss starts with a small escort, and the rest of its wave is released as it takes damage. A boss shouldn't fight alone, and its minions shouldn't overshadow it.

**Modules:**
- Each weapon at base: about the same value as any other weapon, judged by role rather than equal DPS. Weapons that are harder to use (skill shots, close range, placement) may deal more; weapons that bring control or support deal less. See "Weapon base values" (block D).
- Each ship module at base, counting its unique bonus: about the defensive value of the bare ShipCore, so one ship module roughly doubles a fresh ship's durability. Grant flat amounts where possible: percents from different modules multiply (+200% and +200% give ×9), while flats add up as the model assumes.
- Fire-rate bonuses use `TimeScale`, not Cooldown reductions. DPS grows linearly with TimeScale (+36% gives +36%), while Cooldown −36% gives +56% and −100% fires every frame.
- Effects scaled by a multiplier (a module's or ability's `EffectMultiplier`, enemy scaling) avoid negative percents unless the drawback should grow with it: −75% at ×1.33 is −100%. A percent below −100% counts as −100%.

**Ship modules, first pass** (*tunable*). Sized at about +175 effective HP each, about one fresh ship (today's ships have 115-225). These are placeholders until playtesting: some modules should give up the health or shield part in favour of their role. Shield Capacitor's "+4 shield regen" predates the recharge rework and is now an always-on trickle; block E revisits it.

| Module | Passive (ShipCore) | Trigger → buff (duration / cooldown) |
|---|---|---|
| Armor Plating | +150 health, +10 ramming damage | none |
| Shield Capacitor | +110 shields, +4 shield regen | none |
| Repair Nanites | +100 health, +1.5 HP regen | none |
| Salvage Processor | +100 shields, scrap gain +0.25 | none |
| Phase Shroud | +100 health | Hull damage → Ghost, 1.5s / 10s |
| Berserker Core | +100 shields | 5 kills in 4s → weapon TimeScale +33%, 5s / 5s |
| Emergency Thrusters | +100 health | Shields broken → MaxSpeed and Thrust +50%, 3s / 12s |

## Content ideas

Ideas from the user for future content. Each says what the current code supports.

- **Level-scaled upgrades:** see "Upgrade power by rarity". Supported (`perLevel` on stat effects).
- **Support modules:** a weapon or upgrade that is weak alone but strengthens other modules: other weapons' damage or fire rate, the ability, or durability. Size its value as the bonus it gives the rest of the build, not its own DPS.
  - Supported for upgrades: an upgrade's effects may target any tag (`Weapon` covers all 7 weapons; also `ShipCore`, `Ability`, `Thruster`).
  - Supported for control: upgrades can grant weapons on-hit debuffs (`HitEffectsEffect`, see "Shields and damage types").
  - Not supported yet: a weapon granting stat effects just for being installed (ship modules can, through `PassiveEffects`), and "other weapons only" (a tag also matches the module that grants it). Not planned: the user prefers control over stat auras for support.
- **Knockback resistance by enemy rank:** replaced by purple shields on elites and bosses (see "Shields and damage types"), which block knockback until they break.
- **Point-defense weapon:** shoots down enemy missiles (Harbringer, Mites). Not supported yet: projectiles can't be hit. Later.
- **Run statistics:** `RunStatistics` already counts damage dealt per source, kills, and damage taken per run and per wave (the dev `[Stats]` log). Run statistics and run history add more counters to it.
- **Evolution paths differ in role, not only in size:** for example Minigun is raw damage (much faster fire, weaker shots) and Vacuum Cannon is crowd control (strong knockback, splash, weaker shots). A utility path can trade damage for control.
