﻿using System.Runtime.Serialization;
using vigobase;

namespace vigorule;

public class FolderConfigDataHead
{
    [DataMember(Name = "KeepEmptyFolder")]   
    public bool? KeepEmptyFolder { get; set; }

    [DataMember(Name = "ValidChars")]   
    public string? ValidCharacters { get; set; }

    [DataMember(Name = "FileType")]   
    public string? FileType { get; set; }

    [DataMember(Name = "StoredWithEncoding")]   
    public string? SourceFileEncoding { get; set; }

    [DataMember(Name = "DeployWithEncoding")]   
    public string? TargetFileEncoding { get; set; }

    [DataMember(Name = "Newline")]   
    public string? LineEnding { get; set; }

    [DataMember(Name = "FileMode")]   
    public string? FilePermission { get; set; }

    [DataMember(Name = "FixTrailingNewline")]   
    public bool? FixTrailingNewline { get; set; }
    
    [DataMember(Name = "TargetList")]   
    public string? Targets { get; set; }
    
    [DataMember(Name = "Rules")]   
    public List<FolderConfigDataRule> Rules { get; } = [];

    internal FileHandlingParameters GetLocalDefaults(FileHandlingParameters globalDefaults)
    {
        var retval = globalDefaults;

        if (FileType is not null && FileTypeEnumHelper.TryParse(FileType, out var fileType))
            retval = retval with { FileType = fileType.Value };

        if (SourceFileEncoding is not null && FileEncodingEnumHelper.TryParse(SourceFileEncoding, out var sourceFileEncoding))
            retval = retval with { SourceFileEncoding = sourceFileEncoding.Value };
        
        if (TargetFileEncoding is not null && FileEncodingEnumHelper.TryParse(TargetFileEncoding, out var targetFileEncoding))
            retval = retval with { TargetFileEncoding = targetFileEncoding.Value };

        if (LineEnding is not null && LineEndingEnumHelper.TryParse(LineEnding, out var lineEnding))
            retval = retval with { LineEnding = lineEnding.Value };

        if (FilePermission is not null && vigobase.FilePermission.TryParse(FilePermission, out var filePermission))
            retval = retval with { Permissions = filePermission };

        if (FixTrailingNewline.HasValue)
            retval = retval with { FixTrailingNewline = FixTrailingNewline.Value };

        if (ValidCharacters is not null)
            retval = retval with
            {
                ValidCharsRegex = ValidCharactersHelper.ParseConfiguration(ValidCharacters)
            };

        // ReSharper disable once InvertIf
        if (Targets is not null)
        {
            var targets = DeploymentTargetHelper.ParseTargets(Targets).ToList();

            retval = retval with { Targets = targets };
        }
        
        return retval;
    }
}