using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.CompiledScriptHolders;

// Generates compiled script holders for methods marked with [SteamFunction].
[Generator]
public sealed class CompiledScriptHoldersGenerator : IIncrementalGenerator {
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var typeValidator = new RequiredTypeValidator();
		var requiredTypes = typeValidator.RegisterRequiredTypeCheck(context);
		var parseResults = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is MethodDeclarationSyntax mds && mds.AttributeLists.Count > 0,
				(ctx, _) => Parser.Parse(ctx, typeValidator.Resolve(ctx.SemanticModel.Compilation).Symbols))
			.Where(static result => result is not null)
			.Select(static (result, _) => result!);

		// Collect parsed functions to check names across all script holders.
		var output = requiredTypes.Combine(parseResults.Collect())
			.Select(static (data, _) => Analyze(data.Left, data.Right));

		context.RegisterSourceOutput(output, static (spc, result) => {
			foreach (var model in result.Models) {
				spc.AddSource(Renderer.GetHintName(model), Renderer.Render(model));
			}
		});
		// Rebind diagnostic spans to the current compilation without retaining syntax trees in incremental values.
		context.RegisterSourceOutput(output.Combine(context.CompilationProvider), static (spc, data) => {
			foreach (var diagnostic in data.Left.Diagnostics) {
				spc.ReportDiagnostic(diagnostic.ToDiagnostic(data.Right));
			}
		});
	}

	private static ScriptHolderOutput Analyze(
		bool requiredTypesAvailable,
		ImmutableArray<ScriptHolderParseResult> results) {
		if (!requiredTypesAvailable) {
			return new ScriptHolderOutput(EquatableArray<ScriptHolderModel>.Empty, EquatableArray<GeneratorDiagnosticData>.Empty);
		}

		var diagnostics = new List<GeneratorDiagnosticData>();
		var invalidMethods = new List<GeneratorDiagnosticData>();
		var candidates = new List<ScriptHolderCandidate>();
		// Collect can retain its previous order when existing trees are reordered.
		foreach (var result in results.OrderBy(result => result.TreeIndex).ThenBy(result => result.SpanStart)) {
			foreach (var diagnostic in result.Diagnostics) {
				if (diagnostic.Descriptor == Analyzer.SteamFunctionMustBePublicStaticDescriptor) {
					invalidMethods.Add(diagnostic);
				} else {
					diagnostics.Add(diagnostic);
				}
			}
			if (result.Candidate is not null) {
				candidates.Add(result.Candidate);
			}
		}
		diagnostics.AddRange(invalidMethods);

		var models = Analyzer.BuildModels(candidates, diagnostics);
		return new ScriptHolderOutput(
			EquatableArray<ScriptHolderModel>.From(models),
			EquatableArray<GeneratorDiagnosticData>.From(diagnostics));
	}
}
