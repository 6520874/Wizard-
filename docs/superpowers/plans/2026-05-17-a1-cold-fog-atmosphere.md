# A1 Cold Fog Atmosphere Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the selected A1 cold fog atmosphere look in the Unity 2D scene.

**Architecture:** Add a focused `WitcherAtmosphereLayer` MonoBehaviour that generates and owns visual overlay child objects. `WitcherWorldDirector` configures it per room, and `WitcherSceneCreator` adds it to regenerated scenes.

**Tech Stack:** Unity 2022.3 C#, SpriteRenderer, generated Texture2D/Sprite assets at runtime.

---

### Task 1: Ignore Temporary Brainstorm Files

**Files:**
- Modify: `.gitignore`

- [x] **Step 1: Add `.superpowers/` to `.gitignore`**

Expected content:

```gitignore
.superpowers/
```

### Task 2: Add Runtime Atmosphere Component

**Files:**
- Create: `Scripts/World/WitcherAtmosphereLayer.cs`

- [x] **Step 1: Create `WitcherAtmosphereLayer`**

The component should expose `ApplyLook(Color32 tint, Color32 fog, float fogStrength, float vignetteStrength)` and create child SpriteRenderer layers named `Atmosphere Tint`, `Atmosphere Horizon Shadow`, `Atmosphere Fog Near`, `Atmosphere Fog Far`, and `Atmosphere Vignette`.

- [x] **Step 2: Keep generated sprites cached**

Generated solid and radial sprites should be static caches to avoid recreating textures every room transition.

### Task 3: Wire Atmosphere Into Scene Setup

**Files:**
- Modify: `Scripts/Editor/WitcherSceneCreator.cs`
- Modify: `Scripts/World/WitcherWorldDirector.cs`

- [x] **Step 1: Add `WitcherAtmosphereLayer` to generated Background**

`CreateBackground()` should add `WitcherAtmosphereLayer` after `WitcherRuntimeBackground`.

- [x] **Step 2: Extend room look data**

`RoomDefinition` should include atmosphere tint, fog color, fog strength, and vignette strength.

- [x] **Step 3: Apply room look**

`ApplyRoomLook()` should find or add `WitcherAtmosphereLayer` on the Background object and call `ApplyLook(...)`.

### Task 4: Verify

**Files:**
- Check: `Scripts/World/WitcherAtmosphereLayer.cs`
- Check: `Scripts/World/WitcherWorldDirector.cs`
- Check: `Scripts/Editor/WitcherSceneCreator.cs`

- [x] **Step 1: Compile-check C#**

Run a local C# compilation command if available, or run Unity editor compilation if Unity is available.

- [x] **Step 2: Inspect diff**

Confirm only intended source files, docs, and `.gitignore` changed. `.idea/` remains untouched.
