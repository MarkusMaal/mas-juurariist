using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using HardwareInformation;

namespace Markuse_asjade_juurutamise_tööriist;

public class Edition
{
    public string EditionName { get; set; }
    public string Version { get; set; }
    public string BuildNo { get; set; }
    public bool Tested { get; set; }
    public string Username { get; set; }
    public string Language { get; set; }
    public string WinVer { get; set; }
    public string[] Features { get; set; }
    public string Pin { get; set; }
    public string Name { get; set; }
    public string Hash { get; set; }

    public bool Unsupported { get; set; }

    public string MasRoot { get; set; }
    
    private MachineInformation hwinfo = MachineInformationGatherer.GatherInformation(true);
    
    private App? app = ((App?)App.Current);

    public Edition(string textFile)
    {
        string[] lines;
        using (var readText = new StreamReader(textFile))
        {
            lines = readText.ReadToEnd().Split('\n');
        }
        EditionName = lines[0];
        Version = lines[1];
        BuildNo = lines[2];
        Tested = lines[3] == "Yes";
        Username = lines[4];
        Language = lines[5];
        WinVer = lines[6];
        Features = lines[7].Split('-');
        Pin = lines[8];
        Name = lines[9];
        Hash = lines[10];
        Unsupported = false;
    }
    
    
    public bool Verifile()
    {
        string verificatable = q();
        string[] savedstr = File.ReadAllText(MasRoot + "/edition.txt", Encoding.GetEncoding(1252)).ToString()
            .Split('\n');
        string sttr = savedstr[savedstr.Length - 1];
        if (verificatable == sttr)
        {
            return true;
        }

        return false;
    }

    public string q()
    {
        try
        {
            string CPIProcessorID = "CPI0" + ProcessorID().Substring(1);
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            string SHA1HashOfTrimmedEditionInfo = SHA1Hash(RemoveLinesFromBottomUntilPINAddNewLine(File.ReadAllText(MasRoot + "/edition.txt", Encoding.GetEncoding(1252))));

            return (MD5Hash(SHA1HashOfTrimmedEditionInfo + SHA1Hash(CPIProcessorID.Substring(1, CPIProcessorID.Length - 2)
                + CPIProcessorID.Substring(0, 1)
                + CPIProcessorID.Substring(CPIProcessorID.Length - 1, 1))).ToLower() + SHA1Hash(BIOS()).ToLower() + SHA1Hash(BaseboardProduct)).ToLower();
        }
        catch
        {
            Unsupported = true;
            return "N/A";
        }
    }

    static string RemoveLinesFromBottomUntilPINAddNewLine(string s)
    {
        string[] sar = s.Split('\n');
        string ns = "";
        for (int i = 0; i < sar.Length - 3; i++)
        {
            ns += sar[i].ToString() + "\n";
        }
        return ns;
    }
    private string BIOS()
    {
        return hwinfo.SmBios.BIOSVersion.ToString() + "\r";
    }

    public string ProcessorID()
    {
        if (app == null) return "";
        return app.GetCpuId();
    }
    public static string SHA1Hash(string z)
    {
        SHA1 cx = SHA1.Create();
        byte[] xx = Encoding.ASCII.GetBytes(z);
        byte[] hash = cx.ComputeHash(xx);

        StringBuilder t = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            t.Append(hash[i].ToString("X2"));
        }
        return t.ToString();
    }
    public static string MD5Hash(string z)
    {
        MD5 cx = MD5.Create();
        byte[] xx = Encoding.ASCII.GetBytes(z);
        byte[] hash = cx.ComputeHash(xx);

        StringBuilder t = new StringBuilder();
        for (int i = 0; i < hash.Length; i++)
        {
            t.Append(hash[i].ToString("X2"));
        }
        return t.ToString();
    }

    public string BaseboardProduct {
        get
        {
            return "";
        }
    }
    
    /// <summary>
    /// Builds a script that displays all Java binaries and versions for your system and marks it executable (Unix-like systems)
    /// </summary>
    private void BuildJavaFinder()
    {
        if (!File.Exists(Path.GetTempPath() + "verifile2.jar"))
        {
            File.WriteAllBytes(Path.GetTempPath() + "verifile2.jar", Properties.Resources.verifile2);
        }
        if (!File.Exists(Path.GetTempPath() + "/find_java" + (OperatingSystem.IsWindows() ? ".bat" : ".sh")))
        {

            var builder = new StringBuilder();
            using var javaFinder = new StringWriter(builder)
            {
                NewLine = OperatingSystem.IsWindows() ? "\r\n" : "\n"
            };
            if (OperatingSystem.IsWindows())
            {
                javaFinder.WriteLine("@echo off");
                javaFinder.WriteLine("setlocal EnableDelayedExpansion");
                javaFinder.WriteLine("for /f \"delims=\" %%a in ('where java') do (");
                javaFinder.WriteLine("\tset \"javaPath=\"%%a\"\"");
                javaFinder.WriteLine("\tfor /f \"tokens=3\" %%V in ('%%javaPath%% -version 2^>^&1 ^| findstr /i \"version\"') do (");
                javaFinder.WriteLine("\t\tset \"version=%%V\"");
                javaFinder.WriteLine("\t\tset \"version=!version:\"=!\"");
                javaFinder.WriteLine("\t\techo !javaPath:\"=!:!version!");
                javaFinder.WriteLine("\t)");
                javaFinder.WriteLine(")");
                javaFinder.WriteLine("endlocal");
                javaFinder.WriteLine("exit/b");
            }
            else if (OperatingSystem.IsLinux())
            {
                javaFinder.WriteLine("#!/usr/bin/bash");
            }
            else if (OperatingSystem.IsMacOS())
            {
                javaFinder.WriteLine("#!/bin/bash");
            }
            if (!OperatingSystem.IsWindows())
            {
                javaFinder.WriteLine("OLDIFS=$IFS");
                javaFinder.WriteLine("IFS=:");
                javaFinder.WriteLine("for dir in $PATH; do");
                javaFinder.WriteLine("    if [[ -x \"$dir/java\" ]]; then  # Check if java exists and is executable");
                javaFinder.WriteLine("        javaPath=\"$dir/java\"");
                javaFinder.WriteLine("        version=$(\"$javaPath\" -version 2>&1 | awk -F '\"' '/version/ {print $2}')");
                javaFinder.WriteLine("        echo \"$javaPath:$version\"");
                javaFinder.WriteLine("    fi");
                javaFinder.WriteLine("done");
                javaFinder.WriteLine("IFS=$OLDIFS");
            }
            File.WriteAllText(Path.GetTempPath() + "/find_java" + (OperatingSystem.IsWindows() ? ".bat" : ".sh"), builder.ToString(), Encoding.ASCII);
            if (!OperatingSystem.IsWindows())
            {
                File.SetUnixFileMode(Path.GetTempPath() + "/find_java.sh", UnixFileMode.UserRead | UnixFileMode.UserExecute | UnixFileMode.UserWrite);
            }
        }
    }

    /// <summary>
    /// Finds the latest version of Java installed on your system, since if you install the Java SE version, Verifile may not work with it.
    /// </summary>
    /// <returns>Path to the latest Java binary found on your system</returns>
    private string FindJava()
    {
        CultureInfo culture = CultureInfo.CurrentCulture;
        string p = culture.NumberFormat.NumberDecimalSeparator;
        string latest_version = $"0{p}0";
        string latest_path = "";
        string interpreter = OperatingSystem.IsWindows() ? "cmd" : "bash";
        Process pr = new()
        {
            StartInfo = new ProcessStartInfo()
            {
                FileName = interpreter,
                Arguments = (OperatingSystem.IsWindows() ? "/c " : "") + "\"" + Path.GetTempPath() + (OperatingSystem.IsWindows() ? "\\" : "/") + "find_java." + (OperatingSystem.IsWindows() ? "bat" : "sh") + "\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            }
        };
        pr.Start();
        while (!pr.StandardOutput.EndOfStream) {
            string[] path_version = (pr.StandardOutput.ReadLine() ?? ":").Replace(":\\", "_WINDRIVE\\").Split(':');
            string path = path_version[0].Replace("_WINDRIVE\\", ":\\");
            string version = path_version[1].Split('_')[0];
            version = version.Split('.')[0] + p + version.Split('.')[1];
            if (double.Parse(version, NumberStyles.Any) > double.Parse(latest_version, NumberStyles.Any))
            {
                latest_path = path;
                latest_version = version;
            }
        }
        return latest_path;
    }
    
    private string Verifile2()
    {
        BuildJavaFinder();
        Process p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = FindJava(),
                Arguments = "-jar " + Path.GetTempPath() + "verifile2.jar",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            }
        };
        p.Start();
        while (!p.StandardOutput.EndOfStream)
        {
            string line = p.StandardOutput.ReadLine() ?? "";
            return line.Split('\n')[0];
        }
        return "FAILED";
    }

    // change backslashes to forward slashes in case we're not in Windows
    private string BackForwardSlash(string path)
    {
        if (!OperatingSystem.IsWindows())
        {
            path = path.Replace("\\", "/");
        }
        return path;
    }

    public string GetBios()
    {
        
    }
}