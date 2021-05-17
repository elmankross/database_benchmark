using System;

namespace Common
{
    using Models;
    using System.Linq;

    public sealed class Sampler
    {
        /// <summary>
        /// Represents rows with their columns
        /// </summary>
        public object[][] Buffer { get; }
        private readonly Random _random;
        private const string ALPHABETIC = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";

        public Sampler(int bufferSize)
        {
            Buffer = new object[bufferSize][];
            _random = new Random();
        }

        public void FillUpWith(Contract contract)
        {
            for (var i = 0; i < Buffer.Length; i++)
            {
                var instance = Buffer[i] ??= new object[contract.Count];
                var index = 0;
                foreach (var property in contract)
                {
                    instance[index++] = GetRandomTypeValue(property.Value.Type, property.Value.MaxLength);
                }
            }
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="type"></param>
        /// <param name="maxLength"></param>
        /// <returns></returns>
        private object GetRandomTypeValue(ContractProperty.PropertyType type, int maxLength)
        {
            maxLength = maxLength == -1 ? 10 : maxLength;
            object value = null;
            byte[] buffer;
            switch (type)
            {
                case ContractProperty.PropertyType.Guid:
                    value = Guid.NewGuid();
                    break;
                case ContractProperty.PropertyType.Byte:
                    value = (byte)_random.Next(byte.MaxValue);
                    break;
                case ContractProperty.PropertyType.Short:
                    value = (short)_random.Next(short.MaxValue);
                    break;
                case ContractProperty.PropertyType.Int:
                    value = _random.Next(int.MaxValue);
                    break;
                case ContractProperty.PropertyType.Long:
                    buffer = new byte[8];
                    _random.NextBytes(buffer);
                    var @long = BitConverter.ToInt64(buffer);
                    value = Math.Abs(@long);
                    break;
                case ContractProperty.PropertyType.Float:
                    buffer = new byte[4];
                    _random.NextBytes(buffer);
                    value = BitConverter.ToSingle(buffer, 0);
                    break;
                case ContractProperty.PropertyType.Decimal:
                    value = new decimal(
                        lo: _random.Next(int.MaxValue),
                        mid: _random.Next(int.MaxValue),
                        hi: _random.Next(int.MaxValue),
                        isNegative: false,
                        scale: (byte)_random.Next(10));
                    break;
                case ContractProperty.PropertyType.DateTime:
                    value = DateTime.UtcNow;
                    break;
                case ContractProperty.PropertyType.String:
                    // FIXME: Very very VARY mem cost operation. 
                    // It needs a fast & free mem cost operation to get random string
                    var enumeration = Enumerable.Repeat(ALPHABETIC, maxLength)
                            .Select(x => x[_random.Next(ALPHABETIC.Length)])
                            .ToArray();
                    value = new string(enumeration);
                    break;
                case ContractProperty.PropertyType.Bool:
                    value = _random.Next(2) == 1;
                    break;
            }

            return value;
        }
    }
}
