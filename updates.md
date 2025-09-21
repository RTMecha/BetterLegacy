# 1.8.1 [Sep 21, 2025]
## Features
### Editor
- Added an origin indicator to the capture area.

## Changes
### Core
- particleSystem and trailRenderer modifiers now stop emitting when the modifier is inactive. This means you can now precisely control when these emit.

## Fixes
- Fixed capture area becoming offset in some cases.
- Fixed Prefab Panel delete button position setting having the wrong default value.
- Fixed some of the blur modifiers not applying the "Set Back to Normal" value.
- Fixed level combining not carrying over some data.

------------------------------------------------------------------------------------------

# 1.8.0 [Expansion Update]
## Features
### Story
- The story intro now features a keypad where you can enter a custom name for the save slot.
- The base interface also includes a keypad.

### Core
- Server:
  - Level Collections can now be uploaded and downloaded.
  - Prefabs can now be uploaded and downloaded. Uploaded Prefabs from you or other people can be viewed from the View Uploaded dialog.
  - All server items can be collaborated on. This means you can grant specified people permissions to update the level and view it if the level is unlisted / private.
- Configs:
  - Added "Modifiers Display Achievements" in Config Manager > Editor > Modifiers. This allows achievements to display in the editor when they're unlocked via a modifier.
  - Added "Load Sound Asset On Click" in Config Manager > Editor > Data.
  - Finally added Borderless Fullscreen. If the window is in fullscreen, it will not minimize when unfocused with this setting on.
  - Added a few settings for the Object Drag Helper for customization.
  - Added "Open Custom Object Creation Options" in Config Manager > Editor > General. This opens the custom object options popup by default when clicking the Object button.
  - Moved Object Creation settings from Editor > General to Editor > Creation, and added a few more settings for this tab.
  - Added "Analyze BPM On Level Load" in > Config Manager > Editor > BPM.
  - Added "Apply Tail Delay" in Config Manager > Player > General. This forces the tail to not apply the Tail Base Time value. Older versions of PA does not apply the time value delay and ends up making it look nicer.
  - Added "Marker Loop Behavior" in Config Manager > Editor > Markers. This allows you to control how the marker looping works.
  - Added "Snap Created" in Config Manager > Editor > BPM. This forces new objects to snap to the BPM.
  - Added "Retain Copied Prefab Instance Data" in Config Manager > Editor > Data. If the copied Prefab instance data should be kept when loading a different level.
  - Added "Timeline Object Prefab Icon" in Config Manager > Editor > Timeline. With it on, the Prefabs' icon will be prioritzed over the Prefab Type icon.
  - Added "Auto Search" in Config Manager > Editor > Data and Config Manager > Arcade > Sorting.
- Prefabs:
  - Prefab Objects can now use modifiers.
  - Prefabs should now support recursive prefabs. This means prefabs can be contained in other prefabs.
  - Prefab Objects now have a depth offset value.
  - Themes and modifier blocks can now be stored in Prefabs.
  - Reworked Prefab Objects to have their own runtime system. Using prefabs should now be a whole lot more optimized.
- Modifiers:
  - Added else trigger modifier. This inverts the current trigger modifier check. It can be used either as a "not gate" or after a set of triggers & actions to act as an "else" statement in programming.
  - Added a "comment" modifier that allows you to describe what a modifier function does. The comment can be locked / unlocked by right clicking the input field and selecting the context menu button. (editor only)
  - Added await and awaitCounter trigger modifiers. This waits for a set amount of time when active and then triggers.
  - Added resetLoop modifier. This allows already activated non-constant modifiers to run again. The current loop continues running.
  - Added isFocused trigger modifier.
  - Added setRenderType and setRenderTypeOther modifiers. These can Set the render type of the object. Don't know why these weren't in there earlier.
  - Added setTheme and lerpTheme action modifiers. This sets a custom fixed theme that overwrites the theme interpolation.
  - Added spawnPrefabCopy modifiers. These search for an existing Prefab Object and if one is found, copies its data.
  - Added setPrefabTime modifier. This overrides the Prefab Objects' runtime time and sets a custom time.
  - Added playerCollideIndex modifier. Acts like the regular playerCollide modifier except for a specific player.
  - Added storeLocalVariables modifiers. This stores the current local modifier variables to this modifier and passes it to other modifiers in future tick updates.
  - Added playerDrag modifiers. This drags the player with the object but allows it to move. Currently the "Use Rotation" value does not work.
  - Added eventEquals modifiers. These compare an event keyframes' value at a specified time or the current time if the "Time" value is left empty.
  - Added onLevelStart and onLevelRewind modifiers.
  - Added getCurrentLevelID and getCurrentLevelRank modifiers.
  - Added loadLevelCollection modifier.
  - Added getPlayerLives, getLives and getMaxLives modifiers.
  - Added exitInterface modifier. Used for cases where you want to exit the currently open interface under specific circumstances. (Does not work on the pause menu / end level menu. It's just for the loadInterface modifier)
  - Added "Pause Level" and "Pass Variables" values to the loadInterface modifier.
  - Actually implemented playerVelocity modifiers. (they didn't have code before)
  - Added "Remove After Despawn" value to spawnPrefab modifiers.
  - Added whiteHole and more blackHole modifiers. The blackHole modifier now only targets the nearest player and no longer has the "Use Opacity" setting, as there are better ways of doing it now.
  - Added getVisualOpacity modifier.
  - Added playerLockX and playerLockY action modifiers. These prevent the player from moving in the specified axis.
  - Added getAchievementUnlocked and achievementUnlocked trigger modifiers. These check for achievement unlocked states.
  - Added playerEnable modifers. This shows / hides specific players.
  - Added playerEnableDamage modifies. Modifies players damageable state.
  - Added getEditorBin, getEditorLayer and getObjectName modifiers. Good for debugging, level editing and sandboxing. Can be used outside the editor.
  - Added loadSoundAsset action modifier. This loads / unloads a specified sound asset. If loaded, the sound asset will have no load time when playing the sound.
  - Added setCustomObjectIdle modifier. Player animations no longer override the idle state of the object. Only compatible with player related objects.
  - Added onCheckpoint and onMarker trigger modifiers. These trigger when a checkpoint / marker matching the provided values is reached.
  - Added objectActive and objectCustomActive trigger modifiers. These check if the objects' active state is true.
  - Added callModifierBlockTrigger trigger modifier. This does the same thing as the regular callModifierBlock action modifier, except this modifier checks for the final trigger state of the modifier block.
- Player:
  - Implemented Player Modifiers.
  - Added "Allow Player Model Controls" to the Player settings. This allows player models to retain their control values, while making regular levels fair again.
  - Tail parts can now be added / removed in player models.
  - Added rotation speed and curve type to player editor.
  - Added Sprint & Sneak speeds to the Player.
  - You can now edit Player object animations by going to the Custom tab in the Player Editor, selecting a custom object and clicking "View Animations".
  - You can also view the levels' animation library by going to the View dropdown and selecting "View Animations".
- The Audio event keyframe and sound modifiers now have a "Pan Stereo" value. This allows control of the left / right direction the sound is coming from, emulating 3D spaces.
- Implemented seed based random. Can be turned off by going to Config Manager > Core > Level > Use Seed Based Random and turning the setting off if anything breaks or you prefer the original randomization method.
- Implemented the new Checkpoint features to the Checkpoint Editor.
- Added Lives count and Respawn Immediately toggle to the Player Editor.
- Implemented custom achievements. These can be edited via the Edit dropdown in the editor and via the View Achievements button in the Arcade's play level menu.
- Implemented level modifiers. Level modifiers can be converted to triggers in the VG format, but will only save the first trigger and action of a set of modifiers.
- Implemented modifier blocks. These can be called using the callModifierBlock modifier. Good for compacting and reusing modifier code, but not recommended for prefab models, unless you add the modifier blocks to the prefab.
- Level now has settings for start & end offset, default end function, if the level should auto end, etc.
- Added "Preferred Control Type" setting to level metadata. This will be used to notify users of levels with specific mechanics.
- Implemented uploader & creator data to Prefabs, Beatmap Themes and Player Models.
- Fully implemented sprite & sound asset system. These include a list in the editor and a new modifier: "loadSoundAsset".
- Levels now include a video link, where you can provide a recorded video of the level. The link can only be the ID of the video, or after the "watch?v=" part of the link.
- Added a Creator value to Themes and Prefabs, so you can finally credit those assets.
- Added a bunch of fonts.
- Levels can now have their icon set to a "locked.jpg" file if the level is locked.

### Arcade
- Online tab now automatically searches when you switch to that tab.
- Added viewing online Level Collections to the Arcade menu.
- Online tab now has sort settings.

### Interfaces
- Interface now has a dynamic variable system that allows for specific variables to affect anything in the interface.
- Implemented interface list. This allows for easier interface branching and interface loading.
- Improved the Profile menu to include pages and a list of achievements.

### Example Companion
- You can now close Example's chat bubble by clicking it. Useful if his dialogue gets in the way of something.

### Editor
- Object keyframe UI can now be customized via right clicking the X / Y / Z values and changing the UI to a Input Field, Dropdown or Toggle. You can also customize how each of these display.
- Added a way to view your user ID and copy it to the Metadata editor.
- Collaborators can now be added to a level. This means other users can post to the same level and also see it in the Arcade / Editor, even if it's private.
- Fully implemented level collection editing. You can view them in the Level Collections popup under the File dropdown.
  - Levels can be added to the collection either via the level collection itself or via the level panel context menu.
  - "Add File to Collection" context menu button copies the level folder to the collection and adds the reference to the collection. This is only if you want the level to be exclusive to the level collection.
  - "Add Ref to Collection" context menu button only adds the reference. It's recommended the level is on the Arcade server or the workshop before using this button.
- You can now replace the current loaded levels' song by dragging a song file onto the PA window.
- Fully implemented new "Pinned Editor Layer" system. This allows you to fully organize your editor layers, including layer names, descriptions and custom colors!
- Default Prefab Object instance data can now be saved to and loaded from Prefabs. If you have copied instance data, that will be priotized over the saved instance data. This can be used by spawnPrefab modifiers.
- You can now add a default tag to the levels' metadata by right clicking the "Add Tag" button and selecting "Add a Default Tag".
- If you want to refresh an objects' randomization, you can go to the Object Editor, right click the ID and click "Shuffle ID".
- "Pull Level" now exists as a server button in the MetaData Editor.
- The Player editor now has a way to edit the Player Control of a Player by turning "Edit Controls" on in the editor.
- You can now enable / disable the "Show Collapse Prefab Warning" setting via the "Apply / Collapse" buttons' context menu.
- Modifier cards can now display a custom name and have a description display in the info box.
- Keybinds
  - Overhauled the keybind editor to include a new "Keybind Profile" system. Due to this, you will need to setup your keybinds again.
  - With the new profile system, a "SwitchKeybindProfile" function was added to the keybind functions.
  - Added "SetPitch" keybind.
  - Added "ToggleObjectDragHelper" keybind.
  - Added "SetObjectDragHelperAxisX" and "SetObjectDragHelperAxisY" keybind.
- Added "Screen Overlay" default object in the "More Options" popup. Good for fading the screen in / out.
- The animation timeline should now use the Use Mouse As Zoom Point setting.
- Overhauled the Theme system to have internal / external themes. The "View Themes" popup is now used for viewing and importing external themes and the theme keyframe is now for viewing internal level themes.
- Object and Parent Object Search Popups now have a page system.
- Tweaked some editor UI.
- Added some functions to the Multi Object editor.
- Added a file version field to Prefabs, levels, level collections and player models. You can right click the field to edit the version number.
- Player models now have a creator field.
- .vgp Prefabs now load in the External Prefabs list.
- Ported Prefab preview from VG, except you can customize the Prefab's icon with any JPG image file. You can also use the new Capture Area box.
- Added a Capture Area box. This acts as an in-game camera that is draggable. The outline can be dragged to resize the resolution, the slider can be changed to set the zoom, the corners can be dragged to rotate and the inner box can be clicked to take a screenshot of that area.
- Server related processes now have a progress bar popup.

## Changes
### Story
- The end of chapter 1 now has a confirmation interface, giving you a chance to do anything else before you're locked out of progressing the chapter.

### Core
- Beware, some stuff might be broken and definitely will not be compatible with 1.7.x and below.
- Updated demo (Beatmap.zip) files to include some prefabs that demonstrate more features.
- Modifiers:
  - Overhauled modifier system to be a lot more unified than before.
  - Modifier JSON format has been tweaked. 1.7.x should still be compatible with this change as it was accounted for a while ago.
  - Tweaked the audioSource modifier a little. It should be better to use, hopefully.
  - Some group modifiers should now be compatible with more object types.
  - The "Object Group" value in modifiers can be left empty to specify the object the modifier is stored in.
  - containsTag modifier now checks for prefab object tags if the object was spawned from a prefab.
  - spawnPrefab modifiers now use the Prefabs' default instance data.
  - Reworked updateObject modifiers into updateObject, updateObjectOther and reinitLevel.
  - The return modifier now resets the trigger check.
  - The "Else if" toggle on trigger modifiers now allows for multiple "or gate" conditions. (e.g. trigger a is false, trigger b has else if on and is true and trigger c is true, which allows the modifier actions to run)
- Player:
  - Player stop boost function no longer depends on the moveable state of the player.
  - Player Model & Player Editor code has been heavily cleaned up.
  - Preferred Player Count value now blocks the user from entering the level if the player count does not match.
- Reworked some MetaData values.
- Tried optimizing the keyframe sequences a little.
- Optimized parent chain updating by a lot.
- Changed some system library references.
- Renamed playSoundOnline modifier to "playOnlineSound" to match "playDefaultSound".
- Players now respawn when UpdateEverything keybind is used.
- Unlock After Completion toggle now applies regardless of challenge mode.

### Interfaces
- Due to the interface list feature, some interfaces have been combined into one file and there's also some new tests to demonstrate the capabilities of the interface now.
- Updated the splash text.

### Editor
- Renamed "Show Intro" to "Hide Intro". This is because in vanilla "Show Intro" was saved as off, despite the intro showing by default, so had to rename it to something.
- Reorganized the default modifiers list and added icons that display the type of the modifier. 
- The "Create Objects Modifier Order Default" setting now applies to imported Prefab Objects and new Background Objects.
- Refractored some editor element code.
- Overhauled the Metadata editor UI.
- Recently opened popups now appear above previously opened popups.
- The editor context menu should no longer overflow to the right of the application.
- Base Player Model control values can now show in default models if Edit Controls is turned on.
- Removed "Adjust Position Inputs" setting. This isn't needed anymore since position Z has been a part of the mod for a long while.
- Improved timeline object loading by a tiny amount.
- Keybinds
  - Renamed keybind function "OpenPrefabDialog" to "OpenPrefabCreator" to make it more clear as to what it does.
  - Renamed keybind function "ToggleEditor" to "TogglePreview".
  - Renamed keybind function "GoToCurrent" to "GoToCurrentTime".
  - Renamed keybind function "ToggleObjectDragger" to "ToggleObjectDragging".
  - Renamed keybind function "SaveBeatmap" to "SaveLevel".
  - Renamed keybind function "OpenBeatmapPopup" to "OpenLevelPopup".
  - Renamed keybind function "OpenSaveAs" to "SaveLevelCopy".
  - Renamed keybind function "OpenNewLevel" to "CreateNewLevel".
  - ToggleBPMSnap keybind function now notifies the user about whether the BPM snap was turned on or off.
- Removed the Level Combiner dialog as you can just select multiple levels in the Open Level Popup, right click them and select "Combine".
- Simplified the editor popup animation register code to make it easier to add new popups.
- Holding alt while BPM snap is on no longer plays the BPM snap sound.
- Reworked the "View Uploaded" editor dialog to allow viewing of Levels, Level Collections and Prefabs.
- Optimized layer setting a little by updating timeline objects in chunks.
- Collapsed Prefab Objects now have their start position set to the Prefab offset.
- Changed Creator Links.
- Increased amount you scroll in some scroll views
- Changed the Document Full View editor in the Project Planner to be scrollable.

## Fixes
- Fixed the labels in the global Prefab section of the Prefab Object editor not displaying.
- Optimized hidden prefab objects in editor.
- Fixed the Fade Colors list in the Background Object editor.
- Fixed spawnPrefabs modifiers being broken.
- Fixed use visual values in axis modifiers not allowing rotation axis other than the first.
- Prefab Name doesn't wrap now.
- Optimized modifiers and prefabs (FPS should be doubled for enableObjectGroup modifiers).
- Fixed entering editor preview taking the song time to the last seeked time.
- Fixed some issues with the Player face parent.
- Fixed polygon shape in Player Editor.
- Fixed the downloadLevel modifier causing a softlock.
- Fixed editor breaking if you were never logged in.
- Fixed config preset breaking render depth.
- Fixed song link not working.
- Fixed setParent modifier re-activating objects when it shouldn't.
- Cursor position modifiers no longer force the position if the application isn't focused or the user isn't editing.
- Fixed some Player model values not saving & loading.
- Fixed the currently selected Custom Player Object being removed when deleting a different custom object.
- Fixed searching Custom Player Objects not working.
- Fixed updateObject modifiers throwing an error.
- Fixed dragging an empty object not updating the time offset of child objects.
- Fixed Window events persisting in menus.
- Fixed Editor colors for BG objects not rendering correctly.
- Fixed camera parenting not being set properly in some cases.
- Fixed negative zoom being grainy due to UI layer camera.
- Fixed setOpacityOther modifier not having prefab group only toggle.
- Finally fixed polygon shape conversion not working in some cases (why so many edge cases omg).
- Fixed Prefab Object speed not affecting the lifetime.
- The Object Drag Helper should now line up more consistently with objects.


## Up Next in 1.9.0
- Online Multiplayer
- New Player features (versus, etc)

------------------------------------------------------------------------------------------

# 1.7.7 [Jul 6, 2025]
## Features
### Core
- Added playerEnableMove action modifiers. These work like the "Moveable" value of the Player event keyframe.
- Added playerCancelBoost action modifiers. These stop the player from boosting, if they were.

## Fixes
- Fixed axisEquals trigger modifiers not being counted for the Toggle Prefab Group Only command.
- Fixed playerAlive trigger modifier not using the variable system and requiring the object to be empty. (This also meant the modifier wasn't compatible with BG objects. It is now)
- Fixed game speed setting multiplying on death.

------------------------------------------------------------------------------------------

# 1.7.6 [Jun 24, 2025]
## Features
### Editor
- Implemented a layers system for markers. With this, markers can be set to only appear on specific layers.
- You can now directly access the Color Picker via the View dropdown.
- Added Timeline Object color editing to the Mutli Object Editor.

## Changes
### Editor
- Color keyframe toggles now update automatically when the HSV values are changed.
- Color Picker now has animations.

## Fixes
- Fixed opacity collision not working in the arcade due to a vanilla component getting in the way.
- Fixed Practice mode subtracting the health.
- Fixed the Color Picker not working in resolutions outside of 1080p.

------------------------------------------------------------------------------------------

# 1.7.5 [Jun 23, 2025]
## Features
### Editor
- Finally modified the Color Picker. That feature has been untouched the entire time during BetterLegacy's development.
  - The Color Picker can now be dragged around.
  - It can be used for stuff other than themes now.
- Colors per-timeline object can now be changed. This includes: Base color, Selected color, Text color and Mark color. This also comes with some new config settings under Config Manager > Editor > Timeline.
- Continuing work on math parsers for editor input fields.
- Added tooltips to the Autokill Type and Object Type dropdowns.

## Changes
### Editor
- Tweaked the sizing of the Object Type dropdown so you can see the full Decoration word.

## Fixes
- Fixed the confirm menu not pausing the game.

------------------------------------------------------------------------------------------

# 1.7.4 [Jun 21, 2025]
## Features
### Core
- Added Burst Count value to the particleSystem modifier.

### Editor
- Added Reverse Indexes button to the Multi Object Editor. This reverses the index order of all selected objects (objects that appear above others).
- Multi Object Editor shape sync now syncs polygon shape values.
- New Background Objects now align with the current editor layer.
- Added Instance Data section to the Prefab Object Editor. This has buttons for copying & pasting Prefab Object instance data from & to objects.

## Changes
### Editor
- Changed some of the colors in the Multi Object Editor to make it more readable.

## Fixes
- Fixed wrong artist credit for a certain story cutscene.
- Fixed blurOther modifiers only applying to one object.
- Fixed modifiers search not working with Background Objects.
- Fixed duplicating Prefab Objects at start time 0 not having their bin added to.
- Fixed updating prefabs creating duplicates.

------------------------------------------------------------------------------------------

# 1.7.3 [Jun 18, 2025]
## Features
### Example Companion
- Added Toggle Modifiers Prefab Group Only command. This iterates through all selected objects and turns the Prefab Group Only option on for all group modifiers.

## Changes
### Editor
- Updated the Example prefab model to include better controls.
- UpdateObject and UpdateEverything keybind actions now updates all modifiers in an object.

------------------------------------------------------------------------------------------

# 1.7.2 [Jun 17, 2025]
## Fixed
- Fixed cutscene select being bugged.

------------------------------------------------------------------------------------------

# 1.7.1 [Jun 17, 2025]
## Features
### Core
- Added getTag action modifier and containsTag trigger modifier. getTag gets a tag at an index from the object. containsTag checks if the object has a tag.

## Changes
### Core
- Renamed parent desync JSON name from "desync" to "pd".

## Fixes
- Fixed certain interfaces lacking a name.
- Fixed wrong level metadata names.
- Fixed Prefab Object parent type toggles default value not being correct, resulting in the values not being saved.
- Fixed hidden object editor data not being saved to Prefab Objects.
- Fixed fifty_levels achievement displaying the wrong name.
- Hopefully fixed custom player models being usable in the story. Since this story requires specific players, this is necessary.

------------------------------------------------------------------------------------------

# 1.7.0 Unfractured Update [Jun 15, 2025]
## Features
### Story
- Demo of Story Mode chapter 1. Go check it out in the STORY menu!
- The Story Mode can be customized via the "story.json" file. Story levels accept both Unity asset bundles and regular .lsb levels (+ .vgd levels if you really want).
- The story is heavily WIP and is subject to change. However this demo should represent a near final version. (When future chapters release, you may need to replay the previous chapter to experience it fully)
- If you want, you can contribute to the story mode by getting in contact with me (RTMecha). (However not every contribution will be accepted)

### Core
- Added Configs:
  - "Physics Update Match Framerate" in Config Manager > Core > Game. This setting forces physics to match your framerate, this includes player movement and collision.
  - Removed the Modifiers tab and moved its settings to Config Manager > Editor > Modifiers.
  - New "Cursor" sub-tab in Config Manager > Core. This has settings related to the cursors' visibility.
  - "Bin Clamp Behavior" in Config Manager > Editor > Timeline. This changes how timeline object bin dragging is handled when the bin is dragged outside the normal bin range.
  - "Show Background Objects" in Config Manager > Core > Level. Used for showing / hiding Background Objects. If off, there will be a significant FPS boost for levels that use Background Objects.
  - "User Preference" in Config Manager > Editor > Editor GUI. This will automatically change specific settings to fit your style of editor.
  - "Marker Show Context Menu" in Config Manager > Editor > Timeline. Changes marker right click behavior to show a context menu instead of deleting the marker.
  - "Show Experimental Features" in Config Manager > Editor > Editor GUI. This will enable / disable features that are still heavily WIP.
  - Moved some Player settings to other sub tabs. There's now "Controls" and "Sounds" sub tabs.
  - "Play Spawn Sound" in Config Manager > Players > Sounds. This plays a sound when a player spawns or respawns.
  - "Play Checkpoint Sound" and "Play Checkpoint Animation" Config Manager > Core > Level. For toggling the checkpoint sound / animation.
  - Added Arcade Folder and Interfaces Folder to custom menu music options.
  - Master, Music and SFX volumes now play a sound to demonstrate the current volume.
  - "Play Pause Countdown" in Config Manager > Core > Game. With it off, it skips the unpause countdown sequence and brings you immediately to the level.
  - "Paste Background Objects Overwrites" in Config Manager > Editor > Data. This removes the current list of BG objects and replaces it with the copied list.
  - "Mouse Tooltip Requires Help" setting to Config Manager > Editor > Editor GUI. With it on, the mouse tooltip requires the help info box to be active in order to show. It's off by default.
  - "Clamped Timeline Drag" setting to Config Manager > Editor > Timeline. With it off, objects can be moved to outside the song range.
  - "Timestamp Updates Per Level" setting to Config Manager > Core > Discord. With this on, the Discord status timestamp updates every time you load a level.
  - "Show Markers" setting to Config Manager > Editor > Markers.
  - "Overwrite Imported Images" setting to Config Manager > Editor > Data. With it on, imported images overwrite existing image files.
  - "Apply Game Settings In Preview Mode" to Config Manager > Editor > General.
  - "Object Dragger Helper" to Config manager > Editor > Preview. This displays the location of the current object (includes empty and excludes origin offset). Can be dragged and right clicked for a context menu.
- Custom configs can kinda be registered now.

### Interfaces
- Interface text typing can now be sped up by holding the left mouse button. You could already do this with space (keyboard) / A (xbox) X (ps).
- Unpause sequence can now be sped up if the user is speeding up the interface by pressing left mouse button / Xbox controller A button / PS controller X button / keyboard space.
- Input Select menu now has theme music.
- Steam Workshop search sorting (with some extra toggles in Config Manager > Arcade > Sorting).
- You can now view a level creator's levels by clicking the [ Creator ] button in the Play Level menu. Requires the level to have been uploaded after the release of this version, however.

### Game
- Order Matters toggle for modifiers! This makes it so trigger and action order works more like actual code-blocking.
- Else if toggle for trigger modifiers allows different sections of triggers to run the same action modifier.
- Added "Run Count" to modifiers. This acts similarly to constant, except the modifier can only run a set amount of times before stopping.
- Added a "group alive" value to modifiers.
- Ported most compatible modifiers to BG objects.
- Started working on multi language support for stuff outside of tooltips.
- Some secrets ;)
- Mode value for Color Split, Ripples and Double Vision events.
- spawnPrefab modifiers now have "Permanent", "Time Offset" and "Search Prefab" values.
- Added Limit Player settings to the Global tab of the Player Editor. Limit Player is on by default, so if you made a level that has a custom player with specific speeds, please change this.
- The default level intro text can now be disabled via turning "Show Intro" off in the MetaData editor.
- textSequence modifier now has some more values that can be used to add some flavor to your dialogue.
- Music time trigger modifiers now have a Start Time relative value.
- endLevel modifier now has a customizable end level function.
- "Spawn Players" toggle to the Player Editor. With this off, players will not spawn at the start of the level, allowing for cutscenes. If you want to respawn players, create a checkpoint or use the playerRespawn modifiers.
- It now should be possible to parent custom player objects to other custom player objects using the Custom Parent value.
- Added Replay End Level Off setting to the MetaData.
- Ported the Polygon Shape from VG.
- With the Polygon Shape fully implemented, new values were added to it: Thickness Offset, Thickness Scale and Angle.
- Polygon Shape ported to Custom Player Objects.
- Added Store / Clear Data to Player Image Objects. This now means images from Image Objects can be transfered between levels.
- "Align Near Clip Plane" value has been added to the Camera Depth event keyframe. This prevents objects in the Background render type from clipping into the camera, but also stops the negative zoom bleeding from working.
- Compatibility with Shake X & Y from VG.
- Ported gradient scale & rotation from alpha.
- Text Origin auto align to be compatible with how VG handles text object origin offset.
- Parallax Objects from VG can now be converted to and from BG Objects. It's not 100% accurate yet, but at least it's something.
- Player healing now plays a sound and an animation.
- Custom player animations have been implemented internally.
- Added default sounds:
  - example_speak
  - hal_speak
  - anna_speak
  - para_speak
  - t_speak
  - menuflip
  - record_scratch
  - HurtPlayer2
  - HurtPlayer3
  - HealPlayer
  - shoot
  - pop
- Added modifiers:
  - Added return modifier. This prevents the rest of the modifier loop from running if it runs.
  - Added break trigger modifier. This modifier is always active, which creates a break in action trigger checking.
  - Added forLoop and continue modifiers. These allow you to run the next set of modifiers up until a return modifier a certain amount of times.
  - Added a ton of "get variable" action modifiers. These modifiers gets a specific variable and store it for other modifiers to use.
  - Added localVariableEquals trigger modifiers. These modifiers compare local modifier variables.
  - Added getSignaledVariables and signalLocalVariables modifiers. These send / recieve the current local variables.
  - Added createCheckpoint and resetCheckpoint modifiers. These modify the active checkpoint.
  - Added animateColorKF and animateColorKFHex modifiers. These allow you to animate both Beatmap Object and Background Object colors.
  - Added setShape modifier. This does exactly what the name suggests.
  - Added trailRendererHex modifier. Acts like the normal trailRenderer modifier, except it allows for hex codes for the colors.
  - playerMoveToObject action modifiers. These lock the players' position & rotation to the object.
  - controlPress trigger modifiers. These detect controller inputs.
  - animateObjectMath action modifiers. These evaluate a math equation and use the result for the objects' transform offset values.
  - mathEquals trigger modifiers. These evaluate a math equation and compare them to another math equation.
  - spawnPrefab modifiers can now find a prefab via different ways: Index, ID and Name.
  - spawnPrefabOffset action modifiers.
  - spawnMultiPrefab action modifiers.
  - clearSpawnedPrefabs modifier. This finds a group of objects and clears all prefabs spawned from their modifiers.
  - objectAlive trigger modifier. This checks if an object from a group is alive.
  - objectSpawned trigger modifier. This checks if an object from a group just spawned.
  - applyAnimationFrom and applyAnimationTo action modifiers.
  - levelExists and levelPathExists trigger modifiers. These check if a level exists, allowing you to check if players have downloaded a required level or not.
  - doubleSided action modifier. This ensures both sides of an object are rendered for 3D rotation.
  - playerRespawn action modifiers. Respawns the closest player or all players.
  - clearHits, addHit and subHit action modifiers. Modifies the hit counter that is used with calculating a level rank.
  - clearDeaths, addDeath, subDeath action modifiers. Modifies the death counter. (Currently the death counter is unused, but will never be considered for the level rank calculation)
  - setAudioTransition action modifier. This sets the audio transition for the next loaded level.
  - setIntroFade action modifier. This is if you don't want the intro fade to play for the next loaded level.
  - setLevelEndFunc action modifier. Sets the function that occurs when the level ends.
  - loadLevelInCollection action modifier. Loads a level in the current collection.
  - downloadLevel action modifier. Prompts the user to download a level if they don't have it. If they do, opens the level.
  - setMusicPlaying action modifier. This sets the playing state of the current song. This feature is more for sandboxing and less to be used in actual levels due to pause menu taking priority in playing state.
  - pauseLevel action modifier. Pauses the game and opens the pause menu.
  - setParent action modifier. This temporarily sets the parent of the beatmap object to another, until you update the object or reload the level. Can also remove the parent if you set the object group to empty.
  - detachParent modifiers. This makes an object act as if they have "parent desync" on, except it desyncs from where the current song time is.
  - Reworked setVariable modifiers to only set the variable of itself.
  - Added setColorRGBA modifiers. This opperates similarly to setColorHex, except it has individual color channels.
  - Added enableObjectGroup modifier. This allows you to select the active state of multiple groups.
  - setPolygonShape and setPolygonShapeOther modifiers. These modify the values of the current polygon shape.
  - Added isEditing trigger modifier. This checks if you're only in the editor. If you're in preview mode or in the regular game, this won't activate.
  - Added copyEventOffset math evaluator function.
  - Added getModifiedColor and getVisualColor modifiers.
- Replaced math evaluator system with Reimnops' ILMath.
- New math functions!
  - sampleAudio(int sample, float intensity)
  - copyEvent(int type, int valueIndex, float time)
  - findOffset#object_group(int type, int valueIndex)
  - findObject#object_group#Property() ("Property" can be StartTime, Depth and IntVariable)
  - findInterpolateChain#object_group(float time, int type int axis, int includeDepth [0 = false 1 = true], int includeOffsets [0 = false 1 = true], int includeSelf [0 = false 1 = true]) (if type is not position aka type = 0, then don't have includeDepth in the parameters.
  - vectorAngle(float firstX, float firstY, float firstZ, float secondX, float secondY, float secondZ)
  - distance(float firstX, [optional] float firstY, [optional] float firstZ, float secondX, [optional] float secondY, [optional] float secondZ)
  - mirrorNegative(float value)
  - mirrorPositive(float value)
  - worldToViewportPointX(float x, [optional] float y, [optional] float z)
  - worldToViewportPointY(float x, [optional] float y, [optional] float z)
  - worldToViewportPointZ(float x, [optional] float y, [optional] float z)
- New math variables!
  - forwardPitch
  - player0Rot (0 can be other numbers to specify other players, in this case 0 is the first player)
  - mousePosX and mousePosY
  - screenWidth and screenHeight
  - currentEpoch
  - mouseScrollX and mouseScrollY
  - musicLength
  - playerCount
  - activeCheckpointTime
- Background Objects can now be prefabbed.
- Implemented Prefab Object random transform.
- Challenge Mode and Game Speed now exists as Config Manager settings. These only work in the Arcade (aka not Story & Editor) and only update when a level begins. They also have extended functionality, so a custom game speed / challenge mode can be registered.

### Editor
- Added timeline Bin Controls. This means you can add / remove bins from the timeline and scroll up and down. You can find some settings related to this in Config Manager > Editor > Timeline.
- Timeline objects can have their "rendering layer" changed by shifting the index of the objects. What this means is you can customize if an object will appear above or below another object.
- You can now click and hold the mouse scrollwheel to drag both the main timeline and the object keyframe timeline around. This also means you can see objects before the level starts.
- Multiple levels can be selected by holding down the Shift key. This will be used for creating collections in 1.8.0 and combining levels.
- Background Objects are viewable as timeline objects now.
- To go with this feature, BG objects now have editor settings (layer, bin, index).
- Editor Layer toggles now re-enable when Editor Complexity is set to "Simple".
- View Uploaded dialog. Here you can view and download the levels you uploaded.
- Add File to Level now allows for directly importing a Prefab file.
- You can now edit the transform offsets of multiple prefabs in the Multi Object editor.
- Ported old VG waveform type "Bottom".
- Modifiers are now collapse-able.
- Folders in the level, theme and prefab lists now have a "folder" icon. This can be customized by adding a "folder_icon.png" file to the folder or by using the context menu to set it. You can also give the folder a short description by creating a "folder_info.json" file.
- Click the Upload Acknowledgements text link to show Upload a Level editor documentation.
- Clear Modifier Prefabs in the Edit dropdown.
- Timeline Objects' are now Unity Explorer (if it's installed) inspectable in the Object Editor.
- Modifiers in the editor now now have a little bar that lights up when they are active.
- Reload Level in the File dropdown.
- Added Open Source link to the Help dropdown in the editor.
- Project Planner TO DO items can now have their priority changed.
- You can now delete level templates.
- Simplified the level combining process by adding a PA type dropdown and shortening the default path so it looks less daunting.
- Added a reload button to the Keybinds popup. This resets your keybinds to the default list.
- Themes now display info about their colors.
- You can now just click a theme to use it.
- Prefab objects now display a line showing where their offset is at.
- Easing Dropdown in the Multi Object Keyframe Editor now has an "Apply Curves" button.
- Improved BPM Snap settings and added a BPM sub-tab to Config Manager > Editor.
- Added Context Menus:
  - Timeline bar elements context menus.
  - Parent context menus.
  - Some context menus now have a Convert to VG function.
  - A few more functions for different file context menus.
  - Object origin offset context menus.
  - Keyframes context menus.
  - Modifiers can be shifted up and down the modifier list via the modifier context menu. (Useful for the new "Order Matters" toggle) you can also use "Add / Paste Above" or "Add / Paste Below".
  - "Copy Path" to the File Browser context menus.
  - "Shuffle ID" to theme context menus.
  - Added a Text Object context menu. Comes with a new Font Selector Popup list.
  - Added Prefab context menus.
  - Search field filters (default themes, used themes, used prefabs)
  - Default prefab / theme paths for level
  - Apply & Create New for the Apply Prefab button in the Object Editor.
  - Edit for internal prefabs.
  - Prefab offset context menus.
  - Object keyframe value context menus.
  - Image Object search
  - Added "Pull Changes" to Upload Level context menu.
  - Added a "Quick Prefab Target" that can be set via the Select Quick Prefab button context menu. (VG's PrefabOnObject mod port)
  - Added context menus to edit the raw data of most modifier values.
- You can now drag and drop files into the game, both in the editor and the arcade.
  - Dragging a level into the arcade / editor will load it.
  - Dragging a txt file into a loaded editor level will create a text object.
  - Dragging an image into a loaded editor level will create an image object.
  - Dragging a prefab into a loaded editor level will import it. If the mouse is over the timeline, it will place it.
  - Dragging an audio file while the New Level Creator popup is open will set the audio path for the new level.
  - Dragging an audio file into a loaded editor level will create an object with a playSound modifier.
  - Dragging a MP4 file into a loaded editor level will set it as the video BG.
- Internally, a custom "base path" can be set in the editor. This means you can have Project Arrhythmia open on one harddrive, while another harddrive has the beatmaps folder. There is no UI to set this yet.
- Added Prefab Object inspect buttons if you have Unity Explorer installed.
- Added an Index Editor to the Object Editor. This means you can now view and edit the index of an object. The index controls what it appears in front of in the timeline. It also includes a context menu for selecting the previous / next objects in index order.
- Added Image Object editing to Multi Object Editor.
- Added Hide Selection keybinds. These allow you to hide Beatmap Objects, Prefab Objects and Background Objects in the editor.
- Selecting specific objects in the editor preview window can now be disabled, via either the Multi Object Editor or the Timeline Object context menu.
- You can now copy & paste multiple modifiers.
- Added a BG Object counter to Prefab Object Editor.
- Added Background and Dialogue to the default Prefab Types.
- Text & Image object selection in preview area can be customized via the new "Select Text Objects" and "Select Image Objects" settings found under Config Manager > Editor > Preview.
- Due to the above feature, text objects can now be highlighted.
- Moved Editor Level code to its own manager.
- Improved Marker looping usability by adding start & end flags to the Timeline Marker and added "Clear Marker Loop" to the Marker Context Menu.
- You can now double click a timeline object / timeline keyframe to go the time of the object / keyframe.

## Changes
### Core
- Shortened file sizes by trimming quotation marks when necessary.
- User's display name now defaults to the Steam username if it is still "Player".
- Scene loading screen now uses the current interface theme instead of the old interface colors.
- Added a label for the Config Manager UI page field.
- Shapes are now loaded from a shapes.json file in the Assets folder.
- Some JSON values have been changed. This means BetterLegacy is no longer compatible with vanilla Legacy. (why would you use that outdated version anyways)
  - BeatmapObject [shape > s]
  - BackgroundObject [depth > iter]
  - BackgroundObject [layer > depth]
  - BackgroundObject ["LOW" > "Bass"]
  - BackgroundObject ["MID" > "Mids"]
  - BackgroundObject ["HIGH" > "Treble"]
  - BackgroundObject ["zposition" > "zpos"]
  - BackgroundObject ["zscale" > "zsca"]
  - BackgroundObject ["color" > "col"]
  - BackgroundObject ["color_fade" > "fade_col"]
- Optimizations:
  - Modifiers in general. They now use the Catalyst spawn / despawn system.
  - playerCollide modifier.
  - enable/disableObject modifiers.
  - Object updating.
  - Objects with LDM on no longer run their modifiers if the LDM (Low Detail Mode) config is on.
  - Follow Player event.
  - Updating parent chains, object type and shapes.
  - Hopefully optimized enableObject modifiers by preventing animation interpolation when it's inactive.
  - setImageOther modifier now loads the sprite before iterating through the group.
- Refractored a lot of Catalyst code to fit BetterLegacy's code style.
  
### Interfaces
- Default Arcade Menu selection is now the "Local" tab.
- Improved Interface Dark theme.
- Added a custom menu flip sound for changing Arcade menu pages.
- Changed the sound of the emoji in the main menu.
- Added the progress menu to the Arcade server downloading.
- Added an Open Workshop button to the online Steam level menu.
- Added rank display to level buttons in the Arcade menus.
- Changed Difficulty mode in the Pause menu to Challenge mode.
- Moved the Profile menu and Tests menu to a new "Extras" menu on the main menu.
- Reworked Input Select and Load Level menus to use the current interface system.
- Entering the Arcade / Story with players already registered will now skip the Input Select menu.
- Entering the Arcade no longer automatically loads the levels, meaning you can go straight from the main menu to the Arcade menu.
- Increased changelog menu interpolate speed.

### Game
- Trigger modifiers now run their inactive function if they aren't triggered.
- Reverted BG camera near clip plane not changing with the zoom. This means the negative zoom frame bleeding effect is back.
- Changed the default image object sprite to the PA logo.
- Got Checkpoint animation now properly updates its colors to the themes' GUI color.
- Player shape type has been removed for the time being. Wasn't happy with how it worked. Might revisit it at some point when I feel I can do it.
- The Game Timeline no longer overlaps when opacity is less than 1.
- BG Object Modifiers no longer use the "Block" / "Page" system. This is to be consistent with other objects with modifiers.
- Modifiers now run before everything else per-tick, but in some situations they run after.
- Trigger modifiers with constant on should now behave as expected. (Only triggers once)
- A ton of modifiers have been tweaked to fit with the new get variable modifiers.
- formatText modifier now passes variables to the text formatting. You can place <var=VARIABLE_NAME> into a text object that has this modifier, with VARIABLE_NAME being one of the variables you registered. formatText has to be at the end of other modifiers.
- showMouse modifier now has an Enabled toggle.
- setImage and setImageOther modifiers now accept stored images.
- animateObject modifiers now have a "Apply Delta Time" value. With this on and the modifier set to constant, the animation will calculate the distance between the previous frame and the current frame. This is for cases where the game has low FPS or lags, but can cause the object to jitter in some cases.
- Removed some outdated modifiers. They still work, but will not appear in the modifiers list.
- Renamed gameMode modifier to setGameMode.
- Changed playerMove Y value from the 5th value to the 2nd value.
- Reworked level modifier system internally to be more consistent with other modifiers. Due to this change, only the first Trigger and the first Action of a Trigger & Action set of modifiers will be converted to VG. (Level modifiers haven't been implemented yet)
- Added X and Y values to playerBoost modifiers. This forces the player to boost in a specific direction. The values can be left empty to not force that value onto the boost.
- Overhauled how homing keyframes work internally. They should now act a lot more consistent with other keyframes. This also means homing keyframes are no longer considered experimental.
- Dynamic homing keyframes speed value now allows for above 1.
- Added some values to the audioSource modifier to give it more control.
- Started working on player boost damage mechanic.

### Editor
- Heavily updated Editor Documentation.
- Hovering the mouse over a transparent object with highlight objects on now sets the object to opaque.
- Duplicate themes now display in the theme list, but will not be useable.
- Trying to rework how themes are loaded.
- Some modifiers like blackHole, copyAxis (with visual on) and playerMove now allow for empty objects.
- Renamed "Add File to Level Folder" to "Add File to Level".
- Editor now informs editor freecam and Show GUI & Players being toggled.
- Ending event keyframe dragging now updates the event editor.
- Some marker editor values now update when dragging a marker.
- Background Objects now update in the UpdateObject keybind action.
- Fonts in the Text Object documentation now display their actual font.
- Added a bunch of mouse tooltips.
- Changed the layout of the Setting Editor so the color editors are side-by-side.
- Setting Editor is now scrollable.
- Shortened some of the color keyframe labels for gradient objects.
- Cleaned up Multi Object Editor UI a little.
- Render depth now uses the limited range if your Editor Complexity is not set to advanced.
- Some more editor elements are considered for the Editor Complexity setting.
- Made gradient objects work with editor highlighting and layer opacity.
- Removed the "Show Levels Without Cover Notification" setting. This setting wasn't really useful.
- Renamed Timeline Waveform type names to better describe what they are. This is done since what was the "Modern" waveform is no longer modern due to modern PA now using what was the "Beta" waveform.
- Timeline grid now fades depending on timeline zoom. This was in last prerelease, just forgot to include it in the changelog.
- SetSongTimeAutokill keybind function now includes Prefab Object autokill.
- Replaced "Timeline Object Retains Bin On Drag" setting with "Bin Clamp Behavior" in Config Manager > Editor > Timeline. This changes how timeline object bin dragging is handled when the bin is dragged outside the normal bin range.
- Overhauled a lot of Background Editor code.
- There no longer needs to always be 1 Background Object in a level. Due to this, "Delete All Backgrounds" now actually does what it says. If there are no Backgrounds present, the editor UI will disable.
- Removed the "Can't delete only object" warning. This means you can technically have an entirely empty level now.
- Custom Background Object reactive values UI now disable if the reactive type is not custom.
- Removed "Yield Type settings" for Expanding Prefabs and Pasting Objects. This isn't really needed anymore.
- Cleaned up Copy, Paste and Delete functions.
- Finally made the Settings button on the title bar consistent with other dropdown menus.
- Shuffled some dropdown menu buttons around.
- Changed MetaData Editor link buttons icons to a link icon to better represent what they are.
- Default Image Object image selection path changed to "images" folder inside the level folder.
- Renamed "Gamma X" "Gamma Y" "Gamma Z" "Gamma W" to "Red" "Green" "Blue" "Global"
- Reworked timeline object deleting.
- Order Matters Modifiers toggle is now on by default for new objects.
- Renamed Image Objects' Set Data to Store Data.
- Reworked default modifiers and tooltips to allow for modifier / tooltip categories.
- All modifier editors now act the exact same, including BG Object Editor having a "Int Variable" displayer.
- Improved some modifier Editor UI.
- Reorganized / reworked a ton of Editor UI.
- Image objects' selection area now changes to fit the image size.
- Removed "Editor Zen Mode" setting, since all game modes are accessible in the editor now.
- Made object keyframe BPM snap dragging consistent with other dragging.
- ForceSnapBPM keybind function now properly snaps the intended selection.

### Example Companion
- Overhauled Example's code. His code was split into modules that handle different aspects of him.
- Example's dance behavior is now configurable and depends on his happiness and boredom.
- Example's dance speed is now randomized.
- Renamed Example config tab to Companion.
- Renamed a few config settings.
- Example now considers the loading level screen a menu.
- Example now only moves to the warning popup if he is far enough away from his spot. (Sometimes...)
- Example can now notice a lot more things that happen, such as saving, autosaving, beating a level, etc.
- Example now has eyelids for more expressions.
- Started working on tutorials. These won't be implemented for a while, but at least the groundwork is there.
- Example now has a low chance to leave by himself if he's bored.

### Internal
- Reworked beatmap data to not depend on base vanilla data as much. (so many nests and odd names wtf)
- Cursor now behaves consistently across the entire game in terms of visibility.
- Interfaces themes are better to utilize now.
- Reworked some Project Planner code.
- Arcade level player data now saves in a non-encrypted arcade_saves.lss file. Since BetterLegacy doesn't have leaderboards (and likely never will), the encryption wasn't necessary.
- Made levels in the editor consistent with other editor data and also use the same level system as the arcade. This technically means you can load VG levels (alpha branch), though themes won't work.
- Cleaned up a ton of Player code.
- Level rank sayings are now customizable (kinda).
- Reworked some editor timeline stuff.
- Reworked a lot of timeline object, timeline marker and theme & prefab panel related code.
- Reworked Editor Dialogs and Editor Popups.
- Reworked the checkpoint system.
- Created a custom enum system. (Only visual difference is the Resolution dropdown now displays the resolution name without the 'p')


## Fixes
### Config
- Error log when Config Manager is opened to a specific tab.
- Config Manager UI not having rounded corners on the right areas.

### Interfaces
- Online tab page buttons didn't switch pages unless you search.
- Players being able to join outside of the input select screen.
- Steam Workshop level subscribing progress not working.
- SS rank shine not working in some cases.

### Game
- Colliders scaled to 0,0 crashing the game. This was fixed by disabling the collider (and renderer) if the objects' scale is 0,0.
- Video Backgrounds freezing randomly while playing a level.
- Player noclipping issue.
- Player objects not properly checking invincibility.
- Player spawn animation breaking.
- Player moving when the camera rotates in 3D space. (still kinda happens but eh)
- Player leaving the bounds of the camera if camera is moved too quickly.
- Achievement no_volume considering the Audio event.
- Object opacity defaulting to 1 if it's above 0.9. Though now, objects don't render both front and back faces due to this fix. Use the doubleSided modifier if you want this behavior.
- Re-ordered player updates so tail updating occurs after player position is set, meaning there shouldn't be as much tail lag now.
- audioSource modifier issues.
- Modifiers activating due to smoothed time.
- textSequence modifier having inconsistent behavior.
- textSequence Play Sound toggle not actually doing anything.
- ~~Prefab Group modifiers checking for both spawned and expanded objects from prefabs.~~ reverted this due to it being needed for individual expanded prefab groups.
- Checkpoints not being removed from the game timeline.
- Checkpoint animations and sounds not playing.
- Players respawning instantly when restarting a level from the pause menu, sometimes causing the player to get hit.
- Shake speed breaking the shake effect when it interpolates from one value to another. (E.G. going from speed 0.5 to speed 2).
- Restarting the level doesn't reset the currently active checkpoint.
- Song pitch not resetting when the user exits to main menu.
- BG camera layer getting offset from the foreground.
- Prefab Object autokill updating being broken.
- animateObject modifiers and shot bullets being inconsistent with different framerates.
- Hue values not loading correctly for Background Objects.
- BG color delay.
- Most parent settings in Prefab Objects not being read from the .lsb files.

### Editor
- Window event being used in editor mode.
- VG to LS Converting being broken.
- Objects rendering on the event layer when they shouldn't.
- Objects not properly deleting in some cases.
- spawnPrefab modifier editor being broken.
- Prefabs spawned from modifiers being selectable in the preview area.
- Prefab Group Only toggle not showing for modifier with object group fields in some cases.
- Picker not working in some cases.
- BPM Snap offset offsetting the time of objects further than it should.
- Checkpoint dragging being broken.
- variableOther trigger modifiers not using the prefab instance group toggle.
- VG level conversion sharing the same file path with the destination, causing it to break. 
- Selecting a single object after selecting multiple prevents rendering the keyframes correctly.
- Warning Popup appearing behind some popups.
- Objects highlighting and not unhighlighting when they disable and re-enable.
- Some Prefab Object Editor stuff being broken.
- Multi Object Keyframe Editor "Apply Curves" button re-rendering keyframes from previously selected objects.
- Default keybinds for First and Last Keyframe selectors being incorrect.
- Non-shape objects not updating their render type when the dropdown value is changed.
- Parent Desync being broken. Due to this, it's no longer considered experimental and now displays by default with Editor Complexity set to Advanced.
- Players not correctly validating in arcade / story levels.
- Event Editor indexer not displaying "E" for end keyframes.
- Object duplicating moving it to where the audio time is if the objects' start time is 0.
- Both timeline's position being set a frame later.
- Theme updating not updating the stored theme.
- Modifier scroll view resetting when refreshing modifier list.
- Clicking on a dropdown now requires left click. This is to fix context menus on dropdowns still interacting with the dropdown.
- Save As not copying sub-directories in the level folder.
- Objects in prefabs parented to the camera not updating properly.
- Modifier editors not disabling when Editor Complexity is changed.
- Fixed Prefab references not being removed when a Prefab is deleted.
- Fixed some issues relating to VG to LS conversion.

### Misc
- Discord status timestamp being used incorrectly.
- Discord status just being "In Main Menu" when you restart Discord.
- Some cases where the "+=" key was set to the wrong key internally.


## Up Next in 1.8.0
- Level collections.
- Seed-based random.
- Custom achievements.
- Player modifiers.
- Profile Menu rework.
- Interface Editor.
- New Checkpoint features.
- And probably more.  

This probably wasn't everything, but either way this is the biggest update BetterLegacy has ever had. I hope you guys enjoy the update! :D

------------------------------------------------------------------------------------------

# 1.6.9 > [Sep 29, 2024]

## Fixes
- Fixed player spawn issues.
- Fixed theme not updating properly in some cases.

------------------------------------------------------------------------------------------

# 1.6.8 > [Sep 28, 2024]

## Fixes
- Fixed copyAxis not updating in the previous update.
- Did some more fixes for pasting / expanding objects.

------------------------------------------------------------------------------------------

# 1.6.7 > [Sep 28, 2024]

## Features
- Added unlock complete to metadata.
- Implemented level collections. You currently can't make them in the editor, however.

## Fixes
- Fixed Gradient toggles showing in Simple editor complexity type.
- Fixed some bugs with pasting / expanding prefabs introduced in the previous patch.

------------------------------------------------------------------------------------------

# 1.6.6 > [Sep 26, 2024]

## Features
- Added context menus to name, tag and some modifier input fields.

## Changes
- Prefab object expanding and object pasting is now a LOT faster. (around 1000 objects per second)
- Reworked some saving / loading code.

## Fixes
- Fixed editor layer changing for objects being broken.

------------------------------------------------------------------------------------------

# 1.6.5 > [Sep 25, 2024]

## Changes
- Reworked marker looping to be set via context menus instead of the config.
- Changed marker dragging to left click.
- Attempting more editor optimizations.

## Fixes
- Fixed some zen mode stuff being done incorrectly in the arcade.

------------------------------------------------------------------------------------------

# 1.6.4 > [Sep 24, 2024]

## Features
- Added some context menus to the built-in file browser.
- Included with the above feature is the ability to preview audio files.
- Modifiers now have a context menu.
- Added Paste Modifier and Paste Keyframes to Multi Object editor.
- Multi Object editor now finally has shape editing.
- Added Update Object button to Timeline Object context menu.
- You can now properly greet Example.

## Changes
- Organized the Mutli Object editor UI.
- Reworked Example chatting internal code a tiny bit.

## Fixes
- Fixed some modifiers not having the correct parameters, breaking the UI.

------------------------------------------------------------------------------------------

# 1.6.3 > [Sep 23, 2024]

## Features
- Added theme folder navigation.
- Re-implemented online Steam level viewing and the rest of the functions to the arcade menus.

## Changes
- Removed old arcade menus.
- Online Steam search, subscribing and unsubscribing now has better functionality.

## Fixes
- Fixed image objects having their origin break when they appear.

------------------------------------------------------------------------------------------

# 1.6.2 > [Sep 23, 2024]

## Features
- Added an identifier for level format in the Play Level menu. LS / Legacy levels show a Legacy PA heart, VG / Alpha levels show a modern PA heart.

## Fixes
- Alpha layer hotfix.
- Fixed Input select screen text not displaying correctly.

------------------------------------------------------------------------------------------

# 1.6.1 > [Sep 23, 2024]

## Features
- Added context menus to timeline objects.
- You can now rename levels and folders via the level context menu.
- Added animation config for the Folder Creator (aka File Popup).

## Changes
- Changed level button text formatting, replaced song title with level name.
- Made dev player model the default on alpha levels.
- Changed dev player model boost cooldown to 0.

## Fixes
- Fixed themes on some older levels breaking the game.
- Fixed some layer issues with alpha levels.
- Fixed Arcade Browser next page button not allowing itself to be pressed.
- Folder creator now disappears when you create a folder.

------------------------------------------------------------------------------------------

# 1.6.0 > [Sep 22, 2024]

## Features
- Finally implemented Background Object tags. This includes two new modifiers for BG objects: setActiveOther and animateObjectOther. Also includes a new regular object modifier: setBGActive.
- Added BG Global Position toggle to Camera Depth event keyframes. This is to support vanilla Legacy levels that rely on the BG camera being at a specific position. If you made any levels before this change, please do turn the toggle off.
- Added Allow Custom Player Models to the Global tab of the Player Editor. Having it off will force the player to use the player model you set in the editor.
- Added a button that sets the global player model config to the currently selected player model. This is so you can a bit more easily play as a custom player model in whatever level you choose (as long as the feature above is on)
- Added Context Menus to specific areas of the editor (such as the level list, prefab list, etc). Right click something to open the menu.
- Sub-folders in the prefabs folder can now be navigated via the External Prefabs popup.

## Changes
- GAME OPTIMIZATIONS!
  - Optimized a ton of level data code.
  - Modifiers are a lot more optimized now (1.7x-ish FPS boost).
  - Timeline Object rendering is also now more optimized. Which basically means switching between layers shouldn't freeze the game as hard as it did before.
  - Creating an event keyframe should no longer freeze the game.
- Reworked the arcade menus to be more consistent with other interfaces in the mod. Still missing a few features but I'll be adding them throughout the next few patches.
- Changed REPL Editor animation to Text Editor animation. (Completely forgot there was animation configs for a REPL Editor... what)
- Player boost can now be queued while boost cooldown is on. Includes a config located Config Manager > Player > General.

## Fixes
- Fixed Background Object shape toggles not being selected.

------------------------------------------------------------------------------------------

# 1.5.8 > [Sep 19, 2024]

## Changes
- Clear object data when returning to the arcade.
- Removed Page Editor since it is no longer needed. (Will eventually add an interface editor if I can figure out how to do one)

## Fixes
- Fixed prefabs not spawning in arcade levels.

------------------------------------------------------------------------------------------

# 1.5.7 > [Sep 18, 2024]

## Fixes
- Ease Type dropdown not updating keyframe hotfix.

------------------------------------------------------------------------------------------

# 1.5.6 > [Sep 18, 2024]

## Changes
- Trim the ends of evaluated text.

------------------------------------------------------------------------------------------

# 1.5.5 > [Sep 18, 2024]

## Features
- Added some config based modifiers (configLDM, usernameEquals, languageEquals, etc)
- Added formatText action modifier. This does what Allow Custom Text Formatting config does.
- Added playerCount trigger modifier. (apparently code for this already existed, I just never fully implemented it...)
- Added a ton of functionality to the math evaluators (copyAxisMath, copyAxisGroup, etc). Considering how much it can do, I might add it to some modifiers later if I have any ideas on what ones to do. (also, you can type math into some input fields if you really must)
- Added a QOL text editor for editing an input field's text at a larger scale.

## Changes
- Updated the way most items are obtained from dictionaries. (This should hopefully mean a slight improvement in performance)

## Fixes
- Fixed death_hd being achieved when you exit the editor. (hopefully)

------------------------------------------------------------------------------------------

# 1.5.4 > [Sep 16, 2024]

## Fixes
- Fixed editor levels not loading.

------------------------------------------------------------------------------------------

# 1.5.3 > [Sep 16, 2024]

## Features
- Added achievements: no_fps.
- Object modifiers now have a Prefab Group Only toggle. This is for when you want an objects' modifier to only check for objects within the prefab that contains the object.

## Changes
- Achievements now stack.

## Fixes
- Fixed achievement "editor_zoom_break" having the wrong description.
- Fixed player model not saving correctly if custom object name is left empty.

------------------------------------------------------------------------------------------

# 1.5.2 > [Sep 15, 2024]

## Features
- Implemented Rotate Reset for player models.
- Added achievements: player_select, example_touch, time_machine and time_traveler.

## Changes
- Reverted player tail position change from last patch. I have no idea how to properly fix the tail problem...
- Custom Player objects parented to the boost now scale with it.

## Fixes
- Fixed Flip X face rotation being weird.
- Fixed Player Editor lagging on first open.

------------------------------------------------------------------------------------------

# 1.5.1 > [Sep 15, 2024]

## Features
- Added death_hd achievement.
- Replaced Switch to Arcade Mode button in File dropdown with Copy Level to Arcade button. This now copies the current levels' folder into the arcade folder, allowing you to load it from there.
- Added a button under the Edit dropdown for re-rendering the timeline waveform.

## Changes
- Uploading a level to the arcade server will no longer include VG files (level.vgd & metadata.vgm).
- Set pasting objects notification to 5 seconds.
- Made Beatmap Objects only save Gradient Color keyframe values if the object is a gradient.

## Fixes
- Fixed game timeline not clearing previous checkpoint images properly.
- Tried fixing player tail positions on player spawn.

------------------------------------------------------------------------------------------

# 1.5.0 > [Sep 14, 2024]

## Featrues
- Implemented internal achievements!
  You'll unlock some of the internal ones as you play the game.
  Custom achievements will come later once I figure out how to do them.
  Do note achievements do not *currently* stack, so if you get multiple at once, you will only see one of them.
  You also can't currently view achievements anywhere. Eventually it'll be added to the profile menu.
  Here's a list of the current achievements (some names are more obvious than others, so you will need to search around for what these are for)
    - welcome
    - editor
    - no_boost
    - complete_animation
    - costume_party
    - f_rank
    - expert_plus_ss_rank
    - master_ss_rank
    - editor_reverse_speed
    - editor_layer_lol
    - editor_layer_funny
    - example_chat
    - editor_zoom_break
    - no_volume
    - queue_ten
    - friendship
    - holy_keyframes
    - serious_dedication
    - true_dedication
    - upload_level
    - youve_been_trolled
    - ten_levels
    - fifty_levels
    - one_hundred_levels
- Ported gradient objects from default branch.
- Added translateShape modifier. This can move, scale and rotate the mesh (shape) of the object. Good for offsetting gradient rendering or modifying the dimensions of a shape without needing to make another parent.
- You can now specify multiple keyframe indexes in the Multi Object editor Assign Color section by separating numbers with ",".

## Changes
- Settings Info time in level now displays days editing.

## Fixes
- Fixed cases where the menu music would continue playing in the editor.
- Editor camera should now only toggle on / off when you're in the editor.

------------------------------------------------------------------------------------------

# 1.4.9 > [Sep 11, 2024]

## Changes
- Removed homing color as it can just be done using modifiers. Additionally, the UI needs room for gradient objects if it were ever implemented.
- Levels should run a tiny bit smoother since opacity, hue, saturation and value are now in one animation sequence instead of multiple.
- Removed the Reactive Color Lerp setting and made it the default.
- Cleaned up BG Object color code.

## Fixes
- Fixed Config Manager UI not applying theme colors when rounded is on.
- Fixed an error with enable / disableObject modifiers not working.

------------------------------------------------------------------------------------------

# 1.4.8 > [Sep 11, 2024]

## Features
- Added a setting for running custom text formatting. This is off by default as having it on can lag levels with a few text objects. Until a more optimized way of getting the custom formatting to work is found, this setting will exist.

## Changes
- Levels with a ton of text objects should now run a LOT smoother!
- Updated uploading info text (now it's a bit more clear on what you should do when you want to upload your level).

------------------------------------------------------------------------------------------

# 1.4.7 > [Sep 10, 2024]

## Fixes
- Fixed a major bug with the level folder name change in 1.4.5.

------------------------------------------------------------------------------------------

# 1.4.6 > [Sep 10, 2024]

## Features
- Added a "Reset" toggle to enable / disableObject modifiers.
- Added applyColorGroup modifier. This applies the objects' own color to a group of objects.

## Changes
- Shorted multi object editors' "Clear animations" to "Clear anims" so the entire label is visible in specific resolutions.

## Fixes
- Fixed an error log spam that is caused by no players being active if the level has a copyAxisGroup modifier.
- Fixed object type not updating sometimes.

------------------------------------------------------------------------------------------

# 1.4.5 > [Sep 9, 2024]

## Changes
- Downloaded level folder now contains level name for clarity.

## Fixes
- Fixed updating a level creating duplicated levels in local arcade.
- Timeline objects no longer spawn on the first frame.
- Fixed mouse tooltip UI bugging out.

------------------------------------------------------------------------------------------

# 1.4.4 > [Sep 8, 2024]

## Features
- Added tag displaying to arcade.

## Changes
- Resized Play & Download Level menus.

## Fixes
- Attempting video BG fix.
- Fixed search button only being interactable with a mouse.

------------------------------------------------------------------------------------------

# 1.4.3 > [Sep 7, 2024]

## Fixes
- Fixed boost recover sound not playing last update whoops

------------------------------------------------------------------------------------------

# 1.4.2 > [Sep 7, 2024]

## Features
- Added Boost Recover only with Boost Tail to Config Manager > Players > General.

## Fixes
- Actually fixed the download level crash. (The crash was due to no audio being loaded)

------------------------------------------------------------------------------------------

# 1.4.1 > [Sep 6, 2024]

## Changes
- Audio volume keyframe now only changes music volume.

## Fixes
- Fixed a crash when going from Download Level menu to Play Level menu and playing the level.

------------------------------------------------------------------------------------------

# 1.4.0 > [Sep 5, 2024]

## Features
- Continuing work on the BetterLegacy story mode.
- Arcade server is finally up and running! Which now means you can now upload and download modded levels.
- Added "activateModifier". This acts like the signalModifier, except it forces a specific or all actions to run, making them ignore triggers.
- Discord status now displays a timer.
- Added a splash text to the main menu.
- Added some settings for mouse tooltips.
- Added song title to New Level Creator popup.

## Changes
- Spaced the multi object editor UI out a bit to fix some text rendering issues and to make it feel less cramped.
- Moved the "Open Config Key" setting to the "Settings" sub tab in the Config Manager UI. You will need to set the value again.

## Fixes
- Fixed copyAxisGroup modifier.
- Fixed an old bug with theme editing where it wouldn't always stop using the theme preview.
- Fixed debugger refresh breaking the editor.
- You can now select the player in preview to open the Player Editor. (you could technically previously, but it was broken)
- Fixed default menu music not playing.
- Fixed metadata links... again.
- Fixed metadata cover not showing.
- Fixed some issues with debug info and added a setting for displaying only FPS.
- Fixed parent desync being on even if the object doesn't have a parent.

------------------------------------------------------------------------------------------

# 1.3.11 > [Aug 30, 2024]

## Features
- Added tooltips for modifiers so what they do is finally explained in-editor.
- Added mouse tooltip and notification display multiplier settings. They can be found under Config > Editor > Editor GUI.
- Added onPlayerDeath modifier.

## Changes
- Mouse tooltips now display longer based on how long the text is. If the text is really short, then it will disappear really quickly, otherwise it will stay longer.
- Decreased the max wait time Example could potentially have to speak from 16:40:00 (wtf) to 0:10:00.
- Tried cleaning up / optimizing modifier code. Please let me know if there's a decrease in performance!

## Fixes
- Fixed death and hit count not using the correct counter in some cases.

------------------------------------------------------------------------------------------

# 1.3.10 > [Aug 29, 2024]

## Fixes
- Scene loading error hot fix.

------------------------------------------------------------------------------------------

# 1.3.9 > [Aug 29, 2024]

## Changes
- Reloading main menu now randomizes the song index.

## Fixes
- Fixed interfaces not resetting their positions when loaded.

------------------------------------------------------------------------------------------

# 1.3.8 > [Aug 28, 2024]

## Changes
- Removed player custom code. (Been meaning to for a while since there's a security concern with such a feature.)
- Decreased Example dance chance to 0.7% per frame.

## Fixes
- Fixed upload and delete buttons being usable while the arcade server isn't up yet.

------------------------------------------------------------------------------------------

# 1.3.7 > [Aug 27, 2024]

## Features
- Implemented Editor Complexity.
  - Render depth slider and origin offset returns to their original forms in the Simple Complexity setting.
  - Any feature that only shows with the now removed "Show Modded Features in Editor" toggle now only show in Advanced complexity.
  - Solid object type only now show in Advanced complexity.

## Changes
- Removed Editor Preferences as the Config Manager serves the same purpose, but better.

## Fixes
- Fixed timeline zoom not zooming in on the correct area. (I have no idea why it wouldn't before but ok)

------------------------------------------------------------------------------------------

# 1.3.6 > [Aug 26, 2024]

## Features
- Multi object color editing now has Add and Sub buttons.

## Fixes
- Fixed multi object editor color slots being incorrect.

------------------------------------------------------------------------------------------

# 1.3.5 > [Aug 26, 2024]

## Features
- Added a "Don't set color" toggle to the multi object editor, for cases where you want to change any other color value other than the color slot.

## Changes
- Cleaned up the mutli object editor.
- Optimized object render type updating.

------------------------------------------------------------------------------------------

# 1.3.4 > [Aug 25, 2024]

## Fixes
- Shake smoothness (interpolation) now works as intended. Shake speed works, but breaks if you try animating it.
- Fixed homing keyframes breaking when children despawn.

------------------------------------------------------------------------------------------

# 1.3.3 > [Aug 25, 2024]

## Features
- Added preferred player count to metadata.

## Changes
- Removed unused scale homing keyframe code.

## Fixes
- Fixed folders appearing at the bottom instead of the top of the level list.
- Fixed homing keyframes sending an error log when there is no player.
- Examples' dialogue should now always disappear even in cases of lag or low FPS.

------------------------------------------------------------------------------------------

# 1.3.2 > [Aug 25, 2024]

## Changes
- Only update homing keyframes once every time the object becomes inactive.
- Optimized pasting into an input field (it should no longer freeze, unless it's a larger text then it probably will)

## Fixes
- Started investigating memory leaks.
- Stop interface from loading if the EditorOnStartup mod is installed.

------------------------------------------------------------------------------------------

# 1.3.1 > [Aug 24, 2024]

## Fixes
- Fixed the metadata.lsb file not saving in some cases.
- Homing keyframe behavior should now be more consistent across all homing types.
 - Fixed dynamic homing rotation keyframe resetting after a regular rotation keyframe starts.
 - Homing keyframes now properly re-target when the object respawns. (Plus there is now a setting in Config Manager > Editor > General for updating homing keyframes when time is changed)
 - Going from dynamic to static homing should now be more consistent, however going the other way around isn't yet.

------------------------------------------------------------------------------------------

# 1.3.0 > [Aug 24, 2024]

## Features
- Added Unlock Required and Is Hub Level toggles to MetaData. This is used for collections that require unlocking or have a hub level respectively.
- New modifiers include:
  - playDefaultSound
  - levelRankOtherEquals (+ comparison variants)
  - levelRankCurrentEquals (+ comparison variants)
  - levelUnlocked
  - levelCompleted
  - levelCompletedOther
  - loadLevelPrevious
  - loadLevelHub
  - endLevel
- Added some new formatting for text objects to use:
  - <levelRankCurrent>
  - <levelRankCurrentName>
  - <levelRankCurrentColor>
  - <levelRankOther=id>
  - <levelRankOtherName=id>
  - <levelRankOtherColor=id>.
- The editor config now has a "Editor Rank" setting located in Config Manager > Editor > Data. This is used for the above text formatting and modifiers, or anything related to level ranks while you're in the editor.
- Editor Config now has an Editor Complexity setting. This will eventually replace the Show Modded Features setting and will be used for people who prefer a simpler or an advanced editor.
- Added a few "Yield Mode" settings. This is used for the time between each iteration in certain functions. Yield Modes' Null, Delay, EndOfFrame takes longer but doesn't freeze the game, FixedUpdate is quicker but has a chance to lag the game and None is often quicker but will freeze the game for the amount of time it takes to run.
- You can now copy and paste every levels' Background Objects into another.
- Level collections now work, though currently there isn't any way to create / edit them. (They're also missing their own selection menu so yeah)

## Changes
- Completely reworked the menus to use a new & improved system that is far more capable compared to the original.
- Updated play level menu to use the new system. The main arcade system will remain the same as it is for the time being.
- Changed Props and Static Prefab Type icons.
- Removed blur bg from pause menu. Didn't work how I wanted it to unfortunately.
- Homing keyframes now properly re-target when the object respawns.

## Fixes
- Fixed Steam levels not having their IDs' remembered in player saves.
- Fixed dynamic homing rotation keyframe resetting after a regular rotation keyframe starts.

------------------------------------------------------------------------------------------

# 1.2.3 > [Aug 13, 2024]

## Changes
- Changing the Prefab Type in the Prefab Object Editor now update the timeline objects associated with the Prefab.
- Made default Prefab Popup sizes larger.

## Fixes
- Fixed Prefab Popup settings not updating when the values are changed.
- Reloading Prefab types should now refresh the UI.

------------------------------------------------------------------------------------------

# 1.2.2 > [Aug 12, 2024]

## Changes
- Finally removed all indirectly reference objects since the mods are now one.

## Fixes
- Fixed timelines in the Project Planner looking off.
- Fixed some fonts present in the customfonts.asset file not appearing in the docs nor having their names converted.

------------------------------------------------------------------------------------------

# 1.2.1 > [Aug 12, 2024]

## Features
- Added a reload button to the Prefab Type editor.

## Changes
- Reworked Prefab Types to be ID (string) based instead of index (ordered number) based.
- Show Prefab Type editor buttons now have text to make it more clear what to do and what Prefab Type it has selected.

------------------------------------------------------------------------------------------

# 1.2.0 > [Aug 11, 2024]

## Features
- Continued work on the arcade server. It's not quite ready for release but hopefully it'll be in the next few updates, so stay tuned!
- Started working on a custom story mode.
- Added data for Background Object tags.
- Added copyAxisGroup modifier. Allows for multiple object groups and custom expressions.
- Markers are now draggable via clicking and holding the scrollwheel over a marker and moving the mouse around.
- Added preview grid.
- Markers can now show in the object editor timeline.
- Added a Restart Editor button to the File dropdown in the editor.
- Added some new Rotate Modes to the Player Editor and fixed rotation being weird with platformer mode.
- Added jump boost count to Player Editor.
- Added Angle value to particleSystem modifier.
- Example now dances occasionally if the music is playing.
- Added Reset buttons to each config setting.
- Added a way to edit a prefabs' name in the External Prefab Editor.

## Changes
- Reworked Example's chat box to be bigger and have an autocomplete.
- Did a ton of internal code cleanup.
- Object keyframe timeline zoom and position is now properly remembered for when you reselect the associated object.
- Reworked editor tooltips to store them better and have more languages.

## Fixes
- Fixed Only Objects on Current Layer Visible setting.
- Finally fixed dropdown values in the Config Manager UI.
- Fixed Prefab & Theme File Watchers being broken.

------------------------------------------------------------------------------------------

# 1.1.6 > [Jul 7, 2024]

## Features
- Added Hide Timeline config in the Events > Game tab.
- Custom player objects can now be deleted and duplicated via the custom object selector window.
- Level folder can now be set directly in the level list.
- Added delay values to followMousePosition modifier.
- Added the rest of the global player editor values.

## Changes
- Renamed "Steam" to "Upload" in the editor.

------------------------------------------------------------------------------------------

# 1.1.5 > [Jul 1, 2024]

## Features
- Added custom scroll values for object keyframe editor.
- Started working on an animation editor. It's going to look similar to the regular object editor, but instead it'll be exclusively used to edit animations of the player and anything else that will need custom animations.
- Added copyAxisMath modifier. This modifier allows for custom expressions, in case the way the regular copyAxis modifier isn't the way you'd like it to be.

------------------------------------------------------------------------------------------

# 1.1.4 > [Jun 26, 2024]

## Features
- Added shape dropdowns to particleSystem modifier because I forgot to do that before.

## Changes
- Reworked Example's dialogue a tiny bit.
- Optimized dragging.

## Fixes
- Fixed themes not being saved to VG converted levels.
- Fixed sprite data not being saved correctly.

------------------------------------------------------------------------------------------

# 1.1.3 > [Jun 16, 2024]

## Features
- Added Save Async config.
- Added Timeline Collapse Length config.

## Changes
- Moved the shapes folder to the BepInEx/plugins/Assets folder. Feel free to delete the shapes folder in the beatmaps folder.

## Fixes
- Fixed thin triangle collision. (Heart outline shapes are still broken)
- Fixed saving being broken.

------------------------------------------------------------------------------------------

# 1.1.2 > [Jun 16, 2024]

## Features
- Continuing to lay groundwork for future features such as level collections, player modifiers, etc.

## Fixes
- Fixed VG level converting not working properly.

------------------------------------------------------------------------------------------

# 1.1.1 > [Jun 13, 2024]

## Features
- Added a config setting for PA filetype output for Level Combiner. You can output either VG or LS level types.

## Fixes
- Fixed an issue with Pulse and Bullet shapes not rendering correctly.

------------------------------------------------------------------------------------------

# 1.1.0 > [Jun 12, 2024]

## Features
- Added data for timeline object indexing. This will eventually make each timeline object render above or below other objects depending on what you set the index to.
- Added player hit cooldown. (This currently does not affect the hit cooldown animation)
- Started working on a custom achievement system.
- Added setCollision and setCollisionOther modifiers. These can enable / disable the collision of an object or multiple.
- Added game modes! This currently includes two modes: Regular and Platformer. Platformer forces the player to have proper gravity and the only way they can navigate is left / right and jumping. The player can jump multiple times before they can boost.
- Player models now have a can boost value.
- Added a config setting for automatically attempting to parse level data in a more optimized way. Objects alive after they've been scaled down to zero by zero will have their autokill times set to song time.

## Changes
- Levels load a lot faster now.
- Reworked the Player Editor.

## Fixes
- Fixed some config values not saving correctly.

------------------------------------------------------------------------------------------

# 1.0.4 > [May 31, 2024]

## Features
- Added searching to config manager.
- Added interface theme to the Menu Config.

## Changes
- Fully replaced [CONFIG] menu in the interface with Config Manager. Download the Beatmaps.zip file from the Github releases and delete the beatmaps/menus folder and replace it with the one in the zip file.

## Fixes
- Fixed some values not being limited in the configs.

------------------------------------------------------------------------------------------

# 1.0.3 > [May 31, 2024]

## Features
- Added FPS related settings to the Core config for better GPU handling.
- Added a config for global copied objects / events for cases where it breaks.

## Fixes
- Fixed profile folder not being created before config creation.

------------------------------------------------------------------------------------------

# 1.0.2 > [May 26, 2024]

## Fixes
- Fixed modifiers inactive state not being run when the modifier is constant.
- Background Objects now copy & paste the Z Position value.
- Fixed Debug Info and Config Managers' scale being broken.

------------------------------------------------------------------------------------------

# 1.0.1 > [May 23, 2024]

## Changes
- Moved default settings to the top of the Core config.

## Fixes
- Fixed some issues with tooltips internally.
- Fixed an uncommon editor break if the PA application lags.

------------------------------------------------------------------------------------------

# 1.0.0 > [May 22, 2024]
Merged all the PA Legacy mods into one new mod called Better Legacy! This merge should make it clear that this version of the game is now a fully different version of PA.

## Features
- Configs now use a custom system with a brand new config menu, which decreases the load times and halving the file size.
- Background Objects now has Z position. They also have hue, saturation and value for both the base color and the fade color. (RTFunctions)
- Added some new fonts (Pixellet, File Deletion, Sans Sans, Monomaniac One and RocknRoll One).
- All keyframes can now have their times locked. (EditorManagement)
- Added the glitch effects Analog Glitch and Digital Glitch. While this was added to EventsCore, I added it to BetterLegacy first. (EventsCore)
- Added setMousePosition modifier. (Moves the mouse to the center of the screen at an offset)
- Added followMousePosition modifier. (Makes the object track the mouse)
- Added textSequence modifier. (Works like the interface text but more advanced and customizable)

## Changes
- Optimized a ton of stuff compared to the unmerged versions of the mods. (I.E. a level with quite a few modifiers in objects goes from 30 FPS to 100+ FPS)
- Modifier updating now occurs before event updating and object updating.
- Reworked a TON of code to be more readable for anyone interested in helping the modding effort.
- Decreased level.lsb file sizes by comparing default values and only saving values when a default value does not equal the current one. (RTFunctions)
- Newly created markers now have their times snapped to BPM if BPM snap is active. (EditorManagement)
- Made the editor.lse file more readable on the user end.
- Example is now more integrated with the mods than before the merge. (ExampleCompanion)

## Fixes
- Fixed Background Objects and players not properly clearing and respawning when loading from one level to another. (RTFunctions)
- Fixed up loadlevel command and added listlevels::path to interface. (PageCreator)