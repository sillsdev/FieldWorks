using SIL.FieldWorks.Common.FwAvalonia.Poc;
using SIL.FieldWorks.Common.FwAvalonia.Preview;

[assembly: FwPreviewModule(
	"lexical-edit-poc",
	"Lexical Edit POC",
	typeof(PocPreviewWindow),
	typeof(PocPreviewDataProvider))]
