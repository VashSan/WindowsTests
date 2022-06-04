using System;
using System.Diagnostics;
using System.IO;
using Windows.Tests.Helper;
using NUnit.Framework;

namespace Windows.Tests.Commandline;

public class XCopy
{
    string myTestDirectory = String.Empty;
    private string mySource = String.Empty;
    private string myDestination = String.Empty;

    [SetUp]
    public void Setup()
    {   
        myTestDirectory = Paths.NewTestDirectory();
        mySource = Path.Combine(myTestDirectory, "Source");
        Directory.CreateDirectory(mySource);
        myDestination = Path.Combine(myTestDirectory, "Destination");
        Directory.CreateDirectory(myDestination);
        
    }

    [TearDown]
    public void TearDown()
    {
        Paths.RemoveTestDirectory(myTestDirectory);
    }

    [TestCase("")]
    [TestCase("/I")]
    [TestCase("/S")]
    [TestCase("/E")]
    [TestCase("/I /E")]
    [TestCase("/I /S /E")]
    public void EmptySourceDir_ToEmptyDestination_Succeeds(string options)
    {
        // Arrange
        
        // Act
        var process = ExecuteXCopy(mySource, myDestination, options);
        
        // Assert
        Assert.That(process.ExitCode, Is.Zero);
    }

    [TestCase("/I /E")]
    [TestCase("/I /S /E")]
    public void EmptySourceDir_ToNonExistingDestination_CreatesDestination(string options)
    {
        // Arrange
        string newDestination = myDestination + "new";
        
        // Act
        var process = ExecuteXCopy(mySource, newDestination, options);
        
        // Assert
        Assert.That(process.ExitCode, Is.Zero);
        Assert.That(Directory.Exists(newDestination), Is.True);
    }
    
    [TestCase("/I")]
    [TestCase("/I /S")]
    public void EmptySourceDir_ToNonExistingDestination_DoesNothing(string options)
    {
        // Arrange
        string newDestination = myDestination + "new";
        
        // Act
        var process = ExecuteXCopy(mySource, newDestination, options);

        // Assert
        Assert.That(process.ExitCode, Is.Zero);
        Assert.That(Directory.Exists(newDestination), Is.False);
    }

    [TestCase("A*")]
    [TestCase("A*B*")]
    [TestCase("A*C*")]
    [TestCase("A*C*.*")]
    [TestCase("A*C*.txt")]
    [TestCase("A*C*.txt")]
    public void MatchingWildCards_ToExistingDirectory_CopiesMatchingFiles(string wildcard)
    {
        // Arrange
        var file1Target = Path.Combine(myDestination, "ABC.txt");
        var file1 = Path.Combine(mySource, "ABC.txt");
        File.WriteAllText(file1, "");
        
        var file2Target = Path.Combine(myDestination, "DEF.txt");
        var file2 = Path.Combine(mySource, "DEF.txt");
        File.WriteAllText(file2, "");

        string source = Path.Combine(mySource, wildcard);

        // Act
        var process = ExecuteXCopy(source, myDestination);

        // Assert
        Assert.That(process.ExitCode, Is.Zero);
        FileAssert.Exists(file1Target);
        FileAssert.DoesNotExist(file2Target);
    }
    
    [TestCase("A*")]
    [TestCase("A*B*")]
    [TestCase("A*C*")]
    [TestCase("A*C*.*")]
    [TestCase("A*C*.txt")]
    [TestCase("A*C*.*t")]
    [TestCase("*txt")]
    [TestCase("*.txt")]
    public void MatchingWildCards_ToMissingDirectory_CopiesMatchingFiles(string wildcard)
    {
        // Arrange
        string missingDestination = myDestination + "x";
        
        var file1Target = Path.Combine(missingDestination, "ABC.txt");
        var file1 = Path.Combine(mySource, "ABC.txt");
        File.WriteAllText(file1, "");
        
        var file2Target = Path.Combine(missingDestination, "DEF.doc");
        var file2 = Path.Combine(mySource, "DEF.doc");
        File.WriteAllText(file2, "");

        string source = Path.Combine(mySource, wildcard);

        // Act
        var process = ExecuteXCopy(source, missingDestination, "/I");

        // Assert
        Assert.That(process.ExitCode, Is.Zero);
        FileAssert.Exists(file1Target);
        FileAssert.DoesNotExist(file2Target);
    }
    
    [TestCase("*A")]
    [TestCase("*.xxx")]
    public void NotMatchingWildCards_ToExistingDirectory_DoesNothing(string wildcard)
    {
        // Arrange
        var file1Target = Path.Combine(myDestination, "ABC.txt");
        var file1 = Path.Combine(mySource, "ABC.txt");
        File.WriteAllText(file1, "");
        
        var file2Target = Path.Combine(myDestination, "DEF.txt");
        var file2 = Path.Combine(mySource, "DEF.txt");
        File.WriteAllText(file2, "");

        string source = Path.Combine(mySource, wildcard);

        // Act
        var process = ExecuteXCopy(source, myDestination);

        // Assert
        Assert.That(process.ExitCode, Is.Zero);
        FileAssert.DoesNotExist(file1Target);
        FileAssert.DoesNotExist(file2Target);
    }
    
    [Test]
    public void Copying_ToExistingTargets_OverwritesDestination()
    {
        // Arrange
        var file1Target = Path.Combine(myDestination, "ABC.txt");
        var file1 = Path.Combine(mySource, "ABC.txt");
        File.WriteAllText(file1, "");
        File.WriteAllText(file1Target, "xxx");

        // Act
        var process = ExecuteXCopy(mySource, myDestination, "/Y");

        // Assert
        Assert.That(process.ExitCode, Is.Zero);
        FileAssert.Exists(file1Target);
        FileAssert.AreEqual(file1, file1Target);
    }
    
    [Test]
    public void Copying_ToReadonlyTargets_EndsWithAccessDenied()
    {
        // Arrange
        var file1Target = Path.Combine(myDestination, "ABC.txt");
        var file1 = Path.Combine(mySource, "ABC.txt");
        File.WriteAllText(file1, "");
        File.WriteAllText(file1Target, "xxx");
        File.SetAttributes(file1Target, FileAttributes.ReadOnly);

        // Act
        var process = ExecuteXCopy(mySource, myDestination, "/Y");
        File.SetAttributes(file1Target, ~FileAttributes.ReadOnly); // revert otherwise directory cannot be deleted

        // Assert
        Assert.That(process.ExitCode, Is.EqualTo(4));
        FileAssert.AreNotEqual(file1, file1Target);
    }
    
    [Test]
    public void Copying_ToReadonlyTargets_OverwritesDestination()
    {
        // Arrange
        var file1Target = Path.Combine(myDestination, "ABC.txt");
        var file1 = Path.Combine(mySource, "ABC.txt");
        File.WriteAllText(file1, "");
        File.WriteAllText(file1Target, "xxx");
        File.SetAttributes(file1Target, FileAttributes.ReadOnly);

        // Act
        var process = ExecuteXCopy(mySource, myDestination, "/Y /R");

        // Assert
        Assert.That(process.ExitCode, Is.Zero);
        FileAssert.Exists(file1Target);
        FileAssert.AreEqual(file1, file1Target);
    }

    private Process ExecuteXCopy(string source, string destination, string options = "")
    {
        var process = Process.Start("xcopy.exe", $"{source} {destination} {options}");
        if (!process.WaitForExit(500))
        {
            process.Kill();
            Assert.Fail("Process timed out - is there console input pending?");
        }

        process.WaitForExit();
        return process;
    }
}