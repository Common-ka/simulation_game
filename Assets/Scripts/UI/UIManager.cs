using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace IdleEconomy.UI
{
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [SerializeField] private UIDocument document;
        
        private readonly Dictionary<string, VisualElement> _cachedPanels = new Dictionary<string, VisualElement>();
        private const string HiddenClass = "hidden";
        private const string PanelClass = "panel";

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            if (document == null) document = GetComponent<UIDocument>();

            if (document != null)
            {
                var panels = document.rootVisualElement.Query<VisualElement>(className: PanelClass).ToList();
                foreach (var p in panels)
                {
                    if (!string.IsNullOrEmpty(p.name)) _cachedPanels[p.name] = p;
                }
            }
        }

        public void Show(string panelName)
        {
            if (_cachedPanels.TryGetValue(panelName, out var cached))
            {
                cached.RemoveFromClassList(HiddenClass);
                return;
            }

            if (document == null) return;
            var found = document.rootVisualElement.Q<VisualElement>(panelName);
            if (found != null)
            {
                _cachedPanels[panelName] = found;
                found.RemoveFromClassList(HiddenClass);
            }
        }

        public void Hide(string panelName)
        {
            if (_cachedPanels.TryGetValue(panelName, out var cached))
            {
                cached.AddToClassList(HiddenClass);
                return;
            }

            if (document == null) return;
            var found = document.rootVisualElement.Q<VisualElement>(panelName);
            if (found != null)
            {
                _cachedPanels[panelName] = found;
                found.AddToClassList(HiddenClass);
            }
        }

        public void ShowOnly(string panelName)
        {
            foreach (var p in _cachedPanels.Values)
            {
                p.AddToClassList(HiddenClass);
            }
            Show(panelName);
        }
    }
}
