// Copyright (c) 2010-2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using NUnit.Framework;

namespace SIL.FieldWorks.Common.FwUtils
{
	/// ----------------------------------------------------------------------------------------
	/// <summary>
	///
	/// </summary>
	/// ----------------------------------------------------------------------------------------
	[TestFixture]
	public class FwLinkArgsTests
	{
		#region Equals tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Equals method when the parameter is another FwLinkArgs with
		/// the exact same information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Equals_ExactlyTheSame()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("myTool", newGuid, "myTag");
			Assert.That(args1.Equals(new FwLinkArgs("myTool", newGuid, "myTag")), Is.True);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Equals method when the parameter is the same FwLinkArgs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Equals_SameObject()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("myTool", newGuid, "myTag");
			Assert.That(args1.Equals(args1), Is.True);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Equals method with a null parameter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Equals_NullParameter()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("myTool", newGuid, "myTag");
			Assert.That(args1.Equals(null), Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Equals method when the parameter is another FwLinkArgs with a
		/// different tool name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Equals_DifferByToolName()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("myTool", newGuid, "myTag");
			Assert.That(args1.Equals(new FwLinkArgs("myOtherTool", newGuid, "myTag")), Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Equals method when the parameter is another FwLinkArgs with a
		/// tool name that differs only in case
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Equals_ToolNameDiffersByCase()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("MyTool", newGuid, "myTag");
			Assert.That(args1.Equals(new FwLinkArgs("mytool", newGuid, "myTag")), Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Equals method when the parameter is another FwLinkArgs with a
		/// different target guid
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Equals_DiffereByGuid()
		{
			FwLinkArgs args1 = new FwLinkArgs("myTool", Guid.NewGuid(), "myTag");
			Assert.That(args1.Equals(new FwLinkArgs("myTool", Guid.NewGuid(), "myTag")), Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Equals method when the parameter is another FwLinkArgs with a
		/// tag that is an empty string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Equals_TagOfArgumentZeroLength()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("myTool", newGuid, "myTag");
			Assert.That(args1.Equals(new FwLinkArgs("myTool", newGuid, string.Empty)), Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the Equals method when the object has a tag that is an empty string
		/// and the parameter is another FwLinkArgs with a non-empty tag
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void Equals_ThisTagZeroLength()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("myTool", newGuid, string.Empty);
			Assert.That(args1.Equals(new FwLinkArgs("myTool", newGuid, "myTag")), Is.False);
		}
		#endregion

		#region EssentiallyEquals tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EssentiallyEquals method when the parameter is another FwLinkArgs with
		/// the exact same information.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EssentiallyEquals_ExactlyTheSame()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("myTool", newGuid, "myTag");
			Assert.That(args1.EssentiallyEquals(new FwLinkArgs("myTool", newGuid, "myTag")), Is.True);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EssentiallyEquals method when the parameter is the same FwLinkArgs
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EssentiallyEquals_SameObject()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("myTool", newGuid, "myTag");
			Assert.That(args1.EssentiallyEquals(args1), Is.True);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EssentiallyEquals method with a null parameter
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EssentiallyEquals_NullParameter()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("myTool", newGuid, "myTag");
			Assert.That(args1.EssentiallyEquals(null), Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EssentiallyEquals method when the parameter is another FwLinkArgs with a
		/// different tool name
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EssentiallyEquals_DifferByToolName()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("myTool", newGuid, "myTag");
			Assert.That(args1.EssentiallyEquals(new FwLinkArgs("myOtherTool", newGuid, "myTag")), Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EssentiallyEquals method when the parameter is another FwLinkArgs with a
		/// tool name that differs only in case
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EssentiallyEquals_ToolNameDiffersByCase()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("MyTool", newGuid, "myTag");
			Assert.That(args1.EssentiallyEquals(new FwLinkArgs("mytool", newGuid, "myTag")), Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EssentiallyEquals method when the parameter is another FwLinkArgs with a
		/// different target guid
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EssentiallyEquals_DiffereByGuid()
		{
			FwLinkArgs args1 = new FwLinkArgs("myTool", Guid.NewGuid(), "myTag");
			Assert.That(args1.EssentiallyEquals(new FwLinkArgs("myTool", Guid.NewGuid(), "myTag")), Is.False);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EssentiallyEquals method when the parameter is another FwLinkArgs with a
		/// tag that is an empty string
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EssentiallyEquals_TagOfArgumentZeroLength()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("myTool", newGuid, "myTag");
			Assert.That(args1.EssentiallyEquals(new FwLinkArgs("myTool", newGuid, string.Empty)), Is.True);
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests the EssentiallyEquals method when the object has a tag that is an empty string
		/// and the parameter is another FwLinkArgs with a non-empty tag
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void EssentiallyEquals_ThisTagZeroLength()
		{
			Guid newGuid = Guid.NewGuid();
			FwLinkArgs args1 = new FwLinkArgs("myTool", newGuid, string.Empty);
			Assert.That(args1.EssentiallyEquals(new FwLinkArgs("myTool", newGuid, "myTag")), Is.True);
		}
		#endregion

		#region FwAppArgs tests
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating FwAppArgs with a link parameter without the '-link' specified
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFwAppArgs_Link_NoKeySpecified()
		{
			FwAppArgs args = new FwAppArgs("silfw://localhost/link?&database=primate" +
				"&tool=default&guid=F48AC2E4-27E3-404e-965D-9672337E0AAF&tag=");
			Assert.That(args.Database, Is.EqualTo("primate"));
			Assert.That(args.Tag, Is.EqualTo(String.Empty));
			Assert.That(args.TargetGuid, Is.EqualTo(new Guid("F48AC2E4-27E3-404e-965D-9672337E0AAF")));
			Assert.That(args.ToolName, Is.EqualTo("default"));
			Assert.That(args.HasLinkInformation, Is.True);
			Assert.That(args.ConfigFile, Is.EqualTo(string.Empty));
			Assert.That(args.DatabaseType, Is.EqualTo(string.Empty));
			Assert.That(args.Locale, Is.EqualTo(string.Empty));
			Assert.That(args.ShowHelp, Is.False);
			Assert.That(args.PropertyTableEntries.Count, Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating FwAppArgs with a -link parameter specified
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFwAppArgs_Link_OverridesOtherSettings()
		{
			FwAppArgs args = new FwAppArgs("-db", "monkey",
				"-link", "silfw://localhost/link?&database=primate" +
				"&tool=default&guid=F48AC2E4-27E3-404e-965D-9672337E0AAF&tag=front");
			Assert.That(args.Database, Is.EqualTo("primate"));
			Assert.That(args.Tag, Is.EqualTo("front"));
			Assert.That(args.TargetGuid, Is.EqualTo(new Guid("F48AC2E4-27E3-404e-965D-9672337E0AAF")));
			Assert.That(args.ToolName, Is.EqualTo("default"));
			Assert.That(args.HasLinkInformation, Is.True);
			Assert.That(args.ConfigFile, Is.EqualTo(string.Empty));
			Assert.That(args.DatabaseType, Is.EqualTo(string.Empty));
			Assert.That(args.Locale, Is.EqualTo(string.Empty));
			Assert.That(args.ShowHelp, Is.False);
			Assert.That(args.PropertyTableEntries.Count, Is.EqualTo(0));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating FwAppArgs with a link parameter without a database specified
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[Test]
		public void CreateFwAppArgs_Link_NoDatabaseSpecified()
		{
			FwAppArgs args = new FwAppArgs("silfw://localhost/link?" +
				"&tool=default&guid=F48AC2E4-27E3-404e-965D-9672337E0AAF&tag=");
			Assert.That(args.ShowHelp, Is.True, "Bad arguments should set ShowHelp to true");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating FwAppArgs
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CreateFwAppArgs_Normal()
		{
			FwAppArgs args = new FwAppArgs("-db", "monkey");
			Assert.That(args.Database, Is.EqualTo("monkey"));
			Assert.That(args.ConfigFile, Is.EqualTo(string.Empty));
			Assert.That(args.DatabaseType, Is.EqualTo(string.Empty));
			Assert.That(args.Locale, Is.EqualTo(string.Empty));
			Assert.That(args.ShowHelp, Is.False);
			Assert.That(args.PropertyTableEntries.Count, Is.EqualTo(0));
			Assert.That(args.Tag, Is.EqualTo(string.Empty));
			Assert.That(args.TargetGuid, Is.EqualTo(Guid.Empty));
			Assert.That(args.ToolName, Is.EqualTo(string.Empty));
			Assert.That(args.HasLinkInformation, Is.False);
			Assert.That(args.ToString(), Does.Contain("database%3dmonkey%26"), "missing & after project.");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating FwAppArgs when an unknown switch is passed in (this is okay
		/// because maybe the specific app will know what to do with it).
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CreateFwAppArgs_UnknownSwitch()
		{
			FwAppArgs args = new FwAppArgs("-init", "DN");
			Assert.That(args.PropertyTableEntries.Count, Is.EqualTo(1));
			Assert.That(args.PropertyTableEntries[0].name, Is.EqualTo("init"));
			Assert.That(args.PropertyTableEntries[0].value, Is.EqualTo("DN"));
			Assert.That(args.Database, Is.EqualTo(string.Empty));
			Assert.That(args.ConfigFile, Is.EqualTo(string.Empty));
			Assert.That(args.DatabaseType, Is.EqualTo(string.Empty));
			Assert.That(args.Locale, Is.EqualTo(string.Empty));
			Assert.That(args.ShowHelp, Is.False);
			Assert.That(args.Tag, Is.EqualTo(string.Empty));
			Assert.That(args.TargetGuid, Is.EqualTo(Guid.Empty));
			Assert.That(args.ToolName, Is.EqualTo(string.Empty));
			Assert.That(args.HasLinkInformation, Is.False);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating FwAppArgs when -db and -proj are both specified
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CreateFwAppArgs_DbAndProjSame()
		{
			FwAppArgs args = new FwAppArgs("-db", "tim", "-proj", "monkey");
			Assert.That(args.ShowHelp, Is.True, "Bad arguments should set ShowHelp to true");
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating FwAppArgs when no space separates the switches and values.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CreateFwAppArgs_RunTogether()
		{
			FwAppArgs args = new FwAppArgs("-projmonkey", "-typexml");
			Assert.That(args.Database, Is.EqualTo("monkey"));
			Assert.That(args.ConfigFile, Is.EqualTo(string.Empty));
			Assert.That(args.Locale, Is.EqualTo(string.Empty));
			Assert.That(args.ShowHelp, Is.False);
			Assert.That(args.PropertyTableEntries.Count, Is.EqualTo(1));
			Assert.That(args.Tag, Is.EqualTo(string.Empty));
			Assert.That(args.TargetGuid, Is.EqualTo(Guid.Empty));
			Assert.That(args.ToolName, Is.EqualTo(string.Empty));
			Assert.That(args.HasLinkInformation, Is.False);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating FwAppArgs when user is requesting help.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CreateFwAppArgs_Help()
		{
			FwAppArgs args = new FwAppArgs("-?", "-db", "monkey");
			Assert.That(args.ShowHelp, Is.True);
			Assert.That(args.Database, Is.EqualTo(string.Empty), "Showing help should ignore all other parameters");
			Assert.That(args.DatabaseType, Is.EqualTo(string.Empty), "Showing help should ignore all other parameters");
			Assert.That(args.ConfigFile, Is.EqualTo(string.Empty));
			Assert.That(args.Locale, Is.EqualTo(string.Empty));
			Assert.That(args.PropertyTableEntries.Count, Is.EqualTo(0));
			Assert.That(args.Tag, Is.EqualTo(string.Empty));
			Assert.That(args.TargetGuid, Is.EqualTo(Guid.Empty));
			Assert.That(args.ToolName, Is.EqualTo(string.Empty));
			Assert.That(args.HasLinkInformation, Is.False);

			args = new FwAppArgs(new[] { "-h" });
			Assert.That(args.ShowHelp, Is.True);
			Assert.That(args.Database, Is.EqualTo(string.Empty));
			Assert.That(args.DatabaseType, Is.EqualTo(string.Empty));
			Assert.That(args.ConfigFile, Is.EqualTo(string.Empty));
			Assert.That(args.Locale, Is.EqualTo(string.Empty));
			Assert.That(args.PropertyTableEntries.Count, Is.EqualTo(0));
			Assert.That(args.Tag, Is.EqualTo(string.Empty));
			Assert.That(args.TargetGuid, Is.EqualTo(Guid.Empty));
			Assert.That(args.ToolName, Is.EqualTo(string.Empty));
			Assert.That(args.HasLinkInformation, Is.False);

			args = new FwAppArgs(new[] { "-help" });
			Assert.That(args.ShowHelp, Is.True);
			Assert.That(args.Database, Is.EqualTo(string.Empty));
			Assert.That(args.DatabaseType, Is.EqualTo(string.Empty));
			Assert.That(args.ConfigFile, Is.EqualTo(string.Empty));
			Assert.That(args.Locale, Is.EqualTo(string.Empty));
			Assert.That(args.PropertyTableEntries.Count, Is.EqualTo(0));
			Assert.That(args.Tag, Is.EqualTo(string.Empty));
			Assert.That(args.TargetGuid, Is.EqualTo(Guid.Empty));
			Assert.That(args.ToolName, Is.EqualTo(string.Empty));
			Assert.That(args.HasLinkInformation, Is.False);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Tests creating FwAppArgs with a command-line parameter whose value is a
		/// quoted string consisting of multiple words.
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		public void CreateFwAppArgs_MultiWordQuotedValue()
		{
			FwAppArgs args = new FwAppArgs("-db", "monkey on a string.fwdata");
			Assert.That(args.Database, Is.EqualTo("monkey on a string.fwdata"));
			Assert.That(args.DatabaseType, Is.EqualTo(string.Empty));
			Assert.That(args.ConfigFile, Is.EqualTo(string.Empty));
			Assert.That(args.Locale, Is.EqualTo(string.Empty));
			Assert.That(args.ShowHelp, Is.False);
			Assert.That(args.PropertyTableEntries.Count, Is.EqualTo(0));
			Assert.That(args.Tag, Is.EqualTo(string.Empty));
			Assert.That(args.TargetGuid, Is.EqualTo(Guid.Empty));
			Assert.That(args.ToolName, Is.EqualTo(string.Empty));
			Assert.That(args.HasLinkInformation, Is.False);
		}

		///--------------------------------------------------------------------------------------
		/// <summary>
		/// Can open database by absolute path
		/// </summary>
		///--------------------------------------------------------------------------------------
		[Test]
		[Platform(Include="Linux")]
		public void CreateFwAppArgs_DbAbsolutePath_Linux()
		{
			FwAppArgs args = new FwAppArgs("-db", "/database.fwdata");
			Assert.That(args.Database, Is.EqualTo("/database.fwdata"), "Should be able to open up database by absolute path");
			Assert.That(args.ConfigFile, Is.EqualTo(string.Empty));
			Assert.That(args.DatabaseType, Is.EqualTo(string.Empty));
			Assert.That(args.Locale, Is.EqualTo(string.Empty));
			Assert.That(args.ShowHelp, Is.False);
			Assert.That(args.PropertyTableEntries.Count, Is.EqualTo(0));
			Assert.That(args.Tag, Is.EqualTo(string.Empty));
			Assert.That(args.TargetGuid, Is.EqualTo(Guid.Empty));
			Assert.That(args.ToolName, Is.EqualTo(string.Empty));
			Assert.That(args.HasLinkInformation, Is.False);
		}
		#endregion
	}
}
