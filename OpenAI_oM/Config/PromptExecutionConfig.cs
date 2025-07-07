using BH.oM.Adapter;

namespace BH.oM.Adapters.OpenAI
{
    //[Description("Config to be used in each individual call to the Forge API endpoints.")]
    public class PromptExecutionConfig : ActionConfig
    {
        /***************************************************/
        /**** Properties                                ****/
        /***************************************************/

        public virtual double Temperature { get; set; } = 1.0;
        public virtual double TopP { get; set; } = 1.0;
        public virtual int MaxTokens { get; set; } = 2048;

        /***************************************************/
    }
}
