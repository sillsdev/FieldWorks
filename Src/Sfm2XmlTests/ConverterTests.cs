// Copyright (c) 2017 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System.IO;
using NUnit.Framework;
using SIL.TestUtilities;
using Sfm2Xml;

namespace Sfm2XmlTests
{
	[TestFixture]
	public class ConverterTests
	{
		[Test]
		public void ConverterHandlesSubEntryExampleFollowedByEntry()
		{
			const string sfmString = @"\lx a
\ps v
\ge a
\se aa
\xe Should be definition

\lx ab
\ps v
\ge ab";
			const string mappingString = @"<sfmMapping version='6.1'>
<settings>
<meaning app='fw.sil.org'/>
</settings>
<options>
<option id='chkCreateMissingLinks' type='Checkbox' checked='False'/>
</options>
<languages>
<langDef id='National' xml:lang='ignore'/>
<langDef id='English' xml:lang='en'/>
<langDef id='Vernacular' xml:lang='fr'/>
<langDef id='Regional' xml:lang='ignore'/>
</languages>
<hierarchy>
<level name='Entry' partOf='records' beginFields='lx'/>
<level name='Example' partOf='Sense' beginFields='ignore'/>
<level name='ExampleTranslation' partOf='Example' beginFields='xe'/>
<level name='Function' partOf='Subentry Sense Entry' beginFields='ignore'/>
<level name='Picture' partOf='Sense' beginFields='ignore'/>
<level name='Pronunciation' partOf='Entry Subentry' beginFields='ignore'/>
<level name='SemanticDomain' partOf='Sense' beginFields='ignore'/>
<level name='Sense' partOf='Entry Subentry' beginFields='ge ps'/>
<level name='Subentry' partOf='Entry' beginFields='se' uniqueFields='se'/>
</hierarchy>
<fieldDescriptions>
<field sfm='ps' name='Category (Part Of Speech)' type='string' lang='English' abbr='True' >
<meaning app='fw.sil.org' id='pos'/>
</field>
<field sfm='ge' name='Gloss' type='string' lang='English' >
<meaning app='fw.sil.org' id='glos'/>
</field>
<field sfm='lx' name='Lexeme Form' type='string' lang='Vernacular' >
<meaning app='fw.sil.org' id='lex'/>
</field>
<field sfm='se' name='Subentry/Complex Form' type='string' lang='Vernacular' >
<meaning app='fw.sil.org' id='sub' funcWS='en' func='Unspecified Complex Form'/>
</field>
<field sfm='xe' name='Example Translation' type='string' lang='English' >
<meaning app='fw.sil.org' id='trans'/>
</field>
</fieldDescriptions>
</sfmMapping>";
			var sfmFile = Path.GetTempFileName();
			var mappingFile = Path.GetTempFileName();
			var outputFile = Path.GetTempFileName();
			File.WriteAllText(sfmFile, sfmString);
			File.WriteAllText(mappingFile, mappingString);
			var converter = new Converter(null);
			converter.Convert(sfmFile, mappingFile, outputFile);
			AssertThatXmlIn.File(outputFile).HasSpecifiedNumberOfMatchesForXpath("//Entry", 2);
			AssertThatXmlIn.File(outputFile).HasSpecifiedNumberOfMatchesForXpath("//Entry/Subentry", 1);
		}
	}
}
