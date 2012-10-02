// ---------------------------------------------------------------------------------------------
#region // Copyright (c) 2009, SIL International. All Rights Reserved.
// <copyright from='2009' to='2009' company='SIL International'>
//		Copyright (c) 2009, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ParatextAdapterTests.cs
// Responsibility: TE Team
// ---------------------------------------------------------------------------------------------

using NUnit.Framework;

using SIL.FieldWorks.FDO.DomainServices;
using SIL.FieldWorks.Test.ProjectUnpacker;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Integration Tests for ParatextAdapter's interactions with Paratext.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	[Platform(Exclude = "Linux", Reason = "TODO-Linux: ParaText Dependency")]
	public class ParatextAdapterTests : ScrInMemoryFdoTestBase
	{
		#region Tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to save and reload the Scripture and BT Paratext 6 projects
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void LoadParatextMappings_NullProjectName()
		{
			IParatextAdapter sut = new ParatextProxy();

			Assert.IsFalse(sut.LoadProjectMappings(null, null, ImportDomain.Main));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to save and reload the Scripture and BT Paratext 6 projects
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void LoadParatextMappings_Normal()
		{
			Unpacker.UnPackParatextTestProjects();
			IParatextAdapter sut = new ParatextProxy();

			FwStyleSheet stylesheet = new FwStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			ScrMappingList mappingList = new ScrMappingList(MappingSet.Main, stylesheet);

			Assert.IsTrue(sut.LoadProjectMappings("KAM", mappingList, ImportDomain.Main));

			// Test to see that the projects are set correctly
			Assert.AreEqual(44, mappingList.Count);

			Assert.AreEqual(MarkerDomain.Default, mappingList[@"\c"].Domain);
			Assert.AreEqual(MarkerDomain.Default, mappingList[@"\v"].Domain);
			Assert.AreEqual(@"\f*", mappingList[@"\f"].EndMarker);
			Assert.IsTrue(mappingList[@"\p"].IsInUse);
			Assert.IsFalse(mappingList[@"\tb2"].IsInUse);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the ability to load a Paratext 6 project and distinguish between markers in use
		/// in the files and those that only come for them STY file, as well as making sure that
		/// the mappings are not in use when rescanning.
		/// Jiras task is TE-2439
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void LoadParatextMappings_MarkMappingsInUse()
		{
			FwStyleSheet stylesheet = new FwStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			ScrMappingList mappingList = new ScrMappingList(MappingSet.Main, stylesheet);
			mappingList.Add(new ImportMappingInfo(@"\hahaha", @"\*hahaha", false,
				MappingTargetType.TEStyle, MarkerDomain.Default, "laughing",
				null, null, true, ImportDomain.Main));
			mappingList.Add(new ImportMappingInfo(@"\bthahaha", @"\*bthahaha", false,
				MappingTargetType.TEStyle, MarkerDomain.Default, "laughing",
				"en", null, true, ImportDomain.Main));

			Unpacker.UnPackParatextTestProjects();
			IParatextAdapter sut = new ParatextProxy();
			Assert.IsTrue(sut.LoadProjectMappings("TEV", mappingList, ImportDomain.Main));

			Assert.IsTrue(mappingList[@"\c"].IsInUse);
			Assert.IsTrue(mappingList[@"\p"].IsInUse);
			Assert.IsFalse(mappingList[@"\ipi"].IsInUse);
			Assert.IsFalse(mappingList[@"\hahaha"].IsInUse,
				"In-use flag should have been cleared before re-scanning when the P6 project changed.");
			Assert.IsTrue(mappingList[@"\bthahaha"].IsInUse,
				"In-use flag should not have been cleared before re-scanning when the P6 project changed because it was in use by the BT.");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test attempting to load a Paratext project when the Paratext SSF references an
		/// encoding file that does not exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Category("LongRunning")]
		public void LoadParatextMappings_MissingEncodingFile()
		{
			FwStyleSheet stylesheet = new FwStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			ScrMappingList mappingList = new ScrMappingList(MappingSet.Main, stylesheet);

			Unpacker.UnPackMissingFileParatextTestProjects();
			IParatextAdapter sut = new ParatextProxy();
			Assert.IsFalse(sut.LoadProjectMappings("NEC", mappingList, ImportDomain.Main));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test attempting to load a Paratext project when the Paratext SSF references a
		/// style file that does not exist.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		[Ignore("Causes build to hang since Paratext code displays a 'missing style file' message box")]
		public void LoadParatextMappings_MissingStyleFile()
		{
			FwStyleSheet stylesheet = new FwStyleSheet();
			stylesheet.Init(Cache, m_scr.Hvo, ScriptureTags.kflidStyles);
			ScrMappingList mappingList = new ScrMappingList(MappingSet.Main, stylesheet);

			Unpacker.UnPackMissingFileParatextTestProjects();
			IParatextAdapter sut = new ParatextProxy();
			Assert.IsFalse(sut.LoadProjectMappings("NSF", mappingList, ImportDomain.Main));
		}
		#endregion
	}
}
