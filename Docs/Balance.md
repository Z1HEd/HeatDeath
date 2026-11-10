# Balance targets

Source of truth for run pacing, enemy scaling, and power budgets. Values marked *tunable* are inspector fields; change them in playtesting and update this file. Current enemy stats and upgrade values in the assets are placeholders and are **not** a reference.

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

- One multiplier `M(p)` applies to enemy health, shields, shield regen, and weapon damage, through `EnemySpawnController.enemyScalerEffects`. Health and damage scale together, so felt pressure grows roughly with M².
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

Model behind it (rebuild it if the structure below changes): each level-up is drafted per upgrade with the draft rule below. Each module has 12 base upgrades (6 Common, 3 Rare, 2 Epic, 1 Legendary), a 4-upgrade evolution pool, and 1 infinite upgrade. Each weapon or ship module at base is worth about as much as the bare ShipCore. Builds: worst = random picks realizing 40% of nominal power; bad = random, 70%; average = takes the highest rarity, 100%; good = keeps offense and defense even and finishes modules it started, 130%, with +25% scrap from level 8. Averaged over 200 runs. Absolute values depend on how much rarity and synergy pay off, so the keys stay tunable. The table predates two decisions: the ShipCore has no infinite upgrade, and first-pass pools hold only 3-4 base upgrades per module. Treat it as approximate, especially the endless rows, until the model is rerun.

## Module structure

Agreed design. Slots, evolution, and infinite-upgrade gating are not implemented yet; the balance assumes them.

- **9 modules:** 4 drafted weapons (first at run start), 4 drafted ship modules (first at level 5), and the ship-specific **ShipCore**.
- **Ship modules** cover survival, mobility, and economy. Most are passive; some add a buff that triggers on its own, such as invisibility on taking damage or a rage mode after several quick kills. The player never activates them: the ship's ability stays the only active control. They never deal damage themselves, so offense and defense stay separable; a triggered buff that raises offense still counts on the defense side. The draft wants a pool of about 8 ship modules for 4 slots, as weapons have 7 for 4, and at least 6 so the 4th draft still offers 3. The count is the length of the ship-module draft-level list.
- **ShipCore** covers the ship's core, thruster, and ability as one module: one set of slots and one share of draft rolls. It follows the same slot rules. Its pool holds generic hull upgrades plus upgrades specific to the chosen ship, its movement, and its ability. It has no infinite upgrade. Its evolution starts as a stat package and can later evolve the ship's ability.
- **Slots:** 3 before evolution. Filling them with Commons takes 12 picks and gives ×2.5 the module's base value.
- **Evolution:** triggers when all 3 slots hold max-stack upgrades. It keeps the slotted upgrades and gives ×1.6. It opens a 4th slot drawing from a pool specific to that evolution path, and unlocks the module's infinite upgrade if it has one. Evolved modules keep the base module's tag so slotted upgrades still apply. Weapons have 2 paths, and the player picks one when evolution triggers. Evolutions start as stat packages; the new mechanics of evolved weapons come later.
- **Infinite upgrades:** exactly **one per weapon and per ship module**, on its primary output stat (weapon damage, a ship module's main defensive stat). The ShipCore has none. Stats with natural limits stay finite: cooldown, projectile count, range, speed. Infinite upgrades never take a slot, appear only for evolved modules, and never count toward evolution. Each stack adds +10% within the upgrade, so the relative gain falls from 10% toward ~3% after ~25 stacks.
- **Draft rule:** of the 3 options, one stacks an owned upgrade, one is an unowned upgrade, and one is free. Each falls back to free when it has no candidates. Owned infinite upgrades count as stackable, unowned ones as new. Each option first picks a module uniformly among modules with a candidate, then rolls rarity (70/20/8/2) with the existing fallback. With 9 modules the ship side gets 5 of 9 rolls, so there is no separate side share.
- **Rush or balance** is an intended choice. Rushing one module evolves it around level 14 (late sector 1) but leaves the rest bare, and its infinite upgrade then keeps competing for draft options. Balanced builds evolve later and grow evenly. Taking the highest rarity each time evolves around level 19; random picks around level 49. More stacks barely delay the rush (6-stack Commons: level 18). If sector-1 evolutions should be prevented, add a minimum evolution level.

## Upgrade power by rarity

Percent of the targeted module's main output. Higher rarities fill a slot in fewer picks and give more per slot, so they carry tradeoffs or conditions.

| Rarity | Stacks | Per stack | Full slot | Cost to use |
|---|---|---|---|---|
| Common | 4 | +9% | +36% | None. One stat. |
| Rare | 3 | +17% | +50% | Narrower target or a mild condition |
| Epic | 2 | +37% | +75% | A clear tradeoff or condition |
| Legendary | 1 | ×2 or a mechanic | ×2 | Heavy tradeoff or very situational |

Draft weights: Common 70, Rare 20, Epic 8, Legendary 2.

A full pool is about 6 Common, 3 Rare, 2 Epic, and 1 Legendary per module. That mix is a guideline, and pools may differ per module. Every upgrade follows the table above, so adding upgrades later widens choice without shifting the balance.

## Content budgets

**Sectors** (all 5 must match):
- 600 base scrap in total, within 2%.
- 8-10 waves. The last wave group is a single boss wave.
- Total enemy count and total base durability (health + shields, before scaling) within ~10% of the reference. Reference: today's Outer Belt has 148 enemies and 10,650 durability over 5 waves; update this line when Outer Belt is re-authored.

**Waves:**
- Short and frequent: about 15-20 enemies per regular wave, so a sector's total stays near the reference.
- Use `waveOrder` groups for structure: opener(s), shuffled middle waves, mini-boss wave(s), boss.

**Modules:**
- Each weapon at base: about the same DPS as any other weapon.
- Each ship module at base, counting its unique bonus: about the defensive value of the bare ShipCore, so one ship module roughly doubles a fresh ship's durability. Grant flat amounts where possible: percents from different modules multiply (+200% and +200% give ×9), while flats add up as the model assumes.
