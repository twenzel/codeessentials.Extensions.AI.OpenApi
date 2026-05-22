using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using codeessentials.Extensions.AI.OpenApi.Model;
using Json.More;
using Json.Schema;
using Microsoft.OpenApi;

namespace codeessentials.Extensions.AI.OpenApi.Serialization;

/// <summary>
/// Provides functionality for converting OpenApi types - https://swagger.io/docs/specification/data-models/data-types/
/// </summary>
internal static class OpenApiTypeConverter
{
	/// <summary>
	/// Converts the given parameter argument to a JsonNode based on the specified type or schema.
	/// </summary>
	/// <param name="name">The parameter name.</param>
	/// <param name="type">The parameter type.</param>
	/// <param name="argument">The argument to be converted.</param>
	/// <param name="schema">The parameter schema.</param>
	/// <returns>A JsonNode representing the converted value.</returns>
	public static JsonNode Convert(string name, JsonSchemaType type, object argument, KernelJsonSchema? schema = null)
	{
		ArgumentNullException.ThrowIfNull(argument);

		try
		{
			var node = type switch
			{
				JsonSchemaType.String => JsonValue.Create(argument),
				JsonSchemaType.Array => argument switch
				{
					string s => JsonArray.Parse(s) as JsonArray,
					JsonElement jsonElement when jsonElement.ValueKind == JsonValueKind.Array => jsonElement.AsNode(),
					_ => JsonSerializer.SerializeToNode(argument) as JsonArray
				},
				JsonSchemaType.Integer => argument switch
				{
					string stringArgument => JsonValue.Create(long.Parse(stringArgument, CultureInfo.InvariantCulture)),
					byte or sbyte or short or ushort or int or uint or long or ulong => JsonValue.Create(argument),
					JsonElement jsonElement when jsonElement.TryGetInt64(out var intValue) => JsonValue.Create(intValue),
					_ => null
				},
				JsonSchemaType.Boolean => argument switch
				{
					bool b => JsonValue.Create(b),
					string s => JsonValue.Create(bool.Parse(s)),
					JsonElement jsonElement when jsonElement.ValueKind is JsonValueKind.True or JsonValueKind.False => jsonElement.AsNode(),
					_ => null
				},
				JsonSchemaType.Number => argument switch
				{
					string stringArgument when long.TryParse(stringArgument, out var intValue) => JsonValue.Create(intValue),
					string stringArgument when double.TryParse(stringArgument, out var doubleValue) => JsonValue.Create(doubleValue),
					byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal => JsonValue.Create(argument),
					JsonElement jsonElement when jsonElement.TryGetInt64(out var intValue) => JsonValue.Create(intValue),
					JsonElement jsonElement when jsonElement.TryGetDouble(out var doubleValue) => JsonValue.Create(doubleValue),
					_ => null
				},
				_ => schema is null
					? JsonSerializer.SerializeToNode(argument)
					: ValidateSchemaAndConvert(name, schema, argument)
			};

			return node ?? throw new ArgumentOutOfRangeException(name, argument, $"Argument type '{argument.GetType()}' is not convertible to parameter type '{type}'.");
		}
		catch (ArgumentException ex)
		{
			throw new ArgumentOutOfRangeException(name, argument, ex.Message);
		}
		catch (FormatException ex)
		{
			throw new ArgumentOutOfRangeException(name, argument, ex.Message);
		}
	}

	/// <summary>
	/// Validates the argument against the parameter schema and converts it to a JsonNode if valid.
	/// </summary>
	/// <param name="parameterName">The parameter name.</param>
	/// <param name="parameterSchema">The parameter schema.</param>
	/// <param name="argument">The argument to be validated and converted.</param>
	/// <returns>A JsonNode representing the converted value.</returns>
	private static JsonNode? ValidateSchemaAndConvert(string parameterName, KernelJsonSchema parameterSchema, object argument)
	{
		var jsonSchema = JsonSchema.FromText(JsonSerializer.Serialize(parameterSchema));

		var node = JsonSerializer.SerializeToNode(argument);

		if (jsonSchema.Evaluate(node.ToJsonDocument().RootElement).IsValid)
		{
			return node;
		}

		throw new ArgumentOutOfRangeException(parameterName, argument, $"Argument type '{argument.GetType()}' does not match the schema.");
	}
}