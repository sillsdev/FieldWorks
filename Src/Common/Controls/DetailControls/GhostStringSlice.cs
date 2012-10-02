using System;
using System.Windows.Forms;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Xml;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// A ghost string slice displays what purports to be a string (or multistring) property of a missing object.
	/// The canonical example is if a LexSense does not have an example sentence. The ghost slice apparently shows
	/// the missing Example property of the nonexistent ExampleSentence. If the user types something, a real object
	/// is created.
	/// A ghost slice is created when a part displays an object or object sequence that is empty, if the 'obj' or 'seq'
	/// element has an attribute ghost="fieldname" and ghostWs="vernacular/analysis".
	/// Optionally it may also have ghostClass="className". If this is absent it will create an instance of the
	/// base signature class, which had better not be abstract.
	/// </summary>
	public class GhostStringSlice : ViewPropertySlice
	{
		/// <summary>
		/// Create a ghost string slice that pretends to be property flid of the missing object
		/// </summary>
		/// <param name="hvoObj"></param>
		/// <param name="flid">the empty object flid, which this slice is displaying.</param>
		/// <param name="nodeObjProp">the 'obj' or 'seq' element that requested the ghost</param>
		public GhostStringSlice(int hvoObj, int flid, XmlNode nodeObjProp, FdoCache cache)
			: base(new GhostStringSliceView(hvoObj, flid, nodeObjProp, cache), hvoObj, flid)
		{
		}

		public override bool IsGhostSlice
		{
			get
			{
				CheckDisposed();

				return true;
			}
		}

		#region View Constructor

		public class GhostStringSliceVc: VwBaseVc
		{
			int m_flidGhost;

			public GhostStringSliceVc(int flidGhost)
			{
				m_flidGhost = flidGhost;
			}
			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();

				vwenv.AddStringProp(m_flidGhost, this);
			}
		}

		#endregion // View Constructors

		#region RootSite implementation

		class GhostStringSliceView : RootSiteControl
		{
			int m_hvoObj;
			int m_flidEmptyProp; // the object property whose emptiness causes the ghost to appear.
			uint m_clidDst; // of the class of object we will create (int m_flidEmptyProp) if something is typed in the ghost slice
			int m_flidStringProp; // the string property of m_clidDst we are simulating.
			XmlNode m_nodeObjProp; // obj or seq node that requested the ghost.
			GhostStringSliceVc m_vc;
			ISilDataAccess m_sda;
			int m_hvoRoot;  //roob object for slice.
			int m_flidGhost; // dummy virtual flid.
			int m_wsToCreate; // default analysis or vernacular ws.

			public GhostStringSliceView(int hvo, int flid, XmlNode nodeObjProp, FdoCache cache)
			{
				Cache = cache;
				m_hvoObj = hvo;
				m_flidEmptyProp = flid;
				m_nodeObjProp = nodeObjProp;

				// Figure the type of object we are pretending to be.
				string dstClass = XmlUtils.GetOptionalAttributeValue(nodeObjProp, "ghostClass");
				if (dstClass == null)
					m_clidDst = cache.MetaDataCacheAccessor.GetDstClsId((uint)flid);
				else
					m_clidDst = cache.MetaDataCacheAccessor.GetClassId(dstClass);

				// And the one property of that imaginary obejct we are displaying.
				string stringProp = XmlUtils.GetManditoryAttributeValue(nodeObjProp, "ghost");
				m_flidStringProp = (int)cache.MetaDataCacheAccessor.GetFieldId2(m_clidDst, stringProp, true);

				// And what writing system should typing in the field employ?
				string stringWs = XmlUtils.GetManditoryAttributeValue(nodeObjProp, "ghostWs");
				switch (stringWs)
				{
					case "vernacular":
						m_wsToCreate = cache.DefaultVernWs;
						break;

					case "analysis":
						m_wsToCreate = cache.DefaultAnalWs;
						break;

					case "pronunciation":
						m_wsToCreate = cache.LangProject.DefaultPronunciationWritingSystem;
						break;

					default:
						throw new Exception("ghostWs must be vernacular or analysis or pronunciation");
				}
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

				base.Dispose(disposing);

				if (disposing)
				{
					// Dispose managed resources here.
					if (m_vc != null)
						m_vc.Dispose();
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_vc = null;
			}

			#endregion IDisposable override

			/// <summary>
			/// Do nothing if the ghost somehow gets refreshed directly.
			/// It will get called when the main window is refreshing, and we reuse the ghost.
			/// Just ignore the call, since the base call will cause
			/// a crash on the cache with a zero HVO for some reason.
			/// </summary>
			public override void RefreshDisplay()
			{
				CheckDisposed();
			}

			public override void MakeRoot()
			{
				CheckDisposed();

				base.MakeRoot();

				if (DesignMode)
					return;

				m_fdoCache.CreateDummyID(out m_hvoRoot);
				// Pretend it is of our expected destination class. Very few things should care about this,
				// but it allows IsValidObject to return true for it, which is important when reconstructing
				// the root box, as happens during Refresh.
				m_fdoCache.VwCacheDaAccessor.CacheIntProp(m_hvoRoot,
					(int)CmObjectFields.kflidCmObject_Class, (int)m_clidDst);

				// Review JohnT: why doesn't the base class do this??
				m_rootb = VwRootBoxClass.Create();
				m_rootb.SetSite(this);

				m_sda = m_fdoCache.MainCacheAccessor;
				IVwCacheDa cda = m_sda as IVwCacheDa;

				IVwVirtualHandler vh = cda.GetVirtualHandlerName(GhostStringVirtualHandler.GhostClassName,
					GhostStringVirtualHandler.FieldNameForWs(m_wsToCreate));
				if (vh == null)
				{
					vh = new GhostStringVirtualHandler(m_wsToCreate);
					cda.InstallVirtual(vh);
				}
				m_flidGhost = vh.Tag;
				m_rootb.DataAccess = m_sda;

				m_vc = new GhostStringSliceVc(m_flidGhost);

				// arg3 is a meaningless initial fragment, since this VC only displays one thing.
				m_rootb.SetRootObject(m_hvoRoot, m_vc, 1, m_styleSheet);
			}
		#endregion // RootSite implementation

			/// <summary>
			/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
			/// (See LT-8656 and LT-9119.)
			/// </summary>
			protected override void OnKeyPress(KeyPressEventArgs e)
			{
				if (m_fdoCache.VerifyValidObject(m_hvoObj))
					base.OnKeyPress(e);
				else
					e.Handled = true;
			}

			public override void SelectionChanged(IVwRootBox prootb, IVwSelection vwselNew)
			{
				CheckDisposed();
				ITsString tssTyped;
				int ich, hvo, tag, ws;
				bool fAssocPrev;
				vwselNew.TextSelInfo(false, out tssTyped, out ich, out fAssocPrev, out hvo, out tag, out ws);
				base.SelectionChanged(prootb, vwselNew);
				if (tssTyped.Length != 0)
				{
					// user typed something in the dummy slice! Make a real object.
					// Save information required for RestoreSelection. MakeRealObject may well dispose this.
					System.Windows.Forms.Application.Idle += new EventHandler(Application_Idle);
				}
			}

			int MakeRealObject(ITsString tssTyped)
			{
				// Figure whether owning atomic or owning collection or owning sequence. Throw if none.
				IFwMetaDataCache mdc = m_fdoCache.MetaDataCacheAccessor;
				FieldType iType = m_fdoCache.GetFieldType(m_flidEmptyProp);
				iType &= FieldType.kcptVirtualMask;
				ISilDataAccess sdaReal = m_fdoCache.MainCacheAccessor;
				// Make a new object of the specified class in the specified property.
				int ord = 0;
				switch (iType)
				{
					default:
						throw new Exception("ghost string property must be owning object property");
					case FieldType.kcptOwningAtom:
						ord = -2;
						break;
					case FieldType.kcptOwningCollection:
						ord = -1;
						break;
					case FieldType.kcptOwningSequence:
						// ord = 0 set above (inserting the first and only object at position 0).
						break;
				}
				string sClassRaw = mdc.GetClassName((uint)m_clidDst);
				string sClass = m_mediator.StringTbl.GetString(sClassRaw, "ClassNames");
				string sUndo = String.Format(DetailControlsStrings.ksUndoCreate0, sClass);
				string sRedo = String.Format(DetailControlsStrings.ksRedoCreate0, sClass);
				sdaReal.BeginUndoTask(sUndo, sRedo);
				int hvoNewObj = sdaReal.MakeNewObject((int)m_clidDst, m_hvoObj, m_flidEmptyProp, ord);
				// Set its property m_flidStringProp to tssTyped. If it is multilingual, choose based on ghostWs.
				FieldType iTypeString = m_fdoCache.GetFieldType(m_flidStringProp);
				iTypeString &= FieldType.kcptVirtualMask;
				switch (iTypeString)
				{
					default:
						throw new Exception("ghost property must store strings!");
					case FieldType.kcptMultiString:
					case FieldType.kcptMultiBigString:
					case FieldType.kcptMultiUnicode:
					case FieldType.kcptMultiBigUnicode:
						sdaReal.SetMultiStringAlt(hvoNewObj, m_flidStringProp, m_wsToCreate, tssTyped);
						break;
					case FieldType.kcptString:
					case FieldType.kcptBigString:
						sdaReal.SetString(hvoNewObj, m_flidStringProp, tssTyped);
						break;
				}

				string ghostInitMethod = XmlUtils.GetOptionalAttributeValue(m_nodeObjProp, "ghostInitMethod");
				if (ghostInitMethod != null)
				{
					ICmObject obj = CmObject.CreateFromDBObject(m_fdoCache, hvoNewObj);
					Type objType = obj.GetType();
					System.Reflection.MethodInfo mi = objType.GetMethod(ghostInitMethod);
					mi.Invoke(obj, null);
				}
				// Issue PropChanged for the addition of the new object. (could destroy this).
				sdaReal.PropChanged(null, (int)PropChangeType.kpctNotifyAll, m_hvoObj, m_flidEmptyProp, 0, 1, 0);
				sdaReal.EndUndoTask();
				return hvoNewObj;
			}

			/// <summary>
			/// Return true if the target array starts with the objects in the match array.
			/// </summary>
			/// <param name="target"></param>
			/// <param name="match"></param>
			/// <returns></returns>
			static bool StartsWith(object[] target, object[] match)
			{
				if (match.Length > target.Length)
					return false;
				for (int i = 0; i < match.Length; i++)
				{
					object x = target[i];
					object y = match[i];
					// We need this special expression because two objects wrapping the same integer
					// are, pathologically, not equal to each other.
					if (x != y && !(x is int && y is int && ((int)x) == ((int)y)))
						return false;
				}
				return true;
			}

			/// <summary>
			/// This needs to be a static method because typically the ghost slice has been disposed
			/// by the time it is called.
			///
			/// Note that DataTree.AddAtomicNode either displays a layout of the object, or
			/// displays the ghost slice. If it displays a layout of the object, it adds
			/// to its input path first itself (the "obj" element), then the HVO of the object,
			/// then creates slices for the object. For ghost slice, it simply adds itself.
			/// Therefore, a slice created as part of a layout replacing a ghost slice will
			/// have a key matching the ghost slice's key, and followed by the ID of the new object.
			/// Next, anything in this layout will have the template used to display the object,
			/// and the particular part ref that invoked the part, then (for an interesting target)
			/// a slice node with editor 'string' or 'multistring' as appropriate.
			/// AddSeqNode is similar, except that it may display layouts of multiple objects.
			/// </summary>
			static void RestoreSelection(int ich, DataTree datatree, object[] key,
				int hvoNewObj, int flidObjProp, int flidStringProp, int ws)
			{
				// To be written.
				foreach (Slice slice in datatree.Controls)
				{
					if (!StartsWith(slice.Key, key))
						continue;
					if (slice.Key.Length < key.Length + 2)
						continue;
					object nextKeyItem = slice.Key[key.Length]; // should be hvoNewObj
					if (!(nextKeyItem is int))
						continue;
					if ((int)nextKeyItem != hvoNewObj)
						continue;
					XmlNode lastKeyNode = slice.Key[slice.Key.Length - 1] as XmlNode;
					if (lastKeyNode == null)
						continue;
					if (lastKeyNode.Name != "slice")
						continue;
					if (slice is StringSlice)
					{
						StringSlice ss = slice as StringSlice;
						if (ss.FieldId != flidStringProp)
							continue;
						if (ss.WritingSystemId != ws)
							continue;
						// For SelectAt to work, the rootbox must be constructed and visible.
						GetSliceReadyToFocus(ss);
						ss.SelectAt(ich);
						ss.Control.Focus();
						break;
					}
					else if (slice is MultiStringSlice)
					{
						MultiStringSlice mss = slice as MultiStringSlice;
						if (mss.FieldId != flidStringProp)
							continue;
						// Enhance JohnT: add functions to MultiStringSlice and LabeledMultiStringControl
						// so we can check that it's displaying the right writing systems.
						// For SelectAt to work, the rootbox must be constructed and visible.
						GetSliceReadyToFocus(mss);
						mss.SelectAt(ws, ich);
						mss.Control.Focus();
						break;
					}
				}
			}

			private static void GetSliceReadyToFocus(Slice slice)
			{
				if (!slice.IsRealSlice)
					slice.BecomeRealInPlace();
				slice.Visible = true;
				slice.Control.Visible = true;
				slice.ContainingDataTree.Update();
				slice.ContainingDataTree.CurrentSlice = slice;
			}

			/// <summary>
			/// We arrange to be called once when this slice should turn into a real object.
			/// </summary>
			/// <param name="sender"></param>
			/// <param name="e"></param>
			private void Application_Idle(object sender, EventArgs e)
			{
				if (this.Parent == null)
					return;		// wait until we're fully set up.
				// Converting to real while doing a composition messes up the IME. (LT-9932).
				if (m_rootb.IsCompositionInProgress)
					return;
				// We only want to get one idle event...after that we will be disposed!
				System.Windows.Forms.Application.Idle -= new EventHandler(Application_Idle);
				SwitchToReal();
			}

			private void SwitchToReal()
			{
				// Depending on compile switch for SLICE_IS_SPLITCONTAINER,
				// grandParent will be both a Slice and a SplitContainer
				// (Slice is a subclass of SplitContainer),
				// or just a SplitContainer (SplitContainer is the only child Control of a Slice).
				// If grandParent is not a Slice, then we have to move up to the great-grandparent
				// to find the Slice.
				GhostStringSlice slice = Parent.Parent as GhostStringSlice; // Will also be disposed.
				if (slice == null)
					slice = Parent.Parent.Parent as GhostStringSlice; // Will also be disposed.

				// Save info we will need after MakeRealObject destroys this.
				object[] parentKey = slice.Key;
				int flidEmptyProp = m_flidEmptyProp;
				int flidStringProp = m_flidStringProp;
				int wsToCreate = m_wsToCreate;
				DataTree datatree = slice.ContainingDataTree;
				ITsString tssTyped;
				int ich, hvo, tag, ws;
				bool fAssocPrev;
				RootBox.Selection.TextSelInfo(false, out tssTyped, out ich, out fAssocPrev, out hvo, out tag, out ws);

				// Make the real object and set the string property we are ghosting. The final PropChanged
				// will typically dispose this and create a new string slice whose key is our own key
				// followed by the flid of the string property.
				int hvoNewObj = MakeRealObject(tssTyped);

				// Now try to make a suitable selection in the slice that replaces this.
				RestoreSelection(ich, datatree, parentKey, hvoNewObj, flidEmptyProp, flidStringProp, wsToCreate);
			}
		}
	}

	class GhostStringVirtualHandler : FDO.BaseVirtualHandler
	{
		public const string GhostClassName = "CmObject";
		public const string GhostFieldName = "GhostField";
		int m_ws;
		/// <summary>
		/// constructor
		/// </summary>
		/// <param name="configuration">the XML that configures this handler</param>
		/// <param name="cache"></param>
		public GhostStringVirtualHandler(int ws)
		{
			m_ws = ws;
			this.ClassName =  GhostClassName;
			this.FieldName = FieldNameForWs(ws);
			this.Writeable = true;
			this.Type = (int)CellarModuleDefns.kcptString;
		}

		public static string FieldNameForWs(int ws)
		{
			return GhostFieldName + ws;
		}

		public override void Load(int hvo, int tag, int ws, IVwCacheDa cda)
		{
			// Value is always an empty string in the specified writing system.
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ITsString tssEmpty = tsf.MakeString("", m_ws);
			if (ws == 0)
				cda.CacheStringProp(hvo, tag, tssEmpty);
			else
				cda.CacheStringAlt(hvo, tag, ws, tssEmpty);
		}

		/// <summary>
		/// This is called by the framework when a writeable virtual property of type kcptString
		/// or kcptMultiString is written. The _unk parameter may be cast to an ITsString and is
		/// the new value.  The ws parameter is meaningful only for multistrings.
		/// The implementation should take whatever steps are needed to store the change.
		/// You can retrieve the old value of the property from the sda.
		/// The framework will automatically update the value in the cache after your method
		/// returns, unless the property is ComputeEveryTime.
		/// </summary>
		/// <param name="hvo"></param>
		/// <param name="tag"></param>
		/// <param name="ws"></param>
		/// <param name="_unk"></param>
		/// <param name="_sda"></param>
		public override void WriteObj(int hvo, int tag, int ws, object _unk,
			ISilDataAccess _sda)
		{
			// Don't need to do anything.
		}
	}
}
