using UnityEngine;

namespace VonderGames.AiTesting.Core
{
    public static class ProjectileThreat
    {
        // Sweep the projectile relative to a translating axis-aligned player hitbox.
        public static bool Sweep(Vector2 start, Vector2 step, Bounds bounds, float radius, out float fraction)
        {
            Vector2 minimum = (Vector2)bounds.min - Vector2.one * radius;
            Vector2 maximum = (Vector2)bounds.max + Vector2.one * radius;
            float enter = 0;
            float exit = 1;
            for (int axis = 0; axis < 2; axis++)
            {
                if (Mathf.Abs(step[axis]) < 0.000001f)
                {
                    if (start[axis] < minimum[axis] || start[axis] > maximum[axis]) { fraction = 0; return false; }
                    continue;
                }
                float first = (minimum[axis] - start[axis]) / step[axis];
                float last = (maximum[axis] - start[axis]) / step[axis];
                enter = Mathf.Max(enter, Mathf.Min(first, last));
                exit = Mathf.Min(exit, Mathf.Max(first, last));
                if (enter > exit) { fraction = 0; return false; }
            }
            fraction = enter;
            return true;
        }
    }
}
