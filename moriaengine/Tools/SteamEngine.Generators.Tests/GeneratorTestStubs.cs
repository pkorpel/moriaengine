namespace SteamEngine.Generators.Tests;

internal static class GeneratorTestStubs {
	internal const string CanonicalEngineTypes = """
namespace SteamEngine {
	public class Thing { }
	public class ThingDef {
		protected FieldValue InitTypedField(string name, object value, System.Type type) => new FieldValue();
		public static void RegisterThingDef(System.Type defType, System.Type instanceType) { }
	}
	public class AbstractScript { }
	public class Globals { }
	public class AbstractAccount { }
	public interface IAbstractKey { }

	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
	public sealed class SaveableDataAttribute : System.Attribute { }

	[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
	public sealed class CopyableDataAttribute : System.Attribute { }
}

namespace SteamEngine.Regions {
	public class Region { }
}
""";
}
