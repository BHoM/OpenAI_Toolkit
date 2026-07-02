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

using BH.oM.Adapters.OpenAI.Authorization;
using BH.oM.Base.Attributes;
using System.ComponentModel;

namespace BH.Engine.Adapters.OpenAI
{
    public static partial class Query
    {
        /***************************************************/
        /****             Interface methods             ****/
        /***************************************************/

        [Description("Gets the authorization code (token or API key) from the provided authorization source. If the source is null or the code cannot be extracted, an empty string is returned.")]
        [Input("source", "The authorization source from which to extract the code.")]
        [Output("code", "The extracted authorization code as a string. If extraction fails, an empty string is returned.")]
        public static string IAuthorizationCode(this IAuthorizationSource source)
        {
            if (source == null)
            {
                BH.Engine.Base.Compute.RecordError("Could not otain authorization code because authorization source is null.");
                return "";
            }

            object code = "";
            if (!BH.Engine.Base.Compute.TryRunExtensionMethod(source, nameof(AuthorizationCode), out code))
            {
                BH.Engine.Base.Compute.RecordError($"Authorisation code extraction failed for source of type {source.GetType().Name}.");
                return "";
            }

            return code as string;
        }


        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        [Description("Extracts the authorization code from the provided ApiKey object. Returns the API key as a string.")]
        [Input("source", "The ApiKey object from which to extract the authorization code.")]
        [Output("code", "The extracted API key as a string.")]
        public static string AuthorizationCode(this ApiKey source)
        {
            return source?.Key ?? "";
        }

        /***************************************************/
    }
}

