// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Windows.Forms;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;
using SIL.FieldWorks.FDO.Application;

namespace SIL.FieldWorks.XWorks.LexEd
{
	/// <summary>
	/// Summary description for LexReferenceCollectionView.
	/// </summary>
	public class LexReferenceCollectionView : VectorReferenceView
	{
		protected ICmObject m_displayParent = null;

		public LexReferenceCollectionView()
		{
			// This call is required by the Windows.Forms Form Designer.
			InitializeComponent();
		}

		protected override VectorReferenceVc CreateVectorReferenceVc()
		{
			LexReferenceCollectionVc vc = new LexReferenceCollectionVc(m_fdoCache, m_rootFlid, m_displayNameProperty, m_displayWs);
			if (m_displayParent != null)
				vc.DisplayParent = m_displayParent;
			return vc;
		}

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

		protected override void Delete()
		{
			Delete(LexEdStrings.ksUndoDeleteRef, LexEdStrings.ksRedoDeleteRef);
		}

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

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	public class LexReferenceCollectionVc : VectorReferenceVc
	{

		protected ICmObject m_displayParent = null;

		/// <summary>
		/// Constructor for the Vector Reference View Constructor Class.
		/// </summary>
		public LexReferenceCollectionVc(FdoCache cache, int flid, string displayNameProperty, string displayWs)
			: base (cache, flid, displayNameProperty, displayWs)
		{
		}

		/// <summary>
		/// Calling vwenv.AddObjVec() in Display() and implementing DisplayVec() seems to
		/// work better than calling vwenv.AddObjVecItems() in Display().  Theoretically
		/// this should not be case, but experience trumps theory every time.  :-) :-(
		/// </summary>
		public override void DisplayVec(IVwEnv vwenv, int hvo, int tag, int frag)
		{
			ISilDataAccess da = vwenv.DataAccess;
			int count = da.get_VecSize(hvo, tag);
			// Show everything in the collection except the current element from the main display.
			for (int i = 0; i < count; ++i)
			{
				int hvoItem = da.get_VecItem(hvo, tag, i);
				if (m_displayParent != null && hvoItem == m_displayParent.Hvo)
					continue;
				vwenv.AddObj(hvoItem, this,	VectorReferenceView.kfragTargetObj);
				vwenv.AddSeparatorBar();
			}
		}

		public ICmObject DisplayParent
		{
			set { m_displayParent = value; }
		}
	}
}
