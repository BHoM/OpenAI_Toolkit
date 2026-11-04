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
        [Description("Adapter executing commands against a model deployment through OpenAI's API, e.g. generating responses or chat completions.")]
        [Input("url", "Base URL of the OpenAI v1 API, to which each command appends its own endpoint, e.g. https://<resource>.openai.azure.com/openai/v1/ for Azure or https://api.openai.com/v1/ for OpenAI.")]
        [Input("model", "Name of the model deployment to execute commands with, e.g. gpt-6-luna.")]
        [Input("credentialsSource", "Source of the credentials used to authorise the API calls.")]
        public OpenAIAdapter(string url, string model, IAuthorizationSource credentialsSource)
        {
            if (url != null && (url.Contains("?") || url.TrimEnd('/').EndsWith("/responses") || url.TrimEnd('/').EndsWith("/chat/completions")))
                BH.Engine.Base.Compute.RecordError("The URL should be the base URL of the API rather than a specific endpoint, e.g. https://<resource>.openai.azure.com/openai/v1/.");

            m_Url = url == null ? null : url.TrimEnd('/') + "/";
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

