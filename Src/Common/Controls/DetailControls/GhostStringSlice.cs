using System;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

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
		internal const int kflidFake = -2001;
		internal const int khvoFake = -2002;
		/// <summary>
		/// Create a ghost string slice that pretends to be property flid of the missing object
		/// </summary>
		/// <param name="obj">The obj.</param>
		/// <param name="flid">the empty object flid, which this slice is displaying.</param>
		/// <param name="nodeObjProp">the 'obj' or 'seq' element that requested the ghost</param>
		/// <param name="cache">The cache.</param>
		public GhostStringSlice(ICmObject obj, int flid, XmlNode nodeObjProp, FdoCache cache)
			: base(new GhostStringSliceView(obj.Hvo, flid, nodeObjProp, cache), obj, flid)
		{
			AccessibleName = "GhostStringSlice";
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

		public class GhostStringSliceVc: FwBaseVc
		{
			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				// The property is absolutely arbitrary because the ghost DA ignores it.
				vwenv.AddStringProp(GhostStringSlice.kflidFake, this);
			}
		}

		#endregion // View Constructors

		#region RootSite implementation

		class GhostStringSliceView : RootSiteControl
		{
			int m_hvoObj;
			int m_flidEmptyProp; // the object property whose emptiness causes the ghost to appear.
			int m_clidDst; // of the class of object we will create (int m_flidEmptyProp) if something is typed in the ghost slice
			int m_flidStringProp; // the string property of m_clidDst we are simulating.
			XmlNode m_nodeObjProp; // obj or seq node that requested the ghost.
			GhostStringSliceVc m_vc;
			GhostDaDecorator m_sda;
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
					m_clidDst = cache.DomainDataByFlid.MetaDataCache.GetDstClsId((int)flid);
				else
					m_clidDst = cache.DomainDataByFlid.MetaDataCache.GetClassId(dstClass);

				// And the one property of that imaginary obejct we are displaying.
				string stringProp = XmlUtils.GetManditoryAttributeValue(nodeObjProp, "ghost");
				// Special case for making a Text
				if (m_flidEmptyProp == RnGenericRecTags.kflidText)
				{
					m_flidStringProp = StTxtParaTags.kflidContents;
				}
				else
				{
					m_flidStringProp = cache.DomainDataByFlid.MetaDataCache.GetFieldId2(m_clidDst, stringProp, true);
				}

				// And what writing system should typing in the field employ?
				IWritingSystemContainer wsContainer = cache.ServiceLocator.WritingSystems;
				string stringWs = XmlUtils.GetManditoryAttributeValue(nodeObjProp, "ghostWs");
				switch (stringWs)
				{
					case "vernacular":
						m_wsToCreate = wsContainer.DefaultVernacularWritingSystem.Handle;
						break;

					case "analysis":
						m_wsToCreate = wsContainer.DefaultAnalysisWritingSystem.Handle;
						break;

					case "pronunciation":
						// Pronunciation isn't always defined.  Fall back to vernacular.
						if (wsContainer.DefaultPronunciationWritingSystem == null)
							m_wsToCreate = wsContainer.DefaultVernacularWritingSystem.Handle;
						else
							m_wsToCreate = wsContainer.DefaultPronunciationWritingSystem.Handle;
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
			public override bool RefreshDisplay()
			{
				CheckDisposed();
				return false;
			}

			public override void MakeRoot()
			{
				CheckDisposed();

				base.MakeRoot();

				if (DesignMode)
					return;

				// Review JohnT: why doesn't the base class do this??
				m_rootb = VwRootBoxClass.Create();
				m_rootb.SetSite(this);



				m_sda = new GhostDaDecorator(m_fdoCache.DomainDataByFlid as ISilDataAccessManaged, m_fdoCache.TsStrFactory.EmptyString(m_wsToCreate), (int)m_clidDst);

				m_rootb.DataAccess = m_sda;

				m_vc = new GhostStringSliceVc();

				// arg1 is a meaningless root HVO, since this VC only displays one dummy property and gets it from the ghostDA,
				// which ignores the HVO.
				// arg3 is a meaningless initial fragment, since this VC only displays one thing.
				m_rootb.SetRootObject(GhostStringSlice.khvoFake, m_vc, 1, m_styleSheet);
			}
		#endregion // RootSite implementation

			/// <summary>
			/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
			/// (See LT-8656 and LT-9119.)
			/// </summary>
			protected override void OnKeyPress(KeyPressEventArgs e)
			{
				try
				{
					var obj = m_fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoObj);
					if (obj.IsValidObject)
						base.OnKeyPress(e);
					else
						e.Handled = true;
				}
				catch
				{
					e.Handled = true;
				}
			}

			protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
			{
				CheckDisposed();
				ITsString tssTyped;
				int ich, hvo, tag, ws;
				bool fAssocPrev;
				vwselNew.TextSelInfo(false, out tssTyped, out ich, out fAssocPrev, out hvo, out tag, out ws);
				base.HandleSelectionChange(prootb, vwselNew);
				if (tssTyped.Length != 0)
				{
					// user typed something in the dummy slice! Make a real object.
					// Save information required for RestoreSelection. MakeRealObject may well dispose this.
					m_mediator.IdleQueue.Add(IdleQueuePriority.High, SwitchToRealOnIdle);
				}
			}

			int MakeRealObject(ITsString tssTyped)
			{
				// Figure whether owning atomic or owning collection or owning sequence. Throw if none.
				// Unless we're making an unowned IText for a Notebook Record.
				ISilDataAccess sdaReal = m_fdoCache.DomainDataByFlid;
				IFwMetaDataCache mdc = sdaReal.MetaDataCache;
				CellarPropertyType typeOwning =
					(CellarPropertyType)(mdc.GetFieldType(m_flidEmptyProp) & (int)CellarPropertyTypeFilter.VirtualMask);
				// Make a new object of the specified class in the specified property.
				int ord = 0;
				switch (typeOwning)
				{
					default:
						if (m_flidEmptyProp != RnGenericRecTags.kflidText)
							throw new Exception("ghost string property must be owning object property");
						break;
					case CellarPropertyType.OwningAtomic:
						ord = -2;
						break;
					case CellarPropertyType.OwningCollection:
						ord = -1;
						break;
					case CellarPropertyType.OwningSequence:
						// ord = 0 set above (inserting the first and only object at position 0).
						break;
				}
				string sClassRaw = mdc.GetClassName(m_clidDst);
				string sClass = StringTable.Table.GetString(sClassRaw, "ClassNames");
				string sUndo = String.Format(DetailControlsStrings.ksUndoCreate0, sClass);
				string sRedo = String.Format(DetailControlsStrings.ksRedoCreate0, sClass);
				int hvoNewObj = 0;
				int hvoStringObj = 0;
				UndoableUnitOfWorkHelper.Do(sUndo, sRedo, m_fdoCache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					// Special case: if we just created a Text in RnGenericRecord, and we want to show the contents
					// of an StTxtPara, make the intermediate objects
					if (m_flidEmptyProp == RnGenericRecTags.kflidText)
					{
						var servLoc = Cache.ServiceLocator;
						var text = servLoc.GetInstance<ITextFactory>().Create();
						var stText = servLoc.GetInstance<IStTextFactory>().Create();
						text.ContentsOA = stText;
						var para = servLoc.GetInstance<IStTxtParaFactory>().Create();
						stText.ParagraphsOS.Add(para);
						hvoNewObj = text.Hvo;
						hvoStringObj = para.Hvo;
						// Set the RnGenericRec's Text property to reference the new text
						sdaReal.SetObjProp(m_hvoObj, m_flidEmptyProp, hvoNewObj);
					}
					else
						hvoNewObj = hvoStringObj = sdaReal.MakeNewObject(m_clidDst, m_hvoObj, m_flidEmptyProp, ord);

					// Set its property m_flidStringProp to tssTyped. If it is multilingual, choose based on ghostWs.
					var typeString = (CellarPropertyType)(mdc.GetFieldType(m_flidStringProp) &
						(int)CellarPropertyTypeFilter.VirtualMask);
					switch (typeString)
					{
						default:
							throw new Exception("ghost property must store strings!");
						case CellarPropertyType.MultiString:
						case CellarPropertyType.MultiUnicode:
							sdaReal.SetMultiStringAlt(hvoStringObj, m_flidStringProp, m_wsToCreate, tssTyped);
							break;
						case CellarPropertyType.String:
							sdaReal.SetString(hvoStringObj, m_flidStringProp, tssTyped);
							break;
					}

					string ghostInitMethod = XmlUtils.GetOptionalAttributeValue(m_nodeObjProp, "ghostInitMethod");
					if (ghostInitMethod != null)
					{
						var obj = m_fdoCache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoNewObj);
						Type objType = obj.GetType();
						System.Reflection.MethodInfo mi = objType.GetMethod(ghostInitMethod);
						mi.Invoke(obj, null);
					}
				});
				return hvoNewObj;
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
				foreach (Slice slice in datatree.Slices)
				{
					if (slice.Key.Length < key.Length + 2)
						continue;
					if (!StartsWith(slice.Key, key))
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
					else if (slice is StTextSlice)
					{
						var stslice = (StTextSlice) slice;
						GetSliceReadyToFocus(stslice);
						stslice.SelectAt(ich);
						slice.Control.Focus();
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
			/// <param name="parameter"></param>
			private bool SwitchToRealOnIdle(object parameter)
			{
				if (IsDisposed)
					return true;

				if (this.Parent == null)
					return false;		// wait until we're fully set up.
				// Converting to real while doing a composition messes up the IME. (LT-9932).
				if (m_rootb.IsCompositionInProgress)
					return false;
				SwitchToReal();
				return true;
			}

			[SuppressMessage("Gendarme.Rules.Correctness", "EnsureLocalDisposalRule",
				Justification = "datatree is a reference")]
			private void SwitchToReal()
			{
				// Depending on compile switch for SLICE_IS_SPLITCONTAINER,
				// grandParent will be both a Slice and a SplitContainer
				// (Slice is a subclass of SplitContainer),
				// or just a SplitContainer (SplitContainer is the only child Control of a Slice).
				// If grandParent is not a Slice, then we have to move up to the great-grandparent
				// to find the Slice.
				var slice = Parent.Parent as GhostStringSlice; // Will also be disposed.
				if (slice == null)
					slice = Parent.Parent.Parent as GhostStringSlice; // Will also be disposed.

				// Save info we will need after MakeRealObject destroys this.
				object[] parentKey = slice.Key;
				int flidEmptyProp = m_flidEmptyProp;
				int flidStringProp = m_flidStringProp;
				int wsToCreate = m_wsToCreate;
				var datatree = slice.ContainingDataTree;
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

		/// <summary>
		/// The ghost slice displays just one string; this decorator stores and returns it independent of
		/// the flid.
		/// </summary>
		class GhostDaDecorator : DomainDataByFlidDecoratorBase
		{
			private ITsString m_tss;
			private int m_clidDst;

			public GhostDaDecorator(ISilDataAccessManaged domainDataByFlid, ITsString tss, int clidDst)
				: base(domainDataByFlid)
			{
				m_tss = tss;
				m_clidDst = clidDst;
				SetOverrideMdc(new GhostMdc(MetaDataCache as IFwMetaDataCacheManaged));
			}

			public override ITsString get_StringProp(int hvo, int tag)
			{
				Debug.Assert(hvo == GhostStringSlice.khvoFake);
				Debug.Assert(tag == GhostStringSlice.kflidFake);
				return m_tss;
			}

			public override void SetString(int hvo, int tag, ITsString tss)
			{
				Debug.Assert(hvo == GhostStringSlice.khvoFake);
				Debug.Assert(tag == GhostStringSlice.kflidFake);
				m_tss = tss;
			}

			// Pretend it is of our expected destination class. Very few things should care about this,
			// but it allows IsValidObject to return true for it, which is important when reconstructing
			// the root box, as happens during Refresh.
			public override int get_IntProp(int hvo, int tag)
			{
				if (tag == (int)CmObjectFields.kflidCmObject_Class)
					return m_clidDst;
				return base.get_IntProp(hvo, tag);
			}
		}

		/// <summary>
		/// The ghost slice displays just one virtual string; this decorator handles the fake flid.
		/// </summary>
		class GhostMdc : FdoMetaDataCacheDecoratorBase
		{
			public GhostMdc(IFwMetaDataCacheManaged mdc)
				: base(mdc)
			{
			}

			public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
			{
				throw new NotImplementedException();
			}

			/// <summary>
			/// The virtual field store a TsString, so the fake flid returns a type of String.
			/// </summary>
			public override int GetFieldType(int luFlid)
			{
				return luFlid == GhostStringSlice.kflidFake ?
					(int)CellarPropertyType.String : base.GetFieldType(luFlid);
			}
		}
	}
}
