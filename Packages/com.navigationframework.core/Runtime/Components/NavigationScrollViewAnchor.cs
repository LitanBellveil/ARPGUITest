namespace NavigationFramework
{
    /// <summary>
    /// Placeholder for a dynamic list (e.g. a ScrollView whose items don't exist until Play) so
    /// Generate From Scene / Auto Connect / hand-drawn connections in the Graph Window have
    /// something to wire a page's other buttons to, exactly like any other
    /// <see cref="NavigationSelectable"/> — the list itself doesn't need special-casing at
    /// authoring time. At runtime, <see cref="NavigationDynamicListConnector.AttachDynamicList"/>
    /// transplants this anchor's authored connections onto the real list's first/last node and
    /// disables the anchor, so it is never itself reachable during navigation.
    /// </summary>
    public sealed class NavigationScrollViewAnchor : NavigationSelectable
    {
    }
}
