using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
namespace FwBuildTasks
{
	/// <summary>
	/// Check whether the two files are identical.  If not, optionally write an error message.
	/// </summary>
	public class VerifyFilesIdentical : Task
	{
		[Required]
		public string FirstFile { get; set; }

		[Required]
		public string SecondFile { get; set; }

		public string ErrorMessage { get; set; }

		[Output]
		public bool Result { get; set; }

		public override bool Execute()
		{
			try
			{
				Log.LogMessage(MessageImportance.Low, "Comparing {0} with {1}", FirstFile, SecondFile);
				if (!File.Exists(FirstFile))
				{
					Log.LogMessage(MessageImportance.Normal, "File {0} doesn't exist.", FirstFile);
					Result = false;
					return false;
				}
				if (!File.Exists(SecondFile))
				{
					Log.LogMessage(MessageImportance.Normal, "File {0} doesn't exist.", SecondFile);
					Result = false;
					return false;
				}
				string firstContent;
				string secondContent;
				using (var reader = new StreamReader(FirstFile))
				{
					firstContent = reader.ReadToEnd();
				}
				using (var reader = new StreamReader(SecondFile))
				{
					secondContent = reader.ReadToEnd();
				}
				if (firstContent == secondContent)
				{
					Log.LogMessage(MessageImportance.Low, "Files {0} and {1} are identical", FirstFile,
						SecondFile);
					Result = true;
				}
				else
				{
					Log.LogMessage(MessageImportance.Normal, "Files {0} and {1} are different", FirstFile,
						SecondFile);
					Result = false;
				}
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException(ex);
			}
			finally
			{
				if (!Result && !String.IsNullOrEmpty(ErrorMessage) && !BuildEngine.ContinueOnError)
					Log.LogError(ErrorMessage);
			}
			return !Log.HasLoggedErrors;
		}
	}
}
