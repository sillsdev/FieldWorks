// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using LanguageExplorer.Controls.DetailControls;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	internal sealed class LexReferencePairVc : AtomicReferenceVc
	{
		/// <summary />
		private ICmObject m_displayParent;

		public LexReferencePairVc(LcmCache cache, int flid, string displayNameProperty)
			: base(cache, flid, displayNameProperty)
		{
		}

		/// <summary />
		protected override int HvoOfObjectToDisplay(IVwEnv vwenv, int hvo)
		{
			var sda = vwenv.DataAccess;
			var chvo = sda.get_VecSize(hvo, m_flid);
			if (chvo < 2)
			{
				return 0;
			}
			var hvoItem = sda.get_VecItem(hvo, m_flid, 0);
			if (m_displayParent != null && hvoItem == m_displayParent.Hvo)
			{
				hvoItem = sda.get_VecItem(hvo, m_flid, 1);
			}
			return hvoItem;
		}

		/// <summary />
		protected override void DisplayObjectProperty(IVwEnv vwenv, int hvo)
		{
			vwenv.AddObj(hvo, this, AtomicReferenceView.kFragObjName);
		}

		/// <summary />
		public ICmObject DisplayParent
		{
			set { m_displayParent = value; }
		}
	}
}