// Copyright (c) 2009-2013 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)
//
// File: RootSiteEditingHelperTests.cs
// Responsibility: FW Team

using System;
using System.Collections.Generic;
using System.IO;

using NUnit.Framework;

using SIL.CoreImpl;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.FDOTests;

namespace SIL.FieldWorks.Common.RootSites
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class RootSiteEditingHelperTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the TextRepOfObj when the guid doesn't reference a real object in the database
		/// (TE-5012).
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestTextRepOfObj_InvalidObject()
		{
			using (var editHelper = new RootSiteEditingHelper(Cache, null))
			{
				string sTextRepOfObject = editHelper.TextRepOfObj(Cache, Guid.Empty);
				Assert.IsNull(sTextRepOfObject);
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the TextRepOfObj and MakeObjFromText methods (for copying and pasting pictures)
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void TestTextRepOfObj_CmPicture()
		{
			string internalPathOrig = null;
			string internalPathNew = null;
			try
			{
				using (DummyFileMaker filemaker = new DummyFileMaker("junk.jpg", true))
				{
					ITsStrFactory factory = TsStrFactoryClass.Create();
					using (var editHelper = new RootSiteEditingHelper(Cache, null))
					{
						ICmPicture pict = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create(
							filemaker.Filename, factory.MakeString("Test picture", Cache.DefaultVernWs),
							CmFolderTags.LocalPictures);
						Assert.IsNotNull(pict);
						Assert.IsTrue(pict.PictureFileRA.AbsoluteInternalPath == pict.PictureFileRA.InternalPath);
						string sTextRepOfObject = editHelper.TextRepOfObj(Cache, pict.Guid);
						int objectDataType;
						Guid guid = editHelper.MakeObjFromText(Cache, sTextRepOfObject, null,
							out objectDataType);
						ICmPicture pictNew = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);
						Assert.IsTrue(pict != pictNew);
						internalPathOrig = pict.PictureFileRA.AbsoluteInternalPath;
						internalPathNew = pictNew.PictureFileRA.AbsoluteInternalPath;
						Assert.AreEqual(internalPathOrig, internalPathNew);
						Assert.AreEqual(internalPathOrig.IndexOf("junk"), internalPathNew.IndexOf("junk"));
						Assert.IsTrue(internalPathNew.EndsWith(".jpg"));
						AssertEx.AreTsStringsEqual(pict.Caption.VernacularDefaultWritingSystem,
							pictNew.Caption.VernacularDefaultWritingSystem);
						Assert.AreEqual(pict.PictureFileRA.Owner, pictNew.PictureFileRA.Owner);
					}
				}
			}
			finally
			{
				// TODO: When Undo works right, these should get cleaned up automatically
				if (internalPathOrig != null)
					File.Delete(internalPathOrig);
				if (internalPathNew != null)
					File.Delete(internalPathNew);
			}
		}
	}
}
