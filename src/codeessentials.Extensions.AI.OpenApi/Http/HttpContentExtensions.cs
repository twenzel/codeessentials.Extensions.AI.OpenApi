namespace codeessentials.Extensions.AI.OpenApi.Http;

internal static class HttpContentExtensions
{
	/// <summary>
	/// Reads the content of the HTTP response as a byte array and translates any HttpRequestException into an HttpOperationException.
	/// </summary>
	/// <param name="httpContent">The HTTP content to read.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A byte array representing the HTTP content.</returns>
	public static async Task<byte[]> ReadAsByteArrayAndTranslateExceptionAsync(this HttpContent httpContent, CancellationToken cancellationToken = default)
	{
		try
		{
			return await httpContent.ReadAsByteArrayAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (HttpRequestException ex)
		{
			throw new HttpOperationException(message: ex.Message, innerException: ex);
		}
	}

	/// <summary>
	/// Reads the content of the HTTP response as a string and translates any HttpRequestException into an HttpOperationException.
	/// </summary>
	/// <param name="httpContent">The HTTP content to read.</param>
	/// <param name="cancellationToken">The cancellation token.</param>
	/// <returns>A string representation of the HTTP content.</returns>
	public static async Task<string> ReadAsStringWithExceptionMappingAsync(this HttpContent httpContent, CancellationToken cancellationToken = default)
	{
		try
		{
			return await httpContent.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (HttpRequestException ex)
		{
			throw new HttpOperationException(message: ex.Message, innerException: ex);
		}
	}
}
