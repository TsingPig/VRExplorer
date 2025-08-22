# Changelog

## [1.6.6] - 2025-08-22

### Added
- **VREscaper** prefab.
- Interaction Counter for tracking different types of interactions in dataset projects.
- **VREscaper** feature set:
  - Support for importing JSON (.json) format Test Plans and automated test execution.  
  - FileID-based GameObject Finding System (`FileIdResolver.cs` & `TestPlanImporterWindow.cs`).

### Changed
- Added configurable `autonomousEventInterval` parameter (with Inspector slider) to control autonomous event execution delay; Adding `ResetExploration()` in `BaseExplorer` allows repeatable task executaion

### Fixed
- FileID consistency for prefab instance GameObjects across scenes.
- Correct VREscaper prefab path.
- Removed random movement behavior from `BaseTask`.



## [1.5.6] - 2025-06-18

### Fixed

- GameObjectConfigManager prefab import & export logic

## [1.5.5] - 2025-06-18

### Added
- Support for exporting GameObjects with scripts under the `VRExplorer` namespace only.
- Stable identifier logic using `GlobalObjectId` (scene objects) & AssetDatabase GUID(prefabs).

- `RemoveVRExplorerScripts()` for remove all the added VRExplorer Mono predefined scripts.

