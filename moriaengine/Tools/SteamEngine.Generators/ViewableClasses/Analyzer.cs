using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators.ViewableClasses;

/// <summary>
/// Second stage of the ViewableClass pipeline: combines the per-declaration parse results
/// into one compilation-wide model. Descriptors are attached to the types they handle (and
/// to subclasses of those), member lists are flattened, and every generated class gets a
/// stable name, so the renderer only has to print.
/// </summary>
internal static class Analyzer {
	/// <summary>
	/// Builds the render model for the whole compilation and moves every parse diagnostic
	/// into <paramref name="diagnostics"/>.
	/// </summary>
	internal static ViewableOutput BuildOutput(
		IReadOnlyCollection<ViewableParseResult> parseResults,
		ICollection<GeneratorDiagnosticData> diagnostics) {
		// Split the parse results into viewable types and descriptors, keyed by type name.
		// A real [ViewableClass]/Plugin declaration beats a descriptor's snapshot of the
		// same type, because the declaration carries its own label and attributes.
		var typeData = new Dictionary<string, ViewableTypeData>(StringComparer.Ordinal);
		var descriptors = new Dictionary<string, ViewDescriptorData>(StringComparer.Ordinal);
		foreach (var result in parseResults) {
			foreach (var diagnostic in result.Diagnostics) {
				diagnostics.Add(diagnostic);
			}

			var candidate = result.Candidate;
			if (candidate is null) {
				continue;
			}

			if (candidate.ViewType is not null) {
				if (!typeData.TryGetValue(candidate.ViewType.TypeKey, out var existing)
					|| (existing.IsDescriptorTargetSnapshot && !candidate.ViewType.IsDescriptorTargetSnapshot)) {
					typeData[candidate.ViewType.TypeKey] = candidate.ViewType;
				}
			}
			if (candidate.Descriptor is not null) {
				descriptors[candidate.Descriptor.TypeKey] = candidate.Descriptor;
			}
		}

		// One data view per type, in type-name order so the output doesn't depend on
		// declaration order. Rows are the descriptors' rows (base type's descriptor
		// first), then the type's own members minus the names the descriptors hide.
		var dataViewNames = new UniqueNames();
		var dataViews = new List<ViewableDataViewModel>();
		foreach (var type in typeData.Values.OrderBy(static type => type.TypeKey, StringComparer.Ordinal)) {
			var matchingDescriptors = descriptors.Values
				.Where(descriptor => IsApplicable(descriptor, type))
				.OrderBy(descriptor => GetDescriptorOrder(descriptor, type))
				.ThenBy(static descriptor => descriptor.TypeKey, StringComparer.Ordinal)
				.ToArray();

			var nonDisplayed = new HashSet<string>(
				matchingDescriptors.SelectMany(static descriptor => descriptor.NonDisplayedFields),
				StringComparer.Ordinal);
			var fields = new List<ViewableFieldData>();
			var buttons = new List<ViewableButtonData>();
			foreach (var descriptor in matchingDescriptors) {
				fields.AddRange(descriptor.Fields);
				buttons.AddRange(descriptor.Buttons);
			}
			fields.AddRange(type.Fields.Where(field => !nonDisplayed.Contains(field.MemberName is null ? string.Empty : UnescapeIdentifier(field.MemberName))));
			buttons.AddRange(type.Buttons);

			dataViews.Add(new ViewableDataViewModel {
				TypeName = type.TypeName,
				Label = GetViewLabel(type, descriptors.Values),
				ClassName = dataViewNames.Get("GeneratedDataView_" + ToIdentifierPart(type.TypeName)),
				Fields = fields,
				Buttons = buttons
			});
		}

		// Row helpers are shared: an inherited member (or a descriptor row) used by many
		// views is emitted once, keyed by its identity and named after its owner and member.
		var fieldNames = new UniqueNames();
		var uniqueFields = dataViews
			.SelectMany(static dataView => dataView.Fields)
			.GroupBy(static field => field.Identity, StringComparer.Ordinal)
			.OrderBy(static group => group.Key, StringComparer.Ordinal)
			.Select(group => new ViewableFieldViewModel {
				ClassName = fieldNames.Get(GetFieldViewName(group.First())),
				Field = group.First()
			})
			.ToList();
		var fieldClassNames = uniqueFields.ToDictionary(
			static field => field.Field.Identity,
			static field => field.ClassName,
			StringComparer.Ordinal);

		// Button helpers are shared the same way.
		var buttonNames = new UniqueNames();
		var uniqueButtons = dataViews
			.SelectMany(static dataView => dataView.Buttons)
			.GroupBy(static button => button.Identity, StringComparer.Ordinal)
			.OrderBy(static group => group.Key, StringComparer.Ordinal)
			.Select(group => new ViewableButtonViewModel {
				ClassName = buttonNames.Get(GetButtonViewName(group.First())),
				Button = group.First()
			})
			.ToList();
		var buttonClassNames = uniqueButtons.ToDictionary(
			static button => button.Button.Identity,
			static button => button.ClassName,
			StringComparer.Ordinal);

		return new ViewableOutput {
			DataViews = dataViews,
			FieldViews = uniqueFields,
			ButtonViews = uniqueButtons,
			FieldClassNames = fieldClassNames,
			ButtonClassNames = buttonClassNames
		};
	}

	/// <summary>
	/// The dialog headline: the [ViewableClass] label (or type name) for a declared type;
	/// for a type viewable only through a descriptor, that descriptor's label.
	/// </summary>
	private static string GetViewLabel(ViewableTypeData type, IEnumerable<ViewDescriptorData> descriptors) {
		if (!type.IsDescriptorTargetSnapshot) {
			return type.Label;
		}

		return descriptors
			.Where(descriptor => string.Equals(descriptor.HandledTypeKey, type.TypeKey, StringComparison.Ordinal))
			.OrderBy(static descriptor => descriptor.TypeKey, StringComparer.Ordinal)
			.Select(static descriptor => descriptor.Label)
			.FirstOrDefault() ?? type.Label;
	}

	/// <summary>A descriptor applies to the type it handles and to every subclass of it.</summary>
	private static bool IsApplicable(ViewDescriptorData descriptor, ViewableTypeData type) {
		return string.Equals(descriptor.HandledTypeKey, type.TypeKey, StringComparison.Ordinal)
			|| type.BaseTypeKeys.Contains(descriptor.HandledTypeKey, StringComparer.Ordinal);
	}

	/// <summary>
	/// Sort key that puts descriptors of more distant base types first; BaseTypeKeys runs
	/// from the direct base outward, and the type's own descriptor sorts last.
	/// </summary>
	private static int GetDescriptorOrder(ViewDescriptorData descriptor, ViewableTypeData type) {
		var index = -1;
		for (var i = 0; i < type.BaseTypeKeys.Length; i++) {
			if (string.Equals(type.BaseTypeKeys[i], descriptor.HandledTypeKey, StringComparison.Ordinal)) {
				index = i;
				break;
			}
		}
		return index >= 0 ? type.BaseTypeKeys.Length - 1 - index : int.MaxValue;
	}

	/// <summary>
	/// Field helper name from the owner and member, e.g. GeneratedViewableFieldView_Sandbox_Creature_Health;
	/// a descriptor row uses the descriptor type and the row label.
	/// </summary>
	private static string GetFieldViewName(ViewableFieldData field) {
		var owner = field.DescriptorTypeName ?? field.TargetTypeName ?? string.Empty;
		var member = field.MemberName ?? field.Label;
		return "GeneratedViewableFieldView_" + ToIdentifierPart(owner) + "_" + ToIdentifierPart(member);
	}

	/// <summary>Button helper name from the declaring (or descriptor) type and method name.</summary>
	private static string GetButtonViewName(ViewableButtonData button) {
		var owner = button.DescriptorTypeName ?? button.TargetTypeName;
		return "GeneratedViewableButtonView_" + ToIdentifierPart(owner) + "_" + ToIdentifierPart(button.MethodName);
	}

	/// <summary>
	/// Turns a type or member name into an identifier fragment:
	/// global::Sandbox.Outer.Inner becomes Sandbox_Outer_Inner.
	/// </summary>
	private static string ToIdentifierPart(string name) {
		var text = name.StartsWith("global::", StringComparison.Ordinal) ? name.Substring("global::".Length) : name;
		text = UnescapeIdentifier(text);
		var builder = new StringBuilder(text.Length);
		foreach (var character in text) {
			builder.Append(character < 128 && char.IsLetterOrDigit(character) ? character : '_');
		}
		return builder.ToString();
	}

	private static string UnescapeIdentifier(string identifier) {
		return identifier.StartsWith("@", StringComparison.Ordinal) ? identifier.Substring(1) : identifier;
	}

	/// <summary>
	/// Hands out class names, adding _2, _3 ... when two candidates sanitize to the same
	/// name. Callers ask in a sorted order, so the suffixes are deterministic.
	/// </summary>
	private sealed class UniqueNames {
		private readonly HashSet<string> used = new(StringComparer.Ordinal);

		internal string Get(string candidate) {
			var name = candidate;
			for (var suffix = 2; !used.Add(name); suffix++) {
				name = candidate + "_" + suffix.ToString(System.Globalization.CultureInfo.InvariantCulture);
			}
			return name;
		}
	}
}
