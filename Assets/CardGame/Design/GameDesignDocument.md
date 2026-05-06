# Lich Lord: Card & Dice Combat — Game Design Document

## Overview

A turn-based card game with poker-dice mechanics. The player builds a deck of action cards and fights enemies through a series of combat encounters. Each round, both the player and the enemy roll pools of six-sided dice. Cards are played to spend those dice results on damage, defense, dice manipulation, and status effects.

---

## Core Concepts

### Dice
- **Player dice pool**: 5 six-sided dice (d6), rolled at the start of every round.
- **Enemy dice pool**: 2–5 six-sided dice, defined per enemy and rolled at the start of every round.
- Dice results are evaluated using **poker-dice style** rules: singles, pairs, triples, straights, full houses, four-of-a-kind, and five-of-a-kind (Yahtzee).

### Poker-Dice Hand Reference
| Hand | Description | Example |
|---|---|---|
| Single Value | Count dice showing a specific face | Three [4]s showing |
| Pair | Exactly two dice share a value | 3, 3 |
| Two Pair | Two separate pairs | 2, 2, 5, 5 |
| Triple | Exactly three dice share a value | 6, 6, 6 |
| Straight | Five sequential values | 1-2-3-4-5 or 2-3-4-5-6 |
| Full House | One triple + one pair | 4, 4, 4, 2, 2 |
| Four of a Kind | Exactly four dice share a value | 1, 1, 1, 1 |
| Five of a Kind | All dice show the same value | 5, 5, 5, 5, 5 |
| High Die (threshold) | Dice showing a value ≥ threshold | Dice showing 3 or higher |
| Low Die (threshold) | Dice showing a value ≤ threshold | Dice showing 1 or 2 |

### Energy
- The player has a **maximum energy** value (default: 3) that resets at the start of every round.
- Each card has an **energy cost** (0–3). Cards can only be played if the player has enough current energy.
- Playing a card reduces current energy by its cost. Energy does not carry between rounds.

### Cards
- The player builds a **deck** before combat.
- At the start of each round, the player **draws 5 cards** from their deck.
- Cards can be played freely in any order during the player's turn, up to the energy limit.
- Unplayed cards are **discarded** at end of round.
- If the draw pile is empty, the discard pile is **shuffled** back into it.

#### Card Duration
- **Instant**: Effect fires once when played, card goes to discard.
- **Persistent**: Effect was applied once when played (e.g., add a die) AND recurring effects (block, reroll) apply again at the start of each subsequent round. Persistent cards remain in play for the rest of combat. One-time effects like AddDie and status applications do NOT repeat each round.

#### Card Targets
Cards specify which dice pool their effects evaluate — and for block/status effects, which combatant receives the result:
- **Player Dice**: Read your own dice; effect applies to the player.
- **Enemy Dice**: Read the enemy's dice; effect applies to the enemy.

---

## Turn Structure

### 1. Roll Phase
Both the player and the enemy roll all their dice simultaneously.

### 2. Enemy Pre-Round Effects
Enemy dice are adjusted by pre-round passives (e.g., rerolling specific values). The new enemy dice are used for the rest of the round.

### 3. DOT Phase (Damage Over Time)
Any active **Burning** or **Poisoned** status effects deal their damage:
- Each affected combatant takes damage equal to current stacks.
- If either combatant dies here, combat ends immediately.

### 4. Persistent Effect Phase
Any persistent cards already in play apply their recurring effects:
- Block cards grant block based on the current dice roll.
- Persistent enemy-reroll cards force the enemy to re-roll one or more dice.

### 5. Player Turn
The player may play any number of cards from their hand as long as they can afford the energy cost.

Card effects resolve immediately in play order:
- Rerolling dice changes the dice pool for subsequent card evaluations.
- Damage dealt reduces the enemy's health immediately (after enemy block absorbs).
- Block gained reduces incoming damage from the enemy this round.
- Status effects are applied immediately and take full effect this round (damage modifiers) or next round (DOT).

The player ends their turn when they choose to pass or run out of playable cards.

### 6. Enemy Turn
The enemy's turn resolves in order:
1. **Enemy Block**: Enemy gains block from GainBlock passives (evaluated against their current dice roll).
2. **Enemy Status Application**: Any status effects the enemy applies to the player trigger (e.g., Burning, Weak).
3. **Enemy Damage**: All enemy damage passives are totaled. If the enemy is **Stunned**, this step is skipped entirely.
   - **Weak** on the enemy reduces their damage by 25% (rounds down).
   - **Strength** on the enemy adds flat damage.
   - **Vulnerable** on the player increases the incoming damage by 50% (rounds up).
   - The final damage is reduced by the player's remaining block. Excess damages the player's health.

### 7. End of Round
- All **duration-based** status effects on both combatants decrement by 1 (removed at 0). Strength and Dexterity are permanent and never decrement.
- Enemy block is cleared.
- Player block is cleared (only persistent block cards restore it next round).
- The player's hand is discarded.
- A new round begins (Step 1).

---

## Player Rules
- **Starting Health**: 30 HP.
- **Starting Energy**: 3 per round (can be modified by deck/items).
- **Starting Dice Pool**: 5 d6.
- The player wins when the enemy's health reaches 0.
- The player loses when their own health reaches 0.

---

## Enemy Rules
- Enemies do **not** have cards or a hand.
- Each enemy has a set of **passive effects** defined at creation that fire every round automatically.
- Passive effects are evaluated against the **enemy's own dice roll**, not the player's.
- Enemies have **fixed dice counts** (2–5 dice), defined per enemy.
- Some enemies have **pre-round effects** that modify their own dice before the player's turn (e.g., rerolling unfavorable faces).
- Enemies can **gain block** from GainBlock passives (evaluated each enemy turn before dealing damage).
- Enemies can **apply status effects** to the player through ApplyStatusEffect passives.
- Enemy block is cleared at the end of each round, just like the player's.

---

## Status Effects

Status effects are tracked as **stacks**. Multiple applications of the same effect add to the existing stack count. All duration-based effects decrement by 1 at the **end of each round** and are removed when their count reaches 0.

| Status | Target | Effect | Duration |
|---|---|---|---|
| **Weak** | Player or Enemy | Affected combatant deals 25% less damage (rounds down) | Duration (stacks = rounds) |
| **Vulnerable** | Player or Enemy | Affected combatant receives 50% more damage (rounds up) | Duration (stacks = rounds) |
| **Burning** | Player or Enemy | Takes `stacks` fire damage at the start of each round | Duration (stacks = rounds remaining) |
| **Poisoned** | Player or Enemy | Takes `stacks` poison damage at the start of each round | Duration (stacks = rounds remaining) |
| **Stunned** | Enemy only | Enemy skips their damage phase for `stacks` rounds | Duration |
| **Frail** | Player or Enemy | Block gained is reduced by 25% (rounds down). Applied after Dexterity. | Duration |
| **Strength** | Player or Enemy | Deals `stacks` extra flat damage on every DealDamage effect | Permanent (never decrements) |
| **Dexterity** | Player or Enemy | Gains `stacks` extra block on every GainBlock effect. Applied before Frail. | Permanent (never decrements) |

### Damage Modifier Order (Player dealing damage)
1. Base damage = `triggerCount × magnitude`
2. Add Strength bonus (flat)
3. Apply Weak reduction (×0.75, round down)
4. Apply enemy Vulnerable bonus (×1.5, round up)
5. Enemy block absorbs first, remainder reduces enemy health

### Block Modifier Order
1. Base block = `triggerCount × magnitude`
2. Add Dexterity bonus (flat)
3. Apply Frail reduction (×0.75, round down)
4. Block is added to current total

---

## Card Effect Types

| Type | Description |
|---|---|
| Deal Damage | Deal `(magnitude × triggerCount + Strength) × Weak × Vulnerable` damage to the enemy. Enemy block absorbs first. |
| Conditional Damage | Deal `magnitude` damage (with modifiers) if condition is met; otherwise the player takes `altMagnitude` damage |
| Self Damage | Player takes `magnitude` damage |
| Gain Block | Target combatant gains `(magnitude × triggerCount + Dexterity) × Frail` block |
| Reroll Dice | Reroll `count` dice from the target pool (lowest values rerolled first in automation) |
| Reroll All Dice | Reroll all dice in the target pool |
| Reroll By Value | Reroll all dice in target pool that show a specific value |
| Add Die | Add one die to a pool (rolled immediately) |
| Remove Die | Remove one die from a pool |
| Apply Status Effect | Apply `magnitude` stacks of a status effect to the target combatant (`diceTarget` determines player or enemy) |

---

## Example Cards

### Damage Cards
- **Swift Slash** (1 energy): Deal 1 damage for each [1] rolled.
- **Ace High** (1 energy): Deal 3 damage for each [6] rolled.
- **Low Sweep** (1 energy): Deal 1 damage for each die showing [1] or [2].
- **Twin Fangs** (1 energy): Deal 1 damage for each pair rolled.
- **Spirit Strike** (2 energy): Deal 2 damage for each pair rolled.
- **Berserker's Rage** (2 energy): Deal 1 damage for each odd die ([1], [3], [5]) rolled.
- **Focused Strike** (2 energy): Deal 3 damage for each triple rolled.
- **Scatter Shot** (2 energy): Deal 1 damage for each unique die value showing.
- **Chain Lightning** (3 energy): Deal 2 damage for each pair found in the enemy's dice.
- **Gambler's Blade** (2 energy): Deal 5 damage if you have a straight; otherwise deal 1 damage.
- **Precision Strike** (2 energy): Deal 5 damage for each four-of-a-kind rolled.
- **Crushing Blow** (3 energy): Deal 4 damage per full house (triple + pair) rolled.
- **Dragon's Roar** (3 energy): Deal 6 damage per five-of-a-kind rolled.
- **Death's Gamble** (1 energy): If all 5 dice match, deal 8 damage. Otherwise, take 2 damage.

### Defense Cards
- **Iron Shield** (1 energy): Block 1 damage for each [4] rolled.
- **Bone Ward** (1 energy): Block damage equal to the value of your highest die.
- **High Guard** (1 energy): Block 1 damage for each die showing **3 or higher**.
- **Scatter Defense** (1 energy): Block 1 damage per unique die value showing.
- **Brittle Ward** (0 energy): Gain 5 Block. Apply 1 **Frail** to yourself. (Trade-off: free block with a downside)
- **Tower Shield** (2 energy, Persistent): Each round, block 1 damage per die showing 5 or 6.
- **Rune Shield** (2 energy, Persistent): Each round, block 2 damage per [6] rolled.

### Dice Manipulation
- **Lucky Reroll** (1 energy): Reroll up to 3 of your dice.
- **Full Send** (2 energy): Reroll all of your dice.
- **Hex Curse** (2 energy): Force the enemy to reroll 2 of their dice.
- **Cursed Dice** (2 energy): Force the enemy to reroll all of their dice.
- **War Drums** (3 energy): Reroll all enemy dice, then deal 1 damage per unique value in their new roll.
- **Cursed Aura** (2 energy, Persistent): Each round, force the enemy to reroll 1 of their dice.
- **Lucky Charm** (3 energy, Persistent): Add 1 die to your pool for the rest of combat.

### Status Effect Cards
- **Expose** (1 energy): Apply 2 **Vulnerable** to the enemy.
- **Crippling Blow** (2 energy): Deal 2 damage. Apply 2 **Weak** to the enemy.
- **Burning Hands** (2 energy): Deal 1 damage per [5] or [6] rolled. Apply 3 **Burning** to the enemy.
- **Venom Strike** (2 energy): Deal 1 damage per pair rolled. Apply 2 **Poisoned** to the enemy.
- **Stagger** (2 energy): Apply 1 **Stunned** to the enemy (they skip their next attack).
- **Stone Skin** (2 energy): Gain 2 **Dexterity** permanently for this combat.
- **Battle Stance** (2 energy): Gain 2 **Strength** permanently for this combat.

---

## Example Enemies

### Goblin Scout (15 HP, 2 dice)
- Deals 1 damage for each [2] rolled.
- Deals 2 damage for each pair rolled.
- **Applies 1 Burning to the player for each pair rolled.**

### Orc Warrior (25 HP, 3 dice)
- Deals 2 damage for each pair rolled.
- Deals 1 damage for each odd die rolled.
- **Gains 1 Block for each die showing [5] or [6].**
- **Applies 1 Vulnerable to the player for each triple rolled.**

### Shadow Wraith (20 HP, 4 dice)
- Deals 1 damage for each odd die rolled.
- Deals 2 damage for each [1] rolled.
- **Applies 1 Weak to the player if a straight is rolled.**

### Stone Golem (40 HP, 3 dice)
- Rerolls all dice showing [1] at the start of each round (pre-round effect).
- **Gains 1 Block for each even die rolled.**
- Deals 3 damage for each triple rolled.
- Deals 1 damage for each even die rolled.

### Death Knight (35 HP, 5 dice)
- **Gains 2 Block for each pair rolled.**
- Deals 2 damage for each pair rolled.
- Deals 4 damage for each triple rolled.
- **Applies 2 Poisoned to the player for each triple rolled.**
- Deals 6 damage if a straight is rolled.

---

## Deck Building Guidelines (Starter Deck)
A balanced starter deck of 10–15 cards might include:
- 2–3 block cards for sustained defense, including at least one threshold-based card (e.g., High Guard).
- 3–4 damage cards at various energy costs.
- 1–2 reroll cards for dice manipulation.
- 1 persistent card for long-term advantage.
- 1 status effect card (e.g., Expose or Crippling Blow) to weaken the enemy.
- 1 high-risk/high-reward card for burst potential.

---

## Glossary
| Term | Definition |
|---|---|
| Trigger Count | Number of times a condition is met (e.g., 3 dice showing [4] → trigger count = 3) |
| Magnitude | The base value applied per trigger (damage dealt, block gained) |
| Persistent | A card effect that applies repeatedly each round until combat ends |
| Energy | Resource spent to play cards; resets each round |
| Block | Damage reduction that absorbs incoming damage before health is reduced; cleared each round |
| Discard | Cards played or unused at end of round enter the discard pile |
| Weak | Status: combatant deals 25% less damage per round of stacks |
| Vulnerable | Status: combatant takes 50% more damage per round of stacks |
| Burning | DOT status: take stacks fire damage at the start of each round |
| Poisoned | DOT status: take stacks poison damage at the start of each round |
| Stunned | Status: target skips attack phase for stacks rounds |
| Frail | Status: block gained is reduced by 25% per round of stacks |
| Strength | Permanent buff: deal stacks extra flat damage on every DealDamage effect |
| Dexterity | Permanent buff: gain stacks extra block on every GainBlock effect |
| DOT | Damage Over Time; refers to Burning and Poisoned effects |
