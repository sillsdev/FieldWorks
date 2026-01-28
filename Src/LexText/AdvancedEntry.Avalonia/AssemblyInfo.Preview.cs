using SIL.FieldWorks.Common.Avalonia.Preview;
using SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.Views;

[assembly: FwPreviewModule(
	"advanced-entry",
	"Advanced Entry",
	typeof(MainWindow),
	typeof(SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.AdvancedEntryPreviewDataProvider))]