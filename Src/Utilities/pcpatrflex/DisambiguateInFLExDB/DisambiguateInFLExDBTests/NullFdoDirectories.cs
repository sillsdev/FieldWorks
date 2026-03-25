using SIL.LCModel;

namespace SIL.DisambiguateInFLExDBTests
{
	internal class NullFdoDirectories : ILcmDirectories
	{
		public string ProjectsDirectory => null;

		public string TemplateDirectory => null;
	}
}
