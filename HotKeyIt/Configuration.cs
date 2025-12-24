using Dalamud.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using HotKeyIt.Models;
using HotKeyIt.Services;

namespace HotKeyIt;

[Serializable]
public class Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 0;

    public List<Profile> Profiles { get; set; } = [];
    public Guid SelectedProfileId { get; set; }

    public void EnsureDefaults()
    {
        Profiles ??= [];

        if (Profiles.Count == 0)
        {
            SelectedProfileId = Guid.Empty;
            return;
        }

        if (SelectedProfileId == Guid.Empty || Profiles.All(p => p.Id != SelectedProfileId))
            SelectedProfileId = Profiles[0].Id;
    }

    public Profile? GetSelectedProfile()
        => Profiles.FirstOrDefault(p => p.Id == SelectedProfileId);

    public void Save()
    {
        Service.PluginInterface.SavePluginConfig(this);
    }
}
