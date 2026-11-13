using System;
using SteamEngine.Common;
using SteamEngine.Communication.TCP;
using SteamEngine.CompiledScripts.Dialogs;
using SteamEngine.Networking;
using SteamEngine.Persistence;
using SteamEngine.Regions;
using SteamEngine.Timers;

namespace SteamEngine.CompiledScripts {

	[ViewableClass]
	public partial class ShamanItemDef {
	}

	/// <summary>#1017 - the shaman's things: i_soul_case, i_soul, i_empathy_item, i_empathy_bottle.</summary>
	[ViewableClass]
	public partial class ShamanItem {
		public override void On_DClick(AbstractCharacter from) {
			Player player = from as Player;
			if (player == null) {
				return;
			}
			switch (this.Def.PrettyDefname) {
				case "i_soul_case":
					if (ShamanAbilities.IsShaman(player)) {
						ShamanPacts.LifePact(player);
					} else {
						player.SysMessage("Nenapada te spusob jak to pouzit.");
					}
					break;
				case "i_empathy_item":
					player.SysMessage("Toto je mocny krystal videni.");
					player.Target(SingletonScript<Targ_Empathy>.Instance, this);
					break;
				case "i_empathy_bottle":
					if (EmpathyPlugin.Of(player) != null) {
						player.SysMessage("Nelze pouzit.", ClassAbilities.ClassMessageHue);
					} else {
						ShamanPacts.DrinkEmpathy(player, this);
					}
					break;
				default:
					player.SysMessage("Nenapada te spusob jak to pouzit.");
					break;
			}
		}
	}

	/// <summary>The shaman's pacts (blood, mana, stamina), the life pact and empathy, from specabilities.scp.
	/// A pact's giver and receiver are each one's PactPlugin since #1445. Erebor's mana and stamina pacts end at half their time, as
	/// its bonus counts 5 s ticks against tenths; kept. Where Erebor's script was broken a pact ends on death and
	/// shared blood can kill.</summary>
	public static class ShamanPacts {

		internal static string NameOf(string type) {
			switch (type) {
				case "blood":
					return "Pakt krve";
				case "mana":
					return "Pakt many";
				case "stamina":
					return "Pakt staminy";
				default:
					return "Unknown";
			}
		}

		/// <summary>f_pact_can_do.</summary>
		private static bool CanPact(Player self) {
			ShamanProfessionPlugin shaman = ShamanProfessionPlugin.Of(self); //#1442 - the cooldown is its
			if (shaman == null) {
				self.RedMessage("Tohle nemuzes pouzivat");
			} else if (self.Level < 20) {
				self.SysMessage("Na to jsi jeste prilis " + (self.IsFemale ? "nezkusena" : "nezkuseny") + ".", ClassAbilities.ClassMessageHue);
			} else if (ClassAbilities.Points(self, "woodoo_pacts") <= 0) {
				self.SysMessage("Potrebujes body v abilite Voodoo - pakty ", ClassAbilities.ClassMessageHue);
			} else if (shaman.nextPact > Globals.TimeInSeconds) {
				self.SysMessage("Muzes pouzit az za " + (int) (shaman.nextPact - Globals.TimeInSeconds) + " sekund.", ClassAbilities.ClassMessageHue);
			} else if ((PactPlugin.Of(self) != null) && PactPlugin.Of(self).Gives) {
				self.SysMessage("Uz pouzivas " + NameOf(PactPlugin.Of(self).givenType), ClassAbilities.ClassMessageHue);
			} else {
				return true;
			}
			return false;
		}

		[SteamFunction]
		public static void BloodPact(Player self) {
			if (!CanPact(self)) {
				return;
			}
			if (self.Hits < 31) {
				self.SysMessage("Pouziti v tomto stave by te zabilo.", ClassAbilities.ClassMessageHue);
				return;
			}
			self.Target(SingletonScript<Targ_Pact>.Instance, "blood");
		}

		[SteamFunction]
		public static void ManaPact(Player self) {
			RubyPact(self, "mana");
		}

		[SteamFunction]
		public static void StaminaPact(Player self) {
			RubyPact(self, "stamina");
		}

		private static void RubyPact(Player self, string type) {
			if (!CanPact(self)) {
				return;
			}
			if (ShamanAbilities.FindInPack(self, ShamanAbilities.RubyDef) == null) {
				self.SysMessage("K aktivaci paktu potrebujes rubin.", ClassAbilities.ClassMessageHue);
				return;
			}
			self.Target(SingletonScript<Targ_Pact>.Instance, type);
		}

		/// <summary>i_pact_target's @targon_char.</summary>
		internal static void Give(Player shaman, Character target, string type) {
			if (!target.IsPlayer) {
				shaman.SysMessage("Zameruj hrace.");
				return;
			}
			if ((target == shaman) && (type != "mana")) {
				shaman.RedMessage("Nezameruj sebe");
				return;
			}
			if (target.IsGM) {
				shaman.RedMessage("Nezameruj GM");
				return;
			}
			if (target.Realm != shaman.Realm) {
				shaman.SysMessage("Zameruj hrace svyho realmu!");
				return;
			}
			int pacts = ClassAbilities.Points(shaman, "woodoo_pacts");
			PactPlugin bearer = PactPlugin.Ensure(target);
			bearer.borneType = type;
			bearer.giver = shaman;
			PactPlugin giving = PactPlugin.Ensure(shaman);
			giving.givenType = type;
			giving.receiver = target;
			bearer.bloodOff = false;
			giving.bloodOff = false;
			shaman.SysMessage((shaman.IsFemale ? "Dala" : "Dal") + " jsi " + NameOf(type) + " " + target.Name, ClassAbilities.ClassMessageHue);
			if (target != shaman) {
				target.SysMessage((target.IsFemale ? "Dostala" : "Dostal") + " jsi " + NameOf(type) + " od " + shaman.Name, ClassAbilities.ClassMessageHue);
			}
			if (type == "blood") {
				shaman.Hits = (short) (shaman.Hits - 29);
			} else {
				ShamanAbilities.ConsumeOne(shaman, ShamanAbilities.RubyDef);
			}
			ShamanProfessionPlugin cooldowns = ShamanProfessionPlugin.Of(shaman);
			if (cooldowns != null) {
				cooldowns.nextPact = Globals.TimeInSeconds + ClassAbilitySettings.instance.pactAgainSeconds;
			}
			EffectFactory.StationaryEffect(shaman, 0x376a, 9, 20);
			EffectFactory.StationaryEffect(target, 0x376a, 9, 20);
			shaman.Sound(583);
			int seconds = (18 * pacts) + (6 * ClassAbilities.Points(shaman, "woodoo_pacts_boost"));
			bearer.regen = (type == "blood") ? 0 : pacts;
			bearer.ticksLeft = seconds / 10;
			bearer.endsAt = Globals.TimeInSeconds + seconds;
			bearer.Timer = PactPlugin.TickSeconds;
		}

		/// <summary>f_cancelpact_by, from the receiver's side.</summary>
		internal static void CancelBy(Character receiver) {
			PactPlugin bearer = PactPlugin.Of(receiver);
			if (bearer == null) {
				return;
			}
			Character shaman = bearer.giver;
			string name = NameOf(bearer.borneType);
			receiver.SysMessage(name + " zrusen", ClassAbilities.ClassMessageHue);
			if ((shaman != null) && (shaman != receiver)) {
				shaman.SysMessage(name + " zrusen", ClassAbilities.ClassMessageHue);
			}
			PactPlugin giving = PactPlugin.Of(shaman);
			if (giving != null) {
				giving.StopGiving(); //on the receiver itself for an own mana pact: it still bears, so it stays for the next line
			}
			bearer.StopBearing();
		}

		/// <summary>f_cancelpact_to, from the shaman's side.</summary>
		internal static void CancelTo(Character shaman) {
			PactPlugin giving = PactPlugin.Of(shaman);
			if (giving == null) {
				return;
			}
			Character receiver = giving.receiver;
			PactPlugin bearer = PactPlugin.Of(receiver);
			if ((bearer != null) && (bearer.giver == shaman)) {
				CancelBy(receiver);
			} else {
				shaman.SysMessage(NameOf(giving.givenType) + " zrusen", ClassAbilities.ClassMessageHue);
				giving.StopGiving();
			}
		}

		[SteamFunction]
		public static void CancelPact(Character self) {
			PactPlugin pact = PactPlugin.Of(self);
			if ((pact != null) && pact.Gives) {
				CancelTo(self);
			} else if ((pact != null) && pact.Bears) {
				CancelBy(self);
			} else {
				self.SysMessage("Neexistuje ziaden pakt.", ClassAbilities.ClassMessageHue);
			}
		}

		[SteamFunction]
		public static void CancelBloodPact(Character self) {
			CancelOfType(self, "blood");
		}

		[SteamFunction]
		public static void CancelManaPact(Character self) {
			CancelOfType(self, "mana");
		}

		[SteamFunction]
		public static void CancelStaminaPact(Character self) {
			CancelOfType(self, "stamina");
		}

		private static void CancelOfType(Character self, string type) {
			PactPlugin pact = PactPlugin.Of(self);
			string own = (pact == null) ? null : pact.OwnType;
			if (own == type) {
				CancelPact(self);
			} else {
				self.RedMessage("Pouzivas " + NameOf(own) + " a ne " + NameOf(type));
			}
		}

		/// <summary>@death and @Logout: a pact ends with either side gone. The PactPlugin's own @death and @logout call it
		/// since #1445: the borne pact first, otherwise the given one.</summary>
		internal static void OnGone(Character self) {
			PactPlugin pact = PactPlugin.Of(self);
			if ((pact != null) && pact.Bears) {
				CancelBy(self);
			} else if ((pact != null) && pact.Gives) {
				CancelTo(self);
			}
		}

		/// <summary>e_bloodpact's @gethit: 15% of a monster's blow, at most 20, moves from the one hit to the other.
		/// A player's blow switches the sharing off on that side.</summary>
		internal static void ShareBlood(Character attacker, Character hit, int damage) {
			PactPlugin pact = PactPlugin.Of(hit);
			bool bearsBlood = (pact != null) && (pact.borneType == "blood"), givesBlood = (pact != null) && (pact.givenType == "blood");
			if ((!bearsBlood && !givesBlood) || pact.bloodOff || (damage <= 0)) {
				return;
			}
			if (attacker.IsPlayer) {
				pact.bloodOff = true;
				return;
			}
			Character other = bearsBlood ? pact.giver : pact.receiver; //the blood pact's other side, whatever else it holds
			if ((other == null) || (other == hit) || other.Flag_Dead) {
				return;
			}
			int share = Math.Min((damage * ClassAbilitySettings.instance.bloodPactPercent) / 100, 20);
			hit.Hits = (short) Math.Min(hit.MaxHits, hit.Hits + share);
			if (other.Hits - share <= 0) {
				Character.SetHitsBy(other, 0, attacker); //#1464 - one death, by the attacker
			} else {
				other.Hits = (short) (other.Hits - share);
			}
		}

		/// <summary>sphereevents.scp's kill credit: a shaman who gave a blood pact earns nothing himself...</summary>
		internal static bool EarnsNoKillExp(Player fighter) {
			if (GivesBloodPact(fighter)) {
				fighter.SysMessage("Zkusenosti ziskas od nositele paktu.");
				return true;
			}
			return false;
		}

		/// <summary>EarnsNoKillExp without its message (#1416: a kill worth nothing still credits no giver).</summary>
		internal static bool GivesBloodPact(Player fighter) {
			PactPlugin pact = PactPlugin.Of(fighter);
			return (pact != null) && (pact.givenType == "blood") && (pact.receiver != null) && !InCity(fighter);
		}

		/// <summary>...and earns what the one bearing it earns. The shaman paid (for #1416's @killCredit), or null.</summary>
		internal static Player ShareKillExp(Player fighter, int share) {
			PactPlugin pact = PactPlugin.Of(fighter);
			Player shaman = (pact == null) ? null : pact.giver as Player;
			if ((shaman != null) && (pact.borneType == "blood") && !InCity(shaman)) {
				shaman.SysMessage("Ziskavas zkusenosti za aplikovany pakt na " + fighter.Name + ".");
				LevelingSystem.AddExperience(shaman, share);
				return shaman;
			}
			return null;
		}

		private static bool InCity(Character ch) {
			FlaggedRegion area = RegionEventSystem.AreaOf(ch.Region);
			return (area != null) && !string.IsNullOrEmpty(area.City);
		}

		[SteamFunction]
		public static void Pact(Player self) {
			if (!ShamanAbilities.IsShaman(self)) {
				self.RedMessage("Tohle nemuzes pouzivat");
			} else {
				self.Dialog(self, SingletonScript<D_Pacts>.Instance, new DialogArgs());
			}
		}

		/// <summary>[function lifepact]: alive, a shaman gives it; dead, whoever holds a soul comes back.</summary>
		[SteamFunction]
		public static void LifePact(Player self) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			if (self.Flag_Dead) {
				Rise(self);
				return;
			}
			int lifepact = ClassAbilities.Points(self, "woodoo_lifepact");
			ShamanProfessionPlugin shaman = ShamanProfessionPlugin.Of(self); //#1442 - the cooldown is its
			double next = (shaman == null) ? 0 : shaman.nextLifePact;
			if (shaman == null) {
				self.SysMessage("Nic se nestalo.");
			} else if (PlayerHunger.IsHungry(self)) {
				self.RedMessage("Ve tvem soustredeni te rusi hlad...");
			} else if (ShamanAbilities.FindInPack(self, "i_soul_case") == null) {
				self.SysMessage("Nemas zadnou schranku na duse.");
			} else if (next > Globals.TimeInSeconds) {
				int left = (int) (next - Globals.TimeInSeconds);
				self.SysMessage("Muzes pouzit az za " + (left / 60) + "  minut, " + (left % 60) + " sekund.", ClassAbilities.ClassMessageHue);
			} else if ((self.Stam < 60 - lifepact) || (self.Mana < 70 - lifepact)) {
				self.SysMessage("Nemas dost sil.", ClassAbilities.ClassMessageHue);
			} else if (Globals.dice.Next(1, 11) <= ClassAbilities.Points(self, "woodoo_pacts")) { //Erebor rolls the pacts' points
				self.Target(SingletonScript<Targ_LifePact>.Instance);
				self.SysMessage("Zamer hrace kteremu chces darovat LifePact.");
			} else {
				self.SysMessage("Nepovedlo se.");
				shaman.nextLifePact = Globals.TimeInSeconds + settings.lifePactFailSeconds;
			}
		}

		internal static void GiveLife(Player shaman, Character target) {
			ClassAbilitySettings settings = ClassAbilitySettings.instance;
			if (!target.IsPlayer) {
				shaman.SysMessage("Zameruj hrace.");
			} else if (target.Realm != shaman.Realm) {
				shaman.SysMessage("Zameruj hrace svyho realmu!");
			} else if (target.Flag_Dead) {
				shaman.SysMessage("Nezameruj mrtvoly.");
			} else if (ShamanAbilities.FindInPack(target, "i_soul") != null) {
				shaman.SysMessage(target.Name + " jiz ma Pakt zivota.");
			} else if (!ShamanAbilities.ConsumeOne(shaman, "i_soul_case")) {
				shaman.SysMessage("Nemas schranku.");
			} else {
				int lifepact = ClassAbilities.Points(shaman, "woodoo_lifepact");
				shaman.SysMessage("Daroval jsi Pakt zivota " + target.Name, ClassAbilities.ClassMessageHue);
				target.SysMessage(shaman.Name + " ti daroval Pakt zivota.", ClassAbilities.ClassMessageHue);
				shaman.Stam = (short) (shaman.Stam - (60 - lifepact));
				shaman.Mana = (short) (shaman.Mana - (70 - lifepact));
				ShamanProfessionPlugin cooldowns = ShamanProfessionPlugin.Of(shaman);
				if (cooldowns != null) {
					cooldowns.nextLifePact = Globals.TimeInSeconds + settings.lifePactAgainSeconds;
				}
				EffectFactory.StationaryEffect(shaman, 0x376a, 9, 20);
				EffectFactory.StationaryEffect(target, 0x376a, 9, 20);
				shaman.Sound(583);
				int seconds = 1200 + (60 * lifepact);
				NewSoul(target.Backpack, seconds);
				LifePactPlugin old = LifePactPlugin.Of(target);
				if (old != null) {
					old.Delete();
				}
				LifePactPlugin pact = (LifePactPlugin) LifePactPluginDef.instance.Create(); //#1447 - i_soul_remover
				pact.giver = shaman;
				pact.createdAt = Globals.TimeInSeconds;
				target.AddPlugin(LifePactPlugin.Key, pact);
				pact.Timer = seconds;
			}
		}

		/// <summary>#1448 - i_soul, with its own end: i_soul_remover's @Equip sets the soul's timer to the pact's, and the soul's
		/// @timer removes it wherever it is. attr=010 is Sphere's hex 0x10, attr_move_never: its holder can't take it out of
		/// the pack, so it can't be dropped or passed on; newbie too, as our corpse keeps only those (Sphere's kept both).</summary>
		internal static Item NewSoul(Thing cont, double seconds) {
			Item soul = (Item) ((ItemDef) ThingDef.GetByDefname("i_soul")).Create(cont);
			soul.SetAttribute(ItemAttributes.attr_move_never, true);
			soul.SetAttribute(ItemAttributes.attr_newbie, true);
			soul.AddTimer(SoulEndTimer.Key, new SoulEndTimer()).DueInSeconds = seconds;
			return soul;
		}

		/// <summary>i_soul_remover's end: the soul goes from its holder.</summary>
		internal static void RemoveSoul(Character self) {
			Item soul = ShamanAbilities.FindInPack(self, "i_soul");
			if (soul != null) {
				soul.Delete();
			}
		}

		/// <summary>The dead branch: 20 s after the pact was given and 5 s after dying (at once where the area resurrects
		/// at once, region_cf_instantRes), back with 10% of everything, plus 9% a point of the giver's lifepact_stats.</summary>
		private static void Rise(Player self) {
			Item soul = ShamanAbilities.FindInPack(self, "i_soul");
			LifePactPlugin pact = LifePactPlugin.Of(self);
			if (soul == null) {
				self.SysMessage("Nemas u sebe dusi.");
				return;
			}
			if ((pact != null) && (Globals.TimeInSeconds - pact.createdAt < 20)) {
				self.SysMessage("Tva duse je jeste prilis neklidna");
				return;
			}
			FlaggedRegion area = RegionEventSystem.AreaOf(self.Region);
			if ((Globals.TimeInSeconds - PriestAbilities.DiedAt(self) < 5) && !((area != null) && area.HasCustomFlag(FlaggedRegion.InstantRes))) {
				self.SysMessage("Na oziveni jeste nejsi " + (self.IsFemale ? "pripravena" : "pripraven") + ".");
				return;
			}
			Character shaman = ((pact == null) || (pact.giver == null) || pact.giver.IsDeleted) ? null : pact.giver; //a gone giver reads 0, as Erebor's dead uid
			int stats = (shaman == null) ? 0 : ClassAbilities.Points(shaman, "woodoo_lifepact_stats");
			self.Resurrect(null);
			self.Hits = (short) Math.Max(1, (self.MaxHits * (10 + (9 * stats))) / 100);
			self.Mana = (short) ((self.MaxMana * (10 + (9 * stats))) / 100);
			self.Stam = (short) ((self.MaxStam * (10 + (9 * stats))) / 100);
			soul.Delete();
			if (pact != null) {
				pact.Delete();
			}
		}

		/// <summary>i_getphysresists: 8 s of reading a monster's physical resists by Arms Lore, on EmpathyPlugin (#1447).</summary>
		internal static void DrinkEmpathy(Player self, ShamanItem bottle) {
			if (bottle.Amount > 1) {
				bottle.Amount--;
			} else {
				bottle.Delete();
			}
			EmpathyPlugin empathy = (EmpathyPlugin) EmpathyPluginDef.instance.Create();
			empathy.Init(self, EffectFlag.BeneficialEffect | EffectFlag.FromPotion, 0, TimeSpan.FromSeconds(EmpathyPlugin.Seconds));
			self.AddPlugin(EmpathyPlugin.Key, empathy);
			self.SysMessage("Nyni jsi " + (self.IsFemale ? "schopna" : "schopen") + " kratkou chvili rozpoznat rezisty.");
			EffectFactory.StationaryEffect(self, 0x3779, 6, 20); //i_fx_sparkle_2
		}

		/// <summary>e_getphysresists' @AfterSwing, from EmpathyPlugin while it lasts.</summary>
		internal static void ReadPhysical(Player self, Character target) {
			if (target.IsPlayer || (self.GetSkill(SkillName.ArmsLore) <= Globals.dice.Next(10, 1201))) {
				return;
			}
			Tell(self, target, ResistText(target.ResistSlashing, "secnymi zbranemi", "secnym zbranim"));
			Tell(self, target, ResistText(target.ResistStabbing, "pruraznymi zbranemi", "pruraznym zbranim"));
			Tell(self, target, ResistText(target.ResistSharp, "ostrymi zbranemi", "ostrym zbranim"));
			Tell(self, target, ResistText(target.ResistBlunt, "drtivymi zbranemi", "drtivym zbranim"));
			if (Professions.HasProfession(self, Professions.Thief) || Professions.HasProfession(self, Professions.Ranger)) {
				Tell(self, target, BleedText(target.ResistBleed));
				Tell(self, target, PoisonText(target.ResistPoison));
			}
		}

		/// <summary>The crystal: Evaluating Intelligence against 1.0 to 140.0 reads four magic resists.</summary>
		internal static void ReadMagic(Player self, Character target, Item crystal) {
			if ((crystal == null) || crystal.IsDeleted) {
				self.SysMessage("Nemas u sebe potrebny krystal.", ClassAbilities.ClassMessageHue);
				return;
			}
			if (target.IsPlayer) {
				self.SysMessage("Lze pouzit pouze na monstra.", ClassAbilities.ClassMessageHue);
				return;
			}
			crystal.Delete();
			if (self.GetSkill(SkillName.EvalInt) < Globals.dice.Next(10, 1401)) {
				self.SysMessage("Nepovedlo se.", ClassAbilities.ClassMessageHue);
				return;
			}
			Tell(self, target, ResistText(target.ResistElectric, "elektrinou", null));
			Tell(self, target, ResistText(target.ResistFire, "ohnem", null));
			Tell(self, target, ResistText(target.ResistCold, "chladem", null));
			Tell(self, target, ParalyseText(target.ResistParalyse));
		}

		/// <summary>Erebor glues the name to the text; here there's a space.</summary>
		private static void Tell(Player self, Character target, string text) {
			self.SysMessage(target.Name + " " + text, ClassAbilities.ClassMessageHue);
		}

		/// <summary>showresists*: a value of exactly 999 says nothing on Erebor; here it counts as practically.</summary>
		internal static string ResistText(int value, string with, string weaponForm) {
			if (value < -400) {
				return "je smrtelne nachylny na utok " + with + ".";
			} else if (value < 0) {
				return "je extremne nachylny na utok " + with + ".";
			} else if (value < 250) {
				return "je nachylny na utok " + with + ".";
			} else if (value < 500) {
				return "je castecne odolny proti utokum " + with + ".";
			} else if (value < 700) {
				return "je odolny proti utokum " + with + ".";
			} else if (value < 900) {
				return (weaponForm != null) ? "je extremne odolny proti " + weaponForm + "." : "je extremne odolny proti utokum " + with + ".";
			} else if (value < 1000) {
				return "je prakticky nezranitelny " + with + ".";
			}
			return "je nezranitelny " + with + ".";
		}

		private static string ParalyseText(int value) {
			string[] texts = { "je obrovsky nachylny na paralyzu.", "je extremne nachylny na paralyzu.", "je nachylny na paralyzu.",
				"je castecne odolny proti paralyze.", "je odolny proti paralyze.", "je extremne odolny proti paralyze.",
				"je prakticky imunni na paralyzu.", "je imunni na paralyzu." };
			return texts[Step(value, -400, 0, 250, 500, 700, 900, 1000)];
		}

		private static string BleedText(int value) {
			string[] texts = { "je smrtelne nachylny na krvaceni.", "je extremne nachylny na krvaceni.", "je nachylny na krvaceni.",
				"je castecne odolny proti krvaceni.", "je odolny proti krvaceni.", "je extremne odolny proti krvaceni.",
				"je prakticky nezranitelny krvacenim.", "je nezranitelny krvacenim." };
			return texts[Step(value, -400, 0, 250, 500, 700, 900, 1000)];
		}

		private static string PoisonText(int value) {
			string[] texts = { "je smrtelne nachylny na jed.", "je extremne nachylny na jed.", "je nachylny na jed.",
				"je castecne odolny proti jedu.", "je odolny proti jedu.", "je extremne odolny proti jedu.",
				"je prakticky nezranitelny jedem.", "je nezranitelny jedem." };
			return texts[Step(value, -500, -250, 250, 500, 800, 999, 1000)];
		}

		private static int Step(int value, params int[] below) {
			for (int i = 0; i < below.Length; i++) {
				if (value < below[i]) {
					return i;
				}
			}
			return below.Length;
		}
	}

	public class Targ_Pact : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Zamer spolubojovnika kteremu chces dat " + ShamanPacts.NameOf((string) parameter));
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			ShamanPacts.Give(self, targetted, (string) parameter);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.RedMessage("Zameruj hrace.");
			return TargetResult.Done;
		}
	}

	public class Targ_LifePact : CompiledTargetDef {
		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			ShamanPacts.GiveLife(self, targetted);
			return TargetResult.Done;
		}

		protected override TargetResult On_TargonItem(Player self, Item targetted, object parameter) {
			self.RedMessage("Zameruj hrace.");
			return TargetResult.Done;
		}
	}

	public class Targ_Empathy : CompiledTargetDef {
		protected override void On_Start(Player self, object parameter) {
			self.SysMessage("Zamer monstrum u ktereho chces zjistit rezisty.");
			base.On_Start(self, parameter);
		}

		protected override TargetResult On_TargonChar(Player self, Character targetted, object parameter) {
			ShamanPacts.ReadMagic(self, targetted, parameter as Item);
			return TargetResult.Done;
		}
	}

	/// <summary>#1448 - i_soul's @timer: remove, wherever it is.</summary>
	[SaveableClass, DeepCopyableClass]
	public class SoulEndTimer : BoundTimer {
		internal static readonly TimerKey Key = TimerKey.Acquire("_soulEnd_");

		[LoadingInitializer, DeepCopyImplementation]
		public SoulEndTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if ((cont is Item soul) && !soul.IsDeleted) {
				soul.Delete();
			}
		}
	}
}

namespace SteamEngine.CompiledScripts.Dialogs {

	/// <summary>d_pacts.</summary>
	public class D_Pacts : CompiledGumpDef {
		private static readonly string[] rows = { "Pakt krve", "Pakt zivota", "Pakt many", "Pakt staminy" };
		private static readonly string[] hints = { ".bloodpact", ".lifepact", ".manapact", ".staminapact" };

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player player = (Player) sendTo;
			this.SetLocation(250, 405);
			this.AddPage(0);
			this.AddBackground(0, 0, 314, 146, 2620);
			this.AddLabel(100, 12, 42, "Pakty");
			this.AddButton(276, 9, 0xfb1, 0xfb3, 0, GumpButtonType.Reply, 0);
			if (ClassAbilities.Points(player, "woodoo_pacts") <= 0) {
				this.AddLabel(20, 40, 2301, "Nevis, jak to pouzit.");
				return;
			}
			for (int i = 0; i < rows.Length; i++) {
				int y = 40 + (i * 22);
				this.AddButton(20, y, 4020, 4022, i + 1, GumpButtonType.Reply, 0);
				this.AddLabel(55, y, 2301, rows[i]);
				this.AddLabel(210, y, 945, hints[i]);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = (Player) gi.Cont;
			switch (gr.PressedButton) {
				case 1:
					ShamanPacts.BloodPact(player);
					break;
				case 2:
					ShamanPacts.LifePact(player);
					break;
				case 3:
					ShamanPacts.ManaPact(player);
					break;
				case 4:
					ShamanPacts.StaminaPact(player);
					break;
			}
		}
	}
}
