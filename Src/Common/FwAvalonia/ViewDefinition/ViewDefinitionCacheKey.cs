// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace SIL.FieldWorks.Common.FwAvalonia.ViewDefinition
{
	/// <summary>
	/// Immutable cache key for a compiled view definition. Includes the class/layout identity plus a
	/// content fingerprint of the layout and parts source, so that an edit to the XML source produces a
	/// different key (and therefore a recompile) even when the layout name is unchanged.
	/// </summary>
	public sealed class ViewDefinitionCacheKey : IEquatable<ViewDefinitionCacheKey>
	{
		public ViewDefinitionCacheKey(string className, string layoutName, string layoutType, string sourceFingerprint)
		{
			ClassName = className ?? "";
			LayoutName = layoutName ?? "";
			LayoutType = layoutType ?? "";
			SourceFingerprint = sourceFingerprint ?? "";
		}

		public string ClassName { get; }

		public string LayoutName { get; }

		public string LayoutType { get; }

		/// <summary>A stable hash of the layout + parts source text.</summary>
		public string SourceFingerprint { get; }

		public bool Equals(ViewDefinitionCacheKey other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return ClassName == other.ClassName
				&& LayoutName == other.LayoutName
				&& LayoutType == other.LayoutType
				&& SourceFingerprint == other.SourceFingerprint;
		}

		public override bool Equals(object obj) => Equals(obj as ViewDefinitionCacheKey);

		public override int GetHashCode()
		{
			unchecked
			{
				var hash = 17;
				hash = hash * 31 + ClassName.GetHashCode();
				hash = hash * 31 + LayoutName.GetHashCode();
				hash = hash * 31 + LayoutType.GetHashCode();
				hash = hash * 31 + SourceFingerprint.GetHashCode();
				return hash;
			}
		}

		public override string ToString()
			=> $"{ClassName}/{LayoutName}/{LayoutType}@{SourceFingerprint}";
	}
}
