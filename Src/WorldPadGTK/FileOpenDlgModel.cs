// FileOpenDlgModel.cs
// User: Jean-Marc Giffin at 11:53 A 14/07/2008
// JMG: UNNECESSARY TO COMMENT

using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	/// <summary>
	/// Probably not used for anything real currently.
	/// </summary>
	public class FileOpenModel : IDialogModel
	{
		private string filepath_;
		private bool isFilepathSet_;

		public FileOpenModel()
		{
			filepath_ = null;
			isFilepathSet_ = false;
		}

		public string Filepath
		{
			get
			{
				return filepath_;
			}

			set
			{
				if (value != null)
					isFilepathSet_ = true;
				filepath_ = value;
			}
		}

		public bool IsFilepathSet()
		{
			return isFilepathSet_;
		}
	}
}
