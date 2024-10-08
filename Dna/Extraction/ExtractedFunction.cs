﻿using Dna.ControlFlow;
using Iced.Intel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Extraction
{
    /// <inheritdoc cref="IExtractedFunction" />
    public class ExtractedFunction
    {
        /// <inheritdoc />
        public ControlFlowGraph<Instruction> Graph { get; set; }

        /// <inheritdoc />
        public IEnumerable<ExtractedFunction> Callees { get; set; }
    }
}
