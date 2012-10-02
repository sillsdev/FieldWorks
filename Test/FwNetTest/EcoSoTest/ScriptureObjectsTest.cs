// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2002, SIL International. All Rights Reserved.
// <copyright from='2002' to='2002' company='SIL International'>
//		Copyright (c) 2002, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ScriptureObjectsTest.cs
// Responsibility: DavidO
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using NUnit.Framework;
using System.Diagnostics;
using System.Windows.Forms;
using TESOLib;
using SIL.FieldWorks.Test.ProjectUnpacker;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.ScrImportComponents;

namespace SIL.FieldWorks.ScrImportComponents
{
	#region ScriptureObjectsTest class
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Summary description for ScriptureObjectsTest.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class ScriptureObjectsTest: BaseTest
	{
		RegistryData m_regData;

		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ScriptureObjectsTest"/> class.
		/// </summary>
		/// -----------------------------------------------------------------------------------
		public ScriptureObjectsTest()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unpack test files and create registry entries
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			Unpacker.UnPackParatextTestProjects();
			m_regData = Unpacker.PrepareRegistryForPTData();
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_regData != null)
					m_regData.RestoreRegistryData();

				Unpacker.RemoveParatextTestProjects();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_regData = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the returned project names
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyProjectNames()
		{
			CheckDisposed();

			SCScriptureTextClass scText = new SCScriptureTextClass();

			string texts = scText.TextsPresent;
			Assert.IsTrue(texts == "KAM\r\nTEV\r\n" || texts == "TEV\r\nKAM\r\n");

			scText.Load("KAM");
			Assert.AreEqual("Kamwe", scText.FullName);
			scText.Load("TEV");
			Assert.AreEqual("PREDISTRIBUTION Today's English Version (USFM)", scText.FullName);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the returned TE styles
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void VerifyTeStyleNames()
		{
			CheckDisposed();

			string[] ptStyle = new string[] {"id", "h", "io2", "ip", "c", "v", "p", "q", "mt", "th3", "fv"};
			string[] teStyle = new string[] {
				"Book ID",
				ImportWizard.s_sTitleShortStyle,
				"Intro List Item2",
				"Intro Paragraph",
				"Chapter Number",
				"Verse Number",
				"Paragraph",
				"Citation Line1",
				"Title Main",
				"Table Cell Head",
				"Verse Number In Note"
											  };
			SCScriptureTextClass scText = new SCScriptureTextClass();

			scText.Load("TEV");
			SCTagClass scTag;

			for (int i = 0; i < ptStyle.Length; i++)
			{
				int tagIndex = scText.TagIndex(ptStyle[i]);
				scTag = (SCTagClass)scText.NthTag(tagIndex);
				Assert.IsNotNull(scTag);
				Debug.WriteLine(i.ToString() + " - " + scTag.TeStyleName);
				Assert.AreEqual(teStyle[i], scTag.TeStyleName);
			}
		}
		/*
		 * 		/// ------------------------------------------------------------------------------------
				/// <summary>
				/// Test the <see cref="SCScriptureTextClass.GetBooksForFile"/> method
				/// </summary>
				/// ------------------------------------------------------------------------------------
				[Test]
				public void VerifyGetScrBooksFromSFFile()
				{
					CheckDisposed();

					SCScriptureTextClass scText = new SCScriptureTextClass();

					// Test with file with only one book in it
					// Romans is really book 45...
					string testFolder = Unpacker.PtProjectTestFolder;
					System.Array scrBooks =
						(System.Array)scText.GetBooksForFile(testFolder + "KAM\\46ROM.KAM", "\\id");

					Assert.AreEqual(1, scrBooks.Length);
					Assert.AreEqual(45, (short)scrBooks.GetValue(0));

					scrBooks = (System.Array)scText.GetBooksForFile(testFolder + "SOTest.sfm", "\\id");
					Assert.AreEqual(2, scrBooks.Length);
					Assert.AreEqual(56, (short)scrBooks.GetValue(0));
					Assert.AreEqual(65, (short)scrBooks.GetValue(1));
				}
		*/
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the getting of style tags
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void GetStyleTags()
		{
			CheckDisposed();

			SCScriptureTextClass scText = new SCScriptureTextClass();

			scText.Load("TEV");
			SCTagClass scTag;

			string endMarker = "";
			int iEndMarker = 0;
			int i;

			for(i = 0; scText.NthTag(i) != null; i++)
			{
				scTag = (SCTagClass)scText.NthTag(i);
				// Usefull when the USFM.sty is changed and the marker positions are changed
				// Debug.WriteLine(i + "\t\t" + scTag.Marker + "\t\t" + scTag.Endmarker);
				switch(i)
				{
					case 0:
						Assert.AreEqual("id", scTag.Marker);
						Assert.AreEqual("", scTag.Endmarker);
						break;
					case 19:
						Assert.AreEqual("io2", scTag.Marker);
						Assert.AreEqual("", scTag.Endmarker);
						break;
					case 171:
						Assert.AreEqual("nd", scTag.Marker);
						Assert.AreEqual("nd*", scTag.Endmarker);
						break;
					case 172:
						Assert.AreEqual("nd*", scTag.Marker);
						Assert.AreEqual("", scTag.Endmarker);
						break;
					default:
						break;
				}

				// if we got an end marker we expect the next marker to be the end marker
				if (endMarker != string.Empty)
				{
					Assert.AreEqual(endMarker, scTag.Marker);
					Assert.AreEqual(iEndMarker + 1, i);
					endMarker = "";
					iEndMarker = 0;
				}

				// if we have an endmarker, remember that for comparison with next marker
				if (scTag.Endmarker != string.Empty)
				{
					endMarker = scTag.Endmarker;
					iEndMarker = i;
				}
			}

			// our TEV project has 158 markers and 43 endmarkers (which count separately).
			Assert.AreEqual(201, i);
		}
	}

	#endregion

	#region SCTextSegment tests
	///  ----------------------------------------------------------------------------------------
	/// <summary>
	/// Test the <see cref="SCTextSegment"/> COM object
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class SCTextSegmentTests: BaseTest
	{
		RegistryData m_regData;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Unpack test files and create registry entries
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			CheckDisposed();
			base.FixtureSetup();

			Unpacker.UnPackParatextTestProjects();
			m_regData = Unpacker.PrepareRegistryForPTData();
		}

		#region IDisposable override

		/// <summary>
		/// Executes in two distinct scenarios.
		///
		/// 1. If disposing is true, the method has been called directly
		/// or indirectly by a user's code via the Dispose method.
		/// Both managed and unmanaged resources can be disposed.
		///
		/// 2. If disposing is false, the method has been called by the
		/// runtime from inside the finalizer and you should not reference (access)
		/// other managed objects, as they already have been garbage collected.
		/// Only unmanaged resources can be disposed.
		/// </summary>
		/// <param name="disposing"></param>
		/// <remarks>
		/// If any exceptions are thrown, that is fine.
		/// If the method is being done in a finalizer, it will be ignored.
		/// If it is thrown by client code calling Dispose,
		/// it needs to be handled by fixing the bug.
		///
		/// If subclasses override this method, they should call the base implementation.
		/// </remarks>
		protected override void Dispose(bool disposing)
		{
			//Debug.WriteLineIf(!disposing, "****************** " + GetType().Name + " 'disposing' is false. ******************");
			// Must not be run more than once.
			if (IsDisposed)
				return;

			if (disposing)
			{
				// Dispose managed resources here.
				if (m_regData != null)
					m_regData.RestoreRegistryData();

				Unpacker.RemoveParatextTestProjects();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_regData = null;

			base.Dispose(disposing);
		}
		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Test the creation of the <see cref="SCTextSegment"/> COM object
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Create()
		{
			CheckDisposed();

			// Text segment, gets advanced through the text.
			bool fOk = false;
			try
			{
				SCTextSegment scTextSegment = new SCTextSegmentClass();

				// If we come so far, object creation worked
				fOk = true;
			}
			catch
			{
			}

			Assert.IsTrue(fOk, "Can't create SCTextSegment object. Adjust the registry key " +
				@"'HKEY_LOCAL_MACHINE\SOFTWARE\ScrChecks\1.0\Settings_Directory'");
		}
	}
	#endregion
}
