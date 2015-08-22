using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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

    class ClrArraySpec
    {

        #region Data

        int _dimensions;
        bool _isBound;

        #endregion

        #region C-tors

        internal ClrArraySpec(int dimensions, bool bound)
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

    class ClrTypeSpec
    {
        #region Data

        string _name;
        string _assemblyName;
        List<string> _nested;
        List<ClrTypeSpec> _genericParams;
        List<ClrArraySpec> _arraySpec;
        int _pointerLevel;
        bool _isByRef;

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
                (assy, typeName) => assy == null ? (name.Equals(typeName) ? null : RT.classForName(typeName)) : assy.GetType(typeName));
        }

        #endregion

        #region Parsing

        static ClrTypeSpec Parse(string name)
        {
            int pos = 0;
            ClrTypeSpec spec = Parse(name, ref pos, false, false);
            if (spec == null)
                return null;                                           // bad parse
            if (pos < name.Length)
                return null;                                           // ArgumentException ("Count not parse the whole type name", "typeName");
            return spec;
        }

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
        static ClrTypeSpec Parse(string name, ref int p, bool isRecursive, bool allowAssyQualName)
        {
            int pos = p;
            int name_start;
            bool hasModifiers = false;
            ClrTypeSpec spec = new ClrTypeSpec();

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
                        if (name[pos] != '[' && isRecursive)
                            return null;                                              // ArgumentException ("Generic argument can't be byref or pointer type", "typeName");
                        spec.AddName(name.Substring(name_start, pos - name_start));
                        name_start = pos + 1;
                        hasModifiers = true;
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
                                List<ClrTypeSpec> args = new List<ClrTypeSpec>();
                                if (spec.IsArray)
                                    return null;                                          // ArgumentException ("generic args after array spec", "typeName");

                                while (pos < name.Length)
                                {
                                    SkipSpace(name, ref pos);
                                    bool aqn = name[pos] == '[';
                                    if (aqn)
                                        ++pos; //skip '[' to the start of the type
                                    {
                                        ClrTypeSpec arg = Parse(name, ref pos, true, aqn);
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
                                spec.AddArray(new ClrArraySpec(dimensions, bound));
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

        void AddArray(ClrArraySpec array)
        {
            if (_arraySpec == null)
                _arraySpec = new List<ClrArraySpec>();
            _arraySpec.Add(array);
        }

        #endregion

        #region  Resolving

        internal Type Resolve(Func<AssemblyName, Assembly> assemblyResolver, Func<Assembly, string, Type> typeResolver)
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

            Type type = typeResolver(asm, _name);
            if (type == null)
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
                    var tmp = _genericParams[i].Resolve(assemblyResolver, typeResolver);
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

        #endregion
    }
}
