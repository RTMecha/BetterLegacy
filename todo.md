# TODO
Current: Misc
Next: Misc

## Features
### Core
- Online multiplayer.  
  Update: 1.9.0  
  Notes:  
  - Camera follows the clients players, online players from other clients are not taken into account and can go in other directions.
  - Specific effects can be set to only show for specific clients (if one client was in another area and had a bloom effect applied, the other clients shouldn't have the effect)
  - Objects can be set to render differently per client.
  - Includes "isOnline" ("Client" bool) trigger modifier.
  - MetaData toggle for if the level is compatible with online play. If it isn't and a host tries to load the level, they will not be able to.
- 2D option for BG objects (VG parallax support?)
- Level preview song.
- Versus Mode (Players can attack other players via boosting through them, shooting them, etc)  
  Update: 1.9.0  
- Player shooting aiming  
  Update: 1.9.0  
- Player toolkit inventory system. Includes customizable tools and weapons that can be used with triggers and specific object tags.    
  Update: 1.9.0  
  Toolkit mechanics:  
  - Damage (sends a damage signal to a damage modifier, can have a set damage amount)
  - Shoot (spawns a custom built bullet / missile with custom movement behavior)
  - Spawn (spawns a prefab at the player)
  - Heal (heals the player but consumes the item)
  - Movement (controls the players velocity)
  - Consumable toggle (removes the item after use, heal mechanic always has this on for balance sake or I might go for a cooldown mechanic instead)
  - Aiming behavior (mouse, right stick, rotate with player, etc)
- Arcade auto plays song config.
- Global animation library.
  Notes:  
  - Can turn an object into an animation and back.
  - Can be played onto an object using a modifier.
  - Modifiers can also use it to interpolate.
- Active event keyframe value.
  Notes:  
  - Skips the next keyframe if it's value is off. Acts kinda like relative except if the value at a specific index was 0.
- Prefab Parenting child option.
  Notes:  
  - The Prefab Object itself will be considered the child of the parent, rather than base parents spawned from the Prefab Object being considered the child.
- Homing targetting
  - Closest
  - Furthest
  - Index
  - Highest Health
  - Lowest Health
  - Random
- Multiple audio tracks.
  Notes:  
  - The current song can be set from the loaded audio tracks via modifiers.
  - Tracks can be loaded / unloaded via modifiers.
- Event Modifiers layer that interpolate through a sequence and pass the variables to the modifiers.
- Metadata controller config that can be used via modifiers.
- End level music via custom audio source.

### Story
- Chapter 1.  
  Update: 1.7.0  
- Chapter 2.  
  Update: 1.10.0  

### Example
- Example tutorials. (Either ask Example about something or right click an element to show the context menu with a "Tutorial" button)  
  Update: 1.10.0  
- Example customization. (technically doable now, just would like it to be doable via JSON)

### Editor
- Collab sharing via server  
  Update: 1.8.0  
- Prefab preview image. Can set the specific capture size and position.
- Asset sharing on the online server. (Prefabs, themes, player models, etc)  
  Update: 1.8.0  
- Editor online backups / version control  
  Update: 1.8.0+  
- Interface editor.  
  Update: 1.8.0+  
- Multiplayer editor (everyone has their own perspective of the hosts' editor but have limited functionality compared to the host)  
  Update: 1.14.0  
- Hide random / relative toggles (if either is off, the keyframe will be forced to have the random / relative set to default / off)

### Interfaces
- A bind system for interfaces where an element prefab can be spawned based on a JSON file or file list.

### Modifiers
- prizeObject action modifier  
  Update: 1.9.0  
  Details: rewards the player with a prefab, theme, player model or player toolkit item.
- playerAction trigger (allows for multiple different keybinds and buttons)
- particleSystemColored action modifier  
  Update: 1.8.0  
- Ignore opacity toggle for color modifiers.  
  Update: 1.8.0  
- setStartTime modifiers.
- downloadLevelCollection modifier.  
  Update: 1.8.0
  Notes:  
  - Same as downloadLevel except for level collections.
- despawnPrefab modifier.
  Notes:  
  - Despawns the Runtime Prefab Object. If the prefab was spawned from a modifier, clear the modifier.

### Effects
- Camera Jiggle event keyframe (instead of a single thing that doesn't change throughout the entire level)  
  Update: 1.8.0+  
- Add feedback (KinoFeedback) effect to event keyframes.  
  Update: 1.8.0  
- Add a fake Desktop thing like Rhythm Doctor for Window event keyframes. (The Desktop will look like a PA interface)


## Changes
### Core
- Merge all effect managers into single EffectsManager.
- Summary and note as much as I can (or need to).
- Rework audio transition system.
- Update modifier caches.

### Editor
- Multi language support.
- Rework the level combiner into just a selection system.
- Keybind editor rework.
- Make Editor Documentation read from a json file.  
  Update: 1.8.0  
- Give Editor Documentation a cover image so people know what a specific document is talking about.  
  Update: 1.8.0  
- Undo / redo everything.  
  Update: 1.10.0  
- Optimize timeline objects by replacing TextMeshPro with UnityEngine.UI.Text. See if it could be optional?
- New Editor Layer types [Object Only, Prefab Object Only]
- Documentation & tooltips.
- Possible multi object editor rework???
- Update the file browser UI to include more info / functions.
- Overhaul the custom UI settings to be more extensive.
- Rework object dragging to have a lot more control and settings. (take some inspiration from Modern + Blender + EditorManagement)


### Fixes
- null


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
- Game Timeline Editor. (skipping regions, custom images, etc)
- Glow object that acts like bloom.
- Ignore start time object spawn?
- Transition time for applyAnimation modifiers.
- Update the TextMeshPro sprite asset.


## Demos
- Homing objects using animateObjectMath.
- Real-time clock.
- 3D cube