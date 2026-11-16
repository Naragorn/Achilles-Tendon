# Heel of Achilles

Blade & Sorcery mod: cutting an enemy's Achilles tendon makes them fall. They can no longer stand, keep fighting from the ground and slowly bleed out.

Inspired by two heroes who fell to a single strike at the one weak point of their body: **Achilles** (Greek) and **Krishna** (Indian). The idea of the mod is the same: the precise expert strike instead of raw force.

Every feature can be toggled and tuned in the in-game Mod Menu.

Full implementation spec: [`CLAUDE.md`](CLAUDE.md).

## Background: two myths, one weak point

### Achilles - the heel

- His mother, the sea goddess Thetis, dipped the infant Achilles in the river Styx to make him invulnerable. She held him by the heel, so that spot stayed dry and mortal.
- In the Trojan War he was the strongest and fastest fighter. He died when the Trojan prince Paris hit that heel with an arrow, guided by the god Apollo.
- The tendon above the heel is named after him (Achilles tendon), and "Achilles' heel" became the word for a fatal weak spot in great strength.
- Note: Homer's *Iliad* does not tell his death or the heel. The dipping in the Styx is first told explicitly by the Roman poet Statius (*Achilleid* 1.133-134), and the oldest sources speak of the ankle (Latin *talus*) rather than the heel.

### Krishna - the sole of the foot

- In the Indian epic *Mahabharata*, the sage Durvasa granted Krishna a boon: his whole body would be invulnerable except for his feet (*Anushasana Parva*, 13.159).
- At the end of his life, after his own clan had destroyed itself (*Mausala Parva*, Book 16), Krishna sat in deep meditation in the forest.
- A hunter named Jara (Sanskrit "old age") saw the red marks of Krishna's foot through the bushes, took it for a deer and shot. The arrowhead was forged from a piece of iron from the cursed club that had destroyed his clan. The arrow pierced the sole of the foot.
- Jara begged forgiveness; Krishna comforted him and then left the world. (The *Bhagavata Purana* 11.30 tells the same death.)

Sources: [Achilles' heel (Wikipedia)](https://en.wikipedia.org/wiki/Achilles%27_heel), [Statius, *Achilleid* 1 (Theoi)](https://www.theoi.com/Text/StatiusAchilleid1A.html), [Ancient World Magazine: Origins of a hero's weak point](https://www.ancientworldmagazine.com/articles/achilles-heel-origins-heros-weak-point/), [Mausala Parva (Wikipedia)](https://en.wikipedia.org/wiki/Mausala_Parva), [W. Doniger, *Mahabharata* Book 16 (Oxford, 2022)](https://doi.org/10.1093/oso/9780197553398.003.0003), [Jara (mahapurana.com)](https://www.mahapurana.com/hindu-mythology/jara-jaras-hunter-killed-krishna/).

## Status

| Build step | State |
|---|---|
| 1. Project setup, SDK check, mod loads, debug logging, Mod Menu skeleton | built, **not yet tested in game** |
| 2. Heel hit-zone detection, debug visualization, logging | built, **not yet tested in game** |
| 3. Collapse | built; **headless-tested** with BasHarness (`harness/step3-*.txt`: slash on the heel from behind -> NPC down and still down after 30 s; mod off -> stays down or stands up per Stand Up When Disabled; blunt and frontal hits refused). **Not yet tested in the headset** (real weapon contact, physics, look). Attacking from the ground is step 4 (see below) |
| 4. Ground fighting | **experimental, partly working.** Built: screams (on by default), arm strength and ground attacks (both off by default). Headless test (`harness/step4-a-ground-fight.txt`, pictures with `screenshot of=npc`): 8/8 scenarios PASS on 2026-11-16; screams play; a swing is started, but the pictures show no arm movement, and the game never reports the swing's end. So route A does not swing yet; stronger arm pins only stiffen the arms in the lying pose. Crawling: not built. Details and next routes in `CLAUDE.md` |
| 5. Bleed-out | built, unit-tested; harness scenarios written (`harness/step5-*.txt`: cut NPC dies after Bleed-Out Duration; Bleed-Out off or Achilles Tendon off pauses it, on again finishes it) and **headless-tested**, 7/7 scenarios PASS on 2026-11-16 (NPC cut at health 49/50 dies after 10 s, `BLED OUT` at 10.0 s; health dropped a bit faster than the pure drain, 28.4 instead of 34.3 at 3 s, most likely impact damage from the falling ragdoll (not checked), which the drain absorbs as designed). **Not yet tested in the headset** (the blood drip's look and placement). Weakening (crawls slower, screams less often, heavy breathing near the end) built and unit-tested, **headless-tested** 2026-11-16 (`harness/step5-c-weakening.txt`, PASS: log shows crawl x1.0 -> x0.32 and scream interval x1.0 -> x1.9 over 20 s, `GASP` from 50 %; with Weakening off everything stays x1.0); whether the slower crawl is visible is a headset check |
| 6. Player hardcore mode | detection built (`Apply To Player`, default off), **not yet tested in game**; effects follow with steps 3-5 |

Step 2 only detects and reports heel hits. It does not change the fight.

**Player hardcore mode** (`Apply To Player`, default off): the same heel check runs when an NPC hits the player, with the same zone, angle and damage type rules. For now that means only log lines and markers, like for NPCs; collapse and bleed-out for the player come with steps 3-5. In practice it will rarely trigger, because NPCs do not aim at heels - a heel cut on the player only happens by chance, e.g. a low sweep from behind.

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
| NPC weapons collide with the player's leg/foot parts at all | Without that, `Apply To Player` never fires. The game puts standing player parts on layer `Avatar` (`RagdollPart.RefreshLayer`); which layers collide is set in Unity's physics matrix, not in the DLL | With Apply To Player + Debug Logging on, let an NPC hit your legs and look for `target=` lines naming the player |
| The player creature's `transform.forward` follows the body in VR | The attack angle on the player depends on it | Player heel zone spheres (Show Heel Zones) sit behind your real heels |
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

## Mod Menu

| Category | Option | Default | Range |
|---|---|---|---|
| Achilles Tendon | Achilles Tendon Enabled | on | |
| | Heel Zone Size (radius) | 8 cm | 2-20 cm |
| | Heel Zone Back Offset | 4 cm | 0-15 cm |
| | Heel Zone Height Offset | 3 cm | -5-15 cm |
| | Max Hit Angle (0 = straight behind, 90 = side) | 110° | 30-180° |
| | Allowed Damage Types | Slash and Pierce | Slash and Pierce / Slash only / Pierce only |
| | Stand Up When Disabled (NPCs with a cut tendon get up when the mod is turned off) | on | |
| | Downed Pose (Crawl = the game's injured/crawl animations; Lie Flat = ragdoll on the ground) | Crawl | Crawl / Lie Flat |
| | Crawl Speed (movement while crawling, relative to walking) | x0.3 | x0.05-x1 |
| | Crawl Body Height (0 = flat, 1 = standing) | x0.2 | x0-x0.6 |
| Bleed-Out | Bleed-Out Enabled (off pauses the bleeding, on resumes it) | on | |
| | Bleed-Out Duration (cut to death) | 45 s | 10-180 s |
| | Blood Effects (drip from the cut heel, stronger towards the end) | on | |
| | Blood Pulse Interval (between drips and bleed log lines) | 2 s | 0.5-10 s |
| | Weakening (crawls slower, screams less often, heavy breathing near the end) | on | |
| | Weakest Crawl Speed (share of Crawl Speed at the moment of death) | 25 % | 5-100 % |
| | Scream Slowdown (Scream Interval multiplier at the moment of death) | x3 | x1-x5 |
| | Gasping From (bleed-out share from which screams become the voice's `heavybreath`) | 75 % | 50-100 % |
| Ground Fighting | Ground Fighting Enabled | on | |
| | Arm Strength (arm pins of a lying NPC; x1 = the game's own limp value) | x1 | x1-x10 |
| | Ground Attacks (crawling: upper-body swings; lying flat: experimental, no visible swing). Off: a downed NPC does not attack at all (its own melee AI stays off while crawling) | off | |
| | Ground Attack Range | 1.8 m | 0.5-4 m |
| | Ground Attack Interval | 3 s | 1-10 s |
| | Screaming (pain cry, the voice's `hit` dialog) | on | |
| | Scream Interval | 6 s | 2-30 s |
| Player Hardcore Mode | Apply To Player (detection only so far; your own collapse is step 6) | off | |
| Debug | Debug Logging | off | |
| | Show Heel Zones (NPCs; also yours with Apply To Player) | off | |
| | Show Hit Markers | off | |
| | Hit Marker Duration | 5 s | 1-30 s |

Every option above does something; the spec's `groundFightingEnabled` is "Ground Fighting Enabled".

## How a heel hit is decided

Pure logic in `src/AchillesTendon/Logic`, unit-tested without the game. Checks in this order; the first failing one is the reason in the log:

1. `NotPlayerVsNpc` - attacker must be the player and the target an NPC; with `Apply To Player` on, an NPC hitting the player counts too. NPC vs NPC and hits from an unknown source never count (with `Debug Logging` only a one-line `skip (NotPlayerVsNpc)` note). A creature hitting itself (`SelfHit`, checked before this, e.g. a lying NPC touching its own body with its weapon, or the player hitting themself) never counts and is not logged at all
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

**Downed Pose = Crawl (default):** the table above applies to Lie Flat. With Crawl the NPC is not knocked over: it stays a standing ragdoll (`Alive`), and every frame gets the animator settings the game itself uses for a burning NPC that drops (`SetAnimatorHeightRatio(Crawl Body Height)`, animator float `Injured = 1`), plus a locomotion speed modifier (Crawl Speed) and `ragdoll.forcePhysic`. In the game's `Human` animator controller (read from `animations_assets_all.bundle`), a low `Height` and `Injured` blend the idle states into `Laying`/`LayingInjured` and the moving states into `Crawling`/`CrawlBackwards`. A knocked-down NPC is left to get up by itself and goes back into the crawl. Switching the pose live works both ways (`harness/step3-e-crawl.txt`). Full suite 9/9 PASS on 2026-11-16.

**Known issue, the floating sit (cause measured, not fixed yet):** when the crawling NPC is nearly still, the animator flips every 0.25-0.5 s between the sitting idle pose (hips 0.1 m) and the crawl pose (hips 0.3 m), because the AI's small moves keep `Speed` right at the transitions' threshold of 0.1. The physical body cannot follow and hangs in between, sitting in the air. With Debug Logging on, `CRAWL POSE` lines (every 0.5 s per crawling NPC) show the animator state, `Speed`, physical and animated hips and feet; `harness/step3-f-crawl-float.txt` takes a picture every 0.5 s to match them.

While crawling, the NPC's own melee AI is switched off (`BrainModuleMelee.meleeEnabled = false`, restored afterwards): its attack clips move the whole body and raised it upright in the first headless pictures. Instead, Ground Attacks plays one of its attack clips on the **upper-body layer** (`Creature.PlayUpperAnimation`) when you are in reach.

Headless pictures (2026-11-16, `harness/step3-e-crawl.txt`): the NPC stays low the whole time (kneeling or sitting, sword in hand), swings with the upper body, and moves towards the player (logged distance 3.1 m -> 1.3 m over about 12 s). Full suite 9/9 PASS. At times it hangs in a sitting pose slightly above the ground (cause not checked). Not yet judged in the headset.

Turning the mod off in the game's mod list (`ScriptDisable`) or unloading it releases every NPC. The player is never knocked down yet: that is step 6.

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
