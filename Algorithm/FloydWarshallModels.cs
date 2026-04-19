namespace Visual_FloydWarshall.Algorithm
{
	public sealed record FloydWarshallIterationLog(int IntermediateVertex, IReadOnlyList<FloydWarshallCellChange> Changes);

	public sealed record FloydWarshallCellChange(int FromVertex, int ToVertex, long OldDistance, long NewDistance, int OldPredecessor, int NewPredecessor);

	public sealed record FloydWarshallSnapshot(int Step, long[,] Distances);
}
