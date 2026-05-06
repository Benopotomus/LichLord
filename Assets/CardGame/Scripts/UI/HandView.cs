using System.Collections.Generic;
using UnityEngine;

namespace LichLord.CardGame.UI
{
    /// <summary>
    /// Manages the row of <see cref="CardView"/> instances in the player's hand panel.
    /// Destroys and re-creates views whenever the hand changes.
    /// </summary>
    public class HandView : MonoBehaviour
    {
        [SerializeField] public CardView cardPrefab;
        [SerializeField] public Transform cardContainer;

        private readonly List<CardView> _activeViews = new List<CardView>();

        /// <summary>
        /// Rebuilds the hand display.
        /// </summary>
        /// <param name="hand">Current cards in hand.</param>
        /// <param name="currentEnergy">Player's available energy (used to grey out unaffordable cards).</param>
        /// <param name="onPlay">Callback fired with the card when the player clicks it.</param>
        public void RebuildHand(IReadOnlyList<CardData> hand, int currentEnergy,
                                System.Action<CardData> onPlay)
        {
            // Destroy previous views
            foreach (var v in _activeViews)
                if (v != null) Destroy(v.gameObject);
            _activeViews.Clear();

            foreach (var card in hand)
            {
                CardView view = Instantiate(cardPrefab, cardContainer);
                view.Bind(card, currentEnergy >= card.energyCost, onPlay);
                _activeViews.Add(view);
            }
        }

        /// <summary>
        /// Updates the interactable state and colour of all cards without rebuilding.
        /// Useful after energy changes mid-turn.
        /// </summary>
        public void RefreshAffordability(IReadOnlyList<CardData> hand, int currentEnergy,
                                         System.Action<CardData> onPlay)
        {
            // If card count changed, do a full rebuild instead
            if (_activeViews.Count != hand.Count)
            {
                RebuildHand(hand, currentEnergy, onPlay);
                return;
            }

            for (int i = 0; i < _activeViews.Count; i++)
                _activeViews[i].Bind(hand[i], currentEnergy >= hand[i].energyCost, onPlay);
        }
    }
}
