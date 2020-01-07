// Copyright (c) 2006-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;

namespace LanguageExplorer.Controls.Styles
{
	/// <summary>
	/// Implements object representing an single item in a StylesComboBox or StylesListBox.
	/// </summary>
	public class StyleListItem : IComparable
	{
		private enum StyleListItemType
		{
			RealStyle,
			DefaultParaChars,
			DataProperty,
		}

		private StyleListItemType m_itemType = StyleListItemType.RealStyle;

		/// <summary />
		public StyleListItem(BaseStyleInfo styleInfo)
		{
			StyleInfo = styleInfo;
		}

		/// <summary />
		protected StyleListItem()
		{
		}

		/// <summary>
		/// Creates a new StyleListItem for the "Default Paragraph Characters" style and
		/// initializes various properties for that.
		/// </summary>
		/// <returns>A new StyleListItem for the "Default Paragraph Characters"
		/// pseudo-style.</returns>
		public static StyleListItem CreateDefaultParaCharItem()
		{
			return new StyleListItem
			{
				m_itemType = StyleListItemType.DefaultParaChars
			};
		}

		/// <summary>
		/// Creates a new StyleListItem for a pseudo-style (used for import mapping to "data"
		/// properties).
		/// </summary>
		public static StyleListItem CreateDataPropertyItem(string sName)
		{
			return new StyleListItem
			{
				StyleInfo = new BaseStyleInfo
				{
					Name = sName,
					IsParagraphStyle = true
				},
				m_itemType = StyleListItemType.DataProperty
			};
		}

		/// <summary>
		/// Gets the BaseStyleInfo contained in the list item.
		/// </summary>
		public BaseStyleInfo StyleInfo { get; private set; }

		/// <summary>
		/// Gets or sets a value indicating whether this instance is a current style in the
		/// application. This is used to draw an indicator next to the style name to show
		/// that it is the current style.
		/// </summary>
		public bool IsCurrentStyle { get; set; }

		/// <summary>
		/// Gets or sets a flag indicating whether or not this StyleListItem refers to the
		/// "Default Paragraph Characters" style ("Default Paragraph Characters" is not a real
		/// style since its effect is to remove style formatting).
		/// </summary>
		public bool IsDefaultParaCharsStyle => m_itemType == StyleListItemType.DefaultParaChars;

		/// <summary>
		/// Gets or sets a flag indicating whether or not this StyleListItem refers to the
		/// "Data Property" style ("Default Paragraph Characters" is not a real
		/// style since its effect is to remove style formatting).
		/// </summary>
		public bool IsDataPropertyStyle => m_itemType == StyleListItemType.DataProperty;

		/// <summary>
		/// Gets a value indicating whether this instance is a user-modified style.
		/// </summary>
		public bool IsUserModifiedStyle => StyleInfo.IsModified;

		/// <summary>
		/// Gets a value indicating the list item's style name.
		/// </summary>
		public string Name => IsDefaultParaCharsStyle ? StyleUtils.DefaultParaCharsStyleName : StyleInfo.Name;

		/// <summary>
		/// Gets a value indicating the list item's style context.
		/// </summary>
		public ContextValues Context => m_itemType != StyleListItemType.RealStyle ? ContextValues.General : StyleInfo.Context;

		/// <summary>
		/// Gets a value indicating the list item's style function.
		/// </summary>
		public FunctionValues Function => m_itemType != StyleListItemType.RealStyle ? FunctionValues.Prose : StyleInfo.Function;

		/// <summary>
		/// Gets a value indicating the list item's style type.
		/// </summary>
		public StyleType Type => IsDefaultParaCharsStyle ? StyleType.kstCharacter : (StyleInfo.IsCharacterStyle ? StyleType.kstCharacter : StyleType.kstParagraph);

		/// <summary>
		/// Gets the user level of the style.
		/// </summary>
		public int UserLevel => StyleInfo?.UserLevel ?? 0;

		/// <summary>
		/// Override this just to be safe.
		/// </summary>
		/// <returns>The StyleListItem's text property</returns>
		public override string ToString()
		{
			return Name;
		}

		#region IComparable Members
		/// <summary>
		/// Compare a StyleListItem to another item.
		/// </summary>
		/// <param name="obj">StyleListItem to compare to</param>
		/// <returns>less than 0 if this item is less than obj, 0 if they are equal, and
		/// greater than 0 if this item is greater than obj</returns>
		public int CompareTo(object obj)
		{
			var other = obj as StyleListItem;
			return other == null ? 0 : Name.CompareTo(other.Name);
		}

		#endregion
	}
}