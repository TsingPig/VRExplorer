using System.Collections.Generic;
using Unity.Plastic.Newtonsoft.Json.Linq;
using Unity.Plastic.Newtonsoft.Json;
using System;

namespace VRExplorer.JSON
{

    public class ActionUnitConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(ActionUnit);
        }

        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            JObject jo = JObject.Load(reader);
            string type = jo["type"]?.ToString();

            ActionUnit action;
            switch(type)
            {
                case "Grab":
                action = new GrabActionUnit();
                break;
                default:
                action = new ActionUnit();
                break;
            }

            serializer.Populate(jo.CreateReader(), action);
            return action;
        }

        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            serializer.Serialize(writer, value);
        }
    }

}