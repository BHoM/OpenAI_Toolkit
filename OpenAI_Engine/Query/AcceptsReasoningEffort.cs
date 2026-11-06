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
using BH.oM.Adapters.OpenAI.Commands;
using BH.oM.Adapters.OpenAI.Enums;
using BH.oM.Base.Attributes;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text.RegularExpressions;

namespace BH.Engine.Adapters.OpenAI
{
    public static partial class Query
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        [Description("Checks whether the given model accepts the given reasoning effort when executing the given command, as the supported values differ between APIs, e.g. Max is not supported by the Chat Completions API. " + nameof(ReasoningEffort.Default) + " is always accepted, as no reasoning effort is sent to the API in that case. Models not known to the adapter are assumed not to accept any other reasoning effort.")]
        [Input("model", "Name of the model, e.g. gpt-6-luna. A trailing version date, e.g. gpt-6-luna-2026-09-22, is ignored.")]
        [Input("command", "Command to be executed with the reasoning effort, e.g. " + nameof(GenerateResponse) + " or " + nameof(GenerateChatCompletion) + ".")]
        [Input("effort", "Reasoning effort to check.")]
        [Output("accepts", "True if the model accepts the reasoning effort for the command, otherwise false.")]
        public static bool AcceptsReasoningEffort(string model, IExecuteCommand command, ReasoningEffort effort)
        {
            if (effort == ReasoningEffort.Default)
                return true;

            if (command == null)
            {
                BH.Engine.Base.Compute.RecordError("Could not check whether the reasoning effort is accepted because the command is null.");
                return false;
            }

            if (!m_SupportedReasoningEfforts.TryGetValue(ModelName(model), out HashSet<ReasoningEffort> supported))
                return false;

            if (!command.IAcceptsReasoningEffort(effort))
                return false;

            return supported.Contains(effort);
        }


        /***************************************************/
        /****              Private methods              ****/
        /***************************************************/

        private static bool IAcceptsReasoningEffort(this IExecuteCommand command, ReasoningEffort effort)
        {
            return AcceptsReasoningEffort(command as dynamic, effort);
        }

        /***************************************************/

        private static bool AcceptsReasoningEffort(GenerateChatCompletion command, ReasoningEffort effort)
        {
            return effort != ReasoningEffort.Max;
        }

        /***************************************************/

        private static bool AcceptsReasoningEffort(GenerateResponse command, ReasoningEffort effort)
        {
            return true;
        }

        /***************************************************/

        private static bool AcceptsReasoningEffort(IExecuteCommand command, ReasoningEffort effort)
        {
            return false;
        }

        /***************************************************/

        private static string ModelName(string model)
        {
            if (model == null)
                return "";

            // Strip the version date, e.g. gpt-6-luna-2026-09-22 -> gpt-6-luna
            return Regex.Replace(model.Trim().ToLowerInvariant(), @"-\d{4}-\d{2}-\d{2}$", "");
        }


        /***************************************************/
        /****              Private fields               ****/
        /***************************************************/

        private static Dictionary<string, HashSet<ReasoningEffort>> m_SupportedReasoningEfforts = new Dictionary<string, HashSet<ReasoningEffort>>
        {
            { "gpt-6-astra", new HashSet<ReasoningEffort> { ReasoningEffort.Low, ReasoningEffort.Medium, ReasoningEffort.High, ReasoningEffort.XHigh, ReasoningEffort.Max } },
            { "gpt-6-luna", new HashSet<ReasoningEffort> { ReasoningEffort.None, ReasoningEffort.Low, ReasoningEffort.Medium, ReasoningEffort.High, ReasoningEffort.XHigh, ReasoningEffort.Max } },
            { "gpt-6.1-sol", new HashSet<ReasoningEffort> { ReasoningEffort.Low, ReasoningEffort.Medium, ReasoningEffort.High, ReasoningEffort.XHigh, ReasoningEffort.Max } },
            { "gpt-6.1-terra", new HashSet<ReasoningEffort> { ReasoningEffort.None, ReasoningEffort.Low, ReasoningEffort.Medium, ReasoningEffort.High, ReasoningEffort.XHigh, ReasoningEffort.Max } }
        };

        /***************************************************/
    }
}

