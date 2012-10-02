#include <iostream>
#include <cstdlib>

#include "common.h"

int main(int argc, char** argv)
{
	SmartBstr bstr(L"Hello!");
	std::wstring wstr(bstr);
	std::wcout << wstr << L"\n";
}

template<>
StrBase<UChar>::StrBuffer StrBase<UChar>::s_bufEmpty = StrBase<UChar>::StrBuffer();

template<>
void StrBase<UChar>::_Replace(int, int, const UChar*, UChar, int, int)
{
}

#include "Vector_i.cpp"
template class Vector<UChar>;
