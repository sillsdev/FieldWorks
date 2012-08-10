using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.Framework;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.Utils;
using SIL.FieldWorks.LexText.Controls;
using XCore;
using SIL.FieldWorks.Common.Controls;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FdoUi
{
	/// <summary>
	/// Allows a guicontrol to dynamically initializing with a configuration node with respect
	/// to the given sourceObject.
	/// </summary>
	public interface IFwGuiControl : IFWDisposable
	{
		void Init(Mediator mediator, XmlNode configurationNode, ICmObject sourceObject);
		void Launch();
	}

	/// <summary>
	/// This enumeration lists fragments that are supported in some way by all CmObject
	/// subclasses. Many classes may implement them only using the approach here, which
	/// for some of them is very minimal. Override and enhance where possible.
	/// </summary>
	public enum VcFrags : int
	{
		// numbers below this are reserved for internal use by VCs.
		kfragShortName = 10567, // an arbitrary number, unlikely to be confused with others.
		kfragName,
		// Currently only MSAs and subclasses have interlinear names.
		kfragInterlinearName, // often just ShortName, what we want to appear in an interlinear view.
		kfragInterlinearAbbr, // use abbreviation for grammatical category.
		kfragFullMSAInterlinearname, // Used for showing MSAs in the MSA editor dlg.
		kfragHeadWord,	// defined only for LexEntry, fancy form of ShortName.
		kfragPosAbbrAnalysis, // display a PartOfSpeech using its analyis Ws abbreviation.
	}

	public class CmObjectUi : IxCoreColleague, IFWDisposable
	{
		#region Data members

		protected XCore.Mediator m_mediator;
		private Command m_command;
		protected ICmObject m_obj;
		protected int m_hvo;
		protected FdoCache m_cache;
		// Map from uint to uint, specifically, from clsid to clsid.
		// The key is any clsid that we have so far been asked to make a UI object for.
		// The value is the corresponding clsid that actually occurs in the switch.
		// Review JohnH (JohnT): would it be more efficient to store a Class object in the map,
		// and use reflection to make an instance?
		static Dictionary<int, int> m_subclasses = new Dictionary<int, int>();
		protected Control m_hostControl;
		// An additional target that should be included when the CmObjectUi acts as an xcore colleague.
		private IxCoreColleague m_additonalTarget;
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

		public virtual XCore.Mediator Mediator
		{
			set
			{
				CheckDisposed();

				Debug.Assert(value != null);
				// if (m_mediator != null)
				//	throw new ArgumentException("FDO UI object already has its mediator.");
				m_mediator = value;
			}
			get
			{
				CheckDisposed();
				return m_mediator;
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

		public IxCoreColleague AdditionalColleague
		{
			get { return m_additonalTarget; }
			set { m_additonalTarget = value; }
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
		/// In many cases we don't really need the FDO object, which can be relatively expensive
		/// to create. This version saves the information, and creates it when needed.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvo"></param>
		/// <returns></returns>
		public static CmObjectUi MakeUi(FdoCache cache, int hvo)
		{
			return MakeUi(cache, hvo, cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo).ClassID);
		}

		private static CmObjectUi MakeUi(FdoCache cache, int hvo, int clsid)
		{
			IFwMetaDataCache mdc = cache.DomainDataByFlid.MetaDataCache;
			// If we've encountered an object with this Clsid before, and this clsid isn't in
			// the switch below, the dictioanry will give us the appropriate clsid that IS in the
			// map, so the loop below will have only one iteration. Otherwise, we start the
			// search with the clsid of the object itself.
			int realClsid;
			realClsid = m_subclasses.ContainsKey(clsid) ? m_subclasses[clsid] : clsid;
			// Each iteration investigates whether we have a CmObjectUi subclass that
			// corresponds to realClsid. If not, we move on to the base class of realClsid.
			// In this way, the CmObjectUi subclass we return is the one designed for the
			// closest base class of obj that has one.
			CmObjectUi result = null;
			while (result == null)
			{
				switch ((int)realClsid)
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
		/// Create a new FDO object.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="classId"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flid"></param>
		/// <param name="insertionPosition"></param>
		/// <returns></returns>
		public static CmObjectUi CreateNewUiObject(Mediator mediator, int classId, int hvoOwner, int flid, int insertionPosition)
		{
			FdoCache cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			CmObjectUi newUiObj;
			switch (classId)
			{
				default:
					{
						newUiObj = DefaultCreateNewUiObject(classId, hvoOwner, flid, insertionPosition, cache);
						break;
					}
				case CmPossibilityTags.kClassId:
					{
						newUiObj = CmPossibilityUi.CreateNewUiObject(mediator, classId, hvoOwner, flid, insertionPosition);
						break;
					}
				case PartOfSpeechTags.kClassId:
					{
						newUiObj = PartOfSpeechUi.CreateNewUiObject(mediator, classId, hvoOwner, flid, insertionPosition);
						break;
					}
				case FsFeatDefnTags.kClassId:
					{
						newUiObj = FsFeatDefnUi.CreateNewUiObject(mediator, classId, hvoOwner, flid, insertionPosition);
						break;
					}
				case LexSenseTags.kClassId:
					{
						newUiObj = LexSenseUi.CreateNewUiObject(mediator, classId, hvoOwner, flid, insertionPosition);
						break;
					}
				case LexPronunciationTags.kClassId:
					{
						newUiObj = LexPronunciationUi.CreateNewUiObject(mediator, classId, hvoOwner, flid, insertionPosition);
						break;
					}
			}
			return newUiObj;
		}

		internal static CmObjectUi DefaultCreateNewUiObject(int classId, int hvoOwner, int flid, int insertionPosition, FdoCache cache)
		{
			CmObjectUi newUiObj = null;
			UndoableUnitOfWorkHelper.Do(FdoUiStrings.ksUndoInsert, FdoUiStrings.ksRedoInsert, cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
			{
				int newHvo = cache.DomainDataByFlid.MakeNewObject((int)classId, hvoOwner, flid, insertionPosition);
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
		private bool m_isDisposed = false;

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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			// Must not be run more than once.
			if (m_isDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_mediator != null)
					m_mediator.RemoveColleague(this);
				if (m_vc != null && (m_vc is IDisposable))
					(m_vc as IDisposable).Dispose();
				// Leave this static alone.
				//if (m_subclasses != null)
				//	m_subclasses.Clear();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_mediator = null;
			m_obj = null;
			m_cache = null;
			m_vc = null;
			// Leave this static alone.
			// m_subclasses = null;
			m_hostControl = null;

			m_isDisposed = true;

			// Keep this from being collected, since it got removed from the static.
			GC.KeepAlive(this);
		}

		#endregion IDisposable & Co. implementation

		#region IxCoreColleague implementation/

		public void Init(XCore.Mediator mediator, System.Xml.XmlNode configurationParameters)
		{
			CheckDisposed();

		}

		/// <summary>
		/// return an array of all of the objects which should
		/// 1) be queried when looking for someone to deliver a message to
		/// 2) be potential recipients of a broadcast
		/// </summary>
		/// <returns></returns>
		public XCore.IxCoreColleague[] GetMessageTargets()
		{
			CheckDisposed();

			if (m_additonalTarget != null)
				return new XCore.IxCoreColleague[] { m_additonalTarget, this };
			else
				return new XCore.IxCoreColleague[] { this };
		}

		/// <summary>
		/// Should not be called if disposed.
		/// </summary>
		public bool ShouldNotCall
		{
			get { return IsDisposed; }
		}

		public int Priority
		{
			get { return (int)ColleaguePriority.Low; }
		}

		#endregion IxCoreColleague implementation

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

		public virtual void LaunchGuiControl(Command command)
		{
			CheckDisposed();
			string guicontrol = command.GetParameter("guicontrol");
			string xpathToControl = String.Format("/window/controls/parameters/guicontrol[@id=\"{0}\"]", guicontrol);
			XmlNode xnControl = command.ConfigurationNode.SelectSingleNode(xpathToControl);
			if (xnControl != null)
			{
				using (IFwGuiControl dlg = DynamicLoader.CreateObject(xnControl.SelectSingleNode("dynamicloaderinfo")) as IFwGuiControl)
				{
					dlg.Init(m_mediator, xnControl.SelectSingleNode("parameters"), Object);
					dlg.Launch();
				}
			}
		}

		/// <summary>
		/// gives the guid of the object to use in the URL we construct when doing a jump
		/// </summary>
		public virtual Guid GuidForJumping(object commandObject)
		{
			return Object.Guid;
		}

		/// <summary>
		/// This method CALLED BY REFLECTION is required to make various right-click menu commands
		/// like Show Entry in Lexicon work in browse views. FWR-3695.
		/// </summary>
		/// <returns></returns>
		public virtual bool OnJumpToTool(object commandObject)
		{
			CheckDisposed();

			XCore.Command command = (XCore.Command)commandObject;
			string tool = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "tool");
			var guid = GuidForJumping(commandObject);
			m_mediator.PostMessage("FollowLink", new FwLinkArgs(tool, guid));
			return true;
		}

		/// <summary>
		/// called by the mediator to decide how/if a MenuItem or toolbar button should be displayed
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayJumpToTool(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();

			XCore.Command command = (XCore.Command)commandObject;
			string tool = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "tool");
			//string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
			//string toolChoice = m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_" + areaChoice, null);
			string toolChoice = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);
			if (!IsAcceptableContextToJump(toolChoice, tool))
			{
				display.Visible = display.Enabled = false;
				return true;
			}
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");


			int specifiedClsid = 0;
			var mdc = (IFwMetaDataCacheManaged)m_cache.DomainDataByFlid.MetaDataCache;
			if (mdc.ClassExists(className)) // otherwise is is a 'magic' class name treated specially in other OnDisplays.
				specifiedClsid = mdc.GetClassId(className);

			display.Visible = display.Enabled = ShouldDisplayMenuForClass(specifiedClsid, display);
			if (display.Enabled)
				command.TargetId = GuidForJumping(commandObject);
			return true;
		}

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
				// presently, "ActiveClerkSelectedObject" only gets established on Idle()
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
				obj = m_mediator.PropertyTable.GetValue("ActiveClerkSelectedObject", null) as ICmObject;
			}
			return obj;
		}

		protected virtual bool ShouldDisplayMenuForClass(int specifiedClsid, XCore.UIItemDisplayProperties display)
		{
			if (specifiedClsid == 0)
				return false; // a special magic class id, only enabled explicitly.
			if (this.Object.ClassID == specifiedClsid)
				return true;
			else
			{
				int baseClsid = m_cache.DomainDataByFlid.MetaDataCache.GetBaseClsId(Object.ClassID);
				if (baseClsid == specifiedClsid) //handle one level of subclassing
					return true;
				else
				{
					return false;
				}
			}
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
		/// <param name="mediator"></param>
		/// <param name="hostControl"></param>
		/// <param name="shouldDisposeThisWhenClosed">True, if the menu handler is to dispose of the CmObjectUi after menu closing</param>
		/// <returns></returns>
		public bool HandleRightClick(Mediator mediator, Control hostControl, bool shouldDisposeThisWhenClosed)
		{
			CheckDisposed();

			return HandleRightClick(mediator, hostControl, shouldDisposeThisWhenClosed, ContextMenuId);
		}

		/// <summary>
		/// Handle a right click by popping up the implied context menu.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="hostControl"></param>
		/// <param name="shouldDisposeThisWhenClosed">True, if the menu handler is to dispose of the CmObjectUi after menu closing</param>
		/// <param name="adjustMenu"></param>
		/// <returns></returns>
		public bool HandleRightClick(Mediator mediator, Control hostControl, bool shouldDisposeThisWhenClosed, Action<ContextMenuStrip> adjustMenu)
		{
			CheckDisposed();

			return HandleRightClick(mediator, hostControl, shouldDisposeThisWhenClosed, ContextMenuId, adjustMenu);
		}

		/// <summary>
		/// Given a populated choice group, mark the one that will be invoked by a ctrl-click.
		/// This method is typically used as the menuAdjuster argument in calling HandleRightClick.
		/// It's important that it marks the same menu item as selected by HandlCtrlClick.
		/// </summary>
		/// <param name="group"></param>
		public static void MarkCtrlClickItem(ContextMenuStrip menu)
		{
			foreach (var item in menu.Items)
			{
				var item1 = item as ToolStripItem;
				if (item1 == null || !(item1.Tag is CommandChoice) || !item1.Enabled)
					continue;
				var command = (CommandChoice) item1.Tag;
				if (command.Message != "JumpToTool")
					continue;

				item1.Text += FdoUiStrings.ksCtrlClick;
				return;
			}
		}

		/// <summary>
		/// Handle a control-click by invoking the first active JumpToTool menu item.
		/// Note that the item selected here should be the same one that is selected by Mark
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="hostControl"></param>
		/// <returns></returns>
		public bool HandleCtrlClick(Mediator mediator, Control hostControl)
		{
			Mediator = mediator;
			XWindow window = (XWindow)mediator.PropertyTable.GetValue("window");
			m_hostControl = hostControl;
			var group = window.GetChoiceGroupForMenu(ContextMenuId);
			// temporarily the CmObjectUi must function as a colleague that can implement commands.
			mediator.AddTemporaryColleague(this);
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
				this.Dispose();
			}
			return false;
		}

		private static bool IsCtrlClickItem(object item)
		{
			var command = item as CommandChoice;
			if (command == null || command.Message != "JumpToTool")
				return false;
			var displayProps = command.GetDisplayProperties();
			return (displayProps.Visible && displayProps.Enabled);
		}

		/// <summary>
		/// Handle the right click by popping up an explicit context menu id.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="hostControl"></param>
		/// <param name="shouldDisposeThisWhenClosed">True, if the menu handler is to dispose of the CmObjectUi after menu closing</param>
		/// <param name="sMenuId"></param>
		/// <returns></returns>
		public bool HandleRightClick(Mediator mediator, Control hostControl, bool shouldDisposeThisWhenClosed, string sMenuId)
		{
			return HandleRightClick(mediator, hostControl, shouldDisposeThisWhenClosed, sMenuId, null);
		}

		/// <summary>
		/// Handle the right click by popping up an explicit context menu id.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="hostControl"></param>
		/// <param name="shouldDisposeThisWhenClosed">True, if the menu handler is to dispose of the CmObjectUi after menu closing</param>
		/// <param name="sMenuId"></param>
		/// <returns></returns>
		public bool HandleRightClick(Mediator mediator, Control hostControl, bool shouldDisposeThisWhenClosed, string sMenuId, Action<ContextMenuStrip> adjustMenu)
		{
			CheckDisposed();

			Mediator = mediator;
			XWindow window = (XWindow)mediator.PropertyTable.GetValue("window");
			m_hostControl = hostControl;

			string sHostType = m_hostControl.GetType().Name;
			string sType = this.Object.GetType().Name;

			if (sHostType == "XmlBrowseView" && sType == "CmBaseAnnotation")
			{
				// Generally we don't want popups trying to manipulate the annotations as objects in browse views.
				// See e.g. LT-5156, 6534, 7160.
				// Indeed, since CmBaseAnnotation presents itself as a 'Problem Report', we don't want
				// to do it for any kind of annotation that couldn't be one!
				object clrk = m_mediator.PropertyTable.GetValue("ActiveClerk");
				string sClerkType = clrk != null ? clrk.GetType().Name : "";
				if (sClerkType == "OccurrencesOfSelectedUnit")
					return true;		// We don't want this either.  See LT-6101.
			}

			// TODO: The context menu needs to be filtered to remove inappropriate menu items.

			window.ShowContextMenu(sMenuId,
				new Point(Cursor.Position.X, Cursor.Position.Y),
				new TemporaryColleagueParameter(m_mediator, this, shouldDisposeThisWhenClosed),
				null, adjustMenu);
			// Using the sequencer here now causes problems with slices that allow
			// keyboard activity (cf. PhoneEnvReferenceView).
			// If a safe blocking mechanism can be found for the context menu, we can restore the original behavior
			// which will have this code do the setup and teardown work.
			//(hostControl as IReceiveSequentialMessages).Sequencer);

			return true;
		}
		#endregion

		#region Other methods

		/// <summary>
		/// Hack to "remove" the delete menu from the popup menu.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayDeleteSelectedItem(object commandObject, ref XCore.UIItemDisplayProperties display)
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
			display.Text = String.Format(display.Text, DisplayNameOfClass);
			return true;
		}

		public virtual string DisplayNameOfClass
		{
			get
			{
				CheckDisposed();

				if (Object is ICmPossibility)
				{
					var possibility = Object as ICmPossibility;
					return possibility.ItemTypeName(m_mediator.StringTbl);
				}
				string typeName = Object.GetType().Name;
				string className = m_mediator.StringTbl.GetString(typeName, "ClassNames");
				if (className == "*" + typeName + "*")
					className = typeName;

				string altName = null;
				IFsFeatureSystem featsys = Object.OwnerOfClass(FsFeatureSystemTags.kClassId) as IFsFeatureSystem;
				if (featsys != null)
				{
					if (featsys.OwningFlid == LangProjectTags.kflidPhFeatureSystem)
					{
						altName = m_mediator.StringTbl.GetString(className + "-Phonological", "AlternativeTypeNames");
						if (altName != "*" + className + "-Phonological*")
							return altName;
					}
				}
				switch (Object.OwningFlid)
				{
					case MoStemNameTags.kflidRegions:
						altName = m_mediator.StringTbl.GetString(className + "-MoStemName", "AlternativeTypeNames");
						if (altName != "*" + className + "-MoStemName*")
							return altName;
						break;
				}
				return className;
			}
		}

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
					using (CmObjectUi owner = CmObjectUi.MakeUi(m_cache, m_obj.Owner.Hvo))
					{
						owner.Mediator = m_mediator;
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
				object command = this;
				if (m_command != null)
					command = m_command;
				m_mediator.SendMessage("DeleteRecord", command);
			}
			else
			{
				Form mainWindow = (Form)m_mediator.PropertyTable.GetValue("window");
				using (new WaitCursor(mainWindow))
				{
					using (ConfirmDeleteObjectDlg dlg = new ConfirmDeleteObjectDlg(m_mediator.HelpTopicProvider))
					{
						dlg.SetDlgInfo(this, m_cache, Mediator);
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
			if (m_obj is ICmPicture)
			{
				file = ((ICmPicture) m_obj).PictureFileRA;
			}
			else if (m_obj is ICmMedia)
			{
				file = ((ICmMedia) m_obj).MediaFileRA;
			}
			ConsiderDeletingRelatedFile(file, m_mediator);
		}

		public static void ConsiderDeletingRelatedFile(ICmFile file, Mediator mediator)
		{
			if (file == null)
				return;
			var refs = file.ReferringObjects;
			if (refs.Count > 1)
				return; // exactly one if only this CmPicture uses it.
			var path = file.InternalPath;
			if (Path.IsPathRooted(path))
				return; // don't delete external file
			string msg = String.Format(FdoUiStrings.ksDeleteFileAlso, path);
			if (MessageBox.Show(Form.ActiveForm, msg, FdoUiStrings.ksDeleteFileCaption, MessageBoxButtons.YesNo,
				MessageBoxIcon.Question)
				!= DialogResult.Yes)
			{
				return;
			}
			if (mediator != null)
			{
				var app = mediator.PropertyTable.GetValue("App") as FwApp;
				if (app != null)
					app.PictureHolder.ReleasePicture(file.AbsoluteInternalPath);
			}
			string fileToDelete = file.AbsoluteInternalPath;
			// I'm not sure why, but if we try to delete it right away, we typically get a failure,
			// with an exception indicating that something is using the file, despite the code above that
			// tries to make our picture cache let go of it.
			// However, waiting until idle seems to solve the problem.
			mediator.IdleQueue.Add(IdleQueuePriority.Low, obj =>
				{
					try
					{
						File.Delete(fileToDelete);
					}
					catch (IOException)
					{
						// If we can't actually delete the file for some reason, don't bother the user complaining.
					}
					return true; // task is complete, don't try again.
				});
			file.Delete();
		}

		protected virtual void ReallyDeleteUnderlyingObject()
		{
			Logger.WriteEvent("Deleting '" + this.Object.ShortName + "'...");
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(FdoUiStrings.ksUndoDelete, FdoUiStrings.ksRedoDelete,
				m_cache.ActionHandlerAccessor, () =>
				{
					DoRelatedCleanupForDeleteObject();
					Object.Cache.DomainDataByFlid.DeleteObj(Object.Hvo);
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

			Form mainWindow = (Form)m_mediator.PropertyTable.GetValue("window");
			using (new WaitCursor(mainWindow))
			{
				using (MergeObjectDlg dlg = new MergeObjectDlg(m_mediator.HelpTopicProvider))
				{
					WindowParams wp = new WindowParams();
					List<DummyCmObject> mergeCandidates = new List<DummyCmObject>();
					string guiControl, helpTopic;
					DummyCmObject dObj = GetMergeinfo(wp, mergeCandidates, out guiControl, out helpTopic);
					mergeCandidates.Sort();
					dlg.SetDlgInfo(m_cache, m_mediator, wp, dObj, mergeCandidates, guiControl, helpTopic);
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
		/// <param name="fLoseNoTextData"></param>
		protected virtual void ReallyMergeUnderlyingObject(int survivorHvo, bool fLoseNoTextData)
		{
			ICmObject survivor = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(survivorHvo);
			Logger.WriteEvent("Merging '" + this.Object.ShortName + "' into '" + survivor.ShortName + "'.");
			IActionHandler ah = m_cache.ServiceLocator.GetInstance<IActionHandler>();
			UndoableUnitOfWorkHelper.Do(FdoUiStrings.ksUndoMerge, FdoUiStrings.ksRedoMerge, ah, () =>
			{
				survivor.MergeObject(this.Object, fLoseNoTextData);
			});
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

			Form mainWindow = (Form)m_mediator.PropertyTable.GetValue("window");
			MessageBox.Show(mainWindow, FdoUiStrings.ksCannotMoveObjectToCopy, FdoUiStrings.ksBUG);
		}

		/// <summary>
		/// Get a string suitable for use in the left panel of the LexText status bar.
		/// It will show the created and modified dates, if the object has them.
		/// </summary>
		public string ToStatusBar()
		{
			CheckDisposed();

			if (!Object.IsValidObject)
				return FdoUiStrings.ksDeletedObject;
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
		static public uint RGB(Color c)
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
		static public uint RGB(int r, int g, int b)
		{
			return ((uint)(((byte)(r) | ((short)((byte)(g)) << 8)) | (((short)(byte)(b)) << 16)));

		}

		/// <summary>
		///
		/// </summary>
		/// <param name="singlePropertySequenceValue"></param>
		/// <param name="cacheForCheckingValidity">null, if you don't care about checking the validity of the items in singlePropertySequenceValue,
		/// otherwise, pass in a cache to check validity.</param>
		/// <param name="expectedClassId">if you pass a cache, you can also use this too make sure the object matches an expected class,
		/// otherwise it just checks that the object exists in the database (or is a valid virtual object)</param>
		/// <returns></returns>
		static public List<int> ParseSinglePropertySequenceValueIntoHvos(string singlePropertySequenceValue,
			FdoCache cacheForCheckingValidity, int expectedClassId)
		{
			List<int> hvos = new List<int>();
			if (String.IsNullOrEmpty(singlePropertySequenceValue))
				return hvos;
			FdoCache cache = cacheForCheckingValidity;
			foreach (string sHvo in ChoiceGroup.DecodeSinglePropertySequenceValue(singlePropertySequenceValue))
			{
				int hvo;
				if (Int32.TryParse(sHvo, out hvo))
				{
					if (cache != null)
					{
						try
						{
							var obj = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
						}
						catch
						{
							continue;
						}
					}
					hvos.Add(hvo);
				}
			}
			return hvos;
		}

		#endregion Other methods

		#region Embedded View Constructor classes

		public class CmObjectVc : FwBaseVc
		{
			public CmObjectVc(FdoCache cache)
			{
				Cache = cache;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				ISilDataAccess sda = vwenv.DataAccess;
				int wsUi = sda.WritingSystemFactory.UserWs;
				ITsStrFactory tsf = m_cache.TsStrFactory;
				var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
				switch (frag)
				{
					case (int)VcFrags.kfragHeadWord:
						var le = co as ILexEntry;
						if (le != null)
							vwenv.AddString(le.HeadWord);
						else
							vwenv.AddString(tsf.MakeString(co.ShortName, wsUi));
						break;
					case (int)VcFrags.kfragShortName:
						vwenv.AddString(tsf.MakeString(co.ShortName, wsUi));
						break;
					case (int)VcFrags.kfragName:
					default:
						vwenv.AddString(tsf.MakeString(co.ToString(), wsUi));
						break;
				}
			}
		}
		/// <summary>
		/// Special VC for classes that should display in the default analysis writing system.
		/// </summary>
		public class CmAnalObjectVc : FwBaseVc
		{
			public CmAnalObjectVc(FdoCache cache)
				: base(cache.DefaultAnalWs)
			{
				Cache = cache;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				if (hvo == 0)
					return;

				int wsAnal = DefaultWs;
				ITsStrFactory tsf = m_cache.TsStrFactory;
				ICmObject co;
				switch (frag)
				{
					case (int)VcFrags.kfragHeadWord:
						co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
						var le = co as ILexEntry;
						if (le != null)
							vwenv.AddString(le.HeadWord);
						else
							vwenv.AddString(tsf.MakeString(co.ShortName, wsAnal));
						break;
					case (int)VcFrags.kfragShortName:
						co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
						vwenv.AddString(tsf.MakeString(co.ShortName, wsAnal));
						break;
					case (int)VcFrags.kfragPosAbbrAnalysis:
						vwenv.AddStringAltMember(CmPossibilityTags.kflidAbbreviation, wsAnal, this);
						break;
					case (int)VcFrags.kfragName:
					default:
						co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
						vwenv.AddString(tsf.MakeString(co.ToString(), wsAnal));
						break;
				}
			}
		}

		/// <summary>
		/// Special VC for classes that have name flid. It is a MultiString property, and the default user
		/// WS should be used to display it.
		/// </summary>
		public class CmNamedObjVc : CmObjectVc
		{
			protected int m_flidName;

			public CmNamedObjVc(FdoCache cache, int flidName)
				: base(cache)
			{
				m_flidName = flidName;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				switch (frag)
				{
					case (int)VcFrags.kfragName: // Use the name for both name and short name.
					case (int)VcFrags.kfragShortName:
					default:
						int wsUi = vwenv.DataAccess.WritingSystemFactory.UserWs;
						vwenv.AddStringAltMember(m_flidName, wsUi, this);
						break;
				}
			}
		}
		/// <summary>
		/// Special VC for classes that have name and abbreviation, both displayed in UI WS.
		/// </summary>
		public class CmNameAbbrObjVc : CmNamedObjVc
		{
			protected int m_flidAbbr;

			public CmNameAbbrObjVc(FdoCache cache, int flidName, int flidAbbr)
				: base(cache, flidName)
			{
				m_flidAbbr = flidAbbr;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				switch (frag)
				{
					case (int)VcFrags.kfragShortName:
						int wsUi = vwenv.DataAccess.WritingSystemFactory.UserWs;
						vwenv.AddStringAltMember(m_flidAbbr, wsUi, this);
						break;
					default:
						base.Display(vwenv, hvo, frag);
						break;
				}
			}
		}
		/// <summary>
		/// Special VC for classes that have a reference to a CmPossibility whose name/abbr should be
		/// used as the name/abbr for this.
		/// </summary>
		public class CmPossRefVc : CmObjectUi.CmObjectVc
		{
			protected int m_flidRef; // flid that refers to the CmPossibility

			public CmPossRefVc(FdoCache cache, int flidRef)
				: base(cache)
			{
				m_flidRef = flidRef;
			}

			// If the expected reference property is null, insert "??" and return false;
			// otherwise return true.
			private bool HandleObjMissing(IVwEnv vwenv, int hvo)
			{
				if (m_cache.DomainDataByFlid.get_ObjectProp(hvo, m_flidRef) == 0)
				{
					int wsUi = vwenv.DataAccess.WritingSystemFactory.UserWs;
					ITsStrFactory tsf = m_cache.TsStrFactory;
					vwenv.AddString(tsf.MakeString(FdoUiStrings.ksQuestions, wsUi));	// was "??", not "???"
					vwenv.NoteDependency(new int[] { hvo }, new int[] { m_flidRef }, 1);
					return false;
				}
				return true;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				const int kfragAbbr = 17; // arbitrary const from reserved range.
				const int kfragName = 18;
				switch (frag)
				{
					case (int)VcFrags.kfragInterlinearAbbr:
					case (int)VcFrags.kfragInterlinearName:  // abbr is probably more appropriate in interlinear.
					case (int)VcFrags.kfragShortName:
						if (HandleObjMissing(vwenv, hvo))
							vwenv.AddObjProp(m_flidRef, this, kfragAbbr);
						break;
					case kfragAbbr:
						vwenv.AddStringAltMember(CmPossibilityTags.kflidAbbreviation,
							m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle,
							this);
						break;
					case (int)VcFrags.kfragName:
						if (HandleObjMissing(vwenv, hvo))
							vwenv.AddObjProp(m_flidRef, this, kfragName);
						break;
					default:
					case kfragName:
						vwenv.AddStringAltMember(CmPossibilityTags.kflidName,
							m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle,
							this);
						break;
				}
			}
		}

		#endregion Embedded View Constructor classes
	}

	/// <summary>
	/// Special VC for classes that should display in the default vernacular writing system.
	/// </summary>
	public class CmVernObjectVc : FwBaseVc
	{
		public CmVernObjectVc(FdoCache cache)
		{
			Cache = cache;
		}

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			int wsVern = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			ITsStrFactory tsf = m_cache.TsStrFactory;
			var co = m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvo);
			switch (frag)
			{
				case (int)VcFrags.kfragHeadWord:
					var le = co as ILexEntry;
					if (le != null)
						vwenv.AddString(le.HeadWord);
					else
						vwenv.AddString(tsf.MakeString(co.ShortName, wsVern));
					break;
				case (int)VcFrags.kfragShortName:
					vwenv.AddString(tsf.MakeString(co.ShortName, wsVern));
					break;
				case (int)VcFrags.kfragName:
				default:
					vwenv.AddString(tsf.MakeString(co.ToString(), wsVern));
					break;
			}
		}
	}

	/// <summary>
	/// Special UI behaviors for the CmPossibility class.
	/// </summary>
	public class CmPossibilityUi : CmObjectUi
	{
		/// <summary>
		/// Create one. Argument must be a CmPossibility.
		/// Review JohnH (JohnT): should we declare the argument to be CmPossibility?
		/// Note that declaring it to be forces us to just do a cast in every case of MakeUi, which is
		/// passed an obj anyway.
		/// </summary>
		/// <param name="obj"></param>
		public CmPossibilityUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is ICmPossibility);
		}

		internal CmPossibilityUi() { }

		/// <summary>
		/// Gets a special VC that knows to use the abbr for the shortname, etc.
		/// </summary>
		public override IVwViewConstructor Vc
		{
			get
			{
				CheckDisposed();

				if (m_vc == null)
				{
					m_vc = new CmNameAbbrObjVc(m_cache, CmPossibilityTags.kflidName,
						CmPossibilityTags.kflidAbbreviation);
				}
				return base.Vc;
			}
		}

		public override bool OnDisplayJumpToTool(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();

			var command = (Command)commandObject;
			string className = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");

			int specifiedClsid = 0;
			// There are some special commands with dummy class names (WordPartOfSpeech is one).
			// I (JohnT) don't know why we did that, but apparently everything else works for those
			// commands if we just leave specifiedClsid zero (code takes the second branch of the if below,
			// since CmObject does not inherit from CmPossibility, and depending on the override
			// usually it isn't displayed, but there may be some override that does.
			if ((m_cache.DomainDataByFlid.MetaDataCache as IFwMetaDataCacheManaged).ClassExists(className))
				specifiedClsid = m_cache.DomainDataByFlid.MetaDataCache.GetClassId(className);

			var cp = Object as ICmPossibility;
			var owningList = cp.OwningList;
			if (m_cache.ClassIsOrInheritsFrom(specifiedClsid, CmPossibilityTags.kClassId) ||
				specifiedClsid == LexEntryTypeTags.kClassId)	// these appear in 2 separate lists.
			{
				IFwMetaDataCache mdc = m_cache.DomainDataByFlid.MetaDataCache;
				// make sure our object matches the CmPossibilityList flid name.
				int owningFlid = owningList.OwningFlid;
				string owningFieldName = owningFlid == 0 ? "" : mdc.GetFieldName(owningFlid);
				string owningClassName = owningFlid == 0 ? "" : mdc.GetOwnClsName(owningFlid);
				string commandListOwnerName = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "ownerClass");
				string commandListFieldName = XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "ownerField");
				display.Visible = display.Enabled = (commandListFieldName == owningFieldName && commandListOwnerName == owningClassName);
			}
			else
			{
				// we should have a specific class name to match on.
				display.Visible = display.Enabled = ShouldDisplayMenuForClass(specifiedClsid, display);
			}

			// try to insert the name of list into our display.Text for commands with label="{0}"
			FormatDisplayTextWithListName(m_cache, Mediator, owningList, ref display);
			if (display.Enabled)
				command.TargetId = GuidForJumping(commandObject);
			return true;
		}

		public static string FormatDisplayTextWithListName(FdoCache cache, XCore.Mediator mediator,
			ICmPossibilityList pssl, ref UIItemDisplayProperties display)
		{
			string listName;
			if (pssl.Owner != null)
				listName = cache.DomainDataByFlid.MetaDataCache.GetFieldName((int)pssl.OwningFlid);
			else
				listName = pssl.Name.BestAnalysisVernacularAlternative.Text;
			string itemTypeName = pssl.ItemsTypeName(mediator.StringTbl);
			if (itemTypeName != "*" + listName + "*")
			{
				string formattedText = String.Format(display.Text, itemTypeName);
				display.Text = formattedText;
			}
			return display.Text;
		}

		/// <summary>
		/// Check whether it is OK to add a possibility to the specified item. If not, report the
		/// problem to the user and return true.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="hvoItem"></param>
		/// <returns></returns>
		static bool CheckAndReportProblemAddingSubitem(FdoCache cache, int hvoItem)
		{
			var possItem = cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvoItem);
			if (possItem != null)
			{
				var rootPoss = possItem.MainPossibility;
				var hvoRootItem = rootPoss.Hvo;
				var hvoPossList = rootPoss.OwningList.Hvo;

				// If we get here hvoPossList is a possibility list and hvoRootItem is a top level item in that list
				// and possItem is, or is a subpossibility of, that top level item.

				// 1. Check to see if hvoRootItem is a chart template containing our target.
				// If so, hvoPossList is owned in the chart templates property.
				if (CheckAndReportBadDiscourseTemplateAdd(cache, possItem.Hvo, hvoRootItem, hvoPossList))
					return true;

				// 2. Check to see if hvoRootItem is a TextMarkup TagList containing our target (i.e. a Tag type).
				// If so, hvoPossList is owned in the text markup tags property.
				if (CheckAndReportBadTagListAdd(cache, possItem.Hvo, hvoRootItem, hvoPossList))
					return true;
			}
			return false; // not detecting problems with moving other kinds of things.
		}

		private static bool CheckAndReportBadTagListAdd(FdoCache cache, int hvoItem, int hvoRootItem, int hvoPossList)
		{
			if (cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoPossList).OwningFlid != LangProjectTags.kflidTextMarkupTags)
				return false; // some other list we don't care about.

			// Confirm the two-level rule.
			if (hvoItem != hvoRootItem)
			{
				MessageBox.Show(FdoUiStrings.ksMarkupTagsTooDeep,
								FdoUiStrings.ksHierarchyLimit, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			return false;
		}

		private static bool CheckAndReportBadDiscourseTemplateAdd(FdoCache cache, int hvoItem, int hvoRootItem, int hvoList)
		{
			if (cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoList).OwningFlid != DsDiscourseDataTags.kflidConstChartTempl)
				return false; // some other list we don't care about.
			// We can't turn a column into a group if it's in use.
			var col = cache.ServiceLocator.GetInstance<ICmPossibilityRepository>().GetObject(hvoItem);
			// If the item doesn't already have children, we can only add them if it isn't already in use
			// as a column: we don't want to change a column into a group. Thus, if there are no
			// children, we generally call the same routine as when deleting.
			// However, that routine has a special case to prevent deletion of the default template even
			// if NOT in use...and we must not prevent adding to that when it is empty! Indeed any
			// empty CHART can always be added to, so only if col's owner is a CmPossibility (it's not a root
			// item in the templates list) do we need to check for it being in use.
			if (col.SubPossibilitiesOS.Count == 0 && col.Owner is ICmPossibility && col.CheckAndReportProtectedChartColumn())
				return true;
			// Finally, we have to confirm the three-level rule.
			var owner = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoItem).Owner;
			if (hvoItem != hvoRootItem && owner != null && owner.Hvo != hvoRootItem)
			{
				MessageBox.Show(FdoUiStrings.ksTemplateTooDeep,
								FdoUiStrings.ksHierarchyLimit, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			return false;
		}

		public new static CmObjectUi CreateNewUiObject(Mediator mediator, int classId, int hvoOwner,
			int flid, int insertionPosition)
		{
			FdoCache cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			if (CheckAndReportProblemAddingSubitem(cache, hvoOwner))
				return null;
			return DefaultCreateNewUiObject(classId, hvoOwner, flid, insertionPosition, cache);
		}
	}


	/// <summary>
	/// Special UI behaviors for the MoMorphSynAnalysis class.
	/// </summary>
	public class MoMorphSynAnalysisUi : CmObjectUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="obj"></param>
		public MoMorphSynAnalysisUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IMoMorphSynAnalysis);
		}

		internal MoMorphSynAnalysisUi()
		{
		}

		protected override bool ShouldDisplayMenuForClass(int specifiedClsid,
			XCore.UIItemDisplayProperties display)
		{
			return (PartOfSpeechTags.kClassId == specifiedClsid) &&
				(GuidForJumping(null) != Guid.Empty);
		}

		//protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates)
		//{
		//	wp.m_title = FdoUiStrings.ksMergeAnalysis;
		//	wp.m_label = FdoUiStrings.ksAnalyses;
		//	int defAnalWs = m_cache.ServiceLocator.GetInstance<ILgWritingSystemRepository>().GetDefaultAnalysisWritingSystem().Hvo;

		//	var le = Object.Owner as ILexEntry;
		//	foreach (var msa in le.MorphoSyntaxAnalysesOC)
		//	{
		//		if (msa != Object
		//			&& msa.ClassID == Object.ClassID)
		//		{
		//			mergeCandidates.Add(
		//				new DummyCmObject(
		//					msa.Hvo,
		//					msa.ShortName,
		//					defAnalWs));
		//		}
		//	}
		//	return new DummyCmObject(m_hvo, Object.ShortName, defAnalWs);
		//}

		/// <summary>
		/// Gets a special VC that knows to display the name or abbr of the PartOfSpeech.
		/// </summary>
		public override IVwViewConstructor Vc
		{
			get
			{
				CheckDisposed();

				if (m_vc == null)
					m_vc = new MsaVc(m_cache);
				return base.Vc;
			}
		}

		/// <summary>
		/// Special VC for MSAs. These have the InterlinearName method.
		/// Enhance JohnT: it would be better to actually build a view that shows what we want,
		/// so that all the proper dependencies could be noted.  But the algorithms are complex
		/// and involve backreferences.
		/// Todo: Finish reworking this into MsaVc; clean up stuff related to interlinearName
		/// above.
		/// </summary>
		public class MsaVc : CmAnalObjectVc
		{
			public MsaVc(FdoCache cache)
				: base(cache)
			{
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				int wsAnal = DefaultWs;

				ITsStrFactory tsf = m_cache.TsStrFactory;
				var msa = m_cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().GetObject(hvo);

				switch (frag)
				{
					case (int)VcFrags.kfragFullMSAInterlinearname:
						// not editable
						vwenv.OpenParagraph();
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
							(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
						vwenv.AddString(msa.LongNameTs);
						vwenv.CloseParagraph();
						break;
					case (int)VcFrags.kfragInterlinearName:
						// not editable
						//vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						//	(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);

						// InterlinNameTss would need to be implemented, so we can get tss
						// string based upon the specified wsAnal
						//vwenv.AddString(msa.InterlinNameTSS(wsAnal));
						break;
					case (int)VcFrags.kfragInterlinearAbbr:
						// not editable
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
							(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
						vwenv.AddString(msa.InterlinAbbrTSS(wsAnal));
						break;
					default:
						base.Display(vwenv, hvo, frag);
						break;
				}
			}
		}
	}

	/// <summary>
	/// Special UI behaviors for the MoStemMsa class.
	/// </summary>
	public class MoStemMsaUi : MoMorphSynAnalysisUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="obj"></param>
		public MoStemMsaUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IMoStemMsa);
		}

		internal MoStemMsaUi()
		{
		}

		/// <summary>
		/// gives the hvo of the object to use in the URL reconstruct when doing a jump
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public override Guid GuidForJumping(object commandObject)
		{
			IMoStemMsa msa = Object as IMoStemMsa;
			if (msa.PartOfSpeechRA == null)
				return Guid.Empty;
			else
				return msa.PartOfSpeechRA.Guid;
		}
	}
	/// <summary>
	/// UI functions for MoMorphSynAnalysis.
	/// </summary>
	public class MoInflAffMsaUi : MoMorphSynAnalysisUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="obj"></param>
		public MoInflAffMsaUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IMoInflAffMsa);
		}

		internal MoInflAffMsaUi()
		{
		}

		/// <summary>
		/// gives the hvo of the object to use in the URL we construct when doing a jump
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public override Guid GuidForJumping(object commandObject)
		{
			IMoInflAffMsa msa = Object as IMoInflAffMsa;
			if (msa.PartOfSpeechRA == null)
				return Guid.Empty;
			else
				return msa.PartOfSpeechRA.Guid;
		}
	}

	/// <summary>
	/// UI functions for MoMorphSynAnalysis.
	/// </summary>
	public class MoDerivAffMsaUi : MoMorphSynAnalysisUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="obj"></param>
		public MoDerivAffMsaUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IMoDerivAffMsa);
		}

		internal MoDerivAffMsaUi()
		{
		}

		/// <summary>
		/// gives the hvo of the object to use in the URL we construct when doing a jump
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public override Guid GuidForJumping(object commandObject)
		{
			//todo: for now, this will just always send us to the "from" part of speech
			//	we could get at the "to" part of speech using a separate menu command
			//	or else, if this ends up being drawn by a view constructor rather than a string which combines both are from and the to,
			// then we will know which item of the user clicked on and can open the appropriate one.
			IMoDerivAffMsa msa = Object as IMoDerivAffMsa;
			if (msa.FromPartOfSpeechRA == null)
				return Guid.Empty;
			else
				return msa.FromPartOfSpeechRA.Guid;
		}
	}

	/// <summary>
	/// UI functions for MoMorphSynAnalysis.
	/// </summary>
	public class LexSenseUi : CmObjectUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="obj"></param>
		public LexSenseUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is ILexSense);
		}

		internal LexSenseUi() { }

		/// <summary>
		/// gives the hvo of the object to use in the URL we construct when doing a jump
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public override Guid GuidForJumping(object commandObject)
		{
			XCore.Command command = (XCore.Command)commandObject;
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (className == "LexSense")
				return Object.Guid;
			ICmObject cmo = GetSelfOrParentOfClass(Object, LexEntryTags.kClassId);
			return (cmo == null) ? Guid.Empty : cmo.Guid;
		}

		/// <summary>
		/// disable/hide delete selected item for LexSenses (eg. since we don't want them to delete all senses
		/// from its owning entry.)
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public override bool OnDisplayDeleteSelectedItem(object commandObject, ref UIItemDisplayProperties display)
		{
			display.Visible = false;
			display.Enabled = false;
			display.Text = String.Format(display.Text, DisplayNameOfClass);
			return true;
		}

		protected override bool ShouldDisplayMenuForClass(int specifiedClsid, XCore.UIItemDisplayProperties display)
		{
			//			Debug.WriteLine("LexSenseUi:"+display.Text+": "+ (LexEntry.kclsidLexEntry == specifiedClsid));
			return LexEntryTags.kClassId == specifiedClsid || LexSenseTags.kClassId == specifiedClsid;
		}

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out string guiControl, out string helpTopic)
		{
			wp.m_title = FdoUiStrings.ksMergeSense;
			wp.m_label = FdoUiStrings.ksSenses;
			int defAnalWs = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;

			var sense = Object as ILexSense;
			var le = sense.Entry;
			// Exclude subsenses of the chosen sense.  See LT-6107.
			List<int> rghvoExclude = new List<int>();
			foreach (var ls in sense.AllSenses)
				rghvoExclude.Add(ls.Hvo);
			foreach (var senseInner in le.AllSenses)
			{
				if (senseInner != Object && !rghvoExclude.Contains(senseInner.Hvo))
				{
					// Make sure we get the actual WS used (best analysis would be the
					// descriptive term) for the ShortName.  See FWR-2812.
					ITsString tssName = senseInner.ShortNameTSS;
					mergeCandidates.Add(
						new DummyCmObject(
						senseInner.Hvo,
						tssName.Text,
						TsStringUtils.GetWsAtOffset(tssName, 0)));
				}
			}
			guiControl = "MergeSenseList";
			helpTopic = "khtpMergeSense";
			ITsString tss = Object.ShortNameTSS;
			return new DummyCmObject(m_hvo, tss.Text, TsStringUtils.GetWsAtOffset(tss, 0));
		}

		public override void MoveUnderlyingObjectToCopyOfOwner()
		{
			CheckDisposed();

			var obj = Object.Owner;
			int clid = obj.ClassID;
			while (clid != LexEntryTags.kClassId)
			{
				obj = obj.Owner;
				clid = obj.ClassID;
			}
			var le = obj as ILexEntry;
			le.MoveSenseToCopy(Object as ILexSense);
		}
		/// <summary>
		/// When inserting a LexSense, copy the MSA from the one we are inserting after, or the
		/// first one.  If this is the first one, we may need to create an MSA if the owning entry
		/// does not have an appropriate one.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="classId"></param>
		/// <param name="hvoOwner"></param>
		/// <param name="flid"></param>
		/// <param name="insertionPosition"></param>
		/// <returns></returns>
		public new static LexSenseUi CreateNewUiObject(Mediator mediator, int classId, int hvoOwner, int flid, int insertionPosition)
		{
			LexSenseUi result = null;
			FdoCache cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			UndoableUnitOfWorkHelper.Do(FdoUiStrings.ksUndoInsertSense, FdoUiStrings.ksRedoInsertSense,
				cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					ICmObject owner = null;
					int hvoMsa = 0;
					int chvo = cache.DomainDataByFlid.get_VecSize(hvoOwner, flid);
					if (chvo == 0)
					{
						// See if we're inserting a subsense. If so copy from parent.
						owner = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoOwner);
						if (owner is ILexSense)
						{
							var ls = owner as ILexSense;
							hvoMsa = GetSafeHvoMsa(cache, ls);
						}
						else if (owner is ILexEntry)
						{
							// If we don't get the MSA here, trouble ensues.  See LT-5411.
							hvoMsa = (owner as ILexEntry).FindOrCreateDefaultMsa().Hvo;
						}
					}
					else
					{
						int copyFrom = insertionPosition - 1;
						if (copyFrom < 0)
							copyFrom = 0;
						if (copyFrom < chvo)
						{
							var ls = cache.ServiceLocator.GetInstance<ILexSenseRepository>().GetObject(cache.DomainDataByFlid.get_VecItem(hvoOwner, flid, copyFrom));
							hvoMsa = GetSafeHvoMsa(cache, ls);
						}
					}
					owner = cache.ServiceLocator.GetInstance<ICmObjectRepository>().GetObject(hvoOwner);
					var newSense = cache.ServiceLocator.GetInstance<ILexSenseFactory>().Create();
					if (owner is ILexSense)
					{
						(owner as ILexSense).SensesOS.Insert(insertionPosition, newSense);
					}
					else
					{
						((ILexEntry)owner).SensesOS.Insert(insertionPosition, newSense);
					}

					if (hvoMsa != 0)
						newSense.MorphoSyntaxAnalysisRA = cache.ServiceLocator.GetInstance<IMoMorphSynAnalysisRepository>().GetObject(hvoMsa);
					result = new LexSenseUi(newSense);
				});
			return result;
		}

		/// <summary>
		/// This method will get an hvo for the MSA which the senses MorphoSyntaxAnalysisRA points to.
		/// If it is null it will try and find an appropriate one in the owning Entries list, if that fails it will make one and put it there.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="ls">LexSense whose MSA we will use/change</param>
		/// <param name="hvoMsa"></param>
		/// <returns></returns>
		private static int GetSafeHvoMsa(FdoCache cache, ILexSense ls)
		{
			if (ls.MorphoSyntaxAnalysisRA != null)
				return ls.MorphoSyntaxAnalysisRA.Hvo; //situation normal, return

			//Situation not normal.
			int hvoMsa;
			var isAffixType = ls.Entry.PrimaryMorphType.IsAffixType;
			foreach(var msa in ls.Entry.MorphoSyntaxAnalysesOC) //go through each MSA in the Entry list looking for one with an unknown category
			{
				if(!isAffixType && msa is IMoStemMsa && (msa as IMoStemMsa).PartOfSpeechRA == null)
				{
					ls.MorphoSyntaxAnalysisRA = msa;
				}
				else if (msa is IMoUnclassifiedAffixMsa && (msa as IMoUnclassifiedAffixMsa).PartOfSpeechRA == null)
				{
					ls.MorphoSyntaxAnalysisRA = msa;
				}
			}
			if(ls.MorphoSyntaxAnalysisRA == null) //if we didn't find an appropriately unspecific MSA in the list add one
			{
				IMoMorphSynAnalysis item;
				if(isAffixType)
				{
					var factory = cache.ServiceLocator.GetInstance<IMoUnclassifiedAffixMsaFactory>();
					item = factory.Create();
				}
				else
				{
					var factory = cache.ServiceLocator.GetInstance<IMoStemMsaFactory>();
					item = factory.Create();
				}
				ls.Entry.MorphoSyntaxAnalysesOC.Add(item);
				ls.MorphoSyntaxAnalysisRA = item;
				hvoMsa = item.Hvo;
			}
			else
			{
				hvoMsa = ls.MorphoSyntaxAnalysisRA.Hvo;
			}
			return hvoMsa;
		}
	}

	/// <summary>
	/// Handling reference collections is rather minimal at the moment, basically allowing a
	/// different context menu to be used.
	/// </summary>
	public class ReferenceCollectionUi : VectorReferenceUi
	{
		public ReferenceCollectionUi(FdoCache cache, ICmObject rootObj, int referenceFlid, int targetHvo) :
			base(cache, rootObj, referenceFlid, targetHvo)
		{
			Debug.Assert(m_iType == CellarPropertyType.ReferenceCollection);
		}

		public override string ContextMenuId
		{
			get
			{
				CheckDisposed();

				int clidDst = m_cache.DomainDataByFlid.MetaDataCache.GetDstClsId((int)m_flid);
				switch ((int)clidDst)
				{
					case PhEnvironmentTags.kClassId:
						return "mnuEnvReferenceChoices";
					default:
						return "mnuReferenceChoices";
				}
			}
		}
	}

	/// <summary>
	/// Our own minimal implementation of a reference sequence, since we can't just get what
	/// we want from FDO's internal secret implementation of IFdoReferenceSequence.
	/// </summary>
	internal class FdoRefSeq
	{
		FdoCache m_cache;
		int m_hvo;
		int m_flid;

		internal FdoRefSeq(FdoCache cache, int hvo, int flid)
		{
			m_cache = cache;
			m_hvo = hvo;
			m_flid = flid;
		}

		internal int Count
		{
			get
			{
				return m_cache.DomainDataByFlid.get_VecSize(m_hvo, m_flid);
			}
		}

		internal int[] ToHvoArray()
		{
			int chvo = Count;
			using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvo))
			{
				m_cache.DomainDataByFlid.VecProp(m_hvo, m_flid, chvo, out chvo, arrayPtr);
				return MarshalEx.NativeToArray<int>(arrayPtr, chvo);
			}
		}

		internal void RemoveAt(int ihvo)
		{
			m_cache.DomainDataByFlid.Replace(m_hvo, m_flid, ihvo, ihvo + 1, null, 0);
		}

		internal void Insert(int ihvo, int hvo)
		{
			m_cache.DomainDataByFlid.Replace(m_hvo, m_flid, ihvo, ihvo, new int[] { hvo }, 1);
		}
	}

	/// <summary>
	/// Currently only LexReferenceSequenceView displays a full sequence for lexical relations sequence.
	/// Otherwise we could also manufacture ReferenceSequenceUi from ReferenceBaseUi.MakeUi().
	/// But since only LexReferenceSequenceView (e.g. Calendar) is handling changing the sequence of items
	/// through the context menu, we'll wait till we really need it to come up with a solution that can
	/// "exclude self" from the list in moving calculations.
	/// </summary>
	public class ReferenceSequenceUi : VectorReferenceUi
	{
		FdoRefSeq m_fdoRS = null;

		public ReferenceSequenceUi(FdoCache cache, ICmObject rootObj, int referenceFlid, int targetHvo)
			: base(cache, rootObj, referenceFlid, targetHvo)
		{
			Debug.Assert(m_iType == CellarPropertyType.ReferenceSequence);
			m_fdoRS = new FdoRefSeq(m_cache, m_hvo, m_flid);
			m_iCurrent = ComputeTargetVectorIndex();
		}

		protected int ComputeTargetVectorIndex()
		{
			Debug.Assert(m_fdoRS != null);
			Debug.Assert(m_hvoTarget > 0);
			if (m_fdoRS == null || m_hvoTarget <= 0)
				return -1;
			int[] hvos = m_fdoRS.ToHvoArray();
			for (int i = 0; i < hvos.Length; i++)
			{
				if (hvos[i] == m_hvoTarget)
					return i;
			}
			return -1;
		}

		public override bool OnDisplayMoveTargetUpInSequence(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (m_iCurrent < 0 || m_fdoRS == null || m_fdoRS.Count == 0)
			{
				display.Visible = display.Enabled = false;
				return true;
			}

			if ((m_iCurrent + 1) == m_fdoRS.Count)
			{
				display.Visible = true;
				display.Enabled = false;
			}
			else
			{
				display.Visible = display.Enabled = true;
			}
			return true;
		}

		public override bool OnDisplayMoveTargetDownInSequence(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (m_iCurrent < 0 || m_fdoRS == null || m_fdoRS.Count == 0)
			{
				display.Visible = display.Enabled = false;
				return true;
			}

			if (m_iCurrent == 0)
			{
				display.Visible = true;
				display.Enabled = false;
			}
			else
			{
				display.Visible = display.Enabled = true;
			}
			return true;
		}

		public void OnMoveTargetUpInSequence(object commandObject)
		{
			CheckDisposed();

			if (m_obj == null || m_iCurrent < 0)
				return;
			// Move currently selected object to the next location
			int iNew = m_iCurrent + 1;
			Debug.Assert(iNew < m_fdoRS.Count);
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Undo move down/right/later in sequence",
				"Redo move down/right/later in sequence",
				m_cache.ActionHandlerAccessor,
				() =>
					{
						m_fdoRS.RemoveAt(m_iCurrent);
						m_fdoRS.Insert(iNew, m_hvoTarget);
					});
		}

		public void OnMoveTargetDownInSequence(object commandObject)
		{
			CheckDisposed();

			if (m_obj == null || m_iCurrent < 0)
				return;
			// Move currently selected object to the previous location
			int iNew = m_iCurrent - 1;
			Debug.Assert(iNew >= 0);
			UndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW("Undo move up/left/earlier in sequence",
				"Redo move up/left/earlier in sequence",
				m_cache.ActionHandlerAccessor,
				() =>
					{
						m_fdoRS.RemoveAt(m_iCurrent);
						m_fdoRS.Insert(iNew, m_hvoTarget);
					});
		}
	}

	/// <summary>
	/// Handles things common to ReferenceSequence and ReferenceCollection classes.
	/// </summary>
	public class VectorReferenceUi : ReferenceBaseUi
	{
		protected int m_iCurrent = -1;
		protected CellarPropertyType m_iType;

		public VectorReferenceUi(FdoCache cache, ICmObject rootObj, int referenceFlid, int targetHvo)
			: base(cache, rootObj, referenceFlid, targetHvo)
		{
			m_iType = (CellarPropertyType)cache.DomainDataByFlid.MetaDataCache.GetFieldType(m_flid);
			Debug.Assert(m_iType == CellarPropertyType.ReferenceSequence ||
					m_iType == CellarPropertyType.ReferenceCollection);
		}
	}

	/// <summary>
	/// This is the base class for handling references.
	/// </summary>
	public class ReferenceBaseUi : CmObjectUi
	{
		protected int m_flid;
		protected int m_hvoTarget;
		protected CmObjectUi m_targetUi;
		public ReferenceBaseUi(FdoCache cache, ICmObject rootObj, int referenceFlid, int targetHvo)
		{
			// determine whether this is an atomic or vector relationship.
			Debug.Assert(cache.IsReferenceProperty((int)referenceFlid));
			Debug.Assert(rootObj != null);

			base.m_cache = cache;
			base.m_hvo = rootObj.Hvo;
			base.m_obj = rootObj;
			m_flid = referenceFlid;
			m_hvoTarget = targetHvo;
			m_targetUi = CmObjectUi.MakeUi(m_cache, m_hvoTarget);
		}

		/// <summary>
		/// This is the ReferenceUi factory.
		/// We currently exclude ReferenceSequenceUi (see that class for reason).
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="rootObj"></param>
		/// <param name="referenceFlid"></param>
		/// <param name="targetHvo"></param>
		/// <returns></returns>
		static public ReferenceBaseUi MakeUi(FdoCache cache, ICmObject rootObj,
			int referenceFlid, int targetHvo)
		{
			CellarPropertyType iType = (CellarPropertyType)cache.DomainDataByFlid.MetaDataCache.GetFieldType(referenceFlid);
			if (iType == CellarPropertyType.ReferenceSequence || iType == CellarPropertyType.ReferenceCollection)
				return new VectorReferenceUi(cache, rootObj, referenceFlid, targetHvo);
			if (iType == CellarPropertyType.ReferenceAtomic)
				return new ReferenceBaseUi(cache, rootObj, referenceFlid, targetHvo);
			return null;
		}

		public override string ContextMenuId
		{
			get
			{
				CheckDisposed();
				return "mnuReferenceChoices";
			}
		}

		public ReferenceBaseUi(ICmObject rootObj) : base(rootObj) { }
		public ReferenceBaseUi() { }

		public override bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();
			bool result;

			if (m_targetUi != null)
				result = m_targetUi.OnDisplayJumpToTool(commandObject, ref display);
			else
				result = base.OnDisplayJumpToTool(commandObject, ref display);
			return result;
		}

		/// <summary>
		/// JohnT: Transferred this from FW 6 but don't really understand what it's all about.
		/// The overridden method is about various "Show X in Y" commands.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public override bool OnJumpToTool(object commandObject)
		{
			CheckDisposed();

			if (m_targetUi != null)
				return m_targetUi.OnJumpToTool(commandObject);
			else
				return base.OnJumpToTool(commandObject);
		}

		/// <summary>
		/// Overridden by ReferenceSequenceUi.
		/// We put these OnDisplayMove... routines in the base class, so that we can make them explicitly not show up
		/// for non ReferenceSequence classes. Otherwise, they might appear as disabled for commands under mnuReferenceChoices.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveTargetUpInSequence(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Visible = display.Enabled = false;
			return true;
		}

		/// <summary>
		/// Overriden by ReferenceSequenceUi.
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayMoveTargetDownInSequence(object commandObject, ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();

			display.Visible = display.Enabled = false;
			return true;
		}

		public override XCore.Mediator Mediator
		{
			get
			{
				CheckDisposed();

				return base.Mediator;
			}
			set
			{
				CheckDisposed();

				// Make sure we set our target at the same time we're set.
				base.Mediator = value;
				if (m_targetUi != null)
					m_targetUi.Mediator = value;
			}
		}
	}

	/// <summary>
	/// UI functions for MoMorphSynAnalysis.
	/// </summary>
	public class MoFormUi : CmObjectUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="obj"></param>
		public MoFormUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IMoForm);
		}

		internal MoFormUi() { }

		/// <summary>
		/// gives the hvo of the object to use in the URL we construct when doing a jump
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public override Guid GuidForJumping(object commandObject)
		{
			XCore.Command cmd = (XCore.Command)commandObject;
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");
			if (className == "LexEntry")
			{
				ICmObject cmo = GetSelfOrParentOfClass(Object, LexEntryTags.kClassId);
				return (cmo == null) ? Guid.Empty : cmo.Guid;
			}
			else
			{
				return Object.Guid;
			}
		}

		protected override bool ShouldDisplayMenuForClass(int specifiedClsid, XCore.UIItemDisplayProperties display)
		{
			if (LexEntryTags.kClassId == specifiedClsid)
				return true;
			else
				return DomainObjectServices.IsSameOrSubclassOf(m_cache.DomainDataByFlid.MetaDataCache, this.Object.ClassID, specifiedClsid);
		}

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out string guiControl, out string helpTopic)
		{
			wp.m_title = FdoUiStrings.ksMergeAllomorph;
			wp.m_label = FdoUiStrings.ksAlternateForms;
			int defVernWs = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;

			var le = Object.Owner as ILexEntry;
			foreach (var allo in le.AlternateFormsOS)
			{
				if (allo.Hvo != Object.Hvo && allo.ClassID == Object.ClassID)
				{
					mergeCandidates.Add(
						new DummyCmObject(
						allo.Hvo,
						allo.Form.VernacularDefaultWritingSystem.Text,
						defVernWs));
				}
			}

			if (le.LexemeFormOA.ClassID == Object.ClassID)
			{
				// Add the lexeme form.
				mergeCandidates.Add(
					new DummyCmObject(
					le.LexemeFormOA.Hvo,
					le.LexemeFormOA.Form.VernacularDefaultWritingSystem.Text,
					defVernWs));
			}

			guiControl = "MergeAllomorphList";
			helpTopic = "khtpMergeAllomorph";
			return new DummyCmObject(m_hvo, (Object as IMoForm).Form.VernacularDefaultWritingSystem.Text, defVernWs);
		}
	}

	/// <summary>
	/// UI functions for WfiAnalysis.
	/// </summary>
	public class WfiAnalysisUi : CmObjectUi
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="obj">The object.</param>
		/// ------------------------------------------------------------------------------------
		public WfiAnalysisUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IWfiAnalysis);
		}

		internal WfiAnalysisUi()
		{ }

		protected override bool ShouldDisplayMenuForClass(int specifiedClsid, XCore.UIItemDisplayProperties display)
		{
			return WfiAnalysisTags.kClassId == specifiedClsid;
		}

		protected override void ReallyDeleteUnderlyingObject()
		{
			// Gather original counts.
			var wf = Object.Owner as IWfiWordform;
			int prePACount = wf.ParserCount;
			int preUACount = wf.UserCount;
			// we need to include resetting the wordform's checksum as part of the undo action
			// for deleting this analysis.
			using (UndoableUnitOfWorkHelper helper = new UndoableUnitOfWorkHelper(
				m_cache.ActionHandlerAccessor, FdoUiStrings.ksUndoDelete, FdoUiStrings.ksRedoDelete))
			{
				base.ReallyDeleteUnderlyingObject();

				// We need to fire off a notification about the deletion for several virtual fields.
				using (WfiWordformUi wfui = new WfiWordformUi(wf))
				{
					bool updateUserCountAndIcon = (preUACount != wf.UserCount);
					bool updateParserCountAndIcon = (prePACount != wf.ParserCount);
					wfui.UpdateWordsToolDisplay(wf.Hvo,
						updateUserCountAndIcon, updateUserCountAndIcon,
						updateParserCountAndIcon, updateParserCountAndIcon);
				}

				// Make sure it gets parsed the next time.
				wf.Checksum = 0;

				helper.RollBack = false;
			}
		}
	}

	/// <summary>
	/// UI functions for WfiGloss.
	/// </summary>
	public class WfiGlossUi : CmObjectUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="obj"></param>
		public WfiGlossUi(ICmObject obj)
			: base(obj)
		{
			Debug.Assert(obj is IWfiGloss);
		}

		internal WfiGlossUi()
		{ }

		protected override bool ShouldDisplayMenuForClass(int specifiedClsid, XCore.UIItemDisplayProperties display)
		{
			return WfiGlossTags.kClassId == specifiedClsid;
		}

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out string guiControl, out string helpTopic)
		{
			wp.m_title = FdoUiStrings.ksMergeWordGloss;
			wp.m_label = FdoUiStrings.ksGlosses;

			var anal = Object.Owner as IWfiAnalysis;
			ITsString tss;
			int nVar;
			int ws;
			foreach (var gloss in anal.MeaningsOC)
			{
				if (gloss.Hvo != Object.Hvo)
				{
					tss = gloss.ShortNameTSS;
					ws = tss.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
					mergeCandidates.Add(
						new DummyCmObject(
						gloss.Hvo,
						tss.Text,
						ws));
				}
			}

			guiControl = "MergeWordGlossList";
			helpTopic = "khtpMergeWordGloss";

			var me = Object as IWfiGloss;
			tss = me.ShortNameTSS;
			ws = tss.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
			return new DummyCmObject(m_hvo, tss.Text, ws);
		}
	}
}
