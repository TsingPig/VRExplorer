using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json;
using System;

namespace VRExplorer.JSON
{
    [JsonConverter(typeof(ActionUnitConverter))] // ֧��JSON��̬
    public class ActionUnit
    {
        public string type; 
        [JsonProperty("source_object_fileID")] public string objectA;
    }

    public class GrabActionUnit : ActionUnit
    {
        [JsonProperty("target_object_fileID")] public string objectB;
    }
}