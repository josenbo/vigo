﻿using System.Diagnostics.CodeAnalysis;
using vigobase;

namespace vigoconfig;

internal record RuleToCopyUnconditional(
    int Index,
    FileTypeEnum FileType,
    FileEncodingEnum SourceFileEncoding,
    FileEncodingEnum TargetFileEncoding,
    FilePermission FilePermission,
    LineEndingEnum LineEnding,
    bool FixTrailingNewline
) : RuleToCopy(
    Index,
    FileType,
    SourceFileEncoding,
    TargetFileEncoding,
    FilePermission,
    LineEnding,
    FixTrailingNewline
)
{
    internal override bool GetTransformation(string filename, out RuleCheckResultEnum result,
        [NotNullWhen(true)] out IDeploymentTransformationReadWrite? transformation)
    {
        throw new NotImplementedException();
    }
}