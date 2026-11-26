using System;

namespace SteamEngine.DIExperiment {
	[AttributeUsage(AttributeTargets.Class, Inherited = false)]
	internal sealed class IniSectionAttribute(string name) : Attribute {
		internal string Name { get; } = name;
	}
}
