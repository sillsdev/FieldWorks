// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Framework.DetailControls;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	/// Summary description for LexReferenceTreeRootView.
	/// </summary>
	internal class LexReferenceTreeRootView : AtomicReferenceView
	{
		/// <summary />
		public LexReferenceTreeRootView() : base()
		{
		}

		/// <summary />
		public override void SetReferenceVc()
		{
			CheckDisposed();

			m_atomicReferenceVc = new LexReferenceTreeRootVc(m_fdoCache,
					m_rootObj.Hvo, m_rootFlid, m_displayNameProperty);
		}
	}

	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	internal class LexReferenceTreeRootVc : AtomicReferenceVc
	{
		/// <summary />
		protected int m_hvoOwner;

		/// <summary />
		public LexReferenceTreeRootVc(FdoCache cache, int hvo, int flid, string displayNameProperty)
			: base (cache, flid, displayNameProperty)
		{
			m_hvoOwner = hvo;
		}

		/// <summary />
		protected override int HvoOfObjectToDisplay(IVwEnv vwenv, int hvo)
		{
			ISilDataAccess sda = vwenv.DataAccess;
			int chvo = sda.get_VecSize(hvo, m_flid);
			if (chvo < 1)
				return 0;
			else
				return sda.get_VecItem(hvo, m_flid, 0);
		}

		/// <summary />
		protected override void DisplayObjectProperty(IVwEnv vwenv, int hvo)
		{
			vwenv.NoteDependency(new int[] {m_hvoOwner}, new int[] {m_flid}, 1);
			vwenv.AddObj(hvo, this, AtomicReferenceView.kFragObjName);
		}
	}
}
