using kanban_lia.Models.Domain.Boards;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace kanban_lia.Infrastructure.JsonConverters;

public class BoardIdJsonConverter : JsonConverter<BoardId>
{
    public override BoardId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("BoardId must be a JSON string.");
        }

        var value = reader.GetString();

        if (!Guid.TryParse(value, out var guid))
        {
            throw new JsonException(
                $"BoardId value '{value}' is not a valid GUID.");
        }

        return new BoardId(guid);
    }

    public override void Write(
        Utf8JsonWriter writer,
        BoardId value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }
}