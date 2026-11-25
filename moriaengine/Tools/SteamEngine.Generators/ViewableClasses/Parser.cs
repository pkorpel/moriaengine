using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SteamEngine.Generators.ViewableClasses;

/// <summary>
/// First stage of the ViewableClass pipeline. For one type declaration it decides whether
/// the type is a view ([ViewableClass] or a Plugin subclass), a [ViewDescriptor], or neither,
/// walks its members and returns an equatable snapshot (no Roslyn symbols) plus diagnostics.
/// </summary>
internal static class Parser {
	// Fully qualified (global::...) names without nullable markers, used for emitted code and identities.
	private static readonly SymbolDisplayFormat TypeFormat =
		SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
			SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions
				& ~SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

	/// <summary>Cheap syntax filter: every class/struct/record declaration is a candidate.</summary>
	internal static bool IsCandidateNode(SyntaxNode node) => node is TypeDeclarationSyntax;

	/// <summary>
	/// Parses one declaration. Returns null when the type is irrelevant and produced no
	/// diagnostics, so most types in Scripts never reach the analyzer.
	/// </summary>
	internal static ViewableParseResult? Parse(
		GeneratorSyntaxContext context,
		RequiredSymbols? symbols) {
		if (symbols is null || !TryGetCandidateType(context, symbols, out var declaration, out var type)) {
			return null;
		}

		// Read both opt-in attributes; a Plugin subclass is viewable without [ViewableClass].
		var diagnostics = new List<Diagnostic>();
		var viewableAttribute = AttributeUsageValidator.FindAttributeOnSymbol(
			type,
			symbols.Attributes.ViewableClassAttribute,
			diagnostics);
		var descriptorAttribute = AttributeUsageValidator.FindAttributeOnSymbol(
			type,
			symbols.Attributes.ViewDescriptorAttribute,
			diagnostics);
		var isPlugin = NestedTypeValidator.IsAssignableTo(type, symbols.CanonicalTypes.PluginType, includeSelf: true);

		// A view of the type itself.
		if (viewableAttribute is not null || isPlugin) {
			return ParseViewableType(
				context.SemanticModel.Compilation,
				declaration,
				type,
				viewableAttribute,
				descriptorAttribute,
				symbols,
				diagnostics);
		}

		// Neither: keep only attribute-usage diagnostics, if any.
		if (descriptorAttribute is null) {
			return diagnostics.Count == 0
				? null
				: CreateResult(context.SemanticModel.Compilation, null, diagnostics);
		}

		return ParseDescriptorType(context.SemanticModel.Compilation, type, descriptorAttribute, symbols, diagnostics);
	}

	/// <summary>
	/// Resolves the declared symbol, accepting only the first declaration of a partial type
	/// (so it's parsed once) and only classes and structs.
	/// </summary>
	// GameCore has the generator but not the Scripts-only ViewableClassDataView contract.
	// With no contract, ignore viewable candidates for this compilation.
	private static bool TryGetCandidateType(
		GeneratorSyntaxContext context,
		RequiredSymbols symbols,
		out TypeDeclarationSyntax declaration,
		out INamedTypeSymbol type) {
		declaration = null!;
		type = null!;
		if (symbols.ViewableClassDataViewType is null
			|| context.Node is not TypeDeclarationSyntax candidateDeclaration
			|| context.SemanticModel.GetDeclaredSymbol(candidateDeclaration) is not INamedTypeSymbol candidateType
			|| !IsPrimaryDeclaration(candidateType, candidateDeclaration)
			|| (candidateType.TypeKind != TypeKind.Class && candidateType.TypeKind != TypeKind.Struct)) {
			return false;
		}

		declaration = candidateDeclaration;
		type = candidateType;
		return true;
	}

	/// <summary>Builds the snapshot of a [ViewableClass] type or Plugin subclass.</summary>
	private static ViewableParseResult ParseViewableType(
		Compilation compilation,
		TypeDeclarationSyntax declaration,
		INamedTypeSymbol type,
		AttributeData? viewableAttribute,
		AttributeData? descriptorAttribute,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics) {
		// A type cannot be both an opted-in view and a descriptor. For example, put
		// [ViewDescriptor(typeof(Item))] on a separate descriptor class, not on Item.
		if (descriptorAttribute is not null) {
			diagnostics.Add(Diagnostic.Create(
				Diagnostics.ConflictingViewableType,
				GetAttributeLocation(descriptorAttribute, type),
				GetTypeName(type)));
		}

		if (HasOpenTypeParameters(type)) {
			ReportInvalidMember(diagnostics, declaration.GetLocation(), GetTypeName(type), "generic viewable types are not supported");
			return CreateResult(compilation, null, diagnostics);
		}

		// An absent label defaults to the type name: [ViewableClass] Item -> "Item".
		var label = GetStringArgument(viewableAttribute, 0) ?? type.Name;
		var viewType = BuildViewTypeData(type, label, symbols, diagnostics, declaration.GetLocation(), isDescriptorTargetSnapshot: false, Array.Empty<string>());
		return CreateResult(compilation, new ViewableCandidate(viewType, null), diagnostics);
	}

	/// <summary>
	/// Parses a [ViewDescriptor] class into its rows and buttons, and snapshots the type it
	/// handles so that type gets a view of its own.
	/// </summary>
	private static ViewableParseResult ParseDescriptorType(
		Compilation compilation,
		INamedTypeSymbol type,
		AttributeData descriptorAttribute,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics) {
		if (type.TypeKind != TypeKind.Class || HasOpenTypeParameters(type)) {
			ReportInvalidDescriptor(diagnostics, GetAttributeLocation(descriptorAttribute, type), GetTypeName(type), "descriptors must be non-generic classes");
			return CreateResult(compilation, null, diagnostics);
		}

		if (!TryParseDescriptor(type, descriptorAttribute, symbols, diagnostics, out var descriptor, out var handledType)) {
			return CreateResult(compilation, null, diagnostics);
		}

		// A descriptor makes its handled type viewable even if that type has no source
		// declaration in Scripts; snapshot its public members from the referenced symbol.
		var handledView = BuildViewTypeData(
			handledType,
			handledType.Name,
			symbols,
			diagnostics,
			GetAttributeLocation(descriptorAttribute, type),
			isDescriptorTargetSnapshot: true,
			descriptor.NonDisplayedFields);
		return CreateResult(compilation, new ViewableCandidate(handledView, descriptor), diagnostics);
	}

	/// <summary>Wraps a candidate and its diagnostics into the cacheable parse result.</summary>
	private static ViewableParseResult CreateResult(
		Compilation compilation,
		ViewableCandidate? candidate,
		IEnumerable<Diagnostic> diagnostics) {
		// Store diagnostic text and a rebindable location, not Diagnostic/Location objects
		// tied to this compilation; e.g. a later transform can report the same error safely.
		var diagnosticData = diagnostics.Select(diagnostic => GeneratorDiagnosticData.FromDiagnostic(compilation, diagnostic));
		return new ViewableParseResult(candidate, EquatableArray<GeneratorDiagnosticData>.From(diagnosticData));
	}

	/// <summary>True for the first declaration of a (possibly partial) type.</summary>
	private static bool IsPrimaryDeclaration(INamedTypeSymbol type, TypeDeclarationSyntax declaration) {
		var first = type.DeclaringSyntaxReferences.FirstOrDefault();
		return first is not null
			&& first.SyntaxTree == declaration.SyntaxTree
			&& first.Span == declaration.Span;
	}

	/// <summary>True when the type, or a type containing it, still has unbound type parameters.</summary>
	private static bool HasOpenTypeParameters(INamedTypeSymbol type) {
		// Closed types such as List<int> can be emitted; an open definition such as
		// List<> cannot be used in a field cast or typeof expression.
		for (var current = type; current is not null; current = current.ContainingType) {
			if (current.TypeArguments.Any(static argument => argument.TypeKind == TypeKind.TypeParameter)) {
				return true;
			}
		}
		return type.IsUnboundGenericType;
	}

	/// <summary>
	/// Collects the rows and buttons of <paramref name="type"/> and its base types.
	/// <paramref name="hiddenFieldNames"/> (a descriptor's hidden list) is skipped before
	/// validation, so hiding an unsupported member also silences its error.
	/// </summary>
	private static ViewableTypeData BuildViewTypeData(
		INamedTypeSymbol type,
		string label,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		Location fallbackLocation,
		bool isDescriptorTargetSnapshot,
		IEnumerable<string> hiddenFieldNames) {
		// Keep symbol walking and per-member validation together; the renderer only sees
		// this immutable snapshot and never keeps Roslyn symbols past the transform.
		var members = new ViewableMemberCollector(type, symbols, diagnostics, fallbackLocation, hiddenFieldNames);
		members.Collect();

		return new ViewableTypeData(
			GetTypeName(type),
			GetTypeName(type),
			label,
			EquatableArray<string>.From(GetBaseTypeKeys(type)),
			EquatableArray<ViewableFieldData>.From(members.Fields),
			EquatableArray<ViewableButtonData>.From(members.Buttons),
			isDescriptorTargetSnapshot);
	}

	/// <summary>
	/// Walks a viewable type and its bases and turns public instance members into rows
	/// (fields, properties) and buttons ([Button] methods). The nearest declaration of a
	/// name wins, so overrides and shadowing members replace the base ones.
	/// </summary>
	private sealed class ViewableMemberCollector {
		private readonly INamedTypeSymbol targetType;
		private readonly RequiredSymbols symbols;
		private readonly ICollection<Diagnostic> diagnostics;
		private readonly Location fallbackLocation;
		private readonly List<ViewableFieldData> fields = new();
		private readonly List<ViewableButtonData> buttons = new();
		private readonly HashSet<string> fieldNames = new(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> propertyNames = new(StringComparer.OrdinalIgnoreCase);
		private readonly HashSet<string> buttonIdentities = new(StringComparer.Ordinal);
		private readonly HashSet<string> hiddenFieldNames;

		/// <summary>Creates a collector; call <see cref="Collect"/> once, then read Fields and Buttons.</summary>
		internal ViewableMemberCollector(
			INamedTypeSymbol targetType,
			RequiredSymbols symbols,
			ICollection<Diagnostic> diagnostics,
			Location fallbackLocation,
			IEnumerable<string> hiddenFieldNames) {
			this.targetType = targetType;
			this.symbols = symbols;
			this.diagnostics = diagnostics;
			this.fallbackLocation = fallbackLocation;
			this.hiddenFieldNames = new HashSet<string>(hiddenFieldNames, StringComparer.Ordinal);
		}

		internal IReadOnlyList<ViewableFieldData> Fields => fields;
		internal IReadOnlyList<ViewableButtonData> Buttons => buttons;

		/// <summary>Collects members from the target type out to (not including) object.</summary>
		internal void Collect() {
			// Walk from the target toward its bases. If both Derived and Base expose
			// `Name`, the derived declaration wins; other public base members are inherited.
			for (var current = targetType; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType) {
				foreach (var member in current.GetMembers()) {
					Add(member);
				}
			}
		}

		/// <summary>Routes a public instance member to the matching Add method; others are ignored.</summary>
		private void Add(ISymbol member) {
			if (member.IsStatic || member.DeclaredAccessibility != Accessibility.Public || member.IsImplicitlyDeclared) {
				return;
			}

			switch (member) {
				case IFieldSymbol field:
					AddField(field);
					break;
				case IPropertySymbol property:
					AddProperty(property);
					break;
				case IMethodSymbol method:
					AddButton(method);
					break;
			}
		}

		/// <summary>True when the member is declared in source of this compilation.</summary>
		// Members compiled into another assembly (System, 3rd party, GameCore) can't
		// take [NoShow], so unsupported ones are skipped instead of reported.
		private static bool CanBeAnnotated(ISymbol member) {
			return member.Locations.Any(static location => location.IsInSource);
		}

		/// <summary>Adds a field row unless the name is already taken, hidden or [NoShow]; readonly and const fields are read-only.</summary>
		private void AddField(IFieldSymbol field) {
			// Record the name before honoring [NoShow], so a hidden derived member also
			// hides the base member it shadows instead of letting the base one through.
			if (!fieldNames.Add(field.Name)
				|| hiddenFieldNames.Contains(field.Name)
				|| AttributeUsageValidator.HasAttribute(field, symbols.Attributes.NoShowAttribute, diagnostics)) {
				return;
			}

			if (TryCreateField(
				field,
				targetType,
				field.Name,
				field.Type,
				field.IsReadOnly || field.IsConst,
				symbols,
				diagnostics,
				fallbackLocation,
				CanBeAnnotated(field),
				out var fieldData)) {
				fields.Add(fieldData);
			}
		}

		/// <summary>Adds a property row; it's editable only with a public, non-init setter on a class.</summary>
		private void AddProperty(IPropertySymbol property) {
			if (!propertyNames.Add(property.Name)
				|| hiddenFieldNames.Contains(property.Name)
				|| AttributeUsageValidator.HasAttribute(property, symbols.Attributes.NoShowAttribute, diagnostics)) {
				return;
			}

			// Indexers and properties without a public getter can't be shown.
			var reportInvalid = CanBeAnnotated(property);
			if (property.Parameters.Length != 0) {
				if (reportInvalid) {
					ReportInvalidMember(diagnostics, GetLocation(property, fallbackLocation), property.Name, "indexers are not supported");
				}
				return;
			}
			if (property.GetMethod is null || property.GetMethod.DeclaredAccessibility != Accessibility.Public) {
				if (reportInvalid) {
					ReportInvalidMember(diagnostics, GetLocation(property, fallbackLocation), property.Name, "a public getter is required");
				}
				return;
			}

			// A public non-init setter makes class properties editable; struct views
			// stay read-only because the generated setter would otherwise mutate a copy.
			var canWrite = property.SetMethod?.DeclaredAccessibility == Accessibility.Public
				&& !property.SetMethod.IsInitOnly
				&& !targetType.IsValueType;
			if (TryCreateField(
				property,
				targetType,
				property.Name,
				property.Type,
				!canWrite,
				symbols,
				diagnostics,
				fallbackLocation,
				reportInvalid,
				out var propertyData)) {
				fields.Add(propertyData);
			}
		}

		/// <summary>Adds a [Button] method as an action button, once per override family.</summary>
		private void AddButton(IMethodSymbol method) {
			var attribute = AttributeUsageValidator.FindAttributeOnSymbol(
				method,
				symbols.Attributes.ButtonAttribute,
				diagnostics);
			if (attribute is null) {
				return;
			}
			if (!IsCallableWithoutArguments(method)) {
				ReportInvalidMember(diagnostics, GetAttributeLocation(attribute, method), method.Name, "buttons must be instance methods callable without arguments");
				return;
			}

			// Identify the button by the method it originally overrides, so a derived override
			// replaces the base button instead of adding a second one.
			var rootMethod = method;
			while (rootMethod.OverriddenMethod is not null) {
				rootMethod = rootMethod.OverriddenMethod;
			}
			var buttonIdentity = "method|" + rootMethod.OriginalDefinition.ToDisplayString(TypeFormat);
			if (!buttonIdentities.Add(buttonIdentity)) {
				return;
			}

			// The root identifies an override family, while the declaring type keeps
			// helpers safe for each view. BaseView casts to Base; DerivedView casts to Derived.
			var declaringTypeName = GetTypeName(method.ContainingType);
			buttons.Add(new ViewableButtonData(
				buttonIdentity + "|target|" + declaringTypeName,
				GetStringArgument(attribute, 0) ?? method.Name,
				declaringTypeName,
				EscapeIdentifier(method.Name),
				DescriptorTypeName: null));
		}
	}

	/// <summary>
	/// Builds the row for a field or property: label ([InfoField] or member name), type, text
	/// to value conversion and writability. Unrepresentable types are reported (when
	/// <paramref name="reportInvalid"/>) and skipped.
	/// </summary>
	private static bool TryCreateField(
		ISymbol member,
		INamedTypeSymbol targetType,
		string labelFallback,
		ITypeSymbol fieldType,
		bool readOnly,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		Location fallbackLocation,
		bool reportInvalid,
		out ViewableFieldData fieldData) {
		fieldData = null!;
		if (!IsRepresentableFieldType(fieldType)) {
			if (reportInvalid) {
				ReportInvalidMember(diagnostics, GetLocation(member, fallbackLocation), member.Name, "the member type cannot be represented as an object-valued DataView field");
			}
			return false;
		}

		// [InfoField("...")] only renames the row.
		var infoFieldAttribute = AttributeUsageValidator.FindAttributeOnSymbol(
			member,
			symbols.Attributes.InfoFieldAttribute,
			diagnostics);
		var fieldTypeName = GetTypeName(fieldType);
		var loadExpression = GetLoadExpression(fieldType, fieldTypeName, symbols, diagnostics, GetLocation(member, fallbackLocation));
		fieldData = new ViewableFieldData(
			"member|" + member.Kind + "|" + member.ToDisplayString(TypeFormat),
			GetStringArgument(infoFieldAttribute, 0) ?? labelFallback,
			fieldTypeName,
			loadExpression,
			!readOnly && !targetType.IsValueType,
			GetTypeName(member.ContainingType),
			EscapeIdentifier(member.Name),
			DescriptorTypeName: null,
			GetterMethodName: null,
			SetterMethodName: null);
		return true;
	}

	/// <summary>True when values of the type can be boxed into the object-based IDataFieldView contract.</summary>
	private static bool IsRepresentableFieldType(ITypeSymbol fieldType) {
		// IDataFieldView boxes values as object. Span<int>, pointers, and open List<>
		// cannot safely pass through that contract; dynamic is emitted as object instead.
		return fieldType.TypeKind != TypeKind.Error
			&& fieldType.TypeKind != TypeKind.Pointer
			&& fieldType.TypeKind != TypeKind.FunctionPointer
			&& fieldType.SpecialType != SpecialType.System_Void
			&& !fieldType.IsRefLikeType
			&& (fieldType is not INamedTypeSymbol namedType || !HasOpenTypeParameters(namedType));
	}

	/// <summary>Validates the descriptor attribute and collects the descriptor's Get/Set/Button methods.</summary>
	private static bool TryParseDescriptor(
		INamedTypeSymbol descriptorType,
		AttributeData descriptorAttribute,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		out ViewDescriptorData descriptor,
		out INamedTypeSymbol handledType) {
		descriptor = null!;
		handledType = null!;
		var location = GetAttributeLocation(descriptorAttribute, descriptorType);
		if (!TryGetHandledType(descriptorType, descriptorAttribute, location, diagnostics, out handledType)) {
			return false;
		}

		var collector = new DescriptorMemberCollector(descriptorType, symbols, diagnostics, location);
		collector.Collect();
		descriptor = collector.Build(
			handledType,
			GetStringArgument(descriptorAttribute, 1) ?? handledType.Name,
			GetStringArrayArgument(descriptorAttribute, 2));
		return true;
	}

	/// <summary>Reads typeof(T) from [ViewDescriptor]; T must be a closed class or struct.</summary>
	private static bool TryGetHandledType(
		INamedTypeSymbol descriptorType,
		AttributeData descriptorAttribute,
		Location location,
		ICollection<Diagnostic> diagnostics,
		out INamedTypeSymbol handledType) {
		handledType = null!;
		if (descriptorAttribute.ConstructorArguments.Length == 0
			|| descriptorAttribute.ConstructorArguments[0].Value is not INamedTypeSymbol handledTypeSymbol
			|| (handledTypeSymbol.TypeKind != TypeKind.Class && handledTypeSymbol.TypeKind != TypeKind.Struct)
			|| HasOpenTypeParameters(handledTypeSymbol)) {
			ReportInvalidDescriptor(diagnostics, location, GetTypeName(descriptorType), "the handled type must be a closed class or struct");
			return false;
		}
		handledType = handledTypeSymbol;
		return true;
	}

	/// <summary>
	/// Collects the public static methods of a descriptor class: [Button] methods become
	/// buttons, and [GetMethod]/[SetMethod] pairs with the same label become one row.
	/// </summary>
	private sealed class DescriptorMemberCollector {
		private readonly INamedTypeSymbol descriptorType;
		private readonly RequiredSymbols symbols;
		private readonly ICollection<Diagnostic> diagnostics;
		private readonly Location descriptorLocation;
		private readonly Dictionary<string, DescriptorFieldBuilder> fields = new(StringComparer.OrdinalIgnoreCase);
		private readonly List<ViewableButtonData> buttons = new();

		/// <summary>Creates a collector; call <see cref="Collect"/>, then <see cref="Build"/>.</summary>
		internal DescriptorMemberCollector(
			INamedTypeSymbol descriptorType,
			RequiredSymbols symbols,
			ICollection<Diagnostic> diagnostics,
			Location descriptorLocation) {
			this.descriptorType = descriptorType;
			this.symbols = symbols;
			this.diagnostics = diagnostics;
			this.descriptorLocation = descriptorLocation;
		}

		/// <summary>Inspects the descriptor's own public static methods.</summary>
		internal void Collect() {
			// Descriptors are explicit adapters, so inspect only methods declared on
			// this class. For example, ItemDescriptor.FixWeight(object) becomes a button.
			foreach (var member in descriptorType.GetMembers()) {
				if (member is IMethodSymbol method
					&& method.DeclaredAccessibility == Accessibility.Public
					&& method.IsStatic
					&& !method.IsImplicitlyDeclared) {
					AddMethod(method);
				}
			}
		}

		/// <summary>Builds the descriptor snapshot with its label and hidden member names.</summary>
		internal ViewDescriptorData Build(INamedTypeSymbol handledType, string label, string[] nonDisplayedFields) {
			var viewFields = BuildFields();
			return new ViewDescriptorData(
				GetTypeName(descriptorType),
				GetTypeName(handledType),
				label,
				EquatableArray<string>.From(nonDisplayedFields),
				EquatableArray<ViewableFieldData>.From(viewFields),
				EquatableArray<ViewableButtonData>.From(buttons));
		}

		/// <summary>Applies the method's Button, GetMethod and SetMethod attributes, in that order.</summary>
		private void AddMethod(IMethodSymbol method) {
			// A method may have more than one view attribute. Preserve the old ordered
			// checks: an invalid button/getter skips the rest of that method's metadata.
			var buttonAttribute = AttributeUsageValidator.FindAttributeOnSymbol(method, symbols.Attributes.ButtonAttribute, diagnostics);
			if (buttonAttribute is not null) {
				if (!TryAddButton(method, buttonAttribute)) {
					return;
				}
			}

			var getAttribute = AttributeUsageValidator.FindAttributeOnSymbol(method, symbols.Attributes.GetMethodAttribute, diagnostics);
			if (getAttribute is not null && !TryAddGetter(method, getAttribute)) {
				return;
			}

			var setAttribute = AttributeUsageValidator.FindAttributeOnSymbol(method, symbols.Attributes.SetMethodAttribute, diagnostics);
			if (setAttribute is not null) {
				TryAddSetter(method, setAttribute);
			}
		}

		/// <summary>Adds a static Method(object target) as a descriptor button.</summary>
		private bool TryAddButton(IMethodSymbol method, AttributeData attribute) {
			if (method.IsGenericMethod
				|| method.Parameters.Length != 1
				|| method.Parameters[0].Type.SpecialType != SpecialType.System_Object
				|| method.Parameters[0].RefKind != RefKind.None) {
				ReportInvalidDescriptor(diagnostics, GetAttributeLocation(attribute, method), method.Name, "descriptor buttons must be public static methods taking one object argument");
				return false;
			}

			var descriptorTypeName = GetTypeName(descriptorType);
			buttons.Add(new ViewableButtonData(
				"descriptor-button|" + descriptorTypeName + "|" + method.ToDisplayString(TypeFormat),
				GetStringArgument(attribute, 0) ?? method.Name,
				descriptorTypeName,
				EscapeIdentifier(method.Name),
				descriptorTypeName));
			return true;
		}

		/// <summary>Registers the getter of a descriptor row; a row has at most one.</summary>
		private bool TryAddGetter(IMethodSymbol method, AttributeData attribute) {
			if (!IsDescriptorGetter(method)) {
				ReportInvalidDescriptor(diagnostics, GetAttributeLocation(attribute, method), method.Name, "get methods must be public static methods taking one object argument");
				return false;
			}
			if (!TryGetField(attribute, method, out var field)) {
				return false;
			}
			// One descriptor field label (case-insensitive) has exactly one getter.
			if (field.GetterMethodName is not null) {
				ReportInvalidDescriptor(diagnostics, GetAttributeLocation(attribute, method), method.Name, "the descriptor field has more than one getter");
				return false;
			}

			field.GetterMethodName = EscapeIdentifier(method.Name);
			return true;
		}

		/// <summary>Registers the optional setter of a descriptor row; a row has at most one.</summary>
		private bool TryAddSetter(IMethodSymbol method, AttributeData attribute) {
			if (!IsDescriptorSetter(method)) {
				ReportInvalidDescriptor(diagnostics, GetAttributeLocation(attribute, method), method.Name, "set methods must be public static methods taking two object arguments");
				return false;
			}
			if (!TryGetField(attribute, method, out var field)) {
				return false;
			}
			// A setter is optional, so Get-only descriptor fields remain read-only.
			if (field.SetterMethodName is not null) {
				ReportInvalidDescriptor(diagnostics, GetAttributeLocation(attribute, method), method.Name, "the descriptor field has more than one setter");
				return false;
			}

			field.SetterMethodName = EscapeIdentifier(method.Name);
			return true;
		}

		/// <summary>Finds or creates the row named by a Get/Set attribute.</summary>
		private bool TryGetField(AttributeData attribute, IMethodSymbol method, out DescriptorFieldBuilder field) {
			return TryGetDescriptorField(attribute, method, fields, symbols, diagnostics, out field);
		}

		/// <summary>Turns the collected rows into row data; a row without a getter is reported and dropped.</summary>
		private List<ViewableFieldData> BuildFields() {
			var viewFields = new List<ViewableFieldData>();
			foreach (var field in fields.Values) {
				// For example, SetMethod("TagsCount", ...) without a matching GetMethod
				// is diagnosed and omitted rather than generating a setter-only field.
				if (field.GetterMethodName is null) {
					ReportInvalidDescriptor(diagnostics, descriptorLocation, field.Label, "a getter is required");
					continue;
				}

				viewFields.Add(new ViewableFieldData(
					"descriptor-field|" + GetTypeName(descriptorType) + "|" + field.Label.ToUpperInvariant(),
					field.Label,
					field.FieldTypeName,
					field.LoadExpression,
					field.SetterMethodName is not null,
					TargetTypeName: null,
					MemberName: null,
					DescriptorTypeName: GetTypeName(descriptorType),
					GetterMethodName: field.GetterMethodName,
					SetterMethodName: field.SetterMethodName));
			}
			return viewFields;
		}
	}

	/// <summary>Getter shape: static object-in, non-void out, not generic.</summary>
	private static bool IsDescriptorGetter(IMethodSymbol method) {
		return !method.IsGenericMethod
			&& method.Parameters.Length == 1
			&& method.Parameters[0].Type.SpecialType == SpecialType.System_Object
			&& method.Parameters[0].RefKind == RefKind.None
			&& !method.ReturnsVoid;
	}

	/// <summary>Setter shape: static (object target, object value), not generic.</summary>
	private static bool IsDescriptorSetter(IMethodSymbol method) {
		return !method.IsGenericMethod
			&& method.Parameters.Length == 2
			&& method.Parameters.All(static parameter =>
				parameter.Type.SpecialType == SpecialType.System_Object
				&& parameter.RefKind == RefKind.None);
	}

	/// <summary>True for instance methods a button can call with no arguments.</summary>
	private static bool IsCallableWithoutArguments(IMethodSymbol method) {
		// Optional parameters and params arrays are callable with no arguments, e.g.
		// Refresh(bool force = false) or Reset(params object[] options).
		if (method.IsStatic || method.IsGenericMethod || method.ReturnsByRef || method.ReturnsByRefReadonly) {
			return false;
		}
		return method.Parameters.All(static parameter => parameter.IsOptional || parameter.IsParams);
	}

	/// <summary>
	/// Looks up (case-insensitively) or creates the descriptor row named in a Get/Set
	/// attribute, checking that the getter and setter agree on the row type.
	/// </summary>
	private static bool TryGetDescriptorField(
		AttributeData attribute,
		IMethodSymbol method,
		IDictionary<string, DescriptorFieldBuilder> fields,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		out DescriptorFieldBuilder field) {
		field = null!;
		// The attribute's typeof(T) is the displayed field type, shared by its getter
		// and setter; it is not inferred from the getter's return type.
		if (attribute.ConstructorArguments.Length < 2
			|| attribute.ConstructorArguments[0].Value is not string label
			|| attribute.ConstructorArguments[1].Value is not ITypeSymbol fieldType
			|| !IsRepresentableFieldType(fieldType)) {
			ReportInvalidDescriptor(diagnostics, GetAttributeLocation(attribute, method), method.Name, "a field name and representable field type are required");
			return false;
		}

		var fieldTypeName = GetTypeName(fieldType);
		var loadExpression = GetLoadExpression(fieldType, fieldTypeName, symbols, diagnostics, GetAttributeLocation(attribute, method));
		// First attribute for this label creates the row; a later one must use the same type.
		if (!fields.TryGetValue(label, out field)) {
			field = new DescriptorFieldBuilder(label, fieldTypeName, loadExpression);
			fields.Add(label, field);
			return true;
		}

		if (!string.Equals(field.FieldTypeName, fieldTypeName, StringComparison.Ordinal)) {
			ReportInvalidDescriptor(diagnostics, GetAttributeLocation(attribute, method), label, "the getter and setter field types do not match");
			return false;
		}
		return true;
	}

	/// <summary>
	/// C# expression that converts the edit box text (variable <c>value</c>) into the row type,
	/// used by the generated SetStringValue.
	/// </summary>
	private static string GetLoadExpression(
		ITypeSymbol type,
		string typeName,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		Location location) {
		var kind = GetMemberValueKind(type, symbols, diagnostics, location);
		if (kind == MemberValueKind.Enum && type is INamedTypeSymbol enumType && enumType.EnumUnderlyingType is not null) {
			return GetEnumLoadExpression(enumType, typeName, symbols, diagnostics, location);
		}

		if (IsSimpleLoadKind(kind)) {
			return MemberValueLoadRenderer.GetSimpleExpression(kind, typeName, "value");
		}

		return GetFallbackLoadExpression(kind, typeName);
	}

	/// <summary>Classifies a type for conversion, warning when a user type shadows an engine type name.</summary>
	private static MemberValueKind GetMemberValueKind(
		ITypeSymbol type,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		Location location) {
		return MemberValueKindClassifier.GetMemberValueKind(
			type,
			symbols.CanonicalTypes,
			(actual, canonical) => diagnostics.Add(Diagnostic.Create(
				Diagnostics.TypeNameConflict,
				actual.Locations.FirstOrDefault() ?? location,
				actual.ToDisplayString(),
				canonical.ToDisplayString())));
	}

	/// <summary>Conversion expression for an enum, via its underlying integer type.</summary>
	private static string GetEnumLoadExpression(
		INamedTypeSymbol enumType,
		string typeName,
		RequiredSymbols symbols,
		ICollection<Diagnostic> diagnostics,
		Location location) {
		// Parse enums through their storage type, then cast back: a short-backed
		// CreatureMode uses ParseInt16(value) before the generated enum cast.
		var underlyingType = enumType.EnumUnderlyingType!;
		var underlyingKind = GetMemberValueKind(underlyingType, symbols, diagnostics, location);
		var underlyingTypeName = GetTypeName(underlyingType);
		return "(" + typeName + ") " + MemberValueLoadRenderer.GetSimpleExpression(underlyingKind, underlyingTypeName, "value");
	}

	/// <summary>Conversion expression for types without a direct parser.</summary>
	private static string GetFallbackLoadExpression(MemberValueKind kind, string typeName) {
		// Object fields need the persistence loader; other complex types use the
		// simple-type loader and cast, matching the old .info conversion behavior.
		var loadedValue = "global::SteamEngine.Persistence.ObjectSaver.OptimizedLoad_SimpleType(value, typeof(" + typeName + "))";
		if (kind == MemberValueKind.Object) {
			return "global::SteamEngine.Persistence.ObjectSaver.Load(value)";
		}
		if (kind == MemberValueKind.DateTime) {
			return "global::System.Convert.ToDateTime(" + loadedValue + ")";
		}
		return "(" + typeName + ") " + loadedValue;
	}

	/// <summary>True for kinds with a direct ConvertTools/ObjectSaver parser.</summary>
	private static bool IsSimpleLoadKind(MemberValueKind kind) {
		return kind is MemberValueKind.String
			or MemberValueKind.Boolean
			or MemberValueKind.Byte
			or MemberValueKind.SByte
			or MemberValueKind.Int16
			or MemberValueKind.UInt16
			or MemberValueKind.Int32
			or MemberValueKind.UInt32
			or MemberValueKind.Int64
			or MemberValueKind.UInt64
			or MemberValueKind.Single
			or MemberValueKind.Double
			or MemberValueKind.Decimal
			or MemberValueKind.Char
			or MemberValueKind.TimeSpan
			or MemberValueKind.Globals;
	}

	/// <summary>Base type names from the direct base outward, excluding object.</summary>
	private static string[] GetBaseTypeKeys(INamedTypeSymbol type) {
		var bases = new List<string>();
		for (var current = type.BaseType; current is not null && current.SpecialType != SpecialType.System_Object; current = current.BaseType) {
			bases.Add(GetTypeName(current));
		}
		return bases.ToArray();
	}

	/// <summary>Fully qualified name for emitted code; dynamic becomes object.</summary>
	private static string GetTypeName(ITypeSymbol type) {
		if (type.TypeKind == TypeKind.Dynamic) {
			return "global::System.Object";
		}
		return type.ToDisplayString(TypeFormat);
	}

	/// <summary>Prefixes keywords with @ so member names like <c>return</c> compile.</summary>
	private static string EscapeIdentifier(string name) {
		return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None
			|| SyntaxFacts.GetContextualKeywordKind(name) != SyntaxKind.None
			? "@" + name
			: name;
	}

	/// <summary>Reads a string constructor argument of an attribute, or null.</summary>
	private static string? GetStringArgument(AttributeData? attribute, int index) {
		return attribute is not null
			&& attribute.ConstructorArguments.Length > index
			&& attribute.ConstructorArguments[index].Value is string value
				? value
				: null;
	}

	/// <summary>Reads a string[] constructor argument of an attribute, or an empty array.</summary>
	private static string[] GetStringArrayArgument(AttributeData attribute, int index) {
		if (attribute.ConstructorArguments.Length <= index
			|| attribute.ConstructorArguments[index].Kind != TypedConstantKind.Array) {
			return Array.Empty<string>();
		}
		return attribute.ConstructorArguments[index].Values
			.Where(static value => value.Value is string)
			.Select(static value => (string)value.Value!)
			.ToArray();
	}

	/// <summary>Where to report a problem with an attribute: the attribute itself, else the symbol.</summary>
	private static Location GetAttributeLocation(AttributeData attribute, ISymbol fallback) {
		return attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
			?? fallback.Locations.FirstOrDefault()
			?? Location.None;
	}

	/// <summary>The symbol's source location, or the fallback for metadata symbols.</summary>
	private static Location GetLocation(ISymbol symbol, Location fallback) {
		return symbol.Locations.FirstOrDefault(static location => location.IsInSource) ?? fallback;
	}

	/// <summary>Reports SEGEN950.</summary>
	private static void ReportInvalidMember(ICollection<Diagnostic> diagnostics, Location location, string memberName, string reason) {
		diagnostics.Add(Diagnostic.Create(Diagnostics.InvalidViewableMember, location, memberName, reason));
	}

	/// <summary>Reports SEGEN951.</summary>
	private static void ReportInvalidDescriptor(ICollection<Diagnostic> diagnostics, Location location, string name, string reason) {
		diagnostics.Add(Diagnostic.Create(Diagnostics.InvalidViewDescriptor, location, name, reason));
	}

	/// <summary>Mutable row being assembled from a descriptor's Get/Set methods.</summary>
	private sealed class DescriptorFieldBuilder {
		internal DescriptorFieldBuilder(string label, string fieldTypeName, string loadExpression) {
			Label = label;
			FieldTypeName = fieldTypeName;
			LoadExpression = loadExpression;
		}

		internal string Label { get; }
		internal string FieldTypeName { get; }
		internal string LoadExpression { get; }
		internal string? GetterMethodName { get; set; }
		internal string? SetterMethodName { get; set; }
	}
}
