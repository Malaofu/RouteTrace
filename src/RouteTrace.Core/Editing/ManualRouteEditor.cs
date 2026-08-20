using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;

namespace RouteTrace.Core.Editing;

public sealed class ManualRouteEditor
{
    private readonly UndoRedoHistory<ManualRouteState> history;

    public ManualRouteEditor(IEnumerable<RoutePoint>? points = null, IEnumerable<int>? anchorIndices = null)
    {
        RoutePoint[] geometry = [.. points ?? []];
        IReadOnlyList<int> indices = RouteAnchorSelector.ValidateOrSelect(geometry, anchorIndices);
        history = new(CreateState(geometry, indices));
    }

    public IReadOnlyList<RoutePoint> RoutePoints => BuildGeometry(history.Current);
    public IReadOnlyList<int> AnchorIndices => BuildAnchorIndices(history.Current);
    public IReadOnlyList<RoutePoint> AnchorPoints => history.Current.Anchors;
    public IReadOnlyList<GeoCoordinate> Anchors => AnchorPoints.Select(point => point.Coordinate).ToArray();
    public IReadOnlyList<GeoCoordinate> Points => RoutePoints.Select(point => point.Coordinate).ToArray();
    public IReadOnlyList<IReadOnlyList<RoutePoint>> Legs => history.Current.Legs;
    public bool CanUndo => history.CanUndo;
    public bool CanRedo => history.CanRedo;
    public bool IsLoop => history.Current.IsLoop;

    public void ApplyRoutedEdit(
        IEnumerable<RoutePoint> anchors,
        IEnumerable<IReadOnlyList<RoutePoint>> legs,
        bool isLoop)
    {
        var state = new ManualRouteState(Snapshot(anchors), SnapshotLegs(legs), isLoop);
        ValidateState(state);
        history.Apply(state);
    }

    public void Add(GeoCoordinate coordinate)
    {
        if (IsLoop) throw new InvalidOperationException("Add an anchor to a loop by inserting it on an existing leg.");
        RoutePoint[] anchors = [.. AnchorPoints, new RoutePoint(coordinate)];
        IReadOnlyList<RoutePoint>[] legs = AnchorPoints.Count == 0
            ? []
            : [.. Legs, StraightLeg(AnchorPoints[^1], anchors[^1])];
        ApplyRoutedEdit(anchors, legs, false);
    }

    public void InsertAfter(int index, GeoCoordinate coordinate)
    {
        ValidateAnchorIndex(index);
        var anchors = AnchorPoints.ToList();
        var inserted = new RoutePoint(coordinate);
        RoutePoint finish = IsLoop && index == AnchorPoints.Count - 1
            ? anchors[0]
            : anchors[index + 1];
        anchors.Insert(index + 1, inserted);
        var legs = Legs.ToList();
        if (index < legs.Count) legs.RemoveAt(index);
        legs.Insert(index, StraightLeg(inserted, finish));
        legs.Insert(index, StraightLeg(anchors[index], inserted));
        ApplyRoutedEdit(anchors, legs, IsLoop);
    }

    public void Move(int index, GeoCoordinate coordinate)
    {
        ValidateAnchorIndex(index);
        RoutePoint[] anchors = [.. AnchorPoints];
        anchors[index] = anchors[index] with { Coordinate = coordinate };
        IReadOnlyList<RoutePoint>[] legs = [.. Legs];
        if (legs.Length > 0)
        {
            if (index > 0) legs[index - 1] = StraightLeg(anchors[index - 1], anchors[index]);
            else if (IsLoop) legs[^1] = StraightLeg(anchors[^1], anchors[0]);
            if (index < legs.Length) legs[index] = StraightLeg(anchors[index], anchors[(index + 1) % anchors.Length]);
        }
        ApplyRoutedEdit(anchors, legs, IsLoop);
    }

    public bool ReplaceCoordinates(IReadOnlyList<GeoCoordinate> coordinates)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        if (Points.SequenceEqual(coordinates)) return false;
        RoutePoint[] anchors;
        if (coordinates.Count == RoutePoints.Count)
        {
            anchors = RoutePoints.Select((point, index) => point with { Coordinate = coordinates[index] }).ToArray();
        }
        else if (coordinates.Count == RoutePoints.Count + 1)
        {
            int insertedIndex = 0;
            while (insertedIndex < RoutePoints.Count && coordinates[insertedIndex] == RoutePoints[insertedIndex].Coordinate)
                insertedIndex++;
            anchors = coordinates.Select((coordinate, index) => index == insertedIndex
                ? new RoutePoint(coordinate)
                : RoutePoints[index < insertedIndex ? index : index - 1]).ToArray();
        }
        else
        {
            anchors = coordinates.Select(coordinate => new RoutePoint(coordinate)).ToArray();
        }
        bool loop = anchors.Length >= 3 && anchors[0].Coordinate == anchors[^1].Coordinate;
        if (loop) anchors = anchors[..^1];
        ApplyRoutedEdit(anchors, StraightLegs(anchors, loop), loop);
        return true;
    }

    public void Delete(int index)
    {
        ValidateAnchorIndex(index);
        RoutePoint[] anchors = AnchorPoints.Where((_, anchorIndex) => anchorIndex != index).ToArray();
        bool loop = IsLoop && anchors.Length >= 2;
        ApplyRoutedEdit(anchors, StraightLegs(anchors, loop), loop);
    }

    public void Reverse()
    {
        if (AnchorPoints.Count < 2) return;
        RoutePoint[] anchors = IsLoop
            ? [AnchorPoints[0], .. AnchorPoints.Skip(1).Reverse()]
            : AnchorPoints.Reverse().ToArray();
        IReadOnlyList<RoutePoint>[] legs = Legs.Reverse()
            .Select(leg => (IReadOnlyList<RoutePoint>)Array.AsReadOnly(leg.Reverse().ToArray()))
            .ToArray();
        ApplyRoutedEdit(anchors, legs, IsLoop);
    }

    public void CloseLoop()
    {
        if (AnchorPoints.Count < 3 || IsLoop) return;
        ApplyRoutedEdit(AnchorPoints, Legs.Append(StraightLeg(AnchorPoints[^1], AnchorPoints[0])), true);
    }

    public void SetLoopStart(int index)
    {
        if (!IsLoop) throw new InvalidOperationException("The route must be a loop before choosing its starting point.");
        ValidateAnchorIndex(index);
        if (index == 0) return;
        ApplyRoutedEdit(
            AnchorPoints.Skip(index).Concat(AnchorPoints.Take(index)),
            Legs.Skip(index).Concat(Legs.Take(index)),
            true);
    }

    public void Clear() => ApplyRoutedEdit([], [], false);
    public bool Undo() => history.TryUndo();
    public bool Redo() => history.TryRedo();

    public RouteDocument ToDocument(string name = "Manual route") => new(
        tracks: [new Track(name, [new TrackSegment(RoutePoints, AnchorIndices)])],
        metadata: new RouteMetadata(name));

    private static ManualRouteState CreateState(IReadOnlyList<RoutePoint> points, IReadOnlyList<int> anchorIndices)
    {
        bool loop = points.Count >= 3 && points[0].Coordinate == points[^1].Coordinate;
        int visibleAnchorCount = anchorIndices.Count - (loop ? 1 : 0);
        RoutePoint[] anchors = anchorIndices.Take(visibleAnchorCount).Select(index => points[index]).ToArray();
        var legs = new List<IReadOnlyList<RoutePoint>>();
        for (int index = 1; index < anchorIndices.Count; index++)
            legs.Add(Array.AsReadOnly(points.Skip(anchorIndices[index - 1]).Take(anchorIndices[index] - anchorIndices[index - 1] + 1).ToArray()));
        var state = new ManualRouteState(Snapshot(anchors), Array.AsReadOnly(legs.ToArray()), loop);
        ValidateState(state);
        return state;
    }

    private static IReadOnlyList<RoutePoint> BuildGeometry(ManualRouteState state)
    {
        if (state.Legs.Count == 0) return state.Anchors;
        var points = new List<RoutePoint>(state.Legs.Sum(leg => leg.Count));
        foreach (IReadOnlyList<RoutePoint> leg in state.Legs)
        {
            if (points.Count == 0) points.AddRange(leg);
            else points.AddRange(leg.Skip(1));
        }
        return Array.AsReadOnly(points.ToArray());
    }

    private static IReadOnlyList<int> BuildAnchorIndices(ManualRouteState state)
    {
        if (state.Anchors.Count == 0) return [];
        var indices = new List<int> { 0 };
        int pointIndex = 0;
        foreach (IReadOnlyList<RoutePoint> leg in state.Legs)
        {
            pointIndex += leg.Count - 1;
            indices.Add(pointIndex);
        }
        return Array.AsReadOnly(indices.ToArray());
    }

    private static void ValidateState(ManualRouteState state)
    {
        int expectedLegs = state.IsLoop ? state.Anchors.Count : Math.Max(0, state.Anchors.Count - 1);
        if (state.Legs.Count != expectedLegs)
            throw new ArgumentException("The route legs do not match its anchors.", nameof(state));
        for (int index = 0; index < state.Legs.Count; index++)
        {
            IReadOnlyList<RoutePoint> leg = state.Legs[index];
            RoutePoint start = state.Anchors[index];
            RoutePoint finish = state.Anchors[(index + 1) % state.Anchors.Count];
            if (leg.Count < 2 || leg[0].Coordinate != start.Coordinate || leg[^1].Coordinate != finish.Coordinate)
                throw new ArgumentException("Every route leg must connect its adjacent anchors.", nameof(state));
        }
    }

    private void ValidateAnchorIndex(int index)
    {
        if (index < 0 || index >= AnchorPoints.Count) throw new ArgumentOutOfRangeException(nameof(index));
    }

    private static IReadOnlyList<RoutePoint> StraightLeg(RoutePoint start, RoutePoint finish) =>
        Array.AsReadOnly(new[] { start, finish });

    private static IReadOnlyList<IReadOnlyList<RoutePoint>> StraightLegs(IReadOnlyList<RoutePoint> anchors, bool loop)
    {
        int count = loop ? anchors.Count : Math.Max(0, anchors.Count - 1);
        return Array.AsReadOnly(Enumerable.Range(0, count)
            .Select(index => StraightLeg(anchors[index], anchors[(index + 1) % anchors.Count]))
            .ToArray());
    }

    private static IReadOnlyList<RoutePoint> Snapshot(IEnumerable<RoutePoint> points) =>
        Array.AsReadOnly(points.ToArray());

    private static IReadOnlyList<IReadOnlyList<RoutePoint>> SnapshotLegs(IEnumerable<IReadOnlyList<RoutePoint>> legs) =>
        Array.AsReadOnly(legs.Select(leg => (IReadOnlyList<RoutePoint>)Array.AsReadOnly(leg.ToArray())).ToArray());

    private sealed record ManualRouteState(
        IReadOnlyList<RoutePoint> Anchors,
        IReadOnlyList<IReadOnlyList<RoutePoint>> Legs,
        bool IsLoop);
}
