// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO.Application;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Summary description for LexReferenceCollectionView.
	/// </summary>
	internal class LexReferenceCollectionView : VectorReferenceView
	{
		/// <summary />
		protected ICmObject m_displayParent = null;

		/// <summary />
		public LexReferenceCollectionView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		/// <summary />
		protected override VectorReferenceVc CreateVectorReferenceVc()
		{
			LexReferenceCollectionVc vc = new LexReferenceCollectionVc(m_fdoCache, m_rootFlid, m_displayNameProperty, m_displayWs);
			if (m_displayParent != null)
				vc.DisplayParent = m_displayParent;
			return vc;
		}

		/// <summary />
		public ICmObject DisplayParent
		{
			set
			{
				CheckDisposed();

				m_displayParent = value;
				if (m_VectorReferenceVc != null)
					(m_VectorReferenceVc as LexReferenceCollectionVc).DisplayParent = value;
			}
		}

		/// <summary />
		protected override List<ICmObject> GetVisibleItemList()
		{
			var sda = m_rootb.DataAccess as ISilDataAccessManaged;
			var objRepo = Cache.ServiceLocator.ObjectRepository;
			if (sda != null) //sda should never be null
			{
				return (from i in sda.VecProp(m_rootObj.Hvo, m_rootFlid)
						where objRepo.GetObject(i) != m_displayParent
						select objRepo.GetObject(i)).ToList();
			}
			Debug.Assert(false, "Error retrieving DataAccess, crash imminent.");
			return null;
		}

		/// <summary />
		protected override List<ICmObject> GetHiddenItemList()
		{
			var sda = m_rootb.DataAccess as ISilDataAccessManaged;
			var objRepo = Cache.ServiceLocator.ObjectRepository;
			if (sda != null) //sda should never be null
			{
				return (from i in sda.VecProp(m_rootObj.Hvo, m_rootFlid)
						where objRepo.GetObject(i) == m_displayParent
						select objRepo.GetObject(i)).ToList();
			}
			Debug.Assert(false, "Error retrieving DataAccess, crash imminent.");
			return null;
		}

		/// <summary />
		protected override void Delete()
		{
			Delete(LanguageExplorerResources.ksUndoDeleteRef, LanguageExplorerResources.ksRedoDeleteRef);
		}

		/// <summary />
		protected override void UpdateTimeStampsIfNeeded(int[] hvos)
		{
#if WANTPORTMULTI
			for (int i = 0; i < hvos.Length; ++i)
			{
				ICmObject cmo = ICmObject.CreateFromDBObject(m_fdoCache, hvos[i]);
				(cmo as ICmObject).UpdateTimestampForVirtualChange();
			}
#endif
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
