﻿// Copyright (c) Josef Pihrt and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Host;

namespace Roslynator;

internal interface ISyntaxFactsService : ILanguageService
{
    string SingleLineCommentStart { get; }

    bool IsComment(SyntaxTrivia trivia);

    bool IsSingleLineComment(SyntaxTrivia trivia);

    bool IsEndOfLineTrivia(SyntaxTrivia trivia);

    bool IsWhitespaceTrivia(SyntaxTrivia trivia);

    SyntaxTriviaList ParseLeadingTrivia(string text, int offset = 0);

    SyntaxTriviaList ParseTrailingTrivia(string text, int offset = 0);

    bool AreEquivalent(SyntaxTree oldTree, SyntaxTree newTree);

    bool BeginsWithAutoGeneratedComment(SyntaxNode root);

    SyntaxNode GetSymbolDeclaration(SyntaxToken identifier);

    bool IsValidIdentifier(string name);
}
