using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace WindowsPseudoConsole
{
    /// <summary>
    /// Interop Exception
    /// </summary>
    [Serializable]
    public class InteropException : Exception
    {
        /// <summary>
        /// Interop Exception
        /// </summary>
        public InteropException() { }

        /// <summary>
        /// Interop Exception
        /// </summary>
        /// <param name="message"></param>
        public InteropException(string message)
            : base(message) { }

        /// <summary>
        /// Interop Exception
        /// </summary>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public InteropException(string message, Exception innerException)
            : base(message, innerException) { }

        /// <summary>
        /// Interop Exception
        /// </summary>
        /// <param name="serializationInfo"></param>
        /// <param name="streamingContext"></param>
        protected InteropException(SerializationInfo serializationInfo, StreamingContext streamingContext)
              : base(serializationInfo, streamingContext) { }

        /// <summary>
        /// Create exception with inner HResult exception
        /// </summary>
        /// <param name="message"></param>
        /// <returns></returns>
        public static InteropException CreateWithInnerHResultException(string message)
        {
            return new InteropException(
                message,
                Marshal.GetExceptionForHR(Marshal.GetHRForLastWin32Error())!);
        }
    }
}
