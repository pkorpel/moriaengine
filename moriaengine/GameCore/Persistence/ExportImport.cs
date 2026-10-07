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
//using System.IO;
//using System.Collections;
//using System.Collections.Generic;
//using System.Reflection;
//using System.Globalization;
//using SteamEngine.Common;

//namespace SteamEngine.Persistence {
//    public interface IImportable {
//        void Import(ImportHelper helper);

//        void Export(SaveStream stream);
//    }

//    [AttributeUsage(AttributeTargets.Method)]
//    public class ImportAssignMethodAttribute : Attribute {
//    }
//    public delegate void ImportAssign(ImportHelperCollection collection);

//    [AttributeUsage(AttributeTargets.Method)]
//    public class ImportingFinishedMethodAttribute : Attribute {
//    }

//    public class ImportHelper {
//        public IImportable instance;
//        internal ExportImport.ImportInvoker invoker;
//        private PropsSection section;

//        internal ImportHelper(ExportImport.ImportInvoker invoker, PropsSection section) {
//            this.invoker = invoker;
//            this.section = section;
//        }

//        public PropsSection Section { get {
//            return section;
//        } }
//    }

//    public class ImportHelperCollection {
//        internal List<ImportHelper> list = new List<ImportHelper>();

//        internal Dictionary<string, Region> regions = new Dictionary<string, Region>(StringComparer.OrdinalIgnoreCase);
//        internal Dictionary<int, Thing> things = new Dictionary<int, Thing>();

//        public IEnumerable<ImportHelper> Enumerate(Type type) {
//            foreach (ImportHelper helper in list) {
//                if (helper.invoker != null) {
//                    if (type.IsAssignableFrom(helper.invoker.declaringType)) {
//                        yield return helper;
//                    }
//                }
//            }
//        }

//        internal IEnumerable<ImportHelper> EnumerateUnresolved() {
//            foreach (ImportHelper helper in list) {
//                if (helper.instance == null) {
//                    yield return helper;
//                }
//            }
//        }

//        private bool EOFMarked;


//        internal IUnloadable LoadSection(PropsSection input) {
//            if (input == null) {
//                if (!EOFMarked) {
//                    throw new Exception("EOF Marker not reached!");
//                }
//                return null;
//            }
//            string type = input.headerType.ToLower();
//            string name = input.headerName;
//            if (EOFMarked) {
//                Logger.WriteWarning(input.filename, input.headerLine, "[EOF] reached. Skipping "+input);
//                return null;
//            }
//            if (name == "") {
//                if (type == "eof") {
//                    EOFMarked = true;
//                    return null;
//                }
//            }

//            ExportImport.ImportInvoker invoker;
//            ExportImport.invokersByName.TryGetValue(type, out invoker);
//            list.Add(new ImportHelper(invoker, input));
//            return null;
//        }

//        internal void SetReplacedRegion(Region region) {
//            regions[region.Defname] = region;
//        }
//    }

//    public static class ExportImport {
//        internal static Dictionary<string, ImportInvoker> invokersByName = new Dictionary<string, ImportInvoker>(StringComparer.OrdinalIgnoreCase);
//        internal static Dictionary<Type, ImportInvoker> invokersByType = new Dictionary<Type, ImportInvoker>();
//        private static List<ImportInvoker> baseInvokers = new List<ImportInvoker>();

//        private static ImportHelperCollection collection;

//        internal class ImportInvoker {
//            internal Type declaringType;
//            internal readonly string sectionName;
//            internal ImportAssign importAssignDeleg;
//            internal ImportAssign importFinishedDeleg;

//            internal ImportInvoker(Type type) {
//                this.declaringType = type;
//                this.sectionName = type.Name;
//            }
//        }

//        internal static void RegisterExportClass(Type type) {
//            if (type == typeof(IImportable)) return;
//            ImportInvoker invoker = new ImportInvoker(type);
//            invokersByType[type] = invoker;
//        }

//        public static void Init() {
//            invokersByName.Clear();

//            foreach (ImportInvoker invoker in invokersByType.Values) {
//                if (TryCreateDelegates(invoker.declaringType, out invoker.importAssignDeleg,
//                        out invoker.importFinishedDeleg)) {
//                    invokersByName[invoker.sectionName] = invoker;
//                    baseInvokers.Add(invoker);
//                } else {
//                    invoker.importAssignDeleg = null;
//                }
//            }

//            //now look for implementing parents
//            foreach (ImportInvoker invoker in invokersByType.Values) {
//                if (invoker.importAssignDeleg == null) {
//                    Type baseType = invoker.declaringType;
//                    bool success = false;
//                    while (baseType != null) {
//                        ImportInvoker baseInvoker;
//                        if (invokersByType.TryGetValue(baseType, out baseInvoker)) {
//                            if (baseInvoker.importAssignDeleg != null) {
//                                //invoker.AssignFromBase(baseInvoker);
//                                invokersByName[invoker.sectionName] = invoker;
//                                success = true;
//                                break;
//                            }
//                        }
//                        baseType = baseType.BaseType;
//                    }
//                    if (!success) {
//                        Logger.WriteWarning("The class "+LogStr.Ident(invoker.declaringType)+" implements IImportable but does not have the proper attribute-decorated static methods");
//                    }
//                }
//            }

//            //clear and re-add
//            invokersByType.Clear();
//            foreach (ImportInvoker invoker in invokersByName.Values) {
//                invokersByType[invoker.declaringType] = invoker;
//            }
//        }

//        public static void UnloadScripts() {
//            ImportInvoker[] invokers = new ImportInvoker[invokersByType.Count];
//            invokersByType.Values.CopyTo(invokers, 0);
//            Assembly coreAssembly = CompiledScripts.ClassManager.CoreAssembly;

//            foreach (ImportInvoker invoker in invokers) {
//                Type type = invoker.declaringType;
//                if (coreAssembly != type.Assembly) {
//                    invokersByType.Remove(type);
//                    baseInvokers.Remove(invoker);
//                }
//            }
//        }

//        private static bool TryCreateDelegates(Type type, out ImportAssign importAssign,
//                out ImportAssign importFinished) {
//            importAssign = null; importFinished = null;

//            foreach (MethodInfo mi in type.GetMethods(BindingFlags.Static| BindingFlags.Public)) {
//                if (Attribute.IsDefined(mi, typeof(ImportAssignMethodAttribute))) {
//                    if (importAssign == null) {
//                        if (!IsProperStaticMethod(mi, out importAssign)) {
//                            throw new SEException("The method "+mi.Name+" in Import/Export implementing class "+type.Name+" needs to have one parameter of type ImportHelperCollection.");
//                        }
//                    } else {
//                        throw new SEException("Import/Export implementing class "+type.Name+" has more than one ImportAssign-decorated method.");
//                    }
//                } else if (Attribute.IsDefined(mi, typeof(ImportingFinishedMethodAttribute))) {
//                    if (importFinished == null) {
//                        if (!IsProperStaticMethod(mi, out importFinished)) {
//                            throw new SEException("The method "+mi.Name+" in Import/Export implementing class "+type.Name+" needs to have one parameter of type ImportHelperCollection.");
//                        }
//                    } else {
//                        throw new SEException("Import/Export implementing class "+type.Name+" has more than one ImportingFinished-decorated method.");
//                    }
//                }
//            }
//            return ((importAssign != null) && (importFinished != null));
//        }

//        private static bool IsProperStaticMethod(MethodInfo mi, out ImportAssign retVal) {
//            retVal = null;
//            ParameterInfo[] pis = mi.GetParameters();
//            if ((pis.Length == 1) && (pis[0].ParameterType == typeof(ImportHelperCollection))) {
//                retVal = (ImportAssign) Delegate.CreateDelegate(typeof(ImportAssign), mi);
//                return true;
//            }
//            return false;
//        }

//        public static object[] Import(string filename) {
//            if (File.Exists(filename)) {
//                return Import(filename, File.OpenText(filename));
//            } else {
//                throw new SEException("File "+LogStr.Ident(filename)+" not found.");
//            }
//        }

//        public static object[] Import(string filename, TextReader stream) {
//            try {
//                using (stream) {
//                    collection = new ImportHelperCollection();
//                    PropsFileParser.Load(filename, stream, collection.LoadSection, StartsAsScript);

//                    foreach (ImportInvoker invoker in invokersByName.Values) {
//                        if (invoker.importAssignDeleg != null) {
//                            invoker.importAssignDeleg(collection);
//                        }
//                    }

//                    foreach (ImportHelper helper in collection.list) {
//                        if (helper.instance == null) {
//                            throw new SEException(LogStr.FileLine(helper.Section.filename, helper.Section.headerLine)+" Un-importable section "+LogStr.Ident(helper.Section.headerType)+" in imported file.");
//                        }
//                    }


//                    try {
//                        foreach (ImportHelper helper in collection.list) {
//                            helper.instance.Import(helper);
//                        }

//                        DelayedResolver.ResolveAll();//we load the parent stuff
//                        ObjectSaver.LoadingFinished();

//                        foreach (ImportInvoker invoker in invokersByName.Values) {
//                            if (invoker.importFinishedDeleg != null) {
//                                invoker.importFinishedDeleg(collection);
//                            }
//                        }
//                    } catch (FatalException) {
//                        throw;
//                    } catch (Exception e) {//kill!
//                        throw new FatalException("Error while importing. Unable to revert changes, exiting.", e);
//                    }

//                    ObjectSaver.LoadingFinished();
//                    DelayedResolver.ResolveAll();

//                    Globals.SrcWriteLine("Imported "+collection.list.Count+" objects.");

//                    List<ImportHelper> helpers = collection.list;
//                    int n = helpers.Count;
//                    object[] retVal = new object[n];
//                    for (int i = 0; i<n; i++) {
//                        retVal[i] = helpers[i].instance;
//                    }
//                    return retVal;
//                }
//            } finally {
//                collection = null;
//                ObjectSaver.ClearJobs();
//                DelayedResolver.ClearAll();
//            }
//        }

//        internal static Region GetRegionByDefName(string defname) {
//            Region region;
//            if (collection != null) {
//                if (collection.regions.TryGetValue(defname, out region)) {
//                    return region;
//                }
//            }
//            return Region.GetByDefname(defname);
//        }

//        private static bool StartsAsScript(string headerType) {
//            return false;
//        }

//        public static void Export(string filename, IEnumerable<IImportable> list) {
//            Export(new StreamWriter(filename), list);
//        }

//        public static void Export(TextWriter writer, IEnumerable<IImportable> list) {
//            using (writer) {
//                ObjectSaver.ClearJobs();
//                int count = 0;
//                SaveStream stream = new SaveStream(writer);
//                foreach (IImportable importable in list) {
//                    importable.Export(stream);
//                    count++;
//                }
//                stream.WriteLine("[EOF]");

//                Globals.SrcWriteLine("Exported "+count+" objects.");
//            }
//        }
//    }
//}
