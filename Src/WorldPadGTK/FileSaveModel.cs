// FileSaveModel.cs
// User: Jean-Marc Giffin at 11:57 A 14/07/2008

using SIL.FieldWorks.Common.Framework;

namespace SIL.FieldWorks.WorldPad
{
	public class FileSaveModel : IDialogModel {
		private string filepath_;
		private string filename_;
		private bool isFilepathSet_;

		public FileSaveModel()
		{
			filepath_ = null;
			isFilepathSet_ = false;
		}

		public string Filepath
		{
			get {
				return filepath_;
			} set {
				if (value != null)
					isFilepathSet_ = true;
				filepath_ = value;
			}
		}

		public string Filename
		{
			get { return filename_; }
			set { filename_ = value; }
		}

		public bool IsFilepathSet() {
			return isFilepathSet_;
		}
	}
}