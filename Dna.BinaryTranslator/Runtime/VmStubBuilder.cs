using Dna.LLVMInterop.API.RegionAnalysis.Native;
using Dna.LLVMInterop.API.Remill.Arch;
using Iced.Intel;
using LLVMSharp.Interop;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static Iced.Intel.AssemblerRegisters;

namespace Dna.BinaryTranslator.Runtime
{
    public class VmStubBuilder
    {
        public delegate void dgFetchComputableValue(Assembler destRegister, AssemblerMemoryOperand spillQword1, AssemblerMemoryOperand spillQword2);

        private readonly ulong initialEntryKey;

        public Assembler assembler;

        private readonly ulong ptrLiftedFunctionAddr;

        private readonly int stackHeight;

        private readonly IReadOnlyList<RemillRegister> registerArguments;

        /// <summary>
        /// When emitting code with the assembler, we need some kind of reference point for address relative accesses.
        /// For example, on vm-enter we need to push the current RIP. This acts 
        /// </summary>
        public Label initialReferencePoint;


        public ulong initialRva;

        private readonly ulong imageBase;

        private readonly Func<ulong, bool> shouldAllocateStackFrame;

        private ulong localKey;

        public VmStubBuilder(ulong initialEntryKey, Assembler assembler, ulong liftedFunctionAddress, int stackHeight, IReadOnlyList<RemillRegister> registerArguments, Label initialReferencePoint, ulong initialRva, ulong imageBase, Func<ulong, bool> shouldAllocateStackFrame)
        {
            this.initialEntryKey = initialEntryKey;
            this.assembler = assembler;
            this.ptrLiftedFunctionAddr = liftedFunctionAddress;
            this.stackHeight = stackHeight;
            this.registerArguments = registerArguments;
            this.initialReferencePoint = initialReferencePoint;
            this.initialRva = initialRva;
            this.imageBase = imageBase;
            this.shouldAllocateStackFrame = shouldAllocateStackFrame;
        }

        public List<RemillRegister> GetStackArguments()
        {
            return registerArguments.Skip(4).ToList();
        }

        public void EncodeVmEnter(ulong vmKey)
        {
            // If the stack frame has not been allocated, we allocate it ourselves. 
            // This is usually only true at the first vm entry.
            if (MustAllocFrame(vmKey))
                assembler.sub(rsp, stackHeight);

            // Allocate a local variable to keep track of the stack growth.
            int increase = 0;

            // Spill rcx to the stack.
            assembler.mov(__[rsp - 24], rcx);

            // Push the initial set of vm specific entries.
            // Push the image base. 
            assembler.lea(rcx, __[initialReferencePoint]);
            assembler.sub(rcx, (int)initialRva);
            assembler.push(rcx);
            increase += 1;
            // Push the dispatcher key.
            assembler.mov(rcx, vmKey);
            assembler.push(rcx);
            increase += 1;

            // Restore rcx using the spilled value from the stack.
            assembler.mov(rcx, __[rsp - 8]);

            // Push each register argument on the stack.
            // Note that we skip the first four arguments, as the fastcall convention
            // requires that these are passed in registers.
            // Also stack arguments are passed from right to left using the fastcall convention.
            var stackArguments = GetStackArguments();
            stackArguments.Reverse();
            foreach (var inputReg in stackArguments)
            {
                if (inputReg.Name == "RIP")
                {
                    assembler.mov(rsi, vmKey);
                    assembler.mov(r10, imageBase);
                    // Convert the vm key into an address that's relative to the image base.
                    assembler.sub(rsi, r10);
                    // Get the image base.
                    var offset = initialRva;
                    assembler.lea(r10, __[initialReferencePoint]);
                    assembler.sub(r10, (int)initialRva);
                    // Add the vm key to the address base. This gives us our RIP.
                    assembler.add(r10, rsi);
                    assembler.push(r10);
                }

                else if (inputReg.Name == "RSP")
                {
                    throw new InvalidOperationException("RSP must be passed into rcx.");
                }

                else
                {
                    var regValue = (AssemblerRegister64)typeof(AssemblerRegisters).GetFields().Single(x => x.Name.ToUpper() == inputReg.Name).GetValue(null);
                    assembler.push(regValue);
                }

                increase += 1;
            }

            // At this point all stack necessary registers are on the stack.
            // This leaves us with rsp, r11, r12, and r13 which need to go into rcx/rdx/r8/r9
            assembler.mov(rcx, rsp);
            if (MustAllocFrame(vmKey))
                assembler.add(rcx, (int)stackHeight);

            assembler.add(rcx, increase * 8);

            assembler.mov(rdx, r11);
            assembler.mov(r8, r12);
            assembler.mov(r9, r13);

            // Here the arg allocation is okay i guess because we do need to allocate this crap for spills.
            assembler.sub(rsp, 32);

            /*
            // Spill r10 to the stack, calculate the image base, and then push a return address of [imageBase + ptrLiftedFunctionAddress].
            // Finally we restore r10 and RET.
            assembler.mov(__[rsp - 16], r10);
            // Push the RVA
            assembler.lea(r10, __[initialReferencePoint]);
            assembler.sub(r10, (int)initialRva);
            // Compute the pointer address
            assembler.add(r10, (int)(ptrLiftedFunctionAddr));
            // Dereference the pointer
            assembler.push(__qword_ptr[r10]);
            // Compute the image base AGAIN.
            assembler.lea(r10, __[initialReferencePoint]);
            assembler.sub(r10, (int)initialRva);
            assembler.add(__qword_ptr[rsp], r10);

            assembler.mov(r10, __[rsp - 8]);
            assembler.call(__qword_ptr[rsp]);
            */
            //assembler.ret();

            assembler.call(0x014038E000);

            //assembler.call(liftedFunctionAddress);
            // Int3 after this call finishes. This should never execute since the RET handler executes a ROP.
            assembler.int3();
        }

        public void EncodeVmCall(ulong vmKey)
        {
            // Restore all registers.
            var vmEnterLabel = assembler.CreateLabel();
            EncodeRestoreRegisters();

            // Over-write the return address with our re-entry stub.
            assembler.mov(__[rsp - 16], rcx); // Spill rcx to stack
            //assembler.mov(rcx, __[reenterLabel]); // Move address of our vm-reentry into rcx.
            assembler.lea(rcx, __[vmEnterLabel]);
            assembler.mov(__[rsp], rcx); // Overwrite return address with our vm-reentry.
            assembler.mov(rcx, __[rsp - 16]); // Fix up rcx value to point to the actual spilled value.

            // Place rsp to point to our fake ret address.
            assembler.sub(rsp, 8);
            // Return / ROP to our call target.
            assembler.ret();

            // Fill in the vm enter body.
            assembler.Label(ref vmEnterLabel);
            EncodeVmEnter(vmKey);
        }

        public void EncodeVmRet()
        {
            EncodeRestoreRegisters();

            // Now when the vmexit stub is invoked here,
            // the RSP has already been added/subtracted by the emualted RET.
            // So here as a quick hack we need to just sub rsp, 8 to undo the changes to the stack ptr
            // before our RET.
            assembler.sub(rsp, 8);

            // ROP to the actual return target.
            assembler.ret();
        }

        private void EncodeRestoreRegisters()
        {
            // On x64 all functions have a 32 byte scratch space.
            // So here we spill rcx(old stack pointer) to the stack in case we need it
            assembler.mov(__[rsp + 8], rcx);
            assembler.mov(__[rsp + 16], rdx);
            assembler.mov(__[rsp + 24], r8);
            assembler.mov(__[rsp + 32], r9);

            ulong i = 0;
            AssemblerMemoryOperand? ripPtr = null;
            foreach (var inputReg in registerArguments.Skip(4).ToList())
            {
                if (inputReg.Name == "RIPP")
                {
                    i += 1;
                    continue;
                }
                else if (inputReg.Name == "RSP")
                {
                    throw new InvalidOperationException();
                }
                else
                {
                    var index = 40 + (i * 8);
                    var stackPtr = __[rsp + (int)index];

                    if (inputReg.Name == "RIP")
                    {
                        i += 1;
                        ripPtr = stackPtr;
                        continue;
                    }

                    i += 1;
                    continue;
                }
            }

            // Move the fake stack pointer into r8.
            assembler.mov(r8, rcx);
            // Decrement the copy of the stack pointer by 8, effectively emulating the first part of the PUSH.
            assembler.sub(r8, 8);
            // Move the expected call address into our other scratch reg, r9.
            assembler.mov(r9, ripPtr.Value);
            // Write the call address right below the return address.
            assembler.mov(__[r8], r9);
            // Restore all of these registers.
            assembler.mov(rcx, __[rsp + 8]);
            assembler.mov(rdx, __[rsp + 16]);
            assembler.mov(r8, __[rsp + 24]);
            assembler.mov(r9, __[rsp + 32]);


            i = 0;
            ripPtr = null;
            foreach (var inputReg in registerArguments.Skip(4).ToList())
            {
                // Problem: multiple call stubs are necessary,
                // but we need to specify on a per-call basis.
                // For now since there's only 1 call we can just hardcode it. todo: fix.
                // TODO: Push the actual RIP.
                if (inputReg.Name == "RIPP")
                {
                    /*
                    assembler.lea(rsi, __[referenceLabel]);
                    assembler.sub(rsi, (int)startIp);
                    assembler.push(rsi);
                    //assembler.push(0xF00);
                    */
                    //assembler.add(rsp, 8);
                    i += 1;
                    continue;
                }
                else if (inputReg.Name == "RSP")
                {
                    throw new InvalidOperationException();
                }
                else
                {
                    var index = 40 + (i * 8);
                    var stackPtr = __[rsp + (int)index];

                    if (inputReg.Name == "RIP")
                    {
                        i += 1;
                        ripPtr = stackPtr;
                        continue;
                    }

                    var regValue = (AssemblerRegister64)typeof(AssemblerRegisters).GetFields().Single(x => x.Name.ToUpper() == inputReg.Name).GetValue(null);
                    assembler.mov(regValue, stackPtr);

                    i += 1;
                    continue;
                }
            }

            assembler.mov(r11, __[rsp + 16]);
            assembler.mov(r12, __[rsp + 24]);
            assembler.mov(r13, __[rsp + 32]);
            assembler.mov(rsp, __[rsp + 8]);
        }

        /// <summary>
        /// Loads the runtime address of our assembled reference label. This effectively gives us the ability to reference RIP relative variables.
        /// </summary>
        /// <param name="destRegister"></param>
        private void LeaReferencePoint(AssemblerRegister64 destRegister) => assembler.lea(destRegister, __[initialReferencePoint]);

        /// <summary>
        /// Given a vm key, determine whether we must allocate the stack frame ourselves.
        /// </summary>
        /// <param name="vmKey"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        private bool MustAllocFrame(ulong vmKey) => shouldAllocateStackFrame(vmKey);
    }
}
