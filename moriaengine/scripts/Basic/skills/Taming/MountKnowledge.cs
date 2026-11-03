using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#962 - Erebor's def_taming_mount table and f_tamingMounts_stats: each mount of 30.0 taming or more tamed
	/// from a spawn teaches its tamer, up to that mount's maximum. Only the count is kept, the points follow from the
	/// table, so f_tamingBonus_recalculateTotalPoints has nothing to do. Mount powers read the bonus, #925.</summary>
	public static class MountKnowledge {
		private const int Permille = 1000;

		internal sealed class Mount {
			internal ThingDef def;
			internal int points, maxPoints, icon, unknownIcon;
			internal string name;
			internal TagKey countTK;

			internal int TimesForMost {
				get {
					return this.maxPoints / this.points;
				}
			}

			internal int PointsFor(int count) {
				return System.Math.Min(count * this.points, this.maxPoints);
			}
		}

		private sealed class Source : IActiveEffectSource {
			public void AddEffects(Character ch, List<ActiveEffect> into) {
				if (TotalPoints(ch) > 0) {
					string percent = (Bonus(ch) / 10.0).ToString("0.00", CultureInfo.InvariantCulture);
					into.Add(new ActiveEffect(EffectSection.Status, Loc<MountKnowledgeLoc>.Get(ch.Language).Title) { Detail = percent + "%" });
				}
			}
		}

		private static readonly Source source = new Source();
		private static List<Mount> mounts;

		public static void Bootstrap() {
			ActiveEffects.Register(source);
		}

		private static int MaxPoints {
			get {
				return Number("def_taming_max_points");
			}
		}

		private static int Number(string constant) {
			return System.Convert.ToInt32(Constant.GetByName(constant).Value, CultureInfo.InvariantCulture);
		}

		internal static List<Mount> Mounts {
			get {
				if (mounts == null) {
					List<Mount> read = new List<Mount>();
					for (int i = 0, last = Number("def_taming_mounts_count"); i <= last; i++) {
						object chardef = Constant.GetByName("def_taming_mount_chardef[" + i + "]").Value; //a name resolves to its def
						ThingDef def = (chardef as ThingDef) ?? ThingDef.GetByDefname(chardef.ToString());
						read.Add(new Mount {
							def = def,
							points = Number("def_taming_mount_points[" + i + "]"),
							maxPoints = Number("def_taming_mount_maxpoints[" + i + "]"),
							name = Constant.GetByName("def_taming_mount_name[" + i + "]").Value.ToString(),
							icon = Number("def_taming_mount_icon[" + i + "]"),
							unknownIcon = Number("def_taming_mount_unknown[" + i + "]"),
							countTK = TagKey.Acquire("tamingCount_" + i)
						});
					}
					mounts = read;
				}
				return mounts;
			}
		}

		internal static int CountOf(Character ch, Mount mount) {
			object count = ch.GetTag(mount.countTK);
			return (count == null) ? 0 : System.Convert.ToInt32(count, CultureInfo.InvariantCulture);
		}

		public static int TotalPoints(Character ch) {
			int total = 0;
			foreach (Mount mount in Mounts) {
				total += mount.PointsFor(CountOf(ch, mount));
			}
			return total;
		}

		/// <summary>Per mille: what's known, never past 50 a point of the ranger's mountsKnowledge (#925, mount_equip), nor
		/// the tamer's Taming and Animal Lore.</summary>
		public static int Bonus(Character ch) {
			int known = (TotalPoints(ch) * Permille) / MaxPoints;
			known = System.Math.Min(known, ClassAbilities.Points(ch, "mountsKnowledge") * 50);
			return System.Math.Min(known, System.Math.Min(ch.GetSkill(SkillName.AnimalTaming), ch.GetSkill(SkillName.AnimalLore)));
		}

		internal static string ShareOfTotal(Mount mount, int count) {
			return ((mount.PointsFor(count) * 100.0) / MaxPoints).ToString("0.###", CultureInfo.InvariantCulture).Replace('.', ',');
		}

		/// <summary>f_tamingMounts_stats and f_mount_getPoints.</summary>
		public static void Learn(Player self, NPC tamed) {
			Mount mount = Mounts.Find(candidate => candidate.def == tamed.Def);
			if (mount != null) {
				int count = CountOf(self, mount) + 1;
				self.SetTag(mount.countTK, count);
				MountKnowledgeLoc texts = Loc<MountKnowledgeLoc>.Get(self.Language);
				string message = (count * mount.points) > mount.maxPoints
					? (self.IsFemale ? texts.NothingNewFemale : texts.NothingNewMale)
					: (self.IsFemale ? texts.LearnedFemale : texts.LearnedMale);
				self.SysMessage(message, Bandaging.ClassMessageColor);
			}
		}
	}

	/// <summary>d_boosty_MountsKnowledge, .boosts' button.</summary>
	public class D_MountsKnowledge : CompiledGumpDef {
		internal const int DoneHue = 282;
		private const int Columns = 3, Left = 50, Top = 70, Indent = 60, ColumnWidth = 150, RowHeight = 70;
		private const int HelpButton = 1;

		public static void Show(Player player) {
			player.Dialog(player, SingletonScript<D_MountsKnowledge>.Instance, new DialogArgs());
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player viewer = (Player) sendTo;
			this.SetLocation(5, 5);
			this.AddPage(0);
			this.AddBackground(20, 20, 500, 480, 3000);
			this.AddLabel(Left, 30, QuestDialogContent.ColorResponse, Loc<MountKnowledgeLoc>.Get(viewer.Language).Title);
			this.AddButton(495, 40, 0x828, 0x4cb, HelpButton, GumpButtonType.Reply, 0);
			List<MountKnowledge.Mount> mounts = MountKnowledge.Mounts;
			for (int i = 0; i < mounts.Count; i++) {
				this.Row(viewer, mounts[i], Left + (ColumnWidth * (i % Columns)), Top + (RowHeight * (i / Columns)));
			}
		}

		private void Row(Player viewer, MountKnowledge.Mount mount, int x, int y) {
			int count = MountKnowledge.CountOf(viewer, mount), most = mount.TimesForMost;
			int hue = (count >= most) ? DoneHue : QuestDialogContent.ColorResponse;
			if (count == 0) {
				this.AddItem(x, y, mount.unknownIcon);
				this.AddLabel(x + Indent, y - 5, hue, "?????");
				this.AddLabel(x + Indent, y + 12, hue, "?? / ??");
				this.AddLabel(x + Indent, y + 27, hue, "0 %");
			} else {
				this.AddItem(x, y, mount.icon);
				this.AddLabel(x + Indent, y - 5, hue, mount.name);
				this.AddLabel(x + Indent, y + 12, hue, count + " / " + most);
				this.AddLabel(x + Indent, y + 27, hue, MountKnowledge.ShareOfTotal(mount, count) + " %");
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == HelpButton) {
				Player player = (Player) gi.Cont;
				player.Dialog(player, SingletonScript<D_MountsKnowledgeLegend>.Instance, new DialogArgs());
			}
		}
	}

	/// <summary>d_boosty_MountsKnowledge_legend.</summary>
	public class D_MountsKnowledgeLegend : CompiledGumpDef {
		private const int BackButton = 1, Left = 110, Notes = 185;

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Player viewer = (Player) sendTo;
			MountKnowledgeLoc texts = Loc<MountKnowledgeLoc>.Get(viewer.Language);
			int text = QuestDialogContent.ColorResponse;
			this.SetLocation(5, 5);
			this.AddPage(0);
			this.AddBackground(20, 20, 445, 150, 3000);
			this.AddLabel(50, 30, text, texts.Legend);
			this.AddButton(440, 40, 0x828, 0x4cb, BackButton, GumpButtonType.Reply, 0);
			this.AddItem(50, 70, MountKnowledge.Mounts[0].icon);
			this.AddLabel(Left, 65, text, "Lama");
			this.AddLabel(Notes, 65, D_MountsKnowledge.DoneHue, texts.LegendName);
			this.AddLabel(Left, 82, text, "64 / 84");
			this.AddLabel(Notes, 82, D_MountsKnowledge.DoneHue, texts.LegendCount);
			this.AddLabel(Notes, 97, D_MountsKnowledge.DoneHue, texts.LegendForMost);
			this.AddLabel(Left, 112, text, "4,75 %");
			this.AddLabel(Notes, 112, D_MountsKnowledge.DoneHue, texts.LegendShare);
			this.AddLabel(Notes, 127, D_MountsKnowledge.DoneHue, texts.LegendOfTotal);
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (gr.PressedButton == BackButton) {
				D_MountsKnowledge.Show((Player) gi.Cont);
			}
		}
	}

	internal class MountKnowledgeLoc : CompiledLocStringCollection<MountKnowledgeLoc> {
		public string Title = "Znalost jezditek";
		public string NothingNewMale = "Ochocenim tohoto jezditka jsi se nic noveho nedozvedel.";
		public string NothingNewFemale = "Ochocenim tohoto jezditka jsi se nic noveho nedozvedela.";
		public string LearnedMale = "Ochocenim tohoto jezditka jsi rozsiril sve znalosti o jezditkach.";
		public string LearnedFemale = "Ochocenim tohoto jezditka jsi rozsirila sve znalosti o jezditkach.";
		public string Legend = "Legenda";
		public string LegendName = "-> Nazev jezditka";
		public string LegendCount = "-> Ochoceno celkem";
		public string LegendForMost = "     / pocet ochoceni pro max bonus";
		public string LegendShare = "-> Kolik % toto jezditko pridava";
		public string LegendOfTotal = "     do celkove znalosti jezditek";
	}
}
