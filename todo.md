# TODO

## Features

### Core
- Level collections.  
  Update: 1.8.0
  Notes:
  - Level collections in the editor are stored in a separate "collections" folder.
  - Levels can be added to a collection via the context menu. The folder can be added directly to the level collection to make the level exclusive, or it can be just added to the list. (Requires the level to have already been uploaded)
  - Level collections are shown in a different content popup.
  - Non-editor levels can be added a more advanced way by filling out a level's info.
  - Preview song. (done)
  - Overall ranking (similar to regular ranking, except instead of hits it's the level ranks ordinal values)
  - Level collections can be uploaded with only level folders or only references to levels, or a mixture of both.
- Custom Achievement system.  
  Update: 1.8.0
- Online multiplayer.  
  Update: 1.10.0
- Seed-based random.  
  Update: 1.8.0
- Player modifiers.  
  Update: 1.8.0
- Level modifiers (supports VG triggers).  
  Update: 1.8.0
- Global Player toggle for if the players should have their movement speed multiplied by the pitch or not.  
  Update: 1.8.0
- 2D option for BG objects (VG parallax support?)
- Checkpoint features. (Toggle for healing, multiplayer positions / random positions, triggerable via modifiers, respawnable)  
  Update: 1.8.0
  Think about allowing players to respawn even if all players haven't died yet.
- Level preview song.
- Custom end level function defaults.
- Add Set Data to image shape for Custom Player objects.
- Controller Preferences metadata. [Keyboard, Controller, Mouse]
- Versus Mode (Players can attack other players via boosting through them, shooting them, etc)  
  Update: 1.10.0
- Player shooting aiming  
  Update: 1.10.0
- Player toolkit inventory system. Includes customizable tools and weapons that can be used with triggers and specific object tags.    
  Update: 1.10.0  
  Toolkit mechanics:  
  - Damage (sends a damage signal to a damage modifier, can have a set damage amount)
  - Shoot (spawns a custom built bullet / missile with custom movement behavior)
  - Spawn (spawns a prefab at the player)
  - Heal (heals the player but consumes the item)
  - Movement (controls the players velocity)
  - Consumable toggle (removes the item after use, heal mechanic always has this on for balance sake or I might go for a cooldown mechanic instead)
  - Aiming behavior (mouse, right stick, rotate with player, etc)
- Game Timeline Editor. (skipping regions, custom images, etc)
- Glow object that acts like bloom.
- Ignore start time object spawn?
- Arcade auto plays song config.
- Prefab Object Modifiers & Tags.  
  Notes:  
  - This feature might require Prefab Objects to still be loaded in the GameData. Could be a config.
  - Allows for changing the objects in a singular Prefab Object. (prefab group only is on by default)
  - Runs before Object Modifiers.
  - Objects can detect if their Prefab Object has a specific tag.
- Active event keyframe value.
  Notes:  
  - Skips the next keyframe if it's value is off. Acts kinda like relative except if the value at a specific index was 0.

### Story
- Chapter 1.  
  Update: 1.7.0
- Chapter 2.  
  Update: 1.9.0

### Example
- Example tutorials. (Either ask Example about something or right click an element to show the context menu with a "Tutorial" button)  
  Update: 1.11.0
- Example customization. (technically doable now, just would like it to be doable via JSON)

### Editor
- Multiplayer editor (everyone has their own perspective of the hosts' editor but have limited functionality compared to the host)  
  Update: 1.14.0
- Interface editor.  
  Update: 1.8.0
- Animation editor (for player animations).  
  Update: 1.8.0
- Prefab preview image. Can set the specific capture size and position.
- Asset sharing on the online server. (Prefabs, themes, player models, etc)  
  Update: 1.9.0
- Collab sharing via server  
  Update: 1.9.0+
- Editor online backups / version control  
  Update: 1.9.0+
- Sprites list popup that show images in the game data and level folder.  
  Update: 1.8.0
- Some way to replace the song in-editor. (Have the current song update if the song that's being replaced is the same level)  
  Update: 1.8.0
- List for pinned editor layers.  
  Update: 1.8.0
- Add difficulty and artist name to the level creator popup.  
  Update: 1.8.0
- Prefab reference name in object editor.  
  Update: 1.8.0
- Level priority sort  
  Update: 1.8.0

### Interfaces
- A bind system for interfaces where an element prefab can be spawned based on a JSON file or file list.
- Implement custom menu list, where it acts like the old menu system with branches.
- ApplyElement function that parses a JSON from the parameters and applies it to an element.
- Add a way to parse a interface function to text, color, etc.

### Modifiers
- Transition time for applyAnimation modifiers.
- prizeObject action modifier  
  Update: 1.9.0  
  Details: rewards the player with a prefab, theme, player model or player toolkit item.
- playerAction trigger (allows for multiple different keybinds and buttons)
- particleSystemColored action modifier  
  Update: 1.8.0
- Ignore opacity toggle for color modifiers.  
  Update: 1.8.0
- setStartTime modifiers.
- loadLevelCollection modifier.  
  Update: 1.8.0  
  Notes:  
  - Loads a specified level collection from the start level, hub level or default.
- downloadLevelCollection modifier.  
  Update: 1.8.0
  Notes:  
  - Same as downloadLevel except for level collections.
- registerFunc modifier.  
  Update: 1.8.0+
  Notes:  
  - Registers all modifiers up until a return modifier to a function.
  - This function can pass specific variables.
  - Best used with level modifiers since they run before anything else.

### Effects
- Camera Jiggle event keyframe (instead of a single thing that doesn't change throughout the entire level)  
  Update: 1.8.0
- Add feedback (KinoFeedback) effect to event keyframes.  
  Update: 1.8.0
- Add a fake Desktop thing like Rhythm Doctor for Window event keyframes. (The Desktop will look like a PA interface)


## Changes

### Core
- Change how modifiers are saved for better consistency.
- Merge all effect managers into single EffectsManager.
- Rework controller shake to be properly toggleable and have a separate variable.
- Summary and note as much as I can (or need to).

### Editor
- Multi language support.
- Rework the level combiner into just a selection system.
- Give Editor Documentation a cover image so people know what a specific document is talking about.  
  Update: 1.8.0
- Undo / redo everything.  
  Update: 1.13.0
- Improve profile menu.  
  Update: 1.8.0
- Improve changelog menu.  
  Update: 1.8.0
- Optimize timeline objects by replacing TextMeshPro with UnityEngine.UI.Text. See if it could be optional?
- New Editor Layer types [Object Only, Prefab Object Only]
- Update the sprite asset.
- Documentation & tooltips.
- Possible multi object editor rework???
- Make Editor Documentation read from a json file.  
  Update: 1.8.0
- Update the file browser UI to include more info / functions.
- Cleanup Player Editor code.
- Overhaul the custom UI settings to be more extensive.
- Rework object dragging to have a lot more control and settings. (take some inspiration from Modern + Blender + EditorManagement)


## Fixes
- Fix window event keyframe not resetting when player pauses or when the user exits preview mode.
- Fix some issues with Player Models and extra tail parts. (verify if this is fixed, I think it should be)


## Ideas
- Sort Levels Menu?
- MetaData settings that can be adjusted in the Play Level (Settings) menu and can be read using modifiers probably.
- Somehow figure out how to convert parallax in alpha to BG objects in BetterLegacy.
- Freeplay sandbox mode (editor)?
  Notes:  
  - No song is playing. Sequence time is entirely based on when the mode started. However, a custom song can play.
  - Prefab Objects can be spawned + despawned.
  - Player Models can be switched between and tested.
  - GUI and keybinds for interacting with this mode (similar to the editor).


## Demos
- Homing objects using animateObjectMath.
- Real-time clock.
- 3D cube