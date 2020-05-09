using System;
using System.Linq;

namespace WindowsGSM.Functions.CPU
{
    class Affinity
    {
        public static string GetAffinityValidatedString(string bits)
        {
            bits = bits ?? string.Empty;
            bits = (bits.Length < Environment.ProcessorCount) ? string.Concat(Enumerable.Repeat("1", Environment.ProcessorCount)).ToString() : bits;
            bits = (bits.Length > Environment.ProcessorCount) ? bits.Take(Environment.ProcessorCount).ToString() : bits;

            string returnBits = string.Empty;
            foreach (char bit in bits)
            {
                returnBits += (bit == '0') ? '0' : '1';
            }

            // Cannot all '0', at least one '1'
            if (!returnBits.Contains("1"))
            {
                return string.Concat(Enumerable.Repeat("1", Environment.ProcessorCount)).ToString();
            }

            return returnBits;
        }

        public static IntPtr GetAffinityIntPtr(string bits)
        {
            return (IntPtr)Convert.ToInt32(GetAffinityValidatedString(bits), 2);
        }
    }
}
