using System.Text.Json;
using System.Text.Json.Serialization;

namespace Mathy.ELM.Core.Converters;

public class StringOrNumberConverter : JsonConverter<string?>
{
    public override string? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.String)
        {
            return reader.GetString();
        }
        else if (reader.TokenType == JsonTokenType.Number)
        {
            // Handle both integer and decimal numbers
            if (reader.TryGetInt32(out int intValue))
            {
                return intValue.ToString();
            }
            else if (reader.TryGetDecimal(out decimal decimalValue))
            {
                return decimalValue.ToString();
            }
            else
            {
                return reader.GetDouble().ToString();
            }
        }
        else if (reader.TokenType == JsonTokenType.Null)
        {
            return null;
        }
        
        throw new JsonException($"Cannot convert token type {reader.TokenType} to string");
    }

    public override void Write(Utf8JsonWriter writer, string? value, JsonSerializerOptions options)
    {
        if (value == null)
        {
            writer.WriteNullValue();
        }
        else
        {
            writer.WriteStringValue(value);
        }
    }
}