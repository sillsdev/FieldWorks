// --------------------------------------------------------------------------------------------
#region // Copyright (c) 2003, SIL International. All Rights Reserved.
// <copyright from='2003' to='2003' company='SIL International'>
//		Copyright (c) 2003, SIL International. All Rights Reserved.
//
//		Distributable under the terms of either the Common Public License or the
//		GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
#endregion
//
// File: FileComp.cs
// Responsibility: LastufkaM
// Last reviewed:
//
// <remarks>
//  This class is a test driver instruction that compares Files.
// </remarks>
// --------------------------------------------------------------------------------------------
using System;
//using System.Diagnostics;
using System.ComponentModel;
using System.IO;
//using System.Collections;
using System.Security.Cryptography;
using System.Text;
using System.Xml;
using System.Text.RegularExpressions;

namespace GuiTestDriver
{
	/// <summary>
	/// Compares all files, two at a time, in two given folders.
	/// Either two folders or two files are specified.
	/// If any of them are different or missing it returns false.
	///
	/// </summary>
	public class FileComp : CheckBase
	{
		string m_of = null;
		string m_to = null;
		string m_baseStr = null;
		string m_targetStr = null;

		public FileComp()
			: base()
		{
			m_tag = "file-comp";
		}

		/// <summary>
		/// Called to finish construction when an instruction has been instantiated by
		/// a factory and had its properties set.
		/// This can check the integrity of the instruction or perform other initialization tasks.
		/// </summary>
		/// <param name="xn">XML node describing the instruction</param>
		/// <param name="con">Parent xml node instruction</param>
		/// <returns></returns>
		public override bool finishCreation(XmlNode xn, Context con)
		{  // finish factory construction
			m_log.isNotNull(Of, "File-Comp instruction must have an 'of'.");
			m_log.isTrue(Of != "", "File-Comp instruction must have a non-empty 'of'.");
			m_log.isNotNull(To, "File-Comp instruction must have a 'to'.");
			m_log.isTrue(To != "", "File-Comp instruction must have a non-empty 'to'.");
			InterpretMessage(xn.ChildNodes);
			return true;
		}

		public string Of
		{
			get { return m_of; }
			set { m_of = value; }
		}

		public string To
		{
			get { return m_to; }
			set { m_to = value; }
		}

		public override void Execute()
		{
			base.Execute();
			PassFailInContext(m_onPass, m_onFail, out m_onPass, out m_onFail);
			m_log.isNotNull(m_of, makeNameTag() + " 'of' file name is null");
			m_log.isNotNull(m_to, makeNameTag() + " 'to' file name is null");
			m_log.isFalse(m_of == "", makeNameTag() + " 'of' file name is empty");
			m_log.isFalse(m_to == "", makeNameTag() + " 'to' file name is empty");
			m_baseStr = Utilities.evalExpr(m_of);
			m_targetStr = Utilities.evalExpr(m_to);
			// m_baseStr and m_targetStr can be null
			MD5 md5Hasher = MD5.Create();

			try{Application.Process.WaitForInputIdle();}
			catch (Win32Exception e)
			{ m_log.paragraph(makeNameTag() + " WaitForInputIdle: " + e.Message); }

			string[] ext = m_baseStr.Split('.');
			int lastSub = ext.Length;
			string fileType = ext[lastSub - 1];
			string content1 = null;
			string content2 = null;
			try
			{
				content1 = System.IO.File.ReadAllText(m_baseStr);
				content2 = System.IO.File.ReadAllText(m_targetStr);
			}
			catch (FileNotFoundException e) { m_log.fail(makeNameTag() + e.FileName + " not found"); }
			catch (DirectoryNotFoundException e) { m_log.fail(makeNameTag() + e.ToString() + " not found"); }

			if (fileType.ToLower() == "pdf")
			{ // Cut out date and crc file integrity hashes - they always change
				// obj 0a 3c 3c 2f Producer(PrimoPDF)           0a 3e 3e 0a
				// trailer 0a 3c 3c 20            0a 3e 3e 0a
				m_log.paragraph(makeNameTag() + " comparing PDF files: " + m_of + " and " + m_to);
				Regex rx = null;
				Regex rx2 = null;
				string pattern = "(<</Producer\\(PrimoPDF\\)[^>]*>>)";
				string pattern2 = "(trailer[\\s\\S]<<[\\s\\S]*>>)";
				try { rx = new Regex(pattern, System.Text.RegularExpressions.RegexOptions.Multiline);
					  rx2 = new Regex(pattern2, System.Text.RegularExpressions.RegexOptions.Multiline); }
				catch (ArgumentException e)
				{ m_log.paragraph(makeNameTag() + e.Message); }
				FileComp fc = new FileComp();
				MatchEvaluator cut = new MatchEvaluator(fc.cutMatch);
				if (!rx.IsMatch(content1))
					m_log.paragraph(makeNameTag() + " no match in PDF file for pattern: " + pattern);
				content1 = rx.Replace(content1, cut);
				content2 = rx.Replace(content2, cut);
				if (!rx2.IsMatch(content1))
					m_log.paragraph(makeNameTag() + " no match in PDF file for pattern2: " + pattern2);
				content1 = rx2.Replace(content1, cut);
				content2 = rx2.Replace(content2, cut);
			}
			System.Text.Encoding encoding = new System.Text.UnicodeEncoding();
			byte[] hash1 = md5Hasher.ComputeHash(encoding.GetBytes(content1));
			byte[] hash2 = md5Hasher.ComputeHash(encoding.GetBytes(content2));

			string shash1 = getString(hash1);
			string shash2 = getString(hash2);
			m_Result = shash1 == shash2;
			m_message = null;

			m_log.paragraph(makeNameTag() + "hashOf = " + shash1 + " hashTo = " + shash2);
			if (m_message != null) m_log.paragraph(makeNameTag() + "Message is not null");
			Finished = true; // tell do-once it's done

			if ((m_onPass == "assert" && m_Result == true)
				|| (m_onFail == "assert" && m_Result == false))
			{
				if (m_message != null)
					m_log.fail(makeNameTag() + m_message.Read());
				else
					m_log.fail(makeNameTag() + "File-Comp Assert: Result = '" + m_Result + "', on-pass='"
						+ m_onPass + "', on-fail='" + m_onFail + "', hashOf='"
						+ shash1 + "', hashTo='" + shash2 + "'");
			}
			m_log.result(this);
		}

		public string cutMatch(Match m)
		// Replace each Regex match with nothing - cut it out.
		{ return null; }

		/// <summary>
		/// Gets the image of this instruction's data.
		/// </summary>
		/// <param name="name">Name of the data to retrieve.</param>
		/// <returns>Returns the value of the specified data item.</returns>
		public override string GetDataImage(string name)
		{
			if (name == null) name = "result";
			switch (name)
			{
			case "of": return m_of;
			case "to": return m_to;
			case "baseStr": return m_baseStr;
			case "targetStr": return m_targetStr;
			default: return base.GetDataImage(name);
			}
		}

		/// <summary>
		/// Echos an image of the instruction with its attributes
		/// and possibly more for diagnostic purposes.
		/// Over-riding methods should pre-pend this base result to their own.
		/// </summary>
		/// <returns>An image of this instruction.</returns>
		public override string image()
		{
			string image = base.image();
			if (m_of != null) image += @" of=""" + Utilities.attrText(m_of) + @"""";
			if (m_to != null) image += @" to=""" + Utilities.attrText(m_to) + @"""";
			return image;
		}

		/// <summary>
		/// Returns attributes showing results of the instruction for the Logger.
		/// </summary>
		/// <returns>Result attributes.</returns>
		public override string resultImage()
		{
			string image = base.resultImage();
			if (m_baseStr != null) image += @" base=""" + Utilities.attrText(m_baseStr) + @"""";
			if (m_targetStr != null) image += @" target=""" + Utilities.attrText(m_targetStr) + @"""";
			return image;
		}

		/// <summary>
		/// Convert a byte array to a string of hex digits
		/// </summary>
		/// <param name="input">The byte array to be transformed</param>
		/// <returns>String of hex digits</returns>
		private string getString(byte [] data)
		{
			// Create a new Stringbuilder to collect the bytes
			// and create a string.
			StringBuilder sBuilder = new StringBuilder();

			// Loop through each byte of the hashed data
			// and format each one as a hexadecimal string.
			for (int i = 0; i < data.Length; i++)
			{
				sBuilder.Append(data[i].ToString("x2"));
			}

			// Return the hexadecimal string.
			return sBuilder.ToString();
		}


	}
}
