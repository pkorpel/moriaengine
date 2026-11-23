using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.SaveImplementors;

// Generates persistence implementors for [SaveableClass] types by combining
// [LoadingInitializer]/[LoadSection]/[LoadLine]/[LoadingFinalizer]/[Save]
// members and [SaveableData] members into a generated DecoratedClassesSaveImplementor.
[Microsoft.CodeAnalysis.Generator]
public sealed class SaveImplementorsGenerator : IIncrementalGenerator {
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var typeValidator = new RequiredTypeValidator();
		var types = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is TypeDeclarationSyntax tds && tds.AttributeLists.Count > 0,
				static (ctx, _) => (TypeDeclarationSyntax) ctx.Node)
			// Analyze all declarations together to detect duplicate saveable type names.
			.Collect();

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(types),
			(spc, data) => Execute(spc, data.Left, data.Right, typeValidator));
	}

	private static void Execute(
		SourceProductionContext context,
		Compilation compilation,
		IReadOnlyCollection<TypeDeclarationSyntax> declarations,
		RequiredTypeValidator typeValidator) {
		var resolution = typeValidator.Resolve(compilation);
		var diagnostics = new List<Diagnostic>(resolution.Diagnostics);
		var symbols = resolution.Symbols;

		if (symbols is not null) {
			var candidates = new List<SaveImplementorCandidate>();
			foreach (var declaration in declarations) {
				var candidate = Parser.DiscoverCandidate(compilation, declaration, symbols, diagnostics);
				if (candidate is null) {
					continue;
				}
				candidates.Add(candidate);
			}

			var models = Analyzer.BuildModels(candidates, diagnostics, symbols);
			foreach (var model in models) {
				context.AddSource(Renderer.GetHintName(model), Renderer.Render(model));
			}
		}

		foreach (var diagnostic in diagnostics) {
			context.ReportDiagnostic(diagnostic);
		}
	}
}
