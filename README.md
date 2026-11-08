# Sigil Survival

A 2D wizard survival prototype built with Unity **6000.3.15f1**. Open
`Assets/Scene/Initialize.unity`, press Play, and select **Begin run**.

The wand fires automatically. Move through the arena, collect XP, choose upgrades,
and defeat the Warden after five combat minutes.

| Input | Action |
| --- | --- |
| WASD / arrow keys | Move |
| Space | Dash |
| 1 / 2 / 3 or HUD slots | Select Fire / Water / Thunder, then hold and trace the numbered points |
| Right mouse / 4 | Deflect an incoming shot, then trace the diamond |
| Escape | Cancel a trace or pause |

Elemental spells are learned through upgrades. Water followed by Thunder triggers
Shatter Storm. Deflect is available from the start. The game uses primitive art,
the Input System, Cinemachine, DOTween, uGUI, and TextMeshPro.

## Project

- Entry scene: `Assets/Scene/Initialize.unity` (the only enabled build scene).
- Code: `Assets/Script/Wizard/AiTesting`. The established
  `VonderGames.AiTesting` namespaces and asset GUIDs are retained.
- Editable combat, spell, enemy, upgrade, and difficulty data:
  `Assets/ScriptableObject`.
- Runtime ownership: [architecture](docs/runtime-architecture.md).
- Web publishing: [GitHub Pages setup](docs/github-pages-setup.md).

Settings and personal bests are stored in `wizard-survival.json` under Unity's
`Application.persistentDataPath`. Existing saves under the earlier `AI_TESTING`
product directory remain in use when present.

## Verification

Use Unity's Test Runner for the `Wizard.EditMode.Tests` and
`Wizard.PlayMode.Tests` assemblies. See [verification](docs/wizard-verification.md)
for recorded results and known limits. The current survival update has not been
verified through a player build.
