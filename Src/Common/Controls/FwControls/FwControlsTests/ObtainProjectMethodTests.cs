using System;
using System.IO;
using System.Reflection;
using System.Xml;
using NUnit.Framework;
using SIL.CoreImpl;
using SIL.FieldWorks.FDO;
using SIL.Utils;

namespace SIL.FieldWorks.Common.Controls
{
	/// <summary>
	/// This is the beginnings of tests of the ObtainProjectMethod. Only part of it is tested.
	/// </summary>
	[TestFixture]
	public class ObtainProjectMethodTests
	{
		/// <summary>
		/// Basic test of scanning LIFT file for writing systems
		/// </summary>
		[Test]
		public void DefaultWritingSystemsFromLift_FindsCorrectWritingSystemsFromDefn()
		{
			string input = @"<?xml version='1.0' encoding='utf-8'?>
<lift
	version='0.13'
	producer='SIL.FLEx 7.2.5.41073'>
	<entry
		dateCreated='2012-06-12T18:41:19Z'
		id='bɛben_00ff1845-1d48-47cc-b9f4-cd8834bc70e0'
		guid='00ff1845-1d48-47cc-b9f4-cd8834bc70e0'>
		<lexical-unit>
			<form
				lang='mfo'>
				<text>bɛben</text>
			</form>
		</lexical-unit>
		<trait
			name='morph-type'
			value='stem' />
		<sense
			id='6b800abe-c349-4f6a-8ece-0c03f6203b84'>
			<grammatical-info
				value='Noun'></grammatical-info>
			<definition>
				<form
					lang='sp'>
					<text>dance, music</text>
				</form>
			</definition>
		</sense>
	</entry>
</lift>";
			using (var stringReader = new StringReader(input))
			using (XmlReader reader = XmlReader.Create(stringReader))
			{
				string vernWs, analysisWs;
				ObtainProjectMethod.RetrieveDefaultWritingSystemIdsFromLift(reader, out vernWs, out analysisWs);
				reader.Close();
				Assert.That(vernWs, Is.EqualTo("mfo"));
				Assert.That(analysisWs, Is.EqualTo("sp"));
			}
		}

		/// <summary>
		/// Should also be able to get default analysis from gloss
		/// </summary>
		[Test]
		public void DefaultWritingSystemsFromLift_FindsCorrectWritingSystemsFromGloss()
		{
			string input = @"<?xml version='1.0' encoding='utf-8'?>
<lift
	version='0.13'
	producer='SIL.FLEx 7.2.5.41073'>
	<entry
		dateCreated='2012-06-12T18:41:19Z'
		id='bɛben_00ff1845-1d48-47cc-b9f4-cd8834bc70e0'
		guid='00ff1845-1d48-47cc-b9f4-cd8834bc70e0'>
		<lexical-unit>
			<form
				lang='xyz'>
				<text>bɛben</text>
			</form>
		</lexical-unit>
		<trait
			name='morph-type'
			value='stem' />
		<sense
			id='6b800abe-c349-4f6a-8ece-0c03f6203b84'>
			<grammatical-info
				value='Noun'></grammatical-info>
			<gloss>
				<form
					lang='qed'>
					<text>dance, music</text>
				</form>
			</gloss>
		</sense>
	</entry>
</lift>";
			using (var stringReader = new StringReader(input))
			using (XmlReader reader = XmlReader.Create(stringReader))
			{
				string vernWs, analysisWs;
				ObtainProjectMethod.RetrieveDefaultWritingSystemIdsFromLift(reader, out vernWs, out analysisWs);
				reader.Close();
				Assert.That(vernWs, Is.EqualTo("xyz"));
				Assert.That(analysisWs, Is.EqualTo("qed"));
			}
		}

		/// <summary>
		/// May as well do this since that's our default anyway.
		/// </summary>
		[Test]
		public void DefaultWritingSystemsFromLiftWithNoEntries_ReturnsFrenchAndEnglish()
		{
			string input = @"<?xml version='1.0' encoding='utf-8'?>
<lift
	version='0.13'
	producer='SIL.FLEx 7.2.5.41073'>
</lift>";
			using (var stringReader = new StringReader(input))
			using (XmlReader reader = XmlReader.Create(stringReader))
			{
				string vernWs, analysisWs;
				ObtainProjectMethod.RetrieveDefaultWritingSystemIdsFromLift(reader, out vernWs, out analysisWs);
				reader.Close();
				Assert.That(vernWs, Is.EqualTo("fr"));
				Assert.That(analysisWs, Is.EqualTo("en"));
			}
		}

		/// <summary/>
		[Test]
		public void CallPickAnthro_PickAnthroListCanBeFound()
		{
			var flags = (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
			var type = ReflectionHelper.GetType(ObtainProjectMethod.PickAnthroDll,
															ObtainProjectMethod.PickAnthroClass);
			Assert.NotNull(type, "Class used for PickAnthroList moved.");
			var method = type.GetMethod(ObtainProjectMethod.PickAnthroMethod,
												 new [] { typeof(String), typeof(IHelpTopicProvider) });
			Assert.NotNull(method, "Method name changed, or parameters changed.");
		}

#if RANDYTODO
		// TODO: LexEdDll isn't built yet, so tests fials on remake.
		// TODO: Restore at some point.
		/// <summary/>
		[Test]
		public void CallImportObtainedLexicon_ImportObtainedLexiconCanBeFound()
		{
			var flags = (BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Static);
			var type = ReflectionHelper.GetType(ObtainProjectMethod.ImportLexiconDll,
															ObtainProjectMethod.ImportLexiconClass);
			Assert.NotNull(type, "Class used for ImportObtainedLexicon moved.");
			var method = type.GetMethod(ObtainProjectMethod.ImportLexiconMethod,
												 new[] { typeof(FdoCache), typeof(string), typeof(System.Windows.Forms.Form) });
			Assert.NotNull(method, "Method name changed, or parameters changed.");
		}
#endif
	}
}
