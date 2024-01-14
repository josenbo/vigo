namespace vigocfg;

public interface IDatabaseScripts
{
    DirectoryInfo SourceFolder { get; }
    DirectoryInfo? TargetFolder { get; }
    bool CleanTargetFolder { get; }
    FileTypeEnum SourceFileType { get; }
    FileEncodingEnum SourceEncoding { get; }
    FilePermissionValue TargetFilePermission { get; }
    FileEncodingEnum TargetEncoding { get; }
    LineEndingEnum TargetLineEnding { get; }
    bool TargetAppendFinalNewline { get; }
}