//
// NAntContrib
// Copyright (C) 2002 Tomas Restrepo (tomasr@mvps.org)
//
// This library is free software; you can redistribute it and/or
// modify it under the terms of the GNU Lesser General Public
// License as published by the Free Software Foundation; either
// version 2.1 of the License, or (at your option) any later version.
//
// This library is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
// Lesser General Public License for more details.
//
// You should have received a copy of the GNU Lesser General Public
// License along with this library; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place, Suite 330, Boston, MA  02111-1307 USA
//
// Originally from NAntContrib

using System;
using System.IO;
using NAnt.Core;
using NAnt.Core.Attributes;
using NAnt.Core.Types;

namespace NAnt.Contrib.Tasks
{

	/// <summary>
	/// A task that concatenates a set of files.
	/// Loosely based on Ant's Concat task.
	/// </summary>
	/// <remarks>
	/// This task takes a set of input files in a fileset
	/// and concatenates them into a single file. You can
	/// either replace the output file, or append to it
	/// by using the append attribute.
	///
	/// The order the files are concatenated in is not
	/// especified.
	/// </remarks>
	/// <example>
	///   <code><![CDATA[
	///   <concat destfile="${outputdir}/Full.txt">
	///      <fileset>
	///         <includes name="${outputdir}/Test-*.txt" />
	///      </fileset>
	///   </concat>
	///
	///   <concat destfile="${outputdir}/Full.txt" append="true" file="${outputdir}/Test1.txt"/>
	///   ]]></code>
	/// </example>
	[TaskName("concatex")]
	public class ConcatExTask : Task
	{
		#region ConcatExWriter
		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Specialized writer that allows setting the newline character
		/// </summary>
		/// ------------------------------------------------------------------------------------
		private class ConcatExWriter: StreamWriter
		{
			/// --------------------------------------------------------------------------------
			/// <summary>
			/// Initializes a new instance of the <see cref="ConcatExWriter"/> class.
			/// </summary>
			/// <param name="stream">The stream.</param>
			/// <param name="fUseUnixNewline"><c>true</c> to use unix line endings (LF),
			/// <c>false</c> to use system line ending.</param>
			/// --------------------------------------------------------------------------------
			public ConcatExWriter(Stream stream, bool fUseUnixNewline): base(stream)
			{
				if (fUseUnixNewline)
					CoreNewLine = new[] { '\n' };
			}
		}
		#endregion

		/// <summary>
		/// Name of Destination file. If this is not specified, the output goes to the console.
		/// </summary>
		[TaskAttribute("destfile")]
		public string Destination { get; set; }

		/// <summary>
		/// Whether to append to the destination file (true),
		/// or replace it (false). Default is false.
		/// </summary>
		[TaskAttribute("append")]
		[BooleanValidator]
		public bool Append { get; set; }

		/// <summary>
		/// Whether to display all lines (false) or display same consecutive lines
		/// only once and the number of occurences in a second line. Default is false.
		/// </summary>
		[TaskAttribute("smartlines")]
		[BooleanValidator]
		public bool SmartLines { get; set; }

		/// <summary>
		/// Whether to remove the leading whitespace from the file. Default is false.
		/// </summary>
		[TaskAttribute("removeleadingwhitespace")]
		[BooleanValidator]
		public bool RemoveLeadingWhitespace { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Gets or sets a value indicating whether to use just LF as new line character, or
		/// leave as is.
		/// </summary>
		/// <remarks>It would be better to implement this using a filter. However, this approach
		/// seemed to be the faster one for now.</remarks>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("useunixnewline")]
		[BooleanValidator]
		public bool UseUnixNewLine { get; set;  }

		/// <summary>
		/// Set of files to use as input
		/// </summary>
		[BuildElement("fileset")]
		public FileSet FileSet { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Single file as input
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("file")]
		public string Filename { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Force concatenating even if not out of date
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("force")]
		[BooleanValidator]
		public bool Force { get; set; }

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Initializes a new instance of the <see cref="ConcatExTask"/> class.
		/// </summary>
		/// ------------------------------------------------------------------------------------
		public ConcatExTask()
		{
			FileSet = new FileSet();
		}

		///<summary>
		///Initializes task and ensures the supplied attributes are valid.
		///</summary>
		protected override void Initialize()
		{
			if ((FileSet.FileNames == null || FileSet.FileNames.Count == 0) &&
				string.IsNullOrEmpty(Filename))
			{
				if (FileSet.FailOnEmpty)
					throw new BuildException("No input file specified.", Location);

				// Otherwise just return
				return;
			}

			base.Initialize();
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// <summary>Gets the complete output path.</summary>
		/// </summary>
		/// <returns></returns>
		/// ------------------------------------------------------------------------------------
		protected string GetOutputPath()
		{
			return Path.GetFullPath(Path.Combine(Project.BaseDirectory, Destination));
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Determines if we have to compile
		/// </summary>
		/// <returns><c>true</c> if compilation is necessary, otherwise <c>false</c>.</returns>
		/// ------------------------------------------------------------------------------------
		protected virtual bool NeedsCompiling()
		{
			// If we don't have a fileset, we add the filename to the fileset, so that we
			// always can deal with a fileset
			if (FileSet.FileNames.Count <= 0)
				FileSet.FileNames.Add(Filename);
			else
				Filename = null;

			if (Force)
				return true;

			if (Destination == null)
				return true;

			// Does output file exist
			var outputFileInfo = new FileInfo(GetOutputPath());
			if (!outputFileInfo.Exists)
				return true;

			// Files Updated?
			string fileName = FileSet.FindMoreRecentLastWriteTime(FileSet.FileNames,
				outputFileInfo.LastWriteTime);
			if (fileName != null)
			{
				Log(Level.Verbose, "{0} is out of date, recompiling.", fileName);
				return true;
			}

			return false;
		}

		/// <summary>
		/// This is where the work is done
		/// </summary>
		protected override void ExecuteTask()
		{
			if ((FileSet.FileNames == null || FileSet.FileNames.Count == 0) &&
				string.IsNullOrEmpty(Filename))
				return;

			if (!NeedsCompiling())
				return;

			if (Destination != null)
			{
				if (Filename == null)
					Log(Level.Info, "Creating {0}", GetOutputPath());
				else
					Log(Level.Info, "Adding {0} to {1}", Path.GetFileName(Filename),
						GetOutputPath());
			}

			Stream output = null;
			try
			{
				output = (Destination == null) ? Console.OpenStandardOutput() : OpenDestinationFile();
				AppendFiles(output);
			}
			finally
			{
				if (output != null)
					output.Close();
			}
		}


		/// <summary>
		/// Opens the destination file according
		/// to the specified flags
		/// </summary>
		/// <returns></returns>
		private Stream OpenDestinationFile()
		{
			var mode = Append ? FileMode.Append : FileMode.Create;
			try
			{
				return File.Open(GetOutputPath(), mode);
			}
			catch ( IOException e )
			{
				string msg = string.Format("File {0} could not be opened", GetOutputPath());
				throw new BuildException(msg, e);
			}
		}

		/// <summary>
		/// Appends all specified files
		/// </summary>
		/// <param name="output">File to write to</param>
		private void AppendFiles(Stream output)
		{
			using (var writer = new ConcatExWriter(output, UseUnixNewLine))
			{
				foreach (var file in FileSet.FileNames)
				{
					FileStream input;
					try
					{
						Log(Level.Verbose, "Adding file {0}", file);
						input = File.OpenRead(file);
					}
					catch (IOException e)
					{
						Log(Level.Info, "Concat: File {0} could not be read: {1}", file, e.Message);
						continue;
					}


					StreamReader reader = null;
					try
					{
						reader = new StreamReader(input);
						var line = reader.ReadLine();
						if (RemoveLeadingWhitespace && line != null)
						{
							while (line != null && line.Trim() == string.Empty)
							{
								// Eat line.
								line = reader.ReadLine();
							}
						}
						string lastLine = null;
						var nCount = 0;
						while (line != null)
						{
							if (SmartLines)
							{
								// compress lines. We output all blank lines, for others
								// we output only the first of multiple consecutive lines
								// and then a message that tells the number of times this line appeared.
								if (lastLine != line || line.Trim() == string.Empty)
								{
									if (lastLine != null)
									{
										if (nCount > 1)
											writer.WriteLine("(Previous line appeared {0} times)", nCount);
									}
									nCount = 1;
									lastLine = line;
									writer.WriteLine(line);
								}
								else
									nCount++;
							}
							else // no line compression
								writer.WriteLine(line);
							line = reader.ReadLine();
						}
						if (SmartLines && nCount > 1)
						{
							// there are some left-overs
							if (lastLine != null)
							{
								if (nCount > 1)
									writer.WriteLine("(Previous line appeared {0} times)", nCount);
							}
						}
					}
					catch (IOException e)
					{
						throw new BuildException("Concat: Could not read or write from file", e);
					}
					finally
					{
						if (reader != null)
							reader.Close();
						input.Close();
					}
				}
			}
		}

	} // class ConcatTask

} // namespace NAnt.Contrib.Tasks
