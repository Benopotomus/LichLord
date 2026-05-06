using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.CardGame.UI
{
    /// <summary>
    /// Prefab script for a single card in the player's hand.
    /// Attach to the root of the CardPrefab.
    /// The CombatSceneBuilder wires up the text and button references via the Inspector.
    /// </summary>
    public class CardView : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI costText;
        [SerializeField] public TextMeshProUGUI nameText;
        [SerializeField] public TextMeshProUGUI descriptionText;
        [SerializeField] public Button playButton;
        [SerializeField] public Image  background;

        private System.Action<CardData> _onPlay;

        private void Awake()
        {
            playButton.onClick.AddListener(OnClicked);
        }

        /// <summary>
        /// Binds this view to a card and a play callback.
        /// Call this every time the hand is rebuilt.
        /// </summary>
        public void Bind(CardData card, bool canAfford, System.Action<CardData> onPlay)
        {
            _onPlay = () => onPlay(card);

            costText.text        = card.energyCost.ToString();
            nameText.text        = card.cardName;
            descriptionText.text = card.description;

            playButton.interactable = canAfford;
            if (background != null)
                background.color = canAfford
                    ? new Color(0.18f, 0.22f, 0.32f)
                    : new Color(0.15f, 0.15f, 0.18f);
        }

        private void OnClicked()
        {
            _onPlay?.Invoke();
        }
    }
}
