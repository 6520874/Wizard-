# A1 Cold Fog Atmosphere Design

## Goal

Improve the Unity scene art direction with the selected A1 "cold fog pressure" look while keeping the existing PNG assets intact.

## Design

Add a lightweight runtime atmosphere component to the 2D world. The background image remains the base layer. The new component creates several generated SpriteRenderer overlays:

- a cold blue-grey tint over the background,
- two low ground mist bands,
- a dark vignette around the camera view,
- a subtle horizon shadow band.

The component lives on the same `Background` GameObject as `WitcherRuntimeBackground`, so it travels with the existing scene setup. `WitcherWorldDirector` applies room-specific atmosphere colors alongside the current background tint.

## Constraints

No source art is overwritten. The implementation uses generated 1x1 or radial textures so it can be tuned in code and works in the existing scene without new imported assets.

## Verification

Verification should include C# compilation or Unity editor compilation. Visual validation is done by opening `Scenes/WitcherHuntDemo.unity` and confirming the road background is darker, colder, and foggier while Geralt and enemies remain readable.
