using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;
using System;
using UnityEngine;

namespace VRExplorer.JSON
{
    [JsonConverter(typeof(ActionUnitConverter))] // ÷ß≥÷JSON∂‡Ã¨
    public class ActionUnit
    {
        public string type; 
        [JsonProperty("source_object_fileID")] public string objectA;
    }

    public class GrabActionUnit : ActionUnit
    {
        [JsonProperty("target_object_fileID")] public string? objectB;
        [JsonProperty("target_position")] public Vector3? targetPosition;
    }
}