/***********************************************************************************************
	DosMail.cpp

	Usage:
		DosMail from_address to_address [subject] message
/**********************************************************************************************/

#include <winsock2.h>
//#include <sys/socket.h>
//#include <netdb.h>
#include <time.h>
#include <stdio.h>
#include <stdlib.h>
#include <string.h>
#include <ctype.h>
//#include <unistd.h>

#define ERROR_HOST          "Could not find the host."
#define ERROR_SOCKET        "Could not create a socket."
#define ERROR_CONNECT       "Could not connect to the socket."
#define ERROR_READ          "Could not read from the socket."
#define ERROR_WRITE         "Could not write to the socket."
#define ERROR_NO_FROM       "The \"From\" address is not valid."
#define ERROR_NO_RECIPIENTS "The \"To\" address is not valid."
#define ERROR_NO_MESSAGE    "The message could not be sent."
#define ERROR_NO_MEMORY     "There is not enough memory."

int Error(char * pszError, SOCKET s)
{
	printf("\nError: %s\n", pszError);
	if (s)
		closesocket(s);
	WSACleanup();
	return 1;
}

int main(int argc, char ** argv)
{
	if (argc < 4 || argc > 5)
	{
		printf("Usage: DosMail from_address to_address [subject] message\n");
		return 1;
	}

	char * pszFromAddress = argv[1];
	char * pszToAddress = argv[2];
	char * pszSubject = NULL;
	char * pszMessage = NULL;
	if (argc == 4)
	{
		pszMessage = argv[3];
	}
	else
	{
		pszSubject = argv[3];
		pszMessage = argv[4];
	}
	char * pszHost = strchr(pszFromAddress, '@') + 1;
	if (pszHost == (char*)1)
	{
		printf(ERROR_NO_FROM);
		return 1;
	}

	SOCKET s = 0;
	struct hostent * hp;
	struct sockaddr_in sin = {0};
	char szBuffer[200];
	WSADATA wsaData;

	if (WSAStartup(MAKEWORD(2, 0), &wsaData))
	{
		printf("\nError: Could not find the WinSock DLL\n.");
		return 1;
	}
	if ((hp = gethostbyname(pszHost)) == 0)
		return Error(ERROR_HOST, s);

   // Make a connection to the host on port 25
	memcpy(&sin.sin_addr, hp->h_addr, hp->h_length);
	sin.sin_family = AF_INET;
	//sin.sin_family = hp->h_addrtype;
	sin.sin_port = htons(25);
	if ((s = socket(AF_INET, SOCK_STREAM, 0)) == INVALID_SOCKET)
		return Error(ERROR_SOCKET, s);
	if (connect(s, (struct sockaddr *) &sin, sizeof(sin)) == INVALID_SOCKET)
		return Error(ERROR_CONNECT, s);
	if (recv(s, szBuffer, sizeof(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_READ, s);

	// Send helo message
	sprintf(szBuffer, "helo %s\n", pszHost);
	if (send(s, szBuffer, strlen(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_WRITE, s);
	memset(szBuffer, 0, sizeof(szBuffer));
	if (recv(s, szBuffer, sizeof(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_READ, s);
	if (atoi(szBuffer) != 250)
		return Error(ERROR_CONNECT, s);

	// Send "mail from:<>" message
	sprintf(szBuffer, "mail from:<%s>\n", pszFromAddress);
	if (send(s, szBuffer, strlen(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_WRITE, s);
	memset(szBuffer, 0, sizeof(szBuffer));
	if (recv(s, szBuffer, sizeof(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_READ, s);
	if (atoi(szBuffer) != 250)
		return Error("Invalid from address.", s);

	// Send "rcpt to:<>" message(s)
	sprintf(szBuffer, "rcpt to:<%s>\n", pszToAddress);
	if (send(s, szBuffer, strlen(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_WRITE, s);
	memset(szBuffer, 0, sizeof(szBuffer));
	if (recv(s, szBuffer, sizeof(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_READ, s);
	if (atoi(szBuffer) != 250)
		return Error(ERROR_NO_RECIPIENTS, s);

	// Send "data" message
	strcpy(szBuffer, "data\n");
	if (send(s, szBuffer, strlen(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_WRITE, s);
	memset(szBuffer, 0, sizeof(szBuffer));
	if (recv(s, szBuffer, sizeof(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_READ, s);
	if (atoi(szBuffer) != 354)
		return Error(ERROR_NO_MESSAGE, s);

	// Send subject line
	if (pszSubject)
	{
		sprintf(szBuffer, "Subject: %s\n", pszSubject);
		if (send(s, szBuffer, strlen(szBuffer), 0) == INVALID_SOCKET)
			return Error(ERROR_WRITE, s);
	}

	// Send message lines
	sprintf(szBuffer, "%s\n", pszMessage);
	if (strcmp(szBuffer, ".\n") == 0)
		strcpy(szBuffer, "..\n");
	if (send(s, szBuffer, strlen(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_WRITE, s);

	// Sent time stamp
	time_t ltime;
	time(&ltime);
	sprintf(szBuffer, "Time sent: %s", ctime(&ltime));
	if (strcmp(szBuffer, ".\n") == 0)
		strcpy(szBuffer, "..\n");
	if (send(s, szBuffer, strlen(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_WRITE, s);

	// End the message
	strcpy(szBuffer, ".\n");
	if (send(s, szBuffer, strlen(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_WRITE, s);
	memset(szBuffer, 0, sizeof(szBuffer));
	if (recv(s, szBuffer, sizeof(szBuffer), 0) == INVALID_SOCKET)
		return Error(ERROR_READ, s);
	if (atoi(szBuffer) != 250)
		return Error(ERROR_NO_MESSAGE, s);

	// Send "quit" message and close socket
	strcpy(szBuffer, "quit\n");
	send(s, szBuffer, strlen(szBuffer), 0);
	closesocket(s);
	s = 0;

	// Print out success or failure of message
	printf("\nThe message was sent successfully.\n");
	WSACleanup();
	return 0;
}