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
// File: PhoneEnvReferenceView.cs
// Responsibility: Randy Regnier
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;
using System.Xml;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.Framework.DetailControls.Resources;
using SIL.FieldWorks.Common.RootSites;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Validation;
using SIL.FieldWorks.FdoUi;
using SIL.FieldWorks.Common.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	/// <summary>
	/// Main class for displaying the PhoneEnvReferenceSlice.
	/// </summary>
	public class PhoneEnvReferenceView : RootSiteControl
	{
		#region Constants and data members

		// Fake flids.
		public const int kMainObjEnvironments = 5001;
		public const int kEnvStringRep = 5002;
		public const int kErrorMessage = 5003;

		//Fake Ids.
		public const int kDummyPhoneEnvID = -1;
		private int m_id = 1;

		// View frags.
		public const int kFragEnvironments = 1;
		public const int kFragPositions = 2;
		public const int kFragStringRep = 3;
		public const int kFragAnnotation = 4;
		public const int kFragEnvironmentObj = 5;

		// A cache used to interact with the Views code,
		// but which is not the one in the FdoCache.
		private IVwCacheDa m_vwCache;
		// A cast of m_vwCache.
		private ISilDataAccess m_silCache;
		private PhoneEnvReferenceVc m_PhoneEnvReferenceVc;
		private MoForm m_rootObj;
		private int m_rootFlid;
		private int m_hvoOldSelection = 0;
		private ITsStrFactory m_tsf = TsStrFactoryClass.Create();
		private int m_wsVern;
		private PhonEnvRecognizer m_validator;
		private Dictionary<int, PhEnvironment> m_realEnvs = new Dictionary<int, PhEnvironment>();

		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// This allows the view to communicate size changes to the embedding slice.
		/// </summary>
		public event SIL.FieldWorks.Common.Utils.FwViewSizeChangedEventHandler ViewSizeChanged;
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

		public void Initialize(MoForm rootObj, int rootFlid, FdoCache cache)
		{
			CheckDisposed();

			Debug.Assert(rootObj is MoAffixAllomorph || rootObj is MoStemAllomorph);
			Debug.Assert(cache != null && m_fdoCache == null);
			m_fdoCache = cache;
			ResetValidator();
			m_wsVern = m_fdoCache.LangProject.DefaultVernacularWritingSystem;
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
				if (m_PhoneEnvReferenceVc != null)
					m_PhoneEnvReferenceVc.Dispose();
			}
			m_validator = null; // TODO: Make m_validator disposable?
			m_realEnvs.Clear();
			m_realEnvs = null;
			m_rootObj = null;
			if (m_tsf != null)
				System.Runtime.InteropServices.Marshal.ReleaseComObject(m_tsf);
			m_tsf = null;
			m_silCache = null;
			if (m_vwCache != null)
				System.Runtime.InteropServices.Marshal.ReleaseComObject(m_vwCache);
			m_vwCache = null;
			m_PhoneEnvReferenceVc = null;
		}


		private void CacheEnvironments(MoStemAllomorph allomorph)
		{
			CacheEnvironments(allomorph.PhoneEnvRC.HvoArray);
			AppendPhoneEnv(kDummyPhoneEnvID, null);
		}

		private void CacheEnvironments(MoAffixAllomorph allomorph)
		{
			int[] hvos;
			if (m_rootFlid == (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPhoneEnv)
				hvos = allomorph.PhoneEnvRC.HvoArray;
			else
				hvos = allomorph.PositionRS.HvoArray;
			CacheEnvironments(hvos);
			AppendPhoneEnv(kDummyPhoneEnvID, null);
		}

		private void CacheEnvironments(int[] realEnvHvos)
		{
			foreach (int realHvoEnv in realEnvHvos)
			{
				PhEnvironment env = new PhEnvironment(m_fdoCache, realHvoEnv);
				AppendPhoneEnv(m_id, env.StringRepresentation.UnderlyingTsString);
				m_realEnvs[m_id] = env; // NB: m_id gets changed each pass through the loop.
				ValidateStringRep(m_id++);
			}
		}

		/// <summary>
		/// Add new item to the collection (added in the chooser).
		/// </summary>
		/// <param name="realHvo">ID of the envirnoment from the chooser.</param>
		public void AddNewItem(int realHvo)
		{
			CheckDisposed();
			PhEnvironment env = new PhEnvironment(m_fdoCache, realHvo);
			m_realEnvs[m_id] = env;
			int count = m_silCache.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
			InsertPhoneEnv(m_id++, env.StringRepresentation.UnderlyingTsString, count - 1);
			m_silCache.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
				m_rootObj.Hvo, kMainObjEnvironments, count, 1, 0);
			m_heightView = m_rootb.Height;
		}

		/// <summary>
		/// Remove an item from the collection (deleted in the chooser).
		/// </summary>
		/// <param name="realHvo">ID of the environment from the chooser.</param>
		public void RemoveItem(int realHvo)
		{
			CheckDisposed();
			int dummyHvo = 0;
			foreach (KeyValuePair<int, PhEnvironment> kvp in m_realEnvs)
			{
				PhEnvironment env = kvp.Value;
				if (env.Hvo == realHvo)
				{
					dummyHvo = kvp.Key;
					break;
				}
			}
			if (dummyHvo == 0)
				return;
			m_realEnvs.Remove(dummyHvo);
			int count = m_silCache.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
			int loc = -1;
			int hvo;
			for (int i = 0; i < count; ++i)
			{
				hvo = m_silCache.get_VecItem(m_rootObj.Hvo, kMainObjEnvironments, i);
				if (hvo == dummyHvo)
				{
					loc = i;
					break;
				}
			}
			if (loc >= 0)
			{
				m_vwCache.CacheReplace(m_rootObj.Hvo, kMainObjEnvironments, loc, loc + 1,
					new int[0], 0);
				m_silCache.DeleteObj(dummyHvo);
				m_silCache.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
					m_rootObj.Hvo, kMainObjEnvironments, loc, 0, 1);
			}
			m_heightView = m_rootb.Height;
		}

		private void AppendPhoneEnv(int dummyHvo, ITsString rep)
		{
			if(rep == null)
				rep = DummyString;

			InsertPhoneEnv(dummyHvo, rep,
				m_silCache.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments));
		}

		private void InsertPhoneEnv(int dummyHvo, ITsString rep, int location)
		{
			m_vwCache.CacheReplace(m_rootObj.Hvo, kMainObjEnvironments, location, location,
				new int[] {dummyHvo}, 1);
			m_vwCache.CacheStringProp(dummyHvo, kEnvStringRep, rep);
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
			m_vwCache = VwUndoDaClass.Create();
			m_silCache = (ISilDataAccess)m_vwCache;
			m_silCache.WritingSystemFactory = m_fdoCache.LanguageWritingSystemFactoryAccessor;
			IActionHandler handler = ActionHandlerClass.Create();
			m_silCache.SetActionHandler(handler);

			// Populate m_vwCache with data.
			ResynchListToDatabase();

			m_rootb = VwRootBoxClass.Create();
			m_rootb.SetSite(this);
			m_rootb.DataAccess = m_silCache;
			m_rootb.SetRootObject(m_rootObj.Hvo, m_PhoneEnvReferenceVc, kFragEnvironments,
				null);
			m_heightView = m_rootb.Height;
		}

		#endregion // RootSite required methods

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
			if (m_silCache.GetActionHandler().CanUndo())
			{
				display.Enabled = true;
				return true;
			}
			return false; // we don't want to handle the command.
		}

		/// <summary>
		/// We need to override Undo so that we can undo changes within the slice (in its own cache).
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		internal bool OnUndo(object args)
		{
			if (m_silCache.GetActionHandler().CanUndo())
			{
				m_silCache.GetActionHandler().Undo();
				return true;
			}
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
			if (m_silCache.GetActionHandler().CanRedo())
			{
				display.Enabled = true;
				return true;
			}
			return false; // we don't want to handle the command.
		}

		/// <summary>
		/// We need to override Redo so that we can undo changes within the slice.
		/// </summary>
		/// <param name="args"></param>
		/// <returns></returns>
		internal bool OnRedo(object args)
		{
			if (m_silCache.GetActionHandler().CanRedo())
			{
				m_silCache.GetActionHandler().Redo();
				return true;
			}
			return false;
		}

		#endregion UndoRedo

		#region Other methods

		public void ResetValidator()
		{
			CheckDisposed();
			m_validator = new PhonEnvRecognizer(PhPhoneme.PhonemeRepresentations(m_fdoCache),
				PhNaturalClass.ClassAbbreviations(m_fdoCache));
		}

		/// <summary>
		/// If the view's root object is valid, then call the base method.  Otherwise do nothing.
		/// (See LT-8656 and LT-9119.)
		/// </summary>
		protected override void OnKeyPress(KeyPressEventArgs e)
		{
			if (m_fdoCache.VerifyValidObject(m_rootObj))
				base.OnKeyPress(e);
			else
				e.Handled = true;
		}

		public override void SelectionChanged(IVwRootBox rootb, IVwSelection vwselNew)
		{
			CheckDisposed();
			if (vwselNew == null)
				return;
			bool hasFoc = Focused;

			base.SelectionChanged(rootb, vwselNew);

			ITsString tss;
			int ichAnchor;
			bool fAssocPrev;
			int hvoObj;
			int tag;
			int ws; // NB: This will be 0 after each call, since the string does
			// not have alternatives. Ws would be the WS of an alternative,
			// if there were any.
			vwselNew.TextSelInfo(false, out tss, out ichAnchor, out fAssocPrev, out hvoObj,
				out tag, out ws);

			int ichEnd;
			int hvoObjEnd;
			vwselNew.TextSelInfo(true, out tss, out ichEnd, out fAssocPrev, out hvoObjEnd,
				out tag, out ws);

			if (hvoObjEnd != hvoObj)
			{
				CheckHeight();
				return;
			}
			if (m_hvoOldSelection > 0 && hvoObj != m_hvoOldSelection)
			{
				// Try to validate previously selected string rep.
				if (m_silCache.get_StringProp(m_hvoOldSelection, kEnvStringRep).Length==0)
				{
					// Remove it from the dummy cache, since its length is 0.
					int limit = m_silCache.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
					for (int i = 0; i < limit; ++i)
					{
						if (m_hvoOldSelection ==
							m_silCache.get_VecItem(m_rootObj.Hvo, kMainObjEnvironments, i))
						{
							RemoveFromDummyCache(i);
							break;
						}
					}
				}
				else // Validate previously selected string rep.
				{
					ValidateStringRep(m_hvoOldSelection);
				}
			}
			if (hvoObj != kDummyPhoneEnvID)
			{
				m_hvoOldSelection = hvoObj;
				CheckHeight();
				return;
			}
			if (tss.Length == 0)
			{
				CheckHeight();
				return;
			}
			// Create a new object, and recreate a new empty object. Make this part of the Undo
			// Task with the character we typed.
			m_silCache.GetActionHandler().ContinueUndoTask();
			int count = m_silCache.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
			int hvoNew = InsertNewEnv(count - 1);
			m_silCache.SetString(hvoNew, kEnvStringRep, tss);
			m_silCache.SetString(kDummyPhoneEnvID, kEnvStringRep, DummyString);
			m_silCache.EndUndoTask();
			// Refresh
			m_silCache.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
				m_rootObj.Hvo, kMainObjEnvironments, count - 1, 2, 1);

			// Reset selection.
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
			return m_silCache.MakeNewObject(2, m_rootObj.Hvo, kMainObjEnvironments, ord);
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
			m_vwCache.CacheReplace(m_rootObj.Hvo, kMainObjEnvironments, index, index + 1,
				new int[0], 0);
			m_silCache.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
				m_rootObj.Hvo, kMainObjEnvironments, index, 0, 1);
		}


		private void ValidateStringRep(int hvoDummyObj)
		{
			ITsString tss = m_silCache.get_StringProp(hvoDummyObj, kEnvStringRep);
			ITsStrBldr bldr = tss.GetBldr();
			if (m_validator.Recognize(tss.Text))
				ClearSquigglyLine(hvoDummyObj, ref tss, ref bldr);
			else
				MakeSquigglyLine(hvoDummyObj, m_validator.ErrorMessage, ref tss, ref bldr);

			if (m_realEnvs.ContainsKey(hvoDummyObj))
			{
				// Reset the original env, but only if it is invalid.
				PhEnvironment env = m_realEnvs[hvoDummyObj];
				if (!m_validator.Recognize(env.StringRepresentation.Text) &&
					!env.StringRepresentation.UnderlyingTsString.Equals(tss))
				{
					env.StringRepresentation.UnderlyingTsString = tss;
					ConstraintFailure failure;
					env.CheckConstraints((int)PhEnvironment.PhEnvironmentTags.kflidStringRepresentation, out failure,
						/* adjust the squiggly line */ true );
				}
			}
			m_vwCache.CacheStringProp(hvoDummyObj, kEnvStringRep, bldr.GetString());
			m_silCache.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
				hvoDummyObj, kEnvStringRep, 0, tss.Length, tss.Length);
			CheckHeight();
		}

		private void ClearSquigglyLine(int hvo, ref ITsString tss, ref ITsStrBldr bldr)
		{
			bldr.SetIntPropValues(0, tss.Length,
				(int)FwTextPropType.ktptUnderline,
				(int)FwTextPropVar.ktpvEnum,
				(int)FwUnderlineType.kuntNone);
			m_vwCache.CacheUnicodeProp(hvo, kErrorMessage, "", 0);
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
			m_vwCache.CacheUnicodeProp(hvo, kErrorMessage, validatorMessage,
				validatorMessage.Length);
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
			m_vwCache.CacheVecProp(m_rootObj.Hvo, kMainObjEnvironments, new int[] { }, 0);
			// Populate m_vwCache with data.
			if (m_rootObj is MoStemAllomorph)
				CacheEnvironments((MoStemAllomorph)m_rootObj);
			else
				CacheEnvironments((MoAffixAllomorph)m_rootObj);
		}

		/// <summary>
		/// Integrate changes in dummy cache to real cache and DB.
		/// </summary>
		public void ConnectToRealCache()
		{
			CheckDisposed();
			// We're saving any changes to the real cache, so can no longer Undo/Redo local edits.
			CommitLocalEdits();
			Form frm = FindForm();
			// frm will be null, if the record has been switched
			if (frm != null)
				frm.Cursor = Cursors.WaitCursor;
			// [NB: m_silCache is the same cache as m_vwCache,
			// but is is a different cache than m_fdoCache.
			// m_fdoCache has access to the database, and updates it,
			// but m_silCache does not.]
			if (DesignMode
				|| m_rootb == null
				// It may not be valid by now, since it may have been deleted.
				|| !m_rootObj.IsValidObject())
			{
				if (frm != null)
					frm.Cursor = Cursors.Default;
				return;
			}
			FdoOwningSequence<IPhEnvironment> phoneEnvs =
				m_fdoCache.LangProject.PhonologicalDataOA.EnvironmentsOS;
			int count = m_silCache.get_VecSize(m_rootObj.Hvo, kMainObjEnvironments);
			// We need one less than the size,
			// because the last 'env' is a dummy that lets the user type a new one.
			int[] hvos = new int[count -1];
			int cvDel = 0;
			for (int i = hvos.Length - 1; i >= 0; --i)
			{
				IPhEnvironment env = null;
				int hvoDummyObj = m_silCache.get_VecItem(m_rootObj.Hvo, kMainObjEnvironments, i);
				ITsString tss = m_silCache.get_StringProp(hvoDummyObj, kEnvStringRep);
				ITsStrBldr bldr = tss.GetBldr();
				string rep = tss.Text;
				if (rep == null || rep.Length == 0)
				{
					// The environment at 'i' is being deleted, so
					// shrink the array of hvos that go into the real cache.
					cvDel++;
					m_realEnvs.Remove(hvoDummyObj);
					// Remove it from the dummy cache.
					int oldSelId = m_hvoOldSelection;
					m_hvoOldSelection = hvoDummyObj;
					RemoveFromDummyCache(i);
					m_hvoOldSelection = oldSelId;
				}
				else
				{
					foreach (IPhEnvironment envCurrent in phoneEnvs)
					{
						// Compare them without spaces, since they are not needed.
						if (envCurrent.StringRepresentation.Text != null &&
							envCurrent.StringRepresentation.Text.Replace(" ", null) ==
							rep.Replace(" ", null))
						{
							env = envCurrent;
							// Maybe the ws has changed, so change the real one, in case.
							env.StringRepresentation.UnderlyingTsString = tss;
							break;
						}
					}
					if (env == null)
					{
						env = phoneEnvs.Append(new PhEnvironment());
						env.StringRepresentation.UnderlyingTsString = tss;
						m_fdoCache.PropChanged(null, PropChangeType.kpctNotifyAll, m_fdoCache.LangProject.PhonologicalDataOA.Hvo, (int)PhPhonData.PhPhonDataTags.kflidEnvironments, phoneEnvs.Count - 1, 1, 0);
					}
					ConstraintFailure failure;
					if (env.CheckConstraints((int)PhEnvironment.PhEnvironmentTags.kflidStringRepresentation, out failure, /* adjust the squiggly line */ true))
						ClearSquigglyLine(hvoDummyObj, ref tss, ref bldr);
					else
						MakeSquigglyLine(hvoDummyObj, failure.XmlDescription, ref tss, ref bldr);
					hvos[i] = env.Hvo;
					// Refresh
					m_vwCache.CacheStringProp(hvoDummyObj, kEnvStringRep, bldr.GetString());
					m_silCache.PropChanged(null, (int)PropChangeType.kpctNotifyAll,
						hvoDummyObj, kEnvStringRep, 0, tss.Length, tss.Length);
				}
			}
			int[] newHvos = new int[hvos.Length];
			hvos.CopyTo(newHvos, 0);
			if (cvDel > 0)
			{
				newHvos = new int[hvos.Length - cvDel];
				count = 0;
				for (int i = 0; i < hvos.Length; ++i)
				{
					int tempHvo = hvos[i];
					if (tempHvo > 0)
						newHvos[count++] = tempHvo;
				}
			}
			count = m_fdoCache.GetVectorSize(m_rootObj.Hvo, m_rootFlid);
			// Only reset the main property, if it has changed.
			// Otherwise, the parser gets too excited about needing to reload.
			if ((count != newHvos.Length)
				|| !equalArrays(m_fdoCache.GetVectorProperty(m_rootObj.Hvo, m_rootFlid, true), newHvos))
			{
				string fieldname =
					(m_rootFlid == (int)MoAffixAllomorph.MoAffixAllomorphTags.kflidPhoneEnv)
					? "PhoneEnv" : "Position";
				m_fdoCache.BeginUndoTask(
					String.Format(DetailControlsStrings.ksUndoSet, fieldname),
					String.Format(DetailControlsStrings.ksRedoSet, fieldname));
				m_fdoCache.ReplaceReferenceProperty(m_rootObj.Hvo, m_rootFlid, 0, count,
					ref newHvos);
				m_fdoCache.EndUndoTask();
			}
			if (frm != null)
				frm.Cursor = Cursors.Default;
		}

		/// <summary>
		/// Called when Undoing local typing no longer makes sense, because we've
		/// interpreted those changes or overridden them with changes at the object level.
		/// </summary>
		internal void CommitLocalEdits()
		{
			m_silCache.GetActionHandler().Commit();
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
					int pos =0;
					PhEnvironment.CreateErrorMessageFromXml(s, m_validator.ErrorMessage, out pos, out sMsg);
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
			return PhEnvironment.CanInsertItem(s, ichEnd, ichAnchor);
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
			return PhEnvironment.CanInsertHashMark(s, ichEnd, ichAnchor);
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

		private bool HandleRightClickOnObject(int hvoDummy)
		{
			if (hvoDummy == 0)
				return false;

			if (m_realEnvs.ContainsKey(hvoDummy))
			{
				// This displays the "Show in Environments list" item in the popup menu, in
				// addition to all the Insert X" items.
				int hvo = m_realEnvs[hvoDummy].Hvo;
				ReferenceCollectionUi ui = new ReferenceCollectionUi(Cache, m_rootObj, m_rootFlid, hvo);
				return ui.HandleRightClick(Mediator, this, true);
			}
			else
			{
				// We need a CmObjectUi in order to call HandleRightClick().  This won't
				// display the "Show in Environments list" item in the popup menu.
				CmObjectUi ui = new CmObjectUi(m_rootObj);
				return ui.HandleRightClick(Mediator, this, true, "mnuEnvReferenceChoices");
			}
		}
		#endregion

		#region PhoneEnvReferenceVc class

		/// <summary>
		///  View constructor for creating the view details.
		/// </summary>
		public class PhoneEnvReferenceVc : VwBaseVc
		{
			private FdoCache m_cache;

			public PhoneEnvReferenceVc(FdoCache cache)
			{
				Debug.Assert(cache != null);
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
					//case PhoneEnvReferenceView.kFragAnnotation:
					//	break;
				default:
					throw new ArgumentException(
						"Don't know what to do with the given frag.", "frag");
				}
			}

			public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
			{
				CheckDisposed();
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
	}
}
