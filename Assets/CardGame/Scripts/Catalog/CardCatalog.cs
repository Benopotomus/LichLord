using System.Collections.Generic;
using UnityEngine;

namespace LichLord.CardGame
{
    /// <summary>
    /// Programmatic catalog of all 25 player cards.
    /// Call <see cref="CreateAllCards"/> to obtain a list of runtime <see cref="CardData"/> instances.
    /// In the Unity Editor these instances can be saved as .asset files via the CardGame menu.
    /// </summary>
    public static class CardCatalog
    {
        public static List<CardData> CreateAllCards()
        {
            return new List<CardData>
            {
                // ── Damage ────────────────────────────────────────────────────────
                CreateIronShield(),
                CreateSwiftSlash(),
                CreateFocusedStrike(),
                CreateGamblersBlade(),
                CreateDragonsRoar(),
                CreateScatterShot(),
                CreateSpiritStrike(),
                CreateTwinFangs(),
                CreateCrushingBlow(),
                CreatePrecisionStrike(),
                CreateAceHigh(),
                CreateLowSweep(),
                CreateBerserkersRage(),
                CreateChainLightning(),
                CreateDeathsGamble(),

                // ── Defense ───────────────────────────────────────────────────────
                CreateBoneWard(),
                CreateTowerShield(),
                CreateRuneShield(),
                CreateHighGuard(),
                CreateScatterDefense(),
                CreateBrittleWard(),

                // ── Dice Manipulation ─────────────────────────────────────────────
                CreateLuckyReroll(),
                CreateFullSend(),
                CreateHexCurse(),
                CreateCursedDice(),
                CreateWarDrums(),
                CreateCursedAura(),
                CreateLuckyCharm(),

                // ── Status Effects ────────────────────────────────────────────────
                CreateCripplingBlow(),
                CreateExpose(),
                CreateBurningHands(),
                CreateVenomStrike(),
                CreateStagger(),
                CreateStoneSkin(),
                CreateBattleStance(),
            };
        }

        // ── Card 01 ───────────────────────────────────────────────────────────────

        private static CardData CreateIronShield()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Iron Shield";
            card.description = "Block 1 damage for each [4] rolled.";
            card.energyCost  = 1;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType     = EEffectType.GainBlock,
                    diceTarget     = ECardTarget.PlayerDice,
                    triggerOn      = EPokerHandType.PerDieValue,
                    dieValue       = 4,
                    magnitude      = 1,
                },
            };
            return card;
        }

        // ── Card 02 ───────────────────────────────────────────────────────────────

        private static CardData CreateSwiftSlash()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Swift Slash";
            card.description = "Deal 1 damage for each [1] rolled.";
            card.energyCost  = 1;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerDieValue,
                    dieValue   = 1,
                    magnitude  = 1,
                },
            };
            return card;
        }

        // ── Card 03 ───────────────────────────────────────────────────────────────

        private static CardData CreateFocusedStrike()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Focused Strike";
            card.description = "Deal 3 damage for each triple rolled.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerTriple,
                    magnitude  = 3,
                },
            };
            return card;
        }

        // ── Card 04 ───────────────────────────────────────────────────────────────

        private static CardData CreateGamblersBlade()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Gambler's Blade";
            card.description = "If you have a straight, deal 5 damage. Otherwise deal 1 damage.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType   = EEffectType.ConditionalDamage,
                    diceTarget   = ECardTarget.PlayerDice,
                    triggerOn    = EPokerHandType.IfStraight,
                    magnitude    = 5,
                    altMagnitude = 1,
                },
            };
            return card;
        }

        // ── Card 05 ───────────────────────────────────────────────────────────────

        private static CardData CreateDragonsRoar()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Dragon's Roar";
            card.description = "Deal 6 damage for each five-of-a-kind rolled.";
            card.energyCost  = 3;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerFiveOfAKind,
                    magnitude  = 6,
                },
            };
            return card;
        }

        // ── Card 06 ───────────────────────────────────────────────────────────────

        private static CardData CreateScatterShot()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Scatter Shot";
            card.description = "Deal 1 damage for each unique die value showing.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerUniqueDieValue,
                    magnitude  = 1,
                },
            };
            return card;
        }

        // ── Card 07 ───────────────────────────────────────────────────────────────

        private static CardData CreateSpiritStrike()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Spirit Strike";
            card.description = "Deal 2 damage for each pair rolled.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerPair,
                    magnitude  = 2,
                },
            };
            return card;
        }

        // ── Card 08 ───────────────────────────────────────────────────────────────

        private static CardData CreateTwinFangs()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Twin Fangs";
            card.description = "Deal 1 damage for each pair rolled.";
            card.energyCost  = 1;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerPair,
                    magnitude  = 1,
                },
            };
            return card;
        }

        // ── Card 09 ───────────────────────────────────────────────────────────────

        private static CardData CreateCrushingBlow()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Crushing Blow";
            card.description = "Deal 4 damage per full house (triple + pair) rolled.";
            card.energyCost  = 3;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerFullHouse,
                    magnitude  = 4,
                },
            };
            return card;
        }

        // ── Card 10 ───────────────────────────────────────────────────────────────

        private static CardData CreatePrecisionStrike()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Precision Strike";
            card.description = "Deal 5 damage for each four-of-a-kind rolled.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerFourOfAKind,
                    magnitude  = 5,
                },
            };
            return card;
        }

        // ── Card 11 ───────────────────────────────────────────────────────────────

        private static CardData CreateAceHigh()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Ace High";
            card.description = "Deal 3 damage for each [6] rolled.";
            card.energyCost  = 1;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerDieValue,
                    dieValue   = 6,
                    magnitude  = 3,
                },
            };
            return card;
        }

        // ── Card 12 ───────────────────────────────────────────────────────────────

        private static CardData CreateLowSweep()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Low Sweep";
            card.description = "Deal 1 damage for each die showing [1] or [2].";
            card.energyCost  = 1;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType     = EEffectType.DealDamage,
                    diceTarget     = ECardTarget.PlayerDice,
                    triggerOn      = EPokerHandType.PerLowDie,
                    valueThreshold = 2,
                    magnitude      = 1,
                },
            };
            return card;
        }

        // ── Card 13 ───────────────────────────────────────────────────────────────

        private static CardData CreateBerserkersRage()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Berserker's Rage";
            card.description = "Deal 1 damage for each odd die rolled ([1], [3], [5]).";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerOddDie,
                    magnitude  = 1,
                },
            };
            return card;
        }

        // ── Card 14 ───────────────────────────────────────────────────────────────

        private static CardData CreateChainLightning()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Chain Lightning";
            card.description = "Deal 2 damage for each pair found in the enemy's dice.";
            card.energyCost  = 3;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.EnemyDice,
                    triggerOn  = EPokerHandType.PerPair,
                    magnitude  = 2,
                },
            };
            return card;
        }

        // ── Card 15 ───────────────────────────────────────────────────────────────

        private static CardData CreateDeathsGamble()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Death's Gamble";
            card.description = "If all 5 dice match, deal 8 damage. Otherwise, take 2 damage.";
            card.energyCost  = 1;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType   = EEffectType.ConditionalDamage,
                    diceTarget   = ECardTarget.PlayerDice,
                    triggerOn    = EPokerHandType.PerFiveOfAKind,
                    magnitude    = 8,
                    altMagnitude = 2,
                },
            };
            return card;
        }

        // ── Card 16 ───────────────────────────────────────────────────────────────

        private static CardData CreateBoneWard()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Bone Ward";
            card.description = "Block damage equal to the value of your highest die.";
            card.energyCost  = 1;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.GainBlock,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.HighestDieValue,
                    magnitude  = 1,
                },
            };
            return card;
        }

        // ── Card 17 ───────────────────────────────────────────────────────────────

        private static CardData CreateTowerShield()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Tower Shield";
            card.description = "Each round, block 1 damage for each die showing 5 or 6.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Persistent;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType     = EEffectType.GainBlock,
                    diceTarget     = ECardTarget.PlayerDice,
                    triggerOn      = EPokerHandType.PerHighDie,
                    valueThreshold = 5,
                    magnitude      = 1,
                },
            };
            return card;
        }

        // ── Card 18 ───────────────────────────────────────────────────────────────

        private static CardData CreateRuneShield()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Rune Shield";
            card.description = "Each round, block 2 damage for each [6] rolled.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Persistent;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.GainBlock,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerDieValue,
                    dieValue   = 6,
                    magnitude  = 2,
                },
            };
            return card;
        }

        // ── Card 19 ───────────────────────────────────────────────────────────────

        private static CardData CreateLuckyReroll()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Lucky Reroll";
            card.description = "Reroll up to 3 of your dice.";
            card.energyCost  = 1;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.RerollDice,
                    diceTarget = ECardTarget.PlayerDice,
                    count      = 3,
                },
            };
            return card;
        }

        // ── Card 20 ───────────────────────────────────────────────────────────────

        private static CardData CreateFullSend()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Full Send";
            card.description = "Reroll all of your dice.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.RerollAllDice,
                    diceTarget = ECardTarget.PlayerDice,
                },
            };
            return card;
        }

        // ── Card 21 ───────────────────────────────────────────────────────────────

        private static CardData CreateHexCurse()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Hex Curse";
            card.description = "Force the enemy to reroll 2 of their dice.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.RerollDice,
                    diceTarget = ECardTarget.EnemyDice,
                    count      = 2,
                },
            };
            return card;
        }

        // ── Card 22 ───────────────────────────────────────────────────────────────

        private static CardData CreateCursedDice()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Cursed Dice";
            card.description = "Force the enemy to reroll all of their dice.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.RerollAllDice,
                    diceTarget = ECardTarget.EnemyDice,
                },
            };
            return card;
        }

        // ── Card 23 ───────────────────────────────────────────────────────────────

        private static CardData CreateWarDrums()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "War Drums";
            card.description = "Reroll all enemy dice. Then deal 1 damage per unique value in their new roll.";
            card.energyCost  = 3;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.RerollAllDice,
                    diceTarget = ECardTarget.EnemyDice,
                },
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.EnemyDice,
                    triggerOn  = EPokerHandType.PerUniqueDieValue,
                    magnitude  = 1,
                },
            };
            return card;
        }

        // ── Card 24 ───────────────────────────────────────────────────────────────

        private static CardData CreateCursedAura()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Cursed Aura";
            card.description = "At the start of each enemy turn, force the enemy to reroll 1 of their dice.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Persistent;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.RerollDice,
                    diceTarget = ECardTarget.EnemyDice,
                    count      = 1,
                },
            };
            return card;
        }

        // ── Card 25 ───────────────────────────────────────────────────────────────

        private static CardData CreateLuckyCharm()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Lucky Charm";
            card.description = "Add 1 die to your pool for the rest of combat.";
            card.energyCost  = 3;
            card.duration    = ECardDuration.Persistent;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.AddDie,
                    diceTarget = ECardTarget.PlayerDice,
                },
            };
            return card;
        }

        // ── Card 26 ───────────────────────────────────────────────────────────────

        private static CardData CreateHighGuard()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "High Guard";
            card.description = "Block 1 damage for each die showing 3 or higher.";
            card.energyCost  = 1;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType     = EEffectType.GainBlock,
                    diceTarget     = ECardTarget.PlayerDice,
                    triggerOn      = EPokerHandType.PerHighDie,
                    valueThreshold = 3,
                    magnitude      = 1,
                },
            };
            return card;
        }

        // ── Card 27 ───────────────────────────────────────────────────────────────

        private static CardData CreateScatterDefense()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Scatter Defense";
            card.description = "Block 1 damage per unique die value showing.";
            card.energyCost  = 1;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.GainBlock,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerUniqueDieValue,
                    magnitude  = 1,
                },
            };
            return card;
        }

        // ── Card 28 ───────────────────────────────────────────────────────────────

        private static CardData CreateBrittleWard()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Brittle Ward";
            card.description = "Gain 5 Block. Apply 1 Frail to yourself.";
            card.energyCost  = 0;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.GainBlock,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.Always,
                    magnitude  = 5,
                },
                new CardEffectData
                {
                    effectType    = EEffectType.ApplyStatusEffect,
                    diceTarget    = ECardTarget.PlayerDice,
                    statusEffect  = EStatusEffect.Frail,
                    magnitude     = 1,
                },
            };
            return card;
        }

        // ── Card 29 ───────────────────────────────────────────────────────────────

        private static CardData CreateCripplingBlow()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Crippling Blow";
            card.description = "Deal 2 damage. Apply 2 Weak to the enemy.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.Always,
                    magnitude  = 2,
                },
                new CardEffectData
                {
                    effectType   = EEffectType.ApplyStatusEffect,
                    diceTarget   = ECardTarget.EnemyDice,
                    statusEffect = EStatusEffect.Weak,
                    magnitude    = 2,
                },
            };
            return card;
        }

        // ── Card 30 ───────────────────────────────────────────────────────────────

        private static CardData CreateExpose()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Expose";
            card.description = "Apply 2 Vulnerable to the enemy.";
            card.energyCost  = 1;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType   = EEffectType.ApplyStatusEffect,
                    diceTarget   = ECardTarget.EnemyDice,
                    statusEffect = EStatusEffect.Vulnerable,
                    magnitude    = 2,
                },
            };
            return card;
        }

        // ── Card 31 ───────────────────────────────────────────────────────────────

        private static CardData CreateBurningHands()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Burning Hands";
            card.description = "Deal 1 damage per die showing [5] or [6]. Apply 3 Burning to the enemy.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType     = EEffectType.DealDamage,
                    diceTarget     = ECardTarget.PlayerDice,
                    triggerOn      = EPokerHandType.PerHighDie,
                    valueThreshold = 5,
                    magnitude      = 1,
                },
                new CardEffectData
                {
                    effectType   = EEffectType.ApplyStatusEffect,
                    diceTarget   = ECardTarget.EnemyDice,
                    statusEffect = EStatusEffect.Burning,
                    magnitude    = 3,
                },
            };
            return card;
        }

        // ── Card 32 ───────────────────────────────────────────────────────────────

        private static CardData CreateVenomStrike()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Venom Strike";
            card.description = "Deal 1 damage for each pair rolled. Apply 2 Poisoned to the enemy.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType = EEffectType.DealDamage,
                    diceTarget = ECardTarget.PlayerDice,
                    triggerOn  = EPokerHandType.PerPair,
                    magnitude  = 1,
                },
                new CardEffectData
                {
                    effectType   = EEffectType.ApplyStatusEffect,
                    diceTarget   = ECardTarget.EnemyDice,
                    statusEffect = EStatusEffect.Poisoned,
                    magnitude    = 2,
                },
            };
            return card;
        }

        // ── Card 33 ───────────────────────────────────────────────────────────────

        private static CardData CreateStagger()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Stagger";
            card.description = "Apply 1 Stunned to the enemy. They skip their next attack.";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType   = EEffectType.ApplyStatusEffect,
                    diceTarget   = ECardTarget.EnemyDice,
                    statusEffect = EStatusEffect.Stunned,
                    magnitude    = 1,
                },
            };
            return card;
        }

        // ── Card 34 ───────────────────────────────────────────────────────────────

        private static CardData CreateStoneSkin()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Stone Skin";
            card.description = "Gain 2 Dexterity for the rest of combat (+2 Block on every GainBlock effect).";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType   = EEffectType.ApplyStatusEffect,
                    diceTarget   = ECardTarget.PlayerDice,
                    statusEffect = EStatusEffect.Dexterity,
                    magnitude    = 2,
                },
            };
            return card;
        }

        // ── Card 35 ───────────────────────────────────────────────────────────────

        private static CardData CreateBattleStance()
        {
            var card = ScriptableObject.CreateInstance<CardData>();
            card.name = card.cardName = "Battle Stance";
            card.description = "Gain 2 Strength for the rest of combat (+2 flat damage on every DealDamage effect).";
            card.energyCost  = 2;
            card.duration    = ECardDuration.Instant;
            card.effects = new List<CardEffectData>
            {
                new CardEffectData
                {
                    effectType   = EEffectType.ApplyStatusEffect,
                    diceTarget   = ECardTarget.PlayerDice,
                    statusEffect = EStatusEffect.Strength,
                    magnitude    = 2,
                },
            };
            return card;
        }
    }
}
