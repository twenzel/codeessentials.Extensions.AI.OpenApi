namespace codeessentials.Extensions.AI.OpenApi;

/// <summary>
/// Represents the context for an operation selection predicate.
/// </summary>
public readonly struct OperationSelectionPredicateContext : IEquatable<OperationSelectionPredicateContext>
{
	/// <summary>
	/// Initializes a new instance of the <see cref="OperationSelectionPredicateContext"/> struct.
	/// </summary>
	/// <param name="id">The identifier for the operation.</param>
	/// <param name="path">The path of the operation.</param>
	/// <param name="method">The HTTP method (GET, POST, etc.) of the operation.</param>
	/// <param name="description">The description of the operation.</param>
	internal OperationSelectionPredicateContext(string? id, string path, string method, string? description)
	{
		Id = id;
		Path = path;
		Method = method;
		Description = description;
	}

	/// <summary>
	/// The identifier for the operation.
	/// </summary>
	public string? Id { get; }

	/// <summary>
	/// The path of the operation.
	/// </summary>
	public string Path { get; }

	/// <summary>
	/// The HTTP method (GET, POST, etc.) of the operation.
	/// </summary>
	public string Method { get; }

	/// <summary>
	/// The description of the operation.
	/// </summary>
	public string? Description { get; }

	/// <inheritdoc />
	public override bool Equals(object? obj)
	{
		return obj is OperationSelectionPredicateContext other && Equals(other);
	}

	/// <inheritdoc />
	public override int GetHashCode()
	{
		// Using a tuple to create a hash code based on the properties  
		return HashCode.Combine(Id, Path, Method, Description);
	}

	/// <inheritdoc />
	public static bool operator ==(OperationSelectionPredicateContext left, OperationSelectionPredicateContext right)
	{
		return left.Equals(right);
	}

	/// <inheritdoc />
	public static bool operator !=(OperationSelectionPredicateContext left, OperationSelectionPredicateContext right)
	{
		return !(left == right);
	}

	/// <inheritdoc />
	public bool Equals(OperationSelectionPredicateContext other)
	{
		return Id == other.Id &&
			   Path == other.Path &&
			   Method == other.Method &&
			   Description == other.Description;
	}
}