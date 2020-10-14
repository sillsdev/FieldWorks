// Copyright (c) 2004-2020 SIL International
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
	internal sealed class OneIndirectAtomMlPropFinder : StringFinderBase
	{
		/// <summary />
		internal OneIndirectAtomMlPropFinder(ISilDataAccess sda, int flidAtom, int flidString, int ws)
			: base(sda)
		{
			ConstructorSurrogate(flidAtom, flidString, ws);
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		internal OneIndirectAtomMlPropFinder(XElement element)
		{
			ConstructorSurrogate(XmlUtils.GetMandatoryIntegerAttributeValue(element, "flidAtom"),
				XmlUtils.GetMandatoryIntegerAttributeValue(element, "flidString"),
				XmlUtils.GetMandatoryIntegerAttributeValue(element, "ws"));
		}

		private void ConstructorSurrogate(int flidAtom, int flidString, int ws)
		{
			FlidAtom = flidAtom;
			FlidString = flidString;
			Ws = ws;
		}

		/// <summary>
		/// Gets the flid atom.
		/// </summary>
		internal int FlidAtom { get; private set; }

		/// <summary>
		/// Gets the flid string.
		/// </summary>
		internal int FlidString { get; private set; }

		/// <summary>
		/// Gets the ws.
		/// </summary>
		internal int Ws { get; private set; }

		/// <summary>
		/// Persists as XML.
		/// </summary>
		public override void PersistAsXml(XElement element)
		{
			XmlUtils.SetAttribute(element, "flidAtom", FlidAtom.ToString());
			XmlUtils.SetAttribute(element, "flidString", FlidString.ToString());
			XmlUtils.SetAttribute(element, "ws", Ws.ToString());
		}

		#region StringFinder Members

		/// <summary>
		/// Strings the specified hvo.
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
			return other is OneIndirectAtomMlPropFinder oneIndirectAtomMlPropFinder && oneIndirectAtomMlPropFinder.FlidAtom == FlidAtom
																					&& oneIndirectAtomMlPropFinder.DataAccess == DataAccess
																					&& oneIndirectAtomMlPropFinder.FlidString == FlidString
																					&& oneIndirectAtomMlPropFinder.Ws == Ws;
		}

		#endregion
	}
}