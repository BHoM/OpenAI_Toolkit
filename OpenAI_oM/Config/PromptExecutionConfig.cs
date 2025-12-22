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
using System.ComponentModel;

namespace BH.oM.Adapters.OpenAI
{
    [Description("Configuration for executing prompts in OpenAI's API.")]
    public class PromptExecutionConfig : ActionConfig
    {
        /***************************************************/
        /****             Public properties             ****/
        /***************************************************/

        [Description("Temperature setting for the prompt execution, controlling randomness in the output. A higher value (e.g., 1.0) results in more creative responses, while a lower value (e.g., 0.1) makes the output more focused and deterministic.")]
        public virtual double Temperature { get; set; } = 1.0;

        [Description("Top-p setting for the prompt execution, controlling diversity in the output. It limits the model to consider only the most probable tokens whose cumulative probability exceeds this value. A value of 1.0 means no restriction, while lower values (e.g., 0.9) restrict the output to a smaller set of tokens.")]
        public virtual double TopP { get; set; } = 1.0;

        [Description("Maximum number of tokens to generate in the response. This limits the length of the output, with a typical value being 2048 tokens. Adjusting this can help control the verbosity of the response.")]
        public virtual int MaxTokens { get; set; } = 2048;

        [Description("Timeout in seconds for the prompt execution. This setting determines how long the system will wait for a response before timing out. A typical value is 30 seconds, but it can be adjusted based on the expected response time of the API.")]
        public virtual int TimeoutSeconds { get; set; } = 30;

        /***************************************************/
    }
}

