// --------------------------------------------------------------------------------------------
// <copyright from='2011' to='2011' company='SIL International'>
// 	Copyright (c) 2011, SIL International. All Rights Reserved.
//
// 	Distributable under the terms of either the Common Public License or the
// 	GNU Lesser General Public License, as specified in the LICENSING.txt file.
// </copyright>
// --------------------------------------------------------------------------------------------
#ifndef WIN32

//:>********************************************************************************************
//:>	   Include files
//:>********************************************************************************************
#include "main.h"
// any other headers (not precompiled)
#include <execinfo.h>
#include <dlfcn.h>

#undef THIS_FILE
DEFINE_THIS_FILE

/*----------------------------------------------------------------------------------------------
// Execute a command and get the result.
//
// @param   cmd - The system command to run.
// @return  The string command line output of the command.
// see http://stackoverflow.com/questions/2655374/how-to-redirect-the-output-of-a-system-call-to-inside-the-program-in-c-c/3578548#3578548
----------------------------------------------------------------------------------------------*/
std::string GetStdoutFromCommand(std::string cmd)
{
	std::string data;
	FILE * stream;
	const int max_buffer = 256;
	char buffer[max_buffer];
	cmd.append(" 2>&1"); // Do we want STDERR?

	stream = popen(cmd.c_str(), "r");
	if (stream)
	{
		while (!feof(stream))
		{
			if (fgets(buffer, max_buffer, stream) != NULL)
				data.append(buffer);
		}
		pclose(stream);

		if (!data.empty())
		{
			// strip last \n
			data.resize(data.length() - 1);
		}
	}
	return data;
}

char* SplitString(char*& str, char separator)
{
	char* out = NULL;
	char* p = strchr(str, separator);
	if (p)
	{
		str = p + 1;
		out = str;
		*p = '\0';
	}
	return out;
}

void StackDumper::ShowStackCore(HANDLE /*hThread*/, CONTEXT& /*c*/)
{
	// see http://stackoverflow.com/questions/77005/how-to-generate-a-stacktrace-when-my-gcc-c-app-crashes/2526298#2526298
	// compile with -rdynamic
	void * frames[200];
	int size = backtrace(frames, 200);

	char ** symbols = backtrace_symbols(frames, size);

	for (int frameNum = 0; frameNum < size; ++frameNum)
	{
		const char *filename = NULL;
		const char *symbol = NULL;
		const char *rest = NULL;

		if (symbols)
		{
			// find method name and offset
			char* tmp = symbols[frameNum];
			filename = tmp;
			symbol = SplitString(tmp, '(');
			rest = SplitString(tmp, ')');
		}

		// if the line could be processed, attempt to demangle the symbol
		if (filename && symbol && rest)
		{
			std::string objname(filename);

			char* base = 0;
			if (strlen(symbol))
			{
				// We got a symbol name and/or an offset.
				// This means the symbol is probably in a shared library.
				// We have to call addr2line with the offset in the shared library.
				Dl_info dlInfo;
				if (dladdr(frames[frameNum], &dlInfo))
					base = (char*)dlInfo.dli_fbase;

				// Use the library's debug symbols directly, if available.
				// This makes additional function names available.
				char* real = realpath(dlInfo.dli_fname, 0);
				objname = std::string("/usr/lib/debug") + real;
				free(real);
				if (access(objname.c_str(), R_OK) != 0)
				{
					// we can't access the debug symbols
					objname = filename;
				}
			}
			char syscom[256];
			sprintf(syscom, "addr2line --function --demangle -e %s %#tx", objname.c_str(),
				(char*)frames[frameNum] - (char*)base);
			std::string name, lineInfo;
			try
			{
				std::string output = GetStdoutFromCommand(syscom);
				name = output.substr(0, output.find("\n"));
				if (name.size() < output.size())
					lineInfo = output.substr(name.size() + 1);
			}
			catch (...)
			{
			}
			if (name.empty() || name == "??")
				name = symbol;
			m_pstaDump->FormatAppend("%3d %s%s   %s (%s)\n", frameNum, name.c_str(),
				rest, lineInfo.c_str(), filename);
		}
		else if (symbols)
		{
			// Couldn't process the line - just print whatever we have
			m_pstaDump->FormatAppend("%3d %s   (no line # avail)\n", frameNum,
				symbols[frameNum]);
		}
		else // symbols == NULL
		{
			// for whatever reason we can't get the symbols. Just output the address of the
			// method.
			Dl_info dlInfo;
			char * addr = (char*)frames[frameNum];
			if (dladdr(addr, &dlInfo))
				addr -= (intptr_t)dlInfo.dli_fbase;

			// our implementation of Format has problems with pointers on 64-bit, so we use
			// sprintf instead.
			char line[256];
			sprintf(line, "%3d %p (%p)  (no line # avail)\n", frameNum, frames[frameNum],
				addr);
			m_pstaDump->Append(line);
		}
	}

	free(symbols);
}

StrUni ConvertException(DWORD dwExcept)
{
	StrUni stuHrMsg;
	stuHrMsg.Format("hr=0x%08x", dwExcept);
	return stuHrMsg;
}
#endif
