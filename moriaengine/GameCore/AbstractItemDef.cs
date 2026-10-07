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

using System.Collections.Generic;
using System.Collections.ObjectModel;
using SteamEngine.Common;

namespace SteamEngine {
	public abstract class AbstractItemDef : ThingDef {
		private FieldValue type;
		private FieldValue pluralName;

		private FieldValue dupeItem;
		//private FieldValue clilocName;
		private FieldValue mountChar;
		private FieldValue flippable;
		private FieldValue stackable;

		private FieldValue dropSound;

		private List<AbstractItemDef> dupeList;
		private ReadOnlyCollection<AbstractItemDef> dupeListReadOnly;

		private ItemDispidInfo dispidInfo;

		protected AbstractItemDef(string defname, string filename, int headerLine)
			: base(defname, filename, headerLine) {

			this.type = this.InitTypedField("type", null, typeof(TriggerGroup));
			this.pluralName = this.InitTypedField("pluralName", "", typeof(string));

			this.dupeItem = this.InitThingDefField("dupeItem", null, typeof(AbstractItemDef));
			//clilocName = InitField_Typed("clilocName", "0", typeof(uint));
			this.mountChar = this.InitThingDefField("mountChar", null, typeof(AbstractCharacterDef));
			this.flippable = this.InitTypedField("flippable", false, typeof(bool));
			this.stackable = this.InitTypedField("stackable", false, typeof(bool));

			this.dropSound = this.InitTypedField("dropSound", 87, typeof(int));
		}

		public AbstractItemDef DupeItem {
			get {
				return (AbstractItemDef) this.dupeItem.CurrentValue;
			}
			set {
				AbstractItemDef di = (AbstractItemDef) this.dupeItem.CurrentValue;
				if (di != null) {
					di.RemoveFromDupeList(this);
				}
				this.dupeItem.CurrentValue = value;
				if (value != null) {
					value.AddToDupeList(this);
				}
			}
		}

		public void AddToDupeList(AbstractItemDef idef) {
			if (this.dupeList == null) {
				this.dupeList = new List<AbstractItemDef>();
				this.dupeListReadOnly = new ReadOnlyCollection<AbstractItemDef>(this.dupeList);
			}
			if (!this.dupeList.Contains(idef)) {
				this.dupeList.Add(idef);
			}
		}

		public void RemoveFromDupeList(AbstractItemDef idef) {
			Sanity.IfTrueThrow(this.dupeList == null, "RemoveFromDupeList called on an itemdef without a dupelist (" + this + ").");
			Sanity.IfTrueThrow(!this.dupeList.Contains(idef), "In RemoveFromDupeList, Itemdef " + idef + " is not in " + this + "'s dupeList!");
			this.dupeList.Remove(idef);
			if (this.dupeList.Count == 0) {
				this.dupeList = null;
			}
		}

		public ReadOnlyCollection<AbstractItemDef> DupeList {
			get {
				return this.dupeListReadOnly;
			}
		}

		public int GetNextFlipModel(int curModel) {
			if (curModel == this.Model) {
				if (this.dupeList != null) {
					AbstractItemDef dup = this.dupeList[0];
					return dup.Model;
				}
			} else {
				if (this.dupeList != null) {
					int cur = -1;
					for (int a = 0; a < this.dupeList.Count; a++) {
						AbstractItemDef dup = this.dupeList[0];
						if (dup.Model == curModel) {
							cur = a;
							break;
						}
					}
					if (cur + 1 < this.dupeList.Count) {
						AbstractItemDef dup = this.dupeList[cur + 1];
						return dup.Model;
					}
				}
			}
			return this.Model;
		}

		public AbstractCharacterDef MountChar {
			get {
				return (AbstractCharacterDef) this.mountChar.CurrentValue;
			}
			set {
				this.mountChar.CurrentValue = value;
			}
		}

		private static TriggerGroup t_normal;
		private static TriggerGroup T_Normal {
			get {
				if (t_normal == null) {
					t_normal = TriggerGroup.GetByDefname("t_normal");
				}
				return t_normal;
			}
		}


		public TriggerGroup Type {
			get {
				TriggerGroup tg = (TriggerGroup) this.type.CurrentValue;
				if (tg == null) {
					return T_Normal;
				}
				return (TriggerGroup) this.type.CurrentValue;
			}
			set {
				this.type.CurrentValue = value;
			}
		}

		public bool IsStackable {
			get {
				return (bool) this.stackable.CurrentValue;
			}
			set {
				this.stackable.CurrentValue = value;
			}
		}

		public bool IsFlippable {
			get {
				return (bool) this.flippable.CurrentValue;
			}
			set {
				this.flippable.CurrentValue = value;
			}
		}

		public int DropSound {
			get {
				return (int) this.dropSound.CurrentValue;
			}
			set {
				this.dropSound.CurrentValue = value;
			}
		}

		public override string Name {
			get {
				if (this.name.IsDefaultCodedValue) {
					ItemDispidInfo idi = this.DispidInfo;
					if (idi != null) {
						return idi.SingularName;
					}
				}

				return base.Name;
			}
			set {
				string singular, plural;
				if (ItemDispidInfo.ParseName(value, out singular, out plural)) {
					base.Name = singular;
					this.PluralName = plural;
				} else {
					base.Name = value;
				}
			}
		}

		public string PluralName {
			get {
				if (!this.pluralName.IsDefaultCodedValue) {
					return (string) this.pluralName.CurrentValue;
				}
				if (this.name.IsDefaultCodedValue) {
					ItemDispidInfo idi = this.DispidInfo;
					if (idi != null) {
						return idi.PluralName;
					}
				}

				return this.Name;
			}
			set {
				this.pluralName.CurrentValue = value;
			}
		}

		public sealed override bool IsItemDef {
			get {
				return true;
			}
		}

		public sealed override bool IsCharDef {
			get {
				return false;
			}
		}

		public MultiData MultiData {
			get {
				return this.multiData;
			}
		}

		public ItemDispidInfo DispidInfo {
			get {
				int model = this.Model;
				if ((this.dispidInfo == null) || (this.dispidInfo.Id != model)) {
					this.dispidInfo = ItemDispidInfo.GetByModel(model);
				}
				return this.dispidInfo;
			}
		}

		protected override void LoadScriptLine(string filename, int line, string param, string args) {
			if ("stack".Equals(param)) {
				param = "stackable";
			}
			if ("isstackable".Equals(param)) {
				param = "stackable";
			}
			if ("flip".Equals(param)) {
				param = "flippable";
			}
			if ("isflippable".Equals(param)) {
				param = "flippable";
			}
			//if ("cliloc".Equals(param)) {
			//    param = "clilocname";
			//}

			switch (param) {
				case "dupelist": //Do nothing (for now?)
					break;
				case "name":
					args = ConvertTools.LoadSimpleQuotedString(args);

					string singular, plural;
					if (ItemDispidInfo.ParseName(args, out singular, out plural)) {
						this.pluralName.SetFromScripts(filename, line, "\"" + plural + "\"");
						base.LoadScriptLine(filename, line, param, "\"" + singular + "\"");//will normally load name
					} else {
						base.LoadScriptLine(filename, line, param, "\"" + args + "\"");//will normally load name
					}
					break;
				default:
					base.LoadScriptLine(filename, line, param, args);//the AbstractThingDef Loadline
					break;
			}
		}

		public override void Unload() {
			if (this.dupeList != null) {
				this.dupeList.Clear();
			}
			base.Unload();
			//other various properties...
			//todo: not clear those tags/tgs/timers/whatever that were set dynamically (ie not in scripted defs)
		}

		public override int Height {
			get
			{
				if (this.height.IsDefaultCodedValue) {
					//if (this.IsContainer) {
					//    return 4;
					//}
					ItemDispidInfo idi = this.DispidInfo;
					if (idi == null) {
						return 1;
					}
					return idi.CalcHeight;
				}
				return (int) this.height.CurrentValue;
			}
		}
	}
}
