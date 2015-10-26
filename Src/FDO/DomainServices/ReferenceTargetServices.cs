// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Practices.ServiceLocation;
using SIL.FieldWorks.FDO.Infrastructure;

namespace SIL.FieldWorks.FDO.DomainServices
{
	/// <summary>
	/// This class provides a place to put methods that indicate the possible targets of reference attributes,
	/// for the cases where we need access to them apart from calling ReferenceTargetCandidates on an instance.
	/// </summary>
	public class ReferenceTargetServices
	{
		/// <summary>
		/// Return the object (actually a possibility list) that owns the possible targets for the
		/// properties of a particular field of an RnGenericRec.
		/// </summary>
		public static ICmObject RnGenericRecReferenceTargetOwner(FdoCache cache, int flid)
		{
			switch (flid)
			{
				case RnGenericRecTags.kflidAnthroCodes:
					return cache.LanguageProject.AnthroListOA;
				case RnGenericRecTags.kflidConfidence:
					return cache.LanguageProject.ConfidenceLevelsOA;
				case RnGenericRecTags.kflidLocations:
					return cache.LanguageProject.LocationsOA;
				case RnGenericRecTags.kflidResearchers:
				case RnGenericRecTags.kflidSources:
					return cache.LanguageProject.PeopleOA;
				case RnGenericRecTags.kflidRestrictions:
					return cache.LanguageProject.RestrictionsOA;
				case RnGenericRecTags.kflidStatus:
					return cache.LanguageProject.StatusOA;
				case RnGenericRecTags.kflidTimeOfEvent:
					return cache.LanguageProject.TimeOfDayOA;
				case RnGenericRecTags.kflidType:
					return cache.LanguageProject.ResearchNotebookOA.RecTypesOA;
				case RnGenericRecTags.kflidParticipants:
					// This one is anomolous because it is an owning property of RnGenericRec.
					// However supporting it makes it easier to set up the ghost slice for participants
					// in the Text Info tab. The RnRoledPartic is something most users are not even aware of.
					// They choose items from the People list.
					return cache.LanguageProject.PeopleOA;
				default:
					return CmObjectReferenceTargetOwner(cache, flid);
			}
		}

		/// <summary>
		/// Return the object (actually a possibility list) that owns the possible targets for the
		/// properties a particular field of of a CmObject (if any). This only handles the default,
		/// that is, any custom fields. Some more specific method should be used (perhaps created)
		/// if you need the object for some real field.
		/// </summary>
		public static ICmObject CmObjectReferenceTargetOwner(FdoCache cache, int flid)
		{
			var mdc = cache.ServiceLocator.GetInstance<IFwMetaDataCacheManaged>();
			if (mdc.IsCustom(flid))
			{
				Guid listGuid = mdc.GetFieldListRoot(flid);
				if (listGuid != Guid.Empty)
					return cache.ServiceLocator.GetInstance<ICmPossibilityListRepository>().GetObject(listGuid);
			}

			// Is this the best default? It clearly indicates that no target is known.
			// However, the default implementation of ReferenceTargetCandidates returns the
			// current contents of the list. It would be consistent with that for this method
			// to return 'this'. But that would seldom be useful (the user is presumably
			// already editing 'this'), and would require overrides wherever there is
			// definitely NO sensible object to jump to and edit. On the whole I (JohnT) think
			// it is best to make null the default.
			return null;
		}
	}
}
