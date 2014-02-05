// Copyright (c) 2014 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
using System.Collections.Generic;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.XWorks
{
	/// <summary>
	/// This class allows configuring an element of dictionary data
	/// </summary>
	class ConfigurableDictionaryNode
	{
		/// <summary>
		/// Information about the field in the model that this node is configuring
		/// </summary>
		private FieldDescription m_fieldDescription;

		/// <summary>
		/// The style to apply to the data configured by this node
		/// </summary>
		private IStyle m_style;

		/// <summary>
		/// The label to display for this node
		/// </summary>
		private string m_label;

		/// <summary>
		/// String to apply before the content configured by this node
		/// </summary>
		private string m_before;

		/// <summary>
		/// String to apply after the content configured by this node
		/// </summary>
		private string m_after;

		/// <summary>
		/// String to apply between content items configured by this node (only applicable to lists)
		/// </summary>
		private string m_between;

		/// <summary>
		/// Ordered list of nodes contained by this configurable node
		/// </summary>
		private List<ConfigurableDictionaryNode> m_children;

		/// <summary>
		/// Type specific configuration options for this configurable node;
		/// </summary>
		private IDictionaryNodeOptions m_dictionaryNodeOptions;

	}
}
