<ClassProject>
  <Language>CSharp</Language>
  <Entities>
    <Entity type="CSharpInterface">
      <Name>ITriggerGroupHolder</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">void AddEvent()</Operation>
      <Operation type="CSharpMethod">void RemoveTriggerGroup()</Operation>
      <Operation type="CSharpMethod">void Trigger()</Operation>
      <Operation type="CSharpMethod">void CancellableTrigger()</Operation>
    </Entity>
    <Entity type="CSharpInterface">
      <Name>ITagHolder</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">void SetTag()</Operation>
      <Operation type="CSharpMethod">void GetTag()</Operation>
    </Entity>
    <Entity type="CSharpClass">
      <Name>TagHolder</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public Timer AddTimer()</Operation>
      <Operation type="CSharpMethod">public Timer RemoveTimer()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>GameAccount</Name>
      <Access>Public</Access>
      <Operation type="Property">public string Name { get; }</Operation>
      <Operation type="CSharpMethod">public static void Create()</Operation>
      <Operation type="CSharpMethod">public void Delete()</Operation>
      <Operation type="CSharpMethod">public void Block()</Operation>
      <Operation type="CSharpMethod">public void UnBlock()</Operation>
      <Operation type="Property">public int Plevel { get; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>Globals</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public static void Resync()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>Region</Name>
      <Access>Public</Access>
      <Operation type="Property">public string Defname { get; }</Operation>
      <Operation type="Property">public Point P { get; set; }</Operation>
      <Operation type="Property">public Region Parent { get; }</Operation>
      <Operation type="CSharpMethod">public void On_Exit()</Operation>
      <Operation type="CSharpMethod">public void On_Enter()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>GumpInstance</Name>
      <Access>Public</Access>
      <Operation type="Property">public int X { get; set; }</Operation>
      <Operation type="Property">public int Y { get; set; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>Conn</Name>
      <Access>Public</Access>
      <Operation type="Property">public bool IsConnected { get; set; }</Operation>
      <Operation type="Property">public IPAddress IP { get; set; }</Operation>
      <Operation type="CSharpMethod">public void WriteLine()</Operation>
      <Operation type="CSharpMethod">public void Close()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>FlaggedRegion</Name>
      <Access>Public</Access>
      <Operation type="Property">public int Flags { get; set; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>Thing</Name>
      <Access>Public</Access>
      <Operation type="Property">public int Uid { get; set; }</Operation>
      <Operation type="Property">public Point4D P { get; set; }</Operation>
      <Operation type="Property">public ThingDef def { get; set; }</Operation>
      <Operation type="Property">public bool IsChar { get; set; }</Operation>
      <Operation type="Property">public bool IsItem { get; set; }</Operation>
      <Operation type="CSharpMethod">public Thing Dupe()</Operation>
      <Operation type="CSharpMethod">public void Delete()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>GameConn</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public void Target()</Operation>
      <Operation type="CSharpMethod">public void Write()</Operation>
      <Operation type="Property">public ClientVersion Version { get; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>ConsConn</Name>
      <Access>Public</Access>
      <Operation type="Property">public bool IsNativeConsole { get; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>Memory</Name>
      <Access>Public</Access>
      <Operation type="Property">public Character Cont { get; }</Operation>
      <Operation type="Property">public int Flags { get; set; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>AbtractCharacter</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public void Equip()</Operation>
      <Operation type="Property">public GameConn Conn { get; set; }</Operation>
      <Operation type="Property">public GameAccount Account { get; set; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>AbstractItem</Name>
      <Access>Public</Access>
      <Operation type="Property">public Thing Cont { get; set; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>AbstractScript</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public static AbstractScript Get()</Operation>
      <Operation type="Property">public string Defname { get; set; }</Operation>
      <Operation type="CSharpMethod">public void Unload()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>AbstractDef</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public object GetCurrentFieldValue()</Operation>
      <Operation type="CSharpMethod">public object GetDefaultFieldValue()</Operation>
      <Operation type="CSharpMethod">public object GetTag()</Operation>
      <Operation type="CSharpMethod">public void SetTag()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>Gump</Name>
      <Access>Public</Access>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>CompiledGump</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public void Construct()</Operation>
      <Operation type="Property">public int X { get; set; }</Operation>
      <Operation type="Property">public int Y { get; set; }</Operation>
      <Operation type="CSharpMethod">public void AddButton()</Operation>
      <Operation type="CSharpMethod">public void AddTextentry()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>ScriptedGump</Name>
      <Access>Public</Access>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>TriggerGroup</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public object Run()</Operation>
      <Operation type="CSharpMethod">public object TryRun()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>ThingDef</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public Thing Create()</Operation>
      <Operation type="Property">public bool IsCharDef { get; set; }</Operation>
      <Operation type="Property">public bool IsItemDef { get; set; }</Operation>
      <Operation type="Property">public ushort Model { get; set; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>AbstractDefTriggerGroupHolder</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public void AddEvent()</Operation>
      <Operation type="CSharpMethod">public void RemoveEvent()</Operation>
      <Modifier>None</Modifier>
    </Entity>
    <Entity type="CSharpClass">
      <Name>AbstractSkillDef</Name>
      <Access>Public</Access>
      <Operation type="CSharpMethod">public void Select()</Operation>
      <Operation type="Property">public ushort Id { get; set; }</Operation>
      <Operation type="Property">public string Key { get; set; }</Operation>
      <Modifier>None</Modifier>
    </Entity>
  </Entities>
  <Relations>
    <Relation type="Association" first="9" second="2">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="4" second="2">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="3" second="2">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="2" second="5">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="2" second="6">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="2" second="7">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="0" second="2">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="1" second="2">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="7" second="10">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="7" second="11">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="9" second="13">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="9" second="14">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="1" second="16">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="16" second="15">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="17" second="18">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="17" second="19">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="15" second="17">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="15" second="20">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="0" second="22">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="16" second="22">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="22" second="23">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
    <Relation type="Association" first="22" second="21">
      <Direction>None</Direction>
      <IsAggregation>False</IsAggregation>
      <IsComposition>False</IsComposition>
    </Relation>
  </Relations>
  <Positions>
    <Shape>
      <Location left="315" top="61" />
      <Size width="162" height="121" />
    </Shape>
    <Shape>
      <Location left="115" top="61" />
      <Size width="162" height="92" />
    </Shape>
    <Shape>
      <Location left="186" top="218" />
      <Size width="162" height="91" />
    </Shape>
    <Shape>
      <Location left="400" top="346" />
      <Size width="162" height="148" />
    </Shape>
    <Shape>
      <Location left="225" top="346" />
      <Size width="162" height="77" />
    </Shape>
    <Shape>
      <Location left="572" top="346" />
      <Size width="162" height="134" />
    </Shape>
    <Shape>
      <Location left="742" top="346" />
      <Size width="162" height="91" />
    </Shape>
    <Shape>
      <Location left="1003" top="346" />
      <Size width="162" height="120" />
    </Shape>
    <Shape>
      <Location left="572" top="508" />
      <Size width="162" height="77" />
    </Shape>
    <Shape>
      <Location left="50" top="346" />
      <Size width="162" height="162" />
    </Shape>
    <Shape>
      <Location left="916" top="479" />
      <Size width="162" height="105" />
    </Shape>
    <Shape>
      <Location left="1089" top="479" />
      <Size width="162" height="105" />
    </Shape>
    <Shape>
      <Location left="1961" top="346" />
      <Size width="162" height="91" />
    </Shape>
    <Shape>
      <Location left="50" top="608" />
      <Size width="162" height="105" />
    </Shape>
    <Shape>
      <Location left="225" top="608" />
      <Size width="162" height="77" />
    </Shape>
    <Shape>
      <Location left="1517" top="346" />
      <Size width="162" height="105" />
    </Shape>
    <Shape>
      <Location left="1261" top="479" />
      <Size width="162" height="140" />
    </Shape>
    <Shape>
      <Location left="1517" top="479" />
      <Size width="162" height="63" />
    </Shape>
    <Shape>
      <Location left="1435" top="556" />
      <Size width="162" height="134" />
    </Shape>
    <Shape>
      <Location left="1611" top="556" />
      <Size width="162" height="63" />
    </Shape>
    <Shape>
      <Location left="1791" top="479" />
      <Size width="162" height="91" />
    </Shape>
    <Shape>
      <Location left="1089" top="815" />
      <Size width="162" height="120" />
    </Shape>
    <Shape>
      <Location left="1251" top="668" />
      <Size width="162" height="91" />
    </Shape>
    <Shape>
      <Location left="1261" top="815" />
      <Size width="162" height="105" />
    </Shape>
    <Connection>
      <StartNode isHorizontal="False" location="48" />
      <EndNode isHorizontal="True" location="64" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="False" location="147" />
      <EndNode isHorizontal="True" location="84" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="False" location="18" />
      <EndNode isHorizontal="True" location="71" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="True" location="52" />
      <EndNode isHorizontal="False" location="21" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="True" location="40" />
      <EndNode isHorizontal="False" location="17" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="True" location="21" />
      <EndNode isHorizontal="False" location="29" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="True" location="107" />
      <EndNode isHorizontal="False" location="105" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="False" location="92" />
      <EndNode isHorizontal="False" location="21" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="True" location="84" />
      <EndNode isHorizontal="False" location="57" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="True" location="88" />
      <EndNode isHorizontal="False" location="126" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="False" location="37" />
      <EndNode isHorizontal="False" location="35" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="False" location="124" />
      <EndNode isHorizontal="False" location="30" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="False" location="29" />
      <EndNode isHorizontal="False" location="45" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="False" location="95" />
      <EndNode isHorizontal="True" location="48" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="True" location="35" />
      <EndNode isHorizontal="False" location="51" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="True" location="45" />
      <EndNode isHorizontal="False" location="113" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="True" location="84" />
      <EndNode isHorizontal="True" location="14" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="True" location="38" />
      <EndNode isHorizontal="False" location="52" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="True" location="95" />
      <EndNode isHorizontal="False" location="2" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="False" location="61" />
      <EndNode isHorizontal="False" location="68" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="False" location="55" />
      <EndNode isHorizontal="False" location="38" />
    </Connection>
    <Connection>
      <StartNode isHorizontal="True" location="77" />
      <EndNode isHorizontal="False" location="102" />
    </Connection>
  </Positions>
</ClassProject>