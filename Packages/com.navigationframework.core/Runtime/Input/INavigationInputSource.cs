using System;

namespace NavigationFramework
{
    /// <summary>
    /// Contract for anything that can drive a <see cref="NavigationInputRouter"/> — keyboard,
    /// gamepad, touch swipe, an AI agent, replay tooling. Keeping this interface (and
    /// <see cref="NavigationInputRouter"/>) free of any reference to Unity's Input System or a
    /// project's own PlayerControls asset is what lets the Runtime assembly stay input-agnostic;
    /// concrete sources live outside Runtime (see the package's Samples~) since that is the one
    /// place that legitimately needs to know about a specific input backend.
    /// </summary>
    public interface INavigationInputSource
    {
        /// <summary> Raised when this source wants focus to move in the given direction. </summary>
        event Action<Direction> DirectionPressed;

        /// <summary> Raised when this source wants the currently focused node's Submit invoked. </summary>
        event Action SubmitPressed;

        /// <summary> Raised when this source wants the currently focused node's Cancel invoked. </summary>
        event Action CancelPressed;
    }
}
