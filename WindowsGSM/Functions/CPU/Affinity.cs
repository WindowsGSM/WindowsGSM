using System;
using System.Linq;
using System.Text;

namespace WindowsGSM.Functions.CPU
{
    static class Affinity
    {
        public static string GetAffinityValidatedString(string bits)
        {
            bits = bits ?? string.Empty;
            bits = (bits.Length < Environment.ProcessorCount) ? string.Concat(Enumerable.Repeat("1", Environment.ProcessorCount)) : bits;
            bits = (bits.Length > Environment.ProcessorCount) ? bits.Take(Environment.ProcessorCount).ToString() : bits;

            StringBuilder sb = new StringBuilder();
            foreach (char bit in bits)
            {
                sb.Append((bit == '0') ? '0' : '1');
            }
            string returnBits = sb.ToString();

            // Cannot all '0', at least one '1'
            if (!returnBits.Contains("1"))
            {
                return string.Concat(Enumerable.Repeat("1", Environment.ProcessorCount));
            }

            return returnBits;
        }

        public static IntPtr GetAffinityIntPtr(string bits)
        {
            string validatedBits = GetAffinityValidatedString(bits);
            int affinity = 0;
            for (int i = 0; i < validatedBits.Length; i++)
            {
                if (validatedBits[validatedBits.Length - 1 - i] == '1')
                {
                    affinity += 1 << i;
                }
            }
            return (IntPtr)affinity;
        }
    }
}
