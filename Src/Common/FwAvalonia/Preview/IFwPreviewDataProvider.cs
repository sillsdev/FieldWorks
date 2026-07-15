namespace SIL.FieldWorks.Common.FwAvalonia.Preview
{
	/// <summary>
	/// Provides a design-time / preview data context for an Avalonia module window.
	/// Implementations should prefer DTO/sample data and avoid opening live FieldWorks projects.
	/// </summary>
	public interface IFwPreviewDataProvider
	{
		object CreateDataContext(string dataMode);
	}
}
