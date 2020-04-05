// Copyright (c) 2016-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal sealed class LexReferenceUnidirectionalView : VectorReferenceView
	{
		internal LexReferenceUnidirectionalView()
		{
			InitializeComponent();
		}

		/// <summary />
		protected override VectorReferenceVc CreateVectorReferenceVc()
		{
			return new LexReferenceUnidirectionalVc(m_cache, m_rootFlid, m_displayNameProperty, m_displayWs);
		}

		/// <summary />
		protected override void Delete()
		{
			Delete(LanguageExplorerControls.ksUndoDeleteRef, LanguageExplorerControls.ksRedoDeleteRef);
		}

		/// <summary>
		/// Put the hidden item back into the list of visible items to make the list that should be stored in the property.
		/// </summary>
		protected override void AddHiddenItems(List<ICmObject> items)
		{
			var allItems = base.GetVisibleItemList();
			if (allItems.Count != 0)
			{
				items.Insert(0, allItems[0]);
			}
		}

		/// <summary>
		/// In a unidirectional view the FIRST item is hidden.
		/// </summary>
		protected override List<ICmObject> GetVisibleItemList()
		{
			var result = base.GetVisibleItemList();
			result.RemoveAt(0);
			return result;
		}

		#region Component Designer generated code
		/// <summary>
		/// The Superclass handles everything except our Name property.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "LexReferenceUnidirectionalView";
		}
		#endregion
	}
}