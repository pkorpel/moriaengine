# NPC definition loading

`NPCDef` normalises Sphere-style values into typed fields. This is a specialised authoring
contract within [definition persistence](persistence-and-saving.md).

| Shape | Examples | Rule |
|---|---|---|
| Scalar | Aggro mode, realm, movement, spell range, flags | Typed field. |
| Range | Stats, experience, ability tuning | `int[]`; normalise one value or `min max` into array form. |
| Repeated list | Spells, summons, loot, equipment, resources | Collect entries in script order. |
| Associated lists | Equipment colours/names | Keep indexing aligned with equipment. |

Derived properties such as `Spells`, `Loot`, `StartingEquipment` and `SkillValueRanges`
cache parsed views of their backing arrays. Replacing a field through a GM edit invalidates
the corresponding view. `AbilityTuning` supplies named access for some ability families.

## Keywords and loading order

Use distinct names for custom keywords and backing fields, such as `addspell` and `spellList`.
A name matching a typed field is handled in `AbstractDef.LoadScriptLines` before the postponed
custom loader. Lists naming definitions need the later phase, after definitions exist.

Unsupported range shapes are warned about and left unset. List lines containing quotes,
`<` or `>` are rejected to avoid evaluating and freezing per-spawn expressions as one-time
field values. Array fields have no simple text editor in the def info dialog.

When adding syntax, verify script loading, saved overrides and reloading changed defaults.
A C# build alone does not exercise these phases.

## Sources

[NPCDef](../../../scripts/Basic/CharClasses/NPC.cs),
[baseline list values](../../../Tests/Protocol/Data/NpcDefLists.txt),
[NPC AI](../04-gameplay-systems/pets-and-npc-ai.md).
`NpcDefFieldsTests.TheListsReadAsTheyDid` covers interpreted list values.
