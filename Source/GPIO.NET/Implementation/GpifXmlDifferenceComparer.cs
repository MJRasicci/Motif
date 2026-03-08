namespace GPIO.NET.Implementation;

using System.Xml.Linq;

internal static class GpifXmlDifferenceComparer
{
    private static readonly string[] IdentityAttributeCandidates =
    [
        "id",
        "name",
        "string",
        "interval",
        "type",
        "number"
    ];

    public static IReadOnlyList<GpifXmlDifference> Compare(byte[] sourceGpifBytes, byte[] outputGpifBytes)
    {
        using var sourceStream = new MemoryStream(sourceGpifBytes, writable: false);
        using var outputStream = new MemoryStream(outputGpifBytes, writable: false);
        var sourceDocument = XDocument.Load(sourceStream);
        var outputDocument = XDocument.Load(outputStream);

        return Compare(sourceDocument, outputDocument);
    }

    public static IReadOnlyList<GpifXmlDifference> Compare(XDocument sourceDocument, XDocument outputDocument)
    {
        ArgumentNullException.ThrowIfNull(sourceDocument);
        ArgumentNullException.ThrowIfNull(outputDocument);

        var differences = new List<GpifXmlDifference>();
        var sourceRoot = sourceDocument.Root;
        var outputRoot = outputDocument.Root;

        if (sourceRoot is null || outputRoot is null)
        {
            return differences;
        }

        CompareElements(sourceRoot, outputRoot, $"/{sourceRoot.Name.LocalName}", differences);
        return differences;
    }

    public static IReadOnlyList<GpifXmlDifference> Compare(XElement sourceElement, XElement outputElement)
    {
        ArgumentNullException.ThrowIfNull(sourceElement);
        ArgumentNullException.ThrowIfNull(outputElement);

        var differences = new List<GpifXmlDifference>();
        CompareElements(sourceElement, outputElement, $"/{sourceElement.Name.LocalName}", differences);
        return differences;
    }

    private static void CompareElements(
        XElement sourceElement,
        XElement outputElement,
        string path,
        List<GpifXmlDifference> differences)
    {
        if (!XName.Equals(sourceElement.Name, outputElement.Name))
        {
            differences.Add(new GpifXmlDifference(
                Code: "RAW_XML_ELEMENT_NAME_DRIFT",
                Path: path,
                Message: $"GPIF element name changed at {path}.",
                SourceValue: CreateElementPreview(sourceElement),
                OutputValue: CreateElementPreview(outputElement)));
            return;
        }

        CompareAttributes(sourceElement, outputElement, path, differences);
        CompareDirectTextValue(sourceElement, outputElement, path, differences);
        CompareChildren(sourceElement, outputElement, path, differences);
    }

    private static void CompareAttributes(
        XElement sourceElement,
        XElement outputElement,
        string path,
        List<GpifXmlDifference> differences)
    {
        var sourceAttributes = sourceElement.Attributes()
            .Where(attribute => !attribute.IsNamespaceDeclaration)
            .ToDictionary(attribute => attribute.Name.LocalName, StringComparer.Ordinal);
        var outputAttributes = outputElement.Attributes()
            .Where(attribute => !attribute.IsNamespaceDeclaration)
            .ToDictionary(attribute => attribute.Name.LocalName, StringComparer.Ordinal);

        foreach (var attributeName in sourceAttributes.Keys.Union(outputAttributes.Keys, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal))
        {
            var attributePath = $"{path}/@{attributeName}";
            var hasSourceAttribute = sourceAttributes.TryGetValue(attributeName, out var sourceAttribute);
            var hasOutputAttribute = outputAttributes.TryGetValue(attributeName, out var outputAttribute);

            if (hasSourceAttribute && !hasOutputAttribute)
            {
                differences.Add(new GpifXmlDifference(
                    Code: "RAW_XML_ATTRIBUTE_MISSING",
                    Path: attributePath,
                    Message: $"GPIF attribute missing at {attributePath}.",
                    SourceValue: CreateValuePreview(sourceAttribute!.Value),
                    OutputValue: null));
                continue;
            }

            if (!hasSourceAttribute && hasOutputAttribute)
            {
                differences.Add(new GpifXmlDifference(
                    Code: "RAW_XML_ATTRIBUTE_ADDED",
                    Path: attributePath,
                    Message: $"GPIF attribute added at {attributePath}.",
                    SourceValue: null,
                    OutputValue: CreateValuePreview(outputAttribute!.Value)));
                continue;
            }

            if (!string.Equals(sourceAttribute!.Value, outputAttribute!.Value, StringComparison.Ordinal))
            {
                differences.Add(new GpifXmlDifference(
                    Code: "RAW_XML_ATTRIBUTE_DRIFT",
                    Path: attributePath,
                    Message: $"GPIF attribute value changed at {attributePath}.",
                    SourceValue: CreateValuePreview(sourceAttribute.Value),
                    OutputValue: CreateValuePreview(outputAttribute.Value)));
            }
        }
    }

    private static void CompareDirectTextValue(
        XElement sourceElement,
        XElement outputElement,
        string path,
        List<GpifXmlDifference> differences)
    {
        var sourceText = string.Concat(sourceElement.Nodes().OfType<XText>().Select(node => node.Value));
        var outputText = string.Concat(outputElement.Nodes().OfType<XText>().Select(node => node.Value));

        if (string.Equals(sourceText, outputText, StringComparison.Ordinal))
        {
            return;
        }

        differences.Add(new GpifXmlDifference(
            Code: "RAW_XML_VALUE_DRIFT",
            Path: path,
            Message: $"GPIF element value changed at {path}.",
            SourceValue: CreateValuePreview(sourceText),
            OutputValue: CreateValuePreview(outputText)));
    }

    private static void CompareChildren(
        XElement sourceElement,
        XElement outputElement,
        string path,
        List<GpifXmlDifference> differences)
    {
        var sourceGroups = sourceElement.Elements()
            .GroupBy(element => element.Name.LocalName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);
        var outputGroups = outputElement.Elements()
            .GroupBy(element => element.Name.LocalName, StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        CompareOverallChildOrder(sourceElement, outputElement, path, sourceGroups, outputGroups, differences);

        foreach (var childName in sourceGroups.Keys.Union(outputGroups.Keys, StringComparer.Ordinal).OrderBy(name => name, StringComparer.Ordinal))
        {
            sourceGroups.TryGetValue(childName, out var sourceChildren);
            outputGroups.TryGetValue(childName, out var outputChildren);
            CompareChildGroup(
                childName,
                sourceChildren ?? [],
                outputChildren ?? [],
                path,
                differences);
        }
    }

    private static void CompareChildGroup(
        string childName,
        IReadOnlyList<XElement> sourceChildren,
        IReadOnlyList<XElement> outputChildren,
        string parentPath,
        List<GpifXmlDifference> differences)
    {
        var keyAttribute = SelectIdentityAttribute(sourceChildren, outputChildren);
        if (keyAttribute is not null)
        {
            CompareKeyedChildGroup(childName, keyAttribute, sourceChildren, outputChildren, parentPath, differences);
            return;
        }

        var count = Math.Max(sourceChildren.Count, outputChildren.Count);
        for (var index = 0; index < count; index++)
        {
            var childPath = count == 1
                ? $"{parentPath}/{childName}"
                : $"{parentPath}/{childName}[{index + 1}]";
            var hasSourceChild = index < sourceChildren.Count;
            var hasOutputChild = index < outputChildren.Count;

            if (hasSourceChild && !hasOutputChild)
            {
                differences.Add(new GpifXmlDifference(
                    Code: "RAW_XML_ELEMENT_MISSING",
                    Path: childPath,
                    Message: $"GPIF element missing at {childPath}.",
                    SourceValue: CreateElementPreview(sourceChildren[index]),
                    OutputValue: null));
                continue;
            }

            if (!hasSourceChild && hasOutputChild)
            {
                differences.Add(new GpifXmlDifference(
                    Code: "RAW_XML_ELEMENT_ADDED",
                    Path: childPath,
                    Message: $"GPIF element added at {childPath}.",
                    SourceValue: null,
                    OutputValue: CreateElementPreview(outputChildren[index])));
                continue;
            }

            CompareElements(sourceChildren[index], outputChildren[index], childPath, differences);
        }
    }

    private static void CompareKeyedChildGroup(
        string childName,
        string keyAttribute,
        IReadOnlyList<XElement> sourceChildren,
        IReadOnlyList<XElement> outputChildren,
        string parentPath,
        List<GpifXmlDifference> differences)
    {
        var sourceByKey = sourceChildren.ToDictionary(
            element => element.Attribute(keyAttribute)!.Value,
            StringComparer.Ordinal);
        var outputByKey = outputChildren.ToDictionary(
            element => element.Attribute(keyAttribute)!.Value,
            StringComparer.Ordinal);

        var encounteredKeys = new HashSet<string>(StringComparer.Ordinal);
        foreach (var key in sourceChildren.Select(element => element.Attribute(keyAttribute)!.Value)
                     .Concat(outputChildren.Select(element => element.Attribute(keyAttribute)!.Value)))
        {
            if (!encounteredKeys.Add(key))
            {
                continue;
            }

            var childPath = $"{parentPath}/{childName}[@{keyAttribute}='{EscapeForPath(key)}']";
            var hasSourceChild = sourceByKey.TryGetValue(key, out var sourceChild);
            var hasOutputChild = outputByKey.TryGetValue(key, out var outputChild);

            if (hasSourceChild && !hasOutputChild)
            {
                differences.Add(new GpifXmlDifference(
                    Code: "RAW_XML_ELEMENT_MISSING",
                    Path: childPath,
                    Message: $"GPIF element missing at {childPath}.",
                    SourceValue: CreateElementPreview(sourceChild!),
                    OutputValue: null));
                continue;
            }

            if (!hasSourceChild && hasOutputChild)
            {
                differences.Add(new GpifXmlDifference(
                    Code: "RAW_XML_ELEMENT_ADDED",
                    Path: childPath,
                    Message: $"GPIF element added at {childPath}.",
                    SourceValue: null,
                    OutputValue: CreateElementPreview(outputChild!)));
                continue;
            }

            CompareElements(sourceChild!, outputChild!, childPath, differences);
        }
    }

    private static void CompareOverallChildOrder(
        XElement sourceElement,
        XElement outputElement,
        string path,
        IReadOnlyDictionary<string, List<XElement>> sourceGroups,
        IReadOnlyDictionary<string, List<XElement>> outputGroups,
        List<GpifXmlDifference> differences)
    {
        var sourceChildren = sourceElement.Elements().ToList();
        var outputChildren = outputElement.Elements().ToList();
        if (sourceChildren.Count <= 1
            || sourceChildren.Count != outputChildren.Count)
        {
            return;
        }

        var keyAttributes = sourceGroups.Keys
            .Union(outputGroups.Keys, StringComparer.Ordinal)
            .ToDictionary(
                childName => childName,
                childName =>
                {
                    sourceGroups.TryGetValue(childName, out var sourceChildrenForName);
                    outputGroups.TryGetValue(childName, out var outputChildrenForName);
                    return SelectIdentityAttribute(sourceChildrenForName ?? [], outputChildrenForName ?? []);
                },
                StringComparer.Ordinal);

        var sourceTokens = BuildOrderTokens(sourceChildren, sourceGroups, keyAttributes);
        var outputTokens = BuildOrderTokens(outputChildren, outputGroups, keyAttributes);
        if (sourceTokens.SequenceEqual(outputTokens, StringComparer.Ordinal))
        {
            return;
        }

        var sourceOrdered = sourceTokens.OrderBy(token => token, StringComparer.Ordinal).ToArray();
        var outputOrdered = outputTokens.OrderBy(token => token, StringComparer.Ordinal).ToArray();
        if (!sourceOrdered.SequenceEqual(outputOrdered, StringComparer.Ordinal))
        {
            return;
        }

        differences.Add(new GpifXmlDifference(
            Code: "RAW_XML_CHILD_ORDER_DRIFT",
            Path: path,
            Message: $"GPIF child order changed at {path}.",
            SourceValue: CreateValuePreview(string.Join(" ", sourceTokens)),
            OutputValue: CreateValuePreview(string.Join(" ", outputTokens))));
    }

    private static string? SelectIdentityAttribute(
        IReadOnlyList<XElement> sourceChildren,
        IReadOnlyList<XElement> outputChildren)
    {
        if (sourceChildren.Count == 0 && outputChildren.Count == 0)
        {
            return null;
        }

        foreach (var candidate in IdentityAttributeCandidates)
        {
            if (HasUniqueAttributeValues(sourceChildren, candidate)
                && HasUniqueAttributeValues(outputChildren, candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    private static bool HasUniqueAttributeValues(IReadOnlyList<XElement> elements, string attributeName)
    {
        if (elements.Count == 0)
        {
            return true;
        }

        var values = new HashSet<string>(StringComparer.Ordinal);
        foreach (var element in elements)
        {
            var value = element.Attribute(attributeName)?.Value;
            if (string.IsNullOrWhiteSpace(value) || !values.Add(value))
            {
                return false;
            }
        }

        return true;
    }

    private static string[] BuildOrderTokens(
        IReadOnlyList<XElement> children,
        IReadOnlyDictionary<string, List<XElement>> groups,
        IReadOnlyDictionary<string, string?> keyAttributes)
    {
        var counters = new Dictionary<string, int>(StringComparer.Ordinal);
        var tokens = new string[children.Count];

        for (var index = 0; index < children.Count; index++)
        {
            var child = children[index];
            var childName = child.Name.LocalName;
            keyAttributes.TryGetValue(childName, out var keyAttribute);
            if (keyAttribute is not null && child.Attribute(keyAttribute)?.Value is { Length: > 0 } keyValue)
            {
                tokens[index] = $"{childName}[@{keyAttribute}='{EscapeForPath(keyValue)}']";
                continue;
            }

            if (groups.TryGetValue(childName, out var group) && group.Count > 1)
            {
                counters.TryGetValue(childName, out var counter);
                counter++;
                counters[childName] = counter;
                tokens[index] = $"{childName}[{counter}]";
                continue;
            }

            tokens[index] = childName;
        }

        return tokens;
    }

    private static string CreateElementPreview(XElement element)
        => CreateValuePreview(element.ToString(SaveOptions.DisableFormatting));

    private static string CreateValuePreview(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return string.Empty;
        }

        const int maxLength = 240;
        if (value.Length <= maxLength)
        {
            return value;
        }

        return $"[len={value.Length}] {value[..maxLength]}...";
    }

    private static string EscapeForPath(string value)
        => value.Replace("'", "\\'", StringComparison.Ordinal);
}

internal sealed record GpifXmlDifference(
    string Code,
    string Path,
    string Message,
    string? SourceValue,
    string? OutputValue);
