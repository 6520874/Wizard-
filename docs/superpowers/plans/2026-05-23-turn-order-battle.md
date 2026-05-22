# Turn Order Battle Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the current player-then-enemy-phase combat flow with a speed-driven turn order and lunge-based melee attack presentation.

**Architecture:** `TurnBasedBattleManager` remains the combat authority and gains a small in-memory action-unit scheduler for player and enemy turns. `TurnBasedBattleHud` remains the presentation layer and gains target-aware player and enemy melee travel so attack animations move from home position toward the target and return before the next scheduled actor proceeds.

**Tech Stack:** Unity C#, UGUI, coroutines, existing `GeraltController`, `GeraltAnimator`, `TurnBasedEnemyState`, and runtime battle HUD.

---

## File Map

- Modify `Assets/Scripts/TurnBased/TurnBasedBattleManager.cs` to create action units, schedule actors by speed/action value, and replace `EnemyPhase`.
- Modify `Assets/Scripts/TurnBased/TurnBasedBattleHud.cs` to expose stage positions and play player/enemy lunge attacks toward the opposing side.
- Verify with `dotnet build /Users/steven.shan/AITest/PixelRaidGame/Assembly-CSharp.csproj --no-restore`.

### Task 1: Replace fixed enemy phase with a turn scheduler

**Files:**
- Modify: `Assets/Scripts/TurnBased/TurnBasedBattleManager.cs`

- [ ] **Step 1: Add the smallest scheduler data model**

Add a private `BattleTurnUnit` class with `EnemyIndex`, `Speed`, `ActionValue`, `IsPlayer`, and `IsAlive` access through the manager's existing player/enemy data.

- [ ] **Step 2: Initialize units when battle starts**

When `TryBeginBattle` creates enemy states, build one player unit and one unit per enemy. Give the player a default speed above the heavy knight baseline and give enemy units a default speed derived from their encounter slot until per-enemy speed data exists.

- [ ] **Step 3: Dispatch the next actor**

Replace the fixed initial player wait with a coroutine that advances action values to an action threshold, picks the highest ready living unit, then either enables player commands or runs the enemy AI attack.

- [ ] **Step 4: Stop calling the old group counterattack**

After a player command resolves, refresh the HUD, run victory/defeat checks, then schedule the next actor instead of running `EnemyPhase`.

- [ ] **Step 5: Keep enemy AI minimal**

Use the current enemy damage formula for the selected enemy unit only, play its attack, apply damage, refresh HUD, and return control to the scheduler.

### Task 2: Make melee attacks travel toward their target

**Files:**
- Modify: `Assets/Scripts/TurnBased/TurnBasedBattleHud.cs`

- [ ] **Step 1: Add target-aware player attack entry point**

Change player melee animation to accept the selected enemy index. Read that enemy slot home position and move the player figure toward a safe stopping point to the left of the target before returning home.

- [ ] **Step 2: Upgrade enemy attack travel**

Use the player figure home position as the enemy attack destination. Enemy attack frames should move farther forward than the old small pulse, stop before overlapping the player sprite completely, then settle back to the enemy slot.

- [ ] **Step 3: Preserve idle and hurt states**

While a figure is busy, keep idle frame cycling disabled. At the end of the travel animation restore home position, scale, color, and idle fallback frames.

### Task 3: Wire the new scheduler and presentation together

**Files:**
- Modify: `Assets/Scripts/TurnBased/TurnBasedBattleManager.cs`
- Modify: `Assets/Scripts/TurnBased/TurnBasedBattleHud.cs`

- [ ] **Step 1: Pass selected target into player attack HUD animation**

In `PlayerAttack`, call the target-aware HUD attack method before damage and hurt feedback.

- [ ] **Step 2: Keep non-melee actions valid**

Flame should use a shorter player cast gesture or the existing player attack gesture without target travel dependency. Defense, item, and escape should consume or preserve a player turn using their current rules.

- [ ] **Step 3: Add concise Chinese class-adjacent comments for any new private helper**

Keep comments focused on scheduler intent where code would otherwise look like timing math.

### Task 4: Verify and commit

**Files:**
- Inspect: `Assets/Scripts/TurnBased/TurnBasedBattleManager.cs`
- Inspect: `Assets/Scripts/TurnBased/TurnBasedBattleHud.cs`

- [ ] **Step 1: Build the Unity C# assembly**

Run:

```bash
dotnet build /Users/steven.shan/AITest/PixelRaidGame/Assembly-CSharp.csproj --no-restore
```

Expected: exit code `0` with no C# compilation errors.

- [ ] **Step 2: Review the git diff**

Run:

```bash
git -C /Users/steven.shan/AITest/PixelRaidGame/Assets diff -- Assets/Scripts/TurnBased/TurnBasedBattleManager.cs Assets/Scripts/TurnBased/TurnBasedBattleHud.cs
```

Expected: only scheduler and melee presentation changes for this feature.

- [ ] **Step 3: Commit the step in Chinese**

Run:

```bash
git -C /Users/steven.shan/AITest/PixelRaidGame/Assets add Assets/Scripts/TurnBased/TurnBasedBattleManager.cs Assets/Scripts/TurnBased/TurnBasedBattleHud.cs docs/superpowers/specs/2026-05-23-turn-order-battle-design.md docs/superpowers/plans/2026-05-23-turn-order-battle.md
git -C /Users/steven.shan/AITest/PixelRaidGame/Assets commit -m "战斗：加入行动顺序回合演出"
```

Expected: a local commit containing the scheduler, lunge presentation, spec, and plan.
