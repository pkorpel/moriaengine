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
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts {
	[ViewableClass]
	public partial class ContainerDef {
	}

	[ViewableClass]
	public partial class Container : Equippable {
		float weight;

		public override short Gump {
			get {
				short gump = this.TypeDef.Gump;
				if (gump == -1) {		//It has no defined gump
					AbstractItemDef idef = ThingDef.FindItemDef(this.Model);
					ContainerDef cdef = idef as ContainerDef;
					if (cdef != null) {
						gump = cdef.Gump;
					}
					if (gump == -1) {	//That one didn't exist, wasn't a container, or had no defined gump either.
						gump = 0x3c;		//The backpack gump.
					}
				}
				return gump;
			}
		}

		public override AbstractItem NewItem(IThingFactory factory, int amount) {
			Thing t = factory.Create(this);
			AbstractItem i = t as AbstractItem;
			if (i != null) {
				if (i.Cont != this) {
					i.Delete();
					throw new SEException("'" + i + "' ended outside the container... Wtf?");
				}
				if (i.IsStackable) {
					i.Amount = amount;
				}
				return i;
			}
			if (t != null) {
				t.Delete();//we created a character, wtf? :)
			}
			throw new SEException(factory + " did not create an item.");
		}

		public override void On_DClick(AbstractCharacter from) {
			//(TODO): check ownership(?), trigger snooping(done?), etc...
			Character topChar = this.TopObj() as Character;
			Character fromAsChar = (Character) from;
			if ((topChar != null) && (topChar != fromAsChar) && (!fromAsChar.IsGM)) {
				SkillSequenceArgs ssa = SkillSequenceArgs.Acquire(fromAsChar, SkillName.Snooping, this, null, null, null, null);
				ssa.PhaseSelect();
			} else {
				this.OpenTo(fromAsChar);
			}
		}

		public void Open() {
			this.OpenTo(Globals.SrcCharacter);
		}

		public sealed override bool IsContainer {
			get {
				return true;
			}
		}

		public sealed override bool CanContain {
			get {
				return true;
			}
		}

		[RegisterWithRunTests]
		public static void TestContainers() {
			Item backpack = null, sword = null, sword2 = null;

			try {
				Logger.Show("TestSuite", "Creating a backpack.");
				AbstractDef i_backpack = ThingDef.GetByDefname("i_backpack");
				Sanity.IfTrueThrow(i_backpack == null, "i_backpack does not exist!");
				Sanity.IfTrueThrow(!(i_backpack is ContainerDef), "i_backpack is not a ContainerDef!");
				backpack = (Item) ((ContainerDef) i_backpack).Create(1000, 500, 20, 0);
				Sanity.IfTrueThrow(backpack == null, "The backpack was not created!");
				Sanity.IfTrueThrow(!(backpack is Container), "The backpack is not a Container!");
				Logger.Show("TestSuite", "Creating a longsword inside the backpack.");
				AbstractDef i_sword_long = ThingDef.GetByDefname("i_sword_long");
				Sanity.IfTrueThrow(i_sword_long == null, "i_sword_long does not exist!");
				Sanity.IfTrueThrow(!(i_sword_long is EquippableDef), "i_sword_long is not an EquippableDef!");
				sword = (Item) ((EquippableDef) i_sword_long).Create(backpack);
				Sanity.IfTrueThrow(sword == null, "The sword was not created!");
				Sanity.IfTrueThrow(!(sword is Equippable), "The sword is not an Equippable!");
				Sanity.IfTrueThrow(sword.Cont != backpack, "The sword was not created in the backpack!");
				Sanity.IfTrueThrow(backpack.Count != 1, "The backpack thinks it has " + backpack.Count + " items, not 1!");
				Sanity.IfTrueThrow(backpack.FindCont(0) != sword, "The backpack doesn't have the sword as its first item!");
				Logger.Show("TestSuite", "Duping the sword.");
				uint suid = sword.FlaggedUid;
				sword2 = (Item) sword.Dupe();
				Sanity.IfTrueThrow(sword.FlaggedUid == sword2.FlaggedUid, "The duped sword has the same UID.");
				Sanity.IfTrueThrow(sword.FlaggedUid != suid, "The original sword's UID has changed!");

			} finally {
				if (sword2 != null && sword2.FlaggedUid != sword.FlaggedUid) sword2.Delete();
				if (sword != null) sword.Delete();
				if (backpack != null) backpack.Delete();
			}
		}

		public override float Weight {
			get {
				return this.weight;
			}
		}

		public override void FixWeight() {
			float w = this.Def.Weight;
			foreach (AbstractItem i in this) {
				if (i != null) {
					i.FixWeight();
					w += i.Weight;
				}
			}
			this.weight = w;
		}

		protected override void AdjustWeight(float adjust) {
			this.weight += adjust;
			base.AdjustWeight(adjust);
		}

		public override void On_BuildAosToolTips(AosToolTips toolTips, Language language) {
			toolTips.AddLine(1050044, this.Count.ToString(), this.Weight.ToString());
			//~1_COUNT~ items, ~2_WEIGHT~ stones
		}

		public override void On_Click(AbstractCharacter clicker, GameState clickerState, TcpConnection<GameState> clickerConn) {
			base.On_Click(clicker, clickerState, clickerConn);
			Language language = clickerState.Language;
			PacketSequences.SendNameFrom(clicker.GameState.Conn, this,
				string.Concat(this.Count.ToString(), " ", Loc<ContainerLoc>.Get(language).itemsGenitiv, ", ",
				this.Weight.ToString(), " ", Loc<ContainerLoc>.Get(language).stonesGenitiv),
				0);
		}
	}

	internal class ContainerLoc : CompiledLocStringCollection <ContainerLoc>{
		internal string itemsGenitiv = "items";
		internal string stonesGenitiv = "stones";
	}
}