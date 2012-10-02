// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2011, SIL International. All Rights Reserved.
// <copyright from='2011' to='2011' company='SIL International'>
//		Copyright (c) 2011, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlKeyWord.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Encapsulates the information about a key term, including its list of renderings.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("orig")]
	public class XmlKeyWord
	{
		#region static members
		private static readonly Dictionary<int, string> s_mapWsToIcuLocale = new Dictionary<int, string>(2);
		#endregion

		#region XML attributes
		/// <summary>The default language for annotation data (expessed as an ICU locale)</summary>
		[XmlAttribute("xml:lang")]
		public string IcuLocale;
		#endregion

		#region XML Text
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The text of the run
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlText]
		public string Text { get; set; }
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlKeyWord"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlKeyWord()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlKeyWord"/> class, based on the given
		/// run information
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlKeyWord(IChkTerm term)
		{
			ITsString origLangTerm = term.OccurrencesOS.First().KeyWord;
			Debug.Assert(origLangTerm.RunCount == 1);
			int ws = origLangTerm.get_WritingSystem(0);
			string icuLocale;
			if (!s_mapWsToIcuLocale.TryGetValue(ws, out icuLocale))
			{
				ILgWritingSystemFactory lgwsf = term.Cache.LanguageWritingSystemFactoryAccessor;
				s_mapWsToIcuLocale[ws] = icuLocale = lgwsf.GetStrFromWs(ws);
			}
			IcuLocale = icuLocale;
			Text = origLangTerm.Text;
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Text;
		}
	}
}