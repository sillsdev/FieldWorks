using SIL.FieldWorks.FDO;

namespace GenerateHCConfig
{
	internal class NullFdoDirectories : IFdoDirectories
	{
		public string ProjectsDirectory
		{
			get { return null; }
		}

		public string DefaultProjectsDirectory
		{
			get { return null; }
		}

		public string TemplateDirectory
		{
			get { return null; }
		}
	}
}
