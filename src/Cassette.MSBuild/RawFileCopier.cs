using System;
using System.IO;
using System.Linq;
using Cassette.IO;
using Cassette.Utilities;

namespace Cassette.MSBuild
{
    class RawFileCopier : IBundleVisitor
    {
        readonly string absoluteSourceDirectory;
        readonly UrlGenerator urlGenerator;
        readonly HashedCompareSet<string> copiedFilenames = new HashedCompareSet<string>(new string[0], StringComparer.OrdinalIgnoreCase);
 
        public RawFileCopier(string absoluteSourceDirectory, string outputDirectory)
        {
            this.absoluteSourceDirectory = absoluteSourceDirectory;
            urlGenerator = new UrlGenerator(
                new FileSystemDirectory(absoluteSourceDirectory),
                new CombinePathWithUrl(outputDirectory),
                ""
            );
        }

        public void Visit(Bundle bundle)
        {
        }

        public void Visit(IAsset asset)
        {
            var references = asset.References.Where(r => r.Type == AssetReferenceType.RawFilename);
            foreach (var reference in references)
            {
                CopyRawFileToOutput(reference.ToPath);
            }
        }

        void CopyRawFileToOutput(string sourceFilename)
        {
            var absoluteSourceFilename = AbsoluteSourcePath(sourceFilename);
            
            if (!File.Exists(absoluteSourceFilename)) return; // TODO: Log this as a warning?
            if (copiedFilenames.Contains(absoluteSourceFilename)) return;

            var outputPath = CreateOutputFilename(sourceFilename);
            CopyFile(absoluteSourceFilename, outputPath);
            copiedFilenames.Add(absoluteSourceFilename);
        }

        void CopyFile(string absoluteSourceFilename, string outputPath)
        {
            EnsureFilenameDirectoryExists(outputPath);
            File.Copy(absoluteSourceFilename, outputPath);
        }

        string CreateOutputFilename(string sourceFilename)
        {
            return urlGenerator.CreateRawFileUrl(sourceFilename);
        }

        string AbsoluteSourcePath(string path)
        {
            path = path.TrimStart('~', '/');
            return Path.Combine(absoluteSourceDirectory, path);
        }

        void EnsureFilenameDirectoryExists(string outputFilename)
        {
            var directory = Path.GetDirectoryName(outputFilename);
            if (directory == null) throw new ArgumentException("Could not determine directory of file " + outputFilename, "outputFilename");
            if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);
        }
    }
}