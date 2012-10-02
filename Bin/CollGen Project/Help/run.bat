REM Compile the customer assembly
csc /t:library Customer.cs

REM Compile your generated Customer Collection
csc /t:library /r:Customer.dll CustomerCOllection.cs

REM Compile the client application
csc /r:Customer.dll /r:CustomerCollection.dll Client.cs

REM Run the Client code
Client