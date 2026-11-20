using System.Collections.Immutable;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.PluginTriggerDispatch;

[Generator]
public sealed class PluginTriggerDispatchGenerator : IIncrementalGenerator {
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var parseResults = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is ClassDeclarationSyntax,
				static (ctx, _) => Parser.ParseCandidate(ctx))
			.Where(static result => result is not null)
			.Select(static (result, _) => result!);

		context.RegisterSourceOutput(
			parseResults.Collect(),
			static (spc, results) => Execute(spc, results));
	}

	private static void Execute(SourceProductionContext context, ImmutableArray<PluginTriggerParseResult> parseResults) {
		var diagnostics = new List<Diagnostic>();
		var candidates = new List<PluginTriggerCandidate>();

		foreach (var result in parseResults) {
			foreach (var diagnosticData in result.Diagnostics) {
				diagnostics.Add(Analyzer.CreateDiagnostic(diagnosticData));
			}

			if (result.Candidate is not null) {
				candidates.Add(result.Candidate);
			}
		}

		var models = Analyzer.Analyze(candidates, diagnostics);

		foreach (var model in models) {
			if (!Renderer.TryRender(model, out var source)) {
				continue;
			}
			context.AddSource(Renderer.GetHintName(model), source);
		}

		foreach (var diagnostic in diagnostics) {
			context.ReportDiagnostic(diagnostic);
		}
	}
}
