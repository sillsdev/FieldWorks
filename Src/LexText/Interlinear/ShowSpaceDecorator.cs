// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Drawing;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Application;
using SIL.FieldWorks.FDO.DomainServices;
using SIL.Utils;

namespace SIL.FieldWorks.IText
{
	/// <summary>
	/// This class 'decorates' an SDA by intercepting calls to StTxtPara.Contents and, if ShowSpace is true,
	/// replacing zero-width-non-joining-space with a visible space that has a grey background. On write
	/// it replaces spaces with gray background with ZWNJS, and removes all background color.
	/// </summary>
	class ShowSpaceDecorator : DomainDataByFlidDecoratorBase
	{
		public static readonly int KzwsBackColor = (int)ColorUtil.ConvertColorToBGR(Color.LightGray);
		public ShowSpaceDecorator(ISilDataAccessManaged sda) : base(sda)
		{}

		public bool ShowSpaces { get; set; }

		public override ITsString get_StringProp(int hvo, int tag)
		{
			var result = base.get_StringProp(hvo, tag);
			if (!ShowSpaces || tag != StTxtParaTags.kflidContents || result == null)
				return result;
			var text = result.Text;
			if (text == null)
				return result;
			int index = text.IndexOf(AnalysisOccurrence.KchZws);
			if (index < 0)
				return result;

			var bldr = result.GetBldr();
			while (index >= 0)
			{
				bldr.Replace(index, index + 1, " ", null);
				bldr.SetIntPropValues(index, index + 1, (int) FwTextPropType.ktptBackColor, (int) FwTextPropVar.ktpvDefault,
					KzwsBackColor);
				index = text.IndexOf(AnalysisOccurrence.KchZws, index + 1);
			}
			return bldr.GetString();
		}

		public override void SetString(int hvo, int tag, ITsString tss)
		{
			if (!ShowSpaces || tag != StTxtParaTags.kflidContents || tss.Text == null)
			{
				base.SetString(hvo, tag, tss);
				return;
			}
			var text = tss.Text;
			var bldr = tss.GetBldr();
			int index = text.IndexOf(' ');
			while (index >= 0)
			{
				int nVar;
				if (bldr.get_PropertiesAt(index).GetIntPropValues((int) FwTextPropType.ktptBackColor, out nVar) == KzwsBackColor)
					bldr.Replace(index, index + 1, AnalysisOccurrence.KstrZws, null);
				index = text.IndexOf(' ', index + 1);
			}
			for (int irun = bldr.RunCount - 1; irun >= 0;  irun--)
			{
				int nVar;
				if (bldr.get_Properties(irun).GetIntPropValues((int) FwTextPropType.ktptBackColor, out nVar) == KzwsBackColor)
				{
					int ichMin, ichLim;
					bldr.GetBoundsOfRun(irun, out ichMin, out ichLim);
					bldr.SetIntPropValues(ichMin, ichLim, (int)FwTextPropType.ktptBackColor, -1, -1);
				}
			}

			base.SetString(hvo, tag, bldr.GetString());
		}
	}
}
