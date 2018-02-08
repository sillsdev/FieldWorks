// Copyright (c) 2005-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Xml.Linq;
using System.Xml.XPath;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Controls.XMLViews
{
	/// <summary>
	/// Generate parts needed to provide paths to fields specified by a given layout
	/// </summary>
	internal class ChildPartGenerator : PartGenerator
	{
		internal ChildPartGenerator(LcmCache cache, XElement input, XmlVc vc, int rootClassId)
			: base(cache, input, vc, rootClassId)
		{
		}

		/// <summary />
		protected override void InitMemberVariablesFromInput(IFwMetaDataCache mdc, XElement input)
		{
			if (input.Name == "generate")
			{
				// first column child is the node we want to try to generate.
				m_source = input.XPathSelectElement("./column");
				return;
			}

			if (input.Name != "column")
			{
				throw new ArgumentException("ChildPartGenerator expects input to be column node, not {0}", input.Name.LocalName);
			}
			m_source = input;
		}

		/// <summary />
		public List<XElement> GenerateChildPartsIfNeeded()
		{
			return GeneratePartsFromLayouts(m_rootClassId, XmlUtils.GetOptionalAttributeValue(m_source, "layout"), 0, ref m_source);
		}
	}
}