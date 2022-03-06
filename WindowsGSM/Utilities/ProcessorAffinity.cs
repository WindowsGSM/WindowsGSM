namespace WindowsGSM.Utilities
{
    public static class ProcessorAffinity
    {
        public static readonly uint Default = (uint)((1 << Environment.ProcessorCount) - 1);

        public static bool[] GetProcessors(uint affinity)
        {
            if (affinity == 0)
            {
                affinity = Default;
            }

            bool[] _processors = new bool[Environment.ProcessorCount];

            for (int i = 0; i < _processors.Length; i++)
            {
                uint mask = (uint)(1 << i);

                _processors[i] = (mask & affinity) == mask;
            }

            return _processors;
        }

        public static uint GetProcessorAffinity(bool[] _processors)
        {
            uint affinity = 0;

            for (int i = 0; i < _processors.Length; i++)
            {
                if (_processors[i])
                {
                    affinity += (uint)(1 << i);
                }
            }

            return affinity;
        }
    }
}
