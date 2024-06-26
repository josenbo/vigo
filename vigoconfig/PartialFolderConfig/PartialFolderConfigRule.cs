﻿using System.Text;
using vigobase;

namespace vigoconfig;

internal class PartialFolderConfigRule(SourceBlockRule block)
{
    public FileRuleActionEnum Action { get; set; } = FileRuleActionEnum.Undefined;
    public FileRuleConditionEnum Condition { get; set; } = FileRuleConditionEnum.Undefined;
    public string? CompareWith { get; set; } 
    public string? ReplaceWith { get; set; }
    public PartialFolderConfigHandling? Handling { get; set; } 
    public SourceBlockRule Block { get; } = block;
    public INameTestAndReplaceHandler? NameTestAndReplaceHandler { get; set; }

    public string Description
    {
        get
        {
            var sb = new StringBuilder(80 + (CompareWith?.Length ?? 0));

            sb.Append("DO ");
            
            switch (Action)
            {
                case FileRuleActionEnum.IgnoreFile:
                    sb.Append("IGNORE");
                    break;
                case FileRuleActionEnum.DeployFile:
                    sb.Append("DEPLOY");
                    break;
                case FileRuleActionEnum.PreviewFile:
                    sb.Append("PREVIEW");
                    break;
                case FileRuleActionEnum.Undefined:
                default:
                    sb.Append("undefined action");
                    break;
            }

            switch (Condition)
            {
                case FileRuleConditionEnum.Unconditional:
                    sb.Append(" ALL FILES");
                    break;
                case FileRuleConditionEnum.MatchName:
                    sb.Append(" IF NAME EQUALS ").Append(CompareWith ?? "missing value");
                    break;
                case FileRuleConditionEnum.MatchPattern:
                    sb.Append(" IF NAME MATCHES ").Append(CompareWith ?? "missing value");
                    break;
                case FileRuleConditionEnum.MatchHandler:
                    sb.Append(" IF NAME IN ").Append(NameTestAndReplaceHandler?.Identification ?? "missing value");
                    break;
                case FileRuleConditionEnum.Undefined:
                default:
                    sb.Append(" undefined condition");
                    break;
            }

            return sb.ToString();
        }
    }

    public void Deconstruct(out FileRuleActionEnum action, out FileRuleConditionEnum condition, out string? compareWith, out string? replaceWith, out PartialFolderConfigHandling? handling)
    {
        action = Action;
        condition = Condition;
        compareWith = CompareWith;
        replaceWith = ReplaceWith;
        handling = Handling;
    }
}