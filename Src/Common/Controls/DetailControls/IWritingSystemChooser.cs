using SIL.LCModel.Core.WritingSystems;
using System.Collections.Generic;

namespace SIL.FieldWorks.Common.Framework.DetailControls
{
	public interface IWritingSystemChooser
	{
		IEnumerable<CoreWritingSystemDefinition> GetVisibleWritingSystems();
	}
}