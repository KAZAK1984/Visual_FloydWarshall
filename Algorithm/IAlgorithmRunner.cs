namespace Visual_FloydWarshall.Algorithm;

public interface IAlgorithmRunner
{
	AlgorithmConfig? CurrentConfig { get; }
	FloydWarshallResult? CurrentResult { get; }
	void Execute(AlgorithmConfig config);
	void Reset();
	void SaveToFile(string filePath);
	void LoadFromFile(string filePath);
}
