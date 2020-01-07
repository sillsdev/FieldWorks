// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// An instance of this class generates a string that is the value of an integer property.
	/// (This is not an especially efficient way to handle integer properties, but it keeps the
	/// whole interface arrangement so much simpler that I think it is worth it.)
	/// </summary>
	public class OwnIntPropFinder : StringFinderBase
	{
		/// <summary>
		/// Construct one that retrieves a particular integer property from the SDA.
		/// </summary>
		public OwnIntPropFinder(ISilDataAccess sda, int flid)
			: base(sda)
		{
			Flid = flid;
		}

		/// <summary />
		public OwnIntPropFinder()
		{
		}

		/// <summary>
		/// Gets the flid.
		/// </summary>
		public int Flid { get; private set; }

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			XmlUtils.SetAttribute(element, "flid", Flid.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement element)
		{
			base.InitXml(element);
			Flid = XmlUtils.GetMandatoryIntegerAttributeValue(element, "flid");
		}


		#region StringFinder Members
		/// <summary>
		/// Return the (one) string that is the value of the integer property.
		/// </summary>
		public override string[] Strings(int hvo)
		{
			return new[] { DataAccess.get_IntProp(hvo, Flid).ToString() };
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA.
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			var other2 = other as OwnIntPropFinder;
			if (other2 == null)
			{
				return false;
			}
			return other2.Flid == Flid && other2.DataAccess == DataAccess;
		}

		#endregion
	}
}