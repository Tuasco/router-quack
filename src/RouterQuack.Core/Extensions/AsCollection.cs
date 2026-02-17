using System.Diagnostics.Contracts;
using System.Text;
using Microsoft.Extensions.Logging;
using RouterQuack.Core.Models;
using RouterQuack.Core.Processors;
using RouterQuack.Core.Validators;

namespace RouterQuack.Core.Extensions;

public static class AsCollectionExtensions
{
    extension(ICollection<As> source)
    {
        /// <summary>
        /// Call the execution of a config processor of the pipeline.
        /// </summary>
        /// <param name="processor">The configProcessor to execute.</param>
        /// <returns>The modified collection of As objects. This is used to make a call chain.</returns>
        /// <exception cref="StepException">Step executed with errors.</exception>
        public ICollection<As> ExecuteStep(IProcessor processor)
        {
            LogBeginMessage(processor);
            processor.Process(source);

            if (processor.ErrorsOccurred)
                throw new StepException();

            return source;
        }

        /// <summary>
        /// Call the execution of a config processor of the pipeline.
        /// </summary>
        /// <param name="validator">The configProcessor to execute.</param>
        /// <returns>The modified collection of As objects. This is used to make a call chain.</returns>
        /// <exception cref="StepException">Step executed with errors.</exception>
        public ICollection<As> ExecuteStep(IValidator validator)
        {
            LogBeginMessage(validator);
            validator.Validate(source);

            if (validator.ErrorsOccurred)
                throw new StepException();

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

        /// <summary>
        /// Return an Enumerable of links without duplicates, validating <param name="predicate"></param>
        /// </summary>
        /// <param name="predicate">Predicate to filter-in interfaces.</param>
        /// <param name="validateBoth">Whether both interfaces of the link must validate the predicate.</param>
        /// <returns>An <see cref="Enumerable" /> of <see cref="Tuple{Interface, Interface}"/>.</returns>
        /// <exception cref="ArgumentNullException">The neighbour of an interface is null.</exception>
        [Pure]
        public IEnumerable<Tuple<Interface, Interface>> GetAllLinks(
            Func<Interface, bool>? predicate = null,
            bool validateBoth = false)

        {
            predicate ??= _ => true;

            var interfaces = source
                .SelectMany(a => a.Routers)
                .SelectMany(r => r.Interfaces)
                .Where(predicate);

            if (validateBoth)
                interfaces = interfaces.Where(i => predicate(i.Neighbour!));

            return interfaces
                .Where(FilterDuplicates)
                .Select(i => new Tuple<Interface, Interface>(i, i.Neighbour!));

            bool FilterDuplicates(Interface @interface)
            {
                // Enforce a consistent ordering to pick the link only once.
                // If the neighbour doesn't validate the predicate, then only one link is in the list so far.
                if (!validateBoth && !predicate(@interface.Neighbour!))
                    return true;

                return string.Compare(@interface.ParentRouter.Name,
                    @interface.Neighbour!.ParentRouter.Name,
                    StringComparison.Ordinal) < 0;
            }
        }
    }

    private static void LogBeginMessage(IStep step)
    {
        #pragma warning disable CA2254
        if (!string.IsNullOrWhiteSpace(step.BeginMessage))
            step.Logger.LogInformation(step.BeginMessage + "...");
        #pragma warning restore CA2254
    }
}