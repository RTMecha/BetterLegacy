# 1.7.0-pre.2 > [???]

## Features
- You can now drag and drop files into the game, both in the editor and the arcade.
  - Dragging a level into the arcade / editor will load it.
  - Dragging a txt file into a loaded editor level will create a text object.
  - Dragging an image into a loaded editor level will create an image object.
  - Dragging a prefab into a loaded editor level will import it. If the mouse is over the timeline, it will place it.
  - Dragging an audio file while the New Level Creator popup is open will set the audio path for the new level.
  - Dragging an audio file into a loaded editor level will create an object with a playSound modifier.
  - Dragging a MP4 file into a loaded editor level will set it as the video BG.
- Internally, a custom "base path" can be set in the editor. This means you can have Project Arrhythmia open on one harddrive, while another harddrive has the beatmaps folder. There is no UI to set this yet.

## Changes
- Some JSON values have been changed. This means BetterLegacy is no longer compatible with vanilla Legacy. (why would you use that outdated version anyways)
- Timeline grid now fades depending on timeline zoom. This was in last prerelease, just forgot to include it in the changelog.
- Example now has eyelids for more expressions.
- Player shape type has been removed for the time being. Wasn't happy with how it worked. Might revisit it at some point when I feel I can do it.
- Shapes are now loaded from a shapes.json file in the Assets folder.

## Fixes
- Fixed animateObject modifiers and shot bullets being inconsistent with different framerates.
- Fixed default keybinds for First and Last Keyframe selectors being incorrect.
- Fixed non-shape objects not updating their render type when the dropdown value is changed.
- Fixed Multi Object Keyframe Editor "Apply Curves" button re-rendering keyframes from previously selected objects.
- Fixed setParent crashing the game because I got the parent and the child confused.

------------------------------------------------------------------------------------------

# 1.7.0-pre.1 > [Mar 31, 2025]
New changelog format and the first pre-release for 1.7.0!

## Features
### Story
- Most story levels now have a pre-cutscene, post-cutscene, a staff log and PA chat menus, however most of the new stuff remains very unfinished. Getting really close to the finish line, hopefully.

### Configs
- Custom configs can kinda be registered now.
- Added "Timestamp Updates Per Level" setting to Config Manager > Core > Discord. With this on, the Discord status timestamp updates every time you load a level.

### Game
- Due to the beatmap data rework, new "setParent" and "setParentOther" modifiers was added. This temporarily sets the parent of the beatmap object to another, until you update the object or reload the level.
- Added new UI render type. This makes objects render above "Foreground" objects and be unaffected by effects such as bloom.
- "Align Near Clip Plane" value has been added to the Camera Depth event keyframe. This prevents objects in the Background render type from clipping into the camera, but also stops the negative zoom bleeding from working.
- Added currentEpoch to math parser variables.
- Now supports text origin alignment from alpha.
- Ported gradient scale & rotation from alpha.
- Ported polygon shape from alpha. (still waiting on the mesh generator, so it doesn't fully work yet)

### Editor
- Themes now display info about their colors.
- Folders in the level, theme and prefab lists now have a "folder" icon. This can be customized by adding a "folder_icon.png" file to the folder or by using the context menu to set it. You can also give the folder a short description by creating a "folder_info.json" file.
- Added New Prefab Instance button to Multi Object Editor.
- Added a "Quick Prefab Target" (Alpha's PrefabOnObject port).
- Prefab objects now display a line showing where their offset is at.
- Players in the editor now display info on their current state.
- Improved BPM Snap settings and added a BPM sub-tab to Config Manager > Editor.
- Changed Upload Acknowledgements text to use text linking instead.
- New Context Menu stuffs:
  - Search field filters (default themes, used themes, used prefabs)
  - Default prefab / theme paths for level
  - Apply & Create New for the Apply Prefab button in the Object Editor.
  - Edit for internal prefabs.
  - Prefab offset context menus.
  - Object keyframe value context menus.


## Changes
### Core
- Reworked beatmap data to not depend on base vanilla data as much. (so many nests and odd names wtf)
- Shortened file sizes by trimming quotation marks when necessary.

### Example Companion
- Completely overhauled Example. He is now a whole lot more advanced, customizable and a (hopefully) better assistant. He's grown up so much :3
- Example now has a "module" system. This way each main section of Example can interact with each other and should work fine if a module was changed / customized.
- Example remembers things now.

### Interfaces
- Moved the Profile menu and Tests menu to a new "Extras" menu on the main menu.
- Reworked Input Select and Load Level menus to use the current interface system.
- Entering the Arcade / Story with players already registered will now skip the Input Select menu.
- Entering the Arcade no longer automatically loads the levels, meaning you can go straight from the main menu to the Arcade menu.
- Changelog interface now only applies from a "changelog" file as to prevent any issues with online changelog files changing.

### Game
- Trying to rework how themes are loaded.
- spawnPrefab modifiers can now find a prefab via different ways: Index, ID and Name.
- Reverted the fix that changed modifier groups to only check objects spawned from a prefab. I realized it's more useful to have it also search for expanded prefab objects.
- Reverted BG camera near clip plane not changing with the zoom. This means the negative zoom frame bleeding effect is back.
- Reworked the way modifier functions are assigned to fix game freeze on first modifier running.
- doubleSided modifier now supports gradients and opacity above 0.9.

### Editor
- Renamed Timeline Waveform type names to better describe what they are. This is done since what was the "Modern" waveform is no longer modern due to modern PA now using the "Beta" waveform.
- Unclamped the timeline. You can now drag the timeline to before the level starts and see any objects that spawn there. You can also move objects to before the level starts by turning the config "Config Manager > Editor > Timeline > Clamped Timeline Drag" off.
- Removed the modifiers tab from the Config Manager and moved its settings to Editor > Modifiers.


## Fixes
- Fixed metadata editor not closing.
- Fixed some issues with the BG camera layer getting offset from the foreground.
- Fixed objects highlighting and not unhighlighting when they disable and re-enable.
- Fixed Discord status timestamp being used incorrectly.
- Fixed song pitch not resetting when the user exits to main menu.
- Fixed some Prefab Object Editor stuff being broken.

------------------------------------------------------------------------------------------

# snapshot-2025.2.3 - (pre-1.7.0) > [Feb 13, 2025]

## Features
- Added support for Shake X & Y from alpha / default branch.
- Preparing for polygon shape and particle shape port.
- Added Require Version to metadata values. This is for better level compatibility.

## Changes
- Alpha (Arrhythmia) player model no longer can boost to be more accurate to the era the model is from.

------------------------------------------------------------------------------------------

# snapshot-2025.2.2 - (pre-1.7.0) > [Feb 7, 2025]

### Fixes
- Actually fixed Object Editor not opening. This was due to the Object Editor initialization code ending before the UI was assigned.

------------------------------------------------------------------------------------------

# snapshot-2025.2.1 - (pre-1.7.0) > [Feb 3, 2025]

## Changes
- Added animations to the Object Templates Popup.

## Fixes
- Fixed Object Editor not opening.

------------------------------------------------------------------------------------------

# snapshot-2025.1.10 - (pre-1.7.0) > [Jan 26, 2025]

## Features
- Added more_bins achievement.
- You can now drag the timeline around when you drag on timeline objects.
- Added max bin count and default bin count buttons in the timeline context menu.
- Added Toggle Object Preview Visibility to the editor layer field context menu. This is ease of access for the "Only Objects on Current Layer Visible" setting.
- Added a Text Object context menu. Comes with a new Font Selector Popup list.

## Changes
- Removed unused Editor Properties settings since that was removed a long while ago.

## Fixes
- Fixed color modifiers not working with text objects.
- Fixed player tail length being offset.

------------------------------------------------------------------------------------------

# snapshot-2025.1.9 - (pre-1.7.0) > [Jan 21, 2025]

### Fixes
- Fixed editor tick throwing an error in some cases.

------------------------------------------------------------------------------------------

# snapshot-2025.1.8 - (pre-1.7.0) > [Jan 18, 2025]

## Fixes
- Actually hopefully fixed Object Editor not opening in some cases?????
- Fixed Custom Player Objects not having their visibility properly set.

------------------------------------------------------------------------------------------

# snapshot-2025.1.7 - (pre-1.7.0) > [Jan 18, 2025]

## Features
- Interfaces now notify you of what song is currently playing.
- Implemented more story elements.

## Fixes
- Fixed Player Shapes removing their tail.
- Fixed Object Editor not opening.

------------------------------------------------------------------------------------------

# snapshot-2025.1.6 - (pre-1.7.0) > [Jan 16, 2025]

## Features
- Story mode chapter 1 now has a "chapter transition" level, meaning you can technically complete chapter 1 now. However, the story is not finished and needs more work.
- You can now click and hold the mouse scrollwheel to drag the editor timeline around.
- Added a context menu to objects' editor layer field.

## Changes
- Updated some story levels.
- Reworked some editor timeline stuff.
- Reworked Editor Dialog and Editor Popup open / close systems. If you encounter any issues with these changes, please let me know!
- Changed the way multiple selected object keyframes of the same type are handled in the editor. You can now properly set, add and subtract a value to multiple keyframes.
- Moved Marker settings in the config to their own sub-tab.

## Fixes
- Fixed Level Template default preview not displaying correctly.
- Markers in the object timeline now properly update when the start time / autokill time is changed.

------------------------------------------------------------------------------------------

# snapshot-2025.1.5 - (pre-1.7.0) > [Jan 9, 2025]

## Fixes
- Fixed a really old bug with colliders scaled to 0, 0 crashing the game. (Looking at you, pseudo-3D animations)

------------------------------------------------------------------------------------------

# snapshot-2025.1.4 - (pre-1.7.0) > [Jan 9, 2025]

## Fixes
- Fixed internal prefab search using the external search.
- Fixed DevPlus players' tail being further away from the head than it should be.
- Fixed the story mode levels not working.
- Fixed Multi Object Editor color keyframe index scrolling setting itself to a really low negative number.
- Fixed Multi Object Keyframe Editor ease type dropdown not having any values.
- Fixed the BG Modifier card layout.
- Fixed expanding prefab objects lower than the default bin collapsing all the objects bin values.

------------------------------------------------------------------------------------------

# snapshot-2025.1.3 - (pre-1.7.0) > [Jan 8, 2025]

## Features
- Forgot to note last snapshots that the TO DO items in the Project Planner can now have their priority changed.
- Modifier notifier now lights up instead of changing its active state.
- Added some more tooltips to the modifiers.

## Changes
- Reworked setVariable modifiers to only set the variable of itself. Any levels / prefabs that use the same modifier but before this change should be unaffected. If they have, then you'll need to change the modifiers to the new setVariableOther modifiers.
- Reworked the checkpoint system a little.

## Fixes
- Fixed edit button showing on the input field for the Project Planner TO DO Editor.
- Fixed a bug with restarting the level not resetting the currently active checkpoint.
- Got Checkpoint animation now properly updates its colors to the themes' GUI color.
- Fixed Bin Controls affecting the Events layer.

------------------------------------------------------------------------------------------

# snapshot-2025.1.2 - (pre-1.7.0) > [Jan 7, 2025]

## Features
- Implemented Bin Controls. This means you can now scroll up and down in the main editor timeline and you can add new bins to the editor.
- With the Bin Controls feature, Timeline Objects can now retain their bin value depending on the Config Manager > Editor > Timeline > Timeline Object Retains Bin On Drag setting.
- You can now delete level templates.
- You can now edit the transform offsets of multiple prefabs in the Multi Object editor.
- Simplified the level combining process by adding a PA type dropdown and shortening the default path so it looks less daunting.
- Added "Mouse Tooltip Requires Help" setting to Config Manager > Editor > Editor GUI. With it on, the mouse tooltip requires the help info box to be active in order to show. It's off by default.
- Added a bunch of mouse tooltips.
- Easing Dropdown in the Multi Object Keyframe Editor now has an "Apply Curves" button. This is so you can set the easings of multiple keyframes without having to change the dropdown value.
- Added "pop" sound to the default sounds.
- Overwrite the Players' animation system. Includes a new heal animation.
- It now should be possible to parent custom player objects to other custom player objects.
- Custom player animations have been implemented internally.
- Added Replay End Level Off setting to the Metadata Editor.
- Added a reload button to the Keybinds popup. This resets your keybinds to the default list.

## Changes
- Fonts in the Text Object documentation now display their actual font.
- Changed the Modifier Collapse toggle UI to look like other collapse toggles, so it's more clear as to what it does.
- Changed the layout of the Setting Editor so the color editors are side-by-side.
- Shortened some of the color keyframe labels for gradient objects.
- Cleaned up Multi Object Editor UI a little.
- Cleaned up a ton of Player code.
- Changed the default image object sprite to the PA logo.

## Fixes
- Fixed the bug where selecting a single object after selecting multiple prevents rendering the keyframes correctly.
- Fixed keyboard number row + not working with the editor freecam.
- Fixed Warning Popup appearing behind some popups.

------------------------------------------------------------------------------------------

# snapshot-2025.1.1 - (pre-1.7.0) > [Jan 3, 2025]
First snapshot of the new year! I really hope to get this update done soon...

## Features
- Added rank display to level buttons in the Arcade menus.
- Modifiers are now collapse-able in the editor.

## Changes
- Changed Difficulty mode in the Pause menu to Challenge mode.
- Reworked some Project Planner code.
- Made levels in the editor consistent with other editor data and also use the same level system as the arcade. This technically means you can open VG levels now.

## Fixes
- Fixed SS rank shine not working in some cases.
- Player data now updates its level name.
- Fixed the interface Dark theme not being great.
- Hopefully fixed editor crash by clearing levels list from the arcade when loading the editor.
- Fixed objects appearing behind others when created.

------------------------------------------------------------------------------------------

# snapshot-2024.12.7 - (pre-1.7.0) > [Dec 30, 2024]

## Features
- Added Open Source link to the Help dropdown in the editor.
- Added an Open Workshop button to the online Steam level menu.
- loadLevelInCollection and downloadLevel modifiers.
- LoadLevelInCollection to end level function dropdowns.
- setMusicPlaying modifier. This sets the playing state of the current song. This feature is more for sandboxing and less to be used in actual levels due to pause menu taking priority in playing state.
- pauseLevel modifier. Pauses the game and opens the pause menu.
- New "Cursor" sub-tab in Config Manager > Core. This has settings related to the cursors' visibility.

## Changes
- Scene loading screen now uses the current interface theme instead of the old interface colors.
- Arcade level player data now saves in a non-encrypted arcade_saves.lss file. Since BetterLegacy doesn't have leaderboards (and likely never will), the encryption wasn't necessary.
- Cursor now behaves consistently across the entire game in terms of visibility.

------------------------------------------------------------------------------------------

# snapshot-2024.12.6 - (pre-1.7.0) > [Dec 28, 2024]

## Features
- Added "Paste Background Objects Overwrites" setting to Config Manager > Editor > Data. This removes the current list of BG objects and replaces it with the copied list.

## Changes
- Made Prefab Panel code more consistent with other editor elements.

## Fixes
- Fixed Arcade menu local pages not working.
- Fixed Background Objects not being destroyed / properly updated in some cases.

------------------------------------------------------------------------------------------

# snapshot-2024.12.5 - (pre-1.7.0) > [Dec 27, 2024]

## Features
- Added "Play Pause Countdown" setting to Config Manager > Core > Game.
- endLevel modifier now has a customizable end level function. This includes a new "setLevelEndFunc" modifier that only changes the level end behavior without actually ending the level.

## Changes
- Restarting level from Pause menu now restarts after the countdown.
- Increased the modifier dropdown width.

## Fixes
- Fixed the replay button being broken in the End Level screen due to it clearing the level data.

------------------------------------------------------------------------------------------

# snapshot-2024.12.4 - (pre-1.7.0) > [Dec 26, 2024]

## Features

### Story
- Updated the chapter 1 demo. This includes some very WIP cutscenes.
- Story save slots now show your current progress.
- Added an intro sequence to the story mode.

### Game
- Began implementing seed-based random.
- Added the progress menu to the Arcade server downloading.
- Added "Show Markers" setting to Config Manager > Editor > Timeline.
- Added a "Spawn Players" toggle to the Player Editor. With this off, players will not spawn at the start of the level, allowing for cutscenes. If you want to respawn players, create a checkpoint or use the playerRespawn modifiers.
- Added "setAudioTransition" action modifier. This sets the audio transition for the next loaded level.
- Added "setIntroFade" action modifier. This is if you don't want the intro fade to play for the next loaded level.
- Internally a custom end level function can be set, but this probably won't be implemented until 1.8.0.

## Changes
- Background Objects now update in the UpdateObject keybind action.
- Changed the emoji in the main menu sound.
- Implemented default rect values for interfaces.
- Reworked a lot of timeline object and timeline marker related code.
- Rewrote some theme panel code.
- Ending event keyframe dragging now updates the event editor.
- Some marker editor values now update when dragging a marker.
- Example now considers the loading level screen a menu.
- Unpause sequence can now be sped up if the user is speeding up the interface by pressing left mouse button / Xbox controller A button / PS controller X button / keyboard space.

------------------------------------------------------------------------------------------

# snapshot-2024.12.3 - (pre-1.7.0) > [Dec 17, 2024]

## Features
- Added a notification that informs you about editor freecam and show GUI & Players being toggled.
- Added "Copy Path" to the File Browser context menus.
- Added "Shuffle ID" to theme context menus.
- Added Steam Workshop search sorting (with some extra toggles in Config Manager > Arcade > Sorting).

## Changes
- Cleaned up a lot file path related code. If anything goes wrong related to that, please let me know!
- Removed the "Show Levels Without Cover Notification" setting. This setting wasn't really useful.
- Made gradient objects work with editor highlighting and layer opacity.

## Fixes
- Fixed textSequence Play Sound toggle not actually doing anything.
- Example now only moves to the warning popup if he is far enough away from his spot. (Sometimes...)
- Mostly fixed Steam Workshop level subscribing progress not working.

------------------------------------------------------------------------------------------

# snapshot-2024.12.2 - (pre-1.7.0) > [Dec 10, 2024]

## Features
- Added "File Browser Remembers Location" to Config Manager > Editor > Data.

## Changes
- Reworked how some file format related things work.

## Fixes
- Fixed copyAxisGroup min and max values not allowing for decimal points.
- Actually fixed VG to LS conversion.

------------------------------------------------------------------------------------------

# snapshot-2024.12.1 - (pre-1.7.0) > [Dec 8, 2024]

## Features
- Added one more secret. Hint: Japanese music.
- Added "shoot" to default sounds. This plays when the player shoots.
- Added a "Reload Level" button to the File dropdown.
- Did some internal work on Level Collections. It'll hopefully be fully implemented in the 1.8.0 update.
- Started working on multi language support for stuff outside of tooltips.
- Added some hit / death counter modifiers.

## Changes
- Reworked how BetterLegacy's custom animations' time is updated. This allows for a proper speed functionality with interface text.
- Decreased Example's dance chance from 0.7% to 0.2% per frame.
- Some more editor elements are considered for the Editor Complexity setting.
- Default Arcade Menu selection is now the "Local" tab.
- Decreased theme default ID max length from 9 to 7 due to number inaccuracies.
- Renamed Example config tab to Companion.
- Added a label for the Config Manager UI page field.
- Added a custom menu flip sound for changing Arcade menu pages.

## Fixes
- Due to the custom animation time change, shake speed works as intended now.
- Fixed Discord status just being "In Main Menu" when you restart Discord, now it properly sets back to what you currently have.
- Fixed the Play Level Menu being slow to load.
- Hopefully fixed VG convert case where the path was the same as the destination.

------------------------------------------------------------------------------------------

# snapshot-2024.11.21 - (pre-1.7.0) > [Nov 27, 2024]

## Features
- Added some secrets ;)
- Added Arcade Folder and Interfaces Folder to custom menu music options.
- Added some new default sounds.
- Player healing now plays a sound.
- Added setting for toggling the checkpoint sound / animation in Config Manager > Core > Level > Play Checkpoint Sound and Play Checkpoint Animation.

## Changes
- Changed SpawnPlayer default sound to be its own sound.
- Reordered Default Sounds list.

## Fixes
- Fixed typo on Allow Controller If Single Player.
- Fixed level intro fade not working in some cases.
- Fixed checkpoints not properly playing the animation & sound when you reach one.
- Fixed players respawning instantly when restarting a level from the pause menu, sometimes causing the player to get hit.

------------------------------------------------------------------------------------------

# snapshot-2024.11.20 - (pre-1.7.0) > [Nov 24, 2024]

## Features
- Added Allow Controler If Single Player under Config Manager > Players > Controls.
- Added playerRespawn modifiers.
- Added FPS and BG Object Count to the Editor Information section.
- Added Rank to Arcade level sorting.
- Arcade menu now has level sort buttons.
- Added some more fun splash text.
- Added mirrorPositive and mirrorNegative math functions.

## Changes
- Optimized and increased some player functionality.
- Increased the Beatmap Theme ID default length.
- Updated Editor Documentation to include new math functions.

## Fixes
- Fixed checkpoints not being removed from the game timeline.
- Fixed Example commands not working in some cases.
- Fixed Arcade level sorting duplicating levels and not working with local levels.

------------------------------------------------------------------------------------------

# snapshot-2024.11.19 - (pre-1.7.0) > [Nov 22, 2024]

## Features
- Added some math functions.
- Added default Order Matters toggle config.

## Fixes
- Fixed Prefab Group modifiers checking for both spawned and expanded objects from prefabs.
- Fixed variableOther trigger modifiers not using the prefab instance group toggle.

------------------------------------------------------------------------------------------

# snapshot-2024.11.18 - (pre-1.7.0) > [Nov 22, 2024]

## Features
- Added Story File config to Config Manager > Core > File. This is just for testing the story mode.
- Added Limit Player settings to the Global tab of the Player Editor. Limit Player is on by default, so if you made a level that has a custom player with specific speeds, please change this once the update releases.
- Added "Show Intro" to the Metadata editor. Turning this off will stop the intro from showing when you play the level in the arcade.
- Object Modifiers editor now shows a modifier being active by enabling / disabling a little bar based on the modifiers' active state.
- Made the Modifier editors consistent. Background Modifiers now have copy / paste functionality and context menus.
- Modifier context menus now have "Add Above", "Add Below", "Paste Above" and "Paste Below" functions.
- Added a ton of values to textSequence modifier so it can be used with the new "Order Matters" toggle.
- Music time triggers now have a Start Time relative setting.

## Changes
- Updated Editor Documentation.

## Fixes
- Fixed textSequence modifier.

------------------------------------------------------------------------------------------

# snapshot-2024.11.17 - (pre-1.7.0) > [Nov 19, 2024]

## Features
- Added staff logs.
- You can now hold down the left mouse button to speed up the interface.
- Fully implemented modifier trigger / action ordering and else if toggles for triggers.

## Fixes
- Fixed Main Menu having the wrong name.
- Fixed checkpoint dragging not working.

------------------------------------------------------------------------------------------

# snapshot-2024.11.16 - (pre-1.7.0) > [Nov 17, 2024]

## Features
- Added example_speak to default sounds.
- Implemented collectibles you can find in random spots across the levels.
- Some levels now end with a PA Chat.
- spawnPrefab modifiers now have values for setting the spawned prefabs' start time.
- Added Inspect Timeline Object if Unity Explorer is installed.
- Added a "doubleSided" modifier. This acts like the blur modifiers, except it changes the material of the object to be double sided, allowing it to be seen from the back.

## Changes
- You now immediately boot into the story tutorial level when you're playing a new save slot.
- Did some internal work on strings and scenes. If there's any issues with either of those two that you find, please let me know!

------------------------------------------------------------------------------------------

# snapshot-2024.11.15 - (pre-1.7.0) > [Nov 15, 2024]

## Features
- Added To Lower and To Upper to default text Input Field context menus.
- Added Snap Offset to Multi Object Editor. This snaps selected objects relative to the earliest object in the timeline.
- Interfaces can now change music dynamically. Additionally, the story mode now has custom music.
- Input Select screen now has a custom song. If you don't want it to play, then you can turn it off by going to Config Manager > Menus > Music > Play Input Select Music.

## Changes
- Story saves delete buttons now properly align to existing save slots.

## Fixes
- Fixed New Simulation button showing in cases it shouldn't be.
- Fixed BPM Offset causing objects start time to go further than it should.

------------------------------------------------------------------------------------------

# snapshot-2024.11.14 - (pre-1.7.0) > [Nov 14, 2024]

## Fixes
- Hotfix for New Simulation button not showing.

------------------------------------------------------------------------------------------

# snapshot-2024.11.13 - (pre-1.7.0) > [Nov 14, 2024]

## Features
- Made rank sayings data driven so they can be customized per user. If you copy the sayings.json file in the plugins/Assets folder to the profile folder, you can keep a custom version of the rank sayings!
- Added more save slots (up to 9 now).

## Changes
- Interface themes are a little better to use.

## Fixes
- Fixed some more issues with Video BGs, such as it not applying the Audio keyframe pitch.

------------------------------------------------------------------------------------------

# snapshot-2024.11.12 - (pre-1.7.0) > [Nov 12, 2024]

## Features
- Added a new setting "Physics Update Match Framerate" to Config Manager > Core > Game. This setting forces physics to match your framerate, this includes player movement and collision.
- Added levelExists and levelPathExists trigger modifiers.

## Fixes
- Fixed Video BGs freezing issues.
- Fixed some issues with audio not properly loading in some cases.

------------------------------------------------------------------------------------------

# snapshot-2024.11.11 - (pre-1.7.0) > [Nov 10, 2024]

## Features
- Added spawnPrefabOffsetOther modifiers.
- Added findObject#object_group#StartTime() math function. StartTime can be replaced with other object properties such as "Depth" and "IntVariable".
- Added findInterpolateChain#object_group math function. This will find an object and interpolate it.
- Added applyAnimationMath modifiers.
- Started working on seed-based randomization, this probably won't be in this update but will be in a future one.

## Changes
- Updated Editor Documentation.

## Fixes
- Fixed picker not working in some cases.
- Fixed players being able to join outside of the input select screen.

------------------------------------------------------------------------------------------

# snapshot-2024.11.10 - (pre-1.7.0) > [Nov 8, 2024]

## Features
- Added findOffset math function. Used similarly to findAxis.
- Added vectorAngle and distance functions.

## Changes
- Updated Slime Boy Color and a few other story levels to their current versions.

## Fixes
- Fixed cases where object interpolation would go higher than expected.
- Fixed lerp and inverseLerp math functions being incorrect.
- Fixed findAxis math function.
- ACTUALLY hotfix for level crash.

------------------------------------------------------------------------------------------

# snapshot-2024.11.9 - (pre-1.7.0) > [Nov 7, 2024]

## Fixes
- Hotfix for level crash.
- Fixed tutorial level being replaced by the second level.

------------------------------------------------------------------------------------------

# snapshot-2024.11.8 - (pre-1.7.0) > [Nov 7, 2024]

## Features
- Modifiers can now be organized using the modifier context menu.
- Looking into adding an "else" toggle for trigger modifiers.
- Added player0Rot variable to math evaluators.
- Added mousePosX and mousePosY to math evaluators.
- Added screenWidth and screenHeight to math evaluators.
- You can now shift the index order of objects around. What this means is objects that appear above others can now be shifted to below others.
- Added applyAnimation, applyAnimationFrom and applyAnimationTo action modifiers.

## Changes
- Trigger modifiers now have their inactive state properly triggered if when the object respawns.
- Finally reworked the story mode system to be data driven. Which means if you know how you can probably make your own story mode. This also means I can finally get some work done on the story!

------------------------------------------------------------------------------------------

# snapshot-2024.11.7 - (pre-1.7.0) > [Nov 6, 2024]

## Features
- Added objectAlive trigger modifier. This triggers whenever an object from a specific group is alive. Good for timing specific actions.
- You can now view a level creator's levels by clicking the [ Creator ] button in the Play Level menu. Requires the level to have been uploaded after this the release of this snapshot, however.
- Slowly working on a new story mode system so it'll be easier for me to work on.

## Changes
- Prefabs spawned from modifiers are no longer selectable in the preview area.

## Fixes
- Fixed some cases where the Prefab Group Only toggle wasn't showing for modifiers that have object group fields.

------------------------------------------------------------------------------------------

# snapshot-2024.11.6 - (pre-1.7.0) > [Nov 6, 2024]

## Features
- If you want a Prefab spawned from a modifier to not despawn when the main objects' lifespan is done, then you can turn Permanent on.
- Added a way to remove any unwanted spawned modifier Prefabs from the level via Edit > Clear Modifier Prefabs.
- Added a few new prefab spawning related modifiers.

## Changes
- Modifiers now clear specific things when their parent object is deleted.

## Fixes
- Fixed the inactive state of spawnPrefabOffset not occuring.
- Fixed the editor for spawnPrefab modifiers being broken.

------------------------------------------------------------------------------------------

# snapshot-2024.11.5 - (pre-1.7.0) > [Nov 5, 2024]

## Changes
- Completely replaced the math evaluator system with a better one written by Reimnop. Some functions will need to be changed to work, such as findAxis(object_tag, ...) now being findAxis#object_tag(...). Learn more in the editor documentation.

------------------------------------------------------------------------------------------

# snapshot-2024.11.4 - (pre-1.7.0) > [Nov 5, 2024]

## Features
- Added spawnPrefabOffset modifier. This will spawn a prefab at the objects' exact position / scale / rotation, regardless of if the object is an empty or not.

## Fixes
- Fixed an edge case with math evaluators where multiple brackets weren't recognized. For example, "((1 + 1) * 2) * 15".

------------------------------------------------------------------------------------------

# snapshot-2024.11.3 - (pre-1.7.0) > [Nov 4, 2024]

## Features
- Added a preset setting to Editor > Editor GUI > User Preference.
- 1.7.0 is still the story mode update, so currently doing some rework so I can finally finish the chapter 1 demo for the full release of the update.
- Math evaluators now allow for variables to be turned to negative using a -.
- Added a ton of math related modifiers.

## Changes
- Render depth now uses the limited range if your Editor Complexity is not set to advanced.
- Some more player modifiers can now allow empty objects.

## Fixes
- Fixed some more problems with the math evaluators, behaviour should be fully consistent now.

------------------------------------------------------------------------------------------

# snapshot-2024.11.2 - (pre-1.7.0) > [Nov 2, 2024]

## Features
- Added forwardPitch to math evaluators.

## Changes
- Completely reworked the way math evaluators work. They no longer need a ; at the end of each top-level function, supports proper matching of variables and deep nested functions.
- blackHole modifier and copyAxis visual now allows for empty objects.

## Fixes
- Hopefully fixed the cases where objects wouldn't properly delete.

------------------------------------------------------------------------------------------

# snapshot-2024.11.1 - (pre-1.7.0) > [Nov 1, 2024]

## Features
- Added animateObjectMath modifiers.
- Added copyEvent(type, valueIndex, time) to math evaluators.
- You can now right click the Upload Acknowledgements text to show the Upload a Level documentation.

## Changes
- Updated Editor Documentation.

## Fixes
- Fixed objects rendering on the event layer and other layers its not supposed to.

------------------------------------------------------------------------------------------

# snapshot-2024.10.5 - (pre-1.7.0) > [Oct 28, 2024]

## Features
- Add File to Level now allows for directly importing a Prefab into your level from anywhere.
- Added a context menu to keyframes.

## Changes
- Renamed "Add File to Level Folder" to "Add File to Level".
- Slightly optimized Follow Player keyframe.

## Fixes
- Fixed the Config Manager UI rounded corners not being on the right corners.
- Fixed some issues with the audioSource modifier.
- Fixed modifiers activating when they shouldn't. in-editor or when restarting a level due to the smoothed level time.

------------------------------------------------------------------------------------------

# snapshot-2024.10.4 - (pre-1.7.0) > [Oct 17, 2024]

## Changes
- Re-ordered player updates so tail updating occurs after player position is set.
- Optimized modifiers a tiny bit further.

## Fixes
- Fixed a bug with the last snapshot that broke levels.

------------------------------------------------------------------------------------------

# snapshot-2024.10.3 - (pre-1.7.0) > [Oct 17, 2024]

## Features
- Added "Show Experimental Features" toggle to Config Manager > Editor > Editor GUI. This is for specific features that might change in the future, so this toggle is to let people know what is experimental and what isn't. (Homing keyframes and parent desync are currently considered experimental due to them breaking in some cases)
- Added controlPress modifiers. These are used to detect controller inputs.

## Changes
- Duplicate themes now display in the theme list, but will not be usable.
- Updated the story files to the current version.

------------------------------------------------------------------------------------------

# snapshot-2024.10.2 - (pre-1.7.0) > [Oct 4, 2024]

## Features
- Added playerMoveToObject modifiers. These modifiers lock the players' positon / rotation to the object. The object cannot be empty.
- Added sampleAudio(int sample, float intensity) to math evaluators.
- Added a setting for showing / hiding Background Objects. If off, there will be a significant FPS boost for levels that use Background Objects, but unfortunately won't have them render.

## Changes
- Objects with LDM on no longer run their modifiers if the LDM config is on.

## Fixes
- Tried fixing VG to LS Converting issues.

------------------------------------------------------------------------------------------

# snapshot-2024.10.1 - (pre-1.7.0) > [Oct 3, 2024]

## Features
- BetterLegacy now has a demo of chapter 1!
- Added a way to view levels you've uploaded via the "View Uploaded" button in the Upload dropdown in the editor.
- Added a config for changing Marker right click behavior.
- Added some context menus to the timeline bar elements.
- You can now directly parent an object to the camera via the parent search context menu. Additionally, you can also view the child tree and/or parent chain of an object via the parent button context menu.
- Added Convert to VG buttons to context menus where it's needed.
- Added a few more buttons to different context menus relating to the file system.
- Object origin offset now has a context menu.
- Added a Mode dropdown to the Color Split, Ripples and Double Vision event keyframes.
- Started working on config presets.
- Added a new "Modern" timeline waveform type. This is based on the alpha editor timeline waveform where the texture is only along the bottom.

## Changes
- Optimized playerCollide modifier by using property instead of GetComponent.
- Improved enable/disableObject modifiers code.
- Improved object updating a tiny bit more.
- Hovering the mouse over a transparent object with highlight objects on now sets the object to opaque.
- Online levels tab now refreshes when you switch pages.
- User's Display Name now defaults to the Steam username, if Display Name is still "Player".

## Fixes
- Fixed Window event keyframes previewing in the editor instead of preview mode.
- Fixed player noclipping issue.
- Fixed player objects not properly checking invincible / zen mode properties.
- Fixed player spawn animation breaking after the player is set active when it was inactive before
- Fixed no_volume considering Audio event keyframe value instead of just the user's config.
- Fixed the player moving when the camera rotates in 3D where it shouldn't.
- Due to the above fix, the player should now ALWAYS stay in the bounds of the camera if Out of Bounds is turned off in the player event keyframe. There were some cases where the player bugged and exited the camera (Such as in Subnautix' Stormbringer).
- Fixed a bug with object opacity where it will default to 1 if it's higher than or equal to 0.9.
- Fixed random error log when opening Config Manager via the "Editor Preferences" button.