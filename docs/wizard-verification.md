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
