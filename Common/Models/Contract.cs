using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Common.Models
{
    public class Contract : SortedList<int, ContractProperty>
    {
        public string[] Names => this.Select(x => x.Value.Name).ToArray();
    }

    [DebuggerDisplay("{Type}({MaxLength}) {Name}")]
    public struct ContractProperty : IEquatable<ContractProperty>
    {
        public string Name { get; set; }
        public PropertyType Type { get; set; }
        public int MaxLength { get; set; }

        public bool Equals(ContractProperty other) => Name.Equals(other.Name);

        public enum PropertyType
        {
            Guid,
            Byte,
            Short,
            Int,
            Long,
            Float,
            Decimal,
            DateTime,
            String,
            Bool
        }
    }
}
