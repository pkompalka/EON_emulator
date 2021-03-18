@echo off

cd "C:\Users\Piotr\Desktop\EON20\CableCloud\bin\Debug"
start CableCloud.exe
TIMEOUT /T 2
cd "C:\Users\Piotr\Desktop\EON20\Node\bin\Debug"
start Node.exe
TIMEOUT /T 3
cd "C:\Users\Piotr\Desktop\EON20\SubNetwork\bin\Debug"
start SubNetwork.exe
TIMEOUT /T 3
cd "C:\Users\Piotr\Desktop\EON20\NCC\bin\Debug"
start NCC.exe
TIMEOUT /T 3
cd "C:\Users\Piotr\Desktop\EON20\Host\bin\Debug"
start Host.exe
TIMEOUT /T 3
