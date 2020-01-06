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
	public class FwChooseAnthroListModel
	{
		/// <summary>
		/// Enum representing the anthropology list options presented to the user
		/// </summary>
		public enum ListChoice
		{
			/// <summary>
			/// Empty list, user defines later
			/// </summary>
			UserDef,
			/// <summary>
			/// Standard OCM list
			/// </summary>
			OCM,
			/// <summary>
			/// Enhanced OCM list ("FRAME")
			/// </summary>
			FRAME
		}

		/// <summary/>
		public ListChoice CurrentList = ListChoice.FRAME;

		/// <summary/>
		public string AnthroFileName
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
