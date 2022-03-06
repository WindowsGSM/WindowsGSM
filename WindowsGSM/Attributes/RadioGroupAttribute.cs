namespace WindowsGSM.Attributes
{
    /// <summary>
    /// https://mudblazor.com/api/radiogroup#pages
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RadioGroupAttribute : Attribute
    {
        public string Text { get; set; } = string.Empty;

        public string HelperText { get; set; } = string.Empty;
    }
}
