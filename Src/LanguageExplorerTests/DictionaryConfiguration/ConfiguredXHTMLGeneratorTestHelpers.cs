// Copyright (c) 2014-2020 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.Collections.Generic;
using SIL.LCModel;

namespace LanguageExplorerTests.DictionaryConfiguration
{
	#region Test classes, interfaces, and enums for testing the reflection code in GetPropertyTypeForConfigurationNode

	public enum FormType { Specified, Unspecified, None }

	public class TestRootClass
	{
		public ITestInterface RootMember { get; set; }
		public TestNonInterface ConcreteMember { get; set; }
	}

	public interface ITestInterface : ITestBaseOne, ITestBaseTwo
	{
		string TestString { get; }
	}

	public interface ITestBaseOne
	{
		IMoForm TestMoForm { get; }
	}

	public interface ITestBaseTwo : ITestGrandParent
	{
		ICmObject TestIcmObject { get; }
	}

	public class TestNonInterface
	{
		// ReSharper disable UnusedMember.Local // Justification: called by reflection
		private string TestNonInterfaceString { get; set; }
		// ReSharper restore UnusedMember.Local
	}

	public interface ITestGrandParent
	{
		Stack<TestRootClass> TestCollection { get; }
	}

	/// <summary />
	/// <remarks>Used by reflection</remarks>
	public class TestPictureClass
	{
		public ILcmList<ICmPicture> Pictures { get; set; }
	}
	#endregion
}
