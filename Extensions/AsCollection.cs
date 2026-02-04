using System.Text;
using RouterQuack.Models;
using RouterQuack.Steps;

namespace RouterQuack.Extensions;

public static class _
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