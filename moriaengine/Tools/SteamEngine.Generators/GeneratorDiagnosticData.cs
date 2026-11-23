using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace SteamEngine.Generators;

// Value-based source location that can be rebound to the current compilation.
// Line spans participate in equality; ToLocation rebuilds the location from the current tree.
internal readonly record struct SourceLocationData(
	bool HasLocation,
	int TreeIndex,
	TextSpan Span,
	FileLinePositionSpan LineSpan,
	FileLinePositionSpan MappedLineSpan) {
	internal static SourceLocationData From(Compilation compilation, Location? location) {
		if (location is null || !location.IsInSource || location.SourceTree is null) {
			return default;
		}
		return new SourceLocationData(true, GetTreeIndex(compilation, location.SourceTree),
			location.SourceSpan, location.GetLineSpan(), location.GetMappedLineSpan());
	}

	internal static int GetTreeIndex(Compilation compilation, SyntaxTree tree) {
		return compilation.SyntaxTrees.TakeWhile(candidate => candidate != tree).Count();
	}

	internal Location ToLocation(Compilation compilation) {
		return HasLocation
			? Location.Create(compilation.SyntaxTrees.ElementAt(TreeIndex), Span)
			: Location.None;
	}
}

// Diagnostic data retained in incremental results without retaining a source tree.
internal sealed record GeneratorDiagnosticData(
	DiagnosticDescriptor Descriptor,
	SourceLocationData Location,
	EquatableArray<string> Arguments) {
	internal static GeneratorDiagnosticData Create(DiagnosticDescriptor descriptor, SourceLocationData location, params string[] arguments) {
		return new GeneratorDiagnosticData(descriptor, location, EquatableArray<string>.From(arguments));
	}

	internal Diagnostic ToDiagnostic(Compilation compilation) {
		return Diagnostic.Create(Descriptor, Location.ToLocation(compilation), Arguments.Select(argument => (object) argument).ToArray());
	}
}
