// Copyright (c) 2010-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: DiffViewProxy.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>

using System.Windows.Forms;
using SIL.FieldWorks.Common.Controls.SplitGridView;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.TE
{
	#region DiffViewProxy (abstract)
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds information necessary to create a DiffView
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal abstract class DiffViewProxy : ViewProxy
	{
		/// <summary>The instance of the DiffDialog displaying this view</summary>
		protected readonly DiffDialog m_dlg;
		/// <summary>The book to display</summary>
		protected readonly IScrBook m_book;
		/// <summary><c>true</c> if this proxy is for the side representing the saved or
		/// imported version</summary>
		protected readonly bool m_fIsRevision;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DiffViewProxy"/> class.
		/// </summary>
		/// <param name="dlg">The instance of the DiffDialog displaying this view.</param>
		/// <param name="name">The (internal) name of the view.</param>
		/// <param name="book">The book to display.</param>
		/// <param name="fRev"><c>true</c> if this proxy is for the side representing the saved
		/// or imported version</param>
		/// ------------------------------------------------------------------------------------
		internal DiffViewProxy(DiffDialog dlg, string name, IScrBook book, bool fRev)
			: base(name, false)
		{
			m_dlg = dlg;
			m_book = book;
			m_fIsRevision = fRev;
		}
	}
	#endregion

	#region DiffViewScrProxy
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds information necessary to create a DiffView
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DiffViewScrProxy : DiffViewProxy
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DiffViewScrProxy"/> class.
		/// </summary>
		/// <param name="dlg">The instance of the DiffDialog displaying this view.</param>
		/// <param name="name">The (internal) name of the view.</param>
		/// <param name="book">The book to display.</param>
		/// <param name="fRev"><c>true</c> if this proxy is for the side representing the saved
		/// or imported version</param>
		/// ------------------------------------------------------------------------------------
		public DiffViewScrProxy(DiffDialog dlg, string name, IScrBook book, bool fRev)
			: base(dlg, name, book, fRev)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method to create the Scripture diff view when needed
		/// </summary>
		/// <param name="host">The control that will host (or "wrap") the view (can be <c>null</c>)</param>
		/// ------------------------------------------------------------------------------------
		public override Control CreateView(Control host)
		{
			DiffView diffView = new DiffView(m_book.Cache, m_book, m_dlg.m_differences,
				m_fIsRevision, m_dlg.Handle.ToInt32(), m_dlg.App);
			diffView.Zoom = m_dlg.ZoomFactorDraft.Value;
			diffView.Name = m_name;
			m_dlg.RegisterView(diffView);
			return diffView;
		}
	}
	#endregion

	#region DiffViewFootnoteProxy
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Holds information necessary to create a DiffFootnoteView
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DiffViewFootnoteProxy : DiffViewProxy
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="T:DiffViewFootnoteProxy"/> class.
		/// </summary>
		/// <param name="dlg">The instance of the DiffDialog displaying this view.</param>
		/// <param name="name">The (internal) name of the view.</param>
		/// <param name="book">The book to display.</param>
		/// <param name="fRev"><c>true</c> if this proxy is for the side representing the saved
		/// or imported version</param>
		/// ------------------------------------------------------------------------------------
		public DiffViewFootnoteProxy(DiffDialog dlg, string name, IScrBook book, bool fRev)
			: base(dlg, name, book, fRev)
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Method to create the Scripture diff view when needed
		/// </summary>
		/// <param name="host">The control that will host (or "wrap") the view (can be <c>null</c>)</param>
		/// ------------------------------------------------------------------------------------
		public override Control CreateView(Control host)
		{
			DiffFootnoteView fnView = new DiffFootnoteView(m_book.Cache, m_book, m_dlg.m_differences,
				m_fIsRevision, m_dlg.Handle.ToInt32(), m_dlg.App);
			fnView.Zoom = m_dlg.ZoomFactorFootnote.Value;
			fnView.Name = m_name;
			m_dlg.RegisterView(fnView);
			return fnView;
		}
	}
	#endregion
}
