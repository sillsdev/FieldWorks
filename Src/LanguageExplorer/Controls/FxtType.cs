// Copyright (c) 2005-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.Controls
{
	internal struct FxtType
	{
		internal string m_sFormat;
		internal FxtTypes m_ft;
		internal bool m_filtered;
		internal string m_sDataType;
		internal string m_sXsltFiles;
		internal string m_path; // Used to keep track of items after they are sorted.
	}
}