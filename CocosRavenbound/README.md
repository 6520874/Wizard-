# Ravenbound Cocos

Cocos Creator 2.4.15 port scaffold for the Ravenbound hunter-contract prototype.

## Open

Use the locally installed editor:

`/Applications/Cocos/Creator/2.4.15/CocosCreator.app`

Open this folder as the project:

`/Users/steven.shan/AITest/Ravenbound/Assets/CocosRavenbound`

Main scene:

`assets/Main.fire`

## What Was Ported

- Core contract loop: accept contract, investigate with only 3 actions, prepare, fight, judge the truth, collect endings.
- Random contract variables: culprit, weakness, villager attitude, weather, cost, and monster.
- Lightweight turn-based battle tuned for story choices instead of action complexity.
- Reputation stats: gold, conscience, fear, fame.
- Existing Ravenbound art and audio copied into `assets/resources` for Cocos loading.

## Notes

This is a Cocos 2.4.15 playable foundation, not a full one-to-one Unity scene conversion. The old Unity project remains untouched so the port can be iterated safely.
