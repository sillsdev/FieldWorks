// Copyright (c) 2004-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LanguageExplorer.Areas;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.Controls;
using LanguageExplorer.Controls.DetailControls;
using LanguageExplorer.Controls.XMLViews;
using LanguageExplorer.LcmUi.Dialogs;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.Reporting;

namespace LanguageExplorer.LcmUi
{
#if RANDYTODO
	// TODO: This class was an IxCoreColleague implementor. It was added to the Mediator only when its HandleRightClick was called.
	// TODO: The HandleRightClick method called window.ShowContextMenu, which eventually got around to this class being temporarily added to the Mediator
	// TODO: in a now obsolete MenuAdapter class that handled menus of various sorts, including those right-click popup context menus.
	// TODO: If I can pull it off, I'd like to get rid of all of those old mediator related methods in this class,
	// TODO: as well as this class' HandleRightClick method.
#endif
	public class CmObjectUi : IFlexComponent, IDisposable
	{
		#region Data members

		protected ICmObject m_cmObject;
		protected int m_hvo;
		protected LcmCache m_cache;
		// Map from uint to uint, specifically, from clsid to clsid.
		// The key is any clsid that we have so far been asked to make a UI object for.
		// The value is the corresponding clsid that actually occurs in the switch.
		// Review JohnH (JohnT): would it be more efficient to store a Class object in the map,
		// and use reflection to make an instance?
		static readonly Dictionary<int, int> m_subclasses = new Dictionary<int, int>();
		protected Control m_hostControl;
		protected IVwViewConstructor m_vc;
		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> _rightClickTuple;

		#endregion Data members

		#region Properties

		/// <summary>
		/// Retrieve the CmObject we are providing UI functions for.
		/// </summary>
		public ICmObject MyCmObject => m_cmObject ?? (m_cmObject = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvo));

		public string ClassName => MyCmObject.ClassName;

		/// <summary>
		/// Returns a View Constructor that can be used to produce various displays of the
		/// object. Various fragments may be supported, depending on the class.
		///
		/// Typical usage:
		/// 		public override void Display(IVwEnv vwenv, int hvo, int frag)
		/// 		{
		/// 		...
		/// 		switch(frag)
		/// 		{
		/// 		...
		/// 		case sometypeshownbyshortname:
		/// 			IVwViewConstructor vcName = CmObjectUi.MakeLcmModelUiObject(m_cache, hvo).Vc;
		/// 			vwenv.AddObj(hvo, vcName, VcFrags.kfragShortName);
		/// 			break;
		/// 		...
		/// 		}
		///
		/// Note that this involves putting an extra level of object structure into the display,
		/// unless it is done in an AddObjVec loop, where AddObj is needed anyway for each object.
		/// This is unavoidable in cases where the property involves polymorphic objects.
		/// If all objects in a sequence are the same type, the appropriate Vc may be retrieved
		/// in the fragment that handles the sequence and passed to AddObjVecItems.
		/// If an atomic property is to be displayed in this way, code like the following may be used:
		///			case something:
		///				...// possibly other properties of containing object.
		///				// Display shortname of object in atomic object property XYZ
		///				int hvoObj = vwenv.DataAccess.get_ObjectProp(hvo, kflidXYZ);
		///				IVwViewConstructor vcName = CmObjectUi.MakeLcmModelUiObject(m_cache, hvoObj).Vc;
		///				vwenv.AddObjProp(kflidXYZ, vcName, VcFrags.kfragShortName);
		///				...
		///				break;
		/// </summary>
		public virtual IVwViewConstructor Vc => m_vc ?? (m_vc = new CmObjectVc(m_cache));

		/// <summary>
		/// Returns a View Constructor that can be used to produce various displays of the
		/// object in the default vernacular writing system.  Various fragments may be
		/// supported, depending on the class.
		///
		/// Typical usage:
		/// 		public override void Display(IVwEnv vwenv, int hvo, int frag)
		/// 		{
		/// 		...
		/// 		switch(frag)
		/// 		{
		/// 		...
		/// 		case sometypeshownbyshortname:
		/// 			IVwViewConstructor vcName = CmObjectUi.MakeLcmModelUiObject(m_cache, hvo).VernVc;
		/// 			vwenv.AddObj(hvo, vcName, VcFrags.kfragShortName);
		/// 			break;
		/// 		...
		/// 		}
		///
		/// Note that this involves putting an extra level of object structure into the display,
		/// unless it is done in an AddObjVec loop, where AddObj is needed anyway for each
		/// object.  This is unavoidable in cases where the property involves polymorphic
		/// objects.  If all objects in a sequence are the same type, the appropriate Vc may be
		/// retrieved in the fragment that handles the sequence and passed to AddObjVecItems.
		/// If an atomic property is to be displayed in this way, code like the following may be
		/// used:
		///			case something:
		///				...// possibly other properties of containing object.
		///				// Display shortname of object in atomic object property XYZ
		///				int hvoObj = vwenv.DataAccess.get_ObjectProp(hvo, kflidXYZ);
		///				IVwViewConstructor vcName = CmObjectUi.MakeLcmModelUiObject(m_cache, hvoObj).VernVc;
		///				vwenv.AddObjProp(kflidXYZ, vcName, VcFrags.kfragShortName);
		///				...
		///				break;
		/// </summary>
		public virtual IVwViewConstructor VernVc => new CmVernObjectVc(m_cache);

		public virtual IVwViewConstructor AnalVc => new CmAnalObjectVc(m_cache);
		#endregion Properties

		#region Construction and initialization

		/// <summary>
		/// If you KNOW for SURE the right subclass of CmObjectUi, you can just make one
		/// directly. Most clients should use MakeLcmModelUiObject.
		/// </summary>
		/// <param name="obj"></param>
		protected CmObjectUi(ICmObject obj)
		{
			m_cmObject = obj;
			m_cache = obj.Cache;
		}

		/// <summary>
		/// This should only be used by MakeLcmModelUiObject.
		/// </summary>
		protected CmObjectUi()
		{
		}

		/// <summary>
		/// This is the main class factory that makes a corresponding CmObjectUi for any given
		/// CmObject.
		/// </summary>
		public static CmObjectUi MakeLcmModelUiObject(ICmObject obj)
		{
			var result = MakeLcmModelUiObject(obj.Cache, obj.Hvo, obj.ClassID);
			result.m_cmObject = obj;
			return result;
		}

		/// <summary>
		/// In many cases we don't really need the LCM object, which can be relatively expensive
		/// to create. This version saves the information, and creates it when needed.
		/// </summary>
		public static CmObjectUi MakeLcmModelUiObject(LcmCache cache, int hvo)
		{
			return MakeLcmModelUiObject(cache, hvo, cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo).ClassID);
		}

		private static CmObjectUi MakeLcmModelUiObject(LcmCache cache, int hvo, int clsid)
		{
			var mdc = cache.DomainDataByFlid.MetaDataCache;
			// If we've encountered an object with this Clsid before, and this clsid isn't in
			// the switch below, the dictionary will give us the appropriate clsid that IS in the
			// map, so the loop below will have only one iteration. Otherwise, we start the
			// search with the clsid of the object itself.
			var realClsid = m_subclasses.ContainsKey(clsid) ? m_subclasses[clsid] : clsid;
			// Each iteration investigates whether we have a CmObjectUi subclass that
			// corresponds to realClsid. If not, we move on to the base class of realClsid.
			// In this way, the CmObjectUi subclass we return is the one designed for the
			// closest base class of obj that has one.
			CmObjectUi result = null;
			while (result == null)
			{
				switch (realClsid)
				{
					// Todo: lots more useful cases.
					case WfiAnalysisTags.kClassId:
						result = new WfiAnalysisUi();
						break;
					case PartOfSpeechTags.kClassId:
						result = new PartOfSpeechUi();
						break;
					case CmPossibilityTags.kClassId:
						result = new CmPossibilityUi();
						break;
					case CmObjectTags.kClassId:
						result = new CmObjectUi();
						break;
					case LexPronunciationTags.kClassId:
						result = new LexPronunciationUi();
						break;
					case LexSenseTags.kClassId:
						result = new LexSenseUi();
						break;
					case LexEntryTags.kClassId:
						result = new LexEntryUi();
						break;
					case MoMorphSynAnalysisTags.kClassId:
						result = new MoMorphSynAnalysisUi();
						break;
					case MoStemMsaTags.kClassId:
						result = new MoStemMsaUi();
						break;
					case MoDerivAffMsaTags.kClassId:
						result = new MoDerivAffMsaUi();
						break;
					case MoInflAffMsaTags.kClassId:
						result = new MoInflAffMsaUi();
						break;
					case MoAffixAllomorphTags.kClassId:
					case MoStemAllomorphTags.kClassId:
						result = new MoFormUi();
						break;
					case ReversalIndexEntryTags.kClassId:
						result = new ReversalIndexEntryUi();
						break;
					case WfiWordformTags.kClassId:
						result = new WfiWordformUi();
						break;
					case WfiGlossTags.kClassId:
						result = new WfiGlossUi();
						break;
					case CmCustomItemTags.kClassId:
						result = new CmCustomItemUi();
						break;
					default:
						realClsid = mdc.GetBaseClsId(realClsid);
						break;
				}
			}

			if (realClsid != clsid)
			{
				m_subclasses[clsid] = realClsid;
			}

			result.m_hvo = hvo;
			result.m_cache = cache;

			return result;
		}

		/// <summary>
		/// Create a new LCM object.
		/// </summary>
		internal static CmObjectUi MakeLcmModelUiObject(IPropertyTable propertyTable, IPublisher publisher, int classId, int hvoOwner, int flid, int insertionPosition)
		{
			var cache = propertyTable.GetValue<LcmCache>(LanguageExplorerConstants.cache);
			switch (classId)
			{
				default:
					return MakeLcmModelUiObject(classId, hvoOwner, flid, insertionPosition, cache);
				case CmPossibilityTags.kClassId:
					return CmPossibilityUi.MakeLcmModelUiObject(cache, classId, hvoOwner, flid, insertionPosition);
				case PartOfSpeechTags.kClassId:
					return PartOfSpeechUi.MakeLcmModelUiObject(cache, propertyTable, publisher, classId, hvoOwner, flid, insertionPosition);
				case FsFeatDefnTags.kClassId:
					return FsFeatDefnUi.MakeLcmModelUiObject(cache, propertyTable, publisher, classId, hvoOwner, flid, insertionPosition);
				case LexSenseTags.kClassId:
					return LexSenseUi.MakeLcmModelUiObject(cache, hvoOwner, insertionPosition);
				case LexPronunciationTags.kClassId:
					return LexPronunciationUi.MakeLcmModelUiObject(cache, classId, hvoOwner, flid, insertionPosition);
			}
		}

		internal static CmObjectUi MakeLcmModelUiObject(int classId, int hvoOwner, int flid, int insertionPosition, LcmCache cache)
		{
			CmObjectUi newUiObj = null;
			UndoableUnitOfWorkHelper.Do(LcmUiStrings.ksUndoInsert, LcmUiStrings.ksRedoInsert, cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				var newHvo = cache.DomainDataByFlid.MakeNewObject(classId, hvoOwner, flid, insertionPosition);
				newUiObj = MakeLcmModelUiObject(cache, newHvo, classId);
			});
			return newUiObj;
		}

		#endregion Construction and initialization

		#region IDisposable & Co. implementation

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed { get; private set; }

		/// <summary>
		/// Finalizer, in case client doesn't dispose it.
		/// Force Dispose(false) if not already called (i.e. m_isDisposed is true)
		/// </summary>
		/// <remarks>
		/// In case some clients forget to dispose it directly.
		/// </remarks>
		~CmObjectUi()
		{
			Dispose(false);
			// The base class finalizer is called automatically.
		}

		/// <summary>
		///
		/// </summary>
		/// <remarks>Must not be virtual.</remarks>
		public void Dispose()
		{
			Dispose(true);
			// This object will be cleaned up by the Dispose method.
			// Therefore, you should call GC.SuppressFinalize to
			// take this object off the finalization queue
			// and prevent finalization code for this object
			// from executing a second time.
			GC.SuppressFinalize(this);
		}

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
		protected virtual void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (IsDisposed)
			{
				return;
			}

			if (disposing)
			{
				if (_rightClickTuple != null)
				{
					var dataTree = PropertyTable.GetValue<DataTree>("DataTree");
					dataTree.DataTreeStackContextMenuFactory.RightClickPopupMenuFactory.DisposePopupContextMenu(_rightClickTuple);
				}
				// Dispose managed resources here.
				var disposableVC = m_vc as IDisposable;
				disposableVC?.Dispose();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_cmObject = null;
			m_cache = null;
			m_vc = null;
			// Leave this static alone.
			// m_subclasses = null;
			m_hostControl = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;
			_rightClickTuple = null;

			IsDisposed = true;

			// Keep this from being collected, since it got removed from the static.
			GC.KeepAlive(this);
		}

		#endregion IDisposable & Co. implementation

		#region Jumping

		/// <summary>
		/// Return either the object or an owner ("parent") up the ownership chain that is of
		/// the desired class.  Being a subclass of the desired class also matches, unlike
		/// ICmObject.OwnerOfClass() where the class must match exactly.
		/// </summary>
		public static ICmObject GetSelfOrParentOfClass(ICmObject cmo, int classIdToSearchFor)
		{
			if (cmo == null)
			{
				return null;
			}
			var mdc = cmo.Cache.DomainDataByFlid.MetaDataCache;
			for (; cmo != null; cmo = cmo.Owner)
			{
				if ((DomainObjectServices.IsSameOrSubclassOf(mdc, cmo.ClassID, classIdToSearchFor)))
				{
					return cmo;
				}
			}
			return null;
		}

		/// <summary>
		/// gives the guid of the object to use in the URL we construct when doing a jump
		/// </summary>
		public virtual Guid GuidForJumping(object commandObject)
		{
			return MyCmObject.Guid;
		}

		private ICmObject GetCurrentCmObject()
		{
			ICmObject obj = null;
			if (m_hostControl is XmlBrowseViewBase && !m_hostControl.IsDisposed)
			{
				// since we're getting the context menu by clicking on the browse view
				// just use the current object of the browse view.
				// NOTE: This helps to bypass a race condition that occurs when the user
				// right-clicks on a record that isn't (yet) the current record.
				// In that case RecordBrowseView establishes the new index before
				// calling HandleRightClick to create the context menu, but
				// presently, "ActiveListSelectedObject" only gets established on Idle()
				// AFTER the context menu is created. (A side effect of LT-9192, LT-8874,
				// XmlBrowseViewBase.FireSelectionChanged)
				// To get around this, we must use the CurrentObject
				// directly from the Browse view.
				var hvoCurrentObject = (m_hostControl as XmlBrowseViewBase).SelectedObject;
				if (hvoCurrentObject != 0)
				{
					obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoCurrentObject);
				}
			}
			else
			{
				obj = PropertyTable.GetValue<ICmObject>("ActiveListSelectedObject", null);
			}
			return obj;
		}

		/// <summary>
		/// Get the id of the context menu that should be shown for our object
		/// </summary>
		protected virtual string ContextMenuId => AreaServices.mnuObjectChoices;

		/// <summary>
		/// Given a populated choice group, mark the one that will be invoked by a ctrl-click.
		/// This method is typically used as the menuAdjuster argument in calling HandleRightClick.
		/// It's important that it marks the same menu item as selected by HandleCtrlClick.
		/// </summary>
		public static void MarkCtrlClickItem(ContextMenuStrip menu)
		{
#if RANDYTODO
			foreach (var item in menu.Items)
			{
				var item1 = item as ToolStripItem;
				if (item1 == null || !(item1.Tag is CommandChoice) || !item1.Enabled)
					continue;
				var command = (CommandChoice) item1.Tag;
				if (command.Message != "JumpToTool")
					continue;

				item1.Text += LcmUiStrings.ksCtrlClick;
				return;
			}
#endif
		}

		/// <summary>
		/// Handle a control-click by invoking the first active JumpToTool menu item.
		/// Note that the item selected here should be the same one that is selected by Mark
		/// </summary>
		public bool HandleCtrlClick(Control hostControl)
		{
#if RANDYTODO
			var window = PropertyTable.GetValue<IFwMainWnd>(LanguageExplorerConstants.window);
			m_hostControl = hostControl;
			var group = window.GetChoiceGroupForMenu(ContextMenuId);
			group.PopulateNow();
			try
			{
				foreach (var item in group)
				{
					if (!IsCtrlClickItem(item))
						continue;
					((CommandChoice)item).OnClick(this, new EventArgs());
					return true;
				}
			}
			finally
			{
				Dispose();
			}
#endif
			return false;
		}

#if RANDYTODO
		private static bool IsCtrlClickItem(object item)
		{
			var command = item as CommandChoice;
			if (command == null || command.Message != "JumpToTool")
				return false;
			var displayProps = command.GetDisplayProperties();
			return (displayProps.Visible && displayProps.Enabled);
		}
#endif

		/// <summary>
		/// Handle the right click by popping up an explicit context menu id.
		/// </summary>
		public bool HandleRightClick(Control hostControl, bool shouldDisposeThisWhenClosed, string sMenuId = null, Action<ContextMenuStrip> adjustMenu = null)
		{
			if (string.IsNullOrWhiteSpace(sMenuId))
			{
				// Callers outside of the FooUi classes (e.g.: RuleFormulaControl) supply the menu id,
				// or they are happy with the FooUi ContextMenuId virtual property value (e.g.: SandboxBase).
				// In any case, only "hostControl" is of interest to this class, where all other parameters are passed
				// on to something that handles the context menu.
				sMenuId = ContextMenuId;
			}
			m_hostControl = hostControl;

			var sHostType = m_hostControl.GetType().Name;
			var sType = MyCmObject.GetType().Name;

			if (sHostType == "XmlBrowseView" && sType == "CmBaseAnnotation")
			{
				// Generally we don't want popups trying to manipulate the annotations as objects in browse views.
				// See e.g. LT-5156, 6534, 7160.
				// Indeed, since CmBaseAnnotation presents itself as a 'Problem Report', we don't want
				// to do it for any kind of annotation that couldn't be one!
				var activeRecordList = PropertyTable.GetValue<IRecordListRepository>(LanguageExplorerConstants.RecordListRepository).ActiveRecordList;
				if (activeRecordList is MatchingConcordanceItems)
				{
					// We don't want this either.  See LT-6101.
					return true;
				}
			}

			var dataTree = PropertyTable.GetValue<DataTree>("DataTree");
			if (dataTree != null)
			{
				if (_rightClickTuple != null)
				{
					dataTree.DataTreeStackContextMenuFactory.RightClickPopupMenuFactory.DisposePopupContextMenu(_rightClickTuple);
					_rightClickTuple = null;
				}
				_rightClickTuple = dataTree.DataTreeStackContextMenuFactory.RightClickPopupMenuFactory.GetPopupContextMenu(dataTree.CurrentSlice, sMenuId);
				if (_rightClickTuple == null)
				{
					// Nobody home (the menu).
					MessageBox.Show($"Popup menu: '{sMenuId}' not found.{Environment.NewLine}{Environment.NewLine}Register a creator method for it in dataTree.DataTreeStackContextMenuFactory.RightClickPopupMenuFactory.", "Implement missing popup menu", MessageBoxButtons.OK);
					return true;
				}
				if (_rightClickTuple.Item1.Items.Count > 0)
				{
					adjustMenu?.Invoke(_rightClickTuple.Item1);
					_rightClickTuple.Item1.Show(new Point(Cursor.Position.X, Cursor.Position.Y));
				}
			}
			else
			{
				// Nobody home (DataTree).
				MessageBox.Show($"Add DataTree to the PropertyTable.", "Implement missing popup menu", MessageBoxButtons.OK);
				return true;
			}

			return true;
		}
#endregion

#region Other methods


#if RANDYTODO
		/// <summary>
		/// Hack to "remove" the delete menu from the popup menu.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDeleteSelectedItem(object commandObject, ref UIItemDisplayProperties display)
		{
			if (m_hostControl.GetType().Name == "Sandbox"
				// Disable deleting from inside "Try a Word" dialog.  See FWR-3212.
				|| m_hostControl.GetType().Name == "TryAWordSandbox"
				// Disable deleting interior items from a WfiMorphBundle.  See LT-6217.
				|| (m_hostControl.GetType().Name == "OneAnalysisSandbox" && !(m_obj is IWfiMorphBundle)))
			{
				display.Visible = display.Enabled = false;
			}
			display.Text = string.Format(display.Text, DisplayNameOfClass);
			return true;
		}
#endif

		public virtual string DisplayNameOfClass
		{
			get
			{
				var poss = MyCmObject as ICmPossibility;
				if (poss != null)
				{
					return poss.ItemTypeName();
				}
				var typeName = MyCmObject.GetType().Name;
				var className = StringTable.Table.GetString(typeName, "ClassNames");
				if (className == "*" + typeName + "*")
				{
					className = typeName;
				}

				string altName;
				var featsys = MyCmObject.OwnerOfClass(FsFeatureSystemTags.kClassId) as IFsFeatureSystem;
				if (featsys?.OwningFlid == LangProjectTags.kflidPhFeatureSystem)
				{
					altName = StringTable.Table.GetString(className + "-Phonological", "AlternativeTypeNames");
					if (altName != "*" + className + "-Phonological*")
					{
						return altName;
					}
				}
				switch (MyCmObject.OwningFlid)
				{
					case MoStemNameTags.kflidRegions:
						altName = StringTable.Table.GetString(className + "-MoStemName", "AlternativeTypeNames");
						if (altName != "*" + className + "-MoStemName*")
						{
							return altName;
						}
						break;
				}
				return className;
			}
		}

		public virtual bool CanDelete(out string cannotDeleteMsg)
		{
			if (MyCmObject.CanDelete)
			{
				cannotDeleteMsg = null;
				return true;
			}

			cannotDeleteMsg = LcmUiStrings.ksCannotDeleteItem;
			return false;
		}

		/// <summary>
		/// Delete the object, after showing a confirmation dialog.
		/// Return true if deleted, false, if cancelled.
		/// </summary>
		public bool DeleteUnderlyingObject()
		{
			var cmo = GetCurrentCmObject();
			if (cmo != null && m_cmObject != null && cmo.Hvo == m_cmObject.Hvo)
			{
				Publisher.Publish("DeleteRecord", this);
			}
			else
			{
				var mainWindow = PropertyTable.GetValue<Form>(FwUtils.window);
				using (new WaitCursor(mainWindow))
				{
					using (var dlg = new ConfirmDeleteObjectDlg(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
					{
						string cannotDeleteMsg;
						if (CanDelete(out cannotDeleteMsg))
						{
							dlg.SetDlgInfo(this, m_cache, PropertyTable);
						}
						else
						{
							dlg.SetDlgInfo(this, m_cache, PropertyTable, TsStringUtils.MakeString(cannotDeleteMsg, m_cache.DefaultUserWs));
						}
						if (DialogResult.Yes == dlg.ShowDialog(mainWindow))
						{
							ReallyDeleteUnderlyingObject();
							return true; // deleted it
						}
					}
				}
			}
			return false; // didn't delete it.
		}

		/// <summary>
		/// Do any cleanup that involves interacting with the user, after the user has confirmed that our object should be
		/// deleted.
		/// </summary>
		protected virtual void DoRelatedCleanupForDeleteObject()
		{
			// For media and pictures: should we delete the file also?
			// arguably this should be on a subclass, but it's easier to share behavior for both here.
			ICmFile file = null;
			var pict = m_cmObject as ICmPicture;
			if (pict != null)
			{
				file = pict.PictureFileRA;
			}
			else if (m_cmObject is ICmMedia)
			{
				var media = (ICmMedia)m_cmObject;
				file = media.MediaFileRA;
			}
			else if (m_cmObject != null)
			{
				// No cleanup needed
				return;
			}
			ConsiderDeletingRelatedFile(file, PropertyTable);
		}

		public static bool ConsiderDeletingRelatedFile(ICmFile file, IPropertyTable propertyTable)
		{
			if (file == null)
			{
				return false;
			}
			var refs = file.ReferringObjects;
			if (refs.Count > 1)
			{
				return false; // exactly one if only this CmPicture uses it.
			}
			var path = file.InternalPath;
			if (Path.IsPathRooted(path))
			{
				return false; // don't delete external file
			}
			var msg = string.Format(LcmUiStrings.ksDeleteFileAlso, path);
			if (MessageBox.Show(Form.ActiveForm, msg, LcmUiStrings.ksDeleteFileCaption, MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
			{
				return false;
			}

			IFlexApp app;
			if (propertyTable != null && propertyTable.TryGetValue(LanguageExplorerConstants.App, out app))
			{
					app.PictureHolder.ReleasePicture(file.AbsoluteInternalPath);
			}
			var fileToDelete = file.AbsoluteInternalPath;

			propertyTable.GetValue<IFwMainWnd>(FwUtils.window).IdleQueue.Add(IdleQueuePriority.Low, obj =>
			{
				try
				{
					// I'm not sure why, but if we try to delete it right away, we typically get a failure,
					// with an exception indicating that something is using the file, despite the code above that
					// tries to make our picture cache let go of it.
					// However, waiting until idle seems to solve the problem.
					File.Delete(fileToDelete);
				}
				catch (IOException)
				{
					// If we can't actually delete the file for some reason, don't bother the user complaining.
				}
				return true; // task is complete, don't try again.
			});
			return false;
		}

		protected virtual void ReallyDeleteUnderlyingObject()
		{
			Logger.WriteEvent("Deleting '" + MyCmObject.ShortName + "'...");
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(LcmUiStrings.ksUndoDelete, LcmUiStrings.ksRedoDelete, m_cache.ActionHandlerAccessor, () =>
			{
				DoRelatedCleanupForDeleteObject();
				MyCmObject.Cache.DomainDataByFlid.DeleteObj(MyCmObject.Hvo);
			});
			Logger.WriteEvent("Done Deleting.");
			m_cmObject = null;
		}

		/// <summary>
		/// Merge the underling objects. This method handles the confirm dialog, then delegates
		/// the actual merge to ReallyMergeUnderlyingObject. If the flag is true, we merge
		/// strings and owned atomic objects; otherwise, we don't change any that aren't null
		/// to begin with.
		/// </summary>
		public void MergeUnderlyingObject(bool fLoseNoTextData)
		{
			var mainWindow = PropertyTable.GetValue<Form>(FwUtils.window);
			using (new WaitCursor(mainWindow))
			{
				using (var dlg = new MergeObjectDlg(PropertyTable.GetValue<IHelpTopicProvider>(LanguageExplorerConstants.HelpTopicProvider)))
				{
					dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
					var wp = new WindowParams();
					var mergeCandidates = new List<DummyCmObject>();
					string guiControl, helpTopic;
					var dObj = GetMergeinfo(wp, mergeCandidates, out guiControl, out helpTopic);
					mergeCandidates.Sort();
					dlg.SetDlgInfo(m_cache, wp, dObj, mergeCandidates, guiControl, helpTopic);
					if (DialogResult.OK == dlg.ShowDialog(mainWindow))
					{
						ReallyMergeUnderlyingObject(dlg.Hvo, fLoseNoTextData);
					}
				}
			}
		}

		/// <summary>
		/// Merge the underling objects. This method handles the transaction, then delegates
		/// the actual merge to MergeObject. If the flag is true, we merge
		/// strings and owned atomic objects; otherwise, we don't change any that aren't null
		/// to begin with.
		/// </summary>
		protected virtual void ReallyMergeUnderlyingObject(int survivorHvo, bool fLoseNoTextData)
		{
			var survivor = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(survivorHvo);
			Logger.WriteEvent("Merging '" + MyCmObject.ShortName + "' into '" + survivor.ShortName + "'.");
			var ah = m_cache.ServiceLocator.GetInstance<IActionHandler>();
			UndoableUnitOfWorkHelper.Do(LcmUiStrings.ksUndoMerge, LcmUiStrings.ksRedoMerge, ah, () => survivor.MergeObject(MyCmObject, fLoseNoTextData));
			Logger.WriteEvent("Done Merging.");
			m_cmObject = null;
		}

		protected virtual DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out string guiControl, out string helpTopic)
		{
			Debug.Assert(false, "Subclasses must override this method.");
			guiControl = null;
			helpTopic = null;
			return null;
		}

		/// <summary />
		public virtual void MoveUnderlyingObjectToCopyOfOwner()
		{
			MessageBox.Show(PropertyTable.GetValue<Form>(FwUtils.window), LcmUiStrings.ksCannotMoveObjectToCopy, LcmUiStrings.ksBUG);
		}

		/// <summary>
		///  Convert a .NET color to the type understood by Views code and other Win32 stuff.
		/// </summary>
		public static uint RGB(Color c)
		{
			return RGB(c.R, c.G, c.B);
		}

		/// <summary>
		/// Make a standard Win32 color from three components.
		/// </summary>
		public static uint RGB(int r, int g, int b)
		{
			return (uint)((byte)r | ((byte)(g) << 8) | ((byte)b << 16));

		}

		/// <summary />
		/// <param name="singlePropertySequenceValue"></param>
		/// <param name="cacheForCheckingValidity">null, if you don't care about checking the validity of the items in singlePropertySequenceValue,
		/// otherwise, pass in a cache to check validity.</param>
		/// <param name="expectedClassId">if you pass a cache, you can also use this too make sure the object matches an expected class,
		/// otherwise it just checks that the object exists in the database (or is a valid virtual object)</param>
		/// <returns></returns>
		public static List<int> ParseSinglePropertySequenceValueIntoHvos(string singlePropertySequenceValue, LcmCache cacheForCheckingValidity, int expectedClassId)
		{
			var hvos = new List<int>();
			if (string.IsNullOrEmpty(singlePropertySequenceValue))
			{
				return hvos;
			}
			var cache = cacheForCheckingValidity;
			foreach (var sHvo in singlePropertySequenceValue.Split(','))
			{
				int hvo;
				if (!int.TryParse(sHvo, out hvo))
				{
					continue;
				}
				if (cache == null)
				{
					continue;
				}
				ICmObject obj;
				if (!cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(hvo, out obj))
				{
					continue;
				}
				if (obj.IsValidObject)
				{
					hvos.Add(hvo);
				}
			}
			return hvos;
		}

#endregion Other methods

#region Implementation of IPropertyTableProvider

		/// <summary>
		/// Placement in the IPropertyTableProvider interface lets FwApp call IPropertyTable.DoStuff.
		/// </summary>
		public IPropertyTable PropertyTable { get; set; }

#endregion

#region Implementation of IPublisherProvider

		/// <summary>
		/// Get the IPublisher.
		/// </summary>
		public IPublisher Publisher { get; private set; }

#endregion

#region Implementation of ISubscriberProvider

		/// <summary>
		/// Get the ISubscriber.
		/// </summary>
		public ISubscriber Subscriber { get; private set; }

		#endregion

		#region Implementation of IFlexComponent

		/// <summary>
		/// Initialize a FLEx component with the basic interfaces.
		/// </summary>
		/// <param name="flexComponentParameters">Parameter object that contains the required three interfaces.</param>
		public virtual void InitializeFlexComponent(FlexComponentParameters flexComponentParameters)
		{
			FlexComponentParameters.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion
	}
}