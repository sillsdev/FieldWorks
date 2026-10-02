// Copyright (c) 2026 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Globalization;
using SIL.FieldWorks.Common.FwUtils;

// Rules both detail views share. Nothing here may depend on WinForms, Avalonia, or a slice
// or control type, so the rules outlive WinForms. DetailRulesBoundaryTests enforces this.
namespace SIL.FieldWorks.Common.DetailRules
{
	/// <summary>
	/// The jump that opens a tool filtered to the records carrying one anthropology category.
	/// It is offered only on the Anthropology Categories field. The link names the tool, holds
	/// the tool's list load until the filter is in place, and carries the category.
	/// </summary>
	public static class AnthroItemFilterLink
	{
		/// <summary>The field whose items offer the jump.</summary>
		public const string FieldName = "AnthroCodes";

		/// <summary>Whether the items of a field offer the jump.</summary>
		public static bool Applies(string fieldName)
			=> string.Equals(fieldName, FieldName, StringComparison.Ordinal);

		/// <summary>The link for one category.</summary>
		/// <param name="projectHandle">The project the link opens in.</param>
		/// <param name="tool">The tool to filter, e.g. "lexiconEdit".</param>
		/// <param name="anthroItemHvo">The category to filter on.</param>
		public static FwLinkArgs Create(string projectHandle, string tool, int anthroItemHvo)
		{
			if (string.IsNullOrEmpty(tool))
				throw new ArgumentException("The tool is required.", nameof(tool));
			var link = new FwAppArgs(projectHandle, tool, Guid.Empty);
			var properties = link.PropertyTableEntries;
			properties.Add(new Property("SuspendLoadListUntilOnChangeFilter", tool));
			properties.Add(new Property("LinkSetupInfo", "FilterAnthroItems"));
			properties.Add(new Property("HvoOfAnthroItem", anthroItemHvo.ToString(CultureInfo.InvariantCulture)));
			return link;
		}
	}
}
