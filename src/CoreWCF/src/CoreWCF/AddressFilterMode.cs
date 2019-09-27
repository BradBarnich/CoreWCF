namespace CoreWCF
{
    public enum AddressFilterMode
    {
        Exact, // AddressFilterMode.default
        Prefix,
        Any,
    }

    internal static class AddressFilterModeHelper
    {
        static public bool IsDefined(AddressFilterMode x)
        {
            return
                x == AddressFilterMode.Exact ||
                x == AddressFilterMode.Prefix ||
                x == AddressFilterMode.Any;
        }
    }
}