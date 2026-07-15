using UnityEngine;

namespace NavigationFramework
{
    /// <summary>
    /// Owns a <see cref="NavigationManager"/> and the graph-lifecycle boilerplate every sample
    /// would otherwise duplicate (<see cref="NavigationManager.SetGraph"/> plus
    /// <see cref="NavigationManager.SelectDefault"/> or <see cref="NavigationManager.SwitchToPage"/>
    /// on start), then forwards one <see cref="INavigationInputSource"/>'s events into
    /// Move/Submit/Cancel. Swapping input backends (keyboard, gamepad, touch) means swapping which
    /// component is assigned to <see cref="inputSource"/> — this class and
    /// <see cref="NavigationManager"/> never change.
    /// </summary>
    public class NavigationInputRouter : MonoBehaviour
    {
        [SerializeField] private NavigationGraph graph;
        [SerializeField] private string initialPageId;
        [SerializeField, Tooltip("Must implement INavigationInputSource.")] private MonoBehaviour inputSource;

        private INavigationInputSource source;

        /// <summary> The manager driven by this router. Exposed so UI code can subscribe to <c>NodeChanged</c>. </summary>
        public NavigationManager Manager { get; } = new NavigationManager();

        /// <summary> Auto-fills <see cref="inputSource"/> from the first sibling component implementing the interface. </summary>
        protected virtual void Reset()
        {
            foreach (MonoBehaviour candidate in GetComponents<MonoBehaviour>())
            {
                if (candidate is INavigationInputSource)
                {
                    inputSource = candidate;
                    break;
                }
            }
        }

        protected virtual void OnEnable()
        {
            source = inputSource as INavigationInputSource;

            if (source == null)
            {
                Debug.LogError($"[NavigationFramework] '{nameof(inputSource)}' on '{name}' is not assigned or does not implement {nameof(INavigationInputSource)}.", this);
                return;
            }

            source.DirectionPressed += Manager.Move;
            source.SubmitPressed += Manager.Submit;
            source.CancelPressed += Manager.Cancel;

            Manager.SetGraph(graph);

            if (!string.IsNullOrEmpty(initialPageId))
            {
                Manager.SwitchToPage(initialPageId);
            }
            else
            {
                Manager.SelectDefault();
            }
        }

        protected virtual void OnDisable()
        {
            if (source == null)
            {
                return;
            }

            source.DirectionPressed -= Manager.Move;
            source.SubmitPressed -= Manager.Submit;
            source.CancelPressed -= Manager.Cancel;
            source = null;
        }
    }
}
