using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;

namespace SIL.FieldWorks.SharpViews.Hookups
{
	/// <summary>
	/// This class implements the standard behavior of editing at the paragraph level in an StText.
	/// It really only works on StTxtParas, but must use IStPara because it has to match the
	/// signature of StText.Paragraphs.
	/// </summary>
	public class StTextParagraphOperations : BaseParagraphOperationsFdo<IStPara>
	{
		private IStText m_owningText;

		public StTextParagraphOperations(IStText owningText)
		{
			m_owningText = owningText;
		}
		public override IStPara MakeListItem(int index, bool ipAtStartOfPara)
		{
			var newPara = m_owningText.Services.GetInstance<IStTxtParaFactory>().Create();
			m_owningText.ParagraphsOS.Insert(index, newPara);
			// Review JohnT: should probably set contents to an empty string in some appropriate WS.
			return newPara;
		}

		/// <summary>
		/// Override to ensure new paragraph starts with empty string with same properties as end of
		/// previous paragraph.
		/// Enhance JohnT: adjust para properties based on a stylesheet (where from??)
		/// </summary>
		public override bool InsertFollowingParagraph(SIL.FieldWorks.SharpViews.Selections.InsertionPoint ip, out Action makeSelection)
		{
			if (!base.InsertFollowingParagraph(ip, out makeSelection))
				return false;
			int index = ItemIndex(ip);
			var newPara = (IStTxtPara)m_owningText.ParagraphsOS[index + 1];
			var oldPara = (IStTxtPara)m_owningText.ParagraphsOS[index];
			var oldTss = oldPara.Contents;
			var oldProps = oldTss.get_Properties(oldTss.RunCount - 1);
			newPara.Contents = newPara.Cache.TsStrFactory.MakeStringWithPropsRgch("", 0, oldProps);
			// Todo JohnT: copy para properties too.
			return true;
		}

		public override bool InsertPrecedingParagraph(SIL.FieldWorks.SharpViews.Selections.InsertionPoint ip, out Action makeSelection)
		{
			int index = ItemIndex(ip);
			if (!base.InsertPrecedingParagraph(ip, out makeSelection))
				return false;
			var newPara = (IStTxtPara)m_owningText.ParagraphsOS[index];
			var oldPara = (IStTxtPara)m_owningText.ParagraphsOS[index + 1];
			var oldTss = oldPara.Contents;
			var oldProps = oldTss.get_Properties(0);
			newPara.Contents = newPara.Cache.TsStrFactory.MakeStringWithPropsRgch("", 0, oldProps);
			// Todo JohnT: copy para properties too.
			return true;
		}
	}
}
