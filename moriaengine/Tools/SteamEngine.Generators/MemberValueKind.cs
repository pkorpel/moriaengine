namespace SteamEngine.Generators;

internal enum MemberValueKind {
	String,
	Boolean,
	Byte,
	SByte,
	Int16,
	UInt16,
	Int32,
	UInt32,
	Int64,
	UInt64,
	Single,
	Double,
	Decimal,
	Char,
	DateTime,
	TimeSpan,
	Enum,
	Object, // directly known to be Object (e.g. Tags)
	Globals,
	Thing,
	AbstractScript,
	Region,
	IAbstractKey,
	AbstractAccount,
	Fallback // unknown for the generator system. Like Object, but is not Object directly
}
