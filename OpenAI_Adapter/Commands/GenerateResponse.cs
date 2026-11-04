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

using BH.oM.Adapter;
using BH.oM.Adapters.OpenAI;
using BH.oM.Adapters.OpenAI.Commands;
using BH.oM.Adapters.OpenAI.Output;
using BH.oM.Base;
using System;
using System.Collections.Generic;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BH.Adapter.OpenAI
{
    public partial class OpenAIAdapter : BHoMAdapter
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        public async Task<Output<string, bool>> ExecuteAsync(GenerateResponse command, ActionConfig actionConfig = null)
        {
            PromptExecutionConfig config = actionConfig as PromptExecutionConfig ?? new PromptExecutionConfig();

            return await ExecuteSafelyAsync(async () =>
            {
                string responseString = await PostAsync("responses", ResponseRequestBody(command, config), config.TimeoutSeconds).ConfigureAwait(false);
                return ResponseText(responseString);
            }).ConfigureAwait(false);
        }


        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        private Output<List<object>, bool> RunCommand(GenerateResponse command, ActionConfig actionConfig = null)
        {
            return ExecuteOutput(ExecuteAsync(command, actionConfig).Result);
        }

        /***************************************************/

        private Dictionary<string, object> ResponseRequestBody(GenerateResponse command, PromptExecutionConfig config)
        {
            Dictionary<string, object> requestBody = new Dictionary<string, object>
            {
                { "model", m_Model },
                { "input", InputMessages(command.System, command.User, command.Assistant) },
                { "max_output_tokens", config.MaxTokens },
                { "store", false }
            };

            AddSamplingParameters(requestBody, config);

            string effort = SupportedReasoningEffort(config, command);
            if (effort != null)
                requestBody["reasoning"] = new { effort = effort };

            object textFormat = ResponseTextFormat(config.OutputType);
            if (textFormat != null)
                requestBody["text"] = new { format = textFormat };

            return requestBody;
        }

        /***************************************************/

        private static string ResponseText(string responseString)
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

        private static object ResponseTextFormat(IOutputType outputType)
        {
            if (outputType == null || outputType is Text)
                return null;

            if (outputType is JsonObject)
                return new { type = "json_object" };

            if (outputType is JsonSchema jsonSchema)
            {
                // The Responses API expects the schema definition flattened into the format object
                Dictionary<string, object> format = JsonSchemaDefinition(jsonSchema);
                if (format != null)
                    format["type"] = "json_schema";

                return format;
            }

            return null;
        }

        /***************************************************/
    }
}

