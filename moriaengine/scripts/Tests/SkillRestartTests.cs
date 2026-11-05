using System;

namespace SteamEngine.CompiledScripts {
	/// <summary>#1172 - server side of Tests/Protocol/SkillRestartTests.cs: a skill under way when the world is saved goes on
	/// after the restart, as Sphere's does. A smith making two daggers, a second smith between two items, and a monster in the
	/// middle of a swing at a sheep.</summary>
	public static class SkillRestartTests {
		private static readonly TagKey holderTK = TagKey.Acquire("_skillRestartHolder_");
		private const double HoldUntilSaveSeconds = 120; //nothing may fire between the setup and "presave"
		private const double StrokeAfterRestartSeconds = 5; //due while the smith is still offline after the restart
		private const double NextItemAfterRestartSeconds = 15; //the second smith's next item, due while they're offline too

		[SteamFunction]
		public static void TestSkillRestart(Character self, ScriptArgs sa) {
			string what = sa.Args.Trim().Trim('"');
			if (what == "setup") {
				//the smith: copper daggers, two of them, with a forge at hand and a light
				self.SetTag(holderTK, "smith");
				StartDaggers(self, 2);

				//the monster: held still, swinging at a sheep that can take it
				NPC sheep = (NPC) ThingDef.GetByDefname("c_sheep_woolly").Create(self.X, self.Y + 2, self.Z, self.M);
				sheep.SetTag(holderTK, "sheep");
				NPCAiTimer.EnsureInstalled(sheep);
				NPCAiTimer.GetFor(sheep).isHeld = true;
				sheep.MaxHits = 30000;
				sheep.Hits = 30000;
				NPC orc = (NPC) ThingDef.GetByDefname("c_orc_lord").Create(self.X + 1, self.Y + 2, self.Z, self.M);
				orc.SetTag(holderTK, "orc");
				NPCAiTimer.EnsureInstalled(orc);
				NPCAiTimer.GetFor(orc).isHeld = true; //no thinking: only the saved swing may hit
				WeaponSkillTargetQueuePlugin.AddTarget(orc, sheep);
				//a second one, whose sheep is gone by the save ("presave" deletes it): its swing must end quietly
				NPC heldSheep = (NPC) ThingDef.GetByDefname("c_sheep_woolly").Create(self.X, self.Y + 3, self.Z, self.M);
				heldSheep.SetTag(holderTK, "lostSheep");
				NPCAiTimer.EnsureInstalled(heldSheep);
				NPCAiTimer.GetFor(heldSheep).isHeld = true;
				NPC heldOrc = (NPC) ThingDef.GetByDefname("c_orc_lord").Create(self.X + 1, self.Y + 3, self.Z, self.M);
				heldOrc.SetTag(holderTK, "lostOrc");
				NPCAiTimer.EnsureInstalled(heldOrc);
				NPCAiTimer.GetFor(heldOrc).isHeld = true;
				WeaponSkillTargetQueuePlugin.AddTarget(heldOrc, heldSheep);
				if (heldOrc.CurrentSkillTimer != null) {
					heldOrc.CurrentSkillTimer.DueInSeconds = HoldUntilSaveSeconds;
				}

				//held until "presave", which sets them due a little after the save
				SkillSequenceArgs.SkillStrokeTimer smithTimer = ((Character) self).CurrentSkillTimer;
				SkillSequenceArgs.SkillStrokeTimer orcTimer = orc.CurrentSkillTimer;
				if (smithTimer != null) {
					smithTimer.DueInSeconds = HoldUntilSaveSeconds;
				}
				if (orcTimer != null) {
					orcTimer.DueInSeconds = HoldUntilSaveSeconds;
				}
				Console.WriteLine("SKILL_RESTART: setup smithSkill=" + SkillOf(self) + " orcSkill=" + SkillOf(orc) +
					" daggers=" + Count(self, "i_dagger_copper") + " sheepHits=" + sheep.Hits + " |");
				return;
			}
			if (what == "between") {
				//the second smith: one dagger to go and nothing in the hands, as MakeFinished leaves it between two items
				self.SetTag(holderTK, "smithy");
				StartDaggers(self, 1);
				SkillSequenceArgs.SkillStrokeTimer stroke = ((Character) self).CurrentSkillTimer;
				if (stroke != null) {
					self.RemoveTimer(SteamEngine.Timers.TimerKey.Acquire("_skillTimer_"));
					stroke.Delete();
				}
				CraftingProcessPlugin plugin = self.GetPlugin(PluginKey.Acquire("_craftingProcess_")) as CraftingProcessPlugin;
				if (plugin != null) {
					plugin.Timer = HoldUntilSaveSeconds;
				}
				Console.WriteLine("SKILL_RESTART: between smithySkill=" + SkillOf(self) + " plugin=" + (plugin != null) +
					" daggers=" + Count(self, "i_dagger_copper") + " |");
				return;
			}

			Character smith = null, smithy = null, orcAgain = null, sheepAgain = null, lostOrc = null, lostSheep = null;
			foreach (Thing thing in Thing.AllThings) {
				switch (thing.GetTag(holderTK) as string) {
					case "smith":
						smith = (Character) thing;
						break;
					case "smithy":
						smithy = (Character) thing;
						break;
					case "lostOrc":
						lostOrc = (Character) thing;
						break;
					case "lostSheep":
						lostSheep = (Character) thing;
						break;
					case "orc":
						orcAgain = (Character) thing;
						break;
					case "sheep":
						sheepAgain = (Character) thing;
						break;
				}
			}
			string line = "found=" + (smith != null) + "," + (smithy != null) + "," + (orcAgain != null) + "," + (sheepAgain != null);
			if ((smith != null) && (smithy != null) && (orcAgain != null) && (sheepAgain != null)) {
				if (what == "presave") {
					//right before the save: the strokes come due a little after it, so the save catches them under way and, after
					//the restart, the smiths' come due while they're offline (they wait for the login) and the orc's simply lands
					if (smith.CurrentSkillTimer != null) {
						smith.CurrentSkillTimer.DueInSeconds = StrokeAfterRestartSeconds;
					}
					if (orcAgain.CurrentSkillTimer != null) {
						orcAgain.CurrentSkillTimer.DueInSeconds = StrokeAfterRestartSeconds;
					}
					CraftingProcessPlugin plugin = smithy.GetPlugin(PluginKey.Acquire("_craftingProcess_")) as CraftingProcessPlugin;
					if (plugin != null) {
						plugin.Timer = NextItemAfterRestartSeconds;
					}
					if (lostOrc != null) {
						if (lostOrc.CurrentSkillTimer != null) {
							lostOrc.CurrentSkillTimer.DueInSeconds = StrokeAfterRestartSeconds;
						}
						if (lostSheep != null) {
							lostSheep.Delete(); //the swing's target is gone, the swing itself is saved
						}
					}
				}
				//"presave": the state the save catches; "after": once the strokes have had time to fire
				line += " smithSkill=" + SkillOf(smith) + " orcSkill=" + SkillOf(orcAgain) +
					" daggers=" + Count(smith, "i_dagger_copper") + " smithyDaggers=" + Count(smithy, "i_dagger_copper") +
					" smithyPlugin=" + (smithy.GetPlugin(PluginKey.Acquire("_craftingProcess_")) != null) + " sheepHits=" + sheepAgain.Hits +
					" lostOrcSkill=" + ((lostOrc != null) ? SkillOf(lostOrc) : "NoOrc");
			}
			Console.WriteLine("SKILL_RESTART: " + what + " " + line + " |");
		}

		/// <summary>Copper daggers, as many as asked, with a forge at hand and a light.</summary>
		private static void StartDaggers(Character self, int count) {
			self.SetRealSkillValue((int) SkillName.Blacksmith, 1000);
			self.SetRealSkillValue((int) SkillName.Tinkering, 1000);
			Item ingots = (Item) ((ItemDef) ThingDef.GetByDefname("i_ingot_copper")).Create(self.Backpack);
			ingots.Amount = 40;
			self.TryEquip((Item) ((ItemDef) ThingDef.GetByDefname("i_hammer_smith")).Create(self.Backpack));
			Item forge = (Item) ((ItemDef) ThingDef.GetByDefname("i_forge")).Create(self.Backpack);
			forge.P((ushort) (self.X + 1), self.Y, self.Z, self.M);
			Item candle = (Item) ((ItemDef) ThingDef.GetByDefname("i_candle_lit")).Create(self.Backpack);
			candle.P(self.X, self.Y, self.Z, self.M);
			SimpleQueue<CraftingSelection> queue = new SimpleQueue<CraftingSelection>();
			queue.Enqueue(new CraftingSelection((ItemDef) ThingDef.GetByDefname("i_dagger_copper"), count));
			CraftingProcessPlugin.StartCrafting(self, new CraftingOrder((CraftingSkillDef) SkillDef.GetBySkillName(SkillName.Blacksmith), queue));
		}

		/// <summary>The skill under way: None, its name, or SkillLost when a sequence came back without its skill (#1172).</summary>
		private static string SkillOf(Character ch) {
			SkillSequenceArgs ssa = ch.CurrentSkillArgs;
			if (ssa == null) {
				return "None";
			}
			return (ssa.SkillDef == null) ? "SkillLost" : ((SkillName) ssa.SkillDef.Id).ToString();
		}

		private static int Count(Character ch, string defname) {
			ThingDef def = ThingDef.GetByDefname(defname);
			int n = 0;
			foreach (AbstractItem item in ch.Backpack) {
				if (item.Def == def) {
					n += item.Amount;
				}
			}
			return n;
		}
	}
}
