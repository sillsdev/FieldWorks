// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: StyleInfoTable.cs
// Responsibility: TE Team

using System;
using System.Diagnostics;
using System.Collections.Generic;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.DomainServices
{
	#region class StyleInfoTable
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// In-memory "stylesheet" for objects that store information about a style that is part of a
	/// StyleInfoTable
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StyleInfoTable : SortedDictionary<string, BaseStyleInfo>
	{
		#region Data members
		private int m_nextStyleNumber = 1;
		private readonly WritingSystemManager m_wsManager;
		private readonly string m_normalStyleName;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="StyleInfoTable"/> class.
		/// </summary>
		/// <param name="normalStyleName">the name of the normal style</param>
		/// <param name="wsManager">The Writing System Factory (needed to resolve magic font names
		/// to real ones)</param>
		/// ------------------------------------------------------------------------------------
		public StyleInfoTable(string normalStyleName, WritingSystemManager wsManager)
		{
			m_wsManager = wsManager;
			m_normalStyleName = normalStyleName;
		}
		#endregion

		#region public methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the given style info entry to the table.
		/// </summary>
		/// <param name="key">The key of the element to add (typically a TE Stylename, but may
		/// be another unique token (if this entry represents a style which is not known to
		/// exist)</param>
		/// <param name="value">The value of the element to add (must not be null)</param>
		/// <exception cref="T:System.ArgumentException">An element with the same key already
		/// exists in the <see cref="T:System.Collections.Generic.Dictionary`2"></see>.</exception>
		/// <exception cref="T:System.ArgumentNullException">key or value is null.</exception>
		/// ------------------------------------------------------------------------------------
		public new virtual void Add(string key, BaseStyleInfo value)
		{
			if (value == null)
				throw new ArgumentNullException("value");
			base.Add(key, value);
			value.m_styleNumber = m_nextStyleNumber++;
			value.m_owningTable = this;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the <see cref="BaseStyleInfo"/> with the specified key. Do NOT use the setter
		/// to add or attempt to change an element in the collection! Instead use the Add method.
		/// </summary>
		/// <exception cref="InvalidOperationException">For adding an element to the table, use
		/// the Add method instead of this setter.</exception>
		/// ------------------------------------------------------------------------------------
		public new BaseStyleInfo this[string key]
		{
			get { return base[key]; }
			set	{ throw new InvalidOperationException("Use the Add method instead"); }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Connect the "based on" and "next" styles in the style table. Also fix up any
		/// inherited style attributes.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ConnectStyles(/*bool fSetDefaultsForBaselessCharStyles*/)
		{
			// connect "based on" and "next" styles
			foreach (BaseStyleInfo entry in Values)
			{
				// Get the "based on" style and set it.
				if (entry.m_basedOnStyleName == null)
				{
					if (entry.IsParagraphStyle)
					{
						// "Normal" style isn't based on anything
						entry.SetAllDefaults();
					}
				}
				else if (!ContainsKey(entry.m_basedOnStyleName))
				{
					// If the based on style no longer exists then change to the normal style
					// for paragraph styles and nothing for character styles, which will
					// equate to "Default Paragraph Characters"
					if (entry.IsParagraphStyle)
					{
						entry.m_basedOnStyleName = m_normalStyleName;
						entry.m_basedOnStyle = this[m_normalStyleName];
					}
					else
					{
						entry.m_basedOnStyleName = null;
						entry.m_basedOnStyle = null;
					}
				}
				else
					entry.m_basedOnStyle = this[entry.m_basedOnStyleName];

				// Get the "next" style and set it. If the following style no longer exists
				// then set it to self. Only do this for paragraph styles.
				if (entry.IsParagraphStyle)
				{
					if (entry.m_nextStyleName == null || !ContainsKey(entry.m_nextStyleName))
					{
						// If the following style no longer exists then set it to self
						entry.m_nextStyleName = entry.Name;
						entry.m_nextStyle = entry;
					}
					else
						entry.m_nextStyle = this[entry.m_nextStyleName];
				}
			}

			// fix up inherited style properties
			foreach (BaseStyleInfo entry in Values)
				SetInheritedProps(entry);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Set the inherited properties for a style from the based-on styles.
		/// </summary>
		/// <param name="style"></param>
		/// ------------------------------------------------------------------------------------
		private static void SetInheritedProps(BaseStyleInfo style)
		{
			if (style.m_basedOnStyle != null && style.Name != style.m_basedOnStyleName)
			{
				// Go up the inheritance chain
				SetInheritedProps(style.m_basedOnStyle);

				SetFontInheritance(style.m_defaultFontInfo, style.m_basedOnStyle.m_defaultFontInfo);

				if (style.IsParagraphStyle)
				{
					style.m_rtl.InheritValue(style.m_basedOnStyle.m_rtl);
					style.m_alignment.InheritValue(style.m_basedOnStyle.m_alignment);
					style.m_lineSpacing.InheritValue(style.m_basedOnStyle.m_lineSpacing);
					style.m_spaceBefore.InheritValue(style.m_basedOnStyle.m_spaceBefore);
					style.m_spaceAfter.InheritValue(style.m_basedOnStyle.m_spaceAfter);
					style.m_firstLineIndent.InheritValue(style.m_basedOnStyle.m_firstLineIndent);
					style.m_leadingIndent.InheritValue(style.m_basedOnStyle.m_leadingIndent);
					style.m_trailingIndent.InheritValue(style.m_basedOnStyle.m_trailingIndent);
					style.m_border.InheritValue(style.m_basedOnStyle.m_border);
					style.m_borderColor.InheritValue(style.m_basedOnStyle.m_borderColor);
					style.m_bulletInfo.InheritValue(style.m_basedOnStyle.m_bulletInfo);
				}
			}

			// set the inheritance for the ws font overrides so that the overrides are
			// based on the defaults.
			foreach (FontInfo info in style.m_fontInfoOverrides.Values)
				SetFontInheritance(info, style.m_defaultFontInfo);

			if (style.m_basedOnStyle != null && style.Name != style.m_basedOnStyleName)
			{
				// update the inheritance for the ws font overrides so that the overrides are
				// based on the based on style.
				foreach (int ws in style.m_fontInfoOverrides.Keys)
				{
					SetFontInheritance(style.m_fontInfoOverrides[ws],
						style.m_basedOnStyle.m_fontInfoOverrides[ws]);
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Sets up inheritance of font information from a based-on FontInfo to a destination
		/// FontInfo.
		/// </summary>
		/// <param name="dst">The destination font info</param>
		/// <param name="basedOn">The based-on font info</param>
		/// ------------------------------------------------------------------------------------
		private static void SetFontInheritance(FontInfo dst, FontInfo basedOn)
		{
			dst.m_bold.InheritValue(basedOn.m_bold);
			dst.m_italic.InheritValue(basedOn.m_italic);
			dst.m_fontSize.InheritValue(basedOn.m_fontSize);
			dst.m_fontName.InheritValue(basedOn.m_fontName);
			dst.m_fontColor.InheritValue(basedOn.m_fontColor);
			dst.m_backColor.InheritValue(basedOn.m_backColor);
			dst.m_superSub.InheritValue(basedOn.m_superSub);
			dst.m_underline.InheritValue(basedOn.m_underline);
			dst.m_underlineColor.InheritValue(basedOn.m_underlineColor);
			dst.m_offset.InheritValue(basedOn.m_offset);
			dst.m_features.InheritValue(basedOn.m_features);
		}
		#endregion

		#region internal methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Resolve a magic font name to the real font name for the given writing system.
		/// </summary>
		/// <param name="fontName"></param>
		/// <param name="ws"></param>
		/// <exception cref="InvalidOperationException">Thrown if StyleInfoTable was constructed
		/// with a null writing system factory</exception>
		/// ------------------------------------------------------------------------------------
		internal string ResolveMagicFontName(string fontName, int ws)
		{
			if (m_wsManager == null)
				throw new InvalidOperationException("StyleInfoTable was constructed with a null writing system store. Cannot resolve magic font name.");

			CoreWritingSystemDefinition wsObj = m_wsManager.Get(ws);
			switch (fontName)
			{
				case StyleServices.DefaultFont:
					return wsObj.DefaultFontName;
				default:
					Debug.Fail("ResolveMagicFontName called with unexpected (non-magic?) font name.");
					return fontName; // This method probably shouldn't have been called, but oh well...
			}
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the underyling right-to-left value based on the context.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected internal virtual TriStateBool DefaultRightToLeft
		{
			get { return TriStateBool.triNotSet; }
		}
		#endregion
	}
	#endregion
}
