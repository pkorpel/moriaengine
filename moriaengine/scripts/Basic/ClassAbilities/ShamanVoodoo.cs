using System;
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1070 - the shaman's voodoo on a carved head (specabilities.scp's woodoo, gvoodoo, woodoohandle,
	/// woodooeffect). A dagger on the head curses its owner, the dagger's metal deciding how (copper 1 to mithril 7);
	/// a potion on the head blesses (Strength, Cunning, Agility, Stoneskin: 11 to 14). The owner must be in the
	/// shaman's area. A curse lasts while its owner isn't blessed, coming back every 30 s.</summary>
	public static class ShamanVoodoo {
		internal const int Damage = 1, Exhaust = 2, Bleed = 3, Poisoned = 4, Shock = 5, NoPort = 6, NoHorse = 7;
		internal const int Strength = 11, Cunning = 12, Agility = 13, Protect = 14;
		private static readonly TagKey headDurabilityTK = TagKey.Acquire("vydrz");
		private static readonly string[] metals = { "_copper", "_iron", "_verite", "_valorite", "_obsidian", "_adamantinum", "_mithril" };

		/// <summary>The curse of a dagger of each metal: woodoo(1) on i_dagger_copper to woodoo(7) on i_dagger_mithril.</summary>
		internal static int DaggerTier(Item blade) {
			if (blade == null) {
				return 0;
			}
			string name = blade.Def.PrettyDefname;
			for (int i = 0; i < metals.Length; i++) {
				if (string.Equals(name, "i_dagger" + metals[i], StringComparison.OrdinalIgnoreCase)) {
					return i + 1;
				}
			}
			return 0;
		}

		private static bool CanVoodoo(Player self) {
			ShamanProfessionPlugin shaman = ShamanProfessionPlugin.Of(self); //#1442 - the cooldown is its
			if (shaman == null) {
				self.SysMessage("Co tim chces dokazat?");
			} else if (self.Level < 20) {
				self.SysMessage("Nejsi jeste dost dobry saman.");
			} else if (WarriorAbilities.IsFrozen(self)) {
				self.SysMessage("You are frozen and cannot move.");
			} else if ((shaman.nextVoodoo > Globals.TimeInSeconds) && !self.IsGM) {
				self.SysMessage("Jeste nelze pouzit.");
			} else {
				return true;
			}
			return false;
		}

		/// <summary>[Function woodoo]: a dagger on a head.</summary>
		internal static void Curse(Player self, Item head, int curse) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			if (!CanVoodoo(self)) {
				return;
			}
			if (self.HasTimer(VoodooEffectTimer.Key)) {
				self.SysMessage("Prokleti ti brani proklinat nekoho jinyho.");
				return;
			}
			if (Globals.dice.Next(1000) >= 250 + (20 * ClassAbilities.Points(self, "woodoo_curse"))) {
				self.SysMessage("Nepovedlo se.");
				VoodooEffectTimer.Apply(self, curse, null); //a failed curse falls on the shaman
				ShamanProfessionPlugin.Of(self).nextVoodoo = Globals.TimeInSeconds + settings.voodooDelaySeconds;
				return;
			}
			HidingSkillDef.Reveal(self);
			self.Say("Heru Curu Mor Tir");
			ShamanProfessionPlugin.Of(self).nextVoodoo = Globals.TimeInSeconds + (settings.voodooDelaySeconds * 10);
			Handle(self, CorpseCarving.HeadOf(head), curse);
			int durability = Convert.ToInt32(head.GetTag(headDurabilityTK) ?? 0);
			if (durability == 0) {
				head.SetTag(headDurabilityTK, 100);
			} else {
				if (Globals.dice.Next(101) > durability) {
					head.Delete();
					self.SysMessage("Hlava se rozpadla.");
				} else {
					head.SetTag(headDurabilityTK, durability - Globals.dice.Next(1, 11));
				}
				self.Stam = 0;
				self.Mana = (short) (self.Mana / 2);
			}
		}

		/// <summary>[Function gvoodoo]: a potion on a head.</summary>
		internal static void Bless(Player self, Item head, Item potion) {
			if (!CanVoodoo(self)) {
				return;
			}
			if (Globals.dice.Next(10001) > (ClassAbilities.Points(self, "woodoo") * ClassAbilitySettings.instance.voodooBlessChance)) {
				self.SysMessage("Nepovedlo se.");
				return;
			}
			HidingSkillDef.Reveal(self);
			self.Say("Heru Curu Mor Tir");
			ShamanProfessionPlugin.Of(self).nextVoodoo = Globals.TimeInSeconds + (ClassAbilitySettings.instance.voodooDelaySeconds / 10);
			int blessing;
			switch ((potion == null) ? "" : potion.Def.PrettyDefname.ToLowerInvariant()) {
				case "i_potion_strengthless":
					blessing = Strength;
					break;
				case "i_potion_cleverless":
					blessing = Cunning;
					break;
				case "i_potion_agilityless":
					blessing = Agility;
					break;
				case "i_potion_stoneskin":
					blessing = Protect;
					break;
				default:
					self.SysMessage("radeji ne");
					return;
			}
			Handle(self, CorpseCarving.HeadOf(head), blessing);
		}

		/// <summary>woodoohandle: the voodoo reaches its owner in 10 s, a blessing in 5, if they're in the shaman's area.</summary>
		private static void Handle(Player shaman, Character victim, int kind) {
			if ((victim == null) || (RegionEventSystem.AreaOf(victim.Region) != RegionEventSystem.AreaOf(shaman.Region))) {
				shaman.RedMessage("Prokleti nenalezlo cil.");
				return;
			}
			victim.SysMessage("Mas pocit jako by nekdo tancil na tvem hrobe!");
			string[] feelings = { "Citis jako by te probodla dyka", "Citis podivnou bolest", "Citis se velmi divne", "Mas neblahe tuseni smrti" };
			victim.Emote(feelings[Globals.dice.Next(feelings.Length)], 0);
			victim.Sound(0x19d);
			shaman.SysMessage("Prokleti voodoo seslano uspesne", ClassAbilities.ClassMessageHue);
			VoodooHandleTimer timer = new VoodooHandleTimer();
			timer.shaman = shaman;
			timer.kind = kind;
			victim.AddTimer(TimerKey.Acquire("_voodooHandle_" + Globals.dice.Next()), timer);
			timer.DueInSeconds = (kind > 10) ? 5 : 10;
		}

		/// <summary>i_mem_woodoo's @timer: one already cursed throws a curse back, and so does magic reflection.</summary>
		internal static void Arrive(Character victim, Player shaman, int kind) {
			if ((kind < 10) && victim.HasTimer(VoodooEffectTimer.Key)) {
				victim.SysMessage("Neprijemny pocit pominul.");
				shaman?.SysMessage("Cil " + victim.Name + " odolal.", ClassAbilities.ClassMessageHue);
				if (shaman != null) {
					VoodooEffectTimer.Apply(shaman, kind, null);
				}
			} else if ((kind < 10) && (MagicReflectionPluginDef.GetMagicReflectionPlugin(victim) != null)) {
				shaman?.SysMessage("Tva magie byla odrazena!", ClassAbilities.ClassMessageHue);
				victim.SysMessage("Citis jakoby nekdo v dalce vykrikl prekvapenim.");
				if (shaman != null) {
					VoodooEffectTimer.Apply(shaman, kind, null);
				}
			} else {
				shaman?.SysMessage("Cil podlehl voodoo!", ClassAbilities.ClassMessageHue);
				victim.SysMessage("Jsi pod vlivem voodoo magie!");
				VoodooEffectTimer.Apply(victim, kind, shaman);
			}
		}

		/// <summary>woodooeffect's on-line, again every 30 s while it lasts.</summary>
		internal static void On(Character ch, int kind, Character shaman) {
			switch (kind) {
				case Damage:
					ch.Hits = (short) (ch.Hits - (ch.Hits / 5));
					break;
				case Exhaust:
					ch.Stam = 0;
					break;
				case Bleed:
					SingletonScript<BleedingEffectPluginDef>.Instance.Strike(shaman ?? ch, ch, 10);
					break;
				case Poisoned:
					Poison.Apply(ch, shaman, 8, PoisonEffectSettings.instance.SecondsFor(PoisonKind.PercentOfHits), PoisonKind.PercentOfHits);
					break;
				case Strength:
				case Cunning:
				case Agility:
					string spell = (kind == Strength) ? "s_strength" : ((kind == Cunning) ? "s_Cunning" : "s_Agility");
					int power = (1334 * ((shaman == null) ? 0 : shaman.GetSkill(SkillName.SpiritSpeak))) / 1000;
					((SpellDef) SpellDef.GetByDefname(spell)).ApplyEffectChar(shaman ?? ch, ch, power, EffectFlag.FromAbility | EffectFlag.BeneficialEffect);
					break;
				case Protect:
					int protection = (14 * ((shaman == null) ? 0 : shaman.GetSkill(SkillName.SpiritSpeak))) / 10;
					((SpellDef) SpellDef.GetByDefname("s_protection")).ApplyEffectChar(shaman ?? ch, ch, protection, EffectFlag.FromAbility | EffectFlag.BeneficialEffect);
					if (ch.FindLayer(LayerNames.Chest) is Item chest) {
						chest.Cont = ch.Backpack;
					}
					break;
			}
		}

		/// <summary>woodooeffect's off-line.</summary>
		internal static void Off(Character ch, int kind) {
			switch (kind) {
				case Damage:
				case Exhaust:
					ch.Say("Aaaach to je uleva");
					break;
				case Bleed:
					BleedingEffectPluginDef.StopBleeding(ch);
					break;
				case Poisoned:
					Poison.RemoveAll(ch);
					break;
			}
		}

		internal static bool Has(Character ch, int kind) {
			return (ch.GetTimer(VoodooEffectTimer.Key) is VoodooEffectTimer timer) && (timer.kind == kind);
		}

		/// <summary>e_warcry_effect while cursed with it: "Jsi v soku, nemuzes kouzlit!"; e_noport: no recall.</summary>
		internal static bool RefusesCast(Character ch, SpellDef spell) {
			if (Has(ch, Shock)) {
				ch.SysMessage("Jsi v soku, nemuzes kouzlit!");
				return true;
			}
			if (Has(ch, NoPort) && (spell.Id == 32)) {
				ch.SysMessage("Nelze cestovat kouzlem.");
				return true;
			}
			return false;
		}

		/// <summary>e_noport's i_travelstone_delay.</summary>
		internal static bool RefusesRide(Character ch) {
			if (Has(ch, NoPort)) {
				ch.SysMessage("Dostavnik te neprijal.");
				return true;
			}
			return false;
		}

		/// <summary>e_nohorse's @step.</summary>
		internal static bool ThrowsRider(Character ch) {
			return Has(ch, NoHorse) && !ch.IsGM;
		}
	}

	/// <summary>#1070 - [Function astralmorph]: the shaman takes the body of a monster's corpse, its colour too, and wears
	/// nothing meanwhile (e_morphed); .astralmorph again gives the own body back (dismorph). No archive monster has the
	/// m_vit/m_str/m_dex shelter that would move stats, so none move, and Erebor says so. Not on Erebor's command lists,
	/// so it stays a GM command here too.</summary>
	public static class AstralMorph {
		private static readonly TagKey bodyTK = TagKey.Acquire("morphedbody");
		private static readonly TagKey colorTK = TagKey.Acquire("morphedcolor");

		internal static bool IsMorphed(Character ch) {
			return ch.HasTag(bodyTK);
		}

		[SteamFunction("AstralMorph")]
		public static void Command(Player self) {
			if (!Professions.HasProfession(self, Professions.Shaman)) {
				self.SysMessage("Toto muze pouzit pouze Shaman.");
			} else if (IsMorphed(self)) {
				Dismorph(self);
			} else {
				self.Target(SingletonScript<Targ_AstralMorph>.Instance);
			}
		}

		internal static void Morph(Player self, Corpse corpse) {
			self.SetTag(bodyTK, (int) self.Model);
			self.SetTag(colorTK, (int) self.Color);
			self.SysMessage("Tato prisera neni urcena k morfovani (pro alfatest je pripustne morfnout se ovsem bez zmeny statu)");
			self.Model = corpse.CharDef.Model;
			self.Color = corpse.Color;
			self.Dismount();
			foreach (Thing worn in new System.Collections.Generic.List<Thing>(self.VisibleEquip)) {
				if ((worn is Item item) && (item.Z < (int) LayerNames.Mount) && (item != self.Backpack)) {
					item.Cont = self.Backpack;
				}
			}
		}

		internal static void Dismorph(Character self) {
			if (self.GetTag(bodyTK) is int body) {
				self.Model = body;
			}
			if (self.GetTag(colorTK) is int color) {
				self.Color = color;
			}
			self.RemoveTag(bodyTK);
			self.RemoveTag(colorTK);
		}

		/// <summary>e_morphed's @itemEquip: nothing under layer 25 is worn.</summary>
		internal static bool RefusesEquip(Item item, Character ch) {
			return IsMorphed(ch) && (item.Layer < 25);
		}
	}

	public class Targ_AstralMorph : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Do ceho se chces morphnout?");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			if (targetted is Corpse corpse) {
				if ((corpse.Owner is Player) || (corpse.CharDef == null)) {
					self.SysMessage("Toto neni telo monstra!");
					return TargetResult.RestartTargetting;
				}
				AstralMorph.Morph(self, corpse);
				return TargetResult.Done;
			}
			self.SysMessage("Toto neni telo!");
			return TargetResult.RestartTargetting;
		}

		protected override TargetResult On_TargonGround(Player self, IPoint3D targetted, object parameter) {
			self.SysMessage("Toto neni telo!");
			return TargetResult.RestartTargetting;
		}
	}

	/// <summary>i_mem_woodoo: the voodoo on its way to its owner.</summary>
	[SaveableClass, DeepCopyableClass]
	public class VoodooHandleTimer : BoundTimer {
		[SaveableData, CopyableData]
		public Player shaman;

		[SaveableData, CopyableData]
		public int kind;

		[DeepCopyImplementation, LoadingInitializer]
		public VoodooHandleTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is Character victim) {
				ShamanVoodoo.Arrive(victim, this.shaman, this.kind);
			}
		}
	}

	/// <summary>i_woodooeffect: the curse or blessing on someone. A curse comes back every 30 s, "Jsi pod prokletim
	/// voodoo.", until its owner is blessed (the Bless spell) or dead; a blessing is given once.</summary>
	[SaveableClass, DeepCopyableClass]
	public class VoodooEffectTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_woodooEffect_");
		private const double Period = 30;

		[SaveableData, CopyableData]
		public int kind;

		[SaveableData, CopyableData]
		public Character shaman;

		[DeepCopyImplementation, LoadingInitializer]
		public VoodooEffectTimer() {
		}

		internal static void Apply(Character ch, int kind, Character shaman) {
			if (ch.GetTimer(Key) is VoodooEffectTimer old) {
				ShamanVoodoo.Off(ch, old.kind);
				old.Delete();
			}
			VoodooEffectTimer timer = new VoodooEffectTimer();
			timer.kind = kind;
			timer.shaman = shaman;
			ch.AddTimer(Key, timer);
			timer.DueInSeconds = Period;
			timer.PeriodInSeconds = Period;
			ShamanVoodoo.On(ch, kind, shaman);
		}

		protected override void OnTimeout(TagHolder cont) {
			if (!(cont is Character ch)) {
				return;
			}
			if ((this.kind > 10) || ch.Flag_Dead || IsBlessed(ch)) {
				ch.SysMessage("voodoo pominulo");
				ShamanVoodoo.Off(ch, this.kind);
				this.Delete();
				return;
			}
			ch.SysMessage("Jsi pod prokletim voodoo.");
			ShamanVoodoo.On(ch, this.kind, this.shaman);
		}

		private static bool IsBlessed(Character ch) {
			foreach (Plugin plugin in ch.GetAllPlugins()) {
				if (plugin is BlessSpellEffectPlugin) {
					return true;
				}
			}
			return false;
		}
	}

	/// <summary>#1070 - i_flesh_head, i_flesh_head_2: double click, then a potion on it for the shaman's good voodoo.</summary>
	public class t_flesh_head : CompiledTriggerGroup {
		public TriggerResult On_Dclick(Item self, Character dclicker) {
			if (dclicker is Player player) {
				player.Target(SingletonScript<Targ_VoodooHead>.Instance, self);
			}
			return TriggerResult.Cancel;
		}
	}

	public class Targ_VoodooHead : CompiledTargetDef {
		protected override TargetResult On_TargonThing(Player self, Thing targetted, object parameter) {
			if ((parameter is Item head) && !head.IsDeleted) {
				ShamanVoodoo.Bless(self, head, targetted as Item);
			}
			return TargetResult.Done;
		}
	}
}
