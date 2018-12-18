// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// This class implements StringFinder in a way appropriate for a cell that shows a single
	/// ML alternative from an object that is the value of an atomic property. We return the value of the
	/// displayed property for the target object.
	/// </summary>
	public class OneIndirectAtomMlPropFinder : StringFinderBase
	{
		/// <summary />
		public OneIndirectAtomMlPropFinder(ISilDataAccess sda, int flidAtom, int flidString, int ws)
			: base(sda)
		{
			FlidAtom = flidAtom;
			FlidString = flidString;
			Ws = ws;
		}

		/// <summary />
		public OneIndirectAtomMlPropFinder()
		{
		}

		/// <summary>
		/// Gets the flid atom.
		/// </summary>
		public int FlidAtom { get; private set; }

		/// <summary>
		/// Gets the flid string.
		/// </summary>
		public int FlidString { get; private set; }

		/// <summary>
		/// Gets the ws.
		/// </summary>
		public int Ws { get; private set; }

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			base.PersistAsXml(element);
			XmlUtils.SetAttribute(element, "flidAtom", FlidAtom.ToString());
			XmlUtils.SetAttribute(element, "flidString", FlidString.ToString());
			XmlUtils.SetAttribute(element, "ws", Ws.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(XElement element)
		{
			base.InitXml(element);
			FlidAtom = XmlUtils.GetMandatoryIntegerAttributeValue(element, "flidAtom");
			FlidString = XmlUtils.GetMandatoryIntegerAttributeValue(element, "flidString");
			Ws = XmlUtils.GetMandatoryIntegerAttributeValue(element, "ws");
		}

		#region StringFinder Members

		/// <summary>
		/// Stringses the specified hvo.
		/// </summary>
		public override string[] Strings(int hvo)
		{
			return new[] { DataAccess.get_MultiStringAlt(DataAccess.get_ObjectProp(hvo, FlidAtom), FlidString, Ws).Text ?? string.Empty };
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA.
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			var other2 = other as OneIndirectAtomMlPropFinder;
			if (other2 == null)
			{
				return false;
			}
			return other2.FlidAtom == FlidAtom && other2.DataAccess == DataAccess && other2.FlidString == FlidString && other2.Ws == Ws;
		}

		#endregion
	}
}