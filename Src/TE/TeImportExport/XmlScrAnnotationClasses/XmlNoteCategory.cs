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
// File: XmlNoteCategory.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Serialization;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.CoreImpl;

namespace SIL.FieldWorks.TE
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Stores information about a single category in a Scripture annotation.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[XmlType("category")]
	public class XmlNoteCategory
	{
		#region Member variables
		/// <summary>The default language for annotation data (expessed as an ICU locale)</summary>
		[XmlAttribute("xml:lang")]
		public string IcuLocale;

		/// <summary>The category name</summary>
		[XmlText]
		public string CategoryName;

		/// <summary>The subcategory name</summary>
		[XmlElement("category")]
		public XmlNoteCategory SubCategory;

		/// <summary>Full path of the category</summary>
		private string m_categoryPath = string.Empty;
		#endregion

		#region Constructors
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlNoteCategory"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlNoteCategory()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="XmlNoteCategory"/> class based on the
		/// specified category.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public XmlNoteCategory(ICmPossibility category, ILgWritingSystemFactory lgwsf)
		{
			List<CategoryNode> categoryList = GetCategoryHierarchyList(category);
			m_categoryPath = category.NameHierarchyString;

			InitializeCategory(categoryList, lgwsf);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes categories in the hierarchy.
		/// </summary>
		/// <param name="categoryList">The list of categories for a Scripture note.</param>
		/// <param name="lgwsf">The language writing system factory.</param>
		/// <exception cref="ArgumentOutOfRangeException">thrown when the category list is empty.
		/// </exception>
		/// ------------------------------------------------------------------------------------
		private void InitializeCategory(List<CategoryNode> categoryList, ILgWritingSystemFactory lgwsf)
		{
			XmlNoteCategory curCategory = this;
			CategoryNode node = null;
			for (int i = 0; i < categoryList.Count; i++)
			{
				node = categoryList[i];
				curCategory.CategoryName = node.CategoryName;
				curCategory.IcuLocale = lgwsf.GetStrFromWs(node.Ws);
				if (categoryList.Count > i + 1)
				{
					curCategory.SubCategory = new XmlNoteCategory();
					curCategory = curCategory.SubCategory;
				}
				else
				{
					curCategory.SubCategory = null;
				}
			}
		}

		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the full category path.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public string CategoryPath
		{
			get
			{
				if (string.IsNullOrEmpty(m_categoryPath))
					m_categoryPath = GetCategoryHierarchyFromXml(this);
				return m_categoryPath;
			}
		}
		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of the category hierarchy with the top-level category in the first
		/// item of the list and the last item of the list containing information for the given
		/// category.
		/// </summary>
		/// <param name="category">The category.</param>
		/// ------------------------------------------------------------------------------------
		private List<CategoryNode> GetCategoryHierarchyList(ICmPossibility category)
		{
			List<CategoryNode> categoryList = new List<CategoryNode>();
			ITsString tssCategory = null;
			int ws = -1;

			// Go through the hierarchy getting the name of categories until the top-level
			// CmPossibility is found.
			ICmObject categoryObj = category;
			while (categoryObj is ICmPossibility)
			{
				tssCategory = ((ICmPossibility)categoryObj).Name.GetAlternativeOrBestTss(
					category.Cache.DefaultAnalWs, out ws);
				categoryList.Insert(0, new CategoryNode(tssCategory.Text, ws));

				categoryObj = categoryObj.Owner;
			}

			return categoryList;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the category hierarchy from this class.
		/// </summary>
		/// <param name="category">The category.</param>
		/// <returns>an ORC-delimited string providing the full hierarchy path of this category
		/// </returns>
		/// ------------------------------------------------------------------------------------
		private string GetCategoryHierarchyFromXml(XmlNoteCategory category)
		{
			StringBuilder strBldr = new StringBuilder();

			strBldr.Append(category.CategoryName);
			if (category.SubCategory != null)
				strBldr.Append(StringUtils.kChObject + GetCategoryHierarchyFromXml(category.SubCategory));

			return strBldr.ToString();
		}
		#endregion

		#region Methods for writing the category to the cache
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Writes this category to the the specified annotation, creating a new category if
		/// needed.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal void WriteToCache(IScrScriptureNote ann)
		{
			int ws = (string.IsNullOrEmpty(IcuLocale) ? ann.Cache.DefaultAnalWs :
				ScrNoteImportManager.GetWsForLocale(IcuLocale));

			ICmPossibility category;
			if (CategoryPath.IndexOf(StringUtils.kChObject) != -1)
			{
				// The category specified the full path of the category possibility.
				category = ann.Cache.LangProject.TranslatedScriptureOA.NoteCategoriesOA.FindOrCreatePossibility(
					CategoryPath, ws);
			}
			else
			{
				// The category does not contain a delimiter, so we may need to search
				// the entire possibility tree for a matching category in case it is a
				// sub-possibility.
				category = ann.Cache.LangProject.TranslatedScriptureOA.NoteCategoriesOA.FindOrCreatePossibility(
					CategoryPath, ws, false);
			}

			IFdoReferenceSequence<ICmPossibility> categoryList = ann.CategoriesRS;
			if (!categoryList.Contains(category))
				categoryList.Add(category);
		}
		#endregion

		#region Static methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a list of all the categories assigned to the specified annotation.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static List<XmlNoteCategory> GetCategoryList(IScrScriptureNote ann,
			ILgWritingSystemFactory lgwsf)
		{
			List<XmlNoteCategory> list = new List<XmlNoteCategory>();

			foreach (ICmPossibility category in ann.CategoriesRS)
				list.Add(new XmlNoteCategory(category, lgwsf));

			return (list.Count == 0 ? null : list);
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns a <see cref="T:System.String"/> that represents the current <see cref="T:System.Object"/>.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override string ToString()
		{
			return CategoryName;
		}

		#region CategoryNode class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// CategoryNode contains information needed to create a category node in the hierarchy.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		internal class CategoryNode
		{
			internal string CategoryName;
			internal int Ws;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="CategoryNode"/> class.
			/// </summary>
			/// <param name="categoryName">The name of the category.</param>
			/// <param name="wsHvo">The unique identifier for the writing system.</param>
			/// --------------------------------------------------------------------------------
			internal CategoryNode(string categoryName, int wsHvo)
			{
				CategoryName = categoryName;
				Ws = wsHvo;
			}
		}
		#endregion
	}
}
