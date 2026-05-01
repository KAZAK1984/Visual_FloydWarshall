namespace Visual_FloydWarshall.Logging;

public interface ILogger
{
	void Info(string message);
	void Error(string message, Exception? exception = null);
}
