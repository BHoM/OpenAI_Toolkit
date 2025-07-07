using BH.oM.Adapters.OpenAI.Authorization;

namespace BH.Engine.Adapters.OpenAI
{
    public static partial class Query
    {
        public static string IAuthorizationCode(this IAuthorizationSource source)
        {
            if (source == null)
            {
                BH.Engine.Base.Compute.RecordError("Could not otain authorization code because authorization source is null.");
                return "";
            }

            object code = "";
            if (!BH.Engine.Base.Compute.TryRunExtensionMethod(source, nameof(AuthorizationCode), out code))
            {
                BH.Engine.Base.Compute.RecordError($"Authorisation code extraction failed for source of type {source.GetType().Name}.");
                return "";
            }

            return code as string;
        }

        public static string AuthorizationCode(this ApiKey source)
        {
            return source.Key;
        }
    }
}
