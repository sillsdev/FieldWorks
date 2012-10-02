/*************************************************************************************
	Code generator to parse the xml files and produce the output files.
*************************************************************************************/
#include "CmCG.h"

bool g_fVerbose;

int main(int cpsz, char **prgpsz)
{
	AssertPtrSize(prgpsz, cpsz);

	STR strError;
	STR strIn;
	STR strSql;
	STR strSqh;
	char *psz;
	std::ofstream stmSql;
	std::ofstream stmSqh;

	std::cerr << "SIL (R) Conceptual Model Code Generator (" Debug("Debug; ") __DATE__ "; " __TIME__ ")";
	std::cerr << std::endl;
	std::cerr << "Copyright (C) SIL 1999. All rights reserved." << std::endl << std::endl;

	for (prgpsz++; --cpsz > 0; prgpsz++)
	{
		psz = *prgpsz;
		AssertPsz(psz);
		if (psz[0] == '-' || psz[0] == '/')
		{
			if (!_memicmp(psz + 1, "p", 1))
			{
				if (!psz[2])
					continue;
				if (g_strSearchPath.length() > 0 &&
					g_strSearchPath[g_strSearchPath.length() - 1] != ';')
				{
					g_strSearchPath += ";";
				}
				g_strSearchPath += (psz + 2);
				continue;
			}
			else if (!_memicmp(psz + 1, "v", 1))
			{
				g_fVerbose = true;
				continue;
			}
			strError = "Bad command line option.";
			goto LUsage;
		}

		if (!strIn.size())
			strIn = psz;
		else if (!strSql.size())
			strSql = psz;
		else if (!strSqh.size())
			strSqh = psz;
		else
		{
			strError = "Bad command line parameter.";
			goto LUsage;
		}
	}

	if (!strIn.size())
	{
		strError = "Missing source file name.";
		goto LUsage;
	}

	if (!strSql.size())
	{
		strError = "Missing destination .sql file name.";
		goto LUsage;
	}

	if (!strSqh.size())
	{
		strError = "Missing destination .sqh file name.";
		goto LUsage;
	}

	if (!g_mop.Parse(strIn))
	{
		std::cerr << "Parsing " << strIn.c_str() << " Failed." << std::endl;
		goto LUsage;
	}

	stmSql.open(strSql.c_str());
	if (stmSql.bad())
	{
		std::cerr << "Error opening : " << strSql.c_str() << std::endl;
		return 1;
	}

	stmSqh.open(strSqh.c_str());
	if (stmSqh.bad())
	{
		std::cerr << "Error opening : " << strSqh.c_str() << std::endl;
		return 1;
	}

	int wRet;
	wRet = g_mop.GenerateCode(stmSql, stmSqh);

	if (stmSql.bad() || stmSqh.bad())
	{
		std::cerr << "Error writing : " << strSql.c_str() << " or " << strSqh.c_str() << std::endl;
		wRet = 1;
	}

	stmSql.close();
	stmSqh.close();
	if (wRet)
	{
		remove(strSql.c_str());
		remove(strSqh.c_str());
	}

	return wRet;

LUsage:
	std::cerr << strError.c_str() << std::endl << std::endl;
	std::cerr << "Usage: cmcg [-p<search path>] <src> <sql> <sqh>" << std::endl;
	std::cerr << "   <src> : Conceptual model (.cm) file." << std::endl;
	std::cerr << "   <sql> : Output (.sql) file." << std::endl;
	std::cerr << "   <sqh> : Output (.sqh) file." << std::endl;
	std::cerr << "   -p<path> : specifies the search path for finding <src> and its class files." << std::endl;
	std::cerr << std::endl;

	return 1;
}
