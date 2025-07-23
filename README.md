# VRExplorer

 A Model-based Approach for Automated Virtual Reality Scene Exploration and Testing ([TsingPig/VRExplorer_Release (github.com)](https://github.com/TsingPig/VRExplorer_Release))

## Configuration

- Unity → Package Manager → Add package from git URL https://github.com/TsingPig/VRExplorer_Release.git

- Manually set terrain objects (e.g., walls and floors) to Navigation Static.
- Bake the NavMesh.
- Add the VRExplorer agent prefab to the Package/Prefab Folder for the under-test scenes.
- Attach predefined scripts in Package/Scripts/EAT Framework/Mono Folder, or select and implement interfaces. 
