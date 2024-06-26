﻿using System.Diagnostics.CodeAnalysis;
using vigobase;
using vigoconfig;

namespace vigorule;

internal record FileRuleMatchingHandler(
    FileRuleId Id,
    FileRuleActionEnum Action,
    INameTestAndReplaceHandler NameTestAndReplaceHandler, 
    FileHandlingParameters Handling
) : FileRuleConditional(
    Id,
    Action,
    string.Empty, 
    string.Empty,
    Handling
)
{
    internal override FileRuleConditionEnum Condition => FileRuleConditionEnum.MatchHandler;
    internal override bool GetTransformation(
        FileInfo file, 
        bool includePreview,
        [NotNullWhen(true)] out IMutableFileHandling? transformation)
    {
        if (!NameTestAndReplaceHandler.TestName(file.Name, out var derivedFileName))
        {
            transformation = null;
            return false;
        }

        transformation = new FileHandlingImpl(file, Handling, this)
        {
            CanDeploy = (Action == FileRuleActionEnum.DeployFile || (includePreview && Action ==FileRuleActionEnum.PreviewFile)),
            DifferentTargetFileName = derivedFileName
        };
        return true;
    }
}