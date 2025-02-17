﻿// Copyright (c) Josef Pihrt and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections;
using System.Collections.Generic;

namespace Roslynator.Testing.Text;

internal abstract class LinePositionSpanInfoComparer : IComparer<LinePositionSpanInfo>, IEqualityComparer<LinePositionSpanInfo>, IComparer, IEqualityComparer
{
    public static LinePositionSpanInfoComparer Index { get; } = new LinePositionSpanInfoIndexComparer();

    public static LinePositionSpanInfoComparer IndexDescending { get; } = new LinePositionSpanInfoIndexDescendingComparer();

    public abstract int Compare(LinePositionSpanInfo x, LinePositionSpanInfo y);

    public abstract bool Equals(LinePositionSpanInfo x, LinePositionSpanInfo y);

    public abstract int GetHashCode(LinePositionSpanInfo obj);

    public int Compare(object x, object y)
    {
        if (x == y)
            return 0;

        if (x is null)
            return -1;

        if (y is null)
            return 1;

        if (x is LinePositionSpanInfo a
            && y is LinePositionSpanInfo b)
        {
            return Compare(a, b);
        }

        throw new ArgumentException("", nameof(x));
    }

    new public bool Equals(object x, object y)
    {
        if (x == y)
            return true;

        if (x is null)
            return false;

        if (y is null)
            return false;

        if (x is LinePositionSpanInfo a
            && y is LinePositionSpanInfo b)
        {
            return Equals(a, b);
        }

        throw new ArgumentException("", nameof(x));
    }

    public int GetHashCode(object obj)
    {
        if (obj is null)
            return 0;

        if (obj is LinePositionSpanInfo linePositionInfo)
            return GetHashCode(linePositionInfo);

        throw new ArgumentException("", nameof(obj));
    }

    private class LinePositionSpanInfoIndexComparer : LinePositionSpanInfoComparer
    {
        public override int Compare(LinePositionSpanInfo x, LinePositionSpanInfo y)
        {
            return x.Start.Index.CompareTo(y.Start.Index);
        }

        public override bool Equals(LinePositionSpanInfo x, LinePositionSpanInfo y)
        {
            return x.Start.Index == y.Start.Index;
        }

        public override int GetHashCode(LinePositionSpanInfo obj)
        {
            return obj.Start.Index.GetHashCode();
        }
    }

    private class LinePositionSpanInfoIndexDescendingComparer : LinePositionSpanInfoIndexComparer
    {
        public override int Compare(LinePositionSpanInfo x, LinePositionSpanInfo y)
        {
            return -base.Compare(x, y);
        }
    }
}
