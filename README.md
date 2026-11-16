# Heel of Achilles

Blade & Sorcery mod: cutting an enemy's Achilles tendon takes them down. They can no longer stand, keep fighting from the ground and slowly bleed out.

Inspired by Achilles and Krishna (felled by the hunter Jara's arrow): the precise expert strike instead of raw force.

Every feature can be toggled and tuned in the in-game Mod Menu.

Full implementation spec: [`CLAUDE.md`](CLAUDE.md).

## Status

| Build step | State |
|---|---|
| 1. Project setup, SDK check, mod loads, debug logging, Mod Menu skeleton | built, **not yet tested in game** |
| 2. Heel hit-zone detection, debug visualization, logging | built, **not yet tested in game** |
| 3. Collapse | not started |
| 4. Ground fighting | not started |
| 5. Bleed-out | not started |
| 6. Player hardcore mode | not started |

Step 2 only detects and reports heel hits. It does not change the fight.

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
| `impactVelocity` direction | Its sign convention is not documented; logged as `impactVelAngle` for comparison only | Compare it with `angleFromBehind` in the log |
| `item.lastHandler` stays set after a throw | Thrown knives count as player hits | Throw a dagger at a heel |

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
| Debug | Debug Logging | off | |
| | Show Heel Zones | off | |
| | Show Hit Markers | off | |
| | Hit Marker Duration | 5 s | 1-30 s |

Settings from the spec that belong to later steps (`bleedOutDuration`, `groundFightingEnabled`, `applyToPlayer`) are added with their features, so the menu has no options that do nothing yet.

## How a heel hit is decided

Pure logic in `src/AchillesTendon/Logic`, unit-tested without the game. Checks in this order; the first failing one is the reason in the log:

1. `NotPlayerVsNpc` - attacker must be the player, target an NPC (these hits are not logged)
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
