// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary />
	internal sealed class LexReferenceCollectionView : VectorReferenceView
	{
		/// <summary />
		private ICmObject m_displayParent;

		/// <summary />
		internal LexReferenceCollectionView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary />
		protected override VectorReferenceVc CreateVectorReferenceVc()
		{
			var vc = new LexReferenceCollectionVc(m_cache, m_rootFlid, m_displayNameProperty, m_displayWs);
			if (m_displayParent != null)
			{
				vc.DisplayParent = m_displayParent;
			}
			return vc;
		}

		/// <summary />
		internal ICmObject DisplayParent
		{
			set
			{
				m_displayParent = value;
				if (m_VectorReferenceVc != null)
				{
					((LexReferenceCollectionVc)m_VectorReferenceVc).DisplayParent = value;
				}
			}
		}

		/// <summary />
		protected override List<ICmObject> GetVisibleItemList()
		{
			var sda = RootBox.DataAccess as ISilDataAccessManaged;
			var objRepo = Cache.ServiceLocator.ObjectRepository;
			if (sda != null) //sda should never be null
			{
				return (sda.VecProp(m_rootObj.Hvo, m_rootFlid).Where(i => objRepo.GetObject(i) != m_displayParent).Select(i => objRepo.GetObject(i))).ToList();
			}
			Debug.Assert(false, "Error retrieving DataAccess, crash imminent.");
			return null;
		}

		/// <summary />
		protected override List<ICmObject> GetHiddenItemList()
		{
			var sda = RootBox.DataAccess as ISilDataAccessManaged;
			var objRepo = Cache.ServiceLocator.ObjectRepository;
			if (sda != null) //sda should never be null
			{
				return (sda.VecProp(m_rootObj.Hvo, m_rootFlid).Where(i => objRepo.GetObject(i) == m_displayParent).Select(i => objRepo.GetObject(i))).ToList();
			}
			Debug.Assert(false, "Error retrieving DataAccess, crash imminent.");
			return null;
		}

		/// <summary />
		protected override void Delete()
		{
			Delete(LanguageExplorerControls.ksUndoDeleteRef, LanguageExplorerControls.ksRedoDeleteRef);
		}

		#region Component Designer generated code
		/// <summary>
		/// The Superclass handles everything except our Name property.
		/// </summary>
		private void InitializeComponent()
		{
			this.Name = "LexReferenceCollectionView";
		}
		#endregion
	}
}