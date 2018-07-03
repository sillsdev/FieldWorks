using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SIL.FieldWorks.Common.ViewsInterfaces;
using SIL.LCModel;
using XCore;

namespace SIL.FieldWorks.IText
{
	public interface IInterlinConfigurable : IInterlinearTabControl
	{
		PropertyTable PropertyTable { get; set; }
		IVwRootBox Rootb { get; set; }
	}
}
