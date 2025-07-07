using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BH.Engine.Adapters.OpenAI
{
    public static partial class Convert
    {
        public static string ToString(this List<List<object>> objects)
        {
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < objects.Count; i++)
            {
                string line = string.Join("\t", objects[i].Select(x => x.ToString()));
                sb.AppendLine(line);
            }

            return sb.ToString();
        }
    }
}
