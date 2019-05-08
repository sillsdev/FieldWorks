// Copyright (c) 2003-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Linq;
using LanguageExplorer.Controls.DetailControls.Resources;
using SIL.Code;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using SIL.LCModel.Application;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel.Core.Phonology;
using SIL.LCModel.Core.Text;
using SIL.LCModel.DomainServices;
using SIL.LCModel.Infrastructure;
using SIL.LCModel.Utils;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Main class for displaying the PhoneEnvReferenceSlice.
	/// </summary>
	internal class PhoneEnvReferenceView : RootSiteControl
	{
		#region Constants and data members

		// Fake flids.
		public const int kMainObjEnvironments = -5001;  // reference vector
		public const int kEnvStringRep = -5002;         // TsString
		public const int kErrorMessage = -5003;         // unicode
		public const int kDummyClass = -2;              // used to create a new environment
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
		// but which is not the one in the LcmCache.
		private PhoneEnvReferenceSda m_sda;
		private PhoneEnvReferenceVc m_PhoneEnvReferenceVc;
		private IMoForm m_rootObj;
		private int m_rootFlid;
		private int m_hvoOldSelection;
		private int m_wsVern;
		private PhonEnvRecognizer m_validator;
		private Dictionary<int, IPhEnvironment> m_realEnvs = new Dictionary<int, IPhEnvironment>();
		private SliceRightClickPopupMenuFactory _rightClickPopupMenuFactory;
		private Tuple<ContextMenuStrip, List<Tuple<ToolStripMenuItem, EventHandler>>> _contextMenuTuple;
		private System.ComponentModel.IContainer components = null;
		private int m_heightView;

		/// <summary>
		/// This allows the view to communicate size changes to the embedding slice.
		/// </summary>
		internal event FwViewSizeChangedEventHandler ViewSizeChanged;

		#endregion // Constants and data members

		#region Properties

		private ITsString DummyString => TsStringUtils.EmptyString(m_wsVern);

		#endregion Properties

		#region Construction, initialization, and disposal

		public PhoneEnvReferenceView()
		{
			InitializeComponent();
		}

		public void Initialize(IMoForm rootObj, int rootFlid, LcmCache cache)
		{
			Debug.Assert(rootObj is IMoAffixAllomorph || rootObj is IMoStemAllomorph);
			Debug.Assert(cache != null && m_cache == null);
			Cache = cache;
			ResetValidator();
			m_wsVern = m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle;
			m_rootObj = rootObj;
			m_rootFlid = rootFlid;
			if (RootBox == null)
			{
				MakeRoot();
			}
		}

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + ". ****** ");
			if (IsDisposed)
			{
				// No need to run it more than once.
				return;
			}

			base.Dispose(disposing);

			if (disposing)
			{
				components?.Dispose();
				m_realEnvs?.Clear();
			}
			m_validator = null; // TODO: Make m_validator disposable?
			m_realEnvs = null;
			m_rootObj = null;
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
			var hvos = m_rootFlid == MoAffixAllomorphTags.kflidPhoneEnv ? allomorph.PhoneEnvRC.ToHvoArray() : allomorph.PositionRS.ToHvoArray();
			CacheEnvironments(hvos);
			AppendPhoneEnv(kDummyPhoneEnvID, null);
		}

		private void CacheEnvironments(int[] realEnvHvos)
		{
			foreach (var realHvoEnv in realEnvHvos)
			{
				var env = m_cache.ServiceLocator.GetInstance<IPhEnvironmentRepository>().GetObject(realHvoEnv);
				AppendPhoneEnv(m_id, env.StringRepresentation);
				m_realEnvs[m_id] = env; // NB: m_id gets changed each pass through the loop.
				ValidateStringRep(m_id++);
			}
		}

		/// <summary>
		/// Add new item to the collection (added in the chooser).
		/// </summary>
		public void AddNewItem(IPhEnvironment env)
		{
			m_realEnvs[m_id] = env;
			var count = m_sda.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
			InsertPhoneEnv(m_id++, env.StringRepresentation, count - 1);
			RootBox.PropChanged(m_rootObj.Hvo, kMainObjEnvironments, count, 1, 0);
			m_heightView = RootBox.Height;
		}

		/// <summary>
		/// Remove an item from the collection (deleted in the chooser).
		/// </summary>
		public void RemoveItem(IPhEnvironment env)
		{
			var dummyHvo = 0;
			foreach (var kvp in m_realEnvs)
			{
				var realEnv = kvp.Value;
				if (realEnv != env)
				{
					continue;
				}
				dummyHvo = kvp.Key;
				break;
			}
			if (dummyHvo == 0)
			{
				return;
			}
			m_realEnvs.Remove(dummyHvo);
			var count = m_sda.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
			var loc = -1;
			for (var i = 0; i < count; ++i)
			{
				var hvo = m_sda.get_VecItem(m_rootObj.Hvo, kMainObjEnvironments, i);
				if (hvo != dummyHvo)
				{
					continue;
				}
				loc = i;
				break;
			}
			if (loc >= 0)
			{
				m_sda.CacheReplace(m_rootObj.Hvo, kMainObjEnvironments, loc, loc + 1, new int[0], 0);
				m_sda.DeleteObj(dummyHvo);
				RootBox.PropChanged(m_rootObj.Hvo, kMainObjEnvironments, loc, 0, 1);
			}
			m_heightView = RootBox.Height;
		}

		private void AppendPhoneEnv(int dummyHvo, ITsString rep)
		{
			if (rep == null)
			{
				rep = DummyString;
			}
			InsertPhoneEnv(dummyHvo, rep, m_sda.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments));
		}

		private void InsertPhoneEnv(int dummyHvo, ITsString rep, int location)
		{
			m_sda.CacheReplace(m_rootObj.Hvo, kMainObjEnvironments, location, location, new[] { dummyHvo }, 1);
			m_sda.SetString(dummyHvo, kEnvStringRep, rep);
		}

		#endregion // Construction, initialization, and disposal

		#region RootSite required methods

		public override void MakeRoot()
		{
			if (m_cache == null || DesignMode)
			{
				return;
			}
			m_PhoneEnvReferenceVc = new PhoneEnvReferenceVc(m_cache);
			m_sda = new PhoneEnvReferenceSda(m_cache.DomainDataByFlid as ISilDataAccessManaged);
			// Populate m_vwCache with data.
			ResynchListToDatabase();
			base.MakeRoot();
			RootBox.DataAccess = m_sda;
			RootBox.SetRootObject(m_rootObj.Hvo, m_PhoneEnvReferenceVc, kFragEnvironments, null);
			m_heightView = RootBox.Height;
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

		internal void SetCache(LcmCache cache)
		{
			m_cache = cache;
		}

		#region Other methods

		public void ResetValidator()
		{
			m_validator = new PhonEnvRecognizer(m_cache.LangProject.PhonologicalDataOA.AllPhonemes().ToArray(), m_cache.LangProject.PhonologicalDataOA.AllNaturalClassAbbrs().ToArray());
		}

		/// <summary>
		/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
		/// (See LT-8656 and LT-9119.)
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (m_rootObj.IsValidObject)
			{
				base.OnKeyPress(e);
			}
			else
			{
				e.Handled = true;
			}
		}

		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			if (vwselNew == null)
			{
				return;
			}
			// Get the string value for the old selection before it possibly changes due to a delete.
			ITsString tssOldSel = null;
			if (m_hvoOldSelection < 0)
			{
				tssOldSel = m_sda.get_StringProp(m_hvoOldSelection, kEnvStringRep);
			}
			base.HandleSelectionChange(rootb, vwselNew);
			ITsString tss;
			int ichAnchor;
			bool fAssocPrev;
			int hvoObj;
			int tag;
			// NB: This will be 0 after each call, since the string does
			int ws;
			// not have alternatives. Ws would be the WS of an alternative,
			// if there were any.
			vwselNew.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj, out tag, out ws); // start of string info
			int ichEnd;
			int hvoObjEnd;
			vwselNew.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjEnd, out tag, out ws); // end of string info
			if (hvoObjEnd != hvoObj)
			{   // owner of the end of the string in not the same as at the beginning - is this possible?
				CheckHeight();
				return;
			}
			if (m_hvoOldSelection < 0 && (hvoObj != m_hvoOldSelection || (tssOldSel != null && tssOldSel.Length > 0)))
			{   // the new selection is owned by a different object (left the selection) or the old one is not empty
				// Try to validate previously selected string rep.
				var tssOld = m_sda.get_StringProp(m_hvoOldSelection, kEnvStringRep);
				if (tssOld == null || tssOld.Length == 0)
				{
					// deleted or erased
					// Remove it from the dummy cache, since its length is 0.
					var limit = m_sda.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
					for (var i = 0; i < limit; ++i)
					{
						if (m_hvoOldSelection == m_sda.get_VecItem(m_rootObj.Hvo, kMainObjEnvironments, i))
						{
							RemoveFromDummyCache(i);
							break;
						}
					}
				}
				else if (hvoObj != m_hvoOldSelection)
				{
					// Left the selection, so validate previously selected string rep.
					ValidateStringRep(m_hvoOldSelection);
				}
			}
			if (hvoObj != kDummyPhoneEnvID)
			{
				// this is a "real" env, not the place holder for the next one
				// normal editing of the current env exits here
				m_hvoOldSelection = hvoObj;
				CheckHeight();
				return;
			}
			// if this is NOT the first pass for the original PropChanged
			// and the ts string is empty (tss.Text == null) then quit
			if (RootBox.IsPropChangedInProgress && tss.Length == 0)
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
			var count = m_sda.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
			var hvoNew = InsertNewEnv(count - 1);
			m_sda.SetString(hvoNew, kEnvStringRep, tss); // set the last env to the pattern
			// set a new empty env
			m_sda.SetString(kDummyPhoneEnvID, kEnvStringRep, DummyString);
			// Refresh to create a new view box for the DummyString? (doesn't seem to work)
			RootBox.PropChanged(m_rootObj.Hvo, kMainObjEnvironments, count - 1, 2, 1);
			// Set selection after the just added character or on the right side of the separator bar.
			var rgvsli = new SelLevInfo[1];
			rgvsli[0].cpropPrevious = 0;
			rgvsli[0].tag = kMainObjEnvironments;
			rgvsli[0].ihvo = count - 1;
			RootBox.MakeTextSelection(0, rgvsli.Length, rgvsli, tag, 0, ichAnchor, ichEnd, ws, fAssocPrev, -1, null, true);
			m_hvoOldSelection = hvoNew;
			CheckHeight();
		}

		/// <summary>
		/// Insert a new environment and return its ID. We assign it an arbitrary class id.
		/// </summary>
		private int InsertNewEnv(int ord)
		{
			return m_sda.MakeNewObject(kDummyClass, m_rootObj.Hvo, kMainObjEnvironments, ord);
		}

		private void CheckHeight()
		{
			if (RootBox == null)
			{
				return;
			}
			var hNew = RootBox.Height;
			if (m_heightView == hNew)
			{
				return;
			}
			ViewSizeChanged?.Invoke(this, new FwViewSizeEventArgs(hNew, RootBox.Width));
			m_heightView = hNew;
		}

		private void RemoveFromDummyCache(int index)
		{
			m_realEnvs.Remove(m_hvoOldSelection);
			m_sda.CacheReplace(m_rootObj.Hvo, kMainObjEnvironments, index, index + 1, new int[0], 0);
			RootBox?.PropChanged(m_rootObj.Hvo, kMainObjEnvironments, index, 0, 1);
		}


		private void ValidateStringRep(int hvoDummyObj)
		{
			var tss = m_sda.get_StringProp(hvoDummyObj, kEnvStringRep) ?? TsStringUtils.EmptyString(m_cache.DefaultAnalWs);
			var bldr = tss.GetBldr();
			if (m_validator.Recognize(tss.Text))
			{
				ClearSquigglyLine(hvoDummyObj, ref tss, ref bldr);
			}
			else
			{
				MakeSquigglyLine(hvoDummyObj, m_validator.ErrorMessage, ref tss, ref bldr);
			}
			if (m_realEnvs.ContainsKey(hvoDummyObj))
			{
				// Reset the original env, but only if it is invalid.
				var env = m_realEnvs[hvoDummyObj];
				if (!m_validator.Recognize(env.StringRepresentation.Text) && !env.StringRepresentation.Equals(tss))
				{
					UndoableUnitOfWorkHelper.Do(DetailControlsStrings.ksUndoChange, DetailControlsStrings.ksRedoChange, env, () => { env.StringRepresentation = tss; });
					ConstraintFailure failure;
					env.CheckConstraints(PhEnvironmentTags.kflidStringRepresentation, true, out failure, /* adjust the squiggly line */ true);
				}
			}
			m_sda.SetString(hvoDummyObj, kEnvStringRep, bldr.GetString());
			RootBox?.PropChanged(hvoDummyObj, kEnvStringRep, 0, tss.Length, tss.Length);
			CheckHeight();
		}

		private void ClearSquigglyLine(int hvo, ref ITsString tss, ref ITsStrBldr bldr)
		{
			bldr.SetIntPropValues(0, tss.Length, (int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum, (int)FwUnderlineType.kuntNone);
			m_sda.SetUnicode(hvo, kErrorMessage, string.Empty, 0);
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
		private void MakeSquigglyLine(int hvo, string validatorMessage, ref ITsString tss, ref ITsStrBldr bldr)
		{
			// the validator message, unfortunately, maybe invalid XML if
			// there were XML reserved characters in the environment.
			// until we get that fixed, at least don't crash, just draw squiggly under the entire word
			var pos = 0;
			try
			{
				var xdoc = XDocument.Parse(validatorMessage);
				var posAttr = xdoc.Root.Attribute("pos");
				pos = posAttr != null ? Convert.ToInt32(posAttr.Value) : 0;
			}
			catch
			{
			}
			var len = tss.Length;
			if (pos >= len)
			{
				pos = Math.Max(0, len - 1); // make sure something will show
			}
			var col = Color.Red;
			bldr.SetIntPropValues(pos, len, (int)FwTextPropType.ktptUnderline, (int)FwTextPropVar.ktpvEnum, (int)FwUnderlineType.kuntSquiggle);
			bldr.SetIntPropValues(pos, len, (int)FwTextPropType.ktptUnderColor, (int)FwTextPropVar.ktpvDefault, col.R + (col.B * 256 + col.G) * 256);
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
			RootBox.Reconstruct();
			CheckHeight();
		}

		/// <summary>
		/// This method clears the local cache of the any Environments that were stored
		/// for the slice and reloads them from the database.
		/// </summary>
		private void ResynchListToDatabase()
		{
			m_realEnvs.Clear();
			//We need to clear the cache since it is going to be repopulated
			m_sda.CacheVecProp(m_rootObj.Hvo, kMainObjEnvironments, new int[] { }, 0);
			// Populate m_vwCache with data.
			if (m_rootObj is IMoStemAllomorph)
			{
				CacheEnvironments((IMoStemAllomorph)m_rootObj);
			}
			else
			{
				CacheEnvironments((IMoAffixAllomorph)m_rootObj);
			}
		}

		/// <returns>
		/// First matching environment from environmentsHaystack or null if none
		/// found.
		/// </returns>
		private static IPhEnvironment GetEnvironmentFromHvo(ILcmOwningSequence<IPhEnvironment> environmentsHaystack, int hvoPattern)
		{
			return environmentsHaystack.FirstOrDefault(env => env.Hvo == hvoPattern);
		}

		/// <remarks>
		/// From local m_sda, not from database
		/// </remarks>
		private ITsString GetTsStringOfEnvironment(int localDummyHvoOfAnEnvironmentInEntry)
		{
			return m_sda.get_StringProp(localDummyHvoOfAnEnvironmentInEntry, kEnvStringRep) ?? TsStringUtils.EmptyString(m_cache.DefaultAnalWs);
		}

		/// <summary>
		/// From local m_sda, not from database
		/// </summary>
		private string GetStringOfEnvironment(int localDummyHvoOfAnEnvironmentInEntry)
		{
			return GetTsStringOfEnvironment(localDummyHvoOfAnEnvironmentInEntry).Text;
		}

		/// <summary>
		/// Integrate changes in dummy cache to real cache and DB.
		/// </summary>
		public void ConnectToRealCache()
		{
			// (FLEx) Review use of ISilDataAccess and other C++ cache related classes
			// If an Undo or Redo is in progress, we CAN'T save the changes. Ideally it wouldn't be necessary because making
			// any savable change in the slice would discard any pending Redo, and Undo would undo any changes in the slice
			// before undoing anything else. Currently Undo within the slice is not this well integrated. However, doing some editing
			// in the slice and then Undoing or Redoing a previous command DOES save the changes in the slice; I think OnLeave() must
			// be called somewhere in the process of invoking Undo before it is too late. This is not ideal behavior, but it
			// beats crashing.
			if (m_cache.ActionHandlerAccessor.IsUndoOrRedoInProgress)
			{
				return;
			}
			var frm = FindForm();
			WaitCursor wc = null;
			try
			{
				// frm will be null, if the record has been switched
				if (frm != null)
				{
					wc = new WaitCursor(frm);
				}
				// We're saving any changes to the real cache, so can no longer Undo/Redo local edits.
				CommitLocalEdits();
				// [NB: m_silCache is the same cache as m_vwCache, but is is a different cache than
				// m_cache.  m_cache has access to the database, and updates it, but
				// m_silCache does not.]
				if (DesignMode || RootBox == null
					// It may not be valid by now, since it may have been deleted.
					|| !m_rootObj.IsValidObject)
				{
					if (frm != null)
					{
						frm.Cursor = Cursors.Default;
					}
					return;
				}
				var fieldname = (m_rootFlid == MoAffixAllomorphTags.kflidPhoneEnv) ? "PhoneEnv" : "Position";
				m_cache.DomainDataByFlid.BeginUndoTask(string.Format(DetailControlsStrings.ksUndoSet, fieldname), string.Format(DetailControlsStrings.ksRedoSet, fieldname));
				var environmentFactory = m_cache.ServiceLocator.GetInstance<IPhEnvironmentFactory>();
				var allAvailablePhoneEnvironmentsInProject = m_cache.LanguageProject.PhonologicalDataOA.EnvironmentsOS;
				var envsBeingRequestedForThisEntry = EnvsBeingRequestedForThisEntry();
				// Environments just typed into slice that are not already used for
				// this entry or known about in the project.
				var newEnvsJustTyped = envsBeingRequestedForThisEntry.Where(localDummyHvoOfAnEnvInEntry => !allAvailablePhoneEnvironmentsInProject
							.Select(projectEnv => RemoveSpaces(projectEnv.StringRepresentation.Text)).Contains(RemoveSpaces(GetStringOfEnvironment(localDummyHvoOfAnEnvInEntry))));
				// Add the unknown/new environments to project
				foreach (var localDummyHvoOfAnEnvironmentInEntry in newEnvsJustTyped)
				{
					var envTssRep = GetTsStringOfEnvironment(localDummyHvoOfAnEnvironmentInEntry);
					var newEnv = environmentFactory.Create();
					allAvailablePhoneEnvironmentsInProject.Add(newEnv);
					newEnv.StringRepresentation = envTssRep;
				}
				var countOfExistingEnvironmentsInDatabaseForEntry = m_cache.DomainDataByFlid.get_VecSize(m_rootObj.Hvo, m_rootFlid);
				// Contains environments already in entry or recently selected in
				// dialog, but not ones just typed
				int[] existingListOfEnvironmentHvosInDatabaseForEntry;
				var chvoMax = m_cache.DomainDataByFlid.get_VecSize(m_rootObj.Hvo, m_rootFlid);
				using (var arrayPtr = MarshalEx.ArrayToNative<int>(chvoMax))
				{
					m_cache.DomainDataByFlid.VecProp(m_rootObj.Hvo, m_rootFlid, chvoMax, out chvoMax, arrayPtr);
					existingListOfEnvironmentHvosInDatabaseForEntry = MarshalEx.NativeToArray<int>(arrayPtr, chvoMax);
				}
				// Build up a list of real hvos used in database for the
				// environments in the entry
				var newListOfEnvironmentHvosForEntry = new List<int>();
				foreach (var localDummyHvoOfAnEnvironmentInEntry in envsBeingRequestedForThisEntry)
				{
					var envTssRep = GetTsStringOfEnvironment(localDummyHvoOfAnEnvironmentInEntry);
					var envStringRep = envTssRep.Text;
					// Pick a sensible environment from the known environments in
					// the project, by string
					var anEnvironmentInEntry = FindPhoneEnv(allAvailablePhoneEnvironmentsInProject, envStringRep, newListOfEnvironmentHvosForEntry.ToArray(), existingListOfEnvironmentHvosInDatabaseForEntry);
					// Maybe the ws has changed, so change the real env in database,
					// in case.
					anEnvironmentInEntry.StringRepresentation = envTssRep;
					var bldr = envTssRep.GetBldr();
					ConstraintFailure failure;
					if (anEnvironmentInEntry.CheckConstraints(PhEnvironmentTags.kflidStringRepresentation, false, out failure, true))
					{
						ClearSquigglyLine(localDummyHvoOfAnEnvironmentInEntry, ref envTssRep, ref bldr);
					}
					else
					{
						MakeSquigglyLine(localDummyHvoOfAnEnvironmentInEntry, failure.XmlDescription, ref envTssRep, ref bldr);
					}
					newListOfEnvironmentHvosForEntry.Add(anEnvironmentInEntry.Hvo);
					// Refresh
					m_sda.SetString(localDummyHvoOfAnEnvironmentInEntry, kEnvStringRep, bldr.GetString());
					RootBox.PropChanged(localDummyHvoOfAnEnvironmentInEntry, kEnvStringRep, 0, envTssRep.Length, envTssRep.Length);
				}
				// Only reset the main property, if it has changed.
				// Otherwise, the parser gets too excited about needing to reload.
				if (countOfExistingEnvironmentsInDatabaseForEntry != newListOfEnvironmentHvosForEntry.Count
				    || !equalArrays(existingListOfEnvironmentHvosInDatabaseForEntry, newListOfEnvironmentHvosForEntry.ToArray()))
				{
					m_cache.DomainDataByFlid.Replace(m_rootObj.Hvo, m_rootFlid, 0, countOfExistingEnvironmentsInDatabaseForEntry, newListOfEnvironmentHvosForEntry.ToArray(), newListOfEnvironmentHvosForEntry.Count);
				}
				m_cache.DomainDataByFlid.EndUndoTask();
			}
			finally
			{
				wc?.Dispose();
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
			var countOfThisEntrysEnvironments = m_sda.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments) - countOfDummyEnvsForTypingNewEnvs;
			for (var i = countOfThisEntrysEnvironments - 1; i >= 0; i--) // count down so deletions don't mess things up
			{
				var localDummyHvoOfAnEnvironmentInEntry = m_sda.get_VecItem(m_rootObj.Hvo, kMainObjEnvironments, i);
				// Remove and exclude blank entries
				var envStringRep = GetStringOfEnvironment(localDummyHvoOfAnEnvironmentInEntry);
				if (envStringRep == null || envStringRep.Trim().Length == 0)
				{
					m_realEnvs.Remove(localDummyHvoOfAnEnvironmentInEntry);
					// Remove it from the dummy cache.
					var oldSelId = m_hvoOldSelection;
					m_hvoOldSelection = localDummyHvoOfAnEnvironmentInEntry;
					RemoveFromDummyCache(i);
					m_hvoOldSelection = oldSelId;
					continue;
				}
				envsBeingRequestedForThisEntry.Add(localDummyHvoOfAnEnvironmentInEntry);
			}
			return envsBeingRequestedForThisEntry;
		}

		private static string RemoveSpaces(string s)
		{
			return s?.Replace(" ", null);
		}

		private static bool EqualsIgnoringSpaces(string a, string b)
		{
			if (a == null || b == null)
			{
				return false;
			}
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
		private static IPhEnvironment FindPhoneEnv(ILcmOwningSequence<IPhEnvironment> allProjectEnvs, string environmentPattern, int[] alreadyUsedHvos, int[] preferredHvos)
		{
			// Try to find a match in the preferred set that isn't already used
			var preferredMatches = preferredHvos.Where(preferredHvo => EqualsIgnoringSpaces(GetEnvironmentFromHvo(allProjectEnvs, preferredHvo).StringRepresentation.Text, environmentPattern)).ToList();
			if (preferredMatches.Any())
			{
				var unusedPreferred = preferredMatches.Except(alreadyUsedHvos);
				if (unusedPreferred.Any())
				{
					return GetEnvironmentFromHvo(allProjectEnvs, unusedPreferred.First());
				}
			}
			// Broaden where we look to all project environments
			var anyMatches = allProjectEnvs.Where(env => EqualsIgnoringSpaces(env.StringRepresentation.Text, environmentPattern)).ToList();
			if (!anyMatches.Any())
			{
				return null; // Shouldn't happen if adding envs new to project ahead of time.
			}
			// Try to return a match that isn't already used.
			var unused = anyMatches.Select(env => env.Hvo).Except(alreadyUsedHvos).ToList();
			return unused.Any() ? GetEnvironmentFromHvo(allProjectEnvs, unused.First()) : anyMatches.First();
		}

		/// <summary>
		/// Called when Undoing local typing no longer makes sense, because we've
		/// interpreted those changes or overridden them with changes at the object level.
		/// </summary>
		internal void CommitLocalEdits()
		{
			// Do nothing.
		}

		private static bool equalArrays(int[] v1, int[] v2)
		{
			if (v1.Length != v2.Length)
			{
				return false;
			}
			return !v1.Where((t, i) => t != v2[i]).Any();
		}

		#endregion // Other methods

		#region Menu support methods

		/// <summary>
		/// Get the string representation of the current selection in the environment list,
		/// plus the selection object and some of its internal information.
		/// </summary>
		/// <returns>false if selection spans two or more objects, true otherwise</returns>
		internal bool GetSelectedStringRep(out ITsString tss, out IVwSelection vwsel, out int hvoDummyObj, out int ichAnchor, out int ichEnd)
		{
			tss = null;
			vwsel = null;
			hvoDummyObj = 0;
			ichAnchor = 0;
			ichEnd = 0;
			try
			{
				bool fAssocPrev;
				int tag1;
				int ws1;
				vwsel = RootBox.Selection;
				vwsel.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoDummyObj, out tag1, out ws1);
				ITsString tss2;
				int hvoObjEnd;
				int tag2;
				int ws2;
				vwsel.TextSelInfo(true, out tss2, out ichEnd, out fAssocPrev, out hvoObjEnd, out tag2, out ws2);
				if (hvoDummyObj != hvoObjEnd)
				{
					return false;
				}
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
			string s;
			CanGetEnvironmentStringRep(out s);
			string sMsg;
			int pos;
			StringServices.CreateErrorMessageFromXml(s, m_validator.ErrorMessage, out pos, out sMsg);
			MessageBox.Show(sMsg, DetailControlsStrings.ksBadEnv, MessageBoxButtons.OK, MessageBoxIcon.Information);
		}

		internal bool CanShowEnvironmentError()
		{
			string s;
			if (CanGetEnvironmentStringRep(out s))
			{
				return (!m_validator.Recognize(s));
			}
			return false;
		}

		private bool CanGetEnvironmentStringRep(out string s)
		{
			int hvoDummyObj;
			int ichAnchor;
			int ichEnd;
			ITsString tss;
			IVwSelection vwsel;
			s = null;
			if (!GetSelectedStringRep(out tss, out vwsel, out hvoDummyObj, out ichAnchor, out ichEnd))
			{
				return false;
			}
			if (tss == null || hvoDummyObj == 0)
			{
				return false;
			}
			s = tss.Text;
			return !string.IsNullOrEmpty(s);
		}

		internal bool CanInsertSlash
		{
			get
			{
				int hvoDummyObj;
				int ichAnchor;
				int ichEnd;
				ITsString tss;
				IVwSelection vwsel;
				if (!GetSelectedStringRep(out tss, out vwsel, out hvoDummyObj, out ichAnchor, out ichEnd))
				{
					return false;
				}
				if (tss == null || hvoDummyObj == 0)
				{
					return true;
				}
				var s = tss.Text;
				return string.IsNullOrEmpty(s) || s.IndexOf('/') < 0;
			}
		}

		internal bool CanInsertEnvBar
		{
			get
			{
				int hvoDummyObj;
				int ichAnchor;
				int ichEnd;
				ITsString tss;
				IVwSelection vwsel;
				if (!GetSelectedStringRep(out tss, out vwsel, out hvoDummyObj, out ichAnchor, out ichEnd))
				{
					return false;
				}
				if (tss == null || hvoDummyObj == 0)
				{
					return false;
				}
				var s = tss.Text;
				if (string.IsNullOrEmpty(s))
				{
					return false;
				}
				var ichSlash = s.IndexOf('/');
				return ichSlash >= 0 && ichEnd > ichSlash && ichAnchor > ichSlash && s.IndexOf('_') < 0;
			}
		}

		internal bool CanInsertItem
		{
			get
			{
				int hvoDummyObj;
				int ichAnchor;
				int ichEnd;
				ITsString tss;
				IVwSelection vwsel;
				if (!GetSelectedStringRep(out tss, out vwsel, out hvoDummyObj, out ichAnchor, out ichEnd))
				{
					return false;
				}
				if (tss == null || hvoDummyObj == 0)
				{
					return false;
				}
				var s = tss.Text;
				return !string.IsNullOrEmpty(s) && PhonEnvRecognizer.CanInsertItem(s, ichEnd, ichAnchor);
			}
		}

		internal bool CanInsertHashMark
		{
			get
			{
				int hvoDummyObj;
				int ichAnchor;
				int ichEnd;
				ITsString tss;
				IVwSelection vwsel;
				if (!GetSelectedStringRep(out tss, out vwsel, out hvoDummyObj, out ichAnchor, out ichEnd))
				{
					return false;
				}
				if (tss == null || hvoDummyObj == 0)
				{
					return false;
				}
				var s = tss.Text;
				return !string.IsNullOrEmpty(s) && PhonEnvRecognizer.CanInsertHashMark(s, ichEnd, ichAnchor);
			}
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

		internal void SetRightClickContextMenuManager(SliceRightClickPopupMenuFactory rightClickPopupMenuFactory)
		{
			Guard.AgainstNull(rightClickPopupMenuFactory, nameof(rightClickPopupMenuFactory));

			_rightClickPopupMenuFactory = rightClickPopupMenuFactory;
		}

		#region Handle right click menu
		protected override bool OnRightMouseUp(Point pt, Rectangle rcSrcRoot, Rectangle rcDstRoot)
		{
			var sel = RootBox.MakeSelAt(pt.X, pt.Y, rcSrcRoot, rcDstRoot, false);
			var tsi = new TextSelInfo(sel);
			return HandleRightClickOnObject(tsi.Hvo(false));
		}

		protected override bool HandleContextMenuFromKeyboard(IVwSelection sel, Point center)
		{
			var tsi = new TextSelInfo(sel);
			return HandleRightClickOnObject(tsi.Hvo(false));
		}

		private bool HandleRightClickOnObject(int hvoDummy)
		{
			if (_contextMenuTuple != null)
			{
				_rightClickPopupMenuFactory.DisposePopupContextMenu(_contextMenuTuple);
				_contextMenuTuple = null;
			}
			if (hvoDummy == 0)
			{
				return false;
			}
			var contextMenuId = m_realEnvs.ContainsKey(hvoDummy)
				? Cache.DomainDataByFlid.MetaDataCache.GetDstClsId(m_rootFlid) == PhEnvironmentTags.kClassId ? ContextMenuName.mnuEnvReferenceChoices
				: ContextMenuName.mnuReferenceChoices : ContextMenuName.mnuEnvReferenceChoices;
			_contextMenuTuple = _rightClickPopupMenuFactory.GetPopupContextMenu(MySlice, contextMenuId);
			_contextMenuTuple.Item1.Show(new Point(Cursor.Position.X, Cursor.Position.Y));
			return true;
		}

		private Slice MySlice => this.ParentOfType<Slice>();

		#endregion
	}
}