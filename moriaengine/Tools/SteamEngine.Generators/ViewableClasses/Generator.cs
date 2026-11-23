using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.ViewableClasses;

/// <summary>
/// Generates the IDataView classes behind the .info dialog: one per [ViewableClass] type,
/// Plugin subclass and [ViewDescriptor]-handled type. It runs as Parser (per declaration),
/// Analyzer (whole compilation) and Renderer (text). Only Scripts gets output, because only
/// Scripts has the ViewableClassDataView base class the generated views derive from.
/// </summary>
[Generator]
public sealed class ViewableClassGenerator : IIncrementalGenerator {
	/// <summary>Wires the incremental pipeline.</summary>
	public void Initialize(IncrementalGeneratorInitializationContext context) {
		// Reports missing attribute/engine types once and resolves them for the parser.
		var typeValidator = new RequiredTypeValidator();
		typeValidator.RegisterRequiredTypeCheck(context);

		// Per type declaration: an equatable snapshot, so unchanged types are cached
		// between edits. Declarations that are neither viewable nor descriptors drop out.
		var parsedCandidates = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => Parser.IsCandidateNode(node),
				(ctx, _) => Parser.Parse(ctx, typeValidator.Resolve(ctx.SemanticModel.Compilation).Symbols))
			.Where(static result => result is not null)
			.Select(static (result, _) => result!)
			.Collect();

		// Once per compilation: merge all snapshots, report diagnostics (rebound to the
		// current compilation) and emit a single file.
		context.RegisterSourceOutput(parsedCandidates.Combine(context.CompilationProvider), static (sourceContext, data) => {
			var diagnostics = new List<GeneratorDiagnosticData>();
			var output = Analyzer.BuildOutput(data.Left, diagnostics);
			foreach (var diagnostic in diagnostics) {
				sourceContext.ReportDiagnostic(diagnostic.ToDiagnostic(data.Right));
			}
			if (output.DataViews.Count > 0) {
				sourceContext.AddSource("ViewableClasses.g.cs", Renderer.Render(output));
			}
		});
	}
}
