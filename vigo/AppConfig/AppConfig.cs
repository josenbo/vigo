﻿using vigobase;

namespace vigo;

internal abstract record AppConfig
{
    public abstract CommandEnum Command { get; }
    public abstract void LogObject();
}