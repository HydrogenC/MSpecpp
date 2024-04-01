using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace MSpecpp;

public class SettingsCaseFolderModel
{
    public string FolderPath { get; set; }

    public bool Confirmed { get; set; }

    public string[] SelectedSpectrumPaths { get; set; }

    [JsonConstructor]
    public SettingsCaseFolderModel()
    {
    }

    public SettingsCaseFolderModel(CaseFolder folder, string basePath)
    {
        FolderPath = Path.GetRelativePath(basePath, folder.FolderPath);
        Confirmed = folder.Confirmed;
        SelectedSpectrumPaths = folder.SelectedDict
            .Where((x) => x.Value)
            .Select((x) => Path.GetRelativePath(basePath, x.Key)).ToArray();
    }
}

public class SettingsModel
{
    [Obsolete("Global config is deprecated, use per-folder config instead. ")]
    public static string GlobalConfigPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "MSpecppConfig.json");

    public string OpenPath { get; set; }

    public SettingsCaseFolderModel[] CaseFolders { get; set; }
}

[JsonSerializable(typeof(SettingsCaseFolderModel))]
[JsonSerializable(typeof(SettingsModel))]
internal partial class SourceGenerationContext : JsonSerializerContext
{
}