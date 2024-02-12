﻿using vigobase;

namespace vigoconfig;

internal abstract record FileRuleConditional(
    FileRuleId Id,
    FileRuleActionEnum Action,
    string NameToMatch, 
    string NameReplacement,
    FileHandlingParameters Handling
) : FileRule(
    Id,
    Action,
    Handling
);