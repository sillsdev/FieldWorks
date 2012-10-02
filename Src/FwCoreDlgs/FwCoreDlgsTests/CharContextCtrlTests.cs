#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: CharContextCtrlTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.Common.Utils;
using SILUBS.SharedScrUtils;
using NUnit.Framework.SyntaxHelpers;
using System.Collections.Generic;
using SIL.FieldWorks.FDO.Scripture;

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
	public class CharContextCtrlTests : InMemoryFdoTestBase
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
			CharContextCtrl ctrl = new CharContextCtrl();

			// First string is the normalized order that ICU produces.
			// Second string is the normalized order that .Net produces.
			ReflectionHelper.SetField(ctrl, "m_fileData", new string[] { "\u05E9\u05c1\u05b4\u0596",
				"\u05E9\u05b4\u05c1\u0596"});
			ReflectionHelper.SetProperty(ctrl, "Cache", Cache);

			ReflectionHelper.CallMethod(ctrl, "NormalizeFileData");

			string[] results = (string[])ReflectionHelper.GetField(ctrl, "m_fileData");
			Assert.AreEqual(2, results.Length);
			Assert.AreEqual("\u05E9\u05c1\u05b4\u0596", results[0], "Expect ICU-style normalization");
			Assert.AreEqual("\u05E9\u05c1\u05b4\u0596", results[1], "Expect ICU-style normalization");
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
			CharContextCtrl ctrl = new CharContextCtrl();

			// First string is the normalized order that ICU produces.
			// Second string is the normalized order that .Net produces.
			ReflectionHelper.SetField(ctrl, "m_fileData", new string[] { "\u2074" });
			ReflectionHelper.SetProperty(ctrl, "Cache", Cache);

			ReflectionHelper.CallMethod(ctrl, "NormalizeFileData");

			string[] results = (string[])ReflectionHelper.GetField(ctrl, "m_fileData");
			Assert.AreEqual(1, results.Length);
			Assert.AreEqual("\u2074", results[0], "Expect ICU-style normalization");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTokenSubstrings method when no validator is supplied.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTokenSubstrings_NoValidator()
		{
			CharContextCtrl ctrl = new CharContextCtrl();
			ctrl.Cache = Cache;

			List<ITextToken> tokens = new List<ITextToken>();
			ITextToken token = new ScrCheckingToken();
			ReflectionHelper.SetField(token, "m_sText", "Mom. Dad!");
			tokens.Add(token);
			DummyScrInventory inventory = new DummyScrInventory();
			inventory.m_references = new List<TextTokenSubstring>();
			inventory.m_references.Add(new TextTokenSubstring(token, 3, 2));
			List<TextTokenSubstring> validatedList =
				(List<TextTokenSubstring>)ReflectionHelper.GetResult(ctrl, "GetTokenSubstrings",
				inventory, tokens);
			Assert.AreEqual(1, validatedList.Count);
			Assert.AreEqual(". ", validatedList[0].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the GetTokenSubstrings method when a validator is supplied which removes
		/// the first and last substring.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetTokenSubstrings_ValidatorThatRemovesSomeResults()
		{
			CharContextCtrl ctrl = new CharContextCtrl();
			ctrl.Cache = Cache;
			ctrl.ListValidator = RemoveFirstAndLastSubString;

			List<ITextToken> tokens = new List<ITextToken>();
			ITextToken token = new ScrCheckingToken();
			ReflectionHelper.SetField(token, "m_sText", "Mom. Dad! Brother(Sister)");
			tokens.Add(token);
			DummyScrInventory inventory = new DummyScrInventory();
			inventory.m_references = new List<TextTokenSubstring>();
			inventory.m_references.Add(new TextTokenSubstring(token, 3, 2));
			inventory.m_references.Add(new TextTokenSubstring(token, 8, 2));
			inventory.m_references.Add(new TextTokenSubstring(token, 17, 1));
			inventory.m_references.Add(new TextTokenSubstring(token, 24, 1));
			List<TextTokenSubstring> validatedList =
				(List<TextTokenSubstring>)ReflectionHelper.GetResult(ctrl, "GetTokenSubstrings",
				inventory, tokens);
			Assert.AreEqual(2, validatedList.Count);
			Assert.AreEqual("! ", validatedList[0].Text);
			Assert.AreEqual("(", validatedList[1].Text);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Removes the first and last sub string.
		/// </summary>
		/// <param name="list">The list.</param>
		/// ------------------------------------------------------------------------------------
		private void RemoveFirstAndLastSubString(List<TextTokenSubstring> list)
		{
			list.RemoveAt(0);
			list.RemoveAt(list.Count - 1);
		}
	}
	#endregion
}
