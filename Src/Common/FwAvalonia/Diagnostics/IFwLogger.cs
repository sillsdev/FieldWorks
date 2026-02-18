namespace SIL.FieldWorks.Common.Avalonia.Diagnostics;

public interface IFwLogger
{
	void Info(string message);
	void Warn(string message);
	void Error(string message);
	void Error(string message, Exception exception);
}
