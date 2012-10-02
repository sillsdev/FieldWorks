// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StTxtParaBldr.cs
// Responsibility: FieldWorks Team
// Last reviewed:
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Runtime.InteropServices; // needed for Marshal
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// This class makes it fun to build paragraphs as a hobby
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public sealed class StTxtParaBldr
	{
		#region Data members

		private FdoCache m_cache;

		/// <summary>Holds the paragraph style name to be used for creating new paragraphs</summary>
		private IParaStylePropsProxy m_ParaStyle;
		/// <summary>String builder to construct paragraph strings.</summary>
		private ITsStrBldr m_ParaStrBldr;
		/// <summary>TsTextProps for the paragraph.</summary>
		private string m_ParaStyleName;
		/// <summary>Unicode character properties engine</summary>
		private ILgCharacterPropertyEngine m_cpe;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StTxtParaBldr"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StTxtParaBldr(FdoCache cache)
		{
			System.Diagnostics.Debug.Assert(cache != null);
			m_cache = cache;

			ITsStrFactory tsStringFactory = cache.TsStrFactory;
			m_ParaStrBldr = tsStringFactory.GetBldr();

			// Give the builder a default WS so a created string will be legal. If any text
			// is added to the builder, it should replace this WS with the correct WS.
			m_ParaStrBldr.Replace(0, 0, null, StyleUtils.CharStyleTextProps(null, cache.DefaultVernWs));
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the paragraph style proxy to be used for creating the new paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public IParaStylePropsProxy ParaStylePropsProxy
		{
			get
			{
				return m_ParaStyle;
			}
			set
			{
				m_ParaStyle = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets the paragraph style name to be used for creating the new paragraph.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string ParaStyleName
		{
			private get
			{
				return (m_ParaStyle == null ? m_ParaStyleName : m_ParaStyle.StyleId);
			}
			set
			{
				m_ParaStyleName = value;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the length of the text in the ParaStrBldr
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int Length
		{
			get
			{
				return m_ParaStrBldr.Length;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Exposes the underlying ITsStrBldr
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ITsStrBldr StringBuilder
		{
			get
			{
				return m_ParaStrBldr;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// The current final character sent to the StTxtPara builder
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public char FinalCharInPara
		{
			get
			{
				string s = m_ParaStrBldr.Text;
				if (s == null)
					return (char)0;
				return s[s.Length - 1];
			}
		}

		#endregion

		#region Methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates an empty paragraph with the given paragraph style and writing system.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="owner"></param>
		/// <param name="paraStyle"></param>
		/// <param name="ws"></param>
		/// ------------------------------------------------------------------------------------
		public static void CreateEmptyPara(FdoCache cache, IStText owner, string paraStyle, int ws)
		{
			var bldr = new StTxtParaBldr(cache);
			bldr.ParaStyleName = paraStyle;
			bldr.AppendRun(String.Empty, StyleUtils.CharStyleTextProps(null, ws));
			bldr.CreateParagraph(owner);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Appends a run of text with the given TsTextProps.
		/// </summary>
		/// <param name="sRun">The text to append</param>
		/// <param name="props">The properties to use</param>
		/// ------------------------------------------------------------------------------------
		public void AppendRun(string sRun, ITsTextProps props)
		{
			//note: For efficiency, we usually skip the Replace() if the string is empty.
			// However, if the builder is has Length == 0, then we want to replace the
			// properties on the empty run of the TsString.
			// A TsString always has at least one run, even if it is empty, and this controls
			// the props when the user begins to enter text in an empty para.
			if (sRun != string.Empty || Length == 0)
			{
				System.Diagnostics.Debug.Assert(props != null);
				int var;
				int ws = props.GetIntPropValues((int)FwTextPropType.ktptWs, out var);

				// Make sure we handle the magic writing systems
				if (ws == (int)CellarModuleDefns.kwsAnal)
				{
					// default analysis writing system
					ITsPropsBldr bldr = props.GetBldr();
					bldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
						m_cache.DefaultAnalWs);
					props = bldr.GetTextProps();
				}
				else if (ws == (int)CellarModuleDefns.kwsVern)
				{
					// default vernacular writing system
					ITsPropsBldr bldr = props.GetBldr();
					bldr.SetIntPropValues((int)FwTextPropType.ktptWs, 0,
						m_cache.DefaultVernWs);
					props = bldr.GetTextProps();
				}
				else
				{
				System.Diagnostics.Debug.Assert(ws > 0);	// not quite right if >2G objects.
				}
				m_ParaStrBldr.Replace(Length, Length, sRun, props);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new <see cref="IStTxtPara"/>, owned by the given <see cref="IStText"/>.
		/// Set it with data accumulated in this builder.
		/// </summary>
		/// <param name="owner">The <see cref="IStText"/> that is to own the new paragraph</param>
		/// <param name="iPos">0-based index of the position in the sequence of paragraphs where the
		/// new paragraph is to be inserted. If a paragraph is already in this position, the new
		/// paragraph will be inserted before the existing paragraph.</param>
		/// <returns>A new StTextPara whose contents are built up from the prior calls
		/// to <see cref="AppendRun"/> and whose style is set based on the current value of
		/// <see cref="ParaStylePropsProxy"/>.</returns>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara CreateParagraph(IStText owner, int iPos)
		{
			// insert a new para in the owner's collection
			IStTxtPara para = owner.InsertNewTextPara(iPos, ParaStyleName);
			SetStTxtParaPropertiesAndClearBuilder(para);

			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Append a new <see cref="IStTxtPara"/> to the given <see cref="IStText"/>.
		/// Set it with data accumulated in this builder.
		/// </summary>
		/// <param name="owner">The <see cref="IStText"/> that is to own the new paragraph</param>
		/// <returns>A new StTextPara whose contents are built up from the prior calls
		/// to <see cref="AppendRun"/> and whose style is set based on the current value of
		/// <see cref="ParaStylePropsProxy"/>.</returns>
		/// ------------------------------------------------------------------------------------
		public IStTxtPara CreateParagraph(IStText owner)
		{
			// insert a new para in the owner's collection
			IStTxtPara para = owner.AddNewTextPara(ParaStyleName);
			SetStTxtParaPropertiesAndClearBuilder(para);

			return para;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets the StyleRules and Contents properties for the new <see cref="IStTxtPara"/>;
		/// then clears the builder.
		/// </summary>
		/// <param name="para">The <see cref="IStTxtPara"/> that was just created</param>
		/// ------------------------------------------------------------------------------------
		private void SetStTxtParaPropertiesAndClearBuilder(IStTxtPara para)
		{
			// sets the new StTxtPara properties, with contents built up from prior calls
			para.Contents = m_ParaStrBldr.GetString();

			// Clear the builder, for a new paragraph. Give the builder a default WS so a
			// created string will be legal. If any text is added to the builder, it should
			// replace this WS with the correct WS.
			m_ParaStrBldr.Replace(0, Length, null, StyleUtils.CharStyleTextProps(null, m_cache.DefaultVernWs));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// After last call to AppendRun for the current paragraph, but before calling
		/// CreateParagraph, call this method to trim the last character in the builder
		/// if it is a trailing space.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void TrimTrailingSpaceInPara()
		{
			if (m_cpe == null)
				m_cpe = m_cache.ServiceLocator.UnicodeCharProps;
			// check if the last char sent to the builder is a space
			if (Length != 0 && m_cpe.get_IsSeparator(FinalCharInPara))
				m_ParaStrBldr.Replace(Length - 1, Length, null, null);
		}
		#endregion
	}
}
