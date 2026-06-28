// Author: Deci | Project: SmartImage.Lib | Name: LinuxOSIntegration.cs
// Date: 2024/12/04 @ 22:12:30

using System.Runtime.Versioning;
using Novus.OS;
using SmartImage.Lib.Utilities.Diagnostics;

// ReSharper disable InconsistentNaming

namespace SmartImage.Lib.Utilities.Integration;

[SupportedOSPlatform(Common.OS_LINUX)]
public sealed class LinuxOSIntegration : BaseOSIntegration
{

	public override string LaunchArgs { get; } = R1.Linux_Launch_Args;

	public override string ProgramFilesPath => null;

	public override string AppDataPath => null;


	public override bool IsContextMenuAdded => File.Exists(DesktopFile);

	public override string ChromePath => null;

	public override string FirefoxPath => null;

	public static readonly string DesktopFile = Path.Combine(R1.Linux_Applications_Dir, R1.Linux_Desktop_File);

	public override bool? AddToPath(bool option)
	{
		return null;
	}

	public override bool? HandleContextMenu(bool option, string args)
	{
		if (!FileSystem.IsRoot) {
			throw new SmartImageException("Root permissions required");

		}

		args ??= R1.Linux_Launch_Args;

		if (option) {
			string dsk = $"""
			              [Desktop Entry]

			              Type=Application
			              Version=1.0
			              Name=SmartImage
			              Terminal=true
			              Exec={Executable} {args}
			              """;
			File.WriteAllText(DesktopFile, dsk);

		}
		else {
			if (IsContextMenuAdded) {
				File.Delete(DesktopFile);
			}
		}

		// Console.WriteLine(Path.GetFullPath(s));
		// Console.ReadLine();

		// File.WriteAllText("~/.local/share/nautilus/scripts/smartimage.desktop", dsk);

		return true;
	}

	public override void FlashNotify(nint fd) { }

}