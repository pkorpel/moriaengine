using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.CompiledScriptHolders;

// Generates compiled script-holder registry classes from public static methods
// marked with [SteamFunction], including optional custom function names.
[Microsoft.CodeAnalysis.Generator]
public sealed class CompiledScriptHoldersGenerator : IIncrementalGenerator {
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var methods = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is MethodDeclarationSyntax mds && mds.AttributeLists.Count > 0,
				static (ctx, _) => (MethodDeclarationSyntax) ctx.Node)
			.Collect();

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(methods),
			static (spc, data) => Execute(spc, data.Left, data.Right));
	}

	private static void Execute(
		SourceProductionContext context,
		Compilation compilation,
		IReadOnlyCollection<MethodDeclarationSyntax> methodDeclarations) {
		var diagnostics = new List<Diagnostic>();
		var requiredSymbols = RequiredTypeValidator.ResolveRequiredSymbols(compilation, diagnostics);

		if (requiredSymbols is not null) {
			var candidates = new List<ScriptHolderCandidate>();
			foreach (var declaration in methodDeclarations) {
				var candidate = Parser.DiscoverCandidate(compilation, declaration, requiredSymbols, diagnostics);
				if (candidate is null) {
					continue;
				}
				candidates.Add(candidate);
			}

			var models = Analyzer.BuildModels(candidates, diagnostics, requiredSymbols);
			foreach (var model in models) {
				if (!Renderer.TryRender(model, out var source)) {
					continue;
				}
				context.AddSource(Renderer.GetHintName(model), source);
			}
		}

		foreach (var diagnostic in diagnostics) {
			context.ReportDiagnostic(diagnostic);
		}
	}
}
