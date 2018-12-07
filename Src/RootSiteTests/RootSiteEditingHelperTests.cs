// Copyright (c) 2009-2018 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.IO;
using NUnit.Framework;
using SIL.LCModel;
using SIL.LCModel.Core.Text;
using SIL.LCModel.Utils;

namespace SIL.FieldWorks.Common.RootSites
{
	/// <summary />
	[TestFixture]
	public class RootSiteEditingHelperTests : MemoryOnlyBackendProviderRestoredForEachTestTestBase
	{
		/// <summary>
		/// Test the TextRepOfObj when the guid doesn't reference a real object in the database
		/// (TE-5012).
		/// </summary>
		[Test]
		public void TestTextRepOfObj_InvalidObject()
		{
			using (var editHelper = new RootSiteEditingHelper(Cache, null))
			{
				var sTextRepOfObject = editHelper.TextRepOfObj(Cache, Guid.Empty);
				Assert.IsNull(sTextRepOfObject);
			}
		}

		/// <summary>
		/// Test the TextRepOfObj and MakeObjFromText methods (for copying and pasting pictures)
		/// </summary>
		[Test]
		public void TestTextRepOfObj_CmPicture()
		{
			string internalPathOrig = null;
			string internalPathNew = null;
			try
			{
				using (var filemaker = new DummyFileMaker("junk.jpg", true))
				using (var editHelper = new RootSiteEditingHelper(Cache, null))
				{
					var pict = Cache.ServiceLocator.GetInstance<ICmPictureFactory>().Create(filemaker.Filename, TsStringUtils.MakeString("Test picture", Cache.DefaultVernWs), CmFolderTags.LocalPictures);
					Assert.IsNotNull(pict);
					Assert.IsTrue(pict.PictureFileRA.AbsoluteInternalPath == pict.PictureFileRA.InternalPath);
					var sTextRepOfObject = editHelper.TextRepOfObj(Cache, pict.Guid);
					int objectDataType;
					var guid = editHelper.MakeObjFromText(Cache, sTextRepOfObject, null, out objectDataType);
					var pictNew = Cache.ServiceLocator.GetInstance<ICmPictureRepository>().GetObject(guid);
					Assert.IsTrue(pict != pictNew);
					internalPathOrig = pict.PictureFileRA.AbsoluteInternalPath;
					internalPathNew = pictNew.PictureFileRA.AbsoluteInternalPath;
					Assert.AreEqual(internalPathOrig, internalPathNew);
					Assert.AreEqual(internalPathOrig.IndexOf("junk"), internalPathNew.IndexOf("junk"));
					Assert.IsTrue(internalPathNew.EndsWith(".jpg"));
					AssertEx.AreTsStringsEqual(pict.Caption.VernacularDefaultWritingSystem, pictNew.Caption.VernacularDefaultWritingSystem);
					Assert.AreEqual(pict.PictureFileRA.Owner, pictNew.PictureFileRA.Owner);
				}
			}
			finally
			{
				// TODO: When Undo works right, these should get cleaned up automatically
				if (internalPathOrig != null)
				{
					File.Delete(internalPathOrig);
				}
				if (internalPathNew != null)
				{
					File.Delete(internalPathNew);
				}
			}
		}
	}
}