﻿using ELFSharp.MachO;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Dna.LLVMInterop.API.RegionAnalysis.Native
{
    /// <summary>
    /// The kind of a region.
    /// </summary>
    public enum RegionKind : uint
    {
        /// <summary>
        /// Invalid region.
        /// </summary>
        Invalid,
        /// <summary>
        /// The smallest unit(one basic block).
        /// </summary>
        Block,
        /// <summary>
        /// A sequence of regions.
        /// </summary>
        Sequence,
        /// <summary>
        /// An if-then region.
        /// </summary>
        IfThen,
        /// <summary>
        /// An if-then-else region.
        /// </summary>
        IfThenElse,
        /// <summary>
        /// A natural loop region.
        /// </summary>
        NaturalLoop,
        /// <summary>
        /// A region ending with a break jump.
        /// </summary>
        Break,
        /// <summary>
        /// A region ending with a continue jump.
        /// </summary>
        Continue,
        /// <summary>
        /// A region ending with a return.
        /// </summary>
        Return,
        /// <summary>
        /// A switch region.
        /// </summary>
        Switch
    };

    /// <summary>
    /// Flags for SCC.
    /// </summary>
    public enum RegionFlags : uint
    {
        OnStack = 1 << 0,
        OnLoop = 1 << 1,
        IsHeader = 1 << 2,
    }

    public class NativeRegionApi
    {
        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "RegionGetKind")]
        public unsafe static extern RegionKind RegionGetKind(nint region);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "RegionGetOwnerRegion")]
        public unsafe static extern nint RegionGetOwnerRegion(nint region);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "RegionGetEntryRegionBlock")]
        public unsafe static extern nint RegionGetEntryRegionBlock(nint region);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "RegionGetId")]
        public unsafe static extern ulong RegionGetId(nint region);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "RegionGetPredCount")]
        public unsafe static extern ulong RegionGetPredCount(nint region);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "RegionGetPred")]
        public unsafe static extern nint RegionGetPred(nint region, ulong index);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "RegionGetSuccCount")]
        public unsafe static extern ulong RegionGetSuccCount(nint region);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "RegionGetSucc")]
        public unsafe static extern nint RegionGetSucc(nint region, ulong index);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "RegionGetLLVMBasicBlock")]
        public unsafe static extern nint RegionGetLLVMBasicBlock(nint region);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "RegionGetHeadLLVMBasicBlock")]
        public unsafe static extern nint RegionGetHeadLLVMBasicBlock(nint region);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "RegionGetChildCount")]
        public unsafe static extern ulong RegionGetChildCount(nint region);

        [DllImport("Dna.LLVMInterop", CallingConvention = CallingConvention.Cdecl, EntryPoint = "RegionGetChild")]
        public unsafe static extern nint RegionGetChild(nint region, ulong id);
    }
}
