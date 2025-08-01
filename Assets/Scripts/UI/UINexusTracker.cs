using LichLord.Props;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.UI
{
    public class UINexusTracker : UIWidget
    {
        Dictionary<Nexus, UIFloatingNexusStatus> _nexusWidgets = new Dictionary<Nexus, UIFloatingNexusStatus>();
        public List<UIFloatingNexusStatus> _freeWidgets = new List<UIFloatingNexusStatus>();

        [SerializeField] private UIFloatingNexusStatus _floatingNexusWidgetPrefab; // Prefab for the UI widget
        [SerializeField] private Transform _widgetParent; // Parent transform for spawned widgets

        public void OnNexusSpawned(Nexus nexus)
        {
            // Check if widget already exists (defensive check)
            if (!_nexusWidgets.ContainsKey(nexus))
            {
                UIFloatingNexusStatus widget;
                // Get widget from pool if available, otherwise instantiate new
                if (_freeWidgets.Count > 0)
                {
                    widget = _freeWidgets[0];
                    _freeWidgets.RemoveAt(0);
                }
                else
                {
                    // Instantiate new widget from prefab
                    widget = Instantiate(_floatingNexusWidgetPrefab, _widgetParent);
                    AddChild(widget);
                }

                // Setup widget with nexus data
                widget.SetTarget(nexus.transform);
                widget.SetNexus(nexus);
                _nexusWidgets[nexus] = widget;
            }
        }

        public void OnNexusDespawned(Nexus nexus)
        {
            // Remove widget if it exists
            if (_nexusWidgets.TryGetValue(nexus, out var widget))
            {
                // Clean up widget
                widget.SetTarget(null);
                widget.SetNexus(null);

                // Return to pool for reuse
                _freeWidgets.Add(widget);
                // Remove from active tracking
                _nexusWidgets.Remove(nexus);
            }
        }
    }
}