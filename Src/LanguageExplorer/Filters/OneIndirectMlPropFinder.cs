// Copyright (c) 2004-2018 SIL International
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
	public class OneIndirectMlPropFinder : StringFinderBase
	{
		/// <summary />
		public OneIndirectMlPropFinder(ISilDataAccess sda, int flidVec, int flidString, int ws)
			: base(sda)
		{
			FlidVec = flidVec;
			FlidString = flidString;
			Ws = ws;
		}

		/// <summary>
		/// Gets the flid vec.
		/// </summary>
		public int FlidVec { get; private set; }

		/// <summary>
		/// Gets the flid string.
		/// </summary>
		public int FlidString { get; private set; }

		/// <summary>
		/// Gets the ws.
		/// </summary>
		public int Ws { get; private set; }

		/// <summary>
		/// For use with IPersistAsXml
		/// </summary>
		public OneIndirectMlPropFinder()
		{
		}

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
		public override void InitXml(XElement element)
		{
			base.InitXml(element);
			FlidVec = XmlUtils.GetMandatoryIntegerAttributeValue(element, "flidVec");
			FlidString = XmlUtils.GetMandatoryIntegerAttributeValue(element, "flidString");
			Ws = XmlUtils.GetMandatoryIntegerAttributeValue(element, "ws");
		}

		#region StringFinder Members

		/// <summary>
		/// Stringses the specified hvo.
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
			var other2 = other as OneIndirectMlPropFinder;
			if (other2 == null)
			{
				return false;
			}
			return other2.FlidVec == FlidVec && other2.DataAccess == DataAccess && other2.FlidString == FlidString && other2.Ws == Ws;
		}
		#endregion
	}
}