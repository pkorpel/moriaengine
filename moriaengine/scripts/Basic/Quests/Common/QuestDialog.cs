/*
	This program is free software; you can redistribute it and/or modify
	it under the terms of the GNU General Public License as published by
	the Free Software Foundation; either version 2 of the License, or
	(at your option) any later version.

	This program is distributed in the hope that it will be useful,
	but WITHOUT ANY WARRANTY; without even the implied warranty of
	MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
	GNU General Public License for more details.

	You should have received a copy of the GNU General Public License
	along with this program; if not, write to the Free Software
	Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307  USA
	Or visit http://www.gnu.org/copyleft/gpl.html
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {

	/// <summary>#833 - the content of one quest dialog, written the way Erebor's questDialog_framework.scp is: named pages,
	/// each with its NPC text and its responses, in any order. A page a response links to may come later.</summary>
	public sealed class QuestDialogContent {
		internal sealed class ResponseEntry {
			internal string text;
			internal bool isLong, isEmote, isExit;
			internal string target; //page name, or the exit button's name; null closes the dialog
		}

		internal sealed class PageEntry {
			internal string name, html;
			internal List<ResponseEntry> responses = new List<ResponseEntry>();
		}

		internal sealed class BaseButtonEntry {
			internal int number, indent, color;
			internal string label;
		}

		internal const int ColorResponse = 152, ColorEmote = 142, ColorBaseButton = 1152;
		private static readonly int[] defaultIndents = { 43, 25, 40, 30, 47, 38 };
		private static readonly string[] defaultLabels = { "Kdo jsi?", "Mohu pomoci?", "Obchodovat", "Hledam praci", "Nesu ti", "Prohlednout" };

		internal readonly List<PageEntry> pages = new List<PageEntry>();
		internal readonly List<BaseButtonEntry> baseButtons = new List<BaseButtonEntry>();
		private readonly Dictionary<string, string> savedTexts = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
		private readonly Character speaker;
		private readonly Player player;
		private PageEntry page;
		private string text;

		internal QuestDialogContent(Character speaker, Player player) {
			this.speaker = speaker;
			this.player = player;
			this.Page("Pozdrav");
		}

		/// <summary>questDialog_baseButt: one of the six big buttons, each opening the page named like its label.</summary>
		public void BaseButton(int number) {
			this.BaseButton(number, defaultIndents[number - 1], defaultLabels[number - 1]);
		}

		public void BaseButton(int number, int indent, string label) {
			this.baseButtons.Add(new BaseButtonEntry {
				number = number,
				indent = indent,
				label = label,
				color = (number == 6) ? ColorEmote : ColorBaseButton
			});
		}

		/// <summary>dialog_page: what follows goes on this page. Page names are compared without case and surrounding spaces.</summary>
		public void Page(string name) {
			name = name.Trim();
			this.page = this.pages.Find(p => string.Equals(p.name, name, StringComparison.OrdinalIgnoreCase));
			if (this.page == null) {
				this.page = new PageEntry { name = name };
				this.pages.Add(this.page);
			}
		}

		/// <summary>setText(newTextLine, ...): the text for the following Html or response.</summary>
		public void SetText(string text) {
			this.text = ExpandMarkup(text, this.player, this.speaker.Name);
		}

		/// <summary>questDialog_HTMLGUMP: the NPC's text on this page.</summary>
		public void Html() {
			this.page.html = this.text;
		}

		/// <summary>saveTextLine: keeps the last text for a later SameResponse.</summary>
		public void SaveText(string name) {
			this.savedTexts[name.Trim()] = this.text;
		}

		/// <summary>questDialog_addResponse: goes to the page `target`, or with `isExit` closes the dialog and reports the
		/// button named `target`.</summary>
		public void Response(bool isLong, bool isExit, string target, bool isEmote = false) {
			this.page.responses.Add(new ResponseEntry { text = this.text, isLong = isLong, isExit = isExit, target = target.Trim(), isEmote = isEmote });
		}

		/// <summary>questDialog_addSameResponse: a response with a text kept by SaveText under `target`.</summary>
		public void SameResponse(bool isLong, bool isExit, string target, bool isEmote = false) {
			this.text = this.savedTexts[target.Trim()];
			this.Response(isLong, isExit, target, isEmote);
		}

		/// <summary>questDialog_exitResponse: just closes the dialog.</summary>
		public void ExitResponse(bool isLong, bool isEmote) {
			this.page.responses.Add(new ResponseEntry { text = this.text, isLong = isLong, isExit = true, isEmote = isEmote });
		}

		public void ExitResponse() {
			this.text = "*Odejit*";
			this.ExitResponse(false, true);
		}

		//Erebor's own markup in a text, kept as it is in the archive
		private static readonly Regex choice = new Regex("<\\??src\\.(sex|splitRealm)\\(\"?([^\",)]*)\"?,\"?([^\",)]*)\"?\\)\\??>", RegexOptions.CultureInvariant);

		internal static string ExpandMarkup(string text, Player player, string name) {
			string expanded = choice.Replace(text, m => {
				bool first = (m.Groups[1].Value == "sex") ? (player.Gender != Gender.Female) : (player.Realm == Realm.Gondor);
				return first ? m.Groups[2].Value : m.Groups[3].Value;
			});
			return expanded.Replace("<name>", name)
				.Replace("<def_questDialog_colorTextEmote>", "<basefont color=#ffff88>")
				.Replace("<def_questDialog_colorTextNormal>", "<basefont color=#ffffff>")
				.Replace("<def_questDialog_colorTextResponse>", "<basefont color=#D7DF01>")
				.Replace("<def_questDialog_colorTextRespEmote>", "<basefont color=#ff0000>");
		}
	}

	/// <summary>#833 - a dialog of an NPC with quests, Erebor's questDialog_framework.scp: the NPC's face and name, six big
	/// buttons and pages of text with responses, responses over five rows spilling onto next pages. The subclass says what
	/// is on the pages for this player and what its exit buttons do. args: the NPC.</summary>
	public abstract class QuestDialogDef : CompiledGumpDef {
		private const int Width = 645, Height = 355, MaxRows = 6, RowHeight = 30, ResponseX = 30, ResponseY = 395, ReachDistance = 5;
		private static readonly int[] baseX = { 160, 325, 490, 160, 325, 490 };
		private static readonly int[] baseY = { 330, 330, 330, 360, 360, 360 };
		private readonly List<string> exitButtons = new List<string>();

		protected virtual int Face {
			get {
				return 0x3000;
			}
		}

		protected abstract void Build(QuestDialogContent d, Character npc, Player player);

		/// <summary>An exit button was pressed, by a living player still close enough.</summary>
		protected abstract void OnButton(string button, Character npc, Player player);

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			Character npc = (Character) args[0];
			QuestDialogContent d = new QuestDialogContent(npc, (Player) sendTo);
			this.Build(d, npc, (Player) sendTo);

			Dictionary<string, int> numbers = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
			foreach (QuestDialogContent.PageEntry page in d.pages) {
				numbers[page.name] = numbers.Count + 1;
			}
			int nextFreePage = d.pages.Count + 1;

			this.SetLocation(5, 5);
			this.AddPage(0);
			this.AddBackground(20, 200, Width, Height, 2620);
			this.AddImage(27, 210, 0x3182);
			this.AddImage(38, 217, this.Face);
			this.AddLabel(220, 210, 52, npc.Name);
			foreach (QuestDialogContent.BaseButtonEntry button in d.baseButtons) {
				int page;
				if (numbers.TryGetValue(button.label, out page)) {
					int x = baseX[button.number - 1], y = baseY[button.number - 1];
					this.AddButton(x, y, 0x988, 0x988, 0, GumpButtonType.Page, page);
					this.AddLabel(x + button.indent, y + 2, button.color, button.label);
				}
			}
			foreach (QuestDialogContent.PageEntry page in d.pages) {
				int pageNumber = numbers[page.name];
				this.AddPage(pageNumber);
				if (page.html != null) {
					this.AddHtml(170, 230, 450, 100, "<basefont color=#ffffff>" + page.html, false, true);
				}
				int rows = 0;
				foreach (QuestDialogContent.ResponseEntry response in page.responses) {
					int rowsNeeded = response.isLong ? 2 : 1;
					if (rows + rowsNeeded >= MaxRows) {
						int next = nextFreePage++;
						this.AddButton(Width - 15, Height + 160, 9907, 9908, 0, GumpButtonType.Page, next);
						this.AddPage(next);
						this.AddButton(Width - 15, ResponseY, 9901, 9902, 0, GumpButtonType.Page, pageNumber);
						pageNumber = next;
						rows = 0;
					}
					this.AddResponse(response, ResponseY + (rows * RowHeight), numbers);
					rows += rowsNeeded;
				}
			}
		}

		private void AddResponse(QuestDialogContent.ResponseEntry response, int y, Dictionary<string, int> numbers) {
			if (response.isExit) {
				this.AddButton(ResponseX, y, 0xfa5, 0xfa7, (response.target == null) ? 0 : this.ExitButtonId(response.target), GumpButtonType.Reply, 0);
			} else {
				int page;
				numbers.TryGetValue(response.target, out page);
				this.AddButton(ResponseX, y, 0xfa5, 0xfa7, 0, GumpButtonType.Page, page);
			}
			if (response.isLong) {
				this.AddHtml(ResponseX + 35, y, Width - 85, 2 * RowHeight, response.text, false, false);
			} else {
				this.AddLabel(ResponseX + 35, y, response.isEmote ? QuestDialogContent.ColorEmote : QuestDialogContent.ColorResponse, response.text);
			}
		}

		internal int ExitButtonId(string name) {
			int index = this.exitButtons.FindIndex(b => string.Equals(b, name, StringComparison.OrdinalIgnoreCase));
			if (index < 0) {
				this.exitButtons.Add(name);
				index = this.exitButtons.Count - 1;
			}
			return index + 1;
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			Player player = gi.Cont as Player;
			Character npc = args[0] as Character;
			int button = gr.PressedButton;
			if ((player == null) || (npc == null) || npc.IsDeleted || (button < 1) || (button > this.exitButtons.Count)) {
				return;
			}
			QuestDialogLoc loc = Loc<QuestDialogLoc>.Get(player.Language);
			if (Point2D.GetSimpleDistance(npc, player) > ReachDistance) {
				player.RedMessage(loc.TooFar); //Erebor's f_qReach
			} else if (player.Flag_Dead) {
				player.RedMessage(loc.NotAsAGhost); //Erebor's f_qIsDead
			} else {
				this.OnButton(this.exitButtons[button - 1], npc, player);
			}
		}
	}

	/// <summary>#833 - Erebor's f_qSpeech/f_qDClick and f_npcUpset, for NPCs with quest dialogs.</summary>
	public static class QuestNpc {
		public const int TalkDistance = 3;
		private const int BlessingEffect = 0x2ED0, SparkleEffect = 0x3779, SnowHue = 1153, BlackHue = 1;
		private static readonly TagKey upsetTillTK = TagKey.Acquire("_questNpcUpsetTill_");

		private static readonly string[] upsetSays = { "Nech me byt..", "Promin, nechci ted s tebou mluvit.", "Prosim nech me byt!", "Nemluv na me!", "Jdi odeme pryc!", "Odejdi..." };
		private static readonly string[] upsetEmotes = { "s tebou odmita mluvit", "te ignoruje", "je velmi zneklidnena a nekomunikuje" };

		/// <summary>The quests' items and NPCs carry their behavior in their own triggers, which the converter can't port:
		/// a quest's Init attaches its C# trigger groups to the converted defs with this.</summary>
		public static void Attach(string defname, TriggerGroup tg) {
			AbstractDefTriggerGroupHolder def = ThingDef.GetByDefname(defname) as AbstractDefTriggerGroupHolder;
			if (def == null) {
				Logger.WriteWarning("Quest: " + defname + " is missing, its quest behavior isn't attached.");
				return;
			}
			def.AddTriggerGroup(tg);
		}

		/// <summary>#852 - a new rank: Erebor's level-up celebration (experiences.scp), the gate sound, Manwe's blessing and a
		/// circle of sparkles, white for Gondor and black for everyone else.</summary>
		public static void CelebrateRank(Player player) {
			player.Sound((int) SoundNames.gatetrvl);
			EffectFactory.StationaryEffect(player, BlessingEffect, 0, 32);
			int hue = (player.Realm == Realm.Gondor) ? SnowHue : BlackHue;
			for (int dx = -2; dx <= 2; dx++) {
				for (int dy = -2; dy <= 2; dy++) {
					if ((Math.Abs(dx) == 2) || (Math.Abs(dy) == 2)) { //the 16 tiles around the player, as Erebor's do_a_cyrcle
						Point4D spot = new Point4D(player.X + dx, player.Y + dy, player.Z, player.M);
						EffectFactory.StationaryEffectAt(spot, SparkleEffect, 10, 30, true, false, hue, RenderModes.Opaque);
					}
				}
			}
		}

		/// <summary>Whether the player stands close enough to deal with the NPC; tells them if not.</summary>
		public static bool IsCloseEnough(Character npc, Character player) {
			if (Point2D.GetSimpleDistance(npc, player) > TalkDistance) {
				player.SysMessage(Loc<QuestDialogLoc>.Get(player.Language).TooFarToTalk);
				return false;
			}
			return true;
		}

		/// <summary>The NPC won't talk to anyone for this long; a shorter upset doesn't shorten a running one.</summary>
		public static void Upset(Character npc, int seconds) {
			double till = Globals.TimeAsSpan.TotalSeconds + seconds;
			object tag = npc.GetTag(upsetTillTK);
			if ((tag == null) || (Convert.ToDouble(tag, CultureInfo.InvariantCulture) < till)) {
				npc.SetTag(upsetTillTK, till);
			}
		}

		public static bool IsUpset(Character npc) {
			object tag = npc.GetTag(upsetTillTK);
			return (tag != null) && (Convert.ToDouble(tag, CultureInfo.InvariantCulture) > Globals.TimeAsSpan.TotalSeconds);
		}

		public static void ReactUpset(Character npc) {
			int roll = Globals.dice.Next(upsetSays.Length + upsetEmotes.Length);
			if (roll < upsetSays.Length) {
				npc.Say(upsetSays[roll]);
			} else {
				npc.Emote(upsetEmotes[roll - upsetSays.Length]);
			}
		}
	}

	public class QuestDialogLoc : CompiledLocStringCollection<QuestDialogLoc> {
		public string TooFar = "You are too far away!";
		public string NotAsAGhost = "You can't do this as a ghost!";
		public string TooFarToTalk = "They won't deal with you from that distance.";
	}
}
