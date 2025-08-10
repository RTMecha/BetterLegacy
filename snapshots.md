# snapshot-2025.8.9 - (pre-1.8.0) [Aug 10, 2025]
## Features
### Editor
- Added a "comment" modifier that allows you to describe what a modifier function does. The comment can be locked / unlocked by right clicking the input field and selecting the context menu button.
- You can now enable / disable the "Show Collapse Prefab Warning" setting via the "Apply / Collapse" buttons' context menu.
- Modifier cards can now display a custom name and have a description display in the info box.

## Changes
### Core
- Optimized parent chain updating by a lot.

# snapshot-2025.8.8 - (pre-1.8.0) [Aug 9, 2025]
## Features
### Editor
- Object keyframe UI can now be customized via right clicking the X / Y / Z values and changing the UI to a Input Field, Dropdown or Toggle. You can also customize how each of these display.

## Changes
### Editor
- Removed "Adjust Position Inputs" setting. This isn't needed anymore since position Z has been a part of the mod for a long while.

## Fixes
- Fixed sequences with only one keyframe breaking.
- Fixed level collections throwing an error if no "collections" folder exists.
- Fixed setParent modifier re-activating objects when it shouldn't.
- Fixed song link not working.

------------------------------------------------------------------------------------------

# snapshot-2025.8.7 - (pre-1.8.0) [Aug 8, 2025]
## Features
### Core
- Added resetLoop modifier. This allows already activated non-constant modifiers to run again. The current loop continues running.
- Added "Preferred Control Type" setting to level metadata. This will be used to notify users of levels with specific mechanics.

### Example Companion
- Added a setting for delete object notice chance.

## Changes
### Core
- Tried optimizing the keyframe sequences a little.

## Fixes
- Fixed config preset breaking render depth.
- Fixed Normal random type in seed based random being in a straight line.

------------------------------------------------------------------------------------------

# snapshot-2025.8.6 - (pre-1.8.0) [Aug 6, 2025]
## Features
### Core
- Added await and awaitCounter trigger modifiers. This waits for a set amount of time when active and then triggers.
- Added playerEnable modifers. This shows / hides specific players.
- Added playerEnableDamage modifies. Modifies players damageable state.

## Changes
### Editor
- Base Player Model control values can now show in default models if Edit Controls is turned on.

## Fixes
- Fixed prefab parenting desync not working in cases where the both the object and its parent are from a prefab.
- Fixed level ending on restart.

------------------------------------------------------------------------------------------

# snapshot-2025.8.5 - (pre-1.8.0) [Aug 5, 2025]
## Features
### Editor
- The Player editor now has a way to edit Player Control of a Player by turning "Edit Controls" on in the editor.

## Changes
### Interfaces
- Improved the changelog menu so it doesn't overflow anymore. You can now scroll up & down the notes.

------------------------------------------------------------------------------------------

# snapshot-2025.8.4 - (pre-1.8.0) [Aug 4, 2025]
## Features
### Core
- Implemented more level settings.
- Added whiteHole and more blackHole modifiers. The blackHole modifier now only targets the nearest player and no longer has the "Use Opacity" setting, as there are better ways of doing it now.
- Added getVisualOpacity modifier.
- Added playerLockX and playerLockY modifiers. These prevent the player from moving in the specified axis.

### Interfaces
- Improved the Profile menu to include pages and a list of achievements.

### Editor
- Added "Modifiers Display Achievements" in Config Manager > Editor > Modifiers. This allows achievements to display in the editor when they're unlocked via a modifier.

## Fixes
- Fixed "Show Default Themes" setting not working.
- Fixed Hidden & Shared achievement toggles not actually disabling those values when turned off.
- Fixed prefab parenting desync not working.

------------------------------------------------------------------------------------------

# snapshot-2025.8.3 - (pre-1.8.0) [Aug 2, 2025]
## Features
### Editor
- You can now copy & paste modifier blocks.

## Changes
### Editor
- The editor context menu should no longer overflow to the right of the application.

## Fixes
- Fixed modifier blocks not showing all modifiers.

------------------------------------------------------------------------------------------

# snapshot-2025.8.2 - (pre-1.8.0) [Aug 2, 2025]
## Features
### Core
- Added a Global toggle to the getAchievementUnlocked and achievementUnlocked modifiers.
- Added Sprint & Sneak speeds to the Player.
- Level collections now display difficulty, tags and average rank.
- Added loadLevelCollection modifier.
- Implemented level modifiers. Level modifiers can be converted to triggers in the VG format, but will only save the first trigger and action of a set of modifiers.
- Implemented modifier blocks. These can be called using the callModifierBlock modifier. Good for compacting and reusing modifier code, but not recommended for prefab models.

### Example Companion
- Added some dialogues to Example.

## Fixes
- Fixed the downloadLevel modifier causing a softlock hopefully.
- Fixed editor breaking if you were never logged in.
- Fixed some level info settings not working in level collections.

------------------------------------------------------------------------------------------

# snapshot-2025.8.1 - (pre-1.8.0) [Aug 1, 2025]

## Features
### Editor
- Added a way to view your user ID and copy it to the Metadata editor.
- Collaborators can now be added to a level. This means other users can post to the same level. This hasn't been implemented server-side yet so please wait until further notice.
- Fully implemented level collection editing. You can view them in the Level Collections popup under the File dropdown.
  - Levels can be added to the collection either via the level collection itself or via the level panel context menu.
  - "Add File to Collection" context menu button copies the level folder to the collection and adds the reference to the collection. This is only if you want the level to be exclusive to the level collection.
  - "Add Ref to Collection" context menu button only adds the reference. It's recommended the level is on the Arcade server or the workshop before using this button.

## Changes
### Editor
- Overhauled the Metadata editor UI.
- Recently opened popups now appear above previously opened popups.

## Fixes
- Fixed Upload Visibility dropdown having some issues.

------------------------------------------------------------------------------------------

# snapshot-2025.7.7 - (pre-1.8.0) [Jul 29, 2025]

## Features
### Core
- Added rotation speed and curve type to player editor.
- Added "Remove After Despawn" value to spawnPrefab modifiers.

## Changes
### Editor
- Refractored some editor element code.

## Fixed
- Fixed player spawning duplicate players and not destroying the objects properly.

------------------------------------------------------------------------------------------

# snapshot-2025.7.6 - (pre-1.8.0) [Jul 29, 2025]

## Features
### Core
- Started implementing Player Modifiers. Keep in mind, this is only a small part of the full thing and is subject to change.
- Implemented seed based random. Can be turned off by going to Config Manager > Core > Level > Use Seed Based Random and turning the setting off if anything breaks or you prefer the original randomization method.
- Added getCurrentLevelID and getCurrentLevelRank modifiers.
- Implemented the new Checkpoint features to the Checkpoint Editor.
- Added Lives count and Respawn Immediately toggle to the Player Editor.
- Added getPlayerLives, getLives and getMaxLives modifiers.
- Added isFocused trigger modifier.
- Implemented custom achievements. These can be edited via the Edit dropdown in the editor and via the View Achievements button in the Arcade's play level menu.

### Editor
- If you want to refresh an objects' randomization, you can go to the Object Editor, right click the ID and click "Shuffle ID".
- "Pull Level" now exists as a server button in the MetaData Editor.

## Changes
### Core
- spawnPrefab modifiers now use the Prefabs' default instance data.

## Fixes
- Fixed some issues with the Player face parent.
- Fixed polygon shape in Player Editor.

------------------------------------------------------------------------------------------

# snapshot-2025.7.5 - (pre-1.8.0) [Jul 26, 2025]

## Changes
### Editor
- The "Create Objects Modifier Order Default" setting now applies to imported Prefab Objects and new Background Objects.

## Fixes
- Fixed use visual values in axis modifiers not allowing rotation axis other than the first.
- Prefab Name doesn't wrap now.
- Optimized modifiers and prefabs (FPS should be doubled for enableObjectGroup modifiers).
- Fixed entering editor preview taking the song time to the last seeked time.

------------------------------------------------------------------------------------------

# snapshot-2025.7.4 - (pre-1.8.0) [Jul 25, 2025]

## Features
### Core
- Added storeLocalVariables modifiers. This stores the current local modifier variables to this modifier and passes it to other modifiers in future tick updates.
- Added playerDrag modifiers. This drags the player with the object but allows it to move. Currently the "Use Rotation" value does not work.
- Added eventEquals modifiers. These compare an event keyframes' value at a specified time or the current time if the "Time" value is left empty.
- Added onLevelStart and onLevelRewind modifiers.

### Editor
- You can now add a default tag to the levels' metadata by right clicking the "Add Tag" button and selecting "Add a Default Tag".

## Changes
### Core
- containsTag modifier now checks for prefab object tags if the object was spawned from a prefab.

### Editor
- Reorganized the default modifiers list and added icons that display the type of the modifier. 

## Fixes
- Fixed some issues related to recursive prefabs. Objects spawned from the prefab should now reference the prefabs' runtime instead of the main runtime.
- Optimized hidden prefab objects in editor.
- Fixed the Fade Colors list in the Background Object editor.
- Fixed spawnPrefabs modifiers being broken.
- Fixed objects stored in a Prefab Object rendering in the editor timeline on level reload.

------------------------------------------------------------------------------------------

# snapshot-2025.7.3 - (pre-1.8.0) [Jul 24, 2025]

## Features
### Core
- Prefabs should now support recursive prefabs. This means prefabs can be contained in other prefabs.

### Editor
- Default Prefab Object instance data can now be saved to and loaded from Prefabs. If you have copied instance data, that will be priotized over the saved instance data.

## Fixes
- Variables passed from Prefab Object modifiers now should work as intended.
- Fixed the labels in the global Prefab section of the Prefab Object editor not displaying.

------------------------------------------------------------------------------------------

# snapshot-2025.7.2 - (pre-1.8.0) [Jul 24, 2025]

## Features
### Core
- Added "Allow Player Model Controls" to the Player settings. This allows player models to retain their control values, while making regular levels fair again.
- Implemented Modifiers for Prefab Objects!
- Added spawnPrefabCopy modifiers. These search for an existing Prefab Object and if one is found, copies its data.
- Added setPrefabTime modifier. This overrides the Prefab Objects' runtime time and sets a custom time.
- Added playerCollideIndex modifier. Acts like the regular playerCollide modifier except for a specific player.
- Tail parts can now be added / removed in player models.

### Editor
- Copied Prefab Instance data now saves to and loads from the editor.lse file.

## Changes
### Core
- Overhauled modifier system to be a lot more unified than before.
- Player Model & Player Editor code has been heavily cleaned up.
- Beware, some stuff might be broken and definitely will not be compatible with 1.7.x and below.
- Reworked Prefab Objects to have their own runtime system. Using prefabs should now be a whole lot more optimized.
- Modifier JSON format has been tweaked. 1.7.x should still be compatible with this change as it was accounted for a while ago.
- Some group modifiers should now be compatible with more object types.
- The "Object Group" value in modifiers can be left empty to specify the object the modifier is stored in.

------------------------------------------------------------------------------------------

# snapshot-2025.7.1 - (pre-1.8.0) [Jul 20, 2025]

## Features
### Core
- Added "Pause Level" and "Pass Variables" values to the loadInterface modifier.
- Added exitInterface modifier. Used for cases where you want to exit the currently open interface under specific circumstances. (Does not work on the pause menu / end level menu. It's just for the loadInterface modifier)
- Added setRenderType setRenderTypeOther modifiers. These can Set the render type of the object. Don't know why these weren't in there earlier.
- The Audio event keyframe and sound modifiers now have a "Pan Stereo" value. This allows control of the left / right direction the sound is coming from, emulating spaces.
- Actually implemented playerVelocity modifiers.

### Interfaces
- Interface now has a dynamic variable system that allows for specific variables to affect anything in the interface.
- Implemented interface list. This allows for easier interface branching and interface loading.

### Editor
- You can now replace the current loaded levels' song by dragging a song file onto the PA window.
- Fully implemented new "Pinned Editor Layer" system. This allows you to fully organize your editor layers, including layer names, descriptions and custom colors!

## Changes
### Story
- The end of chapter 1 now has a confirmation interface, giving you a chance to do anything else before you're locked out of progressing the chapter.

### Core
- Player stop boost function no longer depends on the moveable state of the player.
- Tweaked the audioSource modifier a little. It should be better to use, hopefully.
- Preferred Player Count value now blocks the user from entering the level if the player count does not match.
- Reworked some MetaData values.

### Interfaces
- Due to the interface list feature, some interfaces have been combined into one file and there's also some new tests to demonstrate the capabilities of the interface now.
- Updated the splash text.

### Editor
- Renamed "Show Intro" to "Hide Intro". This is because in vanilla "Show Intro" was saved as off, despite the intro showing by default, so had to rename it to something.

------------------------------------------------------------------------------------------

# 1.7.0 [Jun 15, 2025]

## Features
### Story
- Story is in a fully playable and enjoyable (possibly? hopefully?) state.
- You can now view cutscenes of levels you've beaten.
- Challenge mode and game settings can be used after you SS rank a level in the story mode.
- Added some attacks to some levels.

### Core
- Color hex modifiers now allow for overriding opacity if the hex code is 8 digits in length. (the last two digits representing opacity)
- Fully implemented Polygon Shape. Thanks Pidge! :D
- With the Polygon Shape fully implemented, new values were added to it: Radius, Thickness Offset and Thickness Scale. Radius can be right clicked to switch between built-in offsets.
- Added setPolygonShape and setPolygonShapeOther modifiers. These modify the values of the current polygon shape.
- Polygon Shape ported to Custom Player Objects.
- Added Store / Clear Data to Player Image Objects. This now means images from Image Objects can be transfered between levels.
- setImage and setImageOther modifiers now accept stored images.
- Implemented Prefab Object random transform.
- Added isEditing trigger modifier. This checks if you're only in the editor. If you're in preview mode or in the regular game, this won't activate.
- Added copyEventOffset math evaluator function.
- Added getModifiedColor and getVisualColor modifiers.
- animateObject modifiers now have a "Apply Delta Time" value. With this on and the modifier set to constant, the animation will calculate the distance between the previous frame and the current frame. This is for cases where the game has low FPS or lags, but can cause the object to jitter in some cases.
- getToggle modifier now has a "Invert Value" toggle. This acts like a not gate.
- Challenge Mode and Game Speed now exists as Config Manager settings. These only work in the Arcade (aka not Story & Editor) and only update when a level begins. They also have extended functionality, so a custom game speed / challenge mode can be registered.
- Custom end level sayings can be added per-level by adding the sayings.json file to the metadata JSON object and modifying it.

### Editor
- Added a BG Object counter to Prefab Object Editor.
- Editor Layer toggles now re-enable when Editor Complexity is set to "Simple".
- Added Indexer to Prefab Object Editor.
- Added Background and Dialogue to the default Prefab Types.
- Text & Image object selection in preview area can be customized via the new "Select Text Objects" and "Select Image Objects" settings found under Config Manager > Editor > Preview.
- Due to the above feature, text objects can now be highlighted.
- Moved Editor Level code to its own manager.
- Added Apply Game Settings In Preview Mode to Config Manager > Editor > General.
- Improved Marker looping usability by adding start & end flags to the Timeline Marker and added "Clear Marker Loop" to the Marker Context Menu.
- Added Snap to BPM context menu for Prefab Creator offset.
- You can now double click a timeline object / timeline keyframe to go the time of the object / keyframe.
- Added Object Dragger Helper setting to Config manager > Editor > Preview. This displays the location of the current object (includes empty and excludes origin offset). Can be dragged and right clicked for a context menu.

### Internal
- Created a custom enum system. (Only visual difference is the Resolution dropdown now displays the resolution name without the 'p')

## Changes
### Core
- Optimized setImageOther modifier by loading the sprite before iterating through the group.
- Reworked dynamic homing speed again to actually act as a delay rather than a speed value.
- Continuing work on the parallax port. It'll be a while until it's fully ready.

### Editor
- Reorganized / reworked a ton of Editor UI.
- Renamed Dynamic Homing's "Speed" value to "Delay" due to the behavior of this changing.
- Image objects' selection area now changes to fit the image size.
- Removed "Editor Zen Mode" setting, since all game modes are accessible in the editor now.
- Made object keyframe BPM snap dragging consistent with other dragging.

### Example Companion
- Example's commands make him say something now.
- Mirror and flip commands now flip BG and Prefab objects.

## Fixes
- Fixed most parent settings in Prefab Objects not being read from the .lsb files.
- Fixed Example's mirror and flip commands being the same.
- Fixed animateObject not running.
- Fixed modifier editors not disabling when Editor Complexity is changed.
- Fixed theme keyframe not transitioning hsv values correctly.
- Fixed Hidden objects unhiding automatically when their regular active state is changed.
- Fixed Prefab references not being removed when a Prefab is deleted.
- Fixed some issues relating to VG to LS conversion.
- Fixed controlPress modifiers not working in some cases.
- Fixed Prefab Type popup appearing in the wrong area.
- Fixed randomization breaking when updating the object.
- Fixed the keyframe random toggles not UI updating properly.
- Fixed Background Objects from Prefab Objects getting saved to level.lsb and displaying in the BG editor list.
- Fixed Search Prefab value in spawnPrefab modifiers having the incorrect dropdown order.
- Fixed all prefab references being removed when a prefab is deleted, even if the prefab reference was not the same as the deleted prefab.

------------------------------------------------------------------------------------------

# snapshot-2025.5.12 - (pre-1.7.0) [May 12, 2025]

## Features
### Core
- Added some values to the audioSource modifier to give it more control.

## Fixes
- Relative keyframes should now work with homing keyframes.
- Fixed homing keyframes not working with parents.
- Fixed objects in prefabs parented to the camera not updating properly.

------------------------------------------------------------------------------------------

# snapshot-2025.5.11 - (pre-1.7.0) [May 11, 2025]

## Features
### Core
- Added opacity and hsv values to the getColorSlotHexCode modifier.
- Added animateColorKF and animateColorKFHex modifiers. These allow you to animate both Beatmap Object and Background Object colors.
- Added setShape modifier. This does exactly what the name suggests.
- Added trailRendererHex modifier. Acts like the normal trailRenderer modifier, accept allows for hex codes for the colors.
- Ported most compatible modifiers to BG objects.

### Editor
- Selecting specific objects in the editor preview window can now be disabled, via either the Multi Object Editor or the Timeline Object context menu.
- You can now copy & paste multiple modifiers.

## Changes
### Core
- Hopefully optimized enableObject modifiers by preventing animation interpolation when it's inactive.
- Overhauled how homing keyframes work internally. They should now act a lot more consistent with other keyframes. This also means homing keyframes are no longer considered experimental.
- Dynamic homing keyframes speed value now allows for above 1.

### Editor
- Hidden objects now save to objects' editor data.
- All modifier editors now act the exact same, including having a "Int Variable" displayer.
- Improved some modifier editor UI.

## Fixes
- Fixed BG objects in Prefabs not working in the Arcade.
- FINALLY fixed the editor re-entry crash. This was due to the code trying to remove a rigidbody from an image object that is trying to load. (For some reason the image object in the editor had a rigidbody, and no this was not me)

------------------------------------------------------------------------------------------

# snapshot-2025.5.10 - (pre-1.7.0) [May 8, 2025]

## Features
### Core
- Started working on player boost damage mechanic.
- Added forLoop and continue modifiers. These allow you to run the next set of modifiers up until a return modifier a certain amount of times.
- Added getParsedString modifier. This returns a string with specific values replaced.
- Added getSplitStringAt and getSplitStringCount modifiers. These return a value in a separated string.
- Added createCheckpoint and resetCheckpoint modifiers. These modify the active checkpoint.
- Added playerCount to math evaluation variables.
- Added activeCheckpointTime to math evaluation variables.
- Added X and Y values to playerBoost modifiers. This forces the player to boost in a specific direction. The values can be left empty to not force that value onto the boost.

### Example Companion
- Added Config Manager > Companion > Behavior > Can Leave setting. This controls if Example should randomly leave if he's bored or not.

## Changes
### Core
- showMouse modifier now has an Enabled toggle.
- Removed some outdated modifiers. They still work, but will not appear in the modifiers list.
- Renamed gameMode modifier to setGameMode.
- Changed playerMove Y value from the 5th value to the 2nd value.
- Reworked level modifier system internally to be more consistent with other modifiers. Due to this change, only the first Trigger and the first Action of a Trigger & Action set of modifiers will be converted to VG. (Level modifiers haven't been implemented yet)

### Editor
- Renamed Image Objects' Set Data to Store Data.
- Reworked default modifiers and tooltips to allow for modifier / tooltip categories.

## Fixes
- Fixed object type collision not updating correctly.
- Fixed Save As not copying sub-directories in the level folder.

------------------------------------------------------------------------------------------

# snapshot-2025.5.9 - (pre-1.7.0) [May 7, 2025]

## Features
### Core
- Added getCollidingPlayers action modifier. This registers a variable for all players, with each variable value being if the player is colliding with the object or not.
- Added localVariableExists trigger modifier. This checks if a modifier exists in the current variables.

## Fixes
- Fixed signalLocalVariables overriding existing variables. This now allows multiple objects to send their variables to one object.

------------------------------------------------------------------------------------------

# snapshot-2025.5.8 - (pre-1.7.0) [May 7, 2025]

## Features
### Core
- Added getPlayerHealth, getPlayerPosX, getPlayerPosY and getPlayerRot modifiers. These return the associated value of a player at an index.
- Added getFormatVariable modifier. This formats multiple text values into a singular string variable.
- Added getComparison and getComparisonMath modifiers. This compares two text / evaluated math values and returns a True or False based on that.
- Added getSignaledVariables and signalLocalVariables modifiers. These send / recieve the current local variables.
- Added getMixedColors modifier. This mixes the colors of multiple hex codes and returns it as a variable.
- Added enableObjectGroup modifier. This allows you to select the active state of multiple groups.
- Added return modifier. This prevents the rest of the modifier loop from running if it runs.
- Added musicLength to math evaluation variables.

## Changes
### Editor
- Order Matters Modifiers toggle is now on by default for new objects.

### Example Companion
- Tweaked Example's boredom to decrease how often he leaves.

## Fixes
- Clicking on a dropdown now requires left click. This is to fix context menus on dropdowns still interacting with the dropdown.
- Modifier run count should now act as intentioned by increasing the run count number by one only when the modifier activates, not when it continues running.

------------------------------------------------------------------------------------------

# snapshot-2025.5.7 - (pre-1.7.0) [May 6, 2025]

## Features
### Core
- Added setColorRGBA modifiers. This opperates similarly to setColorHex, except it has individual color channels.
- Added mouseScrollX and mouseScrollY to math evaluator.
- A few more get variable modifiers have been added.
- formatText modifier now passes variables to the text formatting. You can place <var=VARIABLE_NAME> into a text object that has this modifier, with VARIABLE_NAME being one of the variables you registered. formatText has to be at the end of other modifiers.
- Added break trigger modifier. This modifier is always active, which creates a break in action trigger checking.
- Added localVariableEquals trigger modifiers. These modifiers compare local modifier variables.

### Editor
- setPlayerModel modifier's Model ID value now has a context menu for selecting a player model.

### Example Companion
- Example now has a low chance to leave by himself if he's bored.

## Changes
### Core
- A ton of modifiers have been tweaked to fit with the new get variable modifiers.

## Fixes
- Fixes setPlayerModel modifier not actually updating the player model.
- Fixed modifier scroll view resetting when refreshing modifier list.

------------------------------------------------------------------------------------------

# snapshot-2025.5.6 - (pre-1.7.0) > [May 5, 2025]

## Features
### Core
- Added "get variable" action modifiers. These modifiers gets a specific variable and store it for other modifiers to use.
- Added a "group alive" value to modifiers. Haven't fully implemented it yet, but it's supposed to check only for alive objects in a group.

### Editor
- Added Hide Selection keybinds. These allow you to hide Beatmap Objects, Prefab Objects and Background Objects in the editor.
- Added context menus to edit the raw data of most modifier values.

## Fixes
- Fixed game throwing an error if the checkpoint's name was null. (Meaning you couldn't play the secret...)

------------------------------------------------------------------------------------------

# snapshot-2025.5.5 - (pre-1.7.0) > [May 4, 2025]

## Features
### Core
- Added "Run Count" to modifiers. This acts similarly to constant, except the modifier can only run a set amount of times before stopping.

### Editor
- Added Config Manager > Editor > Data > Overwrite Imported Images setting. With it on, imported images overwrite existing image files.

## Changes
### Core
- Modifiers now run before everything else per-tick, but in some situations they run after.
- Trigger modifiers with constant on should now behave as expected. (Only triggers once)

## Fixes
- Fixed theme updating not updating the stored theme.
- Fixed file drag and drop not checking for singular images.
- Fixed Multi BG indexing not working in some cases.
- Fixed BG modifiers from previous versions not parsing correctly.
- Fixed SetSongTimeAutokill not properly updating objects.
- Fixed switching gradients not updating the color sequence.

------------------------------------------------------------------------------------------

# snapshot-2025.5.4 - (pre-1.7.0) > [May 2, 2025]
Hotfix snapshot

## Features
### Editor
- Actually implemented mutli selection level combining.

## Fixes
- Fixed objects not being removed from the level data when being deleted.

------------------------------------------------------------------------------------------

# snapshot-2025.5.3 - (pre-1.7.0) > [May 2, 2025]

## Features
### Editor
- You can now click the scroll wheel on the Object Keyframe timeline to drag it around, just like with the Main timeline.
- Multiple levels can be selected by holding down the Shift key. This will be used for creating collections in 1.8.0 and combining levels.

## Changes
### Core
- BG Object Modifiers no longer use the "Block" / "Page" system. This is to be consistent with other objects with modifiers.

### Editor
- Fixed the both timeline's position being set a frame later.

### Fixes
- Fixed pasting prefabs with only BG objects not pasting.

------------------------------------------------------------------------------------------

# snapshot-2025.5.2 - (pre-1.7.0) > [May 1, 2025]

## Changes
### Editor
- Removed the "Can't delete only object" warning. This means you can technically have an entirely empty level now.

### Fixes
- Fixed Background Object prefabbing start time offset not working in some cases.

------------------------------------------------------------------------------------------

# snapshot-2025.5.1 - (pre-1.7.0) > [May 1, 2025]
Snapshots are still a thing.

## Features
### Core
- Background Objects can now be prefabbed.

### Editor
- Background Objects are viewable as timeline objects now.
- To go with this feature, BG objects now have editor settings (layer, bin, index).

## Changes
### Core
- The Game Timeline no longer overlaps when opacity is less than 1.
- Refractored more runtime stuff (such as events) to RTLevel. As a consequence, event offsets are now reset every time the runtime level is reinitialized.

### Editor
- Renamed "Gamma X" "Gamma Y" "Gamma Z" "Gamma W" to "Red" "Green" "Blue" "Global"
- Reworked timeline object deleting.

## Fixes
- Fixed Event Editor indexer not displaying "E" for end keyframes.
- Finally fixed BG color delay.
- Fixed some issues relating to the runtime modifier changes last prerelease.
- Fixed object duplicating moving it to where the audio time is if the objects' start time is 0.

------------------------------------------------------------------------------------------

# 1.7.0-pre.2 > [Apr 29, 2025]

## Features
### Story
- More cutscene progress.

### Core
- Parallax Objects from VG can now be converted to and from BG Objects. It's not 100% accurate yet, but at least it's something.
- Added detachParent modifiers. This makes an object act as if they have "parent desync" on, except it desyncs from where the current song time is.

### Example Companion
- Started working on tutorials. These won't be implemented for a while, but at least the groundwork is there.

### Editor
- You can now drag and drop files into the game, both in the editor and the arcade.
  - Dragging a level into the arcade / editor will load it.
  - Dragging a txt file into a loaded editor level will create a text object.
  - Dragging an image into a loaded editor level will create an image object.
  - Dragging a prefab into a loaded editor level will import it. If the mouse is over the timeline, it will place it.
  - Dragging an audio file while the New Level Creator popup is open will set the audio path for the new level.
  - Dragging an audio file into a loaded editor level will create an object with a playSound modifier.
  - Dragging a MP4 file into a loaded editor level will set it as the video BG.
  - Dragging a folder containing images or several images will create an image sequence. The FPS of the sequence can be configured in Config Manager > Editor > Data > Image Sequence FPS.
- Internally, a custom "base path" can be set in the editor. This means you can have Project Arrhythmia open on one harddrive, while another harddrive has the beatmaps folder. There is no UI to set this yet.
- Added Prefab Object inspect buttons if you have Unity Explorer installed.
- Added an Index Editor to the Object Editor. This means you can now view and edit the index of an object. The index controls what it appears in front of in the timeline. It also includes a context menu for selecting the previous / next objects in index order.
- Added context menus to Image Objects.
- Added "Pull Changes" to Upload Level context menu.
- Added Image Object editing to Multi Object Editor.

## Changes
### Core
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
- Player shape type has been removed for the time being. Wasn't happy with how it worked. Might revisit it at some point when I feel I can do it.
- Shapes are now loaded from a shapes.json file in the Assets folder.
- Updating parent chains, object type and shapes have been optimized.
- Refractored a lot of Catalyst code to fit BetterLegacy's code style.
- Due to the above changes, modifiers system has also been heavily optimized.

### Example Companion
- Example now has eyelids for more expressions.

### Interfaces
- Increased changelog menu interpolate speed.

### Editor
- Timeline grid now fades depending on timeline zoom. This was in last prerelease, just forgot to include it in the changelog.
- SetSongTimeAutokill keybind function now includes Prefab Object autokill.
- Replaced "Timeline Object Retains Bin On Drag" setting with "Bin Clamp Behavior" in Config Manager > Editor > Timeline. This changes how timeline object bin dragging is handled when the bin is dragged outside the normal bin range.
- Overhauled a lot of Background Editor code.
- There no longer needs to always be 1 Background Object in a level. Due to this, "Delete All Backgrounds" now actually does what it says. If there are no Backgrounds present, the editor UI will disable.
- Custom Background Object reactive values UI now disable if the reactive type is not custom.
- Removed "Yield Type settings" for Expanding Prefabs and Pasting Objects. This isn't really needed anymore.
- Cleaned up Copy, Paste and Delete functions.
- Finally made the Settings button on the title bar consistent with other dropdown menus.
- Shuffled some dropdown menu buttons around.
- Changed MetaData Editor link buttons icons to a link icon to better represent what they are.
- Default Image Object image selection path changed to "images" folder inside the level folder.

## Fixes
- Fixed animateObject modifiers and shot bullets being inconsistent with different framerates.
- Fixed default keybinds for First and Last Keyframe selectors being incorrect.
- Fixed non-shape objects not updating their render type when the dropdown value is changed.
- Fixed Multi Object Keyframe Editor "Apply Curves" button re-rendering keyframes from previously selected objects.
- Fixed setParent modifiers crashing the game because I got the parent and the child confused.
- Fixed Prefab Object autokill updating being broken.
- Fixed Hue values not loading correctly for Background Objects.
- Fixed Parent Desync being broken. Due to this, it's no longer considered experimental and now displays by default with Editor Complexity set to Advanced.
- Fixed Players not correctly validating in arcade / story levels.

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