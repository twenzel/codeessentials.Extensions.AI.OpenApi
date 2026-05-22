using System.Text.Json;
using System.Text.Json.Serialization;

namespace codeessentials.Extensions.AI.OpenApi.Model;

public class KernelJsonSchema
{
	/// <summary>The schema stored as a string.</summary>
	private string? _schemaAsString;

	/// <summary>Parses a JSON Schema for a parameter type.</summary>
	/// <param name="jsonSchema">The JSON Schema as a string.</param>
	/// <returns>A parsed <see cref="KernelJsonSchema"/>.</returns>
	/// <exception cref="ArgumentException"><paramref name="jsonSchema"/> is null.</exception>
	/// <exception cref="JsonException">The JSON is invalid.</exception>
	public static KernelJsonSchema Parse(string jsonSchema) =>
		new(JsonElement.Parse(jsonSchema));

	/// <summary>Initializes a new instance from the specified <see cref="JsonElement"/>.</summary>
	/// <param name="jsonSchema">The schema to be stored.</param>
	/// <remarks>
	/// The <paramref name="jsonSchema"/> is not validated, which is why this constructor is internal.
	/// All callers must ensure JSON Schema validity.
	/// </remarks>
	internal KernelJsonSchema(JsonElement jsonSchema) =>
		RootElement = jsonSchema;

	/// <summary>Gets a <see cref="JsonElement"/> representing the root element of the schema.</summary>
	public JsonElement RootElement { get; }

	/// <summary>Gets the JSON Schema as a string.</summary>
	public override string ToString() => _schemaAsString ??= JsonSerializer.Serialize(RootElement);

	public sealed class JsonConverter : JsonConverter<KernelJsonSchema>
	{
		/// <inheritdoc/>
		public override KernelJsonSchema? Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
			new(JsonElement.ParseValue(ref reader));

		/// <inheritdoc/>
		public override void Write(Utf8JsonWriter writer, KernelJsonSchema value, JsonSerializerOptions options) =>
			value.RootElement.WriteTo(writer);
	}
}
