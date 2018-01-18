// Copyright (c) 2015-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using SIL.LCModel;
using SIL.FieldWorks.Common.ViewsInterfaces;
using LanguageExplorer.Controls.DetailControls;

namespace LanguageExplorer.Areas.Lexicon.Tools.Edit
{
	/// <summary>
	///  View constructor for creating the view details.
	/// </summary>
	internal sealed class LexReferenceTreeRootVc : AtomicReferenceVc
	{
		/// <summary />
		private readonly int m_hvoOwner;

		/// <summary />
		public LexReferenceTreeRootVc(LcmCache cache, int hvo, int flid, string displayNameProperty)
			: base (cache, flid, displayNameProperty)
		{
			m_hvoOwner = hvo;
		}

		/// <summary />
		protected override int HvoOfObjectToDisplay(IVwEnv vwenv, int hvo)
		{
			var sda = vwenv.DataAccess;
			var chvo = sda.get_VecSize(hvo, m_flid);
			return chvo < 1 ? 0 : sda.get_VecItem(hvo, m_flid, 0);
		}

		/// <summary />
		protected override void DisplayObjectProperty(IVwEnv vwenv, int hvo)
		{
			vwenv.NoteDependency(new int[] {m_hvoOwner}, new int[] {m_flid}, 1);
			vwenv.AddObj(hvo, this, AtomicReferenceView.kFragObjName);
		}
	}
}