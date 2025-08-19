# Changelog



## [1.6.3] - 2025-08-19

### Added  

***VREscaper*** feature: 

- Supports importing JSON (.json) format Test Plans  &  Automates test execution  
-  Uses FileID-based GameObject Finding System (FileIdResolver.cs & TestPlanImporterWindow.cs)

## [1.5.6] - 2025-06-18

### Fixed

- `GameObjectConfigManager.cs` prefab import & export logic

## [1.5.5] - 2025-06-18

### Added
- Support for exporting GameObjects with scripts under the `VRExplorer` namespace only.
- Stable identifier logic using `GlobalObjectId` (scene objects) & AssetDatabase GUID(prefabs).

- `RemoveVRExplorerScripts()` for remove all the added VRExplorer Mono predefined scripts.

