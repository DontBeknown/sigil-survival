# AI Testing — Wizard Survival

Use the `vonder-unity-project` skill and its standards/code/scene references as the
project standard. Unity 6000.3.15f1, URP, Input System 1.19, Cinemachine 3.1.7,
DOTween, TMP, and uGUI are already installed. Preserve package versions, imported
plugins, unrelated user edits, asset GUIDs, serialized enum values, and saves.

## Code and ownership

Source root: `Assets/Script/Wizard/AiTesting`. Retain the established
`VonderGames.AiTesting` namespaces and current asset GUIDs.
Use singular folder names, private `_camelCase` fields, serialized private fields
with adjacent read-only properties where needed, explicit access modifiers,
Allman braces, Vonder member ordering, and `On...` / `...Handler` events/callbacks.
One MonoBehaviour per file. Keep pure gesture, combo, and progression logic in Core.
Do not introduce global searches, automatic singletons, or speculative managers.

`InputManager` owns the only generated input wrapper. All consumers share it; the
UI module uses its runtime asset. Edit the input asset and regenerate its wrapper
through Unity tooling rather than editing generated C# directly. Gameplay owns
Player-map activity; UI stays active for drawing, menus, and number-key choices.
`GameplayInstance` composes runtime services and retains serialized scene references.
`RunSession` owns transitions and run records; `GameplayClock` owns time policy;
`CooldownTracker`, `EnemySpawner`, and `ProgressionController` own their feature state.
Authored ScriptableObject configuration must not be mutated in Play Mode.

Runtime entities receive focused owners instead of GameplayInstance. Player behavior
is composed from PlayerHealth, PlayerMotor, PlayerDash, and PlayerPresentation;
enemy decisions use melee/ranged/boss strategies. UI behavior belongs to GameplayHud,
RunMenuPanel, UpgradeSelectionPanel, and SafeAreaLayout. Keep serialized component
identities/fields stable when editing the scene adapters.

EnemySpawner, ProjectileSpawner, and ExperienceSpawner own scene-local pools backed
by SceneEntityPool. Initialize inactive leases before activation; normal death,
impact, expiry, and collection return leases. Remove registry/scheduler membership,
detach attacker references, clear status/hit/visual state, and cancel transient work
before reuse. Fire hit history and developer projectile tracking use lease versions.
EnemyDeathController owns rewards and kill/victory accounting, not pool storage.
The root initializes input and services before entities/UI; run cleanup releases
Deflect holds before returning projectiles, then clears enemies/pickups on restart.
Dispose subscriptions and pools at teardown. See `docs/runtime-architecture.md` for
the full ownership map, constructor order, and lazy-growth/retention policy.

## Scene and assets

Entry: `Assets/Scene/Initialize.unity`.
- Manager: InputManager and the single EventSystem.
- GameplayInstance: run composition, explicit dependencies, and CombatFeedback.
- CameraRig: MainCamera and ArenaCamera; final camera bounds include shake.
- Level/Environment: Floor and Grid; Level/Collision: Wall and CameraBounds.
- Level/Entity: Player and RunObjects (spawned enemies, projectiles, XP).
- Level/Effect: reusable runtime particles and spell lines.
- UICanvasGameplayInstance/CanvasScreenSpace: SafeArea, HUD, modal menus, drawing.
- UICanvasGameplayInstance/CanvasWorldSpace/DamageTextContainer: pooled damage text.

Prefabs are grouped in `Assets/Prefab/Entity`, `UI`, and `Effect`. Use Editor asset
moves to retain GUIDs. Scene migration is `WizardElementalSetup.Apply`, also exposed
through the Wizard menu. It edits the existing scene and prefab contents.
All scenes belong in `Assets/Scene`; SampleScene is a preserved, disabled template.
The screen UI prefab is `Assets/Prefab/UI/CanvasScreenSpace.prefab`, matching its root.
`WizardProjectStructure.Apply` handles structure-only organization without combat
tuning changes. See `docs/project-structure.md` for the actual folder and scene trees.

## Combat

Neutral bolts auto-target and fire. Fire/Water/Thunder use guided tracing, selected
with 1–3 or slots. Drawing continues combat and WASD at an Inspector-configurable
0.15 time scale, with a three-real-second deadline. Upgrade choices and ordinary
pause stop combat. Element ranks are uncapped. Water then Thunder within three
combat seconds triggers Shatter Storm. Warden arrives after 300 combat seconds;
its death wins. The GameplayInstance Inspector has a Play Mode Skip to Boss button.

## Survival update

Space dashes in the held movement direction (or last movement direction when idle); RMB outside UI, key 4, or the
fourth HUD slot attempts Deflect. Deflect starts unlocked and is excluded from
upgrade offers. Every level-up offers one elemental skill and two distinct stats.
Offers are fixed before the reveal animation and store their awarded rarity bonuses.

GameplayClock owns the stronger of drawing (15%) and perfect-dodge (20%) combat
slowdowns. Combat timers and the boss clock use that rate. Player movement and dash
use player time: normal speed during perfect dodge, 15% during drawing. Dash cooldown
uses player time; spells and Deflect use combat time. Drawing deadlines, feedback,
and card reveals use unscaled time. Perfect-dodge duration counts active real time
and stops during paused menus. Physics timestep scales with combat time to retain
real-time collision sampling. Restore the authored timestep on teardown.

The ranged enemy enters the spawn pool at 60 combat seconds and shares a round-robin
scheduler. Preserve the Warden's simultaneous 16-shot burst as an explicit exception.
All hostile shots use swept collisions and support dodge and Deflect. Track suspended
projectiles explicitly and remove them on death/restart/teardown.

Combat tuning is authored in DefaultCombat, spell, enemy, and difficulty assets;
progression applies bonuses at runtime without changing those assets. Preserve the
48-number, 96-particle, and 24-line cosmetic caps. Essential persistent threat and
immunity indicators are entity-owned; card confetti is limited to 32 reused pieces.

## Verification is opt-in

Do not run tests, builds, compilation checks, Play Mode, linters, formatters,
validators, or profiling unless explicitly requested. Necessary Unity script/asset
imports and Editor authoring are part of implementation, not a verification claim.
Finish with artifacts, remaining wiring, and an accurate statement of checks run.
No player builds, artwork, Animator content, audio production, or publishing.

UI cards use UIUpgradeCard and real edge strips, never an Outline on a full-card
foreground rectangle. Rarity effects are unscaled and bounded to 12 sparks and
16 rays per card plus the shared 32-piece confetti pool. Keep these effects around
card edges. New tuning: ranged distance 9–12, telegraph 0.5 combat seconds, hostile
speed 26, Deflect/perfect-dodge windows 0.12 seconds in their respective clocks.

A successful Deflect catch grants damage immunity only while the caught projectile
is suspended and the Deflect drawing is active. End this protection immediately
on success, failure, cancellation, timeout, death, or teardown. Whiffs and ordinary
elemental drawing grant no immunity. Hostile/reflected shot radius defaults to
0.3 units (double the previous size); visuals, swept hits, and threat prediction
use this same radius. The player protection diamond is distinct from dash outline.

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

## Progression, explosions, and tracing update

Boss maximum HP is snapshotted at spawn from authored health multiplied by
`1 + (player level - 1) * BossHealthPerLevel` (default 0.15). Its HUD and half-health
recovery threshold use this runtime maximum. XP required for each level is
`5 + 3 * (level - 1) + (level - 1)^2`; overflow and queued choices are retained.
Thunder starts at four targets, gains one target and 0.75 link range per rank
above one, and retains two extra combo targets. Upgrade cards show these changes.
Fire retains piercing and adds full-damage impact explosions (1.8-unit base radius,
scaled by Area) on enemies, walls, and travel expiry. Each enemy can be damaged
only once per fireball across direct hits and explosions; cosmetic pool caps remain.
Perfect dodges heal 10% of maximum HP, capped at maximum, once per dash; the
existing slowdown and cooldown reset are retained. These combat values are authored
in DefaultCombat. Elemental and Deflect checkpoint tolerance is 52 guide units;
outside-start clicks are ignored and holding onto checkpoint one starts a stroke.
Incomplete releases permit a fresh stroke; elemental retries retain the original
three-second deadline. Deflect remains untimed and protected. Wrong checkpoint
order still fails elemental traces. Verification was not run.
