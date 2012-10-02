//-------------------------------------------------------------------------------------------------
// <copyright file="tallwreg.cs" company="Microsoft">
//    Copyright (c) Microsoft Corporation.  All rights reserved.
//
//    The use and distribution terms for this software are covered by the
//    Common Public License 1.0 (http://opensource.org/licenses/cpl.php)
//    which can be found in the file CPL.TXT at the root of this distribution.
//    By using this software in any fashion, you are agreeing to be bound by
//    the terms of this license.
//
//    You must not remove this notice, or any other, from this software.
// </copyright>
//
// <summary>
// Utilities for tallow to use while processing registry (.reg) files.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Tools
{
	using System;
	using System.CodeDom.Compiler;
	using System.Collections;
	using System.Collections.Specialized;
	using System.Globalization;
	using System.IO;
	using System.Text;
	using System.Xml;

	/// <summary>
	/// Utilities for tallow to use while processing resource (.rc) files.
	/// </summary>
	public class TallowRegProcessing
	{
		private int currentLineNumber = 0;
		private int regId = 1;

		/// <summary>
		/// Registry value types.
		/// </summary>
		private enum ValueType
		{
			/// <summary>
			/// Binary registry value type.
			/// </summary>
			Binary,
			/// <summary>
			/// DWORD registry value type.
			/// </summary>
			DWORD,
			/// <summary>
			/// Expandable string registry value type.
			/// </summary>
			ExpandableString,
			/// <summary>
			/// MultiString registry value type.
			/// </summary>
			MultiString,
			/// <summary>
			/// String registry value type.
			/// </summary>
			String
		}

		/// <summary>
		/// Registry root.
		/// </summary>
		private enum RegRoot
		{
			/// <summary>
			/// HKEY_CLASSES_ROOT
			/// </summary>
			HKCR,
			/// <summary>
			/// HKEY_CURRENT_USER
			/// </summary>
			HKCU,
			/// <summary>
			/// HKEY_LOCAL_MACHINE
			/// </summary>
			HKLM,
			/// <summary>
			/// HKEY_USERS
			/// </summary>
			HKU
		}

		/// <summary>
		/// Processes a .reg file to the writer. This will copy the input file into a
		/// temp directory and add empty lines before each key declaration. That
		/// processed file is what gets used as input to the WiX translation code.
		/// </summary>
		/// <param name="writer">Writer to output to.</param>
		/// <param name="path">Path to registry file.</param>
		public static void ProcessRegistryFile(XmlWriter writer, string path)
		{
			FileInfo file = new FileInfo(path);
			if (!file.Exists)
			{
				throw new WixFileNotFoundException(path, "File");
			}

			TempFileCollection tempFiles = new TempFileCollection();

			StreamReader reader = new StreamReader(path);
			FileInfo tempFile = new FileInfo(Path.Combine(tempFiles.BasePath, file.Name));
			tempFile.Directory.Create();
			StreamWriter tempWriter = new StreamWriter(Path.Combine(tempFiles.BasePath, file.Name));

			string line;
			while (null != (line = reader.ReadLine()))
			{
				if (line.StartsWith("["))
				{
					tempWriter.WriteLine();
					tempWriter.WriteLine(line);
				}
				else
				{
					tempWriter.WriteLine(line);
				}
			}
			tempWriter.Close();
			reader.Close();

			TallowRegProcessing regProc = new TallowRegProcessing();
			regProc.ConvertRegFile(tempFile, writer);
		}

		/// <summary>
		/// Convert a single registry file into WiX source code.
		/// </summary>
		/// <param name="tempFile">Preprocessed .reg file.</param>
		/// <param name="writer">XmlWriter to write registry information to.</param>
		private void ConvertRegFile(FileInfo tempFile, XmlWriter writer)
		{
			using (StreamReader sr = tempFile.OpenText())
			{
				string line;
				this.currentLineNumber = 0;

				while (null != (line = this.GetNextLine(sr)))
				{
					if (line.StartsWith(@"[HKEY_CLASSES_ROOT\"))
					{
						this.ConvertRoot(sr, writer, RegRoot.HKCR, line.Substring(19, line.Length - 20));
					}
					else if (line.StartsWith(@"[HKEY_CURRENT_USER\"))
					{
						this.ConvertRoot(sr, writer, RegRoot.HKCU, line.Substring(19, line.Length - 20));
					}
					else if (line.StartsWith(@"[HKEY_LOCAL_MACHINE\"))
					{
						this.ConvertRoot(sr, writer, RegRoot.HKLM, line.Substring(20, line.Length - 21));
					}
					else if (line.StartsWith(@"[HKEY_USERS\"))
					{
						this.ConvertRoot(sr, writer, RegRoot.HKU, line.Substring(12, line.Length - 13));
					}
					else if (!line.StartsWith("Windows Registry Editor") && 0 != line.Length)
					{
						//throw new ApplicationException(String.Format("Unrecognized line: {0}", line));
					}
				}
			}
		}

		/// <summary>
		/// Convert a root key into WiX.
		/// </summary>
		/// <param name="sr">Reader for the reg file.</param>
		/// <param name="writer">XmlWriter to write registry information to.</param>
		/// <param name="root">Root registry key.</param>
		/// <param name="line">Current line to parse.</param>
		private void ConvertRoot(StreamReader sr, XmlWriter writer, RegRoot root, string line)
		{
			writer.WriteStartElement("Registry");
			writer.WriteAttributeString("Root", root.ToString());
			writer.WriteAttributeString("Key", line);

			this.regId++; // increment the registry key counter
			this.ConvertValues(sr, writer);

			writer.WriteEndElement();
		}

		/// <summary>
		/// Convert registry values to WiX
		/// </summary>
		/// <param name="sr">Reader for the reg file.</param>
		/// <param name="writer">XmlWriter to write registry information to.</param>
		private void ConvertValues(StreamReader sr, XmlWriter writer)
		{
			string name = null;
			string value = null;
			ValueType type;

			while (this.GetValue(sr, ref name, ref value, out type))
			{
				ArrayList charArray;
				writer.WriteStartElement("Registry");

				if (null != name && 0 != name.Length)
				{
					writer.WriteAttributeString("Name", name);
				}

				switch (type)
				{
					case ValueType.Binary:
						writer.WriteAttributeString("Value", value.Replace(",", "").ToUpper());
						writer.WriteAttributeString("Type", "binary");
						break;
					case ValueType.DWORD:
						writer.WriteAttributeString("Value", Int32.Parse(value, NumberStyles.HexNumber).ToString());
						writer.WriteAttributeString("Type", "integer");
						break;
					case ValueType.ExpandableString:
						charArray = this.ConvertUnicodeCharList(value);
						value = "";

						// create the string, remove the terminating null
						for (int i = 0; i < charArray.Count; i++)
						{
							if ('\0' != (char)charArray[i])
							{
								value += charArray[i];
							}
						}

						writer.WriteAttributeString("Value", value);
						writer.WriteAttributeString("Type", "expandable");
						break;
					case ValueType.MultiString:
						charArray = this.ConvertUnicodeCharList(value);
						value = "";

						// create the string, replace the nulls with [~]
						for (int i = 0; i < charArray.Count; i++)
						{
							if ('\0' == (char)charArray[i])
							{
								value += "[~]";
							}
							else
							{
								value += charArray[i];
							}
						}

						writer.WriteAttributeString("Value", value);
						writer.WriteAttributeString("Type", "multiString");
						break;
					case ValueType.String:
						writer.WriteAttributeString("Value", value);
						writer.WriteAttributeString("Type", "string");
						break;
					default:
						throw new ApplicationException(String.Format("Did not recognize the type of reg value on line {0}", this.currentLineNumber));
						//break;
				}

				writer.WriteEndElement();
			}
		}

		/// <summary>
		/// Parse a value from a line.
		/// </summary>
		/// <param name="sr">Reader for the reg file.</param>
		/// <param name="name">Name of the value.</param>
		/// <param name="value">Value of the value.</param>
		/// <param name="type">Type of the value.</param>
		/// <returns>true if the value can be parsed, false otherwise.</returns>
		private bool GetValue(StreamReader sr, ref string name, ref string value, out ValueType type)
		{
			string line = this.GetNextLine(sr);

			if (null == line || 0 == line.Length)
			{
				type = 0;
				return false;
			}

			string[] parts = line.Trim().Split("=".ToCharArray(), 2);

			if (2 != parts.Length)
			{
				throw new ApplicationException(String.Format("Cannot parse value: {0} at line {1}.", line, this.currentLineNumber));
			}

			if ("@" == parts[0])
			{
				name = null;
			}
			else
			{
				name = parts[0].Substring(1, parts[0].Length - 2);
			}

			if (parts[1].StartsWith("hex:")) // binary
			{
				value = parts[1].Substring(4);
				type = ValueType.Binary;
			}
			else if (parts[1].StartsWith("dword:")) //dword
			{
				value = parts[1].Substring(6);
				type = ValueType.DWORD;
			}
			else if (parts[1].StartsWith("hex(2):")) // expandable string
			{
				value = parts[1].Substring(7);
				type = ValueType.ExpandableString;
			}
			else if (parts[1].StartsWith("hex(7):")) // multi-string
			{
				value = parts[1].Substring(7);
				type = ValueType.MultiString;
			}
			else // string
			{
				value = parts[1].Substring(1, parts[1].Length - 2);
				type = ValueType.String;
			}

			return true;
		}

		/// <summary>
		/// Get the next line from the reg file input stream.
		/// </summary>
		/// <param name="sr">Reader for the reg file.</param>
		/// <returns>The next line.</returns>
		private string GetNextLine(StreamReader sr)
		{
			string line;
			string totalLine = null;

			while (null != (line = sr.ReadLine()))
			{
				bool stop = true;

				this.currentLineNumber++;
				line = line.Trim();

				if (line.EndsWith("\\"))
				{
					stop = false;
					line = line.Substring(0, line.Length - 1);
				}

				if (null == totalLine) // first line
				{
					totalLine = line;
				}
				else // other lines
				{
					totalLine += line;
				}

				// break if there is no more info for this line
				if (stop) break;
			}
			//Console.WriteLine("Line: " + totalLine);

			return totalLine;
		}

		/// <summary>
		/// Convert a Unicode character list into the proper WiX format.
		/// </summary>
		/// <param name="charList">List of Unicode characters.</param>
		/// <returns>Array of Unicode characters.</returns>
		private ArrayList ConvertUnicodeCharList(string charList)
		{
			string[] strChars = charList.Split(",".ToCharArray());

			if (0 != strChars.Length % 2)
			{
				throw new ApplicationException("Problem parsing Expandable string data, its probably not Unicode.");
			}

			ArrayList charArray = new ArrayList();
			for (int i = 0; i < strChars.Length; i += 2)
			{
				string chars = strChars[i + 1] + strChars[i];
				int unicodeInt = Int32.Parse(chars, NumberStyles.HexNumber);
				char unicodeChar = (char)unicodeInt;
				charArray.Add(unicodeChar);
			}

			return charArray;
		}
	}
}
