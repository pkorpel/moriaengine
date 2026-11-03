using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1070 - a blade used on a corpse (weapon_utils.scp's f_weapon_sharp_targon_item, f_markIfCorpse,
	/// funkce.scp's addflesh), or a dagger on a head (the shaman's voodoo). Every carved corpse is marked: a carved
	/// corpse can't be raised by bandages nor rise by itself. A player's body falls apart into a head, a torso,
	/// arms and legs, all of them knowing whose they were; any other body gives what its NPC's RESOURCES list,
	/// as Sphere's own carving. Each body only once.</summary>
	public static class CorpseCarving {
		internal static readonly TagKey CarvedTK = TagKey.Acquire("rozrezano");
		internal static readonly TagKey CarvedByTK = TagKey.Acquire("rozrezal");
		private static readonly TagKey fleshTakenTK = TagKey.Acquire("rezal");
		internal static readonly TagKey HeadOfTK = TagKey.Acquire("_headOf_");
		private const int PartsDecaySeconds = 300;
		private static readonly string[] bodyParts = { "i_flesh_torso", "i_flesh_left_arm", "i_flesh_right_arm", "i_flesh_left_leg", "i_flesh_right_leg" };
		private static readonly int[] blood = { 0x122a, 0x122b, 0x122c, 0x122d, 0x122e };

		/// <summary>True if the target was something a blade does this to.</summary>
		internal static bool TryBlade(Player self, Item blade, Thing targetted) {
			if (IsHead(targetted as Item)) {
				int tier = ShamanVoodoo.DaggerTier(blade);
				if (tier > 0) {
					ShamanVoodoo.Curse(self, (Item) targetted, tier);
					return true;
				}
				return false;
			}
			if (targetted is Corpse corpse) {
				Carve(self, corpse);
				return true;
			}
			return false;
		}

		internal static bool IsHead(Item item) {
			return (item != null) && ((item.Def.PrettyDefname == "i_flesh_head") || (item.Def.PrettyDefname == "i_flesh_head_2"));
		}

		/// <summary>The one whose head this is, or null.</summary>
		internal static Character HeadOf(Item head) {
			Character of = head.GetTag(HeadOfTK) as Character;
			return ((of == null) || of.IsDeleted) ? null : of;
		}

		internal static void Carve(Player self, Corpse corpse) {
			corpse.SetTag(CarvedTK, 1);
			corpse.SetTag(CarvedByTK, self);
			self.SysMessage((self.IsFemale ? "Rozrezala" : "Rozrezal") + " jsi mrtvolu.");
			Logger.WriteDebug(self + " rozrezal mrtvolu " + corpse + " na pozici " + corpse.P());
			if (corpse.HasTag(fleshTakenTK)) {
				return;
			}
			corpse.SetTag(fleshTakenTK, 1);
			Character owner = corpse.Owner;
			if (owner is Player) {
				AddFlesh(corpse, owner);
			} else if ((corpse.CharDef is NPCDef npcDef) && (npcDef.ButcherResources != null)) {
				foreach (NPCDef.ResourceEntry entry in npcDef.ButcherResources) {
					if ((entry.Amount > 0) && (ThingDef.GetByDefname(entry.Defname) is ItemDef def)) {
						Item made = (Item) def.Create(corpse.X, corpse.Y, corpse.Z, corpse.M);
						made.Amount = entry.Amount;
					}
				}
			}
		}

		/// <summary>addflesh: a head (a man's or a woman's) named after the dead, the rest of the body and blood.</summary>
		private static void AddFlesh(Corpse corpse, Character owner) {
			Item head = Part(corpse, owner.IsFemale ? "i_flesh_head_2" : "i_flesh_head", owner);
			if (head != null) {
				head.Name = owner.Name;
			}
			foreach (string part in bodyParts) {
				Part(corpse, part, owner);
			}
			if (ThingDef.FindItemDef(blood[Globals.dice.Next(blood.Length)]) is ItemDef bloodDef) {
				Item pool = (Item) bloodDef.Create(corpse.X, corpse.Y, corpse.Z, corpse.M);
				pool.Name = "krev";
				ItemDecaySystem.ScheduleIn(pool, PartsDecaySeconds);
			}
		}

		private static Item Part(Corpse corpse, string defname, Character owner) {
			if (!(ThingDef.GetByDefname(defname) is ItemDef def)) {
				return null;
			}
			Item part = (Item) def.Create(corpse.X, corpse.Y, corpse.Z, corpse.M);
			part.SetTag(HeadOfTK, owner);
			ItemDecaySystem.ScheduleIn(part, PartsDecaySeconds);
			return part;
		}
	}

	/// <summary>#1070 - a blade with no tool skill of its own, used on something: a corpse is carved.</summary>
	public class Targ_Blade : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Na co to chces pouzit?");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			CorpseCarving.TryBlade(self, parameter as Item, targetted);
			return TargetResult.Done;
		}
	}
}
