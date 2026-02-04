using System.Text;
using RouterQuack.Core.Models;
using RouterQuack.Core.Steps;

namespace RouterQuack.Core.Extensions;

public static class AsCollectionExtensions
{
    extension (ICollection<As> source)
    {
        public ICollection<As> ExecuteStep(IStep step)
        {
            step.Execute(source);
            return source;
        }

        public string Summary()
        {
            var str = new StringBuilder();

            foreach (var @as in source)
                str.Append(@as).Append('\n');
            
            return str.ToString().TrimEnd('\n');
        }
    }
}