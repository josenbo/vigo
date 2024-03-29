﻿using System.Text.RegularExpressions;

namespace vigobase;

public record FileHandlingParameters(
    UnixFileMode StandardModeForFiles,
    UnixFileMode StandardModeForDirectories,
    FileTypeEnum FileType,
    FileEncodingEnum SourceFileEncoding,
    FileEncodingEnum TargetFileEncoding,
    LineEndingEnum LineEnding,
    FilePermission Permissions,
    bool FixTrailingNewline,
    Regex? ValidCharsRegex,
    IReadOnlyList<string> Targets);
    