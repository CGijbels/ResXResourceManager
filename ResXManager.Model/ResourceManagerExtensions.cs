﻿namespace tomenglertde.ResXManager.Model
{
    using System.Collections.Generic;
    using System.Diagnostics.Contracts;
    using System.IO;
    using System.Linq;
    using System.Text.RegularExpressions;

    using JetBrains.Annotations;

    /// <summary>
    /// Resource manager specific extension methods.
    /// </summary>
    public static class ResourceManagerExtensions
    {
        [NotNull]
        public static IList<ProjectFile> GetAllSourceFiles([NotNull] this DirectoryInfo solutionFolder, [NotNull] Configuration configuration, [NotNull] ISourceFileFilter sourceFileFilter)
        {
            Contract.Requires(solutionFolder != null);
            Contract.Requires(configuration != null);
            Contract.Requires(sourceFileFilter != null);
            Contract.Ensures(Contract.Result<IList<ProjectFile>>() != null);

            var sourceFileExclusionFilters = configuration.SourceFileExclusionFilters.Items
                    .Where(sourceFileExclusionFilter => !string.IsNullOrEmpty(sourceFileExclusionFilter.Expression))
                    .Select(sourceFileExclusionFilter => new Regex(sourceFileExclusionFilter.Expression, RegexOptions.IgnoreCase | RegexOptions.Singleline))
                    .ToArray();

            var solutionFolderLength = solutionFolder.FullName.Length + 1;

            var fileInfos = solutionFolder.EnumerateFiles("*.*", SearchOption.AllDirectories);
            Contract.Assume(fileInfos != null);

            var allProjectFiles = fileInfos
                .Where(fileInfo => !sourceFileExclusionFilters.Any(sourceFileExclusionFilter => sourceFileExclusionFilter.IsMatch(fileInfo.FullName)))
                .Select(fileInfo => new ProjectFile(fileInfo.FullName, solutionFolder.FullName, @"<unknown>", null))
                .Where(file => file.IsResourceFile() || sourceFileFilter.IsSourceFile(file))
                .ToArray();

            var fileNamesByDirectory = allProjectFiles.GroupBy(file => file.GetBaseDirectory()).ToArray();

            foreach (var directoryFiles in fileNamesByDirectory)
            {
                var directoryPath = directoryFiles?.Key;

                if (string.IsNullOrEmpty(directoryPath))
                    continue;

                var directory = new DirectoryInfo(directoryPath);
                var project = FindProject(directory, solutionFolder.FullName);

                var projectName = "<no project>";
                var uniqueProjectName = (string)null;

                if (project != null)
                {
                    projectName = Path.ChangeExtension(project.Name, null);

                    var fullProjectName = project.FullName;
                    if (fullProjectName.Length >= solutionFolderLength) // project found is in solution tree
                    {
                        uniqueProjectName = fullProjectName.Substring(solutionFolderLength);
                    }
                }

                foreach (var file in directoryFiles)
                {
                    Contract.Assume(file != null);
                    file.ProjectName = projectName;
                    file.UniqueProjectName = uniqueProjectName;
                }
            }

            return allProjectFiles;
        }

        private static FileInfo FindProject([NotNull] DirectoryInfo directory, [NotNull] string solutionFolder)
        {
            Contract.Requires(directory != null);
            Contract.Requires(solutionFolder != null);

            while ((directory != null) && (directory.FullName.Length >= solutionFolder.Length))
            {
                var projectFiles = directory.EnumerateFiles(@"*.*proj", SearchOption.TopDirectoryOnly);
                Contract.Assume(projectFiles != null);

                var project = projectFiles.FirstOrDefault();
                if (project != null)
                {
                    return project;
                }

                directory = directory.Parent;
            }

            return null;
        }
    }
}
