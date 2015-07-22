// Copyright (c) 2003-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: AtomicReferenceLauncher.cs
// Responsibility: RandyR
using System;
using System.Linq;
using System.Xml;
using SIL.CoreImpl;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.Utils;
using XCore;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public class PossibilityAtomicReferenceLauncher : AtomicReferenceLauncher, IVwNotifyChange
	{
		private PossibilityAutoComplete m_autoComplete;

		#region Construction, Initialization, and Disposition

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				m_autoComplete.PossibilitySelected -= HandlePossibilitySelected;
				m_autoComplete.Dispose();
				m_cache.DomainDataByFlid.RemoveNotification(this);
			}

			base.Dispose(disposing);
		}

		public override void Initialize(FdoCache cache, ICmObject obj, int flid,
			string fieldName, IPersistenceProvider persistProvider, Mediator mediator, PropertyTable propertyTable, string displayNameProperty, string displayWs)
		{
			CheckDisposed();

			base.Initialize(cache, obj, flid, fieldName, persistProvider, mediator, propertyTable, displayNameProperty, displayWs);

			m_autoComplete = new PossibilityAutoComplete(cache, mediator, propertyTable, (ICmPossibilityList) obj.ReferenceTargetOwner(flid),
				m_atomicRefView, displayNameProperty, displayWs);
			m_autoComplete.PossibilitySelected += HandlePossibilitySelected;
			m_atomicRefView.RootBox.DataAccess.AddNotification(this);
		}

		#endregion // Construction, Initialization, and Disposition

		#region Overrides

		/// <summary>
		/// Clear any existing selection in the view when we leave the launcher.
		/// </summary>
		/// <param name="e"></param>
		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);
			if (m_atomicRefView != null && m_atomicRefView.RootBox != null)
			{
				if (AllowEmptyItem && m_atomicRefView.RootBox.DataAccess.get_StringProp(m_obj.Hvo, PossibilityAtomicReferenceView.kflidFake).Length == 0 && Target != null)
				{
					AddItem(null);
				}
				else
				{
					ICmPossibility[] possibilities = m_autoComplete.Possibilities.ToArray();
					if (possibilities.Length == 1)
					{
						if (possibilities[0] != Target)
							AddItem(possibilities[0]);
					}
					else
					{
						UpdateDisplayFromDatabase();
					}
				}
			}
			m_autoComplete.Hide();
		}

		#endregion // Overrides

		protected virtual bool AllowEmptyItem
		{
			get
			{
				XmlNode node = m_configurationNode.SelectSingleNode("deParams");
				string nullLabel = XmlUtils.GetOptionalAttributeValue(node, "nullLabel");
				return nullLabel == null || nullLabel.Length > 0;
			}
		}

		protected override AtomicReferenceView CreateAtomicReferenceView()
		{
			return new PossibilityAtomicReferenceView();
		}

		private void HandlePossibilitySelected(object sender, EventArgs e)
		{
			ICmPossibility poss = m_autoComplete.SelectedPossibility;
			if (poss != Target)
				AddItem(poss);
			else
				UpdateDisplayFromDatabase();
			if (m_atomicRefView != null)
				m_atomicRefView.RootBox.MakeSimpleSel(true, true, true, true);
		}

		public void PropChanged(int hvo, int tag, int ivMin, int cvIns, int cvDel)
		{
			if (tag == PossibilityAtomicReferenceView.kflidFake)
				m_autoComplete.Update(m_atomicRefView.RootBox.DataAccess.get_StringProp(hvo, tag));
		}
	}
}
