class LogFile
{
public:
	LogFile();
	~LogFile();
	void Start();
	void Write(const char * szText);
	void TimeStamp();
	void Terminate();

protected:
	FILE * m_fileLog;
	void Initiate();
};
