namespace codeessentials.Extensions.AI.OpenApi;

public class OpenApiException : Exception
{
	/// <summary>
	/// Initializes a new instance of the <see cref="OpenApiException"/> class.
	/// </summary>
	public OpenApiException()
	{
	}

	/// <summary>
	/// Initializes a new instance of the <see cref="OpenApiException"/> class with its message set to <paramref name="message"/>.
	/// </summary>
	/// <param name="message">A string that describes the error.</param>
	public OpenApiException(string? message) : base(message)
	{
	}
}
