using System.Runtime.CompilerServices;
using Meziantou.Framework.InlineSnapshotTesting;

namespace Axl.Tests;

public static class Bless
{
    public static bool IsEnabled { get; }
    
    static Bless()
    {
        if (Environment.GetEnvironmentVariable("BLESS") is not null)
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