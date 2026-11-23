namespace SteamEngine.Generators.CompiledTriggerGroups;

// Renders the partial trigger-group class and its Run dispatch override.
internal static class Renderer {
	internal static string GetHintName(CompiledTriggerGroupModel model) => "CompiledTriggerGroups/" + model.TypeName + ".g.cs";

	internal static string Render(CompiledTriggerGroupModel model) =>
		DispatchRenderer.Render(model.NamespaceName, model.TypeName, model.TriggerMethods, hasSelf: true);
}
