// Copyright (c) 2019-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.FwCoreDlgs
{
	/// <summary>
	/// Presentation model for the FwChooseAnthroListCtrl
	/// </summary>
	internal sealed class FwChooseAnthroListModel
	{
		/// <summary/>
		internal ListChoice CurrentList = ListChoice.FRAME;

		/// <summary/>
		internal string AnthroFileName
		{
			get
			{
				string sFile = null;
				switch (CurrentList)
				{
					case ListChoice.UserDef:
						break;
					case ListChoice.OCM:
						sFile = Path.Combine(FwDirectoryFinder.TemplateDirectory, FwDirectoryFinder.ksOCMListFilename);
						break;
					case ListChoice.FRAME:
						sFile = Path.Combine(FwDirectoryFinder.TemplateDirectory, FwDirectoryFinder.ksOCMFrameFilename);
						break;
				}

				return sFile;
			}
		}
	}
}
