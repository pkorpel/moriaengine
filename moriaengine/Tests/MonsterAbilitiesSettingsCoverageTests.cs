using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SteamEngine.CompiledScripts;
using Xunit;

namespace SteamEngine.Tests {
	/// <summary>
	/// #783 - standing rule: every monster ability's tunable defaults are editable on the .settings
	/// "Monster Abilities" page (MonsterAbilitiesSettings). The strangler vine (#778) shipped without
	/// it. This finds every monster AbilityDef through MonsterSpecialAttacks' lazy "...Def"
	/// lookups and requires a "&lt;Ability&gt;&lt;Property&gt;" settings property for each public settable
	/// property the ability class declares itself - so a new ability, or a new field on one, that
	/// isn't added to the page fails the fast tier (and the pre-push hook).
	/// </summary>
	public class MonsterAbilitiesSettingsCoverageTests {
		//the settings properties whose names predate this naming rule
		private static readonly Dictionary<string, string> legacyNames = new Dictionary<string, string> {
			{ "SelfReser.Delay", "SelfresDelay" },
			{ "Summoner.SummonLifetime", "SummonerLifetime" },
			{ "RoundattackPull.PullDistance", "RoundattackPullDistance" },
		};

		private static IEnumerable<Type> MonsterAbilityDefTypes() {
			return typeof(MonsterSpecialAttacks)
				.GetProperties(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
				.Select(p => p.PropertyType)
				.Where(t => typeof(AbilityDef).IsAssignableFrom(t))
				.Distinct();
		}

		[Fact]
		public void FindsTheMonsterAbilities() {
			List<Type> types = MonsterAbilityDefTypes().ToList();
			Assert.Contains(typeof(StunAbilityDef), types);
			Assert.Contains(typeof(StranglerVineAbilityDef), types);
			Assert.True(types.Count >= 15, "only found " + types.Count + " monster abilities - did the lookup convention change?");
		}

		[Fact]
		public void EveryMonsterAbilityTunableIsOnTheSettingsPage() {
			HashSet<string> settings = new HashSet<string>(typeof(MonsterAbilitiesSettings)
				.GetProperties(BindingFlags.Instance | BindingFlags.Public)
				.Where(p => p.CanRead && p.CanWrite)
				.Select(p => p.Name));

			List<string> missing = new List<string>();
			foreach (Type type in MonsterAbilityDefTypes()) {
				string ability = type.Name.EndsWith("AbilityDef", StringComparison.Ordinal) ?
					type.Name.Substring(0, type.Name.Length - "AbilityDef".Length) : type.Name;
				foreach (PropertyInfo tunable in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)) {
					if (!tunable.CanWrite || (tunable.GetSetMethod() == null)) {
						continue;
					}
					string key = ability + "." + tunable.Name;
					string expected;
					if (!legacyNames.TryGetValue(key, out expected)) {
						expected = ability + tunable.Name;
					}
					if (!settings.Contains(expected)) {
						missing.Add(key + " -> MonsterAbilitiesSettings." + expected);
					}
				}
			}
			Assert.True(missing.Count == 0, "Monster ability defaults missing from .settings \"Monster Abilities\":\n" + string.Join("\n", missing));
		}
	}
}
