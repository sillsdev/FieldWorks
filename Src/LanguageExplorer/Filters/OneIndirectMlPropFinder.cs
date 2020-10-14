// Copyright (c) 2004-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Xml.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.Xml;

namespace LanguageExplorer.Filters
{
	/// <summary>
	/// This class implements StringFinder in a way appropriate for a cell that shows a sequence
	/// of values from some kind of sequence or collection. We return the values of the
	/// displayed property for each item in the sequence.
	/// </summary>
	internal sealed class OneIndirectMlPropFinder : StringFinderBase
	{
		/// <summary />
		internal OneIndirectMlPropFinder(ISilDataAccess sda, int flidVec, int flidString, int ws)
			: base(sda)
		{
			FlidVec = flidVec;
			FlidString = flidString;
			Ws = ws;
		}

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public OneIndirectMlPropFinder()
		{
		}

		/// <summary>
		/// Gets the flid vec.
		/// </summary>
		internal int FlidVec { get; private set; }

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
			base.PersistAsXml(element);
			XmlUtils.SetAttribute(element, "flidVec", FlidVec.ToString());
			XmlUtils.SetAttribute(element, "flidString", FlidString.ToString());
			XmlUtils.SetAttribute(element, "ws", Ws.ToString());
		}

		/// <summary>
		/// Inits the XML.
		/// </summary>
		public override void InitXml(IPersistAsXmlFactory factory, XElement element)
		{
			base.InitXml(factory, element);
			FlidVec = XmlUtils.GetMandatoryIntegerAttributeValue(element, "flidVec");
			FlidString = XmlUtils.GetMandatoryIntegerAttributeValue(element, "flidString");
			Ws = XmlUtils.GetMandatoryIntegerAttributeValue(element, "ws");
		}

		#region StringFinder Members

		/// <summary>
		/// Strings the specified hvo.
		/// </summary>
		public override string[] Strings(int hvo)
		{
			var count = DataAccess.get_VecSize(hvo, FlidVec);
			var result = new string[count];
			for (var i = 0; i < count; ++i)
			{
				result[i] = DataAccess.get_MultiStringAlt(DataAccess.get_VecItem(hvo, FlidVec, i), FlidString, Ws).Text ?? string.Empty;
			}
			return result;
		}

		/// <summary>
		/// Same if it is the same type for the same flid and DA, etc.
		/// </summary>
		public override bool SameFinder(IStringFinder other)
		{
			return other is OneIndirectMlPropFinder oneIndirectMlPropFinder && oneIndirectMlPropFinder.FlidVec == FlidVec
																			&& oneIndirectMlPropFinder.DataAccess == DataAccess
																			&& oneIndirectMlPropFinder.FlidString == FlidString
																			&& oneIndirectMlPropFinder.Ws == Ws;
		}
		#endregion
	}
}