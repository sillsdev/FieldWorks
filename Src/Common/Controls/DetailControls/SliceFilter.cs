using System;
using System.Diagnostics;
using System.IO;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.Utils;

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
		virtual public bool IncludeSlice(XmlNode configurationNode, ICmObject obj, int flid)
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

			//skip slices representing fields which are not relevant under the current circumstances

			return obj.IsFieldRelevant(flid);
		}
	}
}
