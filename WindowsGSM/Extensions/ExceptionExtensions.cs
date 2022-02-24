namespace WindowsGSM.Extensions
{
    public static class ExceptionExtensions
    {
        /// <summary>
        /// Throw Exception if Exception is not null
        /// </summary>
        /// <param name="exception"></param>
        public static void ThrowIfNotNull(this Exception? exception)
        {
            if (exception != null)
            {
                throw exception;
            }
        }
    }
}
