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
// File: FdoTestBase.cs
// Responsibility: JohnH, RandyR
// Last reviewed:
//
// <remarks>
// Implements FdoTestBase, the base class for the FDO tests
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Xml;

using NUnit.Framework;

using SIL.FieldWorks.FDO;
using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.FDO.Notebk;
using SIL.FieldWorks.Test.TestUtils;
using SIL.FieldWorks.Common.COMInterfaces;
using SIL.FieldWorks.Common.Utils;
using SIL.FieldWorks.Common.FwUtils;
using SIL.Utils;

namespace SIL.FieldWorks.FDO.FDOTests
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for FDO tests that use a real FdoCache, or a fake one.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public abstract class FdoTestBase : BaseTest
	{
		/// <summary> keeps track of the installed virtual handlers </summary>
		protected List<IVwVirtualHandler> m_installedVirtualHandlers = null;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the <see cref="FdoCache"/> object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected abstract FdoCache Cache
		{
			get;
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
			}

			// Dispose unmanaged resources here, whether disposing is true or false.

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public virtual void Initialize()
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undo everything possible in the FDO cache
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public virtual void Exit()
		{
			CheckDisposed();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Install Virtual properties from Fieldworks xml configuration.
		/// </summary>
		/// <param name="fwInstallFile">The fw install file.</param>
		/// <param name="assemblyNamespaces">The assembly namespaces.</param>
		/// ------------------------------------------------------------------------------------
		protected void InstallVirtuals(string fwInstallFile, string[] assemblyNamespaces)
		{
			m_installedVirtualHandlers = BaseVirtualHandler.InstallVirtuals(fwInstallFile, assemblyNamespaces, Cache);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// actually install the virtual handlers specified in virtuals
		/// </summary>
		/// <param name="virtuals">The virtuals.</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected virtual List<IVwVirtualHandler> InstallVirtuals(XmlNode virtuals)
		{
			return BaseVirtualHandler.InstallVirtuals(virtuals, Cache);
		}
	}

	/// ----------------------------------------------------------------------------------------
	/// <summary>
	/// Base class for FDO tests. Tests will be performed by NUnit.
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	public class InDatabaseFdoTestBase : FdoTestBase
	{
		/// <summary>The FDO cache</summary>
		protected FdoCache m_fdoCache;

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TestFixtureSetUp]
		public override void FixtureSetup()
		{
			base.FixtureSetup();

			if (m_fdoCache != null)
				m_fdoCache.DisposeWithWSFactoryShutdown();
			m_fdoCache = SetupCache();
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
				if (m_fdoCache != null)
					m_fdoCache.DisposeWithWSFactoryShutdown();
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_fdoCache = null;
			m_installedVirtualHandlers = null;

			base.Dispose(disposing);
		}

		#endregion IDisposable override

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize the FDO cache and open database
		/// </summary>
		/// <remarks>This method is called before each test</remarks>
		/// ------------------------------------------------------------------------------------
		[SetUp]
		public override void Initialize()
		{
			CheckDisposed();
			UndoEverythingPossible();
			base.Initialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undo everything possible in the FDO cache
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();
			UndoEverythingPossible();
			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Undo everything possible in the FDO cache
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void UndoEverythingPossible()
		{
			if (m_fdoCache != null)
			{
				using (new IgnorePropChanged(m_fdoCache, PropChangedHandling.SuppressAll))
				{
					UndoResult ures = 0;
					while (m_fdoCache.CanUndo)
					{
						m_fdoCache.Undo(out ures);
						if (ures == UndoResult.kuresFailed || ures == UndoResult.kuresError)
							Assert.Fail("ures should not be == " + ures.ToString());
					}
				}
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initialize a cache for testing. This is static public so other tests that do not
		/// derive from InDatabaseFdoTestBase can create a cache in the same way.
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static FdoCache SetupCache()
		{
			FdoCache cache = FdoCache.Create("TestLangProj");
			// For these tests we don't need to run InstallLanguage.
			ILgWritingSystemFactory wsf = cache.LanguageWritingSystemFactoryAccessor;
			wsf.BypassInstall = true;
			// Don't suppress adding undo actions.
			cache.AddAllActionsForTests = true;
			return cache;
		}

		/// <summary>
		/// Get an array of hvos of a given class out of a vector
		/// </summary>
		/// <param name="hvos">The integer array holding a heterogeneous collection of object Hvos</param>
		/// <param name="classId">the class of objects you want</param>
		/// <param name="howMany">how many you want</param>
		/// <returns></returns>
		/// <example>Get the first LexEntry in the lexicon
		/// <code>
		///  GetHvosForFirstNObjectsOfClass(m_fdoCache.LangProject.LexDbOA.EntriesOC,
		///				LexEntry.kclsidLexEntry, 1)[0];
		/// </code></example>
		protected int[] GetHvosForFirstNObjectsOfClass(int[] hvos, int classId, int howMany)
		{
			Assert.IsTrue(howMany > 0);
			Assert.IsTrue(hvos.Length >= howMany, "Caller asked for "
				+ howMany.ToString()
				+ " objects, but only "
				+ hvos.Length.ToString()
				+ " were available.");

			List<int> result = new List<int>(howMany);
			foreach (int hvo in hvos)
			{
				ICmObject o = CmObject.CreateFromDBObject(m_fdoCache, hvo);
				if(o.ClassID == classId)
					result.Add(hvo);
				if (result.Count == howMany)
					break;
			}

			return result.ToArray();
		}

		/// <summary>
		/// Get the ID of the first LexEntry in the DB.
		/// </summary>
		/// <returns>ID of the first LexEntry in the DB.</returns>
		protected int GetHvoOfALexEntry()
		{
			return GetHvosForFirstNObjectsOfClass(m_fdoCache.LangProject.LexDbOA.EntriesOC.HvoArray,
				LexEntry.kclsidLexEntry, 1)[0];
		}

		/// <summary>
		/// Get the ID of the first RnEvent in the DB.
		/// </summary>
		/// <returns>ID of the first RnEvent in the DB.</returns>
		protected  int GetHvoOfARnEvent()
		{
			return GetHvosForFirstNObjectsOfClass(m_fdoCache.LangProject.ResearchNotebookOA.RecordsOC.HvoArray,
				RnEvent.kclsidRnEvent, 1)[0];
		}

		/// <summary>
		/// Get the ID of the Description of the first RnEvent in the DB.
		/// </summary>
		/// <returns>ID of the Description of the first RnEvent in the DB.</returns>
		protected int GetHvoOfAnStText()
		{
			return (new RnEvent(Cache, GetHvoOfARnEvent())).DescriptionOA.Hvo;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the name of the database
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected string DatabaseName
		{
			get
			{
				return m_fdoCache.DatabaseName;
			}
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the <see cref="FdoCache"/> object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override FdoCache Cache
		{
			get { return m_fdoCache; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a footnote reference marker (ref orc)
		/// </summary>
		/// <param name="footnote">given footnote</param>
		/// <param name="para">paragraph owning the translation to insert footnote marker into</param>
		/// <param name="ws">given writing system for the back translation</param>
		/// <param name="ichPos">The 0-based character offset into the translation</param>
		/// ------------------------------------------------------------------------------------
		protected void InsertTestBtFootnote(StFootnote footnote, StTxtPara para, int ws, int ichPos)
		{
			ICmTranslation trans = para.GetOrCreateBT();
			ITsStrBldr bldr = trans.Translation.GetAlternative(ws).UnderlyingTsString.GetBldr();
			footnote.InsertRefORCIntoTrans(bldr, ichPos, ws);
			trans.Translation.SetAlternative(bldr.GetString(), ws);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a picture (no caption set).
		/// </summary>
		/// <param name="para">Paragraph to insert picture into</param>
		/// <param name="ichPos">The 0-based character offset into the paragraph</param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ICmPicture InsertTestPicture(StTxtPara para, int ichPos)
		{
			// Create the picture
			ICmFolder folder = m_fdoCache.LangProject.PicturesOC.Add(new CmFolder());
			ICmFile file = folder.FilesOC.Add(new CmFile());
			file.InternalPath = "there";
			int newHvo = m_fdoCache.CreateObject(CmPicture.kClassId);
			ICmPicture picture = new CmPicture(m_fdoCache, newHvo);
			picture.PictureFileRA = file;

			// Update the paragraph contents to include the footnote marker
			ITsStrBldr tsStrBldr = para.Contents.UnderlyingTsString.GetBldr();
			(picture as CmPicture).InsertOwningORCIntoPara(tsStrBldr, ichPos, 0); // Don't care about ws
			para.Contents.UnderlyingTsString = tsStrBldr.GetString();

			return picture;
		}
	}
}
