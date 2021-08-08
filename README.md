# RoR2GameModeAPI
RoR2GameModeAPI is a fully networked API for the game Risk of Rain 2 that provides a simplified way to add custom game modes, custom votable rulebooks, custom vote polls, as well as useful utilities and callbacks.

The API supports compatibility with vanilla along with modded clients. It should also be able to handle multiple mods and run fine even if the users don't have all the same mods installed that uses the API's functionalities

### Are you a developer?:  
For a guide on how to integrate this API with your own project you can check my [wiki](https://github.com/tung362/RoR2GameModeAPI/wiki)  
For example projects you can check out the one included with the API [here](Example/) or look at my [RoR2PVP](https://github.com/tung362/RoR2PVP) mod  

### Found a bug? Want a feature added?:  
Feel free to submit an issue here on my [github](https://github.com/tung362/RoR2GameModeAPI/issues)!  

![alt text](https://i.imgur.com/ea8UYFd.png)  

### Features:  
- **Vanilla Compatibility**
- **GameModeAPI**
  - Fully networked  
  - Useful general use case event callbacks  
  - Register custom game modes  
  - Expandable and flexible base game mode class
- **VoteAPI**
  - Fully networked  
  - Register custom vote polls  
  - Register custom RuleCategoryDefs  
  - Register custom RuleDefs  
  - Register custom RuleChoiceDefs  
- **Utilities**
  - EmbeddedUtils: Useful utilities methods for handling the loading of embedded files within a assembly
  - GameModeUtils: Useful utilities methods for handling game mode functionalities
- **Extras**
  - Config entry for setting the multiplayer limit, can play with up to 16 players!
  - Config entry for unlocking of all artifacts for debugging
  - Config entry for unlocking of all vanilla characters and loadouts for debugging

### Requirements:  
[BepInExPack 5.4.9](https://thunderstore.io/package/download/bbepis/BepInExPack/5.4.9/)  
[R2API 3.0.50](https://thunderstore.io/package/download/tristanmcpherson/R2API/3.0.50/)  
[HookGenPatcher 1.2.1](https://thunderstore.io/package/download/RiskofThunder/HookGenPatcher/1.2.1/)  

### Installation:  
1. Install BepInExPack (Version is provided above)
2. Install R2API (Version is provided above)
3. Install HookGenPatcher (Version is provided above)
4. Download and unzip RoR2GameModeAPI (From releases or on thunderstore.io)
5. Place `RoR2GameModeAPI.dll` into your `\Risk of Rain 2\BepInEx\plugins\` folder

**Default config values**  
**`GameModeAPI.cfg`**  

| Keys                          | Default values |
| ----------------------------- | -------------- |
| Unlock All Artifacts          |         false  |
| Unlock All Characters         |         false  |
| Max Multiplayer Count         |             4  |
| Modded                        |          true  |

### Change log:  
**1.0.0 (Current)**  
- Initial release  
