// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2008, SIL International. All Rights Reserved.
// <copyright from='2008' to='2008' company='SIL International'>
//		Copyright (c) 2008, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: SimpleDraftViewWrapper.cs
// Responsibility: TE Team
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using Microsoft.Win32;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.Cellar;

namespace SIL.FieldWorks.TE
{
	/// -----------------------------------------------------------------------------------
	/// <summary>
	/// This SimpleDraftViewWrapper class holds two rows.
	/// The top pane contains a style pane and the draft view. The second pane contains a
	/// style pane and the footnotes.
	/// </summary>
	/// -----------------------------------------------------------------------------------
	public class SimpleDraftViewWrapper : ViewWrapper, ISelectionChangeNotifier
	{
		#region Events
		/// <summary>
		/// Event handler for when the rootbox's selection changes.
		/// </summary>
		public event EventHandler<VwSelectionArgs> VwSelectionChanged;
		#endregion

		#region Constructor
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="SimpleDraftViewWrapper"/> class.
		/// </summary>
		/// <param name="name">The name of the split grid</param>
		/// <param name="parent">The parent of the split wrapper (can be null). Will be replaced
		/// with real parent later.</param>
		/// <param name="cache">The cache.</param>
		/// <param name="styleSheet">The style sheet.</param>
		/// <param name="settingsRegKey">The settings reg key.</param>
		/// <param name="draftViewInfo">Information about the draft view.</param>
		/// <param name="draftStylebarInfo">Information about the draft stylebar.</param>
		/// <param name="footnoteViewInfo">Information about the footnote draft view.</param>
		/// <param name="footnoteStylebarInfo">Information about the footnote stylebar.</param>
		/// ------------------------------------------------------------------------------------
		public SimpleDraftViewWrapper(string name, Control parent, FdoCache cache,
			IVwStylesheet styleSheet, RegistryKey settingsRegKey, object draftViewInfo,
			object draftStylebarInfo, object footnoteViewInfo, object footnoteStylebarInfo)
			: base(name, parent, cache, styleSheet, settingsRegKey, draftViewInfo, draftStylebarInfo,
			footnoteViewInfo, footnoteStylebarInfo, 2, 2)
		{
		}
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the draft row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int kDraftRow
		{
			get { return 0; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the footnote row.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int kFootnoteRow
		{
			get { return 1; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the index of the style column. The style column is not used in the
		/// SimpleDraftViewWrapper, so we return -1 so that we won't add the style panes
		/// to the wrapper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override int kStyleColumn
		{
			get
			{
				return -1;
			}
		}
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the main child view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public override DraftView DraftView
		{
			get
			{
				CheckDisposed();

				DraftView view = FocusedRootSite as DraftView;
				if (view != null)
					return view;
				Control upperControl = GetControl(kDraftRow, kDraftViewColumn);
				if (upperControl != null && upperControl.Visible)
					return GetControl(kDraftRow, kDraftViewColumn) as DraftView;
				return base.DraftView;
			}
		}
		#endregion

		#region methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes the component.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.SuspendLayout();
			//
			// SimpleDraftViewWrapper
			//
			this.AccessibleName = "SimpleDraftViewWrapper";
			this.ResumeLayout(false);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when a hosted control has been newly created. We hook into this in order to
		/// listen for VwSelectionChanged events in the draft view and pass them on to anyone who
		/// has subscribed to our VwSelectionChanged event.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void OnHostedControlCreated(Control c)
		{
			ISelectionChangeNotifier selChangeNotifier = c as ISelectionChangeNotifier;
			if (selChangeNotifier != null)
			{
				selChangeNotifier.VwSelectionChanged +=	delegate(object sender, VwSelectionArgs e)
				{
					if (VwSelectionChanged != null)
						VwSelectionChanged(sender, e);
				};
			}
		}
		#endregion
	}
}
