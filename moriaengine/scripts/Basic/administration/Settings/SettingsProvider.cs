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
using SteamEngine.Common;
using SteamEngine.Persistence;
using SteamEngine.Regions;

namespace SteamEngine.CompiledScripts.Dialogs {
	/// <summary>
	/// Class for accepting and setting all values edited in the info or settings dialogs
	/// </summary>
	public static class SettingsProvider {
		//sotre the used saveable prefixes for particular Types
		private static Dictionary<Type, string> prefixTypes = new Dictionary<Type, string>();

		/// <summary>
		/// Try to store all edited (changed) fields from the dialog. Store the results in the special list that will be returned for 
		/// displaying
		/// </summary>
		public static List<SettingResult> AssertSettings(Dictionary<int, IDataFieldView> editFields, GumpResponse resp, object target) {
			List<SettingResult> resList = new List<SettingResult>();
			SettingResult oneRes = null;
			foreach (int key in editFields.Keys) {
				IDataFieldView field = editFields[key];
				oneRes = new SettingResult(field, target); //prepare the result
				string newStringValue = resp.GetTextResponse(key); //get the value from the edit field
				if (!typeof(Enum).IsAssignableFrom(field.FieldType) && !newStringValue.Equals(field.GetStringValue(target))) {
					//hnadled type is not Enum and it has changed somehow...
					oneRes.Outcome = SettingsEnums.ChangedOK; //assume it will be OK
					//something has changed - try to make the setting
					try {
						field.SetStringValue(target, newStringValue);
					} catch {
						//setting failed for some reason
						oneRes.Outcome = SettingsEnums.ChangedError; //it wasnt OK... :-(
						//store the attempted string
						oneRes.ErroneousValue = newStringValue;
					}
					resList.Add(oneRes);//add to the list (only if changed somehow)
				} else if (typeof(Enum).IsAssignableFrom(field.FieldType) && IsEnumValueChanged(field, target, newStringValue)) {
					//if the field handles the Enum type, we have to check it a slightly different way...
					oneRes.Outcome = SettingsEnums.ChangedOK;
					try {
						//try to set the enumeration value
						field.SetValue(target, Enum.Parse(field.FieldType, newStringValue, true));
					} catch {
						oneRes.Outcome = SettingsEnums.ChangedError;
						oneRes.ErroneousValue = newStringValue;
					}
					resList.Add(oneRes);//add to the list (only if changed somehow)
				} else {
					//nothing changed
					oneRes.Outcome = SettingsEnums.NotChanged;
				}
			}
			return resList;
		}

		/// <summary>Look on the outcome value and return the correct color for the dialog</summary>
		public static Hues ResultColor(SettingResult sres) {
			switch (sres.Outcome) {
				case SettingsEnums.ChangedError:
					return Hues.SettingsFailedColor;
				case SettingsEnums.ChangedOK:
					return Hues.SettingsCorrectColor;
				default:
					return Hues.SettingsNormalColor; //but thos won't happen :)
			}
		}

		/// <summary>Count all successfully edited fields</summary>
		public static int CountSuccessfulSettings(List<SettingResult> results) {
			int resCntr = 0;
			foreach (SettingResult sres in results) {
				if (sres.Outcome == SettingsEnums.ChangedOK) {
					resCntr++;
				}
			}
			return resCntr;
		}

		/// <summary>Count all failed fields</summary>
		public static int CountUnSuccessfulSettings(List<SettingResult> results) {
			int resCntr = 0;
			foreach (SettingResult sres in results) {
				if (sres.Outcome == SettingsEnums.ChangedError) {
					resCntr++;
				}
			}
			return resCntr;
		}

		/// <summary>Compare case insensitively the names of enumeration values if they have changed</summary>
		private static bool IsEnumValueChanged(IDataFieldView field, object target, string newEnumValueName) {
			string oldEnumValuName = Enum.GetName(field.FieldType, field.GetValue(target));
			return !StringComparer.OrdinalIgnoreCase.Equals(oldEnumValuName, newEnumValueName);
		}

		/// <summary>
		/// Examine the setings value's member type and get its prefix as used in ObjectSaver.
		/// We will use it in the info/settings dialog for displaying and identification
		/// </summary>
		public static string GetValuePrefix(IDataFieldView field, object target) {
			object value = field.GetValue(target);
			string valuePrefix = "";

			Type t = value.GetType();
			//we will store it in the special dictionary
			if (!prefixTypes.TryGetValue(t, out valuePrefix)) {
				//types like Enum, Numbers, String, Regions  or Globals doesn't have any prefixes, they will be displayed as is
				if (t.IsEnum || ConvertTools.IsNumberType(t) || t.Equals(typeof(string))
					|| typeof(Region).IsAssignableFrom(t) || value == Globals.Instance) {
				} else if (typeof(Thing).IsAssignableFrom(t)) {
					valuePrefix = "#";
				} else if (typeof(AbstractAccount).IsAssignableFrom(t)) {
					valuePrefix = "$";
				} else if (typeof(AbstractScript).IsAssignableFrom(t)) {
					valuePrefix = "#";
				} else {
					//try the simpleimplementors
					ISimpleSaveImplementor iss = ObjectSaver.GetSimpleSaveImplementorByType(t);
					if (iss != null) {
						valuePrefix = iss.Prefix;
					} else {
						valuePrefix = t.Name; //this is the final possibility
					}
				}

				//store the prefix in the dictionary
				prefixTypes[t] = valuePrefix;
			}
			return valuePrefix;
		}

		/// <summary>
		/// Get the settings values prefix and surround it by brackets.
		/// If the value has no prefix (it is of some special type, find out what
		/// type it is and return some description of it
		/// </summary>
		public static string GetTypePrefix(Type t)
		{
			if (t.Equals(typeof(object))) {
				//object is also possible ! - the member can be set to any value
				return "(Obj)";
			}
			if (t.IsEnum) {
				return "(Enum)";
			}
			if (ConvertTools.IsNumberType(t)) {
				return "(Num)";
			}
			if (t.Equals(typeof(string))) {
				return "(Str)";
			}
			if (typeof(Region).IsAssignableFrom(t)) {
				return "(Reg)";
			}
			if (typeof(Thing).IsAssignableFrom(t)) {
				return "(Thg)";
			}
			if (typeof(AbstractAccount).IsAssignableFrom(t)) {
				return "(Acc)";
			}
			if (typeof(AbstractScript).IsAssignableFrom(t)) {
				return "(Scp)";
			}
			if (t.Equals(typeof(Globals))) {
				return "(Glob)";
			}
			//nothing special, try the simpleimplementors
			ISimpleSaveImplementor iss = ObjectSaver.GetSimpleSaveImplementorByType(t);
			if (iss != null) {
				string pref = iss.Prefix;
				if (pref.Contains("(")) {
					return pref; //it already contains the brackets
				} //add the surrounding brackets
				return "(" + pref + ")";
			}
			return "(" + t.Name + ")"; //this is the final desperate possibility
		}
	}

	/// <summary>
	/// Class carrying information about one edit field - its former value, its value after setting and 
	/// the setting result value (success/fail)
	/// </summary>
	public class SettingResult {
		//former value of the field - before settings
		private object formerValue;
		//the EditableField itself (there will also be a newly set value, if successfull)
		private IDataFieldView field;
		//the target object (the one which field we are dealing with)
		private object target;

		//was the setting succesfull/erroneous or even not provided at all ? 
		private SettingsEnums outcome;
		//here will be the value we tried to set and it resulted in some error
		private string erroneousValue;

		public SettingResult(IDataFieldView field, object target) {
			this.field = field;
			this.target = target;
			this.formerValue = field.GetValue(target);
		}

		/// <summary>What is the result of the setting?</summary>
		public SettingsEnums Outcome {
			get {
				return this.outcome;
			}
			set {
				this.outcome = value;
			}
		}

		/// <summary>Get the name from the IDataFieldView</summary>
		public string Name {
			get {
				return this.field.GetName(this.target);
			}
		}

		/// <summary>The value attempted to store which resluted in error - filled only in case of error :)</summary>
		public string ErroneousValue {
			get {
				//if the setting is OK, then return an empty string
				return this.erroneousValue == null ? "" : this.erroneousValue;
			}
			set {
				this.erroneousValue = value;
			}
		}

		/// <summary>Get the former value of the edited field - for comparation with the result</summary>
		public string FormerValue {
			get
			{
				//Handle the Enums differently (show the name, not the value...)
				if (typeof(Enum).IsAssignableFrom(this.field.FieldType)) {
					return Enum.GetName(this.field.FieldType, this.formerValue);
				}
				return ObjectSaver.Save(this.formerValue);
			}
			set {
				this.formerValue = value;
			}
		}

		/// <summary>The actual value of the field - after the setting is made</summary>
		public string CurrentValue {
			get
			{
				//Handle the Enums differently (show the name, not the value...)				
				if (typeof(Enum).IsAssignableFrom(this.field.FieldType)) {
					return Enum.GetName(this.field.FieldType, this.field.GetValue(this.target));
				}
				return this.field.GetStringValue(this.target);
			}
		}
	}
}