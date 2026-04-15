using System.Text.Json.Serialization;

namespace Techlight.MyDesk.MCP.Models;

// MCP Protocol Request/Response Models
public class McpRequest
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public object? Id { get; set; }
    
    [JsonPropertyName("method")]
    public string Method { get; set; } = string.Empty;
    
    [JsonPropertyName("params")]
    public Dictionary<string, object>? Params { get; set; }
}

public class McpResponse
{
    [JsonPropertyName("jsonrpc")]
    public string JsonRpc { get; set; } = "2.0";
    
    [JsonPropertyName("id")]
    public object? Id { get; set; }
    
    [JsonPropertyName("result")]
    public object? Result { get; set; }
    
    [JsonPropertyName("error")]
    public McpError? Error { get; set; }
}

public class McpError
{
    [JsonPropertyName("code")]
    public int Code { get; set; }
    
    [JsonPropertyName("message")]
    public string Message { get; set; } = string.Empty;
    
    [JsonPropertyName("data")]
    public object? Data { get; set; }
}

public class McpTool
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("inputSchema")]
    public McpToolSchema InputSchema { get; set; } = new();
}

public class McpToolSchema
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "object";
    
    [JsonPropertyName("properties")]
    public Dictionary<string, McpToolProperty> Properties { get; set; } = new();
    
    [JsonPropertyName("required")]
    public List<string> Required { get; set; } = new();
}

public class McpToolProperty
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "string";
    
    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;
    
    [JsonPropertyName("enum")]
    public List<string>? Enum { get; set; }
}

public class McpToolCallRequest
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;
    
    [JsonPropertyName("arguments")]
    public Dictionary<string, object> Arguments { get; set; } = new();
}

public class McpToolCallResult
{
    [JsonPropertyName("content")]
    public List<McpContent> Content { get; set; } = new();
    
    [JsonPropertyName("isError")]
    public bool IsError { get; set; }
}

public class McpContent
{
    [JsonPropertyName("type")]
    public string Type { get; set; } = "text";
    
    [JsonPropertyName("text")]
    public string Text { get; set; } = string.Empty;
}

// Server Info
public class McpServerInfo
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = "Techlight MyDesk MCP Server";
    
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";
}

public class McpInitializeResult
{
    [JsonPropertyName("protocolVersion")]
    public string ProtocolVersion { get; set; } = "2024-11-05";
    
    [JsonPropertyName("capabilities")]
    public McpCapabilities Capabilities { get; set; } = new();
    
    [JsonPropertyName("serverInfo")]
    public McpServerInfo ServerInfo { get; set; } = new();
}

public class McpCapabilities
{
    [JsonPropertyName("tools")]
    public McpToolsCapability? Tools { get; set; }
}

public class McpToolsCapability
{
    [JsonPropertyName("listChanged")]
    public bool ListChanged { get; set; } = true;
}

public class McpToolsListResult
{
    [JsonPropertyName("tools")]
    public List<McpTool> Tools { get; set; } = new();
}
