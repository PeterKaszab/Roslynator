# SyntaxExtensions\.PrependToLeadingTrivia Method

[Home](../../../README.md)

**Containing Type**: [SyntaxExtensions](../README.md)

**Assembly**: Roslynator\.Core\.dll

## Overloads

| Method | Summary |
| ------ | ------- |
| [PrependToLeadingTrivia(SyntaxToken, IEnumerable\<SyntaxTrivia\>)](#640281292) | Creates a new token from this token with the leading trivia replaced with a new trivia where the specified trivia is inserted at the beginning of the leading trivia\. |
| [PrependToLeadingTrivia(SyntaxToken, SyntaxTrivia)](#2404824921) | Creates a new token from this token with the leading trivia replaced with a new trivia where the specified trivia is inserted at the beginning of the leading trivia\. |
| [PrependToLeadingTrivia\<TNode\>(TNode, IEnumerable\<SyntaxTrivia\>)](#3915245611) | Creates a new node from this node with the leading trivia replaced with a new trivia where the specified trivia is inserted at the beginning of the leading trivia\. |
| [PrependToLeadingTrivia\<TNode\>(TNode, SyntaxTrivia)](#3276186521) | Creates a new node from this node with the leading trivia replaced with a new trivia where the specified trivia is inserted at the beginning of the leading trivia\. |

<a id="640281292"></a>

## PrependToLeadingTrivia\(SyntaxToken, IEnumerable\<SyntaxTrivia\>\) 

  
Creates a new token from this token with the leading trivia replaced with a new trivia where the specified trivia is inserted at the beginning of the leading trivia\.

```csharp
public static Microsoft.CodeAnalysis.SyntaxToken PrependToLeadingTrivia(this Microsoft.CodeAnalysis.SyntaxToken token, System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.SyntaxTrivia> trivia)
```

### Parameters

**token** &ensp; [SyntaxToken](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken)

**trivia** &ensp; [IEnumerable](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1)\<[SyntaxTrivia](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtrivia)\>

### Returns

[SyntaxToken](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken)

<a id="2404824921"></a>

## PrependToLeadingTrivia\(SyntaxToken, SyntaxTrivia\) 

  
Creates a new token from this token with the leading trivia replaced with a new trivia where the specified trivia is inserted at the beginning of the leading trivia\.

```csharp
public static Microsoft.CodeAnalysis.SyntaxToken PrependToLeadingTrivia(this Microsoft.CodeAnalysis.SyntaxToken token, Microsoft.CodeAnalysis.SyntaxTrivia trivia)
```

### Parameters

**token** &ensp; [SyntaxToken](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken)

**trivia** &ensp; [SyntaxTrivia](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtrivia)

### Returns

[SyntaxToken](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtoken)

<a id="3915245611"></a>

## PrependToLeadingTrivia\<TNode\>\(TNode, IEnumerable\<SyntaxTrivia\>\) 

  
Creates a new node from this node with the leading trivia replaced with a new trivia where the specified trivia is inserted at the beginning of the leading trivia\.

```csharp
public static TNode PrependToLeadingTrivia<TNode>(this TNode node, System.Collections.Generic.IEnumerable<Microsoft.CodeAnalysis.SyntaxTrivia> trivia) where TNode : Microsoft.CodeAnalysis.SyntaxNode
```

### Type Parameters

**TNode**

### Parameters

**node** &ensp; TNode

**trivia** &ensp; [IEnumerable](https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.ienumerable-1)\<[SyntaxTrivia](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtrivia)\>

### Returns

TNode

<a id="3276186521"></a>

## PrependToLeadingTrivia\<TNode\>\(TNode, SyntaxTrivia\) 

  
Creates a new node from this node with the leading trivia replaced with a new trivia where the specified trivia is inserted at the beginning of the leading trivia\.

```csharp
public static TNode PrependToLeadingTrivia<TNode>(this TNode node, Microsoft.CodeAnalysis.SyntaxTrivia trivia) where TNode : Microsoft.CodeAnalysis.SyntaxNode
```

### Type Parameters

**TNode**

### Parameters

**node** &ensp; TNode

**trivia** &ensp; [SyntaxTrivia](https://docs.microsoft.com/en-us/dotnet/api/microsoft.codeanalysis.syntaxtrivia)

### Returns

TNode

