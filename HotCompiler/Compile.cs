using System;
using System.Diagnostics;
using System.IO;

public static class Install {
	private static void Main(string[] args) {
		Console.Title = "UnrealCLR Compilation Tool";

		using StreamReader consoleReader = new StreamReader(Console.OpenStandardInput(8192), Console.InputEncoding, false, bufferSize: 1024);

		Console.SetIn(consoleReader);

		string projectPath = null;
		if (args.Length > 0)
		{
			projectPath = Path.GetDirectoryName(args[0]);
		}
		if (projectPath == null || !Directory.Exists(projectPath))
		{
			projectPath = Directory.GetCurrentDirectory();
		}

		string sourcePath = projectPath + "/CSharp/Source";
		string managedPath = projectPath + "/CSharp";

		if (Directory.GetFiles(projectPath, "*.uproject", SearchOption.TopDirectoryOnly).Length == 0)
		{
			Error($"Project file not found in \"{ projectPath }\" folder!");
			return;
		}

		Console.WriteLine($"Project file found in \"{ projectPath }\" folder!");

		if (!Directory.Exists(projectPath + "/Plugins/UnrealCLR"))
		{
			Error("UnrealCLR plugin is not present!");
			return;
		}

		// \todo : ask if the user wants to (re)compile the managed runtime and the framework
		CompileManagedRuntime(managedPath, projectPath);
		CompileFramework(managedPath, projectPath);
		CompileSourceModules(sourcePath, projectPath);

		Success("Done!");
		
		Console.ReadKey();
		Environment.Exit(0);
	}

	private static void CompileManagedRuntime(String sourcePath, String projectPath)
	{
		Info("Launching compilation of the managed runtime...");

		var runtimeCompilation = Process.Start(new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"publish \"{ sourcePath }/Managed/Runtime\" --configuration Release --framework net5.0 --output \"{ projectPath }/Plugins/UnrealCLR/Managed\"",
			CreateNoWindow = false,
			UseShellExecute = false
		});

		runtimeCompilation.WaitForExit();

		if (runtimeCompilation.ExitCode != 0)
		{
			Error("Compilation of the runtime was finished with an error (Exit code: " + runtimeCompilation.ExitCode + ")!");
		}
		else
		{
			Success("Successfully compiled managed runtime!");
		}
	}
	private static void CompileFramework(String sourcePath, String projectPath)
	{
		Info("Launching compilation of the framework...");

		var frameworkCompilation = Process.Start(new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"publish \"{ sourcePath }/Managed/Framework\" --configuration Release --framework net5.0 --output \"{ sourcePath }/Managed/Framework/bin/Release\"",
			CreateNoWindow = false,
			UseShellExecute = false
		});

		frameworkCompilation.WaitForExit();

		if (frameworkCompilation.ExitCode != 0)
		{
			Error("Compilation of the framework was finished with an error (Exit code: " + frameworkCompilation.ExitCode + ")!");
		}
		else
		{
			Success("Successfully compiled framework!");
		}
	}
	private static void CompileSourceModules(String sourcePath, String projectPath)
	{
		Info("Launching compilation for source modules...");

		foreach (var sourceModulePath in Directory.EnumerateDirectories(sourcePath))
		{
			string sourceModuleName = sourceModulePath.Remove(0, sourcePath.Length);

			Console.ForegroundColor = InfoColor;
			Console.WriteLine("Launching compilation of module [{0}] at [{1}]", sourceModuleName, sourceModulePath);
			Console.ResetColor();

			var moduleCompilation = Process.Start(new ProcessStartInfo
			{
				FileName = "dotnet",
				Arguments = $"publish \"{ sourceModulePath }/\" --configuration Release --framework net5.0 --output \"{ projectPath }/Managed/{ sourceModuleName }\"",
				CreateNoWindow = false,
				UseShellExecute = false
			});

			moduleCompilation.WaitForExit();

			if (moduleCompilation.ExitCode != 0)
			{
				Warning("Compilation of the source module " + sourceModuleName + " was finished with an error (Exit code: " + moduleCompilation.ExitCode + ")!");
			}
			else
			{
				Console.ForegroundColor = SuccessColor;
				Console.WriteLine("Successfully compiled module [{0}] at [{1}]", sourceModuleName, sourceModulePath);
				Console.ResetColor();
			}
		}
	}

	private static void Error(string message) {
		Console.ForegroundColor = ErrorColor;
		Console.WriteLine(message);
		Console.ResetColor();
		Console.ReadKey();
		Environment.Exit(-1);
	}

	private static void Warning(string message)
	{
		Console.ForegroundColor = WarningColor;
		Console.WriteLine(message);
		Console.ResetColor();
	}

	private static void Info(string message)
	{
		Console.ForegroundColor = InfoColor;
		Console.WriteLine(message);
		Console.ResetColor();
	}

	private static void Success(string message)
	{
		Console.ForegroundColor = SuccessColor;
		Console.WriteLine(message);
		Console.ResetColor();
	}

	private static ConsoleColor ErrorColor = ConsoleColor.Red;
	private static ConsoleColor WarningColor = ConsoleColor.Yellow;
	private static ConsoleColor InfoColor = ConsoleColor.Cyan;
	private static ConsoleColor SuccessColor = ConsoleColor.Green;
}
