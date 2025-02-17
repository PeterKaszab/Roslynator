﻿// Copyright (c) Josef Pihrt and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;

namespace Roslynator.Documentation;

public class DocumentationContext
{
    private readonly Func<DocumentationContext, DocumentationWriter> _createWriter;
    private ImmutableHashSet<(INamespaceSymbol, string)> _commonNamespacesAsText;

    public DocumentationContext(
        DocumentationModel documentationModel,
        DocumentationUrlProvider urlProvider,
        DocumentationOptions options,
        Func<DocumentationContext, DocumentationWriter> createWriter,
        DocumentationResources resources = null,
        SourceReferenceProvider sourceReferenceProvider = null,
        IEnumerable<INamespaceSymbol> commonNamespaces = null)
    {
        DocumentationModel = documentationModel;
        UrlProvider = urlProvider;
        Options = options;
        Resources = resources ?? DocumentationResources.Default;
        SourceReferenceProvider = sourceReferenceProvider;

        CommonNamespaces = commonNamespaces?.ToImmutableHashSet(MetadataNameEqualityComparer<INamespaceSymbol>.Instance)
            ?? ImmutableHashSet<INamespaceSymbol>.Empty;

        _createWriter = createWriter;
    }

    public DocumentationModel DocumentationModel { get; }

    public DocumentationUrlProvider UrlProvider { get; }

    public DocumentationOptions Options { get; }

    public DocumentationResources Resources { get; }

    public SourceReferenceProvider SourceReferenceProvider { get; }

    public ImmutableHashSet<INamespaceSymbol> CommonNamespaces { get; }

    internal ImmutableHashSet<(INamespaceSymbol symbol, string displayString)> CommonNamespacesAsText
    {
        get
        {
            return _commonNamespacesAsText ??= CommonNamespaces
                .Select(f => (f, f.ToDisplayString(TypeSymbolDisplayFormats.Name_ContainingTypes_Namespaces_GlobalNamespace_OmittedAsContaining)))
                .ToImmutableHashSet();
        }
    }

    public DocumentationWriter CreateWriter()
    {
        return _createWriter(this);
    }
}
