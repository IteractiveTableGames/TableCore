using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace TableCore.Core.Modules
{
    /// <summary>
    /// Discovers module manifests under a directory and produces descriptors for the lobby to display.
    /// </summary>
    public class ModuleLoader
    {
        private const string ManifestFileName = "module.json";

        private static readonly JsonSerializerOptions SerializerOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };

        public IReadOnlyList<ModuleDescriptor> LoadModules(string modulesRoot)
        {
            if (string.IsNullOrWhiteSpace(modulesRoot))
            {
                return Array.Empty<ModuleDescriptor>();
            }

            string normalizedRoot;
            try
            {
                normalizedRoot = Path.GetFullPath(modulesRoot);
            }
            catch (Exception)
            {
                return Array.Empty<ModuleDescriptor>();
            }

            if (!Directory.Exists(normalizedRoot))
            {
                return Array.Empty<ModuleDescriptor>();
            }

            var descriptors = new List<ModuleDescriptor>();

            foreach (var moduleDirectory in Directory.EnumerateDirectories(normalizedRoot))
            {
                var manifestPath = Path.Combine(moduleDirectory, ManifestFileName);
                if (!File.Exists(manifestPath))
                {
                    continue;
                }

                try
                {
                    using var stream = File.OpenRead(manifestPath);
                    var manifest = JsonSerializer.Deserialize<ModuleManifest>(stream, SerializerOptions);
                    if (manifest is null ||
                        string.IsNullOrWhiteSpace(manifest.ModuleId) ||
                        string.IsNullOrWhiteSpace(manifest.DisplayName))
                    {
                        continue;
                    }

                    var descriptor = new ModuleDescriptor
                    {
                        ModuleId = manifest.ModuleId.Trim(),
                        DisplayName = manifest.DisplayName.Trim(),
                        Summary = manifest.Summary?.Trim() ?? string.Empty,
                        MinPlayers = Math.Max(1, manifest.MinPlayers),
                        MaxPlayers = Math.Max(manifest.MinPlayers, manifest.MaxPlayers),
                        ModulePath = moduleDirectory,
                        IconPath = NormalizeRelativePath(manifest.Icon),
                        EntryScenePath = NormalizeRelativePath(manifest.EntryScene)
                    };

                    if (manifest.Capabilities is not null)
                    {
                        foreach (var (key, value) in manifest.Capabilities)
                        {
                            if (!string.IsNullOrWhiteSpace(key))
                            {
                                descriptor.Capabilities[key] = value;
                            }
                        }
                    }

                    descriptors.Add(descriptor);
                }
                catch (JsonException)
                {
                    // Skip malformed manifests
                }
                catch (IOException)
                {
                    // Skip modules we cannot read
                }
            }

            descriptors.Sort((left, right) =>
                string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));

            return descriptors;
        }

        private static string? NormalizeRelativePath(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return null;
            }

            return path.Replace('\\', '/');
        }

        private sealed class ModuleManifest
        {
            public string? ModuleId { get; set; }
            public string? DisplayName { get; set; }
            public string? Summary { get; set; }
            public int MinPlayers { get; set; } = 1;
            public int MaxPlayers { get; set; } = 4;
            public string? Icon { get; set; }
            public string? EntryScene { get; set; }
            public Dictionary<string, object>? Capabilities { get; set; }
        }
    }
}
