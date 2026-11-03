using System;
using System.Collections.Generic;

namespace SteamEngine.CompiledScripts {

	/// <summary>#923 - server-side helpers for Tests/Protocol/BoostsTests.cs.</summary>
	public static class BoostsTests {
		private const int Strength = 16, Bless = 17, Curse = 27, Invisibility = 44, Protection = 15;
		private static int fails;
		private static Character plain;

		private sealed class TestSource : IActiveEffectSource {
			public void AddEffects(Character ch, List<ActiveEffect> into) {
				if (ch.HasTag(TagKey.Acquire("_boosts_test_source_"))) {
					into.Add(new ActiveEffect(EffectSection.Positive, "Testovaci zdroj") { Detail = "+1 Odvaha" });
				}
			}
		}

		private static readonly TestSource testSource = new TestSource();

		private static void Check(string name, bool ok, string detail = "") {
			if (!ok) {
				fails++;
				Console.WriteLine("BOOSTS_FAIL: " + name + " " + detail);
			}
		}

		private static ActiveEffect Find(Character ch, string name) {
			return ActiveEffects.Of(ch).Find(effect => effect.Name == name);
		}

		private static void Cast(int spellId, Character caster, Character target) {
			SpellDef.GetById(spellId).EffectChar(caster, target, EffectFlag.FromSpellBook);
		}

		private static string Listed(Character ch) {
			List<string> lines = new List<string>();
			foreach (ActiveEffect effect in ActiveEffects.Of(ch)) {
				lines.Add(effect.Section + ":" + effect.Name + "[" + effect.Detail + "][" + effect.Source + "]");
			}
			return string.Join(" ", lines);
		}

		private static void ClearAll(Character self) {
			foreach (Plugin plugin in new List<Plugin>(self.GetAllPlugins())) {
				if (plugin is EffectDurationPlugin) {
					plugin.Delete();
				}
			}
			Notoriety.ClearCriminal(self);
			self.Flag_Hidden = false;
			self.Flag_InvisByMagic = false;
		}

		[SteamFunction]
		public static void TestBoostsRules(Character self) {
			self.Account.PLevel = 1;
			fails = 0;
			ClearAll(self);
			Check("only food at first", ActiveEffects.Of(self).Count == 1, Listed(self));
			Character caster = (Character) ThingDef.GetByDefname("c_a_jackrabbit").Create(self.P());
			self.SetRealSkillValue((int) SkillName.Magery, 1000);

			//any spell that lasts shows by itself
			Cast(Bless, self, self);
			ActiveEffect bless = Find(self, SpellDef.GetById(Bless).Name);
			Check("Bless shows", bless != null, Listed(self));
			if (bless != null) {
				Check("as positive", bless.Section == EffectSection.Positive);
				Check("with what it does", bless.Detail.StartsWith("+", StringComparison.Ordinal) && bless.Detail.Contains("Str") && bless.Detail.Contains("Int"), bless.Detail);
				Check("with its time", bless.SecondsLeft > 0, bless.SecondsLeft.ToString());
				Check("from a spell, nobody else's", bless.Source == "kouzlo", bless.Source);
				Check("with the spell's icon", bless.Icon == 0x8D0, bless.Icon.ToString());
			}
			SpellDef.GetById(Curse).ApplyEffectChar(caster, self, 1000, EffectFlag.FromSpellBook | EffectFlag.HarmfulEffect); //a rabbit's power wouldn't get through
			ActiveEffect curse = Find(self, SpellDef.GetById(Curse).Name);
			Check("Curse shows", curse != null, Listed(self));
			if (curse != null) {
				Check("as negative", curse.Section == EffectSection.Negative);
				Check("with what it does", curse.Detail.Contains("-") && curse.Detail.Contains("Str"), curse.Detail);
				Check("from whom", curse.Source.Contains("od: " + caster.Name), curse.Source);
			}
			ClearAll(self);

			//every spell that lasts has a name to show
			foreach (SpellDef spell in SpellDef.AllSpellDefs) {
				if (spell is DurableCharEffectSpellDef) {
					try {
						spell.EffectChar(self, self, EffectFlag.FromSpellBook);
						foreach (ActiveEffect effect in ActiveEffects.Of(self)) {
							Check("a readable name for " + spell.Defname, (effect.Name.Length > 0) && !effect.Name.StartsWith("p_", StringComparison.Ordinal) && !effect.Name.Contains("Plugin"), effect.Name);
						}
					} catch (Exception e) {
						Check("describing " + spell.Defname, false, e.Message);
					}
					ClearAll(self);
				}
			}
			self.Resurrect();

			//what's kept outside plugins
			Cast(Invisibility, self, self);
			ActiveEffect invisible = Find(self, "Invisibility");
			Check("invisibility shows with its time", (invisible != null) && (invisible.SecondsLeft > 0), Listed(self));
			ClearAll(self);
			self.Flag_Hidden = true;
			Check("hidden has no end", (Find(self, "Hidden") != null) && (Find(self, "Hidden").SecondsLeft < 0), Listed(self));
			ClearAll(self);
			Notoriety.FlagCriminal(self);
			ActiveEffect criminal = Find(self, "Zlocinec");
			Check("criminal is negative, with its time", (criminal != null) && (criminal.Section == EffectSection.Negative) && (criminal.SecondsLeft > 0), Listed(self));
			ClearAll(self);

			//the mount, in status
			Character horse = (Character) ThingDef.GetByDefname("c_horse_brown_dk").Create(self.P());
			self.Mount = horse;
			ActiveEffect mount = Find(self, "Jezditko");
			Check("the mount is in status", (mount != null) && (mount.Section == EffectSection.Status) && (mount.Detail == horse.Name), Listed(self));
			self.Dismount();
			horse.Delete();

			//cooldowns, only while they run
			AbilityDef ability = (AbilityDef) AbilityDef.GetByDefname("a_longrange");
			self.SetRealAbilityPoints(ability, 1);
			Check("an unused ability has no cooldown", Find(self, ability.Name) == null, Listed(self));
			self.GetAbilityObject(ability).LastUsage = Globals.TimeAsSpan;
			ActiveEffect cooldown = Find(self, ability.Name);
			Check("cooldown shows", (cooldown != null) && (cooldown.Section == EffectSection.Cooldown), Listed(self));
			Check("with the ability's icon from its def", (cooldown != null) && (cooldown.Icon == 0x5209), (cooldown != null) ? cooldown.Icon.ToString() : "");
			Check("counting down", (cooldown != null) && (cooldown.SecondsLeft > ability.Cooldown - 10) && (cooldown.SecondsLeft <= ability.Cooldown), (cooldown != null) ? cooldown.SecondsLeft.ToString() : ""); //a_longrange's is Erebor's 20 s since #1047
			self.GetAbilityObject(ability).LastUsage = Globals.TimeAsSpan - TimeSpan.FromSeconds(1000);
			Check("gone when it ran out", Find(self, ability.Name) == null, Listed(self));
			self.SetRealAbilityPoints(ability, 0);

			//a new kind of thing plugs in, the dialog doesn't change
			TagKey sourceTag = TagKey.Acquire("_boosts_test_source_");
			ActiveEffects.Register(testSource);
			ActiveEffects.Register(testSource);
			self.SetTag(sourceTag, true);
			Check("a registered source shows, once", ActiveEffects.Of(self).FindAll(effect => effect.Name == "Testovaci zdroj").Count == 1, Listed(self));
			self.RemoveTag(sourceTag);

			//times
			Check("seconds", ActiveEffects.FormatTime(9.2) == "10s", ActiveEffects.FormatTime(9.2));
			Check("minutes", ActiveEffects.FormatTime(102) == "1:42", ActiveEffects.FormatTime(102));
			Check("no end", ActiveEffects.FormatTime(-1) == "trvale", ActiveEffects.FormatTime(-1));

			caster.Delete();
			Console.WriteLine("BOOSTS_RULES: fails=" + fails + " end");
		}

		[SteamFunction]
		public static void TestBoostsPlain(Character self) {
			self.Account.PLevel = 1;
			plain = self;
			ClearAll(self);
			self.RemoveTag(TagKey.Acquire("_boosts_live_"));
			self.RemoveTag(TagKey.Acquire("_boosts_compact_"));
			Console.WriteLine("BOOSTS_PLAIN: uid=" + (uint) self.FlaggedUid + " end");
		}

		[SteamFunction]
		public static void TestBoostsBless(Character self) {
			self.SetRealSkillValue((int) SkillName.Magery, 1000);
			Cast(Bless, self, self);
			Console.WriteLine("BOOSTS_BLESSED: done");
		}

		[SteamFunction]
		public static void TestBoostsMoreStrength(Character self) {
			Cast(Strength, self, self);
			Console.WriteLine("BOOSTS_STRONGER: done");
		}

		[SteamFunction]
		public static void TestBoostsLiveDefault(Character self, ScriptArgs sa) {
			BoostsSettings.instance.liveByDefault = sa.Args.Trim().Trim('"') == "1";
			BoostsSettings.instance.refreshSeconds = 1000;
			Console.WriteLine("BOOSTS_DEFAULT: done");
		}

		[SteamFunction]
		public static void TestBoostsState(Character self) {
			Player player = (Player) self;
			Console.WriteLine("BOOSTS_STATE: open=" + player.HasOpenedDialog(SingletonScript<D_Boosts>.Instance) + " timer=" + (player.GetTimer(D_Boosts.liveTimerTK) != null) +
				" x=" + TagMath.IGetTag(player, D_Boosts.xTK) + " y=" + TagMath.IGetTag(player, D_Boosts.yTK) + " end");
		}
	}
}
