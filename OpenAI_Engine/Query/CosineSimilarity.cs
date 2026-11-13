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

using BH.oM.Base.Attributes;
using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace BH.Engine.Adapters.OpenAI
{
    public static partial class Query
    {
        /***************************************************/
        /****              Public methods               ****/
        /***************************************************/

        [Description("Computes the cosine similarity of two embedding vectors, i.e. how close in meaning the two embedded texts are.")]
        [Input("vector1", "First embedding vector.")]
        [Input("vector2", "Second embedding vector, generated with the same model and number of dimensions as the first one.")]
        [Output("similarity", "Cosine similarity between -1 and 1, higher meaning more similar. NaN if the vectors are null, empty, of different lengths or all zeros.")]
        public static double CosineSimilarity(this List<double> vector1, List<double> vector2)
        {
            if (vector1 == null || vector2 == null || vector1.Count == 0 || vector1.Count != vector2.Count)
                return double.NaN;

            double dot = 0, norm1 = 0, norm2 = 0;
            for (int i = 0; i < vector1.Count; i++)
            {
                dot += vector1[i] * vector2[i];
                norm1 += vector1[i] * vector1[i];
                norm2 += vector2[i] * vector2[i];
            }

            if (norm1 == 0 || norm2 == 0)
                return double.NaN;

            return dot / (Math.Sqrt(norm1) * Math.Sqrt(norm2));
        }

        /***************************************************/
    }
}
