# VRExplorer

VRExplorer: A Model-based Approach for Semi-Automated Testing of Virtual Reality Scenes (ASE '25) [PDF](https://tsingpig.github.io/files/VRExplorer__A_Model_based_Approach_for_Semi-Automated_Testing_of_Virtual_Reality_Scenes.pdf)

## Guideline

- Unity → Package Manager → Add package from git URL https://github.com/TsingPig/VRExplorer_Release.git
- Manually set terrain objects (e.g., walls and floors) to Navigation Static and Bake the NavMesh.
- Add the VRExplorer agent prefab to the `Package/Prefabs` Folder for the under-test scenes.
- Adding and Inplement interfaces (`Package/EAT Framework/Entity`)
- *[Optional]* Attach predefined scripts in `Package/Scripts/EAT Framework/Mono` Folder, or select and implement interfaces. 
