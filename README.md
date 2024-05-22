# BetterLegacy

Make Project Arrhythmia (Legacy) better than ever before with this singular mod.
If you want to know how to install, go [here](https://github.com/RTMecha/BetterLegacy/master/README.md#installation)

## Info
The mod is made up of 8 sections, all originating from their own mods.
- **Core**
   - Original mod: [RTFunctions](https://github.com/RTMecha/RTFunctions)
   - Summary: Improves the base game by adding a ton of optimization, features and bug fixes.
- **Editor**
   - Original mod: [EditorManagement](https://github.com/RTMecha/EditorManagement)
   - Summary: Increases editor performance, workflow and fixes a ton of editor bugs.
- **Events**
   - Original mod: [EventsCore](https://github.com/RTMecha/EventsCore)
   - Summary: Makes event keyframes auto update and has their easings line up with the object easings. Plus, adds a ton of new events (40 total events).
- **Players**
   - Original mod: [CreativePlayers](https://github.com/RTMecha/CreativePlayers)
   - Summary: Allows for custom player models and for up to 8 players in multiplayer.
- **Modifiers**
   - Original mod: [ObjectModifiers](https://github.com/RTMecha/ObjectModifiers)
   - Summary: Adds a new modifier system to both regular objects and background objects. Modifiers are made up of two types, triggers and actions. Triggers check if something is happening and if it is (or there are no triggers) it'll allow the actions to do their thing.'
- **Arcade**
   - Original mod: [ArcadiaCustoms](https://github.com/RTMecha/ArcadiaCustoms)
   - Summary: Improves the Legacy arcade UI by adding mouse navigation, level searching, locally installed levels, an upcoming online server and even Steam workshop browsing.
- **Menus**
   - Original mod: [PageCreator](https://github.com/RTMecha/PageCreator)
   - Summary: Can use custom menu music and custom lilscript functions.
- **Companion**
   - Original mod: [ExampleCompanion](https://github.com/RTMecha/ExampleCompanion)
   - Summary: Adds a little companion to accompany you on your Project Arrhythmia journey.

## Installation
You can install the mods via the [Project Launcher](https://github.com/RTMecha/ProjectLauncher/releases/latest). You can use that tool to manage multiple instances of Project Arrhythmia and easily keep up to date with different mod updates. However, if you want to install it manually, follow the guide below. (Install guide based on [Catalyst](https://github.com/Reimnop/Catalyst) guide)
1. Verify you are on the Legacy branch.
	- ℹ️ _As BetterLegacy is obviously made for the **Legacy** branch, it will not work on any other._
	- ℹ️ _Go to your Steam library and right click Project Arrhythmia then click on Properties. Navigate to the Betas tab and change the Beta Participation dropdown to the Legacy branch._
1. Open the Project Arrhythmia application folder.
	- ℹ️ _On your Steam library, right click on Project Arrhythmia and go to **Manage** > **Browse local files**_
1. Download BepInEx for the Legacy branch.
	- ℹ️ _Since you're only modding the Legacy branch, you only need [BepInEx 5 x64](https://github.com/BepInEx/BepInEx/releases/download/v5.4.21/BepInEx_x64_5.4.21.0.zip)._
1. Extract the contents of the BepInEx ZIP file to the **Project Arrhythmia folder** you opened earlier.
1. Launch Project Arrhythmia once and then close it.
1. In the Project Arrhythmia folder, go to `BepInEx` > `plugins`.
	- ℹ️ _If you do not see the folder, then you haven't installed BepInEx properly. Try following the previous steps again, or ask for help in the [Discord](https://discord.gg/5XfVScJSK5)._
1. Download the current release of BetterLegacy and place it into the `plugins` folder.
1. In the Project Arrhythmia folder, go to Project Arrhythmia_Data > Plugins.
1. Make a backup of the steam_api64.dll file somewhere outside of the Project Arrhythmia folder, just in case.
1. Download [Steamworks.NET](https://github.com/rlabrecque/Steamworks.NET/releases/download/14.0.0/Steamworks.NET-Standalone_14.0.0.zip), extract it somewhere.
1. Open the Windows-x64 folder and drag & drop the steam_api64.dll file to the recently opened Plugins folder to replace the original file.
	- ℹ️ _This is to allow Legacy to better interact with Steam as normally it will not acknowledge Steam with the older file._
1. Finally, download the Beatmaps.zip file, extract it to the Project Arrhythmia folder.
1. You just installed BetterLegacy, awesome! Hope you have fun with the mods, whatever you end up doing. If something breaks or you have a general suggestion for the mod, feel free to open an [issue](https://github.com/RTMecha/BetterLegacy/issues) or make a [pull request](https://github.com/RTMecha/BetterLegacy/pulls) if you want to help with modding.
