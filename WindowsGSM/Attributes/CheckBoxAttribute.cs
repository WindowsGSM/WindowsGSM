namespace WindowsGSM.Attributes
{
    /// <summary>
    /// https://mudblazor.com/components/checkbox#api
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CheckBoxAttribute : Attribute
    {
        public string Label { get; set; } = string.Empty;

        public string HelperText { get; set; } = string.Empty;

        public bool Required { get; set; }

        public string RequiredError { get; set; } = string.Empty;

        /// <summary>
        /// https://mudblazor.com/api/switch#properties
        /// </summary>
        public bool IsSwitch { get; set; }
    }
}
