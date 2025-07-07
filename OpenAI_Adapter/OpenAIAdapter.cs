using BH.Engine.Adapters.OpenAI;
using BH.oM.Adapters.OpenAI.Authorization;
using System.Net.Http;
using System.Net.Http.Headers;

namespace BH.Adapter.OpenAI
{
    public partial class OpenAIAdapter : BHoMAdapter
    {
        /***************************************************/
        /****               Constructors                ****/
        /***************************************************/

        public OpenAIAdapter(string url, IAuthorizationSource credentialsSource)
        {
            m_Url = url;
            string authCode = credentialsSource.IAuthorizationCode();
            m_HttpClient = new HttpClient();
            m_HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authCode);
            m_HttpClient.DefaultRequestHeaders.Add("api-key", authCode);
        }


        /***************************************************/
        /****               Private  Fields             ****/
        /***************************************************/

        private string m_Url;
        private HttpClient m_HttpClient;

        /***************************************************/
    }
}
