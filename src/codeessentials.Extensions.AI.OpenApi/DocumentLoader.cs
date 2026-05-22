using System.Net.Http.Headers;
using codeessentials.Extensions.AI.OpenApi.Http;
using Microsoft.Extensions.Logging;

namespace codeessentials.Extensions.AI.OpenApi;

internal static class DocumentLoader
{
	internal static async Task<string> LoadDocumentFromUriAsync(Uri uri, ILogger logger, HttpClient httpClient, AuthenticateRequestAsyncCallback? authCallback, string? userAgent, CancellationToken cancellationToken)
	{
		using var response = await LoadDocumentResponseFromUriAsync(uri, logger, httpClient, authCallback, userAgent, cancellationToken).ConfigureAwait(false);

		try
		{
			return await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (HttpRequestException ex)
		{
			throw new HttpOperationException(message: ex.Message, innerException: ex);
		}
	}

	private static async Task<HttpResponseMessage> LoadDocumentResponseFromUriAsync(Uri uri, ILogger logger, HttpClient httpClient, AuthenticateRequestAsyncCallback? authCallback, string? userAgent, CancellationToken cancellationToken)
	{
		using var request = new HttpRequestMessage(HttpMethod.Get, uri.ToString());

		request.Headers.UserAgent.Add(ProductInfoHeaderValue.Parse(userAgent ?? OpenApiFunctionExecutionParameters.HTTPHEADERCONSTANT_USERAGENT));

		if (authCallback is not null)
			await authCallback(request, cancellationToken).ConfigureAwait(false);

		logger.LogTrace("Importing document from '{Uri}'", uri);


		return await httpClient.SendWithSuccessCheckAsync(request, HttpCompletionOption.ResponseContentRead, cancellationToken).ConfigureAwait(false);

	}

	internal static async Task<string> LoadDocumentFromFilePathAsync(string filePath, ILogger logger, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();

		CheckIfFileExists(filePath, logger);

		logger.LogTrace("Importing document from '{FilePath}'", filePath);

		using var sr = File.OpenText(filePath);
		return await sr.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
	}

	internal static async Task<string> LoadDocumentFromStreamAsync(Stream stream, CancellationToken cancellationToken)
	{
		using StreamReader reader = new(stream);
		return await reader.ReadToEndAsync(cancellationToken).ConfigureAwait(false);
	}

	private static void CheckIfFileExists(string filePath, ILogger logger)
	{
		if (!File.Exists(filePath))
		{
			var exception = new FileNotFoundException($"Invalid file path. The specified path '{filePath}' does not exist.");
			logger.LogError(exception, "Invalid file path. The specified path '{FilePath}' does not exist.", filePath);
			throw exception;
		}
	}
}
