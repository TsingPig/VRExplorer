# VRExplorer Guidance

 A Model-based Approach for Automated Virtual Reality Scene Exploration and Testing ([TsingPig/VRExplorer_Release (github.com)](https://github.com/TsingPig/VRExplorer_Release))

## Configuration

- Unity → Package Manager → Add package from git URL https://github.com/TsingPig/VRExplorer_Release.git

- Manually set terrain objects (e.g., walls and floors) to Navigation Static.
- Bake the NavMesh.
- Add the VRExplorer agent prefab to the Package/Prefab Folder for the under-test scenes.
- Attach predefined scripts in Package/Scripts/EAT Framework/Mono Folder, or select and implement interfaces. 



# VRAgent Guidance

LLM + VRExplorer to solve the problem that manual efforts in Model Abstraction / Dataset Analysis.

## Configuration

- 1). The same as VRExplorer Configuration
- 2). **Test Plan Generation:** LLM + RAG / Manual Setting
- 3). **Test Plan Import Import**: Tools -> VRExplorer -> Import Test Plan -> Browse ->  Import Test Plan
- 4). Test Plan Checking: 检查是否在测试的场景中生成 FileIdManager，并且检查ID配置是否正确完整。



# Test Plan Interfaces Definition

**从C#实现层面来讲，**

- 一个测试计划 (Test Plan) 即等于一个 TaskList类对象，即一个**“任务列表”**。

- 一个任务列表，包含一个或者多个 taskUnit，即**“任务单元”**。
- 一个任务单元，包含一个或者多个 actionUnit，即**“动作单元”**，这是执行某个动作的最小单元，比如抓取、触发。

> 定义代码实现：
>
> ```c#
> [Serializable] public class TaskList { [JsonProperty("taskUnits")] public List<TaskUnit> taskUnits; }
> [Serializable] public class TaskUnit { [JsonProperty("actionUnits")] public List<ActionUnit> actionUnits; }
> [JsonConverter(typeof(ActionUnitConverter))] // 支持JSON多态
> 
> public class ActionUnit
> {
>     public string type; 
>     [JsonProperty("source_object_fileID")] public string objectA;
> }
> 
> public class GrabActionUnit : ActionUnit
> {
>     [JsonProperty("target_object_fileID")] public string objectB;
> }
> ```

**从 Json格式来讲**，

- taskUnits 字段包含一个列表，对应多个任务；
- 每个任务有一个 actionUnits字段，包含一个列表，对应多个动作。

> **Notes**:
>
> - Json格式中允许出现格式规范中的额外字段，但是不允许缺少必要字段。

```json
{
  "taskUnits": [
      { 	  // Task1
      "actionUnits": [
        {
         	// Task1-Action1
        },
        {
			// Task1-Action2
        }
      ]
    },
    {		// Task2
      "actionUnits": [
        {
			// Task2-Action1
        }
      ]
    }    
  ]
}

```

例如，下面这种写法包含一个任务，这个任务包含两个 Grab动作。当然也可以写成另一种形式（包含两个任务，每一个任务包含一个动作）。

```json
{
  "taskUnits": [
    {
      "actionUnits": [
        {
          "type": "Grab",
          "source_object_fileID": "2076594680",  
          "target_object_fileID": "64330974"
        },
        {
          "type": "Grab",
          "source_object_fileID": "1875767441",
          "target_object_fileID": "64330974"
        }
      ]
    }
  ]
}
```

另一种格式 （实际上这两种写法等效，只不过在 JSON 组织的逻辑上可读性不一样。更建议写成后者。

```json
{
  "taskUnits": [
    {
      "actionUnits": [
        {
          "type": "Grab",
          "source_object_fileID": "2076594680",  
          "target_object_fileID": "64330974"
        }
      ]
    },
    {
      "actionUnits": [
        {
          "type": "Grab",
          "source_object_fileID": "1875767441",
          "target_object_fileID": "64330974"
        }
      ]
    }
  ]
}
```



## Top-Level Structure

```json
{
  "taskUnit": [
    {
      "actionUnits": [
        {  }, 
        {  }
      ]
    },
    {
      "actionUnits": [
        {  }
      ]
    }
  ]
}
```

- **taskUnit**: Contains a list of taskUnits. One taskUnit is a logical testing unit, usually composing 

    red list of interactions to perform within that task.

## Interaction Interfaces Definition

### Grab Definition

#### Grab1 — Grab Object to Object

```json
{
  "type": "Grab",
  "source_object_name": "<string>",       // Name of the agent or object initiating the grab
  "source_object_fileID": <long>,         // FileID of the source object in the Unity scene file
  "target_object_name": "<string>",       // Name of the target object being grabbed
  "target_object_fileID": <long>          // FileID of the target object in the Unity scene file
}
```

**Example:**

```json
{
  "type": "Grab",
  "source_object_name": "Pyramid_salle2",
  "source_object_fileID": 863577851,
  "target_object_name": "collider_porte",
  "target_object_fileID": 870703383
}
```

> **Notes**:
>
> - 其中 source_object_name 和 target_object_name为非必要字段
> - Typically used when the agent directly manipulates a specific object.

------

#### Grab2 — Grab Object to Position

```json
{
  "type": "Grab",
  "source_object_name": "<string>",       // Name of the agent or object initiating the grab
  "source_object_fileID": <long>,         // FileID of the source object in the Unity scene file
  "target_position": {                    // Target world position to which the object should be moved
    "x": <float>,
    "y": <float>,
    "z": <float>
  }
}
```

**Example:**

```json
{
  "type": "Grab",
  "source_object_name": "Cube_salle2",
  "source_object_fileID": 194480315,
  "target_position": {
    "x": 1.25,
    "y": 0.50,
    "z": -3.40
  }
}
```

> **Notes**:
>
> - 其中 source_object_name 为非必要字段
> - The grab action does not require a second object; instead, the destination is a spatial position.



###  Trigger Definition

用于描述 **事件触发条件** 以及 **触发后执行的方法**。

```json
{
  "type": "Trigger",
  "source_object_name": "<string>",        // 触发器所属对象
  "method": "<string>",                    // Unity 生命周期或事件方法 (e.g., OnTriggerEnter, Update)
  "condition": "<string>"                  // 触发条件说明（可包含脚本ID、GUID、序列化配置、调用预期行为）
}
```

- **使用场景**: 碰撞检测、进入区域触发、脚本生命周期回调。
- **验证点**: 确认条件触发的时机与次数，绑定方法是否被调用，副作用是否符合预期。

### Transform Definition

用于描述对象在 **位置 / 旋转 / 缩放 / 物理约束** 上的变化。

```json
{
  "type": "Transform",
  "source_object_name": "<string>",        // 发生变化的对象
  "transform_type": "<string>",            // 变化类别 (e.g., Translate, Rotate, Scale, Constraint)
  "parameters": {                          // 参数配置
    "duration": "<int>",                   // 持续帧数或时间
    "expected_state": "<string>"           // 变化后的预期状态 (e.g., RigidbodyConstraints.FreezeAll)
  }
}
```

- **使用场景**: 物体吸附（Snap）、旋转动画、冻结物理状态。
- **验证点**: 确认变化是否持续正确时间，变化后对象状态是否与预期一致。

# Changelog

## [1.7.1] - 2025-09-04

### Added

- JSON scripts (ActionUnitConverter, ActionDef, TaskDef for optimize structure of JSON format in test plan)

- TagInitializer for tag the object that instantiated for temporary usage

### Feature

- supported `target_position ` for GrabActionUnit

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

### Added
- Support for exporting GameObjects with scripts under the `VRExplorer` namespace only.
- Stable identifier logic using `GlobalObjectId` (scene objects) & AssetDatabase GUID(prefabs).

- `RemoveVRExplorerScripts()` for remove all the added VRExplorer Mono predefined scripts.

