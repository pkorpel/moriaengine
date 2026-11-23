using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.ThingAndPluginClasses;

// Generates the definition/instance partial classes for [ThingTemplate]/[PluginTemplate]
// and their Def counterparts. The generator replaces legacy .ct generator (Still present, to be phased out)
// Implemented as Roslyn incremental generation, so once fully migrated scripts will compile without runtime codegen.
[Microsoft.CodeAnalysis.Generator]
public sealed partial class ThingAndPluginClassesGenerator : IIncrementalGenerator {
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var typeValidator = new RequiredTypeValidator();
		// Only inspect classes that already have attributes; template participation is
		// attribute-driven and this keeps syntax filtering cheap during design-time builds.
		var classes = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
				static (ctx, _) => (ClassDeclarationSyntax) ctx.Node)
			// Gather declarations to pair templates and check names across all pairs.
			.Collect();

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(classes),
			(spc, data) => Execute(spc, data.Left, data.Right, typeValidator));
	}

	private static void Execute(
		SourceProductionContext context,
		Compilation compilation,
		IReadOnlyCollection<ClassDeclarationSyntax> classDeclarations,
		RequiredTypeValidator typeValidator) {
		// Build all template pairs first, then emit in a single deterministic pass.
		var resolution = typeValidator.Resolve(compilation);
		var diagnostics = new List<Diagnostic>(resolution.Diagnostics);
		var symbols = resolution.Symbols;

		if (symbols is not null) {
			var candidates = new List<TemplateCandidate>();
			foreach (var declaration in classDeclarations) {
				var candidate = Parser.DiscoverCandidate(compilation, declaration, symbols, diagnostics);
				if (candidate is null) {
					continue;
				}
				candidates.Add(candidate);
			}

			var pairs = Analyzer.BuildTemplatePairs(candidates, diagnostics, symbols);
			foreach (var pair in pairs) {
				context.AddSource(Renderer.GetHintName(pair), Renderer.RenderPair(pair));

				var (saveHintName, saveSource) = Renderer.RenderSaveImplementor(pair);
				context.AddSource(saveHintName, saveSource);
				if (Renderer.TryRenderDeepCopyImplementor(pair, out var deepCopyHintName, out var deepCopySource)) {
					context.AddSource(deepCopyHintName, deepCopySource);
				}
			}
		}

		foreach (var diagnostic in diagnostics) {
			context.ReportDiagnostic(diagnostic);
		}
	}

}
