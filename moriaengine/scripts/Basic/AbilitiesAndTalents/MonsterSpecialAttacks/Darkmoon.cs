/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Reflection;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1022 e_darkmoon_boss (Darkmoon): when hit, Chance permille, at most once in Cooldown seconds, she takes another
	/// of her Forms (never the one she has): its look, colour, hits, stamina, mana, Magery, Eval Int, resists, abilities and
	/// their numbers, from the form's NPCDef (darkmoonForms.def; her own def is her own form). Fixed from Erebor, as the plan
	/// on #1022 says: a form replaces the last one's abilities and numbers (Erebor's EVENTS= only added, so after a few
	/// changes she had every form's abilities at once, with the latest numbers), and her current hits never stay above
	/// her new maximum.</summary>
	[ViewableClass]
	public class DarkmoonAbilityDef : PassiveAbilityDef {
		private FieldValue chance, cooldown, forms;

		public DarkmoonAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {
			this.chance = this.InitTypedField("darkmoonChance", 200.0, typeof(double));
			this.cooldown = this.InitTypedField("darkmoonCooldown", 20.0, typeof(double));
			this.forms = this.InitTypedField("darkmoonForms", "c_darkmoon,c_darkmoon_worker,c_darkmoon_warrior,c_darkmoon_beetle,c_darkmoon_bug,c_darkmoon_queen", typeof(string));
		}

		public new double Chance { get { return (double) this.chance.CurrentValue; } set { this.chance.CurrentValue = value; } }
		public new double Cooldown { get { return (double) this.cooldown.CurrentValue; } set { this.cooldown.CurrentValue = value; } }

		/// <summary>NPCDef defnames, split by commas.</summary>
		public string Forms { get { return (string) this.forms.CurrentValue; } set { this.forms.CurrentValue = value; } }
	}

	public static partial class MonsterSpecialAttacks {
		internal static DarkmoonAbilityDef DarkmoonDef { get { return Lookup<DarkmoonAbilityDef>("a_tevent_e_darkmoon_boss"); } }

		private static readonly TagKey tuningDefTK = TagKey.Acquire("_npcTuningDef_");
		private static readonly TagKey nextDarkmoonFormTK = TagKey.Acquire("_darkmoonNextForm_");

		/// <summary>The NPCDef an ability reads its per-species numbers from: a form's while it has one, else its own.</summary>
		internal static NPCDef TuningDef(Character ch) {
			return (ch.GetTag(tuningDefTK) as NPCDef) ?? (ch.Def as NPCDef);
		}

		/// <summary>TryReactToHit: maybe another form.</summary>
		internal static void TryDarkmoonMorph(NPC self) {
			DarkmoonAbilityDef def = DarkmoonDef;
			if (!Reacts(self, def)) {
				return;
			}
			double now = Globals.TimeInSeconds;
			if (((self.GetTag(nextDarkmoonFormTK) is double next) && (next > now)) || !RollChance(def.Chance)) {
				return;
			}
			self.SetTag(nextDarkmoonFormTK, now + def.Cooldown);
			NPCDef form = PickDarkmoonForm(self, def);
			if (form != null) {
				TakeForm(self, form);
			}
		}

		/// <summary>getMorfBody: any form but the current one.</summary>
		internal static NPCDef PickDarkmoonForm(NPC self, DarkmoonAbilityDef def) {
			NPCDef current = TuningDef(self);
			List<NPCDef> choices = new List<NPCDef>();
			foreach (string name in def.Forms.Split(',')) {
				if ((ThingDef.GetByDefname(name.Trim()) is NPCDef form) && (form != current)) {
					choices.Add(form);
				}
			}
			return (choices.Count == 0) ? null : choices[Globals.dice.Next(choices.Count)];
		}

		/// <summary>setMorfBody + f_&lt;form&gt;: the form's look and numbers, its abilities instead of the last form's.</summary>
		internal static void TakeForm(NPC self, NPCDef form) {
			NPCDef previous = TuningDef(self);
			if (previous?.StartingAbilities != null) {
				foreach (AbilityDef ability in previous.StartingAbilities) {
					if ((form.StartingAbilities == null) || !form.StartingAbilities.Contains(ability)) {
						self.SetRealAbilityPoints(ability, 0);
					}
				}
			}
			if (form.StartingAbilities != null) {
				foreach (AbilityDef ability in form.StartingAbilities) {
					self.SetRealAbilityPoints(ability, 1);
				}
			}
			if (form == self.Def) {
				self.RemoveTag(tuningDefTK);
			} else {
				self.SetTag(tuningDefTK, form);
			}
			self.Model = form.Model;
			self.Color = form.Color;
			int maxHits = form.RollMaxHits();
			if (maxHits > 0) {
				self.MaxHits = (short) maxHits;
				self.Hits = (short) Math.Min(self.Hits, self.MaxHits);
			}
			int maxStam = form.RollMaxStam();
			if (maxStam > 0) {
				self.MaxStam = (short) maxStam;
				self.Stam = (short) Math.Min(self.Stam, self.MaxStam);
			}
			int maxMana = form.RollMaxMana();
			if (maxMana >= 0) {
				self.MaxMana = (short) maxMana;
				self.Mana = (short) Math.Min(self.Mana, self.MaxMana);
			}
			if (form.SkillValueRanges != null) {
				foreach (SkillName skill in form.SkillValueRanges.Keys) {
					int rolled = form.RollSkillValue(skill);
					if (rolled >= 0) {
						self.SetRealSkillValue((int) skill, rolled);
					}
				}
			}
			if (form.ResistRanges != null) {
				foreach (KeyValuePair<string, int[]> entry in form.ResistRanges) {
					PropertyInfo resist = typeof(Character).GetProperty(entry.Key, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
					resist?.SetValue(self, NPCDef.RollStatRange(entry.Value), null);
				}
			}
		}
	}
}
