using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.CompiledTriggerGroups;

// Generates CompiledTriggerGroup dispatch wrappers for non-abstract, non-sealed
// subclasses by mapping public instance on_* methods to trigger dispatch.
[Generator]
public sealed class CompiledTriggerGroupsGenerator : IIncrementalGenerator {
	internal static readonly DiagnosticDescriptor DuplicateTypeNameDescriptor = new(
		"SEGEN405",
		"Compiled trigger group type name is not unique",
		"Compiled trigger group type name '{0}' is used by multiple types; names must be distinct for code generation",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	public void Initialize(IncrementalGeneratorInitializationContext context) {
		var declarations = context.SyntaxProvider
			.CreateSyntaxProvider(
				static (node, _) => node is TypeDeclarationSyntax,
				static (ctx, _) => (TypeDeclarationSyntax) ctx.Node)
			.Collect();

		context.RegisterSourceOutput(
			context.CompilationProvider.Combine(declarations),
			static (spc, data) => Execute(spc, data.Left, data.Right));
	}

	private static void Execute(
		SourceProductionContext context,
		Compilation compilation,
		IReadOnlyCollection<TypeDeclarationSyntax> declarations) {
		var diagnostics = new List<Diagnostic>();
		var requiredSymbols = RequiredTypeValidator.ResolveRequiredSymbols(compilation, diagnostics);

		if (requiredSymbols is not null) {
			var candidates = new List<CompiledTriggerGroupCandidate>();
			var seenTypes = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
			foreach (var declaration in declarations) {
				var candidate = Parser.DiscoverCandidate(compilation, declaration, requiredSymbols, diagnostics);
				if (candidate is null) {
					continue;
				}
				if (!seenTypes.Add(candidate.TypeSymbol)) {
					continue;
				}
				candidates.Add(candidate);
			}

			var duplicateNames = candidates
				.GroupBy(c => c.TypeSymbol.Name, StringComparer.Ordinal)
				.Where(g => g.Count() > 1)
				.ToDictionary(g => g.Key, g => g.ToList(), StringComparer.Ordinal);
			foreach (var duplicate in duplicateNames) {
				foreach (var candidate in duplicate.Value) {
					diagnostics.Add(Diagnostic.Create(
						DuplicateTypeNameDescriptor,
						candidate.TypeSymbol.Locations.FirstOrDefault(),
						duplicate.Key));
				}
			}

			var uniqueCandidates = candidates
				.Where(c => !duplicateNames.ContainsKey(c.TypeSymbol.Name))
				.ToList();
			var models = Analyzer.BuildModels(uniqueCandidates, diagnostics, requiredSymbols);
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
