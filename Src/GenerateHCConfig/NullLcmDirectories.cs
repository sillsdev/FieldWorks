using SIL.LCModel;

namespace GenerateHCConfig
{
	internal class NullLcmDirectories : ILcmDirectories
	{
		public string ProjectsDirectory => null;

		public string TemplateDirectory => null;
	}
}
