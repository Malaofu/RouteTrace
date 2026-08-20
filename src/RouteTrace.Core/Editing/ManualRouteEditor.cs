using RouteTrace.Core.Routes.Documents;
using RouteTrace.Core.Routes.Geometry;

namespace RouteTrace.Core.Editing;

public sealed class ManualRouteEditor
{
    private readonly UndoRedoHistory<IReadOnlyList<RoutePoint>> history;

    public ManualRouteEditor(IEnumerable<RoutePoint>? points = null)
    {
        history = new(Snapshot(points ?? []));
    }

    public IReadOnlyList<RoutePoint> RoutePoints => history.Current;

    public IReadOnlyList<GeoCoordinate> Points => RoutePoints.Select(point => point.Coordinate).ToArray();

    public bool CanUndo => history.CanUndo;

    public bool CanRedo => history.CanRedo;

    public bool IsLoop => RoutePoints.Count >= 3 && RoutePoints[0].Coordinate == RoutePoints[^1].Coordinate;

    public void Add(GeoCoordinate coordinate) => Apply([.. RoutePoints, new RoutePoint(coordinate)]);

    public void InsertAfter(int index, GeoCoordinate coordinate)
    {
        ValidateIndex(index, allowClosingPoint: false);
        var points = RoutePoints.ToList();
        points.Insert(index + 1, new RoutePoint(coordinate));
        Apply(points);
    }

    public void Move(int index, GeoCoordinate coordinate)
    {
        ValidateIndex(index, allowClosingPoint: false);
        RoutePoint[] points = [.. RoutePoints];
        points[index] = points[index] with { Coordinate = coordinate };
        if (IsLoop && index == 0) points[^1] = points[^1] with { Coordinate = coordinate };
        Apply(points);
    }

    public bool ReplaceCoordinates(IReadOnlyList<GeoCoordinate> coordinates)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        if (Points.SequenceEqual(coordinates)) return false;

        RoutePoint[] replacement;
        if (coordinates.Count == RoutePoints.Count)
        {
            GeoCoordinate[] adjusted = [.. coordinates];
            KeepLoopClosed(adjusted);
            replacement = RoutePoints.Select((point, index) =>
                point with { Coordinate = adjusted[index] }).ToArray();
        }
        else if (coordinates.Count == RoutePoints.Count + 1)
        {
            int insertedIndex = FirstInsertedIndex(coordinates);
            replacement = new RoutePoint[coordinates.Count];
            for (int index = 0; index < replacement.Length; index++)
            {
                replacement[index] = index == insertedIndex
                    ? new RoutePoint(coordinates[index])
                    : RoutePoints[index < insertedIndex ? index : index - 1];
            }
        }
        else
        {
            replacement = coordinates.Select(coordinate => new RoutePoint(coordinate)).ToArray();
        }

        Apply(replacement);
        return true;
    }

    public void Delete(int index)
    {
        ValidateIndex(index, allowClosingPoint: false);
        var points = RoutePoints.ToList();
        bool removesLoopStart = IsLoop && index == 0;
        points.RemoveAt(index);
        if (removesLoopStart)
        {
            points.RemoveAt(points.Count - 1);
            if (points.Count >= 2) points.Add(points[0]);
        }
        else if (IsLoop && points.Count < 3)
        {
            points.RemoveAt(points.Count - 1);
        }
        Apply(points);
    }

    public void Reverse()
    {
        if (RoutePoints.Count < 2) return;
        Apply(RoutePoints.Reverse());
    }

    public void CloseLoop()
    {
        if (RoutePoints.Count < 3 || IsLoop) return;
        Apply([.. RoutePoints, RoutePoints[0]]);
    }

    public void SetLoopStart(int index)
    {
        if (!IsLoop) throw new InvalidOperationException("The route must be a loop before choosing its starting point.");
        ValidateIndex(index, allowClosingPoint: false);
        if (index == 0) return;

        RoutePoint[] unique = [.. RoutePoints.Take(RoutePoints.Count - 1)];
        Apply([.. unique.Skip(index), .. unique.Take(index), unique[index]]);
    }

    public void Clear()
    {
        if (RoutePoints.Count > 0) Apply([]);
    }

    public bool Undo() => history.TryUndo();

    public bool Redo() => history.TryRedo();

    public RouteDocument ToDocument(string name = "Manual route") => new(
        tracks: [new Track(name, [new TrackSegment(RoutePoints)])],
        metadata: new RouteMetadata(name));

    private void KeepLoopClosed(GeoCoordinate[] coordinates)
    {
        if (!IsLoop || coordinates.Length < 2) return;
        bool firstMoved = coordinates[0] != RoutePoints[0].Coordinate;
        bool lastMoved = coordinates[^1] != RoutePoints[^1].Coordinate;
        if (firstMoved && !lastMoved) coordinates[^1] = coordinates[0];
        else if (lastMoved && !firstMoved) coordinates[0] = coordinates[^1];
    }

    private int FirstInsertedIndex(IReadOnlyList<GeoCoordinate> coordinates)
    {
        int index = 0;
        while (index < RoutePoints.Count && coordinates[index] == RoutePoints[index].Coordinate) index++;
        return index;
    }

    private void Apply(IEnumerable<RoutePoint> points) => history.Apply(Snapshot(points));

    private void ValidateIndex(int index, bool allowClosingPoint)
    {
        int upperBound = RoutePoints.Count - (IsLoop && !allowClosingPoint ? 1 : 0);
        if (index < 0 || index >= upperBound) throw new ArgumentOutOfRangeException(nameof(index));
    }

    private static IReadOnlyList<RoutePoint> Snapshot(IEnumerable<RoutePoint> points) =>
        Array.AsReadOnly(points.ToArray());
}
