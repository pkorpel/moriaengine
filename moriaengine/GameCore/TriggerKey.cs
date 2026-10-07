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
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using SteamEngine.Persistence;

namespace SteamEngine {

	/// <summary>
	/// TriggerKeys are used when calling triggers. You should call Get(name) once to get a TriggerKey, and then use
	/// that from then on for calling that trigger.
	/// </summary>
	public sealed class TriggerKey : AbstractKey<TriggerKey> {
		private TriggerKey(string name, int uid)
			: base(name, uid) {
		}

		public static TriggerKey Acquire(string name) {
			return Acquire(name, (n, u) => new TriggerKey(n, u));
		}


		//Triggers defined as fields for faster access (Won't have to look up the string every time)

		public static readonly TriggerKey command = Acquire("command");

		//public static readonly TriggerKey fastWalk = Get("fastWalk");

		public static readonly TriggerKey startup = Acquire("startup");
		public static readonly TriggerKey shutdown = Acquire("shutdown");

		public static readonly TriggerKey beforeSave = Acquire("beforeSave");
		public static readonly TriggerKey openSaveStream = Acquire("openSaveStream");
		public static readonly TriggerKey afterSave = Acquire("afterSave");
		public static readonly TriggerKey beforeLoad = Acquire("beforeLoad");
		public static readonly TriggerKey openLoadStream = Acquire("openLoadStream");
		public static readonly TriggerKey afterLoad = Acquire("afterLoad");

		public static readonly TriggerKey step = Acquire("step");
		public static readonly TriggerKey itemStep = Acquire("itemStep");

		public static readonly TriggerKey login = Acquire("login");
		public static readonly TriggerKey logout = Acquire("logout");

		public static readonly TriggerKey assign = Acquire("assign");
		public static readonly TriggerKey unAssign = Acquire("unAssign");

		//public static readonly TriggerKey newPC = Get("newPC");

		public static readonly TriggerKey newPosition = Acquire("newPosition");

		public static readonly TriggerKey itemLeave = Acquire("itemLeave");
		public static readonly TriggerKey leaveItem = Acquire("leaveItem");
		public static readonly TriggerKey leaveChar = Acquire("leaveChar");
		public static readonly TriggerKey leaveRegion = Acquire("leaveRegion");

		public static readonly TriggerKey splitFromStack = Acquire("splitFromStack");

		public static readonly TriggerKey itemEnter = Acquire("itemEnter");
		public static readonly TriggerKey enterItem = Acquire("enterItem");
		public static readonly TriggerKey enterChar = Acquire("enterChar");
		public static readonly TriggerKey enterRegion = Acquire("enterRegion");

		public static readonly TriggerKey stackOnItem = Acquire("stackOnItem");
		public static readonly TriggerKey itemStackOn = Acquire("itemStackOn");

		public static readonly TriggerKey denyPickup = Acquire("denyPickup");
		public static readonly TriggerKey denyPickupItem = Acquire("denyPickupItem");
		public static readonly TriggerKey denyPickupItemFrom = Acquire("denyPickupItemFrom");

		public static readonly TriggerKey denyPutOnGround = Acquire("denyPutOnGround");
		public static readonly TriggerKey denyPutItemOnGround = Acquire("denyPutItemOnGround");
		public static readonly TriggerKey denyPutItemOn = Acquire("denyPutItemOn");

		public static readonly TriggerKey denyPutInItem = Acquire("denyPutInItem");
		public static readonly TriggerKey denyPutItemInItem = Acquire("denyPutItemInItem");
		public static readonly TriggerKey denyPutItemIn = Acquire("denyPutItemIn");

		public static readonly TriggerKey putItemOn = Acquire("putItemOn");
		public static readonly TriggerKey putOnItem = Acquire("putOnItem");

		public static readonly TriggerKey putOnChar = Acquire("putOnChar");
		public static readonly TriggerKey putItemOnChar = Acquire("putItemOnChar");

		public static readonly TriggerKey denyEquipOnChar = Acquire("denyEquipOnChar");
		public static readonly TriggerKey denyEquip = Acquire("denyEquip");

		//public static readonly TriggerKey playDropSound = Get("playDropSound");

		public static readonly TriggerKey destroy = Acquire("Destroy");

		public static readonly TriggerKey itemEquip = Acquire("itemEquip");
		public static readonly TriggerKey equip = Acquire("Equip");
		public static readonly TriggerKey itemUnEquip = Acquire("itemUnEquip");
		public static readonly TriggerKey unEquip = Acquire("UnEquip");
		public static readonly TriggerKey hear = Acquire("hear");
		public static readonly TriggerKey say = Acquire("say");

		public static readonly TriggerKey create = Acquire("create");
		public static readonly TriggerKey dupe = Acquire("dupe");

		public static readonly TriggerKey charDClick = Acquire("charDClick");
		public static readonly TriggerKey itemDClick = Acquire("itemDClick");
		public static readonly TriggerKey dClick = Acquire("DClick");
		//public static readonly TriggerKey denyCharDClick = Get("denyCharDClick");
		public static readonly TriggerKey denyItemDClick = Acquire("denyItemDClick");
		public static readonly TriggerKey denyDClick = Acquire("denyDClick");

		public static readonly TriggerKey charClick = Acquire("charClick");
		public static readonly TriggerKey itemClick = Acquire("itemClick");
		public static readonly TriggerKey click = Acquire("Click");

		public static readonly TriggerKey aosClick = Acquire("aosClick");

		public static readonly TriggerKey enter = Acquire("enter");//region trigger
		public static readonly TriggerKey exit = Acquire("exit");//character exiting/entering a region. can be cancellable...
		public static readonly TriggerKey clientAttach = Acquire("ClientAttach");

		public static readonly TriggerKey containerOpen = Acquire("containerOpen");

		public static readonly TriggerKey buildAosToolTips = Acquire("buildAosToolTips");
	}


	public sealed class TriggerKeySaveImplementor : ISimpleSaveImplementor {
		private static Regex re = new Regex(@"^\@(?<value>.+)\s*$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Compiled);

		public Type HandledType {
			get {
				return typeof(TriggerKey);
			}
		}

		public Regex LineRecognizer {
			get {
				return re;
			}
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public string Save(object objToSave) {
			return "@" + ((TriggerKey) objToSave).Name;
		}

		[SuppressMessage("Microsoft.Design", "CA1062:ValidateArgumentsOfPublicMethods")]
		public object Load(Match match) {
			return TriggerKey.Acquire(match.Groups["value"].Value);
		}

		public string Prefix {
			get {
				return "@";
			}
		}
	}
}