#!/usr/bin/env python3
"""Generates docs/balance/combat-balance.xlsx from moriaengine's real combat formulas/content.

Re-run this script whenever the underlying engine numbers change (weapon/armor .def edits,
CombatSettings/RangerSettings/MagerySettings constant changes, new spells/abilities) - every
number in the generated workbook carries a Source + "how to change" note pointing back here.

Weapons, ammo, armor, monsters, spells and professions are read from scripts/ at generation time;
CONSTANTS and the formulas are ported by hand from the settings classes, CombatCalculator and
DamageManager as of #860 (see docs/current/04-gameplay-systems/combat.md). Run it from the
moriaengine/ folder. Requires openpyxl (not in the repo's own Python-free toolchain - install into
a venv: `python3 -m venv /tmp/venv && /tmp/venv/bin/pip install openpyxl && /tmp/venv/bin/python3
tools/balance/generate_combat_balance.py`).
"""

import datetime
import glob
import os
import re
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment
from openpyxl.utils import column_index_from_string, get_column_letter
from openpyxl.worksheet.datavalidation import DataValidation

GENERATED_DATE = datetime.date.today().isoformat()
OUTPUT_PATH = "docs/balance/combat-balance.xlsx"

# --------------------------------------------------------------------------------------
# Styling helpers
# --------------------------------------------------------------------------------------

TITLE_FONT = Font(bold=True, size=14)
H1_FONT = Font(bold=True, size=12, color="FFFFFF")
H1_FILL = PatternFill("solid", fgColor="2F5597")
H2_FONT = Font(bold=True, size=11)
H2_FILL = PatternFill("solid", fgColor="D9E1F2")
HEADER_FONT = Font(bold=True)
HEADER_FILL = PatternFill("solid", fgColor="BDD7EE")
INPUT_FILL = PatternFill("solid", fgColor="FFF2CC")
OUTPUT_FILL = PatternFill("solid", fgColor="E2EFDA")
SOURCE_FONT = Font(italic=True, size=9, color="7F7F7F")
WRAP = Alignment(wrap_text=True, vertical="top")


def h1(ws, row, col, text, span=1):
    c = ws.cell(row=row, column=col, value=text)
    c.font = H1_FONT
    c.fill = H1_FILL
    for i in range(1, span):
        ws.cell(row=row, column=col + i).fill = H1_FILL
    return row + 2


def h2(ws, row, col, text, span=1):
    c = ws.cell(row=row, column=col, value=text)
    c.font = H2_FONT
    c.fill = H2_FILL
    for i in range(1, span):
        ws.cell(row=row, column=col + i).fill = H2_FILL
    return row + 1


def note(ws, row, col, text, span=6):
    c = ws.cell(row=row, column=col, value=text)
    c.font = SOURCE_FONT
    c.alignment = WRAP
    ws.merge_cells(start_row=row, start_column=col, end_row=row, end_column=col + span - 1)
    return row + 1


def header_row(ws, row, col, headers):
    for i, htext in enumerate(headers):
        c = ws.cell(row=row, column=col + i, value=htext)
        c.font = HEADER_FONT
        c.fill = HEADER_FILL
    return row + 1


def data_rows(ws, row, col, rows):
    for r in rows:
        for i, v in enumerate(r):
            ws.cell(row=row, column=col + i, value=v)
        row += 1
    return row


def input_cell(ws, row, col, value, name=None):
    c = ws.cell(row=row, column=col, value=value)
    c.fill = INPUT_FILL
    if name:
        ws.parent.defined_names[name] = _defined_name(ws, name, row, col)
    return c


def output_cell(ws, row, col, formula):
    c = ws.cell(row=row, column=col, value=formula)
    c.fill = OUTPUT_FILL
    return c


def _defined_name(ws, name, row, col):
    from openpyxl.workbook.defined_name import DefinedName
    ref = f"'{ws.title}'!${get_column_letter(col)}${row}"
    return DefinedName(name, attr_text=ref)


def _defined_range(ws, name, col, first_row, last_row):
    from openpyxl.workbook.defined_name import DefinedName
    letter = get_column_letter(col)
    return DefinedName(name, attr_text=f"'{ws.title}'!${letter}${first_row}:${letter}${last_row}")


def autosize(ws, widths):
    for col, w in widths.items():
        ws.column_dimensions[get_column_letter(col)].width = w


# --------------------------------------------------------------------------------------
# SOURCE DATA
# Read from the engine's content when this runs, or ported from its settings classes - see the
# per-table "Source" / "How to change" columns in the generated sheets for exact references.
# --------------------------------------------------------------------------------------

# Material tiers: tier -> (metal name, wood name or None, has_weapons, has_armor)
# scripts/Basic/Enums.cs (Material enum - metal/wood share the same numeric tier)
MATERIALS = [
    (1, "Copper", "Spruce", True, True),
    (2, "Iron", "Chestnut", True, True),
    (3, "Silver", None, False, True),
    (4, "Gold", None, False, True),
    (5, "Verite", "Oak", True, True),
    (6, "Valorite", "Teak", True, True),
    (7, "Obsidian", "Mahagon", True, True),
    (8, "Adamantinum", "Eben", True, True),
    (9, "Mithril", "Elven", True, True),
    (10, "Sand", None, False, False),
]
METAL_TIERS = [m for m in MATERIALS if m[4]]  # has_armor (index 4) - covers all 9 real craftable tiers
WEAPON_TIERS = [m for m in MATERIALS if m[3]]  # has_weapons (index 3) - the 7 tiers with weapon content

# Reads the real content at generation time, so these tables can't drift from scripts/. The file
# is always run from a checkout; a def that went missing raises here instead of leaving a hole.
SCRIPTS_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "..", "..", "scripts")


def read_def_sections(path):
    """A .def file as [(header kind, header name, {lowercase field: value})]. Comments are dropped,
    the first value of a repeated field wins (none of the fields read here repeat)."""
    sections = []
    fields = None
    with open(path, encoding="latin-1") as f:
        for line in f:
            header = re.match(r"\[(\w+)\s+([^\]]+)\]", line)
            if header:
                fields = {}
                sections.append((header.group(1), header.group(2).strip(), fields))
                continue
            if fields is None:
                continue
            field = re.match(r"\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*)", line)
            if field:
                fields.setdefault(field.group(1).lower(), field.group(2).split("//")[0].strip())
    return sections


def content_files(*parts):
    return sorted(glob.glob(os.path.join(SCRIPTS_DIR, *parts)))


def number(text):
    value = float(text)
    return int(value) if value == int(value) else value


def numbers(text):
    return [number(n) for n in re.findall(r"-?\d+(?:\.\d+)?", text or "")]


def damage_flags(text):
    """'DamageType.MagicFire', 'DamageType.Physical|DamageType.Blunt' -> 'Magic|Fire', 'Physical|Blunt'."""
    flags = []
    for token in text.replace("DamageType.", "").split("|"):
        token = token.strip()
        for group in ("Magic", "Physical"):
            if token.startswith(group) and (token != group):
                flags.append(group)
                token = token[len(group):]
        flags.append(token)
    ordered = [g for g in ("Physical", "Magic") if g in flags]
    ordered += [f for f in flags if f not in ("Physical", "Magic")]
    return "|".join(dict.fromkeys(ordered))


def resist_of_flags(flags):
    """The one subtype resist of a single-subtype damage type, '' if it has none."""
    subtypes = [f for f in flags.split("|") if f not in ("Physical", "Magic")]
    return "Resist" + subtypes[0] if subtypes else ""


# Weapon archetypes: (archetype, display-name-root, notes). WeaponType and MaterialType come from
# the defs below, the skill from WEAPON_TYPE_SKILLS. bare_hands is no item (CombatSettings).
WEAPON_ROWS = [
    ("bare_hands", "bare hands", "No item: CombatSettings.bareHandsAttackVsP/VsM, Speed, Piercing; use the Wrestling skill as the weapon skill"),
    ("dagger", "dagger", ""),
    ("club", "club", "NAME uses wood species name at every tier but MaterialType is Metal - content quirk, not fixed here"),
    ("kryss", "kryss", ""),
    ("cutlass", "cutlass", ""),
    ("mace", "mace", ""),
    ("scimitar", "scimitar", "Fencing skill, but slashes (CombatCalculator.GetDamageTypeOfWeapon)"),
    ("axe", "axe", ""),
    ("shepherds_crook", "crook", ""),
    ("staff_black", "black staff", ""),
    ("katana", "katana", ""),
    ("spear_short", "short spear", ""),
    ("war_fork", "war fork", "Stabs, unlike other fencing weapons (CombatCalculator.GetDamageTypeOfWeapon)"),
    ("sword_broad", "broad sword", "WeaponType is OneHandSpike (Fencing) despite 'sword' in the name - converter guess, not corrected here"),
    ("mace_war", "war mace", ""),
    ("sword_long", "long sword", "same WeaponType quirk as broad sword"),
    ("sword_viking", "viking sword", "same WeaponType quirk as broad sword"),
    ("staff_quarter", "quarter staff", ""),
    ("axe_battle", "battle axe", ""),
    ("hammer_pick", "hammer pick", ""),
    ("pitchfork", "pitchfork", ""),
    ("axe_exec", "executioners axe", "Slashes for a player (CombatCalculator.GetDamageTypeOfWeapon), as any axe"),
    ("axe_double", "double axe", ""),
    ("hammer_war", "war hammer", ""),
    ("axe_two_hand", "two handed axe", ""),
    ("spear", "spear", ""),
    ("staff_gnarled", "gnarled staff", ""),
    ("axe_war", "war axe", ""),
    ("axe_battle_large", "battle large axe", ""),
    ("bardiche", "bardiche", ""),
    ("maul", "maul", ""),
    ("halberd", "halberd", ""),
    ("bow", "bow", "A run bow: runBowDamage (0.82) applies, see Ranger tab"),
    ("crossbow", "crossbow", "light crossbow"),
    ("crossbow_heavy", "heavy crossbow", ""),
]

# WeaponSkillTargetQueuePlugin.GetSkillNameByWeaponType
WEAPON_TYPE_SKILLS = {
    "BareHands": "Wrestling", "XBow": "Marksmanship", "Bow": "Archery",
    "OneHandSword": "Swords", "TwoHandSword": "Swords", "OneHandAxe": "Swords", "TwoHandAxe": "Swords",
    "OneHandSpike": "Fencing", "TwoHandSpike": "Fencing", "OneHandBlunt": "Macing", "TwoHandBlunt": "Macing",
}

# CombatCalculator.GetWeaponDamageType
WEAPON_TYPE_DAMAGE = {
    "BareHands": "Physical|Blunt", "Bow": "Physical|Stabbing", "XBow": "Physical|Stabbing",
    "OneHandSword": "Physical|Slashing", "TwoHandSword": "Physical|Slashing",
    "OneHandAxe": "Physical|Slashing", "TwoHandAxe": "Physical|Slashing",
    "OneHandSpike": "Physical|Sharp", "TwoHandSpike": "Physical|Sharp",
    "OneHandBlunt": "Physical|Blunt", "TwoHandBlunt": "Physical|Blunt",
}
# CombatCalculator.GetDamageTypeOfWeapon names these three by model
WEAPON_DAMAGE_OVERRIDES = {"war_fork": "Physical|Stabbing", "scimitar": "Physical|Slashing", "axe_exec": "Physical|Slashing"}


def load_weapons():
    """Attack, speed, piercing and mind power per archetype from scripts/converted/zbrane/[1-7]*.def.
    Speed and piercing are shared by all materials (nastaveni_weapspeed_<model>), checked here.
    The *_run variants are empty leftovers (attack 0): the normal bow already is a run bow."""
    defs = {}
    for path in content_files("converted", "zbrane", "[1-7]*.def"):
        for _, name, fields in read_def_sections(path):
            if "attackvsp" in fields and not name.lower().endswith("_run"):
                defs[name.lower()] = fields

    archetypes, attack, mindpower, speed, piercing, damage = [], {}, {}, {}, {}, {}
    for arche, disp, wnote in WEAPON_ROWS:
        if arche == "bare_hands":
            wtype, mtype = "BareHands", "-"
            attack[arche] = ["=C_bareHandsAttackVsP"] * len(WEAPON_TIERS)
            mindpower[arche] = [-1] * len(WEAPON_TIERS)
            speed[arche], piercing[arche] = "=C_bareHandsSpeed", "=C_bareHandsPiercing"
        else:
            tiers = [defs[f"i_{arche}_{m[1].lower()}"] for m in WEAPON_TIERS]
            wtype = tiers[0]["weapontype"].split(".")[-1]
            mtype = tiers[0]["materialtype"].split(".")[-1]
            attack[arche] = [number(t["attackvsp"]) for t in tiers]
            mindpower[arche] = [number(t.get("mindpowervsp", -1)) for t in tiers]
            speeds = {number(t["speed"]) for t in tiers}
            piercings = {number(t["piercing"]) for t in tiers}
            if (len(speeds) != 1) or (len(piercings) != 1):
                raise ValueError(f"{arche}: speed or piercing differs by material: {speeds} {piercings}")
            speed[arche], piercing[arche] = speeds.pop(), piercings.pop()
        damage[arche] = WEAPON_DAMAGE_OVERRIDES.get(arche, WEAPON_TYPE_DAMAGE[wtype])
        archetypes.append((arche, disp, wtype, WEAPON_TYPE_SKILLS[wtype], mtype, wnote))
    return archetypes, attack, mindpower, speed, piercing, damage


# Columns of WEAPON_ATTACK and friends match WEAPON_TIERS order: Copper, Iron, Verite, Valorite,
# Obsidian, Adamantinum, Mithril. MindPower: staves carry Erebor's nastaveni_staffSMbonus, the other
# OneHandBlunt/TwoHandBlunt the #539 scaling off the bare-hands 700; -1 = not in content, the same
# sentinel Weapon.CalculateMPDurabilty treats as "use the flat bare-hands default".
WEAPON_ARCHETYPES, WEAPON_ATTACK, WEAPON_MINDPOWER, WEAPON_SPEED, WEAPON_PIERCING, WEAPON_DAMAGE = load_weapons()


def load_ammo():
    """Ammo from scripts/converted/zbrane/ammo.def: (family, material, ProjectileType, multiplier, piercing)."""
    ammo = []
    for _, name, fields in read_def_sections(content_files("converted", "zbrane", "ammo.def")[0]):
        if "damagemultiplier" not in fields:
            continue
        material = fields["material"].split(".")[-1]
        family = re.sub("_" + material + "(?=_|$)", "", name, flags=re.IGNORECASE)[2:]
        ammo.append((family, material, fields["projectiletype"].split(".")[-1],
                     number(fields["damagemultiplier"]), number(fields.get("piercing", 0))))
    return ammo


AMMO = load_ammo()
AMMO_FAMILIES = list(dict.fromkeys(a[0] for a in AMMO))


def load_armor():
    """Armor pieces from scripts/converted/brneni/[1-9]*.def: (piece, layer, WearableType, per-material
    armor, shield?, per-material mind defense), materials in METAL_TIERS order. armorVsP == armorVsM
    and mindDefenseVsP == mindDefenseVsM for every piece, checked here."""
    pieces = {}
    for path in content_files("converted", "brneni", "[1-9]*.def"):
        for _, name, fields in read_def_sections(path):
            if "armorvsp" not in fields:
                continue
            if (fields["armorvsp"] != fields["armorvsm"]) or (fields["minddefensevsp"] != fields["minddefensevsm"]):
                raise ValueError(f"{name}: armor or mind defense differs vs players and monsters")
            material = fields["material"].split(".")[-1].capitalize()
            piece = re.sub("_?(golden|" + material + ")(?=_|$)", "", name, flags=re.IGNORECASE).upper()
            pieces.setdefault(piece, (int(fields["layer"]), fields["wearabletype"].split(".")[-1], {}))[2][material] = fields

    armor = []
    for piece, (layer, wtype, by_material) in sorted(pieces.items()):
        tiers = [by_material[m[1]] for m in METAL_TIERS]
        armor.append((piece, layer, wtype, [number(t["armorvsp"]) for t in tiers], layer == 2,
                      [number(t["minddefensevsp"]) for t in tiers]))
    return armor


ARMOR_PIECES = load_armor()

# Body part coverage - Sphere's race body part table, CombatCalculator.bodyPartCoverage and GetBodyPartsOfLayer
BODY_ZONES = [
    ("Head", 0.14, "Helmet"),
    ("Neck", 0.06, "Collar"),
    ("Back", 0.18, "Chest, Cape, Robe, HalfApron, Shirt, Tunic"),
    ("Chest", 0.18, "Chest, Cape, Robe, HalfApron, Shirt, Tunic"),
    ("Arms", 0.14, "Arms, Shirt, Tunic"),
    ("Hands", 0.07, "Gloves"),
    ("Legs", 0.17, "Pants, Skirt, Shoes, Leggins"),
    ("Feet", 0.06, "Pants, Skirt, Shoes, Leggins"),
]

# Named engine constants - (name, value, meaning, source, how-to-change)
CS = "scripts/Basic/skills/Combat/CombatSettings.cs"
RS = "scripts/Basic/skills/Combat/RangerSettings.cs"
MS = "scripts/Basic/skills/Magery/MagerySettings.cs"
SKILLS = "scripts/Basic/skills/Skills.scp"
EDIT_CS = "Edit CombatSettings.cs, rebuild, restart server (or live GM .settings)"
EDIT_MS = "Edit MagerySettings.cs, rebuild, restart server (or live GM .settings)"
CONSTANTS = [
    ("attackStrModifier", 3.170, "Str's weight in the melee attack-value sum", CS, EDIT_CS),
    ("weapAttack", 1.100, "Player's attack multiplier, all targets (nastaveni_global_UC)", CS, EDIT_CS),
    ("weapAttackM", 1.000, "Monster's attack multiplier, all targets (nastaveni_global_MUC)", CS, EDIT_CS),
    ("weapAttackVsM", 1.600, "Extra attack multiplier, target is a monster (nastaveni_global_vMweapattack)", CS, EDIT_CS),
    ("weapAttackVsMPerLevel", 0.010, "Added to weapAttackVsM per player level", CS, EDIT_CS),
    ("weapAttackVsMMaxLevel", 200, "Level at which weapAttackVsMPerLevel stops growing", CS, EDIT_CS),
    ("weapAttackPvP", 0.900, "Extra attack multiplier, Player-vs-Player (nastaveni_global_PvPweapattack)", CS, EDIT_CS),
    ("armorClassP", 0.700, "Armor multiplier in damagecust, target is a player (nastaveni_global_Parmor)", CS, EDIT_CS),
    ("armorClassM", 1.000, "Armor multiplier in damagecust, target is a monster (nastaveni_global_Marmor)", CS, EDIT_CS),
    ("armorClassMvP", 200, "damagecust_MvP's armor factor: armor 100 halves a monster's blow, 200 stops it (nastaveni_global_MvP_armorfactor)", CS, EDIT_CS),
    ("armorRandEffectMin", 0.945, "Armor roll in damagecust, lower bound", CS, EDIT_CS),
    ("armorRandEffectMax", 1.050, "Armor roll in damagecust, upper bound", CS, EDIT_CS),
    ("swingDamagePvP", 0.900, "damageMod, player hits player (nastaveni_global_PvPweapdam)", CS, EDIT_CS),
    ("swingDamageVsM", 1.350, "damageMod, target is a monster (nastaveni_global_vMweapdam)", CS, EDIT_CS),
    ("swingDamageM", 1.000, "damageMod, a monster's blow (nastaveni_global_Mvweapdam): multiplies the attack in damagecust_MvP, swingDamageVsM on a monster", CS, EDIT_CS),
    ("swingDamageRandMvPMin", 0.850, "Random factor of a monster hitting a player, lower bound (nastaveni_global_MvP_randomfactor)", CS, EDIT_CS),
    ("swingDamageRandMvPMax", 1.000, "Random factor of a monster hitting a player, upper bound", CS, EDIT_CS),
    ("weaponSpeedGlobal", 1.000, "Global multiplier on every swing's speed", CS, EDIT_CS),
    ("weaponSpeedNPC", 0.150, "NPC's weapon speed / 1000: speed in the swing formula is this * 1000 (150)", CS, EDIT_CS),
    ("swingTicksPerSecond", 9.14, "Erebor's server ticks per second (Windows rate), turns hitspeed into seconds", CS, EDIT_CS),
    ("weaponDodgePerPoint", 10, "a_weapon_dodge: permille of monsters' physical blows dodged per point (nastaveni_ability_pvMdodge)", CS, EDIT_CS),
    ("bareHandsAttackVsP", 5, "Unarmed attack value vs Player (nastaveni_weapdam_barehands)", CS, EDIT_CS),
    ("bareHandsAttackVsM", 5, "Unarmed attack value vs Monster", CS, EDIT_CS),
    ("bareHandsSpeed", 200, "Unarmed weapon speed (nastaveni_weapspeed_barehands)", CS, EDIT_CS),
    ("bareHandsPiercing", 100, "Unarmed piercing", CS, EDIT_CS),
    ("bareHandsMindPowerVsP", 700, "Unarmed MindPower vs Player (also every non-blunt weapon's fallback - see Mystic tab)", MS, EDIT_MS),
    ("bareHandsMindPowerVsM", 700, "Unarmed MindPower vs Monster", MS, EDIT_MS),
    ("mindPowerIntModifier", 0.700, "Int's weight in the mind-power sum", MS, EDIT_MS),
    ("spellPowerVsM", 2.000, "Spell power multiplier, target is a monster (nastaveni_global_vMsm)", MS, EDIT_MS),
    ("spellPowerPvP", 1.000, "Spell power multiplier, player's spell on a player (nastaveni_global_pvpSM)", MS, EDIT_MS),
    ("spellPowerMonster", 1.000, "Spell power multiplier, caster is a monster (nastaveni_global_MvSM)", MS, EDIT_MS),
    ("fieldSpellPowerPerLevel", 0.020, "Field spells 28/39/47 vs monsters: added to spellPowerVsM per fourth of the level, up to 50 fourths (nastaveni_standcast_sm)", MS, EDIT_MS),
    ("armorMindDefense", 0.800, "Share of worn armor's mind defense that counts (nastaveni_global_armorOM)", MS, EDIT_MS),
    ("mindDefenseGlobal", 0.900, "Multiplier on a player's whole mind defense (nastaveni_global_om)", MS, EDIT_MS),
    ("mysticPowerVsM", 1.100, "Mystic strike power vs monsters (nastaveni_global_mystikvM / 10)", MS, EDIT_MS),
    ("mysticPowerPerLevel", 0.010, "Mystic strike: added to spellPowerVsM per player level (nastaveni_mysticLvL_sm)", MS, EDIT_MS),
    ("mysticStrikeEffect", 16, "Mystic strike: damage = power * this / 1000 (nastaveni_ability_mystikUCbezpary)", MS, EDIT_MS),
    ("mysticWeaponSpeedModifier", 60, "Mystic's weapon speed in the swing formula (nastaveni_weapspeed_mystik)", MS, EDIT_MS),
    ("mysticManaLossPermille", 960, "Mana retained (960/1000 = 96%) per landed Mystic swing (nastaveni_mystik_manaloss)", MS, EDIT_MS),
    ("concentrationDiscountPerPoint", 0.02, "Mage Concentration: mana-cost discount per ability point (cap 0.9)", MS, EDIT_MS),
    ("magicChargeBonusPerPoint", 0.02, "Mage Magic Charge: spell-power bonus per stacked charge", MS, EDIT_MS),
    ("magicChargeDecayIntervalSeconds", 10.0, "Mage Magic Charge: seconds before a charge decays", MS, EDIT_MS),
    ("runBowDamage", 0.820, "A bow's attack is this share of a crossbow's, before the ammo (nastaveni_global_runbowdam)", RS, "Edit RangerSettings.cs, or live GM .settings command"),
    ("Ranges.bow", 8, "Bow range vs monsters (tiles) (nastaveni_global_runbowrangepvm)", CS, "Edit CombatSettings.cs Ranges page, or live GM .settings command"),
    ("Ranges.bowVsPlayers", 7, "Bow range vs players (nastaveni_global_Runbowrangepvp)", CS, "Edit CombatSettings.cs Ranges page, or live GM .settings command"),
    ("Ranges.crossbow", 8, "Light crossbow range", CS, "Edit CombatSettings.cs Ranges page, or live GM .settings command"),
    ("Ranges.heavyCrossbow", 8, "Heavy crossbow range", CS, "Edit CombatSettings.cs Ranges page, or live GM .settings command"),
    ("Ranges.mystic", 8, "Mystic range vs monsters (flat, overrides weapon/bare-hand range) (nastaveni_global_mystikrangepvm)", CS, "Edit CombatSettings.cs Ranges page, or live GM .settings command"),
    ("Ranges.mysticVsPlayers", 7, "Mystic range vs players (nastaveni_global_mystikrangepvp)", CS, "Edit CombatSettings.cs Ranges page, or live GM .settings command"),
    ("Tactics_EffectMax", 303, "Tactics skill's contribution to melee attack value at skill 1000/GM", SKILLS, "Edit Skills.scp's EFFECT=0,x line for Tactics, restart server"),
    ("Anatomy_EffectMax", 155, "Anatomy skill's contribution to melee attack value at skill 1000/GM", SKILLS, "Edit Skills.scp's EFFECT=0,x line for Anatomy, restart server"),
    ("ArmsLore_EffectMax", 76, "ArmsLore skill's contribution to melee attack value at skill 1000/GM", SKILLS, "Edit Skills.scp's EFFECT=0,x line for ArmsLore, restart server"),
    ("EvalInt_EffectMax", 100, "Eval Intelligence's contribution to mind-power at skill 1000/GM", SKILLS, "Edit Skills.scp's EFFECT=0,x line for EvalInt, restart server"),
    ("SpiritSpeak_EffectMax", 25, "Spirit Speak's contribution to mind-power at skill 1000/GM", SKILLS, "Edit Skills.scp's EFFECT=0,x line for SpiritSpeak, restart server"),
    ("MagicResist_EffectMax", 100, "Magic Resistance's flat effect (nastaveni_skilleffect_Skill_MagicResist): player's mind defense gets resist * this / 1000", SKILLS, "Edit Skills.scp's EFFECT=x line for MagicResist, restart server"),
]

RESIST_TYPES = [
    ("ResistMagic", "Magic (group)", "multiplies the averaged magic subtypes below"),
    ("ResistPhysical", "Physical (group)", "multiplies the averaged physical subtypes below"),
    ("ResistFire", "Fire (magic subtype)", "Magic Arrow, Fireball, Explosion, Flame Strike, Meteor Swarm; monsters' fire blows"),
    ("ResistElectric", "Electric (magic subtype)", "Lightning, Mind Blast, Energy Bolt, Chain Lightning; monsters' electric blows"),
    ("ResistAcid", "Acid (magic subtype)", "none of the shipped spells"),
    ("ResistCold", "Cold (magic subtype)", "Harm, Mystic strike; monsters' cold blows"),
    ("ResistPoison", "Poison (magic subtype)", "Poison spell's damage-over-time"),
    ("ResistMystical", "Mystical (magic subtype)", "monsters' Dark Curse (Mystic strike is cold magic since #860)"),
    ("ResistDragon", "Dragon (magic subtype)", "dragon-sourced damage"),
    ("ResistSlashing", "Slashing (physical subtype)", "Swords and axes, scimitar, executioner's axe"),
    ("ResistStabbing", "Stabbing (physical subtype)", "Bows, crossbows, war fork"),
    ("ResistSharp", "Sharp (physical subtype)", "Other fencing weapons; monsters' swords, axes and fencing weapons (with Slashing or Stabbing)"),
    ("ResistBlunt", "Blunt (physical subtype)", "Maces, hammers, staves, bare hands"),
    ("ResistArchery", "Archery (physical subtype)", "no player weapon since #860, bows stab"),
    ("ResistBleed", "Bleed (physical subtype)", "Bleed-DoT effects (e.g. Thief's Bleeding Strike)"),
    ("ResistSummon", "Summon (separate)", "summoned-creature damage, multiplies the result of the rest"),
]
PHYSICAL_SUBTYPES = ["Slashing", "Stabbing", "Blunt", "Sharp", "Bleed", "Archery"]
MAGIC_SUBTYPES = ["Fire", "Cold", "Acid", "Poison", "Electric", "Dragon", "Mystical"]


def load_spells():
    """Damage spells from damageSpells.def: name, DamageType flags, effect min, effect max, mana use,
    cast time, Magery requirement, area range (0 = single target), ArmorPiercing (-1 = armor ignored),
    uses mind power, subtype resist."""
    path = content_files("Basic", "skills", "Magery", "Spells", "DamageSpells", "damageSpells.def")[0]
    spells = []
    for kind, _, fields in read_def_sections(path):
        if not kind.lower().endswith("spelldef"):
            continue
        flags = damage_flags(fields.get("damagetype", "DamageType.Magic"))
        effect = numbers(fields["effect"])
        spells.append((fields["name"].strip('"'), flags, effect[0], effect[-1], numbers(fields["manause"])[0],
                       number(fields["casttime"]), numbers(fields["requirements"])[0],
                       numbers(fields.get("effectrange", "0"))[0], number(fields.get("armorpiercing", -1)),
                       "Yes" if "UseMindPower" in fields["flags"] else "No", resist_of_flags(flags)))
    return spells


SPELLS = load_spells()

THIEF_ABILITIES = [
    ("a_regen_hits/stam/mana", "Regenerates HP/Stamina/Mana over time", "Yes", "scripts/Basic/AbilitiesAndTalents/Abilities/ (shared regen plugin, also used by Mage)"),
    ("a_weapon_dodge", "Chance-based dodge bonus", "Yes", "scripts/Basic/AbilitiesAndTalents/Abilities/"),
    ("a_spell_absorption", "Absorbs incoming spell damage/effect", "Yes", "scripts/Basic/AbilitiesAndTalents/Abilities/"),
    ("a_leech_hits/stam/mana", "Drains HP/Stamina/Mana from target on hit", "Yes", "scripts/Basic/AbilitiesAndTalents/Abilities/"),
    ("a_longrange", "Flat WeaponRangeModifier bonus while active", "Yes", "scripts/Basic/AbilitiesAndTalents/Abilities/LongRangePlugin.cs"),
    ("a_throwing", "Throws a real projectile (i_kudla throwing knife)", "Yes", "scripts/Basic/AbilitiesAndTalents/Abilities/ThrowingAbilityDef.cs"),
    ("a_bleeding_strike", "On-hit Bleed damage-over-time effect", "Yes", "scripts/Basic/AbilitiesAndTalents/Abilities/BleedingStrikePlugin.cs / BleedingEffectPlugin.cs"),
    ("a_kick", "Kick attack", "Unverified - has a .scp file but trigger logic wasn't traced in this pass", "scripts/Basic/AbilitiesAndTalents/Abilities/kick.scp"),
    ("Backstab (stealth opener)", "Designed: hidden+adjacent+facing required, x1.5 vs Player / x2.0 vs Monster bonus, reveals on success", "No - designed only, issue #477", "docs/moria design notes + GitHub issue #477"),
    ("Attack-from-behind (passive)", "Designed: facing-based damage bonus", "No - designed only, issue #478", "GitHub issue #478"),
    ("Podriznuti (toggle amplifier)", "Designed: amplifies the attack-from-behind bonus", "No - designed only, issue #478", "GitHub issue #478"),
    ("Thief bleed cap", "Designed: Thief-specific higher cap on the generic bleed mechanism", "Yes - 300 + level up to 200, 250 for others", "GitHub issue #479"),
    ("Defensive stance + weapon reflect", "Designed: damage-reduction stance, weapon-specific reflect", "No - designed only, issue #480", "GitHub issue #480"),
    ("Shield-strike paralyze", "Designed: niche paralyze-on-shield-strike", "No - designed only (optional), issue #481", "GitHub issue #481"),
]

def load_professions():
    """Stat ranges, allowed gear and PvP changes from scripts/Basic/Professions/Definitions/*.def."""
    professions = {}
    for path in content_files("Basic", "Professions", "Definitions", "*.def"):
        for kind, _, fields in read_def_sections(path):
            if kind.lower() != "professiondef":
                continue
            name = os.path.splitext(os.path.basename(path))[0].capitalize()
            professions[name] = dict(
                str_r=tuple(numbers(fields.get("strrange"))), dex_r=tuple(numbers(fields.get("dexrange"))),
                int_r=tuple(numbers(fields.get("intrange"))), vit_r=tuple(numbers(fields.get("vitrange"))),
                weapons=fields.get("allowedweapontypes", "any"), armor=fields.get("allowedarmormaterials", "any"),
                armor_change=number(fields.get("pvparmorchange", 0)),
                mind_change=number(fields.get("pvpminddefensechange", 0)),
                source=f"scripts/Basic/Professions/Definitions/{os.path.basename(path)}")
    return professions


PROFESSION_STATS = load_professions()
PROFESSION_NAMES = list(PROFESSION_STATS)

# The attacker's damage path per profession in the Fight Simulator. Priest, Shaman and Necromant
# have no calculator tab of their own, Craftsman fights with melee weapons.
PROFESSION_PIPELINES = {"Warrior": "Melee", "Thief": "Melee", "Craftsman": "Melee", "Ranger": "Ranged",
                        "Mystic": "MindPower", "Mage": "Spell", "Priest": "Spell", "Shaman": "Spell", "Necromant": "Spell"}

# Same files the catalog has always skipped: no monsters in them.
NON_MONSTER_CHAR_FILES = {"vendors.def", "guards.def", "mounts.def", "mounts_chars.def", "ucitele.def", "groups.def"}
MONSTER_CATEGORIES = {
    "animals.def": "Animal", "caves.def": "Cave", "daemons.def": "Daemon", "dragons.def": "Dragon/Boss",
    "dragons_tmp.def": "Dragon/Boss", "specNPC_dragons.def": "Dragon/Boss", "elementals.def": "Elemental",
    "ents.def": "Ent", "eyes.def": "Eye", "golems.def": "Golem", "insects.def": "Insect",
    "nb_monsters.def": "Misc Monster", "npc_evil_race.def": "Evil Race (humanoid)", "ogres.def": "Ogre",
    "orcs.def": "Orc", "snowmonsters.def": "Snow Monster", "special.def": "Special", "summons.def": "Summon",
    "t2a.def": "T2A Dungeon", "undeads.def": "Undead", "swamp.def": "Swamp", "tutorial.def": "Tutorial",
}
MONSTER_ATTACK_SKILLS = ["wrestling", "swords", "fencing", "macing", "archery", "marksmanship"]
MONSTER_DEFAULT_DAMAGE = "Physical|Blunt"  # bare hands: CombatCalculator.GetWeaponDamageType
MONSTER_DEFAULTS = dict(max_hits=100, max_stam=100)  # NPC.ct: maxHitpoints, maxStamina


def load_monsters():
    """Monster catalog from scripts/converted/chars/*.def. One row per distinct creature: NPCDefs with
    the same name and the same combat numbers collapse (weapon/color variants); others of that name
    get their defname added, so the picker's names stay unique. Rows hold (name, category, dict)."""

    def stat_range(fields, key, default=0):
        values = numbers(fields.get(key)) or [default]
        return min(values), max(values)

    def average(pair):
        return (pair[0] + pair[1]) / 2

    groups = {}
    for path in content_files("converted", "chars", "*.def"):
        filename = os.path.basename(path)
        if filename in NON_MONSTER_CHAR_FILES:
            continue
        for kind, defname, fields in read_def_sections(path):
            if (kind.lower() != "npcdef") or (("weaponattack" not in fields) and ("maxhits" not in fields)):
                continue
            stam = stat_range(fields, "maxstam", MONSTER_DEFAULTS["max_stam"])
            npc_speed = numbers(fields.get("npcspeed"))
            stats = dict(
                attack=stat_range(fields, "weaponattack"), armor_p=stat_range(fields, "armorvsp"),
                armor_m=stat_range(fields, "armorvsm"), hits=stat_range(fields, "maxhits", MONSTER_DEFAULTS["max_hits"]),
                stam=stam, swing_stam=(sum(npc_speed) / len(npc_speed)) if npc_speed else average(stam),
                skill=max(average(stat_range(fields, s)) for s in MONSTER_ATTACK_SKILLS),
                tactics=average(stat_range(fields, "tactics")), intel=average(stat_range(fields, "int")),
                evalint=average(stat_range(fields, "evalint")), resist=average(stat_range(fields, "magicresist")),
                damage=damage_flags(fields["naturaldamagetype"]) if "naturaldamagetype" in fields else MONSTER_DEFAULT_DAMAGE,
                resists=tuple(average(stat_range(fields, r[0].lower())) for r in RESIST_TYPES))
            defname = fields.get("defname", defname)
            name = fields.get("name", "").strip('"') or defname
            key = (name, tuple(sorted((k, str(v)) for k, v in stats.items())))
            groups.setdefault(key, (name, MONSTER_CATEGORIES.get(filename, "Unique/Boss (named NPC)"), defname, stats))

    per_name = {}
    for name, _, _, _ in groups.values():
        per_name[name] = per_name.get(name, 0) + 1
    rows, seen = [], set()
    for name, category, defname, stats in groups.values():
        label = name if per_name[name] == 1 else f"{name} ({defname})"
        label = "'" + label if label.startswith("=") else label
        if label.lower() not in seen:  # Excel's MATCH ignores case
            seen.add(label.lower())
            rows.append((label, category, stats))
    return sorted(rows, key=lambda r: (r[1], r[0].lower()))


MONSTERS = load_monsters()

WEAPON_NAMES = [w[0] for w in WEAPON_ARCHETYPES]
MATERIAL_NAMES_FOR_WEAPONS = [m[1] for m in WEAPON_TIERS]
MATERIAL_NAMES_FOR_ARMOR = [m[1] for m in METAL_TIERS]
DEFAULT_WEAPON = "sword_long"
DEFAULT_MATERIAL = "Iron"


def sheet_ref(title):
    return f"'{title}'" if " " in title else title


# --------------------------------------------------------------------------------------
# Formula text, shared by every tab. Each one is a port of a method in
# scripts/Basic/skills/Combat/CombatCalculator.cs or DamageManager.cs, rounding steps included:
# ROUND(x,0) is RoundLikeSphere for the non-negative values here, TRUNC is the C# integer cast.
# --------------------------------------------------------------------------------------

def attack_sum_expr(tactics, anatomy, armslore, strength):
    return (f"(({tactics}/1000*C_Tactics_EffectMax)+({anatomy}/1000*C_Anatomy_EffectMax)"
            f"+({armslore}/1000*C_ArmsLore_EffectMax)+({strength}*C_attackStrModifier))/1000")


def mind_sum_expr(evalint, spiritspeak, intelligence):
    return (f"(({evalint}/1000*C_EvalInt_EffectMax)+({spiritspeak}/1000*C_SpiritSpeak_EffectMax)"
            f"+({intelligence}*C_mindPowerIntModifier))/1000")


def attack_vs_monster_expr(weapon_attack, level):
    """DamageManager.GetWeaponSwingArgs + GetAttackVsMonsterMultiplier, a player on a monster."""
    return (f"ROUND(ROUND({weapon_attack}*C_weapAttack,0)"
            f"*(C_weapAttackVsM+MIN({level},C_weapAttackVsMMaxLevel)*C_weapAttackVsMPerLevel),0)")


def attack_vs_player_expr(weapon_attack):
    return f"ROUND(ROUND({weapon_attack}*C_weapAttack,0)*C_weapAttackPvP,0)"


def pvp_armor_expr(armor, change):
    """DamageManager.GetWeaponSwingArgs, a player on a player."""
    return f"MAX(0,{armor}+{change})*C_armorClassP"


def damage_cust_expr(attack, armor, piercing, roll, damage_mod):
    """CombatCalculator.CalculateDamageCust: a player's swing, or a monster's on a monster."""
    obrana = f"ROUND(MAX(0,{armor})*{roll}*(1000-{piercing}),0)"
    return f"MAX(0,ROUND(TRUNC(({attack}*1000-{obrana}+500)/1000)*{damage_mod},0))"


def damage_mvp_expr(attack, armor, piercing, random_factor):
    """CombatCalculator.CalculateDamageMvP: a monster on a player."""
    obrana = f"MAX(0,{armor})*(1000-{piercing})"
    return (f"MAX(0,ROUND(ROUND(ROUND({attack}*C_swingDamageM,0)*(1000-TRUNC({obrana}/C_armorClassMvP))/1000,0)"
            f"*{random_factor},0))")


def clamp_resist(resist):
    return f"MAX(-40000,MIN(1000,{resist}))"


def resist_modifier_expr(group, subtype):
    """DamageManager.GetResistModifier for one subtype and its group resist."""
    return f"TRUNC((1000-{clamp_resist(group)})*(1000-{clamp_resist(subtype)})/1000)/1000"


def sqrt_int_expr(value):
    """Erebor's sqrt_int is an integer Newton iteration of about 100*sqrt; this is the approximation
    the workbook uses for it, up to 1 off the real function, noted on Core Formulas."""
    return f"ROUND(100*SQRT(MAX({value},0)),0)"


def player_root_expr(dex):
    return f"({sqrt_int_expr(dex)}-2)"


def mystic_root_expr(mana):
    return sqrt_int_expr(f"MAX(INT({mana}/2),1)")


def npc_root_expr(swing_stamina):
    return sqrt_int_expr(f"INT({swing_stamina}/2)")


def hitspeed_expr(root, speed):
    """CombatCalculator.CalculateSwingDelay: the ticks of half a swing."""
    rate = f"ROUND({root}*{speed}*C_weaponSpeedGlobal,0)"
    return f"MAX(INT(1048575/IF({rate}<=0,50,{rate})),1)"


def swing_seconds_expr(hitspeed):
    return f"2*{hitspeed}/C_swingTicksPerSecond"


def hit_limit_expr(skill, tactics, target_tactics, target_stamina):
    """CombatCalculator.CalculateHitDifficultyLimit, the target not shooting back."""
    attack = f"TRUNC(({skill}+{tactics})/2)"
    defend = f"TRUNC(({target_tactics}+{target_stamina}*10)/2)"
    return f"MAX(0,MIN(100,TRUNC(({skill}-TRUNC(({attack}-{defend})/5))/10)))"


def hit_chance_expr(skill, limit):
    """WeaponSkillDef.RollHit worked out: difficulty d of 0..limit-1 is equally likely, 0 always
    hits, the others need SkillUtils.CheckSuccess(skill, d*10). H_HitRollD holds d = 1..99."""
    diff = f"({skill}-10*H_HitRollD)"
    dist = f"ABS{diff}"
    halvings = f"(({dist}>0)*INT(({dist}-1)/250))"
    chance = f"INT(500/2^{halvings})"
    bell = f"({chance}-INT(INT({chance}/2)*({dist}-250*{halvings})/250))"
    scurve = f"(({diff}>0)*(1000-{bell})+({diff}<=0)*{bell})"
    return f"IF({limit}<=1,1,(1+SUMPRODUCT((H_HitRollD<{limit})*({scurve}+1)/1000))/{limit})"


def table_lookup(sheet, value_col, key_col, start, end, key):
    """INDEX/MATCH of a column by a key in another column of the same table."""
    return (f"INDEX('{sheet}'!${value_col}${start}:${value_col}${end},"
            f"MATCH({key},'{sheet}'!${key_col}${start}:${key_col}${end},0))")


def weapon_value_lookup(wi, weapon_cell, material_cell, first_col):
    """One of the two per-material blocks of Weapons Reference (attack or mind power). Both are in
    the same material order, so the attack block's header finds the column of either."""
    last_col = get_column_letter(column_index_from_string(first_col) + len(MATERIAL_NAMES_FOR_WEAPONS) - 1)
    return (f"INDEX('Weapons Reference'!${first_col}${wi['data_start']}:${last_col}${wi['data_end']},"
            f"MATCH({weapon_cell},'Weapons Reference'!$A${wi['data_start']}:$A${wi['data_end']},0),"
            f"MATCH({material_cell},'Weapons Reference'!$F${wi['header_row']}:$L${wi['header_row']},0))")


def weapon_column_lookup(wi, column, weapon_cell):
    return table_lookup("Weapons Reference", column, "A", wi["data_start"], wi["data_end"], weapon_cell)


def weapon_mindpower_expr(wi, weapon_cell, material_cell, durability_cell):
    """Weapon.CalculateMPDurabilty: -1 is unset and falls back to bare hands."""
    lookup = weapon_value_lookup(wi, weapon_cell, material_cell, wi["mindpower_col"])
    return (f"IF({lookup}<0,C_bareHandsMindPowerVsP,"
            f"C_bareHandsMindPowerVsP+(({lookup}-C_bareHandsMindPowerVsP)/2)*(1+{durability_cell}/100))")


def ammo_lookup(wi, column, family_cell, material_cell):
    return table_lookup("Weapons Reference", column, "C", wi["ammo_start"], wi["ammo_end"],
                        f'{family_cell}&"_"&{material_cell}')


def pvp_change_expr(table_name, profession_cell):
    return f"INDEX({table_name},MATCH({profession_cell},T_ProfessionName,0))"


# --------------------------------------------------------------------------------------
# Sheet builders
# --------------------------------------------------------------------------------------

def build_overview(wb):
    ws = wb.active
    ws.title = "Overview"
    row = 1
    ws.cell(row=row, column=1, value="moriaengine Combat Balance Workbook").font = TITLE_FONT
    row += 1
    ws.cell(row=row, column=1, value=f"Generated {GENERATED_DATE} by tools/balance/generate_combat_balance.py from real engine source - not hand-authored estimates.")
    row += 2

    row = h2(ws, row, 1, "How to use this workbook", span=6)
    for line in [
        "Yellow cells = inputs you can change. Green cells = computed outputs (formulas - don't hand-edit).",
        "Every reference table and constant has a Source + 'How to change' column/note pointing at the exact file "
        "(and sometimes method) that number comes from in the engine, and what you'd need to do to change it for real "
        "(edit a .cs constant + rebuild + restart, vs. edit a .def content field + restart, vs. a live GM .settings command).",
        "Per-profession tabs have a Calculator block (type in stats/skills/weapon/material/target to see live damage/"
        "DPS/time-to-kill) and a Reference matrix block below it (every weapon or spell at a fixed baseline, for "
        "at-a-glance comparison across materials).",
        "PvM/PvP/MvP axis: PvM = this profession's player attacking a Monster. PvP = attacking a Player. MvP = a "
        "Monster attacking this profession's player (defender side). MvM (Monster-vs-Monster) involves no profession "
        "at all - it's shown once, as a reference row, on the Core Formulas tab, and the Fight Simulator computes it.",
        "Rolls use their average, MvP also its Min and Max as columns: the armor roll 0.945-1.05 averages 0.9975, the "
        "MvP random factor 0.85-1.00 averages 0.925. A real hit lands around that.",
    ]:
        row = note(ws, row, 1, "- " + line, span=8)
    row += 1

    row = h2(ws, row, 1, "Key engine findings worth knowing before you read the numbers", span=6)
    for line in [
        "Combat mirrors the Erebor shard since #860: Erebor's numbers come from save/sphereworld.scp through the "
        "converter, its formulas are ported with their integer rounding. See "
        "docs/current/04-gameplay-systems/combat.md, 'Where the numbers come from' and 'Swing formulas'.",
        "Two armor formulas. A player's swing, or a monster's on a monster, SUBTRACTS armor from the attack. A monster "
        "hitting a player keeps a PERCENTAGE: armor 100 halves the blow, 200 stops it, and the player's armor isn't "
        "multiplied by armorClassP.",
        "Monsters have no stat scaling (#711 is undone): their attack is the def's WeaponAttack. Players get a level "
        "bonus vs monsters, up to level 200. Monster armor in the catalog is the def's own; worn armor adds to it.",
        "Resists are permille, 150 = 15%, and many monsters have them (Monsters Reference): a titan has ResistBlunt 150, "
        "ResistSlashing -50. The subtypes average, the group resist multiplies them. Negative resists raise the damage. "
        "A damage type with no subtype isn't reduced at all.",
        "Bows and crossbows have real attack, speed and piercing per material now. RangerSettings' damage, speed and "
        "tier bonus are unused. The ammo adds its own multiplier and piercing. Every bow is a run bow: attack * 0.82.",
        "Swing time is 2 * hitspeed ticks at 9.14 ticks a second. NPCs swing by their NpcSpeed or max stamina (monsters' "
        "WeaponDelay isn't used any more), Mystics by their mana.",
        "Spells: power is mind power * 10 * spellPowerVsM or spellPowerPvP, less mind defense * 10. A spell with "
        "ArmorPiercing goes through armor like a swing, the others ignore it. Mystic strike is cold magic.",
        "MindPower: staves have Erebor's nastaveni_staffSMbonus, the other OneHandBlunt/TwoHandBlunt the #539 scaling. "
        "Every other weapon falls back to the flat 700 regardless of material.",
        "Silver and Gold are real, fully-stocked armor material tiers with no weapon or wood equivalent - included "
        "in armor tables, absent from weapon tables.",
        "Thief is a real, playable profession today (not just a design doc) with ~6 working abilities - but about "
        "half its talent tree is unimplemented scaffolding. The Thief tab marks each ability Implemented Y/N.",
    ]:
        row = note(ws, row, 1, "- " + line, span=8)
    row += 1

    row = h2(ws, row, 1, "Not modeled here", span=6)
    for line in [
        "On-hit abilities, a_weapon_dodge (each point dodges 1% of monsters' physical blows), elite monsters, Magic Shield "
        "and the like, a target shooting back (its stamina counts 9x in the hit roll), a paralyzed target (rand(10) "
        "instead of the limit), guards always hitting. A GM always passes the hit roll's skill check.",
        "reactive armor, energy shield and rebound on blows taken, Astral Power's effects, bleed damage over time, item wear and repair. "
        "Durability only scales what's typed in here. All of those are in the game, see combat.md.",
    ]:
        row = note(ws, row, 1, "- " + line, span=8)
    row += 1

    row = h2(ws, row, 1, "Tabs in this workbook", span=3)
    row = header_row(ws, row, 1, ["Tab", "Contents"])
    for r in [
        ("Materials", "The 9 real material tiers, metal+wood names, which have weapons/armor/ammo"),
        ("Core Formulas", "Every shared formula, the full constants table (single source of truth), profession PvP changes, worked examples"),
        ("Weapons Reference", "Full weapon archetype x material matrix, speed, piercing, damage type, ammo"),
        ("Armor Reference", "Full armor piece x material matrix + shields, armor and mind defense"),
        ("Monsters Reference", "Every monster's attack, armor, HP, speed, skills and resists"),
        ("Warrior / Ranger / Mage / Mystic / Thief", "Calculator + reference matrix per profession"),
        ("Fight Simulator", "Any Attacker vs any Defender - Player of any profession or a cataloged Monster, full stats/skills/weapon/armor/resists"),
    ]:
        row = data_rows(ws, row, 1, [r])
    autosize(ws, {1: 34, 2: 110})
    for rw in ws.iter_rows():
        for c in rw:
            if c.column == 2 and c.row > row - 7:
                c.alignment = WRAP


def build_materials(wb):
    ws = wb.create_sheet("Materials")
    row = h1(ws, 1, 1, "Materials", span=6)
    row = note(ws, row, 1,
               "Material is ONE shared enum between metal and wood - each wood name is a literal alias for the same "
               "numeric tier as a metal (MaterialType distinguishes which is which). Source: scripts/Basic/Enums.cs (Material enum).",
               span=6)
    row += 1
    row = header_row(ws, row, 1, ["Tier", "Metal name", "Wood name", "Has weapons?", "Has armor?", "Notes"])
    for tier, metal, wood, has_w, has_a in MATERIALS:
        notes = ""
        if metal in ("Silver", "Gold"):
            notes = "Armor-only: no weapon or wood equivalent exists for this tier"
        if metal == "Sand":
            notes = "Unused by weapons or armor in this workbook's scope (believed to be a crafting-only material, e.g. glassblowing - unconfirmed)"
        row = data_rows(ws, row, 1, [[tier, metal, wood or "-", "Yes" if has_w else "No", "Yes" if has_a else "No", notes]])
    row += 1
    row = note(ws, row, 1, "Source: scripts/Basic/Enums.cs (Material enum). How to change: adding a new tier means adding an enum value plus a full set of weapon/armor/ammo .def entries for it - a content project, not a single-constant edit.", span=6)
    autosize(ws, {1: 8, 2: 14, 3: 12, 4: 12, 5: 10, 6: 60})


def build_core_formulas(wb):
    ws = wb.create_sheet("Core Formulas")
    row = h1(ws, 1, 1, "Core Formulas", span=6)
    row = note(ws, row, 1,
               "Shared math every profession tab's calculator references by name (e.g. =C_attackStrModifier). "
               "Change a number here (or better, in its real source file) and every tab that uses it updates.",
               span=6)
    row += 1

    row = h2(ws, row, 1, "Constants", span=5)
    row = header_row(ws, row, 1, ["Name", "Value", "Meaning", "Source", "How to change"])
    const_name_to_cell = {}
    for name, value, meaning, source, howto in CONSTANTS:
        safe_name = "C_" + name.replace(".", "_")
        ws.cell(row=row, column=1, value=name)
        ws.cell(row=row, column=2, value=value)
        ws.cell(row=row, column=3, value=meaning).alignment = WRAP
        ws.cell(row=row, column=4, value=source)
        ws.cell(row=row, column=5, value=howto).alignment = WRAP
        wb.defined_names[safe_name] = _defined_name(ws, safe_name, row, 2)
        const_name_to_cell[name] = f"C_{name.replace('.', '_')}"
        row += 1
    row += 1

    row = h2(ws, row, 1, "Profession PvP changes (added to armor vs players' swings; taken from a spell's defense * 10 vs players' spells)", span=5)
    row = header_row(ws, row, 1, ["Profession", "PvPArmorChange", "PvPMindDefenseChange", "Source"])
    table_start = row
    for name in PROFESSION_NAMES:
        stats = PROFESSION_STATS[name]
        row = data_rows(ws, row, 1, [[name, stats["armor_change"], stats["mind_change"], stats["source"]]])
    table_end = row - 1
    wb.defined_names["T_ProfessionName"] = _defined_range(ws, "T_ProfessionName", 1, table_start, table_end)
    wb.defined_names["T_ProfessionArmorChange"] = _defined_range(ws, "T_ProfessionArmorChange", 2, table_start, table_end)
    wb.defined_names["T_ProfessionMindChange"] = _defined_range(ws, "T_ProfessionMindChange", 3, table_start, table_end)
    row = note(ws, row, 1, "ProfessionDef.PvPArmorChange/PvPMindDefenseChange, kept by hand in the .defs from "
                            "nastaveni_global_armorchange_vP_<profession> and armorchangeom. Used by "
                            "DamageManager.GetPvPArmorChange and CombatCalculator.GetSpellDefense.", span=6)
    row += 1

    row = h2(ws, row, 1, "Body-part armor weights (players and NPCs, on top of the def's own armor)", span=4)
    row = header_row(ws, row, 1, ["Part", "Weight", "Layers feeding it"])
    for zone, weight, layers in BODY_ZONES:
        ws.cell(row=row, column=1, value=zone)
        ws.cell(row=row, column=2, value=weight)
        c = ws.cell(row=row, column=3, value=layers)
        c.alignment = WRAP
        row += 1
    row = note(ws, row, 1, "Source: CombatCalculator.bodyPartCoverage, GetBodyPartsOfLayer, CalculateCombatArmorValues "
                            "(Sphere's CalcArmorDefense). The best piece per part counts, then this weighting. So plate "
                            "on the chest layer alone counts 36%. Shield is handled separately (see formula text below), "
                            "not as a body part. Mind defense from armor weighs the same way, on players only, shield excluded.", span=6)
    row += 1

    row = h2(ws, row, 1, "The PvM / PvP / MvP / MvM swing table", span=6)
    row = note(ws, row, 1, "Which formula and multipliers a swing uses depends on attacker-is-player and "
                            "defender-is-player. The armor stat follows the ATTACKER: a player's swing reads the "
                            "defender's ArmorVsP, a monster's its ArmorVsM. Source: DamageManager.GetWeaponSwingArgs, "
                            "CombatCalculator.CalculateArmorClassEffect.", span=6)
    row = header_row(ws, row, 1, ["Combo", "Attack formula", "Armor formula", "Damage after armor"])
    combo_rows = [
        ("PvM (Player attacks Monster)", "WeaponAttack x weapAttack, rounded, x (weapAttackVsM + MIN(level, 200) x 0.01), rounded", "defender.ArmorVsP x armorClassM", "damagecust with swingDamageVsM (1.35)"),
        ("PvP (Player attacks Player)", "WeaponAttack x weapAttack, rounded, x weapAttackPvP, rounded", "MAX(0, defender.ArmorVsP + PvPArmorChange) x armorClassP", "damagecust with swingDamagePvP (0.9)"),
        ("MvP (Monster attacks Player)", "def's WeaponAttack x weapAttackM, rounded", "defender.ArmorVsM, no multiplier", "damagecust_MvP, RANDOM swingDamageRandMvPMin..Max (0.85-1.0)"),
        ("MvM (Monster attacks Monster) - reference only, no profession involved", "def's WeaponAttack x weapAttackM, rounded, x weapAttackVsM, rounded", "defender.ArmorVsM x armorClassM", "damagecust with swingDamageVsM x swingDamageM; less than 1 does 5, unless summon damage"),
    ]
    for r in combo_rows:
        row = data_rows(ws, row, 1, [r])
    row += 1

    row = h2(ws, row, 1, "Formula reference (text) - exact expressions, for anyone extending this workbook", span=6)
    formula_texts = [
        ("Weapon attack value (player)", "sum = (Tactics_Effect + Anatomy_Effect + ArmsLore_Effect + Str*attackStrModifier) / 1000; WeaponAttack = R(R(R(weapon.AttackVsP * sum) * runBow) * ammoMultiplier), runBow and ammo for bows/crossbows only. R = RoundLikeSphere", "CombatCalculator.CalculateCombatWeaponValues, GetAttackStatMultiplier"),
        ("Weapon attack value (NPC/monster)", "WeaponAttackVsM = WeaponAttackVsP = npcDef.WeaponAttack: no skills, no stats, whatever weapon it holds", "CombatCalculator.CalculateCombatWeaponValues"),
        ("Mind power (player)", "sum = (EvalInt_Effect + SpiritSpeak_Effect + Int*mindPowerIntModifier) / 1000; mindPower = R(weapon.MindPowerVsP * sum)", "CombatCalculator.CalculateCombatWeaponValues, GetMindPowerStatMultiplier"),
        ("Mind power (monster)", "plain Evaluate Intelligence", "CombatCalculator.CalculateCombatWeaponValues"),
        ("Armor class value", "ArmorClassVsP = DefForCombat.ArmorVsP + INT(shield + sum(best piece per part * coverage)) + ArmorClassModifier (ArmorVsM mirrors with VsM fields), players and NPCs alike; shield = armor * Parrying / 1000, added at that value, not by parts", "CombatCalculator.CalculateCombatArmorValues"),
        ("Mind defense (player)", "R((R(MagicResist * MagicResist_Effect / 1000) + sum(best piece's mind defense per part * coverage) * armorMindDefense) * mindDefenseGlobal) + MindDefenseModifier; shield doesn't count", "CombatCalculator.CalculateCombatArmorValues"),
        ("Mind defense (monster)", "INT(MagicResist / 10) + MindDefenseModifier", "CombatCalculator.CalculateCombatArmorValues"),
        ("damagecust: a player's swing, or a monster's on a monster", "obrana = R(MAX(0,armor) * roll(0.945-1.05) * (1000-piercing)); koef = TRUNC((attack*1000 - obrana + 500) / 1000); damage = MAX(0, R(koef * damageMod))", "CombatCalculator.CalculateDamageCust"),
        ("damagecust_MvP: a monster hits a player", "obrana = MAX(0,armor) * (1000-piercing); utok = R(attack * swingDamageM); ubrano = R(utok * (1000 - TRUNC(obrana/armorClassMvP)) / 1000); damage = MAX(0, R(ubrano * random(0.85-1.0)))", "CombatCalculator.CalculateDamageMvP"),
        ("Mystic strike (replaces the armor step)", "power = mindPower*10 * spellPowerPvP vs player, * mysticPowerVsM * (spellPowerVsM + MIN(level,200)*mysticPowerPerLevel) vs monster; damage = MAX(0, TRUNC((TRUNC(power) - defense) * mysticStrikeEffect / 1000)), as Magic|Cold", "DamageManager.GetMindPowerSwingArgs"),
        ("Spell defense", "mindDefense * 10, less the target's PvPMindDefenseChange for a player's spell on a player", "CombatCalculator.GetSpellDefense"),
        ("Spell damage", "power = TRUNC(mindPower*10 * spellPowerVsM or spellPowerPvP); Standcast then Magic Charge multiply it (each rounded); less the spell defense; below 1 the target resists, no damage. damage = low + TRUNC((high-low) * power / 1000), growing past 1000. A spell with ArmorPiercing then goes through CalculateDamageCust like a swing (armor as a swing's, roll, damageMod), even from a monster. Mind Blast multiplies by (casterInt - targetInt) / 1000", "SpellDef.GetSpellPowerAgainstChar, GetMindPowerMultiplier, DamageSpellDef.CalculateDamage, CombatCalculator.CalculateSpellArmorEffect, MindBlastSpellDef"),
        ("Resist modifier (every damage source)", "resists permille, clamped to -40000..1000. phys = TRUNC((1000*count - sum of its physical subtypes' resists) / count), then TRUNC((1000-ResistPhysical)*phys/1000); magic likewise with ResistMagic; both kinds: TRUNC((phys+magic)/2); summon damage: TRUNC((1000-ResistSummon)*factor/1000); modifier = factor/1000. No subtype at all: 1", "DamageManager.GetResistModifier"),
        ("Final swing damage", "R(MAX(0, damage * modifier)); then, unless modifier is 0: a player's bare hands (not a Mystic): 1 if that is 0, otherwise exactly INT(wrestling/200)+1; a monster on a monster: below 1 does 5, unless summon damage", "DamageManager.CauseDamage, ApplyMinimalDamage"),
        ("Swing time", "rate = R(root * weapSpeed * weaponSpeedGlobal), 50 if that is not positive; hitspeed = MAX(INT(0xfffff/rate),1); seconds = 2 * hitspeed / swingTicksPerSecond", "CombatCalculator.CalculateSwingDelay"),
        ("Swing root and speed", "Player: sqrt_int(Dex)-2, weapon's speed. Mystic: sqrt_int(MAX(INT(mana/2),1)), mysticWeaponSpeedModifier. NPC: sqrt_int(INT(NpcSpeed/2)), or max stamina, weaponSpeedNPC*1000", "CombatCalculator.CalculateCombatWeaponValues"),
        ("sqrt_int in Excel", "Excel has no sqrt_int: the workbook uses ROUND(100*SQRT(x),0). Erebor's is an integer Newton iteration, usually the same and at most 1 off, which rarely moves hitspeed by a tick", "CombatCalculator.SqrtInt"),
        ("Hit roll", "attack = TRUNC((skill+tactics)/2); defend = TRUNC((target tactics + target stamina*10)/2); limit = TRUNC((skill - TRUNC((attack-defend)/5))/10), 0-100; difficulty = rand(limit); 0 always hits, otherwise CheckSuccess(skill, difficulty*10). Chance rows sum that over every difficulty", "WeaponSkillDef.RollHit, CombatCalculator.CalculateHitDifficultyLimit, SkillUtils.CheckSuccess"),
        ("Damage type of a swing", "Player: swords and axes slash, fencing weapons are sharp, maces and bare hands blunt, bows and crossbows stab, war fork stabs, scimitar and executioner's axe slash. Monster: NaturalDamageType wins; without one by weapon, swords/axes Sharp|Slashing, fencing Sharp|Stabbing, bare hands Blunt", "CombatCalculator.GetWeaponDamageType, GetDamageTypeOfWeapon"),
        ("Weapon durability scaling", "effective AttackVsP/VsM = (TypeDef.AttackVsP/2) + (TypeDef.AttackVsP/2) * (Durability/MaxDurability) - i.e. half value at 0 durability, full value at full durability. Nothing lowers durability yet", "Weapon.AttackVsP"),
    ]
    row = header_row(ws, row, 1, ["Formula", "Exact expression", "Source"])
    for r in formula_texts:
        ws.cell(row=row, column=1, value=r[0]).alignment = WRAP
        ws.cell(row=row, column=2, value=r[1]).alignment = WRAP
        ws.cell(row=row, column=3, value=r[2]).alignment = WRAP
        row += 1
    row += 1

    row = h2(ws, row, 1, "Worked examples - these formulas against Tests/EreborDamageFormulaTests.cs", span=4)
    row = header_row(ws, row, 1, ["Case", "This workbook's formula", "Test expects"])
    examples = [
        ("damagecust: attack 49, armor 40, piercing 250, roll 1.0, damageMod 1.35", damage_cust_expr(49, 40, 250, 1.0, "C_swingDamageVsM"), 26),
        ("damagecust: attack 75, armor 80, piercing 250, roll 1.0, 1.35 (mithril long sword, titan)", damage_cust_expr(75, 80, 250, 1.0, "C_swingDamageVsM"), 20),
        ("damagecust: attack 49, armor 80: never a heal", damage_cust_expr(49, 80, 250, 1.0, "C_swingDamageVsM"), 0),
        ("damagecust: attack 49, armor 40, high armor roll 1.05", damage_cust_expr(49, 40, 250, 1.05, "C_swingDamageVsM"), 24),
        ("damagecust: attack 50, armor 28, piercing 0, 0.9 (PvP)", damage_cust_expr(50, 28, 0, 1.0, "C_swingDamagePvP"), 20),
        ("damagecust_MvP: attack 80, armor 65, factor 1.0", damage_mvp_expr(80, 65, 0, 1.0), 54),
        ("damagecust_MvP: attack 80, armor 65, factor 0.85", damage_mvp_expr(80, 65, 0, 0.85), 46),
        ("damagecust_MvP: attack 80, armor 65, piercing 500", damage_mvp_expr(80, 65, 500, 1.0), 67),
        ("damagecust_MvP: attack 80, armor 200 stops it", damage_mvp_expr(80, 200, 0, 1.0), 0),
        ("hitspeed: Dex 100, speed 76", hitspeed_expr(player_root_expr(100), 76), 13),
        ("hitspeed: Dex 100, bare hands speed 200", hitspeed_expr(player_root_expr(100), "C_bareHandsSpeed"), 5),
        ("hitspeed: Mystic with 100 mana", hitspeed_expr(mystic_root_expr(100), "C_mysticWeaponSpeedModifier"), 24),
        ("swing seconds: hitspeed 13 at 9.14 ticks/s", swing_seconds_expr(13), round(26 / 9.14, 6)),
        ("hit limit: skill 1000, tactics 1000, target 1000 tactics, 10 stamina", hit_limit_expr(1000, 1000, 1000, 10), 91),
        ("hit limit: skill 500, tactics 500, target 0/0", hit_limit_expr(500, 500, 0, 0), 40),
    ]
    example_start = row
    for label, formula, expected in examples:
        ws.cell(row=row, column=1, value=label).alignment = WRAP
        output_cell(ws, row, 2, "=" + formula)
        ws.cell(row=row, column=3, value=expected)
        row += 1
    row = note(ws, row, 1, "The tests feed damagecust its attack directly. Through the whole rounding chain of "
                            "DamageManager.GetWeaponSwingArgs an iron long sword (33) at GM skills and Str 100 has "
                            "weapon attack 28, then R(28*1.1) = 31, R(31*1.6) = 50, so a calculator tab shows 27 vs armor "
                            "40, not the 26 of attack 49. Attack 49 is 33*0.851*1.76 without the steps.", span=6)
    row += 1

    row = h2(ws, row, 1, "Resist / damage types (16 real Resist* stats, all through GetResistModifier above)", span=3)
    row = header_row(ws, row, 1, ["Character stat", "Category", "Who/what actually uses it"])
    for r in RESIST_TYPES:
        row = data_rows(ws, row, 1, [r])
    row = note(ws, row, 1, "Resist values are permille, as in the game: 150 = 15%, negative is a weakness, clamped to "
                            "-40000..1000. Monsters' are on Monsters Reference, from their .defs. Source: scripts/Basic/Enums.cs "
                            "(DamageType), DamageManager.GetResistModifier (Erebor's handleresists).", span=6)

    # Helper list for hit_chance_expr, out of the way of the tables
    helper_col = 8
    ws.cell(row=3, column=helper_col, value="Hit roll difficulty (helper)").font = HEADER_FONT
    for d in range(1, 100):
        ws.cell(row=3 + d, column=helper_col, value=d)
    wb.defined_names["H_HitRollD"] = _defined_range(ws, "H_HitRollD", helper_col, 4, 102)

    autosize(ws, {1: 30, 2: 46, 3: 46, 4: 30, 5: 40, 8: 12})
    return const_name_to_cell


def build_weapons_ref(wb):
    ws = wb.create_sheet("Weapons Reference")
    row = h1(ws, 1, 1, "Weapons Reference", span=12)
    row = note(ws, row, 1,
               "Read from scripts/converted/zbrane/*.def when this workbook is generated. attackVsP == attackVsM for every "
               "weapon: Erebor's nastaveni_weapdam_<defname>. The real Player/Monster split happens via the global "
               "multipliers on the Core Formulas tab.", span=12)
    row += 1
    row = note(ws, row, 1,
               "Speed and piercing vary by weapon, not by material (nastaveni_weapspeed_<model>, "
               "nastaveni_weappiercing_<model>). range=1, strikeStartRange=5, strikeStopRange=10 are uniform on every "
               "melee weapon - not shown. Damage type is what a player's swing with it does (GetDamageTypeOfWeapon); "
               "Resist stat is the subtype resist that meets it.", span=12)
    row += 1
    header_r = row
    mindpower_col0 = 14  # N - leave M (13) as a spacer column between the two tables
    info_col0 = mindpower_col0 + len(MATERIAL_NAMES_FOR_WEAPONS) + 1  # V - U is a spacer
    headers = ["Archetype", "WeaponType", "Skill", "MaterialType", "Notes"] + MATERIAL_NAMES_FOR_WEAPONS
    row = header_row(ws, row, 1, headers)
    ws.cell(row=header_r, column=mindpower_col0 - 1, value="")
    for i, mat in enumerate(MATERIAL_NAMES_FOR_WEAPONS):
        c = ws.cell(row=header_r, column=mindpower_col0 + i, value=f"MindPower: {mat}")
        c.font = HEADER_FONT
        c.fill = HEADER_FILL
    header_row(ws, header_r, info_col0, ["Speed", "Piercing", "Damage type", "Resist stat"])
    data_start = row
    for arche, disp, wtype, skill, mtype, wnote in WEAPON_ARCHETYPES:
        ws.cell(row=row, column=1, value=arche)
        ws.cell(row=row, column=2, value=wtype)
        ws.cell(row=row, column=3, value=skill)
        ws.cell(row=row, column=4, value=mtype)
        ws.cell(row=row, column=5, value=wnote)
        for i, v in enumerate(WEAPON_ATTACK[arche]):
            ws.cell(row=row, column=6 + i, value=v)
        for i, v in enumerate(WEAPON_MINDPOWER[arche]):
            ws.cell(row=row, column=mindpower_col0 + i, value=v)
        ws.cell(row=row, column=info_col0, value=WEAPON_SPEED[arche])
        ws.cell(row=row, column=info_col0 + 1, value=WEAPON_PIERCING[arche])
        ws.cell(row=row, column=info_col0 + 2, value=WEAPON_DAMAGE[arche])
        ws.cell(row=row, column=info_col0 + 3, value=resist_of_flags(WEAPON_DAMAGE[arche]))
        row += 1
    data_end = row - 1
    row += 1
    row = note(ws, row, 1, "Source: scripts/converted/zbrane/*.def (one file per material), generated by the converter from "
                            "Erebor's save/sphereworld.scp. How to change: fix it in the archive or the converter and "
                            "regenerate the defs, never by hand; for a live test use the GM .settings mass editors.", span=12)
    row += 1
    row = note(ws, row, 1,
               "MindPower columns: a mace, hammer or club has #539's scaling off the bare-hands 700, a staff Erebor's "
               "nastaveni_staffSMbonus. '-1' means content never sets it there; Weapon.CalculateMPDurabilty treats that "
               "as 'unset' and a Mystic or Mage wielding one still gets the flat bare-hands 700 regardless of material "
               "(see Mystic tab).", span=12)
    row += 1
    row = h2(ws, row, 1, "Ammo (multiplies a bow's or crossbow's attack, piercing adds to the weapon's)", span=6)
    row = header_row(ws, row, 1, ["Family", "Material", "Key", "ProjectileType", "Multiplier", "Piercing"])
    ammo_start = row
    for family, material, ptype, multiplier, piercing in AMMO:
        row = data_rows(ws, row, 1, [[family, material, f"{family}_{material}", ptype, multiplier, piercing]])
    ammo_end = row - 1
    row = note(ws, row, 1, "Source: scripts/converted/zbrane/ammo.def, Projectile.DamageMultiplier and Piercing "
                            "(nastaveni_arrowdamage_<tdata1>, <tdata2>). On Erebor too, adamantinum and mithril jagged "
                            "bolts really have a multiplier of 0. Bows shoot Arrow ammo, crossbows Bolt. The poison "
                            "variants' poison isn't modeled here.", span=6)

    autosize(ws, {1: 16, 2: 14, 3: 14, 4: 12, 5: 50})
    for c in range(6, 13):
        ws.column_dimensions[get_column_letter(c)].width = 11
    for c in range(mindpower_col0, mindpower_col0 + len(MATERIAL_NAMES_FOR_WEAPONS)):
        ws.column_dimensions[get_column_letter(c)].width = 14
    for c in range(info_col0, info_col0 + 4):
        ws.column_dimensions[get_column_letter(c)].width = 16
    return dict(header_row=header_r, data_start=data_start, data_end=data_end, mindpower_col=get_column_letter(mindpower_col0),
                speed_col=get_column_letter(info_col0), piercing_col=get_column_letter(info_col0 + 1),
                damage_col=get_column_letter(info_col0 + 2), resist_col=get_column_letter(info_col0 + 3),
                ammo_start=ammo_start, ammo_end=ammo_end)


def build_armor_ref(wb):
    ws = wb.create_sheet("Armor Reference")
    row = h1(ws, 1, 1, "Armor Reference", span=14)
    row = note(ws, row, 1,
               "Read from scripts/converted/brneni/*.def when this workbook is generated. armorVsP == armorVsM and "
               "mindDefenseVsP == mindDefenseVsM for every piece and material (checked when generating).", span=14)
    row += 1
    row = note(ws, row, 1,
               "Armor and mind defense are separate numbers now: armor is Erebor's nastaveni_armorarmor_<metal>_<type> "
               "for the whole set, mind defense nastaveni_armorparts_OM_<defname>. Gold's are 0 on Erebor too, except one "
               "helmet. Mind defense counts against spells and Mystic strikes (see Core Formulas + Mystic tab).", span=14)
    row += 1
    header_r = row
    mind_col0 = 5 + len(MATERIAL_NAMES_FOR_ARMOR) + 1  # O - N is a spacer
    headers = ["Piece", "Layer", "Type", "Shield?"] + MATERIAL_NAMES_FOR_ARMOR
    row = header_row(ws, row, 1, headers)
    header_row(ws, header_r, mind_col0, [f"MindDef: {m}" for m in MATERIAL_NAMES_FOR_ARMOR])
    data_start = row
    for name, layer, wtype, vals, is_shield, mind_vals in ARMOR_PIECES:
        ws.cell(row=row, column=1, value=name)
        ws.cell(row=row, column=2, value=layer)
        ws.cell(row=row, column=3, value=wtype)
        ws.cell(row=row, column=4, value="Yes" if is_shield else "No")
        for i, v in enumerate(vals):
            ws.cell(row=row, column=5 + i, value=v)
        for i, v in enumerate(mind_vals):
            ws.cell(row=row, column=mind_col0 + i, value=v)
        row += 1
    data_end = row - 1
    row += 1
    row = note(ws, row, 1, "Source: scripts/converted/brneni/*.def (one file per material), generated by the converter from "
                            "Erebor's save/sphereworld.scp. How to change: fix it in the archive or the converter and "
                            "regenerate the defs, never by hand. Durability scaling (Wearable) applies on top at runtime: "
                            "effective value = (TypeDef value / 2) + (TypeDef value / 2) * (Durability/MaxDurability) - a "
                            "damaged piece gives proportionally less, and fights wear items down (see combat.md).", span=14)
    row += 1
    row = note(ws, row, 1, "Across a full outfit the best piece per body part counts, then the part weights - see Core "
                            "Formulas tab's Body-part table. A full set of one armor counts exactly its number: iron plate "
                            "is 65. This tab shows each PIECE's own raw value; the profession calculators take the "
                            "defender's total armor as a single input, the Fight Simulator builds it from an outfit. "
                            "Leather pieces have no material tiers and aren't listed here.", span=14)

    autosize(ws, {1: 28, 2: 8, 3: 12, 4: 9})
    for c in range(5, 14):
        ws.column_dimensions[get_column_letter(c)].width = 13
    for c in range(mind_col0, mind_col0 + len(MATERIAL_NAMES_FOR_ARMOR)):
        ws.column_dimensions[get_column_letter(c)].width = 15
    return dict(header_row=header_r, data_start=data_start, data_end=data_end, mind_col0=mind_col0)


def build_monsters_ref(wb):
    ws = wb.create_sheet("Monsters Reference")
    row = h1(ws, 1, 1, "Monsters Reference", span=8)
    row = note(ws, row, 1,
               f"{len(MONSTERS)} distinct creatures, read from scripts/converted/chars/*.def when this workbook is generated "
               "(vendors, guards, mounts, teachers and groups excluded as non-monster content). NPCDefs with the same name "
               "and the same numbers are one row; others of that name show their defname. Where a def gives a range, Min "
               "and Max differ, and the profession tabs' monster-picker uses the AVERAGE - look at the .def for one "
               "specific spawn. Skills and resists show the range's average.", span=8)
    row += 1
    row = note(ws, row, 1,
               "A player's swing reads ArmorVsP, a monster's ArmorVsM. Worn armor adds to these. HP and stamina default to "
               f"{MONSTER_DEFAULTS['max_hits']} when a def doesn't set them (NPC.ct). Swing stamina is NpcSpeed if the def has "
               "it, otherwise max stamina: the root of the swing time (NPC.SwingStamina). Attack skill is the highest of "
               "the def's Wrestling/Swords/Fencing/Macing/Archery/Marksmanship: the real one depends on what it holds. "
               f"Damage type is NaturalDamageType, {MONSTER_DEFAULT_DAMAGE} (bare hands) without one, though a weapon it "
               "holds would decide then. Weapon piercing is 0, no chardef sets it. Mind defense is INT(MagicResist/10), "
               "mind power is EvalInt.", span=8)
    row += 1
    header_r = row
    damage_col0 = 19  # S, the resists follow it
    row = header_row(ws, row, 1, ["Name", "Category", "WeaponAttack Min", "WeaponAttack Max",
                                   "ArmorVsP Min", "ArmorVsP Max", "ArmorVsM Min", "ArmorVsM Max",
                                   "MaxHits Min", "MaxHits Max", "MaxStam Min", "MaxStam Max", "Swing stamina",
                                   "Attack skill", "Tactics", "Int", "EvalInt", "MagicResist"]
                     + ["Damage type"] + [r[0] for r in RESIST_TYPES])
    data_start = row
    data_rows(ws, row, 1, [["(manual)", "(manual)", 0, 0, 0, 0, 0, 0, MONSTER_DEFAULTS["max_hits"], MONSTER_DEFAULTS["max_hits"],
                            MONSTER_DEFAULTS["max_stam"], MONSTER_DEFAULTS["max_stam"], MONSTER_DEFAULTS["max_stam"], 0, 0, 0, 0, 0,
                            MONSTER_DEFAULT_DAMAGE] + [0] * len(RESIST_TYPES)])
    row += 1
    for name, category, s in MONSTERS:
        data_rows(ws, row, 1, [[name, category, s["attack"][0], s["attack"][1], s["armor_p"][0], s["armor_p"][1],
                                s["armor_m"][0], s["armor_m"][1], s["hits"][0], s["hits"][1], s["stam"][0], s["stam"][1],
                                s["swing_stam"], s["skill"], s["tactics"], s["intel"], s["evalint"], s["resist"],
                                s["damage"]] + list(s["resists"])])
        row += 1
    data_end = row - 1
    row += 1
    row = note(ws, row, 1, "Source: scripts/converted/chars/*.def. How to change: fix it in the archive or the converter "
                            "and regenerate the defs, never by hand; for one spawn use a GM command on the instance.", span=8)
    autosize(ws, {1: 34, 2: 22})
    for c in range(3, damage_col0 + len(RESIST_TYPES) + 1):
        ws.column_dimensions[get_column_letter(c)].width = 15
    last_col = get_column_letter(damage_col0 + len(RESIST_TYPES))
    return dict(header_row=header_r, data_start=data_start, data_end=data_end, last_col=last_col)


MELEE_ARCHETYPES = [w for w in WEAPON_ARCHETYPES if w[2] not in ("Bow", "XBow")]
RANGED_ARCHETYPES = [w for w in WEAPON_ARCHETYPES if w[2] in ("Bow", "XBow")]
RANGED_WEAPONS = {"Bow": "bow", "LightCrossbow": "crossbow", "HeavyCrossbow": "crossbow_heavy"}


def monster_match(mi, picker_cell):
    return f"MATCH({picker_cell},'Monsters Reference'!$A${mi['data_start']}:$A${mi['data_end']},0)"


def monster_column_expr(mi, match, column):
    return f"INDEX('Monsters Reference'!${column}${mi['data_start']}:${column}${mi['data_end']},{match})"


def monster_average_expr(mi, match, col_min, col_max):
    return f"AVERAGE({monster_column_expr(mi, match, col_min)},{monster_column_expr(mi, match, col_max)})"


def monster_resist_expr(mi, match, resist_name):
    """A resist column of Monsters Reference by name; resist_name is a formula text or a cell."""
    return (f"IFERROR(INDEX('Monsters Reference'!$A${mi['data_start']}:${mi['last_col']}${mi['data_end']},{match},"
            f"MATCH({resist_name},'Monsters Reference'!$A${mi['header_row']}:${mi['last_col']}${mi['header_row']},0)),0)")


def add_monster_picker(ws, row, mi, kind, manual_cells, prefix, group_resist='"ResistPhysical"', subtype_resist=None):
    """Adds a monster-name dropdown (sourced from the Monsters Reference sheet, '(manual)' as the
    first/default option) plus one resolved output cell per stat, the catalog's when a monster is
    picked, the corresponding manual_cells entry on '(manual)'. kind='defense' resolves what the
    target needs, kind='offense' what the attacker needs. Returns (new_row, {stat_name: cell})."""
    ws.cell(row=row, column=1,
            value=f"{prefix}: pick a cataloged monster (optional, overrides manual values below)")
    ws.cell(row=row, column=2, value="(manual)").fill = INPUT_FILL
    picker_cell = f"$B${row}"
    dv = DataValidation(type="list",
                         formula1=f"='Monsters Reference'!$A${mi['data_start']}:$A${mi['data_end']}")
    ws.add_data_validation(dv)
    dv.add(ws.cell(row=row, column=2))
    row += 1
    match = monster_match(mi, picker_cell)

    resolved = {}

    def add_resolved(key, label, expr):
        nonlocal row
        ws.cell(row=row, column=1, value=f"{prefix}: resolved {label}")
        output_cell(ws, row, 2, f'=IF({picker_cell}="(manual)",{manual_cells[key]},{expr})')
        resolved[key] = f"$B${row}"
        row += 1

    if kind == "defense":
        add_resolved("armor", "ArmorVsP (a player's swing reads VsP)", monster_average_expr(mi, match, "E", "F"))
        add_resolved("hp", "HP", monster_average_expr(mi, match, "I", "J"))
        add_resolved("group", "group resist", monster_resist_expr(mi, match, group_resist))
        add_resolved("subtype", "subtype resist", monster_resist_expr(mi, match, subtype_resist))
        add_resolved("tactics", "Tactics", monster_column_expr(mi, match, "O"))
        add_resolved("stamina", "Stamina", monster_average_expr(mi, match, "K", "L"))
        if "magic_resist" in manual_cells:
            add_resolved("magic_resist", "MagicResist", monster_column_expr(mi, match, "R"))
            add_resolved("int", "Int", monster_column_expr(mi, match, "P"))
    else:
        add_resolved("weapon_attack", "WeaponAttack", monster_average_expr(mi, match, "C", "D"))
        add_resolved("swing_stamina", "swing stamina", monster_column_expr(mi, match, "M"))
        add_resolved("skill", "attack skill", monster_column_expr(mi, match, "N"))
        add_resolved("tactics", "Tactics", monster_column_expr(mi, match, "O"))
        ws.cell(row=row, column=1, value=f"{prefix}: hitspeed (ticks)")
        output_cell(ws, row, 2, "=" + hitspeed_expr(npc_root_expr(resolved["swing_stamina"]), "(C_weaponSpeedNPC*1000)"))
        hitspeed_row = row
        row += 1
        ws.cell(row=row, column=1, value=f"{prefix}: swing seconds")
        output_cell(ws, row, 2, "=" + swing_seconds_expr(f"$B${hitspeed_row}"))
        resolved["delay"] = f"$B${row}"
        row += 1
        ws.cell(row=row, column=1, value=f"{prefix}: damage type (type your resists to it above)")
        output_cell(ws, row, 2, f'=IF({picker_cell}="(manual)","-",{monster_column_expr(mi, match, "S")})')
        row += 1
    return row, resolved


def _profession_header(ws, profession):
    row = h1(ws, 1, 1, profession, span=6)
    stats = PROFESSION_STATS[profession]
    row = note(ws, row, 1,
               f"Stat ranges (min/avg/max): Str {stats['str_r']}, Dex {stats['dex_r']}, Int {stats['int_r']}, "
               f"Vit {stats['vit_r']}. Allowed weapons: {stats['weapons']}. Allowed armor: {stats['armor']}. "
               f"PvPArmorChange {stats['armor_change']}, PvPMindDefenseChange {stats['mind_change']}. "
               f"Source: {stats['source']}.", span=8)
    row += 1
    return row


def add_inputs(ws, row, labels, cells):
    for label, default in labels:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    return row


def add_list(ws, cell, formula):
    dv = DataValidation(type="list", formula1=formula)
    ws.add_data_validation(dv)
    dv.add(ws[cell])


def add_output(ws, row, label, formula):
    ws.cell(row=row, column=1, value=label)
    output_cell(ws, row, 2, "=" + formula)
    return f"$B${row}"


def weapon_list_formula(wi, archetypes):
    """Dropdown of a contiguous slice of Weapons Reference: keeps clear of Excel's 255 character
    limit on a literal list."""
    first = wi["data_start"] + WEAPON_NAMES.index(archetypes[0][0])
    last = wi["data_start"] + WEAPON_NAMES.index(archetypes[-1][0])
    return f"='Weapons Reference'!$A${first}:$A${last}"


def add_swing_results(ws, row, s):
    """The Results table shared by the weapon tabs, one column for PvM, PvP and the three MvP rolls.
    s holds the cells the calculator above resolved."""
    row = h2(ws, row, 1, "Results", span=6)
    row = header_row(ws, row, 1, ["", "PvM", "PvP", "MvP (Min)", "MvP (Avg)", "MvP (Max)"])

    r_attack = row
    ws.cell(row=row, column=1, value="Attack value (pre-armor)")
    output_cell(ws, row, 2, "=" + attack_vs_monster_expr(s["weapon_attack"], s["level"]))
    output_cell(ws, row, 3, "=" + attack_vs_player_expr(s["weapon_attack"]))
    for col in (4, 5, 6):
        output_cell(ws, row, col, f"=ROUND({s['mvp']['weapon_attack']}*C_weapAttackM,0)")
    row += 1

    r_ac = row
    ws.cell(row=row, column=1, value="Armor effective")
    output_cell(ws, row, 2, f"={s['pvm']['armor']}*C_armorClassM")
    output_cell(ws, row, 3, "=" + pvp_armor_expr(s["pvp_armor"], s["pvp_change"]))
    for col in (4, 5, 6):
        output_cell(ws, row, col, f"={s['your_armor']}")
    row += 1

    r_pierce = row
    ws.cell(row=row, column=1, value="Piercing (permille)")
    output_cell(ws, row, 2, f"={s['piercing']}")
    output_cell(ws, row, 3, f"={s['piercing']}")
    for col in (4, 5, 6):
        output_cell(ws, row, col, f"={s['mvp_piercing']}")
    row += 1

    r_roll = row
    ws.cell(row=row, column=1, value="Armor roll (PvM/PvP, average) / random factor (MvP)")
    output_cell(ws, row, 2, "=AVERAGE(C_armorRandEffectMin,C_armorRandEffectMax)")
    output_cell(ws, row, 3, "=AVERAGE(C_armorRandEffectMin,C_armorRandEffectMax)")
    output_cell(ws, row, 4, "=C_swingDamageRandMvPMin")
    output_cell(ws, row, 5, "=AVERAGE(C_swingDamageRandMvPMin,C_swingDamageRandMvPMax)")
    output_cell(ws, row, 6, "=C_swingDamageRandMvPMax")
    row += 1

    r_mod = row
    ws.cell(row=row, column=1, value="damageMod (MvP's multiplies the attack, before armor)")
    output_cell(ws, row, 2, "=C_swingDamageVsM")
    output_cell(ws, row, 3, "=C_swingDamagePvP")
    for col in (4, 5, 6):
        output_cell(ws, row, col, "=C_swingDamageM")
    row += 1

    r_afterac = row
    ws.cell(row=row, column=1, value="Damage after armor")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, "=" + damage_cust_expr(f"{cl}{r_attack}", f"{cl}{r_ac}", f"{cl}{r_pierce}", f"{cl}{r_roll}", f"{cl}{r_mod}"))
    for col in (4, 5, 6):
        cl = get_column_letter(col)
        output_cell(ws, row, col, "=" + damage_mvp_expr(f"{cl}{r_attack}", f"{cl}{r_ac}", f"{cl}{r_pierce}", f"{cl}{r_roll}"))
    row += 1

    r_resist = row
    ws.cell(row=row, column=1, value="Resist modifier")
    output_cell(ws, row, 2, "=" + resist_modifier_expr(s["pvm"]["group"], s["pvm"]["subtype"]))
    output_cell(ws, row, 3, "=" + resist_modifier_expr(s["pvp_group"], s["pvp_subtype"]))
    for col in (4, 5, 6):
        output_cell(ws, row, col, "=" + resist_modifier_expr(s["your_group"], s["your_subtype"]))
    row += 1

    r_final = row
    ws.cell(row=row, column=1, value="Final damage per hit")
    for col in (2, 3, 4, 5, 6):
        cl = get_column_letter(col)
        final = f"ROUND(MAX(0,{cl}{r_afterac}*{cl}{r_resist}),0)"
        if (col < 4) and s.get("bare_hands"):
            final = (f"IF(AND({s['bare_hands']},{cl}{r_resist}>0),"
                     f"IF({final}=0,1,INT({s['skill']}/200)+1),{final})")
        output_cell(ws, row, col, "=" + final)
    row += 1

    r_delay = row
    ws.cell(row=row, column=1, value="Delay between hits (seconds)")
    output_cell(ws, row, 2, f"={s['delay']}")
    output_cell(ws, row, 3, f"={s['delay']}")
    for col in (4, 5, 6):
        output_cell(ws, row, col, f"={s['mvp']['delay']}")
    row += 1

    r_limit = row
    ws.cell(row=row, column=1, value="Hit roll limit (0-100)")
    output_cell(ws, row, 2, "=" + hit_limit_expr(s["skill"], s["tactics"], s["pvm"]["tactics"], s["pvm"]["stamina"]))
    output_cell(ws, row, 3, "=" + hit_limit_expr(s["skill"], s["tactics"], s["pvp_tactics"], s["pvp_stamina"]))
    for col in (4, 5, 6):
        output_cell(ws, row, col, "=" + hit_limit_expr(s["mvp"]["skill"], s["mvp"]["tactics"], s["tactics"], s["your_stamina"]))
    row += 1

    r_hit = row
    ws.cell(row=row, column=1, value="Hit chance")
    output_cell(ws, row, 2, "=" + hit_chance_expr(s["skill"], f"B{r_limit}"))
    output_cell(ws, row, 3, "=" + hit_chance_expr(s["skill"], f"C{r_limit}"))
    for col in (4, 5, 6):
        output_cell(ws, row, col, "=" + hit_chance_expr(s["mvp"]["skill"], f"{get_column_letter(col)}{r_limit}"))
    row += 1

    r_dps = row
    ws.cell(row=row, column=1, value="DPS, if every swing hits")
    for col in (2, 3, 4, 5, 6):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"={cl}{r_final}/{cl}{r_delay}")
    row += 1

    r_edps = row
    ws.cell(row=row, column=1, value="Expected DPS (with hit chance)")
    for col in (2, 3, 4, 5, 6):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"={cl}{r_dps}*{cl}{r_hit}")
    row += 1

    r_hp = row
    ws.cell(row=row, column=1, value="Target HP")
    output_cell(ws, row, 2, f"={s['pvm']['hp']}")
    output_cell(ws, row, 3, f"={s['pvp_hp']}")
    for col in (4, 5, 6):
        output_cell(ws, row, col, f"={s['your_hp']}")
    row += 1

    ws.cell(row=row, column=1, value="Time to kill (seconds)")
    for col in (2, 3, 4, 5, 6):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=IF({cl}{r_edps}=0,\"never\",{cl}{r_hp}/{cl}{r_edps})")
    row += 2
    return row


def add_target_blocks(ws, row, cells, monsters_info, resist_label, resist_cell):
    """The PvM, PvP and MvP input blocks of a weapon tab. resist_label names the subtype inputs,
    resist_cell holds the Resist stat of the weapon. Returns (row, pvm, mvp): the pickers' cells."""
    row = h2(ws, row, 1, "PvM target - a Monster you're attacking", span=2)
    row = add_inputs(ws, row, [("Monster ArmorVsP", 0), ("Monster ResistPhysical (permille)", 0),
                               (f"Monster {resist_label} (permille)", 0), ("Monster HP", 100),
                               ("Monster Tactics", 0), ("Monster Stamina", 100)], cells)
    row, pvm = add_monster_picker(ws, row, monsters_info, "defense",
                                  {"armor": cells["Monster ArmorVsP"], "hp": cells["Monster HP"],
                                   "group": cells["Monster ResistPhysical (permille)"],
                                   "subtype": cells[f"Monster {resist_label} (permille)"],
                                   "tactics": cells["Monster Tactics"], "stamina": cells["Monster Stamina"]},
                                  "PvM target", subtype_resist=resist_cell)
    row += 1

    row = h2(ws, row, 1, "PvP target - another Player you're attacking", span=2)
    row = add_inputs(ws, row, [("Enemy ArmorClassVsP", 0), ("Enemy profession", "Warrior"),
                               ("Enemy ResistPhysical (permille)", 0), (f"Enemy {resist_label} (permille)", 0),
                               ("Enemy HP", 100), ("Enemy Tactics", 1000), ("Enemy Stamina", 100)], cells)
    add_list(ws, cells["Enemy profession"], "=T_ProfessionName")
    row += 1

    row = h2(ws, row, 1, "MvP - a Monster attacking YOU", span=2)
    row = add_inputs(ws, row, [("Monster attacker WeaponAttack (flat)", 10), ("Monster attacker WeaponPiercing", 0),
                               ("Monster attacker swing stamina (NpcSpeed or MaxStam)", 100),
                               ("Monster attacker attack skill", 0), ("Monster attacker Tactics", 0),
                               ("Your ArmorClassVsM", 0), ("Your ResistPhysical (permille)", 0),
                               ("Your Resist<monster's damage subtype> (permille)", 0), ("Your HP", 100),
                               ("Your Stamina", 100)], cells)
    row, mvp = add_monster_picker(ws, row, monsters_info, "offense",
                                  {"weapon_attack": cells["Monster attacker WeaponAttack (flat)"],
                                   "swing_stamina": cells["Monster attacker swing stamina (NpcSpeed or MaxStam)"],
                                   "skill": cells["Monster attacker attack skill"],
                                   "tactics": cells["Monster attacker Tactics"]},
                                  "MvP attacker")
    row = note(ws, row, 1, "Monster stats are flat NPCDef numbers with no formula behind them - pick a monster above to "
                            "auto-fill, or leave on '(manual)' to type your own.", span=8)
    row += 1
    return row, pvm, mvp


def build_melee_profession(wb, profession, weapons_info, monsters_info, ranged_note=None):
    """Warrior/Thief shape: generic melee formula, no profession-specific damage path."""
    ws = wb.create_sheet(profession)
    row = _profession_header(ws, profession)
    if ranged_note:
        row = note(ws, row, 1, ranged_note, span=8)
        row += 1

    row = h2(ws, row, 1, "Calculator", span=6)
    row = note(ws, row, 1, "Yellow = your inputs. This calculator covers MELEE weapons only (generic formula, "
                            "identical for every profession - Warrior/Thief have no unique damage path). Bow/Crossbow "
                            "add a run bow factor and ammo - see the Ranger tab.", span=8)
    row += 1

    wi = weapons_info
    row = h2(ws, row, 1, "Attacker (you)", span=2)
    cells = {}
    row = add_inputs(ws, row, [
        ("Level", 0), ("Str", 100), ("Dex", 100), ("Tactics skill (0-1000, GM=1000)", 1000),
        ("Anatomy skill (0-1000)", 1000), ("ArmsLore skill (0-1000)", 1000),
        ("Weapon skill (its Skill on Weapons Reference, 0-1000)", 1000),
        ("Weapon archetype", DEFAULT_WEAPON), ("Material", DEFAULT_MATERIAL), ("Weapon durability %", 100),
    ], cells)
    weapon_cell, material_cell = cells["Weapon archetype"], cells["Material"]
    add_list(ws, weapon_cell, weapon_list_formula(wi, MELEE_ARCHETYPES))
    add_list(ws, material_cell, f'"{",".join(MATERIAL_NAMES_FOR_WEAPONS)}"')
    row += 1

    # Resolved here, ahead of the target blocks: the monster picker needs the weapon's resist stat
    resist_cell = add_output(ws, row, "Your weapon's Resist stat (looked up)", weapon_column_lookup(wi, wi["resist_col"], weapon_cell))
    row += 2

    row, pvm, mvp = add_target_blocks(ws, row, cells, monsters_info, "Resist<narrow for your weapon>", resist_cell)
    row = note(ws, row, 1, "'Resist<narrow>' is the ONE subtype stat matching your weapon's damage type - the Resist "
                            "stat looked up above (CombatCalculator.GetDamageTypeOfWeapon).", span=8)
    row += 1

    base_attack = add_output(ws, row, "Weapon base AttackVsP/VsM (looked up)", weapon_value_lookup(wi, weapon_cell, material_cell, "F"))
    row += 1
    durab = add_output(ws, row, "Durability-scaled weapon attack", f"({base_attack}/2)+({base_attack}/2)*({cells['Weapon durability %']}/100)")
    row += 1
    skill_sum = add_output(ws, row, "Skill sum (Tactics+Anatomy+ArmsLore+Str contributions)",
                           attack_sum_expr(cells["Tactics skill (0-1000, GM=1000)"], cells["Anatomy skill (0-1000)"],
                                           cells["ArmsLore skill (0-1000)"], cells["Str"]))
    row += 1
    weapon_attack = add_output(ws, row, "Your WeaponAttackVsP/VsM (both equal, rounded)", f"ROUND({durab}*{skill_sum},0)")
    row += 1
    speed = add_output(ws, row, "Weapon speed (looked up)", weapon_column_lookup(wi, wi["speed_col"], weapon_cell))
    row += 1
    piercing = add_output(ws, row, "Weapon piercing (looked up)", weapon_column_lookup(wi, wi["piercing_col"], weapon_cell))
    row += 1
    add_output(ws, row, "Weapon damage type (looked up)", weapon_column_lookup(wi, wi["damage_col"], weapon_cell))
    row += 1
    hitspeed = add_output(ws, row, "Your hitspeed (ticks, Dex-driven)", hitspeed_expr(player_root_expr(cells["Dex"]), speed))
    row += 1
    delay = add_output(ws, row, "Your swing delay (seconds)", swing_seconds_expr(hitspeed))
    row += 2

    row = add_swing_results(ws, row, dict(
        weapon_attack=weapon_attack, level=cells["Level"], skill=cells["Weapon skill (its Skill on Weapons Reference, 0-1000)"],
        tactics=cells["Tactics skill (0-1000, GM=1000)"], piercing=piercing, delay=delay, pvm=pvm, mvp=mvp,
        bare_hands=f'{weapon_cell}="bare_hands"', pvp_armor=cells["Enemy ArmorClassVsP"],
        pvp_change=pvp_change_expr("T_ProfessionArmorChange", cells["Enemy profession"]),
        pvp_group=cells["Enemy ResistPhysical (permille)"], pvp_subtype=cells["Enemy Resist<narrow for your weapon> (permille)"],
        pvp_hp=cells["Enemy HP"], pvp_tactics=cells["Enemy Tactics"], pvp_stamina=cells["Enemy Stamina"],
        your_armor=cells["Your ArmorClassVsM"], your_group=cells["Your ResistPhysical (permille)"],
        your_subtype=cells["Your Resist<monster's damage subtype> (permille)"], your_hp=cells["Your HP"],
        your_stamina=cells["Your Stamina"], mvp_piercing=cells["Monster attacker WeaponPiercing"]))

    row = h2(ws, row, 1, "Reference matrix - all melee archetypes x materials, fixed baseline (GM skills, Str=100, "
                         "Dex=100, Level 0, unarmored Monster target, 0 resist)", span=9)
    header = ["Archetype", "WeaponType", "Skill"] + MATERIAL_NAMES_FOR_WEAPONS + ["Swing seconds"]
    row = header_row(ws, row, 1, header)
    skillsum_baseline = attack_sum_expr(1000, 1000, 1000, 100)
    for arche, disp, wtype, skill, mtype, wnote in MELEE_ARCHETYPES:
        if arche == "bare_hands":
            continue
        ws.cell(row=row, column=1, value=arche)
        ws.cell(row=row, column=2, value=wtype)
        ws.cell(row=row, column=3, value=skill)
        table_row = wi["data_start"] + WEAPON_NAMES.index(arche)
        for i, mat in enumerate(MATERIAL_NAMES_FOR_WEAPONS):
            lookup_cell = f"'Weapons Reference'!{get_column_letter(6 + i)}{table_row}"
            attack_baseline = attack_vs_monster_expr(f"ROUND({lookup_cell}*({skillsum_baseline}),0)", 0)
            ws.cell(row=row, column=4 + i, value="=" + damage_cust_expr(attack_baseline, 0, 0, 1, "C_swingDamageVsM"))
        speed_cell = f"'Weapons Reference'!{wi['speed_col']}{table_row}"
        ws.cell(row=row, column=4 + len(MATERIAL_NAMES_FOR_WEAPONS),
                value="=" + swing_seconds_expr(hitspeed_expr(player_root_expr(100), speed_cell)))
        row += 1
    row = note(ws, row, 1, "This matrix shows PvM damage-after-armor against an unarmored Monster (armor 0) at "
                            "GM skills - i.e. the raw weapon/material scaling with everything else held constant. "
                            "Use the Calculator above for your actual stats/target.", span=9)

    autosize(ws, {1: 30, 2: 18, 3: 14})
    for c in range(4, 12):
        ws.column_dimensions[get_column_letter(c)].width = 12
    ws.column_dimensions["B"].width = 16


def add_thief_ability_table(wb):
    ws = wb["Thief"]
    row = ws.max_row + 3
    row = h2(ws, row, 1, "Thief abilities - the profession's real distinguishing layer (melee formula above is fully generic)", span=4)
    row = note(ws, row, 1, "Thief is a real, selectable, playable profession (not just a design doc) - about half its "
                            "abilities/talents are genuinely working code, the other half is unimplemented talent-tree "
                            "scaffolding (Czech-comment placeholders with no def/plugin behind them). This table "
                            "corrects a prior session's stale 'Thief not implemented' note.", span=4)
    row = header_row(ws, row, 1, ["Ability", "Effect", "Implemented?", "Source"])
    for r in THIEF_ABILITIES:
        ws.cell(row=row, column=1, value=r[0])
        ws.cell(row=row, column=2, value=r[1]).alignment = WRAP
        ws.cell(row=row, column=3, value=r[2]).alignment = WRAP
        ws.cell(row=row, column=4, value=r[3])
        row += 1


def build_ranger(wb, weapons_info, monsters_info):
    ws = wb.create_sheet("Ranger")
    row = _profession_header(ws, "Ranger")
    row = note(ws, row, 1,
               "Bows and crossbows are weapons like any other since #860: attack, speed and piercing per material from "
               "the defs, RangerSettings' damage, speed and tier bonus are unused. Every bow is a run bow, attack * "
               "runBowDamage (0.82). The ammo multiplies the attack and adds its piercing. All of it stabs, so "
               "ResistStabbing meets it, not ResistArchery.", span=8)
    row += 1

    wi = weapons_info
    row = h2(ws, row, 1, "Calculator", span=6)
    row += 1
    cells = {}
    row = add_inputs(ws, row, [
        ("Level", 0), ("Str", 100), ("Dex", 100), ("Tactics skill (0-1000)", 1000), ("Anatomy skill (0-1000)", 1000),
        ("ArmsLore skill (0-1000)", 1000), ("Weapon skill (Archery/Marksmanship, 0-1000)", 1000),
        ("Weapon (Bow/LightCrossbow/HeavyCrossbow)", "Bow"), ("Weapon material", DEFAULT_MATERIAL),
        ("Weapon durability %", 100), ("Ammo family", AMMO_FAMILIES[0]), ("Ammo material", DEFAULT_MATERIAL),
    ], cells)
    weapon_choice = cells["Weapon (Bow/LightCrossbow/HeavyCrossbow)"]
    add_list(ws, weapon_choice, '"Bow,LightCrossbow,HeavyCrossbow"')
    add_list(ws, cells["Weapon material"], f'"{",".join(MATERIAL_NAMES_FOR_WEAPONS)}"')
    add_list(ws, cells["Ammo family"], f'"{",".join(AMMO_FAMILIES)}"')
    add_list(ws, cells["Ammo material"], f'"{",".join(MATERIAL_NAMES_FOR_WEAPONS)}"')
    row += 1
    ws.cell(row=row, column=1, value="Must-stand-still to attack?")
    output_cell(ws, row, 2, f'=IF({weapon_choice}="Bow","No (bowMustStandStill=false)","Yes (crossbowMustStandStill=true)")')
    row += 1
    row = note(ws, row, 1, "Moving while required to stand still simply fails that stroke (no damage, no ammo "
                            "spent) and retries in 1s - not modeled as a damage multiplier here.", span=8)
    row += 1

    arche_cell = add_output(ws, row, "Weapon archetype", f'IF({weapon_choice}="Bow","bow",IF({weapon_choice}="LightCrossbow","crossbow","crossbow_heavy"))')
    row += 1
    resist_cell = add_output(ws, row, "Your weapon's Resist stat (looked up)", weapon_column_lookup(wi, wi["resist_col"], arche_cell))
    row += 2

    row, pvm, mvp = add_target_blocks(ws, row, cells, monsters_info, "ResistStabbing", resist_cell)

    ammo_family, ammo_material = cells["Ammo family"], cells["Ammo material"]
    add_output(ws, row, "Ammo fits the weapon? (bows shoot Arrow ammo, crossbows Bolt)",
               f'IF(({ammo_lookup(wi, "D", ammo_family, ammo_material)}="Arrow")=({weapon_choice}="Bow"),"Yes","NO - this weapon can\'t fire it")')
    row += 1
    base = add_output(ws, row, "Weapon base AttackVsP/VsM (looked up)", weapon_value_lookup(wi, arche_cell, cells["Weapon material"], "F"))
    row += 1
    durab = add_output(ws, row, "Durability-scaled weapon attack", f"({base}/2)+({base}/2)*({cells['Weapon durability %']}/100)")
    row += 1
    multiplier = add_output(ws, row, "Ammo multiplier (looked up)", ammo_lookup(wi, "E", ammo_family, ammo_material))
    row += 1
    skill_sum = add_output(ws, row, "Skill sum (Tactics+Anatomy+ArmsLore+Str)",
                           attack_sum_expr(cells["Tactics skill (0-1000)"], cells["Anatomy skill (0-1000)"],
                                           cells["ArmsLore skill (0-1000)"], cells["Str"]))
    row += 1
    run_bow = add_output(ws, row, "Run bow factor", f'IF({weapon_choice}="Bow",C_runBowDamage,1)')
    row += 1
    weapon_attack = add_output(ws, row, "Your WeaponAttackVsP/VsM (both equal, rounded)",
                               f"ROUND(ROUND(ROUND({durab}*{skill_sum},0)*{run_bow},0)*{multiplier},0)")
    row += 1
    piercing = add_output(ws, row, "Piercing (weapon + ammo)",
                          f"{weapon_column_lookup(wi, wi['piercing_col'], arche_cell)}+{ammo_lookup(wi, 'F', ammo_family, ammo_material)}")
    row += 1
    speed = add_output(ws, row, "Weapon speed (looked up)", weapon_column_lookup(wi, wi["speed_col"], arche_cell))
    row += 1
    hitspeed = add_output(ws, row, "Your hitspeed (ticks, Dex-driven)", hitspeed_expr(player_root_expr(cells["Dex"]), speed))
    row += 1
    delay = add_output(ws, row, "Your swing delay (seconds)", swing_seconds_expr(hitspeed))
    row += 1
    add_output(ws, row, "Range vs monsters (tiles, flat GM setting)",
               f'IF({weapon_choice}="Bow",C_Ranges_bow,IF({weapon_choice}="LightCrossbow",C_Ranges_crossbow,C_Ranges_heavyCrossbow))')
    row += 1
    add_output(ws, row, "Range vs players (tiles)",
               f'IF({weapon_choice}="Bow",C_Ranges_bowVsPlayers,IF({weapon_choice}="LightCrossbow",C_Ranges_crossbow,C_Ranges_heavyCrossbow))')
    row += 2

    row = add_swing_results(ws, row, dict(
        weapon_attack=weapon_attack, level=cells["Level"], skill=cells["Weapon skill (Archery/Marksmanship, 0-1000)"],
        tactics=cells["Tactics skill (0-1000)"], piercing=piercing, delay=delay, pvm=pvm, mvp=mvp,
        pvp_armor=cells["Enemy ArmorClassVsP"], pvp_change=pvp_change_expr("T_ProfessionArmorChange", cells["Enemy profession"]),
        pvp_group=cells["Enemy ResistPhysical (permille)"], pvp_subtype=cells["Enemy ResistStabbing (permille)"],
        pvp_hp=cells["Enemy HP"], pvp_tactics=cells["Enemy Tactics"], pvp_stamina=cells["Enemy Stamina"],
        your_armor=cells["Your ArmorClassVsM"], your_group=cells["Your ResistPhysical (permille)"],
        your_subtype=cells["Your Resist<monster's damage subtype> (permille)"], your_hp=cells["Your HP"],
        your_stamina=cells["Your Stamina"], mvp_piercing=cells["Monster attacker WeaponPiercing"]))

    row = h2(ws, row, 1, "Reference matrix - fixed baseline (GM skills, Str=100, Dex=100, Level 0, unarmored Monster target, "
                         "plain arrow or xbolt of the same material)", span=5)
    row = header_row(ws, row, 1, ["Weapon", "Weapon material", "Ammo", "PvM damage/hit (unarmored Monster)", "Swing seconds"])
    skillsum_baseline = attack_sum_expr(1000, 1000, 1000, 100)
    for wname, arche in RANGED_WEAPONS.items():
        family = "arrow" if wname == "Bow" else "xbolt"
        run_factor = "C_runBowDamage" if wname == "Bow" else "1"
        table_row = wi["data_start"] + WEAPON_NAMES.index(arche)
        for tier_i, mat in enumerate(MATERIAL_NAMES_FOR_WEAPONS):
            ammo_row = wi["ammo_start"] + [(a[0], a[1].lower()) for a in AMMO].index((family, mat.lower()))
            attack_cell = f"'Weapons Reference'!{get_column_letter(6 + tier_i)}{table_row}"
            weapon_attack = (f"ROUND(ROUND(ROUND({attack_cell}*({skillsum_baseline}),0)*{run_factor},0)"
                             f"*'Weapons Reference'!$E${ammo_row},0)")
            ws.cell(row=row, column=1, value=wname)
            ws.cell(row=row, column=2, value=mat)
            ws.cell(row=row, column=3, value=f"{family}_{mat}")
            output_cell(ws, row, 4, "=" + damage_cust_expr(attack_vs_monster_expr(weapon_attack, 0), 0, 0, 1, "C_swingDamageVsM"))
            output_cell(ws, row, 5, "=" + swing_seconds_expr(hitspeed_expr(player_root_expr(100), f"'Weapons Reference'!{wi['speed_col']}{table_row}")))
            row += 1
    autosize(ws, {1: 40, 2: 16, 3: 22, 4: 34, 5: 14})
    ws.column_dimensions["B"].width = 16


def build_mystic(wb, weapons_info, monsters_info):
    ws = wb.create_sheet("Mystic")
    row = _profession_header(ws, "Mystic")
    row = note(ws, row, 1,
               "Mystic's swing is a separate damage path whatever the weapon: mind power against the spell defense, as "
               "cold magic (DamageManager.GetMindPowerSwingArgs). Armor doesn't matter, MagicResist and worn armor's mind "
               "defense do. Staves carry Erebor's nastaveni_staffSMbonus, maces and hammers #539's scaling, every other "
               "weapon the flat bare-hands 700 regardless of material. Not modeled: Astral Power's paralysis chance and "
               "lightning variant (see combat.md). The swing itself still rolls to hit with the weapon's skill.", span=8)
    row += 1

    wi = weapons_info
    row = h2(ws, row, 1, "Calculator - mind-power attack (PvM/PvP)", span=6)
    row += 1
    cells = {}
    row = add_inputs(ws, row, [("Level", 0), ("Int", 150), ("Mana (current)", 100),
                               ("EvalInt skill (0-1000)", 1000), ("SpiritSpeak skill (0-1000)", 1000),
                               ("Tactics skill (0-1000)", 1000), ("Weapon skill (its Skill on Weapons Reference, 0-1000)", 1000),
                               ("Weapon archetype", "staff_gnarled"), ("Material", DEFAULT_MATERIAL),
                               ("Weapon durability %", 100)], cells)
    add_list(ws, cells["Weapon archetype"], weapon_list_formula(wi, WEAPON_ARCHETYPES))
    add_list(ws, cells["Material"], f'"{",".join(MATERIAL_NAMES_FOR_WEAPONS)}"')
    row = note(ws, row, 1, "Pick any weapon archetype above (not just maces/staves) - choosing one with no real "
                            "MindPowerVsP/VsM in content (-1 on the Weapons Reference tab) correctly falls back to "
                            "the flat bare-hands default below, matching the real engine.", span=8)
    row += 1

    row = h2(ws, row, 1, "PvM target - a Monster you're attacking", span=2)
    row = add_inputs(ws, row, [("Monster MagicResist (0-1000)", 0), ("Monster ResistMagic (permille)", 0),
                               ("Monster ResistCold (permille)", 0), ("Monster HP", 100), ("Monster Tactics", 0),
                               ("Monster Stamina", 100), ("Monster ArmorVsP", 0), ("Monster Int", 0)], cells)
    row, pvm = add_monster_picker(ws, row, monsters_info, "defense",
                                  {"armor": cells["Monster ArmorVsP"], "hp": cells["Monster HP"],
                                   "group": cells["Monster ResistMagic (permille)"],
                                   "subtype": cells["Monster ResistCold (permille)"],
                                   "tactics": cells["Monster Tactics"], "stamina": cells["Monster Stamina"],
                                   "magic_resist": cells["Monster MagicResist (0-1000)"], "int": cells["Monster Int"]},
                                  "PvM target", group_resist='"ResistMagic"', subtype_resist='"ResistCold"')
    row += 1
    row = h2(ws, row, 1, "PvP target - another Player you're attacking", span=2)
    row = add_inputs(ws, row, [("Enemy MindDefenseVsP", 0), ("Enemy profession", "Warrior"), ("Enemy ResistMagic (permille)", 0),
                               ("Enemy ResistCold (permille)", 0), ("Enemy HP", 100), ("Enemy Tactics", 1000),
                               ("Enemy Stamina", 100)], cells)
    add_list(ws, cells["Enemy profession"], "=T_ProfessionName")
    row += 1
    row = h2(ws, row, 1, "MvP - a Monster physically attacking YOU (generic path - a monster's swing doesn't use mind-power)", span=2)
    row = add_inputs(ws, row, [("Monster attacker WeaponAttack (flat)", 10), ("Monster attacker WeaponPiercing", 0),
                               ("Monster attacker swing stamina (NpcSpeed or MaxStam)", 100),
                               ("Monster attacker attack skill", 0), ("Monster attacker Tactics", 0),
                               ("Your ArmorClassVsM", 0), ("Your ResistPhysical (permille)", 0),
                               ("Your Resist<monster's damage subtype> (permille)", 0), ("Your HP", 100),
                               ("Your Stamina", 100)], cells)
    row, mvp = add_monster_picker(ws, row, monsters_info, "offense",
                                  {"weapon_attack": cells["Monster attacker WeaponAttack (flat)"],
                                   "swing_stamina": cells["Monster attacker swing stamina (NpcSpeed or MaxStam)"],
                                   "skill": cells["Monster attacker attack skill"],
                                   "tactics": cells["Monster attacker Tactics"]},
                                  "MvP attacker")
    row += 2

    weapon_cell, material_cell, durability_cell = cells["Weapon archetype"], cells["Material"], cells["Weapon durability %"]
    skill_cell, tactics_cell = cells["Weapon skill (its Skill on Weapons Reference, 0-1000)"], cells["Tactics skill (0-1000)"]
    mana_cell = cells["Mana (current)"]

    mind_sum = add_output(ws, row, "Mind-power sum (EvalInt+SpiritSpeak+Int contributions)",
                          mind_sum_expr(cells["EvalInt skill (0-1000)"], cells["SpiritSpeak skill (0-1000)"], cells["Int"]))
    row += 1
    weapon_mp = add_output(ws, row, "Weapon MindPowerVsP/VsM (looked up, durability-scaled; -1=unset falls back to bare hands)",
                           weapon_mindpower_expr(wi, weapon_cell, material_cell, durability_cell))
    row += 1
    mindpower = add_output(ws, row, "Your MindPowerVsP/VsM (weapon value x mind-power sum, rounded)", f"ROUND({weapon_mp}*{mind_sum},0)")
    row += 1
    hitspeed = add_output(ws, row, "Your hitspeed (ticks, MANA-driven)", hitspeed_expr(mystic_root_expr(mana_cell), "C_mysticWeaponSpeedModifier"))
    row += 1
    delay = add_output(ws, row, "Your swing delay (seconds)", swing_seconds_expr(hitspeed))
    row += 1
    add_output(ws, row, "Mana remaining after this swing (a landed one, mysticManaLossPermille)", f"ROUND({mana_cell}*C_mysticManaLossPermille/1000,0)")
    row = note(ws, row + 1, 1, "Delay gets SLOWER as mana drains (sqrt_int(mana/2) in the rate) - this calculator "
                               "shows the delay at your CURRENT mana input; a real fight's delay drifts upward swing "
                               "by swing as mana depletes, not modeled as a full multi-swing simulation here.", span=8)
    row += 1

    row = h2(ws, row, 1, "Results - mind-power attack", span=4)
    row = header_row(ws, row, 1, ["", "PvM", "PvP"])
    r_mp = row
    ws.cell(row=row, column=1, value="Power (mind power * 10, * vs-monster or PvP multiplier)")
    level_factor = f"(C_spellPowerVsM+MIN({cells['Level']},200)*C_mysticPowerPerLevel)"
    output_cell(ws, row, 2, f"={mindpower}*10*C_mysticPowerVsM*{level_factor}")
    output_cell(ws, row, 3, f"={mindpower}*10*C_spellPowerPvP")
    row += 1
    r_md = row
    ws.cell(row=row, column=1, value="Spell defense (mind defense * 10, PvP less the profession's change)")
    output_cell(ws, row, 2, f"=INT({pvm['magic_resist']}/10)*10")
    output_cell(ws, row, 3, f"={cells['Enemy MindDefenseVsP']}*10-{pvp_change_expr('T_ProfessionMindChange', cells['Enemy profession'])}")
    row += 1
    r_aftermd = row
    ws.cell(row=row, column=1, value="Strike damage (power - defense) * mysticStrikeEffect / 1000")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=MAX(0,TRUNC((TRUNC({cl}{r_mp})-{cl}{r_md})*C_mysticStrikeEffect/1000))")
    row += 1
    r_resist = row
    ws.cell(row=row, column=1, value="Resist modifier (ResistMagic+ResistCold)")
    output_cell(ws, row, 2, "=" + resist_modifier_expr(pvm["group"], pvm["subtype"]))
    output_cell(ws, row, 3, "=" + resist_modifier_expr(cells["Enemy ResistMagic (permille)"], cells["Enemy ResistCold (permille)"]))
    row += 1
    r_final = row
    ws.cell(row=row, column=1, value="Final damage per hit")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=ROUND(MAX(0,{cl}{r_aftermd}*{cl}{r_resist}),0)")
    row += 1
    r_limit = row
    ws.cell(row=row, column=1, value="Hit roll limit (0-100)")
    output_cell(ws, row, 2, "=" + hit_limit_expr(skill_cell, tactics_cell, pvm["tactics"], pvm["stamina"]))
    output_cell(ws, row, 3, "=" + hit_limit_expr(skill_cell, tactics_cell, cells["Enemy Tactics"], cells["Enemy Stamina"]))
    row += 1
    r_hit = row
    ws.cell(row=row, column=1, value="Hit chance")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, "=" + hit_chance_expr(skill_cell, f"{cl}{r_limit}"))
    row += 1
    r_dps = row
    ws.cell(row=row, column=1, value="Expected DPS (at current mana's delay, with hit chance)")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"={cl}{r_final}/{delay}*{cl}{r_hit}")
    row += 1
    r_hp = row
    ws.cell(row=row, column=1, value="Target HP")
    output_cell(ws, row, 2, f"={pvm['hp']}")
    output_cell(ws, row, 3, f"={cells['Enemy HP']}")
    row += 1
    for col in (2, 3):
        cl = get_column_letter(col)
        ws.cell(row=row, column=1, value="Time to kill (seconds, ignoring mana-drain slowdown)")
        output_cell(ws, row, col, f"=IF({cl}{r_dps}=0,\"never\",{cl}{r_hp}/{cl}{r_dps})")
    row += 2

    row = h2(ws, row, 1, "Physical defense (MvP) - generic path, same shape as every other profession", span=6)
    row = header_row(ws, row, 1, ["", "MvP (Min)", "MvP (Avg)", "MvP (Max)"])
    r_attack = row
    ws.cell(row=row, column=1, value="Attack value (pre-armor)")
    for col in (2, 3, 4):
        output_cell(ws, row, col, f"=ROUND({mvp['weapon_attack']}*C_weapAttackM,0)")
    row += 1
    r_roll = row
    ws.cell(row=row, column=1, value="Random factor")
    output_cell(ws, row, 2, "=C_swingDamageRandMvPMin")
    output_cell(ws, row, 3, "=AVERAGE(C_swingDamageRandMvPMin,C_swingDamageRandMvPMax)")
    output_cell(ws, row, 4, "=C_swingDamageRandMvPMax")
    row += 1
    r_afterac = row
    ws.cell(row=row, column=1, value="Damage after armor")
    for col in (2, 3, 4):
        cl = get_column_letter(col)
        output_cell(ws, row, col, "=" + damage_mvp_expr(f"{cl}{r_attack}", cells["Your ArmorClassVsM"],
                                                         cells["Monster attacker WeaponPiercing"], f"{cl}{r_roll}"))
    row += 1
    r_resist2 = row
    ws.cell(row=row, column=1, value="Resist modifier")
    for col in (2, 3, 4):
        output_cell(ws, row, col, "=" + resist_modifier_expr(cells["Your ResistPhysical (permille)"],
                                                              cells["Your Resist<monster's damage subtype> (permille)"]))
    row += 1
    r_final2 = row
    ws.cell(row=row, column=1, value="Final damage per hit")
    for col in (2, 3, 4):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=ROUND(MAX(0,{cl}{r_afterac}*{cl}{r_resist2}),0)")
    row += 1
    r_limit2 = row
    ws.cell(row=row, column=1, value="Hit roll limit (0-100)")
    for col in (2, 3, 4):
        output_cell(ws, row, col, "=" + hit_limit_expr(mvp["skill"], mvp["tactics"], tactics_cell, cells["Your Stamina"]))
    row += 1
    r_hit2 = row
    ws.cell(row=row, column=1, value="Hit chance")
    for col in (2, 3, 4):
        cl = get_column_letter(col)
        output_cell(ws, row, col, "=" + hit_chance_expr(mvp["skill"], f"{cl}{r_limit2}"))
    row += 1
    r_dps2 = row
    ws.cell(row=row, column=1, value="Expected DPS (with hit chance)")
    for col in (2, 3, 4):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"={cl}{r_final2}/{mvp['delay']}*{cl}{r_hit2}")
    row += 1
    ws.cell(row=row, column=1, value="Time to kill YOU (seconds)")
    for col in (2, 3, 4):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=IF({cl}{r_dps2}=0,\"never\",{cells['Your HP']}/{cl}{r_dps2})")

    autosize(ws, {1: 44, 2: 18, 3: 18, 4: 18})


# Armor picker slots for the Fight Simulator: (zone label, Armor Reference Layer value, weight).
# "Chest & Back" combines the Chest+Back weights and "Legs & Feet" the Legs+Feet weights from Core
# Formulas' body-part table: the catalog's chest pieces sit on layer 13 and its leggings on layer 4,
# which cover both (CombatCalculator.GetBodyPartsOfLayer). Shield is handled completely separately
# (added on top, Parrying-scaled), not a body part - see Core Formulas.
ARMOR_SLOTS = [("Head", 6, 0.14), ("Neck", 10, 0.06), ("Chest & Back", 13, 0.36),
               ("Arms", 19, 0.14), ("Hands", 7, 0.07), ("Legs & Feet", 4, 0.23)]
SHIELD_PIECES = [p[0] for p in ARMOR_PIECES if p[4]]


def build_fight_simulator(wb, weapons_info, armor_info, monsters_info, spells_info):
    ws = wb.create_sheet("Fight Simulator")
    row = h1(ws, 1, 1, "Fight Simulator", span=8)
    row = note(ws, row, 1,
               "Pick ANY combatant (Player of any profession, or a cataloged Monster) as Attacker "
               "and Defender. Computes ONE direction - Attacker's real damage pipeline (melee/"
               "ranged/Mystic strike/spell/Monster swing, whichever matches what you picked) "
               "against the Defender's real mitigation. To see the reverse (Defender hits back), "
               "swap which combatant you fill into Attacker vs Defender. Rolls use their average.", span=8)
    row += 1
    row = note(ws, row, 1,
               "Armor picker simplifications (flagged, not silently assumed): one shared Material "
               "applies to a combatant's whole outfit (not per-piece); a player's own innate "
               "chardef/race base armor is assumed 0, a monster's is its catalog number plus the outfit. "
               "Mind power (Mystic and casters) reads the SAME weapon archetype/material/durability "
               "inputs below. A Monster attacker here swings; monsters' spells aren't modeled. Priest, "
               "Shaman and Necromant take the Spell path, Craftsman the Melee one.", span=8)
    row += 1

    def combatant_block(label):
        nonlocal row
        c = {}
        row = h2(ws, row, 1, f"{label}", span=2)
        ws.cell(row=row, column=1, value="Combatant type")
        input_cell(ws, row, 2, "Player")
        c["type"] = f"$B${row}"
        dv = DataValidation(type="list", formula1='"Player,Monster"')
        ws.add_data_validation(dv)
        dv.add(ws.cell(row=row, column=2))
        row += 1

        ws.cell(row=row, column=1, value="Monster (used if type=Monster)")
        input_cell(ws, row, 2, "(manual)")
        c["monster_pick"] = f"$B${row}"
        dv = DataValidation(type="list",
                             formula1=f"='Monsters Reference'!$A${monsters_info['data_start']}:$A${monsters_info['data_end']}")
        ws.add_data_validation(dv)
        dv.add(ws.cell(row=row, column=2))
        row += 1
        mi = monsters_info
        match = monster_match(mi, c["monster_pick"])
        for key, text, expr in [
            ("monster_weapon_attack", "Monster WeaponAttack (avg of catalog min/max)", monster_average_expr(mi, match, "C", "D")),
            ("monster_armor_p", "Monster ArmorVsP (avg of catalog min/max)", monster_average_expr(mi, match, "E", "F")),
            ("monster_armor_m", "Monster ArmorVsM (avg of catalog min/max)", monster_average_expr(mi, match, "G", "H")),
            ("monster_hp", "Monster HP (avg of catalog min/max)", monster_average_expr(mi, match, "I", "J")),
            ("monster_stamina", "Monster stamina (avg of catalog min/max)", monster_average_expr(mi, match, "K", "L")),
            ("monster_swing_stamina", "Monster swing stamina", monster_column_expr(mi, match, "M")),
            ("monster_skill", "Monster attack skill", monster_column_expr(mi, match, "N")),
            ("monster_tactics", "Monster Tactics", monster_column_expr(mi, match, "O")),
            ("monster_int", "Monster Int", monster_column_expr(mi, match, "P")),
            ("monster_resist", "Monster MagicResist", monster_column_expr(mi, match, "R")),
            ("monster_damage", "Monster damage type", monster_column_expr(mi, match, "S")),
        ]:
            ws.cell(row=row, column=1, value=text)
            output_cell(ws, row, 2, "=" + expr)
            c[key] = f"$B${row}"
            row += 1
        ws.cell(row=row, column=1, value="Monster WeaponPiercing (no chardef sets it)")
        input_cell(ws, row, 2, 0)
        c["monster_piercing"] = f"$B${row}"
        row += 1

        ws.cell(row=row, column=1, value="Profession (used if type=Player)")
        input_cell(ws, row, 2, "Warrior")
        c["profession"] = f"$B${row}"
        dv = DataValidation(type="list", formula1="=T_ProfessionName")
        ws.add_data_validation(dv)
        dv.add(ws.cell(row=row, column=2))
        row += 1

        for lbl, default in [("Level", 0), ("Str", 100), ("Dex", 100), ("Int", 100), ("Vit", 100), ("Mana (current)", 100),
                              ("Stamina (current)", 100), ("HP", 100)]:
            ws.cell(row=row, column=1, value=lbl)
            input_cell(ws, row, 2, default)
            c[lbl] = f"$B${row}"
            row += 1
        for lbl in ["Tactics", "Anatomy", "ArmsLore", "EvalInt", "SpiritSpeak", "Magery", "Parrying", "MagicResist", "Weapon"]:
            label_text = f"{lbl} skill (0-1000)"
            ws.cell(row=row, column=1, value=label_text)
            input_cell(ws, row, 2, 1000)
            c[lbl] = f"$B${row}"
            row += 1

        is_monster = f'{c["type"]}="Monster"'
        for key, text, expr in [
            ("eff_tactics", "Effective Tactics (monster's or the input)", f"IF({is_monster},{c['monster_tactics']},{c['Tactics']})"),
            ("eff_stamina", "Effective Stamina", f"IF({is_monster},{c['monster_stamina']},{c['Stamina (current)']})"),
            ("eff_int", "Effective Int", f"IF({is_monster},{c['monster_int']},{c['Int']})"),
            ("eff_hp", "Effective HP", f"IF({is_monster},{c['monster_hp']},{c['HP']})"),
        ]:
            ws.cell(row=row, column=1, value=text)
            output_cell(ws, row, 2, "=" + expr)
            c[key] = f"$B${row}"
            row += 1
        row = note(ws, row, 1, "The target's Tactics and stamina raise its defense in the hit roll. 'Weapon skill' is "
                                "the skill of the weapon in hand: Wrestling for bare_hands.", span=6)
        row += 1
        row = note(ws, row, 1, "Offense (used depending on Profession): melee professions->melee weapon, "
                                "Ranger->bow/crossbow+ammo, casters->spell. Mind power is looked up from the SAME "
                                "weapon/material/durability inputs below - see Mystic tab.", span=6)
        row += 1
        wi = weapons_info
        ws.cell(row=row, column=1, value="Weapon archetype (melee; also feeds Mystic/caster MindPower)")
        input_cell(ws, row, 2, DEFAULT_WEAPON)
        c["weapon"] = f"$B${row}"
        add_list(ws, c["weapon"], weapon_list_formula(wi, MELEE_ARCHETYPES))
        row += 1
        ws.cell(row=row, column=1, value="Weapon/ammo material (the weapon's, Ranger's bow and ammo share it)")
        input_cell(ws, row, 2, DEFAULT_MATERIAL)
        c["weapon_material"] = f"$B${row}"
        add_list(ws, c["weapon_material"], f'"{",".join(MATERIAL_NAMES_FOR_WEAPONS)}"')
        row += 1
        ws.cell(row=row, column=1, value="Weapon durability %")
        input_cell(ws, row, 2, 100)
        c["durability"] = f"$B${row}"
        row += 1
        ws.cell(row=row, column=1, value="Ranged weapon (Ranger)")
        input_cell(ws, row, 2, "Bow")
        c["ranged_weapon"] = f"$B${row}"
        add_list(ws, c["ranged_weapon"], '"Bow,LightCrossbow,HeavyCrossbow"')
        row += 1
        ws.cell(row=row, column=1, value="Ammo family (Ranger)")
        input_cell(ws, row, 2, AMMO_FAMILIES[0])
        c["ammo_family"] = f"$B${row}"
        add_list(ws, c["ammo_family"], f'"{",".join(AMMO_FAMILIES)}"')
        row += 1
        ws.cell(row=row, column=1, value="Spell (casters)")
        spell_names = [s[0] for s in SPELLS]
        input_cell(ws, row, 2, spell_names[0])
        c["spell"] = f"$B${row}"
        add_list(ws, c["spell"], f'"{",".join(spell_names)}"')
        row += 1
        ws.cell(row=row, column=1, value="Standcast / Magic Charge bonus fraction (casters, 0+ each)")
        input_cell(ws, row, 2, 0)
        input_cell(ws, row, 3, 0)
        c["standcast"] = f"$B${row}"
        c["magiccharge"] = f"$C${row}"
        row += 1

        row = note(ws, row, 1, "Defense (armor) - one shared Material for the whole outfit, pick \"None\" "
                                "on any slot to leave it bare. A Monster's outfit adds to its catalog armor.", span=6)
        row += 1
        ws.cell(row=row, column=1, value="Armor material (whole outfit)")
        input_cell(ws, row, 2, MATERIAL_NAMES_FOR_ARMOR[0])
        c["armor_material"] = f"$B${row}"
        add_list(ws, c["armor_material"], f'"{",".join(MATERIAL_NAMES_FOR_ARMOR)}"')
        row += 1
        ai = armor_info
        armor_first, armor_last = "E", get_column_letter(4 + len(MATERIAL_NAMES_FOR_ARMOR))
        mind_first = get_column_letter(ai["mind_col0"])
        mind_last = get_column_letter(ai["mind_col0"] + len(MATERIAL_NAMES_FOR_ARMOR) - 1)

        def piece_lookup(piece_cell, first, last):
            return (f"IF({piece_cell}=\"None\",0,INDEX('Armor Reference'!${first}${ai['data_start']}:${last}${ai['data_end']},"
                    f"MATCH({piece_cell},'Armor Reference'!$A${ai['data_start']}:$A${ai['data_end']},0),"
                    f"MATCH({c['armor_material']},'Armor Reference'!$E${ai['header_row']}:${armor_last}${ai['header_row']},0)))")

        slot_value_cells = []
        mind_value_cells = []
        for zone, layer, weight in ARMOR_SLOTS:
            piece_names = [p[0] for p in ARMOR_PIECES if p[1] == layer and not p[4]]
            ws.cell(row=row, column=1, value=f"{zone} piece (weight {weight})")
            input_cell(ws, row, 2, "None")
            piece_cell = f"$B${row}"
            dv = DataValidation(type="list", formula1=f'"{",".join(["None"] + piece_names)}"')
            ws.add_data_validation(dv)
            dv.add(ws.cell(row=row, column=2))
            row += 1
            ws.cell(row=row, column=1, value=f"  -> {zone} armor / mind defense (looked up)")
            output_cell(ws, row, 2, "=" + piece_lookup(piece_cell, armor_first, armor_last))
            output_cell(ws, row, 3, "=" + piece_lookup(piece_cell, mind_first, mind_last))
            slot_value_cells.append((f"$B${row}", weight))
            mind_value_cells.append((f"$C${row}", weight))
            row += 1
        ws.cell(row=row, column=1, value="Shield")
        input_cell(ws, row, 2, "None")
        shield_cell = f"$B${row}"
        dv = DataValidation(type="list", formula1=f'"{",".join(["None"] + SHIELD_PIECES)}"')
        ws.add_data_validation(dv)
        dv.add(ws.cell(row=row, column=2))
        row += 1
        ws.cell(row=row, column=1, value="  -> Shield's own armor (looked up)")
        output_cell(ws, row, 2, "=" + piece_lookup(shield_cell, armor_first, armor_last))
        shield_value_cell = f"$B${row}"
        row += 1
        zone_sum = "+".join(f"{cell}*{weight}" for cell, weight in slot_value_cells)
        mind_sum = "+".join(f"{cell}*{weight}" for cell, weight in mind_value_cells)
        ws.cell(row=row, column=1, value="Part-weighted body armor (no shield, no innate base - see note above)")
        output_cell(ws, row, 2, f"={zone_sum}")
        c["body_ac"] = f"$B${row}"
        row += 1
        ws.cell(row=row, column=1, value="Shield contribution (shield armor * Parrying/1000)")
        output_cell(ws, row, 2, f"={shield_value_cell}*{c['Parrying']}/1000")
        c["shield_contrib"] = f"$B${row}"
        row += 1
        ws.cell(row=row, column=1, value="Worn armor (INT of part-weighted body + shield)")
        output_cell(ws, row, 2, f"=INT({c['body_ac']}+{c['shield_contrib']})")
        c["worn_armor"] = f"$B${row}"
        row += 1
        ws.cell(row=row, column=1, value="Total ArmorClassVsP (a monster's catalog armor + worn)")
        output_cell(ws, row, 2, f"=IF({is_monster},{c['monster_armor_p']},0)+{c['worn_armor']}")
        c["armor_p"] = f"$B${row}"
        row += 1
        ws.cell(row=row, column=1, value="Total ArmorClassVsM")
        output_cell(ws, row, 2, f"=IF({is_monster},{c['monster_armor_m']},0)+{c['worn_armor']}")
        c["armor_m"] = f"$B${row}"
        row += 1
        ws.cell(row=row, column=1, value="Part-weighted mind defense (shield excluded, as in the engine)")
        output_cell(ws, row, 2, f"={mind_sum}")
        worn_mind = f"$B${row}"
        row += 1
        ws.cell(row=row, column=1, value="Total MindDefense (a monster's is INT(MagicResist/10))")
        player_mind = (f"ROUND((ROUND({c['MagicResist']}*C_MagicResist_EffectMax/1000,0)+{worn_mind}*C_armorMindDefense)"
                       f"*C_mindDefenseGlobal,0)")
        output_cell(ws, row, 2, f"=IF({is_monster},INT({c['monster_resist']}/10),{player_mind})")
        c["total_minddef"] = f"$B${row}"
        row += 1

        row = h2(ws, row, 1, "Resist profile (16, permille; column C is what counts: the catalog's for a Monster)", span=4)
        for name, _, _ in RESIST_TYPES:
            ws.cell(row=row, column=1, value=name)
            input_cell(ws, row, 2, 0)
            output_cell(ws, row, 3, f'=IF({is_monster},{monster_resist_expr(mi, match, chr(34) + name + chr(34))},$B${row})')
            c[name] = f"$C${row}"
            row += 1
        row += 1
        return c

    attacker = combatant_block("ATTACKER")
    defender = combatant_block("DEFENDER")

    row = h2(ws, row, 1, "Results", span=2)
    wi = weapons_info
    si = spells_info

    pipeline = f'IF({attacker["type"]}="Monster","Monster",'
    for path_name in ("Melee", "Ranged", "MindPower"):
        professions = [p for p in PROFESSION_NAMES if PROFESSION_PIPELINES[p] == path_name]
        tests = ",".join(f'{attacker["profession"]}="{p}"' for p in professions)
        pipeline += f'IF(OR({tests}),"{path_name}",'
    pipeline += '"Spell"))))'
    ws.cell(row=row, column=1, value="Attacker's pipeline (derived)")
    output_cell(ws, row, 2, f"={pipeline}")
    pipeline_cell = f"$B${row}"
    row += 1

    attacker_is_player = f'{attacker["type"]}="Player"'
    defender_is_player = f'{defender["type"]}="Player"'

    # --- Weapon-based pipeline (Melee/Ranged/Monster share this shape) ---
    weapon_lookup = weapon_value_lookup(wi, attacker["weapon"], attacker["weapon_material"], "F")
    melee_base_row = add_output(ws, row, "Melee: weapon base value (looked up, durability-scaled)",
                                f"({weapon_lookup}/2)+({weapon_lookup}/2)*({attacker['durability']}/100)")
    row += 1
    ranged_arche = add_output(ws, row, "Ranged: weapon archetype",
                              f'IF({attacker["ranged_weapon"]}="Bow","bow",IF({attacker["ranged_weapon"]}="LightCrossbow","crossbow","crossbow_heavy"))')
    row += 1
    ranged_lookup = weapon_value_lookup(wi, ranged_arche, attacker["weapon_material"], "F")
    ranged_base_row = add_output(ws, row, "Ranged: weapon base value (looked up, durability-scaled)",
                                 f"({ranged_lookup}/2)+({ranged_lookup}/2)*({attacker['durability']}/100)")
    row += 1
    ammo_mult = add_output(ws, row, "Ranged: ammo multiplier", ammo_lookup(wi, "E", attacker["ammo_family"], attacker["weapon_material"]))
    row += 1
    ammo_pierce = add_output(ws, row, "Ranged: ammo piercing", ammo_lookup(wi, "F", attacker["ammo_family"], attacker["weapon_material"]))
    row += 1
    skill_sum_row = add_output(ws, row, "Skill sum (Tactics+Anatomy+ArmsLore+Str)",
                               attack_sum_expr(attacker["Tactics"], attacker["Anatomy"], attacker["ArmsLore"], attacker["Str"]))
    row += 1
    run_bow = f'IF({attacker["ranged_weapon"]}="Bow",C_runBowDamage,1)'
    offense_row = add_output(ws, row, "WeaponAttack (Melee/Ranged rounded, Monster flat, whichever applies)",
                             f'IF({pipeline_cell}="Melee",ROUND({melee_base_row}*{skill_sum_row},0),'
                             f'IF({pipeline_cell}="Ranged",ROUND(ROUND(ROUND({ranged_base_row}*{skill_sum_row},0)*{run_bow},0)*{ammo_mult},0),'
                             f'{attacker["monster_weapon_attack"]}))')
    row += 1
    melee_speed = weapon_column_lookup(wi, wi["speed_col"], attacker["weapon"])
    ranged_speed = weapon_column_lookup(wi, wi["speed_col"], ranged_arche)
    player_speed = add_output(ws, row, "Weapon speed (Melee/Ranged)", f'IF({pipeline_cell}="Ranged",{ranged_speed},{melee_speed})')
    row += 1
    hitspeed_row = add_output(ws, row, "Hitspeed (ticks): Dex-driven, Mystic mana-driven, Monster stamina-driven",
                              f'IF({pipeline_cell}="Monster",{hitspeed_expr(npc_root_expr(attacker["monster_swing_stamina"]), "(C_weaponSpeedNPC*1000)")},'
                              f'IF({pipeline_cell}="MindPower",{hitspeed_expr(mystic_root_expr(attacker["Mana (current)"]), "C_mysticWeaponSpeedModifier")},'
                              f'{hitspeed_expr(player_root_expr(attacker["Dex"]), player_speed)}))')
    row += 1

    # --- Mind power (Mystic strike and spells) ---
    weapon_mindpower_row = add_output(ws, row, "Weapon MindPowerVsP/VsM (looked up, durability-scaled; -1=unset falls back to bare hands)",
                                      weapon_mindpower_expr(wi, attacker["weapon"], attacker["weapon_material"], attacker["durability"]))
    row += 1
    mindpower_row = add_output(ws, row, "MindPower (weapon value x mind-power sum, rounded)",
                               f"ROUND({weapon_mindpower_row}*({mind_sum_expr(attacker['EvalInt'], attacker['SpiritSpeak'], attacker['Int'])}),0)")
    row += 1
    spell_defense_row = add_output(ws, row, "Defender's spell defense (mind defense * 10, player on player less the profession's change)",
                                   f"{defender['total_minddef']}*10-IF(AND({attacker_is_player},{defender_is_player}),"
                                   f"{pvp_change_expr('T_ProfessionMindChange', defender['profession'])},0)")
    row += 1
    strike_power = (f'{mindpower_row}*10*IF({defender_is_player},C_spellPowerPvP,'
                    f'C_mysticPowerVsM*(C_spellPowerVsM+MIN({attacker["Level"]},200)*C_mysticPowerPerLevel))')
    strike_row = add_output(ws, row, "Mystic strike damage (TRUNC((power - defense) * mysticStrikeEffect / 1000))",
                            f"MAX(0,TRUNC((TRUNC({strike_power})-{spell_defense_row})*C_mysticStrikeEffect/1000))")
    row += 1

    # --- Spell pipeline (casters) ---
    def spell_lookup(column):
        return table_lookup("Mage", column, "A", si["data_start"], si["data_end"], attacker["spell"])

    ws.cell(row=row, column=1, value="Spell cast time / EFFECT min / EFFECT max / DamageType (looked up from Mage tab)")
    output_cell(ws, row, 2, f"={spell_lookup('F')}")
    output_cell(ws, row, 3, f"={spell_lookup('C')}")
    output_cell(ws, row, 4, f"={spell_lookup('D')}")
    output_cell(ws, row, 5, f"={spell_lookup('B')}")
    spell_casttime, spell_effmin, spell_effmax, spell_dtype = f"$B${row}", f"$C${row}", f"$D${row}", f"$E${row}"
    row += 1
    ws.cell(row=row, column=1, value="Spell ArmorPiercing (-1 = armor ignored) / uses mind power")
    output_cell(ws, row, 2, f"={spell_lookup('I')}")
    output_cell(ws, row, 3, f"={spell_lookup('J')}")
    spell_piercing, spell_mindpower = f"$B${row}", f"$C${row}"
    row += 1
    spell_power0 = (f'IF({spell_mindpower}="Yes",TRUNC({mindpower_row}*10*IF({defender_is_player},C_spellPowerPvP,C_spellPowerVsM)),'
                    f'{attacker["Magery"]})')
    spell_power1 = f"ROUND(ROUND({spell_power0}*(1+{attacker['standcast']}),0)*(1+{attacker['magiccharge']}),0)"
    spell_power_row = add_output(ws, row, "Spell power (multipliers, Standcast, Magic Charge, less the defense)",
                                 f'{spell_power1}-IF({spell_mindpower}="Yes",{spell_defense_row},0)')
    row += 1
    spell_damage_row = add_output(ws, row, "Raw spell damage (EFFECT curve, grows past max; power below 1 resists it)",
                                  f"IF({spell_power_row}<1,0,{spell_effmin}+TRUNC(({spell_effmax}-{spell_effmin})*{spell_power_row}/1000))")
    row += 1
    spell_scaled_row = add_output(ws, row, "Spell damage after Mind Blast's Int difference",
                                  f'IF({attacker["spell"]}="Mind Blast",{spell_damage_row}*({attacker["Int"]}-{defender["eff_int"]})/1000,{spell_damage_row})')
    row += 1

    row += 1
    row = h2(ws, row, 1, "Final result", span=2)
    level = attacker["Level"]
    monster_attack = f"ROUND({offense_row}*C_weapAttackM,0)"
    attack_val = (f'IF({attacker_is_player},IF({defender_is_player},{attack_vs_player_expr(offense_row)},'
                  f'{attack_vs_monster_expr(offense_row, level)}),'
                  f'IF({defender_is_player},{monster_attack},ROUND({monster_attack}*C_weapAttackVsM,0)))')
    attack_val_row = add_output(ws, row, "Attack value (pre-armor) - weapon-based pipelines only", attack_val)
    row += 1
    armor_eff = (f'IF({attacker_is_player},IF({defender_is_player},'
                 f'{pvp_armor_expr(defender["armor_p"], pvp_change_expr("T_ProfessionArmorChange", defender["profession"]))},'
                 f'{defender["armor_p"]}*C_armorClassM),'
                 f'IF({defender_is_player},{defender["armor_m"]},{defender["armor_m"]}*C_armorClassM))')
    ac_eff_row = add_output(ws, row, "Armor effective (the attacker decides VsP or VsM and the multiplier)", armor_eff)
    row += 1
    piercing_row = add_output(ws, row, "Piercing",
                              f'IF({pipeline_cell}="Monster",{attacker["monster_piercing"]},'
                              f'IF({pipeline_cell}="Ranged",{weapon_column_lookup(wi, wi["piercing_col"], ranged_arche)}+{ammo_pierce},'
                              f'{weapon_column_lookup(wi, wi["piercing_col"], attacker["weapon"])}))')
    row += 1
    mvp_swing = f'AND({pipeline_cell}="Monster",{defender_is_player})'
    roll_row = add_output(ws, row, "Armor roll / MvP random factor, at their AVERAGE (see profession tabs for Min/Max)",
                          f"IF({mvp_swing},AVERAGE(C_swingDamageRandMvPMin,C_swingDamageRandMvPMax),AVERAGE(C_armorRandEffectMin,C_armorRandEffectMax))")
    row += 1
    dmgmod_row = add_output(ws, row, "damageMod",
                            f'IF({defender_is_player},IF({attacker_is_player},C_swingDamagePvP,C_swingDamageM),'
                            f'IF({attacker_is_player},C_swingDamageVsM,C_swingDamageVsM*C_swingDamageM))')
    row += 1
    after_ac_row = add_output(ws, row, "Damage after armor (weapon-based pipelines)",
                              f"IF({mvp_swing},{damage_mvp_expr(attack_val_row, ac_eff_row, piercing_row, roll_row)},"
                              f"{damage_cust_expr(attack_val_row, ac_eff_row, piercing_row, roll_row, dmgmod_row)})")
    row += 1
    spell_final_row = add_output(ws, row, "Spell damage after armor (ArmorPiercing spells only)",
                                 f"IF({spell_piercing}>=0,{damage_cust_expr(spell_scaled_row, ac_eff_row, spell_piercing, roll_row, dmgmod_row)},{spell_scaled_row})")
    row += 1
    pre_resist_row = add_output(ws, row, "Pre-resist damage (whichever pipeline applies)",
                                f'IF({pipeline_cell}="Spell",{spell_final_row},IF({pipeline_cell}="MindPower",{strike_row},{after_ac_row}))')
    row += 1

    # DamageManager.GetResistModifier in full: a monster's damage type can have several subtypes
    damage_flags_row = add_output(ws, row, "Damage type (flags)",
                                  f'IF({pipeline_cell}="Spell",{spell_dtype},IF({pipeline_cell}="MindPower","Magic|Cold",'
                                  f'IF({pipeline_cell}="Monster",{attacker["monster_damage"]},'
                                  f'IF({pipeline_cell}="Ranged","Physical|Stabbing",'
                                  f'{weapon_column_lookup(wi, wi["damage_col"], attacker["weapon"])}))))')
    row += 1

    def has_flag(flag):
        return f'ISNUMBER(SEARCH("{flag}",{damage_flags_row}))'

    def subtype_sum(subtypes):
        count = "+".join(f"{has_flag(s)}*1" for s in subtypes)
        total = "+".join(f"{has_flag(s)}*{clamp_resist(defender['Resist' + s])}" for s in subtypes)
        return count, total

    phys_count, phys_total = subtype_sum(PHYSICAL_SUBTYPES)
    mag_count, mag_total = subtype_sum(MAGIC_SUBTYPES)
    phys_count_row = add_output(ws, row, "Physical subtypes in it", phys_count)
    row += 1
    phys_row = add_output(ws, row, "Physical factor (subtypes averaged, ResistPhysical multiplies)",
                          f"IF({phys_count_row}=0,1000,TRUNC((1000-{clamp_resist(defender['ResistPhysical'])})"
                          f"*TRUNC((1000*{phys_count_row}-({phys_total}))/{phys_count_row})/1000))")
    row += 1
    mag_count_row = add_output(ws, row, "Magic subtypes in it", mag_count)
    row += 1
    mag_row = add_output(ws, row, "Magic factor (subtypes averaged, ResistMagic multiplies)",
                         f"IF({mag_count_row}=0,1000,TRUNC((1000-{clamp_resist(defender['ResistMagic'])})"
                         f"*TRUNC((1000*{mag_count_row}-({mag_total}))/{mag_count_row})/1000))")
    row += 1
    factor = (f"IF(AND({phys_count_row}>0,{mag_count_row}>0),TRUNC(({phys_row}+{mag_row})/2),"
              f"IF({phys_count_row}>0,{phys_row},{mag_row}))")
    resist_row = add_output(ws, row, "Resist modifier (type-matched to the attacker's pipeline/weapon/spell)",
                            f"IF({has_flag('Summon')},TRUNC((1000-{clamp_resist(defender['ResistSummon'])})*({factor})/1000),{factor})/1000")
    row += 1
    rounded = f"ROUND(MAX(0,{pre_resist_row}*{resist_row}),0)"
    bare_hands = (f'IF({resist_row}>0,IF({rounded}=0,1,INT({attacker["Weapon"]}/200)+1),{rounded})')
    monster_minimum = f"IF({rounded}<1,5,{rounded})"
    final_row = add_output(ws, row, "Final damage per hit (rounded, minimum damage rules)",
                           f'IF(AND({pipeline_cell}="Melee",{attacker["weapon"]}="bare_hands"),{bare_hands},'
                           f'IF(AND({pipeline_cell}="Monster",NOT({defender_is_player}),{resist_row}>0,NOT({has_flag("Summon")})),'
                           f'{monster_minimum},{rounded}))')
    row += 1
    final_delay_row = add_output(ws, row, "Delay between hits/casts (seconds)",
                                 f'IF({pipeline_cell}="Spell",{spell_casttime},{swing_seconds_expr(hitspeed_row)})')
    row += 1
    attacker_skill = f'IF({pipeline_cell}="Monster",{attacker["monster_skill"]},{attacker["Weapon"]})'
    limit_row = add_output(ws, row, "Hit roll limit (0-100, swings)",
                           hit_limit_expr(attacker_skill, attacker["eff_tactics"], defender["eff_tactics"], defender["eff_stamina"]))
    row += 1
    hit_row = add_output(ws, row, "Hit chance (spells don't roll to hit)",
                         f'IF({pipeline_cell}="Spell",1,{hit_chance_expr(attacker_skill, limit_row)})')
    row += 1
    dps_row = add_output(ws, row, "Expected DPS (with hit chance)", f"{final_row}/{final_delay_row}*{hit_row}")
    row += 1
    hp_row = add_output(ws, row, "Defender HP", defender["eff_hp"])
    row += 1
    add_output(ws, row, "Time to kill (seconds)", f'IF({dps_row}=0,"never",{hp_row}/{dps_row})')

    autosize(ws, {1: 54, 2: 16, 3: 16, 4: 16, 5: 16})


def build_mage(wb, weapons_info, monsters_info):
    ws = wb.create_sheet("Mage")
    row = _profession_header(ws, "Mage")
    row = note(ws, row, 1,
               "Every shipped damage spell has SpellFlag.UseMindPower set, so spell power comes from MindPowerVsX * 10 * "
               "spellPowerVsM or spellPowerPvP, NOT the caster's raw Magery skill, and the target's mind defense * 10 comes "
               "off it after the Standcast and Magic Charge multipliers. A currently-held weapon gives MindPower as for "
               "a Mystic (see Mystic tab/Weapons Reference; none, or a weapon without it, falls back to the flat "
               "bare-hands default). Spells without ArmorPiercing bypass armor entirely, those with it go through "
               "damagecust like a swing (Armor Piercing column below; damage over power 1000 keeps growing, so a "
               "high-Int caster's spells can legitimately exceed the EFFECT=min,max range).", span=8)
    row += 1

    wi = weapons_info
    row = h2(ws, row, 1, "Calculator", span=6)
    row += 1
    spell_names = [s[0] for s in SPELLS]
    cells = {}
    row = add_inputs(ws, row, [("Int", 150), ("EvalInt skill (0-1000)", 1000), ("SpiritSpeak skill (0-1000)", 1000),
                               ("Magery skill (only a spell without UseMindPower)", 1000),
                               ("Spell", spell_names[0]), ("Standcast bonus fraction (0+, e.g. 0.10 = +10%)", 0),
                               ("Magic Charge bonus fraction (0+, matching-school stacks)", 0),
                               ("Held weapon archetype", "bare_hands"), ("Weapon material", DEFAULT_MATERIAL),
                               ("Weapon durability %", 100)], cells)
    add_list(ws, cells["Spell"], f'"{",".join(spell_names)}"')
    add_list(ws, cells["Held weapon archetype"], weapon_list_formula(wi, WEAPON_ARCHETYPES))
    add_list(ws, cells["Weapon material"], f'"{",".join(MATERIAL_NAMES_FOR_WEAPONS)}"')
    row = note(ws, row, 1, "Standcast/Magic Charge are entered as a direct bonus fraction here - the exact "
                            "per-point scaling constants (ability EffectPower, caster.Level derivation) weren't "
                            "pinned down in this research pass; enter 0 to ignore, or your own estimate.", span=8)
    row += 1

    # Ahead of the targets: the monster picker needs the spell's resist stat
    row = h2(ws, row, 1, "Spell data table (lookup source for the calculator)", span=11)
    row = header_row(ws, row, 1, ["Spell", "DamageType", "EFFECT min", "EFFECT max", "ManaUse", "CastTime", "Magery req",
                                  "AreaRange(0=single)", "ArmorPiercing (-1=armor ignored)", "UseMindPower", "Resist stat"])
    spell_data_start = row
    for s in SPELLS:
        data_rows(ws, row, 1, [list(s)])
        row += 1
    spell_data_end = row - 1
    row = note(ws, row, 1, "Source: scripts/Basic/skills/Magery/Spells/DamageSpells/damageSpells.def, read when this workbook "
                           "is generated. How to change: edit the spell's EFFECT=/ArmorPiercing=/MANAUSE=/CASTTIME=/Requirements= "
                           "fields, restart server. Mind Blast also multiplies by (caster's Int - target's Int) / 1000.", span=8)
    row += 1

    def lookup(column):
        return table_lookup("Mage", column, "A", spell_data_start, spell_data_end, cells["Spell"])

    resist_cell = add_output(ws, row, "Spell's Resist stat (looked up, empty = no subtype, not reduced)", f'{lookup("K")}&""')
    row += 2

    row = h2(ws, row, 1, "PvM target - a Monster you're attacking", span=2)
    row = add_inputs(ws, row, [("Monster MagicResist (0-1000)", 0), ("Monster ArmorVsP", 0), ("Monster ResistMagic (permille)", 0),
                               ("Monster Resist (spell narrow sub-type, permille)", 0), ("Monster HP", 100), ("Monster Tactics", 0),
                               ("Monster Stamina", 100), ("Monster Int", 0)], cells)
    row, pvm = add_monster_picker(ws, row, monsters_info, "defense",
                                  {"armor": cells["Monster ArmorVsP"], "hp": cells["Monster HP"],
                                   "group": cells["Monster ResistMagic (permille)"],
                                   "subtype": cells["Monster Resist (spell narrow sub-type, permille)"],
                                   "tactics": cells["Monster Tactics"], "stamina": cells["Monster Stamina"],
                                   "magic_resist": cells["Monster MagicResist (0-1000)"], "int": cells["Monster Int"]},
                                  "PvM target", group_resist='"ResistMagic"', subtype_resist=resist_cell)
    row += 1
    row = h2(ws, row, 1, "PvP target - another Player you're attacking", span=2)
    row = add_inputs(ws, row, [("Enemy MindDefenseVsP", 0), ("Enemy ArmorClassVsP", 0), ("Enemy profession", "Warrior"),
                               ("Enemy ResistMagic (permille)", 0), ("Enemy Resist (spell narrow sub-type, permille)", 0),
                               ("Enemy HP", 100), ("Enemy Int", 100)], cells)
    add_list(ws, cells["Enemy profession"], "=T_ProfessionName")
    row += 2

    weapon_cell, material_cell, durability_cell = cells["Held weapon archetype"], cells["Weapon material"], cells["Weapon durability %"]
    mind_sum = add_output(ws, row, "Mind-power sum (EvalInt+SpiritSpeak+Int contributions)",
                          mind_sum_expr(cells["EvalInt skill (0-1000)"], cells["SpiritSpeak skill (0-1000)"], cells["Int"]))
    row += 1
    weapon_mp = add_output(ws, row, "Held weapon MindPowerVsP/VsM (looked up, durability-scaled; -1/none=unset falls back to bare hands)",
                           weapon_mindpower_expr(wi, weapon_cell, material_cell, durability_cell))
    row += 1
    mindpower = add_output(ws, row, "Your MindPowerVsP/VsM (weapon value x mind-power sum, rounded)", f"ROUND({weapon_mp}*{mind_sum},0)")
    row += 1
    casttime = add_output(ws, row, "Spell cast time (seconds, looked up)", lookup("F"))
    row += 1
    effmin = add_output(ws, row, "Spell EFFECT min (looked up)", lookup("C"))
    row += 1
    effmax = add_output(ws, row, "Spell EFFECT max (looked up)", lookup("D"))
    row += 1
    spell_piercing = add_output(ws, row, "Spell ArmorPiercing (looked up, -1 = armor ignored)", lookup("I"))
    row += 1
    uses_mind = add_output(ws, row, "Uses mind power (looked up)", lookup("J"))
    row += 2

    row = h2(ws, row, 1, "Results", span=4)
    row = header_row(ws, row, 1, ["", "PvM", "PvP"])
    r_power0 = row
    ws.cell(row=row, column=1, value="Raw spellPower = MindPower x 10 x spellPowerVsM or spellPowerPvP")
    magery = cells["Magery skill (only a spell without UseMindPower)"]
    output_cell(ws, row, 2, f'=IF({uses_mind}="Yes",TRUNC({mindpower}*10*C_spellPowerVsM),{magery})')
    output_cell(ws, row, 3, f'=IF({uses_mind}="Yes",TRUNC({mindpower}*10*C_spellPowerPvP),{magery})')
    row += 1
    r_power1 = row
    ws.cell(row=row, column=1, value="After Standcast multiplier")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=ROUND({cl}{r_power0}*(1+{cells['Standcast bonus fraction (0+, e.g. 0.10 = +10%)']}),0)")
    row += 1
    r_power2 = row
    ws.cell(row=row, column=1, value="After Magic Charge multiplier")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=ROUND({cl}{r_power1}*(1+{cells['Magic Charge bonus fraction (0+, matching-school stacks)']}),0)")
    row += 1
    r_defense = row
    ws.cell(row=row, column=1, value="Spell defense (mind defense * 10, PvP less the profession's change)")
    output_cell(ws, row, 2, f'=IF({uses_mind}="Yes",INT({pvm["magic_resist"]}/10)*10,0)')
    output_cell(ws, row, 3, f'=IF({uses_mind}="Yes",{cells["Enemy MindDefenseVsP"]}*10-{pvp_change_expr("T_ProfessionMindChange", cells["Enemy profession"])},0)')
    row += 1
    r_power3 = row
    ws.cell(row=row, column=1, value="Spell power after defense (below 1: the target resists it, no damage)")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"={cl}{r_power2}-{cl}{r_defense}")
    row += 1
    r_dam = row
    ws.cell(row=row, column=1, value="Raw spell damage (EFFECT curve, NOT clamped past max - see note above)")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=IF({cl}{r_power3}<1,0,{effmin}+TRUNC(({effmax}-{effmin})*{cl}{r_power3}/1000))")
    row += 1
    r_blast = row
    ws.cell(row=row, column=1, value="After Mind Blast's Int difference")
    is_blast = f'{cells["Spell"]}="Mind Blast"'
    output_cell(ws, row, 2, f"=IF({is_blast},B{r_dam}*({cells['Int']}-{pvm['int']})/1000,B{r_dam})")
    output_cell(ws, row, 3, f"=IF({is_blast},C{r_dam}*({cells['Int']}-{cells['Enemy Int']})/1000,C{r_dam})")
    row += 1
    r_armor = row
    ws.cell(row=row, column=1, value="After armor (ArmorPiercing spells only, armor roll at its average)")
    roll = "AVERAGE(C_armorRandEffectMin,C_armorRandEffectMax)"
    pvm_armor = f"{pvm['armor']}*C_armorClassM"
    pvp_armor = pvp_armor_expr(cells["Enemy ArmorClassVsP"], pvp_change_expr("T_ProfessionArmorChange", cells["Enemy profession"]))
    output_cell(ws, row, 2, f"=IF({spell_piercing}>=0,{damage_cust_expr(f'B{r_blast}', pvm_armor, spell_piercing, roll, 'C_swingDamageVsM')},B{r_blast})")
    output_cell(ws, row, 3, f"=IF({spell_piercing}>=0,{damage_cust_expr(f'C{r_blast}', pvp_armor, spell_piercing, roll, 'C_swingDamagePvP')},C{r_blast})")
    row += 1
    r_resist = row
    ws.cell(row=row, column=1, value="Resist modifier (ResistMagic + spell's narrow sub-type)")
    output_cell(ws, row, 2, f'=IF({resist_cell}="",1,{resist_modifier_expr(pvm["group"], pvm["subtype"])})')
    output_cell(ws, row, 3, f'=IF({resist_cell}="",1,{resist_modifier_expr(cells["Enemy ResistMagic (permille)"], cells["Enemy Resist (spell narrow sub-type, permille)"])})')
    row += 1
    r_final = row
    ws.cell(row=row, column=1, value="Final damage per hit")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=ROUND(MAX(0,{cl}{r_armor}*{cl}{r_resist}),0)")
    row += 1
    r_dps = row
    ws.cell(row=row, column=1, value="DPS (damage / cast time - no swing-delay formula, casting IS the delay)")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"={cl}{r_final}/{casttime}")
    row += 1
    r_hp = row
    ws.cell(row=row, column=1, value="Target HP")
    output_cell(ws, row, 2, f"={pvm['hp']}")
    output_cell(ws, row, 3, f"={cells['Enemy HP']}")
    row += 1
    for col in (2, 3):
        cl = get_column_letter(col)
        ws.cell(row=row, column=1, value="Time to kill (seconds)")
        output_cell(ws, row, col, f"=IF({cl}{r_dps}=0,\"never\",{cl}{r_hp}/{cl}{r_dps})")

    autosize(ws, {1: 50, 2: 20, 3: 20})
    for c in range(1, 12):
        if ws.column_dimensions[get_column_letter(c)].width is None or ws.column_dimensions[get_column_letter(c)].width < 14:
            ws.column_dimensions[get_column_letter(c)].width = 14
    return dict(data_start=spell_data_start, data_end=spell_data_end)


def main():
    wb = Workbook()
    build_overview(wb)
    build_materials(wb)
    build_core_formulas(wb)
    weapons_info = build_weapons_ref(wb)
    armor_info = build_armor_ref(wb)
    monsters_info = build_monsters_ref(wb)
    build_melee_profession(wb, "Warrior", weapons_info, monsters_info)
    build_ranger(wb, weapons_info, monsters_info)
    spells_info = build_mage(wb, weapons_info, monsters_info)
    build_mystic(wb, weapons_info, monsters_info)
    build_melee_profession(wb, "Thief", weapons_info, monsters_info,
                            ranged_note="Thief is also allowed Bow (per thief.def) - a Thief using a Bow follows "
                                        "the SAME formula as the Ranger tab (not profession-gated). "
                                        "This calculator covers Thief's allowed melee weapons (OneHandSpike, "
                                        "TwoHandSpike, OneHandSword) only.")
    add_thief_ability_table(wb)
    build_fight_simulator(wb, weapons_info, armor_info, monsters_info, spells_info)

    os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)
    wb.save(OUTPUT_PATH)
    print(f"Wrote {OUTPUT_PATH}")


if __name__ == "__main__":
    main()
