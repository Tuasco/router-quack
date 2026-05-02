using System.Diagnostics.Contracts;

namespace RouterQuack.IO.Yaml.Extensions;

public static class VrfDict
{
    extension(IDictionary<string, Vrf>? source)
    {
        /// <summary>
        /// Return an <see cref="IEnumerable{VRF}"/> with populated <see cref="Vrf.Name"/>.
        /// </summary>
        [Pure]
        public IEnumerable<Vrf> ToEnumerable()
        {
            return source?.Select(PopulateName) ?? [];

            Vrf PopulateName(KeyValuePair<string, Vrf> pair)
            {
                pair.Value.Name = pair.Key;
                return pair.Value;
            }
        }
    }
}