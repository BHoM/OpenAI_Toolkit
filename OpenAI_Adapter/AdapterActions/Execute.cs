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

using BH.Engine.Adapters.OpenAI;
using BH.oM.Adapter;
using BH.oM.Adapters.OpenAI;
using BH.oM.Adapters.OpenAI.Enums;
using BH.oM.Adapters.OpenAI.Output;
using BH.oM.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
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
            return RunCommand(command as dynamic, actionConfig);
        }


        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        private Output<List<object>, bool> RunCommand(IExecuteCommand command, ActionConfig actionConfig = null)
        {
            BH.Engine.Base.Compute.RecordError($"Command of type {command?.GetType().Name} is not supported by the {nameof(OpenAIAdapter)}.");
            return new Output<List<object>, bool> { Item1 = new List<object>(), Item2 = false };
        }

        /***************************************************/

        private static Output<List<object>, bool> ExecuteOutput(Output<string, bool> result)
        {
            List<object> toReturn = new List<object>();
            if (result.Item2)
                toReturn.Add(result.Item1);

            return new Output<List<object>, bool> { Item1 = toReturn, Item2 = result.Item2 };
        }

        /***************************************************/

        private static async Task<Output<string, bool>> ExecuteSafelyAsync(Func<Task<string>> execution)
        {
            try
            {
                return new Output<string, bool> { Item1 = await execution().ConfigureAwait(false), Item2 = true };
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"OpenAI command execution failed with the following exception:\n{ex.Message}");
                return new Output<string, bool> { Item1 = "", Item2 = false };
            }
        }

        /***************************************************/

        private async Task<string> PostAsync(string endpoint, Dictionary<string, object> requestBody, int timeoutSeconds)
        {
            if (string.IsNullOrWhiteSpace(m_Model))
                throw new InvalidOperationException("No model deployment name has been provided to the adapter.");

            string authCode;
            lock (m_AuthLock)
            {
                authCode = m_AuthorizationSource.IAuthorizationCode();
            }

            if (string.IsNullOrWhiteSpace(authCode))
                throw new InvalidOperationException("User authorisation failed.");

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, m_Url + endpoint))
            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeoutSeconds)))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authCode);
                request.Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json");

                try
                {
                    using (HttpResponseMessage response = await m_HttpClient.SendAsync(request, cts.Token).ConfigureAwait(false))
                    {
                        string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!response.IsSuccessStatusCode)
                            throw new HttpRequestException($"Request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}): {ErrorMessage(responseString)}");

                        return responseString;
                    }
                }
                catch (OperationCanceledException) when (cts.IsCancellationRequested)
                {
                    throw new TimeoutException($"The request timed out after {timeoutSeconds} seconds. Increase {nameof(PromptExecutionConfig.TimeoutSeconds)} in the {nameof(PromptExecutionConfig)} if the model needs more time to respond.");
                }
            }
        }

        /***************************************************/

        private static List<object> InputMessages(string system, IEnumerable<string> user, IEnumerable<string> assistant)
        {
            List<object> messages = new List<object>();
            if (!string.IsNullOrWhiteSpace(system))
                messages.Add(new { role = "system", content = system });

            // User and assistant messages alternate within a chat thread, starting and ending with a user message
            List<string> userMessages = user?.ToList() ?? new List<string>();
            List<string> assistantMessages = assistant?.ToList() ?? new List<string>();
            for (int i = 0; i < Math.Max(userMessages.Count, assistantMessages.Count); i++)
            {
                if (i < userMessages.Count)
                    messages.Add(new { role = "user", content = userMessages[i] });

                if (i < assistantMessages.Count)
                    messages.Add(new { role = "assistant", content = assistantMessages[i] });
            }

            return messages;
        }

        /***************************************************/

        private void AddSamplingParameters(Dictionary<string, object> requestBody, PromptExecutionConfig config)
        {
            if (!double.IsNaN(config.Temperature))
            {
                if (Query.AcceptsTemperature(m_Model))
                    requestBody["temperature"] = config.Temperature;
                else
                    BH.Engine.Base.Compute.RecordWarning($"{nameof(PromptExecutionConfig.Temperature)} is ignored because model {m_Model} does not support it.");
            }

            if (!double.IsNaN(config.TopP))
            {
                if (Query.AcceptsTopP(m_Model))
                    requestBody["top_p"] = config.TopP;
                else
                    BH.Engine.Base.Compute.RecordWarning($"{nameof(PromptExecutionConfig.TopP)} is ignored because model {m_Model} does not support it.");
            }
        }

        /***************************************************/

        private string SupportedReasoningEffort(PromptExecutionConfig config, IExecuteCommand command)
        {
            if (config.ReasoningEffort == ReasoningEffort.Default)
                return null;

            if (!Query.AcceptsReasoningEffort(m_Model, command, config.ReasoningEffort))
            {
                BH.Engine.Base.Compute.RecordWarning($"{nameof(PromptExecutionConfig.ReasoningEffort)} {config.ReasoningEffort} is ignored because model {m_Model} does not support it with {command?.GetType().Name}, so the model's default reasoning effort is used.");
                return null;
            }

            return config.ReasoningEffort.ToString().ToLowerInvariant();
        }

        /***************************************************/

        private static Dictionary<string, object> JsonSchemaDefinition(JsonSchema jsonSchema)
        {
            if (string.IsNullOrWhiteSpace(jsonSchema?.Schema))
                return null;

            return new Dictionary<string, object>
            {
                { "name", string.IsNullOrWhiteSpace(jsonSchema.Name) ? "structured_output" : jsonSchema.Name },
                { "schema", JsonSerializer.Deserialize<JsonElement>(jsonSchema.Schema) },
                { "strict", true }
            };
        }

        /***************************************************/

        private static string ErrorMessage(string responseString)
        {
            try
            {
                JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseString);
                if (response.ValueKind == JsonValueKind.Object
                    && response.TryGetProperty("error", out JsonElement error)
                    && error.ValueKind == JsonValueKind.Object
                    && error.TryGetProperty("message", out JsonElement message))
                    return message.GetString();
            }
            catch (JsonException)
            {
            }

            return responseString;
        }

        /***************************************************/
    }
}

