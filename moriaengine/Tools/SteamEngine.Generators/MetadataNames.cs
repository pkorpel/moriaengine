namespace SteamEngine.Generators;

internal static class MetadataNames {
	internal static class EngineTypes {
		internal const string Thing = "SteamEngine.Thing";
		internal const string AbstractScript = "SteamEngine.AbstractScript";
		internal const string Region = "SteamEngine.Regions.Region";
		internal const string Globals = "SteamEngine.Globals";
		internal const string IAbstractKey = "SteamEngine.IAbstractKey";
		internal const string AbstractAccount = "SteamEngine.AbstractAccount";
		internal const string ScriptHolder = "SteamEngine.ScriptHolder";
		internal const string ScriptArgs = "SteamEngine.ScriptArgs";
		internal const string CompiledScriptHolder = "SteamEngine.CompiledScripts.CompiledScriptHolder";
		internal const string DeepCopyFactory = "SteamEngine.DeepCopyFactory";
		internal const string IDeepCopyImplementor = "SteamEngine.IDeepCopyImplementor";
		internal const string PropsSection = "SteamEngine.PropsSection";
		internal const string PropsLine = "SteamEngine.PropsLine";
		internal const string SaveStream = "SteamEngine.Persistence.SaveStream";
		internal const string LoadObjectParam = "SteamEngine.Persistence.LoadObjectParam";
		internal const string ObjectSaver = "SteamEngine.Persistence.ObjectSaver";
		internal const string DecoratedClassesSaveImplementor = "SteamEngine.Persistence.DecoratedClassesSaveImplementor";
		internal const string CoreLogger = "SteamEngine.CoreLogger";
		internal const string SEException = "SteamEngine.SEException";
		internal const string FatalException = "SteamEngine.FatalException";
	}

	internal static class AttributeTypes {
		internal const string SaveableData = "SteamEngine.SaveableDataAttribute";
		internal const string CopyableData = "SteamEngine.CopyableDataAttribute";
		internal const string SteamFunction = "SteamEngine.CompiledScripts.SteamFunctionAttribute";
		internal const string ThingTemplate = "SteamEngine.Generators.Abstractions.ThingTemplateAttribute";
		internal const string ThingDefTemplate = "SteamEngine.Generators.Abstractions.ThingDefTemplateAttribute";
		internal const string PluginTemplate = "SteamEngine.Generators.Abstractions.PluginTemplateAttribute";
		internal const string PluginDefTemplate = "SteamEngine.Generators.Abstractions.PluginDefTemplateAttribute";
		internal const string DefField = "SteamEngine.Generators.Abstractions.DefFieldAttribute";
		internal const string DeepCopyableClass = "SteamEngine.DeepCopyableClassAttribute";
		internal const string DeepCopyImplementation = "SteamEngine.DeepCopyImplementationAttribute";
		internal const string SaveableClass = "SteamEngine.Persistence.SaveableClassAttribute";
		internal const string LoadingInitializer = "SteamEngine.Persistence.LoadingInitializerAttribute";
		internal const string LoadingFinalizer = "SteamEngine.Persistence.LoadingFinalizerAttribute";
		internal const string LoadSection = "SteamEngine.Persistence.LoadSectionAttribute";
		internal const string LoadLine = "SteamEngine.Persistence.LoadLineAttribute";
		internal const string Save = "SteamEngine.Persistence.SaveAttribute";
	}
}
