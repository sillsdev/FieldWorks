#region // Copyright (c) 2010, SIL International. All Rights Reserved.
// <copyright from='2010' to='2010' company='SIL International'>
//		Copyright (c) 2010, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// Original author: MarkS 2010-06-24 ShowHelpTests.cs

using NUnit.Framework;
using SIL.FieldWorks.Test.TestUtils;
using SIL.Utils;
using System;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// <summary></summary>
	[TestFixture]
	public class ShowHelpTests : BaseTest
	{
		#region RunNonblockingProcess
		/// <summary></summary>
		[Test]
		[Platform(Include="Linux")]
		public void RunNonblockingProcess_basic()
		{
			bool success = (bool)ReflectionHelper.CallStaticMethod("FwUtils.dll", "SIL.FieldWorks.Common.FwUtils.ShowHelp", "RunNonblockingProcess", new string [] {"echo", "blah"});
			Assert.True(success);
		}

		/// <summary></summary>
		[Test]
		[Platform(Include="Linux")]
		public void RunNonblockingProcess_ReportsFailureWhenCommandNotFound()
		{
			bool success = (bool)ReflectionHelper.CallStaticMethod("FwUtils.dll", "SIL.FieldWorks.Common.FwUtils.ShowHelp", "RunNonblockingProcess", new string [] {"nonexistentCommand", "argument"});
			Assert.False(success);
		}

		/// <summary></summary>
		[Test]
		[Platform(Include="Linux")]
		public void RunNonblockingProcess_NullOrEmptyArguments()
		{
			bool success = (bool)ReflectionHelper.CallStaticMethod("FwUtils.dll", "SIL.FieldWorks.Common.FwUtils.ShowHelp", "RunNonblockingProcess", new string [] {"echo", null});
			Assert.True(success);
			success = (bool)ReflectionHelper.CallStaticMethod("FwUtils.dll", "SIL.FieldWorks.Common.FwUtils.ShowHelp", "RunNonblockingProcess", new string [] {"echo", ""});
			Assert.True(success);
		}

		/// <summary></summary>
		[Test]
		[Platform(Include="Linux")]
		public void RunNonblockingProcess_NullOrEmptyCommand()
		{
			bool success = (bool)ReflectionHelper.CallStaticMethod("FwUtils.dll", "SIL.FieldWorks.Common.FwUtils.ShowHelp", "RunNonblockingProcess", new string [] {null, "argument"});
			Assert.False(success);
			success = (bool)ReflectionHelper.CallStaticMethod("FwUtils.dll", "SIL.FieldWorks.Common.FwUtils.ShowHelp", "RunNonblockingProcess", new string [] {"", "argument"});
			Assert.False(success);
		}
		#endregion

		#region ShowHelpTopic_Linux
		/// <summary></summary>
		[Test]
		[Platform(Include="Linux")]
		[ExpectedException(typeof(ArgumentNullException))]
		public void ShowHelpTopic_Linux_NullHelpFile()
		{
			ShowHelp.ShowHelpTopic_Linux(null, null);
			Assert.Fail("Should have thrown ArgumentNullException for null helpFile");
		}

		/// <summary></summary>
		[Test]
		[Platform(Include="Linux")]
		[ExpectedException(typeof(ArgumentException))]
		public void ShowHelpTopic_Linux_EmptyHelpFile()
		{
			ShowHelp.ShowHelpTopic_Linux(String.Empty, null);
			Assert.Fail("Should have thrown ArgumentException for String.Empty helpFile");
		}
		#endregion
	}
}
