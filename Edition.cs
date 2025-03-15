using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using HardwareInformation;

namespace Markuse_asjade_juurutamise_tööriist;

public class Edition
{
    
    /// <summary>
    /// Edition name (e.g. Pro, Premium, Basic+)
    /// </summary>
    public string EditionName { get; set; }
    
    /// <summary>
    /// Version number (e.g. 10.4)
    /// </summary>
    public string Version { get; set; }
    
    /// <summary>
    /// Build number - first letter(s) represent(s) the initial(s) for the edition name, next few numbers represent major version number and remaining numbers represent minor revisions. The last lowercase letter represents device type (a = physical desktop computer, b = virtual computer, c = tablet)
    /// </summary>
    public string BuildNo { get; set; }
    
    /// <summary>
    /// Boolean representing if a system integrity check has been run during the deployment process, stored in edition.txt as either "Yes" or "No"
    /// </summary>
    public bool Tested { get; set; }
    
    /// <summary>
    /// The user who initially started the deployment process for this computer
    /// </summary>
    public string Username { get; set; }
    
    /// <summary>
    /// System language during the deployment process
    /// </summary>
    public string Language { get; set; }
    
    /// <summary>
    /// Operating system kernel version during the initial deployment process
    /// </summary>
    public string WinVer { get; set; }
    
    /// <summary>
    /// List of optional features, stored in edition.txt with dashes (-) used as separators
    /// </summary>
    public List<string> Features { get; set; }
    
    /// <summary>
    /// Insecure PIN code for this computer, for legacy compatibility
    /// </summary>
    public string Pin { get; set; }
    
    /// <summary>
    /// Name for the current version
    /// </summary>
    public string Name { get; set; }
    
    /// <summary>
    /// Verifile 1.0 hash
    /// </summary>
    public string Hash { get; set; }

    /// <summary>
    /// Determines if this computer is unable to generate valid Verifile 1.0 hashes
    /// </summary>
    public bool Unsupported { get; set; }

    /// <summary>
    /// Root directory for Markus' stuff deployment
    /// </summary>
    public required string MasRoot { get; set; }
    
    /// <summary>
    /// Hardware information object
    /// </summary>
    private MachineInformation? hwinfo;
    
    /// <summary>
    /// App object
    /// </summary>
    private App? app = ((App?)App.Current);

    /// <summary>
    /// Constructor for Edition class taking the path to edition.txt as input
    /// </summary>
    /// <param name="textFile">Full path to edition.txt file</param>
    public Edition(string textFile)
    {
        string[] lines;
        using (var readText = new StreamReader(textFile))
        {
            lines = readText.ReadToEnd().Split('\n');
        }
        EditionName = lines[1];
        Version = lines[2];
        BuildNo = lines[3];
        Tested = lines[4] == "Yes";
        Username = lines[5];
        Language = lines[6];
        WinVer = lines[7];
        Features = lines[8].Split('-').ToList();
        Pin = lines[9];
        Name = lines[10];
        Hash = lines[11];
        if (RuntimeInformation.ProcessArchitecture == Architecture.X86 || RuntimeInformation.ProcessArchitecture == Architecture.X64)
        {
            hwinfo = MachineInformationGatherer.GatherInformation();
        }

        Unsupported = hwinfo == null;
    }
    
    /// <summary>
    /// Run Verifile 1.0 attestation
    /// </summary>
    /// <returns>Attestation result as bool (true = pass, false = fail)</returns>
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

    /// <summary>
    /// Generate Verifile 1.0 hash
    /// </summary>
    /// <returns>Hash in hexadecimal format (lower case)</returns>
    public string q()
    {
        try
        {
            string CPIProcessorID = "CPI0" + ProcessorId().Substring(1);
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

    /// <summary>
    /// Trim edition info
    /// </summary>
    /// <param name="s">Contents of edition.txt</param>
    /// <returns>Contents without version name and verifile hash</returns>
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
    
    /// <summary>
    /// Request BIOS version
    /// </summary>
    /// <returns>SmBios.BIOSVersion string value and carriage return at the end</returns>
    private string BIOS()
    {
        return hwinfo.SmBios.BIOSVersion.ToString() + "\r";
    }

    /// <summary>
    /// Sends a CPUID command to your processor to get its ID (not always unique)
    /// </summary>
    /// <returns>Hexadecimal representation of CPUID value</returns>
    private string ProcessorId()
    {
        if (app == null) return "";
        return app.GetCpuId();
    }
    
    /// <summary>
    /// Calculate SHA-1 hash for a string
    /// </summary>
    /// <param name="z">String to be hashed</param>
    /// <returns>Hexadecimal representation of hashed string</returns>
    private static string SHA1Hash(string z)
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
    
    /// <summary>
    /// Calculate MD5 hash for a string
    /// </summary>
    /// <param name="z">String to be hashed</param>
    /// <returns>Hexadecimal representation of hashed string</returns>
    private static string MD5Hash(string z)
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

    /// <summary>
    /// Get baseboard product. Currently dummy empty string, since Verifile 1.0 tended to return "" for this often.
    /// </summary>
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
    
    /// <summary>
    /// Run Verifile 2.0 attestation check
    /// </summary>
    /// <returns>Attestation result (e.g. VERIFIED, FAILED, TAMPERED, etc.)</returns>
    public string Verifile2()
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
    
    /// <summary>
    /// Gets BIOS ID for current machine
    /// </summary>
    /// <returns>First block of BIOS ID for your system</returns>
    public string GetBios()
    {
        if (!Unsupported)
        {
            return hwinfo.SmBios.BIOSVersion;
        }
        else
        {
            return "N/A";
        }
    }
    
    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Väljaanne: {EditionName}");
        sb.AppendLine($"Versioon: {Version}");
        sb.AppendLine($"Järgunumber: {BuildNo}");
        sb.AppendLine("Testitud: " + (Tested ? "Jah" : "Ei"));
        sb.AppendLine($"Kasutajanimi: {Username}");
        sb.AppendLine($"Kerneli versioon: {WinVer}");
        sb.AppendLine("Funktsioonid: " + string.Join("-", Features.ToArray()));
        sb.AppendLine($"Ebaturvaline PIN kood: {Pin}");
        sb.AppendLine($"Nimi: {Name}");
        return sb.ToString();
    }

    private string CheckRootFile(string file, bool failable = false)
    {
        return (File.Exists(MasRoot + "/" + file) ? "OK" : failable ? "WARN" : "FAIL");
    }

    private string TestFiles(string msg, string[] files)
    {
        string result = $"-> {msg}\n";
        foreach (string file in files)
        {
            result += $"{file}: " + CheckRootFile(file) + "\n";
        }

        return result;
    }
    
    public string PerformTests()
    {
        string result = "";
        string[] commonfiles =
        [
            "bg_common.png", "bg_desktop.png", "bg_login.png", "bg_uncommon.png", "bg_mobile.png", "bg_mobile_lock.png",
            "bg_tablet.png", "bg_tablet_lock.png", "verifile2.jar", "mas.ico", "mas_general.png", "mas_virtualpc.ico"
        ];
        result += TestFiles("Oluliste failide olemasolu testimine", commonfiles);
        result += "edition.txt: " + CheckRootFile("edition.txt", true) + "\n";
        result += "edition_1.txt: " + CheckRootFile("edition_1.txt", true) + "\n";
        string[] rd_tests = ["maia/server.py", "maia/whitelist.txt", "maia/mas_general.ico"];
        if (OperatingSystem.IsWindows())
        {
            string[] testfiles = ["Veebibrauser.lnk", "update_bg.ps1", "explorer.bat", "finalize_install.bat", "game_optimize.bat", "game_preoptimize.bat", "game_restore.bat", "info.bat", "redoexp.cmd", "remas.bat", "ScreenShot.ps1", "servicestart.bat"];
            string[] rainmeter_tests = ["swap_desktop.bat", "swap_desktop_flash.bat", "swap_desktop_rainmeter.bat", "organize_desktop.bat", "start_rainmeter.bat", "startup_optimize.bat"];
            string[] it_tests = ["itstart.bat", "opan_mas.bat"];
            result += TestFiles("Windowsi failide testimine", testfiles);
            if (Features.Contains("RM"))
            {
                result += TestFiles("Rainmeter testid", rainmeter_tests);
            }
            if (Features.Contains("IT"))
            {
                result += TestFiles("Interaktiivse töölaua testid", it_tests);
            }

        } else if (OperatingSystem.IsLinux())
        {
            result += $"-> Linuxi failide testimine\n";
            string[] linuxfiles = ["scripts/Tools.sh", ".mas/attach.sh", ".mas/detach.sh", ".mas/restart_plasma.sh"];
            foreach (string file in linuxfiles)
            {
                result += $"{file}: " + (File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/" + file) ? "OK" : "FAIL") + "\n";
            }

        }
        if (Features.Contains("RD"))
        {
            result += TestFiles("Kaugtöölaua testid", rd_tests);
        }

        if (result.Contains("FAIL"))
        {
            result += "\nSeda arvutit ei saa juurutada, kuna see ei vasta nõuetele";
        }

        else if (result.Contains("WARN"))
        {
            result += "\nSee arvuti ei ole tõenäoliselt juurutatud, aga muidu vastab nõuetele";
        }
        else
        {
            result += "\nSee arvuti vastab juurutamise nõuetele";   
        }
        return result;
    }

    public void Reverificate()
    {
        this.Hash = q();
    }

    public void SaveEditionInfo()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        using var sw = new StreamWriter(MasRoot + "/edition.txt", false);
        sw.Write("[Edition_info]\n");
        sw.Write(EditionName + "\n");
        sw.Write(Version + "\n");
        sw.Write(BuildNo + "\n");
        sw.Write((Tested ? "Yes" : "No") + "\n");
        sw.Write(Username + "\n");
        sw.Write(Language + "\n");
        sw.Write(WinVer + "\n");
        sw.Write(string.Join("-", Features.ToArray()) + "\n");
        sw.Write(Pin + "\n");
        sw.Write(Name + "\n");
        sw.Write(Hash);
        sw.Close();
    }

    public void UnlockVF2()
    {
        BuildJavaFinder();
        Process p = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = FindJava(),
                Arguments = "-jar " + Path.GetTempPath() + "verifile2.jar -w",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                CreateNoWindow = true,
                WindowStyle = ProcessWindowStyle.Hidden,
            }
        };
        p.Start();
        while (!p.StandardOutput.EndOfStream)
        {
            if (p.StandardOutput.ReadLine().Contains("Authentication OK"))
            {
                break;
            }
        }
    }

    public void RelockVF2()
    {
        using StreamWriter sw = new StreamWriter(MasRoot + "/vf2.done", false);
        sw.Write("Verifile 2.0 relock request");
        sw.Close();
    }
}