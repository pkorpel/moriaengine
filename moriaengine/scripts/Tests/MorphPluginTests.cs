using System;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1424 - Polymorph and Incognito as their effect plugins: each is its plugin, a second incognito keeps the name
	/// and skin and only renews the time, its time running out gives the name back, and a death ends both and gives back
	/// the body, Str and name. Protocol side: Tests/Protocol/CoreSpellsTests.cs.</summary>
	public static class MorphPluginTests {
		private static readonly TagKey choiceTK = TagKey.Acquire("_polymorphChoice_"); //PolymorphMenu's

		private static void Check(string name, bool ok) {
			Console.WriteLine("MORPH_CHECK: " + name + "=" + ok);
		}

		[SteamFunction]
		public static void TestMorphPlugins(Character self) {
			int plevel = self.Account.PLevel;
			try {
				self.Account.PLevel = 1;
				string name = self.Name;
				int model = self.Model;
				short str = self.Str, dex = self.Dex;
				int color = self.Color;
				IncognitoSpellDef incognito = (IncognitoSpellDef) SpellDef.GetByDefname("s_incognito");
				PolymorphSpellDef polymorph = (PolymorphSpellDef) SpellDef.GetByDefname("s_polymorph");

				incognito.EffectChar(self, self, EffectFlag.FromSpellBook);
				IncognitoEffectPlugin disguise = self.GetPlugin(incognito.EffectPluginKey_Spell) as IncognitoEffectPlugin;
				string disguisedAs = self.Name;
				int skin = self.Color;
				Check("incognitoIsPlugin", (disguise != null) && IncognitoSpellDef.HasIncognito(self) && (IncognitoSpellDef.TrueName(self) == name)
					&& (disguisedAs != name) && (disguise.Timer > 0));
				disguise.Timer = 5;
				incognito.EffectChar(self, self, EffectFlag.FromSpellBook);
				Check("secondIncognitoRenews", (self.GetPlugin(incognito.EffectPluginKey_Spell) == disguise) && (self.Name == disguisedAs)
					&& (self.Color == skin) && (disguise.Timer > 60));
				disguise.On_Timer(); //its time runs out
				Check("incognitoTimesOut", !IncognitoSpellDef.HasIncognito(self) && (self.Name == name) && (self.Color == color));

				incognito.EffectChar(self, self, EffectFlag.FromSpellBook);
				self.SetTag(choiceTK, ThingDef.GetByDefname("c_dog"));
				polymorph.EffectChar(self, self, EffectFlag.FromSpellBook);
				PolymorphEffectPlugin form = self.GetPlugin(polymorph.EffectPluginKey_Spell) as PolymorphEffectPlugin;
				Check("polymorphIsPlugin", (form != null) && PolymorphSpellDef.IsPolymorphed(self) && (self.Model == ThingDef.GetByDefname("c_dog").Model)
					&& (form.Timer > 0));

				self.CauseDeath(null);
				bool endedAtDeath = !PolymorphSpellDef.IsPolymorphed(self) && !IncognitoSpellDef.HasIncognito(self);
				self.Resurrect();
				Console.WriteLine("MORPH_REPORT: name=" + self.Name + " model=" + self.Model + "/" + model + " str=" + self.Str + "/" + str);
				Check("deathEndsBoth", endedAtDeath && (self.Name == name) && (self.Model == model) && (self.Str == str) && (self.Dex == dex)
					&& (self.Color == color));
			} finally {
				PolymorphSpellDef.End(self);
				IncognitoSpellDef.End(self);
				self.Account.PLevel = (byte) plevel;
			}
			Console.WriteLine("MORPH_DONE");
		}
	}
}
