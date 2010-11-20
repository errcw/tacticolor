using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using System.IO;

using Microsoft.Xna.Framework.Content.Pipeline;

namespace StrategyPipeline
{
    /// <summary>
    /// A passthrough importer that returns to the processor the manifest path.
    /// </summary>
    [ContentImporter(".manifest", DisplayName = "Manifest Importer", DefaultProcessor = "ManifestProcessor")]
    public class ManifestImporter : ContentImporter<string>
    {
        public override string Import(string filename, ContentImporterContext context)
        {
            return filename;
        }
    }

    /// <summary>
    /// A processor that consumes a filename and returns a list of files in the
    /// content project being built or copied to the output directory.
    /// </summary>
    [ContentProcessor(DisplayName = "Manifest Processor")]
    public class ManifestProcessor : ContentProcessor<string, List<string>>
    {
        public override List<string> Process(string input, ContentProcessorContext context)
        {
            // assume the manifest locates the root of the content project
            string contentDirectory = Path.GetDirectoryName(input);
            string[] contentProjects = Directory.GetFiles(contentDirectory, "*.contentproj");
            if (contentProjects.Length != 1)
            {
                throw new InvalidOperationException("Could not locate content project.");
            }

            // rebuild the manifest whenever the content project is modified
            context.AddDependency(contentProjects[0]);

            List<string> files = new List<string>();

            XDocument document = XDocument.Load(contentProjects[0]);
            XNamespace xmlns = document.Root.Attribute("xmlns").Value;

            string contentRootDirectory = document.Descendants(xmlns + "ContentRootDirectory").First().Value;

            // include the assets compiled to XNB
            var compiledAssets = document.Descendants(xmlns + "Compile");
            foreach (var asset in compiledAssets)
            {
                string name = asset.Descendants(xmlns + "Name").First().Value;
                string includePath = asset.Attribute("Include").Value;

                // skip the manifest
                if (includePath.EndsWith(".manifest"))
                {
                    continue;
                }

                if (includePath.Contains(Path.DirectorySeparatorChar))
                {
                    string directory = Path.GetDirectoryName(includePath);
                    string assetPath = Path.Combine(directory, name);
                    files.Add(assetPath);
                }
                else
                {
                    files.Add(name);
                }
            }

            // include the assets copied to the output directory
            var copiedAssets = from node in document.Descendants(xmlns + "ItemGroup").Descendants()
                               where node.Descendants(xmlns + "CopyToOutputDirectory").Count() > 0 &&
                                     node.Descendants(xmlns + "CopyToOutputDirectory").First().Value != "None"
                               select Path.Combine(contentRootDirectory, node.Attribute("Include").Value);
            files.AddRange(copiedAssets);

            // override the manifest with the list for debugging
            using (FileStream stream = new FileStream(input, FileMode.Create, FileAccess.Write))
            {
                using (StreamWriter writer = new StreamWriter(stream))
                {
                    foreach (var file in files)
                    {
                        writer.WriteLine(file);
                    }
                }
            }

            // return the list to be serialized
            return files;
        }
    }
}
