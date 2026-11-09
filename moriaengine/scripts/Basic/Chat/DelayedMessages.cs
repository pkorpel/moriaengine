using System;
using System.Collections.Generic;
using System.Globalization;
using SteamEngine.Common;
using SteamEngine.CompiledScripts.Dialogs;

namespace SteamEngine.CompiledScripts {
	/// <summary>#901 - Erebor's delayedmessage and delayedredMessage (funkce.scp): someone online reads the line at once;
	/// someone offline finds it waiting, the important (red) ones apart from the rest. At login d_login_redmessages
	/// and d_login_messages (various_gumps.scp) list them, a click on one deletes it; .rms and .ms open them again,
	/// .clearms 1 (important) or 0 empties one. Kept as Erebor's tags: a count and one string a message.</summary>
	public static class DelayedMessages {
		internal const int PageRows = 25;

		private static readonly TagKey countTK = TagKey.Acquire("_delayed_messages_");
		private static readonly TagKey redCountTK = TagKey.Acquire("_delayed_red_messages_");

		/// <summary>delayedmessage</summary>
		public static void Send(Character to, string text) {
			Deliver(to, text, false);
		}

		/// <summary>delayedredMessage</summary>
		public static void SendRed(Character to, string text) {
			Deliver(to, text, true);
		}

		private static void Deliver(Character to, string text, bool red) {
			if (!(to is Player) || to.IsDeleted) {
				return;
			}
			if (to.IsOnline) {
				if (red) {
					to.RedMessage(text);
				} else {
					to.SysMessage(text);
				}
				return;
			}
			List<string> queue = Read(to, red);
			queue.Add(text);
			Write(to, red, queue);
		}

		public static int Count(Character who, bool red) {
			return Convert.ToInt32(who.GetTag(red ? redCountTK : countTK) ?? 0, CultureInfo.InvariantCulture);
		}

		internal static List<string> Read(Character who, bool red) {
			int count = Count(who, red);
			List<string> queue = new List<string>(count);
			for (int i = 1; i <= count; i++) {
				queue.Add(Convert.ToString(who.GetTag(MessageTK(red, i)), CultureInfo.InvariantCulture) ?? "");
			}
			return queue;
		}

		private static void Write(Character who, bool red, List<string> queue) {
			int old = Count(who, red);
			for (int i = queue.Count + 1; i <= old; i++) {
				who.RemoveTag(MessageTK(red, i));
			}
			for (int i = 0; i < queue.Count; i++) {
				who.SetTag(MessageTK(red, i + 1), queue[i]);
			}
			if (queue.Count > 0) {
				who.SetTag(red ? redCountTK : countTK, queue.Count);
			} else {
				who.RemoveTag(red ? redCountTK : countTK);
			}
		}

		private static TagKey MessageTK(bool red, int number) {
			return TagKey.Acquire((red ? "_delayed_red_message_" : "_delayed_message_") + number.ToString(CultureInfo.InvariantCulture) + "_");
		}

		/// <summary>The message at this index (0 based) is read and gone.</summary>
		internal static void Remove(Character who, bool red, int index) {
			List<string> queue = Read(who, red);
			if ((index >= 0) && (index < queue.Count)) {
				queue.RemoveAt(index);
				Write(who, red, queue);
			}
		}

		internal static void Clear(Character who, bool red) {
			Write(who, red, new List<string>());
		}

		/// <summary>@Login's redmessage_me and message_me.</summary>
		internal static void ShowAtLogin(Player player) {
			if (Count(player, true) > 0) {
				D_DelayedRedMessages.Show(player, 0);
			}
			if (Count(player, false) > 0) {
				D_DelayedNormalMessages.Show(player, 0);
			}
		}

		/// <summary>.ms</summary>
		[SteamFunction]
		public static void Ms(Character self) {
			if (self is Player player) {
				D_DelayedNormalMessages.Show(player, 0);
			}
		}

		/// <summary>.rms</summary>
		[SteamFunction]
		public static void Rms(Character self) {
			if (self is Player player) {
				D_DelayedRedMessages.Show(player, 0);
			}
		}

		/// <summary>.clearms 1 empties the important messages, .clearms 0 (or nothing) the others.</summary>
		[SteamFunction]
		public static void ClearMs(Character self, ScriptArgs sa) {
			bool red = (sa != null) && (sa.Args.Trim().Trim('"') == "1");
			Clear(self, red);
			self.SysMessage((red ? "Dulezite " : "Normalni ") + "zpravy vycisteny");
		}
	}

	/// <summary>d_login_messages / d_login_redmessages: 25 messages a page, a click on one deletes it, arrows for the pages.
	/// Two sealed dialogs over one base, so both can be open at once (SingletonScript matches any subclass, #674).</summary>
	public abstract class D_DelayedQueue : CompiledGumpDef {
		internal const int Cancel = 0, Previous = 1, Next = 2, MessageBase = 10000;
		private const int Width = 620, Row = 20, TextHue = 955;

		protected abstract bool Red { get; }

		protected static void Show<T>(Player player, int first) where T : D_DelayedQueue {
			D_DelayedQueue dialog = SingletonScript<T>.Instance;
			player.DialogForget(dialog);
			if (DelayedMessages.Count(player, dialog.Red) == 0) {
				player.SysMessage("Zadne zpravy k zobrazeni");
				return;
			}
			player.Dialog(player, dialog, new DialogArgs(first));
		}

		protected abstract void ShowAgain(Player player, int first);

		public override void Construct(Thing focus, AbstractCharacter sendTo, DialogArgs args) {
			List<string> queue = DelayedMessages.Read((Character) sendTo, this.Red);
			int first = Math.Max(0, Math.Min((int) args[0], Math.Max(0, queue.Count - 1)));
			int shown = Math.Min(DelayedMessages.PageRows, queue.Count - first);
			int height = 70 + (Math.Max(shown, 1) * Row);
			this.SetLocation(30, 30);
			this.AddPage(0);
			this.AddBackground(0, 0, Width, height, 2620);
			this.AddLabel(14, 12, this.Red ? (int) Hues.Red : (int) Hues.Blue, (this.Red ? "Nove dulezite zpravy: " : "Nove zpravy: ") + queue.Count);
			this.AddButton(Width - 40, 10, 0xFB1, 0xFB3, Cancel, GumpButtonType.Reply, 0);
			if (first > 0) {
				this.AddButton(Width - 70, 12, 0xFA, 0xFB, Previous, GumpButtonType.Reply, 0);
			}
			int y = 40;
			for (int i = first; i < first + shown; i++) {
				this.AddButton(14, y, 4005, 4007, MessageBase + i, GumpButtonType.Reply, 0); //a click deletes it, as Erebor's
				this.AddLabelCropped(50, y, Width - 64, Row, TextHue, queue[i]);
				y += Row;
			}
			if (first + shown < queue.Count) {
				this.AddButton(Width - 40, height - 30, 0xFC, 0xFD, Next, GumpButtonType.Reply, 0);
			}
		}

		public override void OnResponse(Gump gi, GumpResponse gr, DialogArgs args) {
			if (!(gi.Cont is Player player)) {
				return;
			}
			int first = (int) args[0];
			int button = gr.PressedButton;
			if (button == Previous) {
				this.ShowAgain(player, Math.Max(0, first - DelayedMessages.PageRows));
			} else if (button == Next) {
				this.ShowAgain(player, first + DelayedMessages.PageRows);
			} else if (button >= MessageBase) {
				DelayedMessages.Remove(player, this.Red, button - MessageBase);
				int left = DelayedMessages.Count(player, this.Red);
				if (left > 0) {
					this.ShowAgain(player, (left > first) ? first : Math.Max(0, first - DelayedMessages.PageRows)); //a page back when this one emptied
				}
			}
		}
	}

	public sealed class D_DelayedNormalMessages : D_DelayedQueue {
		protected override bool Red => false;

		internal static void Show(Player player, int first) {
			Show<D_DelayedNormalMessages>(player, first);
		}

		protected override void ShowAgain(Player player, int first) {
			Show(player, first);
		}
	}

	public sealed class D_DelayedRedMessages : D_DelayedQueue {
		protected override bool Red => true;

		internal static void Show(Player player, int first) {
			Show<D_DelayedRedMessages>(player, first);
		}

		protected override void ShowAgain(Player player, int first) {
			Show(player, first);
		}
	}
}
