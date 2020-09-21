// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Linq;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class PossibilityVectorReferenceLauncher : VectorReferenceLauncher, IVwNotifyChange
	{
		#region Data Members

		private PossibilityAutoComplete m_autoComplete;

		private PossibilityAutoComplete AutoComplete
		{
			get => m_autoComplete;
			set
			{
				if (m_autoComplete != null)
				{
					m_autoComplete.PossibilitySelected -= HandlePossibilitySelected;
					m_autoComplete.Dispose();
				}
				m_autoComplete = value;
				if (m_autoComplete != null)
				{
					m_autoComplete.PossibilitySelected += HandlePossibilitySelected;
				}
			}
		}

		#endregion // Data Members

		#region Construction, Initialization, and Disposal

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				AutoComplete = null; // The Property will automatically unwire event handlers and dispose itself
				m_cache.DomainDataByFlid.RemoveNotification(this);
			}

			base.Dispose(disposing);
		}

		public override void Initialize(LcmCache cache, ICmObject obj, int flid,
			string fieldName, IPersistenceProvider persistProvider, Mediator mediator, PropertyTable propertyTable,
			string displayNameProperty, string displayWs)
		{
			CheckDisposed();
			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, propertyTable, displayNameProperty, displayWs);

			AutoComplete = new PossibilityAutoComplete(cache, mediator, propertyTable, (ICmPossibilityList) obj.ReferenceTargetOwner(flid),
				m_vectorRefView, displayNameProperty, displayWs);
			m_vectorRefView.RootBox.DataAccess.AddNotification(this);
		}

		#endregion // Construction, Initialization, and Disposal

		#region Overrides

		/// <summary>
		/// Clear any existing selection in the view when we leave the launcher.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);
			if (m_vectorRefView != null && m_vectorRefView.RootBox != null)
			{
				ICmPossibility[] possibilities = AutoComplete.Possibilities.ToArray();
				if (possibilities.Length == 1)
				{
					ICmObject selected = m_vectorRefView.SelectedObject;
					if (possibilities[0] != selected)
					{
						if (selected == null)
						{
							AddItem(possibilities[0]);
						}
						else
						{
							var newTargets = new List<ICmObject>();
							foreach (ICmObject target in Targets)
								newTargets.Add(target == selected ? possibilities[0] : target);
							SetItems(newTargets);
						}
					}
				}
				else
				{
					UpdateDisplayFromDatabase();
				}
			}
			AutoComplete.Hide();
		}

		#endregion // Overrides

		protected override VectorReferenceView CreateVectorReferenceView()
		{
			return new PossibilityVectorReferenceView();
		}

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == PossibilityVectorReferenceView.kflidFake)
				AutoComplete.Update(m_vectorRefView.RootBox.DataAccess.get_StringProp(hvo, tag));
		}

		private void HandlePossibilitySelected(object sender, EventArgs e)
		{
			ICmPossibility poss = AutoComplete.SelectedPossibility;
			ICmObject curObj = m_vectorRefView.SelectedObject;
			if (curObj == null)
			{
				AddItem(poss);
			}
			else if (poss != curObj)
			{
				var newTargets = new List<ICmObject>();
				foreach (ICmObject target in Targets)
					newTargets.Add(target == curObj ? poss : target);
				SetItems(newTargets);
			}
			else
			{
				UpdateDisplayFromDatabase();
			}
			m_vectorRefView.SelectedObject = poss;
		}
	}
}
