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
			switch (this.Def.Defname) {
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
					if (player.HasTimer(ShamanPacts.EmpathyTK)) {
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
	/// A pact's giver and receiver are tags on both. Erebor's mana and stamina pacts end at half their time, as
	/// its bonus counts 5 s ticks against tenths; kept. Where Erebor's script was broken a pact ends on death and
	/// shared blood can kill.</summary>
	public static class ShamanPacts {
		internal static readonly TagKey TypeTK = TagKey.Acquire("_pactType_");
		internal static readonly TagKey ByTK = TagKey.Acquire("_pactBy_");
		internal static readonly TagKey ToTK = TagKey.Acquire("_pactTo_");
		private static readonly TagKey nextPactTK = TagKey.Acquire("_nextPact_");
		private static readonly TagKey bloodOffTK = TagKey.Acquire("_bloodPactOff_");
		private static readonly TagKey nextLifePactTK = TagKey.Acquire("_nextLifePact_");
		internal static readonly TagKey LifePactByTK = TagKey.Acquire("_lifePactBy_");
		internal static readonly TimerKey PactTK = TimerKey.Acquire("_pact_");
		internal static readonly TimerKey SoulTK = TimerKey.Acquire("_soulRemover_");
		internal static readonly TimerKey EmpathyTK = TimerKey.Acquire("_getPhysResists_");

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
			if (!ShamanAbilities.IsShaman(self)) {
				self.RedMessage("Tohle nemuzes pouzivat");
			} else if (self.Level < 20) {
				self.SysMessage("Na to jsi jeste prilis " + (self.IsFemale ? "nezkusena" : "nezkuseny") + ".", ClassAbilities.ClassMessageHue);
			} else if (ClassAbilities.Points(self, "woodoo_pacts") <= 0) {
				self.SysMessage("Potrebujes body v abilite Voodoo - pakty ", ClassAbilities.ClassMessageHue);
			} else if (ClassAbilities.SecondsTag(self, nextPactTK) > Globals.TimeInSeconds) {
				self.SysMessage("Muzes pouzit az za " + (int) (ClassAbilities.SecondsTag(self, nextPactTK) - Globals.TimeInSeconds) + " sekund.", ClassAbilities.ClassMessageHue);
			} else if (self.GetTag(ToTK) != null) {
				self.SysMessage("Uz pouzivas " + NameOf(self.GetTag(TypeTK) as string), ClassAbilities.ClassMessageHue);
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
			target.SetTag(TypeTK, type);
			target.SetTag(ByTK, shaman);
			shaman.SetTag(TypeTK, type);
			shaman.SetTag(ToTK, target);
			target.RemoveTag(bloodOffTK);
			shaman.RemoveTag(bloodOffTK);
			shaman.SysMessage((shaman.IsFemale ? "Dala" : "Dal") + " jsi " + NameOf(type) + " " + target.Name, ClassAbilities.ClassMessageHue);
			if (target != shaman) {
				target.SysMessage((target.IsFemale ? "Dostala" : "Dostal") + " jsi " + NameOf(type) + " od " + shaman.Name, ClassAbilities.ClassMessageHue);
			}
			if (type == "blood") {
				shaman.Hits = (short) (shaman.Hits - 29);
			} else {
				ShamanAbilities.ConsumeOne(shaman, ShamanAbilities.RubyDef);
			}
			shaman.SetTag(nextPactTK, Globals.TimeInSeconds + ClassAbilitySettings.instance.pactAgainSeconds);
			EffectFactory.StationaryEffect(shaman, 0x376a, 9, 20);
			EffectFactory.StationaryEffect(target, 0x376a, 9, 20);
			shaman.Sound(583);
			int seconds = (18 * pacts) + (6 * ClassAbilities.Points(shaman, "woodoo_pacts_boost"));
			PactTimer timer = new PactTimer();
			timer.regen = (type == "blood") ? 0 : pacts;
			timer.ticksLeft = seconds / 10;
			timer.endsAt = Globals.TimeInSeconds + seconds;
			target.DeleteTimer(PactTK);
			target.AddTimer(PactTK, timer);
			timer.DueInSeconds = 5;
			timer.PeriodInSeconds = 5;
		}

		/// <summary>f_cancelpact_by, from the receiver's side.</summary>
		internal static void CancelBy(Character receiver) {
			Character shaman = receiver.GetTag(ByTK) as Character;
			string name = NameOf(receiver.GetTag(TypeTK) as string);
			receiver.SysMessage(name + " zrusen", ClassAbilities.ClassMessageHue);
			if ((shaman != null) && (shaman != receiver)) {
				shaman.SysMessage(name + " zrusen", ClassAbilities.ClassMessageHue);
			}
			if (shaman != null) {
				shaman.RemoveTag(ToTK);
				shaman.RemoveTag(TypeTK);
			}
			receiver.RemoveTag(TypeTK);
			receiver.RemoveTag(ByTK);
			receiver.DeleteTimer(PactTK);
		}

		/// <summary>f_cancelpact_to, from the shaman's side.</summary>
		internal static void CancelTo(Character shaman) {
			Character receiver = shaman.GetTag(ToTK) as Character;
			if ((receiver != null) && (receiver.GetTag(ByTK) == shaman)) {
				CancelBy(receiver);
			} else {
				shaman.SysMessage(NameOf(shaman.GetTag(TypeTK) as string) + " zrusen", ClassAbilities.ClassMessageHue);
				shaman.RemoveTag(ToTK);
				shaman.RemoveTag(TypeTK);
			}
		}

		[SteamFunction]
		public static void CancelPact(Character self) {
			if (self.GetTag(ToTK) != null) {
				CancelTo(self);
			} else if (self.GetTag(ByTK) != null) {
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
			string own = self.GetTag(TypeTK) as string;
			if (own == type) {
				CancelPact(self);
			} else {
				self.RedMessage("Pouzivas " + NameOf(own) + " a ne " + NameOf(type));
			}
		}

		/// <summary>@death and @Logout: a pact ends with either side gone.</summary>
		internal static void OnGone(Character self) {
			if (self.GetTag(ByTK) != null) {
				CancelBy(self);
			} else if (self.GetTag(ToTK) != null) {
				CancelTo(self);
			}
		}

		/// <summary>e_bloodpact's @gethit: 15% of a monster's blow, at most 20, moves from the one hit to the other.
		/// A player's blow switches the sharing off on that side.</summary>
		internal static void ShareBlood(Character attacker, Character hit, int damage) {
			if ((hit.GetTag(TypeTK) as string) != "blood" || (hit.GetTag(bloodOffTK) != null) || (damage <= 0)) {
				return;
			}
			if (attacker.IsPlayer) {
				hit.SetTag(bloodOffTK, 1);
				return;
			}
			Character other = (hit.GetTag(ByTK) as Character) ?? (hit.GetTag(ToTK) as Character);
			if ((other == null) || (other == hit) || other.Flag_Dead) {
				return;
			}
			int share = Math.Min((damage * ClassAbilitySettings.instance.bloodPactPercent) / 100, 20);
			hit.Hits = (short) Math.Min(hit.MaxHits, hit.Hits + share);
			if (other.Hits - share <= 0) {
				other.Hits = 0;
				other.CauseDeath(attacker);
			} else {
				other.Hits = (short) (other.Hits - share);
			}
		}

		/// <summary>sphereevents.scp's kill credit: a shaman who gave a blood pact earns nothing himself...</summary>
		internal static bool EarnsNoKillExp(Player fighter) {
			if ((fighter != null) && ((fighter.GetTag(TypeTK) as string) == "blood") && (fighter.GetTag(ToTK) != null) && !InCity(fighter)) {
				fighter.SysMessage("Zkusenosti ziskas od nositele paktu.");
				return true;
			}
			return false;
		}

		/// <summary>...and earns what the one bearing it earns.</summary>
		internal static void ShareKillExp(Player fighter, int share) {
			Player shaman = (fighter == null) ? null : fighter.GetTag(ByTK) as Player;
			if ((shaman != null) && ((fighter.GetTag(TypeTK) as string) == "blood") && !InCity(shaman)) {
				shaman.SysMessage("Ziskavas zkusenosti za aplikovany pakt na " + fighter.Name + ".");
				LevelingSystem.AddExperience(shaman, share);
			}
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
			double next = ClassAbilities.SecondsTag(self, nextLifePactTK);
			if (!ShamanAbilities.IsShaman(self)) {
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
				self.SetTag(nextLifePactTK, Globals.TimeInSeconds + settings.lifePactFailSeconds);
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
				target.SetTag(LifePactByTK, shaman);
				shaman.SysMessage("Daroval jsi Pakt zivota " + target.Name, ClassAbilities.ClassMessageHue);
				target.SysMessage(shaman.Name + " ti daroval Pakt zivota.", ClassAbilities.ClassMessageHue);
				shaman.Stam = (short) (shaman.Stam - (60 - lifepact));
				shaman.Mana = (short) (shaman.Mana - (70 - lifepact));
				shaman.SetTag(nextLifePactTK, Globals.TimeInSeconds + settings.lifePactAgainSeconds);
				EffectFactory.StationaryEffect(shaman, 0x376a, 9, 20);
				EffectFactory.StationaryEffect(target, 0x376a, 9, 20);
				shaman.Sound(583);
				Item soul = (Item) ((ItemDef) ThingDef.GetByDefname("i_soul")).Create(target.Backpack);
				soul.SetAttribute(ItemAttributes.attr_newbie, true); //attr 010: it stays on its holder through death
				SoulTimer timer = new SoulTimer();
				timer.createdAt = Globals.TimeInSeconds;
				target.DeleteTimer(SoulTK);
				target.AddTimer(SoulTK, timer);
				timer.DueInSeconds = 1200 + (60 * lifepact);
			}
		}

		/// <summary>The dead branch: 20 s after the pact was given and 5 s after dying, back with 10% of everything,
		/// plus 9% a point of the giver's lifepact_stats.</summary>
		private static void Rise(Player self) {
			Item soul = ShamanAbilities.FindInPack(self, "i_soul");
			SoulTimer timer = self.GetTimer(SoulTK) as SoulTimer;
			if (soul == null) {
				self.SysMessage("Nemas u sebe dusi.");
				return;
			}
			if ((timer != null) && (Globals.TimeInSeconds - timer.createdAt < 20)) {
				self.SysMessage("Tva duse je jeste prilis neklidna");
				return;
			}
			if (Globals.TimeInSeconds - PriestAbilities.DiedAt(self) < 5) {
				self.SysMessage("Na oziveni jeste nejsi " + (self.IsFemale ? "pripravena" : "pripraven") + ".");
				return;
			}
			Character shaman = self.GetTag(LifePactByTK) as Character;
			int stats = (shaman == null) ? 0 : ClassAbilities.Points(shaman, "woodoo_lifepact_stats");
			self.Resurrect(null);
			self.Hits = (short) Math.Max(1, (self.MaxHits * (10 + (9 * stats))) / 100);
			self.Mana = (short) ((self.MaxMana * (10 + (9 * stats))) / 100);
			self.Stam = (short) ((self.MaxStam * (10 + (9 * stats))) / 100);
			soul.Delete();
			self.RemoveTag(LifePactByTK);
			self.DeleteTimer(SoulTK);
		}

		/// <summary>i_getphysresists: 8 s of reading a monster's physical resists by Arms Lore.</summary>
		internal static void DrinkEmpathy(Player self, ShamanItem bottle) {
			if (bottle.Amount > 1) {
				bottle.Amount--;
			} else {
				bottle.Delete();
			}
			EffectEndTimer timer = new EffectEndTimer();
			self.AddTimer(EmpathyTK, timer);
			timer.DueInSeconds = 8;
			self.SysMessage("Nyni jsi " + (self.IsFemale ? "schopna" : "schopen") + " kratkou chvili rozpoznat rezisty.");
		}

		/// <summary>e_getphysresists' @AfterSwing.</summary>
		internal static void ReadPhysical(Player self, Character target) {
			if (!self.HasTimer(EmpathyTK) || target.IsPlayer || (self.GetSkill(SkillName.ArmsLore) <= Globals.dice.Next(10, 1201))) {
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

	/// <summary>i_pact and i_pact_bonus: every 5 s the regen, and the end.</summary>
	[SaveableClass, DeepCopyableClass]
	public class PactTimer : BoundTimer {
		[SaveableData, CopyableData]
		public int regen, ticksLeft;

		[SaveableData, CopyableData]
		public double endsAt;

		[DeepCopyImplementation, LoadingInitializer]
		public PactTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			Character self = cont as Character;
			if (self == null) {
				return;
			}
			string type = self.GetTag(ShamanPacts.TypeTK) as string;
			if (type == "blood") {
				if (Globals.TimeInSeconds >= this.endsAt) {
					ShamanPacts.CancelBy(self);
				}
				return;
			}
			this.ticksLeft--;
			if (this.ticksLeft < 1) {
				ShamanPacts.CancelBy(self);
				return;
			}
			if (type == "mana") {
				self.Mana = (short) Math.Min(self.MaxMana, self.Mana + this.regen);
			} else if (type == "stamina") {
				self.Stam = (short) Math.Min(self.MaxStam, self.Stam + this.regen);
			}
		}
	}

	/// <summary>i_soul_remover: the soul goes, and with it the pact.</summary>
	[SaveableClass, DeepCopyableClass]
	public class SoulTimer : BoundTimer {
		[SaveableData, CopyableData]
		public double createdAt;

		[DeepCopyImplementation, LoadingInitializer]
		public SoulTimer() {
		}

		protected override void OnTimeout(TagHolder cont) {
			if (cont is Character self) {
				Item soul = ShamanAbilities.FindInPack(self, "i_soul");
				if (soul != null) {
					soul.Delete();
				}
				self.RemoveTag(ShamanPacts.LifePactByTK);
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
