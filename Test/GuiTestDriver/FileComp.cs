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
using System.IO;
//using System.Collections;
using System.Security.Cryptography;
using System.Text;
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
			m_baseStr = Utilities.evalExpr(m_of);
			m_targetStr = Utilities.evalExpr(m_to);
			// m_baseStr and m_targetStr can be null
			MD5 md5Hasher = MD5.Create();
			byte[] BytesRead1 = null;
			byte[] BytesRead2 = null;
			try
			{
				BytesRead1 = System.IO.File.ReadAllBytes(m_baseStr);
				BytesRead2 = System.IO.File.ReadAllBytes(m_targetStr);
			}
			catch (FileNotFoundException e) { fail(e.FileName + " not found");}
			catch (DirectoryNotFoundException e) {fail(e.ToString() + " not found");}

			byte[] hash1 = md5Hasher.ComputeHash(BytesRead1);
			byte[] hash2 = md5Hasher.ComputeHash(BytesRead2);

			string shash1 = getString(hash1);
			string shash2 = getString(hash2);
			m_Result = shash1 == shash2;
			m_message = null;

			m_log.paragraph("hashOf = " + shash1 + " hashTo = " + shash2);
			if (m_message != null) m_log.paragraph("Message is not null");
			Finished = true; // tell do-once it's done

			if ((m_onPass == "assert" && m_Result == true)
				|| (m_onFail == "assert" && m_Result == false))
			{
				if (m_message != null)
					fail(m_message.Read());
				else
					fail("File-Comp Assert: Result = '" + m_Result + "', on-pass='"
						+ m_onPass + "', on-fail='" + m_onFail + "', hashOf='"
						+ shash1 + "', hashTo='" + shash2 + "'");
			}
			m_log.result(this);
		}

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
