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
using BH.oM.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace BH.Adapter.OpenAI
{
    public partial class OpenAIAdapter : BHoMAdapter
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        public async Task<Output<List<List<double>>, bool>> ExecuteAsync(GenerateEmbeddings command, ActionConfig actionConfig = null)
        {
            EmbeddingExecutionConfig config = actionConfig as EmbeddingExecutionConfig ?? new EmbeddingExecutionConfig();

            List<string> input = command?.Input ?? new List<string>();
            if (input.Count == 0 || input.Any(x => string.IsNullOrWhiteSpace(x)))
            {
                BH.Engine.Base.Compute.RecordError($"{nameof(GenerateEmbeddings)} needs at least one text to embed and cannot embed empty texts.");
                return new Output<List<List<double>>, bool> { Item1 = new List<List<double>>(), Item2 = false };
            }

            try
            {
                string responseString = await PostAsync("embeddings", EmbeddingsRequestBody(input, config), config.TimeoutSeconds).ConfigureAwait(false);
                return new Output<List<List<double>>, bool> { Item1 = EmbeddingVectors(responseString, input.Count), Item2 = true };
            }
            catch (Exception ex)
            {
                BH.Engine.Base.Compute.RecordError($"OpenAI command execution failed with the following exception:\n{ex.Message}");
                return new Output<List<List<double>>, bool> { Item1 = new List<List<double>>(), Item2 = false };
            }
        }


        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        private Output<List<object>, bool> RunCommand(GenerateEmbeddings command, ActionConfig actionConfig = null)
        {
            Output<List<List<double>>, bool> result = ExecuteAsync(command, actionConfig).Result;
            return new Output<List<object>, bool> { Item1 = result.Item1.Cast<object>().ToList(), Item2 = result.Item2 };
        }

        /***************************************************/

        private Dictionary<string, object> EmbeddingsRequestBody(List<string> input, EmbeddingExecutionConfig config)
        {
            Dictionary<string, object> requestBody = new Dictionary<string, object>
            {
                { "model", m_Model },
                { "input", input },
                { "encoding_format", "float" }
            };

            if (config.Dimensions > 0)
                requestBody["dimensions"] = config.Dimensions;

            return requestBody;
        }

        /***************************************************/

        private static List<List<double>> EmbeddingVectors(string responseString, int expectedCount)
        {
            JsonElement response = JsonSerializer.Deserialize<JsonElement>(responseString);
            if (!response.TryGetProperty("data", out JsonElement data) || data.ValueKind != JsonValueKind.Array)
                throw new InvalidOperationException("The embeddings response contains no data.");

            // Each item carries the index of its input, so vectors are placed by index rather than by array order
            List<double>[] vectors = new List<double>[expectedCount];
            foreach (JsonElement item in data.EnumerateArray())
            {
                int index = item.GetProperty("index").GetInt32();
                if (index < 0 || index >= expectedCount)
                    throw new InvalidOperationException($"The embeddings response contains an unexpected index {index}.");

                vectors[index] = item.GetProperty("embedding").EnumerateArray().Select(x => x.GetDouble()).ToList();
            }

            if (vectors.Any(x => x == null))
                throw new InvalidOperationException("The embeddings response is missing vectors for some of the inputs.");

            return vectors.ToList();
        }

        /***************************************************/
    }
}
