using System.Collections.ObjectModel;

namespace codeessentials.Extensions.AI.OpenApi.Model;

/// <summary>
/// REST API server.
/// </summary>
public sealed class RestApiServer
{
	/// <summary>
	/// Description of the server.
	/// </summary>
	public string? Description { get; }

	/// <summary>
	/// A URL to the target host. This URL supports Server Variables and MAY be relative,
	/// to indicate that the host location is relative to the location where the OpenAPI document is being served.
	/// Variable substitutions will be made when a variable is named in {brackets}.
	/// </summary>
	public string? Url { get; }

	/// <summary>
	/// A map between a variable name and its value. The value is used for substitution in the server's URL template.
	/// </summary>
	public IDictionary<string, RestApiServerVariable> Variables { get; private set; }

	/// <summary>
	/// Construct a new <see cref="RestApiServer"/> object.
	/// </summary>
	/// <param name="url">URL to the target host</param>
	/// <param name="variables">Substitution variables for the server's URL template</param>
	/// <param name="description">Description of the server</param>
	internal RestApiServer(string? url = null, IDictionary<string, RestApiServerVariable>? variables = null, string? description = null)
	{
		Url = string.IsNullOrEmpty(url) ? null : url;
		Variables = variables ?? new Dictionary<string, RestApiServerVariable>();
		Description = description;
	}

	/// <summary>
	/// Makes the current instance unmodifiable.
	/// </summary>
	internal void Freeze()
	{
		Variables = new ReadOnlyDictionary<string, RestApiServerVariable>(Variables);
		foreach (var variable in Variables.Values)
		{
			variable.Freeze();
		}
	}
}