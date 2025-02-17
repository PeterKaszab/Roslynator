﻿// Copyright (c) Josef Pihrt and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Roslynator;

internal class AsyncMethodNameGenerator : NameGenerator
{
    public override string EnsureUniqueName(string baseName, IEnumerable<string> reservedNames, bool isCaseSensitive = true)
    {
        int suffix = 1;

        string name = baseName + "Async";

        while (!IsUniqueName(name, reservedNames, isCaseSensitive))
        {
            suffix++;
            name = baseName + suffix.ToString() + "Async";
        }

        return name;
    }

    public override string EnsureUniqueName(string baseName, ImmutableArray<ISymbol> symbols, bool isCaseSensitive = true)
    {
        int suffix = 1;

        string name = baseName + "Async";

        while (!IsUniqueName(name, symbols, isCaseSensitive))
        {
            suffix++;
            name = baseName + suffix.ToString() + "Async";
        }

        return name;
    }
}
