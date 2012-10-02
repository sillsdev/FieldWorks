using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Xml;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.LangProj;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.RootSites;
using SIL.Utils;
using SIL.FieldWorks.LexText.Controls;
using XCore;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.Controls;

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
		static Dictionary<uint, uint> m_subclasses = new Dictionary<uint, uint>();
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
					m_obj = CmObject.CreateFromDBObject(m_cache, m_hvo);
				return m_obj;
			}
		}

		public string ClassName
		{
			get
			{
				CheckDisposed();

				// TODO: Get this from the string table, if it can be found.
				return Object.Cache.GetClassName((uint)Object.ClassID);
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
			CmObjectUi result = MakeUi(obj.Cache, obj.Hvo, (uint)obj.ClassID);
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
			return MakeUi(cache, hvo, (uint)cache.GetClassOfObject(hvo));
		}

		private static CmObjectUi MakeUi(FdoCache cache, int hvo, uint clsid)
		{
			IFwMetaDataCache mdc = cache.MetaDataCacheAccessor;
			// If we've encountered an object with this Clsid before, and this clsid isn't in
			// the switch below, the dictioanry will give us the appropriate clsid that IS in the
			// map, so the loop below will have only one iteration. Otherwise, we start the
			// search with the clsid of the object itself.
			uint realClsid = 0;
			if (m_subclasses.ContainsKey(clsid))
				realClsid = m_subclasses[clsid];
			else
				realClsid = clsid;
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
					case WfiAnalysis.kclsidWfiAnalysis:
						result = new WfiAnalysisUi();
						break;
					case PartOfSpeech.kclsidPartOfSpeech:
						result = new PartOfSpeechUi();
						break;
					case CmPossibility.kclsidCmPossibility:
						result = new CmPossibilityUi();
						break;
					case CmObject.kclsidCmObject:
						result = new CmObjectUi();
						break;
					case LexSense.kclsidLexSense:
						result = new LexSenseUi();
						break;
					case LexEntry.kclsidLexEntry:
						result = new LexEntryUi();
						break;
					case MoMorphSynAnalysis.kclsidMoMorphSynAnalysis:
						result = new MoMorphSynAnalysisUi();
						break;
					case MoStemMsa.kclsidMoStemMsa:
						result = new MoStemMsaUi();
						break;
					case MoDerivAffMsa.kclsidMoDerivAffMsa:
						result = new MoDerivAffMsaUi();
						break;
					case MoInflAffMsa.kclsidMoInflAffMsa:
						result = new MoInflAffMsaUi();
						break;
					case MoAffixAllomorph.kclsidMoAffixAllomorph:
					case MoStemAllomorph.kclsidMoStemAllomorph:
						result = new MoFormUi();
						break;
					case ReversalIndexEntry.kclsidReversalIndexEntry:
						result = new ReversalIndexEntryUi();
						break;
					case WfiWordform.kclsidWfiWordform:
						result = new WfiWordformUi();
						break;
					case WfiGloss.kclsidWfiGloss:
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
		public static CmObjectUi CreateNewUiObject(Mediator mediator, uint classId, int hvoOwner, int flid, int insertionPosition)
		{
			FdoCache cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			CmObjectUi newUiObj = null;
			switch (classId)
			{
				default:
				{
					newUiObj = DefaultCreateNewUiObject(classId, hvoOwner, flid, insertionPosition, cache);
					break;
				}
				case CmPossibility.kclsidCmPossibility:
				{
					newUiObj = CmPossibilityUi.CreateNewUiObject(mediator, classId, hvoOwner, flid, insertionPosition);
					break;
				}
				case PartOfSpeech.kclsidPartOfSpeech:
				{
					newUiObj = PartOfSpeechUi.CreateNewUiObject(mediator, classId, hvoOwner, flid, insertionPosition);
					break;
				}
				case FDO.Cellar.FsFeatDefn.kclsidFsFeatDefn:
				{
					newUiObj = FsFeatDefnUi.CreateNewUiObject(mediator, classId, hvoOwner, flid, insertionPosition);
					break;
				}
				case FDO.Ling.LexSense.kclsidLexSense:
				{
					newUiObj = LexSenseUi.CreateNewUiObject(mediator, classId, hvoOwner, flid, insertionPosition);
					break;
				}
			}
			return newUiObj;
		}

		internal static CmObjectUi DefaultCreateNewUiObject(uint classId, int hvoOwner, int flid, int insertionPosition, FdoCache cache)
		{
			CmObjectUi newUiObj;
			cache.BeginUndoTask(FdoUiStrings.ksUndoInsert, FdoUiStrings.ksRedoInsert);
			int newHvo = cache.CreateObject((int)classId, hvoOwner, flid, insertionPosition);
			newUiObj = CmObjectUi.MakeUi(cache, newHvo, classId);
			cache.EndUndoTask();
			cache.MainCacheAccessor.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoOwner, flid, insertionPosition, 1, 0);
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
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
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

		#endregion IxCoreColleague implementation

		#region Jumping

		public static int GetParentOfClass(FdoCache m_cache, int hvo, int classIdOfParentToSearchFor)
		{
			//save the caller the need to see if this property was empty or not
			if(hvo <1)
				return -1;

			int classId = m_cache.GetClassOfObject(hvo) ;

			while((!m_cache.IsSameOrSubclassOf(classId,classIdOfParentToSearchFor))
				&& (classId != FDO.LangProj.LangProject.kClassId))
			{
				hvo = m_cache.GetOwnerOfObject(hvo);
				classId = m_cache.GetClassOfObject(hvo) ;
			}
			if((!m_cache.IsSameOrSubclassOf(classId,classIdOfParentToSearchFor)))
				return -1;
			else
				return hvo;
		}

		public virtual void LaunchGuiControl(XCore.Command command)
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
		///
		/// </summary>
		/// <returns></returns>
		public virtual bool OnJumpToTool(object commandObject)
		{
			CheckDisposed();

			XCore.Command command = (XCore.Command) commandObject;
			string tool = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0],"tool");
			int hvo = HvoForJumping(commandObject);
			if (hvo<0)
			{
				System.Windows.Forms.MessageBox.Show(FdoUiStrings.ksCannotFindObject,
					FdoUiStrings.ksProgramError,
					System.Windows.Forms.MessageBoxButtons.OK,
					System.Windows.Forms.MessageBoxIcon.Exclamation);
			}
			else
			{
				m_mediator.PostMessage("FollowLink",
					SIL.FieldWorks.FdoUi.FwLink.Create(tool, m_cache.GetGuidFromId(hvo), m_cache.ServerName, m_cache.DatabaseName));
			}

			return true;
		}

		/// <summary>
		/// gives the hvo of the object to use in the URL we construct when doing a jump
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public virtual int HvoForJumping(object commandObject)
		{
			return m_hvo;
		}

		/// <summary>
		/// called by the mediator to decide how/if a MenuItem or toolbar button should be displayed
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns></returns>
		public virtual bool OnDisplayJumpToTool(object commandObject,	ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();

			XCore.Command command = (XCore.Command) commandObject;
			string tool = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "tool");
			//string areaChoice = m_mediator.PropertyTable.GetStringProperty("areaChoice", null);
			//string toolChoice = m_mediator.PropertyTable.GetStringProperty("ToolForAreaNamed_" + areaChoice, null);
			string toolChoice = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);
			if (!IsAcceptableContextToJump(toolChoice,tool))
			{
				display.Visible = display.Enabled = false;
				return true;
			}
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");

			uint specifiedClsid = this.m_cache.MetaDataCacheAccessor.GetClassId(className);

			display.Visible =display.Enabled = ShouldDisplayMenuForClass(specifiedClsid, display);
			return true;
		}

		protected virtual bool IsAcceptableContextToJump(string toolCurrent, string toolTarget)
		{
			if (toolCurrent == toolTarget)
			{
				ICmObject obj = GetCurrentCmObject();
				// Disable if target is the current object, or target is owned directly by the target object.
				if (obj != null && (obj.Hvo == m_hvo || m_cache.GetOwnerOfObject(m_hvo) == obj.Hvo))
				{
					return false; // we're already there!
				}
			}
			return true;
		}

		private ICmObject GetCurrentCmObject()
		{
			ICmObject obj = null;
			if (m_hostControl is XmlBrowseViewBase)
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
					obj = CmObject.CreateFromDBObject(m_cache, hvoCurrentObject);
			}
			else
			{
				obj = m_mediator.PropertyTable.GetValue("ActiveClerkSelectedObject", null) as CmObject;
			}
			return obj;
		}

		protected virtual bool ShouldDisplayMenuForClass(uint specifiedClsid, XCore.UIItemDisplayProperties display)
		{
			if (specifiedClsid == 0)
				return false; // a special magic class id, only enabled explicitly.
			if(this.Object.ClassID == specifiedClsid)
				return true;
			else
			{
				uint baseClsid = m_cache.MetaDataCacheAccessor.GetBaseClsId((uint)Object.ClassID);
				if(baseClsid == specifiedClsid) //handle one level of subclassing
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
		/// Handle the right click by popping up an explicit context menu id.
		/// </summary>
		/// <param name="mediator"></param>
		/// <param name="hostControl"></param>
		/// <param name="shouldDisposeThisWhenClosed">True, if the menu handler is to dispose of the CmObjectUi after menu closing</param>
		/// <param name="sMenuId"></param>
		/// <returns></returns>
		public bool HandleRightClick(Mediator mediator, Control hostControl, bool shouldDisposeThisWhenClosed, string sMenuId)
		{
			CheckDisposed();

			Mediator = mediator;
			XWindow window = (XWindow)mediator.PropertyTable.GetValue("window");
			m_hostControl = hostControl;

			string sHostType = m_hostControl.GetType().Name;
			string sControl = m_mediator.PropertyTable.GetStringProperty("currentContentControl", null);
			string sType = this.Object.GetType().Name;

			if (sHostType == "XmlBrowseView" && sType == "CmBaseAnnotation")
			{
				// Generally we don't want popups trying to manipulate the annotations as objects in browse views.
				// See e.g. LT-5156, 6534, 7160.
				// We were checking for the exact sControl names to forbid, at least,
				//if (sControl == "wordListConcordance" || sControl == "Analyses")
				// But a more general check is that we don't want this sort of menu item on Twfic annotations.
				// Indeed, since CmBaseAnnotation presents itself as a 'Problem Report', we don't want
				// to do it for any kind of annotation that couldn't be one!
				int twficType = CmAnnotationDefn.Twfic(this.Object.Cache).Hvo;
				ISilDataAccess sda = this.Object.Cache.MainCacheAccessor;
				int annoType = sda.get_ObjectProp(this.Object.Hvo, (int)CmAnnotation.CmAnnotationTags.kflidAnnotationType);
				if (twficType == annoType)
					return true;		// No Pop-up menu wanted in this context.  See LT-5156.
				int segType = CmAnnotationDefn.TextSegment(this.Object.Cache).Hvo;
				if (segType == annoType)
					return true;		// No Pop-up menu wanted here either.
				object clrk = m_mediator.PropertyTable.GetValue("ActiveClerk");
				string sClerkType = clrk != null ? clrk.GetType().Name : "";
				if (sClerkType == "OccurrencesOfSelectedUnit")
					return true;		// We don't want this either.  See LT-6101.
			}

			// TODO: The context menu needs to be filtered to remove inappropriate menu items.

			window.ShowContextMenu(sMenuId,
				new Point(Cursor.Position.X, Cursor.Position.Y),
				new TemporaryColleagueParameter(m_mediator, this, shouldDisposeThisWhenClosed),
				null);
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
				// Disable deleting interior items from a WfiMorphBundle.  See LT-6217.
				|| (m_hostControl.GetType().Name == "OneAnalysisSandbox" && !(m_obj is WfiMorphBundle)))
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

				string typeName = this.Object.GetType().Name;
				string className = null;
				if (this.Object is ICmPossibility)
				{
					ICmPossibility possibility = this.Object as ICmPossibility;
					className = possibility.ItemTypeName(m_mediator.StringTbl);
				}
				else
				{
					className = m_mediator.StringTbl.GetString(typeName, "ClassNames");
				}
				if (className == "*" + typeName + "*")
				{
					className = typeName;
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
				if (m_obj is WfiMorphBundle)
				{
					// we want to delete the owner, not just this object itself.
					CmObjectUi owner = CmObjectUi.MakeUi(m_cache, m_obj.OwnerHVO);
					owner.Mediator = m_mediator;
					owner.DeleteUnderlyingObject();
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

		public void DeleteUnderlyingObject()
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
				mainWindow.Cursor = Cursors.WaitCursor;
				try
				{
					using (ConfirmDeleteObjectDlg dlg = new ConfirmDeleteObjectDlg())
					{
						dlg.SetDlgInfo(this, m_cache, Mediator);
						if (DialogResult.Yes == dlg.ShowDialog(mainWindow))
							ReallyDeleteUnderlyingObject();
					}
				}
				finally
				{
					mainWindow.Cursor = Cursors.Default;
				}
			}
		}

		protected virtual void ReallyDeleteUnderlyingObject()
		{
			Logger.WriteEvent("Deleting '" + this.Object.ShortName + "'...");
			m_cache.BeginUndoTask(FdoUiStrings.ksUndoDelete, FdoUiStrings.ksRedoDelete);
			this.Object.DeleteUnderlyingObject();
			m_cache.EndUndoTask();
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
			mainWindow.Cursor = Cursors.WaitCursor;
			using (MergeObjectDlg dlg = new MergeObjectDlg())
			{
				WindowParams wp = new WindowParams();
				List<DummyCmObject> mergeCandidates = new List<DummyCmObject>();
				string guiControl, helpTopic;
				DummyCmObject dObj = GetMergeinfo(wp, mergeCandidates, out guiControl, out helpTopic);
				dlg.SetDlgInfo(m_cache, m_mediator, wp, dObj, mergeCandidates, guiControl, helpTopic);
				if (DialogResult.OK == dlg.ShowDialog(mainWindow))
					ReallyMergeUnderlyingObject(dlg.Hvo, fLoseNoTextData);
			}
			mainWindow.Cursor = Cursors.Default;
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
			ICmObject survivor = CmObject.CreateFromDBObject(m_cache, survivorHvo);
			Logger.WriteEvent("Merging '" + this.Object.ShortName + "' into '" + survivor.ShortName + "'.");
			m_cache.BeginUndoTask(FdoUiStrings.ksUndoMerge, FdoUiStrings.ksRedoMerge);
			survivor.MergeObject(this.Object, fLoseNoTextData);
			m_cache.EndUndoTask();
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

			if (!Object.IsValidObject())
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
			return ((uint)(((byte)(r)|((short)((byte)(g))<<8))|(((short)(byte)(b))<<16)));

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
						// don't add it to the list, if it's not a valid object
						if (!(expectedClassId != 0 && cache.IsRealObject(hvo, expectedClassId) ||
						expectedClassId == 0 && !cache.IsDummyObject(hvo) && cache.IsValidObject(hvo)))
						{
							continue;	// skip invalid objects.
						}
					}
					hvos.Add(hvo);
				}
			}
			return hvos;
		}

		#endregion Other methods

		#region Embedded View Constructor classes

		public class CmObjectVc : VwBaseVc
		{
			// Unfortunately we can't retrieve this from the vwenv, and we need it to create regular FDO
			// objects to call the ShortName method.
			protected FdoCache m_cache;

			public CmObjectVc(FdoCache cache)
			{
				m_cache = cache;
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

				base.Dispose(disposing);
			}

			#endregion IDisposable override

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();

				ISilDataAccess sda = vwenv.DataAccess;
				int wsUi = sda.WritingSystemFactory.UserWs;
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				ICmObject co = CmObject.CreateFromDBObject(m_cache, hvo);
				switch (frag)
				{
					case (int)VcFrags.kfragHeadWord:
						ILexEntry le = co as ILexEntry;
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
		public class CmAnalObjectVc : VwBaseVc
		{
			// Unfortunately we can't retrieve this from the vwenv, and we need it to create
			// regular FDO objects to call the ShortName method.
			protected FdoCache m_cache;
			protected int m_ws;

			public CmAnalObjectVc(FdoCache cache)
			{
				m_cache = cache;
				m_ws = cache.DefaultAnalWs;
			}

			public int WritingSystemCode
			{
				get
				{
					CheckDisposed();
					return m_ws;
				}
				set
				{
					CheckDisposed();
					m_ws = value;
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

				if (disposing)
				{
					// Dispose managed resources here.
				}

				// Dispose unmanaged resources here, whether disposing is true or false.
				m_cache = null;

				base.Dispose(disposing);
			}

			#endregion IDisposable override

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();

				if (hvo == 0)
					return;

				int wsAnal = m_ws;
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				ICmObject co;
				switch(frag)
				{
					case (int)VcFrags.kfragHeadWord:
						co = CmObject.CreateFromDBObject(m_cache, hvo);
						ILexEntry le = co as ILexEntry;
						if (le != null)
							vwenv.AddString(le.HeadWord);
						else
							vwenv.AddString(tsf.MakeString(co.ShortName, wsAnal));
						break;
					case (int)VcFrags.kfragShortName:
						co = CmObject.CreateFromDBObject(m_cache, hvo);
						vwenv.AddString(tsf.MakeString(co.ShortName, wsAnal));
						break;
					case (int)VcFrags.kfragPosAbbrAnalysis:
						vwenv.AddStringAltMember((int)CmPossibility.CmPossibilityTags.kflidAbbreviation, wsAnal, this);
						break;
					case (int)VcFrags.kfragName:
					default:
						co = CmObject.CreateFromDBObject(m_cache, hvo);
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

			public CmNamedObjVc(FdoCache cache, int flidName) : base(cache)
			{
				m_flidName = flidName;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();

				switch(frag)
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

			public CmNameAbbrObjVc(FdoCache cache, int flidName, int flidAbbr) : base(cache, flidName)
			{
				m_flidAbbr = flidAbbr;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();

				switch(frag)
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

			public CmPossRefVc(FdoCache cache, int flidRef) : base(cache)
			{
				m_flidRef = flidRef;
			}

			// If the expected reference property is null, insert "??" and return false;
			// otherwise return true.
			private bool HandleObjMissing(IVwEnv vwenv, int hvo)
			{
				if (m_cache.GetObjProperty(hvo, m_flidRef) == 0)
				{
					int wsUi = vwenv.DataAccess.WritingSystemFactory.UserWs;
					ITsStrFactory tsf = TsStrFactoryClass.Create();
					vwenv.AddString(tsf.MakeString(FdoUiStrings.ksQuestions, wsUi));	// was "??", not "???"
					vwenv.NoteDependency(new int[] {hvo}, new int[] {m_flidRef}, 1);
					return false;
				}
				return true;
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();

				const int kfragAbbr = 17; // arbitrary const from reserved range.
				const int kfragName = 18;
				switch(frag)
				{
					case (int)VcFrags.kfragInterlinearAbbr:
					case (int)VcFrags.kfragInterlinearName:  // abbr is probably more appropriate in interlinear.
					case (int)VcFrags.kfragShortName:
						if (HandleObjMissing(vwenv, hvo))
							vwenv.AddObjProp(m_flidRef, this, kfragAbbr);
						break;
					case kfragAbbr:
						vwenv.AddStringAltMember((int)CmPossibility.CmPossibilityTags.kflidAbbreviation, m_cache.DefaultAnalWs, this);
						break;
					case (int)VcFrags.kfragName:
						if (HandleObjMissing(vwenv, hvo))
							vwenv.AddObjProp(m_flidRef, this, kfragName);
						break;
					default:
					case kfragName:
						vwenv.AddStringAltMember((int)CmPossibility.CmPossibilityTags.kflidName, m_cache.DefaultAnalWs, this);
						break;
				}
			}
		}

		#endregion Embedded View Constructor classes
	}

	/// <summary>
	/// Class to handle adding commands to the undo stack with an appropriate description.
	/// Alternatively can be used to suppress a given task.
	/// </summary>
	public class UndoRedoCommandHelper : UndoRedoTaskHelper
	{
		Command m_command;

		/// <summary>
		/// Begin undo task based upon the given command, and end the task during dispose.
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="command"></param>
		public UndoRedoCommandHelper(FdoCache cache, Command command)
			: this(cache, command, true, false)
		{
		}

		/// <summary>
		///
		/// </summary>
		/// <param name="cache"></param>
		/// <param name="command"></param>
		/// <param name="fBeginUndoTask">if true, begin an undo task.
		/// if false, suppress this as an undo task.</param>
		/// <param name="fInvalidateUndoActions">if the enclosed sub task can invalidate undo actions, then we want
		/// to clear our actions before proceeding.</param>
		public UndoRedoCommandHelper(FdoCache cache, Command command, bool fBeginUndoTask, bool fInvalidateUndoActions) :
			base(cache, UndoString(command), RedoString(command))
		{
			m_command = command;
			Debug.Assert(command != null, "command argument should not be null if we want to build our strings.");
		}

		private static string UndoString(Command command)
		{
			string undoString = "";
			if (command != null)
			{
				string text = command.UndoRedoTextInsert;
				if (text.EndsWith("..."))
					text = text.Remove(text.Length - 3);
				undoString = String.Format(FdoUiStrings.ksUndoCommand, text);
			}
			return undoString;
		}

		private static string RedoString(Command command)
		{
			string redoString = "";
			if (command != null)
			{
				string text = command.UndoRedoTextInsert;
				if (text.EndsWith("..."))
					text = text.Remove(text.Length - 3);
				redoString = String.Format(FdoUiStrings.ksRedoCommand, text);
			}
			return redoString;
		}

		#region IDisposable
		/// <summary>
		///
		/// </summary>
		/// <param name="disposing"></param>
		protected override void  Dispose(bool disposing)
		{
			if (disposing)
			{
			}
			m_command = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable
	}


	/// <summary>
	/// Special VC for classes that should display in the default vernacular writing system.
	/// </summary>
	public class CmVernObjectVc : VwBaseVc
	{
		// Unfortunately we can't retrieve this from the vwenv, and we need it to create
		// regular FDO objects to call the ShortName method.
		protected FdoCache m_cache;

		public CmVernObjectVc(FdoCache cache)
		{
			m_cache = cache;
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

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		public override void Display(IVwEnv vwenv, int hvo, int frag)
		{
			CheckDisposed();

			int wsVern = m_cache.DefaultVernWs;
			ITsStrFactory tsf = TsStrFactoryClass.Create();
			ICmObject co = CmObject.CreateFromDBObject(m_cache, hvo);
			switch(frag)
			{
				case (int)VcFrags.kfragHeadWord:
					ILexEntry le = co as ILexEntry;
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
		public CmPossibilityUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is ICmPossibility);
		}

		internal CmPossibilityUi() {}

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
					m_vc = new CmNameAbbrObjVc(m_cache, (int)CmPossibility.CmPossibilityTags.kflidName,
						(int)CmPossibility.CmPossibilityTags.kflidAbbreviation);
				}
				return base.Vc;
			}
		}

		public override bool OnDisplayJumpToTool(object commandObject,	ref XCore.UIItemDisplayProperties display)
		{
			CheckDisposed();

			XCore.Command command = (XCore.Command) commandObject;
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0],"className");

			uint specifiedClsid = this.m_cache.MetaDataCacheAccessor.GetClassId(className);

			ICmPossibility cp = this.Object as ICmPossibility;
			ICmPossibilityList owningList = cp.OwningList;
			if (m_cache.ClassIsOrInheritsFrom(specifiedClsid, (uint)CmPossibility.kclsidCmPossibility) ||
				specifiedClsid == LexEntryType.kclsidLexEntryType)	// these appear in 2 separate lists.
			{
				IFwMetaDataCache mdc = m_cache.MetaDataCacheAccessor;
				// make sure our object matches the CmPossibilityList flid name.
				int owningFlid = owningList.OwningFlid;
				string owningFieldName = mdc.GetFieldName((uint)owningFlid);
				string owningClassName = mdc.GetOwnClsName((uint)owningFlid);
				string commandListOwnerName = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0],"ownerClass");
				string commandListFieldName = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0],"ownerField");
				display.Visible = display.Enabled = (commandListFieldName == owningFieldName && commandListOwnerName == owningClassName);
			}
			else
			{
				// we should have a specific class name to match on.
				display.Visible = display.Enabled = ShouldDisplayMenuForClass(specifiedClsid, display);
			}

			// try to insert the name of list into our display.Text for commands with label="{0}"
			FormatDisplayTextWithListName(m_cache, Mediator, owningList, ref display);
			return true;
		}

		public static string FormatDisplayTextWithListName(FdoCache cache, XCore.Mediator mediator,
			ICmPossibilityList pssl, ref UIItemDisplayProperties display)
		{
			string owningFieldName = cache.MetaDataCacheAccessor.GetFieldName((uint)pssl.OwningFlid);
			string itemTypeName = pssl.ItemsTypeName(mediator.StringTbl);
			if (itemTypeName != "*" + owningFieldName + "*")
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
			CmPossibility possItem = CmObject.CreateFromDBObject(cache, hvoItem) as CmPossibility;
			if (possItem != null)
			{
				ICmPossibility rootPoss = possItem.MainPossibility;
				int hvoRootItem = rootPoss.Hvo;
				int hvoPossList = rootPoss.OwningList.Hvo;

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
			if (cache.GetOwningFlidOfObject(hvoPossList) != (int) LangProject.LangProjectTags.kflidTextMarkupTags)
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
			if (cache.GetOwningFlidOfObject(hvoList) != (int)DsDiscourseData.DsDiscourseDataTags.kflidConstChartTempl)
				return false; // some other list we don't care about.
			// We can't turn a column into a group if it's in use.
			CmPossibility col = CmPossibility.CreateFromDBObject(cache, hvoItem) as CmPossibility;
			// If the item doesn't already have children, we can only add them if it isn't already in use
			// as a column: we don't want to change a column into a group. Thus, if there are no
			// children, we generally call the same routine as when deleting.
			// However, that routine has a special case to prevent deletion of the default template even
			// if NOT in use...and we must not prevent adding to that when it is empty! Indeed any
			// empty CHART can always be added to, so only if col's owner is a CmPossibility (it's not a root
			// item in the templates list) do we need to check for it being in use.
			if (col.SubPossibilitiesOS.Count == 0 && col.Owner is CmPossibility && col.CheckAndReportProtectedChartColumn())
				return true;
			// Finally, we have to confirm the two-level rule.
			if (hvoItem != hvoRootItem && cache.GetOwnerOfObject(hvoItem) != hvoRootItem)
			{
				MessageBox.Show(FdoUiStrings.ksTemplateTooDeep,
								FdoUiStrings.ksHierarchyLimit, MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return true;
			}
			return false;
		}

		public new static CmObjectUi CreateNewUiObject(Mediator mediator, uint classId, int hvoOwner,
			int flid, int insertionPosition)
		{
			FdoCache cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			if (CheckAndReportProblemAddingSubitem(cache, hvoOwner))
				return null;
			return CmObjectUi.DefaultCreateNewUiObject(classId, hvoOwner, flid, insertionPosition, cache);
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
		public MoMorphSynAnalysisUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is IMoMorphSynAnalysis);
		}

		internal MoMorphSynAnalysisUi()
		{
		}

		protected override bool ShouldDisplayMenuForClass(uint specifiedClsid,
			XCore.UIItemDisplayProperties display)
		{
			return (PartOfSpeech.kclsidPartOfSpeech == specifiedClsid) &&
				(HvoForJumping(null) > 0);
		}

		//protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates)
		//{
		//    wp.m_title = FdoUiStrings.ksMergeAnalysis;
		//    wp.m_label = FdoUiStrings.ksAnalyses;

		//    ILexEntry le = LexEntry.CreateFromDBObject(m_cache, Object.OwnerHVO);
		//    foreach (IMoMorphSynAnalysis msa in le.MorphoSyntaxAnalysesOC)
		//    {
		//        if (msa.Hvo != Object.Hvo
		//            && msa.ClassID == Object.ClassID)
		//        {
		//            mergeCandidates.Add(
		//                new DummyCmObject(
		//                    msa.Hvo,
		//                    msa.ShortName,
		//                    m_cache.LangProject.DefaultAnalysisWritingSystem));
		//        }
		//    }
		//    return new DummyCmObject(m_hvo, Object.ShortName, m_cache.LangProject.DefaultAnalysisWritingSystem);
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
			//private int m_flidInterlinearName;

			public MsaVc(FdoCache cache) : base(cache)
			{
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				CheckDisposed();
				int wsAnal = m_ws;
				ITsStrFactory tsf = TsStrFactoryClass.Create();
				IMoMorphSynAnalysis msa = MoMorphSynAnalysis.CreateFromDBObject(m_cache, hvo);
				switch (frag)
				{
					case (int)VcFrags.kfragFullMSAInterlinearname:
						// not editable
						vwenv.OpenParagraph();
						vwenv.set_StringProperty((int)FwTextPropType.ktptFontFamily,
							StStyle.DefaultHeadingFont);
						vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
							(int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);
						vwenv.AddString(msa.LongNameTs);
						vwenv.CloseParagraph();
						break;
					case (int)VcFrags.kfragInterlinearName:
						// not editable
						//vwenv.set_IntProperty((int)FwTextPropType.ktptEditable,
						//    (int)FwTextPropVar.ktpvEnum, (int)TptEditable.ktptNotEditable);

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
		public MoStemMsaUi(ICmObject obj) : base(obj)
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
		public override int HvoForJumping(object commandObject)
		{
			return (Object as IMoStemMsa).PartOfSpeechRAHvo;
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
		public MoInflAffMsaUi(ICmObject obj) : base(obj)
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
		public override int HvoForJumping(object commandObject)
		{
			return (Object as IMoInflAffMsa).PartOfSpeechRAHvo;
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
		public MoDerivAffMsaUi(ICmObject obj) : base(obj)
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
		public override int HvoForJumping(object commandObject)
		{
			//todo: for now, this will just always send us to the "from" part of speech
			//	we could get at the "to" part of speech using a separate menu command
			//	or else, if this ends up being drawn by a view constructor rather than a string which combines both are from and the to,
			// then we will know which item of the user clicked on and can open the appropriate one.
			return (Object as IMoDerivAffMsa).FromPartOfSpeechRAHvo;
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
		public LexSenseUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is ILexSense);
		}

		internal LexSenseUi() {}

		/// <summary>
		/// gives the hvo of the object to use in the URL we construct when doing a jump
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public override int HvoForJumping(object commandObject)
		{
			XCore.Command command = (XCore.Command)commandObject;
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(command.Parameters[0], "className");
			if (className == "LexSense")
				return m_hvo;
			else
				return GetParentOfClass(m_cache, m_hvo, LexEntry.kClassId);
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

		protected override bool ShouldDisplayMenuForClass(uint specifiedClsid, XCore.UIItemDisplayProperties display)
		{
//			Debug.WriteLine("LexSenseUi:"+display.Text+": "+ (LexEntry.kclsidLexEntry == specifiedClsid));
			return LexEntry.kclsidLexEntry == specifiedClsid || LexSense.kclsidLexSense == specifiedClsid;
		}

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out string guiControl, out string helpTopic)
		{
			wp.m_title = FdoUiStrings.ksMergeSense;
			wp.m_label = FdoUiStrings.ksSenses;

			ILexEntry le = (Object as LexSense).Entry;
			// Exclude subsenses of the chosen sense.  See LT-6107.
			List<int> rghvoExclude = new List<int>();
			foreach (ILexSense ls in (Object as ILexSense).AllSenses)
				rghvoExclude.Add(ls.Hvo);
			foreach (ILexSense sense in le.AllSenses)
			{
				if (sense.Hvo != Object.Hvo && !rghvoExclude.Contains(sense.Hvo))
				{
					mergeCandidates.Add(
						new DummyCmObject(
						sense.Hvo,
						sense.ShortName,
						m_cache.LangProject.DefaultAnalysisWritingSystem));
				}
			}
			guiControl = "MergeSenseList";
			helpTopic = "khtpMergeSense";
			return new DummyCmObject(m_hvo, Object.ShortName, m_cache.LangProject.DefaultAnalysisWritingSystem);
		}

		public override void MoveUnderlyingObjectToCopyOfOwner()
		{
			CheckDisposed();

			int hvoEntry = this.Object.OwnerHVO;
			int clid = m_cache.GetClassOfObject(hvoEntry);
			while (clid != LexEntry.kClassId)
			{
				hvoEntry = m_cache.GetOwnerOfObject(hvoEntry);
				clid = m_cache.GetClassOfObject(hvoEntry);
			}
			ILexEntry le = LexEntry.CreateFromDBObject(m_cache, hvoEntry);
			le.MoveSenseToCopy(this.Object as ILexSense);
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
		public new static LexSenseUi CreateNewUiObject(Mediator mediator, uint classId, int hvoOwner, int flid, int insertionPosition)
		{
			FdoCache cache = (FdoCache)mediator.PropertyTable.GetValue("cache");
			cache.BeginUndoTask(FdoUiStrings.ksUndoInsertSense, FdoUiStrings.ksRedoInsertSense);
			int hvoMsa = 0;
			int chvo = cache.MainCacheAccessor.get_VecSize(hvoOwner, flid);
			if (chvo == 0)
			{
				// See if we're inserting a subsense. If so copy from parent.
				ICmObject owner = CmObject.CreateFromDBObject(cache, hvoOwner);
				if (owner is ILexSense)
				{
					hvoMsa = (owner as ILexSense).MorphoSyntaxAnalysisRAHvo;
				}
				else if (owner is ILexEntry)
				{
					// If we don't get the MSA here, trouble ensues.  See LT-5411.
					hvoMsa = (owner as ILexEntry).FindOrCreateDefaultMsa();
				}
			}
			else
			{
				int copyFrom = insertionPosition - 1;
				if (copyFrom < 0)
					copyFrom = 0;
				if (copyFrom < chvo)
				{
					int hvoCopyFrom = cache.MainCacheAccessor.get_VecItem(hvoOwner, flid, copyFrom);
					hvoMsa = cache.MainCacheAccessor.get_ObjectProp(hvoCopyFrom, (int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis);
				}
			}
			int newHvo = cache.CreateObject((int)classId, hvoOwner, flid, insertionPosition);
			if (hvoMsa != 0)
				cache.MainCacheAccessor.SetObjProp(newHvo, (int)LexSense.LexSenseTags.kflidMorphoSyntaxAnalysis, hvoMsa);
			LexSenseUi result = new LexSenseUi(new LexSense(cache, newHvo, false, false));
			cache.EndUndoTask();
			cache.MainCacheAccessor.PropChanged(null, (int)PropChangeType.kpctNotifyAll, hvoOwner, flid, insertionPosition, 1, 0);
			return result;
		}
	}

	/// <summary>
	/// Handling reference collections is rather minimal at the moment, basically allowing a
	/// different context menu to be used.
	/// </summary>
	public class ReferenceCollectionUi : VectorReferenceUi
	{
		public ReferenceCollectionUi(FdoCache cache, ICmObject rootObj, int referenceFlid, int targetHvo):
			base(cache, rootObj, referenceFlid, targetHvo)
		{
			Debug.Assert(m_iType == FieldType.kcptReferenceCollection);
		}

		public override string ContextMenuId
		{
			get
			{
				CheckDisposed();

				uint clidDst = m_cache.MetaDataCacheAccessor.GetDstClsId((uint)m_flid);
				switch ((int)clidDst)
				{
				case PhEnvironment.kclsidPhEnvironment:
					return "mnuEnvReferenceChoices";
				default:
					return "mnuReferenceChoices";
				}
			}
		}
	}

	/// <summary>
	/// Currently only LexReferenceSequenceView displays a full sequence for lexical relations sequence.
	/// Otherwise we could also manufacture ReferenceSequenceUi from ReferenceBaseUi.MakeUi().
	/// But since only LexReferenceSequenceView (e.g. Calendar) is handling changing the sequence of items
	/// through the context menu, we'll wait tillwe really need it to come up with a solution that can
	/// "exclude self" from the list in moving calculations.
	/// </summary>
	public class ReferenceSequenceUi : VectorReferenceUi
	{
		FdoReferenceSequence<ICmObject> m_fdoRS = null;

		public ReferenceSequenceUi(FdoCache cache, ICmObject rootObj, int referenceFlid, int targetHvo)
			: base(cache, rootObj, referenceFlid, targetHvo)
		{
			Debug.Assert(m_iType == FieldType.kcptReferenceSequence);
			m_fdoRS = new FdoReferenceSequence<ICmObject>(m_cache, m_hvo, m_flid);
			m_iCurrent = ComputeTargetVectorIndex();
		}

		protected int ComputeTargetVectorIndex()
		{
			Debug.Assert(m_fdoRS != null);
			Debug.Assert(m_hvoTarget > 0);
			if (m_fdoRS == null || m_hvoTarget <= 0)
				return -1;
			for (int i = 0; i < m_fdoRS.Count; i++)
			{
				if ((int)m_fdoRS.HvoArray[i] == m_hvoTarget)
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
			m_fdoRS.RemoveAt(m_iCurrent);
			m_fdoRS.InsertAt(m_hvoTarget, iNew);
		}

		public void OnMoveTargetDownInSequence(object commandObject)
		{
			CheckDisposed();

			if (m_obj == null || m_iCurrent < 0)
				return;
			// Move currently selected object to the previous location
			int iNew = m_iCurrent - 1;
			Debug.Assert(iNew >= 0);
			m_fdoRS.RemoveAt(m_iCurrent);
			m_fdoRS.InsertAt(m_hvoTarget, iNew);
		}
	}

	/// <summary>
	/// Handles things common to ReferenceSequence and ReferenceCollection classes.
	/// </summary>
	public class VectorReferenceUi : ReferenceBaseUi
	{
		protected int m_iCurrent = -1;
		protected FieldType m_iType;
		public VectorReferenceUi(FdoCache cache, ICmObject rootObj, int referenceFlid, int targetHvo)
			: base(cache, rootObj, referenceFlid, targetHvo)
		{
			m_iType = cache.GetFieldType(m_flid);
			Debug.Assert(m_iType == FieldType.kcptReferenceSequence ||
					m_iType == FieldType.kcptReferenceCollection);
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
			Debug.Assert(cache.IsReferenceProperty(referenceFlid));
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
			FieldType iType = cache.GetFieldType(referenceFlid);
			if (iType == FieldType.kcptReferenceSequence || iType == FieldType.kcptReferenceCollection)
				return new VectorReferenceUi(cache, rootObj, referenceFlid, targetHvo);
			else if (iType == FieldType.kcptReferenceAtom)
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
		public ReferenceBaseUi() {}

		public override bool OnDisplayJumpToTool(object commandObject, ref UIItemDisplayProperties display)
		{
			CheckDisposed();

			if (m_targetUi != null)
				return m_targetUi.OnDisplayJumpToTool(commandObject, ref display);
			else
				return base.OnDisplayJumpToTool(commandObject, ref display);
		}

		public override bool OnJumpToTool(object commandObject)
		{
			CheckDisposed();

			if (m_targetUi != null)
				return m_targetUi.OnJumpToTool(commandObject);
			else
				return base.OnJumpToTool(commandObject);
		}

		/// <summary>
		/// Overriden by ReferenceSequenceUi.
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
		public MoFormUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is IMoForm);
		}

		internal MoFormUi() {}

		/// <summary>
		/// gives the hvo of the object to use in the URL we construct when doing a jump
		/// </summary>
		/// <param name="commandObject"></param>
		/// <returns></returns>
		public override int HvoForJumping(object commandObject)
		{
			XCore.Command cmd = (XCore.Command)commandObject;
			string className = SIL.Utils.XmlUtils.GetManditoryAttributeValue(cmd.Parameters[0], "className");
			if (className == "LexEntry")
				return GetParentOfClass(m_cache, m_hvo, LexEntry.kClassId);
			else
				return m_hvo;
		}

		protected override bool ShouldDisplayMenuForClass(uint specifiedClsid, XCore.UIItemDisplayProperties display)
		{
			if (LexEntry.kclsidLexEntry == specifiedClsid)
				return true;
			else
				return m_cache.ClassIsOrInheritsFrom((uint)this.Object.ClassID, specifiedClsid);
		}

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out string guiControl, out string helpTopic)
		{
			wp.m_title = FdoUiStrings.ksMergeAllomorph;
			wp.m_label = FdoUiStrings.ksAlternateForms;

			ILexEntry le = LexEntry.CreateFromDBObject(m_cache, Object.OwnerHVO);
			foreach (IMoForm allo in le.AlternateFormsOS)
			{
				if (allo.Hvo != Object.Hvo && allo.ClassID == Object.ClassID)
				{
					mergeCandidates.Add(
						new DummyCmObject(
						allo.Hvo,
						allo.Form.VernacularDefaultWritingSystem,
						m_cache.LangProject.DefaultVernacularWritingSystem));
				}
			}

			if (le.LexemeFormOA.ClassID == Object.ClassID)
			{
				// Add the lexeme form.
				mergeCandidates.Add(
					new DummyCmObject(
					le.LexemeFormOAHvo,
					le.LexemeFormOA.Form.VernacularDefaultWritingSystem,
					m_cache.LangProject.DefaultVernacularWritingSystem));
			}

			guiControl = "MergeAllomorphList";
			helpTopic = "khtpMergeAllomorph";
			return new DummyCmObject(m_hvo, (Object as IMoForm).Form.VernacularDefaultWritingSystem, m_cache.LangProject.DefaultVernacularWritingSystem);
		}
	}

	/// <summary>
	/// UI functions for WfiAnalysis.
	/// </summary>
	public class WfiAnalysisUi : CmObjectUi
	{
		/// <summary>
		/// Create one.
		/// </summary>
		/// <param name="obj"></param>
		public WfiAnalysisUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is IWfiAnalysis);
		}

		internal WfiAnalysisUi()
		{}

		protected override bool ShouldDisplayMenuForClass(uint specifiedClsid, XCore.UIItemDisplayProperties display)
		{
			return WfiAnalysis.kclsidWfiAnalysis == specifiedClsid;
		}

		protected override void ReallyDeleteUnderlyingObject()
		{
			// Gather original counts.
			IWfiWordform wf = WfiWordform.CreateFromDBObject(m_cache, this.Object.OwnerHVO);
			int wfHvo = wf.Hvo;
			int prePACount = wf.ParserCount;
			int preUACount = wf.UserCount;
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
		public WfiGlossUi(ICmObject obj) : base(obj)
		{
			Debug.Assert(obj is IWfiGloss);
		}

		internal WfiGlossUi()
		{}

		protected override bool ShouldDisplayMenuForClass(uint specifiedClsid, XCore.UIItemDisplayProperties display)
		{
			return WfiGloss.kclsidWfiGloss == specifiedClsid;
		}

		protected override DummyCmObject GetMergeinfo(WindowParams wp, List<DummyCmObject> mergeCandidates, out string guiControl, out string helpTopic)
		{
			wp.m_title = FdoUiStrings.ksMergeWordGloss;
			wp.m_label = FdoUiStrings.ksGlosses;

			IWfiAnalysis anal = WfiAnalysis.CreateFromDBObject(m_cache, Object.OwnerHVO);
			ITsString tss;
			int nVar;
			int ws;
			foreach (IWfiGloss gloss in anal.MeaningsOC)
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

			IWfiGloss me = Object as IWfiGloss;
			tss = me.ShortNameTSS;
			ws = tss.get_PropertiesAt(0).GetIntPropValues((int)FwTextPropType.ktptWs, out nVar);
			return new DummyCmObject(m_hvo, tss.Text, ws);
		}
	}
}
