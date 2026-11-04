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
using System.Text.Json;
using System.Threading.Tasks;

namespace BH.Adapter.OpenAI
{
    public partial class OpenAIAdapter : BHoMAdapter
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        public async Task<Output<string, bool>> ExecuteAsync(GenerateChatCompletion command, ActionConfig actionConfig = null)
        {
            PromptExecutionConfig config = actionConfig as PromptExecutionConfig ?? new PromptExecutionConfig();

            return await ExecuteSafelyAsync(async () =>
            {
                string responseString = await PostAsync("chat/completions", ChatCompletionRequestBody(command, config), config.TimeoutSeconds).ConfigureAwait(false);
                return ChatCompletionText(responseString);
            }).ConfigureAwait(false);
        }


        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        private Output<List<object>, bool> RunCommand(GenerateChatCompletion command, ActionConfig actionConfig = null)
        {
            return ExecuteOutput(ExecuteAsync(command, actionConfig).Result);
        }

        /***************************************************/

        private Dictionary<string, object> ChatCompletionRequestBody(GenerateChatCompletion command, PromptExecutionConfig config)
        {
            Dictionary<string, object> requestBody = new Dictionary<string, object>
            {
                { "model", m_Model },
                { "messages", InputMessages(command.System, command.User, command.Assistant) },
                { "max_completion_tokens", config.MaxTokens }
            };

            AddSamplingParameters(requestBody, config);

            string effort = SupportedReasoningEffort(config, command);
            if (effort != null)
                requestBody["reasoning_effort"] = effort;

            object responseFormat = ChatCompletionResponseFormat(config.OutputType);
            if (responseFormat != null)
                requestBody["response_format"] = responseFormat;

            return requestBody;
        }

        /***************************************************/

        private static string ChatCompletionText(string responseString)
        {
            JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseString);
            if (!response.TryGetProperty("choices", out JsonElement choices) || choices.ValueKind != JsonValueKind.Array || choices.GetArrayLength() == 0)
                throw new InvalidOperationException("The chat completion contains no choices.");

            JsonElement choice = choices[0];
            JsonElement message = choice.GetProperty("message");
            if (message.TryGetProperty("refusal", out JsonElement refusal) && refusal.ValueKind == JsonValueKind.String)
                throw new InvalidOperationException($"The model refused to respond: {refusal.GetString()}");

            string finishReason = choice.TryGetProperty("finish_reason", out JsonElement finishReasonElement) && finishReasonElement.ValueKind == JsonValueKind.String ? finishReasonElement.GetString() : null;
            if (finishReason == "length")
                throw new InvalidOperationException($"The chat completion is incomplete because the token limit was reached. Increase {nameof(PromptExecutionConfig.MaxTokens)} or lower {nameof(PromptExecutionConfig.ReasoningEffort)} in the {nameof(PromptExecutionConfig)}.");

            if (finishReason == "content_filter")
                throw new InvalidOperationException("The chat completion was stopped by the content filter.");

            return message.TryGetProperty("content", out JsonElement content) && content.ValueKind == JsonValueKind.String ? content.GetString() : "";
        }

        /***************************************************/

        private static object ChatCompletionResponseFormat(IOutputType outputType)
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
                    json_schema = new
                    {
                        name = schemaName,
                        schema = JsonSerializer.Deserialize<JsonElement>(jsonSchema.Schema),
                        strict = true
                    }
                };
            }

            return null;
        }

        /***************************************************/
    }
}

