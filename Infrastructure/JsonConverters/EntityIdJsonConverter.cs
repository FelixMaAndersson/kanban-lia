using System.Text.Json;
using System.Text.Json.Serialization;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.JsonConverters;

public class EntityIdJsonConverter : JsonConverter<EntityId>
{
    public override EntityId Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var value = reader.GetString();
        return value is null
            ? throw new JsonException("EntityId cannot be null.")
            : new EntityId(Guid.Parse(value));
    }

    public override void Write(Utf8JsonWriter writer, EntityId value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }
}