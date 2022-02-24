namespace WindowsGSM.Games
{
    public enum Status
    {
        Stopped, Started, Stopping, Starting, Restarting, Killing, Creating, Updating, Deleting
    }

    public static class StatusExtensions
    {
        public static string ToDisplayString(this Status status)
        {
            return status switch
            {
                Status.Stopped => "Stopped",
                Status.Started => "Started",
                Status.Stopping => "Stopping",
                Status.Starting => "Starting",
                Status.Restarting => "Restarting",
                Status.Killing => "Killing",
                Status.Creating => "Creating",
                Status.Updating => "Updating",
                Status.Deleting => "Deleting",
                _ => "Unknown",
            };
        }

        public static bool IsRunning(this Status status)
        {
            return status == Status.Stopping
                || status == Status.Starting
                || status == Status.Restarting
                || status == Status.Killing
                || status == Status.Creating
                || status == Status.Updating
                || status == Status.Deleting;
        }
    }
}
