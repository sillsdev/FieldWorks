using SIL.LCModel;

namespace SIL.GenerateHCConfigForFLExTrans
{
	internal class NullFdoDirectories : ILcmDirectories
	{
		public string ProjectsDirectory => null;

		public string TemplateDirectory => null;
	}
}
