// Copyright (c) 2002-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: FwRootSite.cs
// Responsibility: Eberhard Beilharz

using System;
using System.Collections.Generic; // KeyNotFoundException
using System.ComponentModel;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;

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
		private IContainer components;

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
		#endregion

		#region Other virtual methods
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
		/// Creates an FW-specific helper used for processing editing requests.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override EditingHelper CreateEditingHelper()
		{
			return new FwEditingHelper(m_fdoCache, this);
		}
		#endregion

		#region Overriden methods (of UserControl)
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Overridden to set the mediator
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void OnHandleCreated(EventArgs e)
		{
			base.OnHandleCreated(e);
			Mediator = null;
		}
		#endregion
	}
}
