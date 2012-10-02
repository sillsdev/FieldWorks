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
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.CoreImpl;
using XCore;

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

		internal System.Xml.XmlNode ConfigurationNode { get; set; }

		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// This allows the view to communicate size changes to the embedding slice.
		/// </summary>
		public event FwViewSizeChangedEventHandler ViewSizeChanged;

		#endregion // Constants and data members

		#region Construction, initialization, and disposal

		public VectorReferenceView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		public void Initialize(ICmObject rootObj, int rootFlid, string rootFieldName, FdoCache cache, string displayNameProperty,
			XCore.Mediator mediator, string displayWs)
		{
			CheckDisposed();
			m_displayWs = displayWs;
			Initialize(rootObj, rootFlid, rootFieldName, cache, displayNameProperty, mediator);
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
			}
			m_VectorReferenceVc = null;
			m_rootObj = null;
			m_displayNameProperty = null;
		}

		public void OnMoveTargetDownInSequence(object commandObject)
		{

		}

		public void OnMoveTargetUpInSequence(object commandObject)
		{

		}

		/// <summary>
		/// Reload the vector in the root box, presumably after it's been modified by a chooser.
		/// </summary>
		public virtual void ReloadVector()
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
			m_rootb.DataAccess = m_fdoCache.DomainDataByFlid;
			SetupRoot();
		}

		protected override void SetupRoot()
		{
			m_VectorReferenceVc.Reuse(m_rootFlid, m_displayNameProperty, m_displayWs);
			m_rootb.SetRootObject(m_rootObj == null ? 0 : m_rootObj.Hvo, m_VectorReferenceVc, kfragTargetVector, null);
		}

		protected virtual VectorReferenceVc CreateVectorReferenceVc()
		{
			return new VectorReferenceVc(m_fdoCache, m_rootFlid, m_displayNameProperty, m_displayWs);
		}

		#endregion // RootSite required methods

		#region other overrides and related methods

		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
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
			Debug.Assert(m_rootb != null);
			// Create a selection that covers the entire target object.  If it differs from
			// the new selection, we'll install it (which will recurse back to this method).
			IVwSelection vwselWhole = m_rootb.MakeTextSelInObj(ihvoRoot, cvsli, rgvsli, 0, null,
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
			if (e.KeyCode == Keys.Delete)
				Delete();
			if (e.KeyCode == Keys.Left)
				MoveItem(false);
			if (e.KeyCode == Keys.Right)
				MoveItem(true);
			if (!IsDisposed)		// Delete() can cause the view to be removed.
				base.OnKeyDown(e);
		}

		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (e.KeyChar == (char)Keys.Back)
				Delete();
			base.OnKeyPress(e);
		}

		internal bool CanMoveItem(bool forward, out bool visible)
		{
			int ihvo;
			List<ICmObject> vals;
			return PrepareForMoveItem(forward, out visible, out ihvo, out vals);
		}

		internal void MoveItem(bool forward)
		{
			int ihvo;
			List<ICmObject> vals;
			bool visible;
			if (!PrepareForMoveItem(forward, out visible, out ihvo, out vals))
				return;

			UndoableUnitOfWorkHelper.Do(DetailControlsStrings.ksUndoReorder, DetailControlsStrings.ksRedoReorder, Cache.ActionHandlerAccessor,
				() => ReorderItems(vals));

			// Create a new selection of the moved object.
			SelLevInfo[] rgvsli = new SelLevInfo[1];
			rgvsli[0].ihvo = ihvo;
			rgvsli[0].tag = m_rootFlid;
			IVwSelection vwselWhole = m_rootb.MakeTextSelInObj(0, 1, rgvsli, 0, null,
															   false, false, false, true, true);
		}

		private void ReorderItems(List<ICmObject> vals)
		{
			if (RootPropertyIsRealRefSequence())
			{
				// Since we are re-ordering, presume all objects are replaced by the entire new value.
				Cache.DomainDataByFlid.Replace(m_rootObj.Hvo, m_rootFlid, 0, vals.Count,
					vals.Select(obj => obj.Hvo).ToArray(), vals.Count);
			}
			else
			{
				VirtualOrderingServices.SetVO(m_rootObj, m_rootFlid, vals);
			}
		}

		internal void RemoveOrdering()
		{
			UndoableUnitOfWorkHelper.Do(DetailControlsStrings.ksUndoAlphabeticalOrder, DetailControlsStrings.ksRedoAlphabeticalOrder,
				Cache.ActionHandlerAccessor,
				() => VirtualOrderingServices.ResetVO(m_rootObj, m_rootFlid));
		}

		/// <summary>
		/// This method will determine if the Item can be moved, if it can all the out variables will be set in preparation
		/// for use by MoveItem
		/// </summary>
		/// <param name="forward"></param>
		/// <param name="visible">Used to indicate if a move is even legal</param>
		/// <param name="ihvo">returns the index of the selected object? Not sure this works</param>
		/// <param name="vals">The new ordering of the items in the vector after the requested move</param>
		/// <returns>true if the Move could be accomplished</returns>
		private bool PrepareForMoveItem(bool forward, out bool visible, out int ihvo, out List<ICmObject> vals)
		{
			ihvo = 0; // default
			vals = null; // default
			visible = false;
			// Fundamentally, we can handle either reference sequence properties or ones we are explicitly told
			// to create VirtualPropOrderings for.
			if (!RootPropertyIsRealRefSequence() && !RootPropertySupportsVirtualOrdering())
				return false;
			visible = true; // Command makes sense even if we can't actually do it now.
			if (m_rootb.Selection == null)
				return false;
			int cvsli = m_rootb.Selection.CLevels(false);
			// CLevels includes the string property itself, but AllTextSelInfo doesn't need it.
			cvsli--;
			if (cvsli <= 0)
			{
				// No object in selection, so quit.
				return false;
			}
			int hvoObj, flid, cpropPrevious;
			IVwPropertyStore vps;
			//the index (ihvo) set by this method is the index of the visible items, any logic using this index must also deal with only the visible items
			m_rootb.Selection.PropInfo(false, cvsli, out hvoObj, out flid, out ihvo, out cpropPrevious, out vps);
			Debug.Assert(hvoObj == m_rootObj.Hvo);
			Debug.Assert(flid == m_rootFlid);
			var sda = m_rootb.DataAccess as ISilDataAccessManaged;
			var objRepo = Cache.ServiceLocator.ObjectRepository;
			//set vals to the visible items
			var visibleItems = GetVisibleItemList();
			vals = visibleItems;
			var move = visibleItems[ihvo];
			visibleItems.RemoveAt(ihvo);
			if (forward)
			{
				if (ihvo + 1 > visibleItems.Count)
					return false;
				ihvo++;
			}
			else
			{
				if (ihvo == 0)
					return false;
				ihvo--;
			}
			visibleItems.Insert(ihvo, move);
			visibleItems.AddRange(GetHiddenItemList()); //add the parent (invisible) reference and any other invisibles back into the collection
			return true;
		}

		/// <summary>
		/// This method will return the list of items in this vector which are visible to the user, the base class version returns all items.
		/// </summary>
		protected virtual List<ICmObject> GetVisibleItemList()
		{
			ISilDataAccessManaged sda = m_rootb.DataAccess as ISilDataAccessManaged;
			var objRepo = Cache.ServiceLocator.ObjectRepository;
			if (sda != null)
			{
				return (from i in sda.VecProp(m_rootObj.Hvo, m_rootFlid)
						where objRepo.GetObject(i) != null
						select objRepo.GetObject(i)).ToList();
			}
			return null;
		}
		/// <summary>
		/// This method will return the list of items in this vector which are hidden from the user, the base class version returns an empty list.
		/// </summary>
		protected virtual List<ICmObject> GetHiddenItemList()
		{
			return new List<ICmObject>();
		}

		internal bool RootPropertySupportsVirtualOrdering()
		{
			return XmlUtils.GetOptionalBooleanAttributeValue(ConfigurationNode, "reorder", false);
		}

		/// <summary>
		/// Return true if the property is a non-virtual reference sequence.
		/// </summary>
		private bool RootPropertyIsRealRefSequence()
		{
			if (Cache.MetaDataCacheAccessor.GetFieldType(m_rootFlid) != (int)CellarPropertyType.ReferenceSequence)
				return false;
			return !Cache.MetaDataCacheAccessor.get_IsVirtual(m_rootFlid);
		}

		protected virtual void Delete()
		{
			Delete(string.Format(DetailControlsStrings.ksUndoDeleteItem, m_rootFieldName),
				string.Format(DetailControlsStrings.ksRedoDeleteItem, m_rootFieldName));
		}

		protected void Delete(string undoText, string redoText)
		{
			var sel = m_rootb.Selection;
			int cvsli;
			int hvoObj;
			if (CheckForValidDelete(sel, out cvsli, out hvoObj))
				DeleteObjectFromVector(sel, cvsli, hvoObj, undoText, redoText);
		}

		protected void DeleteObjectFromVector(IVwSelection sel, int cvsli, int hvoObj, string undoText, string redoText)
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
			Debug.Assert(m_rootb != null);
			// Create a selection that covers the entire target object.  If it differs from
			// the new selection, we'll install it (which will recurse back to this method).
			IVwSelection vwselWhole = m_rootb.MakeTextSelInObj(ihvoRoot, cvsli, rgvsli, 0, null,
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
					var hvosOld = ((ISilDataAccessManaged) m_fdoCache.DomainDataByFlid).VecProp(m_rootObj.Hvo, m_rootFlid);
					UpdateTimeStampsIfNeeded(hvosOld);
					for (int i = 0; i < hvosOld.Length; ++i)
					{
						if (hvosOld[i] == hvoObj)
						{
							RemoveObjectFromList(hvosOld, i, undoText, redoText);
							break;
						}
					}
				}
			}
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
			hvoObj = 0;
			if (sel == null)
			{
				cvsli = 0; // satisfy compiler.
				return false; // nothing selected, give up.
			}

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

		/// <summary>
		/// Remove the indicated object from the list, if possible.
		/// </summary>
		protected virtual void RemoveObjectFromList(int[] hvosOld, int ihvo, string undoText, string redoText)
		{
			if (Cache.MetaDataCacheAccessor.get_IsVirtual(m_rootFlid))
			{
				// debug run to see what context variable content is available for the following:

				// which virtual property is this? (like PublishIn)
				//    get real_prop_name (like DoNotPublishIn) from  part/slice/@visField
				var field = XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "field");
				var visField = XmlUtils.GetOptionalAttributeValue(ConfigurationNode, "visField");
				//    get its real property object via its name (like DoNotPublishIn)
				//var visibilityFlid = 0;
				if (visField != null)
				{
					// Get class id: likely LexEntry or LexSense
					var clsid = m_rootObj.ClassID;
					var flidVirt = Cache.MetaDataCacheAccessor.GetFieldId2(clsid, field, true);
					//var flidReal = Cache.MetaDataCacheAccessor.GetFieldId2(clsid, visField, true);
					// remove the item from the virtual list property - thus adding it to the real property
					RemoveObjectFromEditableList(flidVirt, ihvo, undoText, redoText);
				}
				return;
			}
			RemoveObjectFromEditableList(m_rootFlid, ihvo, undoText, redoText);
		}

		/// <summary>
		/// Remove the indicated object from the editable list which can be virtual (like FdoInvertSet).
		/// </summary>
		/// <param name="flid">Field id of list belonging to this vector ref</param>
		/// <param name="ihvo">view handle of the object to remove</param>
		/// <param name="undoText">text to appear with the Edit/Undo menu item</param>
		/// <param name="redoText">text to appear with the Edit/Redo menu item</param>
		private void RemoveObjectFromEditableList(int flid, int ihvo, string undoText, string redoText)
		{
			int startHeight = m_rootb.Height;
			UndoableUnitOfWorkHelper.Do(undoText, redoText, m_rootObj,
										() => m_fdoCache.DomainDataByFlid.Replace(
											m_rootObj.Hvo, m_rootFlid, ihvo, ihvo + 1, new int[0], 0));
			if (m_rootb != null)
			{
				CheckViewSizeChanged(startHeight, m_rootb.Height);
				// Redisplay (?) the vector property.
				m_rootb.SetRootObject(m_rootObj.Hvo, m_VectorReferenceVc, kfragTargetVector, m_rootb.Stylesheet);
			}
		}

		/// <summary>
		/// Update the root object. This is currently used when one is created, so it doesn't need to handle null object.
		/// </summary>
		/// <param name="root"></param>
		internal void UpdateRootObject(ICmObject root)
		{
			m_rootObj = root;
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
	public class VectorReferenceVc : FwBaseVc
	{
		protected int m_flid;
		protected string m_displayNameProperty;
		protected string m_displayWs;

		/// <summary>
		/// Constructor for the Vector Reference View Constructor Class.
		/// </summary>
		public VectorReferenceVc(FdoCache cache, int flid, string displayNameProperty, string displayWs)
		{
			Debug.Assert(cache != null);
			Cache = cache;
			Reuse(flid, displayNameProperty, displayWs);
		}

		/// <summary>
		/// Set to the same state as if constructed with these arguments. (Cache should not change.)
		/// </summary>
		/// <param name="flid"></param>
		/// <param name="displayNameProperty"></param>
		/// <param name="displayWs"></param>
		public void Reuse( int flid, string displayNameProperty, string displayWs)
		{
			m_flid = flid;
			m_displayNameProperty = displayNameProperty;
			m_displayWs = displayWs;

		}

		/// <summary>
		/// This is the basic method needed for the view constructor.
		/// </summary>
		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			switch (frag)
			{
				case VectorReferenceView.kfragTargetVector:
					// Check for an empty vector.
					if (hvo == 0 || m_cache.DomainDataByFlid.get_VecSize(hvo, m_flid) == 0)
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
						if (hvo != 0)
							vwenv.NoteDependency(new[] {hvo}, new[] {m_flid}, 1);
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
						m_cache.WritingSystemFactory;

					vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						(int)FwTextPropVar.ktpvDefault,
						(int)TptEditable.ktptNotEditable);
					ITsString tss;
					ITsStrFactory tsf = m_cache.TsStrFactory;
					Debug.Assert(hvo != 0);
#if USEBESTWS
					if (m_displayWs != null && m_displayWs.StartsWith("best"))
					{
						// The flid can be a variety of types, so deal with those.
						Debug.WriteLine("Using 'best ws': " + m_displayWs);
						int magicWsId = LgWritingSystem.GetMagicWsIdFromName(m_displayWs);
						int actualWS = m_cache.LanguageProject.ActualWs(magicWsId, hvo, m_flid);
						Debug.WriteLine("Actual ws: " + actualWS.ToString());
					}
					else
					{
#endif
					// Use reflection to get a prebuilt name if we can.  Otherwise
						// settle for piecing together a string.
						Debug.Assert(m_cache != null);
						var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
						Debug.Assert(obj != null);
						Type type = obj.GetType();
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
							if (!string.IsNullOrEmpty(m_displayNameProperty))
							{
								pi = type.GetProperty(m_displayNameProperty,
									System.Reflection.BindingFlags.Instance |
									System.Reflection.BindingFlags.Public |
									System.Reflection.BindingFlags.FlattenHierarchy);
							}
							int ws = wsf.GetWsFromStr(obj.SortKeyWs);
							if (ws == 0)
								ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
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
								// ShortNameTss sometimes gets PropChanged, so worth letting the view know that's
								// what we're inserting.
								var flid = Cache.MetaDataCacheAccessor.GetFieldId2(obj.ClassID, "ShortNameTSS", true);
								vwenv.AddStringProp(flid, this);
								break;
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
