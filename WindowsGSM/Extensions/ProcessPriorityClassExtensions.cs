using System.Diagnostics;

namespace WindowsGSM.Extensions
{
    public static class ProcessPriorityClassExtensions
    {
        public static string ToStringEx(this ProcessPriorityClass processPriorityClass)
        {
            return processPriorityClass switch
            {
                ProcessPriorityClass.RealTime => "Realtime",
                ProcessPriorityClass.High => "High",
                ProcessPriorityClass.AboveNormal => "Above normal",
                ProcessPriorityClass.Normal => "Normal",
                ProcessPriorityClass.BelowNormal => "Below normal",
                ProcessPriorityClass.Idle => "Low",
                _ => "Normal",
            };
        }

        public static ProcessPriorityClass FromString(string @string)
        {
            return @string switch
            {
                "Realtime" => ProcessPriorityClass.RealTime,
                "High" => ProcessPriorityClass.High,
                "Above normal" => ProcessPriorityClass.AboveNormal,
                "Normal" => ProcessPriorityClass.Normal,
                "Below normal" => ProcessPriorityClass.BelowNormal,
                "Low" => ProcessPriorityClass.Idle,
                _ => ProcessPriorityClass.Normal,
            };
        }

        public static readonly Func<ProcessPriorityClass, string> ToStringFunc = p => p.ToStringEx();
    }
}
