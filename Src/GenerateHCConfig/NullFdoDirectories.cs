using SIL.LCModel;

namespace GenerateHCConfig
{
	internal class NullFdoDirectories : ILcmDirectories
	{
		public string ProjectsDirectory => null;

		public string TemplateDirectory => null;
	}
}
