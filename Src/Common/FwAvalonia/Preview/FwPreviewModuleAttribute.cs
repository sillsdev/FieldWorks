// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.Preview
{
	/// <summary>
	/// Registers a previewable Avalonia module with the shared preview host.
	/// Applied at the assembly level in the module assembly.
	/// </summary>
	[AttributeUsage(AttributeTargets.Assembly, AllowMultiple = true)]
	public sealed class FwPreviewModuleAttribute : Attribute
	{
		public FwPreviewModuleAttribute(string id, string displayName, Type windowType)
			: this(id, displayName, windowType, dataProviderType: null)
		{
		}

		public FwPreviewModuleAttribute(string id, string displayName, Type windowType, Type dataProviderType)
		{
			Id = id;
			DisplayName = displayName;
			WindowType = windowType;
			DataProviderType = dataProviderType;
		}

		public string Id { get; }
		public string DisplayName { get; }
		public Type WindowType { get; }
		public Type DataProviderType { get; }
	}
}
