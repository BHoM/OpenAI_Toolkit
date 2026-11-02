/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2026, the respective contributors. All rights reserved.
 *
 * Each contributor holds copyright over their respective contributions.
 * The project versioning (Git) records all such contribution source information.
 *                                           
 *                                                                              
 * The BHoM is free software: you can redistribute it and/or modify         
 * it under the terms of the GNU Lesser General Public License as published by  
 * the Free Software Foundation, either version 3.0 of the License, or          
 * (at your option) any later version.                                          
 *                                                                              
 * The BHoM is distributed in the hope that it will be useful,              
 * but WITHOUT ANY WARRANTY; without even the implied warranty of               
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the                 
 * GNU Lesser General Public License for more details.                          
 *                                                                            
 * You should have received a copy of the GNU Lesser General Public License     
 * along with this code. If not, see <https://www.gnu.org/licenses/lgpl-3.0.html>.      
 */

using BH.oM.Adapters.OpenAI.Authorization;
using BH.oM.Base.Attributes;
using System.ComponentModel;
using System.Net.Http;

namespace BH.Adapter.OpenAI
{
    public partial class OpenAIAdapter : BHoMAdapter
    {
        /***************************************************/
        /****               Constructors                ****/
        /***************************************************/

        [PreviousVersion("10.0", "BH.Adapter.OpenAI.OpenAIAdapter(System.String, BH.oM.Adapters.OpenAI.Authorization.IAuthorizationSource)")]
        [Description("Adapter executing prompts against a model deployment through OpenAI's Responses API.")]
        [Input("url", "URL of the Responses API endpoint, e.g. https://<resource>.openai.azure.com/openai/v1/responses.")]
        [Input("model", "Name of the model deployment to execute prompts with, e.g. gpt-6-luna.")]
        [Input("credentialsSource", "Source of the credentials used to authorise the API calls.")]
        public OpenAIAdapter(string url, string model, IAuthorizationSource credentialsSource)
        {
            if (url != null && url.Contains("/chat/completions"))
                BH.Engine.Base.Compute.RecordError("Chat Completions endpoints are not supported, please provide the URL of a Responses API endpoint instead, e.g. https://<resource>.openai.azure.com/openai/v1/responses.");

            m_Url = url;
            m_Model = model;
            m_AuthorizationSource = credentialsSource;
            m_HttpClient = new HttpClient();
        }


        /***************************************************/
        /****               Private  Fields             ****/
        /***************************************************/

        private string m_Url;
        private string m_Model;
        private HttpClient m_HttpClient;
        private IAuthorizationSource m_AuthorizationSource;
        private readonly object m_AuthLock = new object();

        /***************************************************/
    }
}

