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
| 3. Collapse | not started |
| 4. Ground fighting | not started |
| 5. Bleed-out | not started |
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
| `item.lastHandler` stays set after a throw | Thrown knives count as player hits | Throw a dagger at a heel |
| A bow-shot arrow resolves to the player as attacker | The "Paris / Jara" shot should count | Shoot an arrow at a heel |

### Engine behaviours found that shape the code

- **Enum defaults**: the mod menu compares an enum default with the boxed int list and never matches, so it falls back to index 0. `AllowedDamageTypes` therefore lists the default (`SlashAndPierce`) first. A unit test pins this.
- **Float defaults**: the default is matched with `object.Equals` on boxed floats. The game's own `start + i * step` builder drifts, so value lists come from `OptionSpecs` (decimal arithmetic). A unit test checks every default is in its list bit-exactly.
- **Option names must be unique per mod** (saved by name). So the toggle is "Achilles Tendon Enabled", not "Enabled"; later features follow the same pattern.
- **Saved by index**: changing a value range later shifts saved settings. Use the mod page's reset button after updating.
- **No nullable metadata**: the game rejects `ModOption`s compiled with nullables; the project disables them.

## Mod Menu

| Category | Option | Default | Range |
|---|---|---|---|
| Achilles Tendon | Achilles Tendon Enabled | on | |
| | Heel Zone Size (radius) | 8 cm | 2-20 cm |
| | Heel Zone Back Offset | 4 cm | 0-15 cm |
| | Heel Zone Height Offset | 3 cm | -5-15 cm |
| | Max Hit Angle (0 = straight behind, 90 = side) | 110° | 30-180° |
| | Allowed Damage Types | Slash and Pierce | Slash and Pierce / Slash only / Pierce only |
| Player Hardcore Mode | Apply To Player | off | |
| Debug | Debug Logging | off | |
| | Show Heel Zones (NPCs; also yours with Apply To Player) | off | |
| | Show Hit Markers | off | |
| | Hit Marker Duration | 5 s | 1-30 s |

Settings from the spec that belong to later steps (`bleedOutDuration`, `groundFightingEnabled`) are added with their features, so the menu has no options that do nothing yet.

## How a heel hit is decided

Pure logic in `src/AchillesTendon/Logic`, unit-tested without the game. Checks in this order; the first failing one is the reason in the log:

1. `NotPlayerVsNpc` - attacker must be the player and the target an NPC; with `Apply To Player` on, an NPC hitting the player counts too. NPC vs NPC, the player hitting themself and hits from an unknown source never count (these hits are not logged)
2. `NotLegPart` - hit part is `LeftLeg/RightLeg/LeftFoot/RightFoot`
3. `NoAnkle` - the foot part on that side exists
4. `DamageTypeNotAllowed` - per "Allowed Damage Types" (blunt never counts)
5. `OutsideHeelZone` - contact point within the zone sphere: center = ankle + back offset (opposite the NPC's horizontal facing) + height offset
6. `AttackAngleUndetermined` / `AttackAngleTooFrontal` - horizontal angle between "straight behind the NPC" and the direction to the attacker ≤ Max Hit Angle

The attack angle uses the attacker's position, not `impactVelocity`, because the velocity's sign convention is not verified (see above).

## Build

Needs the .NET SDK (8.0 used) and the game installed (the build references its DLLs).

```sh
dotnet test                                                  # unit tests (no game needed)
dotnet build -c Release                                      # mod in dist/AchillesTendon
dotnet build src/AchillesTendon -c Release -p:DeployToGame=true   # copy into the game's Mods folder
# other install path:  -p:BasDir="/path/to/Blade & Sorcery"
```

Log: `<prefix>/drive_c/users/steamuser/AppData/LocalLow/Warpfrog/BladeAndSorcery/Player.log` (Proton) - all lines start with `[AchillesTendon]`.
