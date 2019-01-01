// Copyright (c) 2007-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using SIL.Xml;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// This class provides a stack of nodes that represent a level (possibly hidden) in
	/// displaying the configuration tree.
	/// </summary>
	internal sealed class LayoutLevels
	{
		private readonly List<PartCaller> m_stackCallers = new List<PartCaller>();

		/// <summary>
		/// Add a set of nodes that represent a level in the tree (possibly hidden).
		/// </summary>
		public void Push(XElement partref, XElement layout)
		{
			m_stackCallers.Add(new PartCaller(layout, partref));
		}

		/// <summary>
		/// Remove the most recent set of nodes that represent a (possibly hidden) level.
		/// </summary>
		public void Pop()
		{
			if (m_stackCallers.Count > 0)
			{
				m_stackCallers.RemoveAt(m_stackCallers.Count - 1);
			}
		}

		/// <summary>
		/// Get the most recent part (ref=) node.
		/// </summary>
		public XElement PartRef => m_stackCallers.Count > 0 ? m_stackCallers[m_stackCallers.Count - 1].PartRef : null;

		/// <summary>
		/// Get the most recent layout node.
		/// </summary>
		public XElement Layout => m_stackCallers.Count > 0 ? m_stackCallers[m_stackCallers.Count - 1].Layout : null;

		/// <summary>
		/// If the most recent part (ref=) node was "hidden", get the oldest part (ref=)
		/// on the stack that was hidden.  (This allows multiple levels of hiddenness.)
		/// </summary>
		public XElement HiddenPartRef
		{
			get
			{
				if (!m_stackCallers.Any())
				{
					return null;
				}
				XElement xnHidden = null;
				for (var i = m_stackCallers.Count - 1; i >= 0; --i)
				{
					if (m_stackCallers[i].Hidden)
					{
						xnHidden = m_stackCallers[i].PartRef;
					}
					else
					{
						return xnHidden;
					}
				}
				return xnHidden;
			}
		}

		/// <summary>
		/// If the most recent part (ref=) node was "hidden", get the oldest corresponding
		/// layout on the stack that was hidden.  (This allows multiple levels of
		/// hiddenness.)
		/// </summary>
		public XElement HiddenLayout
		{
			get
			{
				if (!m_stackCallers.Any())
				{
					return null;
				}
				XElement xnHidden = null;
				for (var i = m_stackCallers.Count - 1; i >= 0; --i)
				{
					if (m_stackCallers[i].Hidden)
					{
						xnHidden = m_stackCallers[i].Layout;
					}
					else
					{
						return xnHidden;
					}
				}
				return xnHidden;
			}
		}

		/// <summary>
		/// This class encapsulates a part (ref=) and its enclosing layout.
		/// </summary>
		private sealed class PartCaller
		{
			internal PartCaller(XElement layout, XElement partref)
			{
				Layout = layout;
				PartRef = partref;
				Hidden = XmlUtils.GetOptionalBooleanAttributeValue(partref, "hideConfig", false);
			}

			internal XElement PartRef { get; }

			internal XElement Layout { get; }

			internal bool Hidden { get; }
		}
	}
}