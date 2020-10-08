// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using System.Xml.Linq;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.Cellar;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Infrastructure;
using SIL.Xml;

namespace LanguageExplorer.Controls.DetailControls.Slices
{
	internal class GhostStringSliceView : RootSiteControl
	{
		internal const int khvoFake = -2002;
		private int m_hvoObj;
		private int m_flidEmptyProp; // the object property whose emptiness causes the ghost to appear.
		private int m_clidDst; // of the class of object we will create (int m_flidEmptyProp) if something is typed in the ghost slice
		private int m_flidStringProp; // the string property of m_clidDst we are simulating.
		private XElement m_nodeObjProp; // obj or seq node that requested the ghost.
		private GhostStringSliceVc m_vc;
		private GhostDaDecorator m_sda;
		private int m_wsToCreate; // default analysis or vernacular ws.

		internal GhostStringSliceView(int hvo, int flid, XElement nodeObjProp, LcmCache cache)
		{
			Cache = cache;
			m_hvoObj = hvo;
			m_flidEmptyProp = flid;
			m_nodeObjProp = nodeObjProp;
			// Figure the type of object we are pretending to be.
			var dstClass = XmlUtils.GetOptionalAttributeValue(nodeObjProp, "ghostClass");
			m_clidDst = dstClass == null ? cache.DomainDataByFlid.MetaDataCache.GetDstClsId(flid) : cache.DomainDataByFlid.MetaDataCache.GetClassId(dstClass);
			// And the one property of that imaginary object we are displaying.
			var stringProp = XmlUtils.GetMandatoryAttributeValue(nodeObjProp, "ghost");
			// Special case for making a Text
			m_flidStringProp = m_flidEmptyProp == RnGenericRecTags.kflidText ? StTxtParaTags.kflidContents : cache.DomainDataByFlid.MetaDataCache.GetFieldId2(m_clidDst, stringProp, true);
			// And what writing system should typing in the field employ?
			var wsContainer = cache.ServiceLocator.WritingSystems;
			var stringWs = XmlUtils.GetMandatoryAttributeValue(nodeObjProp, "ghostWs");
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
					m_wsToCreate = wsContainer.DefaultPronunciationWritingSystem?.Handle ?? wsContainer.DefaultVernacularWritingSystem.Handle;
					break;
				default:
					throw new Exception("ghostWs must be vernacular or analysis or pronunciation");
			}
		}

		#region IDisposable override

		/// <inheritdoc />
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			if (disposing)
			{
				PropertyTable?.GetValue<IFwMainWnd>(FwUtilsConstants.window)?.IdleQueue?.Remove(SwitchToRealOnIdle);
			}

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
			return false;
		}

		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}
			base.MakeRoot();
			m_sda = new GhostDaDecorator(m_cache.DomainDataByFlid as ISilDataAccessManaged, TsStringUtils.EmptyString(m_wsToCreate), m_clidDst);
			RootBox.DataAccess = m_sda;
			m_vc = new GhostStringSliceVc();
			// arg1 is a meaningless root HVO, since this VC only displays one dummy property and gets it from the ghostDA,
			// which ignores the HVO.
			// arg3 is a meaningless initial fragment, since this VC only displays one thing.
			RootBox.SetRootObject(khvoFake, m_vc, 1, m_styleSheet);
		}

		/// <summary>
		/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
		/// (See LT-8656 and LT-9119.)
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			try
			{
				var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvoObj);
				if (obj.IsValidObject)
				{
					base.OnKeyPress(e);
				}
				else
				{
					e.Handled = true;
				}
			}
			catch
			{
				e.Handled = true;
			}
		}

		protected override void HandleSelectionChange(IVwRootBox prootb, IVwSelection vwselNew)
		{
			vwselNew.TextSelInfo(false, out var tssTyped, out _, out _, out _, out _, out _);
			base.HandleSelectionChange(prootb, vwselNew);
			if (tssTyped.Length != 0)
			{
				PropertyTable.GetValue<IFwMainWnd>(FwUtilsConstants.window).IdleQueue.Add(IdleQueuePriority.High, SwitchToRealOnIdle);
			}
		}

		private int MakeRealObject(ITsString tssTyped)
		{
			// Figure whether owning atomic or owning collection or owning sequence. Throw if none.
			// Unless we're making an unowned IText for a Notebook Record.
			var sdaReal = m_cache.DomainDataByFlid;
			var mdc = sdaReal.MetaDataCache;
			var typeOwning = (CellarPropertyType)(mdc.GetFieldType(m_flidEmptyProp) & (int)CellarPropertyTypeFilter.VirtualMask);
			// Make a new object of the specified class in the specified property.
			var ord = 0;
			switch (typeOwning)
			{
				default:
					if (m_flidEmptyProp != RnGenericRecTags.kflidText)
					{
						throw new Exception("ghost string property must be owning object property");
					}
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
			var sClass = StringTable.Table.GetString(mdc.GetClassName(m_clidDst), StringTable.ClassNames);
			var hvoNewObj = 0;
			int hvoStringObj;
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(String.Format(DetailControlsStrings.ksUndoCreate0, sClass), String.Format(DetailControlsStrings.ksRedoCreate0, sClass), m_cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
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
				{
					hvoNewObj = hvoStringObj = sdaReal.MakeNewObject(m_clidDst, m_hvoObj, m_flidEmptyProp, ord);
				}
				// Set its property m_flidStringProp to tssTyped. If it is multilingual, choose based on ghostWs.
				var typeString = (CellarPropertyType)(mdc.GetFieldType(m_flidStringProp) & (int)CellarPropertyTypeFilter.VirtualMask);
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
				var ghostInitMethod = XmlUtils.GetOptionalAttributeValue(m_nodeObjProp, "ghostInitMethod");
				if (ghostInitMethod != null)
				{
					var obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoNewObj);
					var objType = obj.GetType();
					var mi = objType.GetMethod(ghostInitMethod);
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
		private static void RestoreSelection(int ich, DataTree datatree, object[] key, int hvoNewObj, int flidStringProp, int ws)
		{
			// To be written.
			foreach (var slice in datatree.Slices)
			{
				if (slice.Key.Length < key.Length + 2 || !LanguageExplorerServices.StartsWith(slice.Key, key))
				{
					continue;
				}
				var nextKeyItem = slice.Key[key.Length]; // should be hvoNewObj
				if (!(nextKeyItem is int) || (int)nextKeyItem != hvoNewObj)
				{
					continue;
				}
				var lastKeyNode = slice.Key[slice.Key.Length - 1] as XElement;
				if (lastKeyNode == null || lastKeyNode.Name != "slice")
				{
					continue;
				}
				if (slice is StringSlice)
				{
					var ss = (StringSlice)slice;
					if (ss.FieldId != flidStringProp)
					{
						continue;
					}
					if (ss.WritingSystemId != ws)
					{
						continue;
					}
					// For SelectAt to work, the rootbox must be constructed and visible.
					GetSliceReadyToFocus(ss);
					ss.SelectAt(ich);
					ss.Control.Focus();
					break;
				}
				if (slice is MultiStringSlice)
				{
					var mss = (MultiStringSlice)slice;
					if (mss.FieldId != flidStringProp)
					{
						continue;
					}
					// Enhance JohnT: add functions to MultiStringSlice and LabeledMultiStringControl
					// so we can check that it's displaying the right writing systems.
					// For SelectAt to work, the rootbox must be constructed and visible.
					GetSliceReadyToFocus(mss);
					mss.SelectAt(ws, ich);
					mss.Control.Focus();
					break;
				}
				if (slice is StTextSlice stslice)
				{
					GetSliceReadyToFocus(stslice);
					stslice.SelectAt(ich);
					stslice.Control.Focus();
					break;
				}
			}
		}

		private static void GetSliceReadyToFocus(Slice slice)
		{
			if (!slice.IsRealSlice)
			{
				slice.BecomeRealInPlace();
			}
			slice.Visible = true;
			slice.Control.Visible = true;
			slice.ContainingDataTree.Update();
			slice.ContainingDataTree.CurrentSlice = slice;
		}

		/// <summary>
		/// We arrange to be called once when this slice should turn into a real object.
		/// </summary>
		private bool SwitchToRealOnIdle(object parameter)
		{
			if (IsDisposed)
			{
				throw new InvalidOperationException("Thou shalt not call methods after I am disposed!");
			}
			if (Parent == null)
			{
				return false;       // wait until we're fully set up.
			}
			// Converting to real while doing a composition messes up the IME. (LT-9932).
			if (RootBox.IsCompositionInProgress)
			{
				return false;
			}
			SwitchToReal();
			return true;
		}

		private void SwitchToReal()
		{
			var slice = Parent.Parent as GhostStringSlice ?? Parent.Parent.Parent as GhostStringSlice; // Will also be disposed.
			// Save info we will need after MakeRealObject destroys this.
			var parentKey = slice.Key;
			var flidStringProp = m_flidStringProp;
			var wsToCreate = m_wsToCreate;
			var datatree = slice.ContainingDataTree;
			RootBox.Selection.TextSelInfo(false, out var tssTyped, out var ich, out _, out _, out _, out _);
			// Make the real object and set the string property we are ghosting. The final PropChanged
			// will typically dispose this and create a new string slice whose key is our own key
			// followed by the flid of the string property.
			var hvoNewObj = MakeRealObject(tssTyped);
			// Now try to make a suitable selection in the slice that replaces this.
			RestoreSelection(ich, datatree, parentKey, hvoNewObj, flidStringProp, wsToCreate);
		}

		/// <summary>
		/// The ghost slice displays just one string; this decorator stores and returns it independent of
		/// the flid.
		/// </summary>
		private sealed class GhostDaDecorator : DomainDataByFlidDecoratorBase
		{
			private ITsString _tss;
			private readonly int _clidDst;

			internal GhostDaDecorator(ISilDataAccessManaged domainDataByFlid, ITsString tss, int clidDst)
				: base(domainDataByFlid)
			{
				_tss = tss;
				_clidDst = clidDst;
				SetOverrideMdc(new GhostDataCacheDecorator((IFwMetaDataCacheManaged)MetaDataCache));
			}

			public override ITsString get_StringProp(int hvo, int tag)
			{
				Debug.Assert(hvo == khvoFake);
				Debug.Assert(tag == GhostStringSliceVc.kflidFake);
				return _tss;
			}

			public override void SetString(int hvo, int tag, ITsString tss)
			{
				Debug.Assert(hvo == khvoFake);
				Debug.Assert(tag == GhostStringSliceVc.kflidFake);
				_tss = tss;
			}

			// Pretend it is of our expected destination class. Very few things should care about this,
			// but it allows IsValidObject to return true for it, which is important when reconstructing
			// the root box, as happens during Refresh.
			public override int get_IntProp(int hvo, int tag)
			{
				return tag == (int)CmObjectFields.kflidCmObject_Class ? _clidDst : base.get_IntProp(hvo, tag);
			}

			/// <summary>
			/// The ghost slice displays just one virtual string; this decorator handles the fake flid.
			/// </summary>
			private sealed class GhostDataCacheDecorator : LcmMetaDataCacheDecoratorBase
			{
				internal GhostDataCacheDecorator(IFwMetaDataCacheManaged mdc)
					: base(mdc)
				{
				}

				public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
				{
					throw new NotSupportedException();
				}

				/// <summary>
				/// The virtual field store a TsString, so the fake flid returns a type of String.
				/// </summary>
				public override int GetFieldType(int luFlid)
				{
					return luFlid == GhostStringSliceVc.kflidFake ? (int)CellarPropertyType.String : base.GetFieldType(luFlid);
				}
			}
		}
	}
}