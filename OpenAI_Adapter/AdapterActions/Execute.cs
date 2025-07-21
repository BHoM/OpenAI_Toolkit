/*
 * This file is part of the Buildings and Habitats object Model (BHoM)
 * Copyright (c) 2015 - 2025, the respective contributors. All rights reserved.
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

using BH.oM.Adapter;
using BH.oM.Adapters.OpenAI;
using BH.oM.Adapters.OpenAI.Commands;
using BH.oM.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace BH.Adapter.OpenAI
{
    public partial class OpenAIAdapter : BHoMAdapter
    {
        /***************************************************/
        /****             Public overrides              ****/
        /***************************************************/

        public override Output<List<object>, bool> Execute(IExecuteCommand command, ActionConfig actionConfig = null)
        {
            return Execute(command as dynamic, actionConfig);
        }


        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        public async Task<Output<string, bool>> ExecuteAsync(ExecutePrompt command, ActionConfig actionConfig = null)
        {
            PromptExecutionConfig config = actionConfig as PromptExecutionConfig ?? new PromptExecutionConfig();

            try
            {
                return new Output<string, bool> { Item1 = await PromptAsync(command.System, command.User, config).ConfigureAwait(false), Item2 = true };
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"OpenAI prompt execution failed with the following exception:\n{ex.Message}");
                return new Output<string, bool> { Item1 = "", Item2 = false };
            }
        }


        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        private Output<List<object>, bool> Execute(ExecutePrompt command, ActionConfig actionConfig = null)
        {
            Output<string, bool> promptResult = ExecuteAsync(command, actionConfig).Result;
            List<object> toReturn = new List<object>();
            if (promptResult.Item2)
                toReturn.Add(promptResult.Item1);

            return new Output<List<object>, bool> { Item1 = toReturn, Item2 = promptResult.Item2 };
        }

        /***************************************************/

        private async Task<string> PromptAsync(string system, IEnumerable<string> user, PromptExecutionConfig config)
        {
            List<object> messages = new List<object>
            {
                new { role = "system", content = system },

            };

            messages.AddRange(user.Select(x => new { role = "user", content = x }));

            var requestBody = new
            {
                messages = messages,
                max_tokens = config.MaxTokens,
                temperature = config.Temperature,
                top_p = config.TopP
            };

            string json = JsonSerializer.Serialize(requestBody);
            StringContent content = new StringContent(json, Encoding.UTF8, "application/json");

            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(config.TimeoutSeconds)))
            {
                HttpResponseMessage response = await m_HttpClient.PostAsync(m_Url, content, cts.Token).ConfigureAwait(false);

                string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                dynamic result = JsonSerializer.Deserialize<dynamic>(responseString);
                response.EnsureSuccessStatusCode();

                return result
                     .GetProperty("choices")[0]
                     .GetProperty("message")
                     .GetProperty("content")
                     .GetString();
            }
        }

        /***************************************************/
    }
}
