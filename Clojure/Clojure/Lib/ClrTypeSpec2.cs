using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace clojure.lang.TypeName2;

// Inspired by similar code in various places
// See  https://github.com/mono/mono/blob/master/mcs/class/corlib/System/TypeSpec.cs
// and see http://www.java2s.com/Open-Source/ASP.NET/Library/sixpack-library/SixPack/Reflection/TypeName.cs.htm 
// The EBNF for fully-qualified type names is here: http://msdn.microsoft.com/en-us/library/yfsftwz6(v=VS.100).aspx
// I primarily followed the mono version. Modifications have to do with assembly and type resolver defaults and some minor details.
// Also, rather than throwing exceptions for badly formed names, we just return null.  Where this is called, generally, an error is not required.
//
// Giving credit where credit is due: please note the following attributions in the mono code:
//
//          Author:
//            Rodrigo Kumpera <kumpera@gmail.com>
//
//          Copyright (C) 2010 Novell, Inc (http://www.novell.com)
//
// Since my initial pull in 2012, Mono made at least seven commits updating this code.
// 2025.09.11 -- bring this code up to date.


// A TypeName is wrapper around type names in display form
// (that is, with special characters escaped).
//
// Note that in general if you unescape a type name, you will
// lose information: If the type name's DisplayName is
// Foo\+Bar+Baz (outer class ``Foo+Bar``, inner class Baz)
// unescaping the first plus will give you (outer class Foo,
// inner class Bar, innermost class Baz).
//
// The correct way to take a TypeName apart is to feed its
// DisplayName to TypeSpec.Parse()
//
public interface IClrTypeName : IEquatable<IClrTypeName>
{
    string DisplayName { get; }

    int ImplicitGenericCount { get; set; } // used in some places to track generic arity


    // add a nested name under this one.
    //IClrTypeName NestedName(IClrTypeIdentifier innerName);
}

// A type identifier is a single component of a type name.
// Unlike a general typename, a type identifier can be be
// converted to internal form without loss of information.
public interface IClrTypeIdentifier : IClrTypeName
{
    string InternalName { get; }
}

internal class ClrTypeNames
{
    internal static IClrTypeName FromDisplay(string displayName)
    {
        return new Display(displayName);
    }

    internal abstract class ATypeName : IClrTypeName
    {
        public abstract string DisplayName { get; }
        public int ImplicitGenericCount { get; set; } = 0;

        //public abstract IClrTypeName NestedName(IClrTypeIdentifier innerName);

        public bool Equals(IClrTypeName other)
        {
            return other != null && DisplayName == other.DisplayName;
        }

        public override int GetHashCode()
        {
            return DisplayName.GetHashCode();
        }

        public override bool Equals(object other)
        {
            return Equals(other as IClrTypeName);
        }
    }


    internal class Display : ATypeName
    {
        string _displayName;

        internal Display(string displayName)
        {
            this._displayName = displayName;
        }

        public override string DisplayName { get { return _displayName; } }

        //public override IClrTypeName NestedName(IClrTypeIdentifier innerName)
        //{
        //    return new Display(DisplayName + "+" + innerName.DisplayName);
        //}

    }
}

internal class ClrTypeIdentifiers
{

    internal static IClrTypeIdentifier FromDisplay(string displayName)
    {
        return new Display(displayName);
    }

    private class Display : ClrTypeNames.ATypeName, IClrTypeIdentifier
    {
        string displayName;
        string internal_name; //cached

        internal Display(string displayName)
        {
            this.displayName = displayName;
            internal_name = null;
        }

        public override string DisplayName
        {
            get { return displayName; }
        }

        public string InternalName
        {
            get
            {
                if (internal_name == null)
                    internal_name = GetInternalName();
                return internal_name;
            }
        }

        private string GetInternalName()
        {
            return ClrTypeSpec.UnescapeInternalName(displayName);
        }

        //public override IClrTypeName NestedName(IClrTypeIdentifier innerName)
        //{
        //    return ClrTypeNames.FromDisplay(DisplayName + "+" + innerName.DisplayName);
        //}
    }
}

public interface IClrModifierSpec
{
    Type Resolve(Type type);
    StringBuilder Append(StringBuilder sb);
}

public class ClrArraySpec : IClrModifierSpec
{

    // dimensions == 1 and bound, or dimensions > 1 and !bound
    private readonly int _dimensions;
    private readonly bool _isBound;

    public ClrArraySpec(int dimensions, bool bound)
    {
        this._dimensions = dimensions;
        this._isBound = bound;
    }

    public override bool Equals(object obj)
    {
        var o = obj as ClrArraySpec;
        if (o == null)
            return false;
        return o._dimensions == _dimensions && o._isBound == _isBound;
    }

    public override int GetHashCode()
    {
        return 37 * _dimensions.GetHashCode() + IsBound.GetHashCode();
    }

    public Type Resolve(Type type)
    {
        if (_isBound)
            return type.MakeArrayType(1);
        else if (_dimensions == 1)
            return type.MakeArrayType();
        return type.MakeArrayType(_dimensions);
    }

    public StringBuilder Append(StringBuilder sb)
    {
        if (_isBound)
            return sb.Append("[*]");
        return sb.Append('[')
            .Append(',', _dimensions - 1)
            .Append(']');
    }

    public override string ToString() => Append(new StringBuilder()).ToString();

    public int Rank => _dimensions;

    public bool IsBound => _isBound;


}

public class ClrPointerSpec : IClrModifierSpec
{
    int pointer_level;

    public ClrPointerSpec(int pointer_level)
    {
        this.pointer_level = pointer_level;
    }

    public override bool Equals(object obj)
    {
        var o = obj as ClrPointerSpec;
        if (o == null)
            return false;
        return o.pointer_level == pointer_level;
    }

    override public int GetHashCode() => pointer_level.GetHashCode();

    public Type Resolve(Type type)
    {
        for (int i = 0; i < pointer_level; ++i)
            type = type.MakePointerType();
        return type;
    }

    public StringBuilder Append(StringBuilder sb) => sb.Append('*', pointer_level);

    public override string ToString() => Append(new StringBuilder()).ToString();
}

public class ClrTypeSpec
{
    #region Data

    IClrTypeIdentifier _name;
    string _assemblyName;
    List<IClrTypeIdentifier> _nested;
    List<ClrTypeSpec> _genericParams;
    List<IClrModifierSpec> _modifierSpec;
    bool _isByRef;

    string _displayFullname;  // cache

    #endregion

    #region Accessors

    public bool HasModifiers => _modifierSpec is not null;
    public bool IsNested => _nested is not null && _nested.Count > 0;
    public bool IsByRef => _isByRef;
    public IClrTypeName Name => _name;
    public string AssemblyName => _assemblyName;

    public IEnumerable<IClrTypeName> Nested
    {
        get
        {
            if (_nested != null)
                return _nested;
            else
                return Array.Empty<IClrTypeName>();
        }
    }

    public IEnumerable<IClrModifierSpec> Modifiers
    {
        get
        {
            if (_modifierSpec != null)
                return _modifierSpec;
            else
                return Array.Empty<IClrModifierSpec>();
        }
    }

    public IEnumerable<ClrTypeSpec> GenericParams
    {
        get
        {
            if (_genericParams != null)
                return _genericParams;
            else
                return Array.Empty<ClrTypeSpec>();
        }
    }

    #endregion

    #region Display name-related

    [Flags]
    internal enum DisplayNameFormat
    {
        Default = 0x0,
        WANT_ASSEMBLY = 0x1,
        NO_MODIFIERS = 0x2,
    }

    //#if DEBUG
    public override string ToString()
    {
        return GetDisplayFullName(DisplayNameFormat.WANT_ASSEMBLY);
    }
    //#endif

    string GetDisplayFullName(DisplayNameFormat flags)
    {
        bool wantAssembly = (flags & DisplayNameFormat.WANT_ASSEMBLY) != 0;
        bool wantModifiers = (flags & DisplayNameFormat.NO_MODIFIERS) == 0;

        var sb = new StringBuilder(_name.DisplayName);

        if (_nested is not null)
        {
            foreach (var n in _nested)
                sb.Append('+').Append(n.DisplayName);
        }

        if (_genericParams is not null)
        {
            sb.Append('[');
            for (int i = 0; i < _genericParams.Count; ++i)
            {
                if (i > 0)
                    sb.Append(", ");
                if (_genericParams[i]._assemblyName != null)
                    sb.Append('[').Append(_genericParams[i].DisplayFullName).Append(']');
                else
                    sb.Append(_genericParams[i].DisplayFullName);
            }
            sb.Append(']');
        }

        if (wantModifiers)
            GetModifierString(sb);

        if (_assemblyName != null && wantAssembly)
            sb.Append(", ").Append(_assemblyName);

        return sb.ToString();
    }

    internal string ModifierString() => GetModifierString(new StringBuilder()).ToString();

    private StringBuilder GetModifierString(StringBuilder sb)
    {
        if (_modifierSpec is not null)
        {
            foreach (var md in _modifierSpec)
                md.Append(sb);
        }

        if (_isByRef)
            sb.Append('&');

        return sb;
    }

    internal string DisplayFullName
    {
        get
        {
            if (_displayFullname is null)
                _displayFullname = GetDisplayFullName(DisplayNameFormat.Default);
            return _displayFullname;
        }
    }

    internal static string EscapeDisplayName(string internalName)
    {
        // initial capacity = length of internalName.
        // Maybe we won't have to escape anything.
        var res = new StringBuilder(internalName.Length);
        foreach (char c in internalName)
        {
            switch (c)
            {
                case '+':
                case ',':
                case '[':
                case ']':
                case '*':
                case '&':
                case '\\':
                    res.Append('\\').Append(c);
                    break;
                default:
                    res.Append(c);
                    break;
            }
        }
        return res.ToString();
    }

    internal static string UnescapeInternalName(string displayName)
    {
        var res = new StringBuilder(displayName.Length);
        for (int i = 0; i < displayName.Length; ++i)
        {
            char c = displayName[i];
            if (c == '\\')
                if (++i < displayName.Length)
                    c = displayName[i];
            res.Append(c);
        }
        return res.ToString();
    }

    internal static bool NeedsEscaping(string internalName)
    {
        foreach (char c in internalName)
        {
            switch (c)
            {
                case ',':
                case '+':
                case '*':
                case '&':
                case '[':
                case ']':
                case '\\':
                    return true;
                default:
                    break;
            }
        }
        return false;
    }

    #endregion

    #region Parsing support

    void AddName(string type_name)
    {
        if (_name is null)
        {
            _name = ParsedTypeIdentifier(type_name);
        }
        else
        {
            if (_nested == null)
                _nested = new List<IClrTypeIdentifier>();
            _nested.Add(ParsedTypeIdentifier(type_name));
        }
    }

    void AddModifier(IClrModifierSpec md)
    {
        if (_modifierSpec is null)
            _modifierSpec = new List<IClrModifierSpec>();
        _modifierSpec.Add(md);
    }

    static void SkipSpace(string name, ref int pos)
    {
        int p = pos;
        while (p < name.Length && Char.IsWhiteSpace(name[p]))
            ++p;
        pos = p;
    }

    static void BoundCheck(int idx, string s)
    {
        if (idx >= s.Length)
            throw new ArgumentException("Invalid generic arguments spec", "typeName");
    }

    static IClrTypeIdentifier ParsedTypeIdentifier(string displayName)
    {
        return ClrTypeIdentifiers.FromDisplay(displayName);
    }

    void SetGenericArgumentCount(int count)
    {
        if (_name == null)
            throw new InvalidOperationException("Type name not set");

        if (IsNested)
            _nested.Last().ImplicitGenericCount = count;
        else
            _name.ImplicitGenericCount = count;
    }

    void MergeNested(ClrTypeSpec nestedSpec)
    {
        // append any generic arguments to the current type
        if (nestedSpec._genericParams != null)
        {
            if (_genericParams == null)
                _genericParams = new List<ClrTypeSpec>();
            _genericParams.AddRange(nestedSpec._genericParams);
        }

        // Append all nested names to the current type
        if (_nested == null)
            _nested = new List<IClrTypeIdentifier>();
        _nested.Add(nestedSpec._name);

        if (nestedSpec._nested != null)
        {
            _nested.AddRange(nestedSpec._nested);
        }
    }


    #endregion

    #region Parsing

    public static ClrTypeSpec Parse(string typeName)
    {
        int pos = 0;
        if (typeName == null)
            throw new ArgumentNullException("typeName");

        ClrTypeSpec res = Parse(typeName, ref pos, false, true, true);
        if (pos < typeName.Length)
            throw new ArgumentException("Count not parse the whole type name", "typeName");
        return res;
    }

    static ClrTypeSpec Parse(string name, ref int p, bool is_recurse, bool allow_aqn, bool allow_mods)
    {
        // Invariants:
        //  - On exit p, is updated to pos the current unconsumed character.
        //
        //  - The callee peeks at but does not consume delimiters following
        //    recurisve parse (so for a recursive call like the args of "Foo[P,Q]"
        //    we'll return with p either on ',' or on ']'.  If the name was aqn'd
        //    "Foo[[P,assmblystuff],Q]" on return p with be on the ']' just
        //    after the "assmblystuff")
        //
        //  - If allow_aqn is True, assembly qualification is optional.
        //    If allow_aqn is False, assembly qualification is prohibited.
        //  - If is_recurse is True, we are parsing a generic argument.
        //    If is_recurse is False, we are parsing a top-level type name.
        //   - If allow_mods is False, we are recursively parsing a nested name just after a generic argument list.
        //     In this case, we allow modifiers (array, pointer, byref) after the generic argument list.
        int pos = p;

        int name_start;
        bool in_modifiers = false;
        ClrTypeSpec data = new();

        SkipSpace(name, ref pos);

        name_start = pos;

        for (; pos < name.Length; ++pos)
        {
            switch (name[pos])
            {
                case '+':
                    data.AddName(name.Substring(name_start, pos - name_start));
                    name_start = pos + 1;
                    break;
                case ',':
                case ']':
                    data.AddName(name.Substring(name_start, pos - name_start));
                    name_start = pos + 1;
                    in_modifiers = true;
                    if (is_recurse && !allow_aqn)
                    {
                        p = pos;
                        return data;
                    }
                    break;
                case '&':
                case '*':
                case '[':
                    if (name[pos] != '[' && is_recurse)
                        throw new ArgumentException("Generic argument can't be byref or pointer type", "typeName");
                    data.AddName(name.Substring(name_start, pos - name_start));
                    name_start = pos + 1;
                    in_modifiers = true;
                    break;
                case '\\':
                    pos++;
                    break;
            }
            if (in_modifiers)
                break;
        }

        if (name_start < pos)
            data.AddName(name.Substring(name_start, pos - name_start));
        else if (name_start == pos)
            data.AddName(String.Empty);

        if (in_modifiers)
        {
            for (; pos < name.Length; ++pos)
            {

                switch (name[pos])
                {
                    case '&':
                        if (!allow_mods)
                        {
                            p = pos;
                            return data;
                        }
                        if (data._isByRef)
                            throw new ArgumentException("Can't have a byref of a byref", "typeName");

                        data._isByRef = true;
                        break;
                    case '*':
                        if (!allow_mods)
                        {
                            p = pos;
                            return data;
                        }
                        if (data._isByRef)
                            throw new ArgumentException("Can't have a pointer to a byref type", "typeName");
                        // take subsequent '*'s too
                        int pointer_level = 1;
                        while (pos + 1 < name.Length && name[pos + 1] == '*')
                        {
                            ++pos;
                            ++pointer_level;
                        }
                        data.AddModifier(new ClrPointerSpec(pointer_level));
                        break;
                    case ',':
                        if (!allow_mods)
                        {
                            p = pos;
                            return data;
                        }
                        if (is_recurse && allow_aqn)
                        {
                            int end = pos;
                            while (end < name.Length && name[end] != ']')
                                ++end;
                            if (end >= name.Length)
                                throw new ArgumentException("Unmatched ']' while parsing generic argument assembly name");
                            data._assemblyName = name.Substring(pos + 1, end - pos - 1).Trim();
                            p = end;
                            return data;
                        }
                        if (is_recurse)
                        {
                            p = pos;
                            return data;
                        }
                        if (allow_aqn)
                        {
                            data._assemblyName = name.Substring(pos + 1).Trim();
                            pos = name.Length;
                        }
                        break;
                    case '[':

                        // We need indefinite lookahead (SkipSpace) to figure out if we have generic arguments or an array spec.
                        // We cache the current position so we can restore it in case we need to exit (when array spec && ! allow_mods)

                        int pos_cache = pos;

                        if (data._isByRef)
                            throw new ArgumentException("Byref qualifier must be the last one of a type", "typeName");
                        ++pos;
                        if (pos >= name.Length)
                            throw new ArgumentException("Invalid array/generic spec", "typeName");
                        SkipSpace(name, ref pos);

                        if (name[pos] != ',' && name[pos] != '*' && name[pos] != ']')
                        {//generic args
                            List<ClrTypeSpec> args = new();
                            if (data.HasModifiers)
                                throw new ArgumentException("generic args after array spec or pointer type", "typeName");

                            while (pos < name.Length)
                            {
                                SkipSpace(name, ref pos);
                                bool aqn = name[pos] == '[';
                                if (aqn)
                                    ++pos; //skip '[' to the start of the type
                                args.Add(Parse(name, ref pos, true, aqn, true));
                                BoundCheck(pos, name);
                                if (aqn)
                                {
                                    if (name[pos] == ']')
                                        ++pos;
                                    else
                                        throw new ArgumentException("Unclosed assembly-qualified type name at " + name[pos], "typeName");   // Is this possible?  AQN Ends with ] pending.
                                    BoundCheck(pos, name);
                                }

                                if (name[pos] == ']')
                                    break;
                                if (name[pos] == ',')
                                    ++pos; // skip ',' to the start of the next arg
                                else
                                    throw new ArgumentException("Invalid generic arguments separator " + name[pos], "typeName");

                            }
                            if (pos >= name.Length || name[pos] != ']')
                                throw new ArgumentException("Error parsing generic params spec", "typeName");
                            data._genericParams = args;
                            data.SetGenericArgumentCount(args.Count);
                            if (pos + 1 < name.Length && name[pos + 1] == '+')
                            {
                                // We have a nested type after a generic argument list.  (Extension to the original syntax.)
                                // Recursively parse to pick up the remainder, then merge the results.
                                ++pos; //skip '+'
                                var nested = Parse(name, ref pos, true, false, false);
                                data.MergeNested(nested);
                            }
                        }
                        else
                        { //array spec

                            if (!allow_mods)
                            {
                                // We have an array spec (a mod) and we are not allowing mods
                                // Backup to the position of the [ and get us out of here.
                                p = pos_cache;
                                return data;
                            }

                            int dimensions = 1;
                            bool bound = false;
                            while (pos < name.Length && name[pos] != ']')
                            {
                                if (name[pos] == '*')
                                {
                                    if (bound)
                                        throw new ArgumentException("Array spec cannot have 2 bound dimensions", "typeName");
                                    bound = true;
                                }
                                else if (name[pos] != ',')
                                    throw new ArgumentException("Invalid character in array spec " + name[pos], "typeName");
                                else
                                    ++dimensions;

                                ++pos;
                                SkipSpace(name, ref pos);
                            }
                            if (pos >= name.Length || name[pos] != ']')
                                throw new ArgumentException("Error parsing array spec", "typeName");
                            if (dimensions > 1 && bound)
                                throw new ArgumentException("Invalid array spec, multi-dimensional array cannot be bound", "typeName");
                            data.AddModifier(new ClrArraySpec(dimensions, bound));
                        }

                        break;
                    case ']':
                        if (is_recurse)
                        {
                            p = pos;
                            return data;
                        }
                        throw new ArgumentException("Unmatched ']'", "typeName");
                    default:
                        throw new ArgumentException("Bad type def, can't handle '" + name[pos] + "'" + " at " + pos, "typeName");
                }
            }
        }

        p = pos;
        return data;
    }

    #endregion

    #region  Resolving

    // We have to get rid of all references to StackCrawlMark -- just not something we have access to.

    public Type Resolve(
        Func<AssemblyName, Assembly> assemblyResolver,
        Func<Assembly, string, bool, Type> typeResolver,
        bool throwOnError,
        bool ignoreCase /*, ref System.Threading.StackCrawlMark stackMark*/)
    {
        Assembly asm = null;

        //if (assemblyResolver == null && typeResolver == null)
        //    return RuntimeType.GetType(DisplayFullName, throwOnError, ignoreCase, false, ref stackMark);
        // We don't have access to RuntimeType, so we just punt.  We will always call with one or the other of assemblyResolver or typeResolver.

        if (assemblyResolver is null && typeResolver is null)
            throw new ArgumentException("At least one of assemblyResolver or typeResolver must be non-null");

        if (_assemblyName != null)
        {
            if (assemblyResolver != null)
                asm = assemblyResolver(new AssemblyName(_assemblyName));
            else
                asm = Assembly.Load(_assemblyName);

            if (asm == null)
            {
                if (throwOnError)
                    throw new FileNotFoundException("Could not resolve assembly '" + _assemblyName + "'");
                return null;
            }
        }

        Type type = null;
        if (typeResolver is not null)
            type = typeResolver(asm, _name.DisplayName, ignoreCase);
        else
            type = asm.GetType(_name.DisplayName, false, ignoreCase);

        if (type is null)
        {
            if (throwOnError)
                throw new TypeLoadException("Could not resolve type '" + _name + "'");
            return null;
        }

        if (_nested != null)
        {
            foreach (var n in _nested)
            {
                var tmp = type.GetNestedType(n.DisplayName, BindingFlags.Public | BindingFlags.NonPublic);
                if (tmp == null)
                {
                    if (throwOnError)
                        throw new TypeLoadException("Could not resolve type '" + n + "'");
                    return null;
                }
                type = tmp;
            }
        }

        if (_genericParams != null)
        {
            Type[] args = new Type[_genericParams.Count];
            for (int i = 0; i < args.Length; ++i)
            {
                var tmp = _genericParams[i].Resolve(assemblyResolver, typeResolver, throwOnError, ignoreCase /*, ref stackMark */);
                if (tmp == null)
                {
                    if (throwOnError)
                        throw new TypeLoadException("Could not resolve type '" + _genericParams[i]._name + "'");
                    return null;
                }
                args[i] = tmp;
            }
            type = type.MakeGenericType(args);
        }

        if (_modifierSpec != null)
        {
            foreach (var md in _modifierSpec)
                type = md.Resolve(type);
        }

        if (_isByRef)
            type = type.MakeByRefType();

        return type;
    }

    #endregion


    #region Entry point

    public static Type GetTypeFromName(string name)
    {
        ClrTypeSpec spec = Parse(name);
        if (spec == null)
            return null;
        return spec.Resolve(
            assyName => Assembly.Load(assyName),
            //(assy, typeName) => assy == null ? RT.classForName(typeName) : assy.GetType(typeName));  <--- this goes into an infinite loop on a non-existent typename
            (assy, typeName, ignoreCase) => assy == null ? (name.Equals(typeName) ? null : RT.classForName(typeName)) : assy.GetType(typeName),
            false,
            false);
    }

    #endregion

}

//        private readonly int _dimensions;
//        private readonly bool _isBound;

//        #endregion

//        #region C-tors

//        internal ClrArraySpec2(int dimensions, bool bound)
//        {
//            this._dimensions = dimensions;
//            this._isBound = bound;
//        }

//        #endregion

//        #region Resolving

//        internal Type Resolve(Type type)
//        {
//            if (_isBound)
//                return type.MakeArrayType(1);
//            else if (_dimensions == 1)
//                return type.MakeArrayType();
//            return type.MakeArrayType(_dimensions);
//        }

//        #endregion
//    }

//    public class ClrTypeSpec2
//    {
//        #region Data

//        string _name;
//        string _assemblyName;
//        List<string> _nested;
//        List<ClrTypeSpec2> _genericParams;
//        List<ClrArraySpec2> _arraySpec;
//        int _pointerLevel;
//        bool _isByRef;

//        #endregion

//        #region Entry point

//        public static Type GetTypeFromName(string name, Namespace ns = null)
//        {
//            ClrTypeSpec2 spec = Parse(name);
//            if (spec == null)
//                return null;
//            return spec.Resolve(
//                ns,
//                name,
//                assyName => Assembly.Load(assyName));
//        }

//        #endregion

//        #region Parsing

//        static ClrTypeSpec2 Parse(string name)
//        {
//            int pos = 0;
//            ClrTypeSpec2 spec = Parse(name, ref pos, false, false);
//            if (spec == null)
//                return null;                                           // bad parse
//            if (pos < name.Length)
//                return null;                                           // ArgumentException ("Count not parse the whole type name", "typeName");
//            return spec;
//        }

//        static ClrTypeSpec2 Parse(string name, ref int p, bool isRecursive, bool allowAssyQualName)
//        {
//            int pos = p;
//            int name_start;
//            bool hasModifiers = false;
//            ClrTypeSpec2 spec = new();

//            SkipSpace(name, ref pos);

//            name_start = pos;

//            for (; pos < name.Length; ++pos)
//            {
//                switch (name[pos])
//                {
//                    case '+':
//                        spec.AddName(name.Substring(name_start, pos - name_start));
//                        name_start = pos + 1;
//                        break;
//                    case ',':
//                    case ']':
//                        spec.AddName(name.Substring(name_start, pos - name_start));
//                        name_start = pos + 1;
//                        if (isRecursive && !allowAssyQualName)
//                        {
//                            p = pos;
//                            return spec;
//                        }
//                        hasModifiers = true;
//                        break;
//                    case '&':
//                    case '*':
//                    case '[':
//                        if (name[pos] != '[' && name[pos] != '<' && isRecursive)
//                            return null;                                              // ArgumentException ("Generic argument can't be byref or pointer type", "typeName");
//                        spec.AddName(name.Substring(name_start, pos - name_start));
//                        name_start = pos + 1;
//                        hasModifiers = true;
//                        break;
//                    case '\\':
//                        pos++;
//                        break;
//                }
//                if (hasModifiers)
//                    break;
//            }

//            if (name_start < pos)
//                spec.AddName(name.Substring(name_start, pos - name_start));

//            if (hasModifiers)
//            {
//                for (; pos < name.Length; ++pos)
//                {

//                    switch (name[pos])
//                    {
//                        case '&':
//                            if (spec._isByRef)
//                                return null;                                           // ArgumentException ("Can't have a byref of a byref", "typeName")

//                            spec._isByRef = true;
//                            break;
//                        case '*':
//                            if (spec._isByRef)
//                                return null;                                           // ArgumentException ("Can't have a pointer to a byref type", "typeName");
//                            ++spec._pointerLevel;
//                            break;
//                        case ',':
//                            if (isRecursive)
//                            {
//                                int end = pos;
//                                while (end < name.Length && name[end] != ']')
//                                    ++end;
//                                if (end >= name.Length)
//                                    return null;                                        // ArgumentException ("Unmatched ']' while parsing generic argument assembly name");
//                                spec._assemblyName = name.Substring(pos + 1, end - pos - 1).Trim();
//                                p = end + 1;
//                                return spec;
//                            }
//                            spec._assemblyName = name.Substring(pos + 1).Trim();
//                            pos = name.Length;
//                            break;
//                        case '[':
//                            if (spec._isByRef)
//                                return null;                                             // ArgumentException ("Byref qualifier must be the last one of a type", "typeName");
//                            ++pos;
//                            if (pos >= name.Length)
//                                return null;                                             // ArgumentException ("Invalid array/generic spec", "typeName");
//                            SkipSpace(name, ref pos);

//                            if (name[pos] != ',' && name[pos] != '*' && name[pos] != ']')
//                            {//generic args
//                                List<ClrTypeSpec2> args = new();
//                                if (spec.IsArray)
//                                    return null;                                          // ArgumentException ("generic args after array spec", "typeName");

//                                while (pos < name.Length)
//                                {
//                                    SkipSpace(name, ref pos);
//                                    bool aqn = name[pos] == '[';
//                                    if (aqn)
//                                        ++pos; //skip '[' to the start of the type
//                                    {
//                                        ClrTypeSpec2 arg = Parse(name, ref pos, true, aqn);
//                                        if (arg == null)
//                                            return null;                                   // bad generic arg
//                                        args.Add(arg);
//                                    }
//                                    if (pos >= name.Length)
//                                        return null;                                       // ArgumentException ("Invalid generic arguments spec", "typeName");

//                                    if (name[pos] == ']')
//                                        break;
//                                    if (name[pos] == ',')
//                                        ++pos; // skip ',' to the start of the next arg
//                                    else
//                                        return null;                                       // ArgumentException ("Invalid generic arguments separator " + name [pos], "typeName")

//                                }
//                                if (pos >= name.Length || name[pos] != ']')
//                                    return null;                                           // ArgumentException ("Error parsing generic params spec", "typeName");
//                                spec._genericParams = args;
//                            }
//                            else
//                            { //array spec
//                                int dimensions = 1;
//                                bool bound = false;
//                                while (pos < name.Length && name[pos] != ']')
//                                {
//                                    if (name[pos] == '*')
//                                    {
//                                        if (bound)
//                                            return null;                                    // ArgumentException ("Array spec cannot have 2 bound dimensions", "typeName");
//                                        bound = true;
//                                    }
//                                    else if (name[pos] != ',')
//                                        return null;                                        // ArgumentException ("Invalid character in array spec " + name [pos], "typeName");
//                                    else
//                                        ++dimensions;

//                                    ++pos;
//                                    SkipSpace(name, ref pos);
//                                }
//                                if (name[pos] != ']')
//                                    return null;                                            // ArgumentException ("Error parsing array spec", "typeName");
//                                if (dimensions > 1 && bound)
//                                    return null;                                            // ArgumentException ("Invalid array spec, multi-dimensional array cannot be bound", "typeName")
//                                spec.AddArray(new ClrArraySpec2(dimensions, bound));
//                            }

//                            break;

//                        case ']':
//                            if (isRecursive)
//                            {
//                                p = pos + 1;
//                                return spec;
//                            }
//                            return null;                                                    // ArgumentException ("Unmatched ']'", "typeName");
//                        default:
//                            return null;                                                    // ArgumentException ("Bad type def, can't handle '" + name [pos]+"'" + " at " + pos, "typeName");
//                    }
//                }
//            }

//            p = pos;
//            return spec;
//        }


//        void AddName(string type_name)
//        {
//            if (_name == null)
//            {
//                _name = type_name;
//            }
//            else
//            {
//                if (_nested == null)
//                    _nested = new List<string>();
//                _nested.Add(type_name);
//            }
//        }

//        static string AppendGenericCountSuffix(string name, int count)
//        {
//            return $"{name}`{count}";
//        }

//        void AppendNameGenericCountSuffix(int count)
//        {
//            if (_nested is not null)
//            {
//                var name = _nested.Last();
//                _nested.RemoveAt(_nested.Count - 1);
//                _nested.Add(AppendGenericCountSuffix(name, count));
//            }
//            else
//            {
//                _name = AppendGenericCountSuffix(_name, count);
//            }
//        }

//        static void SkipSpace(string name, ref int pos)
//        {
//            int p = pos;
//            while (p < name.Length && Char.IsWhiteSpace(name[p]))
//                ++p;
//            pos = p;
//        }

//        bool IsArray
//        {
//            get { return _arraySpec != null; }
//        }

//        void AddArray(ClrArraySpec2 array)
//        {
//            if (_arraySpec == null)
//                _arraySpec = new List<ClrArraySpec2>();
//            _arraySpec.Add(array);
//        }

//        #endregion

//        #region  Resolving

//        internal Type Resolve(
//            Namespace ns,
//            string originalTypename,
//            Func<AssemblyName, Assembly> assemblyResolver)
//        {
//            Assembly asm = null;

//            if (_assemblyName != null)
//            {
//                if (assemblyResolver != null)
//                    asm = assemblyResolver(new AssemblyName(_assemblyName));
//                else
//                    asm = Assembly.Load(_assemblyName);

//                if (asm == null)
//                    return null;
//            }

//            // if _name is same as originalTypename, then the parse is identical to what we started with.
//            // Given that ClrTypeSpec2.GetTypeFromName is called from RT.classForName, 
//            //    call RT.classForName when _name == originalTypename will set off an infinite recrusion.

//            Type type = null;

//            if (asm != null)
//                type = asm.GetType(_name);
//            else
//            {
//                type = HostExpr.maybeSpecialTag(Symbol.create(_name));

//                // check for aliases in the namespace
//                if (type is null && ns is not null)
//                {
//                    type = ns.GetMapping(Symbol.create(_name)) as Type;
//                }

//                if (type is null && (!_name?.Equals(originalTypename) ?? false))
//                    type = RT.classForName(_name);
//            }

//            if (type is null)
//                // Cannot resolve _name
//                return null;

//            if (_nested != null)
//            {
//                foreach (var n in _nested)
//                {
//                    var tmp = type.GetNestedType(n, BindingFlags.Public | BindingFlags.NonPublic);
//                    if (tmp == null)
//                        return null;
//                    type = tmp;
//                }
//            }

//            if (_genericParams != null)
//            {
//                Type[] args = new Type[_genericParams.Count];
//                for (int i = 0; i < args.Length; ++i)
//                {
//                    var tmp = _genericParams[i].Resolve(ns, originalTypename, assemblyResolver);
//                    if (tmp == null)
//                        return null;
//                    args[i] = tmp;
//                }
//                type = type.MakeGenericType(args);
//            }

//            if (_arraySpec != null)
//            {
//                foreach (var arr in _arraySpec)
//                    type = arr.Resolve(type);
//            }

//            for (int i = 0; i < _pointerLevel; ++i)
//                type = type.MakePointerType();

//            if (_isByRef)
//                type = type.MakeByRefType();

//            return type;
//        }


//        private static Type DefaultTypeResolver(Assembly assembly, string typename, Namespace ns)
//        {
//            if (assembly is not null)
//                assembly.GetType(typename);



//            //(assy, typeName) => assy == null ? (name.Equals(typeName) ? null : RT.classForName(typeName)) : assy.GetType(typeName)
//            return null;
//        }

//        #endregion
//    }
//}
