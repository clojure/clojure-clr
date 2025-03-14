/**
 *   Copyright (c) Rich Hickey. All rights reserved.
 *   The use and distribution terms for this software are covered by the
 *   Eclipse Public License 1.0 (http://opensource.org/licenses/eclipse-1.0.php)
 *   which can be found in the file epl-v10.html at the root of this distribution.
 *   By using this software in any fashion, you are agreeing to be bound by
 * 	 the terms of this license.
 *   You must not remove this notice, or any other, from this software.
 **/

/**
 *   Author: David Miller
 **/


using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Runtime.CompilerServices;
using Microsoft.Scripting.Generation;
using System.Collections.Generic;


namespace clojure.lang.CljCompiler.Ast
{
    public class CljILGen : ILGen
    {
        public CljILGen(ILGenerator baseIlg)
            : base(baseIlg)
        {
        }

        private static bool IsVolatile(FieldInfo fi)
        {
            // Cannot work with signatures on uncreated types
            {
                Type t = fi.DeclaringType;
                TypeBuilder tb = t as TypeBuilder;
                if (tb != null && !tb.IsCreated())
                    return false;
            }

            if (RT.IsRunningOnMono)
            {
                foreach (Type t in fi.GetRequiredCustomModifiers())
                    if (t == typeof(IsVolatile))
                        return true;
                return false;
            }
            else
                return FieldSigReader.IsVolatile(fi);
        }

        public void MaybeEmitVolatileOp(FieldInfo fi)
        {
            if (IsVolatile(fi))
                this.Emit(OpCodes.Volatile);
        }

        public void MaybeEmitVolatileOp(bool emit)
        {
            if (emit)
                this.Emit(OpCodes.Volatile);
        }


        public static class FieldSigReader
        {
            enum CustomMod
            {
                Required = 0x1f,   // Required modifier : followed by a TypeDef or TypeRef token
                Optional = 0x20,   // Optional modifier : followed by a TypeDef or TypeRef token
            }

            enum TypeToken : uint
            {
                TypeRef = 0x01000000,
                TypeDef = 0x02000000,
                TypeSpec = 0x1b000000,
                Unknown = 0
            }

            public static bool IsVolatile(FieldInfo fi)
            {
                return GetCustomModTypes(fi).Contains(typeof(IsVolatile));
            }

            public static IList<Type> GetCustomModTypes(FieldInfo fi)
            {
                IList<int> modMetaTokens = GetCustomModMetadataTokens(fi);
                List<Type> types = new List<Type>(modMetaTokens.Count);


                Module module = fi.Module;

                foreach (int mt in modMetaTokens)
                {
                    try
                    {
                        types.Add(module.ResolveType(mt));
                    }
                    catch (ArgumentException)
                    {
                    }
                }
                return types;
            }

            public static IList<int> GetCustomModMetadataTokens(FieldInfo fi)
            {
                byte[] sig = fi.Module.ResolveSignature(fi.MetadataToken);
                return ReadCustomMods(sig);
            }

            static IList<int> ReadCustomMods(byte[] sig)
            {
                return ReadCustomMods(sig, 1, out _);
            }

            static IList<int> ReadCustomMods(byte[] sig, int pos, out int nextPos)
            {
                List<int> mods = new List<int>();
                int start = pos;
                while (true)
                {
                    nextPos = start;
                    CustomMod flag = (CustomMod)ReadCompressedInteger(sig, start, out start);
                    if (flag != CustomMod.Required && flag != CustomMod.Optional)
                        break;
                    mods.Add(ReadCustomModToken(sig, start, out start));
                }
                return mods;
            }

            static int ReadCustomModToken(byte[] sig, int pos, out int nextPos)
            {
                uint raw = (uint)ReadCompressedInteger(sig, pos, out nextPos);
                return MakeMetadataToken(raw);
            }

            public static int MakeMetadataToken(uint raw)
            {

                uint rid = raw >> 2;
                switch (raw & 3)
                {
                    case 0:
                        return (int)((uint)TypeToken.TypeDef | rid);
                    case 1:
                        return (int)((uint)TypeToken.TypeRef | rid);
                    case 2:
                        return (int)((uint)TypeToken.TypeSpec | rid);
                    default:
                        return 0;
                }
            }

            static int ReadCompressedInteger(byte[] data, int pos, out int nextPos)
            {
                nextPos = pos;

                int val;
                if ((data[pos] & 0x80) == 0)
                {
                    val = data[pos];
                    nextPos++;
                }
                else if ((data[pos] & 0x40) == 0)
                {
                    val = (data[pos] & ~0x80) << 8;
                    val |= data[pos + 1];
                    nextPos += 2;
                }
                else
                {
                    val = (data[pos] & ~0xc0) << 24;
                    val |= data[pos + 1] << 16;
                    val |= data[pos + 2] << 8;
                    val |= data[pos + 3];
                    nextPos += 4;
                }
                return val;
            }

        }

    }
}
