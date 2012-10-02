// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2003' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: ProjectTestBase.cs
// Responsibility: TE Team
// --------------------------------------------------------------------------------------------
using System;
using System.IO;
using SIL.FieldWorks.FDO.FDOTests;
using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Infrastructure;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.AcceptanceTests.TE
{

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for tests that want the cache loaded from an XML file.
	/// The TestFixture Setup loads the project from a writeable copy of the XML file.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class XmlProjectTestBase : FdoTestBase
	{
		// Member variables
		//our subclass must set this filename to the file wants loaded from the Test folder
		protected string m_xmlProjectFilename;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This override loads the cache with the data from the given XML file.
		/// </summary>
		/// <returns>the FdoCache, now loaded</returns>
		/// ------------------------------------------------------------------------------------
		protected override FdoCache CreateCache()
		{
			// Get the full path to the given xml file
	// TODO: move some of this to a new DirectoryFinder.FWTestDirectory()
			string projdir = Path.GetDirectoryName(DirectoryFinder.FwSourceDirectory);
			string testdir = Path.Combine(projdir,"Test");
			string sourcePathname = Path.Combine(testdir, m_xmlProjectFilename);

			// Copy the given xml file to a writeable temp file
			string xmlTempFilename = "temp.xml";
			string tempPathname = Path.Combine(testdir, xmlTempFilename);
			File.Copy(sourcePathname, tempPathname, true);

			// Start the cache using the temp xml file
			System.Diagnostics.Debug.WriteLine("loading project from " + m_xmlProjectFilename);
			//Logger.WriteEvent("loading project from " + m_xmlProjectFilename);
			DateTime beg = DateTime.Now;
			m_internalRestart = true; //what does this mean in English?
			BackendBulkLoadDomain loadType = BackendBulkLoadDomain.All;
			FdoCache c = BootstrapSystem(FDOBackendProviderType.kXML, new object[] { tempPathname }, loadType);
			System.Diagnostics.Debug.WriteLine("project load time: " + (DateTime.Now - beg));
			//Logger.WriteEvent("project load time: " + (DateTime.Now - beg));
			return c;
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// A base class for tests that want the cache loaded from an XML file.
	/// This class allows tests to do any kind of data changes without worrying about starting a UOW.
	/// The Test Teardown reverts all changes done in the cache, so that the next test
	///  has a restored copy of the data.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class XmlProjectRestoredForEachTestTestBase : XmlProjectTestBase
	{
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to start an undoable UOW for the test.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestSetup()
		{
			base.TestSetup();
			m_actionHandler.BeginUndoTask("Undo doing stuff", "Redo doing stuff");
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Override to end the undoable UOW, Undo everything, and 'commit',
		/// which will essentially clear out the Redo stack,
		/// so that the next test has a restored copy of the data.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public override void TestTearDown()
		{
			UndoAll();

			// Need to 'Commit' to clear out redo stack,
			// since nothing is really saved.
			m_actionHandler.Commit();

			base.TestTearDown();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// End the undoable UOW and Undo everything.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected void UndoAll()
		{
			// This ends a UOW, but with no Commit.
			if (m_actionHandler.CurrentDepth > 0)
				m_actionHandler.EndUndoTask();
			// Undo the UOW (or more than one of them, if the test made new ones).
			while (m_actionHandler.CanUndo())
				m_actionHandler.Undo();
		}
	}
}
