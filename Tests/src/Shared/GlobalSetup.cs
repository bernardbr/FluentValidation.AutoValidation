using System.Globalization;
using System.Threading;
using Xunit.Abstractions;
using Xunit.Sdk;

[assembly: Xunit.TestFramework("SharpGrip.FluentValidation.AutoValidation.Tests.Shared.GlobalSetup", "FluentValidation.AutoValidation.Tests")]
namespace SharpGrip.FluentValidation.AutoValidation.Tests.Shared;

public class GlobalSetup : XunitTestFramework
{
    public GlobalSetup(IMessageSink messageSink) : base(messageSink)
    {
        SetupEnvironment();
    }

    private static void SetupEnvironment()
    {
        Thread.CurrentThread.CurrentUICulture = new CultureInfo("en-US");
    }
}