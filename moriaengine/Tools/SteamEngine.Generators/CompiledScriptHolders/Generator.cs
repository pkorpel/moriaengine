using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.CompiledScriptHolders;

[Microsoft.CodeAnalysis.Generator]
public sealed class Generator : IIncrementalGenerator {
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var methods = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is MethodDeclarationSyntax mds && mds.AttributeLists.Count > 0,
				static (ctx, _) => Parser.DiscoverCandidate(ctx))
			.Where(static r => r is not null)
			.Collect();

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(methods),
			static (spc, data) => Execute(spc, data.Left, data.Right));
	}

	private static void Execute(
		SourceProductionContext context,
		Compilation compilation,
		IReadOnlyCollection<ScriptHolderDiscoveryResult> discoveries) {
		var diagnostics = new List<Diagnostic>();
		var hasRequiredDeclarations = Analyzer.ValidateRequiredDeclarations(compilation, diagnostics);
		var hasRequiredCanonicalTypes = Analyzer.ValidateRequiredCanonicalTypes(compilation, diagnostics);

		if (hasRequiredDeclarations && hasRequiredCanonicalTypes) {

			var candidates = new List<ScriptHolderCandidate>();
			foreach (var discovery in discoveries) {
				if (discovery is null) {
					continue;
				}

				diagnostics.AddRange(discovery.Diagnostics);
				if (discovery.Candidate is not null) {
					candidates.Add(discovery.Candidate);
				}
			}

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
