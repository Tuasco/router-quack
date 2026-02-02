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

        public void Display()
        {
            var str = new StringBuilder();
            str.AppendLine("\n========== Parsed ASs ==========");

            foreach (var @as in source)
                str.Append(@as);
            
            Console.Write(str.ToString());
        }
    }
}