# TODO
1.9.0 - Online Multiplayer Update  
- Multiplayer with any level in Arcade (local, Steam, etc)
- Multiplayer compatible with story mode (supports Asset Packs)
- Joinable rooms / direct connect
- Voice chat with optional proximity (maybe can be controlled via modifiers?)
- Syncing
  - Sync interface
  - Sync interface selection
  - Sync arcade list (clicking a level will send a request for the host to open it)
  - Sync viewed arcade level
  - Sync end level interface
  - Sync editor level list (clicking a level will send a request for the host to open it)
  - Sync object creation
  - Sync prefab expanding
  - Sync keybind functions
  - Sync saving (client trying to save will request the host to save)
- Player variables
- Player models support multiplayer
- Versus mode
- Player Chat Bubble port (editable via Asset Packs and can select from a default list of bubble styles [Legacy, Modern, etc])
- Steam friends can join editor sessions.
- Players get different perspectives of the host editor.
- Currently editing objects aren't selectable by other users.
- Off-screen player indicators.
1.10.0 - Editor Assistance Update  
- Fully customizable layouts, editor complexity and themes via Asset Packs.
- New Example expressions, dialogue, commands, notices and implemented tutorials.
- Complete tooltip system (old tooltip system reserved for information rather than descriptions)
- Complete documentation (maybe include selecting documentation in context menus)
1.11.0 - Story Mode Update  
- Chapter 2+
- Improved workflow and customizability

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
- Versus Mode (Players can attack other players via boosting through them, shooting them, etc)  
  Update: 1.9.0  
- Player shooting aiming  
  Update: 1.9.0  
- SCRAPPED!!! Player toolkit inventory system. Includes customizable tools and weapons that can be used with triggers and specific object tags.  
  Update: 1.9.0  
  NEEDS IMPLEMENTATION  
  Toolkit mechanics:  
  - Damage (sends a damage signal to a damage modifier, can have a set damage amount)
  - Shoot (spawns a custom built bullet / missile with custom movement behavior)
  - Spawn (spawns a prefab at the player)
  - Heal (heals the player but consumes the item)
  - Movement (controls the players velocity)
  - Consumable toggle (removes the item after use, heal mechanic always has this on for balance sake or I might go for a cooldown mechanic instead)
  - Aiming behavior (mouse, right stick, rotate with player, etc)
- Player Variable (works like Level and Level Collection Variables where it saves per player.)  
  Update: 1.9.0
- Endless shuffle.  
  Update: 1.9.0  
  Notes:  
  - "Next Level" button always shows in endless shuffle mode. Clicking it takes the player to a random level.  
- Arcade auto plays song config.
- Global animation library.  
  Update: 1.10.0  
  Notes:  
  - Can turn an object into an animation and back.
  - Can be played onto an object using a modifier.
  - Modifiers can also use it to interpolate.
- Homing targetting  
  Update: 1.9.0+  
  NEEDS IMPLEMENTATION  
  - Closest
  - Furthest
  - Index
  - Highest Health
  - Lowest Health
  - Random
- Metadata controller config that can be used via modifiers.  
  Update: 1.9.0+  
- Backup arcade and story savedata onto the server.  
  Update: 1.9.0  
- Extra credits (artists, creators, songs)
- Implement BeatmapVariable  
  Update: 1.10.0+  
- Object pooling  
  Update: 1.10.0  
  

### Story
- Chapter 1.  
  Update: 1.7.0  
- Chapter 2.  
  Update: 1.11.0  

### Example
- Example tutorials. (Either ask Example about something or right click an element to show the context menu with a "Tutorial" button)  
  Update: 1.10.0  
- Example customization. (technically doable now, just would like it to be doable via JSON)  
  Update: 1.10.0  

### Editor
- Asset sharing on the online server. (Player models, etc)  
  Update: 1.9.0  
- Editor online backups / version control  
  Update: 1.9.0+  
- Interface editor.  
  Update: 1.10.0+  
- Multiplayer editor (everyone has their own perspective of the hosts' editor but have limited functionality compared to the host)  
  Update: 1.12.0  
- Sync value context menus  
- Prefab collections (a way to properly view prefabs and other things inside of a prefab. clicking a prefab panel with only prefabs and no objects will expand to view the prefabs inside)  

### Interfaces
- Advanced Filter (Levels & Level Collections) interface  
- Extra Level Credits interface  

### Modifiers
- giftAsset (originally prizeObject) action modifier  
  Update: 1.9.0  
  NEEDS IMPLEMENTATION  
  Details: rewards the player with a prefab, theme, player model or player toolkit item.  
  Notes:  
  - Change this to be a Prefab instead, since Prefabs can now store themes. (maybe they should also be able to store Player models? make Player models use the import / export system first)
- playerAction trigger (allows for multiple different keybinds and buttons)  
- setStartTime modifiers.  
- downloadLevelCollection modifier.  
  Update: 1.9.0+  
  Notes:  
  - Same as downloadLevel except for level collections.
- onBPM trigger modifier.

### Effects
- Camera Jiggle  
  Update: 1.9.0+  
  Notes:  
  - Modern feature port
  - Can change throughout a level?
  - Needs to be related to MetaData since the values for it are stored there in VG files.
- Add feedback (KinoFeedback) effect to event keyframes.  
  Update: Undetermined  
  Notes:  
  - Could not get this to work.
- Add a fake Desktop thing like Rhythm Doctor for Window event keyframes. (The Desktop will look like a PA interface)
  Update: 1.10.0+
- Event Keyframe String Values  
  Notes:  
  - Used for themes.
  - Math parsing?
- Active event keyframe value.
  Notes:  
  - Skips the next keyframe if it's value is off. Acts kinda like relative except if the value at a specific index was 0.
  - Only needs to be implemented if it ever becomes a thing in modern.


## Changes
### Core
- Merge all effect managers into single EffectsManager.  
- Summary and note as much as I can (or need to).  
  Notes:
  - Doing good so far, but need to do more. Maybe I could focus on this for 1.10.0?
- Rework audio transition system.  
- Update modifier caches.  
- Allow Window event transition.

### Interfaces

### Editor
- Multi language support.
- Make Editor Documentation read from a json file.  
  Update: 1.10.0  
- Give Editor Documentation a cover image so people know what a specific document is talking about.  
  Update: 1.10.0  
- Undo / redo everything.  
  Update: 1.10.0+  
- Documentation & tooltips.  
  Update: 1.10.0  
- Update the file browser UI to include more info / functions.  
  Update: 1.10.0  
- Overhaul the custom UI config settings to be more extensive.  
- Rework object dragging to have a lot more control and settings. (take some inspiration from Modern + Blender + EditorManagement)  
  Update: 1.10.0  

### Fixes
- Fix controller shake not stopping after death.
- Fix players not respawning on replay for client sides.
- Fix clients not updating the player positions of other clients.

## Ideas
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
- Event Modifiers layer that interpolate through a sequence and pass the variables to the modifiers.  


## Demos
- Homing objects using animateObjectMath.  
- Real-time clock.  
- 3D cube  