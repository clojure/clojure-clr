using clojure.lang.CljCompiler.Ast;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace clojure.lang
{
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

    class ClrArraySpec2
    {

        #region Data

        private readonly int _dimensions;
        private readonly bool _isBound;

        #endregion

        #region C-tors

        internal ClrArraySpec2(int dimensions, bool bound)
        {
            this._dimensions = dimensions;
            this._isBound = bound;
        }

        #endregion

        #region Resolving

        internal Type Resolve(Type type)
        {
            if (_isBound)
                return type.MakeArrayType(1);
            else if (_dimensions == 1)
                return type.MakeArrayType();
            return type.MakeArrayType(_dimensions);
        }

        #endregion
    }

    public class ClrTypeSpec2
    {
        #region Data

        string _name;
        string _assemblyName;
        List<string> _nested;
        List<ClrTypeSpec2> _genericParams;
        List<ClrArraySpec2> _arraySpec;
        int _pointerLevel;
        bool _isByRef;

        #endregion

        #region Entry point

        public static Type GetTypeFromName(string name, Namespace ns = null)
        {
            ClrTypeSpec2 spec = Parse(name);
            if (spec == null)
                return null;
            return spec.Resolve(
                ns,
                name,
                assyName => Assembly.Load(assyName));
        }

        #endregion

        #region Parsing

        static ClrTypeSpec2 Parse(string name)
        {
            int pos = 0;
            ClrTypeSpec2 spec = Parse(name, ref pos, false, false);
            if (spec == null)
                return null;                                           // bad parse
            if (pos < name.Length)
                return null;                                           // ArgumentException ("Count not parse the whole type name", "typeName");
            return spec;
        }

        static ClrTypeSpec2 Parse(string name, ref int p, bool isRecursive, bool allowAssyQualName)
        {
            int pos = p;
            int name_start;
            bool hasModifiers = false;
            ClrTypeSpec2 spec = new();

            SkipSpace(name, ref pos);

            name_start = pos;

            for (; pos < name.Length; ++pos)
            {
                switch (name[pos])
                {
                    case '+':
                        spec.AddName(name.Substring(name_start, pos - name_start));
                        name_start = pos + 1;
                        break;
                    case ',':
                    case ']':
                        spec.AddName(name.Substring(name_start, pos - name_start));
                        name_start = pos + 1;
                        if (isRecursive && !allowAssyQualName)
                        {
                            p = pos;
                            return spec;
                        }
                        hasModifiers = true;
                        break;
                    case '&':
                    case '*':
                    case '[':
                        if (name[pos] != '[' && name[pos] != '<' && isRecursive)
                            return null;                                              // ArgumentException ("Generic argument can't be byref or pointer type", "typeName");
                        spec.AddName(name.Substring(name_start, pos - name_start));
                        name_start = pos + 1;
                        hasModifiers = true;
                        break;
                    case '\\':
                        pos++;
                        break;
                }
                if (hasModifiers)
                    break;
            }

            if (name_start < pos)
                spec.AddName(name.Substring(name_start, pos - name_start));

            if (hasModifiers)
            {
                for (; pos < name.Length; ++pos)
                {

                    switch (name[pos])
                    {
                        case '&':
                            if (spec._isByRef)
                                return null;                                           // ArgumentException ("Can't have a byref of a byref", "typeName")

                            spec._isByRef = true;
                            break;
                        case '*':
                            if (spec._isByRef)
                                return null;                                           // ArgumentException ("Can't have a pointer to a byref type", "typeName");
                            ++spec._pointerLevel;
                            break;
                        case ',':
                            if (isRecursive)
                            {
                                int end = pos;
                                while (end < name.Length && name[end] != ']')
                                    ++end;
                                if (end >= name.Length)
                                    return null;                                        // ArgumentException ("Unmatched ']' while parsing generic argument assembly name");
                                spec._assemblyName = name.Substring(pos + 1, end - pos - 1).Trim();
                                p = end + 1;
                                return spec;
                            }
                            spec._assemblyName = name.Substring(pos + 1).Trim();
                            pos = name.Length;
                            break;
                        case '[':
                            if (spec._isByRef)
                                return null;                                             // ArgumentException ("Byref qualifier must be the last one of a type", "typeName");
                            ++pos;
                            if (pos >= name.Length)
                                return null;                                             // ArgumentException ("Invalid array/generic spec", "typeName");
                            SkipSpace(name, ref pos);

                            if (name[pos] != ',' && name[pos] != '*' && name[pos] != ']')
                            {//generic args
                                List<ClrTypeSpec2> args = new();
                                if (spec.IsArray)
                                    return null;                                          // ArgumentException ("generic args after array spec", "typeName");

                                while (pos < name.Length)
                                {
                                    SkipSpace(name, ref pos);
                                    bool aqn = name[pos] == '[';
                                    if (aqn)
                                        ++pos; //skip '[' to the start of the type
                                    {
                                        ClrTypeSpec2 arg = Parse(name, ref pos, true, aqn);
                                        if (arg == null)
                                            return null;                                   // bad generic arg
                                        args.Add(arg);
                                    }
                                    if (pos >= name.Length)
                                        return null;                                       // ArgumentException ("Invalid generic arguments spec", "typeName");

                                    if (name[pos] == ']')
                                        break;
                                    if (name[pos] == ',')
                                        ++pos; // skip ',' to the start of the next arg
                                    else
                                        return null;                                       // ArgumentException ("Invalid generic arguments separator " + name [pos], "typeName")

                                }
                                if (pos >= name.Length || name[pos] != ']')
                                    return null;                                           // ArgumentException ("Error parsing generic params spec", "typeName");
                                spec._genericParams = args;
                            }
                            else
                            { //array spec
                                int dimensions = 1;
                                bool bound = false;
                                while (pos < name.Length && name[pos] != ']')
                                {
                                    if (name[pos] == '*')
                                    {
                                        if (bound)
                                            return null;                                    // ArgumentException ("Array spec cannot have 2 bound dimensions", "typeName");
                                        bound = true;
                                    }
                                    else if (name[pos] != ',')
                                        return null;                                        // ArgumentException ("Invalid character in array spec " + name [pos], "typeName");
                                    else
                                        ++dimensions;

                                    ++pos;
                                    SkipSpace(name, ref pos);
                                }
                                if (name[pos] != ']')
                                    return null;                                            // ArgumentException ("Error parsing array spec", "typeName");
                                if (dimensions > 1 && bound)
                                    return null;                                            // ArgumentException ("Invalid array spec, multi-dimensional array cannot be bound", "typeName")
                                spec.AddArray(new ClrArraySpec2(dimensions, bound));
                            }

                            break;

                        case ']':
                            if (isRecursive)
                            {
                                p = pos + 1;
                                return spec;
                            }
                            return null;                                                    // ArgumentException ("Unmatched ']'", "typeName");
                        default:
                            return null;                                                    // ArgumentException ("Bad type def, can't handle '" + name [pos]+"'" + " at " + pos, "typeName");
                    }
                }
            }

            p = pos;
            return spec;
        }


        void AddName(string type_name)
        {
            if (_name == null)
            {
                _name = type_name;
            }
            else
            {
                if (_nested == null)
                    _nested = new List<string>();
                _nested.Add(type_name);
            }
        }

        static string AppendGenericCountSuffix(string name, int count)
        {
            return $"{name}`{count}";
        }

        void AppendNameGenericCountSuffix(int count)
        {
            if (_nested is not null)
            {
                var name = _nested.Last();
                _nested.RemoveAt(_nested.Count - 1);
                _nested.Add(AppendGenericCountSuffix(name, count));
            }
            else
            {
                _name = AppendGenericCountSuffix(_name, count);
            }
        }

        static void SkipSpace(string name, ref int pos)
        {
            int p = pos;
            while (p < name.Length && Char.IsWhiteSpace(name[p]))
                ++p;
            pos = p;
        }

        bool IsArray
        {
            get { return _arraySpec != null; }
        }

        void AddArray(ClrArraySpec2 array)
        {
            if (_arraySpec == null)
                _arraySpec = new List<ClrArraySpec2>();
            _arraySpec.Add(array);
        }

        #endregion

        #region  Resolving

        internal Type Resolve(
            Namespace ns,
            string originalTypename,
            Func<AssemblyName, Assembly> assemblyResolver)
        {
            Assembly asm = null;

            if (_assemblyName != null)
            {
                if (assemblyResolver != null)
                    asm = assemblyResolver(new AssemblyName(_assemblyName));
                else
                    asm = Assembly.Load(_assemblyName);

                if (asm == null)
                    return null;
            }

            // if _name is same as originalTypename, then the parse is identical to what we started with.
            // Given that ClrTypeSpec2.GetTypeFromName is called from RT.classForName, 
            //    call RT.classForName when _name == originalTypename will set off an infinite recrusion.

            Type type = null;

            if (asm != null)
                type = asm.GetType(_name);
            else
            {
                type = HostExpr.maybeSpecialTag(Symbol.create(_name));

                // check for aliases in the namespace
                if (type is null && ns is not null)
                {
                    type = ns.GetMapping(Symbol.create(_name)) as Type;
                }

                if (type is null && (!_name?.Equals(originalTypename) ?? false))
                    type = RT.classForName(_name);
            }

            if (type is null)
                // Cannot resolve _name
                return null;

            if (_nested != null)
            {
                foreach (var n in _nested)
                {
                    var tmp = type.GetNestedType(n, BindingFlags.Public | BindingFlags.NonPublic);
                    if (tmp == null)
                        return null;
                    type = tmp;
                }
            }

            if (_genericParams != null)
            {
                Type[] args = new Type[_genericParams.Count];
                for (int i = 0; i < args.Length; ++i)
                {
                    var tmp = _genericParams[i].Resolve(ns, originalTypename, assemblyResolver);
                    if (tmp == null)
                        return null;
                    args[i] = tmp;
                }
                type = type.MakeGenericType(args);
            }

            if (_arraySpec != null)
            {
                foreach (var arr in _arraySpec)
                    type = arr.Resolve(type);
            }

            for (int i = 0; i < _pointerLevel; ++i)
                type = type.MakePointerType();

            if (_isByRef)
                type = type.MakeByRefType();

            return type;
        }


        private static Type DefaultTypeResolver(Assembly assembly, string typename, Namespace ns)
        {
            if (assembly is not null)
                assembly.GetType(typename);



            //(assy, typeName) => assy == null ? (name.Equals(typeName) ? null : RT.classForName(typeName)) : assy.GetType(typeName)
            return null;
        }

        #endregion
    }
}
