﻿// Copyright (c) Josef Pihrt and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;

namespace Roslynator.CSharp.SyntaxRewriters;

internal sealed class WhitespaceRemover : CSharpSyntaxRewriter
{
    private WhitespaceRemover(TextSpan? span = null)
    {
        Span = span;
    }

    private static WhitespaceRemover Default { get; } = new();

    public TextSpan? Span { get; }

    public static SyntaxTrivia Replacement { get; } = CSharpFactory.EmptyWhitespace();

    public static WhitespaceRemover GetInstance(TextSpan? span = null)
    {
        if (span is not null)
        {
            return new WhitespaceRemover(span);
        }
        else
        {
            return Default;
        }
    }

    public override SyntaxTrivia VisitTrivia(SyntaxTrivia trivia)
    {
        if (trivia.IsWhitespaceOrEndOfLineTrivia()
            && (Span?.Contains(trivia.Span) != false))
        {
            return Replacement;
        }

        return base.VisitTrivia(trivia);
    }
}
