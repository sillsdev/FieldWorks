// Copyright (c) 2004-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: TestFwStylesheet.cs
// Responsibility: TE Team
//
// <remarks>
// This is a simple implementation of IVwStylesheet to facilitate testing without a DB
// </remarks>

using System;
using System.Collections.Generic;
using System.Diagnostics;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.Test.TestUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Struct which represents an overriden font for a writing system
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public struct FontOverride
	{
		/// <summary>Writing system to override font for</summary>
		public int writingSystem;
		/// <summary>Font size in Points</summary>
		public int fontSize;
	}


	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// TestFwStylesheet class is based on a mocked IVwStylesheet, with some simple real
	/// implementations as necessary.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class TestFwStylesheet: IVwStylesheet
	{
		private class TestStyle
		{
			public int Hvo;
			public string Name;
			public int BasedOnStyle;
			public int NextStyle;
			public int Type;
			public bool IsBuiltIn;
			public bool IsModified;
			public ITsTextProps Rules;
		}

		/// <summary>Collection of TestStyle objects</summary>
		private List<TestStyle> m_rgStyles = new List<TestStyle>();

		/// <summary>Dictionary of style names to ITsTextProps</summary>
		/// <remarks>These text props are fully derived from the style chain it is based on</remarks>
		private Dictionary<string, ITsTextProps> m_htStyleRules = new Dictionary<string, ITsTextProps>();

		/// <summary>Used for assigning HVO's</summary>
		private int m_hvoLast = 0;

		#region Methods of IVwStylesheet
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the default paragraph style to use as the base for new styles
		/// (Usually "Normal")
		/// </summary>
		/// <returns>Always returns "Normal"</returns>
		/// ------------------------------------------------------------------------------------
		public string GetDefaultBasedOnStyleName()
		{
			return "Normal";
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the style name that is the default style to use for the given context
		/// </summary>
		/// <param name="nContext">the context</param>
		/// <param name="fCharStyle"><c>true</c> if the style is a character style;
		/// otherwise <c>false</c></param>
		/// <returns>Name of the style that is the default for the context</returns>
		/// ------------------------------------------------------------------------------------
		public virtual string GetDefaultStyleForContext(int nContext,bool fCharStyle)
		{
			return GetDefaultBasedOnStyleName();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Store style details in the array.
		/// </summary>
		///
		/// <param name="sName">The style name</param>
		/// <param name="saUsage">The usage information for the style</param>
		/// <param name="hvoStyle">The style to be stored</param>
		/// <param name="hvoBasedOn">What the style is based on</param>
		/// <param name="hvoNext">The next Style</param>
		/// <param name="nType">The Style type</param>
		/// <param name="fBuiltIn">True if predefined style</param>
		/// <param name="fModified">True if user has modified predefined style</param>
		/// <param name="ttp">TextProps, contains the formatting of the style</param>
		/// -----------------------------------------------------------------------------------
		public void PutStyle(string sName, string saUsage, int hvoStyle, int hvoBasedOn,
			int hvoNext, int nType, bool fBuiltIn, bool fModified, ITsTextProps ttp)
		{
			TestStyle style = null; // our local reference

			// Get the matching TestStyle from the List of styles, if it's there
			foreach (TestStyle stStyle in m_rgStyles)
			{
				if (stStyle.Hvo == hvoStyle)
				{
					style = stStyle;
					break;
				}
			}
			// If the hvoStyle is not in the List, this is a new style;
			// create a new TestStyle and insert it into the List of styles
			if (style == null)
			{
				style = new TestStyle();
				style.Hvo = hvoStyle;
				m_rgStyles.Add(style);
			}

			// Save the style properties in the fdocache's style object
			style.Name = sName;
			style.BasedOnStyle = hvoBasedOn;
			style.NextStyle = hvoNext;
			style.Type = nType;
			style.IsBuiltIn = fBuiltIn;
			style.IsModified = fModified;
			style.Rules = ttp;

			m_htStyleRules[sName] = ttp;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Retrieve the properties for the style named sName.
		/// </summary>
		///
		/// <param name="cch">Length of the style name - not used</param>
		/// <param name="sName">The style name</param>
		/// <returns>TextProps with retrieved properties, or null if not found</returns>
		/// ------------------------------------------------------------------------------------
		public ITsTextProps GetStyleRgch(int cch, string sName)
		{
			if (m_htStyleRules.ContainsKey(sName))
				return m_htStyleRules[sName];
			return null;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the next style that will be used if the user types a CR at the end of this
		/// paragraph. If the input is null, return "Normal".
		/// </summary>
		///
		/// <param name="sName">Name of the style for this paragraph.</param>
		/// <returns>Name of the style for the next paragraph.</returns>
		/// -----------------------------------------------------------------------------------
		public string GetNextStyle(string sName)
		{
			TestStyle style = FindStyle(sName);
			if (style != null && style.NextStyle != 0)
			{
				string sNameNext = GetStyleName(style.NextStyle);
				return (sNameNext != null) ? sNameNext : "Normal";
			}
			return "Normal";
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the basedOn style name for the style.
		/// </summary>
		///
		/// <param name="sName">Name of the style</param>
		/// <returns>Name of the BasedOn style if available, otherwise empty string</returns>
		/// -----------------------------------------------------------------------------------
		public string GetBasedOn(string sName)
		{
			TestStyle style = FindStyle(sName);
			if (style != null && style.BasedOnStyle != 0)
			{
				string sNameBasedOn = GetStyleName(style.BasedOnStyle);
				return (sNameBasedOn != null) ? sNameBasedOn : string.Empty;
			}
			return string.Empty;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the type for the style
		/// </summary>
		///
		/// <param name="sName">Style name</param>
		/// <returns>Returns type of the style (0 by default)</returns>
		/// -----------------------------------------------------------------------------------
		public int GetType(string sName)
		{
			TestStyle style = FindStyle(sName);
			if (style != null)
				return style.Type;

			return 0;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the context for the style
		/// </summary>
		///
		/// <param name="sName">Style name</param>
		/// <returns>Returns context of the style (0 by default)</returns>
		/// -----------------------------------------------------------------------------------
		public int GetContext(string sName)
		{
			return 0;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Is the style named sName a predefined style?
		/// </summary>
		///
		/// <param name="sName"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public bool IsBuiltIn(string sName)
		{
			TestStyle style = FindStyle(sName);
			if (style != null)
				return style.IsBuiltIn;
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Was the (predefined) style named sName changed by the user?
		/// </summary>
		///
		/// <param name="sName"></param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public bool IsModified(string sName)
		{
			TestStyle style = FindStyle(sName);
			if (style != null)
				return style.IsModified;
			return false;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return the associated Data Access object
		/// </summary>
		///
		/// <returns>Return the associated Data Access object</returns>
		/// -----------------------------------------------------------------------------------
		public ISilDataAccess DataAccess
		{
			get { return null; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style and return its HVO.
		/// </summary>
		///
		/// <returns>The hvo of the new object, or 0 if not created.</returns>
		/// -----------------------------------------------------------------------------------
		public int MakeNewStyle()
		{
			return GetNewStyleHvo(); //create the style
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return an HVO for a newly created style.
		/// </summary>
		/// <returns>The hvo of the new object.</returns>
		/// -----------------------------------------------------------------------------------
		protected virtual int GetNewStyleHvo()
		{
			return ++m_hvoLast;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Delete a style from a stylesheet.
		/// </summary>
		/// <param name="hvoStyle">ID of style to delete.</param>
		/// -----------------------------------------------------------------------------------
		public void Delete(int hvoStyle)
		{
			for (int i = 0; i < m_rgStyles.Count; i ++)
			{
				TestStyle style = m_rgStyles[i];
				if (style.Hvo == hvoStyle)
				{
					{
						m_rgStyles.RemoveAt(i);
						m_htStyleRules.Remove(style.Name);
						return;
					}
				}
			}

		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get number of styles in sheet.
		/// </summary>
		/// <returns>Number of styles in sheet.</returns>
		/// -----------------------------------------------------------------------------------
		public int CStyles
		{
			get { return m_rgStyles.Count; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the HVO of the Nth style (in an arbitrary order).
		/// </summary>
		///
		/// <param name="iStyle">Index of the style</param>
		/// <returns>HVO</returns>
		/// -----------------------------------------------------------------------------------
		public int get_NthStyle(int iStyle)
		{
			Debug.Assert(0 <= iStyle && iStyle < m_rgStyles.Count);

			return m_rgStyles[iStyle].Hvo;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the name of the Nth style (in an arbitrary order).
		/// </summary>
		///
		/// <param name="iStyle">Index of the style</param>
		/// <returns></returns>
		/// -----------------------------------------------------------------------------------
		public string get_NthStyleName(int iStyle)
		{
			Debug.Assert(0 <= iStyle && iStyle < m_rgStyles.Count);

			return m_rgStyles[iStyle].Name;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return null.
		/// </summary>
		///
		/// <returns>null</returns>
		/// -----------------------------------------------------------------------------------
		public ITsTextProps NormalFontStyle
		{
			get { return null; }
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Return true if the given style is one that is protected within the style sheet.
		/// </summary>
		///
		/// <param name="sName">Name of style</param>
		/// <returns>True if style is protected</returns>
		/// <remarks>This is a default implementation that returns the built-in flag.
		/// Specialized style sheets may derive their own version.
		/// </remarks>
		/// -----------------------------------------------------------------------------------
		public bool get_IsStyleProtected(string sName)
		{
			return IsBuiltIn(sName);
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// No-op.
		/// </summary>
		///
		/// <param name="cch"></param>
		/// <param name="sName"></param>
		/// <param name="hvoStyle"></param>
		/// <param name="ttp"></param>
		/// -----------------------------------------------------------------------------------
		public void CacheProps(int cch, string sName, int hvoStyle, ITsTextProps ttp)
		{
		}
		#endregion

		#region Other methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find the style with specified name
		/// </summary>
		/// <param name="name">Stylename to find</param>
		/// <returns>TestStyle if found, otherwise null.</returns>
		/// -----------------------------------------------------------------------------------
		private TestStyle FindStyle(string name)
		{
			foreach (TestStyle style in m_rgStyles)
			{
				if (style.Name == name)
					return style;
			}

			return null; //not found
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Find the style with specified hvo
		/// </summary>
		/// <param name="hvo">Style ID to find</param>
		/// <returns>Name of the style if found, otherwise null.</returns>
		/// -----------------------------------------------------------------------------------
		private string GetStyleName(int hvo)
		{
			foreach (TestStyle stStyle in m_rgStyles)
			{
				if (stStyle.Hvo == hvo)
					return stStyle.Name;
			}
			return null;
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Set the StyleWs property for the given style, creating an override for the font
		/// for each element in fontOverrides.
		/// </summary>
		/// <param name="styleName">Name of the style whose properties are to be set</param>
		/// <param name="fontOverrides">List of FontOverride's</param>
		/// -----------------------------------------------------------------------------------
		public void OverrideFontsForWritingSystems(string styleName, List<FontOverride> fontOverrides)
		{
			TestStyle style = FindStyle(styleName);
			ITsPropsBldr propsBldr;
			if (style.Rules != null)
				propsBldr = style.Rules.GetBldr();
			else
				propsBldr = TsPropsBldrClass.Create();

			string propsAsString = String.Empty;
			//Byte[] buffer = new Byte[8 * fontOverrides.Count];
			//int i = 0;
			foreach (FontOverride aFontOverride in fontOverrides)
			{
				// First two characters are the lower and upper words of the writing system ID
				propsAsString += Convert.ToChar(aFontOverride.writingSystem & 0xffff);
				propsAsString += Convert.ToChar(aFontOverride.writingSystem >> 16);
				// Next character is length of font family name (We aren't overriding this)
				propsAsString += Convert.ToChar(0);
				// Next character is the count of overridden properties (always 1)
				propsAsString += Convert.ToChar(1);
				propsAsString += Convert.ToChar((Int16)FwTextPropType.ktptFontSize);
				propsAsString += Convert.ToChar((Int16)FwTextPropVar.ktpvMilliPoint);
				propsAsString += Convert.ToChar((aFontOverride.fontSize * 1000) & 0xffff);
				propsAsString += Convert.ToChar((aFontOverride.fontSize * 1000) >> 16);
//				buffer[i++] = (byte)(aFontOverride.writingSystem & 0xffff);
//				buffer[i++] = (byte)(aFontOverride.writingSystem >> 16);
//				buffer[i++] = 0; // Length of font family name (We aren't overriding this)
//				buffer[i++] = 1; // count of overridden properties
//				buffer[i++] = (byte)FwTextPropType.ktptFontSize;
//				buffer[i++] = (byte)FwTextPropVar.ktpvMilliPoint;
//				buffer[i++] = (byte)(aFontOverride.fontSize & 0xffff);
//				buffer[i++] = (byte)(aFontOverride.fontSize >> 16);
			}
			//string propsAsString = Convert.ToString(ToBase64String(buffer);
			propsBldr.SetStrPropValue((int)FwTextStringProp.kstpWsStyle, propsAsString);
			PutStyle(styleName, string.Empty, style.Hvo, style.BasedOnStyle,
				style.NextStyle, style.Type, style.IsBuiltIn, true, propsBldr.GetTextProps());
		}
		#endregion
	}
}
