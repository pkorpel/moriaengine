# Living cities and dynamic quests: a design

A proposal for review (#1237). Nothing here is built. The plan comes from your answers to the questions at the end.

The goal, in your words: cities that feel alive instead of empty, many NPCs offering small quests again and again that are always a little different, and a modern take on quests inspired by other games and by Tolkien's world. The Erebor quests you picked (#1235: the special item rewards and the realm envoy q30) fit in as story quests; section 10 says where.

**Your answers so far (2026-11-07):**
- **Pilot cities:** Minas Tirith, Edoras and Hobitin for Gondor; Barad-dur, Harad and Umbar for Mordor (all three carry Mordor's realm tag).
- **Dialog text:** Czech, without diacritics.
- **Rewards:** scaled by difficulty, never over the line, no cosmetics. Mostly experience, with something alongside: magic items, boosters, money, a mount and the like.
- **Reputation:** I propose the system in section 15.
- **Ports:** all of them, including q018, q041 and q099.
- **Ideas:** all kept.
- **Still open:** how `.venue` works (section 16) and Mordor's flavour (section 17).

## 1. What success looks like

- A player walking through Minas Tirith or Barad-dur at any hour sees people doing things: the smith at the anvil, patrons in the tavern, an actor rehearsing, a guard changing shift, a crier shouting news.
- Any resident can be talked to, and many of them have something small to ask today. Tomorrow it's something else.
- Small tasks connect the city: a message leads to a conversation, a conversation to a rumour, a rumour to a real quest outside the walls.
- Residents remember you. Help them and they greet you by name, give you a better price, tell you secrets. Wrong them and they won't.
- Nothing here can be farmed by a bot: limits per day, text that varies, and tasks that need you to read.

## 2. What other games teach us

| Game | What it does | What we take |
|---|---|---|
| The Lord of the Rings Online | Shire postal service with timed deliveries, pie and mushroom errands, deeds that give titles for exploring, reading lore and slaying; festivals; inns and the Ale Association | timed courier rounds, deeds and titles, festivals, a Tolkien tone that is gentle in the Shire and grim in Mordor |
| Skyrim (radiant quests) | templates filled with a giver, a place and a target from the world; innkeepers' rumours; jobs from residents | templates with slots, rumours in taverns as the entrance to bigger quests |
| Guild Wars 2 | dynamic events that start on their own in an area, scale with the players present, chain into the next event, and change the area (a burnt village, a cleared camp) | world events with an outcome that stays for a while |
| Stardew Valley | every resident has a daily schedule, a friendship level, gift preferences, a help-wanted board, festivals | residents with schedules and friendship; a city that changes by hour |
| The Witcher 3 | notice boards; contracts with an investigation (clues, the right preparation, then the fight); side stories with real choices | contracts and clues; choices whose outcome is remembered |
| Middle-earth: Shadow of Mordor | orc captains with names who remember fighting you, get promoted, rival each other | a "nemesis lite" for Mordor's orc captains and for named bounties |
| Kingdom Come: Deliverance | reputation per town, NPCs who sleep at night, crime that is remembered | reputation per city; night is different from day |
| World of Warcraft | daily quests with a cap, reputation with factions, rested experience after a night at an inn | daily caps, factions, a rested bonus from inns |
| Red Dead Redemption 2 | small encounters on the road (someone asks for help, an ambush) | road encounters between cities |
| Ultima Online itself | escort NPCs who want to go to a town, town criers, Seer-run events | escort, the crier, and GM tools to run events by hand |

What we leave out: quest markers over every head, map pins (3.0.6m can't show them), voiced cutscenes, anything that needs a modern client.

## 3. What we have to build on, and the limits

**Already in the engine:**
- A day/night cycle (24 real minutes is one game day, 12 day and 12 night) and seasons (`LightAndWeather`).
- NPC AI with a home, a leash and a walk, pathfinding, and NPC sleep where no player is near (#1201).
- The quest journal `.questy`, quest dialogs with text markup, the quest arrow (0xBA, used by Tracking today), quest items as player tags.
- `.SetupWorld` for anything that must exist in the world, with permanent ids.
- Spawns, vendors and shops, town stones and the realm government, boosters, magic item bags, weekly quests, chat channels and server announcements, RP rank.
- Name lists from Erebor (`spherename.scp`, used by the thief's mask), 167 books of lore (`knihy.scp`).

**Limits:**
- Most players use the 3.0.6m client: gumps and speech work, AOS tooltips don't, and nothing is drawn above NPCs' heads but speech. So who has a task must show in what they say and in the journal, not as a marker.
- The map data doesn't say which building is a tavern or a bath house. Erebor's shop signs were GM-placed items, and the archive has no positions. **Venues must be marked by a GM** (section 11), or proposed by me and reviewed.
- Czech text in game, without diacritics (your rule). This page is in English; the dialog text will be Czech.
- A peak of about 50 players, so world events must work for 3 players and for 30.

## 4. The living city

### 4.1 Venues

A venue is a building or place with a type, a name, an area, opening hours and roles to fill. A GM marks it once; residents and errands then use it. Types, with the roles each wants:

| Venue | Roles | Life in it |
|---|---|---|
| Tavern / inn (hospoda, hostinec) | innkeeper, server, cook, bard, patrons (evening), a drunk, a stranger with a rumour | busiest in the evening; rumours, dice, songs, rooms to rent |
| Bath house (lazne) | attendant, stoker, bathers | morning and evening; gossip in the steam |
| Theatre (divadlo) | director, actors, prompter, audience | rehearsal by day, a performance at dusk |
| Armoury (zbrojnice) | quartermaster, armourer, guards collecting gear | shift changes at dawn and dusk |
| Smithy (kovarna) | smith, apprentice | anvil sounds all day, closed at night |
| Temple / shrine (chram, svatyne) | priest, acolyte, pilgrims, mourners | prayers at dawn, vigil at night |
| Houses of Healing (lecebna) | healer, herbalist, patients | busy after battles and events |
| Library (knihovna) | librarian, scholar, students | quiet by day, closed at night |
| Harbour (pristav) | harbourmaster, dockers, fishers, sailors | unloading by day, sailors in the tavern at night |
| Stable (staje) | stablemaster, groom | horses fed at dawn and dusk |
| Market (trh) | merchants with stalls, criers, pickpocket | full by day, market day once a week |
| Bakery, kitchen | baker | bread before dawn, a round of deliveries |
| Brewery, distillery (palirna) | brewer, labourers | barrels; Erebor's q025 Palirna lives here |
| Barracks, gate | captain, guards, recruits | drills, shifts, sentries at night |
| Prison | jailer, prisoners | quiet; plots overheard |
| Bank, town hall | banker, clerk, mayor's men | office hours |
| Mill, farm (outside the walls) | miller, farmers | work by day, wolves at night |
| Mine (dwarven cities) | foreman, miners | shifts, never closed |

Mordor gets its own set with the same machinery: the war camp's mess hall instead of a tavern, the pit fights instead of a theatre, the slave pits of Nurn, the forge-halls of Barad-dur, the warg kennels, the overseer's post. Hobbits get more taverns and gardens and fewer barracks; elves get feast glades and a library-like hall of songs; dwarves get mines and forges.

### 4.2 Residents

A resident is a named NPC who belongs to the city:
- **Identity:** a name from the race's name list, a role at a venue, a home (a house in the city), a few traits (talkative, grumpy, pious, greedy, romantic, nosy...), likes and dislikes (gifts, topics).
- **Relations:** a few links to other residents: family, friend, rival, debtor, sweetheart. Errands use them ("take this to my brother", "don't tell my rival").
- **Schedule:** home at night, the venue by day, the tavern or the temple in the evening, by role and trait.
- **Memory of players:** friendship per player (0 to 10, as Stardew's hearts), the last thing you did for them, and whether you lied to them or stole from them.
- **Today's needs:** each game day a resident may have one or two needs drawn from templates that fit the role and traits. A need becomes an errand when a player asks.

Residents are permanent world pieces (`.SetupWorld`), so they keep their names and memory across restarts. Ambient NPCs (passers-by, patrons) can be plain spawns without memory.

### 4.3 Schedules on the game clock

The game day is 24 real minutes, so a resident's day is short: a player who stays an hour sees it go round more than twice. Schedules are blocks: night (home, asleep, door locked), morning, day (work), evening (tavern, temple, walk), with variety by trait. Opening hours of venues follow the same blocks. A shopkeeper who isn't at work says so ("Prijd zitra rano, ted mam zavreno.") or is found at home.

### 4.4 Ambient life

- **Barks:** short lines when a player passes, by role, time and weather ("Cerstvy chleba!", "Zase prsi, a ja mam suseni pradla.").
- **Conversations between residents:** two residents standing together trade three or four lines from a pool, about the city's news, a rival, the weather, a player's deed. They are a natural source of rumours.
- **Performances:** the theatre plays at dusk; actors speak their scenes overhead, the audience claps. Bards sing in taverns. A player bard can perform for tips (Musicianship).
- **The town crier:** announces world events, festivals, the market day, and players' deeds ("Hrdina X zabil trolla u brodu!"). Uses the chat system's announcements.
- **Small sights:** the lamplighter at dusk, the baker's round at dawn, guards changing shifts, a cat on the market, children playing.
- **The city's state:** after a world event the city shows it for a while: a burnt house with workers rebuilding, extra guards, high prices, a festival's decorations.

## 5. Dynamic errands

### 5.1 The building blocks

An errand is a template of steps with slots. The generator fills the slots from the living city: a giver, a receiver, an item, a venue, a time.

Step kinds:
- **talk** to someone (a resident by role, relation or name);
- **deliver** an item to someone, or a message (a tag, not an item, as our quest items are);
- **fetch or buy:** bring N of something, from a shop, from gathering, from a craftsman;
- **craft** something (quality can matter);
- **find:** a person, a pet or an item somewhere in the city, with clues;
- **escort** someone to a venue or a gate;
- **use** an object (light a lamp, ring a bell, fill a bucket at a well);
- **kill near** (rats in a cellar, wolves at the farm, a named bandit outside);
- **choose:** two or three answers that change the outcome and who likes you;
- **skill check** (Musicianship, Cooking, Hiding, Snooping, Taming, Tracking...);
- **wait** until a time of day ("come back at dusk, he's at the tavern then").

### 5.2 Chains and escalation

- A step's result can start the next errand: the reply to a letter, a clue that points to another resident, a debtor who asks for a favour in return.
- Now and then (1 in 10 or so) a small errand escalates: the letter was a smuggler's code, the lost ring was stolen, the quarrel hides a crime. That opens a hand-made mini story (section 9).
- Rumours from taverns and residents' talk lead to world events and story quests outside the city.

### 5.3 Who offers errands

- **Residents** with today's needs: you ask "Potrebujes neco?" in the dialog, or they call out when they like you.
- **Venue owners** with work shifts: paid work at the tavern, the bath house, the docks.
- **Notice boards** (as Erebor's hunting board, `i_sign_gquest`): bounties and contracts for the region, a few new each day.
- **The crier** for city-wide calls (a fire, a festival, a caravan needing escort).

### 5.4 Variety

- Text pools per template, per role and per trait, with the player's name, the city, the time of day and the realm worked in.
- Twists drawn now and then: the receiver isn't where expected (follow the schedule hints), the item is the wrong one, someone offers to buy what you carry, the giver lied.
- A resident's memory changes the lines: a friend greets you by name, someone you wronged refuses.

### 5.5 Limits

- A cap on errands per player per game day per city, and per resident (one or two).
- Diminishing pay for the same template in a row.
- Errands need reading: the receiver's name, a clue, a choice. A step can't be completed by spamming a word.
- The AFK check (#1107) applies as everywhere.

### 5.6 Rewards

Small and many, with the meaningful part in relationships:
- gold and experience scaled by the errand's length and the player's level;
- friendship with the resident and reputation with the city or faction;
- **favours** for friends: discounts at their shop, a free night at the inn, a repair, a secret, an introduction to someone who gives a real quest;
- by difficulty (your call: no cosmetics, nothing over the line):
  - **small** errands give exp and a little money;
  - **medium** add a potion, reagents or a chance of a magic pouch (`i_mi_pouch`);
  - **large** ones and chains add a magic bag or a booster;
  - **story quests** give the big things: magic items, mounts, skill gains.
  The reputation levels in section 15 open better errands, and so better rewards.

### 5.7 Reputation and factions

The full proposal is section 15. In short, reputation is per city, and with factions inside it:
- **Gondor:** the merchants, the city guard, the temple, the theatre company, the Rangers;
- **Mordor:** the orc captains (rivals: serving one angers the other), the overseers, the Mouth of Sauron's spies;
- **neutral:** the dwarves, the elves of the woodland realm, the hobbits, the corsairs, the thieves' den (hidden, found through rumours).

Levels open services, quests and titles. Crimes in a city (the existing notoriety) lower it: residents grow wary, guards ask questions.

## 6. World events

Dynamic events that start on their own in a region, scale with the players present, and leave an outcome for a while:

| Event | What happens | Outcome |
|---|---|---|
| Orc raid on a village | warband attacks at dusk; defend the farms | won: a feast and a reward board; lost: burnt farms, rebuild errands, no bread for a day |
| Caravan on the road | merchants travel between two cities and want an escort; ambush on the way | the caravan's goods appear at the destination market cheaper |
| The beacons of Gondor | a chain of beacons on hilltops must be lit in order before time runs out | realm-wide experience bonus for an hour |
| Fire in the city | a house burns; carry water from the wells, pull people out | saved: the family's friendship; lost: a ruin to rebuild |
| Fever in the city | residents fall ill; healers need herbs (kingsfoil), bandages, care | cured residents; the Houses of Healing remember you |
| Wolf winter | in winter, wolves enter the city at night | fur traders' prices change |
| Troll at the ford | a troll blocks a crossing; keep it busy until sunrise and it turns to stone | a stone troll stays as a landmark for a while |
| Corsairs at the harbour | ships land raiders at Pelargir or Dol Amroth | the harbour closes or stays open |
| Black Rider on the road | at night a rider hunts along a road; hide (Hiding) or flee | a rumour quest |
| Prison break | prisoners escape into the city | bounties |
| Market fair | travelling merchants with rare goods for a day | |
| Festivals by season | Midsummer in Gondor, Yule, harvest, a hobbit party, Mordor's day of the Eye | festival games, food, cosmetics |

Mordor gets its own: a slave revolt in Nurn, a Gondor spy in the camp, a rival orc captain's challenge, the Eye's attention on a region.

## 7. Story quests

Hand-made quests stay the backbone for big rewards. The dynamic layer feeds them: a rumour, a resident's friendship or an escalated errand opens a story quest; its NPCs live in the venues; its stages show in the journal. The Erebor ports (section 10) and new stories (section 9) are this kind.

## 8. In `.questy`

The journal gets new tabs beside Questy, Boosters, Weekly and PvP:
- **Ukoly (errands):** active errands (a few at a time), each with the giver, the next step, where to find the next person ("v hospode U Zlateho bazanta, vecer"), the time left; buttons to show the arrow and to give up.
- **Pribehy (stories):** story quests with their stages, as the Erebor quests had.
- **Mesto (city):** reputation per city and faction, friends among the residents with their friendship, the city tokens.
- **Udalosti (events):** world events running now and where.
- **Skutky (deeds):** deeds and titles.

The quest arrow (0xBA) points to the next person or place on demand. It works on 3.0.6m clients; Tracking already uses it.

## 9. Quest ideas

A long list to choose from. "Dynamic" means a template the generator fills; the rest are hand-made.

### 9.1 Dynamic errand templates

1. **The letter:** take a sealed letter to someone; they write back; sometimes back and forth twice. Twist: they've moved, the neighbours know where.
2. **Ask around:** someone wants news of a person or thing; three or four residents point onward ("videl jsem ho jit k lazni"); the last one knows.
3. **Lost and found:** something lost in the city lies in a venue; return it, or sell it to the shady buyer who asks about it.
4. **The runaway:** a cat, a dog, a goat or a child hides somewhere in the city and moves when found once.
5. **Market run:** buy N of something at the market with the money given; haggle and keep the change.
6. **Ingredients:** herbs, fish, ore or wood for someone (gathering skills).
7. **A commission:** craft an item for a resident; quality changes the pay and the friendship.
8. **The debt:** collect money from a debtor who pleads; pay it yourself, threaten (karma), or forgive (the debtor becomes a friend).
9. **A slow walk:** escort an old resident to the temple or the market; a pickpocket or a stray dog on the way.
10. **The courier race:** timed delivery to another city, by road or travel stone, more for speed (LOTRO's postal service).
11. **The quarrel:** two neighbours argue; hear both and decide; each side's friendship changes.
12. **The secret admirer:** carry an anonymous gift; the receiver asks who sent it; tell or not.
13. **The lamplighter:** light N lamps along a street before full dark.
14. **The cellar:** rats or spiders in a venue's cellar.
15. **The night watch:** walk the guard's round of checkpoints at night; a thief turns up on one.
16. **Stop, thief:** a pickpocket robs a resident in front of you; chase him through the streets.
17. **The invitation:** invite four residents to a feast; each answers with a condition ("prijdu, kdyz prineses vino").
18. **The sick neighbour:** fetch the healer or the right herb before time runs out.
19. **A shift at work:** carry mugs at the tavern, fill buckets at the bath house, haul crates at the docks, polish blades at the armoury; wages and the owner's friendship.
20. **The love letter:** deliver it, read it (the seal shows), or give it to the jealous rival who pays.
21. **Return the books:** the library's overdue books are with residents, who each have an excuse.
22. **The omen:** an old woman wants three odd things for a charm; she gives a small blessing for a while.
23. **Words for the dead:** at night a ghost asks for a message to a living relative (Spirit Speak).
24. **The brawl:** stop a tavern fight with bare hands, or bet on it.
25. **Ale for the guards:** a barrel from the brewery to the gate before the shift changes.
26. **Census:** the town hall counts the households; visit them (ties to the town stone and citizenship).
27. **The stowaway:** find who hides on a ship at the harbour; hand them over or help them off.
28. **The missing shipment:** goods didn't arrive; follow the trail out of the city to an overturned cart.
29. **Press gang** (Mordor): drag drunk orcs from the mess hall to the barracks before the captain notices.
30. **Contraband** (Mordor, Umbar): carry goods past an inspection (Hiding, Snooping).

### 9.2 Venue life

- **Tavern:** rumours from the innkeeper and the stranger in the corner; dice and arm wrestling; a drinking contest (stamina); a bard's evening; a room for the night gives rested experience (a booster) the next session.
- **Bath house:** wood and water for the stoker; bathing gives "fresh" for a while (better prices, friendlier residents) shown in `.boosts`; a ring lost in the pool; what people say in the steam.
- **Theatre:** a play at dusk with lines spoken by actors; be an extra (Musicianship), find the missing actor before curtain, fetch a forgotten prop, prompt a line (pick the right line from three). Mordor's counterpart: the fighting pits.
- **Armoury:** the quartermaster's requisition (crafted weapons and armour), a count of the stock that doesn't add up (a theft), lend gear to the militia during an event.
- **Smithy:** work the bellows, bring ore, take a repaired blade to a guard captain.
- **Temple:** offerings, a night vigil, pilgrims who need guidance, a restless ghost.
- **Houses of Healing:** bandage the wounded after an event, gather kingsfoil, sit with a fevered patient.
- **Library:** find a misplaced book, copy a text (Inscription), a scholar's trip to ruins, lore deeds from the 167 books.
- **Harbour:** unload a ship, fishing orders, a stowaway, a smuggler's code in the cargo list.
- **Stable:** groom, feed, catch a runaway horse, a horse race (Erebor's black mustang fits here).
- **Market:** tend a stall for an hour, find the counterfeit coins, market day with a travelling merchant.
- **Bakery:** the dawn round to five houses before the bread cools.
- **Barracks:** target practice, sentry relief at night, a recruit's first patrol.
- **Prison:** bring a prisoner's food, overhear a plot, a jailbreak.

### 9.3 Story quests by region

- **The Shire (Hobitin):**
  - the hobbit post with ranks, from Postman to Master of the Post;
  - mushroom thieves in a farmer's field (report them, or share the mushrooms);
  - a long-planned birthday party that needs fireworks, food and guests, ending in a party all players can join;
  - relatives who covet the silver spoons.
- **Fornost and the ruins of Arnor:** a barrow mound that wakes at night, with an old blade to return; Rangers' watch duty, reading tracks (Tracking).
- **Edoras:**
  - the muster of an eored, riding with the riders (mounted);
  - horse thieves from the hills;
  - a song for the hall (Musicianship).
- **Minas Tirith:**
  - lighting the beacons (the world event's story);
  - the Houses of Healing and the herb no one remembers;
  - a guard of the Citadel who lost his helm the night before inspection;
  - a mystery in the Steward's archives.
- **Dol Amroth, Pelargir and Linhir:** the swan knights' tourney; corsair spies on the docks; the lighthouse keeper who fell ill.
- **The Grey Havens (Sede pristavy):** letters of departing elves to deliver before the ship sails.
- **The woodland realm:** a feast whose lights vanish when you come near; spider nests; the barrel escape down the river (a timed run); wine from the east for the king (a trade chain across realms).
- **Erebor and the Iron Hills:**
  - the ravens of Erebor, birds that carry messages (a messenger service);
  - an ancestral axe and an old grudge;
  - a deep mining expedition (Mining);
  - a con man selling "the Arkenstone".
- **Esgaroth:** the fish market's quotas; a bargeman who smuggles.
- **Umbar and Harad:** preparing a corsair raid; a mumak handler; a spice caravan through the desert.
- **Rhun:** horse trading with the Easterlings; wine for the woodland realm.
- **The wild:**
  - a cave creature's riddles (answer by typing; a wrong answer and it attacks);
  - trolls at dusk to keep busy until dawn;
  - a ferry crossing chased by a Black Rider;
  - a singing hermit in an old forest who finds lost travellers.

### 9.4 Mordor

- **Rival captains:** two orc captains compete for your service. Pick one and the other sends cutthroats. Captains rise and fall over the weeks (nemesis lite).
- **The pits of Nurn:** meet the overseer's quota, or quietly let a slave escape (karma, and a secret ending).
- **The Eye's spies:** find the Gondor spy among the camp's residents through talk and clues.
- **A warg pup:** raise one over days to a mount.
- **Black Speech:** learn phrases from an old orc for a title.
- **The Mouth's dispatches:** carry orders to the Nazgul's towers on time.
- **The fighting pits:** fights for coin and standing.

### 9.5 By profession

- **Warrior:** the sparring master's ladder; the arena.
- **Ranger:** hunt contracts with tracks to follow; the Rangers' watch.
- **Mage:** strange lights at old stones; a rogue apprentice.
- **Necromancer:** graveyards that won't rest; a bargain with a wraith.
- **Priest:** a pilgrimage between temples; blessing the sick in a fever event.
- **Thief:** the thieves' den, found through rumours; a heist in three parts.
- **Craftsman:** master commissions that lead to the robe, ring and hammer quests (section 10).
- **Mystic:** visions in dreams that point to places.
- **Shaman:** a spirit animal to find; a voodoo request from the wrong person.

### 9.6 Deeds and titles

LOTRO-style deeds that count what players do anyway, for titles and small rewards: explorer (landmarks of a region), slayer (kinds of monsters), lore (read the library's books), courier (deliveries), friend of the city (reputation), festival-goer.

### 9.7 Games

Tavern dice, arm wrestling (Str), drinking contest, archery contest (Archery against targets), a horse race with checkpoints, a fishing contest (the biggest fish), a cooking contest (quality), riddles, hide and seek with the children.

## 10. Where the Erebor ports fit

The picks from #1235 become story quests placed in venues, fully in the journal's Pribehy tab with Erebor's stage texts:

| Quest | Venue | Reward |
|---|---|---|
| q006 Eriol's grandson | Houses of Healing | Robe of the Alchemist, +10 Alchemy |
| q007 Haradir's piglet, then q007b and q007c | a hunter's lodge, the library, the smithy | Inscription book or magic pen; adamantine hammer, +10 Blacksmithing |
| q009 the master tailor | a tailor's shop | Tailor's Ring, +10 Tailoring |
| q017 the alchemist Terlak | the tavern (the innkeeper tells it) | a magic item for your profession |
| q026 poison from the tombs | the temple and the Houses of Healing | immunity to the tomb curse |
| q008 / tameblack | the stable | the black mustang |
| q31 the cook | the tavern's kitchen | a llama |
| q30 the realm's envoy (both realms) | the town hall, the realm office | the warrior's cloak |
| q018 the golden salmon | the harbour (the fisherman) | +3.0 Tailoring |
| q041 the smuggler | a back room of the tavern, then the realm-change gate | a change of realm |
| q099 the outcast | outside the walls, by the outcasts' camp | an outcast's realm restored; Erebor never set what starts it (`tag.vykoupeni`), so the start is designed new, e.g. a reputation level or an act of atonement |

They need the corpse-collecting bag (`t_collector`, about 20 quests use it), spots for their NPCs, and their bugs fixed (listed in the catalogue).

## 11. Tools for GMs

- `.venue` and `.venues`: marking venues, described in section 16.
- **Population from venue types:** once a venue is marked, `.SetupWorld` creates its residents from the type's roles (names, homes, schedules), add-only as everything there.
- `.resident`: inspect or edit a resident (traits, relations, schedule, friendships).
- **Templates as data:** errand templates and text pools in `.def` files, so new errands are written without C#.
- `.errandtest`: run a template step by step on yourself.
- **A log of errands taken, finished and abandoned,** to tune pay and caps.
- **Event controls:** start, stop or schedule a world event by hand (a Seer's tool).

## 12. Performance

The cost is the number of NPCs. Roughly 18 cities with 8-15 venues and 2-5 people each is 500 to 1,000 residents, plus ambient spawns. That is far under the 30,000 NPCs the performance plan measured, and NPC sleep (#1201) keeps a city without players asleep. Schedules don't run while a city sleeps: when it wakes, each resident is put where its schedule says it is now, with no walking. Errand state lives on the player (tags), so a resident's respawn or a restart doesn't break an errand. Barks and conversations are throttled per area.

## 13. A possible order

1. **Foundations:** venues and `.venue`; residents with names, homes, schedules and memory; the journal's new tabs; the `t_collector` bag. Pilot two cities, one per realm.
2. **The errand engine:** templates, chains, limits, friendship and reputation, with the first eight templates (letter, ask around, market run, ingredients, lost and found, debt, slow walk, a shift at work).
3. **Venue life:** tavern, bath house, theatre, armoury, smithy, temple, Houses of Healing, harbour, stable, market. Barks, conversations, the crier.
4. **The Erebor ports** (section 10) as story quests in their venues.
5. **World events and festivals.**
6. **The rest:** more templates, regional stories, Mordor's captains, deeds, games. Roll out to the other cities.

Each step its own issue and PR, as usual.

## 14. Questions for you (answered 2026-11-07, see the top; 2 and 7 are explained in sections 16 and 17)

1. **Pilot cities:** Minas Tirith and Barad-dur, or another pair (Hobitin is the cosiest to start with)?
2. **Marking venues:** will you or your GMs walk the cities with `.venue`, or should I propose venues for each city from the map (buildings near the city's spots) for you to review in game?
3. **Language:** Czech dialog text without diacritics, as the rest of the game?
4. **Rewards:** how generous should errands be? Should they give city tokens, and should tokens buy boosters and cosmetics? A rare magic pouch?
5. **Reputation:** should a bad reputation have teeth (residents refuse, guards suspicious, prices up)?
6. **Your list of ports:** I read "special item reward" as q006, q007 (a, b, c), q009, q017, q026, q008/tameblack, q31, plus q30. Should q018 (+3 Tailoring, a skill rather than an item), q041 (realm change) or q099 (outcast redemption) be in it?
7. **Mordor:** the same living-city machinery with a grim flavour, or less (camps are rougher, with fewer errands and more orders)?
8. **What to cut:** anything in section 9 you don't want at all?

## 15. Reputation: the proposal

### 15.1 What is tracked

- **City reputation**, per city, from -1000 to +1000. It starts at 0 in your own realm's cities. In the other realm's cities it starts at -600, where guards already attack you as they do today.
- **Faction reputation**, a few factions per city, on the same scale. Factions can be rivals, so helping one costs standing with the other.
- **Friendship** with single residents, 0 to 10 (section 4.2). It is personal, and apart from the city's view of you.

Factions in the pilot cities, a proposal:

| City | Factions |
|---|---|
| Minas Tirith | the Citadel guard, the merchants of the lower circles, the Houses of Healing, the archives (scholars) |
| Edoras | the king's household, the riders (eored), the horse breeders |
| Hobitin | the post, the farmers, the inn-keepers (with their own little rivalries) |
| Barad-dur | two rival orc captains, the overseers, the Mouth of Sauron's spies |
| Harad | the caravan masters, the mumak handlers, the temple of the serpent |
| Umbar | the corsair captains, the slave traders and fences, the harbour lords |

### 15.2 Levels

| Level | From | What it means |
|---|---|---|
| Nepritel (enemy) | -1000 | guards attack on sight; a bounty may be posted on you; no trade |
| Podezrely (suspect) | -300 | residents refuse errands; vendors won't sell, or only at +50%; a guard follows you and asks what you're doing |
| Neznamy (stranger) | -100 | small errands only; vendors +10% |
| Znamy (known) | 0 | ordinary errands and prices |
| Vazeny (respected) | 200 | medium errands; 5% off; a free night at the inn now and then |
| Cteny (honoured) | 450 | large errands and chains; 10% off; back rooms and faction vendors open (boosters, mounts, magic items for gold) |
| Hrdina mesta (city hero) | 750 | the city's story quests; 15% off; the title "Hrdina Minas Tirith"; the crier tells your deeds; guards salute you |

The numbers are proposals and would live in `.settings`.

### 15.3 Gains and losses

**Gains:**
- errands, by difficulty;
- world events in and around the city;
- story quests, which give the most;
- a choice in an errand, which raises one faction (and can lower its rival).

**Losses:**
- **crimes seen in the city:** criminal status, theft or snooping caught, the Gondor looting crime (#918), murder. Murder costs the most;
- harming a resident;
- failing or abandoning an errand (a little);
- choices that side against a faction.

**Recovery:**
- negative reputation drifts back towards 0 over real days;
- an **atonement** errand speeds it up: penance at the temple in Gondor, a tribute to the captain in Mordor;
- positive reputation doesn't decay, but the top level wants you to have done something there recently, or it rests one level lower.

### 15.4 How it ties in

- **Errands:** a resident only offers errands your level allows.
- **Vendors:** prices and refusals go through one place, as payments go through `GoldUtils.Pay` (#956).
- **Guards:** the existing realm rules stay. Reputation only adds the suspect's follower and the enemy's bounty for players of your own realm who made trouble.
- **The town stone:** citizenship (#892) could ask for Vazeny or higher.
- **Where you see it:** the journal's Mesto tab shows each city and faction, your level and the next one; `.whoami` shows your best title.

## 16. How `.venue` would work

A venue is a small region inside the city's region, as houses are. GM-made regions are already saved with the world. As a region, a venue gets entry events and an answer to "who is in the tavern" for free.

**Marking a building:**
1. Stand inside the building and type `.venue`.
2. The server finds the room: it spreads from your tile over walkable floor under a roof until it meets walls and doors (capped at about 40 by 40 tiles). It shows the outline with short-lived markers along the edge.
3. A dialog in Czech asks for:
   - the **type** (tavern, bath house, theatre...);
   - a **name** ("U Zlateho bazanta");
   - the **opening hours**, filled from the type and changeable;
   - the **city**, found by itself.
   The dialog also has buttons to fix the outline with two cursor corners, or to add a second rectangle for an L-shaped house.
4. For open-air venues (the market, the harbour, a training yard) there's no roof to find: you set the corners with the cursor.
5. Confirm. The venue is made, with its entrance (the nearest door) and its list of roles to fill. A sign for the type can go up over the door if you want one.

**Managing venues:**
- `.venues` lists the venues of the city you stand in, or all of them. Each row shows the type, the name and how many roles are filled, with buttons to go there, edit, remove, and fill its roles.
- `.venues navrh` (proposals) scans a city for roofed, closed buildings and lists the ones not yet marked. You go through them one by one: Go, look, pick a type or skip. For six pilot cities this is an evening's work, not a week's.
- **Filling roles:** each role becomes a resident with a name, traits and a schedule.
- **Homes:** a resident needs one. Unrented houses would serve; failing that, a back room of the venue.

**Keeping it across a rebuild:**
- You rebuild the world from an empty save, so venues must survive that.
- `.venues export` writes every venue (area, type, name, hours, roles, residents' names) into a setup file in `scripts/Setup`.
- `.SetupWorld` then recreates them on a fresh world, add-only and with permanent ids, like everything else it places.

## 17. Mordor: the two options

The question is whether Mordor's cities run on the same machinery as Gondor's, or on something rougher. One fact first: of the Mordor pilot cities only Barad-dur is an orc stronghold. Harad and Umbar are cities of Men allied with Mordor.

**Option A: the same machinery, grim words.**
- Barad-dur gets venues like any city, under different names: the mess hall for the tavern, the fighting pits for the theatre, the forge-halls, the warg kennels, the overseer's post.
- Residents have names, schedules and friendship, and errands come from the same templates with Mordor's texts. Bribes, threats and theft are ordinary there.
- **For:** equal content for both realms, one system to build.
- **Against:** Mordor risks feeling like Gondor with orcs.

**Option B: orders, not favours.**
- In Barad-dur nobody asks for favours. Superiors give **orders**: a duty roster with deadlines.
  - Carry this to the tower before the second watch.
  - Bring twenty orc-draughts to the pits.
  - Find out who stole from the overseer.
- An order can't be refused freely: refusing or failing costs standing.
- Standing is your **rank in the war camp**, not a friendship. Promotion brings rewards, a better share of loot and bigger orders.
- **Fear and favour** replace friendship. The existing `.fear` and `.honor` (#892) fit here.
- Most of the camp's dynamic life comes from the **rival captains**:
  - they rise, fall and challenge each other;
  - you pick a side, and the other captain remembers;
  - a captain who beats you is promoted and taunts you next time (nemesis lite).
- **For:** a Mordor that plays differently and feels like Tolkien's.
- **Against:** more design and content of its own.

**My recommendation: both, by city.**
- Underneath it's one engine: venues, residents and templates.
- **Barad-dur runs as option B.** Givers are superiors, friendship becomes the captain's favour, city reputation becomes rank in the camp, and the rival captains drive it.
- **Harad and Umbar run as option A,** with their own colour: caravan masters, corsairs, fences and slave traders.
- So Mordor gets one place that feels truly different, and the extra work stays limited to one city's rules and texts.
