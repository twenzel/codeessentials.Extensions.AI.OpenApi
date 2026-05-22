namespace codeessentials.Extensions.AI.OpenApi.Model;

/// <summary>
/// REST API parameter location.
/// </summary>
public enum RestApiParameterLocation
{
	/// <summary>
	/// Query parameter.
	/// </summary>
	Query,

	/// <summary>
	/// Header parameter.
	/// </summary>
	Header,

	/// <summary>
	/// Path parameter.
	/// </summary>
	Path,

	/// <summary>
	/// Cookie parameter.
	/// </summary>
	Cookie,

	/// <summary>
	/// Body parameter.
	/// </summary>
	Body,
}