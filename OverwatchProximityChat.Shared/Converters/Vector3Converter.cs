using System.Text.Json.Serialization;
using System.Text.Json;
using System.Numerics;

namespace OverwatchProximityChat.Shared
{
    public class Vector3Converter : JsonConverter<Vector3>
    {
        public override Vector3 Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                string[] value = reader.GetString().TrimStart('(').TrimEnd(')').Split(",");

                Vector3 toReturn = new Vector3();

                toReturn.X = float.Parse(value[0]);
                toReturn.Y = float.Parse(value[1]);
                toReturn.Z = float.Parse(value[2]);
                return toReturn;
            }
           

            throw new JsonException();
        }

        public override void Write(Utf8JsonWriter writer, Vector3 value, JsonSerializerOptions options)
        {
            writer.WriteStringValue($"({value.X},{value.Y},{value.Z})");
        }
    }
}
