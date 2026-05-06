using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace LichLord.CardGame.UI
{
    /// <summary>
    /// Entry-point MonoBehaviour for the CombatScene.
    /// On Start() it builds a starter deck, picks a random enemy, and kicks off combat.
    /// All serialized UI references are wired up by <c>CombatSceneBuilder</c> in the Editor.
    /// </summary>
    public class CombatBootstrapper : MonoBehaviour
    {
        // ── UI References (set by CombatSceneBuilder) ─────────────────────────────

        [Header("Combatant Views")]
        [SerializeField] public CombatantStatsView playerStatsView;
        [SerializeField] public CombatantStatsView enemyStatsView;

        [Header("Dice Views")]
        [SerializeField] public DiceRowView playerDiceView;
        [SerializeField] public DiceRowView enemyDiceView;

        [Header("Hand")]
        [SerializeField] public HandView handView;

        [Header("Log")]
        [SerializeField] public CombatLogView combatLog;

        [Header("Buttons")]
        [SerializeField] public Button endTurnButton;

        // ── Runtime state ─────────────────────────────────────────────────────────

        private CombatManager _combat;

        // ── Unity lifecycle ───────────────────────────────────────────────────────

        private void Start()
        {
            List<CardData> deck  = BuildStarterDeck();
            EnemyData      enemy = PickRandomEnemy();

            _combat = new CombatManager();
            _combat.OnStateChanged += OnStateChanged;
            _combat.OnRoundStarted += OnRoundStarted;
            _combat.OnCombatEnded  += OnCombatEnded;
            _combat.OnCombatLog    += msg => combatLog.AddLine(msg);

            endTurnButton.onClick.AddListener(OnEndTurnClicked);

            combatLog.AddLine($"⚔  Combat begins against {enemy.enemyName}!");
            _combat.StartCombat(enemy, deck);
        }

        // ── Event handlers ────────────────────────────────────────────────────────

        private void OnStateChanged(ECombatState state)
        {
            RefreshAllViews();

            bool isPlayerTurn = state == ECombatState.PlayerTurn;
            endTurnButton.interactable = isPlayerTurn;

            if (state == ECombatState.EnemyTurn)
                combatLog.AddLine("— Enemy turn —");
        }

        private void OnRoundStarted()
        {
            combatLog.AddLine($"\n═══ Round start ═══");
            combatLog.AddLine($"Your dice: {DiceString(_combat.Player.Dice.CurrentRoll)}");
            combatLog.AddLine($"Enemy dice: {DiceString(_combat.Enemy.Dice.CurrentRoll)}");
            RefreshAllViews();
        }

        private void OnCombatEnded(bool playerWon)
        {
            endTurnButton.interactable = false;
            handView.RebuildHand(new List<CardData>(), 0, null);

            if (playerWon)
            {
                combatLog.AddLine("\n🏆 VICTORY! You defeated the enemy.");
                enemyStatsView.RefreshAsEnemy(_combat.Enemy);
            }
            else
            {
                combatLog.AddLine("\n💀 DEFEAT. You were slain.");
                playerStatsView.RefreshAsPlayer(_combat.Player);
            }
        }

        // ── UI actions ────────────────────────────────────────────────────────────

        private void OnEndTurnClicked()
        {
            combatLog.AddLine("You end your turn.");
            _combat.EndPlayerTurn();
        }

        private void OnCardPlayed(CardData card)
        {
            if (_combat.State != ECombatState.PlayerTurn) return;

            bool played = _combat.TryPlayCard(card);
            if (!played) return;

            combatLog.AddLine($"▶ Played: {card.cardName}");
            RefreshAllViews();
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private void RefreshAllViews()
        {
            if (_combat?.Player == null || _combat?.Enemy == null) return;

            playerStatsView.RefreshAsPlayer(_combat.Player);
            enemyStatsView.RefreshAsEnemy(_combat.Enemy);
            playerDiceView.Refresh(_combat.Player.Dice.CurrentRoll);
            enemyDiceView.Refresh(_combat.Enemy.Dice.CurrentRoll);

            if (_combat.State == ECombatState.PlayerTurn)
            {
                handView.RefreshAffordability(
                    _combat.Player.Deck.Hand,
                    _combat.Player.Energy.CurrentEnergy,
                    OnCardPlayed);
            }
        }

        private static string DiceString(int[] roll)
        {
            if (roll == null || roll.Length == 0) return "—";
            return string.Join("  ", System.Array.ConvertAll(roll, v => $"[{v}]"));
        }

        // ── Deck / Enemy setup ────────────────────────────────────────────────────

        /// <summary>
        /// Returns a balanced 12-card starter deck drawn from the full catalog.
        /// </summary>
        private static List<CardData> BuildStarterDeck()
        {
            var all = CardCatalog.CreateAllCards();

            // Indices match the catalog order (0-based):
            //  0 Iron Shield, 1 Swift Slash, 7 Twin Fangs, 10 Ace High,
            //  18 High Guard, 15 Bone Ward, 2 Focused Strike,
            //  21 Lucky Reroll, 29 Expose, 22 Full Send, 28 Crippling Blow, 16 Tower Shield
            int[] starterIndices = { 0, 1, 7, 10, 18, 15, 2, 21, 29, 22, 28, 16 };

            var deck = new List<CardData>();
            foreach (int idx in starterIndices)
            {
                if (idx < all.Count)
                    deck.Add(all[idx]);
            }
            return deck;
        }

        /// <summary>
        /// Picks a random enemy from the catalog.
        /// </summary>
        private static EnemyData PickRandomEnemy()
        {
            var enemies = EnemyCatalog.CreateAllEnemies();
            return enemies[Random.Range(0, enemies.Count)];
        }
    }
}
