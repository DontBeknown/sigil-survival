using Unity.Cinemachine;
using UnityEngine;

namespace VonderGames.AiTesting.Gameplay.Entity
{
    [ExecuteAlways]
    public sealed class ArenaCameraBounds : CinemachineExtension
    {
        [SerializeField] private Rect _arena = new Rect(-30, -20, 60, 40);
        public Rect Arena => _arena;
        [SerializeField, Min(0)] private float _padding = 0.05f;

        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase camera, CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage != CinemachineCore.Stage.Finalize || !state.Lens.Orthographic)
                return;

            Quaternion orientation = state.GetFinalOrientation();
            Vector3 right = orientation * Vector3.right;
            Vector3 up = orientation * Vector3.up;
            float aspect = Mathf.Max(0.01f, state.Lens.Aspect);
            float widthFactor = Mathf.Abs(right.x) * aspect + Mathf.Abs(up.x);
            float heightFactor = Mathf.Abs(right.y) * aspect + Mathf.Abs(up.y);
            float availableWidth = _arena.width * 0.5f - _padding;
            float availableHeight = _arena.height * 0.5f - _padding;
            state.Lens.OrthographicSize = Mathf.Min(state.Lens.OrthographicSize,
                availableWidth / Mathf.Max(widthFactor, 0.01f), availableHeight / Mathf.Max(heightFactor, 0.01f));
            float halfWidth = state.Lens.OrthographicSize * widthFactor + _padding;
            float halfHeight = state.Lens.OrthographicSize * heightFactor + _padding;
            Vector3 position = state.GetFinalPosition();
            Vector3 bounded = position;
            bounded.x = Mathf.Clamp(position.x, _arena.xMin + halfWidth, _arena.xMax - halfWidth);
            bounded.y = Mathf.Clamp(position.y, _arena.yMin + halfHeight, _arena.yMax - halfHeight);
            state.PositionCorrection += bounded - position;
        }
    }
}
