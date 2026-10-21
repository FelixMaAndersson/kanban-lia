using System.Text.Json;
using System.Text.Json.Serialization;

using kanban_lia.Models.Domain.Placements;

namespace kanban_lia.Infrastructure.JsonConverters
{
    /// <summary>
    /// System.Text.Json converter that reads and writes <see cref="EntityId"/> values as plain GUID strings.
    /// </summary>
    /// <remarks>Register this converter in <c>JsonSerializerOptions.Converters</c> whenever an entity id is
    /// serialised directly, so it is rendered as a bare string rather than an object. Deserialization accepts
    /// only JSON strings; any other token type or an unparseable GUID fails the read.</remarks>
    public class EntityIdJsonConverter : JsonConverter<EntityId>
    {
        /// <summary>
        /// Reads an <see cref="EntityId"/> from a JSON string token.
        /// </summary>
        /// <param name="reader">The reader positioned at the token to convert.</param>
        /// <param name="typeToConvert">The declared type being converted; always <see cref="EntityId"/>.</param>
        /// <param name="options">The serializer options in effect, unused by this converter.</param>
        /// <returns>The <see cref="EntityId"/> wrapping the parsed GUID.</returns>
        /// <exception cref="JsonException">The current token is not a JSON string, or the string is not a
        /// valid GUID.</exception>
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

        /// <summary>
        /// Writes the underlying GUID of an <see cref="EntityId"/> as a JSON string.
        /// </summary>
        /// <param name="writer">The writer to write the string value to.</param>
        /// <param name="value">The entity id whose GUID is written.</param>
        /// <param name="options">The serializer options in effect, unused by this converter.</param>
        public override void Write(
            Utf8JsonWriter writer,
            EntityId value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Id);
        }
    }
}