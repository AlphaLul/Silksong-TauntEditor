# TauntEditor
 Allows the user to customize Hornet's taunt audio with their own .wav audio clips. The mod comes prepackaged with some of Hornet's voice lines from the original Hollow Knight game (including shaw).
 
# Guide
### Installation
1. Install BepInEx.
2. Download `TauntEditor.zip` and extract it to the `BepInEx/plugins` folder.
3. Run the game.
   - After running the game, you'll have access to the config file at `BepInEx/config/alphalul.TauntEditor.cfg`.

---
### Modifying audio clips
**Adding clips:** Add .wav files to the `TauntEditor/Clips` folder. `Clips` folder name can be changed in the config.

**Archiving clips:** Move desired clips to the `TauntEditor/Clips/Archive` folder. This allows you to remove clips while maintaining track of them. `Archive` folder name can be changed in the config.

---
### Config
#### Folders: 
- `string ClipsFolderName` (default: `"Clips"`): Name of folder that will be searched for clips.
- `string ArchivedClipsSubfolderName` (default: `"Archive"`): Name of folder that you wish to exclude from the clips search. Allows you to remove clips without deleting them.

#### Toggles:
- `bool DisableMod` (default: `false`): Whether or not to disable the mod's functionality. True allows you to return to vanilla functionality without moving or deleting clips.
- `bool RefreshOnSaveQuit` (default: `true`): Whether or not to refresh the clips list after returning to the title screen. True allows you to add or remove clips while the game is running.
- `bool IncludeVanillaClips` (default: `false`): Whether or not to include Silksong's vanilla taunt clips.

---
### ⚠️Important notes
 Mono .wav clips seem to play quieter than stereo clips. If your clips are sounding quieter than expected, consider converting them to stereo.

 If the `TauntEditor/Clips` folder is empty, the mod will load the vanilla clips, regardless of the value of `IncludeVanillaClips`.