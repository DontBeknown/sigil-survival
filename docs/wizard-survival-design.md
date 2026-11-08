# Wizard Survival — authored gameplay

Unity 6000.3.15f1, desktop keyboard and mouse. Gameplay and presentation changes are
implemented in source and the Editor migration. Acceptance verification has not
been run. Numerical values are configurable starting points, not tested balance.
The detailed presentation contract is [wizard-survival-update.md](wizard-survival-update.md).

## Controls and run loop

- WASD/arrows move; the wand automatically targets within eight units every 0.6
  combat seconds. Damage and critical upgrades affect bolts; cooldown upgrades and
  defensive cooldown resets do not alter wand cadence.
- Space dashes three units in the held movement direction captured on activation (last movement direction when idle). It
  travels for 0.18 player seconds, stops at walls, ignores enemies, grants immunity,
  and has a 0.8-player-second cooldown. The first 0.12 player seconds can perfect
  dodge a projectile crossing. Each shot rewards once and continues flying.
- Keys 1–3 or HUD slots select learned Fire/Water/Thunder for guided tracing. Hold
  LMB at checkpoint 1, trace in order, and release. The right-side panel has a
  three-real-second deadline. Escape cancels drawing before ordinary pause.
- RMB outside UI, key 4, or its slot attempts Deflect. Spend its four-combat-second
  cooldown immediately. Catch the nearest shot predicted to hit within 0.12 combat
  seconds; otherwise show MISSED TIMING without opening drawing. Clockwise diamond
  tracing suspends one shot and grants damage immunity until the trace resolves; combat continues.
- Completed Deflect deals 40 base damage in three units and returns a 60-base-damage
  piercing shot toward the attacker's current or last recorded position. Each enemy
  is hit once. Failure, cancellation, or timeout releases
  the held projectile toward the player, without rewards. Suspension freezes its
  remaining lifetime; release resumes it. Death and teardown remove held shots.
- Perfect dodge and successful Deflect reset elemental, Deflect, and dash cooldowns.
  Perfect dodge also slows combat to 20% for 0.6 active real seconds. Success cues
  refresh rather than stacking repeated announcements.
- XP orbs retain overflow and fractional progress. Requirements remain 5, 8, 11…;
  every earned level queues one choice. At 300 combat seconds, ordinary spawning
  ends and the Warden arrives. Defeating it wins; zero player health loses.

## Time ownership

GameplayInstance controls global combat time. Use the stronger active slowdown:
15% drawing or 20% perfect dodge, never their product. Combat time advances spawning,
attack telegraphs, stun, contact immunity, projectile travel/lifetime, spell cooldowns,
combos, and boss arrival. Player movement and dash compensate for perfect-dodge
slowdown and retain drawing's 15% slowdown. Dash cooldown uses player time.

Menus freeze gameplay and the remaining perfect-dodge duration. Drawing deadlines,
card reveals, feedback, and UI animation use unscaled time. Physics timestep scales
with combat time to preserve responsive real-time sampling; teardown restores it.

## Spells, enemies, and damage

Rank one always learns the standard version regardless of rarity:

| Spell | Base effect | Cooldown | Common rank increment |
| --- | --- | --- | --- |
| Fire | 80 damage, piercing, eight-unit travel, 0.42 sweep radius | 6s | +30 damage |
| Water | 10 damage, 4.5-unit pulse, 1.5s stun | 8s | +10 damage, +0.3s stun capped at 3s |
| Thunder | 40 damage, nine-unit acquisition, 4.5-unit links, four targets | 7s | +16 damage |

Water followed by Thunder within three combat seconds triggers Shatter Storm:
six targets and 1.5× damage. Fire clears the sequence; ordinary wand shots and
failed tracing do not change it. Successful elemental traces consume cooldown even
without targets; failed elemental traces retain their one-combat-second lockout.
Deflect does not alter the elemental combo sequence.

The ranged family joins the ordinary spawn pool at 60 combat seconds. It retreats
below nine units, approaches beyond twelve, and holds its band. Eligible enemies
receive round-robin aiming turns, telegraph for 0.5 combat seconds, then fire at
the player's launch-time position. Individual cooldown is two seconds; ordinary
ranged launches are separated by at least 0.65 combat seconds. Invalid enemies
relinquish their turn. The Warden retains its simultaneous 16-shot burst.

Hostile shots travel at 26 units per combat second, expire after three combat
seconds, and stop on walls or ordinary player impact. Swept collision includes
relative player motion during dash. Danger sensing and Deflect prediction exclude
wall-blocked, suspended, expired, and friendly shots.

All player attacks use additive Damage bonuses and roll criticals separately per
enemy hit. Criticals deal 2× damage. Area scales radial-effect radius and projectile width, preserving Fire travel
distance, Thunder acquisition/link ranges, and Thunder target count. Ordinary enemy XP rewards are multiplied by three at drop creation.

## Upgrade offers

Each screen fixes one random elemental skill and two distinct stat offers before
its approximately one-second reveal. Selection is disabled until revealed. Skipping
reveals the same rewards, consumes its input, and cannot also select a card.
Queued choices resolve immediately; cosmetics do not block the next choice.

Rarity weights are Common 65, Rare 25, Epic 8, Legendary 2. Multiply non-Common
weights by `1 + Luck / 100`, then normalize. Strength multipliers are 1, 1.5, 2, 3.
Learning does not scale rank one. Later spell upgrades store actual awarded damage
and secondary bonuses separately from rank. Stat bonuses accumulate additively:

| Stat | Common award |
| --- | --- |
| Damage | +15 percentage points |
| Area | +10 percentage points |
| XP Gain | +15 percentage points |
| Luck | +5 points |
| Cooldown Reduction | +5 percentage points, capped at 60% |
| Movement Speed | +5 percentage points |
| Maximum Health | +20 health, also healed immediately |

Crit chance is `min(60%, 5% + Luck × 0.5%)`; bonus-drop chance is
`min(50%, Luck × 1%)`. A successful bonus-drop roll adds one ordinary XP drop with
a star accent. XP Gain applies on collection, including bonus drops. Card previews
show actual before/after benefits, capped awards, resulting rank, and rarity.

## Authoring, presentation, and persistence

The entry scene remains `Assets/Scene/Initialize.unity`; code remains under
`Assets/Script/Wizard/AiTesting` with `VonderGames.AiTesting` namespaces.
`WizardElementalSetup.Apply` updates the existing scene, UI prefab, input asset and
generated wrapper, spell data, ranged data, and DefaultCombat configuration in
place. Existing GUIDs, enum values, package versions, and save format remain intact.
Repeated application uses absolute defaults and existing named children.

Player and threat indicators use dedicated entity-owned renderers. Cosmetic effects
retain caps of 96 particles, 24 lines, and 48 numbers; upgrade confetti reuses at most
32 pieces behind cards. Feedback and reveals use unscaled time. Reduced Flash and
Reduced Shake retain text, shapes, and threat information. Camera shake remains
bounded to the arena; bounce/stretch affect visual children. Restart clears held
shots, scheduler state, slow motion, cooldowns, temporary feedback, and reveal state.

Settings and personal bests use the existing SaveService. There is no run-save
migration, artwork production, audio, player build, or publishing in this update.

## UI redesign follow-up

Upgrade cards now use a dark arcane frame, category header, element/stat sigil,
separate benefit area, keyboard badge, and rarity footer. Actual thin edge images
replace the full-card Outline that previously hid Epic card text. Common settles
quietly; Rare adds blue sparks and a border impulse; Epic adds a violet double
frame and perimeter rays; Legendary adds gold corner ornaments and stronger
perimeter rays/confetti. Reduced Flash softens all accents. Hover and keyboard
focus brighten only the frame/background subtly. Menu, drawing, and HUD palettes
match the cards; health/XP/cooldown fills use an explicit sprite so their fill
amounts render correctly. This revision has not been playtested or visually verified.

## Projectile size and Deflect protection follow-up

Hostile projectiles now have a configurable 0.3-unit sweep radius and a 0.9 by
0.6-unit visible silhouette, double their former size. Reflections retain the
larger radius, multiplied by Area. Threat prediction uses the enlarged hit shape.
A correctly timed Deflect grants immunity to all player damage while its shot
is suspended and the Deflect trace is active. A steady diamond around the player
and PROTECTED panel heading show the state. Success, trace failure, cancellation,
or timeout ends this protection immediately; a whiff grants none. Ordinary elemental
drawing remains vulnerable. No gameplay verification was run for this follow-up.

## Deflect input forgiveness and drawing failure feedback

Clicks outside checkpoint 1 no longer fail Deflect. Holding and moving onto the
start checkpoint can begin the stroke; releasing an incomplete Deflect stroke
resets just the stroke for another attempt. The original three-real-second timer
and caught projectile are retained, with protection active. Wrong checkpoint order,
timeout, and explicit cancellation still resolve as failures. Elemental tracing
retains its existing rules. All failed/cancelled/timed-out traces now produce a
small warm-colored fractured sigil and sparks plus a 1.5-second HUD failure reason
and brief HUD nudge. Effects use unscaled time, bounded pools, and reduced-flash
settings; no extra camera shake or gameplay delay is added.

## Five-shooter cap and untimed Deflect — latest behavior

At most five ordinary ranged/shooter enemies may be active simultaneously. The
spawn pool falls back to ordinary melee choices at the cap; the shared SpawnEnemy
entry point also rejects extra shooters. Death frees a slot. DefaultCombat authors
the cap as five; Warden attacks and in-flight projectiles are unaffected.

After a successful catch, Deflect drawing has no deadline and cannot fail from
wrong checkpoint order, misplaced clicks, or early release. Invalid strokes reset
for unlimited retries while retaining the held shot and damage immunity. Show
NO TIME LIMIT / PROTECTED instead of a countdown. Explicit Escape cancellation,
boss transition, death, and teardown still exit safely and remove protection;
Escape releases the shot without rewards. Ordinary elemental traces retain their
three-second deadline and existing failure feedback. This supersedes earlier
Deflect timeout/wrong-order failure descriptions. Verification was not run.
