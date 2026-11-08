using System;
using UnityEngine;
using VonderGames.AiTesting.Core;

namespace VonderGames.AiTesting.Gameplay.Combat
{
    public sealed class CooldownTracker
    {
        private readonly SpellDefinition[] _spells;
        private readonly CombatDefinition _combat;
        private readonly RunProgress _progress;
        private readonly float[] _remaining = new float[5];
        public float DrawingLockout { get; private set; }

        public CooldownTracker(SpellDefinition[] spells, CombatDefinition combat, RunProgress progress)
        {
            _spells = spells;
            _combat = combat;
            _progress = progress;
        }
        public float GetCooldown(SpellKind kind) => _remaining[(int)kind];
        public float GetCooldownDuration(SpellKind kind) => (kind == SpellKind.Deflect ? _combat.DeflectCooldown : _spells[(int)kind].Cooldown) * _progress.CooldownMultiplier;
        public void Start(SpellKind kind) => _remaining[(int)kind] = GetCooldownDuration(kind);
        public void StartBoltOpportunity() => _remaining[0] = _spells[0].Cooldown;
        public void LockDrawing() => DrawingLockout = 1;
        public void Advance(float delta)
        {
            DrawingLockout = Mathf.Max(0, DrawingLockout - delta);
            for (int i = 0; i < _remaining.Length; i++)
                _remaining[i] = Mathf.Max(0, _remaining[i] - delta);
        }
        public void Refresh()
        {
            DrawingLockout = 0;
            for (int i = 1; i < _remaining.Length; i++) _remaining[i] = 0;
        }
        public void Reset()
        {
            DrawingLockout = 0;
            Array.Clear(_remaining, 0, _remaining.Length);
        }

    }
}
