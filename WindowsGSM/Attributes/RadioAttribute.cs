namespace WindowsGSM.Attributes
{
    /// <summary>
    /// https://mudblazor.com/api/radio#properties
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
    public class RadioAttribute : Attribute
    {
        public string Option { get; set; } = string.Empty;
    }
}
