// Copyright (c) 2005-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Areas
{
	internal struct FxtType
	{
		public string m_sFormat;
		public FxtTypes m_ft;
		public bool m_filtered;
		public string m_sDataType;
		public string m_sXsltFiles;
		public string m_path; // Used to keep track of items after they are sorted.
	}
}