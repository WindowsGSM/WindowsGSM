namespace WindowsGSM.Attributes
{
    /// <summary>
    /// https://mudblazor.com/api/switch#properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class SwitchAttribute : Attribute
    {
        public string Label { get; set; } = string.Empty;

        public bool Required { get; set; }

        public string RequiredError { get; set; } = string.Empty;
    }
}
