using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using codeessentials.Extensions.AI.OpenApi.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.OpenApi;
using Microsoft.OpenApi.Reader;

namespace codeessentials.Extensions.AI.OpenApi;

/// <summary>
/// Parser for OpenAPI documents.
/// </summary>
public sealed class OpenApiDocumentParser(ILoggerFactory? loggerFactory = null)
{
	private readonly ILogger _logger = loggerFactory?.CreateLogger(typeof(OpenApiDocumentParser)) ?? NullLogger.Instance;
	/// <summary>
	/// Max depth to traverse down OpenAPI schema to discover payload properties.
	/// </summary>
	private const int PAYLOAD_PROPERTIES_HIERARCHY_MAX_DEPTH = 10;

	private static readonly ParameterNameAndLocationComparer s_parameterNameAndLocationComparer = new();

	/// <summary>
	/// List of supported Media Types.
	/// </summary>
	private static readonly List<string> s_supportedMediaTypes =
	[
		"application/json",
		"text/plain"
	];

	/// <summary>
	/// Parses OpenAPI document.
	/// </summary>
	/// <param name="stream">Stream containing OpenAPI document to parse.</param>
	/// <param name="options">Options for parsing OpenAPI document.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>Specification of the REST API.</returns>
	public async Task<RestApiSpecification> ParseAsync(Stream stream, OpenApiDocumentParserOptions? options = null, CancellationToken cancellationToken = default)
	{
		(var openApiDocument, var diagnostic) = await OpenApiDocument.LoadAsync(stream, format: "json", cancellationToken: cancellationToken).ConfigureAwait(false);

		HandleLoadingErrors(openApiDocument, diagnostic, options?.IgnoreNonCompliantErrors ?? false);

		_logger.LogDebug("Successfully read api documentation with specification version '{SpecificationVersion}'", diagnostic!.SpecificationVersion);

		return new(
			ExtractRestApiInfo(openApiDocument!),
			CreateRestApiOperationSecurityRequirements(openApiDocument!.Security),
			ExtractRestApiOperations(openApiDocument, options, _logger));
	}

	private void HandleLoadingErrors(OpenApiDocument? openApiDocument, OpenApiDiagnostic? diagnostic, bool ignoreNonCompliantErrors)
	{
		if (diagnostic != null && diagnostic.Errors.Count > 0)
		{
			var title = openApiDocument?.Info?.Title;
			var errors = string.Join(";", diagnostic.Errors);

			if (!ignoreNonCompliantErrors)
			{
				var exception = new OpenApiException($"Parsing of '{title}' OpenAPI document complete with the following errors: {errors}");
				_logger.LogError(exception, "Parsing of '{Title}' OpenAPI document complete with the following errors: {Errors}", title, errors);
				throw exception;
			}

			_logger.LogWarning("Parsing of '{Title}' OpenAPI document complete with the following errors: {Errors}", title, errors);
		}

		if (openApiDocument == null)
		{
			var exception = new OpenApiException("Could not parse OpenAPI document.");
			_logger.LogError(exception, "Could not parse OpenAPI document.");
			throw exception;
		}
	}

	/// <summary>
	/// Parses an OpenAPI document and extracts REST API information.
	/// </summary>
	/// <param name="document">The OpenAPI document.</param>
	/// <returns>Rest API information.</returns>
	internal static RestApiInfo ExtractRestApiInfo(OpenApiDocument document)
	{
		return new()
		{
			Title = document.Info.Title,
			Description = document.Info.Description,
			Version = document.Info.Version,
		};
	}

	/// <summary>
	/// Build a list of <see cref="RestApiSecurityRequirement"/> objects from the given <see cref="OpenApiSecurityRequirement"/> objects.
	/// </summary>
	/// <param name="security">The REST API security.</param>
	internal static List<RestApiSecurityRequirement> CreateRestApiOperationSecurityRequirements(IList<OpenApiSecurityRequirement>? security)
	{
		var operationRequirements = new List<RestApiSecurityRequirement>();

		if (security is not null)
		{
			foreach (var item in security)
			{
				foreach (var keyValuePair in item)
				{
					if (keyValuePair.Key is not OpenApiSecuritySchemeReference openApiSecurityScheme)
					{
						throw new OpenApiException("The security scheme is not supported.");
					}

					operationRequirements.Add(new RestApiSecurityRequirement(new Dictionary<RestApiSecurityScheme, IList<string>> { { CreateRestApiSecurityScheme(openApiSecurityScheme), keyValuePair.Value } }));
				}
			}
		}

		return operationRequirements;
	}

	/// <summary>
	/// Build a <see cref="RestApiSecurityScheme"/> objects from the given <see cref="OpenApiSecurityScheme"/> object.
	/// </summary>
	/// <param name="securityScheme">The REST API security scheme.</param>
	private static RestApiSecurityScheme CreateRestApiSecurityScheme(OpenApiSecuritySchemeReference securityScheme)
	{
		var location = RestApiParameterLocation.Header;

		if (securityScheme.In != null)
			location = Enum.Parse<RestApiParameterLocation>(securityScheme.In.ToString()!);

		return new RestApiSecurityScheme()
		{
			SecuritySchemeType = (securityScheme.Type ?? SecuritySchemeType.Http).ToString(),
			Description = securityScheme.Description,
			Name = ((securityScheme.Name ?? securityScheme.Scheme) ?? securityScheme.Type.ToString()) ?? throw new OpenApiException("No name for security scheme defined"),
			In = location,
			Scheme = securityScheme.Scheme ?? throw new OpenApiException("No scheme for security scheme defined"),
			BearerFormat = securityScheme.BearerFormat,
			Flows = CreateRestApiOAuthFlows(securityScheme.Flows),
			OpenIdConnectUrl = securityScheme.OpenIdConnectUrl
		};
	}

	/// <summary>
	/// Build a <see cref="RestApiOAuthFlows"/> object from the given <see cref="OpenApiOAuthFlows"/> object.
	/// </summary>
	/// <param name="flows">The REST API OAuth flows.</param>
	private static RestApiOAuthFlows? CreateRestApiOAuthFlows(OpenApiOAuthFlows? flows)
	{
		return flows is not null ? new RestApiOAuthFlows()
		{
			Implicit = CreateRestApiOAuthFlow(flows.Implicit),
			Password = CreateRestApiOAuthFlow(flows.Password),
			ClientCredentials = CreateRestApiOAuthFlow(flows.ClientCredentials),
			AuthorizationCode = CreateRestApiOAuthFlow(flows.AuthorizationCode),
		} : null;
	}

	/// <summary>
	/// Build a <see cref="RestApiOAuthFlow"/> object from the given <see cref="OpenApiOAuthFlow"/> object.
	/// </summary>
	/// <param name="flow">The REST API OAuth flow.</param>
	private static RestApiOAuthFlow? CreateRestApiOAuthFlow(OpenApiOAuthFlow? flow)
	{
		return flow is not null ? new RestApiOAuthFlow()
		{
			AuthorizationUrl = flow.AuthorizationUrl,
			TokenUrl = flow.TokenUrl,
			RefreshUrl = flow.RefreshUrl,
			Scopes = new ReadOnlyDictionary<string, string>(flow.Scopes ?? new Dictionary<string, string>())
		} : null;
	}

	/// <summary>
	/// Parses an OpenAPI document and extracts REST API operations.
	/// </summary>
	/// <param name="document">The OpenAPI document.</param>
	/// <param name="options">Options for parsing OpenAPI document.</param>
	/// <param name="logger">Used to perform logging.</param>
	/// <returns>List of Rest operations.</returns>
	private static List<RestApiOperation> ExtractRestApiOperations(OpenApiDocument document, OpenApiDocumentParserOptions? options, ILogger logger)
	{
		var result = new List<RestApiOperation>();

		foreach (var pathPair in document.Paths)
		{
			var operations = CreateRestApiOperations(document, pathPair.Key, pathPair.Value, options, logger);
			result.AddRange(operations);
		}

		return result;
	}

	/// <summary>
	/// Creates REST API operation.
	/// </summary>
	/// <param name="document">The OpenAPI document.</param>
	/// <param name="path">Rest resource path.</param>
	/// <param name="pathItem">Rest resource metadata.</param>
	/// <param name="options">Options for parsing OpenAPI document.</param>
	/// <param name="logger">Used to perform logging.</param>
	/// <returns>Rest operation.</returns>
	internal static List<RestApiOperation> CreateRestApiOperations(OpenApiDocument document, string path, IOpenApiPathItem pathItem, OpenApiDocumentParserOptions? options, ILogger logger)
	{
		try
		{
			var operations = new List<RestApiOperation>();
			var globalServers = CreateRestApiOperationServers(document.Servers);
			var pathServers = CreateRestApiOperationServers(pathItem.Servers);

			foreach (var operationPair in pathItem.Operations)
			{
				var method = operationPair.Key.ToString();
				var operationItem = operationPair.Value;
				var operationServers = CreateRestApiOperationServers(operationItem.Servers);

				// Skip the operation parsing and don't add it to the result operations list if it's explicitly excluded by the predicate.
				if (!options?.OperationSelectionPredicate?.Invoke(new OperationSelectionPredicateContext(operationItem.OperationId, path, method, operationItem.Description)) ?? false)
				{
					continue;
				}

				try
				{
					IEnumerable<IOpenApiParameter>? parameters = operationItem.Parameters;

					if (parameters != null && pathItem.Parameters != null)
						parameters = parameters.Union(pathItem.Parameters, s_parameterNameAndLocationComparer);
					else if (pathItem.Parameters != null)
						parameters = pathItem.Parameters;

					var operation = new RestApiOperation(
						id: operationItem.OperationId,
						servers: globalServers,
						pathServers: pathServers,
						operationServers: operationServers,
						path: path,
						method: new HttpMethod(method),
						description: string.IsNullOrEmpty(operationItem.Description) ? operationItem.Summary : operationItem.Description,
						parameters: CreateRestApiOperationParameters(operationItem.OperationId, parameters),
						payload: CreateRestApiOperationPayload(operationItem.OperationId, operationItem.RequestBody),
						responses: CreateRestApiOperationExpectedResponses(operationItem.Responses).ToDictionary(static item => item.Item1, static item => item.Item2),
						securityRequirements: CreateRestApiOperationSecurityRequirements(operationItem.Security)
					)
					{
						Extensions = CreateRestApiOperationExtensions(operationItem.Extensions, logger),
						Summary = operationItem.Summary
					};

					operations.Add(operation);
				}
				catch (OpenApiException ke)
				{
					logger.LogWarning(ke, "Error occurred creating REST API operation for {OperationId}. Operation will be ignored.", operationItem.OperationId);
				}
			}

			return operations;
		}
		catch (Exception ex)
		{
			logger.LogError(ex, "Fatal error occurred during REST API operation creation.");
			throw;
		}
	}

	/// <summary>
	/// Build a list of <see cref="RestApiServer"/> objects from the given list of <see cref="OpenApiServer"/> objects.
	/// </summary>
	/// <param name="servers">Represents servers which hosts the REST API.</param>
	private static List<RestApiServer> CreateRestApiOperationServers(IList<OpenApiServer> servers)
	{
		if (servers == null || servers.Count == 0)
		{
			return [];
		}

		var result = new List<RestApiServer>(servers.Count);
		foreach (var server in servers)
		{
			var variables = server.Variables?.ToDictionary(item => item.Key, item => new RestApiServerVariable(item.Value.Default, item.Value.Description, item.Value.Enum));
			result.Add(new RestApiServer(server.Url, variables, server.Description));
		}

		return result;
	}

	/// <summary>
	/// Creates REST API parameters.
	/// </summary>
	/// <param name="operationId">The operation id.</param>
	/// <param name="parameters">The OpenAPI parameters.</param>
	/// <returns>The parameters.</returns>
	private static List<RestApiParameter> CreateRestApiOperationParameters(string operationId, IEnumerable<IOpenApiParameter>? parameters)
	{
		if (parameters == null)
			return [];

		var result = new List<RestApiParameter>();

		foreach (var parameter in parameters)
		{
			if (parameter.In is null)
			{
				throw new OpenApiException($"Parameter location of {parameter.Name} parameter of {operationId} operation is undefined.");
			}

			if (parameter.Style is null)
			{
				throw new OpenApiException($"Parameter style of {parameter.Name} parameter of {operationId} operation is undefined.");
			}

			var restParameter = new RestApiParameter(
				parameter.Name,
				parameter.Schema.Type ?? JsonSchemaType.Null,
				parameter.Required,
				parameter.Explode,
				(RestApiParameterLocation)Enum.Parse(typeof(RestApiParameterLocation), parameter.In.ToString()!),
				(RestApiParameterStyle)Enum.Parse(typeof(RestApiParameterStyle), parameter.Style.ToString()!),
				parameter.Schema.Items?.Type,
				GetParameterValue(parameter.Schema.Default, "parameter", parameter.Name),
				parameter.Description,
				parameter.Schema.Format,
				parameter.Schema.ToJsonSchema()
			);

			result.Add(restParameter);
		}

		return result;
	}

	/// <summary>
	/// Creates REST API payload.
	/// </summary>
	/// <param name="operationId">The operation id.</param>
	/// <param name="requestBody">The OpenAPI request body.</param>
	/// <returns>The REST API payload.</returns>
	private static RestApiPayload? CreateRestApiOperationPayload(string operationId, IOpenApiRequestBody requestBody)
	{
		if (requestBody?.Content is null)
		{
			return null;
		}

		var mediaType = GetMediaType(requestBody.Content) ?? throw new OpenApiException($"Neither of the media types of {operationId} is supported.");
		var mediaTypeMetadata = requestBody.Content[mediaType];

		var payloadProperties = GetPayloadProperties(operationId, mediaTypeMetadata.Schema);

		return new RestApiPayload(mediaType, payloadProperties, requestBody.Description, mediaTypeMetadata?.Schema?.ToJsonSchema());
	}

	/// <summary>
	/// Returns the first supported media type. If none of the media types are supported, an exception is thrown.
	/// </summary>
	/// <remarks>
	/// Handles the case when the media type contains additional parameters e.g. application/json; x-api-version=2.0.
	/// </remarks>
	/// <param name="content">The OpenAPI request body content.</param>
	/// <returns>The first support ed media type.</returns>
	/// <exception cref="KernelException"></exception>
	private static string? GetMediaType(IDictionary<string, OpenApiMediaType>? content)
	{
		if (content == null)
			return null;

		foreach (var mediaType in s_supportedMediaTypes)
		{
			foreach (var key in content.Keys)
			{
				var keyParts = key.Split(';');
				if (keyParts[0].Equals(mediaType, StringComparison.OrdinalIgnoreCase))
				{
					return key;
				}
			}
		}
		return null;
	}

	/// <summary>
	/// Create collection of expected responses for the REST API operation for the supported media types.
	/// </summary>
	/// <param name="responses">Responses from the OpenAPI endpoint.</param>
	private static IEnumerable<(string, RestApiExpectedResponse)> CreateRestApiOperationExpectedResponses(OpenApiResponses responses)
	{
		foreach (var response in responses)
		{
			var mediaType = GetMediaType(response.Value.Content);
			if (mediaType is not null)
			{
				var matchingSchema = response.Value.Content[mediaType].Schema;
				var description = response.Value.Description ?? matchingSchema?.Description ?? string.Empty;

				yield return (response.Key, new RestApiExpectedResponse(description, mediaType, matchingSchema?.ToJsonSchema()));
			}
		}
	}

	/// <summary>
	/// Returns REST API payload properties.
	/// </summary>
	/// <param name="operationId">The operation id.</param>
	/// <param name="schema">An OpenAPI document schema representing request body properties.</param>
	/// <param name="level">Current level in OpenAPI schema.</param>
	/// <returns>The REST API payload properties.</returns>
	private static List<RestApiPayloadProperty> GetPayloadProperties(string operationId, IOpenApiSchema? schema, int level = 0)
	{
		if (schema is null || schema.Properties == null)
			return [];

		if (level > PAYLOAD_PROPERTIES_HIERARCHY_MAX_DEPTH)
		{
			throw new OpenApiException($"Max level {PAYLOAD_PROPERTIES_HIERARCHY_MAX_DEPTH} of traversing payload properties of {operationId} operation is exceeded.");
		}

		var result = new List<RestApiPayloadProperty>();

		foreach (var propertyPair in schema.Properties)
		{
			var propertyName = propertyPair.Key;

			var propertySchema = propertyPair.Value;

			var property = new RestApiPayloadProperty(
				propertyName,
				propertySchema.Type ?? JsonSchemaType.Null,
				schema.Required?.Contains(propertyName) ?? false,
				GetPayloadProperties(operationId, propertySchema, level + 1),
				propertySchema.Description,
				propertySchema.Format,
				propertySchema.ToJsonSchema(),
				GetParameterValue(propertySchema.Default, "payload property", propertyName));

			result.Add(property);
		}

		return result;
	}

	/// <summary>
	/// Returns parameter value.
	/// </summary>
	/// <param name="valueMetadata">The value metadata.</param>
	/// <param name="entityDescription">A description of the type of entity we are trying to get a value for.</param>
	/// <param name="entityName">The name of the entity that we are trying to get the value for.</param>
	/// <returns>The parameter value.</returns>
	private static object? GetParameterValue(JsonNode? valueMetadata, string entityDescription, string entityName)
	{
		if (valueMetadata == null)
			return null;

		return valueMetadata.GetValueKind() switch
		{
			JsonValueKind.True => true,
			JsonValueKind.False => false,
			JsonValueKind.Array => valueMetadata.GetValue<Array>(),
			JsonValueKind.Number => valueMetadata.GetValue<int>(),
			JsonValueKind.Object => valueMetadata.GetValue<object>(),
			JsonValueKind.String => valueMetadata.GetValue<string>(),
			JsonValueKind.Null => null,
			_ => throw new OpenApiException($"The value type '{valueMetadata.GetValueKind()}' of {entityDescription} '{entityName}' is not supported."),
		};
	}

	/// <summary>
	/// Build a dictionary of extension key value pairs from the given open api extension model, where the key is the extension name
	/// and the value is either the actual value in the case of primitive types like string, int, date, etc, or a json string in the
	/// case of complex types.
	/// </summary>
	/// <param name="extensions">The dictionary of extension properties in the open api model.</param>
	/// <param name="logger">Used to perform logging.</param>
	/// <returns>The dictionary of extension properties using a simplified model that doesn't use any open api models.</returns>
	/// <exception cref="KernelException">Thrown when any extension data types are encountered that are not supported.</exception>
	private static Dictionary<string, object?> CreateRestApiOperationExtensions(IDictionary<string, IOpenApiExtension>? extensions, ILogger logger)
	{
		if (extensions == null)
			return [];

		var result = new Dictionary<string, object?>();

		// Map each extension property.
		foreach (var extension in extensions)
		{
			if (extension.Value is JsonNodeExtension jsonNodeExtension)
			{
				// Set primitive values directly into the dictionary.
				var extensionValueObj = GetParameterValue(jsonNodeExtension.Node, "extension property", extension.Key);
				result.Add(extension.Key, extensionValueObj);
			}
			else if (extension.Value != null)
			{
				// Serialize complex objects and set as json strings.
				// The only remaining type not referenced here is null, but the default value of extensionValueObj
				// is null, so if we just continue that will handle the null case.

				var schemaBuilder = new StringBuilder();
				var jsonWriter = new OpenApiJsonWriter(new StringWriter(schemaBuilder, CultureInfo.InvariantCulture), new OpenApiJsonWriterSettings() { Terse = true });
				extension.Value.Write(jsonWriter, Microsoft.OpenApi.OpenApiSpecVersion.OpenApi3_0);
				object? extensionValueObj = schemaBuilder.ToString();
				result.Add(extension.Key, extensionValueObj);

			}
			else
			{
				logger.LogWarning("The type of extension property '{ExtensionPropertyName}' is not supported while trying to consume the OpenApi schema.", extension.Key);
			}
		}

		return result;
	}

	/// <summary>
	/// Compares two <see cref="OpenApiParameter"/> objects by their name and location.
	/// </summary>
	private sealed class ParameterNameAndLocationComparer : IEqualityComparer<IOpenApiParameter>
	{
		public bool Equals(IOpenApiParameter? x, IOpenApiParameter? y)
		{
			if (x is null || y is null)
			{
				return x == y;
			}
			return GetHashCode(x) == GetHashCode(y);
		}
		public int GetHashCode([DisallowNull] IOpenApiParameter obj)
		{
			return HashCode.Combine(obj.Name, obj.In);
		}
	}
}
