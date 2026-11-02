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
        private bool _isAwaitingRelease;
        private int _resumeFrame;
        public bool CanCast => !_isAwaitingRelease && !EventSystem.current.IsPointerOverGameObject();

        public void Initialize()
        {
            _actions = new InputActions();
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
            if (_isAwaitingRelease && Time.frameCount > _resumeFrame && !_actions.UI.Click.IsPressed() &&
                !_actions.Player.Bolt.IsPressed() && !_actions.Player.Fireball.IsPressed() && !_actions.Player.Nova.IsPressed())
                _isAwaitingRelease = false;
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

        public void SetGameplayActive(bool isActive)
        {
            if (isActive)
            {
                _actions.Player.Enable();
                _isAwaitingRelease = true;
                _resumeFrame = Time.frameCount;
            }
            else
                _actions.Player.Disable();
        }
    }
}
