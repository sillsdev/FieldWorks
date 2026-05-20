using System;

namespace SIL.FieldWorks.LexText.AdvancedEntry.Avalonia.PropertyGrid;

[AttributeUsage(AttributeTargets.Property)]
public sealed class PresentationNodeMetadataAttribute : Attribute
{
	public PresentationNodeMetadataAttribute(
		string nodeId,
		string fieldName,
		string editorKind,
		string accessibilityId,
		string localizationKey)
	{
		NodeId = nodeId;
		FieldName = fieldName;
		EditorKind = editorKind;
		AccessibilityId = accessibilityId;
		LocalizationKey = localizationKey;
	}

	public string NodeId { get; }
	public string FieldName { get; }
	public string EditorKind { get; }
	public string AccessibilityId { get; }
	public string LocalizationKey { get; }
}