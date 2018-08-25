// Copyright (c) 2005-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Core.KernelInterfaces;
using SIL.LCModel;
using SIL.LCModel.Infrastructure;

namespace LanguageExplorer.Controls.DetailControls
{
	/// <summary>
	/// An StTextSlice implements the "sttext" editor type for atomic attributes whose value is an StText.
	/// The resulting view allows the editing of the text, including creating and destroying (and splitting
	/// and merging) of the paragraphs using the usual keyboard actions.
	/// </summary>
	internal class StTextSlice : ViewPropertySlice
	{
		private readonly int m_ws;

		internal StTextSlice(ICmObject obj, int flid, int ws)
			: base(new StTextView(), obj, flid)
		{
			m_ws = ws;
		}

		public override void FinishInit()
		{
			base.FinishInit();

			var sda = Cache.DomainDataByFlid;
			var objPropHvo = sda.get_ObjectProp(MyCmObject.Hvo, FieldId);
			if (objPropHvo == 0)
			{
				var view = (StTextView)RootSite;
				var textHvo = 0;
				NonUndoableUnitOfWorkHelper.Do(Cache.ServiceLocator.GetInstance<IActionHandler>(), () =>
				{
					textHvo = sda.MakeNewObject(StTextTags.kClassId, MyCmObject.Hvo, FieldId, -2);
					var hvoStTxtPara = sda.MakeNewObject(StTxtParaTags.kClassId, textHvo, StTextTags.kflidParagraphs, 0);
					sda.SetString(hvoStTxtPara, StTxtParaTags.kflidContents, TsStringUtils.EmptyString(m_ws == 0 ? Cache.DefaultAnalWs : m_ws));
				});
				view.StText = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(textHvo);
			}
			else
			{
				var rootSiteAsStTextView = (StTextView) RootSite;
				if (rootSiteAsStTextView.StText == null)
				{
					// Owner has the text, but it isn't in the view yet.
					rootSiteAsStTextView.StText = Cache.ServiceLocator.GetInstance<IStTextRepository>().GetObject(objPropHvo);
				}
			}
			((StTextView)RootSite).Init(m_ws);
		}

		/// <summary>
		/// Select at the specified position in the first paragraph.
		/// </summary>
		internal void SelectAt(int ich)
		{
			((StTextView)Control).SelectAt(ich);
		}
	}
}