# Square Wizard

A true 2D wizard survival prototype in Unity **6000.3.15f1**. Open
`Assets/Scene/Initialize.unity`, press Play, and select **Begin run**.

| Input | Action |
| --- | --- |
| WASD / arrow keys | Move |
| Mouse | Aim |
| Hold left mouse | Arcane Bolt |
| Hold right mouse | Fireball, after unlocking |
| Hold Space | Frost Nova, after unlocking |
| Escape | Pause/resume or return from a settings/controls panel |
| 1 / 2 / 3 | Choose an upgrade |
| Tab / arrows / Enter | Navigate and submit menu choices |

Collect green squares for XP. Level-ups pause combat and offer upgrades, including
Fireball and Frost Nova unlocks. Survive ten minutes, then defeat the Warden to win.
The Warden telegraphs a charge and a radial projectile burst, with faster recovery
below half health. Every retry starts a fresh run.

The arena, player, enemies, spells, and feedback use built-in primitive shapes.
The game uses the new Input System, Cinemachine 3, DOTween, uGUI, and TextMeshPro.
No custom art, Animator content, sound, builds, or publishing are included.

## Editing

- Configuration: `Assets/ScriptableObject` contains editable spells, enemies,
  upgrades, and the difficulty timeline. Runtime progress does not modify these assets.
- Reusable objects and UI: `Assets/Prefab`.
- Code: `Assets/Script/VonderGames/AiTesting`, following Vonder/AWL conventions.
- Input: `Assets/InputSystem/InputActions.inputactions`. Its original GUID is
  preserved; Unity generates the wrapper. `InputManager` owns one shared runtime copy.
- Collision layers: Player (8), Enemy (9), Arena (10). Enemies separate using local
  steering instead of enemy-to-enemy physics collisions.
- Entry scene: `Initialize`. The template `SampleScene` is preserved.

Settings and personal bests are stored in `wizard-survival.json` under Unity's
`Application.persistentDataPath`, with temporary-file writes and backup recovery.
There is no permanent power progression or run resume. When upgrade ranks are
exhausted, the panel offers remaining valid choices and repeatable healing rather
than duplicate or invalid cards.

## Verification

Use Unity's Test Runner for the `Wizard.EditMode.Tests` and
`Wizard.PlayMode.Tests` assemblies. See `docs/wizard-verification.md` for measured
results, the Editor stress scenario, and known limitations.

The older `docs/wizard-survival-design.md` records a different, earlier design;
this implementation follows the manual-casting, ten-minute boss plan approved
in this chat.
