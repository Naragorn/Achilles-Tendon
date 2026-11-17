# Heel of Achilles - development notes

Everything about how the mod is built and tested: build state, versions, the game APIs used and what is verified vs. assumed, how each mechanic decides, and how to build and run the headless tests. The player-facing description and the Mod Menu reference are in [`README.md`](README.md); the implementation spec is [`CLAUDE.md`](CLAUDE.md).

## Status

| Build step | State |
|---|---|
| 1. Project setup, SDK check, mod loads, debug logging, Mod Menu skeleton | built; **headless-tested** (`harness/smoke.txt`: mod and script load, all options exist (36 since `Apply To Player` was removed) with their defaults, live toggles apply, no exceptions). Not yet looked at in the headset (the Mod Menu page itself) |
| 2. Heel hit-zone detection, debug visualization, logging | built; detection and logging **headless-tested** (`harness/step3-*.txt`: `HEEL HIT` for slash/pierce on the heel from behind and from the side, by hand, held, thrown dagger and spell; blunt and frontal hits refused). **Not checked:** the heel zone spheres (Show Heel Zones) and the zone's placement on the model with real weapon contact, both headset checks |
| 3. Collapse | built; **headless-tested** with BasHarness (`harness/step3-*.txt`: slash on the heel from behind -> NPC down and still down after 30 s; mod off -> stays down or stands up per Stand Up When Disabled; blunt and frontal hits refused). **Not yet tested in the headset** (real weapon contact, physics, look). Attacking from the ground is step 4 (see below) |
| 4. Ground fighting | Built: screams (on), ground attacks for crawling NPCs (on, Ground Attack Chance 75 %). Lying flat NPCs only scream: route A (full-body swing on a lying body) did not move it headless (`harness/step4-a`, 2026-11-16), so it and Arm Strength were removed (Nadi, 2026-11-17). Crawling: see "What a heel hit does". **Not yet judged in the headset.** |
| 5. Bleed-out | built, unit-tested; harness scenarios written (`harness/step5-*.txt`: cut NPC dies after Bleed-Out Duration; Bleed-Out off or Achilles Tendon off pauses it, on again finishes it) and **headless-tested**, 7/7 scenarios PASS on 2026-11-16 (NPC cut at health 49/50 dies after 10 s, `BLED OUT` at 10.0 s; health dropped a bit faster than the pure drain, 28.4 instead of 34.3 at 3 s, most likely impact damage from the falling ragdoll (not checked), which the drain absorbs as designed). **Not yet tested in the headset** (the blood drip's look and placement). Weakening (crawls slower, screams less often, heavy breathing near the end) built and unit-tested, **headless-tested** 2026-11-16 (`harness/step5-c-weakening.txt`, PASS: log shows crawl x1.0 -> x0.32 and scream interval x1.0 -> x1.9 over 20 s, `GASP` from 50 %; with Weakening off everything stays x1.0); whether the slower crawl is visible is a headset check |
| 6. Player hardcore mode | **won't do** (Nadi, 2026-11-17). The detection-only option `Apply To Player` was removed too (v0.8.1): hits on the player never count |

## Versions

| What | Version | Source |
|---|---|---|
| Game (installed, Steam) | **1.3.2.261012003**, Steam build 25901209, DLLs dated 2026-10-14 | `Player.log`: `Game version: 1.3.2.261012003`; `appmanifest_629730.acf` |
| Official SDK | **1.3.0** (README), game settings asset says 1.3.1.1; latest commit 2026-11-06 | `dev.azure.com/Warpfrog/BasSDK` (README, `Assets/SDK/Config/ThunderRoad/GameSettings/BladeAndSorcery.asset`). The GitHub repo `KospY/BasSDK` is frozen at 1.1.0 and points to Azure DevOps. |
| Unity | 2021.3.38f1 | `Player.log` and SDK `ProjectSettings/ProjectVersion.txt` agree |
| Manifest `GameVersion` | `1.0.0.0` | SDK `minModVersion: 1.0.0.0`; `ModManager` requires an exact match |
| Target framework | .NET Framework 4.7.2 | all installed code mods target it (DLL metadata) |

**API reference used:** the game's own `ThunderRoad.dll` (1.3.2), decompiled locally with ILSpy 9.1. That is the code that runs, so it beats any doc. The decompiled source is not part of this repo.

## API check: verified vs. assumed

"Verified" = read in the decompiled `ThunderRoad.dll` 1.3.2 and the code compiles against it. "Verified" does not mean "seen working in game" - that is the in-game test below.

### Verified in `ThunderRoad.dll`

| Spec name | Actual API | Notes |
|---|---|---|
| `ThunderScript` | `ThunderRoad.ThunderScript` | `ModManager` instantiates every non-abstract subclass, calls `ScriptLoaded(ModData)`, then `Enable()` → `ScriptEnable()`. `ScriptUpdate()` runs each frame. |
| `[ModOption]` | `ModOption(name, tooltip[, valueSourceName])` on **static** fields/properties/methods | + `ModOptionCategory(name, order)`, `ModOptionOrder`, `ModOptionSlider`, `ModOptionTooltip`, `ModOptionFloatValues`, `ModOptionSave/DontSave`. Supported types: bool, int, float, string, enum, Color, CatalogData. |
| Live apply | `ModOption.Apply` writes the static field when the value changes | Settings take effect immediately. |
| Persistence | `Options.SetModOption(modFolder, optionName, index, save)` | Saved by **option name + value index** per mod folder. |
| Reset to defaults | Built in: `UIModsMenuContentArea.ResetSettings`, wired to the mod page's reset button | So the mod has no own "Reset" option. |
| `EventManager.onCreatureHit` | `delegate CreatureHitEvent(Creature, CollisionInstance, EventTime)` | Raised by `Creature.Damage` with `OnStart` (always) and `OnEnd` (only if the hit does not kill). |
| `CollisionInstance` | `contactPoint`, `contactNormal`, `impactVelocity`, `sourceColliderGroup`, `targetColliderGroup`, `casterHand`, `damageStruct` | |
| `DamageStruct` | `damageType`, `damage`, `active`, `hitRagdollPart`, `penetration`, `damager` | |
| `DamageType` | `Unknown, Pierce, Slash, Blunt, Energy, Fire, Lightning, UnBlockable` | |
| `RagdollPart.Type` | flags: `LeftLeg, RightLeg, LeftFoot, RightFoot`, ... | Legs have `RagdollPart.Section` (`Full, Lower, Mid, Upper`); the game itself uses `GetPart(LeftLeg, Section.Upper)`. |
| `Creature` | `isPlayer`, `isKilled`, `ragdoll`, `creatureId`, static `allActive` | |
| `Ragdoll.GetPart(Type, Section)` | exists | |
| Attacker | `sourceColliderGroup.collisionHandler.item.lastHandler.creature`, else `casterHand.mana.creature` | Same chain `Creature.Damage` uses for `lastInteractionCreature`. |
| Manifest | `Name, Description, Author, ModVersion, GameVersion, Thumbnail` | |
| Knock down (step 3) | `Ragdoll.CancelGetUp(resetState:false)` + `Ragdoll.SetState(Ragdoll.State.Destabilized)` | Exactly what `Creature.PushFallback` does. `MaxPush`/`TryPush` were not used: they go through `BrainModuleHitReaction`, which may only stagger. |
| Keep down (step 3) | `Brain.AddNoStandUpModifier(object)` / `RemoveNoStandUpModifier(object)` | The only caller of `Ragdoll.StandUp` is `BrainModuleHitReaction.OnCycle`, and only while `brain.noStandupModifiers` is empty. Electrocute, Grabbed, GravityZone and Floating use the same hold. Removing it lets the game stand the NPC up by itself. |
| Bleed drain (step 5) | `Creature.currentHealth` setter, `Creature.Kill()` | The setter raises the health-change event. `Creature.Damage` was not used: it multiplies the damage (`GetDamageMultiplier`, `physicsOffDamageMult`), which would distort the duration, and raises `onCreatureHit` every tick. `Kill()` is the game's own kill (Energy, 99999). |
| Blood (step 5) | `Catalog.GetData<EffectData>("PenetrationDeepBleeding")`, `EffectData.Spawn(position, rotation, parent, ...)`, `EffectInstance.SetIntensity/Play/End` | The drip a deep stab leaves when the blade comes out (`Damager.cs`; `HumanMale` uses it on the legs). Its particle runs on the `End` step, so each pulse is Spawn, Play, End. Tagged `sensitiveContent: Blood`, so the game's blood setting applies. The game has no bleeding status of its own (only Burning, Electrocute, Floating, Slowed, Speed). |
| Ground fighting (step 4) | `RagdollPart.bone.SetPinRotationForce` on the arm parts, `Creature.PlayAnimation(AnimationClip, loop:false)`, `BrainModuleSpeak.Play("hit", true)`; attack clips from `BrainModuleMelee.meleeIdle.attackMotions`, else `BrainModuleStance.GetStanceDataFromEquipment().AllStanceNodes()` | Lying pin value: `springRotationForce * destabilizedGroundSpringRotationMultiplier` (`Ragdoll.OnFallEvent`). `PlayAnimation` is what `BrainModuleMelee` uses for its own attacks. A target-less NPC (HumanDummy) holds its weapon in a plain idle pose, hence the stance fallback (23 clips found for a short sword). |
| "Is down" | `Creature.state == Destabilized` (ragdoll `Destabilized` or `Inert`) and `!ragdoll.standingUp` | |

### Assumed - must be confirmed in game

| Assumption | Why it matters | How the test shows it |
|---|---|---|
| The foot `RagdollPart`'s transform sits at the ankle joint | The heel zone is placed relative to it | Heel zone spheres sit behind the ankles |
| The NPC's `transform.forward` is where the body faces | "Behind" and the attack angle depend on it | Spheres behind the heels, angle ≈ 0° from behind |
| Lower leg = `LeftLeg/RightLeg` with `section=Lower` | Only logged for now, the zone check does not depend on it | Log line `section=` on calf hits |
| One of the shaders `Sprites/Default`, `Universal Render Pipeline/Unlit`, `Unlit/Color` exists in the build | Debug spheres need a visible material | Log line `Debug markers use shader: ...` |
| `impactVelocity` direction | Its sign convention is not documented; logged as `impactVelAngle` for comparison only | Compare it with `angleFromBehind` in the log |
| `item.lastHandler` stays set after a real throw | Thrown knives count as player hits. Headless (`harness/step3-d-attackers.txt`) only shows that the mod resolves the player when `lastHandler` is set (the harness sets it itself); a held dagger (real `RagdollHand.Grab`) and a spell (`casterHand`) also resolve to the player | Throw a dagger at a heel |
| A bow-shot arrow resolves to the player as attacker | The "Paris / Jara" shot should count | Shoot an arrow at a heel |

### Engine behaviours found that shape the code

- **Enum defaults**: the mod menu compares an enum default with the boxed int list and never matches, so it falls back to index 0. `AllowedDamageTypes` therefore lists the default (`SlashAndPierce`) first. A unit test pins this.
- **Float defaults**: the default is matched with `object.Equals` on boxed floats. The game's own `start + i * step` builder drifts, so value lists come from `OptionSpecs` (decimal arithmetic). A unit test checks every default is in its list bit-exactly.
- **Option names must be unique per mod** (saved by name). So the toggle is "Achilles Tendon Enabled", not "Enabled"; later features follow the same pattern.
- **Saved by index**: changing a value range later shifts saved settings. Use the mod page's reset button after updating.
- **No nullable metadata**: the game rejects `ModOption`s compiled with nullables; the project disables them.
- **`Brain.Stop` clears every no-stand-up hold** (`ClearNoStandUpModifiers`). It runs on death, despawn and when an NPC catches fire (`Burning`). So the hold is re-applied every frame, and an NPC that started getting up anyway is knocked down again.
- **The AI does not attack while lying down**: `Creature.state` is `Destabilized` then, and `BrainModuleMelee` drops every attack unless the state is `Alive`. The spec's "keep arms/AI attacking" therefore cannot come from the game's own AI while the NPC lies down; it is built in step 4 (ground fighting).

Mod Menu reference (all options, defaults, ranges): [`README.md`](README.md#mod-menu). Every option does something; the spec's `groundFightingEnabled` is "Ground Fighting Enabled".

## How a heel hit is decided

Pure logic in `src/AchillesTendon/Logic`, unit-tested without the game. Checks in this order; the first failing one is the reason in the log:

1. `NotPlayerVsNpc` - attacker must be the player and the target an NPC. NPC vs NPC, any hit on the player and hits from an unknown source never count (with `Debug Logging` only a one-line `skip (NotPlayerVsNpc)` note). A creature hitting itself (`SelfHit`, checked before this, e.g. a lying NPC touching its own body with its weapon, or the player hitting themself) never counts and is not logged at all
2. `NotLegPart` - hit part is `LeftLeg/RightLeg/LeftFoot/RightFoot`
3. `NoAnkle` - the foot part on that side exists
4. `DamageTypeNotAllowed` - per "Allowed Damage Types" (blunt never counts)
5. `OutsideHeelZone` - contact point within the zone sphere: center = ankle + back offset (opposite the NPC's horizontal facing) + height offset
6. `AttackAngleUndetermined` / `AttackAngleTooFrontal` - horizontal angle between "straight behind the NPC" and the direction to the attacker ≤ Max Hit Angle

The attack angle uses the attacker's position, not `impactVelocity`, because the velocity's sign convention is not verified (see above).

## What a heel hit does (step 3)

A heel hit on a living NPC knocks it down and holds it there (`CrippleController`). Every frame, each crippled NPC gets one decision (`CrippleRules.Decide`, unit-tested):

| Situation | Action |
|---|---|
| Dead or despawned | forget it (the game already cleared the hold) |
| Mod off, Stand Up When Disabled on | remove the hold; the game stands it up by itself |
| Mod off, Stand Up When Disabled off | keep holding it down |
| Mod on, lying down | keep holding it down |
| Mod on, up or getting up anyway (e.g. after catching fire) | knock it down again |

**Lie Flat Chance (Nadi, 2026-11-17; replaces the Downed Pose dropdown):** each crippled NPC rolls its pose once at the cut (`CrippleRules.PoseFor`, default 5 % lie flat); a changed chance applies to the next cut, a crippled NPC keeps its pose. **Crawl:** the table above applies to Lie Flat. With Crawl the NPC is not knocked over: it stays a standing ragdoll (`Alive`), and every frame gets the animator settings the game itself uses for a burning NPC that drops (`SetAnimatorHeightRatio(Crawl Body Height)`, animator float `Injured = 1`), plus a locomotion speed modifier (Crawl Speed) and `ragdoll.forcePhysic`. In the game's `Human` animator controller (read from `animations_assets_all.bundle`), a low `Height` and `Injured` blend the idle states into `Laying`/`LayingInjured` and the moving states into `Crawling`/`CrawlBackwards`. A knocked-down NPC is left to get up by itself and goes back into the crawl. With Lie Flat Chance 100 % the next cut NPC lies flat (`harness/step3-e-crawl.txt`). Full suite 9/9 PASS on 2026-11-16.

**Floating sit, fixed headless (Steady Crawl Pose + Leg Strength, 2026-11-17):** when the crawling NPC is nearly still, the game's animator flipped every 0.25-0.5 s between the sitting idle pose (hips 0.1 m) and the crawl pose (hips 0.3 m): the AI's small back-and-forth moves (raw local speed about +-0.25) sit right at the animator's switch at `Speed`/`Strafe` 0.1, which has no hysteresis. The physical body cannot follow and hangs in between, sitting in the air. Steady Crawl Pose adds the hysteresis (`CrawlGaitRules`): it only counts as crawling from Crawl Start Speed and sits again below Crawl Stop Speed; while sitting, `Speed`/`Strafe` are set to 0 in `ScriptUpdate`, which the game runs after every `Creature.ManagedUpdate` (`UpdateManager.Update`). Measured headless (`harness/step3-f-crawl-float.txt`, `GAIT |` and `CRAWL POSE` lines): with Start 0.25 the jitter (0.25-0.26) still crossed it about 10 times a second, with 0.4 (default) the animator stays in the idle state. **Second cause (fixed by Leg Strength, below):** after a real crawl stops, the animator stays idle (animated hips 0.09-0.12 m), but the physical hips only sink slowly, from 0.41 to 0.20 m over about 4 s (run 20261117-095149); pictures of the 0.4 run show it sitting in the air meanwhile. `CRAWL BODY` lines (2026-11-17, run 20261117-101357) show what holds it: during the float only the lower legs touch the ground (0-2 cm), hands, torso and held items are 30-60 cm up; it happens without a shield too (two swords; axe and sword). So the body still rests on its shins as in the kneeling crawl and the legs only slowly fold into the sitting pose. Not measured: why the leg pins do not move them faster (the pins' `Joint.currentForce` read 0). Debug Logging: `CRAWL POSE` lines every 0.5 s (animator state, `Speed`, raw speed, turn speed, physical/animated hips and feet, gait), `GAIT |` on every sit/crawl switch. **Leg Strength (2026-11-17):** while crawling, the legs' rotation pins are the ragdoll's own standing values times Leg Strength (spring and max force x, damper x sqrt, `CrippleRules.LegPin`), re-applied every frame, reset to x1 when the crawl ends. Measured headless (run 20261117-104430, one run each, so small samples): CRAWL POSE samples while sitting with the physical hips more than 15 cm above the animated ones: x1 4 of 10 (earlier x1 runs: 6/8, 6/11, 4/6, 6/11), x4 0 of 4, x8 0 of 7. Pictures of x4 and x8 show it sitting on the ground after about 0.5 s of transition. Default x4. Not checked: the headset look (stiff legs?).

While crawling, the NPC's own melee AI is switched off (`BrainModuleMelee.meleeEnabled = false`, restored afterwards): its attack clips move the whole body and raised it upright in the first headless pictures. Instead, Ground Attacks plays one of its attack clips on the **upper-body layer** (`Creature.PlayUpperAnimation`) when you are in reach.

Headless pictures (2026-11-16, `harness/step3-e-crawl.txt`): the NPC stays low the whole time (kneeling or sitting, sword in hand), swings with the upper body, and moves towards the player (logged distance 3.1 m -> 1.3 m over about 12 s). Full suite 9/9 PASS. At times it hung in a sitting pose slightly above the ground; cause and fix in "Floating sit" above. Not yet judged in the headset.

**Kneel Chance (Nadi, 2026-11-17, default 30 % of the crawlers):** started as a belly crawl (crawlers fall forward and crawl face down, by percentage). Read in the game's `Human` controller (sub-agent, UnityPy, root rotation of each clip): the forward `Crawling` clip is face down on all fours, the idle `Laying`/`LayingInjured` and `CrawlBackwards` lie on the back; no face-down idle exists; the stab deaths `DeathKneelForward` 1-3 end face down. Built: a kneeler plays one of those as a FullBody one-shot at the cut (`FALL FORWARD`, chosen by name: `AnimationData.Pick(Forward)` returned `Sword1h_Death_back`) and is held in the moving blend tree on the forward crawl (`CrawlGaitRules.KneelGait`: Speed at least 0.12, just above the idle switch at 0.1; Strafe 0). Headless (probes `harness/probe/kneel.txt`, then still named belly crawl, runs 20261117-192804 and -195433): it drops to its knees and then **kneels upright**, torso vertical, sword or hands raised, also with ground attacks off; it never lies face down. Not measured: why (upper-body weapon/stance layer keeping the torso up, or the blend at Speed 0.12 sitting closer to `Crouch_Idle`). Nadi kept the kneeling as its own pose (2026-11-17), so there are three poses: sitting crawler (default), kneeler (Kneel Chance of the crawlers), lying flat (Lie Flat Chance, rolled first). Kneelers swing with ground attacks like the sitting crawlers. Not checked: whether a kneeler visibly moves towards the player, the headset look.

**Ground Attack Chance (Nadi, 2026-11-17):** Ground Attacks is on by default; each crippled NPC rolls once at the cut (`GroundFightRules.IsAttacker`, `Random.value` against the chance, 100 % always, 0 % never, unit-tested) whether it swings from the ground. The others only crawl and scream. `GROUND FIGHT START` logs `attacker=yes/no (roll, chance)`. Headless: `harness/step4-b-attack-chance.txt` (0 %: no `GROUND ATTACK` in 8 s within range), `step3-e-crawl` (100 %: swings). Run 20261117-181931, full suite 12/12 PASS.

**Floating death, fixed (2026-11-17):** a crawling NPC that died hung stiffly above the sand for about 3 s, back arched (showcase run 20261117-175741, s17). Cause, measured with `DEATH POSE` lines (Debug Logging, every 0.5 s for 6 s after a crippled NPC's death; probe `harness/probe/death-pose.txt`, run 20261117-180857): after `Creature.Kill` the ragdoll is `Inert`, but the animator stays on and every part keeps a rotation pin of 800 that fades 800 -> 782 -> 719 -> 585 -> 357 -> 140 -> 31 -> off over 3.5 s; torso 30-60 cm and head 65-105 cm above the ground meanwhile, with Leg Strength x4 and x1 alike. That is the game's `BrainModuleDeath` (decompiled 1.3.2): it records the ragdoll state at `OnKill` start, and because a crawling NPC is a standing ragdoll (`Standing` counts as animated), it plays a standing death clip on the FullBody layer with the pins scaled by its death curve (`DyingCoroutine`). The clip is made for a standing body; the crawler's body on the ground is propped up by it. Fix: in `EventManager.onCreatureKill` OnEnd (raised after `Creature.OnKillEvent`, so the dying coroutine has started) Achilles calls the module's own abort `BrainModuleDeath.StopDying()` (the game uses it when a dying NPC is pushed) for an NPC that died crawling (`DeathRules.StopDyingAnimation`, unit-tested); it then falls as a plain ragdoll. `DEATH | <npc> | died crawling, standing death animation stopped`. After the fix (run 20261117-181931, same probe): pins off and animator off from the first sample, torso 0-1 cm and head 0-12 cm above the ground from 0.5 s on, for bleed-out (x4, x1) and a player kill. Lie Flat NPCs are not touched (they keep the game's own death). Not checked: the headset look.

Turning the mod off in the game's mod list (`ScriptDisable`) or unloading it releases every NPC. The player is never knocked down (step 6 dropped).

## Bleed-out (step 5)

Every crippled NPC bleeds out (`BleedController`). Each frame, one decision per NPC (`BleedOutRules.Step`, unit-tested):

| Situation | Action |
|---|---|
| Dead (e.g. finished off by the player) or despawned, or no longer crippled (released) | forget it |
| Bleed-Out Enabled off, or Achilles Tendon Enabled off | pause: no drain, the elapsed time is kept |
| Bleed-Out Duration reached (also when the slider is moved below the elapsed time) or no health left | kill it |
| Otherwise | drain health linearly from its current value to zero at the end of the duration |

Because the drain starts from the current health every frame, extra hits make the NPC die sooner and healing simply refills what drains away; it still dies at the end of the duration. All values are read every frame, so slider changes apply live.

Every Blood Pulse Interval the game's own blood drip plays at the cut, with an intensity that grows from 1/n to 1 over the n pulses. With Debug Logging on, each pulse also logs `BLEED | <npc> | health=.. | t=../.. s`; `BLEED START` and `BLED OUT` mark the start and the end.

**Weakening** (the spec's "gets weaker/slower over time", `WeakeningRules`, unit-tested): from the bleed-out progress p (elapsed / duration, 0 to 1) the crawl speed is multiplied by a value going linearly from 1 to Weakest Crawl Speed, the scream interval by a value going from 1 to Scream Slowdown, and from p >= Gasping From the scream becomes the voice's `heavybreath` dialog (every human voice in the 1.3.2 catalog has it). Paused bleeding keeps the NPC as weak as it is. The BLEED log line shows the current factors (`weakening crawl=x.. scream interval=x..`, `gasping`); a gasp logs `GASP` instead of `SCREAM`.

## DMCS compat (2026-11-17)

What the player sees: README "DMCS compat". Decided with Nadi 2026-11-17:

| Event | Points (menu) | Counts when |
|---|---|---|
| Heel cut | Heel Cut Adrenaline, 20 | the cut cripples the NPC (a second cut on an already crippled NPC does not) |
| Execution | Crippled Kill Adrenaline, 35 | the player kills an NPC that was already crippled: any of the player's hits (weapon, thrown, spell, fist, kick) |

Never counted: bleeding out (`Creature.Kill()` has no attacker), a heel cut that kills at once (the NPC never crawled; the cut itself still counts), kills of NPCs that were not crippled. For scale, DMCS's own gains: a hit 5, disarm and charge stagger 15, full meter 100. DMCS's normal hit gain for the cutting hit comes on top.

How: DMCS owns the meter. Achilles calls `DMCS.Adrenaline.AddExternal(float amount, string source)` (sources `AchillesHeelCut`, `AchillesCrippledKill`) by reflection: the game loads every mod DLL with `Assembly.Load` into one AppDomain (`ModManager.TryLoadAssembly`, decompiled 1.3.2), so Achilles has no reference to DMCS, finds it by name on each gain (load order does not matter) and runs unchanged without it. DMCS applies its own rules (Adrenaline off, or a full meter waiting for the critical, refuses). Rules: `DmcsCompatRules`, lookup: `DmcsBridge`, both unit-tested. The kill uses Achilles' own attacker resolution, not `CollisionInstance.IsDoneByPlayer()`: for a punch or kick that asks the player's own `lastInteractionCreature` (decompiled 1.3.2). Debug Logging: `DMCS | <source> +<points> | <npc> | Applied/Refused`, once `DMCS | not found` without DMCS. Harness: `harness/compat/dmcs-adrenaline.txt` (needs `DMCS_MOD=<DMCS build folder>`, not in the default run). **Headless-tested 2026-11-17** (run 20261117-131457, DMCS ff4c85a, PASS): DMCS's own log shows the meter at 20 (cut), 55 (execution +35), 75 (cut that kills at once, no execution gain), 95 (cut, then bled out, no execution gain); a kill of a never crippled NPC and everything with DMCS Adrenaline off reached DMCS not at all. Without DMCS: `DMCS | not found`, smoke and step3-b PASS (run 20261117-131321). **Every way the player kills counts** (run 20261117-134155, PASS, amounts 5/10 for these cases): a crippled NPC killed by fist (`by=hand`), kick (`by=foot`), thrown dagger and spell each gave the execution gain; DMCS's log continues 100 -> 110 -> ... -> 155/300. **Rerun against DMCS fbe3292** (run 20261117-141039, Achilles with `Apply To Player` removed, smoke + compat 2/2 PASS): DMCS loaded with 83 options, and its log shows exactly the same meter sequence 20, 55, 75, 95, 100 ... 155/300. Not checked: the headset feel of the amounts, a bow-shot arrow (see the assumptions table), real weapon contact (the harness hits through `Creature.Damage`).

## Build

Needs the .NET SDK (8.0 used) and the game installed (the build references its DLLs).

```sh
dotnet test                                                  # unit tests (no game needed)
dotnet build -c Release                                      # mod in dist/AchillesTendon
dotnet build src/AchillesTendon -c Release -p:DeployToGame=true   # copy into the game's Mods folder
tools/harness.sh                                             # headless in-game scenarios (harness/*.txt) via BasHarness
# other install path:  -p:BasDir="/path/to/Blade & Sorcery"
```

Headless tests use [BasHarness](https://github.com/Naragorn/BasHarness) (expected next to this repo): it starts the game with a mock HMD in a mirrored install with its own Mods folder, prefix and options, so the real install is not touched. It checks loading, options, logs and synthetic hits (`hit-npc`: a hit through `Creature.Damage` at the part's position, no physics contact), so real weapon contact, the zone placement on the model and the look of the collapse still need the headset. Scenarios in one run share the prefix and the game keeps option values across them, so each scenario sets the options it depends on.

Log: `<prefix>/drive_c/users/steamuser/AppData/LocalLow/Warpfrog/BladeAndSorcery/Player.log` (Proton) - all lines start with `[AchillesTendon]`.
