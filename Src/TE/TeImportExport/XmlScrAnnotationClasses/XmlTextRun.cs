// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: XmlTextRun.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Xml.Serialization;
using SIL.FieldWorks.Common.COMInterfaces;
using System.Diagnostics;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Common.Framework;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	#region class XmlTextRun
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores information about a single run of text in a paragraph
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("span")]
	public class XmlTextRun
	{
		/// <summary></summary>
		protected string m_text;

		/// <summary>The character style name</summary>
		[XmlIgnore]
		public string StyleName;

		#region XML attributes
		/// <summary>The default language for annotation data (expessed as an ICU locale)</summary>
		[XmlAttribute("xml:lang")]
		public string IcuLocale;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the style name (this is for OXES deserialization support, but is the
		/// same value as the StyleName property).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("type")]
		public virtual string RunType
		{
			get { return StyleName; }
			set { StyleName = value; }
		}

		#endregion

		#region XML Text
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The text of the run
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlText]
		public string Text
		{
			get { return m_text; }
			set { m_text = value; }
		}

		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlTextRun"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlTextRun()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlTextRun"/> class, based on the given
		/// run information
		/// </summary>
		/// <param name="wsDefault">The default writing system of the paragraph.</param>
		/// <param name="lgwsf">The writing system factory.</param>
		/// <param name="text">The text of the run.</param>
		/// <param name="props">The properties of the run.</param>
		/// ------------------------------------------------------------------------------------
		public XmlTextRun(int wsDefault, ILgWritingSystemFactory lgwsf, string text,
			ITsTextProps props)
		{
			int dummy;
			int wsRun = props.GetIntPropValues((int)FwTextPropType.ktptWs, out dummy);
			if (wsRun != wsDefault)
				IcuLocale = lgwsf.GetStrFromWs(wsRun);
			StyleName = props.GetStrPropValue((int)FwTextPropType.ktptNamedStyle);
			m_text = text;
		}

		#endregion

		#region Methods to write run to a paragraph builder
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds to para BLDR.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal virtual void AddToParaBldr(StTxtParaBldr bldr, int ws, FwStyleSheet styleSheet)
		{
			bldr.AppendRun(m_text, StyleUtils.CharStyleTextProps(StyleName, ws));
		}

		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return m_text;
		}
	}

	#endregion

	#region class XmlHyperlinkRun
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("a")]
	public class XmlHyperlinkRun : XmlTextRun
	{
		/// <summary></summary>
		[XmlAttribute("href")]
		public string Href;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is not necessary for hyperlink runs.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[XmlAttribute("type")]
		public override string RunType
		{
			get { return null; }
			set { ; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlHyperlinkRun"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlHyperlinkRun()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlHyperlinkRun"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlHyperlinkRun(int wsDefault, ILgWritingSystemFactory lgwsf, string text,
			ITsTextProps props)
			: base(wsDefault, lgwsf, text, props)
		{
			Href = TsStringUtils.GetURL(props.GetStrPropValue((int)FwTextPropType.ktptObjData));
			if (!string.IsNullOrEmpty(Href))
				Href = Href.TrimEnd(Environment.NewLine.ToCharArray());
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds to para BLDR.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal override void AddToParaBldr(StTxtParaBldr bldr, int ws, FwStyleSheet styleSheet)
		{
			if (string.IsNullOrEmpty(Href))
				throw new Exception("OXESA validation found an invalid URL.");

			bool fSucceeded = FwEditingHelper.AddHyperlink(bldr.StringBuilder,
				ws, Text, Href, styleSheet);
			Debug.Assert(fSucceeded);
		}
	}

	#endregion
}
