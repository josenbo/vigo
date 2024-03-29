﻿using System.Diagnostics.CodeAnalysis;
using JetBrains.Annotations;
using vigobase;

namespace vigoconfig;

/// <summary>
/// Access to services for reading deployment configuration
/// files and building rulesets for file handling 
/// </summary>
[PublicAPI]
public interface IConfigurationReader
{
    /// <summary>
    /// Try to parse the configuration file content and create a folder
    /// configuration using the provided default values for file handling 
    /// </summary>
    /// <param name="configurationScript">The text content of the deployment configuration file</param>
    /// <param name="configurationFile">The name and relative path of the configuration file or a short identification of the configurations origin</param>
    /// <param name="configurationType">The format of the deployment configuration file</param>
    /// <param name="initialDefaults">Default values for file handling which are defined globally or in a parent folder</param>
    /// <param name="folderConfiguration">If parsed successfully, this out parameter contains a folder configuration. It is null, if parsing fails</param>
    /// <returns>True, if parsed successfully (folderConfiguration is guaranteed not to be null in this case)</returns>
    bool TryParse(string configurationScript, string configurationFile, ConfigurationFileTypeEnum configurationType, FileHandlingParameters initialDefaults, [NotNullWhen(true)] out IFolderConfiguration? folderConfiguration);
}