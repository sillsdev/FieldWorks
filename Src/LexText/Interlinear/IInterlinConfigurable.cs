using SIL.FieldWorks.Common.ViewsInterfaces;
using XCore;

namespace SIL.FieldWorks.IText
{
	public interface IInterlinConfigurable : IInterlinearTabControl
	{
		PropertyTable PropertyTable { get; set; }
		IVwRootBox Rootb { get; set; }
	}
}
