using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#1107 - Erebor's d_afk_dialog (systems/administrace/afk_dia_log.scp): the AFK log, filtered by any text and
	/// paged. Each line can be deleted; a line where a player went to the camp also offers Trest (lower the skill they
	/// were macroing, not below their profession's minimum; in the camp it's the Lumberjacking they get back) and Milost
	/// (take back one AFK camp level, releasing them if they're in the camp). Erebor kept a log per GM; here it's one log.</summary>
	public class D_AfkLog : CompiledGumpDef {
		internal const int CloseButton = 0, PrevButton = 1, NextButton = 2, SearchButton = 3, FilterEntry = 1;
		internal const int DeleteBase = 10000, PunishBase = 100000, PardonBase = 1000000;
		internal const int PageSize = 20;
		private const int Width = 760, Row = 19, Margin = 10, Hue = 955, TitleHue = 0x481;

		[SteamFunction("AfkLog")]
		public static void AfkLogCommand(Player self, ScriptArgs sa) {
			string filter = ((sa == null) || (sa.Args == null)) ? "" : sa.Args.Trim().Trim('"');
			Open(self, 0, filter);
		}

		internal static void Open(Player gm, int first, string filter) {
			gm.Dialog(gm, SingletonScript<D_AfkLog>.Instance, new DialogArgs(Math.Max(0, first), filter ?? ""));
		}

		internal static List<AfkLogEntry> Matching(string filter) {
			List<AfkLogEntry> found = new List<AfkLogEntry>();
			foreach (AfkLogEntry entry in AfkLogBook.Lines) {
				if (string.IsNullOrEmpty(filter) || (entry.ToString().IndexOf(filter, StringComparison.OrdinalIgnoreCase) >= 0)) {
					found.Add(entry);
				}
			}
			return found;
		}

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			int first = (int) args[0];
			string filter = (string) args[1];
			List<AfkLogEntry> found = Matching(filter);
			int height = ((PageSize + 3) * Row) + (3 * Margin);
			this.SetLocation(20, 20);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, height, 9200);
			this.AddLabel(Margin, Margin, TitleHue, "Vyhovujici zpravy: " + found.Count + " celkem: " + AfkLogBook.Lines.Count);
			this.AddLabel(300, Margin, TitleHue, "Vyhledavaci kriterium: ");
			this.AddTextEntry(460, Margin, 160, Row, TitleHue, FilterEntry, filter);
			this.AddButton(630, Margin, 4011, 4013, SearchButton, GumpButtonType.Reply, 0);
			this.AddButton(Width - Margin - 33, Margin, 0xfb1, 0xfb3, CloseButton, GumpButtonType.Reply, 0);
			int y = Margin + Row + Margin;
			this.AddLabel(Margin, y, Hue, "Smaz");
			this.AddLabel(50, y, Hue, "Text zpravy");
			this.AddLabel(Width - 90, y, Hue, "Trest");
			this.AddLabel(Width - 45, y, Hue, "Milost");
			if (first > 0) {
				this.AddButton(Width - Margin - 16, y, 0xfa, 0xfb, PrevButton, GumpButtonType.Reply, 0);
			}
			y += Row;
			for (int i = first; (i < found.Count) && (i < first + PageSize); i++) {
				AfkLogEntry entry = found[i];
				int index = AfkLogBook.Lines.IndexOf(entry);
				this.AddButton(Margin, y, 0xfb1, 0xfb3, DeleteBase + index, GumpButtonType.Reply, 0);
				this.AddLabelCropped(50, y, Width - 150, Row, Hue, entry.ToString());
				if (entry.action != null) {
					this.AddButton(Width - 85, y, 4005, 4007, PunishBase + index, GumpButtonType.Reply, 0);
					this.AddButton(Width - 40, y, 0xfb4, 0xfb6, PardonBase + index, GumpButtonType.Reply, 0);
				}
				y += Row;
			}
			if (found.Count > first + PageSize) {
				this.AddButton(Width - Margin - 16, height - Margin - 22, 0xfc, 0xfd, NextButton, GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (!(gi.Cont is Player gm)) {
				return;
			}
			int first = (int) args[0];
			string filter = (string) args[1];
			int button = gr.PressedButton;
			if (button == CloseButton) {
				return;
			} else if (button == PrevButton) {
				Open(gm, first - PageSize, filter);
			} else if (button == NextButton) {
				Open(gm, first + PageSize, filter);
			} else if (button == SearchButton) {
				Open(gm, 0, (gr.GetTextResponse(FilterEntry) ?? "").Trim());
			} else if (button >= PardonBase) {
				Pardon(gm, Entry(button - PardonBase));
			} else if (button >= PunishBase) {
				Punish(gm, Entry(button - PunishBase));
			} else if (button >= DeleteBase) {
				AfkLogEntry entry = Entry(button - DeleteBase);
				if (entry != null) {
					AfkLogBook.Remove(entry);
				}
				int left = Matching(filter).Count;
				Open(gm, (left > first) ? first : (first - PageSize), filter);
			}
		}

		private static AfkLogEntry Entry(int index) {
			return ((index >= 0) && (index < AfkLogBook.Lines.Count)) ? AfkLogBook.Lines[index] : null;
		}

		/// <summary>nullAFKTrest: afklagrodvolat, after a yes.</summary>
		private static void Pardon(Player gm, AfkLogEntry entry) {
			if ((entry == null) || (entry.who == null) || entry.who.IsDeleted) {
				return;
			}
			Character who = entry.who;
			D_TownConfirm.Ask(gm, "Opravdu chces anulovat jeden trest hraci " + who.Name + "?", () => {
				Camp.LowerLevel(who, true);
				AfkLogBook.Add(who, "GM " + gm.Name + " anuloval jeden AFK trest", null);
			});
		}

		/// <summary>The skill a camp line's action names: Erebor's parseSkill.</summary>
		internal static SkillName SkillOf(string action) {
			if ((action != null) && action.StartsWith("Skill_", StringComparison.Ordinal) &&
					Enum.TryParse(action.Substring("Skill_".Length), true, out SkillName skill)) {
				return skill;
			}
			return SkillName.None;
		}

		/// <summary>dropSkill: how much, then down, not below the profession's minimum. A convict's Lumberjacking is the
		/// kept one they get back at release.</summary>
		private static void Punish(Player gm, AfkLogEntry entry) {
			if ((entry == null) || (entry.who == null) || entry.who.IsDeleted) {
				return;
			}
			Character who = entry.who;
			SkillName skill = SkillOf(entry.action);
			if (skill == SkillName.None) {
				gm.SysMessage("Neni znamo, jaky skill makroval (" + entry.action + ").");
				return;
			}
			D_TownInput.Ask(gm, "O kolik snizit skill " + skill + " hraci " + who.Name + "? (Nyni ma " + DroppedSkillValue(who, skill) / 10.0 + ")", "10.0", answer => {
				if (!double.TryParse(answer.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out double by) || (by <= 0)) {
					return;
				}
				int now = DropSkill(who, skill, (int) Math.Round(by * 10));
				gm.SysMessage("Snizil jsi hraci " + who.Name + " skill " + skill + " o " + by.ToString(CultureInfo.InvariantCulture) + " na " + (now / 10.0).ToString(CultureInfo.InvariantCulture));
				who.RedMessage("Skill " + skill + " za trest snizen o " + by.ToString(CultureInfo.InvariantCulture));
				AfkLogBook.Add(who, "snizen skill " + skill + " o " + by.ToString(CultureInfo.InvariantCulture) + " na " + (now / 10.0).ToString(CultureInfo.InvariantCulture) + ". Snizil " + gm.Name, null);
			});
		}

		private static int DroppedSkillValue(Character who, SkillName skill) {
			return ((skill == SkillName.Lumberjacking) && Camp.IsInCamp(who)) ? Camp.KeptLumberjacking(who) : who.GetSkill(skill);
		}

		/// <summary>The new value.</summary>
		internal static int DropSkill(Character who, SkillName skill, int by) {
			int minimum = ((who is Player player) && (player.Profession != null)) ? player.Profession.GetSkillMinimum(skill) : 0;
			int value = Math.Max(Math.Max(0, minimum), DroppedSkillValue(who, skill) - by);
			if ((skill == SkillName.Lumberjacking) && Camp.IsInCamp(who)) {
				Camp.SetKeptLumberjacking(who, value);
			} else {
				who.SetRealSkillValue((int) skill, value);
			}
			return value;
		}
	}
}
