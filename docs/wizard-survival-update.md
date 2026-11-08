# Wizard Survival — Combat, Progression, and Feedback Update

Status: gameplay and presentation implementation authored; acceptance verification
has not been run. All numerical values are initial tuning defaults, not verified balance results.

This document records the update agreed through the design interview. The
[implemented design](wizard-survival-design.md) remains the baseline. Gameplay and Editor asset authoring now follow this specification; see the
implemented design for timing decisions and the verification document for pending checks.

## New features — dash, perfect dodge, and Deflect

### Dash and perfect dodge

- Press **Space** to dash three world units toward the mouse direction, captured
  when pressed. Travel takes **0.18 seconds**, with a **0.8-second cooldown**.
  Walls stop travel; enemies do not block it.
- Grant damage immunity throughout the dash. Show afterimages, a launch pop, and
  a clear ready indicator.
- A hostile projectile crossing the player's hitbox during the first
  **0.10 seconds** triggers perfect dodge. Later interception remains safe but
  grants no reward.
- Perfect dodge instantly refreshes every spell cooldown and dash. Automatic wand
  firing is unaffected.
- Slow enemies and hostile projectiles to **20% speed for 0.6 real seconds**;
  player movement and dash remain responsive.
- Each projectile grants perfect-dodge rewards once. It continues travelling
  afterward. Repeated successes refresh slow-motion duration rather than stacking
  its strength.
- Existing drawing slowdown still applies to the player while drawing. Use the
  stronger active slowdown for enemies rather than multiplying slowdowns.

### Deflect

- Available from the beginning. **Right-click outside UI**, **key 4**, or its HUD
  slot attempts Deflect.
- The attempt succeeds when an incoming projectile would hit within
  **0.08 combat seconds**. Catch the nearest imminent threat when several qualify.
- A mistimed attempt produces a whiff cue, spends the **four-second cooldown**,
  and leaves combat running without opening drawing.
- A successful attempt suspends one projectile and opens the right-side drawing
  panel. Trace a clockwise, diamond-shaped shield loop through five checkpoints
  within **three real seconds**.
- Movement and dash remain available during drawing. Combat continues at the
  existing **15% speed**; other attacks can damage the player.
- Successful drawing releases a damaging burst around the player and fires the
  caught projectile straight toward its attacker's current position. The returned
  projectile pierces enemies, damaging each once. Refresh all spell cooldowns and
  dash.
- Failed, cancelled, timed-out, or damage-interrupted drawing releases the
  projectile toward the player and grants no rewards. Keep Deflect's cooldown
  spent.
- If the attacker has died, aim toward its last recorded position. Death or run
  teardown removes the suspended projectile.
- Initial offensive values: **40 burst damage within three units** and
  **60 reflected damage**.

## New features — ranged enemy and threat readability

- Add a ranged enemy that retreats when the player is close, approaches when too
  far away, and holds position inside its preferred distance band.
- Starting defaults: **5–7 units preferred distance**, **0.35-second aiming
  telegraph**, **18 units/second projectile speed**, and **two-second individual
  attack cooldown**.
- Enemies share a firing scheduler with at least **0.65 combat seconds between
  launches**. Multiple projectiles may remain in flight.
- Aim at the player's position when the shot launches; projectiles travel straight
  without homing.
- Use round-robin firing among eligible enemies. Stunned, dead, or out-of-range
  enemies relinquish their turn.
- Projectiles expire on walls, player impact, or after three combat seconds. Use
  swept collision checks so fast shots cannot skip the player. A dash interception
  follows the continuing-projectile rule above rather than consuming the shot.
- Begin introducing this enemy at **60 combat seconds**. Preserve the existing
  boss transition.
- Display an atmospheric halo or outline when a projectile threatens the player's
  current position. Intensify it as impact approaches, without showing an exact
  parry-window indicator.
- Keep projectile silhouettes and telegraphs readable through spell effects.

## New features — upgrade cards, rarity, and stats

### Level-up choices

Offer exactly **one random elemental skill card and two distinct stat cards**;
choose one. Deflect is already acquired and does not occupy the elemental-card
pool.

Both card types roll **Common, Rare, Epic, or Legendary** independently.

| Rarity | Base probability | Upgrade strength |
| --- | ---: | ---: |
| Common | 65% | ×1 |
| Rare | 25% | ×1.5 |
| Epic | 8% | ×2 |
| Legendary | 2% | ×3 |

- Learning a spell always grants its standard rank-one version.
- Upgrading a learned spell adds one rank, with rarity multiplying that upgrade's
  damage and existing secondary-stat increments. Store the actual awarded bonuses
  so later upgrades retain their value.
- Show the exact resulting benefit before selection.

### Normal stat cards

| Stat | Common bonus |
| --- | ---: |
| Damage | +15 percentage points |
| Area | +10 percentage points |
| XP Gain | +15 percentage points |
| Luck | +5 points |
| Cooldown Reduction | +5 percentage points |
| Movement Speed | +5 percentage points |
| Maximum Health | +20 health |

Apply rarity multipliers to these bonuses. Bonuses accumulate additively; cap
cooldown reduction at **60%**. Increasing maximum health also grants the added
health immediately.

- **Area:** scales radial-effect radius and projectile width; does not increase
  dash distance, parry windows, projectile travel distance, or Thunder target
  count.
- **Damage:** affects every player attack.
- **Luck:** improves rarity, critical-hit chance, and bonus XP drops.
- For rarity, multiply each non-Common base weight by `1 + Luck / 100`, leave the
  Common weight unchanged, then normalize all four weights.
- Critical chance starts at **5%**, gains **0.5 percentage points per Luck point**,
  and caps at **60%**.
- Bonus XP-drop chance gains **one percentage point per Luck point**, capped at
  **50%**. Success creates one additional normal XP drop.
- Critical hits deal **2× damage**, rolled separately per enemy hit. Wand, spells,
  Deflect burst, and reflected shots can crit.
- XP Gain multiplies collected XP, including bonus drops; retain fractional
  progress internally.

## Balance feedback — spell power and XP

- Start with **double elemental spell damage and damage-upgrade increments**.
- Increase Fire width, Water radius, and Thunder targeting/link ranges by **50%**.
- Triple ordinary enemy XP values. Preserve current level requirements, XP
  overflow, and the five-minute boss timing.
- Keep these values configurable for later playtesting.

## Presentation feedback and additions

- Move drawing into a compact **right-side panel**, remove full-screen dimming,
  and keep the camera centered on the player so spell casts remain visible.
- Roll all three upgrade cards together for approximately **one real second**.
  Determine rewards before animation; skipping reveals those same rewards.
- Escalate bounce, outlines, particles, shake, and confetti by rarity. Keep
  confetti cosmetic and prevent it from obscuring card text.
- Strengthen spell launch, hit, and completion effects. Preserve effect pooling,
  bounded camera shake, reduced-flash settings, and reduced-shake settings.

## Detailed in-game feedback specification

The following presentation defaults expand the agreed design. They describe what
the player should see and understand; they do not change damage, collision,
invulnerability, or timing windows. Durations in this section use real time unless
explicitly stated otherwise. Audio production is outside this update's scope.

### Dash and cooldown readiness

- On an accepted dash, briefly stretch the player's visual along the captured
  direction, emit a small launch ring, and return the visual to its resting scale
  within 0.15 seconds. Keep the collision body unchanged.
- Spawn up to four translucent afterimages along the travelled path. Each fades
  within 0.20 seconds. Use a cool cyan tint and preserve the player's silhouette.
- While dash immunity is active, show a thin continuous outline distinct from
  the existing blinking used for immunity after taking damage.
- Show a Space-labelled dash indicator beside the spell HUD with a cooldown fill.
  When ready, pulse it once over 0.15 seconds; avoid a repeated flashing loop.
- A dash that stops at a wall ends its trail at the actual stopping point. Do not
  show a perfect-dodge cue unless a projectile met the perfect-dodge condition.
- Pressing dash during cooldown gives a small HUD nudge and briefly emphasizes
  its remaining cooldown. It produces no launch effect or camera shake.

### Perfect dodge and cooldown refresh

- At the qualifying crossing, emit a compact cyan-white ring at the player and
  show **PERFECT DODGE** above the character for approximately 0.6 seconds.
- Trigger one short, low-strength camera impulse, then let slow motion and the
  continuing projectile trail communicate the successful dodge. Add no hit-stop.
- Animate refreshed spell and dash cooldown fills directly to ready and pulse
  their borders together once. Show **COOLDOWNS RESET** as a smaller HUD cue.
- Ordinary immunity interceptions use only a small pass-through ripple. They do
  not display success text, refresh cooldowns, or trigger slow motion.
- Repeated successes refresh the existing announcement instead of stacking text.
  The same projectile cannot produce another success burst or announcement.

### Deflect attempt, drawing, success, and failure

| State | In-game feedback | Meaning for the player |
| --- | --- | --- |
| Ready | Shield icon with RMB / 4 labels and an unfilled cooldown overlay | Deflect can be attempted |
| Mistimed attempt | A brief broken shield outline, a subdued outward puff, and **MISSED TIMING** for 0.5 seconds; begin HUD cooldown | The attempt was spent without catching a shot |
| Successful catch | Draw a bright diamond cage around the suspended projectile and a thin tether to the player; show **CAUGHT — DRAW TO DEFLECT** | One shot is held, but the reward is not complete |
| Drawing | Show the shield-loop guide, numbered checkpoints, next-checkpoint highlight, and three-second countdown in the right panel | Complete the trace while continuing to evade other threats |
| Drawing completed | Close the panel, expand a shield-shaped burst around the player, launch the piercing return shot, and pulse reset cooldowns | The defensive chain succeeded and granted all rewards |
| Drawing failed | Break the cage into a few fragments, show the failure reason, and briefly accent the released hostile projectile | The held threat is dangerous again |

- Use **DEFLECT!** for completed drawing, plus a medium camera impulse lasting
  at most 0.15 seconds. The burst edge must match the actual damage radius.
- Distinguish the reflected shot with a bright friendly core and diamond-shaped
  trail. Preserve a readable silhouette without relying only on its color.
- Failure messages distinguish **TRACE FAILED**, **TIME EXPIRED**, **CANCELLED**,
  and **INTERRUPTED**. Keep the message outside the player's immediate combat area.
- Damage interruption retains the normal damage feedback. Do not add a second
  strong camera impulse for the released projectile.
- Catch and failure effects must not delay, teleport, or alter the projectile
  beyond the agreed suspension and release mechanics.

### Ranged enemy, projectile, and atmospheric danger sense

- Give the ranged enemy a distinct pointed silhouette and visible firing focus.
  Its retreat/approach motion should remain legible without continuous particles.
- During its 0.35-combat-second aiming telegraph, contract a ring toward the
  firing focus and briefly emphasize the enemy outline. Play the launch flash
  only when the firing scheduler actually authorizes the shot.
- Render hostile shots with a solid high-contrast core, outer border, and short
  directional trail. Wall impact creates a small local spark; expiration fades
  the shot without an impact or success announcement.
- The player's danger sense uses a soft halo and short curved strokes near the
  head. Increase their size and pulse strength as an actual collision threat
  approaches. Do not display a precise countdown or exact parry-window flash.
- Several threatening shots drive one halo using the most imminent threat.
  Clear it smoothly when the threat misses, expires, is caught, or is removed.
- Keep the halo distinct from dash immunity and Deflect's diamond cage. A caught
  projectile no longer contributes danger until it is released as hostile.

### Stronger spell casts and combat impacts

| Spell or event | Detailed feedback |
| --- | --- |
| Fire | Brief casting recoil, a thicker orange lance with a bright inner core, a tapered trail, and a compact spark burst at each pierced enemy |
| Water | A fast expanding blue ring whose edge matches the increased radius, outward-moving motes, a short impact pop on affected enemies, and retained overhead stun diamonds |
| Thunder | A brief player charge outline followed by jagged links along the actual target sequence; emphasize each struck enemy once and let links fade quickly |
| Shatter Storm | A stronger combined ice/lightning burst and **SHATTER STORM** cue; use one bounded camera impulse for the cast |
| Critical hit | A larger damage number with a **CRIT** marker and slightly stronger local impact sparks; no per-target camera shake |
| Enemy death | A short collapse/pop and local radial burst, followed by clearly visible XP drops |

- Scale spell visuals with the real coverage and Area upgrades. Decorative motes
  may fade outside the hit shape but must not resemble additional damaging edges.
- Reserve medium camera impulses for elemental casts, completed Deflect, and
  combos. Wand hits, individual chain links, and ordinary XP pickups use local
  feedback so dense fights do not shake continuously.
- Thunder with no target displays **NO TARGET** and a small fizzle; preserve its
  existing cooldown-consumption rule.

### XP, level-ups, and stat feedback

- Preserve readable XP-orb silhouettes despite larger enemy XP rewards. On pickup,
  send a short visual streak toward the XP bar and animate the bar to its actual
  progress. Merge rapid pickup readouts rather than covering the arena with text.
- Mark Luck-generated bonus drops with a small star-shaped sparkle, without
  making them look like a separate currency.
- Announce **LEVEL UP** once per upgrade screen. Show queued choices with a small
  counter rather than overlapping announcements when XP grants multiple levels.
- Stat cards show the bonus, rarity, and before/after total. Luck cards show their
  resulting rarity, critical-chance, and bonus-drop benefits. Cooldown cards show
  the actual capped benefit rather than an unavailable nominal increase.
- Skill cards show **LEARN** or **UPGRADE**, the element, resulting rank, and exact
  damage/effect change. Learning cards clearly state that rarity does not improve
  the initial rank-one spell.
- After a selection, pop the selected card briefly and pulse the affected HUD
  icon or stat summary. Update values immediately; cosmetics do not delay the
  next queued choice or resume input.

### Slot-machine reveal and rarity effects

- During the approximately one-second reveal, scroll decorative symbols or card
  backs within each card frame, slow toward the end, then settle all three cards
  into their already-determined rewards. Do not imply that player timing changes
  the result or show fabricated near-misses.
- Keep the title and benefit readable after settling. Enable selection only once
  results are revealed. A skip input reveals all fixed results and is consumed,
  so that same input cannot also select a card accidentally.
- Run reveal animations while combat is paused, using unscaled presentation time.

| Rarity | Border and label | Reveal treatment |
| --- | --- | --- |
| Common | Neutral border and COMMON label | Small scale pop, quick ease-out settle, no confetti or camera shake |
| Rare | Blue border and RARE label | Stronger bounce, one outline pulse, a few local sparkles |
| Epic | Purple double border and EPIC label | Larger bounce, a short radial accent, light confetti behind the card row |
| Legendary | Gold ornamental border and LEGENDARY label | Strongest bounded bounce, expanding outline, gold confetti, and one short camera impulse |

- Keep the entire reveal near one second even when several cards are rare.
  Multiple Legendary cards share one impulse and one bounded confetti burst.
- Skipping omits the rolling and large bursts; retain final rarity borders,
  labels, and a brief settle so the result remains clear.

### Drawing panel, readability, and effect limits

- Keep the right panel inside the existing safe area. Start with approximately
  25% of the safe-area width, tuning its size to preserve readable checkpoints at
  supported resolutions. Do not move the arena camera during panel transitions.
- Use a subtle panel-only background. Keep the player, enemy attacks, damage
  zones, and cast origin visible without full-screen dimming.
- On checkpoint completion, give a small local pop and advance the highlight.
  Increase countdown emphasis in the final real second without strobing.
- On cast completion, close the panel promptly so the spell's main impact remains
  visible. Show success or failure text near the HUD rather than over the player.
- Prioritize player/threat silhouettes, attack telegraphs, and suspended shots
  above ordinary sparks, damage numbers, and confetti. Drop excess cosmetic
  requests before obscuring gameplay or exceeding existing effect-pool limits.
- Reduced Flash removes bright pulses and uses stable outlines, labels, and
  softer particles. Reduced Shake suppresses or scales camera impulses using the
  existing setting. Rarity and success states remain recognizable through shapes
  and text as well as color.
- Shake affects only the camera presentation and respects existing arena bounds.
  Scale/bounce affects visual children, never physics bodies or hitboxes.
- Cancel temporary feedback and restore visual scale, tint, trails, and HUD state
  on death, retry, or run teardown. Preserve no particles or animation callbacks
  from the previous run.

## Acceptance scenarios — for later implementation verification

- Dash direction locks on activation, respects walls, and distinguishes perfect
  timing from ordinary immunity.
- Multiple ranged enemies stagger launches fairly; fast projectiles reliably
  register crossings.
- Deflect handles successful drawing, whiffs, cancellation, timeout, damage
  interruption, multiple threats, and a dead attacker.
- Cooldown resets include dash and Deflect, without changing wand cadence.
- Perfect-dodge slow motion, drawing, pause, and upgrade menus interact
  consistently.
- Every level-up contains one skill and two distinct stats; rarity bonuses persist
  correctly.
- Luck affects all three intended systems; XP multipliers preserve fractional
  progress.
- Skipping reveals never changes rewards.
- Restart clears suspended shots, firing turns, slow motion, cooldowns, and
  effects.
- Drawing and combat remain readable at supported screen sizes.
- Dash, ordinary immunity, perfect dodge, caught Deflect, completed Deflect, and
  failed Deflect remain visually distinguishable during crowded combat.
- Slow motion and paused menus do not stall real-time feedback or card reveals;
  skipping a reveal does not accidentally select a reward.
- Reduced Flash and Reduced Shake preserve essential threat, cooldown, and rarity
  information without strong flashes or camera motion.
- Spell edges match their effective areas, critical hits are identifiable without
  per-hit shake, and repeated rewards do not stack unreadable announcements.

No gameplay checks have been run for this implementation; project verification
remains opt-in. These scenarios are pending acceptance criteria, not completed checks.

## Implementation decisions

Perfect dodge slows combat timers as well as threats. Player movement and dash
remain at player speed, with drawing retaining its player slowdown. The 0.6-second
perfect-dodge duration stops during paused menus. The Warden retains its simultaneous
burst; the round-robin launch gap applies to ordinary ranged enemies. These decisions
were confirmed during implementation planning. Dash cooldown is measured in player
time; elemental and Deflect cooldowns use combat time.

## UI and control follow-up

The follow-up request overrides the original mouse-directed dash and ranged tuning:
dash now captures held movement direction, falling back to the last movement
direction when idle. Perfect-dodge and Deflect windows are now 0.12 seconds in
their existing clocks. Ranged enemies use a 9–12-unit preferred band, a 0.5-second
telegraph, and 26-unit/second hostile projectiles. Upgrade/HUD styling and rarity
reveals are redesigned with real edge strips, perimeter sparks/rays, gold corner
ornaments, and bounded confetti; remove the opaque Epic-card border overlay.

## Projectile size and Deflect immunity follow-up

Latest request overrides damage-vulnerable Deflect drawing: a correctly timed
catch protects the player until success, failure, cancellation, or timeout. The
protection derives from the active held shot, and clears on death/restart/teardown.
A whiff or elemental trace grants no protection. Hostile projectiles double in
size, with configurable 0.3-unit sweep radius; returned shots retain this size
and apply Area. Visual width, swept collisions, and threat prediction stay aligned.

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
