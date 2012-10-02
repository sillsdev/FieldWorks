using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.CoreImpl;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.DomainImpl
{
	internal partial class RnResearchNbk
	{
		/// <summary>
		/// Gets all records and sub-records.
		/// </summary>
		/// <value>All records.</value>
		[VirtualProperty(CellarPropertyType.ReferenceCollection, "RnGenericRec")]
		public IEnumerable<IRnGenericRec> AllRecords
		{
			get
			{
				var records = new HashSet<IRnGenericRec>();
				foreach (var rec in RecordsOC)
					CollectRecords(rec, records);
				return records;
			}
		}

		private void CollectRecords(IRnGenericRec rec, HashSet<IRnGenericRec> records)
		{
			records.Add(rec);
			foreach (var subrec in rec.SubRecordsOS)
				CollectRecords(subrec, records);
		}

		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			if (e.Flid == RnResearchNbkTags.kflidRecords)
				UpdateAllRecords();

			base.AddObjectSideEffectsInternal(e);
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			if (e.Flid == RnResearchNbkTags.kflidRecords)
			{
				var rec = e.ObjectRemoved as IRnGenericRec;
				if (rec != null && rec.TextOA != null)
				{
					MoveTextToLangProject(rec);
				}
				UpdateAllRecords();
			}

			base.RemoveObjectSideEffectsInternal(e);
		}

		private void MoveTextToLangProject(IRnGenericRec rec)
		{
			var tex = rec.TextOA;
			Cache.LangProject.TextsOC.Add(tex);
		}

		internal void UpdateAllRecords()
		{
			var flid = m_cache.MetaDataCache.GetFieldId2(RnResearchNbkTags.kClassId, "AllRecords", false);

			var guids = (from rec in AllRecords
						 select rec.Guid).ToArray();

			((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(this, flid,
				new Guid[0], guids);
		}
	}

	internal partial class RnGenericRec
	{
		/// <summary>
		/// Initialize the DateCreated and DateModified values in the constructor.
		/// </summary>
		partial void SetDefaultValuesInConstruction()
		{
			m_DateCreated = DateTime.Now;
			m_DateModified = DateTime.Now;
		}

		/// <summary>
		/// Make a default RoledParticipant. This is where we put participants unless the user
		/// chooses to divide them into roles. Many users just list participants and are not aware of the intervening
		/// RoledParticipant object.
		/// </summary>
		public IRnRoledPartic MakeDefaultRoledParticipant()
		{
			var defaultRoledPartic = Services.GetInstance<IRnRoledParticFactory>().Create();
			ParticipantsOC.Add(defaultRoledPartic);
			return defaultRoledPartic;
		}

		public IEnumerable<ICmPossibility> Roles
		{
			get
			{
				return new HashSet<ICmPossibility>(from rp in ParticipantsOC
												   where rp.RoleRA != null
												   select rp.RoleRA);
			}
		}

		public IRnRoledPartic DefaultRoledParticipants
		{
			get
			{
				foreach (IRnRoledPartic roledPartic in ParticipantsOC)
				{
					if (roledPartic.RoleRA == null)
						return roledPartic;
				}
				return null;
			}
		}

		/// <summary>
		/// Gets the object which, for the indicated property of the recipient, the user is
		/// most likely to want to edit if the ReferenceTargetCandidates do not include the
		/// target he wants.
		/// The canonical example, supported by the default implementation of
		/// ReferenceTargetCandidates, is a possibility list, where the targets are the items.
		/// Subclasses which have reference properties edited by the simple list chooser
		/// should generally override either this or ReferenceTargetCandidates or both.
		/// The implementations of the two should naturally be consistent.
		/// </summary>
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			return ReferenceTargetServices.RnGenericRecReferenceTargetOwner(m_cache, flid);
		}

		/// <summary>
		/// Gets an ITsString that represents the shortname of this object.
		/// TODO (DamienD): register prop change when dependencies change
		/// </summary>
		/// <value></value>
		[VirtualProperty(CellarPropertyType.String)]
		public override ITsString ShortNameTSS
		{
			get
			{
				ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
				if (TypeRA != null)
					tisb.AppendTsString(TypeRA.Name.BestAnalysisAlternative);
				tisb.SetIntPropValues((int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault,
					m_cache.DefaultAnalWs);
				tisb.Append(" - ");
				tisb.AppendTsString(Title);
				tisb.Append(" - ");
				tisb.Append(DateModified.ToString("d"));
				return tisb.GetString();
			}
		}

		/// <summary>
		/// Gets an ITsString that combines the hierarchical position of the record with the
		/// Title of its top-level record.
		/// TODO (JohnT): register prop change when dependencies change
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString SubrecordOf
		{
			get
			{
				if (!(Owner is RnGenericRec))
					return Cache.TsStrFactory.EmptyString(Cache.DefaultAnalWs);
				var pattern = Strings.ksNumberOfParent; // typically "{0} of {1}".
				return FormatNumberOfParent(pattern);
			}
		}

		internal ITsString FormatNumberOfParent(string pattern)
		{
			string position = OutlineNumber;
			ITsString title = RootTitle;
			var bldr = Cache.MakeUserTss(pattern).GetBldr();
			// We need to emulate the relevant bit of string.format because of possible WS variations.
			int numberIndex = pattern.IndexOf("{0}");
			int titleIndex = pattern.IndexOf("{1}");
			if (numberIndex < titleIndex)
			{
				bldr.ReplaceTsString(titleIndex, titleIndex + 3, title);
				bldr.Replace(numberIndex, numberIndex + 3, position, null);
			}
			else
			{
				// Replace the last one first so as not to interfere with the other index.
				bldr.Replace(numberIndex, numberIndex + 3, position, null);
				bldr.ReplaceTsString(titleIndex, titleIndex + 3, title);
			}
			return bldr.GetString();
		}

		/// <summary>
		/// A sort key which sorts first by root record title, then by subrecord position.
		/// It achieves the sort we want for the SubrecordOf property.
		/// Note: called by reflection as a sortmethod for browse columns.
		/// Note that we don't support sorting this from the end; the parameter is currently required
		/// by the code in SortMethodFinder.CallSortMethod which invokes the sort method.
		/// </summary>
		public string SubrecordOfKey(bool sortedFromEnd)
		{
			if (!(Owner is RnGenericRec))
				return Title.Text ?? "";
			return (RootTitle.Text ?? "") + MiscUtils.NumbersAlphabeticKey(OutlineNumber);
		}
		private string OutlineNumber
		{
			get { return Cache.GetOutlineNumber(this, false, true); }
		}

		/// <summary>
		/// Title of the highest-level owning generic record.
		/// </summary>
		private ITsString RootTitle
		{
			get
			{
				ICmObject owningRec = this;
				while (owningRec.Owner is RnGenericRec)
					owningRec = owningRec.Owner;
				return ((RnGenericRec) owningRec).Title;
			}
		}

		partial void TitleSideEffects(ITsString originalValue, ITsString newValue)
		{
			//InternalServices.UnitOfWorkService.RegisterVirtualAsModified(this, "ShortNameTSS", ShortNameTSS);
			NoteSubrecordOfChanges(this, 0);
		}

		/// <summary>
		/// Register a PropChanged for the SubrecordOf property of all your subrecords (after and including
		/// the one at indexOfFirstChildToNote) and their children
		/// </summary>
		/// <param name="parent"></param>
		/// <param name="indexOfFirstChildToNote"></param>
		void NoteSubrecordOfChanges(RnGenericRec parent, int indexOfFirstChildToNote)
		{
			for (int i = indexOfFirstChildToNote; i < parent.SubRecordsOS.Count; i++)
			{
				var child = (RnGenericRec)parent.SubRecordsOS[i];
				InternalServices.UnitOfWorkService.RegisterVirtualAsModified(child, "SubrecordOf", child.SubrecordOf);
				NoteSubrecordOfChanges(child, 0);
			}
		}

		/// <summary>
		/// Gets a TsString that represents this object as it could be used in a deletion confirmation dialogue.
		/// </summary>
		/// <remarks>
		/// Subclasses should override this property, if they want to show something other than the regular ShortNameTSS.
		/// </remarks>
		public override ITsString DeletionTextTSS
		{
			get
			{
				var items = SubRecordsOS.Count;
				if (items > 0)
				{
					ITsIncStrBldr tisb = TsIncStrBldrClass.Create();
					tisb.AppendTsString(ShortNameTSS);
					tisb.Append(String.Format(SIL.FieldWorks.FDO.Application.ApplicationServices.AppStrings.ksNotebkDeleteSubRecords,items));
					return tisb.GetString();
				}
				return ShortNameTSS;
			}
		}

		partial void DateModifiedSideEffects(DateTime originalValue, DateTime newValue)
		{
			InternalServices.UnitOfWorkService.RegisterVirtualAsModified(this, "ShortNameTSS", ShortNameTSS);
		}


		/// <summary>
		/// Gets the shortest, non-abbreviated label for the content of this object.
		/// This is the name that you would want to show up in a chooser list.
		/// </summary>
		/// <value></value>
		public override string ShortName
		{
			get
			{
				return ShortNameTSS.Text;
			}
		}

		private void CheckNotNull(IStText newObjValue)
		{
			if (newObjValue == null)
				throw new InvalidOperationException("New value must not be null.");
		}

		/// <summary>
		/// For subrecords, this is an indexing number like "1." or "2.3." relative
		/// to the root record.  For root records, it's an empty string.
		/// </summary>
		[VirtualProperty(CellarPropertyType.String)]
		public ITsString IndexInOwnerTSS
		{
			get
			{
				ITsStrBldr bldr = m_cache.TsStrFactory.GetBldr();
				bldr.SetIntPropValues(0, 0,
					(int)FwTextPropType.ktptWs, (int)FwTextPropVar.ktpvDefault, m_cache.DefaultAnalWs);
				return SubrecordIndexTSS(bldr);
			}
		}

		private ITsString SubrecordIndexTSS(ITsStrBldr bldr)
		{
			if (Owner.ClassID != RnGenericRecTags.kClassId)
				return bldr.GetString();
			int idx = this.IndexInOwner + 1;
			bldr.Replace(0, 0, String.Format("{0}.", idx.ToString()), null);
			return (Owner as RnGenericRec).SubrecordIndexTSS(bldr);
		}

		partial void ValidateFurtherQuestionsOA(ref IStText newObjValue) { CheckNotNull(newObjValue);}
		partial void ValidateExternalMaterialsOA(ref IStText newObjValue)  { CheckNotNull(newObjValue);}
		partial void ValidateVersionHistoryOA(ref IStText newObjValue)  { CheckNotNull(newObjValue);}
		partial void ValidateDescriptionOA(ref IStText newObjValue) { CheckNotNull(newObjValue);}
		partial void ValidatePersonalNotesOA(ref IStText newObjValue) { CheckNotNull(newObjValue);}
		partial void ValidateConclusionsOA(ref IStText newObjValue) { CheckNotNull(newObjValue);}
		partial void ValidateResearchPlanOA(ref IStText newObjValue) { CheckNotNull(newObjValue); }
		partial void ValidateHypothesisOA(ref IStText newObjValue) { CheckNotNull(newObjValue); }
		partial void ValidateDiscussionOA(ref IStText newObjValue) { CheckNotNull(newObjValue); }

		protected override void AddObjectSideEffectsInternal(AddObjectEventArgs e)
		{
			if (e.Flid == RnGenericRecTags.kflidSubRecords)
			{
				var notebook = OwnerOfClass<RnResearchNbk>();
				notebook.UpdateAllRecords();

				int flid = m_cache.MetaDataCache.GetFieldId2(RnGenericRecTags.kClassId, "IndexInOwnerTSS", false);
				ITsString dummy = m_cache.TsStrFactory.MakeString("", m_cache.DefaultAnalWs);
				RegisterAllSubrecordIndexTSSChanged(this.SubRecordsOS, flid, dummy);
				NoteSubrecordOfChanges(this, e.Index);
			}
			base.AddObjectSideEffectsInternal(e);
		}

		private void RegisterAllSubrecordIndexTSSChanged(IFdoOwningSequence<IRnGenericRec> subs, int flid, ITsString dummy)
		{
			foreach (IRnGenericRec rec in subs)
			{
				((IServiceLocatorInternal)m_cache.ServiceLocator).UnitOfWorkService.RegisterVirtualAsModified(
					rec, flid, null, dummy);
				RegisterAllSubrecordIndexTSSChanged(rec.SubRecordsOS, flid, dummy);
			}
		}

		protected override void RemoveObjectSideEffectsInternal(RemoveObjectEventArgs e)
		{
			if (e.Flid == RnGenericRecTags.kflidSubRecords)
			{
				var notebook = OwnerOfClass<RnResearchNbk>();
				notebook.UpdateAllRecords();

				int flid = m_cache.MetaDataCache.GetFieldId2(RnGenericRecTags.kClassId, "IndexInOwnerTSS", false);
				ITsString dummy = m_cache.TsStrFactory.MakeString("", m_cache.DefaultAnalWs);
				RegisterAllSubrecordIndexTSSChanged(this.SubRecordsOS, flid, dummy);
				NoteSubrecordOfChanges(this, e.Index);
			}
			base.RemoveObjectSideEffectsInternal(e);
		}
	}

	internal partial class RnRoledPartic
	{
		public override ICmObject ReferenceTargetOwner(int flid)
		{
			if (flid == RnRoledParticTags.kflidParticipants)
				return m_cache.LanguageProject.PeopleOA;

			return base.ReferenceTargetOwner(flid);
		}
	}
}
