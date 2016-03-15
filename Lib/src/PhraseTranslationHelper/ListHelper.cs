// Copyright (c) 2016 SIL International
// This software is licensed under the MIT License (http://opensource.org/licenses/MIT)
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Palaso.Xml;

namespace SILUBS.PhraseTranslationHelper
{
	public static class ListHelper
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Loads a list of objects of the specified type by deserializing from the given file.
		/// If the file does not exist or an error occurs during deserialization, a new list is
		/// created.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public static List<T> LoadOrCreateList<T>(string filename, bool reportErrorToUser)
		{
			List<T> list = null;
			if (File.Exists(filename))
			{
				Exception e;
				list = XmlSerializationHelper.DeserializeFromFile<List<T>>(filename, out e);
				if (e != null && reportErrorToUser)
					MessageBox.Show(e.ToString());
			}
			return list ?? new List<T>();
		}
	}
}
