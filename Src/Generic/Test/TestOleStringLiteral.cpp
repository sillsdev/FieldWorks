#include "OleStringLiteral.h"
#include <iostream>
#include <iomanip>

int main(int argc, char** argv)
{
	OleStringLiteral literal(L"HelloHello\u2022\U0001D11BHelloHello");

	const wchar_t* original = literal;
	//std::wcout << original << std::endl;

	std::cout << std::hex << std::setfill('0');
	for (const OleStringLiteral::uchar_t* p = literal; *p; ++p)
	{
		if (int(*p) < 0x80)
			std::cout << char(*p);
		else
			std::cout << "\\u" << std::setw(4) << int(*p);
	}
	std::cout << std::endl;
}
