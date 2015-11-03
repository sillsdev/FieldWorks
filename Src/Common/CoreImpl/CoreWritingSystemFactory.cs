using SIL.WritingSystems;

namespace SIL.CoreImpl
{
	internal class CoreWritingSystemFactory : WritingSystemFactory<CoreWritingSystemDefinition>
	{
		protected override CoreWritingSystemDefinition ConstructDefinition()
		{
			return new CoreWritingSystemDefinition();
		}

		protected override CoreWritingSystemDefinition ConstructDefinition(string ietfLanguageTag)
		{
			return new CoreWritingSystemDefinition(ietfLanguageTag);
		}

		protected override CoreWritingSystemDefinition ConstructDefinition(CoreWritingSystemDefinition ws)
		{
			return new CoreWritingSystemDefinition(ws);
		}
	}
}
