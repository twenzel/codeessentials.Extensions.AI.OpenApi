using Microsoft.Extensions.AI;

namespace codeessentials.Extensions.AI.OpenApi;

/// <summary>
/// Class with data related to an Open API <see cref="KernelFunction"/> invocation.
/// </summary>
public sealed class OpenApiOperationFunctionContext
{
	/// <summary>
	/// Key to access the <see cref="OpenApiOperationFunctionContext"/> in the <see cref="HttpRequestMessage"/>.
	/// </summary>
	public static readonly HttpRequestOptionsKey<OpenApiOperationFunctionContext> KernelFunctionContextKey = new("KernelFunctionContext");

	/// <summary>
	/// Initializes a new instance of the <see cref="OpenApiOperationFunctionContext"/> class.
	/// </summary>
	/// <param name="arguments">The <see cref="KernelArguments"/> associated with this context.</param>
	internal OpenApiOperationFunctionContext(AIFunctionArguments? arguments)
	{
		this.Arguments = arguments;
	}


	/// <summary>
	/// Gets the <see cref="AIFunctionArguments"/>.
	/// </summary>
	public AIFunctionArguments? Arguments { get; }
}