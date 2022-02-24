namespace WindowsGSM.Attributes
{
    /// <summary>
    /// https://mudblazor.com/components/checkbox#api
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    public class CheckBoxAttribute : Attribute
    {
        public string Text { get; set; } = string.Empty;

        public bool Required { get; set; }

        public string RequiredError { get; set; } = string.Empty;
    }
}
