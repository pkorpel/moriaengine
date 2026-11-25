namespace SteamEngine.Generators.PluginTriggerDispatch;

// Renders the partial plugin class and its Run dispatch override.
internal static class Renderer {
	internal static string GetHintName(PluginTriggerModel model) {
		var typePath = IdentifierSanitizer.EncodeHintName(model.FullyQualifiedTypeName);
		return "PluginTriggerDispatch/" + typePath + ".g.cs";
	}

	internal static string Render(PluginTriggerModel model) =>
		DispatchRenderer.Render(model.NamespaceName, model.TypeName, model.TriggerMethods, hasSelf: false);
}
