// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// This class implements StringFinder by looking up one monolingual property of the object
	/// itself.
	/// </summary>
	internal sealed class OwnMonoPropFinder : StringFinderBase
	{
		/// <summary />
		internal OwnMonoPropFinder(ISilDataAccess sda, int flid)
			: base(sda)
		{
			ConstructorSurrogate(flid);
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal OwnMonoPropFinder(XElement element)
		{
			ConstructorSurrogate(XmlUtils.GetMandatoryIntegerAttributeValue(element, "flid"));
		}

		private void ConstructorSurrogate(int flid)
		{
			Flid = flid;
		}

		/// <summary>
		/// Gets the flid.
		/// </summary>
		internal int Flid { get; private set; }

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			XmlUtils.SetAttribute(element, "flid", Flid.ToString());
		}

		#region StringFinder Members

		/// <summary>
		/// Strings the specified hvo.
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
			return other is OwnMonoPropFinder ownMonoPropFinder && ownMonoPropFinder.Flid == Flid && ownMonoPropFinder.DataAccess == DataAccess;
		}

		#endregion
	}
}