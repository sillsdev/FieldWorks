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
		private readonly ViewDefinitionIdentity _identity;

		public ViewDefinitionCacheKey(string className, string layoutName, string layoutType, string sourceFingerprint)
			: this(className, layoutName, layoutType, null, sourceFingerprint)
		{
		}

		public ViewDefinitionCacheKey(string className, string layoutName, string layoutType,
			string choiceGuid, string sourceFingerprint)
		{
			_identity = new ViewDefinitionIdentity(className, layoutType, layoutName, choiceGuid);
			SourceFingerprint = sourceFingerprint ?? "";
		}

		public string ClassName => _identity.ClassName;

		public string LayoutName => _identity.LayoutName;

		public string LayoutType => _identity.LayoutType;

		/// <summary>The nullable selected layout variant. Null and empty are distinct.</summary>
		public string ChoiceGuid => _identity.ChoiceGuid;

		/// <summary>A stable hash of the layout + parts source text.</summary>
		public string SourceFingerprint { get; }

		public bool Equals(ViewDefinitionCacheKey other)
		{
			if (ReferenceEquals(null, other)) return false;
			if (ReferenceEquals(this, other)) return true;
			return _identity.Equals(other._identity)
				&& StringComparer.Ordinal.Equals(SourceFingerprint, other.SourceFingerprint);
		}

		public override bool Equals(object obj) => Equals(obj as ViewDefinitionCacheKey);

		public override int GetHashCode()
			=> (_identity.GetHashCode() * 31)
				+ StringComparer.Ordinal.GetHashCode(SourceFingerprint);

		public override string ToString()
			=> $"{ClassName}/{LayoutName}/{LayoutType}/{(ChoiceGuid == null ? "<null>" : ChoiceGuid)}@{SourceFingerprint}";
	}
}
