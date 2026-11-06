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

using System.ComponentModel;

namespace BH.oM.Adapters.OpenAI.Enums
{
    [Description("How much a reasoning model thinks before it answers. Higher effort generally improves quality on complex tasks at the cost of latency and output tokens. Supported values vary by model.")]
    public enum ReasoningEffort
    {
        [Description("No reasoning effort is sent to the API, so the model's own default applies.")]
        Default,

        [Description("No reasoning: fastest and cheapest, suitable for simple lookups and classification. Not supported by models that always reason, e.g. GPT-6.1 Sol and GPT-6 Astra.")]
        None,

        [Description("Low reasoning effort.")]
        Low,

        [Description("Medium reasoning effort.")]
        Medium,

        [Description("High reasoning effort.")]
        High,

        [Description("Extra high reasoning effort. Supported only by some models.")]
        XHigh,

        [Description("Maximum reasoning effort. Supported only by some models, and not with the Chat Completions API.")]
        Max
    }
}

