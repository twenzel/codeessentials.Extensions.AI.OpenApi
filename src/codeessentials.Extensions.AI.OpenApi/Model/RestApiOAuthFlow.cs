using System.Collections.ObjectModel;

namespace codeessentials.Extensions.AI.OpenApi.Model;

/// <summary>
/// REST API OAuth Flow.
/// </summary>
public sealed class RestApiOAuthFlow
{
	/// <summary>
	/// REQUIRED. The authorization URL to be used for this flow.
	/// Applies to implicit and authorizationCode OAuthFlow.
	/// </summary>
	public required Uri AuthorizationUrl { get; init; }

	/// <summary>
	/// REQUIRED. The token URL to be used for this flow.
	/// Applies to password, clientCredentials, and authorizationCode OAuthFlow.
	/// </summary>
	public required Uri TokenUrl { get; init; }

	/// <summary>
	/// The URL to be used for obtaining refresh tokens.
	/// </summary>
	public Uri? RefreshUrl { get; init; }

	/// <summary>
	/// REQUIRED. A map between the scope name and a short description for it.
	/// </summary>
	public required IDictionary<string, string> Scopes { get; set; }

	/// <summary>
	/// Creates an instance of a <see cref="RestApiOAuthFlow"/> class.
	/// </summary>
	internal RestApiOAuthFlow()
	{
	}

	internal void Freeze()
	{
		Scopes = new ReadOnlyDictionary<string, string>(Scopes);
	}
}