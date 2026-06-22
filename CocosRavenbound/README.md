# Ravenbound Cocos

Cocos Creator 2.4.15 playable port for the Ravenbound hunter-contract prototype.

## Open

Use the locally installed editor:

`/Applications/Cocos/Creator/2.4.15/CocosCreator.app`

Open this folder as the project:

`/Users/steven.shan/AITest/Ravenbound/Assets/CocosRavenbound`

Main scene:

`assets/Main.fire`

## What Was Ported

- Three-night main story loop: opening, night transition, ordered investigation nodes, ambushes, truth choice, boss battle, settlement, and the next-night hook.
- Core contract loop: accept contract, pick a partner, investigate with limited actions, prepare, fight, judge the truth, collect endings.
- Random contract variables: culprit, weakness, villager attitude, weather, cost, and monster.
- Lightweight turn-based battle with hunter, Triss/Yennefer-style allies, mana, potions, statuses, enemy skills, turn order preview, and investigation-based monster modifiers.
- Reputation stats: gold, conscience, fear, fame.
- Existing Ravenbound art and audio copied into `assets/resources` for Cocos loading.

## Notes

This is a Cocos 2.4.15 playable logic port, not a one-to-one Unity physics/map conversion. Unity-specific movement, colliders, UGUI, and Inspector workflows are represented as Cocos button-driven story and battle state machines so the game can run from `assets/Main.fire`.
