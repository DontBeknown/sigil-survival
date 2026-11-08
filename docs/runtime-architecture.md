# Runtime architecture

GameplayInstance is the scene composition root. Its serialized fields and existing
MonoBehaviour identities remain in place so Initialize and the entity/UI prefabs
keep their bindings. New runtime services and presenters are plain C# objects;
they require no additional Inspector wiring. Existing public entry points forward
to feature owners for Editor tools and callers that already use them.

| Owner | Responsibility |
| --- | --- |
| RunSession | State transitions, elapsed time, kills, run records, state/HUD events, input context |
| GameplayClock | Combat/player clock ratios, drawing/perfect-dodge slowdown, global time restoration |
| ProgressionController | XP, queued choices, fixed upgrade offers, awards |
| CooldownTracker | Spell timers and drawing lockout |
| EnemyRegistry | Live membership, ranged count, nearest-target queries |
| EnemySpawner | Spawn budget, difficulty schedule, spawn positions, shooter cap, boss arrival |
| RangedAttackScheduler | Ordinary ranged enemy firing turns |
| EnemyDeathController | Death effects, kill accounting, XP and bonus rewards, victory |
| ExperienceSpawner | Pickup creation and pool ownership |
| ProjectileSpawner | Player/hostile projectile creation, hostile tracking, pool ownership |
| CombatDamage | Critical hits and fire explosion damage |
| SpellCaster | Automatic wand opportunities, elemental dispatch, combo window |
| FireSpell / WaterSpell / ThunderSpell | Individual ISpell effect implementations |
| DrawingController | Elemental drawing requests, completion, retry/cancel, pause coordination |
| DeflectController | Catch selection, held-shot protection, reflection and release |
| PlayerCombatController | Perfect-dodge reward and cooldown reset coordination |
| ProjectileThreatMonitor | Upcoming projectile danger indication |
| CameraFeedback | Camera framing, coordinate conversion, bounded shake |

PlayerController adapts Unity callbacks and hit delivery to PlayerMotor, PlayerDash,
PlayerHealth, and PlayerPresentation. EnemyAgent owns physics and entity lifecycle;
MeleeEnemyBehaviour, RangedEnemyBehaviour, and BossAttackBehaviour implement the
different decisions through IEnemyBehaviour. Authored ScriptableObject data remains
separate from mutable runtime state.

UICanvasGameplayInstance retains scene references, creates GameplayHud,
RunMenuPanel, UpgradeSelectionPanel, and SafeAreaLayout, routes UI commands, and
owns event subscriptions. The HUD receives session/health events. Reveal, cooldown
fill, and other cosmetic animation ticks remain unscaled. UIGestureDrawing talks
to the drawing owner and session rather than the entire gameplay instance.

## Initialization and lifetime

GameplayInstance initializes the single input owner, then constructs session,
clock, registry, camera feedback, progression, cooldowns, damage, player combat,
projectiles, Deflect, spells, drawing, and spawn services. It then initializes
feedback, player composition, gesture drawing, and the UI, attaches lifecycle
handlers, resets player health, and publishes the title state. Constructor callbacks
are invoked only after composition finishes; construction does not spawn entities.

RunSession controls Player-map activity through InputManager. The UI and system
maps remain available for menus, drawing, and pause. State events update the clock
before the UI observes the transition. All action consumers use the shared wrapper.

Run end closes drawing, removes held-shot protection, returns active projectiles,
clears transient feedback, and stops the firing scheduler. Restart/title cleanup
also returns enemies and pickups. Teardown disconnects handlers, disposes feature
owners/pools, and restores time and Cinemachine impulse settings. Pools and services
belong to this scene; none become persistent singletons.

## Pool behavior

SceneEntityPool<T> handles inactive storage and active membership only. The typed
spawners select configuration and initialize inactive objects before activation.
Normal enemy death, projectile impact/expiry, and pickup collection return objects;
Destroy is reserved for pool overflow and disposal.

| Pool | Retained inactive capacity | Growth |
| --- | --- | --- |
| Enemies | Authored difficulty EnemyCap | Lazy creation; schedule obeys gameplay cap |
| Projectiles | 256 | Lazy creation; active gameplay shots are not dropped |
| XP pickups | 256 | Lazy creation; earned pickups are not dropped |

These capacities limit retained inactive instances, not active instances. There is
no automatic prewarm. A new instance is created only when no inactive one is
available; returns above retained capacity destroy the excess. Existing cosmetic
pools and reusable UI effects remain in place. The infrequent upgrade-selection
echo remains a transient UI object.

Inactive EnemyAgentPool, SpellProjectilePool, and ExperiencePickupPool containers
are created under Level/Entity/RunObjects during composition. Active leases are
parented directly under RunObjects; the scene owns both forms of storage.

Enemy returns remove registry and firing-queue membership before reuse, detach
projectile attacker references, stop physics, reset timers/indicators/tweens, and
restore visuals. Projectile returns clear suspension, reflection, reward flags,
hit history, collision buffers, trails, and indicators. Projectile visual children
and TrailRenderer components are created once per instance. Pickup returns clear
collection/attraction state and reset visual tweens.

EnemyHitHistory includes the enemy lease version so a fireball can hit a new enemy
occupying a reused object without damaging the same incarnation twice. Developer
stress tracking also includes projectile lease versions so an old reference does
not count a different shot as its original lease.

## Verification status

Existing PlayMode fixtures were adapted for the new clock owner, isolated save
injection before composition, and projectile returns. The existing stress tool was
adapted for PlayerHealth and projectile leases. Neither was executed. Tests, builds,
compilation checks, Play Mode, validators, and profiling were not requested or run.
No measured frame-time or allocation improvement is claimed.
