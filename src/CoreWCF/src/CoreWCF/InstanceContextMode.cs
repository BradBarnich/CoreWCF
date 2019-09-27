namespace CoreWCF
{
    public enum InstanceContextMode
    {
        PerSession,
        PerCall,
        Single,
    }

    internal static class InstanceContextModeHelper
    {
        static public bool IsDefined(InstanceContextMode x)
        {
            return
                x == InstanceContextMode.PerCall ||
                x == InstanceContextMode.PerSession ||
                x == InstanceContextMode.Single;
        }
    }
}