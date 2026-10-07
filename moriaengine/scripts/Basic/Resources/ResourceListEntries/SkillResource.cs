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
using System.Globalization;
using SteamEngine.Common;

namespace SteamEngine.CompiledScripts {
	/// <summary>Resource as SkillDef</summary>
	public class SkillResource : AbstractResourceListEntry, IResourceListEntry_Simple {
		private readonly SkillDef skillDef;

		public static void Bootstrap() {
			ResourcesListParser.RegisterResourceParser(TryParse);
		}

		/// <summary>Try parsing given string as ItemResource</summary>
		public static bool TryParse(string definition, double number, bool asPercentage, out IResourceListEntry resource) {
			var skl = (SkillDef) AbstractSkillDef.GetByKey(definition); //"hiding", "anatomy" etc.
			if (skl == null) {
				skl = (SkillDef) AbstractSkillDef.GetByDefname(definition); //"skill_hiding, "skill_anatomy" etc.
			}
			if (skl != null) {
				resource = new SkillResource(skl, number, asPercentage);
				return true;
			}
			resource = null;
			return false;
		}

		private SkillResource(SkillDef skillDef, double number, bool asPercentage)
			: base(number, asPercentage) {

			this.skillDef = skillDef;

			if (asPercentage) {
				throw new SEException("Cannot use skill as percentage resource");
			}
		}

		public SkillDef SkillDef {
			get { return this.skillDef; }
		}

		public override string ParsableString {
			get {
				return this.skillDef.PrettyDefname;
			}
		}

		public override bool IsSameResource(IResourceListEntry newOne) {
			SkillResource newResource = newOne as SkillResource;
			if (newResource != null) {
				return (this.skillDef == newResource.skillDef);
			}
			return false;
		}

		public override string GetResourceMissingMessage(Language language) {
			return string.Format(CultureInfo.InvariantCulture,
				Loc<ResListLoc>.Get(language).SkillTooLow, this.skillDef.Key);
		}

		#region IResourceListEntry_Simple Members
		public bool IsResourcePresent(Character chr) {
			return this.skillDef.SkillValueOfChar(chr) >= this.DesiredCount;
		}

		/// <summary>Indicates whether this is a consumable resource</summary>
		public bool IsConsumable { get { return false; } }

		/// <summary>Consumes this resource. Throws if this is not a consumable resource.</summary>
		public void Consume(Character ch) {
			throw new InvalidOperationException("Skills can not be consumed as a resource.");
		}
		#endregion
	}
}