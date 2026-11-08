using UnityEngine;

namespace VonderGames.AiTesting.Core
{
    public sealed class GestureTrace
    {
        public Vector2[] Points { get; private set; }
        public int Reached { get; private set; }
        public bool HasStarted { get; private set; }
        public bool HasFailed { get; private set; }
        public bool IsComplete => Reached == Points.Length;

        private Vector2 _previous;
        private float _radius;

        public void Reset(SpellKind kind, float radius = 52)
        {
            Points = GetPoints(kind);
            Reached = 0;
            HasStarted = false;
            HasFailed = false;
            _radius = radius;
        }

        public static Vector2[] GetPoints(SpellKind kind)
        {
            switch (kind)
            {
                case SpellKind.Fire:
                    return new[] { new Vector2(-145, -95), new Vector2(0, 110), new Vector2(145, -95) };
                case SpellKind.Water:
                    return new[] { new Vector2(-145, 95), new Vector2(-75, -100), new Vector2(75, -100), new Vector2(145, 95) };
                case SpellKind.Deflect:
                    return new[] { new Vector2(0, 120), new Vector2(125, 0), new Vector2(0, -120), new Vector2(-125, 0), new Vector2(0, 120) };
                default:
                    return new[] { new Vector2(125, 120), new Vector2(-75, 30), new Vector2(75, -30), new Vector2(-125, -120) };
            }
        }

        public bool Begin(Vector2 point)
        {
            if (Vector2.Distance(point, Points[0]) > _radius)
                return false;

            HasStarted = true;
            Reached = 1;
            _previous = point;
            return true;
        }

        public void Move(Vector2 point)
        {
            if (!HasStarted || HasFailed || IsComplete)
                return;

            Vector2 segment = point - _previous;
            float length = segment.sqrMagnitude;
            float previousFraction = 0;
            for (int i = Reached; i < Points.Length; i++)
            {
                float fraction = length > 0 ? Mathf.Clamp01(Vector2.Dot(Points[i] - _previous, segment) / length) : 0;
                // A closed loop's final checkpoint overlaps its start. Leaving the start is not an out-of-order return.
                if (i == Points.Length - 1 && Points[i] == Points[0] && fraction <= 0.001f && Reached < i)
                    continue;
                if (Vector2.Distance(_previous + segment * fraction, Points[i]) > _radius)
                    continue;

                if (i != Reached || fraction < previousFraction)
                {
                    HasFailed = true;
                    break;
                }
                Reached++;
                previousFraction = fraction;
            }
            _previous = point;
        }
    }
}
