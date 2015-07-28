// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// a class to determine, at display time, whether individual slices should be displayed or hidden.
	/// </summary>
	public class SliceFilter
	{
		protected XmlDocument m_filterList;

		/// <summary>
		/// create a filter which does not have an external setup notes to filter (but which will still ask FDO)
		/// </summary>
		public SliceFilter()
		{
			m_filterList= null;
		}

		/// <summary>
		/// create a filter which will consult both FDO and an external XML document when making filtering decisions
		/// </summary>
		/// <param name="filterList">XML document (see distfiles/lexed/basicFilter.xml for an example)</param>
		public SliceFilter(XmlDocument filterList)
		{
			m_filterList= filterList;
		}

		/// <summary>
		/// tell whether to include the slice
		/// </summary>
		/// <param name="configurationNode"></param>
		/// <param name="obj"></param>
		/// <param name="flid"></param>
		/// <returns>true if this slice should be included</returns>
		virtual public bool IncludeSlice(XmlNode configurationNode, ICmObject obj, int flid, HashSet<Tuple<int, int>> propsToMonitor)
		{
			if (m_filterList!= null)
			{
				string id = XmlUtils.GetOptionalAttributeValue(configurationNode, "id");
				if (id != null)
				{
					XmlNode instruction = m_filterList.SelectSingleNode("SliceFilter/node[@id='" + id + "']");

					if (instruction != null)
						return false;
				}
			}

			//skip slices representing fields which are not relevant under the current circumstances.
			// If necessary note that the list of slices
			var result = obj.IsFieldRelevant(flid, propsToMonitor);

			return result;
		}
	}
}
