> Historical report for the previous mouse-aimed combat implementation.
> The elemental-combat revision changes these behaviors and test cases.
> Its tests, Play Mode, validators, and profiling have not been run.

# Wizard prototype verification

Verified in the connected Unity 6000.3.15f1 Editor on 2 November 2026.
No player build was produced.

## Functional checks

Compilation completed with zero errors and zero warnings.

- **7/7 Edit Mode tests passed:** persisted primitive texture; XP overflow and queued levels; unlock eligibility
  and reset; five-rank multiplicative cooldowns; distinct choices and exhausted-rank
  healing; persistence and corrupt-primary backup recovery; reported write failure.
- **10/10 Play Mode tests passed:** boss spawns once and victory requires its death;
  telegraph precedes charge and boss remains bounded; damage invulnerability and
  repeated retry reset; held menu clicks cannot cast on resume; hostile wall hits
  cannot hurt the player; normalized diagonal movement through the new Input
  System; pause freezes gameplay; queued upgrades; swept bolt hits once;
  unlocked Fireball/Nova area damage and slowing preserve authored definitions.
- Persistence tests use temporary paths and reload through new service instances.
  They do not modify the player's saved personal bests.
- Live frame counters confirmed Play Mode was advancing. Runtime console checks
  after fixes and under stress showed no errors.

## UI and camera

Title/HUD and upgrade layouts were inspected at **1280×720**, **1920×1080**,
**1920×1200**, and **3440×1440**. Automated geometry/text checks found no zero-size,
off-screen, or overflowing active controls. The upgrade cards were also captured
and visually reviewed; their headings, descriptions, and ranks now align.
Controls, settings, and pause panels were checked separately. Keyboard navigation
uses explicit cyclic neighbors, with a selected button on every menu.

At the top-right arena corner, the 16:9 viewport covered approximately
`(-5.56, 0)` to `(30, 20)`, remaining within the authored camera bounds.
Very wide windows reduce orthographic size when necessary to keep the view bounded.

## Performance

Repeatable Editor scenario: **150 enemies and 200 moving projectiles**, maintained
for a three-second warmup and twelve-second measured interval at 1920×1080.
Frame rate was uncapped and the original frame-limit settings restored afterward.

Hardware: AMD Ryzen 7 6800H / NVIDIA GeForce RTX 3060 Laptop GPU.

| Metric | Result |
| --- | ---: |
| Samples | 936 |
| Average frame | 12.83 ms (~78 FPS) |
| 95th-percentile frame | 14.86 ms |
| Average main thread | 12.81 ms |
| 95th-percentile main thread | 14.84 ms |
| Average recorded GC allocation | 20,977 bytes/frame |
| Maximum recorded GC allocation | 54,700 bytes/frame |

The measured scenario met the 16.67 ms target without pooling. No speculative
optimization was introduced. Counters include Editor/tooling overhead and
synchronization; GPU time was not measured. This is not a player-build performance
guarantee, and a moving/attacking crowd can differ from the controlled stress setup.

To reproduce, enter Play Mode and call `WizardStressScenario.Begin` with the scene's
`GameplayInstance` and a Chaser definition. The Editor-only tool creates temporary
enemy configuration, reports to `Temp/WizardQA/stress.json`, restores settings,
and returns to the title. It is not attached to the shipped scene.

## Asset validation and limitations

The read-only Unity asset validator found **no GUID or script-reference errors in
the new game assets**. Full-project validation still reports four pre-existing
missing script GUIDs in `Assets/Settings/DefaultVolumeProfile.asset`. That template
file was preserved. Automatic approval review rejected removing its stale
subassets as a destructive change outside the approved game scope.

The original template scene, packages, plugin content, and unrelated user edits
were retained. DOTween's module assembly definition was added so the runtime and
test assemblies can use its extension methods.

The ten-minute transition was exercised by advancing the run clock in tests;
an uninterrupted ten-minute human playthrough and an uncoached external playtest
were not performed. Balance values remain prototype defaults. Once ranks are
exhausted, fewer than three distinct valid options may remain; repeatable healing
is offered without duplicating cards.

## Survival update — pending acceptance

The survival update adds source coverage in WizardUpdateTests (Edit Mode) and
WizardGameplayTests (Play Mode). Those tests have been authored but not run.
Earlier reports do not verify this revision. Do not run this checklist or any
verification tooling without a separate explicit request.

| Area | Later acceptance scenario |
| --- | --- |
| Dash | Capture mouse direction once; move exactly three units; stop at a wall; cross enemies; cooldown nudge produces no launch effect. Repeat while drawing and during perfect-dodge slow motion. |
| Crossings | Fire a fast shot across a moving player's hitbox; first 0.10 player seconds reward once, later immunity only. Continue the projectile through the player. Intercept the same shot again without another reward. |
| Scheduling | Spawn several eligible ranged enemies; record launches and confirm round-robin fairness, at least 0.65 combat seconds between launches, two-second personal cooldown, and 0.35-second telegraph. Stun/kill/move the selected enemy out of range. Preserve Warden's simultaneous burst. |
| Deflect | Catch the nearest imminent shot among several; whiff spends cooldown; wall-blocked and expiring shots do not qualify. During tracing other threats remain dangerous and movement/dash remain active. |
| Deflect resolution | Exercise valid clockwise trace, wrong order, early release, timeout, Escape, actual damage interruption, and death. Failed shots resume from their held position toward the player; remaining lifetime resumes. Kill the attacker while its shot is held and confirm the return direction uses its last position. |
| Offense | Burst edge matches effective radius; reflected shots pierce each enemy once; Damage, Area, and per-hit criticals affect every intended attack. Area does not extend dash/Fire travel, parry windows, or Thunder count. |
| Clocks | Combine drawing/perfect dodge/pause/upgrade menus. Choose 15% over 20%, preserve player responsiveness, stop active-real-time slow-motion duration in paused menus, keep presentation moving, and preserve wand cadence on cooldown resets. |
| Offers | Always one elemental card and two distinct stat cards; rarity rolls independently. Legendary learning remains rank one. Mixed-rarity upgrades retain their awarded bonuses; stun caps at three seconds. |
| Stats and XP | Check every additive stat, exact capped CDR previews, immediate maximum-health healing, fractional XP/overflow, triple ordinary drops, Luck rarity/crit/bonus drops, and XP Gain on bonus drops. |
| Reveal input | Skip using click, Enter, or keys 1–3. Verify fixed rewards and no same-input selection, including held inputs and UI event processing order. Queue several levels and select without cosmetic delays. |
| Readability | Check 16:9, 16:10, ultrawide, portrait, and smaller windows. Keep checkpoints readable, panel inside safe area, and the arena camera centered. Check card titles/benefits and Luck details for overflow. |
| Feedback | Distinguish dash immunity, ordinary immunity, perfect dodge, caught/successful/failed Deflect in crowded combat. Repeated announcements replace each other. Criticals and chain links never shake per target. |
| Accessibility | Reduced Flash retains stable outlines/text and softer particles. Reduced Shake suppresses impulses; camera bounds hold after impulses. Rarity remains recognizable by text and shapes. |
| Cleanup | Die/retry/return to title while dashing, holding a shot, aiming, revealing, and showing confetti. Remove held shots, firing turns, slow motion, cooldown state, trails, announcement effects, and callbacks. Restore physics timestep on scene teardown. |
| Authoring | Reapply the migration without duplicated named objects, changed existing GUIDs, multiplied balance values, or additional input wrappers. |

No tests, builds, compilation checks, Play Mode sessions, validators, formatters,
or profiling were requested for this implementation. Unity script imports and
Editor scene/prefab authoring are implementation operations, not gameplay evidence.

### UI/control follow-up — still unverified

Check movement-directed dash (including idle fallback and changed input during
travel), ranged shots at 9–12 units/26 speed, and 0.12-second timing windows. Inspect
all four rarity cards together: readable Epic text; distinct static borders and
unscaled reveals; no particles across benefits; hover/gamepad-keyboard focus;
skip input consumption; Reduced Flash/Shake; narrow screens; retry cleanup.
Existing movement/scheduler acceptance tests were updated but not run.

### Larger shots / Deflect protection — pending

Check larger hostile and reflected shots against walls and the player; danger
sensing and catch timing must use the enlarged radius. A timed catch must block
contact and projectile damage throughout drawing without interrupting the trace.
Protection must end immediately on success, failure, Escape, timeout, or teardown;
released failed shots must become dangerous again. Whiffs and elemental drawing
remain vulnerable. Existing Deflect acceptance assertions were updated, not run.

### Deflect misclick forgiveness — pending

Without running checks: later exercise off-panel clicks, clicking/releasing away
from checkpoint 1, dragging onto 1 while held, and releasing a partial stroke.
Deflect must stay open and immune while the original deadline continues. A fresh
valid trace must still complete. Wrong checkpoint order, timeout, and Escape must
release the shot and end immunity, with visible broken-sigil/HUD feedback. Check
ordinary elemental failures, Reduced Flash, repeated cues, and retry cleanup too.

### Five-shooter cap / untimed Deflect — pending

Acceptance coverage is authored, not run: reject a sixth shooter, permit a
replacement after death, preserve normal spawns and Warden behavior at the cap,
and reset counts on restart. Keep Deflect open/protected beyond three seconds and
through wrong-order/partial/off-start retries; success returns the shot, Escape
cancels it, and teardown clears it. Ordinary spell deadlines still apply.
