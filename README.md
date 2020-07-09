# projfsmirror
A mirror using ProjFS taken from https://github.com/microsoft/VFSForGit

They say they were about to delete the mirrorfs, but I think it is a great example to learn how to create a projected filesystem, and perform speed tests. 
https://github.com/microsoft/VFSForGit/issues/1681#issuecomment-656106722

# How to build
Open Visual Studio 2019 and build.

I got issues with commandline.dll again and again, that I solved (cheaply) doing this:

copy  C:\Users\pablo\.nuget\packages\commandlineparser\2.2.1\lib\netstandard1.5\CommandLine.dll

# How to enable the projfs in Windows 10
As Admin in PowerShell:

Enable-WindowsOptionalFeature -Online -FeatureName Client-ProjFS -NoRestart 

# How to run
Create the FS:

MirrorProvider.Windows.exe clone c:\Users\pablo\wkspaces\dev\five c:\Users\pablo\wkspaces\dynamic


Run:

MirrorProvider.Windows.exe mount c:\Users\pablo\wkspaces\dynamic
