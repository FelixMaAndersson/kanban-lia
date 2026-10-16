using kanban_lia.Models.Domain.Columns;
using kanban_lia.Models.Domain.Placements;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace kanban_lia.Infrastructure.JsonConverters;

public class ColumnIdJsonConverter : JsonConverter<ColumnId>
{
    public override ColumnId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("ColumnId must be a JSON string.");
        }

        var value = reader.GetString();

        if (!Guid.TryParse(value, out var guid))
        {
            throw new JsonException(
                $"ColumnId value '{value}' is not a valid GUID.");
        }

        return new ColumnId(guid);
    }

    public override void Write(
        Utf8JsonWriter writer,
        ColumnId value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }
}