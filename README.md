# Heel of Achilles

Blade & Sorcery mod: cutting an enemy's Achilles tendon makes them fall. They can no longer stand, keep fighting from the ground and slowly bleed out.

Inspired by two heroes who fell to a single strike at the one weak point of their body: **Achilles** (Greek) and **Krishna** (Indian). The idea of the mod is the same: the precise expert strike instead of raw force.

Every feature can be toggled and tuned in the in-game Mod Menu.

> **Work in progress (v0.8.4).** Everything below works in automated in-game tests, but has not yet been played in a headset. Expect rough edges in how crawling and ground attacks look.

## What it does

- **The cut:** slash or stab the back of an enemy's heel, from behind or the side. Blunt hits and hits from the front do not count. You can cut with a held weapon, a thrown blade or a spell.
- **The fall:** the enemy drops and never stands up again, in one of three poses, rolled per enemy: most sit down and crawl on their back with the game's own injured and crawl animations, some (30 % of the crawlers by default) fall forward onto their knees and fight on kneeling, and a few (5 % by default) fall flat and lie still.
- **Fighting from the ground:** they scream in pain, and most of them (75 % by default) swing at you with their upper body when you come close. The rest only crawl and scream, so you never know which one still fights back.
- **Bleeding out:** they bleed from the cut heel and die after the Bleed-Out Duration (45 s by default). Over time they crawl slower, scream less often and end up gasping. You can finish them earlier as usual.
- Only your hits count: NPCs cutting each other's heels, or yours, do nothing.

## Install

Requires Blade & Sorcery 1.3.2 (PC). Copy the `AchillesTendon` folder into `Blade & Sorcery/BladeAndSorcery_Data/StreamingAssets/Mods/` and start the game. All options are on the mod's page in the in-game Mod Menu.

After updating, press the mod page's reset button: the game saves options by their position in each slider, so changed ranges can shift saved values.

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
| | Lie Flat Chance (share of crippled enemies that lie flat as a ragdoll instead of crawling; rolled once per enemy at the cut) | 5 % | 0-100 % |
| | Kneel Chance (share of crawling enemies that fall forward onto their knees and fight on kneeling; the others sit down and crawl on their back) | 30 % | 0-100 % |
| | Crawl Speed (movement while crawling, relative to walking) | x0.3 | x0.05-x1 |
| | Crawl Body Height (0 = flat, 1 = standing) | x0.2 | x0-x0.6 |
| | Steady Crawl Pose (twitches count as standing still) | on | on/off |
| | Crawl Start Speed (sitting -> crawling from this speed) | 0.4 | 0.1-1 |
| | Crawl Stop Speed (crawling -> sitting below this speed) | 0.1 | 0.05-0.5 |
| | Leg Strength (how hard the legs follow the animation while crawling) | x4 | x1-x10 |
| Bleed-Out | Bleed-Out Enabled (off pauses the bleeding, on resumes it) | on | |
| | Bleed-Out Duration (cut to death) | 45 s | 10-180 s |
| | Blood Effects (drip from the cut heel, stronger towards the end) | on | |
| | Blood Pulse Interval (between drips and bleed log lines) | 2 s | 0.5-10 s |
| | Weakening (crawls slower, screams less often, heavy breathing near the end) | on | |
| | Weakest Crawl Speed (share of Crawl Speed at the moment of death) | 25 % | 5-100 % |
| | Scream Slowdown (Scream Interval multiplier at the moment of death) | x3 | x1-x5 |
| | Gasping From (bleed-out share from which screams turn into heavy breathing) | 75 % | 50-100 % |
| Ground Fighting | Ground Fighting Enabled | on | |
| | Ground Attacks (crawling enemies swing with the upper body; enemies lying flat never swing). Off: no downed enemy attacks | on | |
| | Ground Attack Chance (share of crippled enemies that fight back; rolled once per enemy at the cut) | 75 % | 0-100 % |
| | Ground Attack Range | 1.8 m | 0.5-4 m |
| | Ground Attack Interval | 3 s | 1-10 s |
| | Screaming (pain cries) | on | |
| | Scream Interval | 6 s | 2-30 s |
| DMCS Compat | DMCS Adrenaline (with DMCS installed: heel cuts and executions fill its Adrenaline meter; without DMCS it does nothing) | on | |
| | Heel Cut Adrenaline (points for each heel cut that cripples an NPC) | 20 | 0-100 |
| | Crippled Kill Adrenaline (points when you kill an NPC that was already crippled) | 35 | 0-100 |
| Debug | Debug Logging (writes heel hits, collapses and bleed-out to the game log) | off | |
| | Show Heel Zones (draws each NPC's heel zone as a sphere) | off | |
| | Show Hit Markers (green = heel hit, red = miss) | off | |
| | Hit Marker Duration | 5 s | 1-30 s |

## DMCS compat

With **DMCS** (Dark Messiah Combat Systems) installed, the mod fills DMCS's Adrenaline meter:

| Event | Points (menu) | Counts when |
|---|---|---|
| Heel cut | Heel Cut Adrenaline, 20 | the cut cripples an enemy (a second cut on the same enemy does not) |
| Execution | Crippled Kill Adrenaline, 35 | you kill an enemy that was already crippled, with anything: weapon, thrown blade, spell, fist or kick |

Bleeding out gives nothing, and neither does a heel cut that kills at once (the cut itself still counts). For scale: in DMCS a hit gives 5 and a full meter is 100. Without DMCS this does nothing, and the two mods can be installed in any order.

## Compatibility

- **Realistic Deaths** (pineapple54) also makes NPCs crawl and bleed. Using both on the same enemies will most likely double the bleeding and make the two mods fight over the body. Not tested.

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

## For developers

How the mod is built, tested and what is verified in the game's code: [`DEVELOPMENT.md`](DEVELOPMENT.md).
