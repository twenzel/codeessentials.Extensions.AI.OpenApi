using System.Text.Json;
using System.Text.Json.Nodes;
using codeessentials.Extensions.AI.OpenApi.Model;
using Microsoft.Extensions.AI;

namespace codeessentials.Extensions.AI.OpenApi;

internal static class AIFunctionJsonHelper
{
	const string DESCRIPTION_PROPERTY_NAME = "description";

	/// <summary>
	/// Determines a JSON schema for the provided OpenApi operation.
	/// </summary>
	/// <param name="operation">The operation from which to extract schema information.</param>
	/// <param name="title">The title keyword used by the method schema.</param>
	/// <param name="description">The description keyword used by the method schema.</param>
	/// <param name="serializerOptions">The options used to extract the schema from the specified type.</param>
	/// <param name="inferenceOptions">The options controlling schema creation.</param>
	/// <returns>A JSON schema document encoded as a <see cref="T:System.Text.Json.JsonElement" />.</returns>
	/// <exception cref="T:System.ArgumentNullException"><paramref name="method" /> is <see langword="null" />.</exception>
	public static JsonElement CreateFunctionJsonSchema(RestApiOperation operation, IEnumerable<RestApiParameter> parameters, string? title = null, string? description = null, JsonSerializerOptions? serializerOptions = null, AIJsonSchemaCreateOptions? inferenceOptions = null)
	{
		serializerOptions ??= AIJsonUtilities.DefaultOptions;
		inferenceOptions ??= AIJsonSchemaCreateOptions.Default;

		title ??= operation.Id;
		description ??= operation.Description;

		var jsonObject = new JsonObject();
		JsonArray? jsonArray = null;

		foreach (var parameterInfo in parameters)
		{
			if (parameterInfo.Location != RestApiParameterLocation.Body)
				continue;

			if (string.IsNullOrWhiteSpace(parameterInfo.Name))
				throw new ArgumentException("Parameter is missing a name.");

			var value = GetSchema(parameterInfo);
			jsonObject.Add(parameterInfo.Name, value);

			if (parameterInfo.IsRequired)
			{
				(jsonArray ??= []).Add((JsonNode?)parameterInfo.Name);
			}

		}
		JsonNode jsonNode = new JsonObject();
		if (inferenceOptions.IncludeSchemaKeyword)
			jsonNode["$schema"] = "https://json-schema.org/draft/2020-12/schema";

		if (!string.IsNullOrWhiteSpace(title))
			jsonNode["title"] = title;

		if (!string.IsNullOrWhiteSpace(description))
			jsonNode["description"] = description;

		jsonNode["type"] = "object";
		jsonNode["properties"] = jsonObject;
		if (jsonArray != null)
			jsonNode["required"] = jsonArray;

		//if (inferenceOptions.TransformOptions != null)
		//	jsonNode = TransformSchema(jsonNode, inferenceOptions.TransformOptions);

		return JsonSerializer.SerializeToElement(jsonNode, serializerOptions);
	}

	private static JsonNode GetSchema(RestApiParameter p)
	{
		var schema = p.Schema ?? KernelJsonSchema.Parse($$"""{"type":"{{p.Type}}"}""");

		return JsonNode.Parse(schema.RootElement.GetRawText()) ?? throw new InvalidOperationException("Coould not convert JsonElement to JsonNode");
	}

	///// <summary>Creates a JSON schema for the specified type.</summary>
	///// <param name="type">The type for which to generate the schema.</param>
	///// <param name="description">The description of the parameter.</param>
	///// <param name="hasDefaultValue"><see langword="true" /> if the parameter is optional; otherwise, <see langword="false" />.</param>
	///// <param name="defaultValue">The default value of the optional parameter, if applicable.</param>
	///// <param name="serializerOptions">The options used to extract the schema from the specified type.</param>
	///// <param name="inferenceOptions">The options controlling schema creation.</param>
	///// <returns>A <see cref="T:System.Text.Json.JsonElement" /> representing the schema.</returns>
	//public static JsonElement CreateJsonSchema(JsonSchemaType? type, string? description = null, bool hasDefaultValue = false, object? defaultValue = null, JsonSerializerOptions? serializerOptions = null, AIJsonSchemaCreateOptions? inferenceOptions = null)
	//{
	//	return AIJsonUtilities.CreateJsonSchema(GetNetType(type), description, hasDefaultValue, defaultValue, serializerOptions, inferenceOptions);
	//}

	//private static JsonNode TransformSchema(JsonNode? schema, AIJsonSchemaTransformOptions transformOptions)
	//{
	//	var path = (transformOptions.TransformSchemaNode != null) ? new List<string>() : null;
	//	return TransformSchemaCore(schema, transformOptions, path);
	//}

	//private static JsonNode TransformSchemaCore(JsonNode? schema, AIJsonSchemaTransformOptions transformOptions, List<string>? path)
	//{
	//	switch (schema?.GetValueKind())
	//	{
	//		case JsonValueKind.False:
	//			if (transformOptions.ConvertBooleanSchemas)
	//			{
	//				schema = new JsonObject { ["not"] = true };
	//			}
	//			break;
	//		case JsonValueKind.True:
	//			if (transformOptions.ConvertBooleanSchemas)
	//			{
	//				schema = new JsonObject();
	//			}
	//			break;
	//		case JsonValueKind.Object:
	//			{
	//				JsonObject jsonObject = (JsonObject)schema;
	//				JsonObject jsonObject2 = null;
	//				if (jsonObject.TryGetPropertyValue("properties", out JsonNode jsonNode) && jsonNode is JsonObject jsonObject3)
	//				{
	//					jsonObject2 = jsonObject3;
	//					path?.Add("properties");
	//					KeyValuePair<string, JsonNode>[] array = jsonObject2.ToArray();
	//					for (int i = 0; i < array.Length; i++)
	//					{
	//						KeyValuePair<string, JsonNode> keyValuePair = array[i];
	//						path?.Add(keyValuePair.Key);
	//						jsonObject2[keyValuePair.Key] = TransformSchemaCore(keyValuePair.Value, transformOptions, path);
	//						path?.RemoveAt(path.Count - 1);
	//					}
	//					path?.RemoveAt(path.Count - 1);
	//				}
	//				if (jsonObject.TryGetPropertyValue("items", out JsonNode jsonNode2))
	//				{
	//					path?.Add("items");
	//					jsonObject["items"] = TransformSchemaCore(jsonNode2, transformOptions, path);
	//					path?.RemoveAt(path.Count - 1);
	//				}
	//				if (jsonObject.TryGetPropertyValue("additionalProperties", out JsonNode jsonNode3))
	//				{
	//					JsonValueKind? jsonValueKind = jsonNode3?.GetValueKind();
	//					if (!jsonValueKind.HasValue || jsonValueKind != JsonValueKind.False)
	//					{
	//						path?.Add("additionalProperties");
	//						jsonObject["additionalProperties"] = TransformSchemaCore(jsonNode3, transformOptions, path);
	//						path?.RemoveAt(path.Count - 1);
	//					}
	//				}
	//				if (jsonObject.TryGetPropertyValue("not", out JsonNode jsonNode4))
	//				{
	//					path?.Add("not");
	//					jsonObject["not"] = TransformSchemaCore(jsonNode4, transformOptions, path);
	//					path?.RemoveAt(path.Count - 1);
	//				}
	//				string[] buffer = ["anyOf", "oneOf", "allOf"];
	//				ReadOnlySpan<string> readOnlySpan = buffer;
	//				ReadOnlySpan<string> readOnlySpan2 = readOnlySpan;
	//				for (int j = 0; j < readOnlySpan2.Length; j++)
	//				{
	//					string text = readOnlySpan2[j];
	//					if (!jsonObject.TryGetPropertyValue(text, out JsonNode jsonNode5) || !(jsonNode5 is JsonArray jsonArray))
	//					{
	//						continue;
	//					}
	//					path?.Add(text);
	//					for (int k = 0; k < jsonArray.Count; k++)
	//					{
	//						path?.Add($"[{k}]");
	//						JsonNode jsonNode6 = TransformSchemaCore(jsonArray[k], transformOptions, path);
	//						if (jsonNode6 != jsonArray[k])
	//						{
	//							jsonArray[k] = jsonNode6;
	//						}
	//						path?.RemoveAt(path.Count - 1);
	//					}
	//					path?.RemoveAt(path.Count - 1);
	//				}
	//				if (transformOptions.DisallowAdditionalProperties && jsonObject2 != null && !jsonObject.ContainsKey("additionalProperties"))
	//				{
	//					jsonObject["additionalProperties"] = false;
	//				}
	//				if (transformOptions.RequireAllProperties && jsonObject2 != null)
	//				{
	//					JsonArray jsonArray2 = new JsonArray();
	//					foreach (KeyValuePair<string, JsonNode> item in jsonObject2)
	//					{
	//						jsonArray2.Add((JsonNode?)item.Key);
	//					}
	//					jsonObject["required"] = jsonArray2;
	//				}
	//				if (transformOptions.UseNullableKeyword && jsonObject.TryGetPropertyValue("type", out JsonNode jsonNode7) && jsonNode7 is JsonArray jsonArray3)
	//				{
	//					bool flag = false;
	//					string text2 = null;
	//					foreach (JsonNode item2 in jsonArray3)
	//					{
	//						string text3 = (string?)item2;
	//						if (text3 == "null")
	//						{
	//							flag = true;
	//							continue;
	//						}
	//						if (text2 != null)
	//						{
	//							text2 = null;
	//							break;
	//						}
	//						text2 = text3;
	//					}
	//					if (flag && text2 != null)
	//					{
	//						jsonObject["type"] = text2;
	//						jsonObject["nullable"] = true;
	//					}
	//				}
	//				if (transformOptions.MoveDefaultKeywordToDescription && jsonObject.TryGetPropertyValue("default", out JsonNode jsonNode8))
	//				{
	//					string text4 = ((!jsonObject.TryGetPropertyValue("description", out JsonNode jsonNode9)) ? null : jsonNode9?.GetValue<string>());
	//					string text5 = JsonSerializer.Serialize(jsonNode8);
	//					text4 = ((text4 == null) ? ("Default value: " + text5) : (text4 + " (Default value: " + text5 + ")"));
	//					jsonObject["description"] = text4;
	//					jsonObject.Remove("default");
	//				}
	//				break;
	//			}
	//		default:
	//			throw new ArgumentException("Schema must be an object or a boolean value.");

	//	}

	//	Func<AIJsonSchemaTransformContext, JsonNode, JsonNode> transformSchemaNode = transformOptions.TransformSchemaNode;
	//	if (transformSchemaNode != null)
	//	{
	//		//schema = transformSchemaNode(new AIJsonSchemaTransformContext(path.ToArray()), schema);
	//	}
	//	return schema;
	//}

	//private static Type? GetNetType(JsonSchemaType? type)
	//{
	//	if (type == null)
	//		return null;

	//	return type switch
	//	{
	//		JsonSchemaType.Null => typeof(void),
	//		JsonSchemaType.Boolean => typeof(bool),
	//		JsonSchemaType.Integer => typeof(int),
	//		JsonSchemaType.Number => typeof(decimal),
	//		JsonSchemaType.String => typeof(string),
	//		JsonSchemaType.Object => typeof(object),
	//		JsonSchemaType.Array => typeof(Array),
	//		_ => throw new NotImplementedException(),
	//	};
	//}
}
