
// JohnT: This file contains code that calls unmanaged code in ways that I can't get to work
// from inside a predominantly managed file. Currently it is actually compiled as managed,
// but if I refer to INPUT or SendInput within AccessibilityHelperBase.cpp, even after
// defininng _WIN32_WINNT 0x0500 and including windows.h, it gives errors.
#if (_WIN32_WINNT <= 0x0400)
#pragma message("Defining _WIN32_WINNT to 0x0500 for SendInput. Requires W2000 or better")
#define _WIN32_WINNT 0x0500
#endif
#include <windows.h>
#include "Stdafx.h"


// Tried to overload SimulateClickInternal with an arg that defaults to left click
// Compiled, but got a link warning.
// The C# method calling the C# base class couldn't link to the overloaded method.
// Also, I don't think COM allows method overloads.
// I (MDL) ended up giving the right click a different name rather than a parameter.

// option: 0 = left click, 1 = right click
void SimulateInternalClick(int option = 0)
{
	INPUT input[2];
	memset(input, 0, sizeof(input));

	DWORD dwFlags = MOUSEEVENTF_LEFTDOWN;
	if (1 == option) dwFlags = MOUSEEVENTF_RIGHTDOWN;

	// Fill the structure
	input[0].type = INPUT_MOUSE;
	input[0].mi.dwFlags = dwFlags;
	input[0].mi.dwExtraInfo = 0;
	input[0].mi.dx = 0;
	input[0].mi.dy = 0;
	input[0].mi.time = GetTickCount();

	// All inputs are almost the same
	memcpy(&input[1], &input[0], sizeof(INPUT));

	// ... almost
	dwFlags = MOUSEEVENTF_LEFTUP;
	if (1 == option) dwFlags = MOUSEEVENTF_RIGHTUP;
	input[1].mi.dwFlags = dwFlags;

	// MDL: When the application is first started after login, the time delay here may not be great enough
	// for the first click - it failed at least once.
	SendInput(1, input, sizeof(INPUT));
	Sleep(100);
	SendInput(1, input+1, sizeof(INPUT));
}

void SimulateClickInternal()
{
	SimulateInternalClick(0);
}

void SimulateRightClickInternal()
{
	SimulateInternalClick(1);
}

void SimulateInternalDrag()
{
	INPUT input[2];
	memset(input, 0, sizeof(input));

	// Fill the structure
	input[0].type = INPUT_MOUSE;
	input[0].mi.dwFlags = MOUSEEVENTF_LEFTDOWN;
	input[0].mi.dwExtraInfo = 0;
	input[0].mi.dx = 0;
	input[0].mi.dy = 0;
	input[0].mi.time = GetTickCount();

	// All inputs are almost the same
	memcpy(&input[1], &input[0], sizeof(INPUT));

	// ... almost
	input[1].mi.dwFlags = MOUSEEVENTF_LEFTUP;
	input[1].mi.dx = 0;
	input[1].mi.dy = 0;

	// MDL: When the application is first started after login, the time delay here may not be great enough
	// for the first click - it failed at least once.
	SendInput(1, input, sizeof(INPUT));
	Sleep(100);
	SendInput(1, input+1, sizeof(INPUT));
}
