using System.Text.Json;
using System.Text.Json.Serialization;

using kanban_lia.Models.Domain.Boards;

namespace kanban_lia.Infrastructure.JsonConverters
{
    /// <summary>
    /// Converts <see cref="BoardId"/> values to and from their Guid representation in JSON.
    /// </summary>
    /// <remarks>Serializes a board identifier as a plain JSON string (the Guid's text form) instead of the record's
    /// default object shape, and parses such strings back into a <see cref="BoardId"/> wrapper.</remarks>
    public class BoardIdJsonConverter : JsonConverter<BoardId>
    {
        /// <summary>
        /// Reads a <see cref="BoardId"/> from a JSON string containing a Guid.
        /// </summary>
        /// <param name="reader">The reader positioned at the JSON token to convert.</param>
        /// <param name="typeToConvert">The declared type being converted, always <see cref="BoardId"/>.</param>
        /// <param name="options">The serializer options in effect; unused by this converter.</param>
        /// <returns>The <see cref="BoardId"/> parsed from the JSON string.</returns>
        /// <exception cref="JsonException">The current token is not a string, or the string is not a valid Guid.
        /// </exception>
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

        /// <summary>
        /// Writes the Guid held by a <see cref="BoardId"/> as a JSON string.
        /// </summary>
        /// <param name="writer">The writer that receives the JSON string value.</param>
        /// <param name="value">The board identifier to serialize.</param>
        /// <param name="options">The serializer options in effect; unused by this converter.</param>
        public override void Write(
            Utf8JsonWriter writer,
            BoardId value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Id);
        }
    }
}