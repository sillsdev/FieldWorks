// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Xml.Linq;
using SIL.LCModel;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// This interface is used in the conversion of xml configuration from layout and parts files into another form. i.e. TreeNodes
	/// </summary>
	internal interface ILayoutConverter
	{
		/// <summary>
		/// This method is called when the entire layout has been converted a tree of LayoutTreeNodes
		/// </summary>
		void AddDictionaryTypeItem(XElement layoutNode, List<LayoutTreeNode> oldNodes);

		/// <summary>
		/// Returns the configuration nodes for all layout types
		/// </summary>
		IEnumerable<XElement> GetLayoutTypes();

		/// <summary/>
		LcmCache Cache { get; }

		/// <summary/>
		bool UseStringTable { get; }

		/// <summary/>
		LayoutLevels LayoutLevels { get; }

		/// <summary/>
		void ExpandWsTaggedNodes(string sWsTag);

		/// <summary/>
		void SetOriginalIndexForNode(LayoutTreeNode mainLayoutNode);

		/// <summary/>
		XElement GetLayoutElement(string className, string layoutName);

		/// <summary/>
		XElement GetPartElement(string className, string sRef);

		/// <summary/>
		void BuildRelationTypeList(LayoutTreeNode ltn);

		/// <summary/>
		void BuildEntryTypeList(LayoutTreeNode ltn, string layoutName);

		void LogConversionError(string errorLog);
	}
}