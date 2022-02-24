namespace WindowsGSM.Attributes
{
    /// <summary>
    /// https://mudblazor.com/api/numericfield#properties
    /// </summary>
    // C# 10 possible? NumericFieldAttribute<T>, make it in the future
    [AttributeUsage(AttributeTargets.Property)]
    public sealed class NumericFieldAttribute : Attribute
    {
        public string Label { get; set; } = string.Empty;

        public string HelperText { get; set; } = string.Empty;

        public bool Required { get; set; }

        public string RequiredError { get; set; } = string.Empty;

        public double Min { get; set; }

        public double Max { get; set; }

        public double Step { get; set; } = 1;
    }
}
