#!/usr/bin/env python3
"""Generates docs/balance/combat-balance.xlsx from moriaengine's real combat formulas/content.

Re-run this script whenever the underlying engine numbers change (weapon/armor .def edits,
CombatSettings/RangerSettings/MagerySettings constant changes, new spells/abilities) - every
number in the generated workbook carries a Source + "how to change" note pointing back here.

Source data was extracted from the engine on 2026-10-20 (see repo-relative file:line references
throughout). Requires openpyxl (not in the repo's own Python-free toolchain - install into a
venv: `python3 -m venv /tmp/venv && /tmp/venv/bin/pip install openpyxl && /tmp/venv/bin/python3
tools/balance/generate_combat_balance.py`).
"""

import datetime
from openpyxl import Workbook
from openpyxl.styles import Font, PatternFill, Alignment
from openpyxl.utils import get_column_letter
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


def autosize(ws, widths):
    for col, w in widths.items():
        ws.column_dimensions[get_column_letter(col)].width = w


# --------------------------------------------------------------------------------------
# SOURCE DATA
# Everything below was pulled directly from the engine on 2026-10-20 - see the per-table
# "Source" / "How to change" columns in the generated sheets for exact file:line references.
# --------------------------------------------------------------------------------------

# Material tiers: tier -> (metal name, wood name or None, has_weapons, has_armor)
# scripts/Basic/Enums.cs:267-279 (Material enum - metal/wood share the same numeric tier)
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

# Weapon archetypes: (archetype, display-name-root, WeaponType, Skill, MaterialType, notes)
# scripts/converted/zbrane/*.def, cross-checked against WeaponSkillTargetQueuePlugin.cs
WEAPON_ARCHETYPES = [
    ("dagger", "dagger", "OneHandSpike", "Fencing", "Metal", ""),
    ("club", "club", "OneHandBlunt", "Macing", "Metal", "NAME uses wood species name at every tier but MaterialType is Metal - content quirk, not fixed here"),
    ("kryss", "kryss", "OneHandSpike", "Fencing", "Metal", ""),
    ("cutlass", "cutlass", "OneHandSpike", "Fencing", "Metal", ""),
    ("mace", "mace", "OneHandBlunt", "Macing", "Metal", ""),
    ("scimitar", "scimitar", "OneHandSpike", "Fencing", "Metal", ""),
    ("axe", "axe", "TwoHandAxe", "Swords", "Metal", ""),
    ("shepherds_crook", "crook", "OneHandSpike", "Fencing", "Wood", ""),
    ("staff_black", "black staff", "TwoHandBlunt", "Macing", "Wood", ""),
    ("katana", "katana", "OneHandSpike", "Fencing", "Metal", ""),
    ("spear_short", "short spear", "OneHandSpike", "Fencing", "Metal", ""),
    ("war_fork", "war fork", "TwoHandSpike", "Fencing", "Metal", ""),
    ("sword_broad", "broad sword", "OneHandSpike", "Fencing", "Metal", "WeaponType is OneHandSpike (Fencing) despite 'sword' in the name - converter guess, not corrected here"),
    ("mace_war", "war mace", "OneHandBlunt", "Macing", "Metal", ""),
    ("sword_long", "long sword", "OneHandSpike", "Fencing", "Metal", "same WeaponType quirk as broad sword"),
    ("sword_viking", "viking sword", "OneHandSpike", "Fencing", "Metal", "same WeaponType quirk as broad sword"),
    ("staff_quarter", "quarter staff", "TwoHandBlunt", "Macing", "Wood", ""),
    ("axe_battle", "battle axe", "OneHandAxe", "Swords", "Metal", ""),
    ("hammer_pick", "hammer pick", "TwoHandBlunt", "Macing", "Metal", ""),
    ("pitchfork", "pitchfork", "TwoHandSpike", "Fencing", "Metal", ""),
    ("axe_exec", "executioners axe", "OneHandAxe", "Swords", "Metal", ""),
    ("axe_double", "double axe", "TwoHandAxe", "Swords", "Metal", ""),
    ("hammer_war", "war hammer", "TwoHandBlunt", "Macing", "Metal", ""),
    ("axe_two_hand", "two handed axe", "TwoHandAxe", "Swords", "Metal", ""),
    ("spear", "spear", "OneHandSpike", "Fencing", "Metal", ""),
    ("staff_gnarled", "gnarled staff", "TwoHandBlunt", "Macing", "Wood", ""),
    ("axe_war", "war axe", "TwoHandAxe", "Swords", "Metal", ""),
    ("axe_battle_large", "battle large axe", "TwoHandAxe", "Swords", "Metal", ""),
    ("bardiche", "bardiche", "TwoHandAxe", "Swords", "Metal", ""),
    ("maul", "maul", "TwoHandBlunt", "Macing", "Metal", ""),
    ("halberd", "halberd", "TwoHandAxe", "Swords", "Metal", ""),
    ("bow", "bow", "Bow", "Archery", "Wood", "material-scaled value below is IGNORED for players - see Ranger tab, RangerSettings overrides it flatly"),
    ("crossbow", "crossbow", "XBow", "Marksmanship", "Wood", "light crossbow; same override note as bow"),
    ("crossbow_heavy", "heavy crossbow", "XBow", "Marksmanship", "Wood", "same override note as bow"),
]

# attackVsP == attackVsM for every real weapon def (confirmed, zero exceptions) - one value/cell.
# Columns match WEAPON_TIERS order: Copper, Iron, Verite, Valorite, Obsidian, Adamantinum, Mithril
WEAPON_ATTACK = {
    "dagger": [2, 3, 4, 5, 6, 7, 8],
    "club": [4, 6, 7, 8, 9, 10, 11.5],
    "kryss": [4, 6, 7, 8, 9, 10, 11.5],
    "cutlass": [5, 7, 8, 9, 10, 11.5, 13.5],
    "mace": [7, 9, 10, 12, 13, 15, 18],
    "scimitar": [7.5, 9.5, 11, 12.5, 14, 15.5, 17.5],
    "axe": [6, 8, 9.5, 11, 12, 13.5, 16],
    "shepherds_crook": [7, 8.5, 10, 11.5, 13.5, 15.5, 18],
    "staff_black": [8, 10.5, 12.5, 14.5, 17, 19.5, 22],
    "katana": [8.5, 10.5, 12.5, 14.5, 17, 19.5, 22],
    "spear_short": [8.5, 10.5, 12.5, 14.5, 17, 19.5, 22],
    "war_fork": [9, 12, 14, 16, 18, 20, 23],
    "sword_broad": [9, 12, 14, 16, 18, 20, 23],
    "mace_war": [10, 13.5, 16, 18, 20, 23, 26.5],
    "sword_long": [11, 14, 16.5, 19, 22, 24, 28],
    "sword_viking": [12, 16, 19, 22, 25, 28, 32],
    "staff_quarter": [11, 14, 16.5, 18.5, 22, 24, 28],
    "axe_battle": [12, 15, 17.5, 20, 22.5, 25.5, 28],
    "hammer_pick": [12, 15, 17.5, 20, 22.5, 25.5, 28],
    "pitchfork": [12, 15, 17.5, 20, 22.5, 25.5, 28],
    "axe_exec": [13.5, 17, 20, 23, 26, 29.5, 34],
    "axe_double": [13, 16.5, 19.5, 22.5, 25.5, 29, 33],
    "hammer_war": [14, 18, 21, 24, 27, 30, 34],
    "axe_two_hand": [14, 18, 21, 24, 27, 30, 34],
    "spear": [13, 16, 19, 22, 22.5, 25, 32],
    "staff_gnarled": [15.5, 19, 22, 25, 28, 31, 35],
    "axe_war": [16, 19.5, 22.5, 25.5, 29, 32, 36],
    "axe_battle_large": [17, 21, 24.5, 28, 32, 35, 38],
    "bardiche": [19, 22, 26, 30, 33.5, 37, 41],
    "maul": [18, 22.5, 25, 29, 31.5, 34, 37],
    "halberd": [20, 24, 27, 31, 34, 36, 42.5],
    "bow": [5, 7, 8, 9, 10, 11.5, 13.5],
    "crossbow": [7, 12, 14, 16, 18, 20, 23],
    "crossbow_heavy": [13, 18, 21, 24, 27, 30, 34],
}

# Armor pieces: (defname, layer, WearableType, per-material AR [==MindDefense], shield?)
# Columns match METAL_TIERS order: Copper,Iron,Silver,Gold,Verite,Valorite,Obsidian,Adamantinum,Mithril
# scripts/converted/brneni/*.def - armorVsP/armorVsM/mindDefenseVsP/mindDefenseVsM are all equal
# per piece per material (confirmed, no VsP/VsM split actually used on armor content)
ARMOR_PIECES = [
    ("I_ARMOR_FEMALE_BUSTIER_STUDDED", 13, "Studded", [9, 9, 12, 24, 15, 15, 18, 18, 24], False),
    ("I_ARMOR_FEMALE_STUDDED", 13, "Studded", [9, 9, 12, 24, 15, 15, 18, 18, 24], False),
    ("I_BASCINET", 6, "Plate", [30, 35, 40, 45, 50, 55, 60, 65, 80], False),
    ("I_BONE_ARMS", 19, "Bone", [11, 11, 11, 14, 11, 17, 17, 20, 24], False),
    ("I_BONE_CHEST", 13, "Bone", [11, 11, 11, 14, 11, 17, 17, 20, 24], False),
    ("I_BONE_GLOVES", 7, "Bone", [11, 11, 11, 14, 11, 17, 17, 20, 24], False),
    ("I_BONE_HELMET", 6, "Bone", [11, 11, 11, 14, 11, 17, 17, 20, 24], False),
    ("I_BONE_LEGGINGS", 4, "Bone", [11, 11, 11, 14, 11, 17, 17, 20, 24], False),
    ("I_BUCKLER_SHIELD", 2, "Plate", [8, 9, 10, 11, 13, 14, 15, 16, 20], True),
    ("I_CHAINMAIL_COIF", 6, "Chain", [21, 25, 28, 32, 35, 39, 42, 46, 56], False),
    ("I_CHAINMAIL_GLOVES", 7, "Chain", [21, 21, 21, 21, 21, 21, 21, 21, 21], False),
    ("I_CHAINMAIL_GORGET", 10, "Chain", [21, 21, 21, 21, 21, 21, 21, 21, 21], False),
    ("I_CHAINMAIL_LEGGINGS", 4, "Chain", [21, 25, 28, 32, 35, 39, 42, 46, 56], False),
    ("I_CHAINMAIL_SLEEVES", 19, "Chain", [21, 21, 21, 21, 21, 21, 21, 21, 21], False),
    ("I_CHAINMAIL_TUNIC", 13, "Chain", [21, 25, 28, 32, 35, 39, 42, 46, 56], False),
    ("I_CLOSED_HELM", 6, "Plate", [30, 35, 40, 45, 50, 55, 60, 65, 80], False),
    ("I_FEMALE_PLATEMAIL", 13, "Plate", [30, 35, 40, 45, 50, 55, 60, 65, 80], False),
    ("I_HEATER_SHIELD", 2, "Plate", [3, 5, 5, 8, 10, 12, 15, 20, 35], True),
    ("I_KITE_WOOD_SHIELD", 2, "Plate", [21, 21, 21, 21, 21, 21, 21, 21, 21], True),
    ("I_METAL_SHIELD", 2, "Plate", [11, 18, 15, 17, 25, 21, 23, 24, 30], True),
    ("I_NOSE_HELM", 6, "Plate", [30, 35, 40, 45, 50, 55, 60, 65, 80], False),
    ("I_PLATEMAIL", 13, "Plate", [30, 35, 40, 45, 50, 55, 60, 65, 80], False),
    ("I_PLATEMAIL_ARMS", 19, "Plate", [30, 35, 40, 45, 50, 55, 60, 65, 80], False),
    ("I_PLATEMAIL_GAUNTLETS", 7, "Plate", [30, 35, 40, 45, 50, 55, 60, 65, 80], False),
    ("I_PLATEMAIL_GORGET", 10, "Plate", [30, 35, 40, 45, 50, 55, 60, 65, 80], False),
    ("I_PLATEMAIL_LEGGINGS", 4, "Plate", [30, 35, 40, 45, 50, 55, 60, 65, 80], False),
    ("I_PLATE_HELM", 6, "Plate", [30, 35, 40, 45, 50, 55, 60, 65, 80], False),
    ("I_RINGMAIL_GLOVES", 7, "Ring", [23, 26, 30, 34, 38, 41, 45, 49, 60], False),
    ("I_RINGMAIL_GORGET", 10, "Ring", [21, 21, 21, 21, 21, 21, 21, 21, 21], False),
    ("I_RINGMAIL_HELM", 6, "Ring", [21, 21, 21, 21, 21, 21, 21, 21, 21], False),
    ("I_RINGMAIL_LEGGINGS", 4, "Ring", [23, 26, 30, 34, 38, 41, 45, 49, 56], False),
    ("I_RINGMAIL_SLEEVES", 19, "Ring", [23, 26, 30, 34, 38, 41, 45, 49, 60], False),
    ("I_RINGMAIL_TUNIC", 13, "Ring", [23, 26, 30, 34, 38, 41, 45, 49, 60], False),
    ("I_STUDDED_CAP", 6, "Studded", [9, 9, 9, 9, 9, 9, 9, 9, 9], False),
    ("I_STUDDED_GLOVES", 7, "Studded", [9, 9, 12, 24, 15, 15, 18, 18, 24], False),
    ("I_STUDDED_GORGET", 10, "Studded", [9, 9, 12, 24, 15, 15, 18, 18, 24], False),
    ("I_STUDDED_LEGGINGS", 4, "Studded", [9, 9, 12, 24, 15, 15, 18, 18, 24], False),
    ("I_STUDDED_SLEEVES", 19, "Studded", [9, 9, 12, 24, 15, 15, 18, 18, 24], False),
    ("I_STUDDED_TUNIC", 13, "Studded", [9, 9, 12, 24, 15, 15, 18, 18, 24], False),
]

# Body zone weights - CombatCalculator.cs:~221-251
BODY_ZONES = [
    ("Head", 0.10, "Helmet, Collar (content quirk: Collar's material tracking writes to the Head variable instead of Neck - doesn't affect the AC number, only the cosmetic material label)"),
    ("Neck", 0.05, "Collar"),
    ("Back", 0.10, "Shirt/Chest/Tunic, Cape, Robe"),
    ("Chest", 0.30, "Shirt/Chest/Tunic, Robe"),
    ("Arms", 0.10, "Arms, Cape, Robe"),
    ("Hands", 0.10, "Gloves"),
    ("Legs", 0.20, "Pants/Skirt/HalfApron, Robe, Leggins"),
    ("Feet", 0.05, "Shoes, Leggins"),
]

# Named engine constants - (name, value, meaning, source, how-to-change)
CS = "scripts/Basic/skills/Combat/CombatSettings.cs"
RS = "scripts/Basic/skills/Combat/RangerSettings.cs"
MS = "scripts/Basic/skills/Magery/MagerySettings.cs"
SKILLS = "scripts/Basic/skills/Skills.scp"
CONSTANTS = [
    ("attackStrModifier", 3.170, "Str's weight in the melee attack-value sum", CS, "Edit the field in CombatSettings.cs, rebuild, restart server"),
    ("weapAttack", 1.100, "Global base attack multiplier (all combos)", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("weapAttackM", 1.000, "Extra multiplier, attacker is a monster", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("weapAttackVsM", 1.600, "Extra multiplier, defender is a monster", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("weapAttackPvP", 0.900, "Extra multiplier, Player-vs-Player only", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("armorClassP", 0.700, "Defender armor multiplier, defender is a Player", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("armorClassM", 1.000, "Defender armor multiplier, defender is a monster", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("swingDamagePvP", 0.900, "damageMod for Player-attacks-Player", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("swingDamageM", 1.000, "damageMod for PvM or MvM (any monster defender)", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("swingDamageRandMvPMin", 0.850, "damageMod lower bound, Monster-attacks-Player", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("swingDamageRandMvPMax", 1.000, "damageMod upper bound, Monster-attacks-Player", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("weaponSpeedGlobal", 1.000, "Global multiplier on non-Mystic swing delay", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("bareHandsAttackVsP", 10, "Unarmed attack value vs Player", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("bareHandsAttackVsM", 10, "Unarmed attack value vs Monster", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("bareHandsSpeed", 100, "Unarmed weapon Speed field equivalent", CS, "Edit CombatSettings.cs, rebuild, restart server"),
    ("bareHandsMindPowerVsP", 700, "Mystic unarmed MindPower vs Player (also used as every staff's fallback - see Mystic tab)", MS, "Edit MagerySettings.cs, rebuild, restart server"),
    ("bareHandsMindPowerVsM", 700, "Mystic unarmed MindPower vs Monster", MS, "Edit MagerySettings.cs, rebuild, restart server"),
    ("mindPowerIntModifier", 0.700, "Int's weight in the mind-power attack sum", MS, "Edit MagerySettings.cs, rebuild, restart server"),
    ("mysticWeaponSpeedModifier", 1.000, "Multiplier on Mystic's mana-driven swing delay", MS, "Edit MagerySettings.cs, rebuild, restart server"),
    ("mysticManaLossPermille", 970, "Mana retained (970/1000 = 97%) per Mystic swing", MS, "Edit MagerySettings.cs, rebuild, restart server"),
    ("concentrationDiscountPerPoint", 0.02, "Mage Concentration: mana-cost discount per ability point (cap 0.9)", "scripts/Basic/skills/Magery/Magery.cs:242-256", "Edit Magery.cs, rebuild, restart server"),
    ("magicChargeBonusPerPoint", 0.02, "Mage Magic Charge: spell-power bonus per stacked charge", "scripts/Basic/AbilitiesAndTalents/Talents/MagicCharge/MagicChargeHelper.cs", "Edit MagicChargeHelper.cs, rebuild, restart server"),
    ("magicChargeDecayIntervalSeconds", 10.0, "Mage Magic Charge: seconds before a charge decays", "scripts/Basic/AbilitiesAndTalents/Talents/MagicCharge/MagicChargeHelper.cs", "Edit MagicChargeHelper.cs, rebuild, restart server"),
    ("arrowDamageBonusPerTier", 0.05, "Ranger: +dmg per ammo material tier (explicit placeholder, not a ported real value)", RS, "Edit RangerSettings.cs, rebuild, restart server (or live GM .settings if exposed)"),
    ("bowDamage", 5, "Ranger: flat bow AttackVsP/VsM override (ignores the bow's own material)", RS, "Edit RangerSettings.cs, or live GM .settings command"),
    ("lightCrossbowDamage", 7, "Ranger: flat light crossbow AttackVsP/VsM override", RS, "Edit RangerSettings.cs, or live GM .settings command"),
    ("heavyCrossbowDamage", 13, "Ranger: flat heavy crossbow AttackVsP/VsM override", RS, "Edit RangerSettings.cs, or live GM .settings command"),
    ("bowSpeed", 100, "Ranger: bow Speed override", RS, "Edit RangerSettings.cs, or live GM .settings command"),
    ("lightCrossbowSpeed", 100, "Ranger: light crossbow Speed override", RS, "Edit RangerSettings.cs, or live GM .settings command"),
    ("heavyCrossbowSpeed", 90, "Ranger: heavy crossbow Speed override", RS, "Edit RangerSettings.cs, or live GM .settings command"),
    ("Ranges.bow", 8, "Ranger bow attack range (tiles)", CS, "Edit CombatSettings.cs Ranges page, or live GM .settings command"),
    ("Ranges.crossbow", 8, "Ranger light crossbow attack range", CS, "Edit CombatSettings.cs Ranges page, or live GM .settings command"),
    ("Ranges.heavyCrossbow", 8, "Ranger heavy crossbow attack range", CS, "Edit CombatSettings.cs Ranges page, or live GM .settings command"),
    ("Ranges.mystic", 8, "Mystic attack range (flat, overrides weapon/bare-hand range entirely)", CS, "Edit CombatSettings.cs Ranges page, or live GM .settings command"),
    ("Tactics_EffectMax", 303, "Tactics skill's contribution to melee attack value at skill 1000/GM", SKILLS, "Edit Skills.scp's EFFECT=0,x line for Tactics, restart server"),
    ("Anatomy_EffectMax", 155, "Anatomy skill's contribution to melee attack value at skill 1000/GM", SKILLS, "Edit Skills.scp's EFFECT=0,x line for Anatomy, restart server"),
    ("ArmsLore_EffectMax", 76, "ArmsLore skill's contribution to melee attack value at skill 1000/GM", SKILLS, "Edit Skills.scp's EFFECT=0,x line for ArmsLore, restart server"),
    ("EvalInt_EffectMax", 100, "Eval Intelligence's contribution to mind-power at skill 1000/GM", SKILLS, "Edit Skills.scp's EFFECT=0,x line for EvalInt, restart server"),
    ("SpiritSpeak_EffectMax", 25, "Spirit Speak's contribution to mind-power at skill 1000/GM", SKILLS, "Edit Skills.scp's EFFECT=0,x line for SpiritSpeak, restart server"),
    ("MagicResist_EffectMax", 0, "Resisting Spells' contribution to Mystic mind-defense - CURRENTLY INERT: EFFECT line is commented out in Skills.scp:189, so this is always 0 today regardless of skill level", SKILLS, "Uncomment and set Skills.scp's EFFECT=0,x line for MagicResist, restart server"),
]

RESIST_TYPES = [
    ("ResistMagic", "Magic (broad)", "every magic-flagged damage instance"),
    ("ResistPhysical", "Physical (broad)", "every physical-flagged damage instance"),
    ("ResistFire", "Fire (narrow)", "Magic Arrow, Fireball, Explosion, Flame Strike, Meteor Swarm"),
    ("ResistElectric", "Electric (narrow)", "Lightning, Mind Blast, Energy Bolt, Chain Lightning"),
    ("ResistAcid", "Acid (narrow)", "none currently assigned to a shipped spell"),
    ("ResistCold", "Cold (narrow)", "Harm"),
    ("ResistPoison", "Poison (narrow)", "Poison spell's damage-over-time"),
    ("ResistMystical", "Mystical (narrow)", "Mystic profession's own innate melee swing only"),
    ("ResistSlashing", "Slashing (narrow)", "Sword/Axe-type melee weapons"),
    ("ResistStabbing", "Stabbing (narrow)", "Fencing-type melee weapons"),
    ("ResistBlunt", "Blunt (narrow)", "Macing-type melee weapons"),
    ("ResistArchery", "Archery (narrow)", "Bow/Crossbow ranged weapons"),
    ("ResistBleed", "Bleed (narrow)", "Bleed-DoT effects (e.g. Thief's Bleeding Strike)"),
    ("ResistSummon", "Summon (narrow)", "summoned-creature-sourced damage"),
    ("ResistDragon", "Dragon (narrow)", "dragon-sourced damage"),
]

# Mage damage spells - scripts/Basic/skills/Magery/Spells/DamageSpells/damageSpells.def
SPELLS = [
    ("Magic Arrow", "MagicFire", 0, 3, 6, 2.5, 40, 1),
    ("Harm", "MagicCold", 6, 12, 6, 1.2, 200, 1),
    ("Fireball", "MagicFire", 0, 11, 6, 1.7, 300, 1),
    ("Lightning", "MagicElectric", 0, 15, 11, 2.0, 400, 1),
    ("Mind Blast", "MagicElectric", 0, 70, 20, 2.5, 500, 1),
    ("Energy Bolt", "MagicElectric", 0, 16, 22, 2.5, 600, 1),
    ("Explosion", "MagicFire", 0, 25, 22, 2.5, 600, 5),
    ("Chain Lightning", "MagicElectric", 0, 30, 30, 2.8, 700, 5),
    ("Flame Strike", "MagicFire", 0, 35, 40, 2.8, 700, 1),
    ("Meteor Swarm", "MagicFire", 0, 40, 40, 3.5, 700, 5),
    ("Earthquake", "Magic (no sub-school - only DamageType set by the engine's own default)", 0, 50, 50, 4.0, 800, 0),
]
# columns: name, DamageType, effectMin, effectMax, manaUse, castTime, magerySkillRequired, areaRange(0=single-target)

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
    ("Thief bleed cap", "Designed: Thief-specific higher cap on the generic bleed mechanism", "No - designed only, issue #479", "GitHub issue #479"),
    ("Defensive stance + weapon reflect", "Designed: damage-reduction stance, weapon-specific reflect", "No - designed only, issue #480", "GitHub issue #480"),
    ("Shield-strike paralyze", "Designed: niche paralyze-on-shield-strike", "No - designed only (optional), issue #481", "GitHub issue #481"),
]

PROFESSION_STATS = {
    "Warrior": dict(str_r=(80, 120, 180), dex_r=(60, 90, 110), int_r=(20, 50, 60), vit_r=(80, 130, 180),
                     weapons="OneHandSword, TwoHandSword, OneHandAxe, TwoHandAxe, OneHandBlunt, TwoHandBlunt, OneHandSpike, TwoHandSpike",
                     armor="unrestricted", source="scripts/Basic/Professions/Definitions/warrior.def"),
    "Ranger": dict(str_r=(50, 80, 120), dex_r=(80, 130, 200), int_r=(60, 90, 90), vit_r=(60, 90, 130),
                   weapons="Bow, XBow", armor="Ring, Chain, Leather",
                   source="scripts/Basic/Professions/Definitions/ranger.def"),
    "Mage": dict(str_r=(50, 80, 90), dex_r=(60, 90, 100), int_r=(100, 150, 230), vit_r=(50, 80, 100),
                 weapons="OneHandBlunt, TwoHandBlunt", armor="Leather",
                 source="scripts/Basic/Professions/Definitions/mage.def"),
    "Mystic": dict(str_r=(50, 80, 90), dex_r=(50, 90, 100), int_r=(90, 140, 230), vit_r=(50, 80, 100),
                   weapons="OneHandBlunt, TwoHandBlunt (staves)", armor="Bone, Leather",
                   source="scripts/Basic/Professions/Definitions/mystic.def"),
    "Thief": dict(str_r=(60, 90, 120), dex_r=(80, 120, 200), int_r=(50, 80, 90), vit_r=(70, 100, 140),
                  weapons="OneHandSpike, TwoHandSpike, OneHandSword, Bow", armor="Chain",
                  source="scripts/Basic/Professions/Definitions/thief.def"),
}

# Monster catalog - deduplicated (one row per distinct base creature, weapon/color variants
# collapsed) from scripts/converted/chars/*.def (excludes vendors.def/guards.def/mounts.def/
# ucitele.def - confirmed non-monster content). Columns: (Name, Category, WeaponAttack min,
# WeaponAttack max, ArmorVsP min, ArmorVsP max, ArmorVsM min, ArmorVsM max). min==max when every
# merged variant had the same value. HP and WeaponDelay are NOT per-monster (see MONSTER_DEFAULTS).
MONSTERS = [
    ('Bily vlk', 'Animal', 4.0, 4.0, 16.0, 16.0, 16.0, 16.0),
    ('Byk', 'Animal', 5.0, 5.0, 14.0, 14.0, 14.0, 14.0),
    ('Delfin', 'Animal', 3.0, 3.0, 16.0, 16.0, 16.0, 16.0),
    ('Gorila', 'Animal', 6.0, 6.0, 20.0, 20.0, 20.0, 20.0),
    ('Grizzly', 'Animal', 8.0, 8.0, 24.0, 24.0, 24.0, 24.0),
    ('Had', 'Animal', 4.0, 4.0, 16.0, 16.0, 16.0, 16.0),
    ('Horska koza', 'Animal', 4.0, 4.0, 16.0, 16.0, 16.0, 16.0),
    ('Horsky lev', 'Animal', 15.0, 15.0, 8.0, 8.0, 8.0, 8.0),
    ('Jaguar', 'Animal', 21.0, 21.0, 16.0, 16.0, 16.0, 16.0),
    ('Jelen', 'Animal', 5.0, 5.0, 24.0, 24.0, 24.0, 24.0),
    ('Kocka', 'Animal', 1.0, 1.0, 4.0, 4.0, 4.0, 4.0),
    ('Koza', 'Animal', 2.0, 2.0, 16.0, 16.0, 16.0, 16.0),
    ('Kralik', 'Animal', 1.0, 1.0, 6.0, 6.0, 6.0, 6.0),
    ('Krava', 'Animal', 2.0, 2.0, 10.0, 10.0, 10.0, 10.0),
    ('Krysa', 'Animal', 2.0, 2.0, 6.0, 6.0, 6.0, 6.0),
    ('Lavovy had', 'Animal', 4.0, 4.0, 24.0, 24.0, 24.0, 24.0),
    ('Ledovy had', 'Animal', 7.0, 7.0, 30.0, 30.0, 30.0, 30.0),
    ('Lity vlk', 'Animal', 11.0, 11.0, 22.0, 22.0, 22.0, 22.0),
    ('Medved', 'Animal', 6.0, 6.0, 8.0, 12.0, 8.0, 12.0),
    ('Mroz', 'Animal', 3.0, 3.0, 18.0, 18.0, 18.0, 18.0),
    ('Orel', 'Animal', 5.0, 5.0, 22.0, 22.0, 22.0, 22.0),
    ('Ovce', 'Animal', 2.0, 2.0, 6.0, 6.0, 6.0, 6.0),
    ('Panter', 'Animal', 15.0, 15.0, 16.0, 16.0, 16.0, 16.0),
    ('Pes', 'Animal', 4.0, 4.0, 12.0, 12.0, 12.0, 12.0),
    ('Prase', 'Animal', 2.0, 3.0, 5.0, 12.0, 5.0, 12.0),
    ('Ptacek', 'Animal', 1.0, 1.0, 0.0, 0.0, 0.0, 0.0),
    ('Sedy vlk', 'Animal', 4.0, 4.0, 18.0, 18.0, 18.0, 18.0),
    ('Slepice', 'Animal', 1.0, 1.0, 2.0, 2.0, 2.0, 2.0),
    ('Snezny leopard', 'Animal', 15.0, 15.0, 12.0, 12.0, 12.0, 12.0),
    ('Srnec', 'Animal', 2.0, 2.0, 8.0, 8.0, 8.0, 8.0),
    ('Velkej potkan', 'Animal', 5.0, 5.0, 18.0, 18.0, 18.0, 18.0),
    ('Vlk', 'Animal', 5.0, 5.0, 18.0, 18.0, 18.0, 18.0),
    ('Zajic', 'Animal', 5.0, 5.0, 4.0, 4.0, 4.0, 4.0),
    ('ostrihana ovce', 'Animal', 2.0, 2.0, 6.0, 6.0, 6.0, 6.0),
    ('Arboreal Mongbat', 'Cave', 32.0, 32.0, 5.0, 5.0, 5.0, 5.0),
    ('Blody Hellcat', 'Cave', 35.0, 35.0, 65.0, 65.0, 65.0, 65.0),
    ('Blood Vampire', 'Cave', 45.0, 45.0, 70.0, 70.0, 70.0, 70.0),
    ('Corpser', 'Cave', 20.0, 20.0, 19.0, 19.0, 19.0, 19.0),
    ('Corpser Older', 'Cave', 40.0, 40.0, 35.0, 35.0, 35.0, 35.0),
    ('Drapac', 'Cave', 55.0, 55.0, 60.0, 60.0, 60.0, 60.0),
    ('Gargoyle', 'Cave', 28.0, 28.0, 26.0, 26.0, 26.0, 26.0),
    ('Great Vampire', 'Cave', 75.0, 75.0, 90.0, 90.0, 90.0, 90.0),
    ('Harpy', 'Cave', 29.0, 29.0, 24.0, 24.0, 24.0, 24.0),
    ('Hell Hound', 'Cave', 35.0, 35.0, 30.0, 30.0, 30.0, 30.0),
    ('Kentaur', 'Cave', 120.0, 120.0, 80.0, 80.0, 80.0, 80.0),
    ('Krvavy Drapac', 'Cave', 65.0, 65.0, 65.0, 65.0, 65.0, 65.0),
    ('Nightmare', 'Cave', 60.0, 60.0, 80.0, 80.0, 80.0, 80.0),
    ('Phoenix', 'Cave', 55.0, 55.0, 65.0, 65.0, 65.0, 65.0),
    ('Poison Phoenix', 'Cave', 65.0, 65.0, 65.0, 65.0, 65.0, 65.0),
    ('Predator Hellcat', 'Cave', 50.0, 50.0, 15.0, 15.0, 15.0, 15.0),
    ('Stone Gargoyle', 'Cave', 40.0, 40.0, 60.0, 60.0, 60.0, 60.0),
    ('Stone Harpy', 'Cave', 28.0, 28.0, 70.0, 70.0, 70.0, 70.0),
    ('Swamp Tentacles', 'Cave', 30.0, 30.0, 15.0, 15.0, 15.0, 15.0),
    ('Vampire', 'Cave', 38.0, 38.0, 60.0, 60.0, 60.0, 60.0),
    ('Vlkodav', 'Cave', 45.0, 45.0, 60.0, 60.0, 60.0, 60.0),
    ('(unnamed unique NPC, orcs.def)', 'Daemon', 9.0, 120.0, 20.0, 150.0, 20.0, 150.0),
    ('Balrogh', 'Daemon', 120.0, 120.0, 180.0, 180.0, 180.0, 180.0),
    ('Blood Daemon', 'Daemon', 90.0, 90.0, 100.0, 100.0, 100.0, 100.0),
    ('Daemon Swordsman', 'Daemon', 120.0, 120.0, 74.0, 74.0, 90.0, 90.0),
    ('Demon smrti', 'Daemon', 100.0, 100.0, 150.0, 150.0, 150.0, 150.0),
    ('Gold Daemon', 'Daemon', 70.0, 70.0, 100.0, 100.0, 120.0, 120.0),
    ('Gold Daemon Swordsman', 'Daemon', 90.0, 90.0, 110.0, 110.0, 130.0, 130.0),
    ('Ice Fiend', 'Daemon', 95.0, 95.0, 110.0, 110.0, 130.0, 130.0),
    ('the Black Daemon', 'Daemon', 110.0, 110.0, 100.0, 100.0, 120.0, 120.0),
    ('the Daemon', 'Daemon', 60.0, 60.0, 100.0, 100.0, 100.0, 100.0),
    ('(unnamed unique NPC, dragons_tmp.def)', 'Dragon/Boss', 120.0, 155.0, 120.0, 180.0, 120.0, 200.0),
    ('Bahnak', 'Dragon/Boss', 120.0, 120.0, 140.0, 140.0, 150.0, 150.0),
    ('Black Dragon', 'Dragon/Boss', 120.0, 155.0, 135.0, 140.0, 145.0, 150.0),
    ('Black Drake', 'Dragon/Boss', 95.0, 95.0, 100.0, 100.0, 110.0, 110.0),
    ('Blue Dragon', 'Dragon/Boss', 145.0, 145.0, 110.0, 110.0, 120.0, 120.0),
    ('Blue Drake', 'Dragon/Boss', 95.0, 95.0, 70.0, 70.0, 80.0, 80.0),
    ('Copper Dragon', 'Dragon/Boss', 150.0, 150.0, 115.0, 115.0, 125.0, 125.0),
    ('Copper Drake', 'Dragon/Boss', 85.0, 85.0, 80.0, 80.0, 90.0, 90.0),
    ('Dragon', 'Dragon/Boss', 7.0, 7.0, 46.0, 46.0, 46.0, 46.0),
    ('Gold Drake', 'Dragon/Boss', 87.0, 87.0, 100.0, 100.0, 120.0, 120.0),
    ('Green Dragon', 'Dragon/Boss', 135.0, 135.0, 100.0, 100.0, 110.0, 110.0),
    ('Green Drake', 'Dragon/Boss', 90.0, 90.0, 50.0, 50.0, 60.0, 60.0),
    ('Ice Drake', 'Dragon/Boss', 115.0, 115.0, 95.0, 95.0, 105.0, 105.0),
    ('Red Dragon', 'Dragon/Boss', 10.0, 10.0, 60.0, 60.0, 60.0, 60.0),
    ('Red Dragon', 'Dragon/Boss', 140.0, 140.0, 105.0, 105.0, 115.0, 115.0),
    ('Red Drake', 'Dragon/Boss', 90.0, 90.0, 60.0, 60.0, 70.0, 70.0),
    ('Silver Dragon', 'Dragon/Boss', 145.0, 145.0, 120.0, 120.0, 130.0, 130.0),
    ('Silver Drake', 'Dragon/Boss', 105.0, 105.0, 85.0, 85.0, 95.0, 95.0),
    ('Wyrm', 'Dragon/Boss', 130.0, 130.0, 160.0, 160.0, 170.0, 170.0),
    ('Wyrm', 'Dragon/Boss', 175.0, 175.0, 140.0, 140.0, 150.0, 150.0),
    ('Wyvern', 'Dragon/Boss', 125.0, 125.0, 95.0, 95.0, 105.0, 105.0),
    ('the Gold Dragon', 'Dragon/Boss', 150.0, 150.0, 125.0, 125.0, 135.0, 135.0),
    ('the Ice Dragon', 'Dragon/Boss', 165.0, 165.0, 130.0, 130.0, 140.0, 140.0),
    ('Acid Elemental', 'Elemental', 45.0, 120.0, 15.0, 75.0, 15.0, 75.0),
    ('Air Elemental', 'Elemental', 55.0, 135.0, 20.0, 85.0, 20.0, 85.0),
    ('Blood Elemental', 'Elemental', 60.0, 120.0, 20.0, 80.0, 20.0, 80.0),
    ('Earth Elemental', 'Elemental', 50.0, 100.0, 20.0, 80.0, 20.0, 80.0),
    ('Efreet', 'Elemental', 40.0, 40.0, 40.0, 40.0, 40.0, 40.0),
    ('Fire Elemental', 'Elemental', 50.0, 120.0, 20.0, 70.0, 20.0, 70.0),
    ('Forrest Elemental', 'Elemental', 50.0, 50.0, 60.0, 60.0, 60.0, 60.0),
    ('Ice Elemental', 'Elemental', 50.0, 120.0, 0.0, 90.0, 0.0, 90.0),
    ('Poison Elemental', 'Elemental', 45.0, 120.0, 15.0, 75.0, 15.0, 75.0),
    ('Rock Elemental', 'Elemental', 50.0, 50.0, 150.0, 150.0, 150.0, 150.0),
    ('Shadow Elemental', 'Elemental', 65.0, 65.0, 80.0, 80.0, 80.0, 80.0),
    ('Snow Elemental', 'Elemental', 40.0, 120.0, 25.0, 90.0, 25.0, 90.0),
    ('Water Elemental', 'Elemental', 45.0, 120.0, 20.0, 80.0, 20.0, 80.0),
    ('Ent', 'Ent', 55.0, 55.0, 20.0, 20.0, 20.0, 20.0),
    ('Arghelior', 'Evil Race (humanoid)', 45.0, 45.0, 28.0, 28.0, 28.0, 28.0),
    ('Carodejnice', 'Evil Race (humanoid)', 25.0, 30.0, 20.0, 20.0, 20.0, 20.0),
    ('Gurg Ath', 'Evil Race (humanoid)', 33.0, 33.0, 25.0, 40.0, 25.0, 40.0),
    ('Necromancer', 'Evil Race (humanoid)', 25.0, 80.0, 20.0, 80.0, 20.0, 80.0),
    ('Nurn Man', 'Evil Race (humanoid)', 20.0, 40.0, 25.0, 40.0, 25.0, 40.0),
    ('Pirat', 'Evil Race (humanoid)', 50.0, 50.0, 40.0, 40.0, 40.0, 40.0),
    ('Pirat chief', 'Evil Race (humanoid)', 90.0, 90.0, 90.0, 90.0, 90.0, 90.0),
    ('Piratka', 'Evil Race (humanoid)', 60.0, 60.0, 50.0, 50.0, 50.0, 50.0),
    ('Saur Hat', 'Evil Race (humanoid)', 25.0, 33.0, 25.0, 40.0, 25.0, 40.0),
    ('Skurut Hai', 'Evil Race (humanoid)', 33.0, 33.0, 25.0, 40.0, 25.0, 40.0),
    ('Tulak', 'Evil Race (humanoid)', 20.0, 38.0, 20.0, 20.0, 20.0, 20.0),
    ('Golem', 'Golem', 40.0, 180.0, 100.0, 180.0, 100.0, 180.0),
    ('Reaper', 'Golem', 30.0, 100.0, 60.0, 130.0, 60.0, 130.0),
    ('Spirit', 'Golem', 22.0, 72.0, 20.0, 80.0, 20.0, 80.0),
    ('Ancient Ooze', 'Insect', 40.0, 40.0, 150.0, 150.0, 150.0, 150.0),
    ('Black Serpent', 'Insect', 45.0, 45.0, 30.0, 30.0, 30.0, 30.0),
    ('Bull Frog', 'Insect', 30.0, 30.0, 8.0, 8.0, 8.0, 8.0),
    ('Cerna vdova', 'Insect', 55.0, 55.0, 100.0, 100.0, 100.0, 100.0),
    ('Frost Ooze', 'Insect', 25.0, 25.0, 150.0, 150.0, 150.0, 150.0),
    ('Giant Scorpion', 'Insect', 22.0, 22.0, 40.0, 40.0, 40.0, 40.0),
    ('Giant Serpent', 'Insect', 22.0, 22.0, 15.0, 15.0, 15.0, 15.0),
    ('Giant Toad', 'Insect', 25.0, 25.0, 12.0, 12.0, 12.0, 12.0),
    ('Gold Serpent', 'Insect', 40.0, 40.0, 20.0, 20.0, 20.0, 20.0),
    ('Ice Serpent', 'Insect', 50.0, 50.0, 30.0, 30.0, 30.0, 30.0),
    ('Lava Serpent', 'Insect', 40.0, 40.0, 20.0, 20.0, 20.0, 20.0),
    ('Magic Ooze', 'Insect', 25.0, 25.0, 150.0, 150.0, 150.0, 150.0),
    ('Poison Ooze', 'Insect', 25.0, 25.0, 150.0, 150.0, 150.0, 150.0),
    ('Poison Serpent', 'Insect', 35.0, 35.0, 20.0, 20.0, 20.0, 20.0),
    ('Scorpion King', 'Insect', 45.0, 45.0, 40.0, 40.0, 40.0, 40.0),
    ('Scorpion Prince', 'Insect', 35.0, 35.0, 40.0, 40.0, 40.0, 40.0),
    ('Scorpion Queen', 'Insect', 40.0, 40.0, 30.0, 30.0, 30.0, 30.0),
    ('Sea Serpent', 'Insect', 25.0, 25.0, 40.0, 40.0, 40.0, 40.0),
    ('Silver Serpent', 'Insect', 30.0, 30.0, 20.0, 20.0, 20.0, 20.0),
    ('Slime', 'Insect', 20.0, 20.0, 80.0, 80.0, 80.0, 80.0),
    ('Smaragd Ooze', 'Insect', 35.0, 35.0, 150.0, 150.0, 150.0, 150.0),
    ('Spider', 'Insect', 25.0, 60.0, 5.0, 21.0, 5.0, 21.0),
    ('Spider King', 'Insect', 60.0, 60.0, 33.0, 33.0, 33.0, 33.0),
    ('Spider Queen', 'Insect', 50.0, 50.0, 33.0, 33.0, 33.0, 33.0),
    ('Tarantule', 'Insect', 55.0, 55.0, 120.0, 120.0, 120.0, 120.0),
    ('Water Serpent', 'Insect', 33.0, 33.0, 20.0, 20.0, 20.0, 20.0),
    ('c_spider_elder', 'Insect', 100.0, 100.0, 150.0, 150.0, 150.0, 150.0),
    ('Headless', 'Misc Monster', 16.0, 16.0, 9.0, 9.0, 9.0, 9.0),
    ('Hellcat', 'Misc Monster', 11.0, 31.0, 15.0, 15.0, 15.0, 15.0),
    ('Imp', 'Misc Monster', 20.0, 20.0, 15.0, 15.0, 15.0, 15.0),
    ('Subterranean Mongbat', 'Misc Monster', 17.0, 27.0, 10.0, 10.0, 10.0, 10.0),
    ('Cyclop', 'Ogre', 60.0, 60.0, 70.0, 90.0, 70.0, 90.0),
    ('Cyclops King', 'Ogre', 75.0, 75.0, 140.0, 140.0, 140.0, 140.0),
    ('Ettin', 'Ogre', 30.0, 45.0, 25.0, 25.0, 25.0, 25.0),
    ('Ettin Lord', 'Ogre', 65.0, 65.0, 45.0, 45.0, 45.0, 45.0),
    ('Titan', 'Ogre', 80.0, 80.0, 95.0, 95.0, 95.0, 95.0),
    ('Titan Lord', 'Ogre', 95.0, 95.0, 105.0, 105.0, 105.0, 105.0),
    ('Titan Mage', 'Ogre', 75.0, 75.0, 105.0, 105.0, 105.0, 105.0),
    ('Troll', 'Ogre', 20.0, 45.0, 20.0, 35.0, 20.0, 35.0),
    ('Zlobr', 'Ogre', 40.0, 50.0, 50.0, 60.0, 50.0, 60.0),
    ('Zlobr Valecnik', 'Ogre', 60.0, 60.0, 85.0, 85.0, 85.0, 85.0),
    ('Jestirek', 'Orc', 15.0, 40.0, 15.0, 15.0, 15.0, 15.0),
    ('Krysak', 'Orc', 10.0, 30.0, 10.0, 10.0, 10.0, 10.0),
    ('Lizard King', 'Orc', 70.0, 70.0, 60.0, 60.0, 60.0, 60.0),
    ('Morijsky jeskynni obr', 'Orc', 100.0, 100.0, 145.0, 145.0, 145.0, 145.0),
    ('Orc', 'Orc', 25.0, 80.0, 20.0, 95.0, 20.0, 95.0),
    ('Frost Golem', 'Snow Monster', 130.0, 130.0, 120.0, 120.0, 120.0, 120.0),
    ('Ice Archer', 'Snow Monster', 60.0, 60.0, 40.0, 40.0, 40.0, 40.0),
    ('Ice Skeleton', 'Snow Monster', 75.0, 75.0, 70.0, 70.0, 70.0, 70.0),
    ('Ledovy Drapac', 'Snow Monster', 65.0, 65.0, 65.0, 65.0, 65.0, 65.0),
    ('Llama', 'Snow Monster', 4.0, 4.0, 3.0, 3.0, 3.0, 3.0),
    ('Menivec', 'Snow Monster', 140.0, 140.0, 120.0, 120.0, 120.0, 120.0),
    ('Poustevnik', 'Snow Monster', 30.0, 30.0, 40.0, 40.0, 40.0, 40.0),
    ('Slizoun', 'Snow Monster', 50.0, 50.0, 150.0, 150.0, 150.0, 150.0),
    ('Snehovy Drapac', 'Snow Monster', 65.0, 65.0, 65.0, 65.0, 65.0, 65.0),
    ('Snow Craft', 'Snow Monster', 50.0, 50.0, 40.0, 40.0, 40.0, 40.0),
    ('Snow Finger', 'Snow Monster', 25.0, 25.0, 60.0, 60.0, 60.0, 60.0),
    ('Snow Gargoyle', 'Snow Monster', 95.0, 95.0, 90.0, 90.0, 90.0, 90.0),
    ('Snow Gladiator', 'Snow Monster', 125.0, 125.0, 140.0, 140.0, 140.0, 140.0),
    ('Snow Guard', 'Snow Monster', 140.0, 140.0, 120.0, 120.0, 120.0, 120.0),
    ('Snow Harpy', 'Snow Monster', 68.0, 68.0, 70.0, 70.0, 70.0, 70.0),
    ('Snow Inscripter', 'Snow Monster', 60.0, 60.0, 40.0, 40.0, 40.0, 40.0),
    ('Snow King', 'Snow Monster', 100.0, 100.0, 100.0, 100.0, 100.0, 100.0),
    ('Snow Mage', 'Snow Monster', 70.0, 70.0, 40.0, 40.0, 40.0, 40.0),
    ('Snow Miner', 'Snow Monster', 70.0, 70.0, 40.0, 40.0, 40.0, 40.0),
    ('Snow Skeleton', 'Snow Monster', 75.0, 75.0, 70.0, 70.0, 70.0, 70.0),
    ('Snow Smith', 'Snow Monster', 70.0, 70.0, 20.0, 20.0, 20.0, 20.0),
    ('Snow Tamer', 'Snow Monster', 50.0, 50.0, 40.0, 40.0, 40.0, 40.0),
    ('Snow Tarantule', 'Snow Monster', 70.0, 70.0, 100.0, 100.0, 100.0, 100.0),
    ('Snow Titan', 'Snow Monster', 100.0, 100.0, 140.0, 140.0, 140.0, 140.0),
    ('Snow Troll', 'Snow Monster', 55.0, 55.0, 25.0, 25.0, 25.0, 25.0),
    ('Snow Warrior', 'Snow Monster', 90.0, 100.0, 50.0, 50.0, 50.0, 50.0),
    ('Snow spirit', 'Snow Monster', 82.0, 82.0, 88.0, 88.0, 88.0, 88.0),
    ('Strazce Ledu', 'Snow Monster', 110.0, 110.0, 100.0, 100.0, 100.0, 100.0),
    ('Strazce Snehu', 'Snow Monster', 110.0, 110.0, 100.0, 100.0, 100.0, 100.0),
    ('Strazce studny', 'Snow Monster', 30.0, 30.0, 40.0, 40.0, 40.0, 40.0),
    ('Zostrich', 'Snow Monster', 22.0, 22.0, 30.0, 30.0, 30.0, 30.0),
    ('Admantinum Mummy', 'Special', 85.0, 85.0, 130.0, 130.0, 130.0, 130.0),
    ('Beholder', 'Special', 9.0, 9.0, 18.0, 18.0, 18.0, 18.0),
    ('Blood Mummy', 'Special', 75.0, 75.0, 120.0, 120.0, 120.0, 120.0),
    ('Cajun', 'Special', 40.0, 60.0, 60.0, 95.0, 60.0, 95.0),
    ('Cajun Lord', 'Special', 145.0, 145.0, 100.0, 100.0, 100.0, 100.0),
    ('Elder Gazer', 'Special', 21.0, 21.0, 35.0, 35.0, 35.0, 35.0),
    ('Evil', 'Special', 120.0, 120.0, 150.0, 150.0, 150.0, 150.0),
    ('Excelent', 'Special', 140.0, 140.0, 140.0, 140.0, 140.0, 140.0),
    ('Eye', 'Special', 16.0, 30.0, 20.0, 50.0, 20.0, 50.0),
    ('Gazer', 'Special', 11.0, 11.0, 28.0, 28.0, 28.0, 28.0),
    ('Gladiator', 'Special', 80.0, 80.0, 130.0, 130.0, 130.0, 130.0),
    ('Gladiator Lord', 'Special', 120.0, 120.0, 140.0, 140.0, 140.0, 140.0),
    ('Minotaur', 'Special', 90.0, 90.0, 65.0, 65.0, 65.0, 65.0),
    ('Stone Mummy', 'Special', 60.0, 60.0, 110.0, 110.0, 110.0, 110.0),
    ('Strazce', 'Special', 100.0, 100.0, 70.0, 70.0, 70.0, 70.0),
    ('Wisp', 'Special', 25.0, 25.0, 20.0, 20.0, 20.0, 20.0),
    ('c_strazce_ohne', 'Special', 80.0, 80.0, 110.0, 110.0, 110.0, 110.0),
    ('Cerny Medved', 'Summon', 16.0, 16.0, 13.0, 13.0, 13.0, 13.0),
    ('Elemental Ohne', 'Summon', 20.0, 20.0, 20.0, 20.0, 20.0, 20.0),
    ('Elemental Smrti', 'Summon', 120.0, 120.0, 5.0, 5.0, 5.0, 5.0),
    ('Elemental Vody', 'Summon', 50.0, 50.0, 50.0, 50.0, 50.0, 50.0),
    ('Elemental Vzduchu', 'Summon', 55.0, 55.0, 60.0, 60.0, 60.0, 60.0),
    ('Elemental Zeme', 'Summon', 50.0, 50.0, 80.0, 80.0, 80.0, 80.0),
    ('Hnedy Medved', 'Summon', 15.0, 15.0, 20.0, 20.0, 20.0, 20.0),
    ('Kostej', 'Summon', 20.0, 20.0, 15.0, 15.0, 15.0, 15.0),
    ('Kostlivec', 'Summon', 9.0, 15.0, 4.0, 13.0, 4.0, 13.0),
    ('Kostlivy Lucistnik', 'Summon', 17.0, 17.0, 8.0, 8.0, 8.0, 8.0),
    ('Kostlivy Mag', 'Summon', 15.0, 15.0, 7.0, 7.0, 7.0, 7.0),
    ('Kostlivy Sekernik', 'Summon', 16.0, 16.0, 10.0, 10.0, 10.0, 10.0),
    ('Kun', 'Summon', 4.0, 4.0, 10.0, 10.0, 10.0, 10.0),
    ('Ledni Medved', 'Summon', 8.0, 28.0, 18.0, 30.0, 18.0, 30.0),
    ('Lich', 'Summon', 16.0, 16.0, 20.0, 20.0, 20.0, 20.0),
    ('Lord Lich', 'Summon', 30.0, 30.0, 40.0, 40.0, 40.0, 40.0),
    ('Medved Grizzly', 'Summon', 20.0, 20.0, 15.0, 15.0, 15.0, 15.0),
    ('Mumie', 'Summon', 30.0, 30.0, 80.0, 80.0, 80.0, 80.0),
    ('Obri Krysa', 'Summon', 10.0, 10.0, 5.0, 5.0, 5.0, 5.0),
    ('Obri Skorpion', 'Summon', 22.0, 22.0, 15.0, 15.0, 15.0, 15.0),
    ('Prizrak', 'Summon', 20.0, 20.0, 60.0, 60.0, 60.0, 60.0),
    ('Satan', 'Summon', 50.0, 80.0, 60.0, 70.0, 60.0, 70.0),
    ('Temny Kun', 'Summon', 4.0, 4.0, 10.0, 10.0, 10.0, 10.0),
    ('Temny Vampir', 'Summon', 20.0, 20.0, 40.0, 40.0, 40.0, 40.0),
    ('Vampir', 'Summon', 25.0, 25.0, 25.0, 25.0, 25.0, 25.0),
    ('Vortex', 'Summon', 70.0, 70.0, 50.0, 50.0, 50.0, 50.0),
    ('Zombik', 'Summon', 10.0, 10.0, 6.0, 6.0, 6.0, 6.0),
    ('c_blade_spirit', 'Summon', 40.0, 40.0, 40.0, 40.0, 40.0, 40.0),
    ('c_daemon_mage1', 'Summon', 65.0, 65.0, 70.0, 70.0, 70.0, 70.0),
    ('c_daemon_sword1', 'Summon', 65.0, 65.0, 60.0, 60.0, 60.0, 60.0),
    ('Beetle Giant', 'T2A Dungeon', 40.0, 40.0, 40.0, 40.0, 40.0, 40.0),
    ('Fireant Daemon', 'T2A Dungeon', 60.0, 60.0, 100.0, 100.0, 100.0, 100.0),
    ('Fireant Mage', 'T2A Dungeon', 40.0, 40.0, 40.0, 40.0, 40.0, 40.0),
    ('Fireant Queen', 'T2A Dungeon', 100.0, 100.0, 100.0, 100.0, 100.0, 100.0),
    ('Fireant Warrior', 'T2A Dungeon', 60.0, 60.0, 80.0, 80.0, 80.0, 80.0),
    ('Fireant Worker', 'T2A Dungeon', 40.0, 40.0, 60.0, 60.0, 60.0, 60.0),
    ('Giant Bug', 'T2A Dungeon', 71.0, 71.0, 140.0, 140.0, 140.0, 140.0),
    ('Ophidian Archmage', 'T2A Dungeon', 60.0, 60.0, 32.0, 32.0, 32.0, 32.0),
    ('Ophidian King', 'T2A Dungeon', 90.0, 90.0, 80.0, 80.0, 80.0, 80.0),
    ('Ophidian Knight', 'T2A Dungeon', 80.0, 80.0, 30.0, 30.0, 30.0, 30.0),
    ('Ophidian Mage', 'T2A Dungeon', 40.0, 40.0, 35.0, 35.0, 35.0, 35.0),
    ('Ophidian Queen', 'T2A Dungeon', 80.0, 80.0, 35.0, 35.0, 35.0, 35.0),
    ('Ophidian Warrior', 'T2A Dungeon', 50.0, 50.0, 60.0, 60.0, 60.0, 60.0),
    ('Terathan Avenger', 'T2A Dungeon', 30.0, 30.0, 25.0, 25.0, 25.0, 25.0),
    ('Terathan Drone', 'T2A Dungeon', 40.0, 40.0, 35.0, 35.0, 35.0, 35.0),
    ('Terathan Matriarch', 'T2A Dungeon', 30.0, 30.0, 35.0, 35.0, 35.0, 35.0),
    ('Terathan Warrior', 'T2A Dungeon', 50.0, 50.0, 25.0, 25.0, 25.0, 25.0),
    ('Bone Dragon', 'Undead', 140.0, 140.0, 140.0, 140.0, 140.0, 140.0),
    ('Death Elemental', 'Undead', 35.0, 35.0, 20.0, 20.0, 20.0, 20.0),
    ('Elder Shadow', 'Undead', 110.0, 110.0, 80.0, 80.0, 80.0, 80.0),
    ('Fext', 'Undead', 40.0, 40.0, 20.0, 20.0, 20.0, 20.0),
    ('Ghost', 'Undead', 13.0, 13.0, 20.0, 20.0, 20.0, 20.0),
    ('Ghoul', 'Undead', 26.0, 26.0, 15.0, 15.0, 15.0, 15.0),
    ('Kostlivec Carodej', 'Undead', 12.0, 45.0, 10.0, 60.0, 10.0, 60.0),
    ('Kostlivec Lucistnik', 'Undead', 10.0, 45.0, 10.0, 60.0, 10.0, 60.0),
    ('Kostlivec Nesmrtelny', 'Undead', 100.0, 100.0, 110.0, 110.0, 110.0, 110.0),
    ('Kostlivec Rytir', 'Undead', 25.0, 40.0, 25.0, 45.0, 25.0, 45.0),
    ('Kostlivec Sekernik', 'Undead', 10.0, 60.0, 10.0, 60.0, 10.0, 60.0),
    ('Kostlivec Valecnik', 'Undead', 11.0, 16.0, 14.0, 18.0, 14.0, 18.0),
    ('Kostlivec Velmistr', 'Undead', 75.0, 75.0, 70.0, 70.0, 70.0, 70.0),
    ('Kral Nemrtvych', 'Undead', 110.0, 110.0, 80.0, 80.0, 80.0, 80.0),
    ('Liche', 'Undead', 24.0, 24.0, 20.0, 20.0, 20.0, 20.0),
    ('Liche King', 'Undead', 60.0, 60.0, 60.0, 60.0, 60.0, 60.0),
    ('Liche Lord', 'Undead', 35.0, 35.0, 40.0, 40.0, 40.0, 40.0),
    ('Mummy', 'Undead', 28.0, 38.0, 40.0, 40.0, 40.0, 40.0),
    ('Skull head', 'Undead', 12.0, 12.0, 20.0, 25.0, 20.0, 25.0),
    ('Spectre', 'Undead', 22.0, 52.0, 10.0, 10.0, 10.0, 10.0),
    ('Undead Horse', 'Undead', 35.0, 35.0, 50.0, 50.0, 50.0, 50.0),
    ('Wraith', 'Undead', 9.0, 9.0, 15.0, 15.0, 15.0, 15.0),
    ('Zombie', 'Undead', 16.0, 16.0, 15.0, 15.0, 15.0, 15.0),
    ('(unnamed unique NPC, specNPCVerbatim.def)', 'Unique/Boss (named NPC)', 18.0, 45.0, 25.0, 60.0, 25.0, 60.0),
    ('(unnamed unique NPC, specNPCVypravec.def)', 'Unique/Boss (named NPC)', 19.0, 19.0, 60.0, 60.0, 60.0, 60.0),
    ('(unnamed unique NPC, specNPCqv2.def)', 'Unique/Boss (named NPC)', 100.0, 100.0, 110.0, 110.0, 110.0, 110.0),
    ('(unnamed unique NPC, specNPCmandos.def)', 'Unique/Boss (named NPC)', 0.0, 0.0, 0.0, 0.0, 0.0, 0.0),
    ('Acid Phoenix', 'Unique/Boss (named NPC)', 100.0, 100.0, 55.0, 55.0, 55.0, 55.0),
    ('Ajda', 'Unique/Boss (named NPC)', 7.0, 7.0, 40.0, 40.0, 40.0, 40.0),
    ('Ant', 'Unique/Boss (named NPC)', 80.0, 80.0, 90.0, 90.0, 90.0, 90.0),
    ('Ant Daemon', 'Unique/Boss (named NPC)', 60.0, 60.0, 100.0, 100.0, 100.0, 100.0),
    ('Ant Elder', 'Unique/Boss (named NPC)', 60.0, 60.0, 70.0, 70.0, 70.0, 70.0),
    ('Ant Lord', 'Unique/Boss (named NPC)', 110.0, 110.0, 100.0, 100.0, 100.0, 100.0),
    ('Aresion', 'Unique/Boss (named NPC)', 120.0, 120.0, 150.0, 150.0, 150.0, 150.0),
    ('Blood Eye', 'Unique/Boss (named NPC)', 60.0, 60.0, 0.0, 0.0, 0.0, 0.0),
    ('Blood King', 'Unique/Boss (named NPC)', 100.0, 100.0, 150.0, 150.0, 150.0, 150.0),
    ('Blood spirit', 'Unique/Boss (named NPC)', 35.0, 35.0, 38.0, 38.0, 38.0, 38.0),
    ('Bloody Eye', 'Unique/Boss (named NPC)', 60.0, 60.0, 20.0, 20.0, 20.0, 20.0),
    ('Carodejka Strachu', 'Unique/Boss (named NPC)', 120.0, 120.0, 40.0, 40.0, 40.0, 40.0),
    ('Caveant', 'Unique/Boss (named NPC)', 80.0, 80.0, 90.0, 90.0, 90.0, 90.0),
    ('Caveant Daemon', 'Unique/Boss (named NPC)', 60.0, 60.0, 100.0, 100.0, 100.0, 100.0),
    ('Caveant Elder', 'Unique/Boss (named NPC)', 60.0, 60.0, 70.0, 70.0, 70.0, 70.0),
    ('Caveant Lord', 'Unique/Boss (named NPC)', 110.0, 110.0, 100.0, 100.0, 100.0, 100.0),
    ('Cernokneznik Hruzy', 'Unique/Boss (named NPC)', 120.0, 120.0, 80.0, 90.0, 80.0, 90.0),
    ('Chrozountar', 'Unique/Boss (named NPC)', 10.0, 10.0, 10.0, 10.0, 10.0, 10.0),
    ('Chrozountar Nicitel', 'Unique/Boss (named NPC)', 10.0, 10.0, 10.0, 10.0, 10.0, 10.0),
    ('Chrozountar starsi', 'Unique/Boss (named NPC)', 10.0, 10.0, 10.0, 10.0, 10.0, 10.0),
    ('Detheanorus, smutny bard', 'Unique/Boss (named NPC)', 60.0, 60.0, 80.0, 80.0, 80.0, 80.0),
    ('Divoka Kocka', 'Unique/Boss (named NPC)', 1.0, 1.0, 4.0, 4.0, 4.0, 4.0),
    ('Drone', 'Unique/Boss (named NPC)', 20.0, 20.0, 85.0, 85.0, 85.0, 85.0),
    ('Duch Runy', 'Unique/Boss (named NPC)', 70.0, 150.0, 90.0, 100.0, 90.0, 100.0),
    ('Elder Drone', 'Unique/Boss (named NPC)', 135.0, 135.0, 85.0, 85.0, 85.0, 85.0),
    ('Etheria Warrior', 'Unique/Boss (named NPC)', 17.0, 17.0, 25.0, 25.0, 25.0, 25.0),
    ('Etherijsky strazce', 'Unique/Boss (named NPC)', 120.0, 120.0, 150.0, 150.0, 150.0, 150.0),
    ('Eye', 'Unique/Boss (named NPC)', 25.0, 120.0, 20.0, 100.0, 20.0, 100.0),
    ('Eye - Invisible Eye', 'Unique/Boss (named NPC)', 80.0, 80.0, 40.0, 40.0, 40.0, 40.0),
    ('Falesna Kocka', 'Unique/Boss (named NPC)', 80.0, 80.0, 35.0, 35.0, 35.0, 35.0),
    ('Falesna Koza', 'Unique/Boss (named NPC)', 45.0, 45.0, 60.0, 60.0, 60.0, 60.0),
    ('Falesna Krava', 'Unique/Boss (named NPC)', 55.0, 55.0, 100.0, 120.0, 100.0, 120.0),
    ('Falesna Krysa', 'Unique/Boss (named NPC)', 60.0, 60.0, 32.0, 32.0, 32.0, 32.0),
    ('Falesna Llama', 'Unique/Boss (named NPC)', 40.0, 160.0, 100.0, 160.0, 100.0, 160.0),
    ('Falesna Slepice', 'Unique/Boss (named NPC)', 110.0, 110.0, 150.0, 150.0, 180.0, 180.0),
    ('Falesna Srnka', 'Unique/Boss (named NPC)', 110.0, 110.0, 80.0, 80.0, 80.0, 80.0),
    ('Falesna Zaba', 'Unique/Boss (named NPC)', 60.0, 60.0, 85.0, 85.0, 85.0, 85.0),
    ('Falesne Prase', 'Unique/Boss (named NPC)', 120.0, 120.0, 140.0, 140.0, 140.0, 140.0),
    ('Falesnej Medved', 'Unique/Boss (named NPC)', 90.0, 90.0, 65.0, 65.0, 65.0, 65.0),
    ('Falesnej Mroz', 'Unique/Boss (named NPC)', 140.0, 140.0, 140.0, 140.0, 140.0, 140.0),
    ('Falesnej Pes', 'Unique/Boss (named NPC)', 90.0, 90.0, 80.0, 80.0, 80.0, 80.0),
    ('Falesnej Sob', 'Unique/Boss (named NPC)', 80.0, 120.0, 130.0, 150.0, 130.0, 150.0),
    ('Falesnej Tygr', 'Unique/Boss (named NPC)', 80.0, 80.0, 30.0, 30.0, 30.0, 30.0),
    ('Falesnej Vul', 'Unique/Boss (named NPC)', 30.0, 30.0, 22.0, 22.0, 22.0, 22.0),
    ('Falesny Papousek', 'Unique/Boss (named NPC)', 90.0, 90.0, 110.0, 110.0, 130.0, 130.0),
    ('Fanatik', 'Unique/Boss (named NPC)', 155.0, 155.0, 60.0, 60.0, 60.0, 60.0),
    ('Fire Lord', 'Unique/Boss (named NPC)', 145.0, 145.0, 100.0, 100.0, 100.0, 100.0),
    ('Fire Phoenix', 'Unique/Boss (named NPC)', 100.0, 100.0, 55.0, 55.0, 55.0, 55.0),
    ('Forrest Elemental', 'Unique/Boss (named NPC)', 18.0, 18.0, 30.0, 30.0, 30.0, 30.0),
    ('Had', 'Unique/Boss (named NPC)', 140.0, 140.0, 70.0, 70.0, 70.0, 70.0),
    ('Had', 'Unique/Boss (named NPC)', 140.0, 140.0, 70.0, 70.0, 70.0, 70.0),
    ('Harfan', 'Unique/Boss (named NPC)', 60.0, 60.0, 85.0, 85.0, 85.0, 85.0),
    ('Harfan', 'Unique/Boss (named NPC)', 130.0, 170.0, 110.0, 160.0, 110.0, 160.0),
    ('Hejkal', 'Unique/Boss (named NPC)', 25.0, 25.0, 85.0, 85.0, 85.0, 85.0),
    ('Hejkal', 'Unique/Boss (named NPC)', 42.0, 132.0, 35.0, 80.0, 35.0, 80.0),
    ('Hornicky predak', 'Unique/Boss (named NPC)', 30.0, 30.0, 40.0, 40.0, 40.0, 40.0),
    ('Imp', 'Unique/Boss (named NPC)', 50.0, 50.0, 25.0, 25.0, 25.0, 25.0),
    ('Imp', 'Unique/Boss (named NPC)', 50.0, 50.0, 25.0, 25.0, 25.0, 25.0),
    ('Infikovana krysa', 'Unique/Boss (named NPC)', 75.0, 75.0, 45.0, 45.0, 45.0, 45.0),
    ('Kouzelnik Strachu', 'Unique/Boss (named NPC)', 140.0, 140.0, 40.0, 40.0, 40.0, 40.0),
    ('Kracejici hruza', 'Unique/Boss (named NPC)', 180.0, 180.0, 25.0, 25.0, 25.0, 25.0),
    ('Kralovsky zrizenec', 'Unique/Boss (named NPC)', 60.0, 60.0, 80.0, 80.0, 80.0, 80.0),
    ('Krvavy Drapac', 'Unique/Boss (named NPC)', 60.0, 60.0, 80.0, 80.0, 80.0, 80.0),
    ('Krvavy prisluhovac', 'Unique/Boss (named NPC)', 73.0, 73.0, 72.0, 72.0, 72.0, 72.0),
    ('Lady Velera', 'Unique/Boss (named NPC)', 121.0, 121.0, 120.0, 120.0, 120.0, 120.0),
    ('Lord Merxis', 'Unique/Boss (named NPC)', 100.0, 100.0, 150.0, 150.0, 150.0, 150.0),
    ('Lost Soul', 'Unique/Boss (named NPC)', 60.0, 60.0, 90.0, 90.0, 90.0, 90.0),
    ('Maarg', 'Unique/Boss (named NPC)', 150.0, 150.0, 100.0, 100.0, 100.0, 100.0),
    ('Marghuluv zmocnenec', 'Unique/Boss (named NPC)', 60.0, 60.0, 80.0, 80.0, 80.0, 80.0),
    ('Menivec', 'Unique/Boss (named NPC)', 70.0, 140.0, 80.0, 100.0, 80.0, 100.0),
    ('Micka', 'Unique/Boss (named NPC)', 5.0, 5.0, 20.0, 20.0, 20.0, 20.0),
    ('Midnight Elemental', 'Unique/Boss (named NPC)', 55.0, 55.0, 60.0, 60.0, 60.0, 60.0),
    ('Mitabref', 'Unique/Boss (named NPC)', 40.0, 50.0, 50.0, 50.0, 50.0, 50.0),
    ('Nemrtvy Strazce', 'Unique/Boss (named NPC)', 150.0, 150.0, 90.0, 90.0, 90.0, 90.0),
    ('Nexmirias Archer', 'Unique/Boss (named NPC)', 60.0, 60.0, 40.0, 40.0, 40.0, 40.0),
    ('Nexmirias Mage', 'Unique/Boss (named NPC)', 70.0, 70.0, 40.0, 40.0, 40.0, 40.0),
    ('Nexmirias Warrior', 'Unique/Boss (named NPC)', 50.0, 90.0, 60.0, 60.0, 60.0, 60.0),
    ('Ohnivy Fanatik', 'Unique/Boss (named NPC)', 150.0, 150.0, 70.0, 70.0, 70.0, 70.0),
    ('Osoba', 'Unique/Boss (named NPC)', 35.0, 35.0, 20.0, 20.0, 20.0, 20.0),
    ('Pachnouci krysa', 'Unique/Boss (named NPC)', 75.0, 75.0, 45.0, 45.0, 45.0, 45.0),
    ('Phantom', 'Unique/Boss (named NPC)', 6.0, 9.0, 20.0, 20.0, 20.0, 20.0),
    ('Prevoznik', 'Unique/Boss (named NPC)', 60.0, 60.0, 80.0, 80.0, 80.0, 80.0),
    ('Reaper', 'Unique/Boss (named NPC)', 100.0, 100.0, 100.0, 100.0, 100.0, 100.0),
    ('Reznik', 'Unique/Boss (named NPC)', 85.0, 85.0, 45.0, 45.0, 45.0, 45.0),
    ('Rock Elemental', 'Unique/Boss (named NPC)', 18.0, 18.0, 50.0, 50.0, 50.0, 50.0),
    ('Ropucha', 'Unique/Boss (named NPC)', 10.0, 10.0, 18.0, 18.0, 18.0, 18.0),
    ('Shagar Guardian', 'Unique/Boss (named NPC)', 60.0, 60.0, 89.0, 89.0, 89.0, 89.0),
    ('Shagar Guardian', 'Unique/Boss (named NPC)', 90.0, 90.0, 99.0, 99.0, 99.0, 99.0),
    ('Shagar elemental', 'Unique/Boss (named NPC)', 49.0, 49.0, 99.0, 99.0, 99.0, 99.0),
    ('Shagarsky Bojovnik - Shagar (archer)', 'Unique/Boss (named NPC)', 70.0, 70.0, 70.0, 70.0, 70.0, 70.0),
    ('Shagarsky Bojovnik - Shagar (mage)', 'Unique/Boss (named NPC)', 60.0, 60.0, 70.0, 70.0, 70.0, 70.0),
    ('Shagarsky Bojovnik - Shagar (war)', 'Unique/Boss (named NPC)', 85.0, 85.0, 70.0, 70.0, 70.0, 70.0),
    ('Shagarsky bojovnik', 'Unique/Boss (named NPC)', 70.0, 95.0, 70.0, 80.0, 70.0, 80.0),
    ('Silvanie', 'Unique/Boss (named NPC)', 75.0, 75.0, 85.0, 85.0, 85.0, 85.0),
    ('Skull head', 'Unique/Boss (named NPC)', 25.0, 25.0, 20.0, 20.0, 20.0, 20.0),
    ('Sluzebnice Kristalu', 'Unique/Boss (named NPC)', 80.0, 80.0, 140.0, 140.0, 140.0, 140.0),
    ('Sluzebnik Kristalu', 'Unique/Boss (named NPC)', 80.0, 80.0, 140.0, 140.0, 140.0, 140.0),
    ('Smoke Elemental', 'Unique/Boss (named NPC)', 60.0, 60.0, 80.0, 80.0, 80.0, 80.0),
    ('Snow Mairacle', 'Unique/Boss (named NPC)', 70.0, 70.0, 40.0, 40.0, 40.0, 40.0),
    ('Snuffer', 'Unique/Boss (named NPC)', 50.0, 50.0, 22.0, 22.0, 22.0, 22.0),
    ('Spirit', 'Unique/Boss (named NPC)', 22.0, 22.0, 65.0, 65.0, 65.0, 65.0),
    ('Spirit', 'Unique/Boss (named NPC)', 80.0, 80.0, 80.0, 80.0, 80.0, 80.0),
    ('Strazce Mrtvich', 'Unique/Boss (named NPC)', 0.0, 0.0, 0.0, 0.0, 0.0, 0.0),
    ('Strazce pracovniho tabora', 'Unique/Boss (named NPC)', 0.0, 0.0, 0.0, 0.0, 0.0, 0.0),
    ('Strazny golem', 'Unique/Boss (named NPC)', 55.0, 55.0, 60.0, 60.0, 300.0, 300.0),
    ('Summon golem', 'Unique/Boss (named NPC)', 55.0, 55.0, 60.0, 60.0, 300.0, 300.0),
    ('Sun Archer', 'Unique/Boss (named NPC)', 60.0, 60.0, 40.0, 40.0, 40.0, 40.0),
    ('Sun Mage', 'Unique/Boss (named NPC)', 70.0, 70.0, 40.0, 40.0, 40.0, 40.0),
    ('Sun Warrior', 'Unique/Boss (named NPC)', 100.0, 100.0, 20.0, 20.0, 20.0, 20.0),
    ('Supporter maga', 'Unique/Boss (named NPC)', 50.0, 50.0, 85.0, 85.0, 85.0, 85.0),
    ('Supporter necra', 'Unique/Boss (named NPC)', 50.0, 50.0, 85.0, 85.0, 85.0, 85.0),
    ('SwARPUS', 'Unique/Boss (named NPC)', 50.0, 50.0, 180.0, 180.0, 180.0, 180.0),
    ('Tearus', 'Unique/Boss (named NPC)', 10.0, 10.0, 15.0, 15.0, 15.0, 15.0),
    ('Troll', 'Unique/Boss (named NPC)', 18.0, 18.0, 50.0, 50.0, 50.0, 50.0),
    ('Uhlir', 'Unique/Boss (named NPC)', 5.0, 5.0, 5.0, 5.0, 5.0, 5.0),
    ('VAMPIRE', 'Unique/Boss (named NPC)', 100.0, 155.0, 60.0, 110.0, 60.0, 110.0),
    ('Vladce Run', 'Unique/Boss (named NPC)', 100.0, 100.0, 140.0, 140.0, 140.0, 140.0),
    ('Vodni ropucha', 'Unique/Boss (named NPC)', 75.0, 75.0, 55.0, 55.0, 55.0, 55.0),
    ('Water Eye', 'Unique/Boss (named NPC)', 60.0, 60.0, 0.0, 0.0, 0.0, 0.0),
    ('Water man', 'Unique/Boss (named NPC)', 110.0, 110.0, 130.0, 130.0, 130.0, 130.0),
    ('Water spirit', 'Unique/Boss (named NPC)', 35.0, 35.0, 38.0, 38.0, 38.0, 38.0),
    ('Xillyon', 'Unique/Boss (named NPC)', 160.0, 160.0, 160.0, 160.0, 160.0, 160.0),
    ('Zivy Pisek', 'Unique/Boss (named NPC)', 25.0, 25.0, 100.0, 100.0, 100.0, 100.0),
    ('Zkuseny balzamovac', 'Unique/Boss (named NPC)', 20.0, 20.0, 40.0, 40.0, 40.0, 40.0),
    ('cerv', 'Unique/Boss (named NPC)', 14.0, 14.0, 15.0, 15.0, 15.0, 15.0),
    ('serpent druid', 'Unique/Boss (named NPC)', 75.0, 75.0, 80.0, 80.0, 80.0, 80.0),
    ('the Almighty', 'Unique/Boss (named NPC)', 100.0, 100.0, 135.0, 135.0, 135.0, 135.0),
    ('the Blockerka', 'Unique/Boss (named NPC)', 100.0, 100.0, 135.0, 135.0, 135.0, 135.0),
    ('the Knihovnik', 'Unique/Boss (named NPC)', 100.0, 100.0, 135.0, 135.0, 135.0, 135.0),
    ('the Owner', 'Unique/Boss (named NPC)', 110.0, 110.0, 135.0, 135.0, 135.0, 135.0),
    ('the Pan Noci', 'Unique/Boss (named NPC)', 100.0, 100.0, 135.0, 135.0, 135.0, 135.0),
    ('the Player Killer', 'Unique/Boss (named NPC)', 100.0, 100.0, 135.0, 135.0, 135.0, 135.0),
    ('vezen Brinbor', 'Unique/Boss (named NPC)', 100.0, 100.0, 95.0, 95.0, 95.0, 95.0),
]

MONSTER_DEFAULTS = dict(
    max_hits=100,  # scripts/Basic/CharClasses/NPC.Generated.cs:151 (Int16 maxHitpoints = 100) - flat for every monster, no .def override found
    weapon_delay=2.0,  # scripts/Basic/CharClasses/NPC.Generated.cs:44 (InitTypedField("weaponDelay", 2.0, ...)) - flat for every monster, no .def override found
)

WEAPON_NAMES = [w[0] for w in WEAPON_ARCHETYPES]
MATERIAL_NAMES_FOR_WEAPONS = [m[1] for m in WEAPON_TIERS]
MATERIAL_NAMES_FOR_ARMOR = [m[1] for m in METAL_TIERS]



def sheet_ref(title):
    return f"'{title}'" if " " in title else title


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
        "(and sometimes line) that number comes from in the engine, and what you'd need to do to change it for real "
        "(edit a .cs constant + rebuild + restart, vs. edit a .def content field + restart, vs. a live GM .settings command).",
        "Per-profession tabs have a Calculator block (type in stats/skills/weapon/material/target to see live damage/"
        "DPS/time-to-kill) and a Reference matrix block below it (every weapon or spell at a fixed baseline, for "
        "at-a-glance comparison across materials).",
        "PvM/PvP/MvP axis: PvM = this profession's player attacking a Monster. PvP = attacking a Player. MvP = a "
        "Monster attacking this profession's player (defender side). MvM (Monster-vs-Monster) involves no profession "
        "at all - it's shown once, as a reference row, on the Core Formulas tab.",
    ]:
        row = note(ws, row, 1, "- " + line, span=8)
    row += 1

    row = h2(ws, row, 1, "Key engine findings worth knowing before you read the numbers", span=6)
    for line in [
        "All 15 Resist* stats (Physical/Magic/Fire/Cold/Poison/Electric/Mystical/etc.) are currently 0 for every "
        "character in real game content - no armor, race, or buff sets them; only test code ever does. The "
        "calculator's resist inputs default to 0 to match this, but stay editable so you can model hypothetical "
        "values.",
        "Bows and crossbows IGNORE their own material for damage - RangerSettings flatly overrides bow/crossbow "
        "AttackVsP/VsM regardless of tier. Only the loaded ammo's material changes ranged damage (a small per-tier "
        "bonus). See the Ranger tab.",
        "No weapon .def sets a MindPowerVsP/VsM value, so every staff a Mystic wields currently falls back to the "
        "same bare-hands default (700) - materials don't currently differentiate Mystic weapon damage either. See "
        "the Mystic tab.",
        "Resisting Spells (MagicResist)'s EFFECT curve is commented out in content, so its contribution to Mystic's "
        "mind-defense is 0 regardless of skill level today - only worn armor's MindDefense stat does anything.",
        "MvP (Monster attacks Player) uses a random 0.85-1.00 damage multiplier per real swing, unlike the other "
        "three combos which are fixed constants - shown as Min/Avg/Max columns throughout.",
        "Silver and Gold are real, fully-stocked armor material tiers with no weapon or wood equivalent - included "
        "in armor tables, absent from weapon tables.",
        "Thief is a real, playable profession today (not just a design doc) with ~6 working abilities - but about "
        "half its talent tree is unimplemented scaffolding. The Thief tab marks each ability Implemented Y/N.",
    ]:
        row = note(ws, row, 1, "- " + line, span=8)
    row += 1

    row = h2(ws, row, 1, "Tabs in this workbook", span=3)
    row = header_row(ws, row, 1, ["Tab", "Contents"])
    for r in [
        ("Materials", "The 9 real material tiers, metal+wood names, which have weapons/armor/ammo"),
        ("Core Formulas", "Every shared formula + the full constants table (single source of truth)"),
        ("Weapons Reference", "Full weapon archetype x material matrix"),
        ("Armor Reference", "Full armor piece x material matrix + shields"),
        ("Warrior / Ranger / Mage / Mystic / Thief", "Calculator + reference matrix per profession"),
    ]:
        row = data_rows(ws, row, 1, [r])
    autosize(ws, {1: 34, 2: 110})
    for rw in ws.iter_rows():
        for c in rw:
            if c.column == 2 and c.row > row - 6:
                c.alignment = WRAP


def build_materials(wb):
    ws = wb.create_sheet("Materials")
    row = h1(ws, 1, 1, "Materials", span=6)
    row = note(ws, row, 1,
               "Material is ONE shared enum between metal and wood - each wood name is a literal alias for the same "
               "numeric tier as a metal (MaterialType distinguishes which is which). Source: scripts/Basic/Enums.cs:267-279.",
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
        vcell = ws.cell(row=row, column=2, value=value)
        ws.cell(row=row, column=3, value=meaning).alignment = WRAP
        ws.cell(row=row, column=4, value=source)
        ws.cell(row=row, column=5, value=howto).alignment = WRAP
        wb.defined_names[safe_name] = _defined_name(ws, safe_name, row, 2)
        const_name_to_cell[name] = f"C_{name.replace('.', '_')}"
        row += 1
    row += 1

    row = h2(ws, row, 1, "Body-zone armor weights (players only - monsters use a flat ArmorVsP/VsM stat instead)", span=4)
    row = header_row(ws, row, 1, ["Zone", "Weight", "Layers feeding it"])
    for zone, weight, layers in BODY_ZONES:
        ws.cell(row=row, column=1, value=zone)
        ws.cell(row=row, column=2, value=weight)
        c = ws.cell(row=row, column=3, value=layers)
        c.alignment = WRAP
        row += 1
    row = note(ws, row, 1, "Source: CombatCalculator.cs:~221-251 (CalculateCombatArmorValues). Per-slot aggregation is MAX not SUM across overlapping layers, then this weighting is applied. Shield is handled separately (see formula text below), not as a body zone.", span=6)
    row += 1

    row = h2(ws, row, 1, "The PvM / PvP / MvP / MvM attack-and-armor multiplier table", span=6)
    row = note(ws, row, 1, "This is the formula that actually implements the 4-way split - both which attack value (WeaponAttackVsP vs VsM) and which settings multipliers apply depend independently on attacker-is-player and defender-is-player. Source: DamageManager.GetWeaponSwingArgs, scripts/Basic/skills/Combat/DamageManager.cs:240-268.", span=6)
    row = header_row(ws, row, 1, ["Combo", "Attack formula", "Armor-class formula", "damageMod"])
    combo_rows = [
        ("PvM (Player attacks Monster)", "weapAttack x attacker.WeaponAttackVsM x weapAttackVsM", "defender.ArmorClassVsM x armorClassM", "swingDamageM (fixed 1.000)"),
        ("PvP (Player attacks Player)", "weapAttack x weapAttackPvP x attacker.WeaponAttackVsP", "defender.ArmorClassVsP x armorClassP", "swingDamagePvP (fixed 0.900)"),
        ("MvP (Monster attacks Player)", "weapAttack x weapAttackM x attacker.WeaponAttackVsP", "defender.ArmorClassVsP x armorClassP", "RANDOM swingDamageRandMvPMin..Max (0.850-1.000)"),
        ("MvM (Monster attacks Monster) - reference only, no profession involved", "weapAttack x weapAttackM x attacker.WeaponAttackVsM x weapAttackVsM", "defender.ArmorClassVsM x armorClassM", "swingDamageM (fixed 1.000)"),
    ]
    for r in combo_rows:
        row = data_rows(ws, row, 1, [r])
    row += 1

    row = h2(ws, row, 1, "Formula reference (text) - exact expressions, for anyone extending this workbook", span=6)
    formula_texts = [
        ("Weapon attack value (player, melee)", "sum = (Tactics_Effect + Anatomy_Effect + ArmsLore_Effect + Str*attackStrModifier) / 1000; WeaponAttackVsP = weapon.AttackVsP * sum; WeaponAttackVsM = weapon.AttackVsM * sum", "CombatCalculator.CalculateCombatWeaponValues:460-492"),
        ("Weapon attack value (NPC/monster)", "WeaponAttackVsM = npcDef.WeaponAttack (flat, no skill/stat scaling); WeaponAttackVsP = WeaponAttackVsM", "CombatCalculator.CalculateCombatWeaponValues:516-519"),
        ("Mind-power attack value (Mystic)", "sum = (EvalInt_Effect + SpiritSpeak_Effect + Int*mindPowerIntModifier) / 1000; mindPowerVsP/VsM = weapon.MindPowerVsP/VsM * sum", "CombatCalculator.CalculateCombatWeaponValues:487-492"),
        ("Armor class value (player)", "ArmorClassVsP = DefForCombat.ArmorVsP + round(sum(zone_value_VsP * zone_weight)) + ArmorClassModifier (ArmorVsM mirrors with VsM fields); shield ADDS on top at full value (parrying-scaled), not body-zoned", "CombatCalculator.CalculateCombatArmorValues:25-289"),
        ("Armor class value (monster)", "ArmorClassVsP/VsM = flat npcDef.ArmorVsP/VsM + ArmorClassModifier - no per-slot computation", "CombatCalculator.CalculateCombatArmorValues:25-289"),
        ("Damage after armor (any melee/ranged attacker)", "armor = MAX(0, armorClass * (1000-piercing)/1000); attack = MAX(0, attack); damageMod = MAX(0, damageMod); damageAfterAC = MAX(0, (attack - armor) * damageMod)", "CombatCalculator.CalculateArmorClassEffect:599-624"),
        ("Mind-power damage (Mystic, replaces the armor step above)", "damageAfterMindDefense = MAX(0, (mindPower - mindDefense) * damageMod)", "CombatCalculator.CalculateMindPowerEffect:632-652"),
        ("Mage spell damage (bypasses armor/AC entirely)", "spellPower = UseMindPower ? (caster.MindPowerVsX - target.MindDefenseVsX)*10 : caster's raw Magery skill; apply Standcast (1+bonus) then Magic Charge (1+bonus) multipliers in sequence; dam = EvalRangePermille(finalPower, spell's EFFECT=min,max)", "SpellDef.cs:610-656, GetSpellPowerAgainstChar:~628"),
        ("Resist modifier (applied to EVERY damage source, always, in CauseDamage)", "modifier = 1 - ResistBroad(Magic or Physical); modifier = modifier - ResistNarrow(the matching one of 13 sub-types); damage = MAX(0, damage * modifier)", "DamageManager.GetResistModifier:28-80, CauseDamage:~114"),
        ("Weapon delay / attack speed (non-Mystic)", "delay = SQRT(Dex) * weapSpeed * weaponSpeedGlobal; seconds = 1048.575 / delay", "CombatCalculator.CalculateCombatWeaponValues:460-477"),
        ("Weapon delay / attack speed (Mystic)", "delay = SQRT(MAX(Mana,1)/2) * weapSpeed * mysticWeaponSpeedModifier; seconds = 1048.575 / delay", "CombatCalculator.CalculateCombatWeaponValues:461-471"),
        ("Weapon durability scaling", "effective AttackVsP/VsM = (TypeDef.AttackVsP/2) + (TypeDef.AttackVsP/2) * (Durability/MaxDurability) - i.e. half value at 0 durability, full value at full durability", "Weapon.cs:38-50"),
    ]
    row = header_row(ws, row, 1, ["Formula", "Exact expression", "Source"])
    for r in formula_texts:
        ws.cell(row=row, column=1, value=r[0]).alignment = WRAP
        ws.cell(row=row, column=2, value=r[1]).alignment = WRAP
        ws.cell(row=row, column=3, value=r[2])
        row += 1
    row += 1

    row = h2(ws, row, 1, "Resist / damage types (15 real Resist* stats, all through the identical GetResistModifier formula above)", span=3)
    row = header_row(ws, row, 1, ["Character stat", "Category", "Who/what actually uses it"])
    for r in RESIST_TYPES:
        row = data_rows(ws, row, 1, [r])
    row = note(ws, row, 1, "Scale of each Resist* value is NOT confirmed in source (no content currently sets a non-zero value to observe) - assumed 0.0-1.0 fractional (0.30 = 30% resisted) by this workbook; flagged as an open question, not a verified fact. Source: scripts/Basic/Enums.cs:166-207 (DamageType), Character.cs:944-1160 (ResistX properties), DamageManager.cs:28-80 (GetResistModifier).", span=6)

    autosize(ws, {1: 30, 2: 46, 3: 46, 4: 30, 5: 40})
    return const_name_to_cell


def build_weapons_ref(wb):
    ws = wb.create_sheet("Weapons Reference")
    row = h1(ws, 1, 1, "Weapons Reference", span=12)
    row = note(ws, row, 1,
               "attackVsP == attackVsM for every real weapon def (confirmed, zero exceptions) - the content format has "
               "both fields but they're never differentiated per-item; the real Player/Monster split happens via the "
               "global multipliers on the Core Formulas tab. Source: scripts/converted/zbrane/*.def.",
               span=12)
    row += 1
    row = note(ws, row, 1,
               "piercing=100, speed=100, range=1, strikeStartRange=5, strikeStopRange=10 are uniform placeholders on "
               "EVERY weapon in this content (not varied by material or archetype) - not shown as columns since there's "
               "no real variation to compare yet.", span=12)
    row += 1
    header_r = row
    headers = ["Archetype", "WeaponType", "Skill", "MaterialType", "Notes"] + MATERIAL_NAMES_FOR_WEAPONS
    row = header_row(ws, row, 1, headers)
    data_start = row
    for arche, disp, wtype, skill, mtype, wnote in WEAPON_ARCHETYPES:
        vals = WEAPON_ATTACK[arche]
        ws.cell(row=row, column=1, value=arche)
        ws.cell(row=row, column=2, value=wtype)
        ws.cell(row=row, column=3, value=skill)
        ws.cell(row=row, column=4, value=mtype)
        ws.cell(row=row, column=5, value=wnote)
        for i, v in enumerate(vals):
            ws.cell(row=row, column=6 + i, value=v)
        row += 1
    data_end = row - 1
    row += 1
    row = note(ws, row, 1, "Source: scripts/converted/zbrane/*.def (one file per material). How to change: edit the AttackVsP/AttackVsM field on the relevant material's .def entry, restart server (no rebuild needed for content changes).", span=12)
    row += 1
    row = h2(ws, row, 1, "Ammo tier bonus (bow/crossbow damage multiplier from arrow/bolt material)", span=4)
    row = header_row(ws, row, 1, ["Material", "Tier rank", "Bonus = 1 + tier*arrowDamageBonusPerTier(0.05)"])
    tiers = [("Copper", 0), ("Iron", 1), ("Verite", 2), ("Valorite", 3), ("Obsidian", 4), ("Adamantinum", 5), ("Mithril", 6)]
    for mat, rank in tiers:
        row = data_rows(ws, row, 1, [[mat, rank, f"=1+{rank}*C_arrowDamageBonusPerTier"]])
    row = note(ws, row, 1, "Source: CombatCalculator.GetAmmoTierRank (~line 567) + RangerSettings.arrowDamageBonusPerTier. Explicit placeholder value, not a faithfully-ported real number. Silver/Gold/Sand aren't ammo materials (rank falls to 0/Copper-equivalent).", span=6)

    autosize(ws, {1: 16, 2: 14, 3: 14, 4: 12, 5: 50})
    for c in range(6, 13):
        ws.column_dimensions[get_column_letter(c)].width = 11
    return dict(header_row=header_r, data_start=data_start, data_end=data_end)


def build_armor_ref(wb):
    ws = wb.create_sheet("Armor Reference")
    row = h1(ws, 1, 1, "Armor Reference", span=14)
    row = note(ws, row, 1,
               "ArmorVsP == ArmorVsM == MindDefenseVsP == MindDefenseVsM for every piece/material checked (one number "
               "per cell below covers all 4). Source: scripts/converted/brneni/*.def.", span=14)
    row += 1
    row = note(ws, row, 1,
               "Mind Defense (used only against Mystic's innate mind-power attack) comes from the SAME number as "
               "physical AR, per piece per material - armor mitigates Mind damage on equal footing with physical "
               "damage, it's just a separate stat/pipeline (see Core Formulas + Mystic tab).", span=14)
    row += 1
    header_r = row
    headers = ["Piece", "Layer", "Type", "Shield?"] + MATERIAL_NAMES_FOR_ARMOR
    row = header_row(ws, row, 1, headers)
    data_start = row
    for name, layer, wtype, vals, is_shield in ARMOR_PIECES:
        ws.cell(row=row, column=1, value=name)
        ws.cell(row=row, column=2, value=layer)
        ws.cell(row=row, column=3, value=wtype)
        ws.cell(row=row, column=4, value="Yes" if is_shield else "No")
        for i, v in enumerate(vals):
            ws.cell(row=row, column=5 + i, value=v)
        row += 1
    data_end = row - 1
    row += 1
    row = note(ws, row, 1, "Source: scripts/converted/brneni/*.def (one file per material). How to change: edit the armorVsP/armorVsM/mindDefenseVsP/mindDefenseVsM fields on the relevant material's .def entry, restart server. Durability scaling (Wearable.cs:38-58) applies on top at runtime: effective value = (TypeDef value / 2) + (TypeDef value / 2) * (Durability/MaxDurability) - a damaged piece gives proportionally less.", span=14)
    row += 1
    row = note(ws, row, 1, "Per-slot aggregation across a full outfit is MAX (not SUM) per body zone, then zone-weighted - see Core Formulas tab's Body-zone table. This tab shows each PIECE's own raw value; the profession calculators take the defender's total ArmorClassVsP/VsM as a single input rather than re-deriving a full 8-slot outfit, for simplicity - sum/weight manually using this table + the Core Formulas zone weights if you want an exact outfit total.", span=14)

    autosize(ws, {1: 28, 2: 8, 3: 12, 4: 9})
    for c in range(5, 14):
        ws.column_dimensions[get_column_letter(c)].width = 13
    return dict(header_row=header_r, data_start=data_start, data_end=data_end)


def build_monsters_ref(wb):
    ws = wb.create_sheet("Monsters Reference")
    row = h1(ws, 1, 1, "Monsters Reference", span=8)
    row = note(ws, row, 1,
               f"HP (MaxHits) = {MONSTER_DEFAULTS['max_hits']} and WeaponDelay = "
               f"{MONSTER_DEFAULTS['weapon_delay']} seconds are FLAT for every single monster - no "
               f".def file sets either field, confirmed via scripts/Basic/CharClasses/"
               f"NPC.Generated.cs:151 (MaxHits) and :44 (WeaponDelay). Not shown as per-monster "
               f"columns below since there's no real per-monster variation - a GM can override "
               f"either live per-spawn, which this table can't reflect.", span=8)
    row += 1
    row = note(ws, row, 1,
               "417 distinct base creatures, deduplicated from ~761 raw NPCDef blocks across "
               "scripts/converted/chars/*.def (weapon/color variants of the same creature collapsed "
               "into one row; vendors.def/guards.def/mounts.def/ucitele.def excluded as non-monster "
               "content). Where merged variants had different WeaponAttack/ArmorVsP/ArmorVsM, Min "
               "and Max columns differ instead of silently picking one value - use the profession "
               "tabs' monster-picker to auto-fill the AVERAGE of min/max, or look up the specific "
               "variant's real number directly in the source .def if you need the exact figure.",
               span=8)
    row += 1
    header_r = row
    row = header_row(ws, row, 1, ["Name", "Category", "WeaponAttack Min", "WeaponAttack Max",
                                   "ArmorVsP Min", "ArmorVsP Max", "ArmorVsM Min", "ArmorVsM Max"])
    data_start = row
    data_rows(ws, row, 1, [["(manual)", "(manual)", 0, 0, 0, 0, 0, 0]])
    row += 1
    for m in sorted(MONSTERS, key=lambda x: (x[1], x[0])):
        data_rows(ws, row, 1, [list(m)])
        row += 1
    data_end = row - 1
    row += 1
    row = note(ws, row, 1, "Source: scripts/converted/chars/*.def (32 files). How to change: edit "
                            "the WeaponAttack/ArmorVsP/ArmorVsM fields on the relevant NPCDef, "
                            "restart server - or use a GM .settings/live command to override a "
                            "specific spawned instance.", span=8)
    autosize(ws, {1: 34, 2: 22})
    for c in range(3, 9):
        ws.column_dimensions[get_column_letter(c)].width = 15
    return dict(header_row=header_r, data_start=data_start, data_end=data_end)


MELEE_ARCHETYPES = [w for w in WEAPON_ARCHETYPES if w[2] not in ("Bow", "XBow")]
RANGED_ARCHETYPES = [w for w in WEAPON_ARCHETYPES if w[2] in ("Bow", "XBow")]

SKILL_TO_RESIST = {
    "Swords": "ResistSlashing", "Fencing": "ResistStabbing", "Macing": "ResistBlunt",
    "Archery": "ResistArchery", "Marksmanship": "ResistArchery",
}


def add_monster_picker(ws, row, mi, kind, manual_cells, prefix):
    """Adds a monster-name dropdown (sourced from the Monsters Reference sheet, '(manual)' as the
    first/default option) plus one resolved output cell per stat. kind='defense' resolves
    ArmorClassVsM (average of the catalog's Min/Max) + HP (flat MONSTER_DEFAULTS default).
    kind='offense' resolves WeaponAttack (average of Min/Max) + WeaponDelay (flat default).
    Falls back to the corresponding manual_cells entry when '(manual)' is selected.
    Returns (new_row, {stat_name: resolved_cell_ref})."""
    ws.cell(row=row, column=1,
            value=f"{prefix}: pick a cataloged monster (optional, overrides manual values below)")
    ws.cell(row=row, column=2, value="(manual)").fill = INPUT_FILL
    picker_cell = f"$B${row}"
    dv = DataValidation(type="list",
                         formula1=f"='Monsters Reference'!$A${mi['data_start']}:$A${mi['data_end']}")
    ws.add_data_validation(dv)
    dv.add(ws.cell(row=row, column=2))
    row += 1

    def avg_lookup(col_min, col_max):
        m = (f"MATCH({picker_cell},'Monsters Reference'!$A${mi['data_start']}:$A${mi['data_end']},0)")
        return (f"AVERAGE(INDEX('Monsters Reference'!${col_min}${mi['data_start']}:${col_min}${mi['data_end']},{m}),"
                f"INDEX('Monsters Reference'!${col_max}${mi['data_start']}:${col_max}${mi['data_end']},{m}))")

    resolved = {}
    if kind == "defense":
        ws.cell(row=row, column=1, value=f"{prefix}: resolved ArmorClassVsM")
        output_cell(ws, row, 2, f'=IF({picker_cell}="(manual)",{manual_cells["armor"]},{avg_lookup("G","H")})')
        resolved["armor"] = f"$B${row}"
        row += 1
        ws.cell(row=row, column=1, value=f"{prefix}: resolved HP")
        output_cell(ws, row, 2, f'=IF({picker_cell}="(manual)",{manual_cells["hp"]},{MONSTER_DEFAULTS["max_hits"]})')
        resolved["hp"] = f"$B${row}"
        row += 1
    else:
        ws.cell(row=row, column=1, value=f"{prefix}: resolved WeaponAttack")
        output_cell(ws, row, 2, f'=IF({picker_cell}="(manual)",{manual_cells["weapon_attack"]},{avg_lookup("C","D")})')
        resolved["weapon_attack"] = f"$B${row}"
        row += 1
        ws.cell(row=row, column=1, value=f"{prefix}: resolved WeaponDelay")
        output_cell(ws, row, 2, f'=IF({picker_cell}="(manual)",{manual_cells["weapon_delay"]},{MONSTER_DEFAULTS["weapon_delay"]})')
        resolved["weapon_delay"] = f"$B${row}"
        row += 1
    return row, resolved


def _profession_header(ws, profession):
    row = h1(ws, 1, 1, profession, span=6)
    stats = PROFESSION_STATS[profession]
    row = note(ws, row, 1,
               f"Stat ranges (min/avg/max): Str {stats['str_r']}, Dex {stats['dex_r']}, Int {stats['int_r']}, "
               f"Vit {stats['vit_r']}. Allowed weapons: {stats['weapons']}. Allowed armor: {stats['armor']}. "
               f"Source: {stats['source']}.", span=8)
    row += 1
    return row


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
                            "use a different override - see the Ranger tab.", span=8)
    row += 1

    attacker_row = row
    row = h2(ws, row, 1, "Attacker (you)", span=2)
    labels = [
        ("Str", 100), ("Dex", 100), ("Tactics skill (0-1000, GM=1000)", 1000),
        ("Anatomy skill (0-1000)", 1000), ("ArmsLore skill (0-1000)", 1000),
        ("Weapon archetype", MELEE_ARCHETYPES[0][0]), ("Material", MATERIAL_NAMES_FOR_WEAPONS[0]),
        ("Weapon durability %", 100),
    ]
    cells = {}
    for label, default in labels:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    dv_weapon = DataValidation(type="list", formula1=f'"{",".join(a[0] for a in MELEE_ARCHETYPES)}"')
    ws.add_data_validation(dv_weapon)
    dv_weapon.add(ws[cells["Weapon archetype"]])
    dv_mat = DataValidation(type="list", formula1=f'"{",".join(MATERIAL_NAMES_FOR_WEAPONS)}"')
    ws.add_data_validation(dv_mat)
    dv_mat.add(ws[cells["Material"]])
    row += 1

    row = h2(ws, row, 1, "PvM target - a Monster you're attacking", span=2)
    pvm_labels = [("Monster ArmorClassVsM", 0), ("Monster ResistPhysical (0-1)", 0),
                  ("Monster Resist<narrow for your weapon> (0-1)", 0), ("Monster HP", 100)]
    for label, default in pvm_labels:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    row, pvm_resolved = add_monster_picker(ws, row, monsters_info, "defense",
                                            {"armor": cells["Monster ArmorClassVsM"], "hp": cells["Monster HP"]},
                                            "PvM target")
    row += 1

    row = h2(ws, row, 1, "PvP target - another Player you're attacking", span=2)
    pvp_labels = [("Enemy ArmorClassVsP", 0), ("Enemy ResistPhysical (0-1)", 0),
                  ("Enemy Resist<narrow> (0-1)", 0), ("Enemy HP", 100)]
    for label, default in pvp_labels:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    row += 1

    row = h2(ws, row, 1, "MvP - a Monster attacking YOU", span=2)
    mvp_labels = [("Monster attacker WeaponAttack (flat)", 10), ("Monster attacker WeaponDelay (sec, flat)", 2.0),
                  ("Your ArmorClassVsP", 0), ("Your ResistPhysical (0-1)", 0),
                  ("Your Resist<narrow> (0-1)", 0), ("Your HP", 100)]
    for label, default in mvp_labels:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    row, mvp_resolved = add_monster_picker(ws, row, monsters_info, "offense",
                                            {"weapon_attack": cells["Monster attacker WeaponAttack (flat)"],
                                             "weapon_delay": cells["Monster attacker WeaponDelay (sec, flat)"]},
                                            "MvP attacker")
    row = note(ws, row, 1, "Monster WeaponAttack/WeaponDelay are flat NPCDef stats with no formula behind them - "
                            "pick a monster above to auto-fill, or leave on '(manual)' to type your own.",
               span=8)
    row += 1
    row = note(ws, row, 1, "'Resist<narrow>' is the ONE sub-type stat matching your weapon's skill category - "
                            "inferred mapping Swords=Slashing, Fencing=Stabbing, Macing=Blunt (matches the "
                            "DamageType enum's own category names) - this specific per-weapon mapping wasn't "
                            "independently confirmed in the research pass, flagged as an inference, not a verified fact.",
               span=8)
    row += 2

    # Lookup for weapon attack value (shared for PvM/PvP, attacker-driven)
    wi = weapons_info
    weapon_cell, material_cell = cells["Weapon archetype"], cells["Material"]
    lookup = (f"INDEX('Weapons Reference'!$F${wi['data_start']}:$L${wi['data_end']},"
              f"MATCH({weapon_cell},'Weapons Reference'!$A${wi['data_start']}:$A${wi['data_end']},0),"
              f"MATCH({material_cell},'Weapons Reference'!$F${wi['header_row']}:$L${wi['header_row']},0))")
    skill_resist_lookup = (f"INDEX({{\"Slashing\";\"Stabbing\";\"Blunt\";\"Archery\"}},"
                           f"MATCH(INDEX('Weapons Reference'!$B${wi['data_start']}:$B${wi['data_end']},"
                           f"MATCH({weapon_cell},'Weapons Reference'!$A${wi['data_start']}:$A${wi['data_end']},0)),"
                           f"{{\"Swords\";\"Fencing\";\"Macing\";\"Archery\"}},0))")

    base_attack_row = row
    ws.cell(row=row, column=1, value="Weapon base AttackVsP/VsM (looked up)")
    output_cell(ws, row, 2, f"={lookup}")
    row += 1
    durab_row = row
    ws.cell(row=row, column=1, value="Durability-scaled weapon attack")
    output_cell(ws, row, 2, f"=($B${base_attack_row}/2)+($B${base_attack_row}/2)*({cells['Weapon durability %']}/100)")
    row += 1
    sum_row = row
    ws.cell(row=row, column=1, value="Skill sum (Tactics+Anatomy+ArmsLore+Str contributions)")
    output_cell(ws, row, 2,
                f"=(({cells['Tactics skill (0-1000, GM=1000)']}/1000*C_Tactics_EffectMax)"
                f"+({cells['Anatomy skill (0-1000)']}/1000*C_Anatomy_EffectMax)"
                f"+({cells['ArmsLore skill (0-1000)']}/1000*C_ArmsLore_EffectMax)"
                f"+({cells['Str']}*C_attackStrModifier))/1000")
    row += 1
    weapon_attack_row = row
    ws.cell(row=row, column=1, value="Your WeaponAttackVsP/VsM (both equal)")
    output_cell(ws, row, 2, f"=$B${durab_row}*$B${sum_row}")
    row += 1
    delay_row = row
    ws.cell(row=row, column=1, value="Your swing delay (seconds, Dex-driven, weapSpeed=100)")
    output_cell(ws, row, 2, f"=1048.575/(SQRT({cells['Dex']})*100*C_weaponSpeedGlobal)")
    row += 2

    row = h2(ws, row, 1, "Results", span=6)
    row = header_row(ws, row, 1, ["", "PvM", "PvP", "MvP (Min)", "MvP (Avg)", "MvP (Max)"])
    results_header = row - 1

    r_attack = row
    ws.cell(row=row, column=1, value="Attack value (pre-AC)")
    output_cell(ws, row, 2, f"=C_weapAttack*$B${weapon_attack_row}*C_weapAttackVsM")
    output_cell(ws, row, 3, f"=C_weapAttack*C_weapAttackPvP*$B${weapon_attack_row}")
    mvp_attack = f"=C_weapAttack*C_weapAttackM*{mvp_resolved['weapon_attack']}"
    for col in (4, 5, 6):
        output_cell(ws, row, col, mvp_attack)
    row += 1

    r_ac = row
    ws.cell(row=row, column=1, value="Armor Class effective")
    output_cell(ws, row, 2, f"={pvm_resolved['armor']}*C_armorClassM")
    output_cell(ws, row, 3, f"={cells['Enemy ArmorClassVsP']}*C_armorClassP")
    mvp_ac = f"={cells['Your ArmorClassVsP']}*C_armorClassP"
    for col in (4, 5, 6):
        output_cell(ws, row, col, mvp_ac)
    row += 1

    r_mod = row
    ws.cell(row=row, column=1, value="damageMod")
    output_cell(ws, row, 2, "=C_swingDamageM")
    output_cell(ws, row, 3, "=C_swingDamagePvP")
    output_cell(ws, row, 4, "=C_swingDamageRandMvPMin")
    output_cell(ws, row, 5, "=AVERAGE(C_swingDamageRandMvPMin,C_swingDamageRandMvPMax)")
    output_cell(ws, row, 6, "=C_swingDamageRandMvPMax")
    row += 1

    r_afterac = row
    ws.cell(row=row, column=1, value="Damage after AC")
    for col in (2, 3, 4, 5, 6):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=MAX(0,(MAX(0,{cl}{r_attack})-MAX(0,{cl}{r_ac}))*MAX(0,{cl}{r_mod}))")
    row += 1

    r_resist = row
    ws.cell(row=row, column=1, value="Resist modifier")
    output_cell(ws, row, 2, f"=(1-{cells['Monster ResistPhysical (0-1)']})-{cells['Monster Resist<narrow for your weapon> (0-1)']}")
    output_cell(ws, row, 3, f"=(1-{cells['Enemy ResistPhysical (0-1)']})-{cells['Enemy Resist<narrow> (0-1)']}")
    mvp_resist = f"=(1-{cells['Your ResistPhysical (0-1)']})-{cells['Your Resist<narrow> (0-1)']}"
    for col in (4, 5, 6):
        output_cell(ws, row, col, mvp_resist)
    row += 1

    r_final = row
    ws.cell(row=row, column=1, value="Final damage per hit")
    for col in (2, 3, 4, 5, 6):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=MAX(0,{cl}{r_afterac}*{cl}{r_resist})")
    row += 1

    r_delay = row
    ws.cell(row=row, column=1, value="Delay between hits (seconds)")
    output_cell(ws, row, 2, f"=$B${delay_row}")
    output_cell(ws, row, 3, f"=$B${delay_row}")
    for col in (4, 5, 6):
        output_cell(ws, row, col, f"={mvp_resolved['weapon_delay']}")
    row += 1

    r_dps = row
    ws.cell(row=row, column=1, value="DPS")
    for col in (2, 3, 4, 5, 6):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"={cl}{r_final}/{cl}{r_delay}")
    row += 1

    r_hp = row
    ws.cell(row=row, column=1, value="Target HP")
    output_cell(ws, row, 2, f"={pvm_resolved['hp']}")
    output_cell(ws, row, 3, f"={cells['Enemy HP']}")
    for col in (4, 5, 6):
        output_cell(ws, row, col, f"={cells['Your HP']}")
    row += 1

    r_ttk = row
    ws.cell(row=row, column=1, value="Time to kill (seconds)")
    for col in (2, 3, 4, 5, 6):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=IF({cl}{r_dps}=0,\"never\",{cl}{r_hp}/{cl}{r_dps})")
    row += 2

    row = h2(ws, row, 1, "Reference matrix - all melee archetypes x materials, fixed baseline (GM skills, Str=100, "
                         "Dex=100, unarmored Monster target, 0 resist)", span=9)
    header = ["Archetype", "WeaponType", "Skill"] + MATERIAL_NAMES_FOR_WEAPONS
    row = header_row(ws, row, 1, header)
    ref_start = row
    for arche, disp, wtype, skill, mtype, wnote in MELEE_ARCHETYPES:
        ws.cell(row=row, column=1, value=arche)
        ws.cell(row=row, column=2, value=wtype)
        ws.cell(row=row, column=3, value=skill)
        for i, mat in enumerate(MATERIAL_NAMES_FOR_WEAPONS):
            lookup_cell = f"'Weapons Reference'!{get_column_letter(6 + i)}{wi['data_start'] + WEAPON_NAMES.index(arche)}"
            durab_scaled = f"({lookup_cell}/2+{lookup_cell}/2*1)"  # full durability baseline
            skillsum_baseline = f"((1000/1000*C_Tactics_EffectMax)+(1000/1000*C_Anatomy_EffectMax)+(1000/1000*C_ArmsLore_EffectMax)+(100*C_attackStrModifier))/1000"
            attack_baseline = f"C_weapAttack*({durab_scaled}*({skillsum_baseline}))*C_weapAttackVsM"
            dmg_formula = f"=MAX(0,MAX(0,{attack_baseline})*C_swingDamageM)"
            ws.cell(row=row, column=4 + i, value=dmg_formula)
        row += 1
    row = note(ws, row, 1, "This matrix shows PvM damage-after-AC against an unarmored Monster (ArmorClass=0) at "
                            "GM skills - i.e. the raw weapon/material scaling with everything else held constant. "
                            "Use the Calculator above for your actual stats/target.", span=9)

    autosize(ws, {1: 30, 2: 18, 3: 14})
    for c in range(4, 11):
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
               "Bows/crossbows IGNORE their own material for damage - RangerSettings flatly overrides AttackVsP/VsM "
               "regardless of tier. Only the ammo's material changes damage (ammo tier bonus below). This is "
               "confirmed real engine behavior (RangerSettingsTests.HigherTierArrowGrantsDamageBonus), not a bug.",
               span=8)
    row += 1

    row = h2(ws, row, 1, "Calculator", span=6)
    row += 1
    labels = [
        ("Str", 100), ("Dex", 100), ("Tactics skill (0-1000)", 1000), ("Anatomy skill (0-1000)", 1000),
        ("ArmsLore skill (0-1000)", 1000), ("Weapon (Bow/LightCrossbow/HeavyCrossbow)", "Bow"),
        ("Ammo material", MATERIAL_NAMES_FOR_WEAPONS[0]),
    ]
    cells = {}
    for label, default in labels:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    dv_w = DataValidation(type="list", formula1='"Bow,LightCrossbow,HeavyCrossbow"')
    ws.add_data_validation(dv_w)
    dv_w.add(ws[cells["Weapon (Bow/LightCrossbow/HeavyCrossbow)"]])
    dv_m = DataValidation(type="list", formula1=f'"{",".join(MATERIAL_NAMES_FOR_WEAPONS)}"')
    ws.add_data_validation(dv_m)
    dv_m.add(ws[cells["Ammo material"]])
    row += 1
    weapon_cell = cells["Weapon (Bow/LightCrossbow/HeavyCrossbow)"]
    ws.cell(row=row, column=1, value="Must-stand-still to attack?")
    output_cell(ws, row, 2, f'=IF({weapon_cell}="Bow","No (bowMustStandStill=false)","Yes (crossbowMustStandStill=true)")')
    row += 1
    row = note(ws, row, 1, "Moving while required to stand still simply fails that stroke (no damage, no ammo "
                            "spent) and retries in 1s - not modeled as a damage multiplier here.", span=8)
    row += 1

    row = h2(ws, row, 1, "PvM target - a Monster you're attacking", span=2)
    for label, default in [("Monster ArmorClassVsM", 0), ("Monster ResistPhysical (0-1)", 0),
                            ("Monster ResistArchery (0-1)", 0), ("Monster HP", 100)]:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    row, pvm_resolved = add_monster_picker(ws, row, monsters_info, "defense",
                                            {"armor": cells["Monster ArmorClassVsM"], "hp": cells["Monster HP"]},
                                            "PvM target")
    row += 1
    row = h2(ws, row, 1, "PvP target - another Player you're attacking", span=2)
    for label, default in [("Enemy ArmorClassVsP", 0), ("Enemy ResistPhysical (0-1)", 0),
                            ("Enemy ResistArchery (0-1)", 0), ("Enemy HP", 100)]:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    row += 1
    row = h2(ws, row, 1, "MvP - a Monster attacking YOU", span=2)
    for label, default in [("Monster attacker WeaponAttack (flat)", 10), ("Monster attacker WeaponDelay (sec, flat)", 2.0),
                            ("Your ArmorClassVsP", 0), ("Your ResistPhysical (0-1)", 0),
                            ("Your ResistArchery (0-1)", 0), ("Your HP", 100)]:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    row, mvp_resolved = add_monster_picker(ws, row, monsters_info, "offense",
                                            {"weapon_attack": cells["Monster attacker WeaponAttack (flat)"],
                                             "weapon_delay": cells["Monster attacker WeaponDelay (sec, flat)"]},
                                            "MvP attacker")
    row += 2

    base_row = row
    ws.cell(row=row, column=1, value="Base weapon damage (RangerSettings flat override)")
    output_cell(ws, row, 2, f"=IF({cells['Weapon (Bow/LightCrossbow/HeavyCrossbow)']}=\"Bow\",C_bowDamage,"
                            f"IF({cells['Weapon (Bow/LightCrossbow/HeavyCrossbow)']}=\"LightCrossbow\",C_lightCrossbowDamage,C_heavyCrossbowDamage))")
    row += 1
    tier_row = row
    ws.cell(row=row, column=1, value="Ammo tier rank (Copper=0..Mithril=6)")
    output_cell(ws, row, 2, f"=MATCH({cells['Ammo material']},{{\"Copper\";\"Iron\";\"Verite\";\"Valorite\";\"Obsidian\";\"Adamantinum\";\"Mithril\"}},0)-1")
    row += 1
    ammobonus_row = row
    ws.cell(row=row, column=1, value="Ammo tier bonus")
    output_cell(ws, row, 2, f"=1+$B${tier_row}*C_arrowDamageBonusPerTier")
    row += 1
    sum_row = row
    ws.cell(row=row, column=1, value="Skill sum (Tactics+Anatomy+ArmsLore+Str)")
    output_cell(ws, row, 2,
                f"=(({cells['Tactics skill (0-1000)']}/1000*C_Tactics_EffectMax)"
                f"+({cells['Anatomy skill (0-1000)']}/1000*C_Anatomy_EffectMax)"
                f"+({cells['ArmsLore skill (0-1000)']}/1000*C_ArmsLore_EffectMax)"
                f"+({cells['Str']}*C_attackStrModifier))/1000")
    row += 1
    weapon_attack_row = row
    ws.cell(row=row, column=1, value="Your WeaponAttackVsP/VsM (both equal)")
    output_cell(ws, row, 2, f"=$B${base_row}*$B${ammobonus_row}*$B${sum_row}")
    row += 1
    speed_row = row
    ws.cell(row=row, column=1, value="Effective weapon speed (RangerSettings override)")
    output_cell(ws, row, 2, f"=IF({cells['Weapon (Bow/LightCrossbow/HeavyCrossbow)']}=\"Bow\",C_bowSpeed,"
                            f"IF({cells['Weapon (Bow/LightCrossbow/HeavyCrossbow)']}=\"LightCrossbow\",C_lightCrossbowSpeed,C_heavyCrossbowSpeed))")
    row += 1
    delay_row = row
    ws.cell(row=row, column=1, value="Your swing delay (seconds)")
    output_cell(ws, row, 2, f"=1048.575/(SQRT({cells['Dex']})*$B${speed_row}*C_weaponSpeedGlobal)")
    row += 1
    ws.cell(row=row, column=1, value="Range (tiles, flat GM setting, not weapon-derived)")
    output_cell(ws, row, 2, f"=IF({cells['Weapon (Bow/LightCrossbow/HeavyCrossbow)']}=\"Bow\",C_Ranges_bow,"
                            f"IF({cells['Weapon (Bow/LightCrossbow/HeavyCrossbow)']}=\"LightCrossbow\",C_Ranges_crossbow,C_Ranges_heavyCrossbow))")
    row += 2

    row = h2(ws, row, 1, "Results", span=6)
    row = header_row(ws, row, 1, ["", "PvM", "PvP", "MvP (Min)", "MvP (Avg)", "MvP (Max)"])
    r_attack = row
    ws.cell(row=row, column=1, value="Attack value (pre-AC)")
    output_cell(ws, row, 2, f"=C_weapAttack*$B${weapon_attack_row}*C_weapAttackVsM")
    output_cell(ws, row, 3, f"=C_weapAttack*C_weapAttackPvP*$B${weapon_attack_row}")
    mvp_attack = f"=C_weapAttack*C_weapAttackM*{mvp_resolved['weapon_attack']}"
    for col in (4, 5, 6):
        output_cell(ws, row, col, mvp_attack)
    row += 1
    r_ac = row
    ws.cell(row=row, column=1, value="Armor Class effective")
    output_cell(ws, row, 2, f"={pvm_resolved['armor']}*C_armorClassM")
    output_cell(ws, row, 3, f"={cells['Enemy ArmorClassVsP']}*C_armorClassP")
    mvp_ac = f"={cells['Your ArmorClassVsP']}*C_armorClassP"
    for col in (4, 5, 6):
        output_cell(ws, row, col, mvp_ac)
    row += 1
    r_mod = row
    ws.cell(row=row, column=1, value="damageMod")
    output_cell(ws, row, 2, "=C_swingDamageM")
    output_cell(ws, row, 3, "=C_swingDamagePvP")
    output_cell(ws, row, 4, "=C_swingDamageRandMvPMin")
    output_cell(ws, row, 5, "=AVERAGE(C_swingDamageRandMvPMin,C_swingDamageRandMvPMax)")
    output_cell(ws, row, 6, "=C_swingDamageRandMvPMax")
    row += 1
    r_afterac = row
    ws.cell(row=row, column=1, value="Damage after AC")
    for col in (2, 3, 4, 5, 6):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=MAX(0,(MAX(0,{cl}{r_attack})-MAX(0,{cl}{r_ac}))*MAX(0,{cl}{r_mod}))")
    row += 1
    r_resist = row
    ws.cell(row=row, column=1, value="Resist modifier")
    output_cell(ws, row, 2, f"=(1-{cells['Monster ResistPhysical (0-1)']})-{cells['Monster ResistArchery (0-1)']}")
    output_cell(ws, row, 3, f"=(1-{cells['Enemy ResistPhysical (0-1)']})-{cells['Enemy ResistArchery (0-1)']}")
    mvp_resist = f"=(1-{cells['Your ResistPhysical (0-1)']})-{cells['Your ResistArchery (0-1)']}"
    for col in (4, 5, 6):
        output_cell(ws, row, col, mvp_resist)
    row += 1
    r_final = row
    ws.cell(row=row, column=1, value="Final damage per hit")
    for col in (2, 3, 4, 5, 6):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=MAX(0,{cl}{r_afterac}*{cl}{r_resist})")
    row += 1
    r_delay = row
    ws.cell(row=row, column=1, value="Delay between hits (seconds)")
    output_cell(ws, row, 2, f"=$B${delay_row}")
    output_cell(ws, row, 3, f"=$B${delay_row}")
    for col in (4, 5, 6):
        output_cell(ws, row, col, f"={mvp_resolved['weapon_delay']}")
    row += 1
    r_dps = row
    ws.cell(row=row, column=1, value="DPS")
    for col in (2, 3, 4, 5, 6):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"={cl}{r_final}/{cl}{r_delay}")
    row += 1
    r_hp = row
    ws.cell(row=row, column=1, value="Target HP")
    output_cell(ws, row, 2, f"={pvm_resolved['hp']}")
    output_cell(ws, row, 3, f"={cells['Enemy HP']}")
    for col in (4, 5, 6):
        output_cell(ws, row, col, f"={cells['Your HP']}")
    row += 1
    r_ttk = row
    ws.cell(row=row, column=1, value="Time to kill (seconds)")
    for col in (2, 3, 4, 5, 6):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=IF({cl}{r_dps}=0,\"never\",{cl}{r_hp}/{cl}{r_dps})")
    row += 2

    row = h2(ws, row, 1, "Reference matrix - fixed baseline (GM skills, Str=100, Dex=100, unarmored Monster target)", span=4)
    row = header_row(ws, row, 1, ["Weapon", "Ammo material", "Ammo tier", "PvM damage/hit (unarmored Monster)"])
    weapon_defaults = [("Bow", "C_bowDamage", "C_bowSpeed"), ("LightCrossbow", "C_lightCrossbowDamage", "C_lightCrossbowSpeed"),
                        ("HeavyCrossbow", "C_heavyCrossbowDamage", "C_heavyCrossbowSpeed")]
    for wname, dmg_const, _ in weapon_defaults:
        for tier_i, mat in enumerate(MATERIAL_NAMES_FOR_WEAPONS):
            skillsum_baseline = "((1000/1000*C_Tactics_EffectMax)+(1000/1000*C_Anatomy_EffectMax)+(1000/1000*C_ArmsLore_EffectMax)+(100*C_attackStrModifier))/1000"
            formula = (f"=MAX(0,C_weapAttack*({dmg_const}*(1+{tier_i}*C_arrowDamageBonusPerTier)*"
                       f"({skillsum_baseline}))*C_weapAttackVsM)")
            ws.cell(row=row, column=1, value=wname)
            ws.cell(row=row, column=2, value=mat)
            ws.cell(row=row, column=3, value=tier_i)
            output_cell(ws, row, 4, formula)
            row += 1
    autosize(ws, {1: 40, 2: 16, 3: 10, 4: 34})
    ws.column_dimensions["B"].width = 16


def build_mystic(wb, monsters_info):
    ws = wb.create_sheet("Mystic")
    row = _profession_header(ws, "Mystic")
    row = note(ws, row, 1,
               "Mystic's melee swing is forcibly DamageType.MagicMystical regardless of equipped weapon - a "
               "completely separate damage path (mindPower - mindDefense, subtractive) from every other profession's "
               "armor-based physical path. No weapon .def sets MindPowerVsP/VsM, so every staff currently falls back "
               "to the same bare-hands default (700) - material doesn't differentiate Mystic damage today. "
               "Resisting Spells (MagicResist)'s contribution to mind-defense is currently 0 (EFFECT line commented "
               "out in Skills.scp) - only worn armor's MindDefense stat matters today.", span=8)
    row += 1

    row = h2(ws, row, 1, "Calculator - mind-power attack (PvM/PvP)", span=6)
    row += 1
    labels = [("Int", 150), ("Mana (current)", 100), ("EvalInt skill (0-1000)", 1000),
              ("SpiritSpeak skill (0-1000)", 1000)]
    cells = {}
    for label, default in labels:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    row = note(ws, row, 1, "Weapon/material has NO effect on mind-power today (every staff falls back to the "
                            "bare-hands MindPower default) - not shown as an input for that reason.", span=8)
    row += 1

    row = h2(ws, row, 1, "PvM target - a Monster you're attacking", span=2)
    for label, default in [("Monster MindDefenseVsM", 0), ("Monster ResistMagic (0-1)", 0),
                            ("Monster ResistMystical (0-1)", 0), ("Monster HP", 100)]:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    row += 1
    row = h2(ws, row, 1, "PvP target - another Player you're attacking", span=2)
    for label, default in [("Enemy MindDefenseVsP", 0), ("Enemy ResistMagic (0-1)", 0),
                            ("Enemy ResistMystical (0-1)", 0), ("Enemy HP", 100)]:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    row += 1
    row = h2(ws, row, 1, "MvP - a Monster physically attacking YOU (generic path - monsters don't use mind-power)", span=2)
    for label, default in [("Monster attacker WeaponAttack (flat)", 10), ("Monster attacker WeaponDelay (sec, flat)", 2.0),
                            ("Your ArmorClassVsP", 0), ("Your ResistPhysical (0-1)", 0),
                            ("Your Resist<attacker's weapon subtype> (0-1)", 0), ("Your HP", 100)]:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    row, mvp_resolved = add_monster_picker(ws, row, monsters_info, "offense",
                                            {"weapon_attack": cells["Monster attacker WeaponAttack (flat)"],
                                             "weapon_delay": cells["Monster attacker WeaponDelay (sec, flat)"]},
                                            "MvP attacker")
    row += 2

    sum_row = row
    ws.cell(row=row, column=1, value="Mind-power sum (EvalInt+SpiritSpeak+Int contributions)")
    output_cell(ws, row, 2,
                f"=(({cells['EvalInt skill (0-1000)']}/1000*C_EvalInt_EffectMax)"
                f"+({cells['SpiritSpeak skill (0-1000)']}/1000*C_SpiritSpeak_EffectMax)"
                f"+({cells['Int']}*C_mindPowerIntModifier))/1000")
    row += 1
    mp_row = row
    ws.cell(row=row, column=1, value="Your MindPowerVsP/VsM (weapon falls back to bare-hands default)")
    output_cell(ws, row, 2, f"=C_bareHandsMindPowerVsP*$B${sum_row}")
    row += 1
    delay_row = row
    ws.cell(row=row, column=1, value="Your swing delay (seconds, MANA-driven, weapSpeed=100)")
    output_cell(ws, row, 2, f"=1048.575/(SQRT(MAX({cells['Mana (current)']},1)/2)*100*C_mysticWeaponSpeedModifier)")
    row += 1
    ws.cell(row=row, column=1, value="Mana remaining after this swing (97.0% retained)")
    output_cell(ws, row, 2, f"={cells['Mana (current)']}*(C_mysticManaLossPermille/1000)")
    row = note(ws, row+1, 1, "Delay gets SLOWER as mana drains (sqrt(mana) in the denominator) - this calculator "
                             "shows the delay at your CURRENT mana input; a real fight's delay drifts upward swing "
                             "by swing as mana depletes, not modeled as a full multi-swing simulation here.", span=8)
    row += 1

    row = h2(ws, row, 1, "Results - mind-power attack", span=4)
    row = header_row(ws, row, 1, ["", "PvM", "PvP"])
    r_mp = row
    ws.cell(row=row, column=1, value="MindPower (attack)")
    output_cell(ws, row, 2, f"=$B${mp_row}")
    output_cell(ws, row, 3, f"=$B${mp_row}")
    row += 1
    r_md = row
    ws.cell(row=row, column=1, value="Defender MindDefense")
    output_cell(ws, row, 2, f"={cells['Monster MindDefenseVsM']}")
    output_cell(ws, row, 3, f"={cells['Enemy MindDefenseVsP']}")
    row += 1
    r_mod = row
    ws.cell(row=row, column=1, value="damageMod")
    output_cell(ws, row, 2, "=C_swingDamageM")
    output_cell(ws, row, 3, "=C_swingDamagePvP")
    row += 1
    r_aftermd = row
    ws.cell(row=row, column=1, value="Damage after MindDefense")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=MAX(0,({cl}{r_mp}-{cl}{r_md})*{cl}{r_mod})")
    row += 1
    r_resist = row
    ws.cell(row=row, column=1, value="Resist modifier (ResistMagic+ResistMystical)")
    output_cell(ws, row, 2, f"=(1-{cells['Monster ResistMagic (0-1)']})-{cells['Monster ResistMystical (0-1)']}")
    output_cell(ws, row, 3, f"=(1-{cells['Enemy ResistMagic (0-1)']})-{cells['Enemy ResistMystical (0-1)']}")
    row += 1
    r_final = row
    ws.cell(row=row, column=1, value="Final damage per hit")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=MAX(0,{cl}{r_aftermd}*{cl}{r_resist})")
    row += 1
    r_dps = row
    ws.cell(row=row, column=1, value="DPS (at current mana's delay)")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"={cl}{r_final}/$B${delay_row}")
    row += 1
    r_hp = row
    ws.cell(row=row, column=1, value="Target HP")
    output_cell(ws, row, 2, f"={cells['Monster HP']}")
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
    ws.cell(row=row, column=1, value="Attack value (pre-AC)")
    mvp_attack = f"=C_weapAttack*C_weapAttackM*{mvp_resolved['weapon_attack']}"
    for col in (2, 3, 4):
        output_cell(ws, row, col, mvp_attack)
    row += 1
    r_ac = row
    ws.cell(row=row, column=1, value="Armor Class effective")
    mvp_ac = f"={cells['Your ArmorClassVsP']}*C_armorClassP"
    for col in (2, 3, 4):
        output_cell(ws, row, col, mvp_ac)
    row += 1
    r_mod2 = row
    ws.cell(row=row, column=1, value="damageMod")
    output_cell(ws, row, 2, "=C_swingDamageRandMvPMin")
    output_cell(ws, row, 3, "=AVERAGE(C_swingDamageRandMvPMin,C_swingDamageRandMvPMax)")
    output_cell(ws, row, 4, "=C_swingDamageRandMvPMax")
    row += 1
    r_afterac = row
    ws.cell(row=row, column=1, value="Damage after AC")
    for col in (2, 3, 4):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=MAX(0,(MAX(0,{cl}{r_attack})-MAX(0,{cl}{r_ac}))*MAX(0,{cl}{r_mod2}))")
    row += 1
    r_resist2 = row
    ws.cell(row=row, column=1, value="Resist modifier")
    your_narrow_resist_cell = cells["Your Resist<attacker's weapon subtype> (0-1)"]
    mvp_resist = f"=(1-{cells['Your ResistPhysical (0-1)']})-{your_narrow_resist_cell}"
    for col in (2, 3, 4):
        output_cell(ws, row, col, mvp_resist)
    row += 1
    r_final2 = row
    ws.cell(row=row, column=1, value="Final damage per hit")
    for col in (2, 3, 4):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=MAX(0,{cl}{r_afterac}*{cl}{r_resist2})")
    row += 1
    r_dps2 = row
    ws.cell(row=row, column=1, value="DPS")
    for col in (2, 3, 4):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"={cl}{r_final2}/{mvp_resolved['weapon_delay']}")
    row += 1
    r_ttk2 = row
    ws.cell(row=row, column=1, value="Time to kill YOU (seconds)")
    for col in (2, 3, 4):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=IF({cl}{r_dps2}=0,\"never\",{cells['Your HP']}/{cl}{r_dps2})")

    autosize(ws, {1: 44, 2: 18, 3: 18, 4: 18})


def build_mage(wb):
    ws = wb.create_sheet("Mage")
    row = _profession_header(ws, "Mage")
    row = note(ws, row, 1,
               "Every shipped damage spell has SpellFlag.UseMindPower set, so spell power uses the SAME "
               "MindPowerVsX-minus-MindDefenseVsX formula Mystic's melee uses (x10 scaled), NOT the caster's raw "
               "Magery skill. Spell damage BYPASSES armor/AC entirely - only the spell's own DamageType-matched "
               "Resist* stat(s) mitigate it. The power-to-EFFECT-curve mapping is NOT clamped at the curve's own max "
               "in the real engine (EvalRangeDouble extrapolates linearly past ratio=1) - a high-Int caster's spells "
               "can legitimately exceed the EFFECT=min,max range shown per spell below.", span=8)
    row += 1

    row = h2(ws, row, 1, "Calculator", span=6)
    row += 1
    spell_names = [s[0] for s in SPELLS]
    labels = [("Int", 150), ("EvalInt skill (0-1000)", 1000), ("SpiritSpeak skill (0-1000)", 1000),
              ("Spell", spell_names[0]), ("Standcast bonus fraction (0+, e.g. 0.10 = +10%)", 0),
              ("Magic Charge bonus fraction (0+, matching-school stacks)", 0)]
    cells = {}
    for label, default in labels:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    dv_spell = DataValidation(type="list", formula1=f'"{",".join(spell_names)}"')
    ws.add_data_validation(dv_spell)
    dv_spell.add(ws[cells["Spell"]])
    row = note(ws, row, 1, "Standcast/Magic Charge are entered as a direct bonus fraction here - the exact "
                            "per-point scaling constants (ability EffectPower, caster.Level derivation) weren't "
                            "pinned down in this research pass; enter 0 to ignore, or your own estimate.", span=8)
    row += 1

    row = h2(ws, row, 1, "PvM target - a Monster you're attacking", span=2)
    for label, default in [("Monster MindDefenseVsM", 0), ("Monster ResistMagic (0-1)", 0),
                            ("Monster Resist (spell narrow sub-type) 0-1", 0), ("Monster HP", 100)]:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    row += 1
    row = h2(ws, row, 1, "PvP target - another Player you're attacking", span=2)
    for label, default in [("Enemy MindDefenseVsP", 0), ("Enemy ResistMagic (0-1)", 0),
                            ("Enemy Resist (spell narrow sub-type) 0-1", 0), ("Enemy HP", 100)]:
        ws.cell(row=row, column=1, value=label)
        input_cell(ws, row, 2, default)
        cells[label] = f"$B${row}"
        row += 1
    row += 2

    spell_table_row = row
    row = h2(ws, row, 1, "Spell data table (lookup source for the calculator above)", span=8)
    row = header_row(ws, row, 1, ["Spell", "DamageType", "EFFECT min", "EFFECT max", "ManaUse", "CastTime", "Magery req", "AreaRange(0=single)"])
    spell_data_start = row
    for s in SPELLS:
        data_rows(ws, row, 1, [list(s)])
        row += 1
    spell_data_end = row - 1
    row = note(ws, row, 1, "Source: scripts/Basic/skills/Magery/Spells/DamageSpells/damageSpells.def. How to change: "
                            "edit the spell's EFFECT=/MANAUSE=/CASTTIME=/Requirements= fields, restart server.", span=8)
    row += 1

    def lookup(col_offset):
        return (f"INDEX('Mage'!${get_column_letter(2+col_offset)}${spell_data_start}:${get_column_letter(2+col_offset)}${spell_data_end},"
                f"MATCH({cells['Spell']},'Mage'!$A${spell_data_start}:$A${spell_data_end},0))")

    row += 1
    sum_row = row
    ws.cell(row=row, column=1, value="Mind-power sum (EvalInt+SpiritSpeak+Int contributions)")
    output_cell(ws, row, 2,
                f"=(({cells['EvalInt skill (0-1000)']}/1000*C_EvalInt_EffectMax)"
                f"+({cells['SpiritSpeak skill (0-1000)']}/1000*C_SpiritSpeak_EffectMax)"
                f"+({cells['Int']}*C_mindPowerIntModifier))/1000")
    row += 1
    mp_row = row
    ws.cell(row=row, column=1, value="Your MindPowerVsP/VsM (weapon falls back to bare-hands default)")
    output_cell(ws, row, 2, f"=C_bareHandsMindPowerVsP*$B${sum_row}")
    row += 1
    casttime_row = row
    ws.cell(row=row, column=1, value="Spell cast time (seconds, looked up)")
    output_cell(ws, row, 2, f"={lookup(4)}")
    row += 1
    effmin_row = row
    ws.cell(row=row, column=1, value="Spell EFFECT min (looked up)")
    output_cell(ws, row, 2, f"={lookup(1)}")
    row += 1
    effmax_row = row
    ws.cell(row=row, column=1, value="Spell EFFECT max (looked up)")
    output_cell(ws, row, 2, f"={lookup(2)}")
    row += 2

    row = h2(ws, row, 1, "Results", span=4)
    row = header_row(ws, row, 1, ["", "PvM", "PvP"])
    r_power0 = row
    ws.cell(row=row, column=1, value="Raw spellPower = (MindPower - target MindDefense) x 10")
    output_cell(ws, row, 2, f"=($B${mp_row}-{cells['Monster MindDefenseVsM']})*10")
    output_cell(ws, row, 3, f"=($B${mp_row}-{cells['Enemy MindDefenseVsP']})*10")
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
    r_dam = row
    ws.cell(row=row, column=1, value="Raw spell damage (EFFECT curve, NOT clamped past max - see note above)")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=$B${effmin_row}+($B${effmax_row}-$B${effmin_row})*({cl}{r_power2}/1000)")
    row += 1
    r_resist = row
    ws.cell(row=row, column=1, value="Resist modifier (ResistMagic + spell's narrow sub-type)")
    output_cell(ws, row, 2, f"=(1-{cells['Monster ResistMagic (0-1)']})-{cells['Monster Resist (spell narrow sub-type) 0-1']}")
    output_cell(ws, row, 3, f"=(1-{cells['Enemy ResistMagic (0-1)']})-{cells['Enemy Resist (spell narrow sub-type) 0-1']}")
    row += 1
    r_final = row
    ws.cell(row=row, column=1, value="Final damage per hit")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"=MAX(0,{cl}{r_dam}*{cl}{r_resist})")
    row += 1
    r_dps = row
    ws.cell(row=row, column=1, value="DPS (damage / cast time - no swing-delay formula, casting IS the delay)")
    for col in (2, 3):
        cl = get_column_letter(col)
        output_cell(ws, row, col, f"={cl}{r_final}/$B${casttime_row}")
    row += 1
    r_hp = row
    ws.cell(row=row, column=1, value="Target HP")
    output_cell(ws, row, 2, f"={cells['Monster HP']}")
    output_cell(ws, row, 3, f"={cells['Enemy HP']}")
    row += 1
    for col in (2, 3):
        cl = get_column_letter(col)
        ws.cell(row=row, column=1, value="Time to kill (seconds)")
        output_cell(ws, row, col, f"=IF({cl}{r_dps}=0,\"never\",{cl}{r_hp}/{cl}{r_dps})")

    autosize(ws, {1: 50, 2: 20, 3: 20})
    for c in range(1, 9):
        if ws.column_dimensions[get_column_letter(c)].width is None or ws.column_dimensions[get_column_letter(c)].width < 14:
            ws.column_dimensions[get_column_letter(c)].width = 14


def main():
    wb = Workbook()
    build_overview(wb)
    build_materials(wb)
    build_core_formulas(wb)
    weapons_info = build_weapons_ref(wb)
    build_armor_ref(wb)
    monsters_info = build_monsters_ref(wb)
    build_melee_profession(wb, "Warrior", weapons_info, monsters_info)
    build_ranger(wb, weapons_info, monsters_info)
    build_mage(wb)
    build_mystic(wb, monsters_info)
    build_melee_profession(wb, "Thief", weapons_info, monsters_info,
                            ranged_note="Thief is also allowed Bow (per thief.def) - a Thief using a Bow follows "
                                        "the SAME RangerSettings override as the Ranger tab (not profession-gated). "
                                        "This calculator covers Thief's allowed melee weapons (OneHandSpike, "
                                        "TwoHandSpike, OneHandSword) only.")
    add_thief_ability_table(wb)

    import os
    os.makedirs(os.path.dirname(OUTPUT_PATH), exist_ok=True)
    wb.save(OUTPUT_PATH)
    print(f"Wrote {OUTPUT_PATH}")


if __name__ == "__main__":
    main()
