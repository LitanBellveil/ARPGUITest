using System;
using NavigationFramework;
using UnityEngine;
using UnityEngine.InputSystem;

namespace NavigationFramework.Samples
{
    /// <summary>
    /// Reference <see cref="INavigationInputSource"/> implementation: polls arrow keys for
    /// direction, Enter for Submit, Escape for Cancel via the new Input System's
    /// <see cref="Keyboard.current"/>. Assign this (or a gamepad/touch equivalent built the same
    /// way) to a <see cref="NavigationInputRouter"/>'s "Input Source" field. Shared across every
    /// sample in this package instead of each one duplicating <c>NavigationTestDriver</c>'s
    /// keyboard-polling logic.
    /// </summary>
    public class KeyboardInputSource : MonoBehaviour, INavigationInputSource
    {
        public event Action<Direction> DirectionPressed;
        public event Action SubmitPressed;
        public event Action CancelPressed;

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null)
            {
                return;
            }

            if (keyboard.upArrowKey.wasPressedThisFrame) DirectionPressed?.Invoke(Direction.Up);
            if (keyboard.downArrowKey.wasPressedThisFrame) DirectionPressed?.Invoke(Direction.Down);
            if (keyboard.leftArrowKey.wasPressedThisFrame) DirectionPressed?.Invoke(Direction.Left);
            if (keyboard.rightArrowKey.wasPressedThisFrame) DirectionPressed?.Invoke(Direction.Right);

            if (keyboard.enterKey.wasPressedThisFrame || keyboard.numpadEnterKey.wasPressedThisFrame)
            {
                SubmitPressed?.Invoke();
            }

            if (keyboard.escapeKey.wasPressedThisFrame)
            {
                CancelPressed?.Invoke();
            }
        }
    }
}
