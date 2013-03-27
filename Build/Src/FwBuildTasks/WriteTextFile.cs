using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	/// <summary>
	/// Create a short text file (containing one to five lines).
	/// </summary>
	public class WriteTextFile : Task
	{
		[Required]
		public string FirstLine { get; set; }

		public string SecondLine { get; set; }
		public string ThirdLine { get; set; }
		public string FourthLine { get; set; }
		public string FifthLine { get; set; }

		/// <summary>
		/// The name of the file to be written.
		/// </summary>
		[Required]
		public string TargetFile { get; set; }

		/// <summary>
		/// Specify whether the literal line attributes should be appended to an existing file.
		/// </summary>
		/// <value>
		/// <c>true</c> if the lines should be appended; otherwise, <c>false</c>.
		/// </value>
		public bool Append { get; set; }

		public override bool Execute()
		{
			using (StreamWriter writer = new StreamWriter(TargetFile, Append))
			{
				writer.WriteLine(FirstLine);
				if (!String.IsNullOrEmpty(SecondLine))
					writer.WriteLine(SecondLine);
				if (!String.IsNullOrEmpty(ThirdLine))
					writer.WriteLine(ThirdLine);
				if (!String.IsNullOrEmpty(FourthLine))
					writer.WriteLine(FourthLine);
				if (!String.IsNullOrEmpty(FifthLine))
					writer.WriteLine(FifthLine);
				writer.Flush();
				writer.Close();
			}
			return true;
		}
	}

	public class CatenateFiles : Task
	{
		[Required]
		public ITaskItem[] SourceFiles { get; set; }

		[Required]
		public string TargetFile { get; set; }

		public bool UseUnixNewlines { get; set; }

		public override bool Execute()
		{
			using (StreamWriter writer = new StreamWriter(TargetFile))
			{
				foreach (var item in SourceFiles)
				{
					using (StreamReader reader = new StreamReader(item.ItemSpec))
					{
						while (!reader.EndOfStream)
						{
							var line = reader.ReadLine();
							if (!UseUnixNewlines || Environment.OSVersion.Platform == PlatformID.Unix)
								writer.WriteLine(line);
							else
								writer.Write(line + "\n");
						}
						reader.Close();
					}
					writer.Flush();
				}
				writer.Close();
			}
			return true;
		}
	}
}
