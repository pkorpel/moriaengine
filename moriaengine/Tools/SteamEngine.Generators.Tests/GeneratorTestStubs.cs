namespace SteamEngine.Generators.Tests;

internal static class GeneratorTestStubs {
	internal const string CanonicalEngineTypes = """
namespace SteamEngine {
	public class Thing { }
	public class FieldValue { }
	public class ThingDef {
		protected FieldValue InitTypedField(string name, object value, System.Type type) => new FieldValue();
		public static void RegisterThingDef(System.Type defType, System.Type instanceType) { }
	}
	public class AbstractScript { }
	public class Globals { }
	public class AbstractAccount { }
	public interface IAbstractKey { }

	public sealed class TriggerKey : IAbstractKey {
		private TriggerKey(string name) {
			Name = name;
		}

		public string Name { get; }
		public int Uid => 0;

		public static TriggerKey Acquire(string name) {
			return new TriggerKey(name);
		}
	}

	public interface IDeepCopyImplementor {
		object DeepCopy(object copyFrom);
		System.Type HandledType { get; }
	}

	public static class DeepCopyFactory {
		public static bool IsNotCopied(System.Type type) => false;
		public static void GetCopyDelayed(object copyFrom, System.Action<object, object> callback, object self) { }
	}

	public static class CoreLogger {
		public static void WriteWarning(string filename, int line, string message) { }
		public static void WriteWarning(string filename, int line, System.Exception ex) { }
	}

	public sealed class SEException : System.Exception {
		public SEException(string filename, int line, System.Exception inner) : base(filename, inner) { }
		public void TryAddFileLineInfo(string filename, int line) { }
	}

	public sealed class FatalException : System.Exception { }

	public sealed class PropsLine {
		public int Line { get; set; }
		public string Name { get; set; } = "";
		public string Value { get; set; } = "";
	}

	public sealed class PropsSection {
		public int HeaderLine => 0;
		public string Filename => "file";
		public PropsLine TryPopPropsLine(string name) => null;
	}

	public class ScriptArgs {
		public ScriptArgs(params object[] argv) {
			Argv = argv ?? System.Array.Empty<object>();
		}

		public object[] Argv { get; }
	}

	public abstract class ScriptHolder {
		protected ScriptHolder(string name) { }

		protected internal void RegisterAsFunction() { }

		public abstract string Description { get; }
		public abstract object Run(object self, ScriptArgs sa);
	}
}

namespace SteamEngine.Regions {
	public class Region { }
}

namespace SteamEngine.CompiledScripts {
	public abstract class CompiledTriggerGroup {
		public virtual object Run(object self, global::SteamEngine.TriggerKey tk, global::SteamEngine.ScriptArgs sa) => null;
		protected virtual string InternalFirstGetDefname() => "";
	}

	public abstract class CompiledScriptHolder : SteamEngine.ScriptHolder {
		private readonly string desc;

		protected CompiledScriptHolder(string name, string description)
			: base(name) {
			desc = description;
		}

		public override string Description => desc;
	}
}

namespace SteamEngine.Persistence {
	public delegate void LoadObjectParam(object resolved, string filename, int line, object param);

	public sealed class SaveStream {
		public void WriteValue(string key, object value) { }
	}

	public static class ObjectSaver {
		public static object OptimizedLoad_String(string value) => value;
		public static object OptimizedLoad_SimpleType(string value, System.Type type) => value;
		public static void Load(string value, LoadObjectParam callback, string filename, int line, object param) { }
	}

	public abstract class DecoratedClassesSaveImplementor {
		protected DecoratedClassesSaveImplementor(System.Type handledType, string headerName) { }
		public abstract object LoadSection(global::SteamEngine.PropsSection input);
		public abstract void Save(object objToSave, SaveStream writer);
		protected void LoadSectionLines(global::SteamEngine.PropsSection ps, object loadedObject) { }
	}
}
""";
}
