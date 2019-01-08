// Copyright (c) 2017-2019 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using System.Text;
using LanguageExplorer.SendReceive;
using NUnit.Framework;

namespace LanguageExplorerTests.SendReceive
{
	/// <summary />
	[TestFixture]
	public class LiftBridgeTests
	{
		private readonly string _flexNote = @"<annotation
		class='question'
		ref='silfw://localhost/link?app=flex&amp;database=current&amp;server=&amp;tool=default&amp;guid=6b466f54-f88a-42f6-b770-aca8fee5734c&amp;tag=&amp;id=6b466f54-f88a-42f6-b770-aca8fee5734c&amp;label=bother'
		guid='10fa26c9-ce35-4341-8a30-c1aa1250d0e0'>
		<message
			author='WhoAmI?'
			status=''
			date='2013-01-28T08:51:40Z'
			guid='25f47900-f6f6-4288-89ac-44f738e63431'>Is this the strongest expression of annoyance?</message>
	</annotation>
".Replace("'", "\"");
		private readonly string _liftNote = @"<annotation
		class='question'
		ref='lift://Fred.lift?type=entry&amp;label=bother&amp;id=6b466f54-f88a-42f6-b770-aca8fee5734c'
		guid='10fa26c9-ce35-4341-8a30-c1aa1250d0e0'>
		<message
			author='WhoAmI?'
			status=''
			date='2013-01-28T08:51:40Z'
			guid='25f47900-f6f6-4288-89ac-44f738e63431'>Is this the strongest expression of annoyance?</message>
	</annotation>
".Replace("'", "\"");

		[Test]
		public void ConvertFlexNotesToLift_ConvertsRefs()
		{
			var builder = new StringBuilder();
			using (var reader = new StringReader(_flexNote))
			using (var writer = new StringWriter(builder))
			{
				LiftBridge.ConvertFlexNotesToLift(reader, writer, "Fred.lift");
			}
			Assert.That(builder.ToString(), Is.EqualTo(_liftNote));
		}

		[Test]
		public void ConvertLiftNotesToFlex_ConvertsRefs()
		{
			var builder = new StringBuilder();
			using (var reader = new StringReader(_liftNote))
			using (var writer = new StringWriter(builder))
			{
				LiftBridge.ConvertLiftNotesToFlex(reader, writer);
			}
			Assert.That(builder.ToString(), Is.EqualTo(_flexNote));
		}
	}
}