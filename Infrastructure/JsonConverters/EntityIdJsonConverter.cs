using System.Text.Json;
using System.Text.Json.Serialization;
using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.JsonConverters;

public class EntityIdJsonConverter : JsonConverter<EntityId>
{
    public override EntityId Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options)
    {
        if (reader.TokenType != JsonTokenType.String)
        {
            throw new JsonException("EntityId must be a JSON string.");
        }

        var value = reader.GetString();

        if (!Guid.TryParse(value, out var guid))
        {
            throw new JsonException(
                $"EntityId value '{value}' is not a valid GUID.");
        }

        return new EntityId(guid);
    }

    public override void Write(
        Utf8JsonWriter writer,
        EntityId value,
        JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.Id);
    }
}