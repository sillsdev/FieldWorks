using System;
using System.Diagnostics;
using System.Windows.Forms;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Validation;
using XCore;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.XWorks.MorphologyEditor
{
	/// <summary>
	/// Summary description for PhEnvStrRepresentationSlice.
	/// </summary>
	public class PhEnvStrRepresentationSlice : ViewPropertySlice
	{
		// private int m_ws = 0; // CS0414

		public PhEnvStrRepresentationSlice(ICmObject obj)
			: base(new StringRepSliceView(obj.Hvo), obj, StringRepSliceVc.Flid)
		{
		}

		/// <summary>
		/// We want the persistence provider, and the easiest way to get it is to get all
		/// this other stuff we don't need or use.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="editor"></param>
		/// <param name="flid"></param>
		/// <param name="node"></param>
		/// <param name="obj"></param>
		/// <param name="stringTbl"></param>
		/// <param name="persistenceProvider"></param>
		/// <param name="ws"></param>
		public PhEnvStrRepresentationSlice(FdoCache cache, string editor, int flid,
			System.Xml.XmlNode node, ICmObject obj, StringTable stringTbl,
			IPersistenceProvider persistenceProvider, int ws)
			: base(new StringRepSliceView(obj.Hvo), obj, StringRepSliceVc.Flid)
		{
			m_persistenceProvider = persistenceProvider;
			// m_ws = ws; // CS0414
		}

		public PhEnvStrRepresentationSlice()
		{
		}

		/// <summary>
		/// Therefore this method, called once we have a cache and object, is our first chance to
		/// actually create the embedded control.
		/// </summary>
		public override void FinishInit()
		{
			CheckDisposed();

			StringRepSliceView ctrl = Control as StringRepSliceView; //new StringRepSliceView(m_hvoContext);
			ctrl.Cache = (FdoCache)Mediator.PropertyTable.GetValue("cache");
			ctrl.ResetValidator();

			if (ctrl.RootBox == null)
				ctrl.MakeRoot();
		}

		#region Special menu item methods
		/// <summary>
		/// This menu item is turned off if a slash already exists in the environment string.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayShowEnvironmentError(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			StringRepSliceView view = Control as StringRepSliceView;
			if (view == null)
				return false;
			display.Enabled = view.CanShowEnvironmentError();
			return true;
		}

		public bool OnShowEnvironmentError(object args)
		{
			CheckDisposed();
			StringRepSliceView view = Control as StringRepSliceView;
			view.ShowEnvironmentError();
			return true;
		}

		/// <summary>
		/// This menu item is turned off if a slash already exists in the environment string.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertSlash(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			StringRepSliceView view = Control as StringRepSliceView;
			if (view == null)
				return false;
			display.Enabled = view.CanInsertSlash();
			return true;
		}

		public bool OnInsertSlash(object args)
		{
			CheckDisposed();
			StringRepSliceView view = Control as StringRepSliceView;
			m_cache.DomainDataByFlid.BeginUndoTask(MEStrings.ksInsertEnvironmentSlash, MEStrings.ksInsertEnvironmentSlash);
			view.RootBox.OnChar((int)'/');
			m_cache.DomainDataByFlid.EndUndoTask();
			return true;
		}

		/// <summary>
		/// This menu item is turned off if an underscore already exists in the environment
		/// string.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertEnvironmentBar(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			StringRepSliceView view = Control as StringRepSliceView;
			if (view == null)
				return false;
			display.Enabled = view.CanInsertEnvBar();
			return true;
		}

		public bool OnInsertEnvironmentBar(object args)
		{
			CheckDisposed();
			StringRepSliceView view = Control as StringRepSliceView;
			m_cache.DomainDataByFlid.BeginUndoTask(MEStrings.ksInsertEnvironmentBar, MEStrings.ksInsertEnvironmentBar);
			view.RootBox.OnChar((int)'_');
			m_cache.DomainDataByFlid.EndUndoTask();
			return true;
		}

		/// <summary>
		/// This menu item is on if a slash already exists in the environment.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertNaturalClass(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			StringRepSliceView view = Control as StringRepSliceView;
			if (view == null)
				return false;
			display.Enabled = view.CanInsertItem();
			return true;
		}

		public bool OnInsertNaturalClass(object args)
		{
			CheckDisposed();
			StringRepSliceView view = Control as StringRepSliceView;
			m_cache.DomainDataByFlid.BeginUndoTask(MEStrings.ksInsertNaturalClass, MEStrings.ksInsertNaturalClass);
			bool fOk = SimpleListChooser.ChooseNaturalClass(view.RootBox, m_cache,
				m_persistenceProvider, Mediator);
			m_cache.DomainDataByFlid.EndUndoTask();
			return fOk;
		}

		/// <summary>
		/// This menu item is on if a slash already exists in the environment.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertOptionalItem(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			StringRepSliceView view = Control as StringRepSliceView;
			if (view == null)
				return false;
			display.Enabled = view.CanInsertItem();
			return true;
		}

		public bool OnInsertOptionalItem(object args)
		{
			CheckDisposed();
			StringRepSliceView view = Control as StringRepSliceView;
			IVwRootBox rootb = view.RootBox;
			m_cache.DomainDataByFlid.BeginUndoTask(MEStrings.ksInsertOptionalItem, MEStrings.ksInsertOptionalItem);
			PhoneEnvReferenceSlice.InsertOptionalItem(rootb);
			m_cache.DomainDataByFlid.EndUndoTask();
			return true;
		}

		/// <summary>
		/// This menu item is on if a slash already exists in the environment.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayInsertHashMark(object commandObject,
			ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			StringRepSliceView view = Control as StringRepSliceView;
			if (view == null)
				return false;
			display.Enabled = view.CanInsertHashMark();
			return true;
		}

		public bool OnInsertHashMark(object args)
		{
			CheckDisposed();
			StringRepSliceView view = Control as StringRepSliceView;
			m_cache.DomainDataByFlid.BeginUndoTask(MEStrings.ksInsertWordBoundary, MEStrings.ksInsertWordBoundary);
			view.RootBox.OnChar((int)'#');
			m_cache.DomainDataByFlid.EndUndoTask();
			return true;
		}
		#endregion

		#region RootSite class

		class StringRepSliceView : RootSiteControl, INotifyControlInCurrentSlice
		{
			IPhEnvironment m_env;
			int m_hvoObj;
			StringRepSliceVc m_vc = null;
			private PhonEnvRecognizer m_validator;

			public StringRepSliceView(int hvo)
			{
				m_hvoObj = hvo;
			}

			public void ResetValidator()
			{
				CheckDisposed();

				m_validator = new PhonEnvRecognizer(
					m_fdoCache.LangProject.PhonologicalDataOA.AllPhonemes().ToArray(),
					m_fdoCache.LangProject.PhonologicalDataOA.AllNaturalClassAbbrs().ToArray());
			}

			/// <summary>
			/// Clean up any resources being used.
			/// </summary>
			protected override void Dispose(bool disposing)
			{
				// Must not be run more than once.
				if (IsDisposed)
					return;

				base.Dispose(disposing);

				if (disposing)
				{
				}

				m_env = null;
				m_vc = null;
				m_validator = null; // TODO: Make m_validator disposable?
			}

			#region INotifyControlInCurrentSlice implementation

			/// <summary>
			/// Adjust controls based on whether the slice is the current slice.
			/// </summary>
			public bool SliceIsCurrent
			{
				set
				{
					CheckDisposed();

					// SliceIsCurrent may be called in the process of deleting the object after the object
					// has been partially cleared out and thus would certainly fail the constraint
					// check, then try to instantiate an error annotation which wouldn't have an
					// owner, causing bad things to happen.
					if (DesignMode || m_rootb == null || !m_env.IsValidObject)
						return;

					if (!value)
					{
						DoValidation(true); // JohnT: do we really always want a Refresh? Trying to preserve the previous behavior...
					}
				}
			}

			#endregion INotifyControlInCurrentSlice implementation

			private void DoValidation(bool refresh)
			{
				Form frm = FindForm();
				// frm may be null, if the record has been switched
				WaitCursor wc = null;
				try
				{
					if (frm != null)
						wc = new WaitCursor(frm);
					ConstraintFailure failure;
					m_env.CheckConstraints(PhEnvironmentTags.kflidStringRepresentation, true, out failure, /* adjust the squiggly line */ true);
					// This will make the record list update to the new value.
					if(refresh)
						Mediator.BroadcastMessage("Refresh", null);
				}
				finally
				{
					if (wc != null)
						wc.Dispose();
				}
			}

			/// <summary>
			/// This method seems to get called when we are switching to another tool (or area, or slice) AND when the
			/// program is shutting down. This makes it a good point to check constraints, since in some of these
			/// cases, SliceIsCurrent may not get set false.
			/// </summary>
			protected override void OnValidating(System.ComponentModel.CancelEventArgs e)
			{
				base.OnValidating(e);
				// Only necessary to ensure that validation is done when window is going away. We don't need a Refresh then!
				DoValidation(false);
			}

			public override void MakeRoot()
			{
				CheckDisposed();

				base.MakeRoot();

				if (m_fdoCache == null || DesignMode)
					return;

				// A crude way of making sure the property we want is loaded into the cache.
				m_env = m_fdoCache.ServiceLocator.GetInstance<IPhEnvironmentRepository>().GetObject(m_hvoObj);
				m_vc = new StringRepSliceVc();
				// Review JohnT: why doesn't the base class do this??
				m_rootb = VwRootBoxClass.Create();
				m_rootb.SetSite(this);
				// And maybe this too, at least by default?
				m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;

				// arg3 is a meaningless initial fragment, since this VC only displays one thing.
				// arg4 could be used to supply a stylesheet.
				m_rootb.SetRootObject(m_hvoObj, m_vc, StringRepSliceVc.Flid, null);
			}

			internal bool CanShowEnvironmentError()
			{
				CheckDisposed();

				string s = m_env.StringRepresentation.Text;
				if (s == null || s == String.Empty)
					return false;
				return (!m_validator.Recognize(s));
			}

			internal void ShowEnvironmentError()
			{
				CheckDisposed();

				string s = m_env.StringRepresentation.Text; ;
				if (s == null || s == String.Empty)
					return;
				if (!m_validator.Recognize(s))
				{
					string sMsg;
					int pos = 0;
					PhonEnvRecognizer.CreateErrorMessageFromXml(s, m_validator.ErrorMessage, out pos, out sMsg);
					MessageBox.Show(sMsg, MEStrings.ksErrorInEnvironment,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}

			internal bool CanInsertSlash()
			{
				CheckDisposed();

				string s = m_env.StringRepresentation.Text;
				if (s == null || s == String.Empty)
					return true;
				return s.IndexOf('/') < 0;
			}

			private int GetSelectionEndPoint(bool fEnd)
			{
				IVwSelection vwsel = m_rootb.Selection;
				if (vwsel == null)
					return -1;
				int ichEnd;
				ITsString tss;
				bool fAssocPrev;
				int hvo;
				int flid;
				int ws;
				vwsel.TextSelInfo(fEnd, out tss, out ichEnd, out fAssocPrev, out hvo, out flid,
					out ws);
				Debug.Assert(hvo == m_env.Hvo);
				Debug.Assert(flid == PhEnvironmentTags.kflidStringRepresentation);
				return ichEnd;
			}

			internal bool CanInsertEnvBar()
			{
				CheckDisposed();

				string s = m_env.StringRepresentation.Text;
				if (s == null || s == String.Empty)
					return false;
				int ichSlash = s.IndexOf('/');
				if (ichSlash < 0)
					return false;
				int ichEnd = GetSelectionEndPoint(true);
				if (ichEnd < 0)
					return false;
				int ichAnchor = GetSelectionEndPoint(false);
				if (ichAnchor < 0)
					return false;
				return (ichEnd > ichSlash) && (ichAnchor > ichSlash) && (s.IndexOf('_') < 0);
			}

			internal bool CanInsertItem()
			{
				CheckDisposed();

				string s = m_env.StringRepresentation.Text;
				if (s == null || s == String.Empty)
					return false;
				int ichEnd = GetSelectionEndPoint(true);
				int ichAnchor = GetSelectionEndPoint(false);
				return PhonEnvRecognizer.CanInsertItem(s, ichEnd, ichAnchor);
			}

			internal bool CanInsertHashMark()
			{
				CheckDisposed();

				string s = m_env.StringRepresentation.Text;
				if (s == null || s == String.Empty)
					return false;
				int ichEnd = GetSelectionEndPoint(true);
				int ichAnchor = GetSelectionEndPoint(false);
				return PhonEnvRecognizer.CanInsertHashMark(s, ichEnd, ichAnchor);
			}

			#region Handle right click menu
			protected override bool OnRightMouseUp(System.Drawing.Point pt,
				System.Drawing.Rectangle rcSrcRoot, System.Drawing.Rectangle rcDstRoot)
			{
				if (m_env == null)
					return false;
				// We need a CmObjectUi in order to call HandleRightClick().
				using (SIL.FieldWorks.FdoUi.CmObjectUi ui = new SIL.FieldWorks.FdoUi.CmObjectUi(m_env))
					return ui.HandleRightClick(Mediator, this, true, "mnuEnvChoices");
			}
			#endregion
		}

		#endregion RootSite class


		#region View Constructor

		public class StringRepSliceVc : FwBaseVc
		{
			static public int Flid
			{
				get { return PhEnvironmentTags.kflidStringRepresentation; }
			}

			public StringRepSliceVc()
			{
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				vwenv.AddStringProp(Flid, this);
			}
		}

		#endregion View Constructor
	}
}
