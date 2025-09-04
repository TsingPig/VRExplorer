using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;
using System;

namespace VRExplorer.JSON
{
    [Serializable] public class TaskList { [JsonProperty("taskUnits")] public List<TaskUnit> taskUnits; }
    [Serializable] public class TaskUnit { [JsonProperty("actionUnits")] public List<ActionUnit> actionUnits; }
}