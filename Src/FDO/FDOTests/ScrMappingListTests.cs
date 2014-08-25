// Copyright (c) 2006-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: ScrMappingListTests.cs
// Responsibility: TE Team

using System;

using NUnit.Framework;
using Rhino.Mocks;

using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.FwUtils;
using SIL.FieldWorks.Common.ScriptureUtils;
using SIL.FieldWorks.Resources;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using SIL.FieldWorks.FDO.DomainServices;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Tests for ScrMappingList class.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScrMappingListTests : BaseTest
	{
		#region Save/load mappings tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test setting and retrieving mappings in the Scripture list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetMappings_Main()
		{
			ScrMappingList list = new ScrMappingList(MappingSet.Main, null, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.Add(new ImportMappingInfo(@"\a", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.NormalParagraph, null));
			list.Add(new ImportMappingInfo(@"\a", null, false, MappingTargetType.TEStyle, MarkerDomain.BackTrans, ScrStyleNames.NormalParagraph, "es"));
			list.Add(new ImportMappingInfo(@"\b", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.NormalParagraph, null));
			list.Add(new ImportMappingInfo(@"\c", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, "wrong style for chapter", null));
			list.Add(new ImportMappingInfo(@"\v", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, "wrong style for verse", null));
			list.Add(new ImportMappingInfo(@"\id", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, "id should be null", null));
			list.Add(new ImportMappingInfo(@"\btp", null, false, MappingTargetType.TEStyle, MarkerDomain.BackTrans, ScrStyleNames.NormalParagraph, null));

			Assert.AreEqual(6, list.Count);

			ImportMappingInfo mapping = list[0];
			Assert.AreEqual(@"\a", mapping.BeginMarker);
			Assert.IsNull(mapping.EndMarker);
			Assert.AreEqual(ScrStyleNames.NormalParagraph, mapping.StyleName);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
			Assert.IsFalse(mapping.IsExcluded);
			Assert.IsFalse(mapping.IsInline);
			Assert.AreEqual(MappingTargetType.TEStyle, mapping.MappingTarget);
			Assert.AreEqual("es", mapping.WsId);

			mapping = list[1];
			Assert.AreEqual(@"\b", mapping.BeginMarker);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain);

			mapping = list[2];
			Assert.AreEqual(@"\btp", mapping.BeginMarker);
			Assert.AreEqual(MarkerDomain.BackTrans, mapping.Domain);

			mapping = list[3];
			Assert.AreEqual(@"\c", mapping.BeginMarker);
			Assert.AreEqual(ScrStyleNames.ChapterNumber, mapping.StyleName);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain);

			mapping = list[4];
			Assert.AreEqual(@"\id", mapping.BeginMarker);
			Assert.IsNull(mapping.StyleName);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain);

			mapping = list[5];
			Assert.AreEqual(@"\v", mapping.BeginMarker);
			Assert.AreEqual(ScrStyleNames.VerseNumber, mapping.StyleName);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test setting and retrieving mappings in the Notes list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void SetMappings_Notes()
		{
			ScrMappingList list = new ScrMappingList(MappingSet.Notes, null, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.Add(new ImportMappingInfo(@"\a", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null));
			list.Add(new ImportMappingInfo(@"\b", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.Remark, null));
			try
			{
				list.Add(new ImportMappingInfo(@"\q", null, false, MappingTargetType.TEStyle, MarkerDomain.BackTrans, ScrStyleNames.NormalParagraph, null));
				Assert.Fail("Illegal mapping (to BackTrans domain) was not caught");
			}
			catch (ArgumentException) {}

			Assert.AreEqual(2, list.Count);

			ImportMappingInfo mapping = list[0];
			Assert.AreEqual(@"\a", mapping.BeginMarker);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain);

			mapping = list[1];
			Assert.AreEqual(@"\b", mapping.BeginMarker);
			Assert.AreEqual(MarkerDomain.Default, mapping.Domain);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test deleting mappings
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Delete()
		{
			ScrMappingList list = new ScrMappingList(MappingSet.Main, null, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.Add(new ImportMappingInfo(@"\aa", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.Remark, null));
			list.Add(new ImportMappingInfo(@"\bb", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.Remark, null));
			list.Add(new ImportMappingInfo(@"\cc", null, false, MappingTargetType.TEStyle, MarkerDomain.BackTrans, ScrStyleNames.Remark, null));

			Assert.AreEqual(3, list.Count);

			list.Delete(list[1]);
			Assert.AreEqual(2, list.Count);

			Assert.AreEqual(@"\aa", list[0].BeginMarker);
			Assert.AreEqual(@"\cc", list[1].BeginMarker);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test that HasChanged works correctly when mappings are added and deleted.
		/// Jira # is TE-7687.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void HasChanged()
		{
			ScrMappingList list = new ScrMappingList(MappingSet.Main, null, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.Add(new ImportMappingInfo(@"\aa", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.Remark, null));
			list.Add(new ImportMappingInfo(@"\bb", null, false, MappingTargetType.TEStyle, MarkerDomain.Default, ScrStyleNames.Remark, null));
			list.Add(new ImportMappingInfo(@"\cc", null, false, MappingTargetType.TEStyle, MarkerDomain.BackTrans, ScrStyleNames.Remark, null));
			Assert.IsTrue((bool)ReflectionHelper.GetProperty(list, "HasChanged"));

			Assert.AreEqual(3, list.Count);

			ReflectionHelper.SetProperty(list[0], "HasChanged", false);
			ReflectionHelper.SetProperty(list[1], "HasChanged", false);
			ReflectionHelper.SetProperty(list[2], "HasChanged", false);
			Assert.IsFalse((bool)ReflectionHelper.GetProperty(list, "HasChanged"));

			list.Delete(list[1]);
			Assert.AreEqual(2, list.Count);
			Assert.IsTrue((bool)ReflectionHelper.GetProperty(list, "HasChanged"));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the indexers for the mapping list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void Index_OutOfRange()
		{
			ScrMappingList list = new ScrMappingList(MappingSet.Notes, null, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.Add(new ImportMappingInfo(@"\a", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null));

			// Access the second element which should throw an exception
			ImportMappingInfo info = list[1];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests looking up a key in the mapping list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LookupByKey()
		{
			ScrMappingList list = new ScrMappingList(MappingSet.Notes, null, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.Add(new ImportMappingInfo(@"\aa", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null));
			list.Add(new ImportMappingInfo(@"\bb", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null));
			list.Add(new ImportMappingInfo(@"\cc", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null));

			ImportMappingInfo info = list[@"\bb"];
			Assert.AreEqual(@"\bb", info.BeginMarker);
			Assert.AreEqual(list[1], info);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests looking up a non-existent key in the mapping list
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LookupByKey_NonExistent()
		{
			ScrMappingList list = new ScrMappingList(MappingSet.Notes, null, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			Assert.IsNull(list["moogy"]);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the enumerator
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Enumerator()
		{
			ScrMappingList list = new ScrMappingList(MappingSet.Notes, null, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.Add(new ImportMappingInfo(@"\aa", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null));
			list.Add(new ImportMappingInfo(@"\bb", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null));
			list.Add(new ImportMappingInfo(@"\cc", null, false, MappingTargetType.TEStyle, MarkerDomain.Note, ScrStyleNames.Remark, null));

			int i = 0;
			foreach (ImportMappingInfo info in list)
				Assert.AreEqual(list[i++], info);
			Assert.AreEqual(3, i);
		}
		#endregion

		#region Test AddDefaultMappingIfNeeded method
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that figure markers map automatically to the correct property
		/// Jira task is TE-5732
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddDefaultMappingIfNeeded_FigureMarkers()
		{
			IVwStylesheet stylesheet = MockRepository.GenerateMock<IVwStylesheet>();

			ScrMappingList list = new ScrMappingList(MappingSet.Main, stylesheet, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.AddDefaultMappingIfNeeded(@"\cap", ImportDomain.Main, true);
			list.AddDefaultMappingIfNeeded(@"\cat", ImportDomain.Main, true);
			list.AddDefaultMappingIfNeeded(@"\gmb", ImportDomain.Main, true);
			list.AddDefaultMappingIfNeeded(@"\gmbj", ImportDomain.Main, true);
			Assert.AreEqual(4, list.Count);

			ImportMappingInfo info = list[@"\cap"];
			Assert.AreEqual(MarkerDomain.Default, info.Domain);
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.FigureCaption, info.MappingTarget);
			Assert.IsNull(info.StyleName);

			info = list[@"\cat"];
			Assert.AreEqual(MarkerDomain.Default, info.Domain);
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.FigureFilename, info.MappingTarget);
			Assert.IsNull(info.StyleName);

			info = list[@"\gmb"];
			Assert.AreEqual(MarkerDomain.Default, info.Domain);
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.FigureFilename, info.MappingTarget);
			Assert.IsNull(info.StyleName);

			info = list[@"\gmbj"];
			Assert.AreEqual(MarkerDomain.Default, info.Domain);
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.FigureCaption, info.MappingTarget);
			Assert.IsNull(info.StyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that back-translation markers map automatically (keys off a "bt" prefix).
		/// Jira task is TE-1812
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddDefaultMappingIfNeeded_btMappings()
		{
			IVwStylesheet stylesheet = MockRepository.GenerateStub<IVwStylesheet>();

			stylesheet.Stub(x => x.GetContext(ScrStyleNames.NormalFootnoteParagraph)).Return((int)ContextValues.Note);
			stylesheet.Stub(x => x.GetContext(ScrStyleNames.NormalParagraph)).Return((int)ContextValues.Text);
			stylesheet.Stub(x => x.GetContext("Emphasis")).Return((int)ContextValues.General);
			stylesheet.Stub(x => x.GetContext(ScrStyleNames.Remark)).Return((int)ContextValues.Annotation);

			stylesheet.Stub(x => x.GetType(ScrStyleNames.NormalFootnoteParagraph)).Return((int)StyleType.kstParagraph);
			stylesheet.Stub(x => x.GetType(ScrStyleNames.NormalParagraph)).Return((int)StyleType.kstParagraph);
			stylesheet.Stub(x => x.GetType("Emphasis")).Return((int)StyleType.kstCharacter);
			stylesheet.Stub(x => x.GetType(ScrStyleNames.Remark)).Return((int)StyleType.kstParagraph);

			ScrMappingList list = new ScrMappingList(MappingSet.Main, stylesheet, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.AddDefaultMappingIfNeeded(@"\bt", ImportDomain.Main, true);
			list.AddDefaultMappingIfNeeded(@"\btc", ImportDomain.Main, true);
			list.AddDefaultMappingIfNeeded(@"\btf", ImportDomain.Main, true);
			list.AddDefaultMappingIfNeeded(@"\btp", ImportDomain.Main, true);
			list.Add(new ImportMappingInfo(@"\emph", null, "Emphasis"));
			list.AddDefaultMappingIfNeeded(@"\btemph", ImportDomain.Main, true);
			list.AddDefaultMappingIfNeeded(@"\btrem", ImportDomain.Main, true);
			list.AddDefaultMappingIfNeeded(@"\bty", ImportDomain.Main, true);
			Assert.AreEqual(8, list.Count);

			// Test that \bt does not map automatically as a Back-trans marker.
			ImportMappingInfo info = list[@"\bt"];
			Assert.AreEqual(MarkerDomain.Default, info.Domain, @"\bt should not map automatically as a Back-trans marker");
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.TEStyle, info.MappingTarget);
			Assert.IsNull(info.StyleName);

			// Test that \btc does not map automatically as a Back-trans marker (this is a special exception to the rul).
			info = list[@"\btc"];
			Assert.AreEqual(MarkerDomain.Default, info.Domain, @"\btc should not map automatically as a Back-trans marker");
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.TEStyle, info.MappingTarget);
			Assert.IsNull(info.StyleName);

			// Test that \btf maps automatically as a Back-trans marker.
			info = list[@"\btf"];
			Assert.AreEqual(MarkerDomain.BackTrans | MarkerDomain.Footnote, info.Domain, @"\btf should map automatically as a Back-trans marker");
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.TEStyle, info.MappingTarget);
			Assert.AreEqual(ScrStyleNames.NormalFootnoteParagraph, info.StyleName);

			// Test that \btp maps automatically as a Back-trans marker.
			info = list[@"\btp"];
			Assert.AreEqual(MarkerDomain.BackTrans, info.Domain, @"\btp should map automatically as a Back-trans marker");
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.TEStyle, info.MappingTarget);
			Assert.AreEqual(ScrStyleNames.NormalParagraph, info.StyleName);

			// Test that \btemph maps automatically to the corresponding vernacular style but does not map
			// into the Back-trans marker domain because \emph is a character style.
			info = list[@"\btemph"];
			Assert.AreEqual(MarkerDomain.Default, info.Domain, @"\btemph should not map automatically as a Back-trans marker");
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.TEStyle, info.MappingTarget);
			Assert.AreEqual("Emphasis", info.StyleName);

			// Test that \btrem does not map automatically as a Back-trans marker (because \rem is a Note style).
			info = list[@"\btrem"];
			Assert.AreEqual(MarkerDomain.Default, info.Domain, @"\btrem should not map automatically as a Back-trans marker");
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.TEStyle, info.MappingTarget);
			Assert.IsNull(info.StyleName);

			// Test that \bty does not map automatically as a Back-trans marker (because \y has no default mapping).
			info = list[@"\bty"];
			Assert.AreEqual(MarkerDomain.Default, info.Domain, @"\bty should not map automatically as a Back-trans marker");
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.TEStyle, info.MappingTarget);
			Assert.IsNull(info.StyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that back-translation markers map automatically (keys off a "bt" prefix) when
		/// corresponding vernacular marker is mapped to a non-default style.
		/// Jira task is TE-1812
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddDefaultMappingIfNeeded_btMappingsWithNonDefaultMappings()
		{
			IVwStylesheet stylesheet = MockRepository.GenerateStub<IVwStylesheet>();
			stylesheet.Stub(x => x.GetContext("Emphasis")).Return((int)ContextValues.General);
			stylesheet.Stub(x => x.GetType("Emphasis")).Return((int)StyleType.kstCharacter);

			ScrMappingList list = new ScrMappingList(MappingSet.Main, stylesheet, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.Add(new ImportMappingInfo(@"\p", null, "Emphasis"));
			list.AddDefaultMappingIfNeeded(@"\btp", ImportDomain.Main, true);
			Assert.AreEqual(2, list.Count);

			// Test that \btp maps automatically to the corresponding vernacular style ("Emphasis")
			// but does not map into the Back-trans marker domain because Emphasis is a character style.
			ImportMappingInfo info = list[@"\btp"];
			Assert.AreEqual(MarkerDomain.Default, info.Domain, @"\btp should not map automatically as a Back-trans marker");
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.TEStyle, info.MappingTarget);
			Assert.AreEqual("Emphasis", info.StyleName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests that back-translation markers map automatically (keys off a "bt" prefix) when
		/// corresponding vernacular marker is mapped to a property, not a real style.
		/// Jira task is TE-1812
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void AddDefaultMappingIfNeeded_btNotFromTeStyle()
		{
			IVwStylesheet stylesheet = MockRepository.GenerateStrictMock<IVwStylesheet>();

			ScrMappingList list = new ScrMappingList(MappingSet.Main, stylesheet, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.Add(new ImportMappingInfo(@"\h", null, false, MappingTargetType.TitleShort,
				MarkerDomain.Default, null, null));
			list.AddDefaultMappingIfNeeded(@"\bth", ImportDomain.Main, true);
			list.Add(new ImportMappingInfo(@"\vt", null, false, MappingTargetType.DefaultParaChars,
				MarkerDomain.Default, null, null));
			list.AddDefaultMappingIfNeeded(@"\btvt", ImportDomain.Main, true);
			Assert.AreEqual(4, list.Count);

			// Test that \bth maps automatically to the corresponding vernacular import property
			// in the Back-trans marker domain.
			ImportMappingInfo info = list[@"\bth"];
			Assert.AreEqual(MarkerDomain.BackTrans, info.Domain);
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.TitleShort, info.MappingTarget);

			// Test that \btvt maps automatically to Default Paragraph Characters
			// in the Back-trans marker domain.
			info = list[@"\btvt"];
			Assert.AreEqual(MarkerDomain.BackTrans, info.Domain);
			Assert.IsFalse(info.IsExcluded);
			Assert.AreEqual(MappingTargetType.DefaultParaChars, info.MappingTarget);
		}
		#endregion

		#region Exception handling
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure attempting to add a null marker throws an exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentNullException))]
		public void AddNullMappingInfo()
		{
			ScrMappingList list = new ScrMappingList(MappingSet.Main, null, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.Add(null);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure attempting to add an ImportMappingInfo with a null marker throws an
		/// exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddInfoWithNullMarker()
		{
			ScrMappingList list = new ScrMappingList(MappingSet.Main, null, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.Add(new ImportMappingInfo(null, null, null));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Make sure attempting to add an ImportMappingInfo with an empty marker throws an
		/// exception
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[ExpectedException(typeof(ArgumentException))]
		public void AddBlankMarker()
		{
			ScrMappingList list = new ScrMappingList(MappingSet.Main, null, ResourceHelper.DefaultParaCharsStyleName, FwDirectoryFinder.TeStylesPath);
			list.Add(new ImportMappingInfo(string.Empty, string.Empty, string.Empty));
		}
		#endregion
	}
}
