using System.Runtime.CompilerServices;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests;

public static class BlessingMode
{
    public static bool IsEnabled { get; }
    
    static BlessingMode()
    {
        if (Environment.GetEnvironmentVariable("BLESS") is "1")
        {
            IsEnabled = true;
        }
        else
        {
            IsEnabled = false;
        }
    }

    [ModuleInitializer]
    public static void SetupSnapshotBless()
    {
            InlineSnapshotSettings.Default.SnapshotUpdateStrategy = IsEnabled 
                ? SnapshotUpdateStrategy.Overwrite
                : SnapshotUpdateStrategy.Disallow;
        
    }
}