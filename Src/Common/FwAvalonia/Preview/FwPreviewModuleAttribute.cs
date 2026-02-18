using System;

namespace SIL.FieldWorks.Common.Avalonia.Preview;

/// <summary>
/// Registers an Avalonia module with the shared preview host.
/// Applied at the assembly level in the module assembly.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
public sealed class FwPreviewModuleAttribute : Attribute
{
	public FwPreviewModuleAttribute(string id, string displayName, Type windowType)
		: this(id, displayName, windowType, dataProviderType: null)
	{
	}

	public FwPreviewModuleAttribute(string id, string displayName, Type windowType, Type? dataProviderType)
	{
		Id = id;
		DisplayName = displayName;
		WindowType = windowType;
		DataProviderType = dataProviderType;
	}

	public string Id { get; }
	public string DisplayName { get; }
	public Type WindowType { get; }
	public Type? DataProviderType { get; }
}
