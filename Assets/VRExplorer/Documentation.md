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



# Test Plan Format

## Top-Level Structure

- 一个测试计划 (Test Plan) 即等于一个 TaskList类对象，即一个**“任务列表”**。
- 一个任务列表，包含一个或者多个 taskUnit，即**“任务单元”**。
- 一个任务单元，包含一个或者多个 actionUnit，即**“动作单元”**，这是执行某个动作的最小单元，比如抓取、触发。
- Note:
    - Json格式中允许出现格式规范中的额外字段，但是不允许缺少必要字段。

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

## Event System

在某些交互中，可能需要触发或调用物体脚本上捆绑的公共函数。这类交互对应的动作单元需要包含一个或多个 **事件列表（events）**。事件列表在 Unity 中对应 `List<UnityEvent>`，而每一个事件单元对应 Unity 中的 `UnityEvent`，它是一种支持序列化、可视化（Inspector 窗口中可见）的封装委托（Delegate），用于按顺序调用绑定的函数。每个事件单元包含多个回调函数单元对应 UnityEvent 的一条 Listener，即实际调用的函数。总结如下：

- 每一个事件列表包含1个或者多个的 eventUnit，即**“事件单元”**。对应`UnityEvent`
- 每一个事件单元，包含1个或者多个methodCallUnit，即**“回调函数单元”**，它是对应执行到具体函数的最小单元，对应一条Listener
- 一个事件列表内的多个事件单元宏观上顺序执行；同一个事件单元 里面的多个 回调函数单元在帧内部顺序执行，在宏观上并发；

``` json
"events": [                 // 列表
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
  ]
```



**methodCallUnit 字段说明**

| 字段名                           | 类型   | 必填情况                         | 说明                                           |
| -------------------------------- | ------ | -------------------------------- | ---------------------------------------------- |
| methodCallUnits.script_fileID    | long   | 必填                             | 目标脚本的 FileID                              |
| methodCallUnits.method_name      | string | 必填                             | 要调用的方法名                                 |
| methodCallUnits.parameter_fileID | array  | 必填（截至 v1.7.2 版本必须为空） | 方法参数的 FileID 列表，当前版本不支持参数操作 |

**Example:**

```json
"events": [
  {
    "methodCallUnits": [
      {
        "script_fileID": 124958031,     
        "method_name": "OpenTheDoor",     
        "parameter_fileID": []         
      }
    ]
  }
]
```

## Interaction Definition

### Grab

Grab 动作分为两类：**Grab1: 对象到对象（Grab Object to Object）、Grab2: 对象到位置（Grab Object to Position）**。在 JSON 中，这两种 Grab 动作共享部分字段，例如 `type` 和 `source_object_fileID` 总是必填，而 `source_object_name` 可选。两者的区别在于目标字段：对象到对象需要指定目标对象的名称和 FileID，而对象到位置则需要指定目标的空间坐标 `target_position`。通过统一表格展示字段的必填情况，使用者可以清晰了解在不同 Grab 类型下哪些字段必须提供，哪些可以省略，从而正确构建任务或动作 JSON 数据。

| 字段名               | 类型   | 必填情况                | 说明                                                        |
| -------------------- | ------ | ----------------------- | ----------------------------------------------------------- |
| type                 | string | 必填                    | 固定为 "Grab"                                               |
| source_object_name   | string | 非必填                  | 发起抓取对象名称，可选填写                                  |
| source_object_fileID | long   | 必填                    | 发起抓取对象的 Unity FileID                                 |
| target_object_name   | string | Grab1 必填/Grab2 非必填 | 被抓取对象名称，仅在 Grab 对象到对象时使用                  |
| target_object_fileID | long   | Grab1 必填/Grab2 非必填 | 被抓取对象的 Unity FileID，仅在 Grab 对象到对象时使用       |
| target_position      | object | Grab2 必填/Grab1 非必填 | 目标位置，仅在 Grab 对象到位置时使用，包含 x, y, z 三个字段 |
| target_position.x    | float  | Grab2 必填              | 目标位置 X 坐标，仅在 Grab 对象到位置时使用                 |
| target_position.y    | float  | Grab2 必填              | 目标位置 Y 坐标，仅在 Grab 对象到位置时使用                 |
| target_position.z    | float  | Grab2 必填              | 目标位置 Z 坐标，仅在 Grab 对象到位置时使用                 |

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

#### Examples

**Grab1_Example1**

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

**Grab1_Example2**

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

**Grab2_Example1:**

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





###  Trigger

Trigger 描述 **交互动作中的触发过程**，主要用于模拟玩家在 VR 场景中对物体的交互，例如点击按钮、拉动拉杆或触发某个状态变化。

Trigger 动作基于框架的事件系统，支持 **触发过程事件（triggerring_events）** 和 **完成后事件（triggerred_events）**，两类事件均可调用 Unity 脚本中的公共函数。通过这种方式能够组合实现对复杂函数逻辑的调用。

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

| 字段名               | 类型   | 必填情况 | 说明                                           |
| -------------------- | ------ | -------- | ---------------------------------------------- |
| type                 | string | 必填     | 固定为 `"Trigger"`                             |
| source_object_name   | string | 非必填   | 源对象名称                                     |
| source_object_fileID | long   | 必填     | Unity 场景文件中源对象的 FileID                |
| condition            | string | 非必填   | 触发条件说明                                   |
| triggerring_time     | float  | 必填     | 触发过程持续时间（秒）                         |
| triggerring_events   | array  | 非必填   | 触发过程中的事件列表，序列化为 UnityEvent 列表 |
| triggerred_events    | array  | 非必填   | 触发完成后的事件列表，序列化为 UnityEvent 列表 |

#### Examples

**Example1**

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

Transform 描述 **物体的平移、旋转、缩放变换操作**，用于在动作单元中实现物体状态的增量调整（delta）。

- 所有字段均为 **偏移量**，例如让物体 Y 轴缩放 1.1 倍，则 `delta_scale.y` 设置为 0.1，而非绝对值。
- Transform 动作继承 Trigger 的事件设计，支持 **事件列表（triggerring_events）** 和 **完成后事件列表（triggerred_events）**，可在变换执行过程中或结束后触发物体脚本中的函数。
- `trigger_time` 指定动作持续时间，实现平滑过渡。

Transform 的核心用途包括：

1. **动态调整物体状态**：例如平移、旋转、缩放，实现动画效果或交互反馈。
2. **触发脚本行为**：通过事件列表调用绑定在 Unity 脚本上的方法，支持复杂动作与交互逻辑。
3. **增量式控制**：偏移量设计允许动作单元相对于当前状态进行调整，无需知道物体绝对位置、旋转或缩放。

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
  "triggerring_time": <float>                  // 动作持续时间
}

```

| 字段名               | 类型   | 必填情况                                             | 说明                                           |
| -------------------- | ------ | ---------------------------------------------------- | ---------------------------------------------- |
| type                 | string | 必填                                                 | 固定为 "Transform"                             |
| source_object_name   | string | 非必填                                               | 目标对象名称，可选填写                         |
| source_object_fileID | long   | 必填                                                 | Unity 场景中对象的 FileID                      |
| delta_position       | object | 必填，不需要时需填0占位，后续同理                    | 位置增量，包含 x, y, z 三个 float 字段         |
| delta_rotation       | object | 必填                                                 | 旋转增量，包含 x, y, z 三个 float 字段         |
| delta_scale          | object | 必填                                                 | 缩放增量，包含 x, y, z 三个 float 字段         |
| triggerring_events   | array  | 非必填触发过程中的事件列表，序列化为 UnityEvent 列表 | 触发过程中的事件列表，序列化为 UnityEvent 列表 |
| triggerred_events    | array  | 非必填                                               | 触发过程后的事件列表，序列化为 UnityEvent 列表 |
| triggerring_time     | float  | 必填                                                 | 动作持续时间（秒）                             |

#### Examples

**Example1:** 能够在3秒内让对应的物体变成原来的 1.5倍大，其delta值为0.5

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
            "x": 0.5,
            "y": 0.5,
            "z": 0.5
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
- XRTriggerable: Triggered Events Execution problem

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



# Appendix

**C# 接口 代码**

> ```c#
> [Serializable] public class TaskList { [JsonProperty("taskUnits")] public List<TaskUnit> taskUnits; }
> [Serializable] public class TaskUnit { [JsonProperty("actionUnits")] public List<ActionUnit> actionUnits; }
> [Serializable] public class eventUnit { [JsonProperty("methodCallUnits")] public List<methodCallUnit> methodCallUnits; }
> 
> [Serializable]
> public class methodCallUnit
> {
> [JsonProperty("script_fileID")] public string script;
> [JsonProperty("method_name")] public string methodName;
> [JsonProperty("parameter_fileID")] public List<string>? parameters;
> }
> 
> [JsonConverter(typeof(ActionUnitConverter))] // 支持JSON多态
> public class ActionUnit
> {
> public string type; 
> [JsonProperty("source_object_fileID")] public string objectA;
> }
> 
> public class GrabActionUnit : ActionUnit
> {
> [JsonProperty("target_object_fileID")] public string? objectB;
> [JsonProperty("target_position")] public Vector3? targetPosition;
> }
> 
> public class TriggerActionUnit: ActionUnit
> {
> [JsonProperty("triggerring_events")] public List<eventUnit> triggerringEvents;
> [JsonProperty("triggerred_events")] public List<eventUnit> triggerredEvents;
> }
> 
> /// <summary>
> /// TransformActionUnit 用于描述物体的平移/旋转/缩放操作
> /// </summary>
> public class TransformActionUnit : TriggerActionUnit
> {
>  [JsonProperty("delta_position")] public Vector3 deltaPosition;
>  [JsonProperty("delta_rotation")] public Vector3 deltaRotation;
>  [JsonProperty("delta_scale")] public Vector3 deltaScale;
> }
> ```

