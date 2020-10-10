// Copyright (c) 2004-2020 SIL International
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
	internal sealed class OwnMlPropFinder : StringFinderBase
	{
		/// <summary />
		internal OwnMlPropFinder(ISilDataAccess sda, int flid, int ws)
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
		internal int Flid { get; private set; }

		/// <summary>
		/// Gets the ws.
		/// </summary>
		internal int Ws { get; }

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
		/// Strings the specified hvo.
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
			return other is OwnMlPropFinder ownMlPropFinder && ownMlPropFinder.Flid == Flid && ownMlPropFinder.DataAccess == DataAccess && ownMlPropFinder.Ws == Ws;
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