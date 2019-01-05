// Copyright (c) 2014-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Diagnostics;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.DictionaryConfiguration
{
	/// <summary>
	/// Intended to be used in unit tests to set, then reset on disposal, the Guid of CmObject.
	/// </summary>
	/// <remarks>
	/// Since this is a disposable class, one needs to dispose of it, such as in a <code>using</code> statement.
	/// </remarks>
	/// <typeparam name="T">An ICmObject class of object.</typeparam>
	internal sealed class TempGuidOn<T> : IDisposable where T : ICmObject
	{
		internal T Item { get; }
		private readonly Guid m_OriginalGuid;

		internal TempGuidOn(T item, Guid tempGuid)
		{
			Item = item;
			m_OriginalGuid = item.Guid;
			SetGuidOn(item, tempGuid);
		}

		~TempGuidOn()
		{
			Dispose(false);
		}

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		private void Dispose(bool disposing)
		{
			Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");

			if (disposing)
			{
				SetGuidOn(Item, m_OriginalGuid);
			}
		}

		private static void SetGuidOn(ICmObject item, Guid newGuid)
		{
			var refGuidField = ReflectionHelper.GetField(item, "m_guid");
			ReflectionHelper.SetField(refGuidField, "m_guid", newGuid);
		}
	}
}