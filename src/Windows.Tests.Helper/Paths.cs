using System;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Interfaces;

namespace Windows.Tests.Helper;

public static class Paths
{
    /// <summary>
    /// Returns a directory where to put temporary test files into
    /// </summary>
    public static string NewTestDirectory()
    {
        string testDirectory = Path.Combine(Path.GetTempPath(), "Windows.Tests",
            TestContext.CurrentContext.Test.ClassName ?? "NoClassName",
            TestContext.CurrentContext.Test.MethodName ?? "NoMethodName",
            TestContext.CurrentContext.Test.ID,
            $"{DateTime.UtcNow:yyyy-MM-ddThh-mm-ss-ffff}");
        Directory.CreateDirectory(testDirectory);
        TestContext.Out.WriteLine($"Created test directory '{testDirectory}'");
        return testDirectory;
    }

    /// <summary>
    /// Removes the given directory.
    /// </summary>
    /// <param name="testDirectory"></param>
    /// <remarks>Will work with any directory, be careful.</remarks>
    public static void RemoveTestDirectory(string testDirectory)
    {
        if (TestContext.CurrentContext.Result.Outcome.Status != TestStatus.Failed)
        {
            TestContext.Out.WriteLine($"Removing test directory '{testDirectory}'");
            const bool deleteRecursive = true;
            Directory.Delete(testDirectory, deleteRecursive);
        }
    }
}