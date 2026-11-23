using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.CompiledTriggerGroups;

// Generates CompiledTriggerGroup dispatch wrappers for non-abstract, non-sealed
// subclasses by mapping public instance on_* methods to trigger dispatch.
[Generator]
public sealed class CompiledTriggerGroupsGenerator : IIncrementalGenerator {
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var typeValidator = new RequiredTypeValidator();
		var requiredTypesAvailable = typeValidator.RegisterRequiredTypeCheck(context);

		var parseResults = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is ClassDeclarationSyntax,
				(ctx, _) => Parser.Parse(ctx, typeValidator.Resolve(ctx.SemanticModel.Compilation).Symbols))
			.Where(static result => result is not null)
			.Select(static (result, _) => result!);

		// Collect parsed groups to check type names across all trigger groups.
		var output = parseResults.Collect().Combine(requiredTypesAvailable)
			.Select(static (data, _) => Analyze(data.Left, data.Right));
		context.RegisterSourceOutput(output, static (spc, result) => {
			foreach (var model in result.Models) {
				spc.AddSource(Renderer.GetHintName(model), Renderer.Render(model));
			}
		});
		context.RegisterSourceOutput(output.Combine(context.CompilationProvider), static (spc, data) => {
			foreach (var diagnostic in data.Left.Diagnostics) {
				spc.ReportDiagnostic(diagnostic.ToDiagnostic(data.Right));
			}
		});
	}

	private static CompiledTriggerGroupOutput Analyze(ImmutableArray<CompiledTriggerGroupParseResult> results, bool requiredTypesAvailable) {
		if (!requiredTypesAvailable) {
			return new CompiledTriggerGroupOutput(EquatableArray<CompiledTriggerGroupModel>.Empty, EquatableArray<GeneratorDiagnosticData>.Empty);
		}
		var diagnostics = new List<GeneratorDiagnosticData>();
		var candidates = new List<CompiledTriggerGroupCandidate>();
		foreach (var result in results.OrderBy(result => (result.Candidate?.Location ?? result.Diagnostic!.Location).TreeIndex)
			.ThenBy(result => (result.Candidate?.Location ?? result.Diagnostic!.Location).Span.Start)) {
			if (result.Diagnostic is not null) {
				diagnostics.Add(result.Diagnostic);
			}
			if (result.Candidate is not null) {
				candidates.Add(result.Candidate);
			}
		}
		var models = Analyzer.BuildModels(candidates, diagnostics);
		return new CompiledTriggerGroupOutput(
			EquatableArray<CompiledTriggerGroupModel>.From(models),
			EquatableArray<GeneratorDiagnosticData>.From(diagnostics));
	}
}
