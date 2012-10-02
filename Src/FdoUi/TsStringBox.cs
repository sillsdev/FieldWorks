using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.RootSites;

namespace SIL.FieldWorks.FdoUi
{
	public class TsStringBox : Panel, IFWDisposable
	{
		private InnerTsStringBox m_innerTextBox;
		/// <summary>
		/// Constructor
		/// </summary>
		public TsStringBox()
		{
			InitializeComponent();
			m_innerTextBox = new InnerTsStringBox();
			m_innerTextBox.Dock = DockStyle.Fill;
			BorderStyle = BorderStyle.Fixed3D;
			Controls.Add(m_innerTextBox);
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

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				Controls.Remove(m_innerTextBox);
				m_innerTextBox.Dispose();
			}
			m_innerTextBox = null;

			base.Dispose(disposing);
		}

		private void InitializeComponent()
		{
			//
			// TsStringBox
			//
			this.Layout += new System.Windows.Forms.LayoutEventHandler(this.TsStringBox_Layout);

		}

		private void TsStringBox_Layout(object sender, System.Windows.Forms.LayoutEventArgs e)
		{
			//BackColorChanged never is called (even when wire up), so we do this instead
			m_innerTextBox.BackColor = this.BackColor;
			 // not implemented: m_innerTextBox.BorderStyle = this.BorderStyle;
		}

		/// <summary>
		/// Set the main object to be displayed.
		/// </summary>
		public ICmObject Object
		{
			set
			{
				CheckDisposed();
				m_innerTextBox.Object = value;
			}
		}

		/// <summary>
		/// This is the text to display in this text box.
		/// </summary>
		public ITsString Tss
		{
			set
			{
				CheckDisposed();
				m_innerTextBox.Tss = value;
			}
		}

		#region InnerTsStringBox class

		internal class InnerTsStringBox : SimpleRootSite
		{
			protected bool m_sizeChangedSuppression = true;
			protected ICmObject m_object;
			protected ITsString m_tss;
			// This 'view' displays the string m_tssData by pretending it is property ktagText of
			// object khvoRoot.
			protected internal const int ktagDeleteText = 9001; // completely arbitrary, but recognizable.
			protected const int kfragRoot = 8002; // likewise.
			protected const int khvoRoot = 7003; // likewise.
			protected IVwCacheDa m_CacheDa; // Main cache object
			protected ISilDataAccess m_DataAccess; // Another interface on m_CacheDa.
			protected TsStringBoxVc m_vc;
			//protected int m_WritingSystem; // Writing system to use when Text is set.
			protected bool m_fUsingTempWsFactory;

			/// <summary>
			/// This is the text to display in this text box.
			/// </summary>
			public ITsString Tss
			{
				get
				{
					CheckDisposed();
					return m_tss;
				}
				set
				{
					CheckDisposed();
					m_tss = value;
				}
			}

			public ICmObject Object
			{
				get
				{
					CheckDisposed();
					return m_object;
				}
				set
				{
					CheckDisposed();

					SizeChangedSuppression = false;
					Debug.Assert(m_object == null, "Can't set the Object again.");
					if (value != null)
					{
						m_object = value;
						WritingSystemFactory = value.Cache.LanguageWritingSystemFactoryAccessor;
						m_DataAccess.WritingSystemFactory = WritingSystemFactory;
						m_fUsingTempWsFactory = false;
						if (m_rootb != null)
							m_rootb.Reconstruct();
					}
				}
			}
			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// For this class, if we haven't been given a WSF we create a default one (based on
			/// the registry). (Note this is kind of overkill, since the constructor does this too.
			/// But I left it here in case we change our minds about the constructor.)
			/// </summary>
			/// ------------------------------------------------------------------------------------
			[BrowsableAttribute(false), DesignerSerializationVisibilityAttribute
											(DesignerSerializationVisibility.Hidden)]
			public override ILgWritingSystemFactory WritingSystemFactory
			{
				get
				{
					CheckDisposed();

					if (base.WritingSystemFactory == null)
					{
						CreateTempWritingSystemFactory();
					}
					return base.WritingSystemFactory;
				}
				set
				{
					CheckDisposed();

					if (base.WritingSystemFactory != value)
					{
						ShutDownTempWsFactory();
						// when the writing system factory changes, delete any string that was there
						// and reconstruct the root box.
						base.WritingSystemFactory = value;
						// Enhance JohnT: Base class should probably do this.
						if (m_DataAccess != null)
							m_DataAccess.WritingSystemFactory = value;
					}
				}
			}

			public InnerTsStringBox()
			{
				m_CacheDa = VwCacheDaClass.Create();
				m_DataAccess = (ISilDataAccess)m_CacheDa;
				m_vc = new TsStringBoxVc(this);
				// So many things blow up so badly if we don't have one of these that I finally decided to just
				// make one, even though it won't always, perhaps not often, be the one we want.
				CreateTempWritingSystemFactory();
				m_DataAccess.WritingSystemFactory = WritingSystemFactory;
				VScroll = false; // no vertical scroll bar visible.
				AutoScroll = false; // not even if the root box is bigger than the window.
				BackColor = Color.White;
			}

			/// <summary>
			/// Clean up any resources being used.
			/// </summary>
			protected override void Dispose( bool disposing )
			{
				//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
				// Must not be run more than once.
				if (IsDisposed)
					return;

				base.Dispose(disposing);

				if (disposing)
				{
					if (m_vc != null)
						m_vc.Dispose();
				}

				ShutDownTempWsFactory(); // *May* set m_wsf to null.
				//m_wsf = null;
				m_vc = null;
				m_object = null;
				m_DataAccess = null;
				if (m_CacheDa != null)
				{
					m_CacheDa.ClearAllData();
					if (Marshal.IsComObject(m_CacheDa))
						Marshal.ReleaseComObject(m_CacheDa);
					m_CacheDa = null;
				}
			}

			/// <summary>
			/// Root site slaves sometimes need to suppress the effects of OnSizeChanged.
			/// </summary>
			public override bool SizeChangedSuppression
			{
				get
				{
					CheckDisposed();

					return m_sizeChangedSuppression;
				}
				set
				{
					CheckDisposed();

					m_sizeChangedSuppression = value;
				}
			}

			/// -------------------------------------------------------------------------------------
			/// <summary>
			/// Make a writing system factory that is based on the Languages folder (ICU-based).
			/// This is only used in Designer, tests, and momentarily (during construction) in
			/// production, until the client sets supplies a real one.
			/// </summary>
			/// -------------------------------------------------------------------------------------
			private void CreateTempWritingSystemFactory()
			{
				m_wsf = LgWritingSystemFactoryClass.Create();
				m_wsf.BypassInstall = true;
				m_fUsingTempWsFactory = true;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Shut down the writing system factory and release it explicitly.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			private void ShutDownTempWsFactory()
			{
				// Don't use the WritingSystemFactory property, as it may be called
				// while disposing and throw a disposed object exception.
				// Cf. LT-7262 & LT-7263 on how it can do that.
				if (m_fUsingTempWsFactory && m_wsf != null)
				{
					// Doing this crashes the program if another FwTextBox is still visible and
					// using the factory.
					//m_wsf.Shutdown();
					if (Marshal.IsComObject(m_wsf))
						Marshal.ReleaseComObject(m_wsf);
					m_wsf = null;
					m_fUsingTempWsFactory = false;
				}
			}
#if false
			#region Designer generated code
			/// <summary>
			/// Required method for Designer support - do not modify
			/// the contents of this method with the code editor.
			/// </summary>
			private void InitializeComponent()
			{
				components = new System.ComponentModel.Container();
			}
			#endregion
#endif

			#region RootSite overrides

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Create the root box and initialize it. We want this one to work even in design mode, and since
			/// we supply the cache and data ourselves, that's possible.
			/// </summary>
			/// ------------------------------------------------------------------------------------
			public override void MakeRoot()
			{
				CheckDisposed();

				if (DesignMode)
					return;

				m_rootb = VwRootBoxClass.Create();
				m_rootb.SetSite(this);
				m_rootb.DataAccess = m_DataAccess;
				m_rootb.SetRootObject(khvoRoot, m_vc, kfragRoot, StyleSheet);
				m_dxdLayoutWidth = kForceLayout; // Don't try to draw until we get OnSize and do layout.
				base.MakeRoot();
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Simulate infinite width.
			/// </summary>
			/// <returns>Int32.MaxValue / 2</returns>
			/// ------------------------------------------------------------------------------------
			public override int GetAvailWidth(IVwRootBox prootb)
			{
				CheckDisposed();

				//return Int32.MaxValue / 2;
				// Displaying Right-To-Left Graphite behaves badly if available width gets up to
				// one billion (10**9) or so.  See LT-6077.  One million (10**6) should be ample
				// for simulating infinite width.
				return 1000000;
			}

			/// ------------------------------------------------------------------------------------
			/// <summary>
			/// Overridden property to indicate that this control will handle horizontal scrolling
			/// </summary>
			/// ------------------------------------------------------------------------------------
			protected override bool DoAutoHScroll
			{
				get { return true; }
			}
			/// -----------------------------------------------------------------------------------
			/// <summary>
			/// Refreshes the Display :)
			/// </summary>
			/// -----------------------------------------------------------------------------------
			public override void RefreshDisplay()
			{
				CheckDisposed();

				if (m_fUsingTempWsFactory)
					return;

				base.RefreshDisplay();
			}

			/// -----------------------------------------------------------------------------------
			/// <summary>
			/// Process left or right mouse button down
			/// </summary>
			/// <param name="e"></param>
			/// -----------------------------------------------------------------------------------
			protected override void OnMouseDown(MouseEventArgs e)
			{
				// Eat the mouse click, since we don't allow the control to be focussed.
			}

			#endregion RootSite overrides

			#region View Constructor

			internal class TsStringBoxVc : VwBaseVc
			{
				private InnerTsStringBox m_textbox;

				public TsStringBoxVc(InnerTsStringBox textbox)
				{
					Debug.Assert(textbox != null);
					m_textbox = textbox;
				}

				#region IDisposable override

				/// <summary>
				/// Executes in two distinct scenarios.
				///
				/// 1. If disposing is true, the method has been called directly
				/// or indirectly by a user's code via the Dispose method.
				/// Both managed and unmanaged resources can be disposed.
				///
				/// 2. If disposing is false, the method has been called by the
				/// runtime from inside the finalizer and you should not reference (access)
				/// other managed objects, as they already have been garbage collected.
				/// Only unmanaged resources can be disposed.
				/// </summary>
				/// <param name="disposing"></param>
				/// <remarks>
				/// If any exceptions are thrown, that is fine.
				/// If the method is being done in a finalizer, it will be ignored.
				/// If it is thrown by client code calling Dispose,
				/// it needs to be handled by fixing the bug.
				///
				/// If subclasses override this method, they should call the base implementation.
				/// </remarks>
				protected override void Dispose(bool disposing)
				{
					//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
					// Must not be run more than once.
					if (IsDisposed)
						return;

					if (disposing)
					{
						// Dispose managed resources here.
					}

					// Dispose unmanaged resources here, whether disposing is true or false.
					m_textbox = null;

					base.Dispose(disposing);
				}

				#endregion IDisposable override

				/// ------------------------------------------------------------------------------------
				/// <summary>
				/// The main method just displays the deletion text with the appropriate properties.
				/// </summary>
				/// <param name="vwenv"></param>
				/// <param name="hvo"></param>
				/// <param name="frag"></param>
				/// ------------------------------------------------------------------------------------
				public override void Display(IVwEnv vwenv, int hvo, int frag)
				{
					CheckDisposed();

					vwenv.OpenParagraph();
					if (m_textbox.Tss != null)
						vwenv.AddString(m_textbox.Tss);
					vwenv.CloseParagraph();
				}
			}

			#endregion View Constructor
		}
		#endregion InnerTsStringBox class
	}
}
