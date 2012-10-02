// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FwDataEntryForm.cs
// Responsibility: TomB
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Drawing;
using System.Collections;
using System.ComponentModel;
using System.Windows.Forms;
using System.Diagnostics;

using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Framework
{
	/// <summary>
	/// Summary description for FwDataEntryForm.
	/// </summary>
	public class FwDataEntryForm : Form, IFWDisposable
	{
		#region Data Members
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.Container components = null;
		LangProject m_LangProj;
		#endregion

		#region Construction, Initialization and Destruction
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Default constructor for the FwDataEntryForm class (needed for Designer).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwDataEntryForm()
		{
			//
			// Required for Windows Form Designer support
			//
			InitializeComponent();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Constructor for the FwDataEntryForm class that takes a language project.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public FwDataEntryForm(LangProject lp) : this()
		{
			m_LangProj = lp;
		}

		/// <summary>
		/// Check to see if the object has been disposed.
		/// All public Properties and Methods should call this
		/// before doing anything else.
		/// </summary>
		public void CheckDisposed()
		{
			if (IsDisposed)
				throw new ObjectDisposedException(String.Format("'{0}' in use after being disposed.", GetType().Name));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override void Dispose( bool disposing )
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if( disposing )
			{
				if(components != null)
				{
					components.Dispose();
				}
			}
			base.Dispose( disposing );
		}

		#region Windows Form Designer generated code
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			this.Size = new System.Drawing.Size(300,300);
			this.Text = "FwDataEntryForm";
		}
		#endregion
		#endregion

		#region Properties
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Language Project whose data is displayed in the field editors on this form.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public LangProject LangProj
		{
			get
			{
				CheckDisposed();

				return m_LangProj;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Number of pixels of padding above font in the data entry field editors contained in
		/// this form
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FontPaddingAbove
		{
			get
			{
				CheckDisposed();

				return 1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Number of pixels of padding above font in the data entry field editors contained in
		/// this form
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FontPaddingBelow
		{
			get
			{
				CheckDisposed();

				return 1;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Thickness of separator line (in pixels) between the data entry field editors contained
		/// in this form
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public int FieldEditorSeparatorWeight
		{
			get
			{
				CheckDisposed();

				return 1;
			}
		}
		#endregion

		#region Virtual methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This is called whenever a field editor is opened to begin editing.
		/// </summary>
		/// <remarks>
		/// Subclasses can override this to do things such as conditionally saving the
		/// modification date.
		/// </remarks>
		/// <param name="fwDataEntryFieldEditor"></param>
		/// ------------------------------------------------------------------------------------
		public virtual void BeginEdit(FwDataEntryFieldEditor fwDataEntryFieldEditor)
		{
			CheckDisposed();

		}
		#endregion
	}
}
