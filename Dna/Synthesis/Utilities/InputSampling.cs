using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dna.Synthesis.Utilities
{
    public static class InputSampling
    {
        private static readonly Random rand = new Random();

        private static readonly ulong[] specialValues = new ulong[] 
        {
            0x0,
            0x1,
            0x2,
            0x80,
            0xff,
            0x8000,
            0xffff,
            0x8000_0000,
            0xffff_ffff,
            0x8000_0000_0000_0000,
            0xffff_ffff_ffff_ffff,
        };

        /// <summary>
        /// Gets the oracle inputs.
        /// </summary>
        /// <param name="variableCount">The number of variables in the synthesis function.</param>
        /// <param name="sampleCount">The number of independent oracle queries.</param>
        public static List<List<long>> GenInputs(int variableCount, int sampleCount)
        {
            var inputs = new List<List<long>>();   
            for(int i = 0; i < sampleCount; i++)
            {
                inputs.Add(GenInputsArray(variableCount));
            }

            return inputs;
        }

        /// <summary>
        /// Gets an array of random values.
        /// </summary>
        public static List<long> GenInputsArray(int count)
        {
            List<long> inputs = new List<long>();
            for(int i = 0; i < count; i++)
            {
                inputs.Add(GetRandInput());
            }

            return inputs;
        }

        /// <summary>
        /// Gets a random values. It equally chooses between
        /// u8, u16, u32, u64, and special values for better
        /// synthesis coverage.
        /// </summary>
        public static long GetRandInput()
        {
            // Get a random between value 0 and 4.
            var coin = rand.NextInt64(byte.MinValue, byte.MaxValue) % 5;

            if (coin == 0)
                return GetRandBits(8);
            else if (coin == 1)
                return GetRandBits(16);
            else if (coin == 2)
                return GetRandBits(32);
            else if (coin == 3)
                return GetRandBits(64);
            else if (coin == 4)
                return (long)Choice(specialValues);
            else
                throw new InvalidOperationException();
        }

        public static long GetRandBits(int size)
        {
            if (size == 8)
                return rand.NextInt64(byte.MinValue, byte.MaxValue);
            else if (size == 16)
                return rand.NextInt64(ushort.MinValue, ushort.MaxValue);
            else if (size == 32)
                return rand.NextInt64(uint.MinValue, uint.MaxValue);
            else if (size == 64)
                return rand.NextInt64(long.MinValue, long.MaxValue);
            else
                throw new NotImplementedException($"Cannot get random integer of size {size}");
        }

        /// <summary>
        /// Gets a random item from a list.
        /// </summary>
        public static T Choice<T>(T[] input)
        {
            return input[rand.Next(0, input.Length - 1)];
        }

        /// <summary>
        /// Gets a random item from a list.
        /// </summary>
        public static T Choice<T>(IEnumerable<T> input)
        {
            // Get the length of the collection.
            var length = input.Count();

            // Select a random value from the collection.
            return input.ElementAt(rand.Next(0, length - 1));
        }
    }
}
