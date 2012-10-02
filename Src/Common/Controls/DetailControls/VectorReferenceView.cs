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
// File: VectorReferenceView.cs
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
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.FDO.Validation;
using SIL.FieldWorks.Common.Utils;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Main class for displaying the VectorReferenceSlice.
	/// </summary>
	public class VectorReferenceView : ReferenceViewBase
	{
		#region Constants and data members

		// View frags.
		public const int kfragTargetVector = 1;
		public const int kfragTargetObj = 2;

		protected VectorReferenceVc m_VectorReferenceVc;
		protected string m_displayWs;
		/// <summary>
		/// This is set true during OnKeyPress (backspace) or OnKeyDown (Delete) for the only
		/// two keystrokes that are allowed to delete items. Others will fail.
		/// </summary>
		protected bool m_fOkToDeleteItem = false;

		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// This allows the view to communicate size changes to the embedding slice.
		/// </summary>
		public event SIL.FieldWorks.Common.Utils.FwViewSizeChangedEventHandler ViewSizeChanged;

		#endregion // Constants and data members

		#region Construction, initialization, and disposal

		public VectorReferenceView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		public void Initialize(ICmObject rootObj, int rootFlid, FdoCache cache, string displayNameProperty,
			XCore.Mediator mediator, string displayWs)
		{
			CheckDisposed();
			m_displayWs = displayWs;
			base.Initialize(rootObj, rootFlid, cache, displayNameProperty, mediator);
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
				if (m_VectorReferenceVc != null)
					m_VectorReferenceVc.Dispose();
			}
			m_VectorReferenceVc = null;
			m_rootObj = null;
			m_displayNameProperty = null;
		}


		/// <summary>
		/// Reload the vector in the root box, presumably after it's been modified by a chooser.
		/// </summary>
		public void ReloadVector()
		{
			CheckDisposed();
			m_rootb.SetRootObject(m_rootObj.Hvo, m_VectorReferenceVc, kfragTargetVector, m_rootb.Stylesheet);
		}

		#endregion // Construction, initialization, and disposal

		#region RootSite required methods

		public override void MakeRoot()
		{
			CheckDisposed();
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			m_VectorReferenceVc = CreateVectorReferenceVc();
			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_fdoCache.MainCacheAccessor;
			m_rootb.SetRootObject(m_rootObj.Hvo, m_VectorReferenceVc, kfragTargetVector, null);
		}

		protected virtual VectorReferenceVc CreateVectorReferenceVc()
		{
			return new VectorReferenceVc(m_fdoCache, m_rootFlid, m_displayNameProperty, m_displayWs);
		}

		#endregion // RootSite required methods

		#region other overrides and related methods

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

		/// <summary>
		/// override this to allow deleting an item IF the key is Delete.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnKeyDown(KeyEventArgs e)
		{
			try
			{
				if (e.KeyCode == Keys.Delete)
					m_fOkToDeleteItem = true;
				base.OnKeyDown (e);
			}
			finally
			{
				m_fOkToDeleteItem = false;
			}
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			try
			{
				if (e.KeyChar == (char)Keys.Back)
					m_fOkToDeleteItem = true;
				base.OnKeyPress (e);
			}
			finally
			{
				m_fOkToDeleteItem = false;
			}
		}

		public override VwDelProbResponse OnProblemDeletion(IVwSelection sel,
			VwDelProbType dpt)
		{
			CheckDisposed();
			int cvsli;
			int hvoObj;
			if (!CheckForValidDelete(sel, out cvsli, out hvoObj))
				return VwDelProbResponse.kdprAbort;

			return DeleteObjectFromVector(sel, cvsli, hvoObj);
		}

		protected VwDelProbResponse DeleteObjectFromVector(IVwSelection sel, int cvsli, int hvoObj)
		{
			int hvoObjEnd = hvoObj;
			int ichAnchor;
			int ichEnd;
			bool fAssocPrev;
			int ws;
			int ihvoRoot;
			int tagTextProp;
			int cpropPrevious;
			int ihvoEnd;
			ITsTextProps ttp;
			SelLevInfo[] rgvsli = SelLevInfo.AllTextSelInfo(sel, cvsli,
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
					ichAnchor == ichAnchorWhole && ichEnd == ichEndWhole)
				{
					// We've selected the whole string for it, so remove the object from the
					// vector.
					int[] hvosOld = m_fdoCache.GetVectorProperty(m_rootObj.Hvo, m_rootFlid,
						false);
					UpdateTimeStampsIfNeeded(hvosOld);
					for (int i = 0; i < hvosOld.Length; ++i)
					{
						if (hvosOld[i] == hvoObj)
						{
							RemoveObjectFromList(hvosOld, i);
							break;
						}
					}
				}
			}
			return VwDelProbResponse.kdprDone;
		}

		/// <summary>
		/// When deleting from a LexReference, all the affected LexEntry objects need to
		/// have their timestamps updated.  (See LT-5523.)  Most of the time, this operation
		/// does nothing.
		/// </summary>
		/// <param name="hvos"></param>
		protected virtual void UpdateTimeStampsIfNeeded(int[] hvos)
		{
		}

		protected bool CheckForValidDelete(IVwSelection sel, out int cvsli, out int hvoObj)
		{
			cvsli = 0;
			hvoObj = 0;
			if (!m_fOkToDeleteItem)
				return false;

			cvsli = sel.CLevels(false);
			// CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			cvsli--;
			if (cvsli == 0)
			{
				// No object in selection, so quit.
				return false;
			}
			ITsString tss;
			int ichAnchor;
			bool fAssocPrev;
			int tag;
			int ws;
			int ichEnd;
			int hvoObjEnd;
			sel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj,
				out tag, out ws);
			sel.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjEnd,
				out tag, out ws);
			return (hvoObj == hvoObjEnd);
		}

		private void RemoveObjectFromList(int[] hvosOld, int ihvo)
		{
			int startHeight = m_rootb.Height;
			int oldCount = m_fdoCache.GetVectorSize(m_rootObj.Hvo, m_rootFlid);
			Debug.Assert(oldCount == hvosOld.Length);
			int[] hvos = new int[oldCount - 1];
			for (int i = 0; i < ihvo; ++i)
				hvos[i] = hvosOld[i];
			for (int i = ihvo + 1; i < oldCount; ++i)
				hvos[i - 1] = hvosOld[i];
			m_fdoCache.BeginUndoTask(
				String.Format(DetailControlsStrings.ksUndoDeleteItem, m_rootFlid),
				String.Format(DetailControlsStrings.ksRedoDeleteItem, m_rootFlid));
			m_fdoCache.ReplaceReferenceProperty(m_rootObj.Hvo, m_rootFlid, 0, oldCount,
				ref hvos);
			m_fdoCache.EndUndoTask();
			m_fdoCache.PropChanged(null, PropChangeType.kpctNotifyAll, m_rootObj.Hvo,
				m_rootFlid, 0, hvos.Length, oldCount - hvos.Length);
			CheckViewSizeChanged(startHeight, m_rootb.Height);
			// Redisplay (?) the vector property.
			m_rootb.SetRootObject(m_rootObj.Hvo, m_VectorReferenceVc, kfragTargetVector, m_rootb.Stylesheet);
		}

		protected void CheckViewSizeChanged(int startHeight, int endHeight)
		{
			if (startHeight != endHeight && ViewSizeChanged != null)
				ViewSizeChanged(this, new FwViewSizeEventArgs(endHeight, m_rootb.Width));
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
			// VectorReferenceView
			//
			this.Name = "VectorReferenceView";
			this.Size = new System.Drawing.Size(232, 40);

		}
		#endregion
	}

	#region VectorReferenceVc class

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class VectorReferenceVc : VwBaseVc
	{
		protected FdoCache m_cache;
		protected int m_flid;
		protected string m_displayNameProperty;
		protected string m_displayWs;

		/// <summary>
		/// Constructor for the Vector Reference View Constructor Class.
		/// </summary>
		public VectorReferenceVc(FdoCache cache, int flid, string displayNameProperty, string displayWs)
		{
			Debug.Assert(cache != null);
			m_cache = cache;
			m_flid = flid;
			m_displayNameProperty = displayNameProperty;
			m_displayWs = displayWs;
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

		/// <summary>
		/// This is the basic method needed for the view constructor.
		/// </summary>
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();
			switch (frag)
			{
				case VectorReferenceView.kfragTargetVector:
					// Check for an empty vector.
					if (m_cache.GetVectorSize(hvo, m_flid) == 0)
					{
						vwenv.set_IntProperty((int)FwTextPropType.ktptForeColor,
							(int)FwTextPropVar.ktpvDefault,
							(int)ColorUtil.ConvertColorToBGR(Color.Gray));
						vwenv.set_IntProperty((int)FwTextPropType.ktptLeadingIndent,
							(int)FwTextPropVar.ktpvMilliPoint, 18000);
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
							(int)FwTextPropVar.ktpvDefault,
							(int)TptEditable.ktptNotEditable);
						vwenv.set_IntProperty((int)FwTextPropType.ktptAlign,
							(int)FwTextPropVar.ktpvEnum, (int)FwTextAlign.ktalRight);
						//vwenv.AddString(m_cache.MakeUserTss("Click to select -->"));
						vwenv.NoteDependency(new int[] {hvo}, new int[] {m_flid}, 1);
					}
					else
					{
						vwenv.OpenParagraph();
						vwenv.AddObjVec(m_flid, this, frag);
						vwenv.CloseParagraph();
					}
					break;
				case VectorReferenceView.kfragTargetObj:
					// Display one object from the vector.
				{
					ILgWritingSystemFactory wsf =
						m_cache.LanguageWritingSystemFactoryAccessor;

					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvDefault,
						(int)TptEditable.ktptIsEditable);
					ITsString tss;
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					Debug.Assert(hvo != 0);
#if USEBESTWS
					if (m_displayWs != null && m_displayWs.StartsWith("best"))
					{
						// The flid can be a variety of types, so deal with those.
						Debug.WriteLine("Using 'best ws': " + m_displayWs);
						int magicWsId = LangProject.GetMagicWsIdFromName(m_displayWs);
						int actualWS = m_cache.LangProject.ActualWs(magicWsId, hvo, m_flid);
						Debug.WriteLine("Actual ws: " + actualWS.ToString());
					}
					else
					{
#endif
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
								object s = pi.GetValue(obj, null);
								if (s is ITsString)
									tss = (ITsString)s;
								else
									tss = tsf.MakeString((string)s, ws);
							}
							else
							{
								tss = obj.ShortNameTSS;
							}
#if USEBESTWS
						}
#endif
					}
					vwenv.AddString(tss);
				}
					break;
				default:
					throw new ArgumentException(
						"Don't know what to do with the given frag.", "frag");
			}
		}

		/// <summary>
		/// Calling vwenv.AddObjVec() in Display() and implementing DisplayVec() seems to
		/// work better than calling vwenv.AddObjVecItems() in Display().  Theoretically
		/// this should not be case, but experience trumps theory every time.  :-) :-(
		/// </summary>
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			CheckDisposed();
			ISilDataAccess da = vwenv.DataAccess;
			int count = da.get_VecSize(hvo, tag);
			for (int i = 0; i < count; ++i)
			{
				vwenv.AddObj(da.get_VecItem(hvo, tag, i), this,
					VectorReferenceView.kfragTargetObj);
				vwenv.AddSeparatorBar();
			}
		}
	}

	#endregion // VectorReferenceVc class
}
