using System.Text;
using RouterQuack.Models;
using RouterQuack.Steps;

namespace RouterQuack.Extensions;

public static class _
{
    extension (ICollection<As> source)
    {
        public IEnumerable<As> ExecuteStep(IStep step)
        {
            step.Execute(source);
            return source;
        }

        // TODO Actually override ToString()
        public string ShowASs()
        {
            var str = new StringBuilder();

            foreach (var @as in source)
                str.Append(@as);
            
            return str.ToString();
        }
    }
}