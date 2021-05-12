using System.IO;

namespace Common.Databases
{
    public sealed class Configuration
    {
        /// <summary>
        /// 
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public string SetupScript
        {
            get => _setupScript;
            set => _setupScript = ResolveContentOrKeepPath(value);
        }
        private string _setupScript;

        /// <summary>
        /// 
        /// </summary>
        public string TeardownScript
        {
            get => _teardownScript;
            set => _teardownScript = ResolveContentOrKeepPath(value);
        }
        private string _teardownScript;

        /// <summary>
        /// 
        /// </summary>
        public string InsertOneScript
        {
            get => _insertOneScript;
            set => _insertOneScript = ResolveContentOrKeepPath(value);
        }
        private string _insertOneScript;

        /// <summary>
        /// 
        /// </summary>
        public string InsertManyScript
        {
            get => _insertManyScript;
            set => _insertManyScript = ResolveContentOrKeepPath(value);
        }
        private string _insertManyScript;

        /// <summary>
        /// 
        /// </summary>
        public string SelectScript
        {
            get => _selectScipt;
            set => _selectScipt = ResolveContentOrKeepPath(value);
        }
        private string _selectScipt;


        /// <summary>
        /// 
        /// </summary>
        /// <param name="smth"></param>
        /// <returns></returns>
        private static string ResolveContentOrKeepPath(string smth)
            => !File.Exists(smth) ? smth : File.ReadAllText(smth)
                .Replace('\t', ' ')
                .Replace("\r", null)
                .Replace('\n', ' ');
    }
}
