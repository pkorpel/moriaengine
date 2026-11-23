using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace SteamEngine.Generators;

internal sealed class RequiredTypeValidator {
	private sealed record TypeCheckResult(bool Available, EquatableArray<Diagnostic> Diagnostics);

	// Syntax transforms and the compilation check share one resolution without retaining old compilations.
	// Lazy ensures concurrent lookups use the same resolution even if the weak-table factory runs more than once.
	private readonly ConditionalWeakTable<Compilation, Lazy<RequiredTypeResolution>> resolutions = new();

	internal static readonly DiagnosticDescriptor MissingGeneratorAttributeDeclarationDescriptor = new(
		"SEGEN900",
		"Missing generator attribute declaration",
		"Required generator attribute declaration '{0}' was not found",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	internal static readonly DiagnosticDescriptor MissingCanonicalEngineTypeDescriptor = new(
		"SEGEN901",
		"Missing canonical engine type",
		"Required canonical engine type '{0}' was not found",
		"Usage",
		DiagnosticSeverity.Error,
		true);

	// Report missing types and expose their availability to the incremental pipeline.
	internal IncrementalValueProvider<bool> RegisterRequiredTypeCheck(IncrementalGeneratorInitializationContext context) {
		var check = context.CompilationProvider.Select((compilation, _) => {
			var resolution = Resolve(compilation);
			return new TypeCheckResult(resolution.Symbols is not null, resolution.Diagnostics);
		});
		context.RegisterSourceOutput(check, static (spc, result) => {
			foreach (var diagnostic in result.Diagnostics) {
				spc.ReportDiagnostic(diagnostic);
			}
		});
		return check.Select(static (result, _) => result.Available);
	}

	internal RequiredTypeResolution Resolve(Compilation compilation) {
		return resolutions.GetValue(compilation,
			static value => new Lazy<RequiredTypeResolution>(() => ResolveCore(value))).Value;
	}

	private static RequiredTypeResolution ResolveCore(Compilation compilation) {
		var diagnostics = new List<Diagnostic>();
		var symbols = ResolveRequiredSymbols(compilation, diagnostics);
		return new RequiredTypeResolution(symbols, EquatableArray<Diagnostic>.From(diagnostics));
	}

	private static RequiredSymbols? ResolveRequiredSymbols(
		Compilation compilation,
		ICollection<Diagnostic> diagnostics) {
		var allPresent = true;
		var saveableDataAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.SaveableData, ref allPresent);
		var copyableDataAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.CopyableData, ref allPresent);
		var steamFunctionAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.SteamFunction, ref allPresent);
		var thingTemplateAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.ThingTemplate, ref allPresent);
		var thingDefTemplateAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.ThingDefTemplate, ref allPresent);
		var pluginTemplateAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.PluginTemplate, ref allPresent);
		var pluginDefTemplateAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.PluginDefTemplate, ref allPresent);
		var defFieldAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.DefField, ref allPresent);
		var deepCopyableClassAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.DeepCopyableClass, ref allPresent);
		var deepCopyImplementationAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.DeepCopyImplementation, ref allPresent);
		var saveableClassAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.SaveableClass, ref allPresent);
		var loadingInitializerAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.LoadingInitializer, ref allPresent);
		var loadingFinalizerAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.LoadingFinalizer, ref allPresent);
		var loadSectionAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.LoadSection, ref allPresent);
		var loadLineAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.LoadLine, ref allPresent);
		var saveAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.Save, ref allPresent);
		var viewableClassAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.ViewableClass, ref allPresent);
		var noShowAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.NoShow, ref allPresent);
		var infoFieldAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.InfoField, ref allPresent);
		var buttonAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.Button, ref allPresent);
		var viewDescriptorAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.ViewDescriptor, ref allPresent);
		var getMethodAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.GetMethod, ref allPresent);
		var setMethodAttribute = ResolveAttribute(compilation, diagnostics, MetadataNames.AttributeTypes.SetMethod, ref allPresent);

		var thingType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.Thing, ref allPresent);
		var abstractScriptType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.AbstractScript, ref allPresent);
		var regionType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.Region, ref allPresent);
		var globalsType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.Globals, ref allPresent);
		var iAbstractKeyType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.IAbstractKey, ref allPresent);
		var abstractAccountType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.AbstractAccount, ref allPresent);
		var iDeepCopyImplementorType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.IDeepCopyImplementor, ref allPresent);
		var deepCopyFactoryType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.DeepCopyFactory, ref allPresent);
		var scriptHolderType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.ScriptHolder, ref allPresent);
		var scriptArgsType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.ScriptArgs, ref allPresent);
		var pluginType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.Plugin, ref allPresent);
		var compiledScriptHolderType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.CompiledScriptHolder, ref allPresent);
		var compiledTriggerGroupType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.CompiledTriggerGroup, ref allPresent);
		var decoratedClassesSaveImplementorType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.DecoratedClassesSaveImplementor, ref allPresent);
		var propsSectionType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.PropsSection, ref allPresent);
		var propsLineType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.PropsLine, ref allPresent);
		var saveStreamType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.SaveStream, ref allPresent);
		var loadObjectParamType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.LoadObjectParam, ref allPresent);
		var objectSaverType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.ObjectSaver, ref allPresent);
		var coreLoggerType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.CoreLogger, ref allPresent);
		var seExceptionType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.SEException, ref allPresent);
		var fatalExceptionType = ResolveCanonicalType(compilation, diagnostics, MetadataNames.EngineTypes.FatalException, ref allPresent);

		if (!allPresent) {
			return null;
		}

		var canonicalTypes = new CanonicalTypeSymbols {
			ThingType = thingType!,
			AbstractScriptType = abstractScriptType!,
			RegionType = regionType!,
			GlobalsType = globalsType!,
			IAbstractKeyType = iAbstractKeyType!,
			AbstractAccountType = abstractAccountType!,
			ScriptHolderType = scriptHolderType!,
			ScriptArgsType = scriptArgsType!,
			PluginType = pluginType!,
			CompiledScriptHolderType = compiledScriptHolderType!,
			CompiledTriggerGroupType = compiledTriggerGroupType!,
			DeepCopyFactoryType = deepCopyFactoryType!,
			IDeepCopyImplementorType = iDeepCopyImplementorType!,
			PropsSectionType = propsSectionType!,
			PropsLineType = propsLineType!,
			SaveStreamType = saveStreamType!,
			LoadObjectParamType = loadObjectParamType!,
			ObjectSaverType = objectSaverType!,
			DecoratedClassesSaveImplementorType = decoratedClassesSaveImplementorType!,
			CoreLoggerType = coreLoggerType!,
			SEExceptionType = seExceptionType!,
			FatalExceptionType = fatalExceptionType!
		};

		var attributes = new GeneratorAttributeSymbols {
			SaveableDataAttribute = saveableDataAttribute!,
			CopyableDataAttribute = copyableDataAttribute!,
			SteamFunctionAttribute = steamFunctionAttribute!,
			ThingTemplateAttribute = thingTemplateAttribute!,
			ThingDefTemplateAttribute = thingDefTemplateAttribute!,
			PluginTemplateAttribute = pluginTemplateAttribute!,
			PluginDefTemplateAttribute = pluginDefTemplateAttribute!,
			DefFieldAttribute = defFieldAttribute!,
			DeepCopyableClassAttribute = deepCopyableClassAttribute!,
			DeepCopyImplementationAttribute = deepCopyImplementationAttribute!,
			SaveableClassAttribute = saveableClassAttribute!,
			LoadingInitializerAttribute = loadingInitializerAttribute!,
			LoadingFinalizerAttribute = loadingFinalizerAttribute!,
			LoadSectionAttribute = loadSectionAttribute!,
			LoadLineAttribute = loadLineAttribute!,
			SaveAttribute = saveAttribute!,
			ViewableClassAttribute = viewableClassAttribute!,
			NoShowAttribute = noShowAttribute!,
			InfoFieldAttribute = infoFieldAttribute!,
			ButtonAttribute = buttonAttribute!,
			ViewDescriptorAttribute = viewDescriptorAttribute!,
			GetMethodAttribute = getMethodAttribute!,
			SetMethodAttribute = setMethodAttribute!
		};

		var viewableClassDataViewType = compilation.GetTypeByMetadataName(MetadataNames.EngineTypes.ViewableClassDataView);
		return new RequiredSymbols(canonicalTypes, attributes, viewableClassDataViewType);
	}

	private static INamedTypeSymbol? ResolveAttribute(
		Compilation compilation,
		ICollection<Diagnostic> diagnostics,
		string metadataName,
		ref bool allPresent) =>
		ResolveType(compilation, diagnostics, metadataName, MissingGeneratorAttributeDeclarationDescriptor, ref allPresent);

	private static INamedTypeSymbol? ResolveCanonicalType(
		Compilation compilation,
		ICollection<Diagnostic> diagnostics,
		string metadataName,
		ref bool allPresent) =>
		ResolveType(compilation, diagnostics, metadataName, MissingCanonicalEngineTypeDescriptor, ref allPresent);

	private static INamedTypeSymbol? ResolveType(
		Compilation compilation,
		ICollection<Diagnostic> diagnostics,
		string metadataName,
		DiagnosticDescriptor descriptor,
		ref bool allPresent) {
		var symbol = compilation.GetTypeByMetadataName(metadataName);
		if (symbol is not null) {
			return symbol;
		}

		diagnostics.Add(Diagnostic.Create(
			descriptor,
			location: null,
			metadataName));
		allPresent = false;
		return null;
	}
}
