using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.CompiledTriggerGroups;

// Generates CompiledTriggerGroup dispatch wrappers for non-abstract, non-sealed
// subclasses by mapping public instance on_* methods to trigger dispatch.
[Generator]
public sealed class CompiledTriggerGroupsGenerator : IIncrementalGenerator {

	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var declarations = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is TypeDeclarationSyntax,
				static (ctx, _) => (TypeDeclarationSyntax) ctx.Node)
			.Collect();

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(declarations),
			static (spc, data) => Execute(spc, data.Left, data.Right));
	}

	private static void Execute(
		SourceProductionContext context,
		Compilation compilation,
		IReadOnlyCollection<TypeDeclarationSyntax> declarations) {
		var diagnostics = new List<Diagnostic>();
		var symbols = RequiredTypeValidator.ResolveRequiredSymbols(compilation, diagnostics);

		if (symbols is not null) {
			var candidates = new List<CompiledTriggerGroupCandidate>();
			var seenTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
			foreach (var declaration in declarations) {
				var candidate = Parser.DiscoverCandidate(compilation, declaration, symbols, diagnostics);
				if (candidate is null) {
					continue;
				}
				if (!seenTypes.Add(candidate.TypeSymbol)) {
					continue;
				}
				candidates.Add(candidate);
			}

			var models = Analyzer.BuildModels(candidates, diagnostics, symbols);
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
