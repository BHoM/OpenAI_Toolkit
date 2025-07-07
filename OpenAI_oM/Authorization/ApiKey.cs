namespace BH.oM.Adapters.OpenAI.Authorization
{
    public class ApiKey : IAuthorizationSource
    {
        public virtual string Key { get; set; } = "";
    }
}
