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
	public class ConfigurableDictionaryNode
	{
		/// <summary>
		/// Information about the field in the model that this node is configuring
		/// </summary>
		public FieldDescription FieldDescription { get; set; }

		/// <summary>
		/// The style to apply to the data configured by this node
		/// </summary>
		public IStyle Style { get; set; }

		/// <summary>
		/// The label to display for this node
		/// </summary>
		public string Label { get; set; }

		/// <summary>
		/// String to apply before the content configured by this node
		/// </summary>
		public string Before { get; set; }

		/// <summary>
		/// String to apply after the content configured by this node
		/// </summary>
		public string After { get; set; }

		/// <summary>
		/// String to apply between content items configured by this node (only applicable to lists)
		/// </summary>
		public string Between { get; set; }

		/// <summary>
		/// Ordered list of nodes contained by this configurable node
		/// </summary>
		public List<ConfigurableDictionaryNode> Children { get; set; }

		/// <summary>
		/// Type specific configuration options for this configurable node;
		/// </summary>
		public IDictionaryNodeOptions DictionaryNodeOptions { get; set; }

		/// <summary>
		/// Whether this element of dictionary data is to shown as part of the dictionary.
		/// </summary>
		public bool IsEnabled { get; set; }
	}
}
