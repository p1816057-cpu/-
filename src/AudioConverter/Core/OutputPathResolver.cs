using System;
using System.IO;
using AudioConverter.Models;

namespace AudioConverter.Core
{
    public enum OutputResolveAction
    {
        Create,
        Skip
    }

    public sealed class OutputPathResolution
    {
        public string OutputPath { get; set; }

        public OutputResolveAction Action { get; set; }
    }

    public static class OutputPathResolver
    {
        public static OutputPathResolution Resolve(
            string inputPath,
            string outputDirectory,
            OutputFormat format,
            ConflictPolicy policy)
        {
            string directory = string.IsNullOrWhiteSpace(outputDirectory)
                ? Path.GetDirectoryName(inputPath)
                : outputDirectory.Trim();

            string ext = "." + format.ToString().ToLowerInvariant();
            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string candidate = Path.Combine(directory, baseName + ext);

            bool sameAsInput = string.Equals(
                Path.GetFullPath(candidate),
                Path.GetFullPath(inputPath),
                StringComparison.OrdinalIgnoreCase);

            if (!File.Exists(candidate) || (sameAsInput && policy == ConflictPolicy.Overwrite))
            {
                if (sameAsInput)
                {
                    candidate = GetUniqueName(directory, baseName, ext);
                }

                return new OutputPathResolution { OutputPath = candidate, Action = OutputResolveAction.Create };
            }

            switch (policy)
            {
                case ConflictPolicy.Overwrite:
                    return new OutputPathResolution { OutputPath = candidate, Action = OutputResolveAction.Create };
                case ConflictPolicy.Skip:
                    return new OutputPathResolution { OutputPath = candidate, Action = OutputResolveAction.Skip };
                default:
                    return new OutputPathResolution
                    {
                        OutputPath = GetUniqueName(directory, baseName, ext),
                        Action = OutputResolveAction.Create
                    };
            }
        }

        private static string GetUniqueName(string directory, string baseName, string ext)
        {
            for (int i = 1; i < 10000; i++)
            {
                string candidate = Path.Combine(directory, string.Format("{0} ({1}){2}", baseName, i, ext));
                if (!File.Exists(candidate))
                {
                    return candidate;
                }
            }

            return Path.Combine(directory, baseName + "_" + Guid.NewGuid().ToString("N").Substring(0, 6) + ext);
        }
    }
}
