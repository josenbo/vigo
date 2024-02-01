﻿using Serilog.Events;

namespace vigo;

internal record ConfigurationCheckCommit(
    DirectoryInfo RepositoryRoot, 
    string DeploymentConfigFileName,
    DirectoryInfo TemporaryDirectory,
    FileInfo? Logfile,
    LogEventLevel LogLevel
) : Configuration(
    RepositoryRoot, 
    DeploymentConfigFileName,
    TemporaryDirectory,
    Logfile,
    LogLevel
)
{
    public override CommandEnum Command => CommandEnum.CheckCommit;
}