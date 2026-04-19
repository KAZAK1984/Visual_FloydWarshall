using System.Text.Json;
using System.IO;
using Visual_FloydWarshall.Logging;

namespace Visual_FloydWarshall.Algorithm;

public sealed class FloydAlgorithmRunner(ILogger logger) : IAlgorithmRunner
{
    private const string FileSignature = "VFLOYD1";

    public AlgorithmConfig? CurrentConfig { get; private set; }
    public FloydWarshallResult? CurrentResult { get; private set; }

    public void Execute(AlgorithmConfig config)
    {
        try
        {
            if (config is not FloydAlgorithmConfig floydConfig)
                throw new ArgumentException("Unsupported algorithm configuration type.", nameof(config));

            ValidateConfig(floydConfig);

            CurrentResult = FloydWarshallSolver.Solve(floydConfig.AdjacencyMatrix, floydConfig.TrackIterationChanges);
            CurrentConfig = floydConfig;

            logger.Info($"Algorithm executed. Vertices: {floydConfig.CollectionSize}, start: {floydConfig.StartVertex}, end: {floydConfig.EndVertex}.");
        }
        catch (Exception ex)
        {
            logger.Error("Algorithm execution failed.", ex);
            throw;
        }
    }

    public void SaveToFile(string filePath)
    {
        try
        {
            if (CurrentConfig is not FloydAlgorithmConfig config || CurrentResult is null)
                throw new InvalidOperationException("There is no calculated result to save.");

            var payload = new FloydFilePayload
            {
                Signature = FileSignature,
                AlgorithmName = config.AlgorithmName,
                Description = config.Description,
                CollectionSize = config.CollectionSize,
                StartVertex = config.StartVertex,
                EndVertex = config.EndVertex,
                TrackIterationChanges = config.TrackIterationChanges,
                AdjacencyMatrix = ToJagged(config.AdjacencyMatrix),
                Distances = ToJagged(CurrentResult.Distances),
                Predecessors = ToJagged(CurrentResult.Predecessors),
                HasNegativeCycle = CurrentResult.HasNegativeCycle,
                IterationLogs = CurrentResult.IterationLogs
                    .Select(log => new FloydIterationLogPayload
                    {
                        IntermediateVertex = log.IntermediateVertex,
                        Changes = log.Changes
                            .Select(change => new FloydCellChangePayload
                            {
                                FromVertex = change.FromVertex,
                                ToVertex = change.ToVertex,
                                OldDistance = change.OldDistance,
                                NewDistance = change.NewDistance,
                                OldPredecessor = change.OldPredecessor,
                                NewPredecessor = change.NewPredecessor
                            })
                            .ToArray()
                    })
                    .ToArray(),
                Snapshots = CurrentResult.Snapshots
                    .Select(snapshot => new FloydSnapshotPayload
                    {
                        Step = snapshot.Step,
                        Distances = ToJagged(snapshot.Distances)
                    })
                    .ToArray()
            };

            var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(filePath, json);

            logger.Info($"Result saved to '{filePath}'.");
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to save result to '{filePath}'.", ex);
            throw;
        }
    }

    public void LoadFromFile(string filePath)
    {
        try
        {
            var json = File.ReadAllText(filePath);
            var payload = JsonSerializer.Deserialize<FloydFilePayload>(json)
                ?? throw new InvalidDataException("The file is empty or corrupted.");

            if (!string.Equals(payload.Signature, FileSignature, StringComparison.Ordinal))
                throw new InvalidDataException("Invalid file format signature.");

            var config = new FloydAlgorithmConfig(
                payload.CollectionSize,
                payload.StartVertex,
                payload.EndVertex,
                ToNullableRectangular(payload.AdjacencyMatrix),
                payload.TrackIterationChanges,
                payload.AlgorithmName,
                payload.Description);

            var result = new FloydWarshallResult(
                ToRectangular(payload.Distances),
                ToRectangular(payload.Predecessors),
                payload.HasNegativeCycle,
                payload.IterationLogs
                    .Select(log => new FloydWarshallIterationLog(
                        log.IntermediateVertex,
                        log.Changes
                            .Select(change => new FloydWarshallCellChange(
                                change.FromVertex,
                                change.ToVertex,
                                change.OldDistance,
                                change.NewDistance,
                                change.OldPredecessor,
                                change.NewPredecessor))
                            .ToArray()))
                    .ToArray(),
                payload.Snapshots
                    .Select(snapshot => new FloydWarshallSnapshot(snapshot.Step, ToRectangular(snapshot.Distances)))
                    .ToArray());

            CurrentConfig = config;
            CurrentResult = result;

            logger.Info($"Result loaded from '{filePath}'.");
        }
        catch (Exception ex)
        {
            logger.Error($"Failed to load result from '{filePath}'.", ex);
            throw;
        }
    }

    private static void ValidateConfig(FloydAlgorithmConfig config)
    {
        ArgumentNullException.ThrowIfNull(config.AdjacencyMatrix);

        var size = config.AdjacencyMatrix.GetLength(0);
        if (size != config.AdjacencyMatrix.GetLength(1))
            throw new ArgumentException("Adjacency matrix must be square.", nameof(config));

        if (config.CollectionSize != size)
            throw new ArgumentException("Collection size must match adjacency matrix size.", nameof(config));

        if (config.StartVertex < 0 || config.StartVertex >= size)
            throw new ArgumentOutOfRangeException(nameof(config.StartVertex));

        if (config.EndVertex < 0 || config.EndVertex >= size)
            throw new ArgumentOutOfRangeException(nameof(config.EndVertex));
    }

    private static long?[][] ToJagged(long?[,] source)
    {
        var rows = source.GetLength(0);
        var cols = source.GetLength(1);
        var result = new long?[rows][];

        for (var i = 0; i < rows; i++)
        {
            result[i] = new long?[cols];
            for (var j = 0; j < cols; j++)
                result[i][j] = source[i, j];
        }

        return result;
    }

    private static long[][] ToJagged(long[,] source)
    {
        var rows = source.GetLength(0);
        var cols = source.GetLength(1);
        var result = new long[rows][];

        for (var i = 0; i < rows; i++)
        {
            result[i] = new long[cols];
            for (var j = 0; j < cols; j++)
                result[i][j] = source[i, j];
        }

        return result;
    }

    private static int[][] ToJagged(int[,] source)
    {
        var rows = source.GetLength(0);
        var cols = source.GetLength(1);
        var result = new int[rows][];

        for (var i = 0; i < rows; i++)
        {
            result[i] = new int[cols];
            for (var j = 0; j < cols; j++)
                result[i][j] = source[i, j];
        }

        return result;
    }

    private static long?[,] ToNullableRectangular(long?[][] source)
    {
        var rows = source.Length;
        var cols = rows == 0 ? 0 : source[0].Length;
        var result = new long?[rows, cols];

        for (var i = 0; i < rows; i++)
        {
            if (source[i].Length != cols)
                throw new InvalidDataException("Invalid matrix format in file.");

            for (var j = 0; j < cols; j++)
                result[i, j] = source[i][j];
        }

        return result;
    }

    private static long[,] ToRectangular(long[][] source)
    {
        var rows = source.Length;
        var cols = rows == 0 ? 0 : source[0].Length;
        var result = new long[rows, cols];

        for (var i = 0; i < rows; i++)
        {
            if (source[i].Length != cols)
                throw new InvalidDataException("Invalid matrix format in file.");

            for (var j = 0; j < cols; j++)
                result[i, j] = source[i][j];
        }

        return result;
    }

    private static int[,] ToRectangular(int[][] source)
    {
        var rows = source.Length;
        var cols = rows == 0 ? 0 : source[0].Length;
        var result = new int[rows, cols];

        for (var i = 0; i < rows; i++)
        {
            if (source[i].Length != cols)
                throw new InvalidDataException("Invalid matrix format in file.");

            for (var j = 0; j < cols; j++)
                result[i, j] = source[i][j];
        }

        return result;
    }

    private sealed class FloydFilePayload
    {
        public string Signature { get; set; } = FileSignature;
        public string AlgorithmName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CollectionSize { get; set; }
        public int StartVertex { get; set; }
        public int EndVertex { get; set; }
        public bool TrackIterationChanges { get; set; }
        public long?[][] AdjacencyMatrix { get; set; } = [];
        public long[][] Distances { get; set; } = [];
        public int[][] Predecessors { get; set; } = [];
        public bool HasNegativeCycle { get; set; }
        public FloydIterationLogPayload[] IterationLogs { get; set; } = [];
        public FloydSnapshotPayload[] Snapshots { get; set; } = [];
    }

    private sealed class FloydIterationLogPayload
    {
        public int IntermediateVertex { get; set; }
        public FloydCellChangePayload[] Changes { get; set; } = [];
    }

    private sealed class FloydCellChangePayload
    {
        public int FromVertex { get; set; }
        public int ToVertex { get; set; }
        public long OldDistance { get; set; }
        public long NewDistance { get; set; }
        public int OldPredecessor { get; set; }
        public int NewPredecessor { get; set; }
    }

    private sealed class FloydSnapshotPayload
    {
        public int Step { get; set; }
        public long[][] Distances { get; set; } = [];
    }
}
