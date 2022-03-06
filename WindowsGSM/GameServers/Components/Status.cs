namespace WindowsGSM.GameServers.Components
{
    public enum Status
    {
        Stopped, Started, Starting, Stopping, Restarting, Killing, Creating, Updating, Deleting, Backuping, Restoring, InstallingMod, UpdatingMod, DeletingMod
    }

    public enum Operation
    {
        Start, Stop, Restart, Kill, Create, Update, Delete, Backup, Restore, InstallMod, UpdateMod, DeleteMod
    }

    public static class StatusExtensions
    {
        public static bool IsRunning(this Status status)
        {
            return status != Status.Stopped && status != Status.Started;
        }

        public static bool IsDisabled(this Status status, Operation operation)
        {
            return operation switch
            {
                Operation.Start => status != Status.Stopped,
                Operation.Stop => status != Status.Started,
                Operation.Restart => status != Status.Started,
                Operation.Kill => status != Status.Started,
                Operation.Create => status != Status.Stopped,
                Operation.Update => status != Status.Stopped,
                Operation.Delete => status != Status.Stopped,
                Operation.Backup => status != Status.Stopped,
                Operation.Restore => status != Status.Stopped,
                Operation.InstallMod => status != Status.Stopped,
                _ => false,
            };
        }
    }
}
