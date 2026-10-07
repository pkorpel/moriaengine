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

using System.Collections.Generic;
using SteamEngine.Networking;

namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>Class encapsulating stacking an recalling dialogs</summary>
	public static class DialogStacking {
		private static readonly Dictionary<GameState, Dictionary<int, Stack<Gump>>> globalDialogStacksCache = new Dictionary<GameState, Dictionary<int, Stack<Gump>>>();

		/// <summary>Tag used for stacking information about clients dialogs to be called back</summary>
		private static readonly TagKey dialogStackTKa = TagKey.Acquire("_guta_dialogStackTree_");

		/// <summary>LSCript used method for setting the specified argument's value</summary>
		public static void SetArgValue(Gump gi, int index, object value) {
			gi.InputArgs[index] = value;
		}

		/// <summary>LSCript used method for getting the specified argument's value</summary>
		public static object GetArgValue(Gump gi, int index) {
			return gi.InputArgs[index];
		}

		//get the DialogStack for the given gamestate from the global dialog stacks cache
		private static Dictionary<int, Stack<Gump>> AcquireDialogsDictionary(GameState forWho) {
			Dictionary<int, Stack<Gump>> dlgStack;
			if (globalDialogStacksCache.TryGetValue(forWho, out dlgStack)) {
				return dlgStack;
			}
			dlgStack = new Dictionary<int, Stack<Gump>>();
			globalDialogStacksCache[forWho] = dlgStack;
			return dlgStack;
		}

		/// <summary>
		/// Store the info about the dialog to the dialog stack. Used for 
		/// storing info about the last used dialog which can be used to reopen the dialog 
		/// when exiting from the following dialog for instance.
		/// e.g. info dialog -> clicked on the 'account info' button which closes the info dialog,
		/// after finishing with the accout info dialog we would like to have the info dialog reopened!
		/// UPDATED - using Gump as a parameter only (it contains everything necessary)
		/// UPDATED2 - oldGi - dialog being enstacked; newGi - newly opened dialog for storing stacking info
		/// </summary>
		public static void EnstackDialog(Gump oldGi, Gump newGi) {
			/*Dictionary<int, Stack<Gump>> dialogsMultiStack = oldGi.Cont.GetTag(dialogStackTK) as Dictionary<int, Stack<Gump>>;
			if (dialogsMultiStack == null) { //zatim nemame ani stack
				dialogsMultiStack = new Dictionary<int, Stack<Gump>>();
				oldGi.Cont.SetTag(dialogStackTK, dialogsMultiStack);
			}*/
			Dictionary<int, Stack<Gump>> dialogsMultiStack = AcquireDialogsDictionary(oldGi.Cont.GameState);
			Stack<Gump> actualStack;
			if (!dialogsMultiStack.TryGetValue(oldGi.Uid, out actualStack)) {
				actualStack = new Stack<Gump>(); //vytvortit a ulozit
				dialogsMultiStack[oldGi.Uid] = actualStack;
			}
			actualStack.Push(oldGi); //stackneme si starej dialog
			dialogsMultiStack[newGi.Uid] = actualStack; //dat k dispozici novemu dialogu pro pozdejsi navraty
		}

		/// <summary>
		/// Recall the last stored dialog from the dialog stack.
		/// Find it according to the given actual Gump in the dialogs multistack.
		/// Handles also emptying and clearing unused (empty) stacks and the multistack too.
		/// </summary>
		public static Gump PopStackedDialog(Gump actualGi) {
			//Dictionary<int, Stack<Gump>> dialogsMultiStack = actualGi.Cont.GetTag(dialogStackTK) as Dictionary<int, Stack<Gump>>;
			Dictionary<int, Stack<Gump>> dialogsMultiStack;
			Gump sgi = null;
			//if (dialogsMultiStack != null) {
			if (globalDialogStacksCache.TryGetValue(actualGi.Cont.GameState, out dialogsMultiStack)) { //something was stored
				Stack<Gump> actualsOwnStack;
				if (dialogsMultiStack.TryGetValue(actualGi.Uid, out actualsOwnStack)) {
					if (actualsOwnStack.Count != 0) {
						sgi = actualsOwnStack.Pop(); //popovat budem jen pokud ve stacku vubec neco je
						dialogsMultiStack.Remove(actualGi.Uid); //po popnuti uz muzeme vyhodit soucasny dialog z multistacku (uz ho stejne zaviram...)					
					}

					if (actualsOwnStack.Count == 0) {//stack je uz prazdnej (nebo byl prazdnej i predtim), odstranit stack
						dialogsMultiStack.Remove(actualGi.Uid);
						if (dialogsMultiStack.Count == 0) {
							globalDialogStacksCache.Remove(actualGi.Cont.GameState); //multistack je prazdnej, uvolnit
							//actualGi.Cont.RemoveTag(dialogStackTK);
						}
					}
				}
			}
			return sgi;
		}

		/// <summary>
		/// When we need to resend the actual dialog (e.g. only with changed parameters such as going to another page or 
		/// refrehsing some list where we need the actual data to be refreshed, we need the complete initialization phase 
		/// to be performed - we will resend the dialog in a standard way but we have to make sure the stacked information 
		/// remains valid => we will store the new (resent) gumpinstance with the same stacking info as the old one 
		/// This is necesasry since the resent dialog has a different uid than its older form
		/// </summary>
		public static void ResendAndRestackDialog(Gump oldInstance) {
			Gump newInstance = oldInstance.Cont.SendGump(
				oldInstance.Focus, oldInstance.Def, oldInstance.InputArgs);//resend
			RenewStackedDialog(oldInstance, newInstance);//and restack
		}

		/// <summary>
		/// After resending the gump to the player, we have a new instance with a new UID - we need to
		/// update the stacked information in the multistack - assign the stack info to the new UID
		/// </summary>
		private static void RenewStackedDialog(Gump oldInstance, Gump resentInstance) {
			//Dictionary<int, Stack<Gump>> dialogsMultiStack = oldInstance.Cont.GetTag(dialogStackTK) as Dictionary<int, Stack<Gump>>;
			Dictionary<int, Stack<Gump>> dialogsMultiStack;
			//if (dialogsMultiStack != null) {
			if (globalDialogStacksCache.TryGetValue(oldInstance.Cont.GameState, out dialogsMultiStack)) { //something was stored
				Stack<Gump> storedStack;
				if (dialogsMultiStack.TryGetValue(oldInstance.Uid, out storedStack)) {
					//store the stack back to the multistack but now under the new UID!
					dialogsMultiStack[resentInstance.Uid] = storedStack;
					dialogsMultiStack.Remove(oldInstance.Uid); //the old dialog has been discarded
				}
			}
		}

		/// <summary>For possible clearing of the stacked dialogs (if needed)</summary>
		public static void ClearDialogStack(AbstractCharacter fromWho) {
			//fromWho.RemoveTag(dialogStackTK);
			globalDialogStacksCache.Remove(fromWho.GameState);

		}

		/// <summary>For displaying the previously stored dialog (if any)</summary>
		public static void ShowPreviousDialog(Gump actualGi) {
			Gump sgi = PopStackedDialog(actualGi);
			if (sgi != null) {
				ResendAndRestackDialog(sgi); //we also have to alter stack information about the resent dialog...
			}
		}
	}
}