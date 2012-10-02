#include <stdio.h>

int my_getword(char s[],int lim)
{
int c,i;
for(i=0;i<lim-1 && (c= getchar()) != EOF && !isspace(c) && (c != '<') && (c != '>'); ++i)
	s[i] = c;
if(c != EOF ){
	s[i] = c;
	++i;
}
s[i] = '\0';
return i;
}
