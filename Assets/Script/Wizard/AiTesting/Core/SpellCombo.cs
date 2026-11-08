using UnityEngine;

namespace VonderGames.AiTesting.Core
{
    public sealed class SpellCombo
    {
        public float Remaining { get; private set; }

        public void Reset() => Remaining = 0;
        public void Advance(float delta) => Remaining = Mathf.Max(0, Remaining - delta);

        public bool Cast(SpellKind kind)
        {
            if (kind == SpellKind.Bolt)
                return false;

            bool isCombo = kind == SpellKind.Thunder && Remaining > 0;
            Remaining = kind == SpellKind.Water ? 3 : 0;
            return isCombo;
        }
    }
}
