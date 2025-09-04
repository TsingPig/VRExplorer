# Interfaces Definition

**从C#实现层面来讲，**

- 一个测试计划 (Test Plan) 即等于一个 TaskList类对象，即一个**“任务列表”**。

- 一个任务列表，包含一个或者多个 taskUnit，即**“任务单元”**。
- 一个任务单元，包含一个或者多个 actionUnit，即**“动作单元”**，这是执行某个动作的最小单元，比如抓取、触发。

```c#
// Supporting classes for JSON deserialization
[System.Serializable]
public class TaskList
{
    public List<TaskUnit> taskUnits;
}

[System.Serializable]
public class TaskUnit
{
    public List<ActionUnit> actionUnits;
}

[System.Serializable]
public class ActionUnit
{
    public string type; // "Grab", "Move", "Drop", etc.
    public string objectA;
}

[System.Serializable]
public class GrabActionUnit: ActionUnit
{
    public string objectB;
}
```

从 Json格式来讲，

```json
{
  "taskUnits": [
    {
      "actionUnits": [
        {
          "type": "Grab",
          "objectA": "194480315",
          "objectB": "855068735"
        },
        {
          "type": "Grab",
          "objectA": "863577851",
          "objectB": "896816000"
        },
        {
          "type": "Grab",
          "objectA": "284893529",
          "objectB": "559748133"
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

## Interaction

### Grab Definition

Describes an **agent-to-object interaction** where the agent grabs, releases, moves, throws, or carries an object.

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
> - Typically used when the agent directly manipulates a specific object.
> - Verification should ensure that the grab action succeeds and the target object responds as expected (e.g., becomes attached, follows agent movement, etc.).

------

#### Grab2 — Grab Object to Position

```
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
> - Useful for testing object relocation or drop mechanics.
> - The grab action does not require a second object; instead, the destination is a spatial position.
> - Verification includes confirming the object is released or placed at the correct coordinates.

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

## [1.7.0] - 2025-09-04

### Added

- JSON scripts (ActionUnitConverter, ActionDef, TaskDef for optimize structure of JSON format in test plan)

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

