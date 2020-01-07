// Copyright (c) 2010-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lists
{
	/// <summary>
	/// Method Object used to delete Custom Lists.
	/// </summary>
	internal class DeleteCustomList
	{
		#region Member Variables

		private readonly LcmCache m_cache;
		private List<FieldDescription> m_customFields;
		private readonly ISilDataAccessManaged m_ddbf;

		private ICmPossibilityList m_listToDelete;

		#endregion

		#region Constructor

		public DeleteCustomList(LcmCache cache)
		{
			m_cache = cache;
			m_ddbf = m_cache.GetManagedSilDataAccess();
		}

		#endregion

		/// <summary>
		/// Main entry point for deleting Custom Lists.
		/// Needs to:
		/// 1 - Make sure the given list really is a Custom List
		/// 2 - Let the user know if any of the possibilities of this list are being referenced.
		/// 3 - Delete the list (and its possibilities and subpossibilities) and, consequently,
		///     any references to those possibilities.
		/// </summary>
		public void Run(ICmPossibilityList curList)
		{
			if (curList.Owner != null)
			{
				// Custom lists have no owner.
				return;
			}
			m_listToDelete = curList;
			// Make sure user knows if any possibilities owned by this list are referenced
			// by anything else!
			GetCustomFieldsReferencingList(m_listToDelete.Guid);
			ICmPossibility poss;
			if (HasPossibilityReferences(out poss) > 0)
			{
				var name = poss.Name.BestAnalysisVernacularAlternative.Text;
				// Warn user that possibilities in this list are in use.
				if (CheckWithUser(name) != DialogResult.Yes)
				{
					return;
				}
			}
			DeleteList(m_listToDelete);
		}

		private int HasPossibilityReferences(out ICmPossibility poss1)
		{
			var refs = m_listToDelete.ReallyReallyAllPossibilities.Where(poss => poss.ReferringObjects.Count > 0).ToList();
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
			{
				return;
			}
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
			m_customFields.AddRange(fdCollection.Where(fd => fd.IsCustomField && fd.ListRootId == listGuid));
		}

		/// <summary>
		/// Protected so we can test without user intervention.
		/// </summary>
		/// <param name="name"></param>
		/// <returns></returns>
		protected virtual DialogResult CheckWithUser(string name)
		{
			return MessageBox.Show(string.Format(ListResources.ksReferencedPossibility, name), LanguageExplorerResources.ksWarning, MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
		}
	}
}