using System.Collections.Immutable;
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
		var diagnostics = ImmutableArray.CreateBuilder<Diagnostic>();
		var candidates = ImmutableArray.CreateBuilder<PluginTriggerCandidate>();

		foreach (var result in parseResults) {
			foreach (var diagnosticData in result.Diagnostics) {
				diagnostics.Add(Diagnostics.Create(diagnosticData));
			}

			if (result.Candidate is not null) {
				candidates.Add(result.Candidate);
			}
		}

		var analysis = Analyzer.Analyze(candidates.ToImmutable());
		foreach (var diagnosticData in analysis.Diagnostics) {
			diagnostics.Add(Diagnostics.Create(diagnosticData));
		}

		foreach (var model in analysis.Models) {
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
