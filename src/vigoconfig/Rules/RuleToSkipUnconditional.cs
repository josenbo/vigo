﻿using System.Diagnostics.CodeAnalysis;
using vigobase;

namespace vigoconfig;

internal record RuleToSkipUnconditional(int Index) : RuleToSkip(Index)
{
    internal override bool GetTransformation(string filename, out RuleCheckResultEnum result,
        [NotNullWhen(true)] out IDeploymentTransformationReadWrite? transformation)
    {
        throw new NotImplementedException();
    }
}