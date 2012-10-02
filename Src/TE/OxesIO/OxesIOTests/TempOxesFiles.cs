using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace OxesIO.Tests
{
	/// <summary>
	/// This class is used to create test OXES files, which are used by either the validator
	/// tests or the migrator tests, or both.
	/// </summary>
	public class TempOxesFiles
	{
		/// <summary>
		/// Create the minimal valid OXES file.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		internal static string MinimalValidFile(string path)
		{
			// This is actually the minimal file that validates!
			StringBuilder bldr = new StringBuilder();
			bldr.AppendLine("<oxes xmlns='http://www.wycliffe.net/scripture/namespace/version_1.1.2'>");
			bldr.AppendLine("    <oxesText xml:lang='en' type='Wycliffe-1.1.1' oxesIDWork='WBT.en' canonical='true'>");
			bldr.AppendLine("        <header>");
			bldr.AppendLine("            <work oxesWork='WBT.en'>");
			bldr.AppendLine("                <titleGroup>");
			bldr.AppendLine("                    <title type='main'><trGroup/></title>");
			bldr.AppendLine("                </titleGroup>");
			bldr.AppendLine("                <contributor ID='x' role='Translator'></contributor>");
			bldr.AppendLine("            </work>");
			bldr.AppendLine("        </header>");
			bldr.AppendLine("        <canon ID='ot'>");
			bldr.AppendLine("            <book ID='GEN'>");
			bldr.AppendLine("                <titleGroup>");
			bldr.AppendLine("                    <title type='main'><trGroup/></title>");
			bldr.AppendLine("                </titleGroup>");
			bldr.AppendLine("                <section>");
			bldr.AppendLine("					<sectionHead/>");
			bldr.AppendLine("					<p/>");
			bldr.AppendLine("                </section>");
			bldr.AppendLine("            </book>");
			bldr.AppendLine("        </canon>");
			bldr.AppendLine("    </oxesText>");
			bldr.AppendLine("</oxes>");
			if (String.IsNullOrEmpty(path))
				path = Path.GetTempFileName();
			File.WriteAllText(path, bldr.ToString());
			return path;
		}

		/// <summary>
		/// Create a minimally valid, but out of date, OXES file.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		internal static string MinimalVersion107File(string path)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendLine("<oxes xmlns='http://www.wycliffe.net/scripture/namespace/version_1.0.7'>");
			bldr.AppendLine("    <oxesText xml:lang='en' type='Wycliffe-1.0.7' oxesIDWork='WBT.en' canonical='true'>");
			bldr.AppendLine("        <header>");
			bldr.AppendLine("            <work oxesWork='WBT.en'>");
			bldr.AppendLine("                <titleGroup>");
			bldr.AppendLine("                    <title type='main'><trGroup/></title>");
			bldr.AppendLine("                </titleGroup>");
			bldr.AppendLine("                <contributor ID='x' role='Translator'></contributor>");
			bldr.AppendLine("            </work>");
			bldr.AppendLine("        </header>");
			bldr.AppendLine("        <canon ID='ot'>");
			bldr.AppendLine("            <book ID='GEN'>");
			bldr.AppendLine("                <titleGroup>");
			bldr.AppendLine("                    <title type='main'><trGroup/></title>");
			bldr.AppendLine("                </titleGroup>");
			bldr.AppendLine("                <section>");
			bldr.AppendLine("					<sectionHead/>");
			bldr.AppendLine("					<p/>");
			bldr.AppendLine("                </section>");
			bldr.AppendLine("            </book>");
			bldr.AppendLine("        </canon>");
			bldr.AppendLine("    </oxesText>");
			bldr.AppendLine("</oxes>");
			if (String.IsNullOrEmpty(path))
				path = Path.GetTempFileName();
			File.WriteAllText(path, bldr.ToString());
			return path;
		}

		/// <summary>
		/// Create a minimally valid, but woefully out of date (cannot be migrated), OXES file.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		internal static string MinimalVersion099File(string path)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendLine("<oxes xmlns='http://www.wycliffe.net/scripture/namespace/version_0.9.9'>");
			bldr.AppendLine("    <oxesText xml:lang='en' type='Wycliffe-0.9.9' oxesIDWork='WBT.en' canonical='true'>");
			bldr.AppendLine("        <header>");
			bldr.AppendLine("            <work oxesWork='WBT.en'>");
			bldr.AppendLine("                <titleGroup>");
			bldr.AppendLine("                    <title type='main'><trGroup/></title>");
			bldr.AppendLine("                </titleGroup>");
			bldr.AppendLine("                <contributor ID='x' role='Translator'></contributor>");
			bldr.AppendLine("            </work>");
			bldr.AppendLine("        </header>");
			bldr.AppendLine("        <canon ID='ot'>");
			bldr.AppendLine("            <book ID='GEN'>");
			bldr.AppendLine("                <titleGroup>");
			bldr.AppendLine("                    <title type='main'><trGroup/></title>");
			bldr.AppendLine("                </titleGroup>");
			bldr.AppendLine("                <section/>");
			bldr.AppendLine("            </book>");
			bldr.AppendLine("        </canon>");
			bldr.AppendLine("    </oxesText>");
			bldr.AppendLine("</oxes>");
			if (String.IsNullOrEmpty(path))
				path = Path.GetTempFileName();
			File.WriteAllText(path, bldr.ToString());
			return path;
		}

		/// <summary>
		/// Create a small valid file.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		internal static string SmallValidFile(string path)
		{
			// This was exported from TE on June 13, 2008.  If the OXES version number changes, then
			// this file needs to change.  If the schema changes enough to invalidate this, then a
			// new version of this small file must be generated.
			StringBuilder bldr = new StringBuilder();
			bldr.AppendLine("<?xml version='1.0' encoding='utf-8'?>");
			bldr.AppendLine("<oxes xmlns='http://www.wycliffe.net/scripture/namespace/version_1.1.2'>");
			bldr.AppendLine("	<oxesText type='Wycliffe-1.1.1' oxesIDWork='WBT.en' xml:lang='en' canonical='true'>");
			bldr.AppendLine("		<header>");
			bldr.AppendLine("			<revisionDesc resp='mcc'>");
			bldr.AppendLine("				<date>2008.07.25</date>");
			bldr.AppendLine("				<para xml:lang='en'>Export by Dallas\\mcconnel from Test</para>");
			bldr.AppendLine("			</revisionDesc>");
			bldr.AppendLine("			<work oxesWork='WBT.en'>");
			bldr.AppendLine("				<titleGroup>");
			bldr.AppendLine("					<title type='main'>");
			bldr.AppendLine("						<trGroup>");
			bldr.AppendLine("							<tr>TODO: title of New Testament or Bible goes here</tr>");
			bldr.AppendLine("						</trGroup>");
			bldr.AppendLine("					</title>");
			bldr.AppendLine("				</titleGroup>");
			bldr.AppendLine("				<contributor role='Translator' ID='mcc'>DALLAS\\mcconnel</contributor>");
			bldr.AppendLine("			</work>");
			bldr.AppendLine("		</header>");
			bldr.AppendLine("		<titlePage>");
			bldr.AppendLine("			<titleGroup>");
			bldr.AppendLine("				<title type='main'>");
			bldr.AppendLine("					<trGroup>");
			bldr.AppendLine("						<tr>TODO: Title of New Testament or Bible goes here</tr>");
			bldr.AppendLine("					</trGroup>");
			bldr.AppendLine("				</title>");
			bldr.AppendLine("			</titleGroup>");
			bldr.AppendLine("		</titlePage>");
			bldr.AppendLine("		<canon ID='nt'>");
			bldr.AppendLine("			<book ID='MAT'>");
			bldr.AppendLine("				<titleGroup short='Matthew'>");
			bldr.AppendLine("					<title type='main'>");
			bldr.AppendLine("						<trGroup>");
			bldr.AppendLine("							<tr>Matthew</tr>");
			bldr.AppendLine("							<bt xml:lang='en'>Matthew</bt>");
			bldr.AppendLine("						</trGroup>");
			bldr.AppendLine("					</title>");
			bldr.AppendLine("				</titleGroup>");
			bldr.AppendLine("				<section>");
			bldr.AppendLine("					<sectionHead>");
			bldr.AppendLine("						<trGroup />");
			bldr.AppendLine("					</sectionHead>");
			bldr.AppendLine("					<p>");
			bldr.AppendLine("						<chapterStart ID='MAT.1' n='1' />");
			bldr.AppendLine("						<verseStart ID='MAT.1.1' n='1' />");
			bldr.AppendLine("						<trGroup>");
			bldr.AppendLine("							<tr>The book of the generation of Jesus Christ, the son of David, the son of Abraham. </tr>");
			bldr.AppendLine("							<bt xml:lang='en'>This is the list of the ancestors of Jesus Christ, a descendant of David, who was a descendant of Abraham. </bt>");
			bldr.AppendLine("						</trGroup>");
			bldr.AppendLine("						<verseEnd ID='MAT.1.1' />");
			bldr.AppendLine("						<verseStart ID='MAT.1.2' n='2' />");
			bldr.AppendLine("						<trGroup>");
			bldr.AppendLine("							<tr>Abraham begat Isaac; and Isaac begat Jacob; and Jacob begat Judas and his brethren;</tr>");
			bldr.AppendLine("							<bt xml:lang='en'>Abraham was the father of Isaac; and Isaac was the father of Jacob; and Jacob was the father of Judah and his brothers;</bt>");
			bldr.AppendLine("						</trGroup>");
			bldr.AppendLine("						<verseEnd ID='MAT.1.2' />");
			bldr.AppendLine("						<chapterEnd ID='MAT.1' />");
			bldr.AppendLine("					</p>");
			bldr.AppendLine("				</section>");
			bldr.AppendLine("			</book>");
			bldr.AppendLine("		</canon>");
			bldr.AppendLine("	</oxesText>");
			bldr.AppendLine("</oxes>");
			if (String.IsNullOrEmpty(path))
				path = Path.GetTempFileName();
			File.WriteAllText(path, bldr.ToString());
			return path;
		}

		/// <summary>
		/// Create a minimal file that has an invalid element inserted.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		internal static string BadMinimalFile(string path)
		{
			StringBuilder bldr = new StringBuilder();
			bldr.AppendLine("<oxes xmlns='http://www.wycliffe.net/scripture/namespace/version_1.1.2'>");
			bldr.AppendLine("    <oxesText xml:lang='en' type='Wycliffe-1.1.1' oxesIDWork='WBT.en' canonical='true'>");
			bldr.AppendLine("        <header>");
			bldr.AppendLine("            <work oxesWork='WBT.en'>");
			bldr.AppendLine("                <title>");
			bldr.AppendLine("                    <title type='main'><trGroup/></title>");
			bldr.AppendLine("                </title>");
			bldr.AppendLine("                <contributor ID='x' role='Translator'></contributor>");
			bldr.AppendLine("            </work>");
			bldr.AppendLine("        </header>");
			bldr.AppendLine("        <canon ID='ot'>");
			bldr.AppendLine("            <book ID='GEN'>");
			bldr.AppendLine("                <title>");
			bldr.AppendLine("                    <title type='main'><trGroup/></title>");
			bldr.AppendLine("                </title>");
			bldr.AppendLine("                <section><tr>BAD ELEMENT FOR THIS CONTEXT!</tr></section>");
			bldr.AppendLine("            </book>");
			bldr.AppendLine("        </canon>");
			bldr.AppendLine("    </oxesText>");
			bldr.AppendLine("</oxes>");
			if (String.IsNullOrEmpty(path))
				path = Path.GetTempFileName();
			File.WriteAllText(path, bldr.ToString());
			return path;
		}

		/// <summary>
		/// Create an empty OXES file, one containing only an empty &lt;oxes&gt; element.
		/// </summary>
		/// <param name="path"></param>
		/// <returns></returns>
		internal static string EmptyOxesFile(string path)
		{
			if (String.IsNullOrEmpty(path))
				path = Path.GetTempFileName();
			File.WriteAllText(path, "<oxes xmlns='http://www.wycliffe.net/scripture/namespace/version_1.1.2'></oxes>");
			return path;
		}
	}
}
