using System;
using System.IO;
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;

namespace FwBuildTasks
{
	/// <summary>
	/// Forcibly delete one or more files or directories.
	/// </summary>
	public class ForceDelete : Task
	{
		public ForceDelete()
		{
		}

		/// <summary>
		/// Get or set the files to delete.  If a "file" is actually a directory, then
		/// the entire folder is deleted.
		/// </summary>
		[Required]
		public ITaskItem[] Files { get; set; }

		public override bool Execute()
		{
			foreach (var item in Files)
			{
				if (File.Exists(item.ItemSpec))
				{
					try
					{
						// Ensure that the file is writeable.
						File.SetAttributes(item.ItemSpec, FileAttributes.Normal);
						File.Delete(item.ItemSpec);
					}
					catch (Exception ex)
					{
						Log.LogWarningFromException(ex);
						continue;
					}
				}
				else if (Directory.Exists(item.ItemSpec))
				{
					try
					{
						// Ensure that all the files in the directory tree are writeable.
						var filelist = Directory.GetFiles(item.ItemSpec, "*", SearchOption.AllDirectories);
						foreach (var file in filelist)
						{
							// Symbolic links are not handled nicely by File.SetAttributes().
							try { File.SetAttributes(file, FileAttributes.Normal); } catch {}
						}
						Directory.Delete(item.ItemSpec, true);
					}
					catch (Exception ex)
					{
						Log.LogWarningFromException(ex);
					}
				}
			}
			return true;
		}
	}
}
