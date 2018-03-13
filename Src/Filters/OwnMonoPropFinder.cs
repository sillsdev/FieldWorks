// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace SIL.FieldWorks.Filters
{
	/// <summary>
	/// This class implements StringFinder by looking up one monlingual property of the object
	/// itself.
	/// </summary>
	public class OwnMonoPropFinder : StringFinderBase
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="T:OwnMonoPropFinder"/> class.
		/// </summary>
		public OwnMonoPropFinder(ISilDataAccess sda, int flid)
			: base(sda)
		{
			Flid = flid;
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="T:OwnMonoPropFinder"/> class.
		/// </summary>
		public OwnMonoPropFinder()
		{
		}

		/// <summary>
		/// Gets the flid.
		/// </summary>
		public int Flid { get; private set; }

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement node)
		{
			base.PersistAsXml(node);
			XmlUtils.SetAttribute(node, "flid", Flid.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement node)
		{
			base.InitXml(node);
			Flid = XmlUtils.GetMandatoryIntegerAttributeValue(node, "flid");
		}

		#region StringFinder Members

		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		public override string[] Strings(int hvo)
		{
			return new[] { DataAccess.get_StringProp(hvo, Flid).Text ?? string.Empty };
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA.
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			var other2 = other as OwnMonoPropFinder;
			if (other2 == null)
			{
				return false;
			}
			return other2.Flid == Flid && other2.DataAccess == DataAccess;
		}

		#endregion
	}
}