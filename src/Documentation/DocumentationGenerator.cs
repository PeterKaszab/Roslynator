﻿// Copyright (c) Josef Pihrt and Contributors. Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;

namespace Roslynator.Documentation;

public class DocumentationGenerator
{
    private ImmutableArray<RootDocumentationParts> _enabledAndSortedRootParts;
    private ImmutableArray<NamespaceDocumentationParts> _enabledAndSortedNamespaceParts;
    private ImmutableArray<TypeDocumentationParts> _enabledAndSortedTypeParts;
    private ImmutableArray<MemberDocumentationParts> _enabledAndSortedMemberParts;

    public DocumentationGenerator(DocumentationContext context)
    {
        Context = context;
    }

    public DocumentationContext Context { get; }

    public DocumentationModel DocumentationModel => Context.DocumentationModel;

    public DocumentationOptions Options => Context.Options;

    public DocumentationResources Resources => Context.Resources;

    public DocumentationUrlProvider UrlProvider => Context.UrlProvider;

    public SourceReferenceProvider SourceReferenceProvider => Context.SourceReferenceProvider;

    public virtual IComparer<RootDocumentationParts> RootPartComparer
    {
        get { return RootDocumentationPartComparer.Instance; }
    }

    public virtual IComparer<NamespaceDocumentationParts> NamespacePartComparer
    {
        get { return NamespaceDocumentationPartComparer.Instance; }
    }

    public virtual IComparer<TypeDocumentationParts> TypePartComparer
    {
        get { return TypeDocumentationPartComparer.Instance; }
    }

    public virtual IComparer<MemberDocumentationParts> MemberPartComparer
    {
        get { return MemberDocumentationPartComparer.Instance; }
    }

    internal ImmutableArray<RootDocumentationParts> EnabledAndSortedRootParts
    {
        get
        {
            if (_enabledAndSortedRootParts.IsDefault)
            {
                IEnumerable<RootDocumentationParts> parts = Enum.GetValues(typeof(RootDocumentationParts))
                    .Cast<RootDocumentationParts>()
                    .Where(f => f != RootDocumentationParts.None
                        && f != RootDocumentationParts.All
                        && (Options.IgnoredRootParts & f) == 0);

                if (parts.Contains(RootDocumentationParts.Namespaces)
                    && parts.Contains(RootDocumentationParts.Types))
                {
                    parts = parts.Where(f => f != RootDocumentationParts.Namespaces);
                }

                _enabledAndSortedRootParts = parts
                    .OrderBy(f => f, RootPartComparer)
                    .ToImmutableArray();
            }

            return _enabledAndSortedRootParts;
        }
    }

    internal ImmutableArray<NamespaceDocumentationParts> EnabledAndSortedNamespaceParts
    {
        get
        {
            if (_enabledAndSortedNamespaceParts.IsDefault)
            {
                _enabledAndSortedNamespaceParts = Enum.GetValues(typeof(NamespaceDocumentationParts))
                    .Cast<NamespaceDocumentationParts>()
                    .Where(f => f != NamespaceDocumentationParts.None
                        && f != NamespaceDocumentationParts.All
                        && (Options.IgnoredNamespaceParts & f) == 0)
                    .OrderBy(f => f, NamespacePartComparer)
                    .ToImmutableArray();
            }

            return _enabledAndSortedNamespaceParts;
        }
    }

    internal ImmutableArray<TypeDocumentationParts> EnabledAndSortedTypeParts
    {
        get
        {
            if (_enabledAndSortedTypeParts.IsDefault)
            {
                _enabledAndSortedTypeParts = Enum.GetValues(typeof(TypeDocumentationParts))
                    .Cast<TypeDocumentationParts>()
                    .Where(f => f != TypeDocumentationParts.None
                        && f != TypeDocumentationParts.All
                        && f != TypeDocumentationParts.NestedTypes
                        && f != TypeDocumentationParts.AllExceptNestedTypes
                        && (Options.IgnoredTypeParts & f) == 0)
                    .OrderBy(f => f, TypePartComparer)
                    .ToImmutableArray();
            }

            return _enabledAndSortedTypeParts;
        }
    }

    internal ImmutableArray<MemberDocumentationParts> EnabledAndSortedMemberParts
    {
        get
        {
            if (_enabledAndSortedMemberParts.IsDefault)
            {
                _enabledAndSortedMemberParts = Enum.GetValues(typeof(MemberDocumentationParts))
                    .Cast<MemberDocumentationParts>()
                    .Where(f => f != MemberDocumentationParts.None
                        && f != MemberDocumentationParts.All
                        && (Options.IgnoredMemberParts & f) == 0)
                    .OrderBy(f => f, MemberPartComparer)
                    .ToImmutableArray();
            }

            return _enabledAndSortedMemberParts;
        }
    }

    private DocumentationWriter CreateWriter(ISymbol currentSymbol = null)
    {
        DocumentationWriter writer = Context.CreateWriter();

        writer.CurrentSymbol = currentSymbol;
        writer.CanCreateMemberLocalUrl = Options.Depth == DocumentationDepth.Member;
        writer.CanCreateTypeLocalUrl = Options.Depth <= DocumentationDepth.Type;

        return writer;
    }

    public IEnumerable<DocumentationGeneratorResult> Generate(string heading = null, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        DocumentationDepth depth = Options.Depth;

        using (DocumentationWriter writer = CreateWriter())
        {
            yield return GenerateRoot(writer, heading);
        }

        if (depth <= DocumentationDepth.Namespace)
        {
            IEnumerable<INamedTypeSymbol> typeSymbols = DocumentationModel.Types.Where(f => !Options.ShouldBeIgnored(f));

            foreach (INamespaceSymbol namespaceSymbol in typeSymbols
                .SelectMany(f => f.GetContainingNamespaces())
                .Distinct(MetadataNameEqualityComparer<INamespaceSymbol>.Instance))
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (DocumentationUtility.ShouldGenerateNamespaceFile(namespaceSymbol, Context.CommonNamespaces))
                    yield return GenerateNamespace(namespaceSymbol);
            }

            if (depth <= DocumentationDepth.Type)
            {
                foreach (INamedTypeSymbol typeSymbol in typeSymbols)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    if (!Options.ShouldBeIgnored(typeSymbol))
                    {
                        TypeDocumentationModel typeModel = DocumentationModel.GetTypeModel(typeSymbol);

                        yield return GenerateType(typeModel);

                        if (depth == DocumentationDepth.Member)
                        {
                            foreach (DocumentationGeneratorResult result in GenerateMembers(typeModel))
                            {
                                cancellationToken.ThrowIfCancellationRequested();

                                yield return result;
                            }
                        }
                    }
                }
            }
        }

        foreach (INamedTypeSymbol typeSymbol in DocumentationModel.GetExtendedExternalTypes())
        {
            if (!Options.ShouldBeIgnored(typeSymbol))
            {
                yield return GenerateExtendedExternalType(typeSymbol);
            }
        }
    }

    public DocumentationGeneratorResult GenerateRoot(string heading)
    {
        using (DocumentationWriter writer = CreateWriter())
        {
            return GenerateRoot(writer, heading);
        }
    }

    internal DocumentationGeneratorResult GenerateRoot(DocumentationWriter writer, string heading)
    {
        writer.WriteStartDocument(null, DocumentationFileKind.Root);

        if (Options.ScrollToContent)
            writer.WriteLinkTarget(WellKnownNames.TopFragmentName);

        writer.WriteStartHeading(1);
        writer.WriteString(heading);
        writer.WriteEndHeading();

        GenerateRoot(writer);

        writer.WriteEndDocument(null, DocumentationFileKind.Root);

        return CreateResult(writer, DocumentationFileKind.Root);
    }

    private void GenerateRoot(DocumentationWriter writer)
    {
        IEnumerable<INamedTypeSymbol> typeSymbols = DocumentationModel.Types.Where(f => !Options.ShouldBeIgnored(f));

        foreach (RootDocumentationParts part in EnabledAndSortedRootParts)
        {
            switch (part)
            {
                case RootDocumentationParts.Content:
                    {
                        IEnumerable<string> names = EnabledAndSortedRootParts
                            .Where(f => HasContent(f))
                            .OrderBy(f => f, RootPartComparer)
                            .Select(f => Resources.GetHeading(f));

                        if ((Options.IgnoredRootParts & RootDocumentationParts.Content) == 0)
                            writer.WriteContent(names);

                        break;
                    }
                case RootDocumentationParts.Types:
                case RootDocumentationParts.Namespaces:
                    {
                        IEnumerable<INamespaceSymbol> namespaceSymbols = typeSymbols
                            .Select(f => f.ContainingNamespace)
                            .Distinct(MetadataNameEqualityComparer<INamespaceSymbol>.Instance);

                        bool includeTypes = (Options.IgnoredRootParts & RootDocumentationParts.Types) == 0;

                        writer.WriteTypesByNamespace(
                            typeSymbols,
                            (includeTypes) ? Resources.TypesTitle : Resources.NamespacesTitle,
                            2,
                            includeTypes: includeTypes);

                        break;
                    }
                case RootDocumentationParts.ClassHierarchy:
                    {
                        if (typeSymbols.Any(f => !f.IsStatic && f.TypeKind == TypeKind.Class))
                        {
                            INamedTypeSymbol objectType = DocumentationModel.Compilations[0].ObjectType;

                            IEnumerable<INamedTypeSymbol> instanceClasses = typeSymbols.Where(f => !f.IsStatic && f.TypeKind == TypeKind.Class);

                            writer.WriteHeading2(Resources.ClassHierarchyTitle);

                            writer.WriteClassHierarchy(objectType, instanceClasses, includeContainingNamespace: Options.IncludeContainingNamespace(IncludeContainingNamespaceFilter.ClassHierarchy));

                            writer.WriteLine();
                        }

                        break;
                    }
                case RootDocumentationParts.Other:
                    {
                        GenerateExternalTypesExtensions(writer);
                        break;
                    }
            }
        }

        bool HasContent(RootDocumentationParts part)
        {
            switch (part)
            {
                case RootDocumentationParts.Content:
                    return false;
                case RootDocumentationParts.Namespaces:
                    return typeSymbols.Any();
                case RootDocumentationParts.ClassHierarchy:
                    return typeSymbols.Any(f => !f.IsStatic && f.TypeKind == TypeKind.Class);
                case RootDocumentationParts.Types:
                    return typeSymbols.Any();
                case RootDocumentationParts.Other:
                    return DocumentationModel.GetExtendedExternalTypes().Any(f => !Options.ShouldBeIgnored(f));
                default:
                    throw new InvalidOperationException();
            }
        }
    }

    private DocumentationGeneratorResult GenerateNamespace(INamespaceSymbol namespaceSymbol)
    {
        IEnumerable<INamedTypeSymbol> typeSymbols = DocumentationModel
            .Types
            .Where(f => MetadataNameEqualityComparer<INamespaceSymbol>.Instance.Equals(f.ContainingNamespace, namespaceSymbol));

        using (DocumentationWriter writer = CreateWriter(namespaceSymbol))
        {
            writer.WriteStartDocument(namespaceSymbol, DocumentationFileKind.Namespace);

            SymbolXmlDocumentation xmlDocumentation = DocumentationModel.GetXmlDocumentation(namespaceSymbol, Options.PreferredCultureName);

            writer.WriteHeading(
                1,
                namespaceSymbol,
                ((Options.IgnoredTitleParts & SymbolTitleParts.ContainingNamespace) != 0)
                    ? TypeSymbolDisplayFormats.Name
                    : TypeSymbolDisplayFormats.Name_ContainingTypes_Namespaces,
                addLink: false,
                linkDestination: (Options.ScrollToContent) ? WellKnownNames.TopFragmentName : null);

            foreach (NamespaceDocumentationParts part in EnabledAndSortedNamespaceParts)
            {
                switch (part)
                {
                    case NamespaceDocumentationParts.Content:
                        {
                            IEnumerable<string> names = EnabledAndSortedNamespaceParts
                                .Where(f => HasContent(f))
                                .OrderBy(f => f, NamespacePartComparer)
                                .Select(f => Resources.GetHeading(f));

                            if ((Options.IgnoredNamespaceParts & NamespaceDocumentationParts.Content) == 0)
                                writer.WriteContent(names, includeLinkToRoot: true);

                            break;
                        }
                    case NamespaceDocumentationParts.ContainingNamespace:
                        {
                            INamespaceSymbol containingNamespace = namespaceSymbol.ContainingNamespace;

                            if (containingNamespace?.IsGlobalNamespace == false)
                                writer.WriteContainingNamespace(containingNamespace, Resources.ContainingNamespaceTitle);

                            break;
                        }
                    case NamespaceDocumentationParts.Summary:
                        {
                            xmlDocumentation?.GetElement(WellKnownXmlTags.Summary)?.WriteContentTo(writer);
                            break;
                        }
                    case NamespaceDocumentationParts.Examples:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteExamples(namespaceSymbol, xmlDocumentation);

                            break;
                        }
                    case NamespaceDocumentationParts.Remarks:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteRemarks(namespaceSymbol, xmlDocumentation);

                            break;
                        }
                    case NamespaceDocumentationParts.Classes:
                        {
                            WriteTypes(typeSymbols, TypeKind.Class);
                            break;
                        }
                    case NamespaceDocumentationParts.Structs:
                        {
                            WriteTypes(typeSymbols, TypeKind.Struct);
                            break;
                        }
                    case NamespaceDocumentationParts.Interfaces:
                        {
                            WriteTypes(typeSymbols, TypeKind.Interface);
                            break;
                        }
                    case NamespaceDocumentationParts.Enums:
                        {
                            WriteTypes(typeSymbols, TypeKind.Enum);
                            break;
                        }
                    case NamespaceDocumentationParts.Delegates:
                        {
                            WriteTypes(typeSymbols, TypeKind.Delegate);
                            break;
                        }
                    case NamespaceDocumentationParts.Namespaces:
                        {
                            if (HasContent(NamespaceDocumentationParts.Namespaces))
                            {
                                IEnumerable<INamespaceSymbol> namespaces = DocumentationModel
                                    .Types
                                    .SelectMany(f => f.GetContainingNamespaces())
                                    .Where(f => MetadataNameEqualityComparer<INamespaceSymbol>.Instance.Equals(f.ContainingNamespace, namespaceSymbol))
                                    .Distinct(MetadataNameEqualityComparer<INamespaceSymbol>.Instance)
                                    .OrderBy(f => f, SymbolDefinitionComparer.SystemFirst);

                                WriteNamespaces(namespaces);
                            }

                            break;
                        }
                    case NamespaceDocumentationParts.SeeAlso:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteSeeAlso(namespaceSymbol, xmlDocumentation);

                            break;
                        }
                    default:
                        {
                            throw new InvalidOperationException();
                        }
                }
            }

            writer.WriteEndDocument(namespaceSymbol, DocumentationFileKind.Namespace);

            return CreateResult(writer, DocumentationFileKind.Namespace, namespaceSymbol);

            void WriteTypes(
                IEnumerable<INamedTypeSymbol> types,
                TypeKind typeKind)
            {
                writer.WriteTable(
                    types.Where(f => f.TypeKind == typeKind),
                    Resources.GetPluralName(typeKind),
                    headingLevel: 2,
                    Resources.GetName(typeKind),
                    Resources.SummaryTitle,
                    TypeSymbolDisplayFormats.Name_ContainingTypes_TypeParameters,
                    addLink: Options.Depth <= DocumentationDepth.Type);
            }

            void WriteNamespaces(IEnumerable<INamespaceSymbol> namespaces)
            {
                writer.WriteHeading(2, Resources.NamespacesTitle);

                foreach (INamespaceSymbol namespaceSymbol in namespaces)
                {
                    writer.WriteStartBulletItem();
                    writer.WriteLink(namespaceSymbol, TypeSymbolDisplayFormats.Name);
                    writer.WriteEndBulletItem();
                }
            }

            bool HasContent(NamespaceDocumentationParts part)
            {
                switch (part)
                {
                    case NamespaceDocumentationParts.Content:
                    case NamespaceDocumentationParts.Summary:
                    case NamespaceDocumentationParts.ContainingNamespace:
                        {
                            return false;
                        }
                    case NamespaceDocumentationParts.Examples:
                        {
                            return xmlDocumentation?.HasElement(WellKnownXmlTags.Example) == true;
                        }
                    case NamespaceDocumentationParts.Remarks:
                        {
                            return xmlDocumentation?.HasElement(WellKnownXmlTags.Remarks) == true;
                        }
                    case NamespaceDocumentationParts.Classes:
                        {
                            return typeSymbols.Any(f => f.TypeKind == TypeKind.Class);
                        }
                    case NamespaceDocumentationParts.Structs:
                        {
                            return typeSymbols.Any(f => f.TypeKind == TypeKind.Struct);
                        }
                    case NamespaceDocumentationParts.Interfaces:
                        {
                            return typeSymbols.Any(f => f.TypeKind == TypeKind.Interface);
                        }
                    case NamespaceDocumentationParts.Enums:
                        {
                            return typeSymbols.Any(f => f.TypeKind == TypeKind.Enum);
                        }
                    case NamespaceDocumentationParts.Delegates:
                        {
                            return typeSymbols.Any(f => f.TypeKind == TypeKind.Delegate);
                        }
                    case NamespaceDocumentationParts.Namespaces:
                        {
                            return !typeSymbols.Any()
                                && DocumentationModel
                                    .Types
                                    .SelectMany(f => f.GetContainingNamespaces())
                                    .Any(f => MetadataNameEqualityComparer<INamespaceSymbol>.Instance.Equals(f.ContainingNamespace, namespaceSymbol));
                        }
                    case NamespaceDocumentationParts.SeeAlso:
                        {
                            return xmlDocumentation?.GetElements(WellKnownXmlTags.SeeAlso).Any() == true;
                        }
                    default:
                        {
                            throw new InvalidOperationException();
                        }
                }
            }
        }
    }

    private void GenerateExternalTypesExtensions(DocumentationWriter writer)
    {
        IEnumerable<INamedTypeSymbol> extendedExternalTypes = DocumentationModel.GetExtendedExternalTypes()
            .Where(f => !Options.ShouldBeIgnored(f));

        writer.WriteTypesByNamespace(extendedExternalTypes, Resources.ExtensionsOfExternalTypesTitle, 2, includeTypes: true);
    }

    private DocumentationGeneratorResult GenerateExtendedExternalType(INamedTypeSymbol typeSymbol)
    {
        using (DocumentationWriter writer = CreateWriter(typeSymbol))
        {
            writer.WriteStartDocument(typeSymbol, DocumentationFileKind.Type);

            if (Options.ScrollToContent)
            {
                writer.WriteLinkTarget(WellKnownNames.TopFragmentName);
                writer.WriteLine();
            }

            writer.WriteStartHeading(1);
            writer.WriteLink(typeSymbol, TypeSymbolDisplayFormats.Name_ContainingTypes_TypeParameters);
            writer.WriteSpace();
            writer.WriteString(Resources.GetName(typeSymbol.TypeKind));
            writer.WriteSpace();
            writer.WriteString(Resources.ExtensionsTitle);
            writer.WriteEndHeading();

            if ((Options.IgnoredRootParts & RootDocumentationParts.Content) == 0)
                writer.WriteContent(Array.Empty<string>(), includeLinkToRoot: true);

            writer.WriteTable(
                DocumentationModel.GetExtensionMethods(typeSymbol),
                heading: null,
                headingLevel: -1,
                Resources.ExtensionMethodTitle,
                Resources.SummaryTitle,
                DocumentationDisplayFormats.SimpleDeclaration);

            writer.WriteEndDocument(typeSymbol, DocumentationFileKind.Type);

            return CreateResult(writer, DocumentationFileKind.Type, typeSymbol);
        }
    }

    private DocumentationGeneratorResult GenerateType(TypeDocumentationModel typeModel)
    {
        INamedTypeSymbol typeSymbol = typeModel.Symbol;

        ImmutableArray<INamedTypeSymbol> derivedTypes = ImmutableArray<INamedTypeSymbol>.Empty;

        if (EnabledAndSortedTypeParts.Contains(TypeDocumentationParts.Derived))
        {
            derivedTypes = (Options.IncludeAllDerivedTypes)
                ? DocumentationModel.GetAllDerivedTypes(typeSymbol).ToImmutableArray()
                : DocumentationModel.GetDerivedTypes(typeSymbol).ToImmutableArray();
        }

        bool includeInherited = typeModel.TypeKind != TypeKind.Interface || Options.IncludeInheritedInterfaceMembers;

        SymbolXmlDocumentation xmlDocumentation = DocumentationModel.GetXmlDocumentation(typeModel.Symbol, Options.PreferredCultureName);

        using (DocumentationWriter writer = CreateWriter(typeSymbol))
        {
            writer.WriteStartDocument(typeSymbol, DocumentationFileKind.Type);

            writer.WriteHeading(
                1,
                typeSymbol,
                ((Options.IgnoredTitleParts & SymbolTitleParts.ContainingType) != 0)
                        ? TypeSymbolDisplayFormats.Name_TypeParameters
                        : TypeSymbolDisplayFormats.Name_ContainingTypes_TypeParameters,
                SymbolDisplayAdditionalMemberOptions.UseItemPropertyName | SymbolDisplayAdditionalMemberOptions.UseOperatorName,
                addLink: false,
                linkDestination: (Options.ScrollToContent) ? WellKnownNames.TopFragmentName : null);

            foreach (TypeDocumentationParts part in EnabledAndSortedTypeParts)
            {
                switch (part)
                {
                    case TypeDocumentationParts.Content:
                        {
                            IEnumerable<string> names = EnabledAndSortedTypeParts
                                .Where(f => HasContent(f))
                                .OrderBy(f => f, TypePartComparer)
                                .Select(f => Resources.GetHeading(f));

                            if ((Options.IgnoredTypeParts & TypeDocumentationParts.Content) == 0)
                                writer.WriteContent(names, includeLinkToRoot: true);

                            break;
                        }
                    case TypeDocumentationParts.ContainingNamespace:
                        {
                            INamespaceSymbol containingNamespace = typeModel.ContainingNamespace;

                            if (containingNamespace is not null)
                                writer.WriteContainingNamespace(containingNamespace, Resources.NamespaceTitle);

                            break;
                        }
                    case TypeDocumentationParts.ContainingAssembly:
                        {
                            writer.WriteContainingAssembly(typeModel.ContainingAssembly, Resources.AssemblyTitle);
                            break;
                        }
                    case TypeDocumentationParts.ObsoleteMessage:
                        {
                            if (typeModel.IsObsolete)
                                writer.WriteObsoleteMessage(typeSymbol);

                            break;
                        }
                    case TypeDocumentationParts.Summary:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteSummary(typeSymbol, xmlDocumentation);

                            break;
                        }
                    case TypeDocumentationParts.Declaration:
                        {
                            writer.WriteDefinition(typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.TypeParameters:
                        {
                            writer.WriteTypeParameters(typeModel.TypeParameters);
                            break;
                        }
                    case TypeDocumentationParts.Parameters:
                        {
                            writer.WriteParameters(typeModel.Parameters);
                            break;
                        }
                    case TypeDocumentationParts.ReturnValue:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteReturnType(typeSymbol, xmlDocumentation);

                            break;
                        }
                    case TypeDocumentationParts.Inheritance:
                        {
                            writer.WriteInheritance(typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.Attributes:
                        {
                            writer.WriteAttributes(typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.Derived:
                        {
                            if (derivedTypes.Any())
                                writer.WriteDerivedTypes(derivedTypes);

                            break;
                        }
                    case TypeDocumentationParts.Implements:
                        {
                            writer.WriteImplementedInterfaces(typeModel.GetImplementedInterfaces(Options.OmitIEnumerable));
                            break;
                        }
                    case TypeDocumentationParts.Examples:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteExamples(typeSymbol, xmlDocumentation);

                            break;
                        }
                    case TypeDocumentationParts.Remarks:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteRemarks(typeSymbol, xmlDocumentation);

                            break;
                        }
                    case TypeDocumentationParts.Constructors:
                        {
                            writer.WriteConstructors(typeModel.GetConstructors());
                            break;
                        }
                    case TypeDocumentationParts.Fields:
                        {
                            if (typeModel.TypeKind == TypeKind.Enum)
                            {
                                writer.WriteEnumFields(typeModel.GetFields(), typeSymbol);
                            }
                            else
                            {
                                writer.WriteFields(typeModel.GetFields(includeInherited: includeInherited), containingType: typeSymbol);
                            }

                            break;
                        }
                    case TypeDocumentationParts.Indexers:
                        {
                            writer.WriteIndexers(typeModel.GetIndexers(includeInherited: includeInherited), containingType: typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.Properties:
                        {
                            writer.WriteProperties(typeModel.GetProperties(includeInherited: includeInherited), containingType: typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.Methods:
                        {
                            writer.WriteMethods(typeModel.GetMethods(includeInherited: includeInherited), containingType: typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.Operators:
                        {
                            writer.WriteOperators(typeModel.GetOperators(includeInherited: true), containingType: typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.Events:
                        {
                            writer.WriteEvents(typeModel.GetEvents(includeInherited: includeInherited), containingType: typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.ExplicitInterfaceImplementations:
                        {
                            writer.WriteExplicitInterfaceImplementations(typeModel.GetExplicitImplementations());
                            break;
                        }
                    case TypeDocumentationParts.ExtensionMethods:
                        {
                            writer.WriteExtensionMethods(DocumentationModel.GetExtensionMethods(typeSymbol));
                            break;
                        }
                    case TypeDocumentationParts.Classes:
                        {
                            writer.WriteNestedTypes(typeModel.GetClasses(includeInherited: includeInherited), TypeKind.Class, typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.Structs:
                        {
                            writer.WriteNestedTypes(typeModel.GetStructs(includeInherited: includeInherited), TypeKind.Struct, typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.Interfaces:
                        {
                            writer.WriteNestedTypes(typeModel.GetInterfaces(includeInherited: includeInherited), TypeKind.Interface, typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.Enums:
                        {
                            writer.WriteNestedTypes(typeModel.GetEnums(includeInherited: includeInherited), TypeKind.Enum, typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.Delegates:
                        {
                            writer.WriteNestedTypes(typeModel.GetDelegates(includeInherited: includeInherited), TypeKind.Delegate, typeSymbol);
                            break;
                        }
                    case TypeDocumentationParts.AppliesTo:
                        {
                            if (SourceReferenceProvider is not null)
                                writer.WriteAppliesTo(typeSymbol, SourceReferenceProvider.GetSourceReferences(typeSymbol));

                            break;
                        }
                    case TypeDocumentationParts.SeeAlso:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteSeeAlso(typeSymbol, xmlDocumentation);

                            break;
                        }
                }
            }

            if (derivedTypes.Any()
                && derivedTypes.Length > Options.MaxDerivedTypes)
            {
                writer.WriteTypeList(
                    derivedTypes,
                    heading: Resources.DerivedAllTitle,
                    headingLevel: 2,
                    includeContainingNamespace: Options.IncludeContainingNamespace(IncludeContainingNamespaceFilter.DerivedType),
                    addSeparatorAtIndex: Options.MaxDerivedTypes);
            }

            writer.WriteEndDocument(typeSymbol, DocumentationFileKind.Type);

            return CreateResult(writer, DocumentationFileKind.Type, typeSymbol);
        }

        bool HasContent(TypeDocumentationParts part)
        {
            switch (part)
            {
                case TypeDocumentationParts.Content:
                case TypeDocumentationParts.ContainingNamespace:
                case TypeDocumentationParts.ContainingAssembly:
                case TypeDocumentationParts.ObsoleteMessage:
                case TypeDocumentationParts.Summary:
                case TypeDocumentationParts.Declaration:
                case TypeDocumentationParts.TypeParameters:
                case TypeDocumentationParts.Parameters:
                case TypeDocumentationParts.ReturnValue:
                case TypeDocumentationParts.Inheritance:
                case TypeDocumentationParts.Attributes:
                case TypeDocumentationParts.Derived:
                case TypeDocumentationParts.Implements:
                case TypeDocumentationParts.AppliesTo:
                    {
                        return false;
                    }
                case TypeDocumentationParts.Examples:
                    {
                        return xmlDocumentation?.HasElement(WellKnownXmlTags.Example) == true;
                    }
                case TypeDocumentationParts.Remarks:
                    {
                        return xmlDocumentation?.HasElement(WellKnownXmlTags.Remarks) == true;
                    }
                case TypeDocumentationParts.Constructors:
                    {
                        return typeModel.GetConstructors().Any();
                    }
                case TypeDocumentationParts.Fields:
                    {
                        if (typeModel.TypeKind == TypeKind.Enum)
                        {
                            return typeModel.GetFields().Any();
                        }
                        else
                        {
                            return typeModel.GetFields(includeInherited: true).Any();
                        }
                    }
                case TypeDocumentationParts.Indexers:
                    {
                        return typeModel.GetIndexers(includeInherited: includeInherited).Any();
                    }
                case TypeDocumentationParts.Properties:
                    {
                        return typeModel.GetProperties(includeInherited: includeInherited).Any();
                    }
                case TypeDocumentationParts.Methods:
                    {
                        return typeModel.GetMethods(includeInherited: includeInherited).Any();
                    }
                case TypeDocumentationParts.Operators:
                    {
                        return typeModel.GetOperators(includeInherited: true).Any();
                    }
                case TypeDocumentationParts.Events:
                    {
                        return typeModel.GetEvents(includeInherited: includeInherited).Any();
                    }
                case TypeDocumentationParts.ExplicitInterfaceImplementations:
                    {
                        return typeModel.GetExplicitImplementations().Any();
                    }
                case TypeDocumentationParts.ExtensionMethods:
                    {
                        return DocumentationModel.GetExtensionMethods(typeSymbol).Any();
                    }
                case TypeDocumentationParts.Classes:
                    {
                        return typeModel.GetClasses(includeInherited: includeInherited).Any();
                    }
                case TypeDocumentationParts.Structs:
                    {
                        return typeModel.GetStructs(includeInherited: includeInherited).Any();
                    }
                case TypeDocumentationParts.Interfaces:
                    {
                        return typeModel.GetInterfaces(includeInherited: includeInherited).Any();
                    }
                case TypeDocumentationParts.Enums:
                    {
                        return typeModel.GetEnums(includeInherited: includeInherited).Any();
                    }
                case TypeDocumentationParts.Delegates:
                    {
                        return typeModel.GetDelegates(includeInherited: includeInherited).Any();
                    }
                case TypeDocumentationParts.SeeAlso:
                    {
                        return xmlDocumentation?.GetElements(WellKnownXmlTags.SeeAlso).Any() == true;
                    }
                default:
                    {
                        throw new InvalidOperationException();
                    }
            }
        }
    }

    private IEnumerable<DocumentationGeneratorResult> GenerateMembers(TypeDocumentationModel typeModel)
    {
        foreach (IGrouping<string, ISymbol> grouping in typeModel
            .GetMembers(Options.IgnoredTypeParts)
            .GroupBy(f => f.Name))
        {
            using (IEnumerator<ISymbol> en = grouping.GetEnumerator())
            {
                if (en.MoveNext())
                {
                    ISymbol symbol = en.Current;

                    using (DocumentationWriter writer = CreateWriter(symbol))
                    {
                        writer.WriteStartDocument(symbol, DocumentationFileKind.Member);

                        bool isOverloaded = en.MoveNext();

                        if (Options.ScrollToContent)
                        {
                            writer.WriteLinkTarget(WellKnownNames.TopFragmentName);
                            writer.WriteLine();
                        }

                        writer.WriteMemberTitle(symbol, isOverloaded);

                        if ((Options.IgnoredMemberParts & MemberDocumentationParts.Content) == 0)
                            writer.WriteContent(Array.Empty<string>(), includeLinkToRoot: true);

                        if ((Options.IgnoredMemberParts & MemberDocumentationParts.ContainingType) == 0)
                            writer.WriteContainingType(symbol.ContainingType, Resources.ContainingTypeTitle);

                        if ((Options.IgnoredMemberParts & MemberDocumentationParts.ContainingAssembly) == 0)
                            writer.WriteContainingAssembly(symbol.ContainingAssembly, Resources.AssemblyTitle);

                        if (isOverloaded)
                        {
                            SymbolDisplayFormat format = DocumentationDisplayFormats.SimpleDeclaration;

                            const SymbolDisplayAdditionalMemberOptions additionalOptions = SymbolDisplayAdditionalMemberOptions.UseItemPropertyName | SymbolDisplayAdditionalMemberOptions.UseOperatorName;

                            writer.WriteTable(
                                grouping,
                                heading: Resources.OverloadsTitle,
                                headingLevel: 2,
                                header1: Resources.GetName(symbol),
                                header2: Resources.SummaryTitle,
                                format: format,
                                additionalOptions: additionalOptions);

                            foreach (ISymbol overloadSymbol in grouping.OrderBy(f => f.ToDisplayString(format, additionalOptions)))
                            {
                                string id = UrlProvider.GetFragment(overloadSymbol);

                                writer.WriteLinkTarget(id);
                                writer.WriteStartHeading(2);
                                writer.WriteString(overloadSymbol.ToDisplayString(format, additionalOptions));
                                writer.WriteSpace();
                                writer.WriteEndHeading();

                                GenerateMemberContent(writer, overloadSymbol, headingLevelBase: 1);
                            }
                        }
                        else
                        {
                            GenerateMemberContent(writer, symbol);
                        }

                        writer.WriteEndDocument(symbol, DocumentationFileKind.Member);

                        yield return CreateResult(writer, DocumentationFileKind.Member, symbol);
                    }
                }
            }
        }

        void GenerateMemberContent(DocumentationWriter writer, ISymbol symbol, int headingLevelBase = 0)
        {
            SymbolXmlDocumentation xmlDocumentation = DocumentationModel.GetXmlDocumentation(symbol, Options.PreferredCultureName);

            foreach (MemberDocumentationParts part in EnabledAndSortedMemberParts)
            {
                switch (part)
                {
                    case MemberDocumentationParts.ObsoleteMessage:
                        {
                            if (symbol.HasAttribute(MetadataNames.System_ObsoleteAttribute))
                                writer.WriteObsoleteMessage(symbol);

                            break;
                        }
                    case MemberDocumentationParts.Summary:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteSummary(symbol, xmlDocumentation, headingLevelBase: headingLevelBase);

                            break;
                        }
                    case MemberDocumentationParts.Declaration:
                        {
                            writer.WriteDefinition(symbol);
                            break;
                        }
                    case MemberDocumentationParts.TypeParameters:
                        {
                            writer.WriteTypeParameters(symbol.GetTypeParameters());
                            break;
                        }
                    case MemberDocumentationParts.Parameters:
                        {
                            writer.WriteParameters(symbol.GetParameters());
                            break;
                        }
                    case MemberDocumentationParts.ReturnValue:
                        {
                            writer.WriteReturnType(symbol, xmlDocumentation);
                            break;
                        }
                    case MemberDocumentationParts.Implements:
                        {
                            writer.WriteImplementedInterfaceMembers(symbol.FindImplementedInterfaceMembers());
                            break;
                        }
                    case MemberDocumentationParts.Attributes:
                        {
                            writer.WriteAttributes(symbol);
                            break;
                        }
                    case MemberDocumentationParts.Exceptions:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteExceptions(symbol, xmlDocumentation);

                            break;
                        }
                    case MemberDocumentationParts.Examples:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteExamples(symbol, xmlDocumentation, headingLevelBase: headingLevelBase);

                            break;
                        }
                    case MemberDocumentationParts.Remarks:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteRemarks(symbol, xmlDocumentation, headingLevelBase: headingLevelBase);

                            break;
                        }
                    case MemberDocumentationParts.AppliesTo:
                        {
                            if (SourceReferenceProvider is null)
                                break;

                            writer.WriteAppliesTo(symbol, SourceReferenceProvider.GetSourceReferences(symbol), headingLevelBase: headingLevelBase);

                            break;
                        }
                    case MemberDocumentationParts.SeeAlso:
                        {
                            if (xmlDocumentation is not null)
                                writer.WriteSeeAlso(symbol, xmlDocumentation, headingLevelBase: headingLevelBase);

                            break;
                        }
                }
            }
        }
    }

    private DocumentationGeneratorResult CreateResult(DocumentationWriter writer, DocumentationFileKind kind, ISymbol symbol = null)
    {
        return new DocumentationGeneratorResult(writer?.ToString(), GetPath(), kind, (symbol is not null) ? DocumentationUtility.GetSymbolLabel(symbol, Context) : null);

        string GetPath()
        {
            string fileName = UrlProvider.GetFileName(kind);

            switch (kind)
            {
                case DocumentationFileKind.Root:
                case DocumentationFileKind.Extensions:
                    return fileName;
                case DocumentationFileKind.Namespace:
                case DocumentationFileKind.Type:
                case DocumentationFileKind.Member:
                    return UrlProvider.GetUrl(symbol, fileName, Path.DirectorySeparatorChar);
                default:
                    throw new ArgumentException("", nameof(kind));
            }
        }
    }
}
