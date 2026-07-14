using System.Text.Json.Serialization;

namespace MyDesk.Shared.Models.AgentsOS;

public class AgentsOsResponse<T>
{
    [JsonPropertyName("status")]
    public string? Status { get; set; }

    [JsonPropertyName("project_id")]
    public string? ProjectId { get; set; }

    [JsonPropertyName("data")]
    public T? Data { get; set; }
}

public class PdoResponse
{
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = "brief_ingested";

    [JsonPropertyName("goals")]
    public List<string>? Goals { get; set; }

    [JsonPropertyName("epics")]
    public List<EpicDto>? Epics { get; set; }
}

public class EpicDto
{
    [JsonPropertyName("epic_id")]
    public string EpicId { get; set; } = string.Empty;

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("tasks")]
    public List<TaskDto>? Tasks { get; set; }
}

public class TaskDto
{
    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;
}

public class DagResponse
{
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("nodes")]
    public List<DagNodeDto> Nodes { get; set; } = new();

    [JsonPropertyName("edges")]
    public List<DagEdgeDto> Edges { get; set; } = new();
}

public class DagNodeDto
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("goal")]
    public string Goal { get; set; } = string.Empty;

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("score")]
    public double? Score { get; set; }
}

public class DagEdgeDto
{
    [JsonPropertyName("from_id")]
    public string FromId { get; set; } = string.Empty;

    [JsonPropertyName("to_id")]
    public string ToId { get; set; } = string.Empty;

    [JsonPropertyName("relation")]
    public string? Relation { get; set; }
}

public class LedgerResponse
{
    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("entries")]
    public List<LedgerEntryDto> Entries { get; set; } = new();

    [JsonPropertyName("count")]
    public int Count { get; set; }
}

public class LedgerEntryDto
{
    [JsonPropertyName("entry_id")]
    public string EntryId { get; set; } = string.Empty;

    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("task_id")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("actor")]
    public string Actor { get; set; } = string.Empty;

    [JsonPropertyName("action")]
    public string Action { get; set; } = string.Empty;

    [JsonPropertyName("command")]
    public string Command { get; set; } = string.Empty;

    [JsonPropertyName("result")]
    public string Result { get; set; } = string.Empty;
}

public class OrchestratorResult
{
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("project_id")]
    public string ProjectId { get; set; } = string.Empty;

    [JsonPropertyName("epics")]
    public int Epics { get; set; }

    [JsonPropertyName("tasks_submitted")]
    public int TasksSubmitted { get; set; }
}
