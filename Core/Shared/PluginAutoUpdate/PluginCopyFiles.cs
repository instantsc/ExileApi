﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ExileCore.Shared.PluginAutoUpdate
{
    public class PluginCopyFiles
    {
        public static List<string> SettingsDirectoryNames => new List<string> { "settings", "Settings", "config", "Config" };     
        public static List<string> DependenciesDirectoryNames => new List<string> { "libs", "Libs", "lib", "Lib", "packages", "Packages" };
        public static List<string> StaticFilesNames => new List<string> { "images", "Images", "img", "Img", "static", "Static" };


        public static Task CopyTxtAndJsonFromRoot(DirectoryInfo sourceDirectory, DirectoryInfo compiledDirectory)
        {
            return Task.Run(() =>
            {
                var fileExtensionsToCopy = new List<string> { ".txt", ".json" };
                CopyAll(sourceDirectory, compiledDirectory, fileExtensionsToCopy);
            });
        }

        public static List<Task> CopySettings(DirectoryInfo sourceDirectory, DirectoryInfo compiledDirectory)
        {
            return CopyFolder(
                sourceDirectory,
                compiledDirectory,
                SettingsDirectoryNames,
                "_new"
            );            
        }

        public static List<Task> CopyDependencies(DirectoryInfo sourceDirectory, DirectoryInfo compiledDirectory)
        {
            return CopyFolder(
                sourceDirectory,
                compiledDirectory,
                DependenciesDirectoryNames,
                "",
                true
            );
        }

        public static List<Task> CopyStaticFiles(DirectoryInfo sourceDirectory, DirectoryInfo compiledDirectory)
        {
            return CopyFolder(
                sourceDirectory,
                compiledDirectory,
                StaticFilesNames
            );
        }

        private static List<Task> CopyFolder(DirectoryInfo sourceDirectory, DirectoryInfo compiledDirectory, List<string> possibleFolderName, string suffixIfExists = "", bool putFilesIntoRoot = false)
        {
            var sourceFolders = GetDirectoryByNamesSource(sourceDirectory, possibleFolderName);
            if (sourceFolders == null) return null;
            compiledDirectory.Create();
            var compiledFolders = GetDirectoryByNamesCompiled(compiledDirectory, possibleFolderName);

            List<Task> copyTasks = new List<Task>();
            foreach (var sourceFolder in sourceFolders)
            {
                string targetName = sourceFolder.Name;
                if (compiledFolders.Any(c => c.Name.Equals(targetName)) && suffixIfExists != "")
                {
                    targetName = targetName + suffixIfExists;
                }

                string targetPath;
                if (!putFilesIntoRoot)
                {
                    targetPath = Path.Combine(compiledDirectory.FullName, targetName);
                }
                else
                {
                    targetPath = compiledDirectory.FullName;
                }
                var target = new DirectoryInfo(targetPath);

                copyTasks.Add(Task.Run(() => CopyAll(sourceFolder, target)));
            }
            return copyTasks;
        }

        public static DirectoryInfo[] GetDirectoryByNamesCompiled(DirectoryInfo directory, List<string> names)
        {
            var result = directory
                .GetDirectories()
                .Where(d => names.Contains(d.Name))
                .ToArray();
            return result;
        }


        public static DirectoryInfo[] GetDirectoryByNamesSource(DirectoryInfo directory, List<string> names)
        {
            var csprojPath = directory.GetFiles($"{directory.Name}.csproj", SearchOption.AllDirectories).FirstOrDefault();
            if (csprojPath == null) return null;

            var result = csprojPath.Directory
                .GetDirectories()
                .Where(d => names.Contains(d.Name))
                .ToArray();
            return result;
        }

        private static void CopyAll(DirectoryInfo source, DirectoryInfo target, List<string> limitFileExtensions = null)
        {
            Directory.CreateDirectory(target.FullName);

            // Copy each file into the new directory.
            foreach (FileInfo fi in source.GetFiles())
            {
                /*if (limitFileExtensions != null && !limitFileExtensions.Contains(fi.Extension)) continue;*/
                fi.CopyTo(Path.Combine(target.FullName, fi.Name), true);
            }

            // Copy each subdirectory using recursion.
            foreach (DirectoryInfo diSourceSubDir in source.GetDirectories())
            {
                DirectoryInfo nextTargetSubDir =
                    target.CreateSubdirectory(diSourceSubDir.Name);
                CopyAll(diSourceSubDir, nextTargetSubDir);
            }
        }
    }
}
