// Copyright (c) 2003-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using System.Windows.Forms;
using LanguageExplorer.Controls.DetailControls.Resources;
using LanguageExplorer.Controls.XMLViews;
using SIL.FieldWorks.Common.FwUtils;
using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// Main class for displaying the VectorReferenceSlice.
	/// </summary>
	internal class PossibilityVectorReferenceView : VectorReferenceView
	{
		#region Constants and data members

		public const int khvoFake = -2333;
		public const int kflidFake = -2444;
		private int m_prevSelectedHvo;
		private PossibilityVectorReferenceViewSdaDecorator m_sda;

		#endregion // Constants and data members

		#region Construction, initialization, and disposal

		/// <summary>
		/// Reload the vector in the root box, presumably after it's been modified by a chooser.
		/// </summary>
		public override void ReloadVector()
		{
			var ws = 0;
			if (m_rootObj != null && m_rootObj.IsValidObject)
			{
				var count = m_sda.get_VecSize(m_rootObj.Hvo, m_rootFlid);
				// This loop is mostly redundant now that the decorator will generate labels itself as needed.
				// It still serves the purpose of figuring out the WS that should be used for the 'fake' item where the user
				// is typing to select.
				for (var i = 0; i < count; ++i)
				{
					var hvo = m_sda.get_VecItem(m_rootObj.Hvo, m_rootFlid, i);
					Debug.Assert(hvo != 0);
					ws = m_sda.GetLabelFor(hvo).get_WritingSystem(0);
				}

				if (ws == 0)
				{
					var list = (ICmPossibilityList) m_rootObj.ReferenceTargetOwner(m_rootFlid);
					ws = list.IsVernacular ? m_cache.ServiceLocator.WritingSystems.DefaultVernacularWritingSystem.Handle
						 : m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
					if (list.PossibilitiesOS.Count > 0)
					{
						var label = ObjectLabel.CreateObjectLabel(m_cache, list.PossibilitiesOS[0], m_displayNameProperty, m_displayWs);
						ws = label.AsTss.get_WritingSystem(0);
					}
				}
			}

			if (ws == 0)
			{
				ws = m_cache.ServiceLocator.WritingSystems.DefaultAnalysisWritingSystem.Handle;
			}
			m_sda.Strings[khvoFake] = TsStringUtils.EmptyString(ws);
			base.ReloadVector();
		}

		#endregion // Construction, initialization, and disposal

		#region RootSite required methods

		protected override VectorReferenceVc CreateVectorReferenceVc()
		{
			return new PossibilityVectorReferenceVc(m_cache, m_rootFlid, m_displayNameProperty, m_displayWs);
		}

		protected override ISilDataAccess GetDataAccess()
		{
			return m_sda ?? (m_sda = new PossibilityVectorReferenceViewSdaDecorator(m_cache.GetManagedSilDataAccess(), m_cache, m_displayNameProperty, m_displayWs)
			{
				Empty = TsStringUtils.EmptyString(m_cache.DefaultAnalWs)
			});
		}

		#endregion // RootSite required methods

		protected override void OnEnter(EventArgs e)
		{
			base.OnEnter(e);
			m_prevSelectedHvo = 0;
		}

		protected override void OnLeave(EventArgs e)
		{
			base.OnLeave(e);
			DeleteItem();
		}

		protected override void HandleSelectionChange(IVwRootBox rootb, IVwSelection vwselNew)
		{
			var selected = SelectedObject;
			var selectedHvo = 0;
			if (selected != null)
			{
				selectedHvo = selected.Hvo;
			}

			if (selectedHvo == m_prevSelectedHvo)
			{
				return;
			}
			if (DeleteItem())
			{
				m_prevSelectedHvo = selectedHvo;
				SelectedObject = selected;
			}
			else
			{
				m_prevSelectedHvo = selectedHvo;
				vwselNew.ExtendToStringBoundaries();
			}
		}

		protected override bool HandleRightClickOnObject(int hvo)
		{
			return hvo != khvoFake && base.HandleRightClickOnObject(hvo);
		}

		private bool DeleteItem()
		{
			if (m_prevSelectedHvo == 0)
			{
				return false;
			}

			var tss = m_sda.get_StringProp(m_prevSelectedHvo, kflidFake);
			if (tss == null || tss.Length > 0)
			{
				return false;
			}

			if (m_rootObj.Hvo < 0)
			{
				return false;    // already deleted.  See LT-15042.
			}

			var hvosOld = m_sda.VecProp(m_rootObj.Hvo, m_rootFlid);
			for (var i = 0; i < hvosOld.Length; ++i)
			{
				if (hvosOld[i] == m_prevSelectedHvo)
				{
					RemoveObjectFromList(hvosOld, i, string.Format(DetailControlsStrings.ksUndoDeleteItem, m_rootFieldName), string.Format(DetailControlsStrings.ksRedoDeleteItem, m_rootFieldName));
					break;
				}
			}
			return true;
		}

		protected override void HandleKeyDown(KeyEventArgs e)
		{
		}

		protected override void HandleKeyPress(KeyPressEventArgs e)
		{
		}
	}
}