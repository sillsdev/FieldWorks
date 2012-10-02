using System;
using System.Reflection;
using BearCanyon;

class TestApp
{
	static int Main( string[] args )
	{
		try
		{
			// If a path to an assembly is passed on the command line, use
			// that as a test target.  If the command line is empty, use
			// the path to this program's EXE.
			//
			string fileName = (args.Length > 0 ? args[0] : Assembly.GetExecutingAssembly().Location);

			using (AssemblyParser asmParser = new AssemblyParser(fileName))
			{
				if (asmParser.IsAssembly)
				{
					return 1;
				}
			}
		}
		catch
		{
		}
		return 0;
	}
}
