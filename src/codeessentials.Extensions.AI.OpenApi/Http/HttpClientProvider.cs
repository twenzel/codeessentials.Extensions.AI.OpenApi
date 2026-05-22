using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace codeessentials.Extensions.AI.OpenApi.Http;

/// <summary>
/// Provides functionality for retrieving instances of HttpClient.
/// </summary>
internal static class HttpClientProvider
{
	/// <summary>
	/// Retrieves an instance of HttpClient.
	/// </summary>
	/// <returns>An instance of HttpClient.</returns>
	public static HttpClient GetHttpClient() => new(NonDisposableHttpClientHandler.Instance, disposeHandler: false);

	/// <summary>
	/// Retrieves an instance of HttpClient.
	/// </summary>
	/// <returns>An instance of HttpClient.</returns>
	public static HttpClient GetHttpClient(HttpClient? httpClient) => httpClient ?? GetHttpClient();

	/// <summary>
	/// Represents a singleton implementation of <see cref="HttpClientHandler"/> that is not disposable.
	/// </summary>
	private sealed class NonDisposableHttpClientHandler : DelegatingHandler
	{
		/// <summary>
		/// Private constructor to prevent direct instantiation of the class.
		/// </summary>
		private NonDisposableHttpClientHandler() : base(CreateHandler())
		{
		}

		/// <summary>
		/// Gets the singleton instance of <see cref="NonDisposableHttpClientHandler"/>.
		/// </summary>
		public static NonDisposableHttpClientHandler Instance { get; } = new();

		/// <summary>
		/// Disposes the underlying resources held by the <see cref="NonDisposableHttpClientHandler"/>.
		/// This implementation does nothing to prevent unintended disposal, as it may affect all references.
		/// </summary>
		/// <param name="disposing">True if called from <see cref="Dispose"/>, false if called from a finalizer.</param>
		protected override void Dispose(bool disposing)
		{
			// Do nothing if called explicitly from Dispose, as it may unintentionally affect all references.
			// The base.Dispose(disposing) is not called to avoid invoking the disposal of HttpClientHandler resources.
			// This implementation assumes that the HttpMessageHandler is being used as a singleton and should not be disposed directly.
		}


		private static SocketsHttpHandler CreateHandler()
		{
			return new SocketsHttpHandler()
			{
				// Limit the lifetime of connections to better respect any DNS changes
				PooledConnectionLifetime = TimeSpan.FromMinutes(2),

				// Check cert revocation
				SslOptions = new SslClientAuthenticationOptions()
				{
					CertificateRevocationCheckMode = X509RevocationMode.Online,
				},
			};
		}
	}
}
