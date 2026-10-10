using System.Reflection;
using System.Runtime.InteropServices;
using Xunit;

// Tests/Protocol's full-boot tier spawns real AuxiliaryServer/GameCore processes bound to
// fixed ports (GameServerPairFixture.LoginPort/GamePort) - any two xunit *collections* that each
// own one of these fixtures would otherwise be free to run concurrently (xunit only serializes
// *within* a collection by default) and race on those ports. Serializing the whole assembly is
// the simplest fix given there are only a couple of these collections.
[assembly: CollectionBehavior(DisableTestParallelization = true)]

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("SteamEngine.Tests")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("SteamEngine.Tests")]
[assembly: AssemblyCopyright("Copyright ©  2017")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("84a9e98b-1d63-4616-9beb-85fd7fbb0b5e")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// You can specify all the values or you can default the Build and Revision Numbers 
// by using the '*' as shown below:
// [assembly: AssemblyVersion("1.0.*")]
[assembly: AssemblyVersion("1.0.0.0")]
[assembly: AssemblyFileVersion("1.0.0.0")]
