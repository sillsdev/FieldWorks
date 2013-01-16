// --------------------------------------------------------------------------------------------
#region /// Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2009' to='2010' company='SIL International'>
//    Copyright (c) 2010, SIL International. All Rights Reserved.
// </copyright>
#endregion
//
// Distributable under the terms of either the Common Public License or the
// GNU Lesser General Public License, as specified in the LICENSING.txt file.
//
// File: FdoRepositoryAdditions.cs
// Responsibility: FW Team
//
// <remarks>
// Add additional methods/properties to Repository interfaces in this file.
// Implementation of the additional interface information should go into the FdoRepositoryAdditions.cs file.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.FDO.DomainImpl;
using SIL.FieldWorks.FDO.Validation;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.CoreImpl;

namespace SIL.FieldWorks.FDO.Infrastructure.Impl
{
	#region AnalysisRepository class
	/// <summary>
	/// Implements the IAnalysisRepository interface.
	/// </summary>
	internal class AnalysisRepository : IAnalysisRepository
	{
		private readonly ICmObjectRepository m_everythingRepos;
		private readonly IWfiWordformRepository m_wordformRepos;
		private readonly IPunctuationFormRepository m_punctFormRepos;
		private readonly IWfiAnalysisRepository m_analysisRepos;
		private readonly IWfiGlossRepository m_glossRepos;

		/// <summary>
		/// Constructor
		/// </summary>
		internal AnalysisRepository(ICmObjectRepository everythingRepos, IWfiWordformRepository wordformRepos,
			IPunctuationFormRepository punctFormRepos, IWfiAnalysisRepository analysisRepos, IWfiGlossRepository glossRepos)
		{
			if (everythingRepos == null) throw new ArgumentNullException("everythingRepos");
			if (wordformRepos == null) throw new ArgumentNullException("wordformRepos");
			if (punctFormRepos == null) throw new ArgumentNullException("punctFormRepos");
			if (analysisRepos == null) throw new ArgumentNullException("analysisRepos");
			if (glossRepos == null) throw new ArgumentNullException("glossRepos");

			m_everythingRepos = everythingRepos;
			m_wordformRepos = wordformRepos;
			m_punctFormRepos = punctFormRepos;
			m_analysisRepos = analysisRepos;
			m_glossRepos = glossRepos;
		}

		#region Implementation of IRepository<IAnalysis>

		/// <summary>
		/// Get the object with the given ID.
		/// </summary>
		public IAnalysis GetObject(ICmObjectId id)
		{
			return (IAnalysis) m_everythingRepos.GetObject(id);
		}

		/// <summary>
		/// Get the object with the given id.
		/// </summary>
		/// <exception cref="KeyNotFoundException">Thrown if the object does not exist.</exception>
		public IAnalysis GetObject(Guid id)
		{
			return (IAnalysis)m_everythingRepos.GetObject(id);
		}

		/// <summary>
		/// Try to get a value for an uncertain guid.
		/// </summary>
		public bool TryGetObject(Guid guid, out IAnalysis obj)
		{
			ICmObject target;
			if (m_everythingRepos.TryGetObject(guid, out target))
			{
				obj = (IAnalysis) target;
				return true;
			}
			obj = null;
			return false;
		}

		/// <summary>
		/// Get the object with the given HVO.
		/// </summary>
		/// <exception cref="KeyNotFoundException">Thrown if the object does not exist.</exception>
		public IAnalysis GetObject(int hvo)
		{
			return (IAnalysis)m_everythingRepos.GetObject(hvo);
		}

		public bool TryGetObject(int hvo, out IAnalysis obj)
		{
			ICmObject result;
			if (m_everythingRepos.TryGetObject(hvo, out result))
			{
				obj = (IAnalysis) result;
				return true;
			}
			obj = null;
			return false;
		}

		/// <summary>
		/// Get all instances of the type.
		/// </summary>
		/// <returns>A Set of all instances. (There will be zero, or more instances in the IEnumerable.)</returns>
		public IEnumerable<IAnalysis> AllInstances()
		{
			var retval = new List<IAnalysis>(Count);
			retval.AddRange(from IAnalysis wf in m_wordformRepos.AllInstances()
							select wf);
			retval.AddRange(from IAnalysis anal in m_analysisRepos.AllInstances()
							select anal);
			retval.AddRange(from IAnalysis gloss in m_glossRepos.AllInstances()
							select gloss);
			retval.AddRange(from IAnalysis pf in m_punctFormRepos.AllInstances()
							select pf);
			return retval;
		}

		public IEnumerable<ICmObject> AllInstances(int classId)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Get the count of the objects.
		/// </summary>
		public int Count
		{
			get
			{
				return m_wordformRepos.Count
					+ m_analysisRepos.Count
					+ m_glossRepos.Count
					+ m_punctFormRepos.Count;
			}
		}

		#endregion
	}
	#endregion

	#region ConstChartMovedTextMarkerRepository class
	/// <summary>
	/// Implements the IConstChartMovedTextMarkerRepository interface.
	/// </summary>
	internal partial class ConstChartMovedTextMarkerRepository
	{
		/// <summary>
		/// Determines whether a ConstChartWordGroup has a MovedTextMarker
		/// that references it.
		/// </summary>
		/// <param name="ccwg"></param>
		/// <returns></returns>
		public bool WordGroupIsMoved(IConstChartWordGroup ccwg)
		{
			var refs = from ccmtm in AllInstances()
					   where ccmtm.WordGroupRA != null && ccmtm.WordGroupRA.Hvo == ccwg.Hvo
					   select ccmtm;
			return refs.Count() > 0;
		}
	}
	#endregion

	#region CmObjectRepository class
	internal partial class CmObjectRepository : ICmObjectRepositoryInternal
	{
		/// <summary>
		/// Flids on which EnsureCompleteIncomingRefs has been called.
		/// </summary>
		Set<int> m_completeIncomingRefs = new Set<int>();
		/// <summary>
		/// Objects created (not fluffed up from an external store) since this repo was set up.
		/// Note that some of these may have been deleted. That's rare enough that we live with
		/// the memory leak, since it is quite difficult to remove them when deleted. (The deletion
		/// might be undone, at which point we have to re-insert the object, but only if it was
		/// previously a newby...)
		/// </summary>
		HashSet<ICmObject> m_newInstancesThisSession = new HashSet<ICmObject>();

		private HashSet<int> m_classesWithNewInstancesThisSession = new HashSet<int>();
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Returns the class ID of the object with the specified HVO
		/// </summary>
		/// <param name="hvo">The HVO</param>
		/// <returns>The class ID of the object with the specified HVO</returns>
		/// <exception cref="KeyNotFoundException">If no object has the specified HVO</exception>
		/// ------------------------------------------------------------------------------------
		public int GetClsid(int hvo)
		{
			ICmObject obj = m_dataReader.GetObject(hvo);
			Debug.Assert(obj != null, "We assume that GetObject will throw an exception if the object doesn't exist");
			return obj.ClassID;
		}

		/// <summary>
		/// Returns true if this is an ID that can be looked up using GetObject().
		/// </summary>
		public bool IsValidObjectId(int hvo)
		{
			return m_dataReader.HasObject(hvo);
		}

		/// <summary>
		/// Returns true if this is an ID that can be looked up using GetObject().
		/// </summary>
		public bool IsValidObjectId(Guid guid)
		{
			return m_dataReader.HasObject(guid);
		}

		public ICmObjectOrId GetObjectOrIdWithHvoFromGuid(Guid guid)
		{
			return m_dataReader.GetObjectOrIdWithHvoFromGuid(guid);
		}

		public int GetHvoFromObjectOrId(ICmObjectOrId id)
		{
			return m_dataReader.GetHvoFromObjectOrId(id);
		}

		public bool WasCreatedThisSession(ICmObject obj)
		{
			return m_newInstancesThisSession.Contains(obj);
		}
		/// <summary>
		/// Answer true if instances of the specified class have been created this session, that is, since
		/// the program started up (or, more precisely, since this repository was instantiated).
		/// Note: may answer true, even if all such creations have been Undone.
		/// </summary>
		public bool InstancesCreatedThisSession(int classId)
		{
			return m_classesWithNewInstancesThisSession.Contains(classId);
		}

		private SimpleBag<ICmObject> m_focusedObjects;

		/// <summary>
		/// See interface defn.
		/// </summary>
		/// <param name="obj"></param>
		public void AddFocusedObject(ICmObject obj)
		{
			if (obj == null)
				return;
			m_focusedObjects.Add(obj);
		}

		/// <summary>
		/// See interface defn
		/// </summary>
		/// <param name="obj"></param>
		public void RemoveFocusedObject(ICmObject obj)
		{
			if (obj == null)
				return;
			m_focusedObjects.Remove(obj);
			if (m_focusedObjects.Occurrences(obj) == 0 && m_objectsToDeleteWhenNoLongerFocused.Contains(obj))
			{
				m_objectsToDeleteWhenNoLongerFocused.Remove(obj);
				var wf = (WfiWordform) obj; // for now this is the only kind of object we put in the set.

				NonUndoableUnitOfWorkHelper.DoSomehow(Cache.ActionHandlerAccessor, wf.DeleteIfSpurious);
			}
		}

		public bool IsFocused(ICmObject obj)
		{
			return m_focusedObjects.Occurrences(obj) > 0;
		}

		HashSet<ICmObject> m_objectsToDeleteWhenNoLongerFocused = new HashSet<ICmObject>();

		public void DeleteFocusedObjectWhenNoLongerFocused(ICmObject obj)
		{
			m_objectsToDeleteWhenNoLongerFocused.Add(obj);
		}


		public void RegisterObjectAsCreated(ICmObject newby)
		{
			m_newInstancesThisSession.Add(newby);
			m_classesWithNewInstancesThisSession.Add(newby.ClassID);
		}

		/// <summary>
		/// If the identity map for this ID contains a CmObject, return it.
		/// </summary>
		ICmObject ICmObjectRepositoryInternal.GetObjectIfFluffed(ICmObjectId id)
		{
			return m_dataReader.GetObjectIfFluffed(id);
		}

		/// <summary>
		/// Clear any necessary caches when an Undo or Redo occurs.
		/// </summary>
		public void ClearCachesOnUndoRedo()
		{
			((MoStemAllomorphRepository)Cache.ServiceLocator.GetInstance<IMoStemAllomorphRepository>()).ClearMonomorphemicMorphData();
		}

		/// <summary>
		/// Ensure that all objects having the specified flid have been fluffed up, and all instances of the
		/// specified property have true references to the actual objects, not just objectIDs. This means
		/// that the target object's m_incomingRefs will include a complete collection of references for
		/// this property.
		/// </summary>
		/// <param name="flid"></param>
		public void EnsureCompleteIncomingRefsFrom(int flid)
		{
			lock (SyncRoot)
			{
				if (m_completeIncomingRefs.Contains(flid))
					return; // No need to do it more than once per flid.
				var mdc = m_cache.ServiceLocator.MetaDataCache;
				var classId = mdc.GetOwnClsId(flid);
				switch (mdc.GetFieldType(flid))
				{
					case (int) CellarPropertyType.ReferenceAtomic:
						foreach (ICmObjectInternal obj in m_dataReader.AllInstances<ICmObject>(classId))
						{
							obj.GetObjectProperty(flid); // forces it to be fluffed up.
						}
						break;
					case (int) CellarPropertyType.ReferenceCollection:
					case (int) CellarPropertyType.ReferenceSequence:
						foreach (ICmObjectInternal obj in m_dataReader.AllInstances<ICmObject>(classId))
						{
							// JohnT: I think this call is needed to force the enumeration to be fully evaluated,
							// so every object in the vector gets fluffed.
							obj.GetVectorProperty(flid).LastOrDefault();
						}
						break;
					default:
						throw new ArgumentException("Can only ensure incoming refs from a reference flid");
				}
				m_completeIncomingRefs.Add(flid);
			}
		}
	}
	#endregion

	#region ConstituentChartCellPartRepository class
	internal partial class ConstituentChartCellPartRepository
	{
		/// <summary>
		/// Answer all the constituent chart cells that use this possibility to identify their column.
		/// </summary>
		public IEnumerable<IConstituentChartCellPart> InstancesWithChartCellColumn(ICmPossibility target)
		{
			((ICmObjectRepositoryInternal)m_cache.ServiceLocator.ObjectRepository).EnsureCompleteIncomingRefsFrom(
				ConstituentChartCellPartTags.kflidColumn);
			return ((ICmObjectInternal)target).IncomingRefsFrom(ConstituentChartCellPartTags.kflidColumn).Cast
					<IConstituentChartCellPart>();
		}
	}
	#endregion

	#region DsChartRepository class
	internal partial class DsChartRepository
	{
		/// <summary>
		/// Answer all the charts that use this possibility as their template.
		/// </summary>
		public IEnumerable<IDsChart> InstancesWithTemplate(ICmPossibility target)
		{
			// This is probably more efficient than selecting from IncomingRefsFrom, since a typical language project
			// has few charts, but some CmPossibilities have a LOT of incoming refs.
			return from chart in AllInstances() where chart.TemplateRA == target select chart;
		}
	}
	#endregion

	#region PhEnvironmentRepository class
	internal partial class PhEnvironmentRepository
	{
		#region Implementation of IPhEnvironmentRepository

		/// <summary>
		/// Get all IPhEnvironment that have no problem annotations.
		/// </summary>
		/// <returns></returns>
		public IEnumerable<IPhEnvironment> AllValidInstances()
		{
			PhonEnvRecognizer rec = PhEnvironment.CreatePhonEnvRecognizer(m_cache);
			return AllInstances().Where(env => rec.Recognize(env.StringRepresentation.Text));
		}

		#endregion
	}
	#endregion

	#region StTextRepository class
	internal partial class StTextRepository
	{
		public IList<IStText> GetObjects(IList<int> hvos)
		{
			return RepositoryUtils<IStTextRepository, IStText>.GetObjects(m_cache, hvos);
		}
	}
	#endregion

	#region StFootnoteRepository class
	internal partial class StFootnoteRepository
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a footnote from the object data property of a text
		/// </summary>
		/// <returns>The footnote referenced in the properties or <c>null</c> if the properties
		/// were not for a footnote ORC</returns>
		/// ------------------------------------------------------------------------------------
		public IStFootnote GetFootnoteFromObjData(string objData)
		{
			if (String.IsNullOrEmpty(objData))
				return null;
			Guid objGuid = TsStringUtils.GetHotGuidFromObjData(objData);
			if (objGuid == Guid.Empty)
				return null;

			// This used to throw an exception if not found, which we would probably want in
			// most cases, but some tests create bad data so we can verify how we will process
			// it.
			ICmObject footnote;
			if (m_cache.ServiceLocator.ObjectRepository.TryGetObject(objGuid, out footnote))
				return footnote as IStFootnote;
			return null;
		}
	}
	#endregion

	#region ScrFootnoteRepository class
	internal partial class ScrFootnoteRepository
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets a footnote from the ORC reference in text properties
		/// </summary>
		/// <param name="ttp">The text properties</param>
		/// <returns>The footnote referenced in the properties or <c>null</c> if the properties
		/// were not for a footnote ORC</returns>
		/// ------------------------------------------------------------------------------------
		public IScrFootnote GetFootnoteFromProps(ITsTextProps ttp)
		{
			Guid objGuid = TsStringUtils.GetHotObjectGuidFromProps(ttp);
			if (objGuid == Guid.Empty)
				return null;

			ICmObject obj;
			if (m_cache.ServiceLocator.GetInstance<ICmObjectRepository>().TryGetObject(objGuid, out obj))
				return obj as IScrFootnote;
			Debug.Fail("Guid is not in the repository.");
			return null;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tries to get a footnote from a guid.
		/// </summary>
		/// <param name="footnoteGuid">Guid that identifies a footnote</param>
		/// <param name="footnote">Footnote with footnoteGuid as its id</param>
		/// <returns><c>true</c> if the footnote is found; <c>false</c> otherwise</returns>
		/// ------------------------------------------------------------------------------------
		public bool TryGetFootnote(Guid footnoteGuid, out IScrFootnote footnote)
		{
			footnote = null;
			ICmObject obj;
			if (m_cache.ServiceLocator.ObjectRepository.TryGetObject(footnoteGuid, out obj) && obj is IScrFootnote)
			{
				footnote = (IScrFootnote) obj;
				return true;
			}
			// footnote not found or GUID is to a different object type - like a picture
			return false;
		}

	}
	#endregion

	#region StTxtParaRepository class
	internal partial class StTxtParaRepository
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="hvos"></param>
		/// <returns></returns>
		public IList<IStTxtPara> GetObjects(IList<int> hvos)
		{
			return RepositoryUtils<IStTxtParaRepository, IStTxtPara>.GetObjects(m_cache, hvos);
		}
	}
	#endregion

	#region CmAnnotationDefnRepository class
	internal partial class CmAnnotationDefnRepository
	{
		public ICmAnnotationDefn TranslatorAnnotationDefn
		{
			get { return GetObject(CmAnnotationDefnTags.kguidAnnTranslatorNote); }
		}

		public ICmAnnotationDefn ConsultantAnnotationDefn
		{
			get { return GetObject(CmAnnotationDefnTags.kguidAnnConsultantNote); }
		}

		public ICmAnnotationDefn CheckingError
		{
			get { return GetObject(CmAnnotationDefnTags.kguidAnnCheckingError); }
		}
	}
	#endregion

	#region PunctuationFormRepository class
	internal partial class PunctuationFormRepository : IPunctuationFormRepositoryInternal
	{
		private Dictionary<string, IPunctuationForm> m_punctFormFromForm;
		private Dictionary<OrcStringHashcode, IPunctuationForm> m_orcFormFromForm;

		/// <summary>
		/// Find the PunctuationForm that has the specified target as its form (WS and other properties are ignored).
		/// </summary>
		public bool TryGetObject(ITsString tssTarget, out IPunctuationForm pf)
		{
			lock (SyncRoot)
			{
				if (m_punctFormFromForm == null)
				{
					var instances = AllInstances();
					m_punctFormFromForm = new Dictionary<string, IPunctuationForm>(instances.Count());
					m_orcFormFromForm = new Dictionary<OrcStringHashcode, IPunctuationForm>();
					foreach (var pfT in instances)
						AddFormToCache(pfT);
				}
			}
			return m_punctFormFromForm.TryGetValue(tssTarget.Text, out pf)
				|| m_orcFormFromForm.TryGetValue(new OrcStringHashcode(tssTarget), out pf);
		}

		void IPunctuationFormRepositoryInternal.UpdateForm(ITsString oldForm, IPunctuationForm pf)
		{
			if (m_punctFormFromForm == null)
				return; // nothing cached.
			string oldKey = RemoveForm(oldForm) ? oldForm.Text : null;
			if (AddFormToCache(pf))
			{
				var action = new UndoUpdateFormAction(oldKey, pf.Form.Text, pf, m_punctFormFromForm);
				m_cache.ActionHandlerAccessor.AddAction(action);
			}
		}

		void IPunctuationFormRepositoryInternal.RemoveForm(ITsString oldForm)
		{
			if (RemoveForm(oldForm))
			{
				var action = new UndoUpdateFormAction(oldForm.Text, null, null, m_punctFormFromForm);
				m_cache.ActionHandlerAccessor.AddAction(action);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Adds the specified punctuation form to the internal cache
		/// </summary>
		/// <param name="pf">The punctuation form.</param>
		/// <returns>True if the form was added to the internal cache, false otherwise</returns>
		/// ------------------------------------------------------------------------------------
		private bool AddFormToCache(IPunctuationForm pf)
		{
			ITsString tssKey = (pf != null) ? pf.Form : null;
			if (tssKey == null || tssKey.Length == 0)
				return false;

			if (tssKey.Text != StringUtils.kszObject)
				m_punctFormFromForm[tssKey.Text] = pf;
			else
				m_orcFormFromForm[new OrcStringHashcode(tssKey)] = pf;
			return true;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tries to remove the given Form and indicates whether it did.
		/// </summary>
		/// <param name="oldForm">The old form.</param>
		/// ------------------------------------------------------------------------------------
		private bool RemoveForm(ITsString oldForm)
		{
			if (m_punctFormFromForm == null || oldForm == null || String.IsNullOrEmpty(oldForm.Text))
				return false;
			return (m_punctFormFromForm.Remove(oldForm.Text) || m_orcFormFromForm.Remove(new OrcStringHashcode(oldForm)));
		}

		#region OrcStringHashcode class
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Helper class for computing a fast hashcode for TsStrings whose only run is an ORC
		/// with a writing system and object data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class OrcStringHashcode
		{
			private readonly int m_ws;
			private readonly string m_objData;

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initialize a new instance of the <see cref="OrcStringHashcode"/> class.
			/// </summary>
			/// <param name="tss">A TsString</param>
			/// --------------------------------------------------------------------------------
			public OrcStringHashcode(ITsString tss)
			{
				if (tss == null || tss.Text != StringUtils.kszObject)
					m_ws = 0;
				else
				{
					ITsTextProps textProps = tss.get_Properties(0);
					Debug.Assert(textProps.IntPropCount == 1 && textProps.StrPropCount <= 1);
					int dummy;
					m_ws = textProps.GetIntPropValues((int)FwTextPropType.ktptWs, out dummy);
					m_objData = textProps.GetStrPropValue((int)FwTextPropType.ktptObjData) ?? string.Empty;
				}
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Determines whether the specified <see cref="T:System.Object"/> is equal to this instance.
			/// </summary>
			/// <param name="obj">The <see cref="T:System.Object"/> to compare with this instance.</param>
			/// --------------------------------------------------------------------------------
			public override bool Equals(object obj)
			{
				OrcStringHashcode otherObj = obj as OrcStringHashcode;
				// If either object has m_ws == 0, then it is a semi-bogus OrcStringHashcode and
				// is assumed to not be equal to any other. This can happen, for instance, if
				// the TSS used to initialize it was not an ORC.
				return otherObj != null && otherObj.m_ws > 0 && m_ws > 0 &&
					otherObj.m_ws == m_ws && otherObj.m_objData == m_objData;
			}

			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Returns a hash code for this instance.
			/// </summary>
			/// --------------------------------------------------------------------------------
			public override int GetHashCode()
			{
				return m_ws > 0 ? m_ws ^ m_objData.GetHashCode() : m_ws;
			}
		}
		#endregion

		#region UndoUpdateFormAction class
		class UndoUpdateFormAction : IUndoAction
		{
			private string m_oldKey;
			private string m_newKey;
			private IPunctuationForm m_pf;
			private Dictionary<string, IPunctuationForm> m_lookupTable;

			public UndoUpdateFormAction(string oldKey, string newKey, IPunctuationForm pf, Dictionary<string, IPunctuationForm>lookupTable)
			{
				m_oldKey = oldKey;
				m_newKey = newKey;
				m_pf = pf;
				m_lookupTable = lookupTable;
			}
			public bool Undo()
			{
				if (!String.IsNullOrEmpty(m_newKey))
					m_lookupTable.Remove(m_newKey);
				if (!String.IsNullOrEmpty(m_oldKey))
					m_lookupTable[m_oldKey] = m_pf;
				return true;
			}

			public bool Redo()
			{
				if (!String.IsNullOrEmpty(m_oldKey))
					m_lookupTable.Remove(m_oldKey);
				if (!String.IsNullOrEmpty(m_newKey))
					m_lookupTable[m_newKey] = m_pf;
				return true;
			}

			public void Commit()
			{
			}

			public bool IsDataChange
			{
				get { return false; }
			}

			public bool IsRedoable
			{
				get { return true; }
			}

			public bool SuppressNotification
			{
				set { }
			}
		}
		#endregion
	}
	#endregion

	#region ReversalIndexRepository class
	internal partial class ReversalIndexRepository
	{
		public IReversalIndex FindOrCreateIndexForWs(int ws)
		{
			var revIndex = (from index in AllInstances() where m_cache.ServiceLocator.WritingSystemManager.GetWsFromStr(index.WritingSystem) == ws select index).FirstOrDefault();
			if (revIndex == null)
			{
				NonUndoableUnitOfWorkHelper.DoUsingNewOrCurrentUOW(m_cache.ActionHandlerAccessor,
					() =>
						{
							revIndex = m_cache.ServiceLocator.GetInstance<IReversalIndexFactory>().Create();
							m_cache.LangProject.LexDbOA.ReversalIndexesOC.Add(revIndex);

							IWritingSystem wsObj = m_cache.ServiceLocator.WritingSystemManager.Get(ws);
							revIndex.WritingSystem = wsObj.Id;
							revIndex.Name.SetUserWritingSystem(wsObj.DisplayLabel);
						});
			}
			return revIndex;
		}
	}
	#endregion

	#region WfiMorphBundleRepository class
	internal partial class WfiMorphBundleRepository
	{
		/// <summary>
		/// Answer all the bundles that have the specified Sense as their target.
		/// </summary>
		public IEnumerable<IWfiMorphBundle> InstancesWithSense(ILexSense target)
		{
			((ICmObjectRepositoryInternal) m_cache.ServiceLocator.ObjectRepository).EnsureCompleteIncomingRefsFrom(
				WfiMorphBundleTags.kflidSense);
			return ((ICmObjectInternal) target).IncomingRefsFrom(WfiMorphBundleTags.kflidSense).Cast<IWfiMorphBundle>();
		}
	}
	#endregion

	#region WfiWordformRepository class
	internal partial class WfiWordformRepository : IWfiWordformRepositoryInternal
	{
		/// <summary>
		///
		/// </summary>
		/// <param name="tssForm"></param>
		/// <param name="fIncludeLowerCaseForm"></param>
		/// <param name="wf"></param>
		/// <returns></returns>
		public bool TryGetObject(ITsString tssForm, bool fIncludeLowerCaseForm, out IWfiWordform wf)
		{
			if (!TryGetObject(tssForm, out wf) && fIncludeLowerCaseForm)
			{
				int ws = TsStringUtils.GetWsAtOffset(tssForm, 0);
				// try finding a lowercase version.
				var cf = new CaseFunctions(m_cache.ServiceLocator.WritingSystemManager.Get(ws).IcuLocale);
				string lcForm = cf.ToLower(tssForm.Text);
				// we only want to look up the lower case form, if the given form was not already lowercased.
				if (lcForm != tssForm.Text)
					TryGetObject(TsStringUtils.MakeTss(lcForm, ws), false, out wf);
			}
			return wf != null;
		}

		private readonly Dictionary<int, Dictionary<string, IWfiWordform>> m_wordformFromForm = new Dictionary<int, Dictionary<string, IWfiWordform>>();

		/// <summary>
		/// Dictionary mapping the first word of a phrase (lowercased) to the body of the phrase.
		/// </summary>
		private Dictionary<string, HashSet<ITsString>> m_firstWordToPhrases;

		Dictionary<string, HashSet<ITsString>> IWfiWordformRepositoryInternal.FirstWordToPhrases
		{
			get
			{
				if (m_firstWordToPhrases != null)
					return m_firstWordToPhrases;
				m_firstWordToPhrases = new Dictionary<string, HashSet<ITsString>>();
				foreach (var wf in AllInstances())
					AddToPhraseDictionary(wf);
				return m_firstWordToPhrases;
			}
		}

		/// <summary>
		/// If (any of the forms of ) the worform is a phrase, add it to FirstWordToPhrases
		/// </summary>
		private void AddToPhraseDictionary(IWfiWordform wf)
		{
			foreach (var ws in wf.Form.AvailableWritingSystemIds)
			{
				var possiblePhrase = wf.Form.get_String(ws);
				((IWfiWordformRepositoryInternal)this).AddToPhraseDictionary(possiblePhrase);
			}
		}

		/// <summary>
		/// If the input string is a phrase, add it to FirstWordToPhrases.
		/// </summary>
		void IWfiWordformRepositoryInternal.AddToPhraseDictionary(ITsString possiblePhrase)
		{
			if (possiblePhrase == null || possiblePhrase.Length == 0 || m_firstWordToPhrases == null)
				return;
			string firstWordLowered;
			var firstWord = ParagraphParser.FirstWord(possiblePhrase, m_cache.WritingSystemFactory, out firstWordLowered);
			if (firstWordLowered == null || firstWordLowered.Length == possiblePhrase.Length)
				return;
			HashSet<ITsString> phrases;
			if (!m_firstWordToPhrases.TryGetValue(firstWordLowered, out phrases))
			{
				phrases = new HashSet<ITsString>();
				m_firstWordToPhrases[firstWordLowered] = phrases;
			}
			phrases.Add(possiblePhrase);
		}
		/// <summary>
		/// If the input string is a phrase, remove it from FirstWordToPhrases.
		/// </summary>
		void IWfiWordformRepositoryInternal.RemovePhraseFromDictionary(ITsString possiblePhrase)
		{
			if (possiblePhrase == null || possiblePhrase.Length == 0 || m_firstWordToPhrases == null)
				return;
			string firstWordLowered;
			var firstWord = ParagraphParser.FirstWord(possiblePhrase, m_cache.WritingSystemFactory, out firstWordLowered);
			if (firstWordLowered.Length == possiblePhrase.Length)
				return;
			HashSet<ITsString> phrases;
			if (m_firstWordToPhrases.TryGetValue(firstWordLowered, out phrases))
			{
				phrases.Remove(possiblePhrase);
			}
		}

		/// <summary>
		/// Find the Wordform that has the specified target as its form (in the WS of the first character of the target).
		/// </summary>
		/// <param name="tssTarget"></param>
		/// <param name="wf"></param>
		/// <returns></returns>
		public bool TryGetObject(ITsString tssTarget, out IWfiWordform wf)
		{
			var ws = TsStringUtils.GetWsAtOffset(tssTarget, 0);
			Dictionary<string, IWfiWordform> lookupTable;
			lock (SyncRoot)
			{
				if (!m_wordformFromForm.TryGetValue(ws, out lookupTable))
				{
					var instances = AllInstances();
					lookupTable = new Dictionary<string, IWfiWordform>(instances.Count());
					m_wordformFromForm[ws] = lookupTable;
					foreach (var wfT in instances)
					{
						ITsString tssKey = wfT.Form.get_String(ws);
						if (tssKey.Length > 0)
							lookupTable[tssKey.Text.Normalize(NormalizationForm.FormD)] = wfT;
					}
				}
			}
			return lookupTable.TryGetValue(tssTarget.Text.Normalize(NormalizationForm.FormD), out wf);
		}

		void IWfiWordformRepositoryInternal.UpdateForm(ITsString oldForm, IWfiWordform wf, int ws)
		{
			Dictionary<string, IWfiWordform> lookupTable;
			if (!m_wordformFromForm.TryGetValue(ws, out lookupTable))
				return; // this ws not yet cached.
			string oldKey = null;
			if (oldForm != null)
			{
				oldKey = oldForm.Text;
				if (!String.IsNullOrEmpty(oldForm.Text))
					lookupTable.Remove(oldForm.Text);
				((IWfiWordformRepositoryInternal)this).RemovePhraseFromDictionary(oldForm);
			}
			var newForm = wf.Form.get_String(ws);
			string key = newForm.Text;
			if (!String.IsNullOrEmpty(key))
				lookupTable[key] = wf;
			((IWfiWordformRepositoryInternal)this).AddToPhraseDictionary(newForm);
			UndoUpdateFormAction action = new UndoUpdateFormAction(oldForm, newForm, wf, this, lookupTable);
			m_cache.ActionHandlerAccessor.AddAction(action);
		}

		void IWfiWordformRepositoryInternal.RemoveForm(ITsString oldForm, int ws)
		{
			Dictionary<string, IWfiWordform> lookupTable;
			if (!m_wordformFromForm.TryGetValue(ws, out lookupTable))
				return; // this ws not yet cached.
			if (oldForm != null && !String.IsNullOrEmpty(oldForm.Text))
			{
				IWfiWordform wf; // needed so undo can put it back properly.
				if (lookupTable.TryGetValue(oldForm.Text, out wf))
				{
					lookupTable.Remove(oldForm.Text);
				}
				((IWfiWordformRepositoryInternal)this).RemovePhraseFromDictionary(oldForm);
				UndoUpdateFormAction action = new UndoUpdateFormAction(oldForm, null, wf, this, lookupTable);
				m_cache.ActionHandlerAccessor.AddAction(action);
			}
		}

		class UndoUpdateFormAction : UndoUpdateDictionaryAction<IWfiWordform>
		{
			private ITsString m_oldForm, m_newForm;
			private IWfiWordformRepositoryInternal m_wfRepo;
			public UndoUpdateFormAction(ITsString oldForm, ITsString newForm, IWfiWordform wf, IWfiWordformRepositoryInternal wfRepo,
				Dictionary<string, IWfiWordform> lookupTable)
				: base(oldForm == null ? null : oldForm.Text , newForm == null ? null : newForm.Text, wf, lookupTable)
			{
				m_oldForm = oldForm;
				m_newForm = newForm;
				m_wfRepo = wfRepo;
			}

			public override bool Undo()
			{
				base.Undo();
				m_wfRepo.AddToPhraseDictionary(m_oldForm);
				m_wfRepo.RemovePhraseFromDictionary(m_newForm);
				return true;
			}

			public override bool Redo()
			{
				base.Redo();
				m_wfRepo.AddToPhraseDictionary(m_newForm);
				m_wfRepo.RemovePhraseFromDictionary(m_oldForm);
				return true;
			}
		}


		/// <summary>
		/// Get an existing wordform that matches the given <paramref name="form"/>
		/// and <paramref name="ws"/>
		/// </summary>
		/// <param name="ws">Writing system of the form</param>
		/// <param name="form">Form to match, along with the writing system</param>
		/// <returns>The matching wordform, or null if not found.</returns>
		public IWfiWordform GetMatchingWordform(int ws, string form)
		{
			if (ws == 0) throw new ArgumentNullException("ws");
			if (string.IsNullOrEmpty(form)) throw new ArgumentNullException("form");

			// FWR-3119 Won't find a match if 'form' with diacritics is NFC
			// and wfiWordforms are NFD! This causes AddToDictionary to add duplicate each time.
			var decompForm = Cache.TsStrFactory.MakeString(form, ws).get_NormalizedForm(
				FwNormalizationMode.knmNFD).Text;

			return (from wf in AllInstances()
					where wf.Form.get_String(ws).Text == decompForm
					select wf).FirstOrDefault();
		}

		private bool m_occurrencesInTextsInitialized;

		bool IWfiWordformRepositoryInternal.OccurrencesInTextsInitialized
		{
			get
			{
				lock (SyncRoot)
					return m_occurrencesInTextsInitialized;
			}
		}

		void IWfiWordformRepositoryInternal.EnsureOccurrencesInTexts()
		{
			lock (SyncRoot)
			{
				if (m_occurrencesInTextsInitialized)
					return;

				foreach (var seg in m_cache.ServiceLocator.GetInstance<ISegmentRepository>().AllInstances())
				{
					foreach (var analysis in seg.AnalysesRS)
					{
						if (analysis.HasWordform)
							((WfiWordform) analysis.Wordform).AddOccurenceInText(seg);
					}
				}
				m_occurrencesInTextsInitialized = true;
			}
		}
	}
	#endregion

	#region MoMorphTypeRepository class
	internal partial class MoMorphTypeRepository
	{
		#region Implementation of IMoMorphTypeRepository

		/// <summary>
		/// Get the MoMorphType objects for the major types.
		/// </summary>
		/// <param name="mmtStem"></param>
		/// <param name="mmtPrefix"></param>
		/// <param name="mmtSuffix"></param>
		/// <param name="mmtInfix"></param>
		/// <param name="mmtBoundStem"></param>
		/// <param name="mmtProclitic"></param>
		/// <param name="mmtEnclitic"></param>
		/// <param name="mmtSimulfix"></param>
		/// <param name="mmtSuprafix"></param>
		public void GetMajorMorphTypes(out IMoMorphType mmtStem, out IMoMorphType mmtPrefix, out IMoMorphType mmtSuffix, out IMoMorphType mmtInfix, out IMoMorphType mmtBoundStem, out IMoMorphType mmtProclitic, out IMoMorphType mmtEnclitic, out IMoMorphType mmtSimulfix, out IMoMorphType mmtSuprafix)
		{
			mmtStem = null;
			mmtPrefix = null;
			mmtSuffix = null;
			mmtInfix = null;
			mmtBoundStem = null;
			mmtProclitic = null;
			mmtEnclitic = null;
			mmtSimulfix = null;
			mmtSuprafix = null;

			foreach (var mmt in AllInstances())
			{
				switch (mmt.Guid.ToString())
				{
					case MoMorphTypeTags.kMorphStem:
						mmtStem = mmt;
						break;
					case MoMorphTypeTags.kMorphPrefix:
						mmtPrefix = mmt;
						break;
					case MoMorphTypeTags.kMorphSuffix:
						mmtSuffix = mmt;
						break;
					case MoMorphTypeTags.kMorphInfix:
						mmtInfix = mmt;
						break;
					case MoMorphTypeTags.kMorphBoundStem:
						mmtBoundStem = mmt;
						break;
					case MoMorphTypeTags.kMorphProclitic:
						mmtProclitic = mmt;
						break;
					case MoMorphTypeTags.kMorphEnclitic:
						mmtEnclitic = mmt;
						break;
					case MoMorphTypeTags.kMorphSimulfix:
						mmtSimulfix = mmt;
						break;
					case MoMorphTypeTags.kMorphSuprafix:
						mmtSuprafix = mmt;
						break;
				}
			}
		}

		#endregion
	}
	#endregion

	internal partial class MoStemAllomorphRepository
	{
		private Dictionary<Tuple<int, string>, IMoStemAllomorph> m_monomorphemicMorphData;
		/// <summary>
		/// Return a dictionary keyed by ws/form pair of the stem allomorphs that can stand alone as wordforms.
		/// </summary>
		/// <returns></returns>
		internal Dictionary<Tuple<int, string>, IMoStemAllomorph> MonomorphemicMorphData()
		{
			if (m_monomorphemicMorphData == null)
			{
				m_monomorphemicMorphData = new Dictionary<Tuple<int, string>, IMoStemAllomorph>();
				// If the morph is either a proclitic or an enclitic, then it can stand alone; it does not have to have any
				// prefix or postfix even when such is defined for proclitic and/or enclitc.  So we augment the query to allow
				// these two types to be found without the appropriate prefix or postfix.  See LT-8124.
				foreach (var morph in from mf in AllInstances()
									  where
										/* check for orphans See UndoAllIssueTest */
										mf.Owner != null && mf.Owner.IsValidObject &&
										mf.MorphTypeRA != null &&
										mf.MorphTypeRA.Guid != MoMorphTypeTags.kguidMorphBoundRoot &&
										mf.MorphTypeRA.Guid != MoMorphTypeTags.kguidMorphBoundStem &&
										((String.IsNullOrEmpty(mf.MorphTypeRA.Prefix) && String.IsNullOrEmpty(mf.MorphTypeRA.Postfix)) ||
										 // most non-affix types
										 mf.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphEnclitic ||
										 mf.MorphTypeRA.Guid == MoMorphTypeTags.kguidMorphProclitic)
									  select mf)
				{
					foreach (var ws in morph.Form.AvailableWritingSystemIds)
					{
						var form = morph.Form.get_String(ws).Text;
						if (!String.IsNullOrEmpty(form))
						{
							var key = new Tuple<int, string>(ws, form);
							// JohnT: I think this is only important for tests, but for at least one test,
							// we want to record the first morpheme with a given key.
							if (!m_monomorphemicMorphData.ContainsKey(key))
								m_monomorphemicMorphData[key] = morph;
						}
					}
				}
			}
			return m_monomorphemicMorphData;
		}

		internal void ClearMonomorphemicMorphData()
		{
			m_monomorphemicMorphData = null; // regenerate when next needed.
		}
	}

	#region LexEntryRepository class
	internal partial class LexEntryRepository : ILexEntryRepositoryInternal
	{
		/// Returns the list of all the complex form LexEntry objects that refer to the specified
		/// LexEntry/LexSense in ShowComplexFormsIn.
		public IEnumerable<ILexEntry> GetVisibleComplexFormEntries(ICmObject mainEntryOrSense)
		{
			var retval = new Set<ILexEntry>();
			foreach (ILexEntryRef ler in m_cache.ServiceLocator.GetInstance<ILexEntryRefRepository>().AllInstances())
			{
				if (ler.RefType == LexEntryRefTags.krtComplexForm && ler.ShowComplexFormsInRS.Contains(mainEntryOrSense))
				{
					Debug.Assert(ler.Owner is ILexEntry);
					retval.Add(ler.Owner as ILexEntry);
				}
			}
			return SortEntries(retval);
		}
		/// Returns the list of all the complex form LexEntry objects that refer to the specified
		/// LexEntry/LexSense in ComponentLexemes.
		public IEnumerable<ILexEntry> GetComplexFormEntries(ICmObject mainEntryOrSense)
		{
			var entry = mainEntryOrSense as LexEntry;
			IEnumerable<ILexEntryRef> candidates;
			if (entry != null)
				candidates = entry.ComplexFormRefsWithThisComponentEntry;
			else
				candidates = ((LexSense) mainEntryOrSense).ComplexFormRefsWithThisComponentSense;
			var retval = new Set<ILexEntry>();
			foreach (ILexEntryRef ler in candidates)
			{
				if (ler.RefType == LexEntryRefTags.krtComplexForm)
				{
					Debug.Assert(ler.Owner is ILexEntry);
					retval.Add(ler.Owner as ILexEntry);
				}
			}
			return SortEntries(retval);
		}

		// A temporary cache for fast lookup of headwords (for sorting).
		// Cleared
		Dictionary<ILexEntry, string> m_cachedHeadwords = new Dictionary<ILexEntry, string>();

		internal void SomeHeadWordChanged()
		{
			m_cachedHeadwords.Clear();
		}

		public string HeadWordText(ILexEntry entry)
		{
			string result;
			if (m_cachedHeadwords.TryGetValue(entry, out result))
				return result;
			result = entry.HeadWord.Text;
			m_cachedHeadwords[entry] = result;
			return result;
		}

		IEnumerable<ILexEntry> SortEntries(IEnumerable<ILexEntry> input)
		{
			var retval = new List<ILexEntry>(input);
			var collator = Cache.LangProject.DefaultVernacularWritingSystem.Collator;
			retval.Sort((left, right) => collator.Compare(HeadWordText(left), HeadWordText(right)));
			return retval;
		}

		/// Returns the list of all the complex form LexEntry objects that refer to the specified
		/// LexEntry/LexSense in PrmimaryLexemes.
		public IEnumerable<ILexEntry> GetSubentries(ICmObject mainEntryOrSense)
		{
			var retval = new Set<ILexEntry>();
			foreach (ILexEntryRef ler in m_cache.ServiceLocator.GetInstance<ILexEntryRefRepository>().AllInstances())
			{
				if (ler.RefType == LexEntryRefTags.krtComplexForm && ler.PrimaryLexemesRS.Contains(mainEntryOrSense))
				{
					Debug.Assert(ler.Owner is ILexEntry);
					retval.Add(ler.Owner as ILexEntry);
				}
			}
			return SortEntries(retval);
		}

		/// Returns the list of all the variant form LexEntry objects that refer to the specified
		/// LexEntry/LexSense as the main entry or sense.
		public IEnumerable<ILexEntry> GetVariantFormEntries(ICmObject mainEntryOrSense)
		{
			Set<ILexEntry> retval = new Set<ILexEntry>();
			foreach (ILexEntryRef ler in m_cache.ServiceLocator.GetInstance<ILexEntryRefRepository>().AllInstances())
			{
				// For a variant, ComponentLexemes is all that matters; PrimaryLexemes is not used.
				if (ler.RefType == LexEntryRefTags.krtVariant && ler.ComponentLexemesRS.Contains(mainEntryOrSense))
				{
					Debug.Assert(ler.Owner is ILexEntry);
					retval.Add(ler.Owner as ILexEntry);
				}
			}
			return retval;
		}

		/// <summary>
		/// Dictionary to track homographs. Entry may be a single (non-homograph) entry, or a list of entries.
		/// Key is the HomographForm of each entry.
		/// </summary>
		Dictionary<string, object> m_homographInfo;

		/// <summary>
		/// Clear the list of homograph information
		/// </summary>
		public void ResetHomographs(ProgressBar progressBar)
		{
			m_homographInfo = null; // GetHomographs() will rebuild the homograph list
			Cache.LanguageProject.LexDbOA.ResetHomographNumbers(progressBar);
		}

		/// <summary>
		/// Return a list of all the homographs of the specified form.
		/// </summary>
		/// <param name="sForm">This form must come from LexEntry.HomographFormKey</param>
		/// <returns></returns>
		public List<ILexEntry> GetHomographs(string sForm)
		{
			lock (SyncRoot)
			{
				if (m_homographInfo == null)
				{
					// Review DamienD(JohnT): what should we do about locking here? This is USUALLY a read-only routine.
					// Maybe we can make a private variable, not copy it to m_homographInfo until done, and lock only
					// if we need to initialize it? We'd have to check after locking whether some other thread had
					// initialized it.
					var instances = AllInstances();
					// Build the homograph dictionary.
					m_homographInfo = new Dictionary<string, object>(instances.Count());
						// may be a little large, but most usually not homographs?
					foreach (var entry in instances)
					{
						entry.HomographNumber = 0;
						AddToHomographDict(entry);
					}
				}
			}
			object val;
			if (m_homographInfo.TryGetValue(sForm, out val))
			{
				if (val is List<ILexEntry>)
					// This serves both to eliminate deleted objects...deletion normally removes them, but I think
					// undoing creation does not...and to make a copy, so any changes the caller makes to the list
					// are not a problem.
					return (from entry in ((List<ILexEntry>) val) where entry.IsValidObject select entry).ToList();
				else
				{
					var entry = (ILexEntry) val;
					if (entry.IsValidObject)
					{
						var result = new List<ILexEntry>(1);
						result.Add((ILexEntry) val);
						return result; // already a new list, no need to copy.
					}
					// could remove the key, cleaning up, but we have to worry about locking if we change things
					// in a normally read-only routine.
				}
			}
			return new List<ILexEntry>(); // no known homographs of this string.
		}

		private void AddToHomographDict(ILexEntry entry)
		{
			string key = entry.HomographFormKey;
			object oldVal;
			if (m_homographInfo.TryGetValue(key, out oldVal))
			{
				List<ILexEntry> list = oldVal as List<ILexEntry>;
				if (list == null)
				{
					list = new List<ILexEntry>();
					m_homographInfo[key] = list;
					list.Add((ILexEntry)oldVal); // previously we optimized and just stored one.
				}
				list.Add(entry);
			}
			else
			{
				m_homographInfo[key] = entry; // first one.
			}
		}

		/// <summary>
		/// Update the homograph cache when an entry's HomographForm changes.
		/// </summary>
		void ILexEntryRepositoryInternal.UpdateHomographCache(ILexEntry entry, string oldHf)
		{
			if (m_homographInfo == null)
				return; // unlikely, but we don't have to maintain it if we haven't built it.
			object oldVal;
			if (m_homographInfo.TryGetValue(oldHf, out oldVal))
			{
				if (oldVal is ILexEntry)
					m_homographInfo.Remove(oldHf);
				else
				{
					var list = (List<ILexEntry>) oldVal;
					list.Remove(entry);
					if (list.Count == 0)
						m_homographInfo.Remove(oldHf);
				}
			}
			if (!m_cache.ObjectsBeingDeleted.Contains(entry))
				AddToHomographDict(entry);
		}
		/// <summary>
		/// This overload is useful to get a list of homographs for a given string, not starting
		/// with any particlar entry, but limited to ones compatible with the specified morph type.
		/// </summary>
		public List<ILexEntry> CollectHomographs(string sForm, IMoMorphType morphType)
		{
			return CollectHomographs(sForm, 0, GetHomographs(sForm), morphType);
		}

		/// <summary>
		/// Main method to collect all the homographs of the given form from the given list of entries.
		/// Set hvo to 0 to collect absolutely every matching homograph.
		/// </summary>
		public List<ILexEntry> CollectHomographs(string sForm, int hvo, List<ILexEntry> entries,
														IMoMorphType morphType)
		{
			return ((ILexEntryRepositoryInternal)this).CollectHomographs(sForm, hvo, entries, morphType, false);
		}

		/// <summary>
		/// Collect all the homographs of the given form from the given list of entries.  If fMatchLexForms
		/// is true, then match against lexeme forms even if citation forms exist.  (This behavior is needed
		/// to fix LT-6024 for categorized entry [now called Collect Words].)
		/// </summary>
		 List<ILexEntry> ILexEntryRepositoryInternal.CollectHomographs(string sForm, int hvo, List<ILexEntry> entries,
														  IMoMorphType morphType, bool fMatchLexForms)
		{
			if (sForm == null || sForm == String.Empty || sForm == Strings.ksQuestions)		// was "??", not "???"
				return new List<ILexEntry>(0);
			if (entries.Count == 0)
				return new List<ILexEntry>(0);

			var cache = entries[0].Cache;
			Debug.Assert(cache != null);
			var rgHomographs = new List<ILexEntry>();

			morphType = HomographMorphType(cache, morphType);

			foreach (var le in entries)
				{
					var homographForm = le.HomographFormKey;
					var lexemeHomograph = homographForm;
					if (fMatchLexForms)
						lexemeHomograph = StringServices.LexemeFormStatic(le);
					Debug.Assert(le != null);
					if (le.Hvo != hvo && (homographForm == sForm || lexemeHomograph == sForm))
					{
						var types = le.MorphTypes;
						foreach (var mmt in types)
						{
							if (HomographMorphType(cache, mmt) == morphType)
							{
								rgHomographs.Add(le);
								// Only add it once, even if it has multiple morph type matches.
								break;
							}
						}
						// Go ahead and use it, since it has no types at all, as may be the case
						// for entries created by the Rapid Data Entry tool.
						if (types.Count == 0)
							rgHomographs.Add(le);
					}
				}
			return rgHomographs;
		}

		/// <summary>
		/// Maps the specified morph type onto a canonical one that should be used in comparing two
		/// entries to see whether they are homographs.
		/// </summary>
		public IMoMorphType HomographMorphType(FdoCache cache, IMoMorphType morphType)
		{
			IMoMorphTypeRepository morphTypeRep = cache.ServiceLocator.GetInstance<IMoMorphTypeRepository>();
			// TODO: what about entries with mixed morph types?
			// Treat stems and roots as equivalent, bound or unbound, as well as entries with no
			// idea what they are.
			if (morphType == null ||
				morphType.Guid == MoMorphTypeTags.kguidMorphBoundRoot ||
					morphType.Guid == MoMorphTypeTags.kguidMorphBoundStem ||
						morphType.Guid == MoMorphTypeTags.kguidMorphRoot ||
							morphType.Guid == MoMorphTypeTags.kguidMorphParticle ||
								morphType.Guid == MoMorphTypeTags.kguidMorphPhrase ||
									morphType.Guid == MoMorphTypeTags.kguidMorphDiscontiguousPhrase ||
										morphType.Guid == MoMorphTypeTags.kguidMorphClitic)
			{
				morphType = morphTypeRep.GetObject(MoMorphTypeTags.kguidMorphStem);
			}
			return morphType;
		}
	}
	#endregion

	#region LexSenseRepository class
	internal partial class LexSenseRepository
	{
		/// <summary>
		/// Backreference property for SemanticDomain property of LexSense.
		/// </summary>
		public IEnumerable<ILexSense> InstancesWithSemanticDomain(ICmSemanticDomain domain)
		{
			return (domain as CmSemanticDomain).ReferringSenses;
		}

		/// <summary>
		/// Backreference property for ReversalEntries property of LexSense.
		/// </summary>
		public IEnumerable<ILexSense> InstancesWithReversalEntry(IReversalIndexEntry entry)
		{
			return (entry as ReversalIndexEntry).ReferringSenses;
		}
	}
	#endregion

	#region LexEntryRefRepository class
	internal partial class LexEntryRefRepository
	{
		/// <summary>
		/// Returns the list of all the variant LexEntryRef objects that refer to the specified
		/// LexEntry/LexSense as the main entry or sense.
		/// </summary>
		public IEnumerable<ILexEntryRef> GetVariantEntryRefsWithMainEntryOrSense(ICmObject mainEntryOrSense)
		{
			var entry = mainEntryOrSense as LexEntry;
			if (entry != null)
				return from ler in entry.EntryRefsWithThisMainEntry where ler.RefType == LexEntryRefTags.krtVariant select ler;
			var sense = mainEntryOrSense as LexSense;
			if (sense != null)
				return from ler in sense.EntryRefsWithThisMainSense where ler.RefType == LexEntryRefTags.krtVariant select ler;
			return new ILexEntryRef[0];
		}

		/// <summary>
		/// Returns the list of all the complex form LexEntryRef objects that refer to the specified
		/// LexEntry/LexSense as one of the primary entries or senses.
		/// </summary>
		public IEnumerable<ILexEntryRef> GetSubentriesOfEntryOrSense(ICmObject mainEntryOrSense)
		{
			var entry = mainEntryOrSense as LexEntry;
			if (entry != null)
				return entry.ComplexFormRefsWithThisPrimaryEntry;
			var sense = mainEntryOrSense as LexSense;
			if (sense != null)
				return sense.ComplexFormRefsWithThisPrimarySense;
			return new ILexEntryRef[0];
		}

		/// <summary>
		/// Default way of sorting lex entry refs.
		/// </summary>
		/// <param name="input"></param>
		/// <returns></returns>
		public IEnumerable<ILexEntryRef> SortEntryRefs(IEnumerable<ILexEntryRef> input)
		{
			var retval = new List<ILexEntryRef>(input);
			var collator = Cache.LangProject.DefaultVernacularWritingSystem.Collator;
			var entryRepo = (LexEntryRepository) Cache.ServiceLocator.GetInstance<ILexEntryRepository>();
			retval.Sort((left, right) => collator.Compare(entryRepo.HeadWordText((ILexEntry)left.Owner), entryRepo.HeadWordText((ILexEntry)right.Owner)));
			return retval;
		}

		/// <summary>
		/// Returns the list of all the complex form LexEntryRef objects that refer to the specified
		/// LexEntry/LexSense in ShowComplexFormIn.
		/// </summary>
		public IEnumerable<ILexEntryRef> VisibleEntryRefsOfEntryOrSense(ICmObject mainEntryOrSense)
		{
			var entry = mainEntryOrSense as LexEntry;
			if (entry != null)
				return entry.ComplexFormRefsVisibleInThisEntry;
			var sense = mainEntryOrSense as LexSense;
			if (sense != null)
				return sense.ComplexFormRefsVisibleInThisSense;
			return new ILexEntryRef[0];
		}

		/// <summary>
		/// Returns the list of all the complex form LexEntryRef objects that refer to any of the specified
		/// LexEntry/LexSense as one of the primary entries or senses.
		/// </summary>
		public IEnumerable<ILexEntryRef> GetSubentriesOfEntryOrSense(IEnumerable<ICmObject> targets)
		{
			return from item in targets from entry in GetSubentriesOfEntryOrSense(item) select entry;
		}

		/// <summary>
		/// Returns the list of all the complex form LexEntryRef objects that refer to any of the specified
		/// LexEntry/LexSense in ShowComplexFormIn.
		/// </summary>
		public IEnumerable<ILexEntryRef> VisibleEntryRefsOfEntryOrSense(IEnumerable<ICmObject> targets)
		{
			return from item in targets from entry in VisibleEntryRefsOfEntryOrSense(item) select entry;
		}

	}
	#endregion

	#region LexReferenceRepository class
	internal partial class LexReferenceRepository
	{
		/// <summary>
		/// Returns the list of all the LexReference objects that refer to the specified
		/// LexEntry/LexSense as a target.
		/// </summary>
		public IEnumerable<ILexReference> GetReferencesWithTarget(ICmObject target)
		{
			var entry = target as LexEntry;
			if (entry != null)
				return entry.ReferringLexReferences;
			var sense = target as LexSense;
			if (sense != null)
				return sense.ReferringLexReferences;
			return new ILexReference[0];
		}
	}
	#endregion

	#region PhSequenceContextRepository class
	internal partial class PhSequenceContextRepository
	{
		/// <summary>
		/// Returns all sequence contexts that contain the specified context.
		/// </summary>
		/// <param name="ctxt">The context.</param>
		/// <returns></returns>
		public IEnumerable<IPhSequenceContext> InstancesWithMember(IPhPhonContext ctxt)
		{
			return from seqCtxt in AllInstances()
				   where seqCtxt.MembersRS.Contains(ctxt)
				   select seqCtxt;
		}
	}
	#endregion

	#region PhIterationContextRepository class
	internal partial class PhIterationContextRepository
	{
		/// <summary>
		/// Returns all iteration contexts that iterate over the specified context.
		/// </summary>
		/// <param name="ctxt">The context.</param>
		/// <returns></returns>
		public IEnumerable<IPhIterationContext> InstancesWithMember(IPhPhonContext ctxt)
		{
			return from iterCtxt in AllInstances()
				   where iterCtxt.MemberRA == ctxt
				   select iterCtxt;
		}
	}
	#endregion

	#region PhSimpleContextSegRepository class
	internal partial class PhSimpleContextSegRepository
	{
		/// <summary>
		/// Returns all phoneme contexts that reference the specified phoneme.
		/// </summary>
		/// <param name="phoneme">The phoneme.</param>
		/// <returns></returns>
		public IEnumerable<IPhSimpleContextSeg> InstancesWithPhoneme(IPhPhoneme phoneme)
		{
			return from segCtxt in AllInstances()
				   where segCtxt.FeatureStructureRA == phoneme
				   select segCtxt;
		}
	}
	#endregion

	#region PhSimpleContextNCRepository class
	internal partial class PhSimpleContextNCRepository
	{
		/// <summary>
		/// Returns all natural class contexts that reference the specified natural class.
		/// </summary>
		/// <param name="nc">The natural class.</param>
		/// <returns></returns>
		public IEnumerable<IPhSimpleContextNC> InstancesWithNC(IPhNaturalClass nc)
		{
			return from ncCtxt in AllInstances()
				   where ncCtxt.FeatureStructureRA == nc
				   select ncCtxt;
		}
	}
	#endregion

	#region ScrCheckRunRepository class
	internal partial class ScrCheckRunRepository
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the instance of the run history for the given check if any.
		/// </summary>
		/// <param name="bookId">The canonical number (1-based) of the desired book</param>
		/// <param name="checkId">A GUID that uniquely identifies the editorial check</param>
		/// <returns>The run history for the requested check or <c>null</c></returns>
		/// ------------------------------------------------------------------------------------
		public IScrCheckRun InstanceForCheck(int bookId, Guid checkId)
		{

			IScrBookAnnotations annotations =
				m_cache.ServiceLocator.GetInstance<IScrBookAnnotationsRepository>().InstanceForBook(bookId);
			foreach (IScrCheckRun run in annotations.ChkHistRecsOC)
			{
				if (run.CheckId == checkId)
					return run;
			}
			return null;
		}
	}
	#endregion

	#region ScrDraftRepository class
	internal partial class ScrDraftRepository
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the specified ScrDraft.
		/// </summary>
		/// <param name="description">The description of the ScrDraft to find.</param>
		/// <param name="draftType">Type of the ScrDraft to find.</param>
		/// ------------------------------------------------------------------------------------
		public IScrDraft GetDraft(string description, ScrDraftType draftType)
		{
			return AllInstances().FirstOrDefault(draft => draft.Type == draftType && draft.Description == description);
		}
	}
	#endregion

	#region ScrBookAnnotationsRepository class
	internal partial class ScrBookAnnotationsRepository
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the annotations for the given book.
		/// </summary>
		/// <param name="bookId">The canonical number (1-based) of the desired book</param>
		/// <returns>The annotations for the requested book</returns>
		/// ------------------------------------------------------------------------------------
		public IScrBookAnnotations InstanceForBook(int bookId)
		{
			IScripture scr =
				m_cache.ServiceLocator.GetInstance<IScriptureRepository>().AllInstances().First();
			return scr.BookAnnotationsOS[bookId - 1];
		}
	}
	#endregion

	#region PublicationRepository class
	internal partial class PublicationRepository
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Finda a publication with the given name.
		/// </summary>
		/// <param name="name">Name of the desired publication</param>
		/// <returns>The publication or null if no matching publicaiton is found</returns>
		/// ------------------------------------------------------------------------------------
		public IPublication FindByName(string name)
		{
			foreach (var pub in AllInstances())
				if (pub.Name == name)
					return pub;
			return null;
		}
	}
	#endregion
}
