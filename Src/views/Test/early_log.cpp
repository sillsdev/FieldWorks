#include <stdio.h>

// Run as early as possible during C++ static initialization.
struct EarlyLog
{
    EarlyLog()
    {
        printf("DEBUG: EarlyLog ctor\n");
        fflush(stdout);
    }
    ~EarlyLog()
    {
        printf("DEBUG: EarlyLog dtor\n");
        fflush(stdout);
    }
};

static EarlyLog g_earlyLog;
