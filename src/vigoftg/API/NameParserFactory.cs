﻿using JetBrains.Annotations;
using vigobase;

namespace vigoftg;

[PublicAPI]
public static class NameParserFactory
{
    public static INameParser Create(IEnvironmentDescriptor descriptor)
    {
        return new NameParser(descriptor);
    }
}