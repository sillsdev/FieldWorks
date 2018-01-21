// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using LanguageExplorer.Areas.TextsAndWords.Interlinear;
using LanguageExplorer.LcmUi.Dialogs;
using LanguageExplorer.Controls.LexText;
using LanguageExplorer.Controls.XMLViews;
using SIL.LCModel.Core.Text;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.Reporting;

namespace LanguageExplorer.LcmUi
{
	public class CmObjectUi : IFlexComponent, IDisposable
	{
		#region Data members

		protected ICmObject m_obj;
		protected int m_hvo;
		protected LcmCache m_cache;
		// Map from uint to uint, specifically, from clsid to clsid.
		// The key is any clsid that we have so far been asked to make a UI object for.
		// The value is the corresponding clsid that actually occurs in the switch.
		// Review JohnH (JohnT): would it be more efficient to store a Class object in the map,
		// and use reflection to make an instance?
		static readonly Dictionary<int, int> m_subclasses = new Dictionary<int, int>();
		protected Control m_hostControl;
		protected IVwViewConstructor m_vc = null;

		#endregion Data members

		#region Properties

		/// <summary>
		/// Retrieve the CmObject we are providing UI functions for. If necessary create it.
		/// </summary>
		public ICmObject Object
		{
			get
			{
				CheckDisposed();

				if (m_obj == null)
					m_obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(m_hvo);
				return m_obj;
			}
		}

		public string ClassName
		{
			get
			{
				CheckDisposed();

				return Object.ClassName;
			}
		}

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
		/// 			IVwViewConstructor vcName = CmObjectUi.MakeUi(m_cache, hvo).Vc;
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
		///				IVwViewConstructor vcName = CmObjectUi.MakeUi(m_cache, hvoObj).Vc;
		///				vwenv.AddObjProp(kflidXYZ, vcName, VcFrags.kfragShortName);
		///				...
		///				break;
		/// </summary>
		public virtual IVwViewConstructor Vc
		{
			get
			{
				CheckDisposed();

				if (m_vc == null)
					m_vc = new CmObjectVc(m_cache);
				return m_vc;
			}
		}

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
		/// 			IVwViewConstructor vcName = CmObjectUi.MakeUi(m_cache, hvo).VernVc;
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
		///				IVwViewConstructor vcName = CmObjectUi.MakeUi(m_cache, hvoObj).VernVc;
		///				vwenv.AddObjProp(kflidXYZ, vcName, VcFrags.kfragShortName);
		///				...
		///				break;
		/// </summary>
		public virtual IVwViewConstructor VernVc
		{
			get
			{
				CheckDisposed();
				return new CmVernObjectVc(m_cache);
			}
		}

		public virtual IVwViewConstructor AnalVc
		{
			get
			{
				CheckDisposed();
				return new CmAnalObjectVc(m_cache);
			}
		}

		#endregion Properties

		#region Construction and initialization

		/// <summary>
		/// If you KNOW for SURE the right subclass of CmObjectUi, you can just make one
		/// directly. Most clients should use MakeUi.
		/// </summary>
		/// <param name="obj"></param>
		public CmObjectUi(ICmObject obj)
		{
			m_obj = obj;
			m_cache = obj.Cache;
		}

		/// <summary>
		/// This should only be used by MakeUi.
		/// </summary>
		internal CmObjectUi()
		{
		}

		/// <summary>
		/// This is the main class factory that makes a corresponding CmObjectUi for any given
		/// CmObject.
		/// </summary>
		/// <param name="obj"></param>
		/// <returns></returns>
		public static CmObjectUi MakeUi(ICmObject obj)
		{
			CmObjectUi result = MakeUi(obj.Cache, obj.Hvo, obj.ClassID);
			result.m_obj = obj;
			return result;
		}

		/// <summary>
		/// In many cases we don't really need the LCM object, which can be relatively expensive
		/// to create. This version saves the information, and creates it when needed.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public static CmObjectUi MakeUi(LcmCache cache, int hvo)
		{
			return MakeUi(cache, hvo, cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo).ClassID);
		}

		private static CmObjectUi MakeUi(LcmCache cache, int hvo, int clsid)
		{
			IFwMetaDataCache mdc = cache.DomainDataByFlid.MetaDataCache;
			// If we've encountered an object with this Clsid before, and this clsid isn't in
			// the switch below, the dictioanry will give us the appropriate clsid that IS in the
			// map, so the loop below will have only one iteration. Otherwise, we start the
			// search with the clsid of the object itself.
			int realClsid = m_subclasses.ContainsKey(clsid) ? m_subclasses[clsid] : clsid;
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
						// This isn't needed because CmObject.kClassId IS 0.
						//					if (realClsid == 0)
						//					{
						//						// Somehow the class doesn't have CmObject in its inheritance path!
						//						Debug.Assert(false);
						//						// this may help make us more robust if this somehow happens.
						//						realClsid = (uint)CmObject.kClassId;
						//					}
						break;
				}
			}
			if (realClsid != clsid)
				m_subclasses[clsid] = realClsid;

			result.m_hvo = hvo;
			result.m_cache = cache;

			return result;
		}

		/// <summary>
		/// Create a new LCM object.
		/// </summary>
		public static CmObjectUi CreateNewUiObject(IPropertyTable propertyTable, IPublisher publisher, int classId, int hvoOwner, int flid, int insertionPosition)
		{
			var cache = propertyTable.GetValue<LcmCache>("cache");
			switch (classId)
			{
				default:
					return DefaultCreateNewUiObject(classId, hvoOwner, flid, insertionPosition, cache);
				case CmPossibilityTags.kClassId:
					return CmPossibilityUi.CreateNewUiObject(cache, classId, hvoOwner, flid, insertionPosition);
				case PartOfSpeechTags.kClassId:
					return PartOfSpeechUi.CreateNewUiObject(cache, propertyTable, publisher, classId, hvoOwner, flid, insertionPosition);
				case FsFeatDefnTags.kClassId:
					return FsFeatDefnUi.CreateNewUiObject(cache, propertyTable, publisher, classId, hvoOwner, flid, insertionPosition);
				case LexSenseTags.kClassId:
					return LexSenseUi.CreateNewUiObject(cache, hvoOwner, insertionPosition);
				case LexPronunciationTags.kClassId:
					return LexPronunciationUi.CreateNewUiObject(cache, classId, hvoOwner, flid, insertionPosition);
			}
		}

		internal static CmObjectUi DefaultCreateNewUiObject(int classId, int hvoOwner, int flid, int insertionPosition, LcmCache cache)
		{
			CmObjectUi newUiObj = null;
			UndoableUnitOfWorkHelper.Do(LcmUiStrings.ksUndoInsert, LcmUiStrings.ksRedoInsert, cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				int newHvo = cache.DomainDataByFlid.MakeNewObject(classId, hvoOwner, flid, insertionPosition);
				newUiObj = MakeUi(cache, newHvo, classId);
			});
			return newUiObj;
		}

		#endregion Construction and initialization

		#region IDisposable & Co. implementation
		// Region last reviewed: never

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
		/// True, if the object has been disposed.
		/// </summary>
		private bool m_isDisposed;

		/// <summary>
		/// See if the object has been disposed.
		/// </summary>
		public bool IsDisposed
		{
			get { return m_isDisposed; }
		}

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
			// Therefore, you should call GC.SupressFinalize to
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
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				var disposableVC = m_vc as IDisposable;
				if (disposableVC != null)
					disposableVC.Dispose();
				// Leave this static alone.
				//if (m_subclasses != null)
				//	m_subclasses.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_obj = null;
			m_cache = null;
			m_vc = null;
			// Leave this static alone.
			// m_subclasses = null;
			m_hostControl = null;
			PropertyTable = null;
			Publisher = null;
			Subscriber = null;

			m_isDisposed = true;

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
				return null;
			IFwMetaDataCache mdc = cmo.Cache.DomainDataByFlid.MetaDataCache;
			for (; cmo != null; cmo = cmo.Owner)
			{
				if ((DomainObjectServices.IsSameOrSubclassOf(mdc, cmo.ClassID, classIdToSearchFor)))
					return cmo;
			}
			return null;
		}

#if RANDYTODO
		public virtual void LaunchGuiControl(Command command)
		{
			CheckDisposed();
			string guicontrol = command.GetParameter("guicontrol");
			string xpathToControl = String.Format("/window/controls/parameters/guicontrol[@id=\"{0}\"]", guicontrol);
			XmlNode xnControl = command.ConfigurationNode.SelectSingleNode(xpathToControl);
			if (xnControl != null)
			{
				using (var dlg = (IFwGuiControl) DynamicLoader.CreateObject(xnControl.SelectSingleNode("dynamicloaderinfo")))
				{
					dlg.Init(m_mediator, m_propertyTable, xnControl.SelectSingleNode("parameters"), Object);
					dlg.Launch();
				}
			}
		}
#endif

		/// <summary>
		/// gives the guid of the object to use in the URL we construct when doing a jump
		/// </summary>
		public virtual Guid GuidForJumping(object commandObject)
		{
			return Object.Guid;
		}

#if RANDYTODO
		/// <summary>
		/// This method CALLED BY REFLECTION is required to make various right-click menu commands
		/// like Show Entry in Lexicon work in browse views. FWR-3695.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnJumpToTool(object commandObject)
		{
			CheckDisposed();

			var command = (Command) commandObject;
			string tool = XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "tool");
			var guid = GuidForJumping(commandObject);
			var commands = new List<string>
									{
										"AboutToFollowLink",
										"FollowLink"
									};
			var parms = new List<object>
									{
										null,
										new FwLinkArgs(tool, guid)
									};
			Publisher.Publish(commands, parms);
			return true;
		}

		/// <summary>
		/// called by the mediator to decide how/if a MenuItem or toolbar button should be displayed
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			var command = (Command) commandObject;
			string tool = XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "tool");
			//string areaChoice = m_propertyTable.GetValue<string>(AreaServices.AreaChoice);
			//string toolChoice = m_propertyTable.GetValue<string>($"{AreaServices.ToolForAreaNamed_}{areaChoice}");
			string toolChoice = m_propertyTable.GetValue<string>(AreaServices.ToolChoice);
			if (!IsAcceptableContextToJump(toolChoice, tool))
			{
				display.Visible = display.Enabled = false;
				return true;
			}
			string className = XmlUtils.GetMandatoryAttributeValue(command.Parameters[0], "className");


			int specifiedClsid = 0;
			var mdc = m_cache.GetManagedMetaDataCache();
			if (mdc.ClassExists(className)) // otherwise is is a 'magic' class name treated specially in other OnDisplays.
				specifiedClsid = mdc.GetClassId(className);

			display.Visible = display.Enabled = ShouldDisplayMenuForClass(specifiedClsid, display);
			if (display.Enabled)
				command.TargetId = GuidForJumping(commandObject);
			return true;
		}
#endif

		protected virtual bool IsAcceptableContextToJump(string toolCurrent, string toolTarget)
		{
			if (toolCurrent == toolTarget)
			{
				ICmObject obj = GetCurrentCmObject();
				// Disable if target is the current object, or target is owned directly by the target object.
				if (obj != null && (obj.Hvo == m_hvo || m_cache.ServiceLocator.GetObject(m_hvo).Owner == obj))
				{
					return false; // we're already there!
				}
			}
			return true;
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
				int hvoCurrentObject = (m_hostControl as XmlBrowseViewBase).SelectedObject;
				if (hvoCurrentObject != 0)
					obj = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoCurrentObject);
			}
			else
			{
				obj = PropertyTable.GetValue<ICmObject>("ActiveListSelectedObject", null);
			}
			return obj;
		}

		protected virtual bool ShouldDisplayMenuForClass(int specifiedClsid)
		{
			if (specifiedClsid == 0)
				return false; // a special magic class id, only enabled explicitly.
			if (Object.ClassID == specifiedClsid)
				return true;
				int baseClsid = m_cache.DomainDataByFlid.MetaDataCache.GetBaseClsId(Object.ClassID);
				if (baseClsid == specifiedClsid) //handle one level of subclassing
					return true;
					return false;
		}

		/// <summary>
		/// Get the id of the XCore Context menu that should be shown for our object
		/// </summary>
		public virtual string ContextMenuId
		{
			get
			{
				CheckDisposed();
				return "mnuObjectChoices";
			}
		}

		/// <summary>
		/// Handle a right click by popping up the implied context menu.
		/// </summary>
		/// <param name="hostControl"></param>
		/// <param name="shouldDisposeThisWhenClosed">True, if the menu handler is to dispose of the CmObjectUi after menu closing</param>
		/// <returns></returns>
		public bool HandleRightClick(Control hostControl, bool shouldDisposeThisWhenClosed)
		{
			CheckDisposed();

			return HandleRightClick(hostControl, shouldDisposeThisWhenClosed, ContextMenuId);
		}

		/// <summary>
		/// Handle a right click by popping up the implied context menu.
		/// </summary>
		/// <param name="hostControl"></param>
		/// <param name="shouldDisposeThisWhenClosed">True, if the menu handler is to dispose of the CmObjectUi after menu closing</param>
		/// <param name="adjustMenu"></param>
		/// <returns></returns>
		public bool HandleRightClick(Control hostControl, bool shouldDisposeThisWhenClosed, Action<ContextMenuStrip> adjustMenu)
		{
			CheckDisposed();

			return HandleRightClick(hostControl, shouldDisposeThisWhenClosed, ContextMenuId, adjustMenu);
		}

		/// <summary>
		/// Given a populated choice group, mark the one that will be invoked by a ctrl-click.
		/// This method is typically used as the menuAdjuster argument in calling HandleRightClick.
		/// It's important that it marks the same menu item as selected by HandlCtrlClick.
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
		/// <param name="hostControl"></param>
		/// <returns></returns>
		public bool HandleCtrlClick(Control hostControl)
		{
#if RANDYTODO
			var window = PropertyTable.GetValue<IFwMainWnd>("window");
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
		/// <param name="hostControl"></param>
		/// <param name="shouldDisposeThisWhenClosed">True, if the menu handler is to dispose of the CmObjectUi after menu closing</param>
		/// <param name="sMenuId"></param>
		/// <returns></returns>
		public bool HandleRightClick(Control hostControl, bool shouldDisposeThisWhenClosed, string sMenuId)
		{
			return HandleRightClick(hostControl, shouldDisposeThisWhenClosed, sMenuId, null);
		}

		/// <summary>
		/// Handle the right click by popping up an explicit context menu id.
		/// </summary>
		/// <param name="hostControl"></param>
		/// <param name="shouldDisposeThisWhenClosed">True, if the menu handler is to dispose of the CmObjectUi after menu closing</param>
		/// <param name="sMenuId"></param>
		/// <param name="adjustMenu"></param>
		/// <returns></returns>
		public bool HandleRightClick(Control hostControl, bool shouldDisposeThisWhenClosed, string sMenuId, Action<ContextMenuStrip> adjustMenu)
		{
			CheckDisposed();

			m_hostControl = hostControl;

			string sHostType = m_hostControl.GetType().Name;
			string sType = Object.GetType().Name;

			if (sHostType == "XmlBrowseView" && sType == "CmBaseAnnotation")
			{
				// Generally we don't want popups trying to manipulate the annotations as objects in browse views.
				// See e.g. LT-5156, 6534, 7160.
				// Indeed, since CmBaseAnnotation presents itself as a 'Problem Report', we don't want
				// to do it for any kind of annotation that couldn't be one!
				var activeRecordList = RecordList.ActiveRecordListRepository.ActiveRecordList;
				if (activeRecordList is MatchingConcordanceItems)
				{
					// We don't want this either.  See LT-6101.
					return true;
				}
			}

			// TODO: The context menu needs to be filtered to remove inappropriate menu items.
#if RANDYTODO
			var window = PropertyTable.GetValue<IFwMainWnd>("window");
			window.ShowContextMenu(sMenuId,
				new Point(Cursor.Position.X, Cursor.Position.Y),
				new TemporaryColleagueParameter(m_mediator, this, shouldDisposeThisWhenClosed),
				null, adjustMenu);
			// Using the sequencer here now causes problems with slices that allow
			// keyboard activity (cf. PhoneEnvReferenceView).
			// If a safe blocking mechanism can be found for the context menu, we can restore the original behavior
			// which will have this code do the setup and teardown work.
			//(hostControl as IReceiveSequentialMessages).Sequencer);
#endif

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
			CheckDisposed();

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
				CheckDisposed();

				var poss = Object as ICmPossibility;
				if (poss != null)
				{
					return poss.ItemTypeName();
				}
				string typeName = Object.GetType().Name;
				string className = StringTable.Table.GetString(typeName, "ClassNames");
				if (className == "*" + typeName + "*")
					className = typeName;

				string altName;
				var featsys = Object.OwnerOfClass(FsFeatureSystemTags.kClassId) as IFsFeatureSystem;
				if (featsys != null)
				{
					if (featsys.OwningFlid == LangProjectTags.kflidPhFeatureSystem)
					{
						altName = StringTable.Table.GetString(className + "-Phonological", "AlternativeTypeNames");
						if (altName != "*" + className + "-Phonological*")
							return altName;
					}
				}
				switch (Object.OwningFlid)
				{
					case MoStemNameTags.kflidRegions:
						altName = StringTable.Table.GetString(className + "-MoStemName", "AlternativeTypeNames");
						if (altName != "*" + className + "-MoStemName*")
							return altName;
						break;
				}
				return className;
			}
		}


#if RANDYTODO
		public void OnDeleteSelectedItem(object commandObject)
		{
			CheckDisposed();
			m_command = commandObject as Command;

			try
			{
				// Instead of deleting a single WfiMorphBundle (which is what would normally happen
				// in our automated handling, delete the owning WfiAnalysis.  (See LT-6217.)
				if (m_obj is IWfiMorphBundle)
				{
					// we want to delete the owner, not just this object itself.
					using (CmObjectUi owner = MakeUi(m_cache, m_obj.Owner.Hvo))
					{
						owner.Mediator = m_mediator;
						owner.PropTable = m_propertyTable;
						owner.DeleteUnderlyingObject();
					}
				}
				else
				{
					DeleteUnderlyingObject();
				}
			}
			finally
			{
				m_command = null;
			}
		}
#endif

		public virtual bool CanDelete(out string cannotDeleteMsg)
		{
			if (Object.CanDelete)
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
			CheckDisposed();

			ICmObject cmo = GetCurrentCmObject();
			if (cmo != null && m_obj != null && cmo.Hvo == m_obj.Hvo)
			{
				Publisher.Publish("DeleteRecord", this);
			}
			else
			{
				var mainWindow = PropertyTable.GetValue<Form>("window");
				using (new WaitCursor(mainWindow))
				{
					using (var dlg = new ConfirmDeleteObjectDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
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
		protected virtual bool DoRelatedCleanupForDeleteObject()
		{
			// For media and pictures: should we delete the file also?
			// arguably this should be on a subclass, but it's easier to share behavior for both here.
			ICmFile file = null;
			var pict = m_obj as ICmPicture;
			if (pict != null)
			{
				file = pict.PictureFileRA;
			}
			else if (m_obj is ICmMedia)
			{
				var media = (ICmMedia)m_obj;
				file = media.MediaFileRA;
			}
			else if (m_obj != null)
			{
				// No cleanup needed
				return true;
			}
			return ConsiderDeletingRelatedFile(file, PropertyTable);
		}

		public static bool ConsiderDeletingRelatedFile(ICmFile file, IPropertyTable propertyTable)
		{
			if (file == null)
				return false;
			var refs = file.ReferringObjects;
			if (refs.Count > 1)
				return false; // exactly one if only this CmPicture uses it.
			var path = file.InternalPath;
			if (Path.IsPathRooted(path))
				return false; // don't delete external file
			string msg = String.Format(LcmUiStrings.ksDeleteFileAlso, path);
			if (MessageBox.Show(Form.ActiveForm, msg, LcmUiStrings.ksDeleteFileCaption, MessageBoxButtons.YesNo,
				MessageBoxIcon.Question)
				!= DialogResult.Yes)
			{
				return false;
			}

			IFlexApp app;
			if (propertyTable != null && propertyTable.TryGetValue("App", out app))
			{
					app.PictureHolder.ReleasePicture(file.AbsoluteInternalPath);
			}
			var fileToDelete = file.AbsoluteInternalPath;

			propertyTable.GetValue<IFwMainWnd>("window").IdleQueue.Add(IdleQueuePriority.Low, obj =>
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
			Logger.WriteEvent("Deleting '" + Object.ShortName + "'...");
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(LcmUiStrings.ksUndoDelete, LcmUiStrings.ksRedoDelete, m_cache.ActionHandlerAccessor, () =>
			{
				if (DoRelatedCleanupForDeleteObject())
				{
					Object.Cache.DomainDataByFlid.DeleteObj(Object.Hvo);
				}
			});
			Logger.WriteEvent("Done Deleting.");
			m_obj = null;
		}

		/// <summary>
		/// Merge the underling objects. This method handles the confirm dialog, then delegates
		/// the actual merge to ReallyMergeUnderlyingObject. If the flag is true, we merge
		/// strings and owned atomic objects; otherwise, we don't change any that aren't null
		/// to begin with.
		/// </summary>
		/// <param name="fLoseNoTextData"></param>
		public void MergeUnderlyingObject(bool fLoseNoTextData)
		{
			CheckDisposed();

			var mainWindow = PropertyTable.GetValue<Form>("window");
			using (new WaitCursor(mainWindow))
			{
				using (var dlg = new MergeObjectDlg(PropertyTable.GetValue<IHelpTopicProvider>("HelpTopicProvider")))
				{
					dlg.InitializeFlexComponent(new FlexComponentParameters(PropertyTable, Publisher, Subscriber));
					var wp = new WindowParams();
					var mergeCandidates = new List<DummyCmObject>();
					string guiControl, helpTopic;
					DummyCmObject dObj = GetMergeinfo(wp, mergeCandidates, out guiControl, out helpTopic);
					mergeCandidates.Sort();
					dlg.SetDlgInfo(m_cache, wp, dObj, mergeCandidates, guiControl, helpTopic);
					if (DialogResult.OK == dlg.ShowDialog(mainWindow))
						ReallyMergeUnderlyingObject(dlg.Hvo, fLoseNoTextData);
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
			ICmObject survivor = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(survivorHvo);
			Logger.WriteEvent("Merging '" + Object.ShortName + "' into '" + survivor.ShortName + "'.");
			var ah = m_cache.ServiceLocator.GetInstance<IActionHandler>();
			UndoableUnitOfWorkHelper.Do(LcmUiStrings.ksUndoMerge, LcmUiStrings.ksRedoMerge, ah, () => survivor.MergeObject(Object, fLoseNoTextData));
			Logger.WriteEvent("Done Merging.");
			m_obj = null;
		}

		protected virtual DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out string guiControl, out string helpTopic)
		{
			Debug.Assert(false, "Subclasses must override this method.");
			guiControl = null;
			helpTopic = null;
			return null;
		}

		/// <summary>
		///
		/// </summary>
		public virtual void MoveUnderlyingObjectToCopyOfOwner()
		{
			CheckDisposed();

			var mainWindow = PropertyTable.GetValue<Form>("window");
			MessageBox.Show(mainWindow, LcmUiStrings.ksCannotMoveObjectToCopy, LcmUiStrings.ksBUG);
		}

		/// <summary>
		/// Get a string suitable for use in the left panel of the LexText status bar.
		/// It will show the created and modified dates, if the object has them.
		/// </summary>
		public string ToStatusBar()
		{
			CheckDisposed();

			if (!Object.IsValidObject)
				return LcmUiStrings.ksDeletedObject;
			DateTime dt;
			string created = "";
			string modified = "";
			System.Reflection.PropertyInfo pi = Object.GetType().GetProperty("DateCreated");
			if (pi != null)
			{
				dt = (DateTime)pi.GetValue(Object, null);
				created = dt.ToString("dd/MMM/yyyy", System.Globalization.DateTimeFormatInfo.InvariantInfo);
			}
			pi = Object.GetType().GetProperty("DateModified");
			if (pi != null)
			{
				dt = (DateTime)pi.GetValue(Object, null);
				modified = dt.ToString("dd/MMM/yyyy", System.Globalization.DateTimeFormatInfo.InvariantInfo);
			}
			return String.Format("{0} {1}", created, modified);
		}

		/// <summary>
		///  Convert a .NET color to the type understood by Views code and other Win32 stuff.
		/// </summary>
		/// <param name="c"></param>
		/// <returns></returns>
		public static uint RGB(Color c)
		{
			return RGB(c.R, c.G, c.B);
		}

		/// <summary>
		/// Make a standard Win32 color from three components.
		/// </summary>
		/// <param name="r"></param>
		/// <param name="g"></param>
		/// <param name="b"></param>
		/// <returns></returns>
		public static uint RGB(int r, int g, int b)
		{
			return ((uint)(((byte)(r) | ((byte)(g) << 8)) | ((byte)(b) << 16)));

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

#region Embedded View Constructor classes

#endregion Embedded View Constructor classes

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
			FlexComponentCheckingService.CheckInitializationValues(flexComponentParameters, new FlexComponentParameters(PropertyTable, Publisher, Subscriber));

			PropertyTable = flexComponentParameters.PropertyTable;
			Publisher = flexComponentParameters.Publisher;
			Subscriber = flexComponentParameters.Subscriber;
		}

		#endregion
	}
}