using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using NovelNode.Data;

namespace NovelNode.Helpers;

public class JsonNodeConverter : JsonConverter
{
    public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
    {
        throw new NotImplementedException();

    }

    public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
    {
        var jsonObject = JObject.Load(reader);
        var type = jsonObject["Type"]?.Value<string>();

        switch (type)
        {
            case "Dialogue":
                return jsonObject.ToObject<NodeDialogue>();
            case "Choice":
                return jsonObject.ToObject<NodeChoice>();
            case "Background":
                return jsonObject.ToObject<NodeBackground>();
            case "Character":
                return jsonObject.ToObject<NodeCharacter>();
            default:
                throw new JsonSerializationException($"Unknown node type: {type}");
        }
    }

    public override bool CanRead => false;

    public override bool CanConvert(Type objectType)
    {
        return objectType == typeof(Node);
    }
}