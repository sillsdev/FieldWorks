// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: DeleteCustomList.cs
// Responsibility: GordonM
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using System.Windows.Forms;

namespace SIL.FieldWorks.XWorks
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Method Object used to delete Custom Lists.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class DeleteCustomList
	{
		#region Member Variables

		private readonly FdoCache m_cache;
		private List<FieldDescription> m_customFields;
		private readonly ISilDataAccessManaged m_ddbf;

		private ICmPossibilityList m_listToDelete;

		#endregion

		#region Constructor

		public DeleteCustomList(FdoCache cache)
		{
			m_cache = cache;
			m_ddbf = (ISilDataAccessManaged)m_cache.DomainDataByFlid;
		}

		#endregion

		/// ----------------------------------------------------------------------------------------
		/// <summary>
		/// Main entry point for deleting Custom Lists.
		/// Needs to:
		/// 1 - Make sure the given list really is a Custom List
		/// 2 - Let the user know if any of the possibilities of this list are being referenced.
		/// 3 - Delete the list (and its possibilities and subpossibilities) and, consequently,
		///     any references to those possibilities.
		/// </summary>
		/// ----------------------------------------------------------------------------------------
		public void Run(ICmPossibilityList curList)
		{
			m_listToDelete = curList;

			// Make sure list is a CUSTOM list!
			var owner = m_listToDelete.Owner; // Custom lists are unowned!
			if (owner != null)
				return; // Not a Custom list!

			// Make sure user knows if any possibilities owned by this list are referenced
			// by anything else!
			GetCustomFieldsReferencingList(m_listToDelete.Guid);
			ICmPossibility poss;
			if (HasPossibilityReferences(out poss) > 0)
			{
				var name = poss.Name.BestAnalysisVernacularAlternative.Text;
				// Warn user that possibilities in this list are in use.
				if (CheckWithUser(name) != DialogResult.Yes)
					return;
			}
			DeleteList(m_listToDelete);
		}

		private int HasPossibilityReferences(out ICmPossibility poss1)
		{
			var refs = m_listToDelete.ReallyReallyAllPossibilities.Where(
				poss => poss.ReferringObjects.Count > 0).ToList();
			poss1 = refs.FirstOrDefault();
			return refs.Count;
		}

		private void DeleteList(ICmPossibilityList listToDelete)
		{
			// Delete any custom fields that reference this list
			DeleteCustomFieldsReferencingList();
			// Delete the Custom list
			m_ddbf.DeleteObj(listToDelete.Hvo);
		}

		private void DeleteCustomFieldsReferencingList()
		{
			// Get all the custom fields
			//GetCustomFieldsReferencingList(listGuid); now done earlier
			if (m_customFields.Count == 0)
				return;
			foreach (var fd in m_customFields)
			{
				fd.MarkForDeletion = true;
				fd.UpdateCustomField();
			}
			FieldDescription.ClearDataAbout();
		}

		private void GetCustomFieldsReferencingList(Guid listGuid)
		{
			FieldDescription.ClearDataAbout();
			var fdCollection = FieldDescription.FieldDescriptors(m_cache);
			m_customFields = new List<FieldDescription>();
			m_customFields.AddRange(from fd in fdCollection
									where fd.IsCustomField && fd.ListRootId == listGuid
									select fd);
		}

		/// <summary>
		/// Protected so we can test without user intervention.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected virtual DialogResult CheckWithUser(string name)
		{
			return MessageBox.Show(String.Format(xWorksStrings.ksReferencedPossibility, name),
								   xWorksStrings.ksWarning,
								   MessageBoxButtons.YesNo,
								   MessageBoxIcon.Warning,
								   MessageBoxDefaultButton.Button2);
		}
	}
}
