using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using SteamEngine.LScript;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1587 - every constant and def field whose script text holds Sphere's braces ({min max} ranges, {a 1 b 2}
	/// weighted picks, RAND{...}) resolves, every NPCDef creates an NPC, and every TemplateDef and NPC loot line makes its items. Values resolve lazily, so before this a value
	/// LScript couldn't parse only failed when something first read it, e.g. a monster summoning that NPC.
	/// TestResolveAllDefValues writes the brace and creation failures to the given file, one per line, and every other
	/// value that doesn't resolve to the same path + ".other" (the follow-ups), then prints the counts.</summary>
	public static class DefValuesResolveTests {
		private const BindingFlags Any = BindingFlags.Static | BindingFlags.Instance | BindingFlags.NonPublic;
		private const int Runs = 50;

		[SteamFunction]
		public static void TestResolveAllDefValues(Character self, ScriptArgs sa) {
			string path = sa.Args.Trim().Trim('"');
			List<string> failures = new List<string>(), others = new List<string>();

			IDictionary constants = (IDictionary) typeof(Constant).GetField("allConstantsByName", Any).GetValue(null);
			FieldInfo implementationField = typeof(Constant).GetField("implementation", Any);
			int constantCount = 0, braceCount = 0;
			foreach (Constant c in new ArrayList(constants.Values)) {
				constantCount++;
				string text = RawText(implementationField.GetValue(c), "str");
				braceCount += HasBraces(text) ? 1 : 0;
				try {
					_ = c.Value;
				} catch (Exception e) {
					(HasBraces(text) ? failures : others).Add("constant " + c.Name + " = " + text + " : " + FirstLine(e));
				}
			}

			FieldInfo fieldValuesField = typeof(AbstractDef).GetField("fieldValues", Any);
			FieldInfo defaultValueField = typeof(FieldValue).GetField("defaultValue", Any);
			HashSet<AbstractDef> seen = new HashSet<AbstractDef>(); //AllScripts holds a def under each of its names
			List<NPCDef> npcDefs = new List<NPCDef>();
			List<TemplateDef> templates = new List<TemplateDef>();
			int fieldCount = 0;
			foreach (AbstractScript script in new List<AbstractScript>(AbstractScript.AllScripts)) {
				if ((script is AbstractDef def) && seen.Add(def) && !def.IsUnloaded) {
					Hashtable fieldValues = (Hashtable) fieldValuesField.GetValue(def);
					foreach (DictionaryEntry entry in new Hashtable(fieldValues)) {
						if (entry.Key is string name) { //a tag field is also held under its TagKey
							FieldValue fv = (FieldValue) entry.Value;
							string text = RawText(defaultValueField.GetValue(fv), "valueString");
							fieldCount++;
							braceCount += HasBraces(text) ? 1 : 0;
							try {
								_ = fv.CurrentValue;
							} catch (Exception e) {
								(HasBraces(text) ? failures : others).Add("def " + def.PrettyDefname + " " + name + " = " + text + " : " + FirstLine(e));
							}
						}
					}
					if (def is NPCDef npcDef) {
						npcDefs.Add(npcDef);
					} else if (def is TemplateDef template) {
						templates.Add(template);
					}
				}
			}

			foreach (NPCDef npcDef in npcDefs) {
				try {
					Thing npc = npcDef.Create(self.X, self.Y, self.Z, self.M);
					npc.Delete();
				} catch (Exception e) {
					failures.Add("create " + npcDef.PrettyDefname + " : " + FirstLine(e));
				}
			}

			//a template's ITEM lines and an NPC's loot lines are code, not fields: each runs many times, so a weighted list's
			//picks come up (a list of 11 misses one in 50 runs about 1 time in 120)
			Item bag = (Item) ThingDef.GetByDefname("i_backpack").Create(self.X, self.Y, self.Z, self.M);
			foreach (TemplateDef template in templates) {
				try {
					for (int i = 0; i < Runs; i++) {
						template.Create(bag);
					}
				} catch (Exception e) {
					failures.Add("template " + template.PrettyDefname + " : " + FirstLine(e));
				}
				Empty(bag);
			}
			int lootLines = 0;
			foreach (NPCDef npcDef in npcDefs) { //as NPCDef.CreateLoot runs them, which logs a failure instead of throwing
				foreach (string loot in npcDef.Loot ?? new List<string>()) {
					if (QuestItems.IsQuestItem(QuestItems.LootDefname(loot))) {
						continue; //a tag, not an item
					}
					lootLines++;
					try {
						LScriptHolder runner = LScriptMain.GetNewSnippetRunner("NPCDef " + npcDef.PrettyDefname, 0, "ITEM = " + loot);
						for (int i = 0; i < Runs; i++) {
							runner.Run(bag, (ScriptArgs) null);
						}
					} catch (Exception e) {
						(HasBraces(loot) ? failures : others).Add("loot " + npcDef.PrettyDefname + " " + loot + " : " + FirstLine(e));
					}
					Empty(bag);
				}
			}
			bag.Delete();

			failures.Sort(StringComparer.Ordinal);
			others.Sort(StringComparer.Ordinal);
			File.WriteAllText(path, string.Join("\n", failures));
			File.WriteAllText(path + ".other", string.Join("\n", others));
			Console.WriteLine("DEFVALUES_RESOLVED: constants=" + constantCount + " fields=" + fieldCount + " braces=" + braceCount
				+ " npcs=" + npcDefs.Count + " templates=" + templates.Count + " lootLines=" + lootLines + " failures=" + failures.Count + " others=" + others.Count + " end");
		}

		/// <summary>Counts the NPCs of the given def next to self (what ".add" just made), then deletes them.</summary>
		[SteamFunction]
		public static void TestCountAddedNpcs(Character self, ScriptArgs sa) {
			string defname = sa.Args.Trim().Trim('"');
			AbstractScript def = AbstractScript.GetByDefname(defname);
			int count = 0;
			foreach (AbstractCharacter ch in new List<AbstractCharacter>(self.GetMap().GetCharsInRange(self.X, self.Y, 5))) {
				if ((ch != self) && (ch.Def == def)) {
					count++;
					ch.Delete();
				}
			}
			Console.WriteLine("DEFVALUES_ADDED: def=" + defname + " count=" + count + " end");
		}

		private static void Empty(Item bag) {
			foreach (AbstractItem made in new List<AbstractItem>(bag)) {
				made.Delete();
			}
		}

		/// <summary>The script text of a value not resolved yet; null once it is (then it can't fail any more).</summary>
		private static string RawText(object implementation, string fieldName) {
			if (implementation == null) {
				return null;
			}
			FieldInfo field = implementation.GetType().GetField(fieldName, Any);
			return (field == null) ? null : (string) field.GetValue(implementation);
		}

		private static bool HasBraces(string text) {
			return (text != null) && ((text.IndexOf('{') >= 0) || (text.IndexOf('}') >= 0));
		}

		private static string FirstLine(Exception e) {
			StringBuilder sb = new StringBuilder();
			for (Exception x = e; x != null; x = x.InnerException) {
				if (x is TargetInvocationException) {
					continue;
				}
				string message = x.Message.Split('\n')[0].Trim();
				if (sb.ToString().IndexOf(message, StringComparison.Ordinal) < 0) {
					if (sb.Length > 0) {
						sb.Append(" / ");
					}
					sb.Append(message);
				}
			}
			return sb.ToString();
		}
	}
}
