// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// An instance of this class generates a string that is the value of an integer property.
	/// (This is not an especially efficient way to handle integer properties, but it keeps the
	/// whole interface arrangement so much simpler that I think it is worth it.)
	/// </summary>
	public class OwnIntPropFinder : StringFinderBase
	{
		int m_flid;

		/// <summary>
		/// Construct one that retrieves a particular integer property from the SDA.
		/// </summary>
		/// <param name="sda"></param>
		/// <param name="flid"></param>
		public OwnIntPropFinder(ISilDataAccess sda, int flid)
			: base(sda)
		{
			m_flid = flid;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:OwnIntPropFinder"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public OwnIntPropFinder()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the flid.
		/// </summary>
		/// <value>The flid.</value>
		/// ------------------------------------------------------------------------------------
		public int Flid
		{
			get { return m_flid; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Persists as XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void PersistAsXml(System.Xml.XmlNode node)
		{
			base.PersistAsXml (node);
			XmlUtils.AppendAttribute(node, "flid", m_flid.ToString());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Inits the XML.
		/// </summary>
		/// <param name="node">The node.</param>
		/// ------------------------------------------------------------------------------------
		public override void InitXml(System.Xml.XmlNode node)
		{
			base.InitXml (node);
			m_flid = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flid");
		}


		#region StringFinder Members
		/// <summary>
		/// Return the (one) string that is the value of the integer property.
		/// </summary>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public override string[] Strings(int hvo)
		{
			return new string[] {m_sda.get_IntProp(hvo, m_flid).ToString()};
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA.
		/// </summary>
		/// <param name="other"></param>
		/// <returns></returns>
		public override bool SameFinder(IStringFinder other)
		{
			OwnIntPropFinder other2 = other as OwnIntPropFinder;
			if (other2 == null)
				return false;
			return other2.m_flid == this.m_flid && other2.m_sda == this.m_sda;
		}


		#endregion
	}
}
