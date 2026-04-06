using SIL.LCModel;

namespace SIL.DisambiguateInFLExDBTests
{
	public class NullFdoDirectories : ILcmDirectories
	{
		public string ProjectsDirectory => null;

		public string TemplateDirectory => null;
	}
}
