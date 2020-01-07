// Copyright (c) 2017-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary />
	internal enum SaveFile
	{
		/// <summary>The destination file does not exist</summary>
		DoesNotExist,
		/// <summary>A file with the same name exists but it has the wrong contents</summary>
		NotIdenticalExists,
		/// <summary>A file with the same name exists and it has the right contents</summary>
		IdenticalExists
	}
}