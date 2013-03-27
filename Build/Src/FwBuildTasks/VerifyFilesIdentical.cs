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

		public override bool Execute()
		{
			try
			{
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
					return true;
				if (!String.IsNullOrEmpty(ErrorMessage))
					Log.LogError(ErrorMessage);
			}
			catch (Exception ex)
			{
				Log.LogErrorFromException(ex);
			}
			return false;
		}
	}
}
