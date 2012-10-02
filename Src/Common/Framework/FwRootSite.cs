// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwRootSite.cs
// Responsibility: Eberhard Beilharz
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Accessibility;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for hosting a FieldWorks view in an application. The main difference
	/// between <see cref="RootSite"/> and <see cref="FwRootSite"/> is that the latter knows
	/// about FwMainWnd and ties in with menu and styles combo box.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class FwRootSite : RootSite
	{
		private System.ComponentModel.IContainer components;

		#region Constructor, Dispose, Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="FwRootSite"/> class.
		/// </summary>
		/// <param name="cache">The FDO Cache</param>
		/// -----------------------------------------------------------------------------------
		public FwRootSite(FdoCache cache) : base(cache)
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			base.Dispose(disposing);

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
		}

		#region Component Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
		}
		#endregion
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the property that tells if paragraphs are displayed in a table.
		/// </summary>
		/// <remarks>The default implementation returns always false.</remarks>
		/// ------------------------------------------------------------------------------------
		public virtual bool ShowInTable
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the EditingHelper cast as an FwEditingHelper.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwEditingHelper FwEditingHelper
		{
			get
			{
				CheckDisposed();
				return EditingHelper as FwEditingHelper;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Find the containing FwMainWnd.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public virtual FwMainWnd TheMainWnd
		{
			get
			{
			CheckDisposed();

				Control ctrl = Parent;
				while (ctrl != null)
				{
					if (ctrl is FwMainWnd)
						return (FwMainWnd)ctrl;

					ctrl = ctrl.Parent;
				}

				return null;
			}
		}
		#endregion

		#region Other virtual methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Remove your root registration.
		/// </summary>
		/// <remarks>This is a separate (virtual) method because some VwWnds, such as
		/// AfDeVwWnd, which share their root box, may not want to unregister it.
		/// </remarks>
		/// -----------------------------------------------------------------------------------
		protected virtual void RemoveRootRegistration()
		{
			// REVIEW (EberhardB): It's debatable if this belongs in this class (where it knows
			// about FwMainWnd) or in the base class RootSite. We can decide when we implement
			// it.

			// TODO (EberhardB): Implement this if we know how we handle/get active view in MainWnd
			//			AssertPtr(m_pwndSubclass);
			//			AfMainWnd * pafw = m_pwndSubclass->MainWindow();
			//
			//			// Unregister the root box with the main window. Only if we are not the second pane of
			//			// two that are sharing it.
			//			if (m_qrootb && pafw && !OtherPane())
			//				pafw->UnregisterRootBox(m_qrootb);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Given a real flid, return the corresponding tag, which may be a virtual one (to
		/// support filtering and sorting). Any external object that wants to talk to an
		/// FwRootSite about a particular flid it is presumably displaying (i.e., for getting
		/// or making a selection in some property) should use this method to translate the flid.
		/// It is "safe" to just use the flid if you're absolutely sure the view isn't using a
		/// virtual property to filter/sort it, but using this method is much safer.
		/// </summary>
		/// <remarks>Any view that supports filtering or sorting should override this for any
		/// fields that can be sorted/filtered</remarks>
		/// <param name="flid">The field identifier</param>
		/// <returns>The default implementation just returns the flid</returns>
		/// ------------------------------------------------------------------------------------
		public virtual int GetVirtualTagForFlid(int flid)
		{
			CheckDisposed();

			return flid;
		}
		#endregion

		#region Other non-virtual methods
		/// -----------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="vss"></param>
		/// -----------------------------------------------------------------------------------
		protected override void Activate(VwSelectionState vss)
		{
			base.Activate(vss);
			if (TheMainWnd != null)
				TheMainWnd.UpdateStyleComboBoxValue(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if the current selection contains only a footnote reference, or is an IP
		/// associated with	only a footnote reference.
		/// </summary>
		/// <param name="vwsel">Selection to check for footnote marker.</param>
		/// <returns><c>True</c>if current selection is on a footnote</returns>
		/// ------------------------------------------------------------------------------------
		protected override bool SelectionIsFootnoteMarker(IVwSelection vwsel)
		{
			return (GetFootnoteFromMarkerSelection(vwsel) != null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if the current selection contains only a footnote reference, or is an IP
		/// associated with	only a footnote reference.
		/// </summary>
		/// <returns>the footnote object, if current selection is on a footnote;
		/// otherwise null</returns>
		/// ------------------------------------------------------------------------------------
		public IStFootnote GetFootnoteFromMarkerSelection()
		{
			CheckDisposed();

			if (EditingHelper != null && EditingHelper.CurrentSelection != null)
				return GetFootnoteFromMarkerSelection(EditingHelper.CurrentSelection.Selection);
			else
				return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Checks if the current selection contains only a footnote reference, or is an IP
		/// associated with	only a footnote reference.
		/// </summary>
		/// <param name="vwsel">selection to get info from</param>
		/// <returns>the footnote object, if current selection is on a footnote;
		/// otherwise null</returns>
		/// ------------------------------------------------------------------------------------
		public IStFootnote GetFootnoteFromMarkerSelection(IVwSelection vwsel)
		{
			CheckDisposed();

			IStFootnote footnote = null;
			// if we find a single run with the correct props, we are on an ORC hot link
			string sGuid = GetOrcHotLinkStrProp(vwsel);
			if (sGuid == null)
				return null; // not a footnote

			// Get the underlying object for the guid.
			Guid guid = MiscUtils.GetGuidFromObjData(sGuid.Substring(1));
			int hvoObj = Cache.GetIdFromGuid(guid);

			if (hvoObj != 0)
			{
				try
				{
					ICmObject obj = CmObject.CreateFromDBObject(Cache, hvoObj);
					footnote = obj as IStFootnote;
				}
				catch (NullReferenceException)
				{
					return null;
				}
			}

			return footnote;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Same as the version of GetVirtualTagForFlid that takes an int, but allows FDO-style
		/// "kflid" enumerations to be passed without explicitly casting them to an int.
		/// </summary>
		/// <param name="flid">The field identifier</param>
		/// <returns>The default implementation just returns the flid cast as an int</returns>
		/// ------------------------------------------------------------------------------------
		public int GetVirtualTagForFlid(object flid)
		{
			CheckDisposed();

			return GetVirtualTagForFlid((int)flid);
		}
		#endregion // Other non-virtual methods

		#region Overridden methods (of SimpleRootSite)
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper used for processing editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override EditingHelper EditingHelper
		{
			get
			{
			CheckDisposed();

				if (m_editingHelper == null)
					m_editingHelper = new FwEditingHelper(m_fdoCache, this);
				return m_editingHelper;
			}
		}
		#endregion

		#region Overriden methods (of UserControl)
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Do cleaning up when handle gets destroyed
		/// </summary>
		/// <param name="e"></param>
		/// <remarks>Formerly AfVwRootSite::OnReleasePtr()</remarks>
		/// -----------------------------------------------------------------------------------
		protected override void OnHandleDestroyed(EventArgs e)
		{
			if (DesignMode)
				return;

			// We need this test for the benefit of the ActiveX control subclass.
			if (FwApp.App != null)
			{
				RemoveRootRegistration(); // Before clearing root!
				// TODO (EberhardB): Implement this if we know how we handle/get active view in MainWnd
				//				// We need to clear the frame window's active root box pointer if it is set to our
				//				// m_qrootb.
				//				AssertPtr(m_pwndSubclass);
				//				AfMainWnd * pafw = m_pwndSubclass->MainWindow();
				//
				//				// Previously, there was an assert test on pafw, but in some rare circumstances, it is
				//				// possible to have no MainWindow. For example, during a Restore from backup operation,
				//				// the progress dialog runs with no parent, and can produce child dialogs with "what's
				//				// this?" helps. Closing those helps used to assert here, as there was no MainWindow.
				//				if (pafw)
				//				{
				//					if (pafw->GetActiveRootBox() == m_qrootb)
				//						pafw->SetActiveRootBox(NULL);
				//				}
			}

			base.OnHandleDestroyed(e);
		}
		#endregion

		#region Message handlers

		#endregion
	}
}
