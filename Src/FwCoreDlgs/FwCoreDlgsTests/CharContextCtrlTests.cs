// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: CharContextCtrlTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

using System;
using NUnit.Framework;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.Utils;
using SILUBS.SharedScrUtils;
using System.Collections.Generic;

namespace SIL.FieldWorks.FwCoreDlgs
{
	#region class DummyScrInventory
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Dummy class because NMock can't generate a dynamic mock for this interface. Grr...
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	internal class DummyScrInventory : IScrCheckInventory
	{
		internal List<TextTokenSubstring> m_references;
		#region IScrCheckInventory Members

		public List<TextTokenSubstring> GetReferences(IEnumerable<ITextToken> tokens, string desiredKey)
		{
			return m_references;
		}

		public string InvalidItems
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		public string InventoryColumnHeader
		{
			get { throw new NotImplementedException(); }
		}

		public void Save()
		{
			throw new NotImplementedException();
		}

		public string ValidItems
		{
			get
			{
				throw new NotImplementedException();
			}
			set
			{
				throw new NotImplementedException();
			}
		}

		#endregion

		#region IScriptureCheck Members

		public void Check(IEnumerable<ITextToken> toks, RecordErrorHandler record)
		{
			throw new NotImplementedException();
		}

		public string CheckGroup
		{
			get { throw new NotImplementedException(); }
		}

		public Guid CheckId
		{
			get { throw new NotImplementedException(); }
		}

		public string CheckName
		{
			get { throw new NotImplementedException(); }
		}

		public string Description
		{
			get { throw new NotImplementedException(); }
		}

		public float RelativeOrder
		{
			get { throw new NotImplementedException(); }
		}

		#endregion
	}
	#endregion

	#region class CharContextCtrlTests
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// These tests test the CharContextCtrl.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class CharContextCtrlTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the NormalizeFileData method against some Hebrew data that normalizes
		/// differently in ICU and .Net
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NormalizeFileData_Hebrew()
		{
			using (var ctrl = new CharContextCtrl())
			{
				ctrl.Initialize(Cache, Cache.ServiceLocator.WritingSystems,
				null, null, null, null);

				// First string is the normalized order that ICU produces.
				// Second string is the normalized order that .Net produces.
				ReflectionHelper.SetField(ctrl, "m_fileData", new[] { "\u05E9\u05c1\u05b4\u0596",
				"\u05E9\u05b4\u05c1\u0596" });

				ReflectionHelper.CallMethod(ctrl, "NormalizeFileData");

				var results = (string[])ReflectionHelper.GetField(ctrl, "m_fileData");
				Assert.AreEqual(2, results.Length);
				Assert.AreEqual("\u05E9\u05c1\u05b4\u0596", results[0], "Expect ICU-style normalization");
				Assert.AreEqual("\u05E9\u05c1\u05b4\u0596", results[1], "Expect ICU-style normalization");
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the NormalizeFileData method against some data that normalizes differently in
		/// NFKD (compatibility decomposition) and NFD (canonical decomposition). This is to
		/// ensure that we're doing NFD. TE-8384
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void NormalizeFileData_EnsureNFD()
		{
			using (var ctrl = new CharContextCtrl())
			{
				ctrl.Initialize(Cache, Cache.ServiceLocator.WritingSystems,
					null, null, null, null);

				// First string is the normalized order that ICU produces.
				// Second string is the normalized order that .Net produces.
				ReflectionHelper.SetField(ctrl, "m_fileData", new[] { "\u2074" });

				ReflectionHelper.CallMethod(ctrl, "NormalizeFileData");

				var results = (string[])ReflectionHelper.GetField(ctrl, "m_fileData");
				Assert.AreEqual(1, results.Length);
				Assert.AreEqual("\u2074", results[0], "Expect ICU-style normalization");
			}
		}
	}
	#endregion
}