using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.ThingAndPluginClasses;

// Generates the definition/instance partial classes for [ThingTemplate]/[PluginTemplate]
// and their Def counterparts. The generator replaces legacy .ct generator (Still present, to be phased out)
// Implemented as Roslyn incremental generation, so once fully migrated scripts will compile without runtime codegen.
[Microsoft.CodeAnalysis.Generator]
public sealed partial class Generator : IIncrementalGenerator {
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		// Only inspect classes that already have attributes; template participation is
		// attribute-driven and this keeps syntax filtering cheap during design-time builds.
		var classes = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is ClassDeclarationSyntax cds && cds.AttributeLists.Count > 0,
				static (ctx, _) => Parser.DiscoverCandidate(ctx))
			.Where(static r => r is not null)
			.Collect();

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(classes),
			static (spc, data) => Execute(spc, data.Left, data.Right!));
	}

	private static void Execute(
		SourceProductionContext context,
		Compilation compilation,
		IReadOnlyCollection<TemplateDiscoveryResult> discoveries) {
		// Build all template pairs first, then emit in a single deterministic pass.
		var diagnostics = new List<Diagnostic>();
		var hasRequiredDeclarations = Analyzer.ValidateRequiredDeclarations(compilation, diagnostics);
		var hasRequiredCanonicalTypes = Analyzer.ValidateRequiredCanonicalTypes(compilation, diagnostics);
		var candidates = new List<TemplateCandidate>();
		foreach (var discovery in discoveries) {
			if (discovery is null) {
				continue;
			}
			diagnostics.AddRange(discovery.Diagnostics);
			if (discovery.Candidate is not null) {
				candidates.Add(discovery.Candidate);
			}
		}

		if (hasRequiredDeclarations && hasRequiredCanonicalTypes) {
			var pairs = Analyzer.BuildTemplatePairs(candidates, diagnostics);
			foreach (var pair in pairs) {
				if (!Renderer.TryRenderPair(pair, out var source)) {
					continue;
				}
				context.AddSource(Renderer.GetHintName(pair), source);
			}
		}

		foreach (var diagnostic in diagnostics) {
			context.ReportDiagnostic(diagnostic);
		}
	}

}
