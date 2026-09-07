using System.Text.Json;
using System.Text.Json.Serialization;
using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Infrastructure.JsonConverters;

public class ColumnIdJsonConverter : JsonConverter<ColumnId>
{
    public override ColumnId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null
            ? throw new JsonException("ColumnId cannot be null.")
            : new ColumnId(Guid.Parse(value));
    }

    public override void Write(Utf8JsonWriter writer, ColumnId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }
}