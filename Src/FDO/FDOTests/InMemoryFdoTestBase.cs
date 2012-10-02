// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2005, SIL International. All Rights Reserved.
// <copyright from='2005' to='2005' company='SIL International'>
//		Copyright (c) 2005, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: InMemoryFdoTestBase.cs
// Responsibility:
// Last reviewed:
//
// <remarks>
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Data.SqlClient;

using NUnit.Framework;
using NMock;

using SIL.FieldWorks.FDO.Cellar;
using SIL.FieldWorks.FDO.Ling;
using SIL.FieldWorks.Common.COMInterfaces;

namespace SIL.FieldWorks.FDO.FDOTests
{
	#region IWsFactoryProvider interface
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// Needed to allow tests to provide factory with overriden methods
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public interface IWsFactoryProvider
	{
		/// -----------------------------------------------------------------------------------
		/// <summary>
		/// This is the only thing in the interface
		/// </summary>
		/// -----------------------------------------------------------------------------------
		ILgWritingSystemFactory NewILgWritingSystemFactory
		{
			get;
		}
	}
	#endregion

	#region InMemoryFdoTestBase class
	/// ---------------------------------------------------------------------------------------
	/// <summary>
	/// A class that uses InMemoryFdoCache instead of the real FdoCache.
	/// </summary>
	/// ---------------------------------------------------------------------------------------
	public abstract class InMemoryFdoTestBase : FdoTestBase, IWsFactoryProvider
	{
		#region Member variables
		/// <summary></summary>
		public const string kParagraphText = "This is a test paragraph.  It has more \"stuff\" in it. \"I wish,\" said John.";
		/// <summary></summary>
		protected InMemoryFdoCache m_inMemoryCache;
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets the <see cref="FdoCache"/> object.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected override FdoCache Cache
		{
			get { return m_inMemoryCache.Cache; }
		}

		#region Test setup and teardown

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
				if (m_inMemoryCache != null)
				{
					if (m_inMemoryCache != null)
						m_inMemoryCache.Dispose();
				}
			}

			// Dispose unmanaged resources here, whether disposing is true or false.
			m_inMemoryCache = null;

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

			Debug.Assert(m_inMemoryCache == null, "m_inMemoryCache is not null, but should be.");
			//if (m_inMemoryCache != null)
			//	m_inMemoryCache.Dispose();
			m_inMemoryCache = CreateInMemoryFdoCache(this);
			m_inMemoryCache.InitializeLangProject();
			m_inMemoryCache.InitializeActionHandler();
			InitializeCache();

			base.Initialize();

			CreateTestData();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Shuts down the cache.
		/// </summary>
		/// <remarks>This method is called after each test</remarks>
		/// ------------------------------------------------------------------------------------
		[TearDown]
		public override void Exit()
		{
			CheckDisposed();

			m_inMemoryCache.Dispose();
			m_inMemoryCache = null;

			base.Exit();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Creates the in memory fdo cache.
		/// </summary>
		/// <param name="provider">The provider.</param>
		/// ------------------------------------------------------------------------------------
		protected virtual InMemoryFdoCache CreateInMemoryFdoCache(IWsFactoryProvider provider)
		{
			return InMemoryFdoCache.CreateInMemoryFdoCache(this);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Called to make the test data for the tests
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void CreateTestData()
		{
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Allows subclasses to do other stuff to initialize the cache before it gets used
		/// </summary>
		/// ------------------------------------------------------------------------------------
		protected virtual void InitializeCache()
		{
		}

		#endregion

		#region Helper methods
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style and add it to the Language Project stylesheet.
		/// </summary>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// <param name="styleCollection">The style collection.</param>
		/// <returns>The style</returns>
		/// ------------------------------------------------------------------------------------
		public IStStyle AddTestStyle(string name, ContextValues context, StructureValues structure,
			FunctionValues function, bool isCharStyle, FdoOwningCollection<IStStyle> styleCollection)
		{
			return AddTestStyle(name, context, structure, function, isCharStyle, 0, styleCollection);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Create a new style and add it to the Language Project stylesheet.
		/// </summary>
		/// <param name="name">style name</param>
		/// <param name="context">style context</param>
		/// <param name="structure">style structure</param>
		/// <param name="function">style function</param>
		/// <param name="isCharStyle">true if character style, otherwise false</param>
		/// <param name="userLevel">The user level.</param>
		/// <param name="styleCollection">The style collection.</param>
		/// <returns>The style</returns>
		/// ------------------------------------------------------------------------------------
		public IStStyle AddTestStyle(string name, ContextValues context, StructureValues structure,
			FunctionValues function, bool isCharStyle, int userLevel,
			FdoOwningCollection<IStStyle> styleCollection)
		{
			CheckDisposed();
			ITsPropsBldr bldr = TsPropsBldrClass.Create();
			StStyle style = new StStyle();
			styleCollection.Add(style);
			style.Name = name;
			style.Context = context;
			style.Structure = structure;
			style.Function = function;
			style.Rules = bldr.GetTextProps();
			style.Type = (isCharStyle ? StyleType.kstCharacter : StyleType.kstParagraph);
			style.UserLevel = userLevel;

			return style;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		///
		/// </summary>
		/// <param name="ld"></param>
		/// <param name="cf"></param>
		/// <param name="defn"></param>
		/// <param name="hvoDomain"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected ILexEntry MakeLexEntry(ILexDb ld, string cf, string defn, int hvoDomain)
		{
			ILexEntry le = ld.EntriesOC.Add(new LexEntry());
			le.CitationForm.VernacularDefaultWritingSystem = cf;
			ILexSense ls = le.SensesOS.Append(new LexSense());
			ls.Definition.AnalysisDefaultWritingSystem.Text = defn;
			if (hvoDomain != 0)
				ls.SemanticDomainsRC.Add(hvoDomain);
			MoMorphSynAnalysis msa = new MoStemMsa();
			le.MorphoSyntaxAnalysesOC.Add(msa);
			ls.MorphoSyntaxAnalysisRA = msa;
			return le;
		}
		#endregion

		#region IWsFactoryProvider Members
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// By default, just return a normal factory
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ILgWritingSystemFactory NewILgWritingSystemFactory
		{
			get
			{
				CheckDisposed();

				ILgWritingSystemFactory wsf = LgWritingSystemFactoryClass.Create();
				wsf.BypassInstall = true;
				return wsf;
			}
		}
		#endregion

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get an array of hvos of a given class out of a vector. (No source vector needed)
		/// </summary>
		/// <param name="className">the class of objects you want</param>
		/// <param name="howMany">how many you want</param>
		/// <returns></returns>
		/// <example>Get the first LexEntry in the lexicon
		/// <code>
		///  GetHvosForFirstNObjectsOfClass(m_fdoCache.LangProject.LexDbOA.EntriesOC,
		///				LexEntry.kclsidLexEntry, 1)[0];
		/// </code></example>
		/// ------------------------------------------------------------------------------------
		protected int[] GetHvosForFirstNObjectsOfClass(string className, int howMany)
		{
			List<int> hvos = new List<int>(howMany);
			Assert.IsTrue(howMany > 0);

			SqlConnection con = new SqlConnection(string.Format("Server={0}; Database={1}; User ID = fwdeveloper;"
				+ "Password=careful; Pooling=false;",
				Cache.ServerName, Cache.DatabaseName));
			con.Open();
			SqlCommand cmd = con.CreateCommand();
			cmd.CommandText = string.Format("select top {0} id from {1}",
						howMany, className);
			SqlDataReader reader = cmd.ExecuteReader();
			while (reader.Read())
			{
				hvos.Add(reader.GetInt32(0));
			}
			Assert.IsTrue(hvos.Count == howMany, "Caller asked for " + howMany.ToString() + " objects, but only " + hvos.Count.ToString() + " were available.");
			reader.Close();
			cmd.Dispose();
			con.Close();
			return hvos.ToArray();
		}
	}
	#endregion
}
