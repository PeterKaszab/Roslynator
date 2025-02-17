﻿// Copyright (c) Josef Pihrt and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System.Diagnostics;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Roslynator.CSharp.Syntax.SyntaxInfoHelpers;

namespace Roslynator.CSharp.Syntax;

/// <summary>
/// Provides information about simple assignment expression.
/// </summary>
[DebuggerDisplay("{DebuggerDisplay,nq}")]
public readonly struct SimpleAssignmentExpressionInfo
{
    private SimpleAssignmentExpressionInfo(
        AssignmentExpressionSyntax assignmentExpression,
        ExpressionSyntax left,
        ExpressionSyntax right)
    {
        AssignmentExpression = assignmentExpression;
        Left = left;
        Right = right;
    }

    /// <summary>
    /// The simple assignment expression.
    /// </summary>
    public AssignmentExpressionSyntax AssignmentExpression { get; }

    /// <summary>
    /// The expression on the left of the assignment operator.
    /// </summary>
    public ExpressionSyntax Left { get; }

    /// <summary>
    /// The expression on the right of the assignment operator.
    /// </summary>
    public ExpressionSyntax Right { get; }

    /// <summary>
    /// The operator of the simple assignment expression.
    /// </summary>
    public SyntaxToken OperatorToken
    {
        get { return AssignmentExpression?.OperatorToken ?? default; }
    }

    /// <summary>
    /// Determines whether this struct was initialized with an actual syntax.
    /// </summary>
    public bool Success
    {
        get { return AssignmentExpression is not null; }
    }

    [DebuggerBrowsable(DebuggerBrowsableState.Never)]
    private string DebuggerDisplay
    {
        get { return SyntaxInfoHelpers.ToDebugString(Success, this, AssignmentExpression); }
    }

    internal static SimpleAssignmentExpressionInfo Create(
        SyntaxNode node,
        bool walkDownParentheses = true,
        bool allowMissing = false)
    {
        return Create(WalkAndCheck(node, walkDownParentheses, allowMissing) as AssignmentExpressionSyntax, walkDownParentheses, allowMissing);
    }

    internal static SimpleAssignmentExpressionInfo Create(
        AssignmentExpressionSyntax assignmentExpression,
        bool walkDownParentheses = true,
        bool allowMissing = false)
    {
        if (assignmentExpression?.Kind() != SyntaxKind.SimpleAssignmentExpression)
            return default;

        ExpressionSyntax left = WalkAndCheck(assignmentExpression.Left, walkDownParentheses, allowMissing);

        if (left is null)
            return default;

        ExpressionSyntax right = WalkAndCheck(assignmentExpression.Right, walkDownParentheses, allowMissing);

        if (right is null)
            return default;

        return new SimpleAssignmentExpressionInfo(assignmentExpression, left, right);
    }
}
