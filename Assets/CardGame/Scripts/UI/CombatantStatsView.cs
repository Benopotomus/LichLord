using System.Text;
using TMPro;
using UnityEngine;

namespace LichLord.CardGame.UI
{
    /// <summary>
    /// Displays the stats (HP, Block, Energy, Status effects) for one combatant.
    /// Works for both the player and the enemy — call the appropriate Refresh overload.
    /// </summary>
    public class CombatantStatsView : MonoBehaviour
    {
        [SerializeField] public TextMeshProUGUI nameText;
        [SerializeField] public TextMeshProUGUI hpText;
        [SerializeField] public TextMeshProUGUI blockText;
        [SerializeField] public TextMeshProUGUI energyText;   // null for enemy
        [SerializeField] public TextMeshProUGUI statusText;

        private static readonly StringBuilder _sb = new StringBuilder();

        // ── Player refresh ────────────────────────────────────────────────────────

        public void RefreshAsPlayer(PlayerCombatant player)
        {
            if (nameText  != null) nameText.text   = "You";
            if (hpText    != null) hpText.text      = $"HP: {player.CurrentHealth} / {player.MaxHealth}";
            if (blockText != null) blockText.text   = $"Block: {player.Block}";
            if (energyText != null)
                energyText.text = $"Energy: {player.Energy.CurrentEnergy} / {player.Energy.MaxEnergy}";
            if (statusText != null) statusText.text = BuildStatusString(player.StatusEffects);
        }

        // ── Enemy refresh ─────────────────────────────────────────────────────────

        public void RefreshAsEnemy(EnemyCombatant enemy)
        {
            if (nameText  != null) nameText.text   = enemy.Data.enemyName;
            if (hpText    != null) hpText.text      = $"HP: {enemy.CurrentHealth} / {enemy.Data.maxHealth}";
            if (blockText != null) blockText.text   = $"Block: {enemy.Block}";
            if (energyText != null) energyText.text = "";
            if (statusText != null) statusText.text = BuildStatusString(enemy.StatusEffects);
        }

        // ── Helpers ───────────────────────────────────────────────────────────────

        private static string BuildStatusString(StatusEffectState effects)
        {
            _sb.Clear();

            AppendEffect(EStatusEffect.Weak,        "Weak",       effects, _sb);
            AppendEffect(EStatusEffect.Vulnerable,  "Vuln",       effects, _sb);
            AppendEffect(EStatusEffect.Burning,     "Burn",       effects, _sb);
            AppendEffect(EStatusEffect.Poisoned,    "Poison",     effects, _sb);
            AppendEffect(EStatusEffect.Stunned,     "Stun",       effects, _sb);
            AppendEffect(EStatusEffect.Frail,       "Frail",      effects, _sb);
            AppendEffect(EStatusEffect.Strength,    "Str",        effects, _sb);
            AppendEffect(EStatusEffect.Dexterity,   "Dex",        effects, _sb);

            return _sb.Length > 0 ? _sb.ToString() : "";
        }

        private static void AppendEffect(EStatusEffect effect, string label,
                                          StatusEffectState effects, StringBuilder sb)
        {
            int stacks = effects.Get(effect);
            if (stacks <= 0) return;
            if (sb.Length > 0) sb.Append("  ");
            sb.Append(label);
            sb.Append('(');
            sb.Append(stacks);
            sb.Append(')');
        }
    }
}
