# TODO

## Features

### Core
- Level collections.  
  Update: 1.8.0
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
- Polygon Shape port. (almost done, still just waiting on the code...)

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
- Customizable object templates. (reads from a .json file)  
  Update: 1.7.0 - 1.8.0
- Prefab reference name in object editor.  
  Update: 1.7.0 - 1.8.0

### Interfaces
- A bind system for interfaces where an element prefab can be spawned based on a JSON file or file list.
- Implement custom menu list, where it acts like the old menu system with branches.
- ApplyElement function that parses a JSON from the parameters and applies it to an element.
- Add a way to parse a interface function to text, color, etc.

### Modifiers
- Variable Modifier that registers an object variable based on what the modifier takes.  
  Update: 1.8.0
- Transition time for applyAnimation modifiers.
- prizeObject action modifier  
  Update: 1.9.0  
  Details: rewards the player with a prefab, theme, player model or player toolkit item.
- playerAction trigger (allows for multiple different keybinds and buttons)
- trailRendererGradient action modifier  
  Update: 1.8.0
- trailRendererColored action modifier  
  Update: 1.8.0
- particleSystemColored action modifier  
  Update: 1.8.0
- color math modifiers.  
  Update: 1.7.0 - 1.8.0
- Ignore opacity toggle for color modifiers.  
  Update: 1.7.0 - 1.8.0

### Effects
- Camera Jiggle event keyframe (instead of a single thing that doesn't change throughout the entire level)  
  Update: 1.8.0
- Add feedback (KinoFeedback) effect to event keyframes.  
  Update: 1.8.0
- Add a fake Desktop thing like Rhythm Doctor for Window event keyframes. (The Desktop will look like a PA interface)


## Changes

### Core
- Change how modifiers are saved for better consistency.
- Rework object updating system to be more consistent and have extended updating functionality.
- Make Background Objects based on timeline objects instead of a list. This means they have start time, autokill and keyframe functionality much like Beatmap Objects.
- Summary and note as much as I can (or need to).

### Editor
- Color picker rework.  
  Update: 1.8.0
- Multi language support.
- Rework the level combiner into just a selection system.
- Rework controller shake to be properly toggleable and have a separate variable.
- Give Editor Documentation a cover image so people know what a specific document is talking about.  
  Update: 1.8.0
- Undo / redo everything.  
  Update: 1.13.0
- Somehow figure out mutli BG object editing. (maybe consider reworking the BG object selection system to be timeline based?)
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


## Fixes
- Fix window event keyframe not resetting when player pauses or when the user exits preview mode.
- Fix homing keyframe behaviour. (Going from homing to a normal relative keyframe should prevent the keyframe from moving)
- Fix homing keyframes not retargetting in some cases.
- Fix parent desync not working for specific parent chains.
- Fix some issues with Player Models and extra tail parts. (verify if this is fixed, I think it should be)
- Fix editor crash on re-entry.


## Ideas
- Sort Levels Menu?
- MetaData settings that can be adjusted in the Play Level (Settings) menu and can be read using modifiers probably.
- Somehow figure out how to convert parallax in alpha to BG objects in BetterLegacy.
- Tags for Prefab Objects so you can change a prefab via modifiers.


## Demos
- Homing objects using animateObjectMath.
- Real-time clock.
- 3D cube