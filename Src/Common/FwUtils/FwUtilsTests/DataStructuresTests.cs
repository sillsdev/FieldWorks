// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: LanguageDefinitionTest.cs
// Responsibility: Erik Freund, Tres London, Zachariah Yoder
//
// <remarks>
// </remarks>
// ---------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using SIL.FieldWorks.Common.COMInterfaces;	// for ILgWritingSystemFactory
using SIL.FieldWorks.Common.FwUtils;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary>
	/// These test test the PuaCharacterDlg dialog and the PuaCharacter tab on the WritingSystemPropertiesDialog.
	/// </summary>
	[TestFixture]
	public class DataStructuresTests
	{
		/// <summary>
		/// Tests the compareHex method.
		/// </summary>
		[Test]
		public void TestRedBlackTree()
		{
			RedBlackTree tree = new RedBlackTree("");
			tree.Insert("1");
			tree.Insert("2");
			tree.Insert("3");
			tree.Insert("4");
			tree.Insert("5");
			tree.Insert("6");

			tree.Insert("16");
			tree.Insert("17");
			tree.Insert("18");
			tree.Insert("19");
			tree.Insert("0");

			tree.Insert("e");
			tree.Insert("F");
			tree.Insert("G");
			tree.Insert("h");

			tree.Insert("7");
			tree.Insert("8");
			tree.Insert("9");
			tree.Insert("10");
			tree.Insert("11");
			tree.Insert("12");
			tree.Insert("13");
			tree.Insert("14");
			tree.Insert("15");

			tree.Insert("A");
			tree.Insert("b");
			tree.Insert("C");
			tree.Insert("d");

			tree.PrintTree();

			string test = (string)tree.Find("5");
			Assert.AreEqual("5",test,"5 should have been added");

			test = (string)tree.Find("b",new System.Collections.Comparer(new System.Globalization.CultureInfo("en-us")));
			Assert.AreEqual("b",test,"b should have been added");
			test = (string)tree.Find("B",new System.Collections.Comparer(new System.Globalization.CultureInfo("en-us")));
			Assert.AreEqual(null,test,"B should not have been found when searching via a case-sensitive search");
			test = (string)tree.Find("B",new System.Collections.CaseInsensitiveComparer());
			Assert.AreEqual("b",test,"B should be found when searching via a case-insensitive search");
		}
	}
}
