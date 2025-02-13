﻿// Copyright (c) Josef Pihrt and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;

namespace Roslynator;

internal static class EquivalenceKey
{
    private const string Prefix = "Roslynator" + Separator;

    private const string Separator = ".";

    public static string Create(RefactoringDescriptor descriptor, string additionalKey1 = null, string additionalKey2 = null)
    {
        return Create(descriptor.Id, additionalKey1, additionalKey2);
    }

    public static string Create(Diagnostic diagnostic, string additionalKey1 = null, string additionalKey2 = null)
    {
        return Create(diagnostic.Id, additionalKey1, additionalKey2);
    }

    public static string Create(string key, string additionalKey1 = null, string additionalKey2 = null)
    {
        Debug.Assert(!string.IsNullOrEmpty(key));

        if (additionalKey1 is not null)
        {
            if (additionalKey2 is not null)
            {
                return Prefix + key + Separator + additionalKey1 + Separator + additionalKey2;
            }
            else
            {
                return Prefix + key + Separator + additionalKey1;
            }
        }
        else if (additionalKey2 is not null)
        {
            return Prefix + key + Separator + additionalKey2;
        }
        else
        {
            return Prefix + key;
        }
    }

    internal static string Join(string value1, string value2)
    {
        if (value1 is not null)
        {
            if (value2 is not null)
            {
                return value1 + Separator + value2;
            }
            else
            {
                return value1;
            }
        }
        else if (value2 is not null)
        {
            return value2;
        }
        else
        {
            return "";
        }
    }
}
