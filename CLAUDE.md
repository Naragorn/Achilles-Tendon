# Heel of Achilles — Achilles Tendon mod (Blade & Sorcery)

Implementation spec for this repo. Standalone mod, independent of DMCS.

Blade & Sorcery (VR) mod built with the official B&S SDK (Unity + C#, `ThunderRoad` namespace).

> **Important for the implementer:** API names below (`ThunderScript`, `EventManager.onCreatureHit`, `CollisionInstance`, `RagdollPart.Type`, `Creature`, `Item`, `Handle`, etc.) are best-guess references. **Verify every API against the current SDK version before using it** (official B&S modding wiki / SDK docs / decompiled `ThunderRoad.dll`). If an API differs, adapt — don't invent.

---

## General requirements

- Target the current B&S version (check the SDK version first and note it in `DEVELOPMENT.md`).
- Each mod is its own project/folder with its own manifest, so they can be installed independently.
- Mechanics apply **player vs. NPC only** (no NPC vs. NPC).
- Keep each mechanic in its own class so it can be enabled/disabled independently at runtime.

## In-game Mod Menu (required)

**Every feature must have an on/off toggle and all of its settings exposed in the in-game Mod Menu.** No magic numbers in code; every value listed under "Config" in each feature section below is a Mod Menu setting.

- Use the SDK's built-in mod options system (in recent versions: `[ModOption]` attributes on static fields/properties in a `ThunderScript` — **verify the exact attribute, parameters and supported types against the current SDK**).
- Group settings per feature (category = feature name), e.g. `Achilles Tendon`, `Bleed-Out`, `Ground Fighting`.
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

**Collapse pose: decision (Nadi, 2026-11-16)**
- Preferred pose for a crippled NPC: **on its knees / crawling**, not lying flat.
- **For now (option D): it lies flat.** Step 3 knocks the NPC down as a full ragdoll (`Ragdoll.State.Destabilized`) and holds it there. That stays as the fallback in any case.
- What the game offers (checked in `ThunderRoad.dll` 1.3.2 and the animation catalog `StreamingAssets/Default/bas.jsondb`, 26 `AnimationData` entries):
  - Kneeling: only `HumanIdleExamineKneel` (KneelingStart / ExamineKneel loop / KneelingEnd), an idle "examine the ground" pose, not a combat pose. Playable through `Creature.PlayAnimation(id)`.
  - Crawling: none. No crawl animation in the catalog, and no "crawl" or "kneel" identifier in `ThunderRoad.dll`.
- Options, with cost:
  - **A: kneel with the game's own kneel animation.** The NPC stays `Alive`, movement is blocked and the kneel loop plays. Cost: medium. Risk: it is an examine pose, and attack animations or the AI may override it. Plus: while `Alive`, the melee AI might keep attacking.
  - **B: crawl through physics**, with forces on the hands and torso of the lying ragdoll pulling it toward the player. Cost: large. Risk: it may look janky, and only the headset can judge it.
  - **C: own kneel/crawl animations**, built with the Unity SDK, shipped in an asset bundle and wired into the animator. Cost: very large.
  - **D: lie flat** (current state). No cost, but it is not the preferred pose.
- **Plan: we most likely build our own animation (C).** Alternative: ask someone for one, e.g. in the B&S modding Discord or elsewhere. Revisit this together with step 4 (ground fighting).

**Step 4 (ground fighting): researched, route A built and tested headless (2026-11-16)**
- Read in `ThunderRoad.dll` 1.3.2: in `Destabilized` the animator stays on and drives the joints through rotation pins (`Ragdoll.SetState`); pins can be set per body part (`Ragdoll.SetPinForce(..., partTypes)`); electrocution animates a lying NPC the same way (`BrainModuleElectrocute`). The weapon stays in hand (`RagdollHand.OnRagdollStateChange` re-attaches it). Screams: `BrainModuleSpeak.Play(dialogId, ...)`. The navmesh agent is off while lying (`Brain`), so crawling needs physics. Every human body part has `sliceForceKill: true`: a foot cut clean off kills.
- Routes: **A** animated arms on the lying ragdoll (stronger arm pins + the NPC's own attack clip), small-medium, recommended; **B** physics swings/crawl, medium; **C** kneel while `Alive`, medium; **D** own animations, large. The open question for A (does the animator let a dynamic clip through while falling) needs one in-game test.
- Existing mod with crawling / "go down for good": **Realistic Deaths (1.3) by pineapple54** (nexusmods.com/bladeandsorcery/mods/14774). Checked 2026-11-16: closed, deliberately obfuscated DLL, no public API; its Nexus permissions forbid modifying its files and using its assets. Triggering its crawl is therefore not viable (only by recreating one of its own triggers, e.g. cutting the foot off, which changes our mechanic). Both mods on the same NPC would very likely double the bleed-out and fight over the ragdoll. Per the author, its crawl is the **game's own** injured/crawl animations plus physics.
- The game's `animations_assets_all.bundle` contains clip names `Crawl`, `CrawlBackwards2`, `LyingInjured`, `Injured` (strings in the bundle; not in the AnimationData catalog, so presumably wired into the humanoid animator). The animator has an `Injured` float (`Brain.hashInjured`); `Burning` sets `SetAnimatorHeightRatio(0.2f)` + `Injured = 1` on a burning NPC that is still `Standing` with `forcePhysic`. Unverified whether that blends into crawling. This contradicts "Crawling: none" above, which only checked the catalog and the DLL.
- **Route A tested headless (2026-11-16, `harness/step4-a-ground-fight.txt`):** screams work. A swing (`PlayAnimation` with one of 23 stance clips) starts, but six pictures 0.3 s apart show no arm movement, and the game never reports the swing's end (`isPlayingDynamicAnimation` stays set, so only one swing ever starts). Stronger arm pins (x4) only stiffen the arms in the lying pose. Derived (not proven): the dynamic animation layer most likely does not drive a `Destabilized` body; a headset look would confirm. Defaults therefore: screams on, Arm Strength x1, Ground Attacks off.
- **Crawl pose built (2026-11-16, Nadi: "dann nutzen wir doch die Crawl-Animation"):** `Downed Pose = Crawl`, see `DEVELOPMENT.md` "What a heel hit does". The game's `Human` animator controller has no separate crawl state: `Crawling`/`CrawlBackwards` sit in the moving blend trees and `Laying`/`LayingInjured` in the idle blend trees, driven by `Height`, `Speed`/`Strafe` and `Injured`. Headless: low pose works, but the AI's full-body attack clips raise the NPC upright. Then built: attacks through `PlayUpperAnimation` with the AI's own melee off while crawling; headless pictures show it staying low and swinging, and it approaches the player (3.1 m -> 1.3 m). Open: the headset look.
- **Floating sit: cause measured (2026-11-16, `harness/step3-f-crawl-float.txt`, Debug Logging `CRAWL POSE` lines every 0.5 s).** The animator flips between the idle state (sitting pose `LayingInjured`, animated hips 0.09-0.12 m) and the moving state (crawl, animated hips 0.26-0.38 m) every 0.25-0.5 s, because the AI's small corrective moves keep the animator's `Speed` at about -0.13..0.17, right at the transitions' threshold of +-0.1 with no hysteresis (`Dual Wield (Idle) <-> (Moving)`, read from the controller). The physical hips cannot follow 0.2-0.3 m jumps and hang at 0.3-0.44 m with the sitting legs, which looks like sitting in the air. Ruled out by the same log: `Height` is not overwritten by another module (stays 0.20), nothing is under the hips but the ground and the NPC's own colliders, the root pin is the game's normal standing value (spring 1000, max force 1000, gravity on). While it truly crawls (Speed 0.3-0.58) or sits still for seconds, it is on the ground. A short float also shows in the first 0.5 s of the drop (body still falling from 0.89 m). **2026-11-17:** raising `Creature.stationaryVelocityThreshold` does not work: the game adds `angularSpeed` in degrees/s (`Locomotion`, `DeltaAngle/deltaTime`), and the nearly still NPC turns at 15-42 deg/s, so any useful threshold is always exceeded (measured). Built instead: **Steady Crawl Pose** (`CrawlGaitRules`, hysteresis on the raw local speed, Speed/Strafe zeroed in `ScriptUpdate`, which runs after all `Creature.ManagedUpdate`). Start 0.25 still flips (AI jitter is +-0.25-0.26 raw), Start 0.4 (default) keeps the animator idle. **Open:** after a real crawl stops, the animator stays idle (animated hips 0.1 m) but the physical hips sink slowly, 0.41 -> 0.20 m over about 4 s (run `20261117-095149`; pictures in `20261117-094912/step3-g-start04` f15-f17 show the float); `CRAWL BODY` (run `20261117-101357`): only the lower legs touch the ground during the float, items/hands/torso 30-60 cm up, with and without shield; the body rests on its shins as in the kneeling crawl. Why the leg pins fold them so slowly is not measured (`Joint.currentForce` read 0). **Built: Leg Strength** (legs' rotation pins x4 while crawling, damper x sqrt; `CrippleRules.LegPin`). Run `20261117-104430`: sitting samples with physical hips >15 cm above animated: x1 4/10, x4 0/4, x8 0/7 (one run each). Open: headset look.
- Earlier next routes for swinging/crawling (before the crawl pose): **D** the game's injured/crawl animator states (keep the NPC `Standing` with `forcePhysic`, low height ratio, `Injured = 1`, as `Burning` does), medium, needs the animator graph confirmed (read-only extraction of the controller from the bundle) and a headset look; **B** physics forces on the weapon hand / torso, medium.

**DMCS compat (Nadi, 2026-11-17):** a heel cut (20) and the player's kill of an already crippled NPC (35, "execution" = any player kill, decided with Nadi) fill DMCS's Adrenaline meter. Route chosen with Nadi: a public API in DMCS (`DMCS.Adrenaline.AddExternal(float, string)`, built by the DMCS session), called by reflection, no hard dependency; the amounts are Achilles menu options (category `DMCS Compat`). Details in `DEVELOPMENT.md` "DMCS compat".

---

## Build order

1. Project setup + SDK version check + empty mod loading in game with debug logging + Mod Menu skeleton.
2. Hit-zone detection with debug visualization (gizmo for the heel zone, logging of part/angle/damage type).
3. Collapse: NPC can no longer stand or walk.
4. Ground fighting: crawling, swinging from the ground.
5. Bleed-out over time with blood effects and weakening.
6. ~~Optional player hardcore mode.~~ Won't do (Nadi, 2026-11-17); the detection-only option `Apply To Player` was removed as well (v0.8.1).

After each step: build, test in game, and note what was verified vs. assumed (in `DEVELOPMENT.md`). `README.md` is only for players: what the mod does, install, Mod Menu, compat; no implementation detail (Nadi, 2026-11-17).
