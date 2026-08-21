using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Finnrr.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Finnrr;

/// <summary>
/// Lyrics plugin.
/// </summary>
public class FinnrrPlugin : BasePlugin<PluginConfiguration>, IHasWebPages
{
    /// <summary>
    /// Initializes a new instance of the <see cref="FinnrrPlugin"/> class.
    /// </summary>
    /// <param name="applicationPaths">Instance of the <see cref="IApplicationPaths"/>.</param>
    /// <param name="xmlSerializer">Instance of the <see cref="IXmlSerializer"/>.</param>
    public FinnrrPlugin(IApplicationPaths applicationPaths, IXmlSerializer xmlSerializer)
        : base(applicationPaths, xmlSerializer)
    {
        Instance = this;
        CompetingPluginCleanup.Run(applicationPaths);
    }

    /// <inheritdoc />
    public override string Name => "Finnrr Lyrics";

    /// <inheritdoc />
    public override Guid Id => Guid.Parse("2186e1c8-cca5-4b3e-bd2a-0535b1170b15");

    /// <summary>
    /// Gets the current plugin instance.
    /// </summary>
    public static FinnrrPlugin? Instance { get; private set; }

    /// <inheritdoc />
    public IEnumerable<PluginPageInfo> GetPages()
    {
        yield return new PluginPageInfo
        {
            Name = Name,
            EmbeddedResourcePath = $"{GetType().Namespace}.Configuration.config.html"
        };
    }
}
