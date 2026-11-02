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
using BH.oM.Adapters.OpenAI.Commands;
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
                return new Output<string, bool> { Item1 = await PromptAsync(command.System, command.User, command.Assistant, config).ConfigureAwait(false), Item2 = true };
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

        private async Task<string> PromptAsync(string system, IEnumerable<string> user, IEnumerable<string> assistant, PromptExecutionConfig config)
        {
            if (string.IsNullOrWhiteSpace(m_Model))
                throw new InvalidOperationException("No model deployment name has been provided to the adapter.");

            string authCode;
            lock (m_AuthLock)
            {
                authCode = m_AuthorizationSource.IAuthorizationCode();
            }

            if (string.IsNullOrWhiteSpace(authCode))
            {
                BH.Engine.Base.Compute.RecordError("Cannot execute the prompt because user authorisation failed.");
                return "";
            }

            string json = JsonSerializer.Serialize(RequestBody(system, user, assistant, config));

            using (HttpRequestMessage request = new HttpRequestMessage(HttpMethod.Post, m_Url))
            using (CancellationTokenSource cts = new CancellationTokenSource(TimeSpan.FromSeconds(config.TimeoutSeconds)))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authCode);
                request.Content = new StringContent(json, Encoding.UTF8, "application/json");

                using (HttpResponseMessage response = await m_HttpClient.SendAsync(request, cts.Token).ConfigureAwait(false))
                {
                    string responseString = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
                    if (!response.IsSuccessStatusCode)
                        throw new HttpRequestException($"Request failed with status code {(int)response.StatusCode} ({response.ReasonPhrase}): {ErrorMessage(responseString)}");

                    return ExtractResponseText(responseString);
                }
            }
        }

        /***************************************************/

        private Dictionary<string, object> RequestBody(string system, IEnumerable<string> user, IEnumerable<string> assistant, PromptExecutionConfig config)
        {
            Dictionary<string, object> requestBody = new Dictionary<string, object>
            {
                { "model", m_Model },
                { "input", InputMessages(system, user, assistant) },
                { "max_output_tokens", config.MaxTokens },
                { "store", false }
            };

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

            string effort = ReasoningEffortValue(config.ReasoningEffort);
            if (effort != null)
            {
                if (Query.AcceptsReasoningEffort(m_Model, config.ReasoningEffort))
                    requestBody["reasoning"] = new { effort = effort };
                else
                    BH.Engine.Base.Compute.RecordWarning($"{nameof(PromptExecutionConfig.ReasoningEffort)} {config.ReasoningEffort} is ignored because model {m_Model} does not support it, so the model's default reasoning effort is used.");
            }

            object textFormat = BuildTextFormat(config.OutputType);
            if (textFormat != null)
                requestBody["text"] = new { format = textFormat };

            return requestBody;
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

        private static string ExtractResponseText(string responseString)
        {
            JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseString);

            string status = response.TryGetProperty("status", out JsonElement statusElement) ? statusElement.GetString() : null;
            if (status == "failed")
                throw new InvalidOperationException($"The response failed: {ErrorMessage(responseString)}");

            if (status == "incomplete")
            {
                string reason = "unknown";
                if (response.TryGetProperty("incomplete_details", out JsonElement details) && details.ValueKind == JsonValueKind.Object && details.TryGetProperty("reason", out JsonElement reasonElement))
                    reason = reasonElement.GetString();

                throw new InvalidOperationException($"The response is incomplete (reason: {reason}). If the reason is max_output_tokens, increase {nameof(PromptExecutionConfig.MaxTokens)} or lower {nameof(PromptExecutionConfig.ReasoningEffort)} in the {nameof(PromptExecutionConfig)}.");
            }

            StringBuilder text = new StringBuilder();
            foreach (JsonElement item in response.GetProperty("output").EnumerateArray())
            {
                // Reasoning and tool call items carry no text output
                if (!item.TryGetProperty("type", out JsonElement itemType) || itemType.GetString() != "message" || !item.TryGetProperty("content", out JsonElement content))
                    continue;

                foreach (JsonElement part in content.EnumerateArray())
                {
                    string partType = part.TryGetProperty("type", out JsonElement partTypeElement) ? partTypeElement.GetString() : null;
                    if (partType == "output_text")
                        text.Append(part.GetProperty("text").GetString());
                    else if (partType == "refusal")
                        throw new InvalidOperationException($"The model refused to respond: {part.GetProperty("refusal").GetString()}");
                }
            }

            return text.ToString();
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

        private static string ReasoningEffortValue(ReasoningEffort effort)
        {
            if (effort == ReasoningEffort.Default)
                return null;

            return effort.ToString().ToLowerInvariant();
        }

        /***************************************************/

        private static object BuildTextFormat(IOutputType outputType)
        {
            if (outputType == null || outputType is Text)
                return null;

            if (outputType is JsonObject)
                return new { type = "json_object" };

            if (outputType is JsonSchema jsonSchema)
            {
                if (string.IsNullOrWhiteSpace(jsonSchema.Schema))
                    return null;

                string schemaName = string.IsNullOrWhiteSpace(jsonSchema.Name) ? "structured_output" : jsonSchema.Name;

                return new
                {
                    type = "json_schema",
                    name = schemaName,
                    schema = JsonSerializer.Deserialize<JsonElement>(jsonSchema.Schema),
                    strict = true
                };
            }

            return null;
        }

        /***************************************************/
    }
}

