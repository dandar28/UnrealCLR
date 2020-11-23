using System;
using System.Diagnostics;
using System.IO;

public class HotCompiler
{
	static private String pluginName { get { return "UnrealCLR"; } }
	static private String frameworkRootPath { get { return Directory.GetCurrentDirectory() + "/../../../../../UnrealCLR"; } }
	static private String frameworkSourcePath { get { return frameworkRootPath + "/CSharpSource"; } }
	static private String frameworkNativeSourcePluginPath { get { return frameworkRootPath + "/Plugin"; } }
	static private String frameworkNativeSourceManagedPath { get { return frameworkSourcePath + "/Managed"; } }
	static private String frameworkNativeSourceGameSourcePath { get { return frameworkSourcePath + "/Source"; } }

	public String projectPath { get; set; } = "";
	private String projectPluginsPath { get { return projectPath + "/Plugins"; } }
	private String projectPluginFolder { get { return projectPluginsPath + "/" + pluginName; } }
	private String projectCSharpPath { get { return projectPath + "/CSharp"; } }
	private String projectSourcePath { get { return projectCSharpPath + "/Source"; } }
	private String projectManagedPath { get { return projectCSharpPath + "/Managed"; } }
	private String projectManagedRuntimeFolder { get { return projectManagedPath + "/Runtime"; } }
	private String projectManagedFrameworkFolder { get { return projectManagedPath + "/Framework"; } }

	public bool Compile()
	{
		if (!TryCompile())
		{
			Error("Could not compile");
			return false;
		}
		Success("Successfully installed and compiled");
		return true;
	}

	public bool TryCompile()
	{
		Info(Environment.NewLine + "*** TRY INSTALL AND COMPILE ***");

		bool bShouldAskPreBuildSteps = !Ask("Would you just like to compile the game source?");

		if (!ValidateProject())
		{
			return false;
		}

		ShowSeparator();

		if (bShouldAskPreBuildSteps)
		{
			if (!DownloadPlugin())
			{
				Warning("Could not install the plugin");
			}

			ShowSeparator();
		}

		if (!ValidatePlugin())
		{
			return false;
		}

		ShowSeparator();

		if (bShouldAskPreBuildSteps)
		{
			if (!DownloadSource())
			{
				Warning("Could not install the base game source");
			}

			ShowSeparator();

			if (Ask("Would you like to compile the managed runtime?"))
			{
				CompileManagedRuntime();
			}

			ShowSeparator();

			if (Ask("Would you like to compile the framework?"))
			{
				CompileFramework();
			}

			ShowSeparator();
		}

		CompileSourceModules();

		ShowSeparator();

		Success("Done!");

		return true;
	}

	private bool ValidateProject()
	{
		Info(Environment.NewLine + "*** VALIDATE PROJECT ***");

		// Find all project files in the specified project path
		String[] projectFiles = Directory.GetFiles(projectPath, "*.uproject", SearchOption.TopDirectoryOnly);
		if (projectFiles.Length == 0)
		{
			// No any project file found, trigger error and exit
			Error($"Project file not found in \"{ projectPath }\" folder!");
			return false;
		}

		// For each project file found, let's print an info message
		foreach (String projectFile in projectFiles)
		{
			Info($"Project file \"{projectFile}\" found in \"{ projectPath }\" folder!");
		}

		return true;
	}
	private bool ValidatePlugin()
	{
		Info(Environment.NewLine + "*** VALIDATE PLUGIN ***");

		// If the plugin does not exist, trigger an error
		if (!Directory.Exists(projectPluginFolder))
		{
			Error($"{pluginName} plugin is not present!");
			return false;
		}

		Info($"Plugin file \"{pluginName}\" found in \"{ projectPluginsPath }\" folder!");
		return true;
	}

	static private bool CopyFolderWithMessage(String folderDisplayName, String fromFolder, String toFolder, bool bRemovePrevious = false, bool bShouldOverwrite = false)
	{

		// Remove previous installation folder
		if (bRemovePrevious)
		{
			Info($"Removing the previous {folderDisplayName} installation...");

			try
			{
				if (Directory.Exists(toFolder))
				{
					Directory.Delete(toFolder, true);
					Warning($"Deleted previous {folderDisplayName} installation");
				}
			}
			catch (Exception e)
			{
				Warning(e.Message);
			}
		}

		// Copying installation folder to the specified destination
		{
			Info($"Copying {folderDisplayName}...");

			try
			{
				foreach (string directoriesPath in Directory.GetDirectories(fromFolder, "*", SearchOption.AllDirectories))
				{
					try
					{
						Directory.CreateDirectory(directoriesPath.Replace(fromFolder, toFolder, StringComparison.Ordinal));
					}
					catch (Exception e)
					{
						Warning(e.Message);
						continue;
					}
				}

				foreach (string filesPath in Directory.GetFiles(fromFolder, "*.*", SearchOption.AllDirectories))
				{
					try
					{
						File.Copy(filesPath, filesPath.Replace(fromFolder, toFolder, StringComparison.Ordinal), bShouldOverwrite);
					}
					catch (Exception e)
					{
						Warning(e.Message);
						continue;
					}
				}
			}
			catch (Exception e)
			{
				Warning(e.Message);
				return false;
			}

			Success($"Copied {folderDisplayName} successfully!");
		}

		return true;
	}

	private bool DownloadPlugin()
	{
		Info(Environment.NewLine + "*** DOWNLOAD/INSTALL PLUGIN ***");

		if (!Ask("Installation will delete all previous files of the plugin. Do you want to continue?"))
		{
			return true;
		}

		return CopyFolderWithMessage("plugin", frameworkNativeSourcePluginPath, projectPluginFolder, true, true);
	}

	private bool DownloadSource()
	{
		Info(Environment.NewLine + "*** DOWNLOAD/INSTALL SOURCE ***");

		if (Ask("Installation will delete all previous managed framework and runtime source files. Do you want to continue?"))
		{
			if (!CopyFolderWithMessage("managed framework and runtime", frameworkNativeSourceManagedPath, projectManagedPath, true, true))
			{
				return false;
			}
		}

		ShowSeparator();

		if (Ask("Installation will delete all previous game source files. Do you want to continue?"))
		{
			if (!CopyFolderWithMessage("game source", frameworkNativeSourceGameSourcePath, projectSourcePath))
			{
				return true;
			}
		}

		return true;
	}

	private void CompileManagedRuntime()
	{
		Info("Launching compilation of the managed runtime...");

		var runtimeCompilation = Process.Start(new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"publish \"{ projectManagedRuntimeFolder }\" --configuration Release --framework net5.0 --output \"{ projectPluginFolder }/Managed\"",
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
	private void CompileFramework()
	{
		Info("Launching compilation of the framework...");

		var frameworkCompilation = Process.Start(new ProcessStartInfo
		{
			FileName = "dotnet",
			Arguments = $"publish \"{ projectManagedFrameworkFolder }\" --configuration Release --framework net5.0 --output \"{ projectManagedFrameworkFolder }/bin/Release\"",
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
	private void CompileSourceModules()
	{
		Info("Launching compilation for source modules...");

		foreach (var sourceModulePath in Directory.EnumerateDirectories(projectSourcePath))
		{
			string sourceModuleName = sourceModulePath.Remove(0, projectSourcePath.Length);

			Info($"Launching compilation of module [{sourceModuleName}] at [{sourceModulePath}]");

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
				Success($"Successfully compiled module [{sourceModuleName}] at [{sourceModulePath}]");
			}
		}
	}

	static private bool Ask(String Message)
	{
		Warning(Message + " [y/n]");

		Console.ForegroundColor = SuccessColor;
		bool bResponse = (Console.ReadKey(false).Key == ConsoleKey.Y);
		Console.ResetColor();
		Console.Write(Environment.NewLine);

		return bResponse;
	}
	private static void ShowSeparator()
	{
		SpecialMessage("\n\n***\n\n");
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

	private static void SpecialMessage(string message)
	{
		Console.ForegroundColor = SpecialMessageColor;
		Console.WriteLine(message);
		Console.ResetColor();
	}

	private static ConsoleColor ErrorColor = ConsoleColor.Red;
	private static ConsoleColor WarningColor = ConsoleColor.Yellow;
	private static ConsoleColor InfoColor = ConsoleColor.Cyan;
	private static ConsoleColor SuccessColor = ConsoleColor.Green;
	private static ConsoleColor SpecialMessageColor = ConsoleColor.Magenta;
}

public static class HotCompilerProgram
{

	private static void Main(string[] args)
	{
		HotCompiler compiler = new HotCompiler();
		compiler.projectPath = GetProjectPath(args);
		compiler.Compile();

		Console.ReadKey();
		Environment.Exit(0);
	}
	private static void PrepareConsole()
	{
		Console.Title = "UnrealCLR Compilation Tool";

		using StreamReader consoleReader = new StreamReader(Console.OpenStandardInput(8192), Console.InputEncoding, false, bufferSize: 1024);
		Console.SetIn(consoleReader);
	}

	private static String GetProjectPath(string[] args)
	{
		string projectPath = null;
		if (args.Length > 0)
		{
			projectPath = Path.GetDirectoryName(args[0]);
		}
		if (projectPath == null || !Directory.Exists(projectPath))
		{
			projectPath = Directory.GetCurrentDirectory();
		}
		return projectPath;
	}
}