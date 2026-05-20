namespace SIL.FieldWorks.Common.Avalonia.Preview;

/// <summary>
/// Provides a design-time (preview) DataContext for an Avalonia module window.
/// Implementations should prefer DTO or staged sample data and avoid opening live FieldWorks projects.
/// </summary>
public interface IFwPreviewDataProvider
{
	object? CreateDataContext(string dataMode);
}
