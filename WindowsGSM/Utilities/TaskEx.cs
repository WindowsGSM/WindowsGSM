namespace WindowsGSM.Utilities
{
    public static class TaskEx
    {
        public static async Task Run(Action action)
        {
            Exception? exception = await Task.Run(() =>
            {
                try
                {
                    action();
                    return null;
                }
                catch (Exception e)
                {
                    return e;
                }
            });

            if (exception != null)
            {
                throw exception;
            }
        }

        public static async Task Run(Func<Task?> function)
        {
            Exception? exception = await Task.Run(() =>
            {
                try
                {
                    function();
                    return null;
                }
                catch (Exception e)
                {
                    return e;
                }
            });

            if (exception != null)
            {
                throw exception;
            }
        }

        public static async Task<TResult?> Run<TResult>(Func<Task<TResult>?> function)
        {
            object? result = await Task.Run<object?>(() =>
            {
                try
                {
                    return function();      
                }
                catch (Exception e)
                {
                    return e;
                }
            });

            if (result is Exception exception)
            {
                throw exception;
            }

            return (TResult?)result;
        }

        public static async Task<TResult?> Run<TResult>(Func<TResult> function)
        {
            object? result = await Task.Run<object?>(() =>
            {
                try
                {
                    return function();
                }
                catch (Exception e)
                {
                    return e;
                }
            });

            if (result is Exception exception)
            {
                throw exception;
            }

            return (TResult?)result;
        }
    }
}
