using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.XWorks;

namespace MacroCapitalize
{
	/// <summary>
	/// Sample FLEx macro. To make this work, drop the resulting DLL into output/debug (or wherever FLEx is executing).
	/// </summary>
	public class MacroCapitalize : IFlexMacro
	{
		public string CommandName
		{
			get { return "Capitalize"; }
		}

		public bool Enabled(ICmObject target, int targetField, int wsId, int start, int length)
		{
			return length > 0;
		}

		public void RunMacro(ICmObject target, int targetField, int wsId, int start, int length)
		{
			if (length == 0)
				return;
			var sda = target.Cache.DomainDataByFlid;
			ITsString input;
			if (wsId == 0)
				input = sda.get_StringProp(target.Hvo, targetField);
			else
				input = sda.get_MultiStringAlt(target.Hvo, targetField, wsId);
			var bldr = input.GetBldr();
			var selText = input.Text.Substring(start, length).ToUpper();
			// This is oversimplified. For real use we would probably want to handle the possibility of writing system or style variations within the selection.
			bldr.Replace(start, start + length, selText, null);
			var output = bldr.GetString();
			if (wsId == 0)
				sda.SetString(target.Hvo, targetField, output);
			else
				sda.SetMultiStringAlt(target.Hvo, targetField, wsId, output);

		}

		public Keys PreferredFunctionKey
		{
			get { return Keys.F6; }
		}
	}
}
