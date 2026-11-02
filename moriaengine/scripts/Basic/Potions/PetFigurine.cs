using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>t_figurine_2: a shrunk pet, a double click brings it back as the new owner's.</summary>
	[ViewableClass]
	public partial class PetFigurine {
		internal void Keep(NPC pet) {
			this.petDef = (NPCDef) pet.Def;
			this.petName = pet.Name;
			this.petColor = pet.Color;
			this.petHits = pet.Hits;
			this.Name = pet.Name;
			this.Color = pet.Color;
			int icon = IconOf(this.petDef);
			if (icon != 0) {
				this.Model = icon;
			}
		}

		//Erebor makes the item its ICON names; two of the archive's name nothing, and 394 creatures have none
		internal static int IconOf(NPCDef def) {
			try {
				return def.Icon;
			} catch (SEException) {
				return 0;
			}
		}

		public override void On_DClick(AbstractCharacter from) {
			Character owner = from as Character;
			if ((owner == null) || (this.petDef == null)) {
				return;
			}
			Character pet = (Character) this.petDef.Create(owner.P());
			pet.Name = this.petName;
			pet.Color = this.petColor;
			pet.ControlMaster = owner;
			if (this.petHits > 0) {
				pet.Hits = (short) this.petHits;
			}
			this.Delete();
		}
	}

	[ViewableClass]
	public partial class PetFigurineDef {
	}
}
