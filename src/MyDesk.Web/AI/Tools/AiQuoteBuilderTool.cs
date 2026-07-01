using System.Text.Json;

namespace MyDesk.Web.AI.Tools;

/// <summary>
/// AI tool that parses a natural language quote description into structured line items.
/// When the agent calls this tool, it provides the structured quote content which the
/// AI Quote Generator page can read from the agent trace.
/// </summary>
public class AiQuoteBuilderTool : IAiTool
{
    public string Name => "parse_quote_items";

    public string Description =>
        "Parses a natural language quote description into structured line items with quantities, " +
        "descriptions, unit costs, and sell prices. Use this to generate quote content from user prompts.";

    public JsonElement ParametersSchema => JsonDocument.Parse("""
    {
        "type": "object",
        "properties": {
            "items": {
                "type": "array",
                "items": {
                    "type": "object",
                    "properties": {
                        "description":  { "type": "string" },
                        "quantity":     { "type": "number", "description": "Qty" },
                        "unit_cost":    { "type": "number", "description": "Cost price (ex-GST)" },
                        "sell_price":   { "type": "number", "description": "Sell price per unit (ex-GST)" },
                        "product_code": { "type": "string" }
                    },
                    "required": ["description", "quantity", "sell_price"]
                }
            },
            "reference": { "type": "string", "description": "Quote reference/title" },
            "notes":     { "type": "string", "description": "Customer-facing notes" }
        },
        "required": ["items"]
    }
    """).RootElement;

    public Task<AiToolResult> ExecuteAsync(JsonElement args, CancellationToken ct = default)
    {
        // The tool simply echoes back the structured data the model produced.
        // The caller (AiQuoteGenerator page) reads this from the agent's tool trace.
        var json = JsonSerializer.Serialize(args);
        return Task.FromResult(new AiToolResult(json));
    }
}
