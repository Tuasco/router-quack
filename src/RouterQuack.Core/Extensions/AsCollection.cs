using System.Diagnostics.Contracts;
using System.Text;
using RouterQuack.Core.Models;
using RouterQuack.Core.Steps;

namespace RouterQuack.Core.Extensions;

public static class AsCollectionExtensions
{
    extension(ICollection<As> source)
    {
        /// <summary>
        /// Call the execution of a step of the pipeline.
        /// </summary>
        /// <param name="step">The step to execute.</param>
        /// <returns>The modified collection of As objects. This is used to make a call chain.</returns>
        public ICollection<As> ExecuteStep(IStep step)
        {
            step.Execute(source);
            return source;
        }

        /// <summary>
        /// Return a multi-line summary of the As collection.
        /// </summary>
        [Pure]
        public string Summary()
        {
            var str = new StringBuilder();

            foreach (var @as in source)
                str.Append(@as).Append('\n');

            return str.ToString().TrimEnd('\n');
        }
    }
}