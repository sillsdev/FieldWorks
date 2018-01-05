// Copyright (c) 2017-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// DoesNotExist: The destination file does not exist
	/// NotIdenticalExists: A file with the same name exists
	///  but it has the wrong contents
	/// IdenticalExists: A file with the same name exists
	///  and it has the right contents
	/// </summary>
	public enum SaveFile
	{
		DoesNotExist,
		NotIdenticalExists,
		IdenticalExists
	}
}