using Microsoft.Extensions.AI;

namespace codeessentials.Extensions.AI.OpenApi.Model;

/// <summary>
/// Options for REST API operation run.
/// </summary>
internal sealed class RestApiOperationRunOptions
{
	/// <summary>
	/// Override for REST API operation server URL.
	/// </summary>
	public Uri? ServerUrlOverride { get; set; }

	/// <summary>
	/// The URL of REST API host.
	/// </summary>
	public Uri? ApiHostUrl { get; set; }

	/// <summary>
	/// The arguments used for the operation run.
	/// </summary>
	public AIFunctionArguments Arguments { get; set; }
}