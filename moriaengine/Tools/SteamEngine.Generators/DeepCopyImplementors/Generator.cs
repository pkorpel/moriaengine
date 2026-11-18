using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.DeepCopyImplementors;

[Microsoft.CodeAnalysis.Generator]
public sealed class Generator : IIncrementalGenerator {
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var classes = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is TypeDeclarationSyntax tds && tds.AttributeLists.Count > 0,
				static (ctx, _) => Parser.DiscoverCandidate(ctx))
			.Where(static discovery => discovery is not null)
			.Select(static (discovery, _) => discovery!)
			.Collect();

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(classes),
			static (spc, data) => Execute(spc, data.Left, data.Right));
	}

	private static void Execute(
		SourceProductionContext context,
		Compilation compilation,
		IReadOnlyCollection<DeepCopyDiscoveryResult> discoveries) {
		var diagnostics = new List<Diagnostic>();
		var hasRequiredDeclarations = Analyzer.ValidateRequiredDeclarations(compilation, diagnostics);
		var hasRequiredCanonicalTypes = Analyzer.ValidateRequiredCanonicalTypes(compilation, diagnostics);

		var candidates = new List<DeepCopyCandidate>();
		foreach (var discovery in discoveries) {
			diagnostics.AddRange(discovery.Diagnostics);
			if (discovery.Candidate is not null) {
				candidates.Add(discovery.Candidate);
			}
		}

		if (hasRequiredDeclarations && hasRequiredCanonicalTypes) {
			var models = Analyzer.BuildModels(candidates, diagnostics, compilation);
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
