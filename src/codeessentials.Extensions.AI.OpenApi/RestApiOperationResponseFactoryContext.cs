using codeessentials.Extensions.AI.OpenApi.Model;

namespace codeessentials.Extensions.AI.OpenApi;

/// <summary>
/// Represents the context for the <see cref="RestApiOperationResponseFactory"/>."/>
/// </summary>
public sealed class RestApiOperationResponseFactoryContext
{
	/// <summary>
	/// Initializes a new instance of the <see cref="RestApiOperationResponseFactoryContext"/> class.
	/// </summary>
	/// <param name="operation">The REST API operation.</param>
	/// <param name="request">The HTTP request message.</param>
	/// <param name="response">The HTTP response message.</param>
	/// <param name="internalFactory">The internal factory to create instances of the <see cref="RestApiOperationResponse"/>.</param>
	internal RestApiOperationResponseFactoryContext(RestApiOperation operation, HttpRequestMessage request, HttpResponseMessage response, RestApiOperationResponseFactory internalFactory)
	{
		InternalFactory = internalFactory;
		Operation = operation;
		Request = request;
		Response = response;
	}

	/// <summary>
	/// The REST API operation.
	/// </summary>
	public RestApiOperation Operation { get; }

	/// <summary>
	/// The HTTP request message.
	/// </summary>
	public HttpRequestMessage Request { get; }

	/// <summary>
	/// The HTTP response message.
	/// </summary>
	public HttpResponseMessage Response { get; }

	/// <summary>
	/// The internal factory to create instances of the <see cref="RestApiOperationResponse"/>.
	/// </summary>
	public RestApiOperationResponseFactory InternalFactory { get; }
}

/// <summary>
/// Represents a factory for creating instances of the <see cref="RestApiOperationResponse"/>.
/// </summary>
/// <param name="context">The context that contains the operation details.</param>
/// <param name="cancellationToken">The cancellation token used to signal cancellation.</param>
/// <returns>A task that represents the asynchronous operation, containing an instance of <see cref="RestApiOperationResponse"/>.</returns>
public delegate Task<RestApiOperationResponse> RestApiOperationResponseFactory(RestApiOperationResponseFactoryContext context, CancellationToken cancellationToken = default);