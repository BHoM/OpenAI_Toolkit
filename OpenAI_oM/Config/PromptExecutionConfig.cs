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
using BH.oM.Adapters.OpenAI.Output;
using System.ComponentModel;

namespace BH.oM.Adapters.OpenAI
{
    [Description("Configuration for executing prompt commands in OpenAI's API, e.g. " + nameof(GenerateResponse) + " or " + nameof(GenerateChatCompletion) + ".")]
    public class PromptExecutionConfig : ActionConfig
    {
        /***************************************************/
        /****             Public properties             ****/
        /***************************************************/

        [Description("Temperature setting for the prompt execution, controlling randomness in the output. A higher value (e.g., 1.0) results in more creative responses, while a lower value (e.g., 0.1) makes the output more focused and deterministic." +
                     "\nOnly supported by some models (currently GPT-6 Astra), otherwise ignored with a warning. Leave as NaN to use the model's default.")]
        public virtual double Temperature { get; set; } = double.NaN;

        [Description("Top-p setting for the prompt execution, controlling diversity in the output. It limits the model to consider only the most probable tokens whose cumulative probability exceeds this value. A value of 1.0 means no restriction, while lower values (e.g., 0.9) restrict the output to a smaller set of tokens." +
                     "\nOnly supported by some models (currently GPT-6 Astra), otherwise ignored with a warning. Leave as NaN to use the model's default.")]
        public virtual double TopP { get; set; } = double.NaN;

        [Description("Maximum number of tokens the model can generate, including reasoning tokens that are not part of the visible output. If the limit is reached, the response is incomplete and the prompt execution fails, so leave enough headroom for reasoning.")]
        public virtual int MaxTokens { get; set; } = 16384;

        [Description("How much the model reasons before answering. Lower effort is faster and cheaper; Default leaves the choice to the model. Values not supported by the model are ignored with a warning.")]
        public virtual ReasoningEffort ReasoningEffort { get; set; } = ReasoningEffort.Default;

        [Description("Timeout in seconds for the prompt execution. This setting determines how long the system will wait for a response before timing out. Reasoning models can take noticeably longer to respond than non-reasoning ones.")]
        public virtual int TimeoutSeconds { get; set; } = 120;

        [Description("How the model response should be formatted. Defaults to plain text with no format constraint.")]
        public virtual IOutputType OutputType { get; set; } = new Text();

        /***************************************************/
    }
}

