# Project and scene structure

The established source root and namespaces are preserved. Folder moves use
Unity's AssetDatabase to retain GUIDs and serialized references.

```text
Assets/
  Script/Wizard/AiTesting/
    Core/
      InputSystem/             Generated shared input wrapper
    Gameplay/
      Entity/                  Player, enemies, projectiles, pickups, feedback
      Instance/                Run composition and state
      Manager/                 Shared input owner
      Session/                 Run state and gameplay clocks
      Combat/                  Spells, damage, drawing, Deflect, projectile lifecycle
      Spawning/                Enemy registry, scheduling, enemy/XP lifecycle
      Progression/             XP and upgrade coordination
      Pooling/                 Scene-local entity storage and leases
    UI/                        HUD, menu, upgrade presenters and gesture drawing
    Editor/                    Scene authoring and Inspector tools
    TesterTool/                Existing opt-in stress tool
  InputSystem/                 Authored input actions
  Scene/
    Initialize.unity           Enabled entry scene
    SampleScene.unity          Preserved template, excluded from build
  Prefab/
    Entity/
    Effect/
    UI/CanvasScreenSpace.prefab
  ScriptableObject/            Existing authored tuning and primitive assets
  Settings/                    Render pipeline and platform settings
  Tests/                       Existing opt-in EditMode and PlayMode tests
```

Imported Plugins, TextMesh Pro, TutorialInfo, and Resources keep their existing
locations. Runtime namespaces remain `VonderGames.AiTesting`; folder cleanup does
not rename serialized types or introduce new gameplay systems.

```text
Initialize
  Manager
    InputManager
    EventSystem
  GameplayInstance
  CameraRig
    MainCamera
    ArenaCamera
  Level
    Environment
      Floor
      Grid
        GridVertical ...
        GridHorizontal ...
    Entity
      RunObjects
      Player
        Visual
        AimMarker
    Collision
      CameraBounds
      Wall
        BoundaryWall ...
    Effect
  UICanvasGameplayInstance
    CanvasScreenSpace
      SafeArea
        HUD
        ModalContainer
        DrawingOverlay
    CanvasWorldSpace
      DamageTextContainer
```

The player belongs to this level. Enemies, projectiles, and XP are created under
RunObjects at runtime. CombatFeedback uses Level/Effect and the world-space damage
container for bounded cosmetic pools. The UI component remains on CanvasScreenSpace
and retains explicit serialized references. InputManager owns the single input
wrapper and the EventSystem module's runtime actions.

Use **Wizard > Organize project and scene structure** on the saved Initialize scene
to reapply the folder consolidation and hierarchy grouping. The command preserves
world transforms, scene references, and existing asset GUIDs. It does not run
gameplay checks. The elemental setup command also reapplies this organization.
