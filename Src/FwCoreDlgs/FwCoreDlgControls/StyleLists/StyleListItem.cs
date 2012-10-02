// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2006' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: StyleListItem.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using System.Diagnostics;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FwCoreDlgControls
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Implements object representing an single item in a StylesComboBox or StylesListBox.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class StyleListItem : IComparable
	{
		private enum StyleListItemType
		{
			RealStyle,
			DefaultParaChars,
			DataProperty,
		}

		private StyleListItemType m_itemType = StyleListItemType.RealStyle;
		private BaseStyleInfo m_styleInfo;
		private bool m_isCurrentStyle;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a StyleListItem (i.e. an StStyle object) based on a real style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StyleListItem(BaseStyleInfo styleInfo)
		{
			m_styleInfo = styleInfo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructs a StyleListItem which is not a real style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected StyleListItem()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new StyleListItem for the "Default Paragraph Characters" style and
		/// initializes various properties for that.
		/// </summary>
		/// <returns>A new StyleListItem for the "Default Paragraph Characters"
		/// psuedo-style.</returns>
		/// ------------------------------------------------------------------------------------
		public static StyleListItem CreateDefaultParaCharItem()
		{
			StyleListItem item = new StyleListItem();
			item.m_itemType = StyleListItemType.DefaultParaChars;
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates a new StyleListItem for a pseudo-style (used for import mapping to "data"
		/// properties).
		/// </summary>
		/// <param name="sName"></param>
		/// <returns>A new StyleListItem for the requested psuedo-style.</returns>
		/// ------------------------------------------------------------------------------------
		public static StyleListItem CreateDataPropertyItem(string sName)
		{
			StyleListItem item = new StyleListItem();
			item.m_styleInfo = new BaseStyleInfo();
			item.m_styleInfo.Name = sName;
			// We set this to Paragraph, but when filtering the list, we actually allow psuedo
			// styles to match either Paragraph or Character. The import code handles either
			// case correctly (I hope).
			item.m_styleInfo.IsParagraphStyle = true;
			item.m_itemType = StyleListItemType.DataProperty;
			return item;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the BaseStyleInfo contained in the list item.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public BaseStyleInfo StyleInfo
		{
			get {return m_styleInfo;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether this instance is a current style in the
		/// application. This is used to draw an indicator next to the style name to show
		/// that it is the current style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsCurrentStyle
		{
			get { return m_isCurrentStyle; }
			set { m_isCurrentStyle = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a flag indicating whether or not this StyleListItem refers to the
		/// "Default Paragraph Characters" style ("Default Paragraph Characters" is not a real
		/// style since its effect is to remove style formatting).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsDefaultParaCharsStyle
		{
			get {return m_itemType == StyleListItemType.DefaultParaChars;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a flag indicating whether or not this StyleListItem refers to the
		/// "Data Property" style ("Default Paragraph Characters" is not a real
		/// style since its effect is to remove style formatting).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsDataPropertyStyle
		{
			get {return m_itemType == StyleListItemType.DataProperty;}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating whether this instance is a user-modified style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public bool IsUserModifiedStyle
		{
			get { return m_styleInfo.IsModified; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating the list item's style name.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string Name
		{
			get
			{
				return IsDefaultParaCharsStyle ? ResourceHelper.DefaultParaCharsStyleName : m_styleInfo.Name;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating the list item's style context.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ContextValues Context
		{
			get
			{
				return m_itemType != StyleListItemType.RealStyle ? ContextValues.General :
					m_styleInfo.Context;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating the list item's style function.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FunctionValues Function
		{
			get
			{
				return m_itemType != StyleListItemType.RealStyle ? FunctionValues.Prose :
					m_styleInfo.Function;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a value indicating the list item's style type.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public StyleType Type
		{
			get
			{
				if (IsDefaultParaCharsStyle)
					return StyleType.kstCharacter;
				return m_styleInfo.IsCharacterStyle ? StyleType.kstCharacter :
					StyleType.kstParagraph;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the user level of the style.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int UserLevel
		{
			get
			{
				if (m_styleInfo == null)
					return 0;
				return m_styleInfo.UserLevel;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override this just to be safe.
		/// </summary>
		/// <returns>The StyleListItem's text property</returns>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return Name;
		}

		#region IComparable Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Compare a StyleListItem to another item.
		/// </summary>
		/// <param name="obj">StyleListItem to compare to</param>
		/// <returns>less than 0 if this item is less than obj, 0 if they are equal, and
		/// greater than 0 if this item is greater than obj</returns>
		/// ------------------------------------------------------------------------------------
		public int CompareTo(object obj)
		{
			StyleListItem other = obj as StyleListItem;
			if (other == null)
				return 0;

			// compare the names
			return Name.CompareTo(other.Name);
		}

		#endregion
	}
}
