using System.Text.Json;
using System.Text.Json.Serialization;
using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Infrastructure.JsonConverters;

public class BoardIdJsonConverter : JsonConverter<BoardId>
{
    public override BoardId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null
            ? throw new JsonException("BoardId cannot be null.")
            : new BoardId(Guid.Parse(value));
    }

    public override void Write(Utf8JsonWriter writer, BoardId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }
}