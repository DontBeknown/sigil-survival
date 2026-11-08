using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using VonderGames.AiTesting.Core.InputSystem;

namespace VonderGames.AiTesting.Gameplay.Manager
{
    public sealed class InputManager : MonoBehaviour
    {
        [SerializeField] private InputSystemUIInputModule _uiModule;

        private InputActions _actions;
        public InputActions Actions => _actions;
        public InputAction Dash { get; private set; }
        public InputAction Deflect { get; private set; }
        public InputAction Choice4 { get; private set; }
        private bool _isAwaitingRelease;
        private int _resumeFrame;
        private int _releasedFrame = -1;
        public bool CanCast => !_isAwaitingRelease && !EventSystem.current.IsPointerOverGameObject();
        public bool CanSelectSpell => !_isAwaitingRelease;

        public void Initialize()
        {
            _actions = new InputActions();
            Dash = _actions.asset.FindAction("Player/Dash", true);
            Deflect = _actions.asset.FindAction("Player/Deflect", true);
            Choice4 = _actions.asset.FindAction("UI/Choice4", true);
            // Project-wide actions would otherwise be a second, automatically enabled copy.
            UnityEngine.InputSystem.InputSystem.actions?.Disable();
            _uiModule.actionsAsset = _actions.asset;
            _uiModule.point = InputActionReference.Create(_actions.UI.Point);
            _uiModule.leftClick = InputActionReference.Create(_actions.UI.Click);
            _uiModule.move = InputActionReference.Create(_actions.UI.Navigate);
            _uiModule.submit = InputActionReference.Create(_actions.UI.Submit);
            _uiModule.cancel = InputActionReference.Create(_actions.UI.Cancel);
            _uiModule.scrollWheel = InputActionReference.Create(_actions.UI.ScrollWheel);
            _actions.System.Enable();
            _actions.UI.Enable();
        }

        private void Update()
        {
            if (!_isAwaitingRelease || Time.frameCount <= _resumeFrame) return;
            bool held = _actions.UI.Click.IsPressed() || _actions.UI.Choice1.IsPressed() || _actions.UI.Choice2.IsPressed() ||
                _actions.UI.Choice3.IsPressed() || Choice4.IsPressed() || Dash.IsPressed() || Deflect.IsPressed() || _actions.UI.Submit.IsPressed();
            if (held) { _releasedFrame = -1; return; }
            if (_releasedFrame < 0) { _releasedFrame = Time.frameCount; return; }
            // UI Button clicks fire on release. Keep that entire frame consumed regardless of Update order.
            if (Time.frameCount > _releasedFrame) _isAwaitingRelease = false;
        }

        private void OnDestroy()
        {
            if (_actions == null)
                return;

            _actions.Disable();
            foreach (var reference in new[] { _uiModule.point, _uiModule.leftClick, _uiModule.move, _uiModule.submit, _uiModule.cancel, _uiModule.scrollWheel })
                Destroy(reference);

            _actions.Dispose();
        }

        public void SuppressUntilRelease()
        {
            _isAwaitingRelease = true;
            _resumeFrame = Time.frameCount;
            _releasedFrame = -1;
        }

        public void SetGameplayActive(bool isActive)
        {
            if (isActive)
            {
                _actions.Player.Enable();
                SuppressUntilRelease();
            }
            else
                _actions.Player.Disable();
        }
    }
}
