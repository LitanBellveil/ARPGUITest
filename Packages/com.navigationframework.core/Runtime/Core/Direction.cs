namespace NavigationFramework
{
    /// <summary>
    /// The four cardinal directions focus can move between <see cref="NavigationNode"/> instances.
    /// Submit and Cancel are deliberately not part of this enum — they are distinct actions
    /// exposed directly as <c>NavigationManager.Submit()</c> / <c>NavigationManager.Cancel()</c>,
    /// not directional moves, so a <see cref="NavigationConnection"/> can never target them.
    /// </summary>
    public enum Direction
    {
        /// <summary> Move focus upward. </summary>
        Up,

        /// <summary> Move focus downward. </summary>
        Down,

        /// <summary> Move focus to the left. </summary>
        Left,

        /// <summary> Move focus to the right. </summary>
        Right
    }
}
