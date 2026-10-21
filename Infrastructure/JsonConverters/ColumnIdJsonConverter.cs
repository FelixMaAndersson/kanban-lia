using System.Text.Json;
using System.Text.Json.Serialization;

using kanban_lia.Models.Domain.Columns;

namespace kanban_lia.Infrastructure.JsonConverters
{
    /// <summary>
    /// Converts <see cref="ColumnId"/> values to and from their GUID representation in JSON.
    /// </summary>
    /// <remarks>Registered on the HTTP JSON options so column ids travel over the API as plain GUID strings rather
    /// than as objects. Reading is strict: anything that is not a string, or a string that is not a GUID, is rejected
    /// instead of being coerced.</remarks>
    public class ColumnIdJsonConverter : JsonConverter<ColumnId>
    {
        /// <summary>
        /// Reads a <see cref="ColumnId"/> from the JSON string under the reader.
        /// </summary>
        /// <param name="reader">Reader positioned at the token to convert.</param>
        /// <param name="typeToConvert">The type being converted; always <see cref="ColumnId"/>.</param>
        /// <param name="options">The serializer options in effect; unused.</param>
        /// <returns>The <see cref="ColumnId"/> wrapping the parsed GUID.</returns>
        /// <exception cref="JsonException">The current token is not a string, or its value is not a valid GUID.</exception>
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

        /// <summary>
        /// Writes a <see cref="ColumnId"/> as the GUID string held by its <c>Id</c> value.
        /// </summary>
        /// <param name="writer">Writer the value is written to.</param>
        /// <param name="value">The column id to serialize.</param>
        /// <param name="options">The serializer options in effect; unused.</param>
        public override void Write(
            Utf8JsonWriter writer,
            ColumnId value,
            JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.Id);
        }
    }
}