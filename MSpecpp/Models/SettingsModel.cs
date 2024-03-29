using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;

namespace MSpecpp;

public class SettingsCaseFolderModel
{
    public string FolderPath { get; set; }

    public string[] SelectedSpectrumPaths { get; set; }

    [JsonConstructor]
    public SettingsCaseFolderModel()
    {
        
    }
    
    public SettingsCaseFolderModel(CaseFolder folder)
    {
        FolderPath = folder.FolderPath;
        SelectedSpectrumPaths = folder.SelectedDict
            .Where((x) => x.Value).Select((x) => x.Key).ToArray();
    }
}

public class SettingsModel
{
    public static string DefaultConfigPath => Path.Combine(
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