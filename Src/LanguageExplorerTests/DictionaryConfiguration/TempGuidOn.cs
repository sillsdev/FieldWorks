// Copyright (c) 2014-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using SIL.LCModel;
using SIL.LCModel.Utils;

namespace LanguageExplorerTests.DictionaryConfiguration
{
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
			System.Diagnostics.Debug.WriteLineIf(!disposing, "****** Missing Dispose() call for " + GetType().Name + " ******");

			if (disposing)
				SetGuidOn(Item, m_OriginalGuid);
		}

		private static void SetGuidOn(ICmObject item, Guid newGuid)
		{
			var refGuidField = ReflectionHelper.GetField(item, "m_guid");
			ReflectionHelper.SetField(refGuidField, "m_guid", newGuid);
		}
	}
}