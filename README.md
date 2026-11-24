# Heel of Achilles

A Blade & Sorcery mod about one precise cut. Slice an enemy's Achilles tendon and they go down for good. Some die on the spot. The rest crawl, fight on from the ground and slowly bleed out.

Achilles in Greek myth and Krishna in Indian myth both fell to a single strike at their one weak spot. This mod gives your enemies that weak spot. Skill beats raw force.

> **Work in progress (v0.8.9).** Everything below passes automated in-game tests but has not been played in a headset yet. Crawling and ground attacks may still look rough.

## What it does

- **The cut.** Slash or stab the back of an enemy's heel, from behind or the side. Blunt hits and hits from the front don't count. Held weapons, thrown blades and spells all work.
- **Instant death.** Half of all cuts kill on the spot. The enemy drops with the game's own death animation, and it counts as your kill.
- **Down for good.** The survivors cry out with their death sound as they go down and never stand up again. Each one rolls its own pose: most sit down and crawl on their back, some (30 %) fall to their knees and fight on kneeling, and a few (5 %) fall flat and lie still.
- **Slow to turn.** Crawling and kneeling enemies turn toward you at a quarter of the normal speed, so you can circle them.
- **Still dangerous.** They scream and crawl after you, and most of them (75 %) swing at you when you get close. The rest only crawl and scream. You can't tell which is which.
- **Bleeding out.** Blood drips from the cut heel, heavier toward the end. They crawl slower, scream less, start gasping and die after 45 seconds. You can finish them sooner as usual.
- **Only your cuts count.** Enemies cutting each other's heels, or yours, do nothing.
- **Your rules.** Every feature has a toggle or slider in the Mod Menu, and changes apply live. Turn the mod off and crippled enemies get back up.
- **Practice aids.** Show the heel zones as spheres and mark every leg hit green (heel) or red (miss).
- **DMCS.** With Dark Messiah Combat Systems installed, heel cuts fill its Adrenaline meter. See [DMCS compat](#dmcs-compat).

All numbers are defaults. You can change every one of them in the Mod Menu.

## Install

You need Blade & Sorcery 1.3.2 (PC). Copy the `AchillesTendon` folder into `BladeAndSorcery_Data/StreamingAssets/Mods/` in the game folder and start the game. The options are on the mod's page in the Mod Menu.

After an update, press the reset button on the mod's page. The game saves each option as a position on its slider, so a changed range can shift a saved value.

## Mod Menu

### Achilles Tendon

| Option | Default | Range | What it does |
|---|---|---|---|
| Achilles Tendon Enabled | on | | Turns the mod on or off. |
| Heel Zone Size | 10 cm | 2-20 cm | Radius of the hit zone at the heel. |
| Heel Zone Back Offset | 4 cm | 0-15 cm | How far behind the ankle the zone sits. |
| Heel Zone Height Offset | 3 cm | -5 to 15 cm | How far above the ankle the zone sits. |
| Max Hit Angle | 110° | 30-180° | How far from straight behind you can cut. 0 = only from behind, 90 = side, 180 = anywhere. |
| Allowed Damage Types | Slash and Pierce | Slash and Pierce, Slash only, Pierce only | Which hits cut the tendon. Blunt never does. |
| Stand Up When Disabled | on | | Crippled enemies get back up when you turn the mod off. |
| Instant Death Chance | 50 % | 0-100 % | Share of cuts that kill on the spot. |
| Lie Flat Chance | 5 % | 0-100 % | Share of crippled enemies that lie flat instead of crawling. |
| Kneel Chance | 30 % | 0-100 % | Share of crawlers that kneel and fight on instead. |
| Crawl Speed | x0.3 | x0.05-x1 | Crawling speed compared to walking. |
| Crawl Body Height | x0.2 | x0-x0.6 | How low the crawling body stays. 0 = flat, 1 = standing. |
| Steady Crawl Pose | on | | Small twitches count as standing still, so the enemy doesn't flicker between sitting and crawling. |
| Crawl Start Speed | 0.4 | 0.1-1 | Speed at which a sitting enemy starts to crawl. |
| Crawl Stop Speed | 0.1 | 0.05-0.5 | Speed below which a crawler sits down again. |
| Leg Strength | x4 | x1-x10 | How firmly the legs follow the crawl animation. |
| Crawl Turn Speed | 25 % | 10-100 % | How fast crawlers and kneelers turn toward you. 100 % = the game's normal speed. |

### Bleed-Out

| Option | Default | Range | What it does |
|---|---|---|---|
| Bleed-Out Enabled | on | | Off pauses the bleeding, on resumes it. |
| Bleed-Out Duration | 45 s | 10-180 s | Time from the cut to death. |
| Blood Effects | on | | Blood drips from the heel, heavier toward the end. |
| Blood Pulse Interval | 2 s | 0.5-10 s | Time between drips. |
| Weakening | on | | They crawl slower, scream less and gasp near the end. |
| Weakest Crawl Speed | 25 % | 5-100 % | Crawl speed at the moment of death, as a share of Crawl Speed. |
| Scream Slowdown | x3 | x1-x5 | How much longer the pauses between screams get by the end. |
| Gasping From | 75 % | 50-100 % | Point of the bleed-out where screams turn into gasping. |

### Ground Fighting

| Option | Default | Range | What it does |
|---|---|---|---|
| Ground Fighting Enabled | on | | Crippled enemies scream and fight from the ground. |
| Ground Attacks | on | | Crawlers and kneelers swing at you. Enemies lying flat never do. |
| Ground Attack Chance | 75 % | 0-100 % | Share of crippled enemies that fight back. |
| Ground Attack Range | 1.8 m | 0.5-4 m | How close you must be before they swing. |
| Ground Attack Interval | 3 s | 1-10 s | Time between swings. |
| Screaming | on | | Pain cries. |
| Scream Interval | 6 s | 2-30 s | Time between screams. |

### DMCS Compat

| Option | Default | Range | What it does |
|---|---|---|---|
| DMCS Adrenaline | on | | Fills DMCS's Adrenaline meter. Does nothing without DMCS. |
| Heel Cut Adrenaline | 20 | 0-100 | Points per heel cut. |
| Crippled Kill Adrenaline | 35 | 0-100 | Points for killing an enemy that was already crippled. |

### Debug

| Option | Default | Range | What it does |
|---|---|---|---|
| Debug Logging | off | | Writes heel hits, falls and bleeding to the game log. |
| Show Heel Zones | off | | Draws each enemy's heel zone as a sphere. |
| Show Hit Markers | off | | Marks each leg hit: green = heel, red = miss. |
| Hit Marker Duration | 5 s | 1-30 s | How long a marker stays. |

## DMCS compat

With **DMCS** (Dark Messiah Combat Systems) installed, this mod fills DMCS's Adrenaline meter:

- **Heel cut:** 20 points, also when the cut kills at once. A second cut on the same enemy gives nothing.
- **Killing a crippled enemy:** 35 points, with any hit: weapon, thrown blade, spell, fist or kick.

Bleeding out gives nothing. For scale: in DMCS a normal hit gives 5 and a full meter is 100. If you switched on DMCS's *Adrenaline Needs Skill*, the meter only fills once you own that skill. Without DMCS nothing happens, and load order doesn't matter.

## Compatibility

- **Realistic Deaths** (pineapple54) also makes enemies crawl and bleed. Both on the same enemy will most likely double the bleeding and fight over the body. Not tested.

## Background: two myths, one weak spot

### Achilles and the heel

- His mother, the sea goddess Thetis, dipped baby Achilles in the river Styx to make him invulnerable. She held him by the heel, so that spot stayed mortal.
- He became the greatest fighter of the Trojan War. The Trojan prince Paris killed him with an arrow to that heel, guided by the god Apollo.
- The tendon above the heel carries his name, and "Achilles' heel" now means a fatal weak spot in someone strong.
- Homer's *Iliad* tells neither his death nor the heel. The Roman poet Statius is the first to tell the dip in the Styx outright (*Achilleid* 1.133-134), and the oldest sources name the ankle (Latin *talus*), not the heel.

### Krishna and the sole of the foot

- In the Indian epic *Mahabharata*, the sage Durvasa granted Krishna a boon: his whole body would be invulnerable except his feet (*Anushasana Parva*, 13.159).
- Late in life, after his own clan had wiped itself out (*Mausala Parva*, Book 16), Krishna sat meditating in the forest.
- A hunter named Jara (Sanskrit for "old age") saw the red marks of Krishna's foot through the bushes, took it for a deer and shot. The arrowhead was made from the iron of the cursed club that had destroyed his clan. It pierced the sole of his foot.
- Jara begged forgiveness. Krishna comforted him and left the world. (The *Bhagavata Purana* 11.30 tells the same death.)

Sources: [Achilles' heel (Wikipedia)](https://en.wikipedia.org/wiki/Achilles%27_heel), [Statius, *Achilleid* 1 (Theoi)](https://www.theoi.com/Text/StatiusAchilleid1A.html), [Ancient World Magazine: Origins of a hero's weak point](https://www.ancientworldmagazine.com/articles/achilles-heel-origins-heros-weak-point/), [Mausala Parva (Wikipedia)](https://en.wikipedia.org/wiki/Mausala_Parva), [W. Doniger, *Mahabharata* Book 16 (Oxford, 2022)](https://doi.org/10.1093/oso/9780197553398.003.0003), [Jara (mahapurana.com)](https://www.mahapurana.com/hindu-mythology/jara-jaras-hunter-killed-krishna/).

## For developers

How the mod is built and tested, and what is checked against the game's code: [`DEVELOPMENT.md`](DEVELOPMENT.md).
