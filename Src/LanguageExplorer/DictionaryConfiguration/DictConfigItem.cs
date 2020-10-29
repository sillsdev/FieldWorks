// Copyright (c) 2011-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;

namespace LanguageExplorer.DictionaryConfiguration
{
	/// <summary>
	/// Item to be managed by the DictionaryConfigManager. Represents a user-configurable
	/// Dictionary view layout.
	/// </summary>
	/// <remarks>This needs to be internal, because tests use it. Otherwise, it would be private</remarks>
	internal sealed class DictConfigItem
	{
		private readonly string m_initialName;

		#region Auto-Properties

		internal string DispName { get; set; }
		internal string UniqueCode { get; }
		internal bool IsProtected { get; }
		internal bool UserMarkedDelete { get; set; }
		internal string CopyOf { get; }

		#endregion

		private const string ksUnnamed = "NoName";

		/// <summary>
		/// Constructor for config items being sent to dialog.
		/// </summary>
		/// <param name="code"></param>
		/// <param name="name"></param>
		/// <param name="original">Original items to be protected.</param>
		internal DictConfigItem(string code, string name, bool original)
		{
			UniqueCode = code;
			m_initialName = name;
			DispName = m_initialName;
			IsProtected = original;
			CopyOf = null;
			UserMarkedDelete = false;
		}

		/// <summary>
		/// Constructor for config items created by the Manager as copies of
		/// an existing item.
		/// </summary>
		internal DictConfigItem(DictConfigItem source)
		{
			UniqueCode = CreateUniqueIdCode(source.DispName);
			IsProtected = false;
			UserMarkedDelete = false;
			CopyOf = source.UniqueCode;
			DispName = string.Format(DictionaryConfigurationStrings.ksDictConfigCopyOf, source.DispName);
			m_initialName = DispName;
		}

		private static string CreateUniqueIdCode(string oldName)
		{
			var nameSeed = oldName.PadRight(5);
			var result = nameSeed + ksUnnamed; // make sure we don't have something too short!
			var num = DateTime.UtcNow.Millisecond.ToString();
			return result.Substring(0, 5) + num;
		}

		internal bool IsNew => !string.IsNullOrEmpty(CopyOf);

		internal bool IsRenamed => !IsNew && !UserMarkedDelete && m_initialName != DispName;
	}
}