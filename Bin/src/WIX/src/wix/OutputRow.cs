//-------------------------------------------------------------------------------------------------
// <copyright file="OutputRow.cs" company="Microsoft">
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
// Wrapper around a table row used for output.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
	using System;
	using System.Diagnostics;
	using System.Globalization;
	using System.Text;
	using System.Text.RegularExpressions;
	using System.Xml;

	/// <summary>
	/// OutputRow wraps a Row for output.
	/// </summary>
	public class OutputRow
	{
		private Row row;

		/// <summary>
		/// Creates a new output row.
		/// </summary>
		/// <param name="row">Table row to wrap.</param>
		public OutputRow(Row row) : this(row, row.SectionId)
		{
		}

		/// <summary>
		/// Creates a new output row.
		/// </summary>
		/// <param name="row">Table row to wrap.</param>
		/// <param name="sectionId">If not null, the sectionId attribute will be set on this row in the output.</param>
		public OutputRow(Row row, string sectionId)
		{
			if (null == row)
			{
				throw new ArgumentNullException("row");
			}

			this.row = row;
			this.row.SectionId = sectionId;
		}

		/// <summary>
		/// Gets the table row for this output row.
		/// </summary>
		/// <value>Table row for this output row.</value>
		public Row Row
		{
			get { return this.row; }
		}

		/// <summary>
		/// Returns the row in a format usable in IDT files.
		/// </summary>
		/// <param name="moduleGuid">String containing the GUID of the Merge Module, if appropriate.</param>
		/// <param name="ignoreModularizations">Optional collection of identifers that should not be modularized.</param>
		/// <remarks>moduleGuid is expected to be null when not being used to compile a Merge Module.</remarks>
		/// <returns>null if OutputRow is unreal, or string with tab delimited field values otherwise.</returns>
		public string ToIdtDefinition(string moduleGuid, IgnoreModularizationCollection ignoreModularizations)
		{
			if (this.row.IsUnreal)
			{
				return null;
			}

			bool first = true;
			StringBuilder sb = new StringBuilder();

			for (int i = 0; i < this.row.Fields.Length; ++i)
			{
				if (this.row.Fields[i].Column.IsUnreal) // skip virtual columns
				{
					continue;
				}

				if (first)
				{
					first = false;
				}
				else
				{
					sb.Append('\t');
				}

				sb.Append(this.GetIdtValue(this.row.Fields[i], moduleGuid, ignoreModularizations));
			}
			sb.Append("\r\n");

			return sb.ToString();
		}

		/// <summary>
		/// Returns the field data in a format usable in IDT files.
		/// </summary>
		/// <param name="field">The field to modularize.</param>
		/// <param name="moduleGuid">String containing the GUID of the Merge Module to append the the field value, if appropriate.</param>
		/// <param name="ignoreModularizations">Optional collection of identifers that should not be modularized.</param>
		/// <remarks>moduleGuid is expected to be null when not being used to compile a Merge Module.</remarks>
		/// <returns>Field data in string IDT format.</returns>
		public string GetIdtValue(Field field, string moduleGuid, IgnoreModularizationCollection ignoreModularizations)
		{
			if (field.Column.IsUnreal)
			{
				return null;
			}
			if (null == field.Data)
			{
				return String.Empty;
			}

			string fieldData = Convert.ToString(field.Data);

			// special idt-specific escaping
			if (field.Column.EscapeIdtCharacters)
			{
				fieldData = fieldData.Replace('\t', '\x10');
				fieldData = fieldData.Replace('\r', '\x11');
				fieldData = fieldData.Replace('\n', '\x19');
			}

			string idtValue;
			if (null != moduleGuid && ColumnModularizeType.None != field.Column.ModularizeType && !Common.IsExcludedFromModularization(fieldData))
			{
				StringBuilder sb;
				int start;
				ColumnModularizeType modularizeType = field.Column.ModularizeType;

				// special logic for the ControlEvent table's Argument column
				// this column requires different modularization methods depending upon the value of the Event column
				if (ColumnModularizeType.ControlEventArgument == field.Column.ModularizeType)
				{
					switch (this.row[2].ToString())
					{
						case "CheckExistingTargetPath": // redirectable property name
						case "CheckTargetPath":
						case "DoAction": // custom action name
						case "NewDialog": // dialog name
						case "SelectionBrowse":
						case "SetTargetPath":
						case "SpawnDialog":
						case "SpawnWaitDialog":
							if (CompilerCore.IsIdentifier(fieldData))
							{
								modularizeType = ColumnModularizeType.Column;
							}
							else
							{
								modularizeType = ColumnModularizeType.Property;
							}
							break;
						default: // formatted
							modularizeType = ColumnModularizeType.Property;
							break;
					}
				}

				switch (modularizeType)
				{
					case ColumnModularizeType.Column:
						// ensure the value is an identifier (otherwise it shouldn't be modularized this way)
						if (!CompilerCore.IsIdentifier(fieldData))
						{
							throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "The value '{0}' is not a legal identifier and therefore cannot be modularized.", fieldData));
						}

						// if we're not supposed to ignore this identifier
						if (null == ignoreModularizations || !ignoreModularizations.ShouldIgnoreModularization(fieldData))
						{
							idtValue = String.Concat(fieldData, ".", moduleGuid);
						}
						else
						{
							idtValue = fieldData;
						}
						break;

					case ColumnModularizeType.Property:
					case ColumnModularizeType.Condition:
						Regex regex;
						if (ColumnModularizeType.Property == modularizeType)
						{
							regex = new Regex(@"\[(?<identifier>[#$!]?[a-zA-Z_][a-zA-Z0-9_\.]*)]", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);
						}
						else
						{
							Debug.Assert(ColumnModularizeType.Condition == modularizeType);

							// This heinous looking regular expression is actually quite an elegant way
							// to shred the entire condition into the identifiers that need to be
							// modularized.  Let's break it down piece by piece:
							//
							// 1. Look for the operators: NOT, EQV, XOR, OR, AND, IMP.  Note that the
							//    regular expression is case insensitive so we don't have to worry about
							//    all the permutations of these strings.
							// 2. Look for quoted strings.  Quoted strings are just text and are ignored
							//    outright.
							// 3. Look for environment variables.  These look like identifiers we might
							//    otherwise be interested in but start with a percent sign.  Like quoted
							//    strings these enviroment variable references are ignored outright.
							// 4. Match all identifiers that are things that need to be modularized.  Note
							//    the special characters (!, $, ?, &) that denote Component and Feature states.
							regex = new Regex(@"NOT|EQV|XOR|OR|AND|IMP|"".*?""|%[a-zA-Z_][a-zA-Z0-9_\.]*|(?<identifier>[!$\?&]?[a-zA-Z_][a-zA-Z0-9_\.]*)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);

							// less performant version of the above with captures showing where everything lives
							// regex = new Regex(@"(?<operator>NOT|EQV|XOR|OR|AND|IMP)|(?<string>"".*?"")|(?<environment>%[a-zA-Z_][a-zA-Z0-9_\.]*)|(?<identifier>[!$\?&]?[a-zA-Z_][a-zA-Z0-9_\.]*)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
						}

						MatchCollection matches = regex.Matches(fieldData);

						sb = new StringBuilder(fieldData);

						// notice how this code walks backward through the list
						// because it modifies the string as we through it
						for (int i = matches.Count - 1; 0 <= i; i--)
						{
							Group group = matches[i].Groups["identifier"];
							if (group.Success)
							{
								string identifier = group.Value;
								if (!Common.IsStandardProperty(identifier) && (null == ignoreModularizations || !ignoreModularizations.ShouldIgnoreModularization(identifier)))
								{
									sb.Insert(group.Index + group.Length, '.');
									sb.Insert(group.Index + group.Length + 1, moduleGuid);
								}
							}
						}

						idtValue = sb.ToString();
						break;

					case ColumnModularizeType.CompanionFile:
						// if we're not supposed to ignore this identifier and the value does not start with
						// a digit, we must have a companion file so modularize it
						if ((null == ignoreModularizations || !ignoreModularizations.ShouldIgnoreModularization(fieldData)) &&
							0 < fieldData.Length && !Char.IsDigit(fieldData, 0))
						{
							idtValue = String.Concat(fieldData, ".", moduleGuid);
						}
						else
						{
							idtValue = fieldData;
						}
						break;

					case ColumnModularizeType.Icon:
						if (null == ignoreModularizations || !ignoreModularizations.ShouldIgnoreModularization(fieldData))
						{
							start = fieldData.LastIndexOf(".");
							if (-1 == start)
							{
								idtValue = String.Concat(fieldData, ".", moduleGuid);
							}
							else
							{
								idtValue = String.Concat(fieldData.Substring(0, start), ".", moduleGuid, fieldData.Substring(start));
							}
						}
						else
						{
							idtValue = fieldData;
						}
						break;

					case ColumnModularizeType.SemicolonDelimited:
						string[] keys = fieldData.Split(";".ToCharArray());
						for (int i = 0; i < keys.Length; ++i)
						{
							keys[i] = String.Concat(keys[i], ".", moduleGuid);
						}
						idtValue = String.Join(";", keys);
						break;

					default:
						idtValue = fieldData;
						break;
				}
			}
			else // no modularization necessary, just use the field data
			{
				idtValue = fieldData;
			}

			return idtValue;
		}
	}
}
