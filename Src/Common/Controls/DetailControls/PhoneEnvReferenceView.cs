// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: PhoneEnvReferenceView.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.FDO.Validation;
using SIL.FieldWorks.FdoUi;
using SIL.Utils;
using XCore;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Main class for displaying the PhoneEnvReferenceSlice.
	/// </summary>
	public class PhoneEnvReferenceView : RootSiteControl
	{
		#region Constants and data members

		// Fake flids.
		public const int kMainObjEnvironments = -5001;	// reference vector
		public const int kEnvStringRep = -5002;			// TsString
		public const int kErrorMessage = -5003;			// unicode

		public const int kDummyClass = -2;				// used to create a new environment

		//Fake Ids.
		public const int kDummyPhoneEnvID = -1;
		private int m_id = -1000000;

		// View frags.
		public const int kFragEnvironments = 1;
		public const int kFragPositions = 2;
		public const int kFragStringRep = 3;
		public const int kFragAnnotation = 4;
		public const int kFragEnvironmentObj = 5;

		// A cache used to interact with the Views code,
		// but which is not the one in the FdoCache.
		private PhoneEnvReferenceSda m_sda;
		private PhoneEnvReferenceVc m_PhoneEnvReferenceVc;
		private IMoForm m_rootObj;
		private int m_rootFlid;
		private int m_hvoOldSelection = 0;
		private ITsStrFactory m_tsf;
		private int m_wsVern;
		private PhonEnvRecognizer m_validator;
		private Dictionary<int, IPhEnvironment> m_realEnvs = new Dictionary<int, IPhEnvironment>();

		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// This allows the view to communicate size changes to the embedding slice.
		/// </summary>
		public event SIL.Utils.FwViewSizeChangedEventHandler ViewSizeChanged;
		private int m_heightView = 0;

		#endregion // Constants and data members

		#region Properties

		private ITsString DummyString
		{
			get { return m_tsf.MakeString("", m_wsVern); }
		}

		#endregion Properties

		#region Construction, initialization, and disposal

		public PhoneEnvReferenceView()
		{
			// This call is required by the Windows Form Designer.
			InitializeComponent();
		}

		public void Initialize(IMoForm rootObj, int rootFlid, FdoCache cache)
		{
			CheckDisposed();

			Debug.Assert(rootObj is IMoAffixAllomorph || rootObj is IMoStemAllomorph);
			Debug.Assert(cache != null && m_fdoCache == null);
			Cache = cache;
			m_tsf = cache.TsStrFactory;
			ResetValidator();
			m_wsVern = m_fdoCache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			m_rootObj = rootObj;
			m_rootFlid = rootFlid;
			if (m_rootb == null)
				MakeRoot();
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
			m_validator = null; // TODO: Make m_validator disposable?
			if (m_realEnvs != null)
			{
				m_realEnvs.Clear();
				m_realEnvs = null;
			}
			m_rootObj = null;
			m_tsf = null;
			m_sda = null;
			m_PhoneEnvReferenceVc = null;
		}


		private void CacheEnvironments(IMoStemAllomorph allomorph)
		{
			CacheEnvironments(allomorph.PhoneEnvRC.ToHvoArray());
			AppendPhoneEnv(kDummyPhoneEnvID, null);
		}

		private void CacheEnvironments(IMoAffixAllomorph allomorph)
		{
			int[] hvos;
			if (m_rootFlid == MoAffixAllomorphTags.kflidPhoneEnv)
				hvos = allomorph.PhoneEnvRC.ToHvoArray();
			else
				hvos = allomorph.PositionRS.ToHvoArray();
			CacheEnvironments(hvos);
			AppendPhoneEnv(kDummyPhoneEnvID, null);
		}

		private void CacheEnvironments(int[] realEnvHvos)
		{
			foreach (int realHvoEnv in realEnvHvos)
			{
				var env = m_fdoCache.ServiceLocator.GetInstance<IPhEnvironmentRepository>().GetObject(realHvoEnv);
				AppendPhoneEnv(m_id, env.StringRepresentation);
				m_realEnvs[m_id] = env; // NB: m_id gets changed each pass through the loop.
				ValidateStringRep(m_id++);
			}
		}

		/// <summary>
		/// Add new item to the collection (added in the chooser).
		/// </summary>
		/// <param name="realHvo">ID of the envirnoment from the chooser.</param>
		public void AddNewItem(IPhEnvironment env)
		{
			CheckDisposed();
			m_realEnvs[m_id] = env;
			int count = m_sda.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
			InsertPhoneEnv(m_id++, env.StringRepresentation, count - 1);
			m_rootb.PropChanged(m_rootObj.Hvo, kMainObjEnvironments, count, 1, 0);
			m_heightView = m_rootb.Height;
		}

		/// <summary>
		/// Remove an item from the collection (deleted in the chooser).
		/// </summary>
		/// <param name="realHvo">ID of the environment from the chooser.</param>
		public void RemoveItem(IPhEnvironment env)
		{
			CheckDisposed();
			int dummyHvo = 0;
			foreach (var kvp in m_realEnvs)
			{
				var realEnv = kvp.Value;
				if (realEnv == env)
				{
					dummyHvo = kvp.Key;
					break;
				}
			}
			if (dummyHvo == 0)
				return;
			m_realEnvs.Remove(dummyHvo);
			int count = m_sda.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
			int loc = -1;
			int hvo;
			for (int i = 0; i < count; ++i)
			{
				hvo = m_sda.get_VecItem(m_rootObj.Hvo, kMainObjEnvironments, i);
				if (hvo == dummyHvo)
				{
					loc = i;
					break;
				}
			}
			if (loc >= 0)
			{
				m_sda.CacheReplace(m_rootObj.Hvo, kMainObjEnvironments, loc, loc + 1,
					new int[0], 0);
				m_sda.DeleteObj(dummyHvo);
				m_rootb.PropChanged(m_rootObj.Hvo, kMainObjEnvironments, loc, 0, 1);
			}
			m_heightView = m_rootb.Height;
		}

		private void AppendPhoneEnv(int dummyHvo, ITsString rep)
		{
			if(rep == null)
				rep = DummyString;

			InsertPhoneEnv(dummyHvo, rep,
				m_sda.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments));
		}

		private void InsertPhoneEnv(int dummyHvo, ITsString rep, int location)
		{
			m_sda.CacheReplace(m_rootObj.Hvo, kMainObjEnvironments, location, location,
				new int[] {dummyHvo}, 1);
			m_sda.SetString(dummyHvo, kEnvStringRep, rep);
		}

		#endregion // Construction, initialization, and disposal

		#region RootSite required methods

		public override void MakeRoot()
		{
			CheckDisposed();
			base.MakeRoot();

			if (m_fdoCache == null || DesignMode)
				return;

			m_PhoneEnvReferenceVc = new PhoneEnvReferenceVc(m_fdoCache);
			m_sda = new PhoneEnvReferenceSda(m_fdoCache.DomainDataByFlid as ISilDataAccessManaged);

			// Populate m_vwCache with data.
			ResynchListToDatabase();

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_sda;
			m_rootb.SetRootObject(m_rootObj.Hvo, m_PhoneEnvReferenceVc, kFragEnvironments,
				null);
			m_heightView = m_rootb.Height;
		}

		#endregion // RootSite required methods

		// for testing only.
		internal void SetSda(PhoneEnvReferenceSda sda)
		{
			m_sda = sda;
		}

		internal void SetRoot(IMoForm form)
		{
			m_rootObj = form;
		}

		internal void SetCache(FdoCache cache)
		{
			m_fdoCache = cache;
		}

		#region UndoRedo
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables/enables the Edit/Undo menu item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDisplayUndo(object commandObject, ref UIItemDisplayProperties display)
		{
			//if (m_sda.GetActionHandler().CanUndo())
			//{
			//    display.Enabled = true;
			//    return true;
			//}
			return false; // we don't want to handle the command.
		}

		/// <summary>
		/// We need to override Undo so that we can undo changes within the slice (in its own cache).
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		internal bool OnUndo(object args)
		{
			//if (m_silCache.GetActionHandler().CanUndo())
			//{
			//    m_silCache.GetActionHandler().Undo();
			//    return true;
			//}
			return false;
		}
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Disables/enables the Edit/Redo menu item
		/// </summary>
		/// <param name="commandObject"></param>
		/// <param name="display"></param>
		/// <returns><c>true</c></returns>
		/// ------------------------------------------------------------------------------------
		protected bool OnDisplayRedo(object commandObject, ref UIItemDisplayProperties display)
		{
			//if (m_silCache.GetActionHandler().CanRedo())
			//{
			//    display.Enabled = true;
			//    return true;
			//}
			return false; // we don't want to handle the command.
		}

		/// <summary>
		/// We need to override Redo so that we can undo changes within the slice.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		internal bool OnRedo(object args)
		{
			//if (m_silCache.GetActionHandler().CanRedo())
			//{
			//    m_silCache.GetActionHandler().Redo();
			//    return true;
			//}
			return false;
		}

		#endregion UndoRedo

		#region Other methods

		public void ResetValidator()
		{
			CheckDisposed();
			m_validator = new PhonEnvRecognizer(
				m_fdoCache.LangProject.PhonologicalDataOA.AllPhonemes().ToArray(),
				m_fdoCache.LangProject.PhonologicalDataOA.AllNaturalClassAbbrs().ToArray());
		}

		/// <summary>
		/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
		/// (See LT-8656 and LT-9119.)
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (m_rootObj.IsValidObject)
				base.OnKeyPress(e);
			else
				e.Handled = true;
		}

		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();
			if (vwselNew == null)
				return;

			// Get the string value for the old selection before it possibly changes due to a delete.
			ITsString tssOldSel = null;
			if (m_hvoOldSelection < 0)
				tssOldSel = m_sda.get_StringProp(m_hvoOldSelection, kEnvStringRep);

			base.HandleSelectionChange(rootb, vwselNew);

			ITsString tss;
			int ichAnchor;
			bool fAssocPrev;
			int hvoObj;
			int tag;
			int ws; // NB: This will be 0 after each call, since the string does
			// not have alternatives. Ws would be the WS of an alternative,
			// if there were any.
			vwselNew.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj,
				out tag, out ws); // start of string info

			int ichEnd;
			int hvoObjEnd;
			vwselNew.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjEnd,
				out tag, out ws); // end of string info

			if (hvoObjEnd != hvoObj)
			{	// owner of the end of the string in not the same as at the beginning - is this possible?
				CheckHeight();
				return;
			}
			if (m_hvoOldSelection < 0 &&
				(hvoObj != m_hvoOldSelection || (tssOldSel != null && tssOldSel.Length > 0)))
			{	// the new selection is owned by a different object (left the selection) or the old one is not empty
				// Try to validate previously selected string rep.
				ITsString tssOld = m_sda.get_StringProp(m_hvoOldSelection, kEnvStringRep);
				if (tssOld == null || tssOld.Length == 0)
				{	// deleted or erased
					// Remove it from the dummy cache, since its length is 0.
					int limit = m_sda.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
					for (int i = 0; i < limit; ++i)
					{
						if (m_hvoOldSelection ==
							m_sda.get_VecItem(m_rootObj.Hvo, kMainObjEnvironments, i))
						{
							RemoveFromDummyCache(i);
							break;
						}
					}
				}
				else if (hvoObj != m_hvoOldSelection)
				{	// Left the selection, so validate previously selected string rep.
					ValidateStringRep(m_hvoOldSelection);
				}
			}
			if (hvoObj != kDummyPhoneEnvID)
			{   // this is a "real" env, not the place holder for the next one
				// normal editing of the current env exits here
				m_hvoOldSelection = hvoObj;
				CheckHeight();
				return;
			}
			// if this is NOT the first pass for the original PropChanged
			// and the ts string is empty (tss.Text == null) then quit
			if (m_rootb.IsPropChangedInProgress && tss.Length == 0)
			{
				CheckHeight();
				return;
			}

			// This point is only passed when the empty env pattern becomes "real"
			// or when this is the original PropChanged pass and the string is null.
			// This happens when
			//  1) a characher is typed in the empty env. (was empty but not now)
			//  2) a non-empty env is left by right-arrowing or clicking passed the separator bar.
			// Before this, the empty env is a placeholder for the next env.
			// After it is made real by giving it an hvo for the sda and selection mechanisms,
			// it must not pass this point again.

			// Create a new object, and recreate a new empty object.
			int count = m_sda.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
			int hvoNew = InsertNewEnv(count - 1);
			m_sda.SetString(hvoNew, kEnvStringRep, tss); // set the last env to the pattern
			m_sda.SetString(kDummyPhoneEnvID, kEnvStringRep, DummyString); // set a new empty env
			// Refresh to create a new view box for the DummyString? (doesn't seem to work)
			m_rootb.PropChanged(m_rootObj.Hvo, kMainObjEnvironments, count - 1, 2, 1);
			// Set selection after the just added character or on the right side of the separator bar.
			SelLevInfo[] rgvsli = new SelLevInfo[1];
			rgvsli[0].cpropPrevious = 0;
			rgvsli[0].tag = kMainObjEnvironments;
			rgvsli[0].ihvo = count - 1;
			m_rootb.MakeTextSelection(0, rgvsli.Length, rgvsli, tag, 0, ichAnchor, ichEnd, ws,
									  fAssocPrev, -1, null, true);
			m_hvoOldSelection = hvoNew;
			CheckHeight();
		}

		// Inset a new environment and return its ID.
		// We assign it an arbitrary class id.
		int InsertNewEnv(int ord)
		{
			return m_sda.MakeNewObject(kDummyClass, m_rootObj.Hvo, kMainObjEnvironments, ord);
		}

		private void CheckHeight()
		{
			if (m_rootb != null)
			{
				int hNew = m_rootb.Height;
				if (m_heightView != hNew)
				{
					if (ViewSizeChanged != null)
					{
						ViewSizeChanged(this,
							new FwViewSizeEventArgs(hNew, m_rootb.Width));
					}
					m_heightView = hNew;
				}
			}
		}

		private void RemoveFromDummyCache(int index)
		{
			m_realEnvs.Remove(m_hvoOldSelection);
			m_sda.CacheReplace(m_rootObj.Hvo, kMainObjEnvironments, index, index + 1,
				new int[0], 0);
			if (m_rootb != null) // should only be in testing
				m_rootb.PropChanged(m_rootObj.Hvo, kMainObjEnvironments, index, 0, 1);
		}


		private void ValidateStringRep(int hvoDummyObj)
		{
			ITsString tss = m_sda.get_StringProp(hvoDummyObj, kEnvStringRep);
			if (tss == null)
				tss = m_fdoCache.TsStrFactory.MakeString(String.Empty, m_fdoCache.DefaultAnalWs);
			ITsStrBldr bldr = tss.GetBldr();
			if (m_validator.Recognize(tss.Text))
				ClearSquigglyLine(hvoDummyObj, ref tss, ref bldr);
			else
				MakeSquigglyLine(hvoDummyObj, m_validator.ErrorMessage, ref tss, ref bldr);

			if (m_realEnvs.ContainsKey(hvoDummyObj))
			{
				// Reset the original env, but only if it is invalid.
				var env = m_realEnvs[hvoDummyObj];
				if (!m_validator.Recognize(env.StringRepresentation.Text) &&
					!env.StringRepresentation.Equals(tss))
				{
					UndoableUnitOfWorkHelper.Do(DetailControlsStrings.ksUndoChange, DetailControlsStrings.ksRedoChange,
						env, () => { env.StringRepresentation = tss; });
					ConstraintFailure failure;
					env.CheckConstraints(PhEnvironmentTags.kflidStringRepresentation, true, out failure,
						/* adjust the squiggly line */ true );
				}
			}
			m_sda.SetString(hvoDummyObj, kEnvStringRep, bldr.GetString());
			if (m_rootb != null)
				m_rootb.PropChanged(hvoDummyObj, kEnvStringRep, 0, tss.Length, tss.Length);
			CheckHeight();
		}

		private void ClearSquigglyLine(int hvo, ref ITsString tss, ref ITsStrBldr bldr)
		{
			bldr.SetIntPropValues(0, tss.Length,
				(int)FwTextPropType.ktptUnderline,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwUnderlineType.kuntNone);
			m_sda.SetUnicode(hvo, kErrorMessage, "", 0);
		}

		/// <summary>
		/// Make red squiggly line, which starts at the point the validator
		/// determined it was wrong, ands at the end of the string.
		/// </summary>
		/// <remarks>
		/// We have it go to the end of the string,
		/// because the validator doesn't tell us where the problem ends.
		/// Since it didn't tell us, we don't try to guess.
		/// </remarks>
		/// <param name="hvo"></param>
		/// <param name="validatorMessage"></param>
		/// <param name="bldr"></param>
		private void MakeSquigglyLine(int hvo, string validatorMessage, ref ITsString tss,
			ref ITsStrBldr bldr)
		{

			//the validator message, unfortunately, maybe invalid XML if
			//	there were XML reserved characters in the environment.
			//until we get that fixed, at least don't crash, just draw squiggly under the entire word
			int pos = 0;
			try
			{
				XmlDocument xdoc = new XmlDocument();
				xdoc.LoadXml(validatorMessage);
				XmlAttribute posAttr = xdoc.DocumentElement.Attributes["pos"];
				pos = (posAttr != null) ? Convert.ToInt32(posAttr.Value) : 0;
			}
			catch (Exception)
			{
			}

			int len = tss.Length;
			if (pos >= len)
				pos = Math.Max(0, len - 1); // make sure something will show
			Color col = Color.Red;
			bldr.SetIntPropValues(pos, len, (int)FwTextPropType.ktptUnderline,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwUnderlineType.kuntSquiggle);
			bldr.SetIntPropValues(pos, len, (int)FwTextPropType.ktptUnderColor,
				(int)FwTextPropVar.ktpvDefault,
				col.R + (col.B * 256 + col.G) * 256);

			//!!!NB: the code up to this point is, at least on the surface, identical to code in
			//PhEnvStrRepresentationSlice. so if you make a change here, go make it over there.
			//I assume this was done for lack of a commonplace to put the code?
			m_sda.SetUnicode(hvo, kErrorMessage, validatorMessage, validatorMessage.Length);
		}

		/// <summary>
		/// This method is called to handle Undo/Redo operations for this slice.
		/// It refreshes the display to reflect the changes made by the Undo/Redo
		/// operation.
		/// </summary>
		public void ResynchListToDatabaseAndRedisplay()
		{
			ResynchListToDatabase();
			m_rootb.Reconstruct();
			CheckHeight();
		}

		/// <summary>
		/// This method clears the local cache of the any Enviromnents that were stored
		/// for the slice and reloads them from the database.
		/// </summary>
		private void ResynchListToDatabase()
		{
			CheckDisposed();

			m_realEnvs.Clear();
			//We need to clear the cache since it is going to be repopulated
			m_sda.CacheVecProp(m_rootObj.Hvo, kMainObjEnvironments, new int[] { }, 0);
			// Populate m_vwCache with data.
			if (m_rootObj is IMoStemAllomorph)
				CacheEnvironments((IMoStemAllomorph)m_rootObj);
			else
				CacheEnvironments((IMoAffixAllomorph)m_rootObj);
		}

		/// <returns>
		/// First matching environment from environmentsHaystack or null if none
		/// found.
		/// </returns>
		private static IPhEnvironment GetEnvironmentFromHvo(
			IFdoOwningSequence<IPhEnvironment> environmentsHaystack,
			int hvoPattern)
		{
			return environmentsHaystack.FirstOrDefault(env =>
				env.Hvo == hvoPattern);
		}

		/// <remarks>
		/// From local m_sda, not from database
		/// </remarks>
		private ITsString GetTsStringOfEnvironment(
			int localDummyHvoOfAnEnvironmentInEntry)
		{
			ITsString environmentTssRep = m_sda.get_StringProp(
				localDummyHvoOfAnEnvironmentInEntry, kEnvStringRep);
			return environmentTssRep ?? m_fdoCache.TsStrFactory.MakeString(
				String.Empty, m_fdoCache.DefaultAnalWs);
		}

		/// <summary>
		/// From local m_sda, not from database
		/// </summary>
		private string GetStringOfEnvironment(
			int localDummyHvoOfAnEnvironmentInEntry)
		{
			return GetTsStringOfEnvironment(localDummyHvoOfAnEnvironmentInEntry).Text;
		}

		/// <summary>
		/// Integrate changes in dummy cache to real cache and DB.
		/// </summary>
		public void ConnectToRealCache()
		{
			// (FLEx) Review use of ISilDataAccess and other C++ cache related classes
			CheckDisposed();
			// If an Undo or Redo is in progress, we CAN'T save the changes. Ideally it wouldn't be necessary because making
			// any savable change in the slice would discard any pending Redo, and Undo would undo any changes in the slice
			// before undoing anything else. Currently Undo within the slice is not this well integrated. However, doing some editing
			// in the slice and then Undoing or Redoing a previous command DOES save the changes in the slice; I think OnLeave() must
			// be called somewhere in the process of invoking Undo before it is too late. This is not ideal behavior, but it
			// beats crashing.
			if (m_fdoCache.ActionHandlerAccessor.IsUndoOrRedoInProgress)
				return;
			Form frm = FindForm();
			WaitCursor wc = null;
			try
			{
				// frm will be null, if the record has been switched
				if (frm != null)
					wc = new WaitCursor(frm);
				// We're saving any changes to the real cache, so can no longer Undo/Redo local edits.
				CommitLocalEdits();
				// [NB: m_silCache is the same cache as m_vwCache, but is is a different cache than
				// m_fdoCache.  m_fdoCache has access to the database, and updates it, but
				// m_silCache does not.]
				if (DesignMode || m_rootb == null
					// It may not be valid by now, since it may have been deleted.
					|| !m_rootObj.IsValidObject)
				{
					if (frm != null)
						frm.Cursor = Cursors.Default;
					return;
				}
				string fieldname =
					(m_rootFlid == MoAffixAllomorphTags.kflidPhoneEnv) ? "PhoneEnv" : "Position";
				m_fdoCache.DomainDataByFlid.BeginUndoTask(
					String.Format(DetailControlsStrings.ksUndoSet, fieldname),
					String.Format(DetailControlsStrings.ksRedoSet, fieldname));
				IPhEnvironmentFactory environmentFactory = m_fdoCache.ServiceLocator.GetInstance<IPhEnvironmentFactory>();
				IFdoOwningSequence<IPhEnvironment> allAvailablePhoneEnvironmentsInProject =
					m_fdoCache.LanguageProject.PhonologicalDataOA.EnvironmentsOS;

				var envsBeingRequestedForThisEntry = EnvsBeingRequestedForThisEntry();

				// Environments just typed into slice that are not already used for
				// this entry or known about in the project.
				var newEnvsJustTyped =
					envsBeingRequestedForThisEntry.Where(localDummyHvoOfAnEnvInEntry =>
						!allAvailablePhoneEnvironmentsInProject
							.Select(projectEnv => RemoveSpaces(projectEnv.StringRepresentation.Text))
							.Contains(RemoveSpaces(GetStringOfEnvironment(localDummyHvoOfAnEnvInEntry))));
				// Add the unknown/new environments to project
				foreach (var localDummyHvoOfAnEnvironmentInEntry in
					newEnvsJustTyped)
				{
					ITsString envTssRep = GetTsStringOfEnvironment(
						localDummyHvoOfAnEnvironmentInEntry);
					IPhEnvironment newEnv = environmentFactory.Create();
					allAvailablePhoneEnvironmentsInProject.Add(newEnv);
					newEnv.StringRepresentation = envTssRep;
				}

				var countOfExistingEnvironmentsInDatabaseForEntry =
					m_fdoCache.DomainDataByFlid.get_VecSize(m_rootObj.Hvo, m_rootFlid);
				// Contains environments already in entry or recently selected in
				// dialog, but not ones just typed
				int[] existingListOfEnvironmentHvosInDatabaseForEntry;
				int chvoMax = m_fdoCache.DomainDataByFlid.get_VecSize(
					m_rootObj.Hvo, m_rootFlid);
				using (ArrayPtr arrayPtr = MarshalEx.ArrayToNative<int>(chvoMax))
				{
					m_fdoCache.DomainDataByFlid.VecProp(m_rootObj.Hvo, m_rootFlid, chvoMax, out chvoMax, arrayPtr);
					existingListOfEnvironmentHvosInDatabaseForEntry = MarshalEx.NativeToArray<int>(arrayPtr, chvoMax);
				}

				// Build up a list of real hvos used in database for the
				// environments in the entry
				var newListOfEnvironmentHvosForEntry = new List<int>();
				foreach (var localDummyHvoOfAnEnvironmentInEntry in
					envsBeingRequestedForThisEntry)
				{
					ITsString envTssRep = GetTsStringOfEnvironment(
						localDummyHvoOfAnEnvironmentInEntry);
					string envStringRep = envTssRep.Text;

					// Pick a sensible environment from the known environments in
					// the project, by string
					IPhEnvironment anEnvironmentInEntry = FindPhoneEnv(
						allAvailablePhoneEnvironmentsInProject, envStringRep,
						newListOfEnvironmentHvosForEntry.ToArray(),
						existingListOfEnvironmentHvosInDatabaseForEntry);

					// Maybe the ws has changed, so change the real env in database,
					// in case.
					anEnvironmentInEntry.StringRepresentation = envTssRep;

					ITsStrBldr bldr = envTssRep.GetBldr();
					ConstraintFailure failure;
					if (anEnvironmentInEntry.CheckConstraints(PhEnvironmentTags.kflidStringRepresentation, false, out failure, true))
						ClearSquigglyLine(localDummyHvoOfAnEnvironmentInEntry, ref envTssRep, ref bldr);
					else
						MakeSquigglyLine(localDummyHvoOfAnEnvironmentInEntry, failure.XmlDescription, ref envTssRep, ref bldr);

					newListOfEnvironmentHvosForEntry.Add(anEnvironmentInEntry.Hvo);

					// Refresh
					m_sda.SetString(localDummyHvoOfAnEnvironmentInEntry, kEnvStringRep, bldr.GetString());
					m_rootb.PropChanged(localDummyHvoOfAnEnvironmentInEntry, kEnvStringRep, 0,
						envTssRep.Length, envTssRep.Length);
				}

				// Only reset the main property, if it has changed.
				// Otherwise, the parser gets too excited about needing to reload.
				if ((countOfExistingEnvironmentsInDatabaseForEntry !=
					newListOfEnvironmentHvosForEntry.Count()) ||
					!equalArrays(existingListOfEnvironmentHvosInDatabaseForEntry,
						newListOfEnvironmentHvosForEntry.ToArray()))
				{
					m_fdoCache.DomainDataByFlid.Replace(m_rootObj.Hvo, m_rootFlid, 0,
						countOfExistingEnvironmentsInDatabaseForEntry,
						newListOfEnvironmentHvosForEntry.ToArray(),
						newListOfEnvironmentHvosForEntry.Count());
				}
				m_fdoCache.DomainDataByFlid.EndUndoTask();
			}
			finally
			{
				if (wc != null)
				{
					wc.Dispose();
					wc = null;
				}
			}
		}

		// internal for testing.
		internal List<int> EnvsBeingRequestedForThisEntry()
		{
// Build list of local dummy hvos of environments in the entry
			// (including any changes).
			var envsBeingRequestedForThisEntry = new List<int>();
			// Last env in local m_sda is a dummy that lets the user type a
			// new environment
			const int countOfDummyEnvsForTypingNewEnvs = 1;
			int countOfThisEntrysEnvironments =
				m_sda.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments) -
					countOfDummyEnvsForTypingNewEnvs;
			for (int i = countOfThisEntrysEnvironments - 1; i >= 0; i--) // count down so deletions don't mess things up
			{
				int localDummyHvoOfAnEnvironmentInEntry =
					m_sda.get_VecItem(m_rootObj.Hvo, kMainObjEnvironments, i);

				// Remove and exclude blank entries
				string envStringRep = GetStringOfEnvironment(
					localDummyHvoOfAnEnvironmentInEntry);
				if (envStringRep == null || envStringRep.Trim().Length == 0)
				{
					m_realEnvs.Remove(localDummyHvoOfAnEnvironmentInEntry);
					// Remove it from the dummy cache.
					int oldSelId = m_hvoOldSelection;
					m_hvoOldSelection = localDummyHvoOfAnEnvironmentInEntry;
					RemoveFromDummyCache(i);
					m_hvoOldSelection = oldSelId;
					continue;
				}

				envsBeingRequestedForThisEntry.Add(
					localDummyHvoOfAnEnvironmentInEntry);
			}
			return envsBeingRequestedForThisEntry;
		}

		private static string RemoveSpaces(string s)
		{
			if (s == null)
				return null;
			return s.Replace(" ", null);
		}

		private static bool EqualsIgnoringSpaces(string a, string b)
		{
			if (a == null || b == null)
				return false;
			return RemoveSpaces(a) == RemoveSpaces(b);
		}

		/// <summary>
		/// Find an environment in allProjectEnvs of string representation
		/// environmentPattern. Prefer to find a match that is in preferredHvos
		/// (such as hvos used before recent editing) and not in alreadyUsedHvos
		/// (hvos already used in slice).
		/// Preferring matching a hvo used before recent editing helps the
		/// Environments dialog behave more sensibly in the case of multiple
		/// items with the same string representation. (eg FWNX-822)
		/// </summary>
		private static IPhEnvironment FindPhoneEnv(
			IFdoOwningSequence<IPhEnvironment> allProjectEnvs,
			string environmentPattern, int[] alreadyUsedHvos,
			int[] preferredHvos)
		{
			// Try to find a match in the preferred set that isn't already used
			var preferredMatches = preferredHvos.Where(preferredHvo =>
				EqualsIgnoringSpaces(
					GetEnvironmentFromHvo(allProjectEnvs, preferredHvo)
						.StringRepresentation.Text,
					environmentPattern));
			if (preferredMatches.Count() > 0)
			{
				var unusedPreferred = preferredMatches.Except(alreadyUsedHvos);
				if (unusedPreferred.Count() > 0)
					return GetEnvironmentFromHvo(allProjectEnvs,
						unusedPreferred.First());
			}

			// Broaden where we look to all project environments
			var anyMatches = allProjectEnvs.Where(env =>
				EqualsIgnoringSpaces(
					env.StringRepresentation.Text,
					environmentPattern));
			if (anyMatches.Count() == 0)
				return null; // Shouldn't happen if adding envs new to project ahead of time.

			// Try to return a match that isn't already used.
			var unused = anyMatches.Select(env => env.Hvo).Except(alreadyUsedHvos);

			if (unused.Count() > 0)
				return GetEnvironmentFromHvo(allProjectEnvs, unused.First());

			// Just re-use an env
			return anyMatches.First();
		}

		/// <summary>
		/// Called when Undoing local typing no longer makes sense, because we've
		/// interpreted those changes or overridden them with changes at the object level.
		/// </summary>
		internal void CommitLocalEdits()
		{
			//m_silCache.GetActionHandler().Commit();
		}

		private bool equalArrays(int[] v1, int[] v2)
		{
			if (v1.Length != v2.Length)
				return false;
			for (int i = 0; i < v1.Length; i++)
				if (v1[i] != v2[i])
					return false;
			return true;
		}


		#endregion // Other methods

		#region Menu support methods

		/// <summary>
		/// Get the string representation of the current selection in the environment list,
		/// plus the selection object and some of its internal information.
		/// </summary>
		/// <param name="tss"></param>
		/// <param name="vwsel"></param>
		/// <param name="hvoDummyObj"></param>
		/// <param name="ichAnchor"></param>
		/// <param name="ichEnd"></param>
		/// <returns>false if selection spans two or more objects, true otherwise</returns>
		internal bool GetSelectedStringRep(out ITsString tss, out IVwSelection vwsel,
			out int hvoDummyObj, out int ichAnchor, out int ichEnd)
		{
			CheckDisposed();
			tss = null;
			vwsel = null;
			hvoDummyObj = 0;
			ichAnchor = 0;
			ichEnd = 0;
			try
			{
				ITsString tss2;
				bool fAssocPrev;
				int hvoObjEnd;
				int tag1;
				int tag2;
				int ws1;
				int ws2;
				vwsel = m_rootb.Selection;
				vwsel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoDummyObj,
					out tag1, out ws1);
				vwsel.TextSelInfo(true, out tss2, out ichEnd, out fAssocPrev, out hvoObjEnd,
					out tag2, out ws2);
				if (hvoDummyObj != hvoObjEnd)
					return false;
			}
			catch
			{
				vwsel = null;
				tss = null;
				return true;
			}
			return true;
		}

		internal void ShowEnvironmentError()
		{
			CheckDisposed();
			string s;
			if (CanGetEnvironmentStringRep(out s))
			{
				if (!m_validator.Recognize(s))
				{
					string sMsg;
					int pos;
					StringServices.CreateErrorMessageFromXml(s, m_validator.ErrorMessage, out pos, out sMsg);
					MessageBox.Show(sMsg, DetailControlsStrings.ksBadEnv,
						MessageBoxButtons.OK, MessageBoxIcon.Information);
				}
			}
		}


		internal bool CanShowEnvironmentError()
		{
			CheckDisposed();
			string s;
			if (CanGetEnvironmentStringRep(out s))
				return (!m_validator.Recognize(s));
			else
				return false;
		}

		private bool CanGetEnvironmentStringRep(out string s)
		{
			int hvoDummyObj = 0;
			int ichAnchor;
			int ichEnd;
			ITsString tss;
			IVwSelection vwsel;
			s = null;
			if (!GetSelectedStringRep(out tss, out vwsel, out hvoDummyObj, out ichAnchor, out ichEnd))
				return false;
			if (tss == null || hvoDummyObj == 0)
				return false;
			s = tss.Text;
			if (s == null || s == String.Empty)
				return false;
			return true;
		}

		internal bool CanInsertSlash()
		{
			CheckDisposed();
			int hvoDummyObj = 0;
			int ichAnchor;
			int ichEnd;
			ITsString tss;
			IVwSelection vwsel;
			if (!GetSelectedStringRep(out tss, out vwsel, out hvoDummyObj, out ichAnchor, out ichEnd))
				return false;
			if (tss == null || hvoDummyObj == 0)
				return true;
			string s = tss.Text;
			if (s == null || s == String.Empty)
				return true;
			return s.IndexOf('/') < 0;
		}

		internal bool CanInsertEnvBar()
		{
			CheckDisposed();
			int hvoDummyObj = 0;
			int ichAnchor;
			int ichEnd;
			ITsString tss;
			IVwSelection vwsel;
			if (!GetSelectedStringRep(out tss, out vwsel, out hvoDummyObj, out ichAnchor, out ichEnd))
				return false;
			if (tss == null || hvoDummyObj == 0)
				return false;
			string s = tss.Text;
			if (s == null || s == String.Empty)
				return false;
			int ichSlash = s.IndexOf('/');
			return (ichSlash >= 0) && (ichEnd > ichSlash) && (ichAnchor > ichSlash) &&
				(s.IndexOf('_') < 0);
		}

		internal bool CanInsertItem()
		{
			CheckDisposed();
			int hvoDummyObj = 0;
			int ichAnchor;
			int ichEnd;
			ITsString tss;
			IVwSelection vwsel;
			if (!GetSelectedStringRep(out tss, out vwsel, out hvoDummyObj, out ichAnchor, out ichEnd))
				return false;
			if (tss == null || hvoDummyObj == 0)
				return false;
			string s = tss.Text;
			if (s == null || s == String.Empty)
				return false;
			return PhonEnvRecognizer.CanInsertItem(s, ichEnd, ichAnchor);
		}

		internal bool CanInsertHashMark()
		{
			CheckDisposed();
			int hvoDummyObj = 0;
			int ichAnchor;
			int ichEnd;
			ITsString tss;
			IVwSelection vwsel;
			if (!GetSelectedStringRep(out tss, out vwsel, out hvoDummyObj, out ichAnchor, out ichEnd))
				return false;
			if (tss == null || hvoDummyObj == 0)
				return false;
			string s = tss.Text;
			if (s == null || s == String.Empty)
				return false;
			return PhonEnvRecognizer.CanInsertHashMark(s, ichEnd, ichAnchor);
		}
		#endregion

		#region Designer generated code
		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent()
		{
			System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(PhoneEnvReferenceView));
			this.SuspendLayout();
			//
			// PhoneEnvReferenceView
			//
			this.Name = "PhoneEnvReferenceView";
			resources.ApplyResources(this, "$this");
			this.ResumeLayout(false);

		}
		#endregion

		#region Handle right click menu
		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot,
			Rectangle rcDstRoot)
		{
			IVwSelection sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
			TextSelInfo tsi = new TextSelInfo(sel);
			return HandleRightClickOnObject(tsi.Hvo(false));
		}

		override protected bool HandleContextMenuFromKeyboard(IVwSelection sel, Point center)
		{
			TextSelInfo tsi = new TextSelInfo(sel);
			return HandleRightClickOnObject(tsi.Hvo(false));
		}

		private bool HandleRightClickOnObject(int hvoDummy)
		{
			if (hvoDummy == 0)
				return false;

			if (m_realEnvs.ContainsKey(hvoDummy))
			{
				// This displays the "Show in Environments list" item in the popup menu, in
				// addition to all the Insert X" items.
				int hvo = m_realEnvs[hvoDummy].Hvo;
				using (ReferenceCollectionUi ui = new ReferenceCollectionUi(Cache, m_rootObj, m_rootFlid, hvo))
					return ui.HandleRightClick(Mediator, this, true);
			}
			else
			{
				// We need a CmObjectUi in order to call HandleRightClick().  This won't
				// display the "Show in Environments list" item in the popup menu.
				using (CmObjectUi ui = new CmObjectUi(m_rootObj))
					return ui.HandleRightClick(Mediator, this, true, "mnuEnvReferenceChoices");
			}
		}
		#endregion

		#region PhoneEnvReferenceVc class

		/// <summary>
		///  View constructor for creating the view details.
		/// </summary>
		public class PhoneEnvReferenceVc : FwBaseVc
		{
			public PhoneEnvReferenceVc(FdoCache cache)
			{
				Debug.Assert(cache != null);
			}

			public override void Display(IVwEnv vwenv, int hvo, int frag)
			{
				switch (frag)
				{
				case PhoneEnvReferenceView.kFragEnvironmentObj:
					vwenv.AddStringProp(PhoneEnvReferenceView.kEnvStringRep, this);
					break;
				case PhoneEnvReferenceView.kFragEnvironments:
					vwenv.OpenParagraph();
					vwenv.AddObjVec(PhoneEnvReferenceView.kMainObjEnvironments, this, frag);
					vwenv.CloseParagraph();
					break;
				default:
					throw new ArgumentException(
						"Don't know what to do with the given frag.", "frag");
				}
			}

			public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
			{
				ISilDataAccess da = vwenv.DataAccess;
				int count = da.get_VecSize(hvo, tag);
				for (int i = 0; i < count; ++i)
				{
					if (i != 0)
						vwenv.AddSeparatorBar();
					vwenv.AddObj(da.get_VecItem(hvo, tag, i), this,
						PhoneEnvReferenceView.kFragEnvironmentObj);
				}
			}
		}

		#endregion // PhoneEnvReferenceVc class

		#region PhoneEnvReferenceSda class
		internal class PhoneEnvReferenceSda : DomainDataByFlidDecoratorBase
		{
			Dictionary<int, List<int>> m_MainObjEnvs = new Dictionary<int, List<int>>();
			Dictionary<int, ITsString> m_EnvStringReps = new Dictionary<int, ITsString>();
			Dictionary<int, string> m_ErrorMsgs = new Dictionary<int, string>();

			int m_NextDummyId = -500000;

			public PhoneEnvReferenceSda(ISilDataAccessManaged domainDataByFlid)
				: base(domainDataByFlid)
			{
				SetOverrideMdc(new PhoneEnvReferenceMdc(MetaDataCache as IFwMetaDataCacheManaged));
			}

			public override int get_VecSize(int hvo, int tag)
			{
				if (tag == PhoneEnvReferenceView.kMainObjEnvironments)
				{
					List<int> objs;
					if (m_MainObjEnvs.TryGetValue(hvo, out objs))
						return objs.Count;
					else
						return 0;
				}
				else
				{
					return base.get_VecSize(hvo, tag);
				}
			}

			public override int get_VecItem(int hvo, int tag, int index)
			{
				if (tag == PhoneEnvReferenceView.kMainObjEnvironments)
				{
					List<int> objs;
					if (m_MainObjEnvs.TryGetValue(hvo, out objs))
					{
						return objs[index];
					}
					else
					{
						return 0;
					}
				}
				else
				{
					return base.get_VecItem(hvo, tag, index);
				}
			}

			public override void DeleteObj(int hvoObj)
			{
				if (hvoObj < 0)
				{
					if (m_MainObjEnvs.ContainsKey(hvoObj))
						m_MainObjEnvs.Remove(hvoObj);
					if (m_EnvStringReps.ContainsKey(hvoObj))
						m_EnvStringReps.Remove(hvoObj);
					if (m_ErrorMsgs.ContainsKey(hvoObj))
						m_ErrorMsgs.Remove(hvoObj);
					foreach (KeyValuePair<int, List<int>> x in m_MainObjEnvs)
					{
						if (x.Value.Contains(hvoObj))
							x.Value.Remove(hvoObj);
					}
				}
				else
				{
					base.DeleteObj(hvoObj);
				}
			}

			public override ITsString get_StringProp(int hvo, int tag)
			{
				if (tag == PhoneEnvReferenceView.kEnvStringRep)
				{
					ITsString tss;
					if (m_EnvStringReps.TryGetValue(hvo, out tss))
						return tss;
					else
						return null;
				}
				else
				{
					return base.get_StringProp(hvo, tag);
				}
			}

			public override void SetString(int hvo, int tag, ITsString _tss)
			{
				if (tag == PhoneEnvReferenceView.kEnvStringRep)
				{
					m_EnvStringReps[hvo] = _tss;
				}
				else
				{
					base.SetString(hvo, tag, _tss);
				}
			}

			public override string get_UnicodeProp(int hvo, int tag)
			{
				if (tag == PhoneEnvReferenceView.kErrorMessage)
				{
					string sMsg;
					if (m_ErrorMsgs.TryGetValue(hvo, out sMsg))
						return sMsg;
					else
						return null;
				}
				else
				{
					return base.get_UnicodeProp(hvo, tag);
				}
			}

			public override void SetUnicode(int hvo, int tag, string _rgch, int cch)
			{
				if (tag == PhoneEnvReferenceView.kErrorMessage)
				{
					m_ErrorMsgs[hvo] = _rgch;
				}
				else
				{
					base.SetUnicode(hvo, tag, _rgch, cch);
				}
			}

			public override int MakeNewObject(int clid, int hvoOwner, int tag, int ord)
			{
				if (tag == PhoneEnvReferenceView.kMainObjEnvironments)
				{
					int hvo = --m_NextDummyId;
					List<int> objs;
					if (!m_MainObjEnvs.TryGetValue(hvoOwner, out objs))
					{
						objs = new List<int>();
						m_MainObjEnvs.Add(hvoOwner, objs);
					}
					objs.Insert(ord, hvo);
					return hvo;
				}
				else
				{
					return base.MakeNewObject(clid, hvoOwner, tag, ord);
				}
			}

			#region extra methods borrowed from IVwCacheDa

			public void CacheReplace(int hvoObj, int tag, int ihvoMin, int ihvoLim, int[] _rghvo, int chvo)
			{
				if (tag == PhoneEnvReferenceView.kMainObjEnvironments)
				{
					List<int> objs;
					if (m_MainObjEnvs.TryGetValue(hvoObj, out objs))
					{
						int cDel = ihvoLim - ihvoMin;
						if (cDel > 0)
							objs.RemoveRange(ihvoMin, cDel);
						objs.InsertRange(ihvoMin, _rghvo);
					}
					else
					{
						objs = new List<int>();
						objs.AddRange(_rghvo);
						m_MainObjEnvs.Add(hvoObj, objs);
					}
				}
			}

			public void CacheVecProp(int hvoObj, int tag, int[] rghvo, int chvo)
			{
				if (tag == PhoneEnvReferenceView.kMainObjEnvironments)
				{
					List<int> objs;
					if (m_MainObjEnvs.TryGetValue(hvoObj, out objs))
					{
						objs.Clear();
					}
					else
					{
						objs = new List<int>();
						m_MainObjEnvs.Add(hvoObj, objs);
					}
					objs.AddRange(rghvo);
				}
			}

			#endregion
		}
		#endregion // PhoneEnvReferenceSda class

		#region PhoneEnvReferenceMdc class
		class PhoneEnvReferenceMdc : FdoMetaDataCacheDecoratorBase
		{
			public PhoneEnvReferenceMdc(IFwMetaDataCacheManaged mdc)
				: base(mdc)
			{
			}

			public override void AddVirtualProp(string bstrClass, string bstrField, int luFlid, int type)
			{
				throw new NotImplementedException();
			}

			public override int GetFieldType(int luFlid)
			{
				switch (luFlid)
				{
					case kMainObjEnvironments:
						return (int)CellarPropertyType.ReferenceSequence;
					case kEnvStringRep:
						return (int)CellarPropertyType.String;
					case kErrorMessage:
						return (int)CellarPropertyType.Unicode;
					default:
						return base.GetFieldType(luFlid);
				}
			}
		}
		#endregion
	}
}
