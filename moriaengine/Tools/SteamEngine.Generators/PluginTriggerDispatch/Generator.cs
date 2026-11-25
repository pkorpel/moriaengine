using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.PluginTriggerDispatch;

// Generates trigger dispatch overrides for concrete plugin subclasses.
[Generator]
public sealed class PluginTriggerDispatchGenerator : IIncrementalGenerator {
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var typeValidator = new RequiredTypeValidator();
		typeValidator.RegisterRequiredTypeCheck(context);
		var output = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is ClassDeclarationSyntax,
				(ctx, _) => Parser.Parse(ctx, typeValidator.Resolve(ctx.SemanticModel.Compilation).Symbols))
			.Where(static result => result is not null)
			.Select(static (result, _) => Analyzer.Analyze(result!));

		context.RegisterSourceOutput(output, static (spc, result) => {
			if (result.Model is { } model) {
				spc.AddSource(Renderer.GetHintName(model), Renderer.Render(model));
			}
		});
		context.RegisterSourceOutput(output.Combine(context.CompilationProvider), static (spc, data) => {
			foreach (var diagnostic in data.Left.Diagnostics) {
				spc.ReportDiagnostic(diagnostic.ToDiagnostic(data.Right));
			}
		});
	}
}
