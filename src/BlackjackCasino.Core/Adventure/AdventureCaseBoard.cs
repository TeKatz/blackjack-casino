namespace BlackjackCasino.Core.Adventure;

public enum CaseNodeCategory
{
    Person,
    Location,
    Evidence,
    Lead,
    Theory,
}

public enum CaseConnectionType
{
    Suspected,
    Timeline,
    Ownership,
    Motive,
    Relationship,
    Witnessed,
    Financial,
    Communication,
    Location,
}

public enum CaseConnectionStatus
{
    Suspected,
    Confirmed,
    Uncertain,
    Contradicted,
}

public enum CaseLeadState
{
    Unresolved,
    Resolved,
    Contradicted,
}

public enum CaseTheoryState
{
    Proposed,
    Supported,
    Confirmed,
    Contradicted,
}

public enum CaseReliability
{
    Unknown,
    Questionable,
    Credible,
    Confirmed,
    Disproven,
}

public sealed record CaseNodeDefinition(
    string Id,
    CaseNodeCategory Category,
    string Title,
    string Description,
    string? Subtitle = null,
    string? ImageKey = null,
    string? DiscoveryContext = null,
    IReadOnlyList<string>? KnownFacts = null,
    IReadOnlyList<string>? StoryQuotes = null,
    CaseReliability InitialReliability = CaseReliability.Unknown,
    double? X = null,
    double? Y = null);

public sealed record CaseConnectionDefinition(
    string Id,
    string FromNodeId,
    string ToNodeId,
    CaseConnectionType Type,
    CaseConnectionStatus InitialStatus = CaseConnectionStatus.Suspected,
    string? Label = null,
    string? DiscoveredAtSceneId = null,
    string? Notes = null);

public sealed record CaseBoardDefinition
{
    public CaseBoardDefinition(
        IReadOnlyList<CaseNodeDefinition> Nodes,
        IReadOnlyList<CaseConnectionDefinition>? Connections = null,
        string Title = "Case Board",
        bool AllowManualConnections = false)
    {
        this.Nodes = Nodes ?? throw new ArgumentNullException(nameof(Nodes));
        this.Connections = Connections ?? Array.Empty<CaseConnectionDefinition>();
        this.Title = string.IsNullOrWhiteSpace(Title) ? "Case Board" : Title;
        this.AllowManualConnections = AllowManualConnections;
    }

    public IReadOnlyList<CaseNodeDefinition> Nodes { get; init; }
    public IReadOnlyList<CaseConnectionDefinition> Connections { get; init; }
    public string Title { get; init; }
    public bool AllowManualConnections { get; init; }
}

public sealed class CaseNodeState
{
    public bool IsKnown { get; set; }
    public CaseReliability Reliability { get; set; } = CaseReliability.Unknown;
    public CaseLeadState LeadState { get; set; } = CaseLeadState.Unresolved;
    public CaseTheoryState TheoryState { get; set; } = CaseTheoryState.Proposed;
    public string? DiscoveredAtSceneId { get; set; }
    public string? LastUpdatedSceneId { get; set; }
}

public sealed class CaseConnectionState
{
    public bool IsKnown { get; set; }
    public CaseConnectionStatus Status { get; set; } = CaseConnectionStatus.Suspected;
    public string? LastUpdatedSceneId { get; set; }
}

public sealed class CaseBoardNodePosition
{
    public double X { get; set; }
    public double Y { get; set; }
}

public sealed class BoardLayoutState
{
    public int LayoutVersion { get; set; }
    public Dictionary<string, CaseBoardNodePosition> NodePositions { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public double Zoom { get; set; } = 1d;
    public double PanX { get; set; }
    public double PanY { get; set; }
    public double? DetachedLeft { get; set; }
    public double? DetachedTop { get; set; }
    public double? DetachedWidth { get; set; }
    public double? DetachedHeight { get; set; }

    public static BoardLayoutState Normalize(BoardLayoutState? value)
    {
        BoardLayoutState state = value ?? new BoardLayoutState();
        state.LayoutVersion = Math.Max(0, state.LayoutVersion);
        state.NodePositions = state.NodePositions is null
            ? new Dictionary<string, CaseBoardNodePosition>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, CaseBoardNodePosition>(state.NodePositions, StringComparer.OrdinalIgnoreCase);
        state.Zoom = double.IsFinite(state.Zoom) ? Math.Clamp(state.Zoom, 0.2d, 4d) : 1d;
        if (!double.IsFinite(state.PanX)) state.PanX = 0;
        if (!double.IsFinite(state.PanY)) state.PanY = 0;
        state.DetachedWidth = NormalizeDimension(state.DetachedWidth, 500, 3840);
        state.DetachedHeight = NormalizeDimension(state.DetachedHeight, 350, 2160);
        if (state.DetachedLeft is double left && !double.IsFinite(left)) state.DetachedLeft = null;
        if (state.DetachedTop is double top && !double.IsFinite(top)) state.DetachedTop = null;
        return state;
    }

    private static double? NormalizeDimension(double? value, double minimum, double maximum) =>
        value is double current && double.IsFinite(current)
            ? Math.Clamp(current, minimum, maximum)
            : null;
}

public sealed class CaseBoardState
{
    public Dictionary<string, CaseNodeState> Nodes { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<string, CaseConnectionState> Connections { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public BoardLayoutState Layout { get; set; } = new();

    public static CaseBoardState Normalize(CaseBoardState? value)
    {
        CaseBoardState state = value ?? new CaseBoardState();
        state.Nodes = state.Nodes is null
            ? new Dictionary<string, CaseNodeState>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, CaseNodeState>(state.Nodes, StringComparer.OrdinalIgnoreCase);
        state.Connections = state.Connections is null
            ? new Dictionary<string, CaseConnectionState>(StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, CaseConnectionState>(state.Connections, StringComparer.OrdinalIgnoreCase);
        state.Layout = BoardLayoutState.Normalize(state.Layout);
        return state;
    }
}

public static class AdventureCaseBoard
{
    public static CaseBoardState Initialize(CaseBoardDefinition? definition, CaseBoardState? state = null)
    {
        CaseBoardState normalized = CaseBoardState.Normalize(state);
        if (definition is null) return normalized;

        CaseBoardDefinitionValidator.Validate(definition);
        foreach (CaseNodeDefinition node in definition.Nodes)
        {
            if (!normalized.Nodes.TryGetValue(node.Id, out CaseNodeState? nodeState))
            {
                normalized.Nodes[node.Id] = new CaseNodeState
                {
                    Reliability = node.InitialReliability,
                    LeadState = CaseLeadState.Unresolved,
                    TheoryState = CaseTheoryState.Proposed,
                };
            }
            else if (nodeState.Reliability == CaseReliability.Unknown && node.InitialReliability != CaseReliability.Unknown)
            {
                nodeState.Reliability = node.InitialReliability;
            }

            if (node.X is double x && node.Y is double y && !normalized.Layout.NodePositions.ContainsKey(node.Id))
                normalized.Layout.NodePositions[node.Id] = new CaseBoardNodePosition { X = x, Y = y };
        }

        foreach (CaseConnectionDefinition connection in definition.Connections)
        {
            if (!normalized.Connections.ContainsKey(connection.Id))
            {
                normalized.Connections[connection.Id] = new CaseConnectionState
                {
                    Status = connection.InitialStatus,
                };
            }
        }

        return normalized;
    }

    public static void DiscoverNode(CaseBoardDefinition definition, CaseBoardState state, string nodeId, string? sceneId = null)
    {
        CaseNodeDefinition node = FindNode(definition, nodeId);
        CaseBoardState normalized = Initialize(definition, state);
        CaseNodeState nodeState = normalized.Nodes[node.Id];
        nodeState.IsKnown = true;
        nodeState.DiscoveredAtSceneId ??= sceneId;
        nodeState.LastUpdatedSceneId = sceneId ?? nodeState.LastUpdatedSceneId;
    }

    public static void RevealConnection(CaseBoardDefinition definition, CaseBoardState state, string connectionId, string? sceneId = null)
    {
        CaseConnectionDefinition connection = FindConnection(definition, connectionId);
        CaseBoardState normalized = Initialize(definition, state);
        CaseConnectionState connectionState = normalized.Connections[connection.Id];
        connectionState.IsKnown = true;
        connectionState.LastUpdatedSceneId = sceneId ?? connection.DiscoveredAtSceneId ?? connectionState.LastUpdatedSceneId;
    }

    public static void SetConnectionStatus(
        CaseBoardDefinition definition,
        CaseBoardState state,
        string connectionId,
        CaseConnectionStatus status,
        string? sceneId = null)
    {
        CaseConnectionDefinition connection = FindConnection(definition, connectionId);
        CaseBoardState normalized = Initialize(definition, state);
        CaseConnectionState connectionState = normalized.Connections[connection.Id];
        connectionState.IsKnown = true;
        connectionState.Status = status;
        connectionState.LastUpdatedSceneId = sceneId ?? connectionState.LastUpdatedSceneId;
    }

    public static void SetLeadState(
        CaseBoardDefinition definition,
        CaseBoardState state,
        string nodeId,
        CaseLeadState leadState,
        string? sceneId = null)
    {
        CaseNodeDefinition node = FindNode(definition, nodeId);
        if (node.Category != CaseNodeCategory.Lead)
            throw new ArgumentException($"Case node '{nodeId}' is not a lead.", nameof(nodeId));

        CaseBoardState normalized = Initialize(definition, state);
        CaseNodeState nodeState = normalized.Nodes[node.Id];
        nodeState.IsKnown = true;
        nodeState.LeadState = leadState;
        nodeState.LastUpdatedSceneId = sceneId ?? nodeState.LastUpdatedSceneId;
    }

    public static void SetTheoryState(
        CaseBoardDefinition definition,
        CaseBoardState state,
        string nodeId,
        CaseTheoryState theoryState,
        string? sceneId = null)
    {
        CaseNodeDefinition node = FindNode(definition, nodeId);
        if (node.Category != CaseNodeCategory.Theory)
            throw new ArgumentException($"Case node '{nodeId}' is not a theory.", nameof(nodeId));

        CaseBoardState normalized = Initialize(definition, state);
        CaseNodeState nodeState = normalized.Nodes[node.Id];
        nodeState.IsKnown = true;
        nodeState.TheoryState = theoryState;
        nodeState.LastUpdatedSceneId = sceneId ?? nodeState.LastUpdatedSceneId;
    }

    private static CaseNodeDefinition FindNode(CaseBoardDefinition definition, string nodeId) =>
        definition.Nodes.FirstOrDefault(node => string.Equals(node.Id, nodeId, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Unknown case-board node '{nodeId}'.", nameof(nodeId));

    private static CaseConnectionDefinition FindConnection(CaseBoardDefinition definition, string connectionId) =>
        definition.Connections.FirstOrDefault(connection => string.Equals(connection.Id, connectionId, StringComparison.OrdinalIgnoreCase))
        ?? throw new ArgumentException($"Unknown case-board connection '{connectionId}'.", nameof(connectionId));
}

public static class CaseBoardDefinitionValidator
{
    public static void Validate(CaseBoardDefinition definition)
    {
        ArgumentNullException.ThrowIfNull(definition);
        var nodeIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (CaseNodeDefinition node in definition.Nodes)
        {
            if (string.IsNullOrWhiteSpace(node.Id) || !nodeIds.Add(node.Id))
                throw new ArgumentException($"Case Board contains a blank or duplicate node id '{node.Id}'.", nameof(definition));
            if (string.IsNullOrWhiteSpace(node.Title))
                throw new ArgumentException($"Case Board node '{node.Id}' requires a title.", nameof(definition));
            if ((node.X is null) != (node.Y is null))
                throw new ArgumentException($"Case Board node '{node.Id}' must define both X and Y or neither.", nameof(definition));
        }

        var connectionIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (CaseConnectionDefinition connection in definition.Connections)
        {
            if (string.IsNullOrWhiteSpace(connection.Id) || !connectionIds.Add(connection.Id))
                throw new ArgumentException($"Case Board contains a blank or duplicate connection id '{connection.Id}'.", nameof(definition));
            if (!nodeIds.Contains(connection.FromNodeId) || !nodeIds.Contains(connection.ToNodeId))
                throw new ArgumentException($"Case Board connection '{connection.Id}' references a missing node.", nameof(definition));
            if (string.Equals(connection.FromNodeId, connection.ToNodeId, StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException($"Case Board connection '{connection.Id}' cannot connect a node to itself.", nameof(definition));
        }
    }
}
