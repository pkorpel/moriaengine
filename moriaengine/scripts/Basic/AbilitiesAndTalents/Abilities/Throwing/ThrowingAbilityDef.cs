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

using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public class ThrowingAbilityDef : ActivableAbilityDef {

		private FieldValue range;
		private FieldValue immunityDuration;

		private static ProjectileDef i_kudla;
		public static ProjectileDef ThrowingKnifeDef {
			get {
				if (i_kudla == null) {
					i_kudla = (ProjectileDef) ThingDef.GetByDefname("i_kudla");
				}
				return i_kudla;
			}
		}

		public ThrowingAbilityDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.range = this.InitTypedField("range", 5, typeof(int));
			this.immunityDuration = this.InitTypedField("immunityDuration", 10, typeof(double));
		}

		public int Range {
			get {
				return (int) this.range.CurrentValue;
			}
			set {
				this.range.CurrentValue = value;
			}
		}

		public double ImmunityDuration {
			get {
				return (double) this.immunityDuration.CurrentValue;
			}
			set {
				this.immunityDuration.CurrentValue = value;
			}
		}

		protected override void On_DenyActivate(DenyAbilityArgs args) {
			Character self = args.abiliter;
			if (GetThrowingKnife(self) == null) {
				args.Result = DenyMessages_Throwing.Deny_YouNeedThrowingKnife;
				return;
			}

			SkillSequenceArgs seq = self.CurrentSkillArgs;
			if ((seq != null) && (seq.SkillDef is WeaponSkillDef)) {
				Character target = (Character) seq.Target1;
				int distance = Point2D.GetSimpleDistance(self, target);
				int range = this.Range + self.WeaponRangeModifier;
				if (distance > range) {
					args.Result = DenyResultMessages.Deny_ThatIsTooFarAway;
					return;
				}
				DenyResult losAndAlive = self.CanInteractWith(target);
				if (!losAndAlive.Allow) {
					args.Result = losAndAlive;
					return;
				}
			} else {
				args.Result = DenyMessages_Throwing.Deny_OnlyWorksWhenFighting;
				return;
			}

			base.On_DenyActivate(args);
		}

		protected override void On_Activate(Character self, Ability ab) {
			Projectile knife = (Projectile) self.GetTag(throwingKnifeTK); //checked and set in DenyActivate

			Character target = (Character) self.CurrentSkillArgs.Target1; //checked in DenyActivate
			int distance = Point2D.GetSimpleDistance(self, target);

			//TODO

		}


		private static TagKey throwingKnifeTK = TagKey.Acquire("_throwing_knife_");		

		//todo? return specialised knife according to players preset preference
		public static Projectile GetThrowingKnife(Character self) {
			Projectile knife = self.GetTag(throwingKnifeTK) as Projectile;
			if (knife == null) {
				if (self.CanPickup(knife).Allow) {
					return knife;
				}
			}

			ProjectileDef def = ThrowingKnifeDef;
			foreach (Item i in self.Backpack.EnumShallow()) {
				knife = i as Projectile;
				if ((knife != null) && (knife.Def == def)) {
					if (self.CanPickup(knife).Allow) {
						self.SetTag(throwingKnifeTK, knife);
						return knife;
					}
				}
			}

			self.RemoveTag(throwingKnifeTK);
			return null;
		}
	}

	public static class DenyMessages_Throwing {
		public static readonly DenyResult Deny_YouNeedThrowingKnife =
			new CompiledLocDenyResult<AbilityDefLoc>("youNeedThrowingKnife");

		public static readonly DenyResult Deny_OnlyWorksWhenFighting =
			new CompiledLocDenyResult<AbilityDefLoc>("onlyWorksWhenFighting");
	}

	public class ThrowingLoc : CompiledLocStringCollection<ThrowingLoc> {
		public string youNeedThrowingKnife = "Nemáš žádný házecí nùž.";
		public string onlyWorksWhenFighting = "Házecí nože lze použít jen bìhem boje.";
	}
}
