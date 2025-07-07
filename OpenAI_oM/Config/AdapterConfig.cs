using BH.oM.Adapters.OpenAI.Authorization;
using BH.oM.Base;

namespace BH.oM.Adapters.OpenAI
{
    //[Description("Config to be used in each individual call to the Forge API endpoints.")]
    public class AdapterConfig : IObject
    {
        /***************************************************/
        /**** Properties                                ****/
        /***************************************************/

        public IAuthorizationSource Credentials { get; set; }

        /***************************************************/
    }
}
