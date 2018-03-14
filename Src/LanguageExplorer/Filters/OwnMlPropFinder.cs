// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// This class implements StringFinder by looking up one multilingual property of the object
	/// itself.
	/// </summary>
	public class OwnMlPropFinder : StringFinderBase
	{
		/// <summary>
		/// Make one.
		/// </summary>
		public OwnMlPropFinder(ISilDataAccess sda, int flid, int ws)
			: base(sda)
		{
			Flid = flid;
			Ws = ws;
		}

		/// <summary>
		/// For persistence with IPersistAsXml
		/// </summary>
		public OwnMlPropFinder()
		{
		}

		/// <summary>
		/// Gets the flid.
		/// </summary>
		public int Flid { get; private set; }

		/// <summary>
		/// Gets the ws.
		/// </summary>
		public int Ws { get; }

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
			return new[] { DataAccess.get_MultiStringAlt(hvo, Flid, Ws).Text ?? string.Empty };
		}

		/// <summary>
		/// Same if it is the same type for the same flid, ws, and DA.
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			var other2 = other as OwnMlPropFinder;
			return other2 != null && (other2.Flid == Flid && other2.DataAccess == DataAccess && other2.Ws == Ws);
		}

		/// <summary>
		/// Keys the specified item.
		/// </summary>
		public override ITsString Key(IManyOnePathSortItem item)
		{
			return DataAccess.get_MultiStringAlt(item.KeyObject, Flid, Ws);
		}

		#endregion
	}
}