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
- 4). Test Plan **Checking**: 检查是否在测试的场景中生成 FileIdManager，并且检查ID配置是否正确完整；**同时检查对应的需要测试的物体是否发生变化 （比如已经附加上测试脚本）**



# Test Plan Interfaces Definition

## Top-Level Structure

- 一个测试计划 (Test Plan) 即等于一个 TaskList类对象，即一个**“任务列表”**。
- 一个任务列表，包含一个或者多个 taskUnit，即**“任务单元”**。
- 一个任务单元，包含一个或者多个 actionUnit，即**“动作单元”**，这是执行某个动作的最小单元，比如抓取、触发。
- 除此以外，某些动作单元可能包含一个或者多个**“事件列表”**，当然有可能没有事件列表。
- 每一个事件列表包含**0个**、1个或者多个的 eventUnit，即**“事件单元”**。
- 每一个事件单元，包含**0个**、1个或者多个methodCallUnit，即**“回调函数单元”**，它是对应执行到具体函数的最小单元
- Note:
    - Json格式中允许出现格式规范中的额外字段，但是不允许缺少必要字段。
    - 一个事件列表内的多个事件单元宏观上顺序执行；同一个事件单元 里面的多个 回调函数单元在帧内部顺序执行，在宏观上并发；

**C#定义代码**

> ```c#
> [Serializable] public class TaskList { [JsonProperty("taskUnits")] public List<TaskUnit> taskUnits; }
> [Serializable] public class TaskUnit { [JsonProperty("actionUnits")] public List<ActionUnit> actionUnits; }
> [Serializable] public class eventUnit { [JsonProperty("methodCallUnits")] public List<methodCallUnit> methodCallUnits; }
> 
> [Serializable]
> public class methodCallUnit
> {
>     [JsonProperty("script_fileID")] public string script;
>     [JsonProperty("method_name")] public string methodName;
>     [JsonProperty("parameter_fileID")] public List<string>? parameters;
> }
> 
> [JsonConverter(typeof(ActionUnitConverter))] // 支持JSON多态
> public class ActionUnit
> {
>     public string type; 
>     [JsonProperty("source_object_fileID")] public string objectA;
> }
> 
> public class GrabActionUnit : ActionUnit
> {
>     [JsonProperty("target_object_fileID")] public string? objectB;
>     [JsonProperty("target_position")] public Vector3? targetPosition;
> }
> 
> public class TriggerActionUnit: ActionUnit
> {
>     [JsonProperty("triggerring_events")] public List<eventUnit> triggerringEvents;
>     [JsonProperty("triggerred_events")] public List<eventUnit> triggerredEvents;
> }
> ```

**JSON format structure**

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

**Example1**

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

**Example2**

下面这种写法包含一个任务，这个任务包含两个 Grab动作。当然也可以写成另一种形式（包含两个任务，每一个任务包含一个动作）。

```json
{
  "taskUnits": [
    {
      "actionUnits": [
        {
          "type": "Grab",
          "source_object_fileID": 2076594680,  
          "target_object_fileID": 64330974
        },
        {
          "type": "Grab",
          "source_object_fileID": 1875767441,
          "target_object_fileID": 64330974
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
          "source_object_fileID": 2076594680,  
          "target_object_fileID": 64330974
        }
      ]
    },
    {
      "actionUnits": [
        {
          "type": "Grab",
          "source_object_fileID": 1875767441,
          "target_object_fileID": 64330974
        }
      ]
    }
  ]
}
```



#### Grab2 — Grab Object to Position

```json
{
  "type": "Grab",
  "source_object_name": "<string>",       // Name of the source object
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

用于描述触发某个物体的过程中需要调用的函数链、触发完成后需要调用的函数链。

```json
{
  "type": "Trigger",
  "source_object_name": "<string>",       // 触发事件的源对象名称
  "triggerring_time": <float>, 			  // 触发的持续时间
  "source_object_fileID": <long>,         // Unity 场景文件中源对象的 FileID
  "condition": "<string>",                // 触发条件说明（可包含脚本ID、GUID、序列化配置、调用预期行为）
  "triggerring_events": [                 // Trigger过程中的事件列表
    // 0个或者若干个事件单元
    {
      "methodCallUnits": [                // 一个事件单元，包含0个或者多个methodCallUnit
        {
          "script_fileID": <long>,     // 目标脚本的 FileID
          "method_name": "<string>",     // 要调用的方法名
          "parameter_fileID": []         // 方法参数的 FileID 列表
        }
      ]
    }
  ],
  "triggerred_events": [                  //  Trigger完成后的事件列表
    	// 0个或者若干个事件单元
  ]
}

```

> **Note**:
>
> - 目前还无法绑定有参数的函数，parameter_fileID必须为空。
> - condition 和 source_object_name 为非必要选项
> - triggerring_events 和 triggerred_events 可为空，但一般需要包含触发事件

**Example**

下面的例子中包含了一个Trigger任务，其中triggerring_events 包含两个 eventUnit，每个eventUnit包含一个methodCallUnit；triggerred_events包含一个eventUnit，它拥有两个methodCallUnit。

Triggerring过程中的事件：换弹 -> 开火

Triggerred完成后的事件：换弹 + 换弹（同时）

```json
{
  "taskUnits": [
    {
      "actionUnits": [
        {
          "type": "Trigger",
          "triggerring_time": 1.5,
          "source_object_fileID": 1448458900,
          "triggerring_events": [
            {
              "methodCallUnits": [
                {
                  "script_fileID": 1448458903,
                  "method_name": "Reload",
                  "parameter_fileID": []
                }
              ]
            },
            {
              "methodCallUnits": [
                {
                  "script_fileID": 1448458903,
                  "method_name": "Fire",
                  "parameter_fileID": []
                }
              ]
            }
          ],
          "triggerred_events": [
            {
              "methodCallUnits": [
                {
                  "script_fileID": 1448458903,
                  "method_name": "Reload",
                  "parameter_fileID": []
                },
                {
                  "script_fileID": 1448458903,
                  "method_name": "Reload",
                  "parameter_fileID": []
                }
              ]
            }
          ]
        }
      ]
    }
  ]
}
```



### Transform Definition

在设定上，Transform沿用Trigger的设计，包含一切Trigger的功能。额外增加了用于描述 **物体的平移、旋转、缩放变换操作**的字段。其中所有字段皆为偏移量，例如让物体的 Y轴变成1.1x，偏移量的y设定为 0.1。

> **Note**:
>
> - 目前还无法绑定有参数的函数，parameter_fileID必须为空。
> - condition 和 source_object_name 为非必要选项
> - triggerring_events 和 triggerred_events 一般为空

```json
{
  "type": "Transform",
  "source_object_name": "<string>",        // 目标对象名称
  "source_object_fileID": <long>,          // Unity 场景中对象的 FileID
  "target_position": {                     // 位置delta量
    "x": <float>,
    "y": <float>,
    "z": <float>
  },
  "target_rotation": {                     // 旋转delta量
    "x": <float>,
    "y": <float>,
    "z": <float>
  },
  "target_scale": {                        // 缩放delta量
    "x": <float>,
    "y": <float>,
    "z": <float>
  },
  "triggerring_events": [                 // Trigger过程中的事件列表
        // 0个或者若干个事件单元
        {
          "methodCallUnits": [                // 一个事件单元，包含0个或者多个methodCallUnit
            {
              "script_fileID": <long>,     // 目标脚本的 FileID
              "method_name": "<string>",     // 要调用的方法名
              "parameter_fileID": []         // 方法参数的 FileID 列表
            }
          ]
        }
      ],
  "triggerred_events": [                  //  Trigger完成后的事件列表
            // 0个或者若干个事件单元
      ],
  "trigger_time": <float>                  // 动作持续时间
}

```

**Example**

能够在3秒内让对应的物体变成原来的 1.5倍大

```json
{
      "actionUnits": [
        {
          "type": "Transform",
          "source_object_fileID": 1760679936,
          "delta_position": {
            "x": 0,
            "y": 0,
            "z": 0
          },
          "delta_rotation": {
            "x": 0,
            "y": 0,
            "z": 0
          },
          "delta_scale": {
            "x": 1.5,
            "y": 1.5,
            "z": 1.5
          },
          "triggerring_time": 3
        }
      ]
    }
```



# Changelog

## [1.7.2] - 2025-09-04

### Added

- JSON scripts (ActionUnitConverter, ActionDef, TaskDef for optimize structure of JSON format in test plan)

- TagInitializer for tag the object that instantiated for temporary usage
- TriggerActionUnit for Test Plan

### Feature

- supported `target_position ` for GrabActionUnit, `triggerring_time` for TriggerActionUnit
- **Trigger Action/ Transform Action** supported initailly in Test Plan Json;  (supporting Event List)
- GetObjectFileID supporting Object parameter 

### Fixed 

- prefab can't be identified when it is on the top-level of the scene
- XRTriggerable: Triggerred Events Execution problem

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

