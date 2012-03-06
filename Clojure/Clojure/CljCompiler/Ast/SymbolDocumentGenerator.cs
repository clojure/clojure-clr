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
using System.Collections.Generic;
using System.Diagnostics.SymbolStore;
using System.Linq.Expressions;
using System.Reflection.Emit;
using System.Reflection;
using clojure.lang.Runtime;
using Microsoft.Scripting.Generation;

namespace clojure.lang.CljCompiler.Ast
{
    // Idea copied from the eponymous class in the DLR
    sealed class SymbolDocumentGenerator
    {
        private Dictionary<SymbolDocumentInfo, ISymbolDocumentWriter> _symbolWriters;

        private ISymbolDocumentWriter GetSymbolWriter(ModuleBuilder module, SymbolDocumentInfo document)
        {
            ISymbolDocumentWriter result;

            if (_symbolWriters == null)
            {
                _symbolWriters = new Dictionary<SymbolDocumentInfo, ISymbolDocumentWriter>();
            }

            if (!_symbolWriters.TryGetValue(document, out result))
            {
                result = module.DefineDocument(document.FileName, ClojureContext.Default.LanguageGuid, ClojureContext.Default.VendorGuid, Guid.Empty);
                _symbolWriters.Add(document, result);
            }

            return result;
        }


        public void MarkSequencePoint(ModuleBuilder mb, SymbolDocumentInfo doc, ILGenerator ilg, int startLine, int startColumn, int endLine, int endColumn)
        {
            ilg.MarkSequencePoint(GetSymbolWriter(mb, doc), startLine, startColumn, endLine, endColumn);
        }

        public void MarkSequencePoint(ModuleBuilder mb, SymbolDocumentInfo doc, ILGen ilg, int startLine, int startColumn, int endLine, int endColumn)
        {
            ilg.MarkSequencePoint(GetSymbolWriter(mb, doc), startLine, startColumn, endLine, endColumn);
        }
    }


}
