#include <string.h>
#include <wchar.h>
#include <stdio.h>

#define MAXBUFF 40
#define MAXSZ 80

void fgetline(s_ptr,fp)
char *s_ptr;
FILE *fp;
{
/*   Collects the string from the FILE fp, into the string s_ptr */
	char c;
	while((c = getc(fp)) != '\n' && !feof(fp)) *s_ptr++ = c;
	*s_ptr = '\0';
}

int load_map_table(const char* filename, const char* delimit, char* table)
{
  FILE *fp;
  int i=0;
  size_t len;
  char inpt[2*MAXBUFF];
  char tmp[2*MAXBUFF];

  if ((fp = fopen(filename,"r")) == NULL)
  {
	perror(filename);
	return -1;
  }

  tmp[0] = '\0';
  while(!feof(fp))
  {
	fgetline(inpt,fp);
	if(inpt[0] != '\0')
	{
	strcpy(tmp,strtok(inpt,delimit));
	len = strlen(tmp);
	table[2*MAXBUFF*i] = '\0';
	strncat(table+2*MAXBUFF*i,tmp,len);
	table[2*MAXBUFF*i+len] = '\0';
	strcpy(tmp,strtok(NULL,delimit));
	len = strlen(tmp);
	table[MAXBUFF+2*MAXBUFF*i] = '\0';
	strncat(table+MAXBUFF+2*MAXBUFF*i,tmp,len);
	table[MAXBUFF+2*MAXBUFF*i+len] = '\0';
	i++;
   }
  }
  return i;
}
