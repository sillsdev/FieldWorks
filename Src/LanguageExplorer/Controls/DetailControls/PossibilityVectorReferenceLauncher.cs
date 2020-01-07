// Copyright (c) 2003-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	internal class PossibilityVectorReferenceLauncher : VectorReferenceLauncher, IVwNotifyChange
	{
		private PossibilityAutoComplete m_autoComplete;

		#region Construction, Initialization, and Disposal

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

			if (disposing)
			{
				m_autoComplete.PossibilitySelected -= HandlePossibilitySelected;
				m_autoComplete.Dispose();
				m_cache.DomainDataByFlid.RemoveNotification(this);
			}

			base.Dispose(disposing);
		}

		public override void Initialize(LcmCache cache, ICmObject obj, int flid, string fieldName, IPersistenceProvider persistProvider, string displayNameProperty, string displayWs)
		{
			base.Initialize(cache, obj, flid, fieldName, persistProvider, displayNameProperty, displayWs);
			m_autoComplete = new PossibilityAutoComplete(cache, PropertyTable, (ICmPossibilityList) obj.ReferenceTargetOwner(flid), m_vectorRefView, displayNameProperty, displayWs);
			m_autoComplete.PossibilitySelected += HandlePossibilitySelected;
			m_vectorRefView.RootBox.DataAccess.AddNotification(this);
		}

		#endregion // Construction, Initialization, and Disposal

		#region Overrides

		/// <summary>
		/// Clear any existing selection in the view when we leave the launcher.
		/// </summary>
		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);
			if (m_vectorRefView?.RootBox != null)
			{
				var possibilities = m_autoComplete.Possibilities.ToArray();
				if (possibilities.Length == 1)
				{
					var selected = m_vectorRefView.SelectedObject;
					if (possibilities[0] != selected)
					{
						if (selected == null)
						{
							AddItem(possibilities[0]);
						}
						else
						{
							SetItems(Targets.Select(target => target == selected ? possibilities[0] : target).ToList());
						}
					}
				}
				else
				{
					UpdateDisplayFromDatabase();
				}
			}
			m_autoComplete.Hide();
		}

		#endregion // Overrides

		protected override VectorReferenceView CreateVectorReferenceView()
		{
			return new PossibilityVectorReferenceView();
		}

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == PossibilityVectorReferenceView.kflidFake)
			{
				m_autoComplete.Update(m_vectorRefView.RootBox.DataAccess.get_StringProp(hvo, tag));
			}
		}

		private void HandlePossibilitySelected(object sender, EventArgs e)
		{
			var poss = m_autoComplete.SelectedPossibility;
			var curObj = m_vectorRefView.SelectedObject;
			if (curObj == null)
			{
				AddItem(poss);
			}
			else if (poss != curObj)
			{
				SetItems(Targets.Select(target => target == curObj ? poss : target).ToList());
			}
			else
			{
				UpdateDisplayFromDatabase();
			}
			m_vectorRefView.SelectedObject = poss;
		}
	}
}