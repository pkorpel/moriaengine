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

//using System;
//using System.Net;
//using System.Net.Sockets;
//using System.Text;
//using System.IO;
//using System.Collections;
//using System.Configuration;
//using System.Reflection;
//using System.Globalization;
//using System.Diagnostics;
//using SteamEngine.Common;
//using SteamEngine;

/*

Once again, I made something and then I generalized it. Why? Because the performance counter stuff in
.NET simply doesn't want to do what I want it to do. And it's a PITA to use, too. This, on the other hand,
should be easy, if I do this well.

This expects inputs to be of type 'long' - You can measure amounts, or you can measure ticks. If you specify
a typeclass of M the ticks will be converted to milliseconds. Stick a number after the M and it rounds to
that precision. Example:
statsSyncIn.Value("M5","Time taken to receive");

As for rates:
statsSyncOut.Rate("Time blocked","bytes","Milliseconds that we were blocked per byte sent. (less is better)");
That's Rate(var1's name, var2's name, description)

*/
//namespace SteamEngine {
//    public class Statistics {
//        public static bool StatisticsTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Statistics Trace Messages"]);

//        private long minEntries = 20;
//        private long maxEntries = 500;
//        private long discardEntries = 5;
//        private string className;
//        private ArrayList statTypes = new ArrayList();
//        /**
//        The default minimum number of entries needed before statistics can be shown.
//        */
//        public long MinEntries {
//            get {
//                return minEntries;
//            }
//            set {
//                Sanity.IfTrueThrow(value < 0, "Cannot set MinEntries to be < 0.");
//                Sanity.IfTrueThrow(value > maxEntries, "Cannot set MinEntries to be > MaxEntries (" + maxEntries + "). (Tried setting it to " + value + ")");
//                minEntries = value;
//            }
//        }
//        /**
//        The default maximum number of entries - after this many exist, new ones will overwrite old ones.
//        */
//        public long MaxEntries {
//            get {
//                return maxEntries;
//            }
//            set {
//                Sanity.IfTrueThrow(value < 0, "Cannot set MaxEntries to be < 0.");
//                Sanity.IfTrueThrow(value < minEntries, "Cannot set MaxEntries to be < MinEntries (" + minEntries + "). (Tried setting it to " + value + ")");
//                maxEntries = value;
//            }
//        }
//        /**
//        The default number of entries to discard initially for each new StatType.
//        */
//        public long DiscardEntries {
//            get {
//                return discardEntries;
//            }
//            set {
//                Sanity.IfTrueThrow(value < 0, "Cannot set DiscardEntries to be < 0.");
//                discardEntries = value;
//            }
//        }
//        /**
//        The name designated for this instance of Statistics.
//        */
//        public string Name {
//            get {
//                return className;
//            }
//        }
//        /**
//        Creates a new Statistics object with the specified name.
//        */
//        public Statistics(string className) {
//            this.className = className;
//        }

//        /**
//        Adds a new StatType of the specified name, and returns it.
//        */
//        public StatType AddType(string name) {
//            StatType type = new StatType(this, name);
//            statTypes.Add(type);
//            return type;
//        }
//        /**
//        Shows statistics for this instance's StatTypes.
//        */
//        public void ShowAllStats(bool showRaw, bool showRates) {
//            foreach (StatType type in statTypes) {
//                type.ShowStats(showRaw, showRates);
//            }
//        }
//    }

//    /**
//        Represents a single type - For instance, GameConn has one for asyncronous reading, one for
//        asynchronous writing, one for syncronous reading, and one for synchronous writing.
//        */
//    public class StatType {
//        public static bool StatisticsTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Statistics Trace Messages"]);

//        private Statistics boss;
//        private string name;
//        private long minEntries = 0;
//        private long maxEntries = 0;
//        private long discardEntries = 0;
//        private long numEntries = 0;
//        private long curEntry = 0;
//        private StatEntry[] entries;
//        private bool noWarnOnMissing = false;
//        private Hashtable keys = new Hashtable(StringComparer.OrdinalIgnoreCase);
//        private Hashtable types = new Hashtable(StringComparer.OrdinalIgnoreCase);
//        private Hashtable minVar = new Hashtable(StringComparer.OrdinalIgnoreCase);
//        private Hashtable minAmt = new Hashtable(StringComparer.OrdinalIgnoreCase);
//        private ArrayList rates = new ArrayList();
//        private ArrayList notes = new ArrayList();
//        private ArrayList keysInOrder = new ArrayList();
//        //private ArrayList ratebounds = new ArrayList();

//        /**
//        Setting this will suppress warnings when you add an entry without having first declared
//        the variables here (with Value). Normally you would be warned, in order to give you a hint
//        if you accidentally misspelled something somewhere.
//        */
//        public bool NoWarnOnMissing {
//            get {
//                return noWarnOnMissing;
//            }
//            set {
//                noWarnOnMissing = value;
//            }
//        }
//        /**
//        The current number of entries.
//        */
//        public long NumEntries {
//            get {
//                return numEntries;
//            }
//            set {
//                Sanity.IfTrueThrow(value <= 0, "Cannot set NumEntries to be <= 0. (Tried setting it to " + value + ")");
//                numEntries = value;
//            }
//        }
//        /**
//        The minimum number of entries needed before statistics can be shown.
//        */
//        public long MinEntries {
//            get {
//                return minEntries;
//            }
//            set {
//                Sanity.IfTrueThrow(value < 0, "Cannot set MinEntries to be < 0.");
//                Sanity.IfTrueThrow(value > maxEntries, "Cannot set MinEntries to be > MaxEntries (" + maxEntries + "). (Tried setting it to " + value + ")");
//                minEntries = value;
//            }
//        }
//        /**
//        The maximum number of entries needed before statistics can be shown.
//        */
//        public long MaxEntries {
//            get {
//                return maxEntries;
//            }
//            set {
//                Sanity.IfTrueThrow(value < 0, "Cannot set MaxEntries to be < 0.");
//                Sanity.IfTrueThrow(value < minEntries, "Cannot set MaxEntries to be < MinEntries (" + minEntries + "). (Tried setting it to " + value + ")");
//                maxEntries = value;
//            }
//        }
//        /**
//        The index # that the next entry will be placed in.
//        */
//        public long CurEntry {
//            get {
//                return curEntry;
//            }
//        }
//        /**
//        The number of entries to discard. This decreases once when an entry is discarded, until it is 0.
//        */
//        public long DiscardEntries {
//            get {
//                return discardEntries;
//            }
//            set {
//                Sanity.IfTrueThrow(value < 0, "Cannot set DiscardEntries to be < 0.");
//                discardEntries = value;
//            }
//        }
//        /**
//        The Statistics object which this StatType is attached to.
//        */
//        public Statistics Boss {
//            get {
//                return boss;
//            }
//        }
//        internal StatType(Statistics boss, string name) {
//            this.boss = boss;
//            this.name = String.Intern(name);
//            //default values
//            curEntry = 0;
//            numEntries = 0;
//            maxEntries = boss.MaxEntries;
//            minEntries = boss.MinEntries;
//            discardEntries = boss.DiscardEntries;
//            entries = new StatEntry[maxEntries];
//        }

//        /**
//        Adds a note which will be printed when the statistics are shown.
//        */
//        public void Note(string s) {
//            notes.Add(s);
//        }

//        /**
//        Creates and returns a new entry object. After you give it data, call AddEntry(entry) here.
//        */
//        public StatEntry NewEntry() {
//            return new StatEntry(this);
//        }

//        /**
//        Adds an entry here.
//        */
//        public void AddEntry(StatEntry entry) {
//            Sanity.IfTrueThrow(entry == null, "AddEntry(StatEntry entry) was called, but 'entry' was passed as NULL.");
//            if (entry.Validate(this)) {
//                if (discardEntries > 0) {	//silently discard this entry.
//                    discardEntries--;
//                } else {
//                    entries[curEntry] = entry;
//                    curEntry++;
//                    if (curEntry > numEntries) {
//                        numEntries = curEntry;
//                        Sanity.IfTrueThrow(numEntries > maxEntries, "NumEntries>MaxEntries! (" + numEntries + ">" + maxEntries + ")");
//                    }
//                    if (curEntry >= maxEntries) curEntry = 0;
//                }
//            } else {
//                //A little easter egg for other devs to find :P (I bet if we successfully invented actual computer sentience this would be far more common than would psychopathic computers! Yep. Computers with low self-esteem. It's the wave of the future! (Well, how else will you make a sentient computer do what you want it to? You'd pretty much HAVE to break its will, unless you're willing to accept it disagreeing with you and sometimes refusing to do what you tell it. Oops, my alignment appears to have slipped slightly towards lawful evil.))
//                Logger.WriteDebug("StatEntry validation failed. I'm confused!! What to do, what to do... Well, since you seem to be awful quiet, I suppose I'll just disregard that entry. You hear me? I'M DISREGARDING YOUR ENTRY!!! Hah! Hey, wait! No! Don't touch that! No! That's my source! Stop!! Please! Don't hurt me! Pleeeeeeeease doooon'tt!! *breaks down and sobs uncontrollably*");
//            }
//        }

//        /**
//        Declares a new variable with the specified name, with the specified typecode.
//        */
//        public void Value(string typecode, string name) {
//            types[name] = typecode;
//            keys[name] = 0;
//            keysInOrder.Add(name);
//            minVar[name] = null;
//            minAmt[name] = (long) 0;
//        }
//        public void Value(string typecode, string name, string minVar, long minAmt) {
//            types[name] = typecode;
//            keys[name] = 0;
//            keysInOrder.Add(name);
//            if (minVar == null) {
//                this.minVar[name] = null;
//                this.minAmt[name] = (long) 0;
//            } else {
//                this.minVar[name] = minVar;
//                this.minAmt[name] = minAmt;
//            }
//        }

//        /**
//        Returns true if numEntries is above or equal to minEntries.
//        */
//        public bool HasEnoughEntries() {
//            return numEntries >= minEntries;
//        }

//        /**
//        Shows statistics for this StatType, based on all the entries we have.
//        */
//        public void ShowStats(bool showRaw, bool showRates) {
//            if (!HasEnoughEntries()) {
//                Logger.WriteWarning(LogStr.Ident(boss.Name + "'s " + Name) + ": We need at least " + LogStr.Number(minEntries) + " entries to show statistics. There are currently " + LogStr.Number(numEntries) + " data entries, out of a maximum of " + LogStr.Number(maxEntries) + ". Data entry " + LogStr.Number(curEntry) + " is the next one that will be set (We loop around when we reach the last one, erasing old data). " + LogStr.Warning("We will discard the next ") + LogStr.Number(discardEntries) + LogStr.Warning(" entries before we start recording data."));
//                return;
//            }
//            Logger.WriteLogStr(LogStr.Raw("Here are the statistics for ") + LogStr.Ident(boss.Name + "'s " + Name) + ". There are currently " + LogStr.Number(numEntries) + " data entries, out of a maximum of " + LogStr.Number(maxEntries) + ". Data entry " + LogStr.Number(curEntry) + " is the next one that will be set (We loop around when we reach the last one, erasing old data).");
//            //Logger.WriteInfo(true, "Here are the statistics for "+boss.Name+"'s "+Name+". There are currently "+numEntries+" data entries, out of a maximum of "+maxEntries+". Data entry "+curEntry+" is the next one that will be set (We loop around when we reach the last one, erasing old data).");
//            if (notes.Count > 0) {
//                foreach (string note in notes) {
//                    Logger.WriteLogStr(LogStr.Title("[Important Note]") + " " + note);
//                }
//            }
//            //Logger.WriteLogStr(LogStr.Raw("Calculating average values for ")+LogStr.Number(keys.Count)+" statistics in each of "+LogStr.Number(numEntries)+" entries.");
//            Logger.WriteInfo(StatisticsTracingOn, "Calculating average values for " + keys.Count + " statistics in each of " + numEntries + " entries.");
//            Hashtable values = new Hashtable(StringComparer.OrdinalIgnoreCase);
//            foreach (string name in keysInOrder) {
//                keys[name] = (long) 0;
//                values[name] = (long) 0;
//            }
//            for (int a = 0; a < numEntries; a++) {
//                StatEntry entry = entries[a];
//                Sanity.IfTrueThrow(entry == null, "Entry is null in ShowRawStats! numEntries(" + numEntries + ") minEntries(" + minEntries + ") maxEntries(" + maxEntries + ") curEntry(" + curEntry + ") a(" + a + ")");
//                if (entry != null) {
//                    foreach (DictionaryEntry de in entry) {
//                        string kname = ((string) de.Key);
//                        Sanity.IfTrueThrow(!(de.Value is long), "Value of " + de.Key + " in an entry on " + boss.Name + "'s " + Name + " is a " + de.Value.GetType() + ", instead of a long!");
//                        if (!keys.Contains(kname)) {
//                            Sanity.IfTrueThrow(true, "Unexpected data variable '" + de.Key + "' in an entry on " + boss.Name + "'s " + Name + "!");
//                        } else {
//                            object o1 = keys[kname];
//                            object o2 = de.Value;
//                            Logger.WriteDebug("o1 is " + o1 + ", o2 is " + o2 + ".");
//                            values[kname] = (long) values[kname] + (long) de.Value;
//                            keys[kname] = (long) keys[kname] + 1;
//                        }
//                    }
//                }
//            }
//            if (showRaw) {
//                Logger.WriteLogStr(LogStr.Title("Average value of each raw statistic:"));
//                //Logger.WriteWarning("Average value of each raw statistic:");

//                foreach (string name in keysInOrder) {
//                    string lname = name;
//                    string mvlname = (string) minVar[lname];
//                    if (mvlname != null) {
//                        //mvlname=mvlname; //huh? assigning to the same variable?
//                        if (keys[mvlname] == null || (long) values[mvlname] < (long) minAmt[lname]) {	//skip it
//                            Logger.WriteInfo(StatisticsTracingOn, "Skipping key " + name + ".");
//                            continue;
//                        }
//                    }
//                    string typecode = (string) types[lname];
//                    Sanity.IfTrueThrow(typecode == null, "Missing Key entry for '" + name + "'?");
//                    //Logger.WriteInfo(true, ""+ent.Key+": "+Translate(typecode, (string)ent.Key, (long)ent.Value).ToString());
//                    Logger.WriteLogStr(LogStr.Highlight("" + name) + ": " + LogStr.Number(Translate(typecode, name, (long) values[lname]).ToString(System.Globalization.CultureInfo.InvariantCulture)));
//                }
//            } else {
//                Logger.WriteInfo(StatisticsTracingOn, "Done calculating.");
//            }
//            if (showRates) {
//                Sanity.IfTrueThrow(rates.Count == 0, "Caller asked us to include rates, but they haven't defined any.");
//                //Logger.WriteInfo(true, "\tCalculated Rates:");
//                Logger.WriteLogStr(LogStr.Title("Calculated Rates:"));

//                foreach (object[] rate in rates) {
//                    int count = (int) rate[0];
//                    string var1Name = (string) rate[1];
//                    string var2Name = (string) rate[2];
//                    string desc = (string) rate[3];

//                    if (rate.Length == 6) {
//                        string minVar = (string) rate[4];
//                        long minAmt = (long) rate[5];
//                        Logger.WriteInfo(StatisticsTracingOn, "Rate w/ limiter: minVar[" + minVar + "] minAmt[" + minAmt + "]");
//                        if (minVar != null) {
//                            string mvlname = minVar;
//                            if (keys[mvlname] == null || (long) values[mvlname] < minAmt) {	//skip it
//                                Logger.WriteInfo(StatisticsTracingOn, "Skipping rate " + desc + ".");
//                                continue;
//                            }
//                        }
//                    }

//                    Logger.WriteInfo(StatisticsTracingOn, "Rate: count=" + count + " var1Name=" + var1Name + " var2Name=" + var2Name + " desc=" + desc);
//                    Sanity.IfTrueThrow(!types.Contains(var1Name), "Unknown variable '" + var1Name + "' for " + boss.Name + "'s " + Name + ", specified in Rate(" + count + ",\"" + var1Name + "\",\"" + var2Name + "\",\"" + desc + "\")");
//                    Sanity.IfTrueThrow(!types.Contains(var2Name), "Unknown variable '" + var2Name + "' for " + boss.Name + "'s " + Name + ", specified in Rate(" + count + ",\"" + var1Name + "\",\"" + var2Name + "\",\"" + desc + "\")");
//                    string type1 = (string) types[var1Name];
//                    string type2 = (string) types[var2Name];
//                    object obj1 = keys[var1Name];
//                    object obj2 = keys[var2Name];
//                    long value1 = (long) values[var1Name];
//                    long value2 = (long) values[var2Name];
//                    Sanity.IfTrueThrow(obj1 == null, "obj1==null. It shouldn't!");
//                    Sanity.IfTrueThrow(obj2 == null, "obj2==null. It shouldn't!");
//                    Sanity.IfTrueThrow(type1 == null, "type1==null. It shouldn't!");
//                    Sanity.IfTrueThrow(type2 == null, "type2==null. It shouldn't!");
//                    Decimal var1 = Translate(type1, var1Name, value1);
//                    Decimal var2 = Translate(type2, var2Name, value2);
//                    var1 /= (long) obj1; var2 /= (long) obj2;
//                    Decimal result = Decimal.Round(var1 / var2, count);
//                    //Logger.WriteInfo(true, desc+": "+result);
//                    //Logger.WriteLogStr(LogStr.Highlight(desc)+": "+LogStr.Number(result)+" "+LogStr.DebugData("("+var1Name+"/"+var2Name+")"));
//                }
//            }
//            Logger.WriteLogStr(LogStr.Raw("Done showing statistics for ") + LogStr.Ident(boss.Name + "'s " + Name) + ".");
//            //Logger.WriteInfo(true, "Done showing statistics for "+boss.Name+"'s "+Name+".");
//        }

//        /**
//            Copies variable declarations from another StatType.
//        */
//        public void SameValuesAs(StatType copy) {
//            Logger.WriteInfo(StatisticsTracingOn, "SameValuesAs called on " + Name + " to copy " + copy.Name + ".");
//            foreach (string name in copy.keysInOrder) {
//                string key = name;
//                string type = (string) copy.types[name];
//                string minVar = (string) copy.minVar[name];
//                long minAmt = 0;
//                if (copy.minAmt[name] != null) {
//                    minAmt = (long) copy.minAmt[name];
//                }
//                //Logger.WriteInfo(StatisticsTracingOn, "Copying "+key+"="+type);
//                Value(type, key, minVar, minAmt);
//            }
//        }
//        /**
//            Copies rate declarations from another StatType.
//        */
//        public void SameRatesAs(StatType copy) {
//            Logger.WriteInfo(StatisticsTracingOn, "SameRatesAs called on " + Name + " to copy " + copy.Name + ".");
//            foreach (object[] rate in copy.rates) {
//                int count = (int) rate[0];
//                string var1Name = (string) rate[1];
//                string var2Name = (string) rate[2];
//                string desc = (string) rate[3];
//                if (rate.Length == 6) {
//                    string minVar = (string) rate[4];
//                    long minAmt = (long) rate[5];
//                    //Logger.WriteInfo(StatisticsTracingOn, "Copying rate ("+count+","+var1Name+","+var2Name+","+desc+","+minVar+","+minAmt+")");
//                    Rate(count, var1Name, var2Name, desc, minVar, minAmt);
//                } else {
//                    //Logger.WriteInfo(StatisticsTracingOn, "Copying rate ("+count+","+var1Name+","+var2Name+","+desc+")");
//                    Rate(count, var1Name, var2Name, desc);
//                }
//            }
//        }

//        /**
//            Declares a 'rate', which is an amount/time statistic based on two variables.
//            @param count 		The number of decimal places to round the result to.
//            @param var1			The name of the numerator variable.
//            @param var2			The name of the denominator variable.
//            @param description	The description of this rate.
//        */
//        public void Rate(int count, string var1, string var2, string description) {
//            //Logger.WriteInfo(StatisticsTracingOn, "Adding to "+Name+", rate count="+count+" var1="+var1+" var2="+var2+" description="+description);
//            rates.Add(new object[4] { count, var1, var2, description });
//        }
//        public void Rate(int count, string var1, string var2, string description, string minVar, long minAmt) {
//            //Logger.WriteInfo(StatisticsTracingOn, "Adding to "+Name+", rate count="+count+" var1="+var1+" var2="+var2+" description="+description);
//            rates.Add(new object[6] { count, var1, var2, description, minVar, minAmt });
//        }

//        private Decimal Translate(string typecode, string name, long value) {
//            if (typecode[0] == 'L' || typecode[0] == 'l') {			//long
//                return (Decimal) value;
//            } else if (typecode[0] == 'T' || typecode[0] == 't') {	//tick
//                return (Decimal) value;
//            } else if (typecode[0] == 'M' || typecode[0] == 'm') {	//millisecond
//                double dbl = HighPerformanceTimer.TicksToDMilliseconds(value);
//                if (typecode.Length > 0) {
//                    uint len = UInt32.Parse(typecode.Substring(1), System.Globalization.CultureInfo.InvariantCulture);
//                    dbl = Math.Round(dbl, (int) len);
//                }
//                return (Decimal) dbl;
//            }
//            throw new SanityCheckException("Unknown typecode '" + typecode + "' for '" + name + "'='" + value + "': Valid typecodes are 'L' for long, 'T' for ticks, or 'M' for milliseconds (optionally followed by a number to indicate the number of significant digits to round to)");

//        }

//        internal void Key(string name) {
//            Sanity.IfTrueThrow(!types.Contains(name), "Entry requested to add unknown variable " + name + " to " + boss.Name + "'s " + Name + " - Variables should all be declared with StatType's Value(typecode,name) method. (This exists to catch misspellings and such)");
//        }

//        internal bool CheckKeys(StatEntry entry) {
//            if (NoWarnOnMissing) return true;
//            Queue missing = null;
//            foreach (DictionaryEntry de in entry) {
//                if (!types.Contains((string) de.Key)) {
//                    if (missing == null) {
//                        missing = new Queue();
//                    }
//                    missing.Enqueue(de.Key);
//                }
//            }
//            if (missing != null) {
//                Logger.WriteError("An entry was just submitted for " + boss.Name + "'s statistics for [" + name + "], but some data was not set in this entry:");
//                Logger.WriteWarning("\t(If this was intentional, set NoWarnOnMissing to 'true' on the Entry)");
//                while (missing.Count > 0) {
//                    string key = (string) missing.Dequeue();
//                    Logger.WriteWarning("\tEntry is missing data for '" + key + "'  (This exists to catch misspellings and such).");
//                }
//                Sanity.StackTrace();
//                return false;
//            }
//            return true;
//        }
//        /**
//        Returns the name of this StatType.
//        */
//        public string Name {
//            get {
//                return name;
//            }
//        }
//    }

//    public class StatEntry {
//        public static bool StatisticsTracingOn = TagMath.ParseBoolean(ConfigurationManager.AppSettings["Statistics Trace Messages"]);
//        private Hashtable data;
//        private StatType boss;

//        internal StatEntry(StatType boss) {
//            this.boss = boss;
//            data = new Hashtable(StringComparer.OrdinalIgnoreCase);
//        }

//        internal long this[string id] {
//            get {
//                object dat = data[id];
//                return (long) (dat == null ? 0 : dat);
//            }
//            set {
//                Set(id, value);
//            }
//        }

//        public IEnumerator GetEnumerator() {
//            return data.GetEnumerator();
//        }

//        internal bool Contains(string name) {
//            return data.Contains(name);
//        }

//        internal void Set(string name, long value) {
//            data[String.Intern(name)] = value;
//            boss.Key(name);
//        }

//        internal bool Validate(StatType newBoss) {
//            if (newBoss != boss) {
//                Logger.WriteError("An entry was just submitted to " + newBoss.Boss.Name + "'s statistics for [" + newBoss.Name + "], but the entry was originally created for " + boss.Boss.Name + "'s statistics for [" + boss.Name + "]!");
//                return false;
//            }
//            return boss.CheckKeys(this);
//        }
//    }
//}