using System.Globalization;
using System.Text;
using codeessentials.Extensions.AI.OpenApi.Model;
using Microsoft.OpenApi;

namespace codeessentials.Extensions.AI.OpenApi;

internal static class OpenApiSchemaExtensions
{
	/// <summary>
	/// Gets a JSON serialized representation of an <see cref="OpenApiSchema"/>
	/// </summary>
	/// <param name="schema">The schema.</param>
	/// <returns>An instance of <see cref="KernelJsonSchema"/> that contains the JSON Schema.</returns>
	internal static KernelJsonSchema ToJsonSchema(this IOpenApiSchema schema)
	{
		var schemaBuilder = new StringBuilder();
		var jsonWriter = new OpenApiJsonWriter(new StringWriter(schemaBuilder, CultureInfo.InvariantCulture));
		jsonWriter.Settings.InlineLocalReferences = true;
		schema.SerializeAsV3(jsonWriter);
		return KernelJsonSchema.Parse(schemaBuilder.ToString());
	}
}
