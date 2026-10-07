Starting SteamEngine

The server part of SE consists of 2 executables/processes - GameCore and AuxiliaryServer. GameCore is the one where all the UO stuff is happening, while Auxiliary is for secondary tasks - it server connections from remote consoles (the third major SE executable) and it serves as login server for the gamecore(s) running on the same server computer. It can also update sources from SVN and restart both itself and the nearby gameserver(s) when commanded so from remote console(s).


Running SE for the first time, start by launching gamecore (Release_ or Debug_) - it will create steamengine.ini config file. Look at the file, fix the settings if needed (theyre commented inside) and then run both GameCore and AuxiliaryServer (most likely Release_). Then you can connect with either UO client or remoteconsole - in both cases, using any username/password will result in creating the first account - with admin priviledges.

RemoteConsole - the GUI program - supports adding buttons as shortcuts for simple commands. For the basic set of recommendes buttons, look in steamrc.example.ini, where you can directly copy the buttons from.

We used to use NANT for building, but we converted to msbuild.

-tar







now this is a little outdated, so don't believe everything:



About the different builds

There are three builds, Debug, Sane, and Optimized. 

The Sane build is the one that you probably will be using most often (unless you are a SteamEngine developer, in which case you will probably use the Debug build most).

The Sane build prints understandable error messages, and does sanity-checking to detect if something tries to call a method with invalid parameters, or tries to set a variable/property to something illegal, or if an assumption made by some code is false, etc.

This means that if you make a script which does something which shouldn't be done, if you are using the Sane (or Debug) build, then you will probably be told so right away, and if not, then you will be told when a sanity check somewhere else detects a problem, which should be fairly soon in most cases.

The Debug build acts like the Sane build, but it will tend to print more information than the Sane build when something goes wrong. If you do see something go wrong, you're encouraged to see if it is repeatable, firstly, and secondly, see if the Debug build will print more information. If you're reporting it as a bug, it would be best if you included the information the Debug build prints, instead of what the Sane build prints, because the greater amount of information given by the Debug build will make it easier to find the source of the problem. If the bug is in a compiled script (not LScript), then the Debug build may help you find the problem faster. With LScript scripts, the Debug build may be more useful in some situations, but not necessarily in others (If the problem is in an LScript script, you are encouraged to try the Debug build if you are having trouble figuring out the problem, but otherwise it may not be necessary).

If you've been running SteamEngine for a while without any problems, and you want better performance, you could try the Optimized build. It is faster than the Sane build, but you shouldn't use the Optimized build if you are writing or modifying scripts (or if you are modifying the core!), because the Optimized build will NOT detect many problems that the Sane or Debug builds detect. However, these kinds of problems are never detected and silently ignored in the Debug or Sane builds, so if you haven't been seeing any "error" messages (or "fatal" or "critical" messages), and you've been running the Sane (or Debug) build for long enough to be satisfied that everything is working properly, then it is probably safe to run the Optimized build.

-SL
