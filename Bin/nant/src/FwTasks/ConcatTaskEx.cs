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
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using NAnt.Core.Attributes;
using NAnt.Core;
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
		private string _destination;
		private bool _append = false;
		private FileSet _fileset = new FileSet();
		private string _filename;
		private bool _force = false;
		private bool m_fSmartLines = false;
		private bool m_removeleadingwhitespace = false;

		/// <summary>
		/// Name of Destination file. If this is not specified, the output goes to the console.
		/// </summary>
		[TaskAttribute("destfile")]
		public string Destination
		{
			get { return _destination; }
			set { _destination = value; }
		}

		/// <summary>
		/// Whether to append to the destination file (true),
		/// or replace it (false). Default is false.
		/// </summary>
		[TaskAttribute("append"), BooleanValidator()]
		public bool Append
		{
			get { return _append; }
			set { _append = value; }
		}

		/// <summary>
		/// Whether to display all lines (false) or display same consecutive lines
		/// only once and the number of occurences in a second line. Default is false.
		/// </summary>
		[TaskAttribute("smartlines")]
		[BooleanValidator]
		public bool SmartLines
		{
			get { return m_fSmartLines; }
			set { m_fSmartLines = value; }
		}

		/// <summary>
		/// Whether to remove the leading whitespace from the file. Default is false.
		/// </summary>
		[TaskAttribute("removeleadingwhitespace")]
		[BooleanValidator]
		public bool RemoveLeadingWhitespace
		{
			get { return m_removeleadingwhitespace; }
			set { m_removeleadingwhitespace = value; }
		}

		/// <summary>
		/// Set of files to use as input
		/// </summary>
		[BuildElement("fileset")]
		public FileSet FileSet
		{
			get { return _fileset; }
			set { _fileset = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Single file as input
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("file")]
		public string Filename
		{
			get { return _filename; }
			set { _filename = value; }
		}

		/// ------------------------------------------------------------------------------------
		/// <summary>
		/// Force concatenating even if not out of date
		/// </summary>
		/// ------------------------------------------------------------------------------------
		[TaskAttribute("force"), BooleanValidator()]
		public bool Force
		{
			get { return _force; }
			set { _force = value; }
		}

		///<summary>
		///Initializes task and ensures the supplied attributes are valid.
		///</summary>
		///<param name="taskNode">Xml node used to define this task instance.</param>
		protected override void InitializeTask(System.Xml.XmlNode taskNode)
		{
			if (FileSet.FileNames.Count == 0 && Filename.Length == 0)
			{
				throw new BuildException("No input file specified.", Location);
			}

			base.InitializeTask(taskNode);
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
			if (_fileset.FileNames.Count <= 0)
				_fileset.FileNames.Add(Filename);
			else
				Filename = null;

			if (Force)
				return true;

			if (Destination == null)
				return true;

			// Does output file exist
			FileInfo outputFileInfo = new FileInfo(GetOutputPath());
			if (!outputFileInfo.Exists)
				return true;

			// Files Updated?
			string fileName = FileSet.FindMoreRecentLastWriteTime(_fileset.FileNames,
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
			if (NeedsCompiling())
			{
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
					if (Destination == null)
						output = Console.OpenStandardOutput();
					else
						output = OpenDestinationFile();
					AppendFiles(output);
				}
				finally
				{
					if (output != null)
						output.Close();
				}
			}
		}


		/// <summary>
		/// Opens the destination file according
		/// to the specified flags
		/// </summary>
		/// <returns></returns>
		private Stream OpenDestinationFile()
		{
			FileMode mode;
			if ( _append )
			{
				mode = FileMode.Append | FileMode.OpenOrCreate;
			}
			else
			{
				mode = FileMode.Create;
			}
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
			foreach ( string file in FileSet.FileNames )
			{
				FileStream input = null;
				try
				{
					Log(Level.Verbose, "Adding file {0}", file);
					input = File.OpenRead(file);
				}
				catch ( IOException e )
				{
					Log(Level.Info, "Concat: File {0} could not be read: {1}", file, e.Message);
					continue;
				}

				StreamReader reader = null;
				StreamWriter writer = null;
				try
				{
					reader = new StreamReader(input);
					writer = new StreamWriter(output);
					string line = reader.ReadLine();
					if (m_removeleadingwhitespace && line != null)
					{
						while (line != null && line.Trim() == string.Empty)
						{
							// Eat line.
							line = reader.ReadLine();
						}
					}
					string lastLine = null;
					int nCount = 0;
					while (line != null)
					{
						if (m_fSmartLines)
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
					if (m_fSmartLines && nCount > 1)
					{ // there are some left-overs
						if (lastLine != null)
						{
							if (nCount > 1)
								writer.WriteLine("(Previous line appeared {0} times)", nCount);
						}
					}
				}
				catch ( IOException e )
				{
					throw new BuildException("Concat: Could not read or write from file", e);
				}
				finally
				{
					if (reader != null)
						reader.Close();
					if (writer != null)
						writer.Close();
					input.Close();
				}
			}
		}

	} // class ConcatTask

} // namespace NAnt.Contrib.Tasks
