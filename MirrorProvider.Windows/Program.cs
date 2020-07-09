namespace MirrorProvider.Windows
{
    class Program
    {
        static void Main(string[] args)
        {
            // enable projfs: as root in powershell: Enable-WindowsOptionalFeature -Online -FeatureName Client-ProjFS -NoRestart 

            MirrorProviderCLI.Run(args, new WindowsFileSystemVirtualizer());
        }
    }
}
