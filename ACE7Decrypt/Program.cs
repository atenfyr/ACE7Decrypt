using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UAssetAPI;

namespace ACE7Decrypt
{
	public enum CommandLineFlags
    {
		Quiet,
		NoMagic,
		Nesting,
		TestMode,
		Credits
    }

    public class Program
    {
		internal static Dictionary<string, CommandLineFlags> GlobalFlagMap = new Dictionary<string, CommandLineFlags>()
		{
			{ "--quiet", CommandLineFlags.Quiet },
			{ "-Q", CommandLineFlags.Quiet },
			{ "--magic", CommandLineFlags.NoMagic },
			{ "-M", CommandLineFlags.NoMagic },
			{ "--recursive", CommandLineFlags.Nesting },
			{ "-R", CommandLineFlags.Nesting },
			{ "--test", CommandLineFlags.TestMode },
			{ "-T", CommandLineFlags.TestMode },
			{ "--credits", CommandLineFlags.Credits },
			{ "-C", CommandLineFlags.Credits }
		};

		internal static Dictionary<CommandLineFlags, bool> EnabledFlags = new Dictionary<CommandLineFlags, bool>();

		internal static void EnableFlag(CommandLineFlags flag)
        {
			EnabledFlags[flag] = true;
        }

		internal static bool IsFlagEnabled(CommandLineFlags flag)
        {
			return EnabledFlags.ContainsKey(flag) && EnabledFlags[flag];
        }

		internal static bool VerifyBinaryEquality(string file1, string file2)
		{
			int file1byte;
			int file2byte;
			FileStream fs1;
			FileStream fs2;

			if (file1 == file2) return true;

			fs1 = new FileStream(file1, FileMode.Open);
			fs2 = new FileStream(file2, FileMode.Open);

			if (fs1.Length != fs2.Length)
			{
				fs1.Close();
				fs2.Close();
				return false;
			}

			do
			{
				file1byte = fs1.ReadByte();
				file2byte = fs2.ReadByte();
			}
			while ((file1byte == file2byte) && (file1byte != -1));

			fs1.Close();
			fs2.Close();

			return (file1byte - file2byte) == 0;
		}

		internal static uint GetFileSignature(string path)
		{
			byte[] buffer = new byte[4];
			using (FileStream fs = new FileStream(path, FileMode.Open, FileAccess.Read))
			{
				var bytes_read = fs.Read(buffer, 0, buffer.Length);
				fs.Close();
			}
			return BitConverter.ToUInt32(buffer, 0);
		}

		internal static string GetDirectoryName(string path, string cwd = null)
        {
			if (cwd == null) cwd = Directory.GetCurrentDirectory();
			return Path.IsPathRooted(path) ? Path.GetDirectoryName(path) : Path.GetDirectoryName(Path.Combine(cwd, path));
		}

		internal static void ShowHeader()
        {
			System.Reflection.Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
			System.Diagnostics.FileVersionInfo fvi = System.Diagnostics.FileVersionInfo.GetVersionInfo(assembly.Location);

			Console.WriteLine("ACE7Decrypt v" + fvi.FileVersion + " - Decrypts Ace Combat 7 PC assets.");
			Console.WriteLine();
		}

		public static void Main(string[] args)
		{
			foreach (string arg in args)
			{
				if (arg.Length >= 1 && arg[0] == '-')
				{
					if (GlobalFlagMap.ContainsKey(arg)) EnableFlag(GlobalFlagMap[arg]);
				}
			}

			if (IsFlagEnabled(CommandLineFlags.Credits))
			{
				ShowHeader();
				Console.WriteLine("Copyright: (C) 2022 atenfyr");
				Console.WriteLine("License: MIT License, see LICENSE.md file");
				return;
			}

			if (args.Length < 3)
			{
				ShowHeader();
				Console.WriteLine("Usage: ACE7Decrypt [options ...] <encrypt/decrypt> <input asset> <output asset>");
				Console.WriteLine("Ensure that the matching uexp file is present in the same directory on use.");
				Console.WriteLine("NOTE: After encrypting a file, it cannot be renamed. The name of the file is used in the decryption algorithm.");
				Console.WriteLine();
				Console.WriteLine("Options:");
				Console.WriteLine("-Q, --quiet\t\tdisables output");
				Console.WriteLine("-M, --magic\t\tdisables file signature check");
				Console.WriteLine("-R, --recursive\t\tenables operating on files in subfolders");
				Console.WriteLine("-T, --test\t\ttest mode, verifies that algorithm works on provided files");
				Console.WriteLine("-C, --credits\t\tdisplays credits");
				Console.WriteLine();
				Console.WriteLine("Example: ACE7Decrypt decrypt plwp_6aam_a0.uasset plwp_6aam_a0_NEW.uasset");
				Console.WriteLine("         ACE7Decrypt -R encrypt *.umap C:\\*.umap");
				Console.WriteLine("         ACE7Decrypt -Q decrypt *.uasset *_NEW.uasset");
				return;
			}

			var mode = args[args.Length - 3];
			if (mode != "decrypt" && mode != "encrypt")
			{
				ShowHeader();
				Console.WriteLine("Error: Invalid mode \"" + mode + "\" specified. Must be \"encrypt\" or \"decrypt\"");
				return;
			}

			if (!IsFlagEnabled(CommandLineFlags.Quiet)) ShowHeader();

			var inputData = args[args.Length - 2];
			var outputData = args[args.Length - 1];

			Stopwatch stopWatch = new Stopwatch();
			stopWatch.Start();

			int numParsed = 0;
			var decryptor = new AC7Decrypt();
			foreach (var path in Directory.GetFiles(GetDirectoryName(inputData), Path.GetFileName(inputData), IsFlagEnabled(CommandLineFlags.Nesting) ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly))
            {
				string targPath = outputData.Replace("*", Path.GetFileNameWithoutExtension(path));
				string outputPath = Path.Combine(GetDirectoryName(targPath, GetDirectoryName(path)), Path.GetFileName(targPath));

				if (IsFlagEnabled(CommandLineFlags.TestMode))
				{
					if (numParsed > 0 && !IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine();

					File.Copy(path, path + ".bak", true);
					File.Copy(Path.ChangeExtension(path, "uexp"), Path.ChangeExtension(path, "uexp") + ".bak", true);
					if (!IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine("Created backup of file: " + Path.GetFileName(path));

					if (mode == "encrypt")
                    {
						decryptor.Encrypt(path, path);
						if (!IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine("Encrypted file: " + Path.GetFileName(path));
						decryptor.Decrypt(path, path);
						if (!IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine("Decrypted file: " + Path.GetFileName(path));
					}
					else if (mode == "decrypt")
                    {
						decryptor.Decrypt(path, path);
						if (!IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine("Decrypted file: " + Path.GetFileName(path));
						decryptor.Encrypt(path, path);
						if (!IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine("Encrypted file: " + Path.GetFileName(path));
					}

					bool didVerify = VerifyBinaryEquality(path, path + ".bak") && VerifyBinaryEquality(Path.ChangeExtension(path, "uexp"), Path.ChangeExtension(path, "uexp") + ".bak");
					if (!IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine(didVerify ? "Verified binary equality" : "FAILED TO VERIFY BINARY EQUALITY!");
					if (didVerify)
					{
						File.Delete(path + ".bak");
						File.Delete(Path.ChangeExtension(path, "uexp") + ".bak");
						if (!IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine("Deleted backup files");
					}

					numParsed++;
				}
				else if (mode == "encrypt")
				{
					if (!IsFlagEnabled(CommandLineFlags.NoMagic) && GetFileSignature(path) == UAsset.ACE7_MAGIC)
					{
						if (!IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine("Skipped encrypted file: " + Path.GetFileName(path));
						continue;
					}
					decryptor.Encrypt(path, outputPath);
					if (!IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine("Encrypted file: " + Path.GetFileName(path));
					numParsed++;
				}
				else if (mode == "decrypt")
				{
					if (!IsFlagEnabled(CommandLineFlags.NoMagic) && GetFileSignature(path) != UAsset.ACE7_MAGIC)
					{
						if (!IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine("Skipped unencrypted file: " + Path.GetFileName(path));
						continue;
					}
					decryptor.Decrypt(path, outputPath);
					if (!IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine("Decrypted file: " + Path.GetFileName(path));
					numParsed++;
				}
			}

			stopWatch.Stop();
			if (!IsFlagEnabled(CommandLineFlags.Quiet)) Console.WriteLine("\nDone! Parsed " + numParsed + " files in " + ((double)stopWatch.Elapsed.Ticks / TimeSpan.TicksPerMillisecond) + " ms.");
		}
	}
}
