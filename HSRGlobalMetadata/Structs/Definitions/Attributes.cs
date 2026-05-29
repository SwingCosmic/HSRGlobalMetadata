namespace HSRGlobalMetadata.Structs.Definitions;

using System;

[Flags]
public enum FieldAttributes : uint {
    FieldAccessMask = 0x0007,
    CompilerControlled = 0x0000,
    Private = 0x0001,
    FamAndAssem = 0x0002,
    Assembly = 0x0003,
    Family = 0x0004,
    FamOrAssem = 0x0005,
    Public = 0x0006,
    Static = 0x0010,
    InitOnly = 0x0020,
    Literal = 0x0040,
    NotSerialized = 0x0080,
    SpecialName = 0x0200,
    PInvokeImpl = 0x2000,
    ReservedMask = 0x9500,
    RtSpecialName = 0x0400,
    HasFieldMarshal = 0x1000,
    HasDefault = 0x8000,
    HasFieldRva = 0x0100,
}

[Flags]
public enum MethodAttributes : uint {
    MemberAccessMask = 0x0007,
    CompilerControlled = 0x0000,
    Private = 0x0001,
    FamAndAssem = 0x0002,
    Assem = 0x0003,
    Family = 0x0004,
    FamOrAssem = 0x0005,
    Public = 0x0006,
    Static = 0x0010,
    Final = 0x0020,
    Virtual = 0x0040,
    HideBySignature = 0x0080,
    VtableLayoutMask = 0x0100,
    ReuseSlot = 0x0000,
    NewSlot = 0x0100,
    Strict = 0x0200,
    Abstract = 0x0400,
    SpecialName = 0x0800,
    PInvokeImpl = 0x2000,
    UnmanagedExport = 0x0008,
}

[Flags]
public enum TypeAttributes : uint {
    VisibilityMask = 0x00000007,
    NotPublic = 0x00000000,
    Public = 0x00000001,
    NestedPublic = 0x00000002,
    NestedPrivate = 0x00000003,
    NestedFamily = 0x00000004,
    NestedAssembly = 0x00000005,
    NestedFamAndAssem = 0x00000006,
    NestedFamOrAssem = 0x00000007,
    LayoutMask = 0x00000018,
    AutoLayout = 0x00000000,
    SequentialLayout = 0x00000008,
    ExplicitLayout = 0x00000010,
    ClassSemanticMask = 0x00000020,
    Class = 0x00000000,
    Interface = 0x00000020,
    Abstract = 0x00000080,
    Sealed = 0x00000100,
    SpecialName = 0x00000400,
    Import = 0x00001000,
    Serializable = 0x00002000,
    StringFormatMask = 0x00030000,
    AnsiClass = 0x00000000,
    UnicodeClass = 0x00010000,
    AutoClass = 0x00020000,
    BeforeFieldInit = 0x00100000,
    Forwarder = 0x00200000,
    ReservedMask = 0x00040800,
    RtSpecialName = 0x00000800,
    HasSecurity = 0x00040000,
}

public static class AttributeFormatter {
    public static string FormatField(FieldAttributes attr) {
        var attributes = new List<string>();
        var parts = new List<string>();

        if (attr.HasFlag(FieldAttributes.NotSerialized))
            attributes.Add("[NonSerialized]");

        switch (attr & FieldAttributes.FieldAccessMask) {
            case FieldAttributes.Public: parts.Add("public"); break;
            case FieldAttributes.Private: parts.Add("private"); break;
            case FieldAttributes.Family: parts.Add("protected"); break;
            case FieldAttributes.Assembly: parts.Add("internal"); break;
            case FieldAttributes.FamOrAssem: parts.Add("protected internal"); break;
            case FieldAttributes.FamAndAssem: parts.Add("private protected"); break;
        }

        bool isConst = attr.HasFlag(FieldAttributes.Literal);
        bool isStatic = attr.HasFlag(FieldAttributes.Static);
        bool isReadonly = attr.HasFlag(FieldAttributes.InitOnly);

        if (isConst) {
            parts.Add("const");
        } else {
            if (isStatic) parts.Add("static");
            if (isReadonly) parts.Add("readonly");
        }

        return string.Join("\n", attributes) + (attributes.Count > 0 ? "\n" : "") + string.Join(" ", parts);
    }

    public static string FormatMethod(MethodAttributes attr) {
        var parts = new List<string>();

        switch (attr & MethodAttributes.MemberAccessMask) {
            case MethodAttributes.Public: parts.Add("public"); break;
            case MethodAttributes.Private: parts.Add("private"); break;
            case MethodAttributes.Family: parts.Add("protected"); break;
            case MethodAttributes.Assem: parts.Add("internal"); break;
            case MethodAttributes.FamOrAssem: parts.Add("protected internal"); break;
            case MethodAttributes.FamAndAssem: parts.Add("private protected"); break;
        }

        if (attr.HasFlag(MethodAttributes.Static))
            parts.Add("static");

        bool isNewSlot = (attr & MethodAttributes.VtableLayoutMask) == MethodAttributes.NewSlot;
        bool isReuseSlot = (attr & MethodAttributes.VtableLayoutMask) == MethodAttributes.ReuseSlot;

        if (attr.HasFlag(MethodAttributes.Abstract)) {
            parts.Add("abstract");
            if (isReuseSlot) parts.Add("override");
        } else if (attr.HasFlag(MethodAttributes.Final)) {
            if (isReuseSlot) {
                parts.Add("sealed");
                parts.Add("override");
            }
        } else if (attr.HasFlag(MethodAttributes.Virtual)) {
            if (isNewSlot) parts.Add("virtual");
            else parts.Add("override");
        }

        if (attr.HasFlag(MethodAttributes.PInvokeImpl)) parts.Add("extern");

        return string.Join(" ", parts);
    }

    public static string FormatType(TypeAttributes attr, int bitfield, string pad = "") {
        bool isValueType = (bitfield & 1) != 0;
        bool isEnum = ((bitfield >> 1) & 1) != 0;
    
        var attributes = new List<string>();
        var parts = new List<string>();
    
        if (attr.HasFlag(TypeAttributes.Serializable))
            attributes.Add("[Serializable]");
    
        if (attr.HasFlag(TypeAttributes.Import))
            attributes.Add("[ComImport]");
    
        var layout = attr & TypeAttributes.LayoutMask;
        var charset = attr & TypeAttributes.StringFormatMask;
    
        if (layout == TypeAttributes.SequentialLayout || layout == TypeAttributes.ExplicitLayout) {
            string layoutKind = layout == TypeAttributes.ExplicitLayout
                ? "LayoutKind.Explicit"
                : "LayoutKind.Sequential";
    
            string charSetStr = charset switch {
                TypeAttributes.UnicodeClass => ", CharSet = CharSet.Unicode",
                TypeAttributes.AnsiClass    => ", CharSet = CharSet.Ansi",
                _ => ""
            };
    
            attributes.Add($"[StructLayout({layoutKind}{charSetStr})]");
        }
    
        switch (attr & TypeAttributes.VisibilityMask) {
            case TypeAttributes.Public:
            case TypeAttributes.NestedPublic:
                parts.Add("public"); break;
    
            case TypeAttributes.NotPublic:
            case TypeAttributes.NestedAssembly:
                parts.Add("internal"); break;
    
            case TypeAttributes.NestedPrivate:
                parts.Add("private"); break;
    
            case TypeAttributes.NestedFamily:
                parts.Add("protected"); break;
    
            case TypeAttributes.NestedFamOrAssem:
                parts.Add("protected internal"); break;
    
            case TypeAttributes.NestedFamAndAssem:
                parts.Add("private protected"); break;
        }
    
        bool isInterface = attr.HasFlag(TypeAttributes.Interface);
        bool isAbstract = attr.HasFlag(TypeAttributes.Abstract);
        bool isSealed = attr.HasFlag(TypeAttributes.Sealed);
    
        if (!isInterface && !isEnum) {
            if (isAbstract && isSealed) {
                parts.Add("static");
            } else {
                if (isAbstract) parts.Add("abstract");
                if (isSealed) parts.Add("sealed");
            }
        }
    
        if (isEnum)
            parts.Add("enum");
        else if (isInterface)
            parts.Add("interface");
        else if (isValueType)
            parts.Add("struct");
        else
            parts.Add("class");
    
        return string.Join($"\n{pad}", attributes) + (attributes.Count > 0 ? $"\n{pad}" : "") + string.Join(" ", parts);
    }
}