using LichLord.Props;
using System.Collections.Generic;
using UnityEngine;

namespace LichLord.UI
{
    public class UIStrongholdTracker : UIWidget
    {
        Dictionary<Stronghold, UIFloatingStrongholdStatus> _strongholdWidgets = new Dictionary<Stronghold, UIFloatingStrongholdStatus>();
        public List<UIFloatingStrongholdStatus> _freeWidgets = new List<UIFloatingStrongholdStatus>();

        [SerializeField] private UIFloatingStrongholdStatus _floatingStrongholdWidgetPrefab; // Prefab for the UI widget
        [SerializeField] private Transform _widgetParent; // Parent transform for spawned widgets

        public void OnStrongholdSpawned(Stronghold stronghold)
        {
            // Check if widget already exists (defensive check)
            if (!_strongholdWidgets.ContainsKey(stronghold))
            {
                UIFloatingStrongholdStatus widget;
                // Get widget from pool if available, otherwise instantiate new
                if (_freeWidgets.Count > 0)
                {
                    widget = _freeWidgets[0];
                    _freeWidgets.RemoveAt(0);
                }
                else
                {
                    // Instantiate new widget from prefab
                    widget = Instantiate(_floatingStrongholdWidgetPrefab, _widgetParent);
                    AddChild(widget);
                }

                // Setup widget with nexus data
                widget.SetTarget(stronghold.transform);
                widget.SetStronghold(stronghold);
                _strongholdWidgets[stronghold] = widget;
            }
        }

        public void OnStrongholdDespawned(Stronghold stronghold)
        {
            // Remove widget if it exists
            if (_strongholdWidgets.TryGetValue(stronghold, out var widget))
            {
                // Clean up widget
                widget.SetTarget(null);
                widget.SetStronghold(null);

                // Return to pool for reuse
                _freeWidgets.Add(widget);
                // Remove from active tracking
                _strongholdWidgets.Remove(stronghold);
            }
        }
    }
}