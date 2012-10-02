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
// File: AtomicReferenceView.cs
// Responsibility: Steve McConnel
// Last reviewed:
//
// <remarks>
// borrowed and hacked from PhoneEnvReferenceView
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Validation;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Main class for displaying the AtomicReferenceSlice.
	/// </summary>
	public class AtomicReferenceView : ReferenceViewBase
	{
		#region Constants and data members

		// View frags.
		public const int kFragAtomicRef = 1;
		public const int kFragObjName = 2;

		protected AtomicReferenceVc m_atomicReferenceVc;
		// this is used to guarantee correct initial size.
		private int m_hOld = 0;

		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// This allows the view to communicate size changes to the embedding slice.
		/// </summary>
		public event SIL.FieldWorks.Common.Utils.FwViewSizeChangedEventHandler ViewSizeChanged;

		#endregion // Constants and data members

		#region Construction, initialization, and disposal

		public AtomicReferenceView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
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

			base.Dispose(disposing);

			if (disposing)
			{
				if (components != null)
				{
					components.Dispose();
				}
				if (m_atomicReferenceVc != null)
					m_atomicReferenceVc.Dispose();
			}
			m_atomicReferenceVc = null;
			m_rootObj = null;
			m_displayNameProperty = null;
		}


		/// <summary>
		/// Set the item from the chooser.
		/// </summary>
		/// <param name="realHvo">ID of the object from the chooser.</param>
		public void SetObject(int realHvo)
		{
			CheckDisposed();
			if (realHvo != 0)
				m_rootObj = CmObject.CreateFromDBObject(m_fdoCache, realHvo);
			else
				m_rootObj = null;
			if (m_rootb != null)
			{
				SetRootBoxObj();
				int h2 = m_rootb.Height;
				if (m_hOld != h2)
				{
					m_hOld = h2;
					if (ViewSizeChanged != null)
						ViewSizeChanged(this, new FwViewSizeEventArgs(h2, m_rootb.Width));
				}
			}
		}

		public int ObjectHvo
		{
			get
			{
				CheckDisposed();
				if (m_rootObj == null)
					return 0;
				else
					return m_rootObj.Hvo;
			}
		}

		private void SetRootBoxObj()
		{
			if (m_rootObj != null && m_rootObj.Hvo != 0)
			{
				// The ViewSizeChanged logic should be triggered automatically by a notification
				// from the rootbox.
				int h1 = m_rootb.Height;
				int w1 = m_rootb.Width;
				m_rootb.SetRootObject(m_rootObj.Hvo, m_atomicReferenceVc,
					AtomicReferenceView.kFragAtomicRef, m_rootb.Stylesheet);
				int h2 = m_rootb.Height;
				int w2 = m_rootb.Width;
				if (h1 != h2)
				{
					if (ViewSizeChanged != null)
						ViewSizeChanged(this, new FwViewSizeEventArgs(h2, w2));
				}
				if (w1 != w2 && w2 > Width)
				{
				}
			}
		}

		#endregion // Construction, initialization, and disposal

		#region RootSite required methods

		public override void MakeRoot()
		{
			CheckDisposed();
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			SetReferenceVc();
			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
			SetRootBoxObj();
		}

		public virtual void SetReferenceVc()
		{
			CheckDisposed();
			m_atomicReferenceVc = new AtomicReferenceVc(m_fdoCache, m_rootFlid, m_displayNameProperty);
		}

		#endregion // RootSite required methods

		#region other overrides

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called when the focus leaves the control. We want to hide the selection.
		/// </summary>
		/// <param name="e">An <see cref="T:System.EventArgs"/> that contains the event data.</param>
		/// ------------------------------------------------------------------------------------
		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);

			if (RootBox != null)
				RootBox.DestroySelection();
		}

		public override void SelectionChanged(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();
			if (vwselNew == null)
				return;
			int cvsli = vwselNew.CLevels(false);
			// CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			cvsli--;
			if (cvsli == 0)
			{
				// No objects in selection: don't allow a selection.
				m_rootb.DestroySelection();
				// Enhance: invoke launcher's selection dialog.
				return;
			}
			ITsString tss;
			int ichAnchor;
			int ichEnd;
			bool fAssocPrev;
			int hvoObj;
			int hvoObjEnd;
			int tag;
			int ws;
			vwselNew.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj,
				out tag, out ws);
			vwselNew.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjEnd,
				out tag, out ws);
			if (hvoObj != hvoObjEnd)
				return;
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ihvoEnd;
			ITsTextProps ttp;
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(vwselNew, cvsli,
				out ihvoRoot, out tagTextProp, out cpropPrevious, out ichAnchor, out ichEnd,
				out ws, out fAssocPrev, out ihvoEnd, out ttp);
			IVwSelection vwselWhole = null;
			Debug.Assert(m_rootb != null);
			// Create a selection that covers the entire target object.  If it differs from
			// the new selection, we'll install it (which will recurse back to this method).
			vwselWhole = m_rootb.MakeTextSelInObj(ihvoRoot, cvsli, rgvsli, 0, null,
				false, false, false, true, false);
			if (vwselWhole != null)
			{
				ITsString tssWhole;
				int ichAnchorWhole;
				int ichEndWhole;
				int hvoObjWhole;
				int hvoObjEndWhole;
				bool fAssocPrevWhole;
				int tagWhole;
				int wsWhole;
				vwselWhole.TextSelInfo(false, out tssWhole, out ichAnchorWhole,
					out fAssocPrevWhole, out hvoObjWhole, out tagWhole, out wsWhole);
				vwselWhole.TextSelInfo(true, out tssWhole, out ichEndWhole,
					out fAssocPrevWhole, out hvoObjEndWhole, out tagWhole, out wsWhole);
				if (hvoObj == hvoObjWhole && hvoObjEnd == hvoObjEndWhole &&
					(ichAnchor != ichAnchorWhole || ichEnd != ichEndWhole))
				{
					// Install it this time!
					m_rootb.MakeTextSelInObj(ihvoRoot, cvsli, rgvsli, 0, null,
						false, false, false, true, true);
				}
			}
		}

		#endregion

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			//
			// AtomicReferenceView
			//
			this.Name = "AtomicReferenceView";
			this.Size = new System.Drawing.Size(232, 18);
		}
		#endregion

	}

	#region AtomicReferenceVc class

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class AtomicReferenceVc : VwBaseVc
	{
		protected FdoCache m_cache;
		protected int m_flid;
		protected string m_displayNameProperty;

		public AtomicReferenceVc(FdoCache cache, int flid, string displayNameProperty)
		{
			Debug.Assert(cache != null);
			m_cache = cache;
			m_flid = flid;
			m_displayNameProperty = displayNameProperty;
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
			m_cache = null;
			m_displayNameProperty = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			switch (frag)
			{
				case AtomicReferenceView.kFragAtomicRef:
					// Display a paragraph with a single item.
					int hvoProp = HvoOfObjectToDisplay(vwenv, hvo);
					if (hvoProp == 0)
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
							(int)FwTextPropVar.ktpvDefault,
							(int)ColorUtil.ConvertColorToBGR(Color.Gray));
						vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent,
							(int)FwTextPropVar.ktpvMilliPoint, 18000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
							(int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);

						//vwenv.AddString(m_cache.MakeUserTss("Click to select -->"));
						vwenv.NoteDependency(new int[] {hvo}, new int[] {m_flid}, 1);
					}
					else
					{
						vwenv.OpenParagraph();		// vwenv.OpenMappedPara();
						DisplayObjectProperty(vwenv, hvoProp);
						vwenv.CloseParagraph();
					}
					break;
				case AtomicReferenceView.kFragObjName:
					// Display one reference.
				{
					ILgWritingSystemFactory wsf =
						m_cache.LanguageWritingSystemFactoryAccessor;

					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvDefault,
						(int)TptEditable.ktptNotEditable);
					ITsString tss;
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					Debug.Assert(hvo != 0);
					// Use reflection to get a prebuilt name if we can.  Otherwise
					// settle for piecing together a string.
					Debug.Assert(m_cache != null);
					ICmObject obj = CmObject.CreateFromDBObject(m_cache, hvo);
					Debug.Assert(obj != null);
					System.Type type = obj.GetType();
					System.Reflection.PropertyInfo pi = type.GetProperty("TsName",
						System.Reflection.BindingFlags.Instance |
						System.Reflection.BindingFlags.Public |
						System.Reflection.BindingFlags.FlattenHierarchy);
					if (pi != null)
					{
						tss = (ITsString)pi.GetValue(obj, null);
					}
					else
					{
						if (m_displayNameProperty != null && m_displayNameProperty != string.Empty)
						{
							pi = type.GetProperty(m_displayNameProperty,
								System.Reflection.BindingFlags.Instance |
								System.Reflection.BindingFlags.Public |
								System.Reflection.BindingFlags.FlattenHierarchy);
						}
						int ws = wsf.GetWsFromStr(obj.SortKeyWs);
						if (ws == 0)
							ws = m_cache.DefaultAnalWs;
						if (pi != null)
						{
							object info = pi.GetValue(obj, null);
							// handle the object type
							if (info is String)
								tss = tsf.MakeString((string)info, ws);
							else if (info is MultiUnicodeAccessor)
							{
								MultiUnicodeAccessor accessor = info as MultiUnicodeAccessor;
								tss = accessor.GetAlternativeTss(ws); // try the requested one (or default analysis)
								if (tss == null || tss.Length == 0)
									tss = accessor.BestAnalysisVernacularAlternative; // get something
							}
							else if (info is ITsString)
								tss = (ITsString)info;
							else
								tss = null;
						}
						else
						{
							tss = obj.ShortNameTSS; // prefer this, which is hopefully smart about wss.
							if (tss == null || tss.Length == 0)
							{
								tss = tsf.MakeString(obj.ShortName, ws);
							}
						}
					}
					vwenv.AddString(tss);
				}
					break;
				default:
					throw new ArgumentException(
						"Don't know what to do with the given frag.", "frag");
			}
		}

		protected virtual void DisplayObjectProperty(IVwEnv vwenv, int hvo)
		{
			vwenv.AddObjProp(m_flid, this, AtomicReferenceView.kFragObjName);
		}

		protected virtual int HvoOfObjectToDisplay(IVwEnv vwenv, int hvo)
		{
			ISilDataAccess sda = vwenv.DataAccess;
			return sda.get_ObjectProp(hvo, m_flid);
		}
	}

	#endregion // AtomicReferenceVc class
}
