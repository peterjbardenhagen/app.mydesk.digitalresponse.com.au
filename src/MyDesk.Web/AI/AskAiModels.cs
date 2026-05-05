using System.Text.Json;
using System.Text.Json.Serialization;

namespace MyDesk.Web.AI;

/// <summary>
/// Tool / function schema declared by an <see cref="IAiTool"/>.
/// The shape is OpenAI-compatible (function calling / tools) so it can be
/// serialised straight into the Azure OpenAI Chat Completions request.
/// </summary>
public record AiToolDefinition(string Name, string Description, JsonElement ParametersSchema);

/// <summary>A tool the model can invoke. Implementations live in MyDesk.Web/AI/Tools/.</summary>
public interface IAiTool
{
    /// <summary>Stable name, lowercase_snake_case, used by the model to call this tool.</summary>
    string Name { get; }

    /// <summary>Short description shown to the model in the tool list.</summary>
    string Description { get; }

    /// <summary>JSON Schema for the tool's arguments object.</summary>
    JsonElement ParametersSchema { get; }

    /// <summary>Run the tool. <paramref name="argsJson"/> is whatever the model emitted.</summary>
    Task<AiToolResult> ExecuteAsync(JsonElement argsJson, CancellationToken ct = default);
}

/// <summary>
/// Result of a tool execution. <see cref="ContentJson"/> is fed back to the model.
/// <see cref="Renderable"/> is captured so the UI can show charts/tables alongside
/// the model's natural-language response.
/// </summary>
public record AiToolResult(string ContentJson, AiRenderable? Renderable = null);

/// <summary>
/// Renderable artefact returned by a tool — currently a chart spec or a tabular
/// summary. The Ask AI page renders these inline beneath the assistant's reply.
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(AiChartSpec),  typeDiscriminator: "chart")]
[JsonDerivedType(typeof(AiTableSpec),  typeDiscriminator: "table")]
[JsonDerivedType(typeof(AiNoticeSpec), typeDiscriminator: "notice")]
public abstract record AiRenderable(string Title);

/// <summary>A chart spec the UI converts into a MudChart.</summary>
public record AiChartSpec(
    string Title,
    /// <summary>"bar" | "line" | "pie" | "donut".</summary>
    string ChartType,
    string[] Labels,
    AiChartSeries[] Series,
    string? XAxisLabel = null,
    string? YAxisLabel = null
) : AiRenderable(Title);

public record AiChartSeries(string Name, double[] Values);

/// <summary>A simple table spec for tabular data.</summary>
public record AiTableSpec(
    string Title,
    string[] Columns,
    string[][] Rows
) : AiRenderable(Title);

/// <summary>A simple notice / confirmation card (e.g. "task scheduled").</summary>
public record AiNoticeSpec(
    string Title,
    string Message,
    /// <summary>"info" | "success" | "warning" | "error".</summary>
    string Severity = "info"
) : AiRenderable(Title);

/// <summary>One turn in the agent's reply — text plus any renderables produced this turn.</summary>
public record AskAiReply(
    string Text,
    IReadOnlyList<AiRenderable> Renderables,
    IReadOnlyList<AskAiToolTrace> Trace
);

public record AskAiToolTrace(string ToolName, string ArgsJson, string ResultPreview, bool Success);
