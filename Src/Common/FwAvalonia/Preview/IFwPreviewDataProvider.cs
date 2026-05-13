namespace SIL.FieldWorks.Common.Avalonia.Preview;

/// <summary>
/// Provides a design-time (preview) DataContext for an Avalonia module window.
/// Implementations should avoid FieldWorks/LCM dependencies.
/// </summary>
public interface IFwPreviewDataProvider
{
	object? CreateDataContext(string dataMode);
}
