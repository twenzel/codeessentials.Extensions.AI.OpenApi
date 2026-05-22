using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using codeessentials.Extensions.AI.OpenApi.Http;
using codeessentials.Extensions.AI.OpenApi.Model;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace codeessentials.Extensions.AI.OpenApi;

/// <summary>
/// Provides static factory methods for creating AITools from OpenAPI specifications.
/// </summary>
public static partial class OpenApiToolFactory
{
	/// <summary>
	/// Creates <see cref="AITool"/>s from an OpenAPI specification.
	/// </summary>
	/// <param name="filePath">The file path to the OpenAPI specification.</param>
	/// <param name="executionParameters">The OpenAPI specification parsing and function execution parameters.</param>
	/// <param name="cancellationToken">The cancellation token.</param>	
	public static async Task<AITool[]> GetToolsFromSpec(string filePath, OpenApiFunctionExecutionParameters? executionParameters = null, CancellationToken cancellationToken = default)
	{
		var loggerFactory = executionParameters?.LoggerFactory;
		var logger = loggerFactory?.CreateLogger(typeof(OpenApiToolFactory)) ?? NullLogger.Instance;
		var httpClient = HttpClientProvider.GetHttpClient(executionParameters?.HttpClient);

		var openApiSpec = await DocumentLoader.LoadDocumentFromFilePathAsync(filePath, logger, cancellationToken).ConfigureAwait(false);

		return await GetToolsFromOpenApiSpec(executionParameters, httpClient, openApiSpec, executionParameters?.LoggerFactory, logger, null, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Creates <see cref="AITool"/>s from an OpenAPI specification.
	/// </summary>
	/// <param name="uri">A URI referencing the OpenAPI specification.</param>
	/// <param name="executionParameters">The OpenAPI specification parsing and function execution parameters.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	public static async Task<AITool[]> GetToolsFromSpec(Uri uri, OpenApiFunctionExecutionParameters? executionParameters = null, CancellationToken cancellationToken = default)
	{
		var loggerFactory = executionParameters?.LoggerFactory;
		var logger = loggerFactory?.CreateLogger(typeof(OpenApiToolFactory)) ?? NullLogger.Instance;
		var httpClient = HttpClientProvider.GetHttpClient(executionParameters?.HttpClient);

		var openApiSpec = await DocumentLoader.LoadDocumentFromUriAsync(uri, logger, httpClient, executionParameters?.AuthCallback, executionParameters?.UserAgent, cancellationToken).ConfigureAwait(false);

		return await GetToolsFromOpenApiSpec(executionParameters, httpClient, openApiSpec, executionParameters?.LoggerFactory, logger, uri, cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Creates <<see cref="AITool"/>s from an OpenAPI specification.
	/// </summary>
	/// <param name="stream">A stream representing the OpenAPI specification.</param>
	/// <param name="executionParameters">The OpenAPI specification parsing and function execution parameters.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	public static async Task<AITool[]> GetToolsFromSpec(Stream stream, OpenApiFunctionExecutionParameters? executionParameters = null, CancellationToken cancellationToken = default)
	{
		var loggerFactory = executionParameters?.LoggerFactory;
		var logger = loggerFactory?.CreateLogger(typeof(OpenApiToolFactory)) ?? NullLogger.Instance;
		var httpClient = HttpClientProvider.GetHttpClient(executionParameters?.HttpClient);

		var openApiSpec = await DocumentLoader.LoadDocumentFromStreamAsync(stream, cancellationToken).ConfigureAwait(false);

		return await GetToolsFromOpenApiSpec(executionParameters, httpClient, openApiSpec, executionParameters?.LoggerFactory, logger, null, cancellationToken).ConfigureAwait(false);
	}

	private static async Task<AITool[]> GetToolsFromOpenApiSpec(OpenApiFunctionExecutionParameters? executionParameters, HttpClient httpClient, string openApiSpec, ILoggerFactory? loggerFactory, ILogger logger, Uri? documentUri, CancellationToken cancellationToken)
	{
		using var documentStream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(openApiSpec));
		var parser = new OpenApiDocumentParser(loggerFactory);

		var restApi = await parser.ParseAsync(
			stream: documentStream,
			options: new OpenApiDocumentParserOptions
			{
				IgnoreNonCompliantErrors = executionParameters?.IgnoreNonCompliantErrors ?? false,
				OperationSelectionPredicate = (context) => SelectOperations(context, executionParameters)
			},
			cancellationToken: cancellationToken).ConfigureAwait(false);


		var runner = new RestApiOperationRunner(
			httpClient,
			executionParameters?.AuthCallback,
			executionParameters?.UserAgent,
			executionParameters?.EnableDynamicPayload ?? true,
			executionParameters?.EnablePayloadNamespacing ?? false,
			executionParameters?.HttpResponseContentReader,
			executionParameters?.RestApiOperationResponseFactory,
			serverUrlValidationOptions: executionParameters?.ServerUrlValidationOptions);

		var functions = new List<AITool>();

		foreach (var operation in restApi.Operations)
		{
			try
			{
				functions.Add(CreateRestApiFunction(runner, restApi.Info, restApi.SecurityRequirements, operation, executionParameters, documentUri, logger));
			}
			catch (Exception ex) when (!ex.IsCriticalException())
			{
				// Logging the exception and keep registering other Rest functions
				logger.LogWarning(ex, "Something went wrong while rendering the Rest function. Function: {Operation}. Error: {Message}", operation, ex.Message);
			}
		}

		restApi.Freeze();

		return functions.ToArray();
	}

	/// <summary>
	/// Selects operations to parse and import.
	/// </summary>
	/// <param name="context">Operation selection context.</param>
	/// <param name="executionParameters">Execution parameters.</param>
	/// <returns>True if the operation should be selected; otherwise, false.</returns>
	private static bool SelectOperations(OperationSelectionPredicateContext context, OpenApiFunctionExecutionParameters? executionParameters)
	{
		if (executionParameters?.OperationSelectionPredicate is { } predicate)
			return predicate(context);

		return true;
	}

	/// <summary>
	/// Registers <see cref="KernelFunctionFactory"/>> for a REST API operation.
	/// </summary>
	/// <param name="pluginName">Plugin name.</param>
	/// <param name="runner">The REST API operation runner.</param>
	/// <param name="info">The REST API info.</param>
	/// <param name="security">The REST API security requirements.</param>
	/// <param name="operation">The REST API operation.</param>
	/// <param name="executionParameters">Function execution parameters.</param>
	/// <param name="documentUri">The URI of OpenAPI document.</param>
	/// <param name="loggerFactory">The logger factory.</param>
	/// <returns>An instance of <see cref="KernelFunctionFromPrompt"/> class.</returns>
	internal static AITool CreateRestApiFunction(
		RestApiOperationRunner runner,
		RestApiInfo info,
		IList<RestApiSecurityRequirement>? security,
		RestApiOperation operation,
		OpenApiFunctionExecutionParameters? executionParameters,
		Uri? documentUri,
		ILogger logger)
	{
		var restOperationParameters = operation.GetParameters(
			executionParameters?.EnableDynamicPayload ?? true,
			executionParameters?.EnablePayloadNamespacing ?? false,
			executionParameters?.ParameterFilter
		);

		async Task<RestApiOperationResponse> ExecuteAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
		{
			try
			{
				var options = new RestApiOperationRunOptions
				{
					Arguments = arguments,
					ServerUrlOverride = executionParameters?.ServerUrlOverride,
					ApiHostUrl = documentUri is not null ? new Uri(documentUri.GetLeftPart(UriPartial.Authority)) : null
				};

				return await runner.RunAsync(operation, arguments, options, cancellationToken).ConfigureAwait(false);
			}
			catch (Exception ex) when (!ex.IsCriticalException())
			{
				logger!.LogError(ex, "RestAPI function {Operation} execution failed with error {Error}", operation, ex.Message);
				throw;
			}
		}

		var returnParameter = operation.GetDefaultReturnParameter();
		JsonElement? returnSchema = null;

		if (returnParameter != null && returnParameter.Schema != null)
			returnSchema = returnParameter.Schema.RootElement;

		var schema = AIFunctionJsonHelper.CreateFunctionJsonSchema(operation, restOperationParameters, serializerOptions: executionParameters?.JsonSerializerOptions);

		return new OpenApiFunction(ExecuteAsync, ConvertOperationToValidFunctionName(operation, logger), operation.Description, schema, returnSchema);
	}

	/// <summary>
	/// Converts operation id to valid <see cref="AIFunction"/> name.
	/// A function name can contain only ASCII letters, digits, and underscores.
	/// </summary>
	/// <param name="operation">The REST API operation.</param>
	/// <param name="logger">The logger.</param>
	/// <returns>Valid <see cref="AIFunction"/>> name.</returns>
	private static string ConvertOperationToValidFunctionName(RestApiOperation operation, ILogger logger)
	{
		if (!string.IsNullOrWhiteSpace(operation.Id))
			return ConvertOperationIdToValidFunctionName(operationId: operation.Id!, logger: logger);

		// Tokenize operation path on forward and back slashes
		var tokens = operation.Path.Split('/', '\\');
		StringBuilder result = new();
		result.Append(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(operation.Method.ToString()));

		foreach (var token in tokens)
		{
			// Removes all characters that are not ASCII letters, digits, and underscores.
			var formattedToken = RemoveInvalidCharsRegex().Replace(token, "");
			result.Append(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formattedToken.ToLower(CultureInfo.CurrentCulture)));
		}

		logger.LogInformation("""Operation method "{Method}" with path "{Path}" converted to "{Result}" to comply with SK Function name requirements. Use "{Result}" when invoking function.""", operation.Method, operation.Path, result, result);

		return result.ToString();
	}

	/// <summary>
	/// Converts operation id to valid <see cref="AIFunction"/> name.
	/// A function name can contain only ASCII letters, digits, and underscores.
	/// </summary>
	/// <param name="operationId">The operation id.</param>
	/// <param name="logger">The logger.</param>
	/// <returns>Valid <see cref="AIFunction"/> name.</returns>
	private static string ConvertOperationIdToValidFunctionName(string operationId, ILogger logger)
	{
		try
		{
			ValidFunctionName(operationId);
			return operationId;
		}
		catch (ArgumentException)
		{
			// The exception indicates that the operationId is not a valid function name.
			// To comply with the KernelFunction name requirements, it needs to be converted or sanitized.
			// Therefore, it should not be re-thrown, but rather swallowed to allow the conversion below.
		}

		// Tokenize operation id on forward and back slashes
		var tokens = operationId.Split('/', '\\');
		var result = new StringBuilder();

		foreach (var token in tokens)
		{
			// Removes all characters that are not ASCII letters, digits, and underscores.
			var formattedToken = RemoveInvalidCharsRegex().Replace(token, "");
			result.Append(CultureInfo.CurrentCulture.TextInfo.ToTitleCase(formattedToken.ToLower(CultureInfo.CurrentCulture)));
		}

		logger.LogInformation("""Operation name "{OperationId}" converted to "{Result}" to comply with SK Function name requirements. Use "{Result}" when invoking function.""", operationId, result, result);

		return result.ToString();
	}

	internal static void ValidFunctionName(string? functionName)
	{
		ArgumentNullException.ThrowIfNullOrWhiteSpace(functionName);

		if (!AllowedFunctionNameSymbolsRegex().IsMatch(functionName))
		{
			throw new ArgumentException("Invalid function name", functionName);
		}
	}

	[GeneratedRegex("^[0-9A-Za-z_-]*$")]
	private static partial Regex AllowedFunctionNameSymbolsRegex();

	[GeneratedRegex("[^0-9A-Za-z_]")]
	private static partial Regex RemoveInvalidCharsRegex();
}
