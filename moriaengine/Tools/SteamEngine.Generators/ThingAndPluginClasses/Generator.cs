using System;
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
		// Only inspect classes that already have attributes; template participation is
		// attribute-driven and this keeps syntax filtering cheap during design-time builds.
		var classes = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
				static (ctx, _) => (ClassDeclarationSyntax) ctx.Node)
			.Collect();

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(classes),
			static (spc, data) => Execute(spc, data.Left, data.Right));
	}

	private static void Execute(
		SourceProductionContext context,
		Compilation compilation,
		IReadOnlyCollection<ClassDeclarationSyntax> classDeclarations) {
		// Build all template pairs first, then emit in a single deterministic pass.
		var diagnostics = new List<Diagnostic>();
		var symbols = RequiredTypeValidator.ResolveRequiredSymbols(compilation, diagnostics);

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
				if (!Renderer.TryRenderPair(pair, out var source)) {
					continue;
				}
				context.AddSource(Renderer.GetHintName(pair), source);

				if (Renderer.TryRenderSaveImplementor(pair, out var saveHintName, out var saveSource)) {
					context.AddSource(saveHintName, saveSource);
				}
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
