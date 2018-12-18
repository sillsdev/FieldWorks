// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LanguageExplorer.Controls.DetailControls;
using SIL.LCModel;
using SIL.LCModel.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary />
	internal class LexReferenceCollectionView : VectorReferenceView
	{
		/// <summary />
		protected ICmObject m_displayParent;

		/// <summary />
		public LexReferenceCollectionView()
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
		public ICmObject DisplayParent
		{
			set
			{
				m_displayParent = value;
				if (m_VectorReferenceVc != null)
				{
					(m_VectorReferenceVc as LexReferenceCollectionVc).DisplayParent = value;
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
			Delete(AreaResources.ksUndoDeleteRef, AreaResources.ksRedoDeleteRef);
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