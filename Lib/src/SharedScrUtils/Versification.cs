// Copyright (c) 2015 SIL International
// This software is licensed under the LGPL, version 2.1 or later
// (http://www.gnu.org/licenses/lgpl-2.1.html)

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;

namespace SILUBS.SharedScrUtils
{
	/// <summary>
	/// Manipulate information for standard chatper/verse schemes
	/// </summary>
	public class VersificationTable
	{
		private ScrVers scrVers;
		private List<int[]> bookList;
		private Dictionary<string, string> toStandard;
		private Dictionary<string, string> fromStandard;

		private static string baseDir;
		private static VersificationTable[] versifications = null;

		// Names of the versificaiton files. These are in "\My Paratext Projects"
		private static string[] versificationFiles = new string[] { "",
			"org.vrs", "lxx.vrs", "vul.vrs", "eng.vrs", "rsc.vrs", "rso.vrs", "oth.vrs",
			"oth2.vrs", "oth3.vrs", "oth4.vrs", "oth5.vrs", "oth6.vrs", "oth7.vrs", "oth8.vrs",
			"oth9.vrs", "oth10.vrs", "oth11.vrs", "oth12.vrs", "oth13.vrs", "oth14.vrs",
			"oth15.vrs", "oth16.vrs", "oth17.vrs", "oth18.vrs", "oth19.vrs", "oth20.vrs",
			"oth21.vrs", "oth22.vrs", "oth23.vrs", "oth24.vrs" };

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// This method should be called once before an application accesses anything that
		/// requires versification info.
		/// TODO: Paratext needs to call this with ScrTextCollection.SettingsDirectory.
		/// </summary>
		/// <param name="vrsFolder">Path to the folder containing the .vrs files</param>
		/// ------------------------------------------------------------------------------------
		public static void Initialize(string vrsFolder)
		{
			baseDir = vrsFolder;
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Get the versification table for this versification
		/// </summary>
		/// <param name="vers"></param>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		public static VersificationTable Get(ScrVers vers)
		{
			Debug.Assert(vers != ScrVers.Unknown);

			if (versifications == null)
				versifications = new VersificationTable[versificationFiles.GetUpperBound(0)];

			// Read versification table if not already read
			if (versifications[(int)vers] == null)
			{
				versifications[(int)vers] = new VersificationTable(vers);
				ReadVersificationFile(FileName(vers), versifications[(int)vers]);
			}

			return versifications[(int)vers];
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Read versification file and "add" its entries.
		/// At the moment we only do this once. Eventually we will call this twice.
		/// Once for the standard versification, once for custom entries in versification.vrs
		/// file for this project.
		/// </summary>
		/// <param name="fileName"></param>
		/// <param name="versification"></param>
		/// ------------------------------------------------------------------------------------
		private static void ReadVersificationFile(string fileName, VersificationTable versification)
		{
			using (TextReader reader = new StreamReader(fileName))
			{
				for (string line = reader.ReadLine(); line != null; line = reader.ReadLine())
				{
					line = line.Trim();
					if (line == "" || line[0] == '#')
						continue;

					if (line.Contains("="))
						ParseMappingLine(fileName, versification, line);
					else
						ParseChapterVerseLine(fileName, versification, line);
				}
			}
		}

		// Parse lines mapping from this versification to standard versification
		// GEN 1:10	= GEN 2:11
		// GEN 1:10-13 = GEN 2:11-14
		private static void ParseChapterVerseLine(string fileName, VersificationTable versification, string line)
		{
			string[] parts = line.Split(' ');
			int bookNum = BCVRef.BookToNumber(parts[0]);
			if (bookNum == -1)
				return; // Deuterocanonical books not supported

			if (bookNum == 0)
				throw new Exception("Invalid [" + parts[0] + "] " + fileName);

			while (versification.bookList.Count < bookNum)
				versification.bookList.Add(new int[1] { 1 });

			List<int> verses = new List<int>();

			for (int i = 1; i <= parts.GetUpperBound(0); ++i)
			{
				string[] pieces = parts[i].Split(':');
				int verseCount;
				if (pieces.GetUpperBound(0) != 1 ||
					!int.TryParse(pieces[1], out verseCount) || verseCount <= 0)
				{
					throw new Exception("Invalid [" + line + "] " + fileName);
				}

				verses.Add(verseCount);
			}

			versification.bookList[bookNum - 1] = verses.ToArray();
		}

		// Parse lines giving number of verses for each chapter like
		// GEN 1:10 2:23 ...
		private static void ParseMappingLine(string fileName, VersificationTable versification, string line)
		{
			try
			{
				string[] parts = line.Split('=');
				string[] leftPieces = parts[0].Trim().Split('-');
				string[] rightPieces = parts[1].Trim().Split('-');

				BCVRef left = new BCVRef(leftPieces[0]);
				int leftLimit = leftPieces.GetUpperBound(0) == 0 ? 0 : int.Parse(leftPieces[1]);

				BCVRef right = new BCVRef(rightPieces[0]);

				while (true)
				{
					versification.toStandard[left.ToString()] = right.ToString();
					versification.fromStandard[right.ToString()] = left.ToString();

					if (left.Verse >= leftLimit)
						break;

					left.Verse = left.Verse + 1;
					right.Verse = right.Verse + 1;
				}
			}
			catch
			{
				// ENHANCE: Make it so the TE version of Localizer can have its own resources for stuff
				// like this.
				throw new Exception("Invalid [" + line + "] " + fileName);
			}
		}

		/// <summary>
		/// Gets the name of this requested versification file.
		/// </summary>
		/// <param name="vers">Versification scheme</param>
		public static string GetFileNameForVersification(ScrVers vers)
		{
			return versificationFiles[(int)vers];
		}

		// Get path of this versification file.
		// Fall back to eng.vrs if not present.
		private static string FileName(ScrVers vers)
		{
			if (baseDir == null)
				throw new InvalidOperationException("VersificationTable.Initialize must be called first");

			string fileName = Path.Combine(baseDir, GetFileNameForVersification(vers));

			if (!File.Exists(fileName))
				fileName = Path.Combine(baseDir, GetFileNameForVersification(ScrVers.English));

			return fileName;
		}

		// Create empty versification table
		private VersificationTable(ScrVers vers)
		{
			this.scrVers = vers;

			bookList = new List<int[]>();
			toStandard = new Dictionary<string, string>();
			fromStandard = new Dictionary<string, string>();
		}

		public int LastBook()
		{
			return bookList.Count;
		}

		/// <summary>
		/// Last chapter number in this book.
		/// </summary>
		/// <param name="bookNum"></param>
		/// <returns></returns>
		public int LastChapter(int bookNum)
		{
			if (bookNum <= 0)
				return 0;

			if (bookNum - 1 >= bookList.Count)
				return 1;

			int[] chapters = bookList[bookNum - 1];
			return chapters.GetUpperBound(0) + 1;
		}

		/// <summary>
		/// Last verse number in this book/chapter.
		/// </summary>
		/// <param name="bookNum"></param>
		/// <param name="chapterNum"></param>
		/// <returns></returns>
		public int LastVerse(int bookNum, int chapterNum)
		{
			if (bookNum <= 0)
				return 0;

			if (bookNum - 1 >= bookList.Count)
				return 1;

			int[] chapters = bookList[bookNum - 1];
			// Chapter "0" is the intro material. Pretend that it has 1 verse.
			if (chapterNum - 1 > chapters.GetUpperBound(0) || chapterNum < 1)
				return 1;

			return chapters[chapterNum - 1];
		}

		/// <summary>
		/// Change the passed VerseRef to be this versification.
		/// </summary>
		/// <param name="vref"></param>
		public void ChangeVersification(IVerseReference vref)
		{
			if (vref.Versification == scrVers)
				return;

			// Map from existing to standard versification
			string verse = vref.ToString();
			string verse2;
			Get(vref.Versification).toStandard.TryGetValue(verse, out verse2);
			if (verse2 == null)
				verse2 = verse;

			// Map from standard versification to this versification
			string verse3;
			fromStandard.TryGetValue(verse2, out verse3);
			if (verse3 == null)
				verse3 = verse2;

			// If verse has changed, parse new value
			if (verse != verse3)
				vref.Parse(verse3);

			vref.Versification = scrVers;
		}
	}
}
