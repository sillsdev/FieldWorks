// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DummyDraftViewForm.cs
// Responsibility: TE Team
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Scripture;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary>
	/// Dummy form for a <see cref="DummyDraftView"/>, so that we can create a view
	/// </summary>
	public class DummyDraftViewForm : Form, IFWDisposable
	{
		private System.ComponentModel.IContainer components;
		/// <summary></summary>
		protected DummyBasicView m_basicView;
		private FdoCache m_cache;

		#region Constructor, Dispose, Generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="DummyDraftViewForm"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummyDraftViewForm()
		{
			InitializeComponent();
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

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected override void Dispose( bool fDisposing )
		{
			if (fDisposing)
			{
				if(components != null)
				{
					components.Dispose();
				}
			}

			base.Dispose( fDisposing );

			if (fDisposing)
			{
				Cache.Dispose();
				Cache = null;
			}
		}
		#endregion

		#region Windows Form Designer generated code
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		private void InitializeComponent()
		{
			this.components = new System.ComponentModel.Container();
			//
			// DummyDraftViewForm
			//
			this.AutoScaleBaseSize = new System.Drawing.Size(5, 13);
			this.ClientSize = new System.Drawing.Size(292, 273);
			this.Name = "DummyDraftViewForm";
			this.Text = "DummyDraftViewForm";
			this.TopMost = true;
		}
		#endregion

		#region Properties
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets and Sets the Fdo cache
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public FdoCache Cache
		{
			get
			{
				CheckDisposed();

				return m_cache;
			}
			set
			{
				CheckDisposed();

				m_cache = value;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the basic view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummyBasicView BasicView
		{
			get
			{
				CheckDisposed();
				return m_basicView;
			}
		}
		#endregion

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the server and name to be used for establishing a database connection. First
		/// try to get this info based on the command-line arguments. If not given, try using
		/// the info in the registry. Otherwise, just get the first FW database on the local
		/// server.
		/// </summary>
		/// <returns>A new FdoCache, based on options, or null, if not found.</returns>
		/// <remarks>This method was originally taken from FwApp.cs</remarks>
		/// -----------------------------------------------------------------------------------
		private FdoCache GetCache()
		{
			Dictionary<string, string> cacheOptions = new Dictionary<string, string>();
			cacheOptions.Add("c", MiscUtils.LocalServerName);
			cacheOptions.Add("db", "TestLangProj");
			cacheOptions.Add("proj", "Kalaba");
			FdoCache cache = null;
			cache = FdoCache.Create(cacheOptions);
			// For these tests we don't need to run InstallLanguage.
			ILgWritingSystemFactory wsf = cache.LanguageWritingSystemFactoryAccessor;
			wsf.BypassInstall = true;
			return cache;	// After all of this, it may be still be null, so watch out.
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates and opens the given view in a form.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		protected void CreateView(DummyBasicView theView)
		{
			Cache = GetCache();
			FwStyleSheet styleSheet = new FwStyleSheet();

			ILangProject lgproj = Cache.LangProject;
			IScripture scripture = lgproj.TranslatedScriptureOA;
			styleSheet.Init(Cache, scripture.Hvo,
				(int)Scripture.ScriptureTags.kflidStyles);

			m_basicView = theView;
			m_basicView.Anchor = AnchorStyles.Top | AnchorStyles.Left |
				AnchorStyles.Right | AnchorStyles.Bottom;
			m_basicView.Cache = Cache;
			m_basicView.Dock = DockStyle.Fill;
			m_basicView.Name = "basicView";
			m_basicView.Visible = true;
			m_basicView.StyleSheet = styleSheet;
			Controls.Add(m_basicView);
			m_basicView.ActivateView();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Scroll the DraftView to the end.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void ScrollToEnd()
		{
			CheckDisposed();

			m_basicView.ScrollToEnd();
			// The actual DraftView code for handling Ctrl-End doesn't contain this method call.
			// The call to CallOnExtendedKey() in OnKeyDown() handles setting the IP.
			m_basicView.RootBox.MakeSimpleSel(false, true, false, true);
			PerformLayout();
			Application.DoEvents();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void ScrollToTop()
		{
			CheckDisposed();

			m_basicView.ScrollToTop();
			// The actual DraftView code for handling Ctrl-Home doesn't contain this method call.
			// The call to CallOnExtendedKey() in OnKeyDown() handles setting the IP.
			m_basicView.RootBox.MakeSimpleSel(true, true, false, true);
			Application.DoEvents();
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get visibility of IP
		/// </summary>
		/// <returns>Returns <c>true</c> if selection is visible</returns>
		/// -----------------------------------------------------------------------------------
		public bool IsSelectionVisible()
		{
			CheckDisposed();

			return m_basicView.IsSelectionVisible(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Does what the method name says.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public void SimulateCtrlSpace()
		{
			CheckDisposed();

			//m_basicView.RemoveCharFormatting();
			m_basicView.CallRootSiteOnKeyDown(new KeyEventArgs(Keys.Space | Keys.Control));
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Get the Y positon off the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public int YPosition
		{
			get
			{
				CheckDisposed();

				return m_basicView.ScrollPosition.Y;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Gets the draft view
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public DummyDraftView DraftView
		{
			get
			{
				CheckDisposed();
				return m_basicView as DummyDraftView;
			}
		}

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Creates and opens a draft view in a form. Loads scripture from the DB.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public void CreateDraftView()
		{
			CheckDisposed();

			CreateView(new DummyDraftView());
		}
	}
}
