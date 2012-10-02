#include "UnicodeConverter.h"
#include <iostream>
#include <iomanip>
#include <cstdlib>

int main(int argc, char** argv)
{
	const wchar_t* data = L"HelloHello\u2022\U0001D11BHelloHello";

	char    bufc[100];
	UChar   bufu[100];
	wchar_t bufw[100];

	int nc = UnicodeConverter::Convert(data, -1, bufc, 10);
	int nw = UnicodeConverter::Convert(bufc, -1, bufw, 5);
	bufc[nc] = 0;
	bufw[nw] = 0;
	char result[100]; std::wcstombs(result, bufw, sizeof(result));
	std::cout << "Convert(wchar_t*, char*): " << nc << " " << bufc << "\n";
	std::cout << "Convert(char*, wchar_t*): " << nw << " " << result << "\n";

	int nu = UnicodeConverter::Convert(data, -1, bufu, sizeof(bufu));
	std::cout << "Convert(wchar_t*, UChar*): " << nu << " ";
	std::cout << std::hex << std::setfill('0');
	for (const UChar* p = bufu; *p; ++p)
	{
		if (int(*p) < 0x80)
			std::cout << char(*p);
		else
			std::cout << "\\u" << std::setw(4) << int(*p);
	}
	std::cout << "\n";
}
