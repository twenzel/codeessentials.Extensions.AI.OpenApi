using System.Text.Json;
using Microsoft.Extensions.AI;

namespace codeessentials.Extensions.AI.OpenApi;

internal class OpenApiFunction : AIFunction
{
	private readonly OpenApiFunctionOperation _operation;
	private readonly string _name;
	private readonly string? _description;
	private readonly JsonElement _inputSchema;
	private readonly JsonElement? _returnSchema;

	public OpenApiFunction(OpenApiFunctionOperation operation, string name, string? description, JsonElement inputSchema, JsonElement? returnSchema)
	{
		_operation = operation;
		_name = name;
		_description = description;
		_inputSchema = inputSchema;
		_returnSchema = returnSchema;
	}

	public override string Name => _name;

	public override string Description => _description ?? base.Description;

	public override JsonElement JsonSchema => _inputSchema;

	public override JsonElement? ReturnJsonSchema => _returnSchema;

	protected override async ValueTask<object?> InvokeCoreAsync(AIFunctionArguments arguments, CancellationToken cancellationToken)
	{
		var response = await _operation(arguments, cancellationToken);

		return response;
	}

	public delegate Task<RestApiOperationResponse> OpenApiFunctionOperation(AIFunctionArguments arguments, CancellationToken cancellationToken);
}
