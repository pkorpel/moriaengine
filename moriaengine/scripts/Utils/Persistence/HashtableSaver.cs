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
using System.Collections;
using SteamEngine.Persistence;

namespace SteamEngine.CompiledScripts {
	public sealed class HashtableSaver : ISaveImplementor, IDeepCopyImplementor {
		public string HeaderName { get {
			return "Hashtable";
		} }

		public Type HandledType { get {
			return typeof(Hashtable);
		} }
		
		public void Save(object objToSave, SaveStream writer) {
			Hashtable table = (Hashtable) objToSave;
			int count = table.Count;
			writer.WriteValue("count", count);
			int i = 0;
			foreach (DictionaryEntry entry in table) {
				writer.WriteValue(i+".K", entry.Key);
				writer.WriteValue(i+".V", entry.Value);
				i++;
			}
		}
		
		public object LoadSection(PropsSection input) {
			int currentLineNumber = input.HeaderLine;
			try {
				PropsLine countLine = input.PopPropsLine("count");
				currentLineNumber = countLine.Line;
				int count = int.Parse(countLine.Value);
				Hashtable table = new Hashtable(count);
				for (int i = 0; i<count; i++) {
					PropsLine keyLine = input.PopPropsLine(i+".K");
					PropsLine valueLine = input.PopPropsLine(i+".V");
					HashtableLoadHelper helper = new HashtableLoadHelper(table);
					currentLineNumber = keyLine.Line;
					ObjectSaver.Load(keyLine.Value, helper.DelayedLoad_Key, input.Filename, keyLine.Line);
					currentLineNumber = valueLine.Line;
					ObjectSaver.Load(valueLine.Value, helper.DelayedLoad_Value, input.Filename, valueLine.Line);
				}
				return table;
			} catch (FatalException) {
				throw;
			} catch (SEException sex) {
				sex.TryAddFileLineInfo(input.Filename, currentLineNumber);
				throw;
			} catch (Exception e) {
				throw new SEException(input.Filename, currentLineNumber, e);
			}
		}
		
		private class HashtableLoadHelper {
			internal Hashtable table;
			private object key;
			private object value;
			private bool valueSet;
			private bool keySet;


			internal HashtableLoadHelper(Hashtable table) {
				this.table = table;
			}

			private void TryInsertIntoTable() {
				if (this.valueSet && this.keySet) {
					this.table[this.key] = this.value;
				}
			}

			internal void DelayedLoad_Value(object loadedObj, string filename, int line) {
				this.value = loadedObj;
				this.valueSet = true;
				this.TryInsertIntoTable();
			}

			internal void DelayedLoad_Key(object loadedObj, string filename, int line) {
				this.key = loadedObj;
				this.keySet = true;
				this.TryInsertIntoTable();
			}

			internal void DelayedCopy_Value(object loadedObj) {
				this.value = loadedObj;
				this.valueSet = true;
				this.TryInsertIntoTable();
			}

			internal void DelayedCopy_Key(object loadedObj) {
				this.key = loadedObj;
				this.keySet = true;
				this.TryInsertIntoTable();
			}
		}

		public object DeepCopy(object copyFrom) {
			Hashtable copyFromTable = (Hashtable) copyFrom;
			Hashtable newTable = new Hashtable();

			foreach (DictionaryEntry entry in copyFromTable) {
				HashtableLoadHelper helper = new HashtableLoadHelper(newTable);
				DeepCopyFactory.GetCopyDelayed(entry.Key, helper.DelayedCopy_Key);
				DeepCopyFactory.GetCopyDelayed(entry.Value, helper.DelayedCopy_Value);
			}
			return newTable;
		}
	}
}