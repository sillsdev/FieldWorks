// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2007, SIL International. All Rights Reserved.
// <copyright from='2007' to='2007' company='SIL International'>
//		Copyright (c) 2007, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ViewCreateInfo.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.TE
{
	#region DraftViewCreateInfo
	/// <summary>Holds information necessary to create a draft view.</summary>
	public struct DraftViewCreateInfo
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DraftViewCreateInfo"/> class.
		/// </summary>
		/// <param name="name">The name of the view.</param>
		/// <param name="fBackTrans"><c>true</c> if this view displays a back translation.</param>
		/// <param name="fShowInTable"><c>true</c> to display the view as a table.</param>
		/// <param name="fMakeRootAutomatically"><c>true</c> to call MakeRoot(), <c>false</c>
		/// if caller will call MakeRoot().</param>
		/// <param name="fPersistSettings"><c>true</c> to persist settings.</param>
		/// <param name="fEditable"><c>true</c> if this draft view is editable, <c>false</c>
		/// if it is read-only.</param>
		/// <param name="viewType">Type of the view.</param>
		/// ------------------------------------------------------------------------------------
		public DraftViewCreateInfo(string name, bool fBackTrans, bool fShowInTable,
			bool fMakeRootAutomatically, bool fPersistSettings, bool fEditable,
			TeViewType viewType)
		{
			Name = name;
			IsBackTrans = fBackTrans;
			ShowInTable = fShowInTable;
			MakeRootAutomatically = fMakeRootAutomatically;
			PersistSettings = fPersistSettings;
			IsEditable = fEditable;
			ViewType = viewType;
		}

		/// <summary>The name of the view.</summary>
		public string Name;
		/// <summary><c>true</c> if this view displays a back translation.</summary>
		public bool IsBackTrans;
		/// <summary><c>true</c> to display the view as a table.</summary>
		public bool ShowInTable;
		/// <summary><c>true</c> to call MakeRoot(), <c>false</c> if caller will call
		/// MakeRoot().</summary>
		public bool MakeRootAutomatically;
		/// <summary><c>true</c> to persist settings.</summary>
		public bool PersistSettings;
		/// <summary><c>true</c> if this draft view is editable, <c>false</c> if it
		/// is read-only.</summary>
		public bool IsEditable;
		/// <summary>The view spec</summary>
		public TeViewType ViewType;
	}
	#endregion

	#region ChecksDraftViewCreateInfo
	/// <summary>Holds information necessary to create a key terms draft view.</summary>
	public struct ChecksDraftViewCreateInfo
	{
		/// --------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:KeyTermsDraftViewCreateInfo"/> class.
		/// </summary>
		/// <param name="name">The name of the view.</param>
		/// <param name="fEditable"><c>true</c> if this draft view is editable, <c>false</c>
		/// if it is read-only.</param>
		/// --------------------------------------------------------------------------------
		public ChecksDraftViewCreateInfo(string name, bool fEditable)
		{
			Name = name;
			IsEditable = fEditable;
		}

		/// <summary>The name of the view.</summary>
		public string Name;
		/// <summary><c>true</c> if this draft view is editable, <c>false</c> if it
		/// is read-only.</summary>
		public bool IsEditable;
	}
	#endregion

	#region StylebarCreateInfo
	/// <summary>Holds information necessary to create a style bar.</summary>
	internal struct StylebarCreateInfo
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:StylebarCreateInfo"/> class.
		/// </summary>
		/// <param name="name">The name of the view.</param>
		/// <param name="fForFootnotes"><c>true</c> if this stylebar is for footnotes.</param>
		/// ------------------------------------------------------------------------------------
		public StylebarCreateInfo(string name, bool fForFootnotes)
		{
			Name = name;
			IsForFootnotes = fForFootnotes;
		}

		/// <summary>The name of the view.</summary>
		public string Name;
		/// <summary><c>true</c> if this stylebar is for footnotes.</summary>
		public bool IsForFootnotes;
	}
	#endregion

	#region FootnoteCreateInfo
	/// <summary>Holds information necessary to create a footnote view.</summary>
	internal struct FootnoteCreateInfo
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:FootnoteCreateInfo"/> class.
		/// </summary>
		/// <param name="name">The name of the view.</param>
		/// <param name="fBackTrans"><c>true</c> if this view displays a back translation.</param>
		/// <param name="fEditable"><c>true</c> if this draft view is editable, <c>false</c>
		/// if it is read-only.</param>
		/// ------------------------------------------------------------------------------------
		public FootnoteCreateInfo(string name, bool fBackTrans, bool fEditable)
		{
			Name = name;
			IsBackTrans = fBackTrans;
			IsEditable = fEditable;
		}

		/// <summary>The name of the view.</summary>
		public string Name;
		/// <summary><c>true</c> if this view displays a back translation.</summary>
		public bool IsBackTrans;
		/// <summary><c>true</c> if this draft view is editable, <c>false</c> if it
		/// is read-only.</summary>
		public bool IsEditable;
	}
	#endregion
}
