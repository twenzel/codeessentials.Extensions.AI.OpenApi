using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace codeessentials.Extensions.AI.OpenApi;

/// <summary>
/// OpenAPI function execution parameters.
/// </summary>
public class OpenApiFunctionExecutionParameters
{
	internal const string HTTPHEADERCONSTANT_USERAGENT = "codeessentials.Extensions.AI.OpenApi";

	/// <summary>
	/// HttpClient to use for sending HTTP requests.
	/// </summary>
	public HttpClient? HttpClient { get; set; }

	/// <summary>
	/// Callback for adding authentication data to HTTP requests.
	/// </summary>
	public AuthenticateRequestAsyncCallback? AuthCallback { get; set; }

	/// <summary>
	/// Override for REST API server url.
	/// </summary>
	public Uri? ServerUrlOverride { get; set; }

	/// <summary>
	/// Flag indicating whether to ignore non-compliant errors of the OpenAPI document or not.
	/// If set to true, the execution will not throw exceptions for non-compliant documents.
	/// Please note that enabling this option may result in incomplete or inaccurate execution results.
	/// </summary>
	public bool IgnoreNonCompliantErrors { get; set; }

	/// <summary>
	/// Optional user agent header value.
	/// </summary>
	public string UserAgent { get; set; }

	/// <summary>
	/// Determines whether the REST API operation payload is constructed dynamically based on payload metadata.
	/// It's enabled by default and allows to support operations with simple payload structure - no properties with the same name at different levels.
	/// To support more complex payloads, it should be disabled and the payload should be provided via the 'payload' argument.
	/// See the 'Providing Payload for OpenAPI Functions' ADR for more details: https://github.com/microsoft/semantic-kernel/blob/main/docs/decisions/0062-open-api-payload.md
	/// </summary>
	public bool EnableDynamicPayload { get; set; }

	/// <summary>
	/// Determines whether payload parameter names are augmented with namespaces. It's only applicable when EnableDynamicPayload property is set to true.
	/// Namespaces prevent naming conflicts by adding the parent parameter name as a prefix, separated by dots.
	/// For instance, without namespaces, the 'email' parameter for both the 'sender' and 'receiver' parent parameters
	/// would be resolved from the same 'email' argument, which is incorrect. However, by employing namespaces,
	/// the parameters 'sender.email' and 'sender.receiver' will be correctly resolved from arguments with the same names.
	/// See the 'Providing Payload for OpenAPI Functions' ADR for more details: https://github.com/microsoft/semantic-kernel/blob/main/docs/decisions/0062-open-api-payload.md
	/// </summary>
	public bool EnablePayloadNamespacing { get; set; }

	/// <summary>
	/// Operation selection predicate to apply to all OpenAPI document operations.
	/// If set, the predicate will be applied to each operation in the document.
	/// If the predicate returns true, the operation will be imported; otherwise, it will be skipped.
	/// This can be used to import or filter operations based on various operation properties: Id, Path, Method, and Description.
	/// </summary>
	public Func<OperationSelectionPredicateContext, bool>? OperationSelectionPredicate { get; set; }

	/// <summary>
	/// A custom HTTP response content reader. It can be useful when the internal reader
	/// for a specific content type is either missing, insufficient, or when custom behavior is desired.
	/// For instance, the internal reader for "application/json" HTTP content reads the content as a string.
	/// This may not be sufficient in cases where the JSON content is large, streamed chunk by chunk, and needs to be accessed
	/// as soon as the first chunk is available. To handle such cases, a custom reader can be provided to read the content
	/// as a stream rather than as a string.
	/// If the custom reader is not provided, or the reader returns null, the internal reader is used.
	/// </summary>
	public HttpResponseContentReader? HttpResponseContentReader { get; set; }

	/// <summary>
	/// A custom factory for the <see cref="RestApiOperationResponse"/>.
	/// It allows modifications of various aspects of the original response, such as adding response headers,
	/// changing response content, adjusting the schema, or providing a completely new response.
	/// If a custom factory is not supplied, the internal factory will be used by default.
	/// </summary>
	public RestApiOperationResponseFactory? RestApiOperationResponseFactory { get; set; }

	/// <summary>
	/// A custom REST API parameter filter.
	/// </summary>
	public RestApiParameterFilter? ParameterFilter { get; set; }

	/// <summary>
	/// Options for validating server URLs before making HTTP requests.
	/// When set, the plugin will validate each resolved URL against the configured allowed base URLs and schemes
	/// before sending the HTTP request. This helps prevent Server-Side Request Forgery (SSRF) attacks.
	/// If null (default), no URL validation is performed.
	/// </summary>
	public RestApiOperationServerUrlValidationOptions? ServerUrlValidationOptions { get; set; }

	/// <summary>
	/// The <see cref="ILoggerFactory"/> to use for logging. If null, no logging will be performed.
	/// </summary>
	public ILoggerFactory? LoggerFactory { get; set; }

	public JsonSerializerOptions? JsonSerializerOptions { get; set; }

	/// <summary>
	/// Initializes a new instance of the <see cref="OpenApiFunctionExecutionParameters"/> class.
	/// </summary>
	/// <param name="httpClient">The HttpClient to use for sending HTTP requests.</param>
	/// <param name="authCallback">The callback for adding authentication data to HTTP requests.</param>
	/// <param name="serverUrlOverride">The override for the REST API server URL.</param>
	/// <param name="userAgent">Optional user agent header value.</param>
	/// <param name="ignoreNonCompliantErrors">A flag indicating whether to ignore non-compliant errors of the OpenAPI document or not
	/// If set to true, the execution will not throw exceptions for non-compliant documents.
	/// Please note that enabling this option may result in incomplete or inaccurate execution results.</param>
	public OpenApiFunctionExecutionParameters(
		HttpClient? httpClient = null,
		AuthenticateRequestAsyncCallback? authCallback = null,
		Uri? serverUrlOverride = null,
		string? userAgent = null,
		bool ignoreNonCompliantErrors = false,
		bool enableDynamicOperationPayload = true,
		bool enablePayloadNamespacing = false)
	{
		HttpClient = httpClient;
		AuthCallback = authCallback;
		ServerUrlOverride = serverUrlOverride;
		UserAgent = userAgent ?? HTTPHEADERCONSTANT_USERAGENT;
		IgnoreNonCompliantErrors = ignoreNonCompliantErrors;
		EnableDynamicPayload = enableDynamicOperationPayload;
		EnablePayloadNamespacing = enablePayloadNamespacing;
	}
}

/// <summary>
/// Represents a delegate that defines the method signature for asynchronously authenticating an HTTP request.
/// </summary>
/// <param name="request">The <see cref="HttpRequestMessage"/> to authenticate.</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
public delegate Task AuthenticateRequestAsyncCallback(HttpRequestMessage request, CancellationToken cancellationToken = default);

/// <summary>
/// Represents a delegate for reading HTTP response content.
/// </summary>
/// <param name="context">The context containing HTTP operation details.</param>
/// <param name="cancellationToken">The cancellation token.</param>
/// <returns>The HTTP response content.</returns>
public delegate Task<object?> HttpResponseContentReader(HttpResponseContentReaderContext context, CancellationToken cancellationToken = default);