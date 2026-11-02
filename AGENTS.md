# AI Testing — Wizard Survival

Unity 6000.3.15f1, URP, Input System 1.19, Cinemachine 3.1.7, DOTween, and uGUI.
Preserve installed package versions, imported plugins, and unrelated user changes.

Use Vonder/AWL conventions: `Assets/Script/VonderGames/AiTesting`, singular folders,
`VonderGames.AiTesting` namespaces, private `_camelCase` fields, serialized private
fields with read-only properties, explicit access modifiers, Allman braces, and
`On...` events with `...Handler` callbacks. One MonoBehaviour per file.

`InputManager` exclusively owns the generated input wrapper. Consumers share it;
UI uses its runtime asset. `GameplayInstance` owns run state and time scale.
The entry scene is `Assets/Scene/Initialize.unity`; Systems, Level, Cameras, and UI
group scene responsibilities. Use explicit serialized dependencies, not searches
or automatic global singleton creation. Configuration assets remain immutable in play.

The user explicitly authorized compilation, focused Edit/Play Mode tests, UI
inspection, asset validation, and Editor profiling for this implementation.
No player builds, artwork, Animator content, audio, or publishing.
