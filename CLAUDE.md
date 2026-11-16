# Heel of Achilles — Achilles Tendon mod (Blade & Sorcery)

Implementation spec for this repo. Standalone mod, independent of DMCS.

Blade & Sorcery (VR) mod built with the official B&S SDK (Unity + C#, `ThunderRoad` namespace).

> **Important for the implementer:** API names below (`ThunderScript`, `EventManager.onCreatureHit`, `CollisionInstance`, `RagdollPart.Type`, `Creature`, `Item`, `Handle`, etc.) are best-guess references. **Verify every API against the current SDK version before using it** (official B&S modding wiki / SDK docs / decompiled `ThunderRoad.dll`). If an API differs, adapt — don't invent.

---

## General requirements

- Target the current B&S version (check the SDK version first and note it in a README).
- Each mod is its own project/folder with its own manifest, so they can be installed independently.
- Mechanics apply **player vs. NPC only** (no NPC vs. NPC).
- Keep each mechanic in its own class so it can be enabled/disabled independently at runtime.

## In-game Mod Menu (required)

**Every feature must have an on/off toggle and all of its settings exposed in the in-game Mod Menu.** No magic numbers in code; every value listed under "Config" in each feature section below is a Mod Menu setting.

- Use the SDK's built-in mod options system (in recent versions: `[ModOption]` attributes on static fields/properties in a `ThunderScript` — **verify the exact attribute, parameters and supported types against the current SDK**).
- Group settings per feature (category = feature name), e.g. `Achilles Tendon`, `Bleed-Out`, `Ground Fighting`, `Player Hardcore Mode`.
- Each group starts with `Enabled` (bool toggle), followed by its settings.
- Use sliders with sensible min/max/step for numbers, dropdowns for enums, toggles for bools. Give every option a short tooltip/description.
- Changes must apply **live** without restarting the game or reloading the level. Toggling a feature off mid-fight must cleanly stop it (e.g. stop bleed-out coroutines, let crippled NPCs stand again if desired).
- Settings must persist between sessions (via the mod options system's own saving, or a JSON fallback if not supported).
- Global extras per mod: `Debug Logging` toggle (logs heel hits, angle checks, collapse, bleed-out ticks to the console) and a `Reset to Defaults` option if the SDK supports buttons.
- Sub-effects get their own toggles too, e.g. `Blood Effects`, `Screaming`, `Crawling`, `Ground Attacks` each individually switchable.

---

Separate mod. Realistic crippling hit: a cut to the Achilles tendon takes the enemy down permanently, and they bleed out slowly.

**Behavior**
- Trigger: a **slashing/piercing** hit (not blunt) on the lower leg/foot region, at the **back of the heel**, from behind or the side.
- Effect:
  1. NPC collapses immediately and can no longer stand or walk.
  2. NPC keeps fighting from the ground: crawling, flailing, swinging at the player if in reach, screaming.
  3. Progressive **bleed-out**: health drains over `bleedOutDuration` (e.g. 30–60 s) with growing blood effect, NPC gets weaker/slower over time, then dies.
- Player can finish them earlier normally.
- Optional (config, default off): same mechanic applies to the player (hardcore mode — player can't stand/locomote properly and bleeds).

**Implementation hints**
- On hit: check part type (lower leg/foot), damage type (slash/pierce), and hit location in the part's local space (back of heel = local position near the posterior lower end; tune with a debug gizmo).
- Check the angle between the hit direction and the NPC's facing (from behind / side within `maxHitAngle`).
- Disable the NPC's ability to stand (force ragdoll/destabilize state, block get-up) while keeping arms/AI attack active.
- Bleed-out: coroutine applying damage over time + blood decals/particles.

**Config:** `heelZoneSize`, `maxHitAngle`, `allowedDamageTypes`, `bleedOutDuration`, `groundFightingEnabled`, `applyToPlayer`.

---

## Build order

1. Project setup + SDK version check + empty mod loading in game with debug logging + Mod Menu skeleton.
2. Hit-zone detection with debug visualization (gizmo for the heel zone, logging of part/angle/damage type).
3. Collapse: NPC can no longer stand or walk.
4. Ground fighting: crawling, swinging from the ground.
5. Bleed-out over time with blood effects and weakening.
6. Optional player hardcore mode.

After each step: build, test in game, and note what was verified vs. assumed (in README).
